'XFL 批68/69/71：任务栏图标长按拖动换位
'
'交互设计：
'   · 在任务栏图标上按住约 350ms（长按）→ 进入拖动状态
'   · 拖动中：拖影是按下瞬间的渲染快照（RenderTargetBitmap），跟随鼠标；
'     被拖按钮本体隐藏，其余图标按鼠标位置实时让位（瞬时重排，稳定优先）
'   · 松手：拖影滑进目标槽位并淡出，落位并保存顺序
'   · 短按（未达长按阈值就松开）→ 正常切页，行为完全不变
'
'实现要点：
'   ① 拖影用 RenderTargetBitmap 快照 —— 与实时按钮完全解耦，本体可以安全隐藏，
'      不会出现「本体 + 拖影」双份渲染的乱闪；快照失败时退回 VisualBrush 克隆；
'   ② 插入下标只统计「其它按钮」的中心，排除被拖按钮自身 ——
'      原实现把自己也算进去，移除/重插后自身中心改变，下标会在相邻两值间反复跳
'      （即用户看到的「位置会被随机更改」）；
'   ③ 中心位置按 StackPanel 布局纯推算（LayoutCenterX），不受 RenderTransform 偏移影响；
'   ④ 不再对真实按钮做 FLIP 滑动 —— RenderTransform 会与 StackPanel 自身布局相互干扰，
'      且 AniStop 不会还原属性，未完成的滑动会留下渲染偏移并污染下标测量，
'      导致位置错乱与图标乱闪；改为瞬时重排，滑动手感由拖影的拿起/落位动画承担；
'   ⑤ 进入拖动即把鼠标捕获到 PanTaskbarStack，抬起与失去捕获都在 Stack 上收尾 ——
'      原实现 MouseUp 挂在按钮上，拖动中按钮被重新插入后光标下的元素改变、
'      或鼠标移出任务栏，就收不到 MouseUp，拖影残留在界面上。
Friend Module ModXflTaskbarDrag

    Private Const Tag As String = "[XFL][任务栏排序]"

    '默认顺序（用户指定：启动/下载/horizon/登录/网页/音乐/设置；联机放在设置前；2/4 为隐藏项放末尾）
    Private ReadOnly DefaultOrder As Integer() = {0, 1, 14, 10, 11, 12, 13, 3, 2, 4}

    '长按阈值（毫秒）
    Private Const HoldMs As Integer = 350

    '拖动状态
    Private _hooked As Boolean = False
    Private _pressedBtn As MyRadioButton = Nothing
    Private _dragging As Boolean = False
    Private _suppressClick As Boolean = False
    Private _dragGhost As Border = Nothing
    Private _ghostLayer As Canvas = Nothing
    Private _holdTimer As System.Windows.Threading.DispatcherTimer = Nothing
    'XFL 批71★：自检期间屏蔽真实鼠标事件（自检会 CaptureMouse，真实光标一动就会
    '   带着 DragMoveTo 到处乱插，把自动化检查的结果污染成随机值）
    Private _testing As Boolean = False
    'XFL 批72★★：被隐藏本体的登记表。收尾路径只要有一条没把可见性还回去
    '   （落位动画被下一次长按打断、异常中断…），按钮就永久隐身；而且下次长按时
    '   RenderTargetBitmap 只会截到一张全透明的图 —— 界面上什么都不剩。
    '   用表而不是单槽：连续快速拖动时可能同时有多个「待恢复」。
    Private _hiddenBtns As New List(Of MyRadioButton)
    'XFL 批72：自检的第二阶段（落位动画结束后再核对本体是否恢复可见）
    Private _selfTestTarget As MyRadioButton = Nothing
    Private _selfTestSavedOrder As String = Nothing
    Private _selfTestPending As Boolean = False
    'XFL 批73：本次拖动里快照是怎么拿到的（写进日志，方便定位「拖动时看不见图标」）
    Private _snapHow As String = ""

    ''' <summary>挂接任务栏拖拽（幂等，InitXflTaskbar 之后调用）。</summary>
    Friend Sub Hook()
        Try
            If _hooked Then Return
            If FrmMain Is Nothing OrElse FrmMain.PanTaskbarStack Is Nothing Then Return
            _hooked = True

            '长按计时器
            _holdTimer = New System.Windows.Threading.DispatcherTimer() With {.Interval = TimeSpan.FromMilliseconds(HoldMs)}
            AddHandler _holdTimer.Tick, AddressOf HoldElapsed

            '按下/抬起/点击挂在每个按钮上（要知道按的是谁）；
            '移动与抬起同时挂在 StackPanel 上 —— 进入拖动后鼠标被捕获到 StackPanel，
            '事件只会送到那里，挂在按钮上收不到。
            For Each btn As MyRadioButton In FrmMain.PanTaskbarStack.Children.OfType(Of MyRadioButton)()
                AddHandler btn.PreviewMouseLeftButtonDown, AddressOf Btn_MouseDown
                AddHandler btn.PreviewMouseLeftButtonUp, AddressOf Btn_MouseUp
                AddHandler btn.PreviewClick, AddressOf Btn_PreviewClick
            Next
            AddHandler FrmMain.PanTaskbarStack.MouseMove, AddressOf Stack_MouseMove
            AddHandler FrmMain.PanTaskbarStack.PreviewMouseLeftButtonUp, AddressOf Stack_MouseUp
            AddHandler FrmMain.PanTaskbarStack.LostMouseCapture, AddressOf Stack_LostCapture

            '恢复上次保存的顺序
            ApplySavedOrder()
            Logger.Info($"{Tag} 已挂接，按钮数={FrmMain.PanTaskbarStack.Children.OfType(Of MyRadioButton)().Count()}")
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 挂接失败")
        End Try
    End Sub

    '===== 顺序持久化 =====
    Private Function OrderToText(order As IEnumerable(Of Integer)) As String
        Try
            Return String.Join(",", order)
        Catch
            Return ""
        End Try
    End Function

    Private Function ParseOrder(text As String) As List(Of Integer)
        Dim r As New List(Of Integer)
        Try
            If String.IsNullOrWhiteSpace(text) Then Return r
            For Each s In text.Split(","c)
                Dim v As Integer
                If Integer.TryParse(s.Trim, v) Then r.Add(v)
            Next
        Catch
        End Try
        Return r
    End Function

    ''' <summary>把当前顺序保存到设置。</summary>
    Private Sub SaveOrder()
        Try
            Dim order As New List(Of Integer)
            For Each btn As MyRadioButton In FrmMain.PanTaskbarStack.Children.OfType(Of MyRadioButton)()
                Dim tagv As Integer
                If Integer.TryParse(If(btn.Tag, "").ToString, tagv) Then order.Add(tagv)
            Next
            Settings.Set("XflTaskbarOrder", OrderToText(order))
            Logger.Info($"{Tag} 已保存顺序：{OrderToText(order)}")
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 保存顺序失败")
        End Try
    End Sub

    ''' <summary>按保存的顺序重排（缺的补在后面，多的忽略）。</summary>
    Friend Sub ApplySavedOrder()
        Try
            Dim rawOrder As String = ""
            Try : rawOrder = Settings.Get(Of String)("XflTaskbarOrder") : Catch : End Try
            Dim saved As List(Of Integer) = ParseOrder(rawOrder)
            Dim order As List(Of Integer) = If(saved.Count > 0, saved, DefaultOrder.ToList())
            ReorderByTags(order)
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 应用顺序失败")
        End Try
    End Sub

    ''' <summary>按 Tag 序列重排 StackPanel 里的按钮。未列出的按钮按原相对顺序追加到末尾。</summary>
    Private Sub ReorderByTags(order As List(Of Integer))
        Try
            If FrmMain Is Nothing OrElse FrmMain.PanTaskbarStack Is Nothing Then Return
            Dim pool As New List(Of MyRadioButton)
            For Each b As MyRadioButton In FrmMain.PanTaskbarStack.Children.OfType(Of MyRadioButton)()
                pool.Add(b)
            Next
            If pool.Count = 0 Then Return
            Dim ordered As New List(Of MyRadioButton)
            For Each t In order
                Dim hit As MyRadioButton = pool.FirstOrDefault(Function(b) Val(If(b.Tag, "").ToString) = t)
                If hit IsNot Nothing AndAlso Not ordered.Contains(hit) Then ordered.Add(hit)
            Next
            For Each b In pool
                If Not ordered.Contains(b) Then ordered.Add(b)
            Next
            '已一致就不动（避免无谓的视觉抖动）
            Dim same As Boolean = True
            For i As Integer = 0 To ordered.Count - 1
                If i >= FrmMain.PanTaskbarStack.Children.Count OrElse
                   Not ReferenceEquals(FrmMain.PanTaskbarStack.Children(i), ordered(i)) Then
                    same = False
                    Exit For
                End If
            Next
            If same Then Return
            FrmMain.PanTaskbarStack.Children.Clear()
            For Each b In ordered
                FrmMain.PanTaskbarStack.Children.Add(b)
            Next
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 重排失败")
        End Try
    End Sub

    '===== 按下 / 长按 =====
    Private Sub Btn_MouseDown(sender As Object, e As MouseButtonEventArgs)
        Try
            Dim btn As MyRadioButton = TryCast(sender, MyRadioButton)
            If btn Is Nothing Then Return
            _pressedBtn = btn
            _dragging = False
            _suppressClick = False
            If _holdTimer IsNot Nothing Then
                _holdTimer.Stop()
                _holdTimer.Start()
            End If
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 按下处理失败")
        End Try
    End Sub

    ''' <summary>长按到点：进入拖动状态。</summary>
    Private Sub HoldElapsed(sender As Object, e As EventArgs)
        Try
            If _holdTimer IsNot Nothing Then _holdTimer.Stop()
            If _pressedBtn Is Nothing OrElse _dragging Then Return
            _dragging = True
            _suppressClick = True
            '★ 关键：把鼠标捕获到 StackPanel —— 之后按钮被移除/重插、或光标移出任务栏，
            '   移动与抬起事件仍然会送到这里（否则收不到 MouseUp，拖影会残留）。
            FrmMain.PanTaskbarStack.CaptureMouse()
            'XFL 批71：清掉历史拖动可能残留的渲染偏移（旧 FLIP 实现会在按钮上留下
            '   TranslateTransform，污染后续位置测量 → 位置错乱）
            ClearSlideTransforms()
            'XFL 批72★★：快照前必须先把本体恢复可见 —— 上一次拖动若因落位动画被打断
            '   （松手后 220ms 内又按住别的图标）没能恢复，本体还停在 Opacity=0，
            '   RenderTargetBitmap 只会截到全透明的图，拖影也就完全看不见。
            RestorePressedBtn()
            CreateGhost(_pressedBtn)
            'XFL 批71：本体隐藏（Opacity=0，不改变布局、不占位跳变），拖动中只渲染快照拖影，
            '   避免「本体 + 拖影」双份渲染造成的图标乱闪；仅快照成功（ImageBrush）时隐藏，
            '   VisualBrush 兜底路径仍引用实时按钮，隐藏会让拖影消失
            Try
                If _dragGhost IsNot Nothing AndAlso TypeOf _dragGhost.Background Is ImageBrush Then
                    HideBtn(_pressedBtn)
                End If
            Catch
            End Try
            'XFL 批70：Windows 同款「拿起」动画 —— 拖影从 1.0 弹性放大到 1.08，
            '  同时透明度从 0 快速升到 0.92（有拿起的手感）
            Try
                Dim ghostScale As New Windows.Media.ScaleTransform() With {.ScaleX = 1.0, .ScaleY = 1.0}
                _dragGhost.RenderTransform = ghostScale
                _dragGhost.RenderTransformOrigin = New Point(0.5, 0.5)
                _dragGhost.Opacity = 0
                'XFL 批71★：AaDouble 的 lambda 收到的是 0→Value 的绝对插值。
                '   写死 ghostScale.ScaleX = v 会让拖影从 0 倍「涨」起来（先消失再胀开）；
                '   必须写成 基准 + 幅度 * v，才是从 1.0 弹到 1.08。
                '   （注：注释不能写在 AniStart({ ... }) 的数组字面量里面，会截断续行）
                AniStart({
                    AaDouble(Sub(v) ghostScale.ScaleX = 1.0 + 0.08 * v, 1.0, 170, 0, New AniEaseOutBack(AniEasePower.Weak)),
                    AaDouble(Sub(v) ghostScale.ScaleY = 1.0 + 0.08 * v, 1.0, 170, 0, New AniEaseOutBack(AniEasePower.Weak)),
                    AaOpacity(_dragGhost, 0.92, 120)
                }, "TaskbarDragGhost")
            Catch
                _dragGhost.Opacity = 0.92
            End Try
            'XFL 批72★：淡入保险丝 —— 300ms 后拖影仍不可见就强制恢复并留日志。
            '   正常情况拿起动画 120ms 已把透明度补到 0.92，这里只是兜底。
            Try
                Dim guard As New System.Windows.Threading.DispatcherTimer(Windows.Threading.DispatcherPriority.Background) With {
                    .Interval = TimeSpan.FromMilliseconds(300)}
                AddHandler guard.Tick, AddressOf GhostFadeGuard
                guard.Start()
            Catch
            End Try
            Logger.Info($"{Tag} 进入拖动：{_pressedBtn.Name} 快照={If(_dragGhost Is Nothing, "无", _dragGhost.Background.GetType().Name)} alpha={SampleGhostAlpha().ToString("0.000")} 本体隐藏={_hiddenBtns.Count} 来源={_snapHow}")
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 进入拖动失败")
        End Try
    End Sub

    'XFL 批72★：拿起淡入的保险丝 —— 300ms 后拖影仍不可见就强制恢复。
    Private Sub GhostFadeGuard(sender As Object, e As EventArgs)
        Try
            Dim t As System.Windows.Threading.DispatcherTimer = TryCast(sender, System.Windows.Threading.DispatcherTimer)
            If t IsNot Nothing Then t.Stop()
            If _dragGhost IsNot Nothing AndAlso _dragGhost.Opacity < 0.5 Then
                _dragGhost.Opacity = 0.92
                Logger.Warn($"{Tag} 拖影淡入未生效，已强制恢复可见")
            End If
        Catch
        End Try
    End Sub

    ''' <summary>XFL 批72：采样拖影快照的不透明像素占比 —— 0 表示拖影完全透明（拖动时看不见图标）。</summary>
    Private Function SampleGhostAlpha() As Double
        Try
            If _dragGhost Is Nothing Then Return -1
            Dim ib As ImageBrush = TryCast(_dragGhost.Background, ImageBrush)
            If ib Is Nothing Then Return -2
            '注意：源可能是 CroppedBitmap（整窗裁剪），所以按 BitmapSource 取，不能只认 RenderTargetBitmap
            Dim bs As BitmapSource = TryCast(ib.ImageSource, BitmapSource)
            If bs Is Nothing Then Return -3
            Dim w As Integer = bs.PixelWidth
            Dim h As Integer = bs.PixelHeight
            If w * h = 0 Then Return -5
            Dim bytes As Byte() = New Byte(w * h * 4 - 1) {}
            bs.CopyPixels(bytes, w * 4, 0)
            Dim hit As Integer = 0
            For i As Integer = 3 To bytes.Length - 1 Step 4
                If bytes(i) > 16 Then hit += 1
            Next
            Return hit / (w * h)
        Catch
            Return -4
        End Try
    End Function

    '===== 拖影 =====
    ''' <summary>找最靠近窗口的顶层 Grid 作为浮层宿主（拖影不被任务栏 60px 高度裁掉）。</summary>
    Private Function TopGrid() As Grid
        Try
            Dim cur As DependencyObject = FrmMain.PanTaskbarStack
            Dim best As Grid = Nothing
            While cur IsNot Nothing
                Dim g As Grid = TryCast(cur, Grid)
                If g IsNot Nothing Then best = g
                cur = VisualTreeHelper.GetParent(cur)
            End While
            Return best
        Catch
            Return Nothing
        End Try
    End Function

    ''' <summary>XFL 批72：位图里是否存在不透明像素（全透明 = 拖动时看不见图标）。</summary>
    Private Function BitmapHasPixels(rtb As RenderTargetBitmap) As Boolean
        Try
            If rtb Is Nothing Then Return False
            Dim w As Integer = rtb.PixelWidth
            Dim h As Integer = rtb.PixelHeight
            If w * h = 0 Then Return False
            Dim bytes As Byte() = New Byte(w * h * 4 - 1) {}
            rtb.CopyPixels(bytes, w * 4, 0)
            For i As Integer = 3 To bytes.Length - 1 Step 4
                If bytes(i) > 16 Then Return True
            Next
        Catch
        End Try
        Return False
    End Function

    ''' <summary>XFL 批73：把按钮渲染成位图快照。
    '''   ①直接渲染按钮（屏幕 DPI） ②直接渲染按钮（96 DPI） ③渲染最近的顶层 Grid 后裁剪；
    '''   全部拿不到像素就返回 Nothing，由调用方退回 VisualBrush。</summary>
    Private Function TrySnapshot(btn As MyRadioButton, ByRef how As String) As Brush
        Dim dpi As Double = 96.0
        Try
            Dim ps As PresentationSource = PresentationSource.FromVisual(btn)
            If ps IsNot Nothing AndAlso ps.CompositionTarget IsNot Nothing Then dpi = ps.CompositionTarget.TransformToDevice.M11 * 96.0
        Catch
        End Try
        If dpi <= 0 Then dpi = 96.0
        If btn.ActualWidth <= 0 OrElse btn.ActualHeight <= 0 Then Return Nothing

        '① 直接渲染按钮（屏幕 DPI）
        Try
            Dim w As Integer = CInt(Math.Round(btn.ActualWidth * dpi / 96.0))
            Dim h As Integer = CInt(Math.Round(btn.ActualHeight * dpi / 96.0))
            Dim rtb As New RenderTargetBitmap(w, h, dpi, dpi, PixelFormats.Pbgra32)
            rtb.Render(btn)
            If BitmapHasPixels(rtb) Then
                how = "快照(直接 " & w & "x" & h & "@" & CInt(dpi) & ")"
                Return New ImageBrush(rtb) With {.Stretch = Stretch.Fill}
            End If
        Catch
        End Try

        '② 直接渲染按钮（96 DPI）—— 某些 DPI/显卡组合下高 DPI 目标会被整片丢弃
        Try
            Dim w2 As Integer = CInt(Math.Round(btn.ActualWidth))
            Dim h2 As Integer = CInt(Math.Round(btn.ActualHeight))
            Dim rtb2 As New RenderTargetBitmap(w2, h2, 96, 96, PixelFormats.Pbgra32)
            rtb2.Render(btn)
            If BitmapHasPixels(rtb2) Then
                how = "快照(96DPI " & w2 & "x" & h2 & ")"
                Return New ImageBrush(rtb2) With {.Stretch = Stretch.Fill}
            End If
        Catch
        End Try

        '③ 渲染最近的顶层 Grid，再按按钮在其中的矩形裁剪
        Try
            Dim host As Grid = TopGrid()
            If host IsNot Nothing AndAlso host.ActualWidth > 1 AndAlso host.ActualHeight > 1 Then
                Dim hw As Integer = CInt(Math.Round(host.ActualWidth * dpi / 96.0))
                Dim hh As Integer = CInt(Math.Round(host.ActualHeight * dpi / 96.0))
                If hw > 0 AndAlso hh > 0 AndAlso hw * hh <= 60000000 Then
                    Dim tick0 As Integer = Environment.TickCount
                    Dim rtb3 As New RenderTargetBitmap(hw, hh, dpi, dpi, PixelFormats.Pbgra32)
                    rtb3.Render(host)
                    If BitmapHasPixels(rtb3) Then
                        Dim r As Rect = btn.TransformToAncestor(host).TransformBounds(New Rect(0, 0, btn.ActualWidth, btn.ActualHeight))
                        'XFL 批73：RenderTargetBitmap.Render(根视觉) 会把根自身的窗口内偏移一起画进位图（本机 host 边距 10px → 内容整体偏移 10,10），
                        '   裁剪矩形必须补上同一偏移，否则拖影里的图标会缩到右下角（用户报告的「大小位置缩放不对」）。
                        Dim offX As Double = 0, offY As Double = 0
                        Try
                            Dim win As Window = Window.GetWindow(host)
                            If win IsNot Nothing Then
                                Dim off As Point = host.TranslatePoint(New Point(0, 0), win)
                                offX = off.X : offY = off.Y
                            End If
                        Catch
                        End Try
                        Dim cx As Integer = CInt(Math.Round((r.X + offX) * dpi / 96.0))
                        Dim cy As Integer = CInt(Math.Round((r.Y + offY) * dpi / 96.0))
                        Dim cw As Integer = CInt(Math.Round(r.Width * dpi / 96.0))
                        Dim ch As Integer = CInt(Math.Round(r.Height * dpi / 96.0))
                        If cx < 0 Then cx = 0
                        If cy < 0 Then cy = 0
                        If cx > hw - 1 Then cx = hw - 1
                        If cy > hh - 1 Then cy = hh - 1
                        If cw < 1 Then cw = 1
                        If ch < 1 Then ch = 1
                        If cx + cw > hw Then cw = hw - cx
                        If cy + ch > hh Then ch = hh - cy
                        Dim cb As New Windows.Media.Imaging.CroppedBitmap(rtb3, New Windows.Int32Rect(cx, cy, cw, ch))
                        how = "快照(整窗裁剪 " & cw & "x" & ch & "," & (Environment.TickCount - tick0) & "ms)"
                        Dim diag As String = "dpi=" & dpi.ToString("0.#") & " host=" & host.ActualWidth.ToString("0.#") & "x" & host.ActualHeight.ToString("0.#") & " rtb=" & hw & "x" & hh & " rect=" & r.X.ToString("0.#") & "," & r.Y.ToString("0.#") & "," & r.Width.ToString("0.#") & "," & r.Height.ToString("0.#") & " off=" & offX.ToString("0.#") & "," & offY.ToString("0.#") & " btn=" & btn.ActualWidth.ToString("0.#") & "x" & btn.ActualHeight.ToString("0.#")
                        Logger.Info($"{Tag} 裁剪诊断: {diag}")
                        Return New ImageBrush(cb) With {.Stretch = Stretch.Fill}
                    End If
                End If
            End If
        Catch
        End Try

        Return Nothing
    End Function

    Private Sub CreateGhost(btn As MyRadioButton)
        Try
            Dim host As Grid = TopGrid()
            If host Is Nothing Then Return
            Dim layer As New Canvas()
            host.Children.Add(layer)
            Panel.SetZIndex(layer, 999)
            layer.IsHitTestVisible = False
            _ghostLayer = layer

            'XFL 批71：拖影 = 按下瞬间的渲染快照（RenderTargetBitmap）—— 与实时按钮完全
            '   解耦，本体可以安全隐藏，不会再出现「本体 + 拖影」双份渲染的乱闪；
            '   快照失败（如按钮尚未完成首帧渲染）时退回 VisualBrush 实时克隆。
            'XFL 批73★★：批71 直接把 RenderTargetBitmap(按钮) 当拖影，但实测在部分
            '   显卡/DPI 组合下它渲染出的是**全透明**位图。透明图照样算成功、本体照样
            '   被隐藏 —— 拖动时界面上什么都不剩（用户反馈「被按下拖动的图标没有显示」）。
            '   现在按「直接渲染 → 96DPI 重试 → 整窗口渲染再裁剪」逐级尝试，
            '   全透明一律视为失败，最终退回引用实时按钮的 VisualBrush（不隐藏本体）。
            Dim brush As Brush = Nothing
            _snapHow = "无"
            Try
                brush = TrySnapshot(btn, _snapHow)
            Catch
                brush = Nothing
            End Try
            If brush Is Nothing Then
                Dim vb As New Windows.Media.VisualBrush() With {
                    .Visual = btn,
                    .Stretch = Windows.Media.Stretch.None,
                    .AlignmentX = Windows.Media.AlignmentX.Left,
                    .AlignmentY = Windows.Media.AlignmentY.Top}
                brush = vb
                _snapHow = "VisualBrush(实时引用，本体不能隐藏)"
            End If
            Dim ghost As New Border() With {
                .Width = btn.ActualWidth,
                .Height = btn.ActualHeight,
                .CornerRadius = New CornerRadius(13),
                .Opacity = 0.92,
                .Background = brush,
                .IsHitTestVisible = False}
            ghost.Effect = New Windows.Media.Effects.DropShadowEffect() With {
                .BlurRadius = 16, .ShadowDepth = 3, .Direction = 270, .Opacity = 0.4}
            layer.Children.Add(ghost)
            _dragGhost = ghost

            '先放到按钮当前位置，避免第一帧闪现在左上角
            Dim p0 As Point = btn.TranslatePoint(New Point(btn.ActualWidth / 2, btn.ActualHeight / 2), layer)
            Canvas.SetLeft(ghost, p0.X - ghost.Width / 2)
            Canvas.SetTop(ghost, p0.Y - ghost.Height / 2)
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 创建拖影失败")
        End Try
    End Sub

    Private Sub MoveGhostTo(stackPoint As Point)
        Try
            If _dragGhost Is Nothing OrElse _ghostLayer Is Nothing Then Return
            Dim p As Point = FrmMain.PanTaskbarStack.TranslatePoint(stackPoint, _ghostLayer)
            Canvas.SetLeft(_dragGhost, p.X - _dragGhost.Width / 2)
            Canvas.SetTop(_dragGhost, p.Y - _dragGhost.Height / 2)
        Catch
        End Try
    End Sub

    Private Sub DestroyGhost()
        Try
            If _dragGhost IsNot Nothing AndAlso _ghostLayer IsNot Nothing Then
                _ghostLayer.Children.Remove(_dragGhost)
            End If
            If _ghostLayer IsNot Nothing Then
                Dim parent As DependencyObject = VisualTreeHelper.GetParent(_ghostLayer)
                If TypeOf parent Is Grid Then DirectCast(parent, Grid).Children.Remove(_ghostLayer)
            End If
        Catch
        End Try
        _dragGhost = Nothing
        _ghostLayer = Nothing
        'XFL 批72★：兜底 —— 拖影销毁时，登记表里的隐身本体必须一起还回来
        RestoreAllHidden()
    End Sub

    '===== 拖动过程 =====
    Private Sub Stack_MouseMove(sender As Object, e As MouseEventArgs)
        Try
            If Not _dragging OrElse _pressedBtn Is Nothing Then Return
            If _testing Then Return
            DragMoveTo(e.GetPosition(FrmMain.PanTaskbarStack))
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 拖动移动失败")
        End Try
    End Sub

    ''' <summary>按 stack 内坐标移动拖影并重排（Stack_MouseMove 与 SelfTest 共用）。</summary>
    Private Sub DragMoveTo(p As Point)
        'XFL 批73：竖直约束 —— 拖影只允许在任务栏这一行里移动。
        '   纵向锁死在任务栏中线上：鼠标上下跑出任务栏时拖影不会跟着飞出去（用户报告的显示 bug）。
        '   横向钳制在任务栏范围内，且「插入下标」用同一个钳制后的坐标，
        '   保证看得见的位置与实际换位位置永远一致。
        Dim halfW As Double = If(_pressedBtn IsNot Nothing, _pressedBtn.ActualWidth / 2, 0)
        Dim stackW As Double = FrmMain.PanTaskbarStack.ActualWidth
        Dim stackH As Double = FrmMain.PanTaskbarStack.ActualHeight
        If stackH > 0 Then p = New Point(p.X, stackH / 2)
        If stackW > 0 AndAlso halfW > 0 Then
            p = New Point(Math.Max(halfW, Math.Min(stackW - halfW, p.X)), p.Y)
        End If
        MoveGhostTo(p)
        Dim target As Integer = InsertIndexFor(p.X)
        Dim cur As Integer = FrmMain.PanTaskbarStack.Children.IndexOf(_pressedBtn)
        If target >= 0 AndAlso cur >= 0 AndAlso target <> cur Then
            'XFL 批71：瞬时重排 —— 不再对真实按钮做 FLIP 滑动。
            '   RenderTransform 会与 StackPanel 自身布局相互干扰，且 AniStop 不还原属性，
            '   未完成的滑动会留下渲染偏移并污染下标测量（位置错乱、图标乱闪的根源）。
            '   滑动手感由拖影的拿起/落位动画承担，重排本身瞬时完成，位置永远等于布局位置。
            FrmMain.PanTaskbarStack.Children.Remove(_pressedBtn)
            If target > FrmMain.PanTaskbarStack.Children.Count Then target = FrmMain.PanTaskbarStack.Children.Count
            FrmMain.PanTaskbarStack.Children.Insert(target, _pressedBtn)
            FrmMain.PanTaskbarStack.UpdateLayout()
        End If
    End Sub

    ''' <summary>
    ''' 按 StackPanel 布局推算某按钮中心 X（纯布局坐标，不受 RenderTransform 偏移影响）。
    ''' 任何一步测量失败返回 -1。
    ''' </summary>
    Private Function LayoutCenterX(btn As MyRadioButton) As Double
        Try
            Dim idx As Integer = FrmMain.PanTaskbarStack.Children.IndexOf(btn)
            If idx < 0 Then Return -1
            Dim x As Double = 0
            For i As Integer = 0 To idx - 1
                Dim c As UIElement = FrmMain.PanTaskbarStack.Children(i)
                If c.Visibility <> Visibility.Visible Then Continue For
                Dim mb As FrameworkElement = TryCast(c, FrameworkElement)
                If mb Is Nothing Then Continue For
                x += mb.ActualWidth + mb.Margin.Left + mb.Margin.Right
            Next
            'XFL 批71★：必须加上自身的 Margin.Left —— 否则算出的中心比真实布局位置恒定偏左
            '   一个左边距（11px），换位阈值整体偏移，手感发涩。
            Return x + btn.Margin.Left + btn.ActualWidth / 2
        Catch
            Return -1
        End Try
    End Function

    ''' <summary>
    ''' 按鼠标 X 求插入下标。
    ''' ★ 只统计「其它按钮」的中心，排除被拖按钮自身 ——
    '''   若把自己也算进去，移除/重插后自身中心改变，下标会在相邻两值之间反复跳
    '''   （即用户看到的「位置会被随机更改」）。
    ''' </summary>
    Private Function InsertIndexFor(x As Double) As Integer
        Try
            'XFL 批71★：先按「可见的其它按钮」排出名次，再映射回真实的 Children 下标。
            '   旧实现把「排在光标左边的按钮个数」直接当 Children 下标用 —— 一旦有隐藏
            '   按钮（Tag 2 / 4 常驻 Collapsed）夹在中间，下标就整体错位，落位点随机偏移。
            Dim others As New List(Of MyRadioButton)
            For Each child As UIElement In FrmMain.PanTaskbarStack.Children
                If ReferenceEquals(child, _pressedBtn) Then Continue For
                Dim b As MyRadioButton = TryCast(child, MyRadioButton)
                If b Is Nothing Then Continue For
                If b.Visibility <> Visibility.Visible Then Continue For
                others.Add(b)
            Next
            If others.Count = 0 Then Return -1
            Dim k As Integer = 0
            For Each b In others
                Dim cx As Double = LayoutCenterX(b)
                If cx >= 0 AndAlso x > cx Then k += 1
            Next
            Dim res As Integer
            If k <= 0 Then
                res = FrmMain.PanTaskbarStack.Children.IndexOf(others(0))
            ElseIf k >= others.Count Then
                res = FrmMain.PanTaskbarStack.Children.IndexOf(others(others.Count - 1)) + 1
            Else
                res = FrmMain.PanTaskbarStack.Children.IndexOf(others(k))
            End If
            If res < 0 Then Return -1
            '换算到「先移除自身」之后的下标空间
            Dim cur As Integer = FrmMain.PanTaskbarStack.Children.IndexOf(_pressedBtn)
            If cur >= 0 AndAlso cur < res Then res -= 1
            Return res
        Catch
            Return -1
        End Try
    End Function

    'XFL 批71：辅助 —— 恢复单个按钮可见
    Private Sub ShowBtn(btn As MyRadioButton)
        Try
            If btn IsNot Nothing Then btn.Opacity = 1
        Catch
        End Try
    End Sub

    'XFL 批72：隐藏本体并登记（唯一的隐藏入口）
    Private Sub HideBtn(btn As MyRadioButton)
        Try
            If btn Is Nothing Then Return
            btn.Opacity = 0
            If Not _hiddenBtns.Contains(btn) Then _hiddenBtns.Add(btn)
        Catch
        End Try
    End Sub

    'XFL 批72：恢复单个按钮可见并销账
    Private Sub RestoreBtn(btn As MyRadioButton)
        Try
            If btn Is Nothing Then Return
            btn.Opacity = 1
            _hiddenBtns.Remove(btn)
        Catch
        End Try
    End Sub

    'XFL 批72：把登记表里所有隐身按钮一次性还回来
    Private Sub RestoreAllHidden()
        Try
            For Each b In _hiddenBtns.ToArray()
                Try : b.Opacity = 1 : Catch : End Try
            Next
            _hiddenBtns.Clear()
        Catch
        End Try
    End Sub

    ''' <summary>XFL 批72：恢复本体可见 —— 当前按下的按钮 + 所有遗留隐身按钮。</summary>
    Private Sub RestorePressedBtn()
        ShowBtn(_pressedBtn)
        RestoreAllHidden()
    End Sub

    'XFL 批71：辅助 —— 清掉按钮上可能残留的 TranslateTransform（旧 FLIP 实现的遗留物，
    '   会污染位置测量导致错乱；新实现不再写入，这里只做清理）
    Private Sub ClearSlideTransforms()
        Try
            For Each b As MyRadioButton In FrmMain.PanTaskbarStack.Children.OfType(Of MyRadioButton)()
                Try
                    If TypeOf b.RenderTransform Is TranslateTransform Then b.RenderTransform = Nothing
                Catch
                End Try
            Next
        Catch
        End Try
    End Sub

    '===== 收尾 =====
    Private Sub Stack_MouseUp(sender As Object, e As MouseButtonEventArgs)
        Try
            If Not _dragging Then Return
            FinishDrag(True)
            e.Handled = True
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} Stack 抬起处理失败")
        End Try
    End Sub

    ''' <summary>捕获丢失（窗口失焦、被别处抢走等）也要收尾，否则拖影残留。</summary>
    Private Sub Stack_LostCapture(sender As Object, e As MouseEventArgs)
        Try
            If _dragging Then FinishDrag(False)
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 失去捕获收尾失败")
        End Try
    End Sub

    ''' <summary>结束拖动。</summary>
    ''' <param name="Animated">True=正常松手（拖影弹性滑进目标槽位再淡出）；False=异常中断（立即清理）。</param>
    Private Sub FinishDrag(Animated As Boolean)
        'XFL 批71★★ 位置错乱/乱闪的主因：释放鼠标捕获会 **同步** 触发 LostMouseCapture，
        '   而 _dragging 那时还是 True，于是 Stack_LostCapture 递归再调一次 FinishDrag：
        '   ① 顺序被保存两次；② 第二次走 Animated=False 把拖影整层 DestroyGhost，
        '   外层刚要播的落位动画被自己抹掉 —— 松手瞬间图标「啪」一下消失再跳位。
        '   先把 _dragging 清掉，再释放捕获，收尾只会发生一次。
        _dragging = False
        Try
            Try : FrmMain.PanTaskbarStack.ReleaseMouseCapture() : Catch : End Try
            ClearSlideTransforms()
            'XFL 批71★：瞬时收尾立即恢复本体；落位动画路径在动画结束时才恢复，
            '   避免动画期间「本体 + 拖影」同时可见（双图标闪烁）
            If Not Animated Then RestorePressedBtn()
            SaveOrder()
            Logger.Info($"{Tag} 拖动结束，已落位")
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 结束拖动失败")
        Finally
            _dragging = False
            If _holdTimer IsNot Nothing Then _holdTimer.Stop()
            '★ 抑制标志下一帧自动复位：
            '  捕获后 PreviewClick 可能不再触发，若只靠它清零会一直吞掉下一次正常点击。
            Try
                FrmMain.Dispatcher.BeginInvoke(New Action(
                    Sub()
                        _suppressClick = False
                    End Sub), Windows.Threading.DispatcherPriority.Background)
            Catch
                _suppressClick = False
            End Try
        End Try
        'XFL 批70：落位动画 —— 拖影弹性滑进目标槽位中心并缩回 1.0、淡出，然后移除浮层
        '  （异常中断时不做动画，立即清理）
        If Animated AndAlso _dragGhost IsNot Nothing AndAlso _ghostLayer IsNot Nothing Then
            Try
                Dim ghost As Border = _dragGhost
                Dim layer As Canvas = _ghostLayer
                Dim btn As MyRadioButton = Nothing
                Dim oldPressed As MyRadioButton = _pressedBtn
                '找到目标槽位中心（用保存前最后位置；_pressedBtn 已在 Finally 清空，先留引用）
                Dim targetCenter As Point = New Point(0, 0)
                'XFL 批71：落点 = 被拖按钮恢复可见后的真实布局中心（不依赖固定步距，
                '   隐藏按钮不占位时也不会算错）
                Try
                    FrmMain.PanTaskbarStack.UpdateLayout()
                    targetCenter = oldPressed.TranslatePoint(New Point(oldPressed.ActualWidth / 2, oldPressed.ActualHeight / 2), layer)
                Catch
                    Dim slot As Integer = 0
                    For i As Integer = 0 To FrmMain.PanTaskbarStack.Children.Count - 1
                        If ReferenceEquals(FrmMain.PanTaskbarStack.Children(i), oldPressed) Then slot = i : Exit For
                    Next
                    targetCenter = New Point(46 + 68 * slot, FrmMain.PanTaskbarStack.ActualHeight / 2)
                End Try
                'XFL 批71★：targetCenter 上一行已经 TranslatePoint 到 layer（浮层坐标）了，
                '   这里再 TranslatePoint 一次会把任务栏自身在窗口里的偏移重复加一遍，
                '   动画落点比真实槽位偏十几像素，收起时看起来就是「跳了一下」。
                Dim left0 As Double = Canvas.GetLeft(ghost)
                Dim top0 As Double = Canvas.GetTop(ghost)
                Dim dx As Double = targetCenter.X - ghost.Width / 2 - left0
                Dim dy As Double = targetCenter.Y - ghost.Height / 2 - top0
                Dim sc As Windows.Media.ScaleTransform = TryCast(ghost.RenderTransform, Windows.Media.ScaleTransform)
                If sc Is Nothing Then
                    sc = New Windows.Media.ScaleTransform() With {.ScaleX = 1.0, .ScaleY = 1.0}
                    ghost.RenderTransform = sc
                    ghost.RenderTransformOrigin = New Point(0.5, 0.5)
                End If
                'XFL 批71★：落位动画的三个语义坑
                '  ① AaDouble 的 lambda 收到的是 0→Value 的绝对插值：写 sc.ScaleX = v 会让拖影
                '     从 0 倍开始（先炸开再回弹），必须从「当前值」插到目标值。
                '  ② AaOpacity 是累加（Value 即总增量）：传 0 等于不动，拖影永不淡出，
                '     到时被硬删除 —— 这就是松手「闪一下」。淡出必须传负值。
                '  ③ 本体等动画结算后再恢复可见，动画期间不会出现「双图标」。
                Dim sc0x As Double = sc.ScaleX
                Dim sc0y As Double = sc.ScaleY
                Dim op0 As Double = ghost.Opacity
                AniStart({
                    AaDouble(Sub(v) Canvas.SetLeft(ghost, left0 + v), dx, 170, 0, New AniEaseOutFluent(AniEasePower.Weak)),
                    AaDouble(Sub(v) Canvas.SetTop(ghost, top0 + v), dy, 170, 0, New AniEaseOutFluent(AniEasePower.Weak)),
                    AaDouble(Sub(v) sc.ScaleX = sc0x + (1.0 - sc0x) * v, 1.0, 170, 0, New AniEaseOutFluent(AniEasePower.Weak)),
                    AaDouble(Sub(v) sc.ScaleY = sc0y + (1.0 - sc0y) * v, 1.0, 170, 0, New AniEaseOutFluent(AniEasePower.Weak)),
                    AaOpacity(ghost, -op0, 130, 70),
                    AaCode(Sub()
                               Try
                                   layer.Children.Remove(ghost)
                                   Dim parent As DependencyObject = VisualTreeHelper.GetParent(layer)
                                   If TypeOf parent Is Grid Then DirectCast(parent, Grid).Children.Remove(layer)
                               Catch
                               End Try
                               Try
                                   RestoreBtn(oldPressed)
                               Catch
                               End Try
                               If ReferenceEquals(_ghostLayer, layer) Then _ghostLayer = Nothing
                               If ReferenceEquals(_dragGhost, ghost) Then _dragGhost = Nothing
                           End Sub, 220)
                }, "TaskbarDragGhost")
            Catch
                'XFL 批71★：动画起不来时必须补回本体可见，否则按下的按钮会一直隐身
                RestorePressedBtn()
                DestroyGhost()
            End Try
        Else
            RestorePressedBtn()
            DestroyGhost()
        End If
    End Sub

    ''' <summary>按钮上的抬起：只在「没进入拖动」时用于停掉长按计时器。</summary>
    Private Sub Btn_MouseUp(sender As Object, e As MouseButtonEventArgs)
        Try
            If _dragging Then Return          '拖动中的收尾由 Stack_MouseUp 负责
            If _holdTimer IsNot Nothing Then _holdTimer.Stop()
            _pressedBtn = Nothing
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 抬起处理失败")
        End Try
    End Sub

    ''' <summary>拖动结束的那一次点击要吞掉，避免"拖完顺手切了页"。</summary>
    Private Sub Btn_PreviewClick(sender As Object, e As RouteEventArgs)
        Try
            If _dragging OrElse _suppressClick Then
                _suppressClick = False
                e.Handled = True
            End If
        Catch
        End Try
    End Sub

    ''' <summary>XFL 批72：自检（两阶段）—— 不依赖鼠标注入，直接验证拖动稳定性：
    '''   阶段一：①拖影是渲染快照且「有像素」（alpha>0，否则拖动中看不见图标）
    '''           ②插入下标单调 ③进入拖动会把遗留隐身本体恢复回来
    '''   阶段二（600ms 后）：④落位动画结束后本体自动恢复可见、无残留浮层/偏移</summary>
    Friend Function SelfTest() As String
        Dim sb As New System.Text.StringBuilder
        Try
            If FrmMain Is Nothing OrElse FrmMain.PanTaskbarStack Is Nothing Then Return "stack 未就绪"
            Try : _selfTestSavedOrder = Settings.Get(Of String)("XflTaskbarOrder") : Catch : _selfTestSavedOrder = Nothing : End Try
            Dim stack = FrmMain.PanTaskbarStack
            Dim btns As New List(Of MyRadioButton)
            For Each b As MyRadioButton In stack.Children.OfType(Of MyRadioButton)()
                If b.Visibility = Visibility.Visible Then btns.Add(b)
            Next
            If btns.Count < 6 Then Return "可见按钮不足: " & btns.Count
            _testing = True
            _selfTestTarget = btns(2)
            _pressedBtn = _selfTestTarget
            _dragging = False
            _suppressClick = False
            '★ 制造「上一次拖动落位动画被打断」现场：本体已隐藏却没人恢复。
            '   进入拖动时若不先恢复可见，快照就是全透明的 → 拖动中看不见图标。
            RestoreAllHidden()
            HideBtn(_selfTestTarget)
            sb.Append("preHidden=" & _hiddenBtns.Count)
            HoldElapsed(Nothing, Nothing)
            sb.Append(" ghost=" & If(_dragGhost Is Nothing, "无", _dragGhost.Background.GetType().Name))
            sb.Append(" alpha=" & SampleGhostAlpha().ToString("0.000"))
            sb.Append(" ghostOp=" & (If(_dragGhost Is Nothing, -1.0, _dragGhost.Opacity)).ToString("0.##"))
            sb.Append(" hiddenInDrag=" & _hiddenBtns.Count)
            Dim baseX As Double = _selfTestTarget.TranslatePoint(New Point(_selfTestTarget.ActualWidth / 2, 0), stack).X
            Dim seq As New List(Of Integer)
            For k As Integer = 0 To 6
                DragMoveTo(New Point(baseX + k * 12, _selfTestTarget.ActualHeight / 2))
                seq.Add(stack.Children.IndexOf(_selfTestTarget))
            Next
            sb.Append(" idxSeq=" & String.Join(",", seq))
            Dim mono As Boolean = True
            For i As Integer = 1 To seq.Count - 1
                If seq(i) < seq(i - 1) Then mono = False
            Next
            sb.Append(" mono=" & mono)
            'XFL 批73：拖动中途暂停 2.5 秒 —— 必须用 DispatcherTimer 保持 UI 泵送（Thread.Sleep 会把渲染也冻住，拖影不落屏），供外部截屏核对拖影大小/位置/缩放
            _selfTestPending = True
            sb.Append(" pending=1")
            Dim pause As New System.Windows.Threading.DispatcherTimer(Windows.Threading.DispatcherPriority.Background) With {
                .Interval = TimeSpan.FromMilliseconds(2500)}
            AddHandler pause.Tick,
                Sub(s2 As Object, e2 As EventArgs)
                    Try
                        DirectCast(s2, System.Windows.Threading.DispatcherTimer).Stop()
                        Logger.Info("[XFL][测试] DRAG 自检暂停结束，开始落位")
                        '★ 走真实的松手路径（带落位动画）—— 本体必须在动画结束后自动恢复
                        FinishDrag(True)
                        Dim late As New System.Windows.Threading.DispatcherTimer(Windows.Threading.DispatcherPriority.Background) With {
                            .Interval = TimeSpan.FromMilliseconds(600)}
                        AddHandler late.Tick, AddressOf SelfTestCheck
                        late.Start()
                    Catch ex2 As Exception
                        Logger.Warn(ex2, "[XFL][测试] 自检暂停段失败")
                        _selfTestPending = False
                        _dragging = False
                        _testing = False
                        _pressedBtn = Nothing
                        RestoreAllHidden()
                        DestroyGhost()
                        Try
                            Settings.Set("XflTaskbarOrder", If(_selfTestSavedOrder, ""))
                            ApplySavedOrder()
                        Catch
                        End Try
                    End Try
                End Sub
            pause.Start()
            Return sb.ToString()
        Catch ex As Exception
            Return "SelfTest 异常: " & ex.Message
        Finally
            If Not _selfTestPending Then
                _dragging = False
                _testing = False
                _pressedBtn = Nothing
                DestroyGhost()
                Try
                    Settings.Set("XflTaskbarOrder", If(_selfTestSavedOrder, ""))
                    ApplySavedOrder()
                Catch
                End Try
            End If
        End Try
    End Function

    ''' <summary>XFL 批72：自检第二阶段 —— 落位动画结束后核对本体可见性与残留。</summary>
    Private Sub SelfTestCheck(sender As Object, e As EventArgs)
        Dim sb As New System.Text.StringBuilder
        Try
            Dim t As System.Windows.Threading.DispatcherTimer = TryCast(sender, System.Windows.Threading.DispatcherTimer)
            If t IsNot Nothing Then t.Stop()
            Dim op As Double = -1
            Try : op = _selfTestTarget.Opacity : Catch : End Try
            sb.Append("opacity=" & op.ToString("0.##"))
            sb.Append(" hiddenLeft=" & _hiddenBtns.Count)
            sb.Append(" ghost=" & If(_dragGhost Is Nothing, "clean", "LEFTOVER"))
            Dim xf As Integer = 0
            For Each b As MyRadioButton In FrmMain.PanTaskbarStack.Children.OfType(Of MyRadioButton)()
                If TypeOf b.RenderTransform Is TranslateTransform Then xf += 1
            Next
            sb.Append(" xf=" & xf)
            Dim ord As String = ""
            Try : ord = Settings.Get(Of String)("XflTaskbarOrder") : Catch : End Try
            sb.Append(" order=" & ord)
        Catch ex As Exception
            sb.Append("异常: " & ex.Message)
        Finally
            Logger.Info($"[XFL][测试] DRAG 自检收尾: {sb.ToString()}")
            _selfTestPending = False
            _dragging = False
            _testing = False
            _pressedBtn = Nothing
            RestoreAllHidden()
            DestroyGhost()
            Try
                Settings.Set("XflTaskbarOrder", If(_selfTestSavedOrder, ""))
                ApplySavedOrder()
            Catch
            End Try
        End Try
    End Sub

    ''' <summary>恢复默认顺序（供设置用）。</summary>
    Friend Sub ResetOrder()
        Try
            Settings.Set("XflTaskbarOrder", "")
            ReorderByTags(DefaultOrder.ToList())
            Logger.Info($"{Tag} 已恢复默认顺序")
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 恢复默认失败")
        End Try
    End Sub

End Module
