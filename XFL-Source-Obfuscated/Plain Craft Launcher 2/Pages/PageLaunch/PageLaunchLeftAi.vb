
Imports System.Windows.Threading
Imports System.Text.RegularExpressions

Partial Public Class PageLaunchLeft

#Region "XFL AI 对话"

    Private AiUiInited As Boolean = False
    Private AiBusy As Boolean = False
    Private _lastRole As String = ""
    Private _suppressAutoScroll As Boolean = False

    Private Const AnsFontSize As Double = 15
    Private Const AnsLineHeight As Double = 24
    Private Const AnsParaGap As Double = 8
    Private Const AnsMaxWidth As Double = 620
    Private Const UsrMaxWidth As Double = 560
    Private Const ThinkFontSize As Double = 13
    Private Const ThinkLineHeight As Double = 20
    Private Const ThinkMaxWidth As Double = 560
    Private Const MsgGap As Double = 18
    Private Const MsgGapSame As Double = 6
    Private Const ListIndent As Double = 12
    Private Const ListItemGap As Double = 4
    Private Const ThinkFoldHeight As Double = 140

    Public Sub InitAiChat()
        Try
            If AiUiInited OrElse TxtAiInput Is Nothing OrElse BtnAiSend Is Nothing Then Return
            AiUiInited = True
            AddHandler BtnAiSend.MouseLeftButtonUp, AddressOf AiSend_Click
            AddHandler TxtAiInput.KeyDown, AddressOf AiInput_KeyDown
            If BtnAiThink IsNot Nothing Then AddHandler BtnAiThink.MouseLeftButtonUp, AddressOf AiThink_Click
            If BtnAiSearch IsNot Nothing Then AddHandler BtnAiSearch.MouseLeftButtonUp, AddressOf AiSearch_Click
            SyncAiToggles()
            UpdateAiPlaceholder()
            InitAiProvider()
            AddHandler TxtAiInput.TextChanged, AddressOf AiInput_TextChanged

        Try
            If BtnAiExec IsNot Nothing Then AddHandler BtnAiExec.MouseLeftButtonDown, AddressOf AiExec_Click
        Catch
        End Try
            _suppressAutoScroll = True

            AddAiBubble("assistant", "你好！我是 Xiaofu AI 助手 🤖 可以陪你聊天、回答问题，也能帮你了解启动器的用法。开启「🌐 智能搜索」我会先联网查资料再标注来源，开启「🧠 深度思考」我会先给出思考过程。在下面输入消息，按回车或点右侧箭头发送。")
            BuildAiSuggestions()

            Dispatcher.BeginInvoke(New Action(
                Sub()
                    Try
                        _suppressAutoScroll = False
                        If PanAiScroll IsNot Nothing Then PanAiScroll.ScrollToTop()
                    Catch
                    End Try
                End Sub), DispatcherPriority.ApplicationIdle)
        Catch ex As Exception
            Logger.Error(ex, "[XFL] AI 对话初始化失败")
        End Try
    End Sub

    Private Const ProviderFree As String = "free"
    Private Const ProviderDeepSeek As String = "deepseek"
    Private _providerInited As Boolean = False
    Private _currentProvider As String = ProviderFree

    Private Sub InitAiProvider()
        Try
            If _providerInited OrElse BtnProviderFree Is Nothing OrElse BtnProviderDs Is Nothing Then Return
            _providerInited = True
            AddHandler BtnProviderFree.MouseLeftButtonUp,
                Sub(s As Object, e As MouseButtonEventArgs)
                    e.Handled = True
                    SwitchAiProvider(ProviderFree)
                End Sub
            AddHandler BtnProviderDs.MouseLeftButtonUp,
                Sub(s As Object, e As MouseButtonEventArgs)
                    e.Handled = True
                    SwitchAiProvider(ProviderDeepSeek)
                End Sub

            If LabDsStatus IsNot Nothing Then
                AddHandler LabDsStatus.MouseLeftButtonUp,
                    Sub(s As Object, e As MouseButtonEventArgs)
                        Try
                            e.Handled = True
                            LabDsStatus.Text = "DeepSeek：正在检测登录…"
                            ModDeepSeek.CheckLoginAsync(AddressOf OnDsLoginChecked)
                        Catch
                        End Try
                    End Sub
                LabDsStatus.Cursor = Cursors.Hand
                LabDsStatus.ToolTip = "点击重新检测登录状态"
            End If

            Dim saved As String = Settings.Get(Of String)("AiProvider")
            If String.IsNullOrWhiteSpace(saved) Then saved = ProviderFree
            ApplyAiProvider(saved, False)
            Logger.Info($"[XFL] AI 服务切换器已就绪，当前：{saved}")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 初始化 AI 服务切换失败")
        End Try
    End Sub

    Private Sub SwitchAiProvider(provider As String)
        Try
            If provider = _currentProvider Then Return
            Logger.Info($"[XFL] 用户切换 AI 服务：{_currentProvider} → {provider}")
            Settings.Set("AiProvider", provider)
            ApplyAiProvider(provider, True)
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 切换 AI 服务失败")
        End Try
    End Sub

    Private Sub ApplyAiProvider(provider As String, userInitiated As Boolean)
        Try
            _currentProvider = provider
            Dim isDs As Boolean = (provider = ProviderDeepSeek)

            StyleProviderPill(BtnProviderFree, LabProviderFree, Not isDs)
            StyleProviderPill(BtnProviderDs, LabProviderDs, isDs)

            If PanAiScroll IsNot Nothing Then PanAiScroll.Visibility = If(isDs, Visibility.Collapsed, Visibility.Visible)
            If PanAiInputArea IsNot Nothing Then PanAiInputArea.Visibility = If(isDs, Visibility.Collapsed, Visibility.Visible)
            If LabAiEmpty IsNot Nothing AndAlso isDs Then LabAiEmpty.Visibility = Visibility.Collapsed

            If PanDsHost IsNot Nothing Then PanDsHost.Visibility = If(isDs, Visibility.Visible, Visibility.Collapsed)
            If LabDsStatus IsNot Nothing Then LabDsStatus.Visibility = If(isDs, Visibility.Visible, Visibility.Collapsed)
            ModDeepSeek.SetVisible(isDs)

            If Not isDs Then
                Logger.Info("[XFL] 当前为免费 AI 模式，跳过 DeepSeek 视图初始化")
                Return
            End If

            If isDs Then
                If LabDsStatus IsNot Nothing Then LabDsStatus.Text = "DeepSeek：正在连接…"
                ModDeepSeek.EnsureView(PanDsView,
                    Sub(okView As Boolean)
                        Try
                            If Not okView Then
                                If LabDsStatus IsNot Nothing Then LabDsStatus.Text = "DeepSeek：WebView2 运行时不可用"
                                Return
                            End If
                            ModDeepSeek.CheckLoginAsync(AddressOf OnDsLoginChecked)
                        Catch ex As Exception
                            Logger.Error(ex, "[XFL] DeepSeek 视图就绪回调异常")
                        End Try
                    End Sub)
            End If
            Logger.Info($"[XFL] AI 服务已应用：{provider}（DeepSeek 容器 {(If(isDs, "显示", "隐藏"))}）")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 应用 AI 服务失败")
        End Try
    End Sub

    Private Sub OnDsLoginChecked(loggedIn As Boolean)
        Try
            If LabDsStatus Is Nothing Then Return
            If loggedIn Then
                LabDsStatus.Text = "DeepSeek：已登录 ✓（可在下方页面直接对话）"
            Else
                LabDsStatus.Text = "DeepSeek：未登录 —— 请在下方页面用你的账号登录"
            End If
        Catch
        End Try
    End Sub

    Private Sub StyleProviderPill(pill As Border, label As TextBlock, active As Boolean)
        Try
            If pill Is Nothing Then Return
            If active Then
                pill.SetResourceReference(Border.BackgroundProperty, "ColorBrush3")
                If label IsNot Nothing Then
                    label.Foreground = New SolidColorBrush(Colors.White)
                    label.FontWeight = FontWeights.SemiBold
                End If
            Else
                pill.Background = Brushes.Transparent
                If label IsNot Nothing Then
                    label.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushTextSecondary")
                    label.FontWeight = FontWeights.Normal
                End If
            End If
        Catch
        End Try
    End Sub

    Private Sub SyncAiToggles()
        Try
            Dim thinkOn As Boolean = Settings.Get(Of Boolean)("AiShowReasoning")
            Dim searchOn As Boolean = Settings.Get(Of Boolean)("AiWebSearch")
            SetPillState(BtnAiThink, LabAiThink, thinkOn)
            SetPillState(BtnAiSearch, LabAiSearch, searchOn)

            Dim execOn As Boolean = Settings.Get(Of Boolean)("AiExecEnabled")
            SetPillState(BtnAiExec, LabAiExec, execOn)
        Catch ex As Exception
            Logger.Error(ex, "[XFL] AI 胶囊状态同步失败")
        End Try
    End Sub

    Private Sub SetPillState(pill As Border, label As TextBlock, isOn As Boolean)
        Try
            If pill Is Nothing OrElse label Is Nothing Then Return

            If isOn Then
                pill.SetResourceReference(Border.BackgroundProperty, "ColorBrush2")
                label.Foreground = Brushes.White
            Else
                pill.SetResourceReference(Border.BackgroundProperty, "ColorBrushGray6")
                label.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray1")
            End If
        Catch ex As Exception
            Logger.Error(ex, "[XFL] AI 胶囊外观设置失败")
        End Try
    End Sub

    Private Sub AiThink_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            Dim isOn As Boolean = Not Settings.Get(Of Boolean)("AiShowReasoning")
            Settings.Set("AiShowReasoning", isOn)
            SyncAiToggles()
            Logger.Info($"[XFL] 深度思考已{(If(isOn, "开启", "关闭"))}")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 深度思考切换失败")
        End Try
    End Sub

    Private Sub AiSearch_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            Dim isOn As Boolean = Not Settings.Get(Of Boolean)("AiWebSearch")
            Settings.Set("AiWebSearch", isOn)
            SyncAiToggles()
            Logger.Info($"[XFL] 智能搜索已{(If(isOn, "开启", "关闭"))}")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 智能搜索切换失败")
        End Try
    End Sub

    Private Sub AiExec_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            Dim isOn As Boolean = Not Settings.Get(Of Boolean)("AiExecEnabled")
            Settings.Set("AiExecEnabled", isOn)
            SyncAiToggles()
            Hint(If(isOn,
                    "已允许 AI 执行操作。它在下达动作前会先列出计划让你确认。",
                    "已关闭 AI 执行。"), If(isOn, HintType.Blue, HintType.Blue))
            Logger.Info($"[XFL] AI 执行已{(If(isOn, "开启", "关闭"))}")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] AI 执行开关切换失败")
        End Try
    End Sub

    Private Sub HandleAiExec(reply As String, userText As String)
        Try
            If Not ModXflAiExec.Enabled Then Return
            Dim acts As List(Of JObject) = ModXflAiExec.ExtractActions(reply)
            If acts Is Nothing OrElse acts.Count = 0 Then Return
            Dim plan As String = ModXflAiExec.Describe(acts)
            If String.IsNullOrWhiteSpace(plan) Then Return

            Dim warn As String = ""
            Try
                warn = ModXflAiExec.VersionMismatchWarning(userText, acts)
            Catch
            End Try
            Dim body As String = "AI 想要执行以下操作：" & vbCrLf & vbCrLf & plan
            If warn <> "" Then body &= vbCrLf & vbCrLf & "请注意：" & warn
            body &= vbCrLf & vbCrLf & "确认后会立即开始（下载任务可在底部进度里看）。"
            Dim choice As Integer = MyMsgBox(
                body, "AI 执行确认", "执行", "取消", ForceWait:=True)
            If choice <> 1 Then
                AddAiBubble("assistant", "已取消，没有执行任何操作。")
                Return
            End If
            AddAiBubble("assistant", "▶ 开始执行：" & vbCrLf & plan)
            ModXflAiExec.Execute(acts,
                Sub(msg As String)
                    Try
                        AddAiBubble("assistant", "· " & msg)
                    Catch
                    End Try
                End Sub)
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] AI 执行处理失败")
        End Try
    End Sub

    Private Sub AiInput_TextChanged(sender As Object, e As TextChangedEventArgs)
        UpdateAiPlaceholder()
    End Sub

    Private Sub UpdateAiPlaceholder()
        Try
            If LabAiPlaceholder Is Nothing OrElse TxtAiInput Is Nothing Then Return
            LabAiPlaceholder.Visibility = If(String.IsNullOrEmpty(TxtAiInput.Text), Visibility.Visible, Visibility.Collapsed)
        Catch
        End Try
    End Sub

    Private Sub AiInput_KeyDown(sender As Object, e As KeyEventArgs)
        Try
            If e.Key = Key.Enter Then
                e.Handled = True
                SendAiMessage()
            End If
        Catch ex As Exception
            Logger.Error(ex, "[XFL] AI 输入框按键处理失败")
        End Try
    End Sub

    Private Sub AiSend_Click(sender As Object, e As MouseButtonEventArgs)
        SendAiMessage()
    End Sub

    Private Sub SetAiSendEnabled(enabled As Boolean)
        Try
            If BtnAiSend Is Nothing Then Return
            If enabled Then
                BtnAiSend.SetResourceReference(Border.BackgroundProperty, "ColorBrush3")
                If PathAiSend IsNot Nothing Then PathAiSend.Stroke = Brushes.White
            Else
                BtnAiSend.SetResourceReference(Border.BackgroundProperty, "ColorBrush7")
                If PathAiSend IsNot Nothing Then
                    Dim g4 As Brush = TryCast(Application.Current.Resources("ColorBrushGray4"), Brush)
                    PathAiSend.Stroke = If(g4 IsNot Nothing, g4, Brushes.Gray)
                End If
            End If
        Catch ex As Exception
            Logger.Error(ex, "[XFL] AI 发送钮状态设置失败")
        End Try
    End Sub

    Public Sub SendAiMessage()
        Try
            If AiBusy Then Return
            If TxtAiInput Is Nothing Then Return
            Dim text As String = If(TxtAiInput.Text, "").Trim()
            If text = "" Then Return
            TxtAiInput.Text = ""
            AiBusy = True
            SetAiSendEnabled(False)
            RemoveAiSuggestions()

            AddAiBubble("user", text)
            Dim pending As Border = AddAiBubble("assistant", "正在思考…")

            ModAi.AiSendAsync(text,
                Sub(status As String)
                    UpdateAiBubble(pending, status)
                    Logger.Info("[XFL] AI 阶段：" & status)
                End Sub,
                Sub(thinking As String)
                    Try
                        Dim showThink As Boolean = Settings.Get(Of Boolean)("AiShowReasoning")
                        If showThink AndAlso Not String.IsNullOrWhiteSpace(thinking) Then
                            AddAiBubble("think", thinking.Trim())
                            ScrollAiToBottom()
                        End If
                    Catch
                    End Try
                End Sub,
                Sub(reply As String)

                    Dim shown As String = reply
                    Try
                        If ModXflAiExec.Enabled Then shown = ModXflAiExec.StripActions(reply)
                    Catch
                    End Try
                    UpdateAiBubble(pending, shown)
                    HandleAiExec(reply, text)
                    AiBusy = False
                    SetAiSendEnabled(True)
                    Logger.Info($"[XFL] AI 回复已显示，长度 {If(reply, "").Length} 字")

                    Try
                        Dim body As String = If(reply, "").Replace(vbCrLf, " ").Replace(vbLf, " ")
                        Logger.Info("[XFL] AI 回复内容：" & If(body.Length > 220, body.Substring(0, 220) & "…", body))
                    Catch
                    End Try
                End Sub,
                Sub(err As String)
                    UpdateAiBubble(pending, "⚠ 出错了：" & err & vbCrLf & vbCrLf &
                                   "常见原因与处理：" & vbCrLf &
                                   "· 免费接口限速（每 45 秒 1 次）—— 程序会自动倒计时重试，也可关掉「智能搜索」让响应更快" & vbCrLf &
                                   "· 接口或网络不可达 —— 检查 设置 → 其他 → AI 对话 里的接口地址与模型名" & vbCrLf &
                                   "· 想更稳定 —— 换成自己的 API Key（如 DeepSeek）")
                    AiBusy = False
                    SetAiSendEnabled(True)
                    Logger.Error("[XFL] AI 对话失败：" & err)
                End Sub,
                Sub(chunk As String)

                    Try
                        If Not String.IsNullOrWhiteSpace(chunk) Then
                            UpdateAiBubble(pending, chunk)
                        End If
                    Catch
                    End Try
                End Sub)
        Catch ex As Exception
            AiBusy = False
            SetAiSendEnabled(True)
            Logger.Error(ex, "[XFL] AI 消息发送失败")
        End Try
    End Sub

    Public Sub ClearAiChat()
        Try
            If PanAiMessages Is Nothing Then Return
            PanAiMessages.Children.Clear()
            ModAi.AiClearHistory()
            _lastRole = ""
            AddAiBubble("assistant", "对话已清空，我们重新开始吧 🙌")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] AI 对话清空失败")
        End Try
    End Sub

#Region "消息排版引擎"

    Private Function BuildMessageBody(sp As StackPanel, text As String, style As String) As StackPanel
        Dim fs As Double = If(style = "think", ThinkFontSize, If(style = "status", 13.0, AnsFontSize))
        Dim lh As Double = If(style = "think", ThinkLineHeight, If(style = "status", 20.0, AnsLineHeight))
        Dim fg As Brush
        Select Case style
            Case "think"
                fg = Nothing
            Case "status"
                fg = TryCast(Application.Current.Resources("ColorBrushGray2"), Brush)
            Case Else
                fg = Nothing
        End Select
        If fg Is Nothing Then fg = TryCast(Application.Current.Resources("ColorBrushBubbleAnswerText"), Brush)
        If fg Is Nothing Then fg = Brushes.Black

        Dim fgKey As String = If(style = "think", "ColorBrushBubbleThinkText",
                                 If(style = "status", "ColorBrushTextTertiary", "ColorBrushBubbleAnswerText"))
        If String.IsNullOrEmpty(text) Then text = ""

        Dim blocks As New List(Of List(Of String))
        Dim cur As New List(Of String)
        For Each line As String In text.Replace(vbCrLf, vbLf).Split(ControlChars.Lf)
            If line.Trim().Length = 0 Then
                If cur.Count > 0 Then
                    blocks.Add(cur)
                    cur = New List(Of String)
                End If
            Else
                cur.Add(line)
            End If
        Next
        If cur.Count > 0 Then blocks.Add(cur)

        Dim first As Boolean = True
        For Each blk As List(Of String) In blocks
            If IsListLine(blk(0)) Then
                Dim idx As Integer = 0
                For Each item As String In blk
                    idx += 1
                    Dim tbLi As New TextBlock With {
                        .Text = "·  " & StripListMarker(item),
                        .TextWrapping = TextWrapping.Wrap,
                        .FontSize = fs, .LineHeight = lh, .Foreground = fg,
                        .Margin = New Thickness(ListIndent, 0, 0,
                            If(idx = blk.Count, ListItemGap + If(first, 0.0, AnsParaGap), ListItemGap))}
                    tbLi.SetResourceReference(TextBlock.ForegroundProperty, fgKey)
                    sp.Children.Add(tbLi)
                Next
            Else
                Dim tbP As New TextBlock With {
                    .Text = String.Join(vbCrLf, blk.ToArray()),
                    .TextWrapping = TextWrapping.Wrap,
                    .FontSize = fs, .LineHeight = lh, .Foreground = fg,
                    .Margin = New Thickness(0, 0, 0, If(first, 0.0, AnsParaGap))}
                tbP.SetResourceReference(TextBlock.ForegroundProperty, fgKey)
                sp.Children.Add(tbP)
            End If
            first = False
        Next

        If sp.Children.Count > 0 Then
            Dim last As TextBlock = TryCast(sp.Children(sp.Children.Count - 1), TextBlock)
            If last IsNot Nothing Then
                Dim m As Thickness = last.Margin
                last.Margin = New Thickness(m.Left, m.Top, m.Right, 0)
            End If
        End If
        Return sp
    End Function

    Private Function IsListLine(line As String) As Boolean
        Dim s As String = line.TrimStart()
        If s.Length = 0 Then Return False
        If s.StartsWith("·") OrElse s.StartsWith("•") OrElse s.StartsWith("- ") OrElse s.StartsWith("* ") Then Return True
        Return Regex.IsMatch(s, "^\d{1,2}\s*[\.、\)]\s+")
    End Function

    Private Function StripListMarker(line As String) As String
        Dim s As String = line.TrimStart()
        If s.StartsWith("·") OrElse s.StartsWith("•") Then Return s.Substring(1).TrimStart()
        If s.StartsWith("- ") OrElse s.StartsWith("* ") Then Return s.Substring(2).TrimStart()
        Return Regex.Replace(s, "^\d{1,2}\s*[\.、\)]\s+", "")
    End Function

    Private Function NextMessageGap(role As String) As Double
        If _lastRole = "" Then Return 0
        Return If(String.Equals(_lastRole, role, StringComparison.OrdinalIgnoreCase), MsgGapSame, MsgGap)
    End Function

#End Region

    Friend _suggestBlock As Border = Nothing

    Private Sub BuildAiSuggestions()
        Try
            If PanAiMessages Is Nothing Then Return
            Dim items As String() = {
                "介绍一下 XFL 启动器的主要功能",
                "帮我推荐几个适合新手的模组",
                "正版登录失败怎么办？",
                "怎么给 Minecraft 分配更多内存？",
                "怎么安装 Forge 或 Fabric？",
                "登录与下载常见问题有哪些？"}
            Dim wrap As New StackPanel With {.Margin = New Thickness(0, MsgGap, 0, 0)}
            wrap.Children.Add(New TextBlock With {
                .Text = "猜你想问",
                .FontSize = 12, .Foreground = New SolidColorBrush(Color.FromRgb(&H8A, &H8F, &H98)),
                .Margin = New Thickness(0, 0, 0, 6)})
            For Each q As String In items

                Dim chip As New Border With {
                    .CornerRadius = New CornerRadius(14),
                    .Padding = New Thickness(12, 7, 12, 7),
                    .Margin = New Thickness(0, 0, 0, 6),
                    .Width = 340,
                    .HorizontalAlignment = HorizontalAlignment.Left,
                    .Cursor = Cursors.Hand,
                    .Tag = q}
                chip.SetResourceReference(Border.BackgroundProperty, "ColorBrush8")
                Dim tb As New TextBlock With {
                    .Text = q, .FontSize = 13, .TextWrapping = TextWrapping.Wrap}
                tb.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush2")
                chip.Child = tb
                AddHandler chip.MouseLeftButtonUp,
                    Sub(s As Object, e As MouseButtonEventArgs)
                        Try
                            If TxtAiInput IsNot Nothing Then
                                TxtAiInput.Text = CStr(CType(s, Border).Tag)
                                SendAiMessage()
                            End If
                        Catch
                        End Try
                    End Sub
                wrap.Children.Add(chip)
            Next
            _suggestBlock = New Border With {.Child = wrap}
            PanAiMessages.Children.Add(_suggestBlock)
            _lastRole = "assistant"
            HideAiEmptyHint()
            ScrollAiToBottom()
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 建议卡片创建失败")
        End Try
    End Sub

    Private Sub RemoveAiSuggestions()
        Try
            If _suggestBlock Is Nothing OrElse PanAiMessages Is Nothing Then Return
            If PanAiMessages.Children.Contains(_suggestBlock) Then PanAiMessages.Children.Remove(_suggestBlock)
            _suggestBlock = Nothing
        Catch
        End Try
    End Sub

    Public Sub SetCompactMode(compact As Boolean)
        Try

            If LabXflTitle IsNot Nothing Then
                LabXflTitle.FontSize = If(compact, 20.0, 26.0)
                LabXflTitle.LineHeight = If(compact, 26.0, 34.0)
            End If
            If LabXflClock IsNot Nothing Then
                LabXflClock.FontSize = If(compact, 11.0, 12.0)
                LabXflClock.Margin = If(compact, New Thickness(0, 2, 0, 0), New Thickness(0, 4, 0, 0))
            End If

            Try
                Dim head As FrameworkElement = TryCast(LabXflTitle?.Parent, FrameworkElement)
                If head IsNot Nothing Then
                    head.Margin = If(compact, New Thickness(0, 3, 0, 3), New Thickness(0, 7, 0, 7))
                End If
            Catch
            End Try

            If PanInput IsNot Nothing Then

                Try
                    Dim card As FrameworkElement = TryCast(TxtAiInput?.Parent, FrameworkElement)
                    If card IsNot Nothing AndAlso TypeOf card Is Grid Then
                        card.Margin = If(compact, New Thickness(20, 4, 20, 6), New Thickness(20, 8, 20, 12))
                    End If
                Catch
                End Try
            End If

            Try
                If _suggestBlock IsNot Nothing Then
                    _suggestBlock.Visibility = If(compact, Visibility.Collapsed, Visibility.Visible)
                End If
            Catch
            End Try
            Logger.Info($"[XFL] 主页紧凑模式已{(If(compact, "开启", "关闭"))}")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 设置紧凑模式失败")
        End Try
    End Sub

    Private Function AddAiBubble(role As String, text As String) As Border
        Try
            Dim isUser As Boolean = String.Equals(role, "user", StringComparison.OrdinalIgnoreCase)
            Dim isThink As Boolean = String.Equals(role, "think", StringComparison.OrdinalIgnoreCase)
            Dim accent As Brush = TryCast(Application.Current.Resources("ColorBrush3"), Brush)
            If accent Is Nothing Then accent = New SolidColorBrush(Color.FromRgb(&H3B, &H82, &HF6))

            Dim bubble As New Border With {
                .MaxWidth = If(isUser, UsrMaxWidth, If(isThink, ThinkMaxWidth, AnsMaxWidth)),
                .HorizontalAlignment = If(isUser, HorizontalAlignment.Right, HorizontalAlignment.Left),
                .Margin = New Thickness(0, NextMessageGap(role), 0, 0),
                .Padding = If(isUser, New Thickness(16, 12, 16, 12),
                              If(isThink, New Thickness(14, 10, 14, 10), New Thickness(16, 14, 16, 14))),
                .CornerRadius = If(isUser, New CornerRadius(14, 14, 4, 14),
                                   If(isThink, New CornerRadius(12), New CornerRadius(4, 14, 14, 14)))}

            If isUser Then
                bubble.Background = accent
            ElseIf isThink Then

                bubble.SetResourceReference(Border.BackgroundProperty, "ColorBrushBubbleThink")
                bubble.SetResourceReference(Border.BorderBrushProperty, "ColorBrushBubbleThinkBorder")
                bubble.BorderBrush = accent
                bubble.BorderThickness = New Thickness(3, 1, 1, 1)
            Else
                bubble.SetResourceReference(Border.BackgroundProperty, "ColorBrushBubbleAnswer")
            End If

            Dim body As New StackPanel()
            If isThink Then

                Dim contentHost As New StackPanel()
                If text.Length > 220 Then
                    Dim inner As New StackPanel()
                    BuildMessageBody(inner, text, "think")
                    Dim sv As New ScrollViewer With {
                        .MaxHeight = ThinkFoldHeight,
                        .VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        .HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled}
                    sv.Content = inner
                    contentHost.Children.Add(sv)
                Else
                    BuildMessageBody(contentHost, text, "think")
                End If

                Dim headRow As New Grid()
                headRow.ColumnDefinitions.Add(New ColumnDefinition With {.Width = GridLength.Auto})
                headRow.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(1, GridUnitType.Star)})
                headRow.ColumnDefinitions.Add(New ColumnDefinition With {.Width = GridLength.Auto})
                Dim labHead As New TextBlock With {
                    .Text = "💭 思考过程", .FontSize = 12, .FontWeight = FontWeights.SemiBold,
                    .Foreground = accent, .VerticalAlignment = VerticalAlignment.Center}
                Dim labCount As New TextBlock With {
                    .Text = $"共 {text.Length} 字", .FontSize = 11,
                    .Foreground = New SolidColorBrush(Color.FromRgb(&H8A, &H8F, &H98)),
                    .VerticalAlignment = VerticalAlignment.Center, .Margin = New Thickness(6, 0, 0, 0)}
                Dim labToggle As New TextBlock With {
                    .Text = "▾ 展开", .FontSize = 12, .Foreground = accent,
                    .VerticalAlignment = VerticalAlignment.Center, .Cursor = Cursors.Hand}
                Grid.SetColumn(labHead, 0)
                Grid.SetColumn(labCount, 1)
                Grid.SetColumn(labToggle, 2)
                headRow.Children.Add(labHead)
                headRow.Children.Add(labCount)
                headRow.Children.Add(labToggle)
                headRow.Cursor = Cursors.Hand
                headRow.ToolTip = "点击展开 / 收起思考过程"

                contentHost.Visibility = Visibility.Collapsed
                Dim isOpen As Boolean = False
                AddHandler headRow.MouseLeftButtonUp,
                    Sub(s2 As Object, e2 As MouseButtonEventArgs)
                        Try
                            isOpen = Not isOpen
                            contentHost.Visibility = If(isOpen, Visibility.Visible, Visibility.Collapsed)
                            labToggle.Text = If(isOpen, "▴ 收起", "▾ 展开")
                            If isOpen Then ScrollAiToBottom()
                        Catch
                        End Try
                    End Sub
                body.Children.Add(headRow)
                body.Children.Add(contentHost)
            Else
                BuildMessageBody(body, text, If(isUser, "user", "answer"))
            End If
            bubble.Child = body

            If isUser Then
                For Each tb As TextBlock In FlattenTextBlocks(body)
                    tb.Foreground = Brushes.White
                Next
            End If

            If PanAiMessages IsNot Nothing Then
                PanAiMessages.Children.Add(bubble)
                _lastRole = role
                HideAiEmptyHint()
                ScrollAiToBottom()
            End If
            Return bubble
        Catch ex As Exception
            Logger.Error(ex, "[XFL] AI 气泡创建失败")
            Return New Border()
        End Try
    End Function

    Private Iterator Function FlattenTextBlocks(root As DependencyObject) As IEnumerable(Of TextBlock)
        If root Is Nothing Then Return
        Dim tb As TextBlock = TryCast(root, TextBlock)
        If tb IsNot Nothing Then Yield tb
        Dim n As Integer = VisualTreeHelper.GetChildrenCount(root)
        For i As Integer = 0 To n - 1
            For Each child As TextBlock In FlattenTextBlocks(VisualTreeHelper.GetChild(root, i))
                Yield child
            Next
        Next
    End Function

    Private Sub UpdateAiBubble(bubble As Border, text As String)
        Try
            If bubble Is Nothing Then Return
            Dim s As String = If(text, "")
            Dim isStatus As Boolean = s.StartsWith("⏳") OrElse s.StartsWith("🔁") OrElse
                                     s.StartsWith("🔍") OrElse s.Contains("正在")
            Dim sp As New StackPanel()
            BuildMessageBody(sp, s, If(isStatus, "status", "answer"))
            bubble.Child = sp
            ScrollAiToBottom()
        Catch ex As Exception
            Logger.Error(ex, "[XFL] AI 气泡更新失败")
        End Try
    End Sub

    Private Sub HideAiEmptyHint()
        Try
            If LabAiEmpty IsNot Nothing Then LabAiEmpty.Visibility = Visibility.Collapsed
        Catch
        End Try
    End Sub

    Private Sub ScrollAiToBottom()
        Try
            If PanAiScroll Is Nothing Then Return
            If _suppressAutoScroll Then Return
            Dispatcher.BeginInvoke(New Action(
                Sub()
                    Try
                        PanAiScroll.ScrollToEnd()
                    Catch
                    End Try
                End Sub), DispatcherPriority.Background)
        Catch
        End Try
    End Sub

#End Region

End Class