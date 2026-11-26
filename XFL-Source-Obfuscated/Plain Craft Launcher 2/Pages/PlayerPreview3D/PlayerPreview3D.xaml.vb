
Imports Microsoft.Web.WebView2.Core
Imports Microsoft.Web.WebView2.Wpf
Imports System.Windows.Media.Imaging

Partial Public Class PlayerPreview3D

    Private _initStarted As Boolean = False
    Private _ready As Boolean = False
    Private _isPremium As Boolean = False
    Private _currentSkinPath As String = ""
    Private _accountSkinPath As String = ""
    Private _currentCapeName As String = ""
    Private _currentModelType As String = "classic"

    Private _shouldBeVisible As Boolean = True

    Public Class AccountCape
        Public Id As String = ""
        Public CapeAlias As String = ""
        Public Url As String = ""
        Public Active As Boolean = False
    End Class
    Private _accountCapes As New List(Of AccountCape)
    Private _accountSig As String = ""
    Private ReadOnly _assetLock As New Object()

    Private ReadOnly CapeNames As Dictionary(Of String, String) = New Dictionary(Of String, String) From {
        {"Migrator", "Migrator"}, {"MapMaker", "MapMaker"}, {"Moderator", "Moderator"},
        {"Translator-Chinese", "Translator-Chinese"}, {"Translator", "Translator"},
        {"Cobalt", "Cobalt"}, {"Vanilla", "Vanilla"}, {"Minecon2011", "Minecon2011"},
        {"Minecon2012", "Minecon2012"}, {"Minecon2013", "Minecon2013"}, {"Minecon2015", "Minecon2015"},
        {"Minecon2016", "Minecon2016"}, {"Cherry Blossom", "Cherry Blossom"},
        {"15th Anniversary", "15th Anniversary"}, {"Purple Heart", "Purple Heart"}}

    Public Sub New()
        InitializeComponent()

        AddHandler Me.Loaded, AddressOf Page_Loaded
        AddHandler BtnUploadSkin.Click, AddressOf BtnUploadSkin_Click

        AddHandler BtnModelClassic.MouseLeftButtonUp,
            Sub(s As Object, e As MouseButtonEventArgs)
                e.Handled = True
                SetModelType("classic")
            End Sub
        AddHandler BtnModelSlim.MouseLeftButtonUp,
            Sub(s As Object, e As MouseButtonEventArgs)
                e.Handled = True
                SetModelType("slim")
            End Sub

        Dispatcher.BeginInvoke(New Action(
            Sub()
                Try
                    InitCore()
                Catch ex As Exception
                    Logger.Error(ex, "[XFL] 3D 预览初始化失败")
                End Try
            End Sub), Windows.Threading.DispatcherPriority.ApplicationIdle)
    End Sub

    Public Property Premium As Boolean
        Get
            Return _isPremium
        End Get
        Set(ByVal value As Boolean)
            _isPremium = value
            Try
                LabCapeHint.Text = If(value, "可切换披风", "离线账号不可使用披风")
                PanCapeList.Visibility = If(value, Visibility.Visible, Visibility.Collapsed)
                If value Then LoadCapeList()
            Catch
            End Try
        End Set
    End Property

    Public Sub SetSkin(path As String)
        Try
            If String.IsNullOrWhiteSpace(path) Then Return
            ApplySkin(path)
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 设置皮肤失败")
        End Try
    End Sub

    Public Sub SetVisible(show As Boolean)
        _shouldBeVisible = show
        UpdateWebViewVisibility(show)
    End Sub

    Private Sub UpdateWebViewVisibility(Optional reason As Boolean? = Nothing)
        Try
            If WebView Is Nothing Then Return
            Dim show As Boolean = _shouldBeVisible AndAlso Me.IsVisible AndAlso
                                  Me.ActualWidth > 1 AndAlso Me.ActualHeight > 1
            If show AndAlso HasModalOverlay() Then show = False
            Dim want As Visibility = If(show, Windows.Visibility.Visible, Windows.Visibility.Collapsed)
            If WebView.Visibility = want Then Return
            WebView.Visibility = want
            Logger.Info($"[XFL] 3D 预览视图{(If(show, "显示", "隐藏"))}" &
                        $"(期望={_shouldBeVisible} 实际可见={Me.IsVisible} 尺寸={Me.ActualWidth:F0}x{Me.ActualHeight:F0})")
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 3D 预览视图切换失败")
        End Try
    End Sub

    Private Function HasModalOverlay() As Boolean
        Try
            If FrmMain Is Nothing Then Return False
            If FrmMain.PanMsg IsNot Nothing AndAlso FrmMain.PanMsg.IsVisible Then Return True
        Catch
        End Try
        Return False
    End Function

    Private Sub HookVisibilityTracking()
        Try
            AddHandler Me.IsVisibleChanged, Sub(s As Object, e As DependencyPropertyChangedEventArgs) UpdateWebViewVisibility()
            AddHandler Me.SizeChanged, Sub(s As Object, e As SizeChangedEventArgs) UpdateWebViewVisibility()

            Try
                If FrmMain IsNot Nothing AndAlso FrmMain.PanMsg IsNot Nothing Then
                    AddHandler FrmMain.PanMsg.IsVisibleChanged,
                        Sub(s As Object, e As DependencyPropertyChangedEventArgs) UpdateWebViewVisibility()
                End If
            Catch
            End Try
            Logger.Info("[XFL] 3D 预览已挂上可见性跟踪")
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 挂可见性跟踪失败")
        End Try
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        HookVisibilityTracking()

        Try
            LoadSkinList()
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 加载皮肤列表失败")
        End Try
    End Sub

    Private Sub InitCore()
        If _initStarted Then Return
        _initStarted = True
        Try
            Dim ver As String = ""
            Try
                ver = CoreWebView2Environment.GetAvailableBrowserVersionString()
            Catch
            End Try
            If String.IsNullOrWhiteSpace(ver) Then
                LabLoading.Text = "未检测到 WebView2 运行时，无法显示 3D 模型。"
                Return
            End If
            LabLoading.Text = "正在加载 3D 模型…"
            AddHandler WebView.CoreWebView2InitializationCompleted, AddressOf OnInitCompleted

            Try
                Dim dir As String = IO.Path.Combine(Paths.Base, "XFL", "WebView2Player3D")
                IO.Directory.CreateDirectory(dir)
                WebView.CreationProperties = New CoreWebView2CreationProperties() With {.UserDataFolder = dir}
                Logger.Info($"[XFL] 3D 预览使用独立数据目录：{dir}")
            Catch ex As Exception
                Logger.Warn(ex, "[XFL] 设置 3D 预览数据目录失败")
            End Try
            Try
                WebView.EnsureCoreWebView2Async()
            Catch ex As Exception
                Logger.Error(ex, "[XFL] 3D 预览内核初始化失败")
            End Try
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 3D 预览初始化失败")
        End Try
    End Sub

    Private Sub OnInitCompleted(sender As Object, e As CoreWebView2InitializationCompletedEventArgs)
        Try
            If Not e.IsSuccess Then
                LabLoading.Text = "3D 模型初始化失败：" & If(e.InitializationException IsNot Nothing, e.InitializationException.Message, "未知错误")
                Return
            End If
            _ready = True

            UpdateWebViewVisibility()
            LabLoading.Visibility = Visibility.Collapsed
            AddHandler WebView.CoreWebView2.NavigationCompleted, AddressOf OnNavCompleted

            Dim htmlPath As String = IO.Path.Combine(Paths.Base, "Pages", "PlayerPreview3D", "player3d.html")
            Dim url As String = "file:///" & htmlPath.Replace("\", "/")
            Logger.Info($"[XFL] 3D 预览导航到：{url}")
            WebView.CoreWebView2.Navigate(url)
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 3D 预览就绪回调失败")
        End Try
    End Sub

    Private Sub OnNavCompleted(sender As Object, e As CoreWebView2NavigationCompletedEventArgs)
        Try
            Logger.Info($"[XFL] 3D 预览页面加载完成：Success={e.IsSuccess}")
            If e.IsSuccess Then

                Dim savedType As String = Settings.Get(Of String)("Player3DModelType")
                If String.IsNullOrWhiteSpace(savedType) Then savedType = "classic"
                ApplyModelTypeVisual(savedType)
                _currentModelType = savedType
                RunInNewThread(
                    Sub()
                        Threading.Thread.Sleep(500)
                        RunInUi(Sub() InjectModelType(savedType))
                    End Sub)

                If _currentSkinPath.Length > 0 Then ApplySkin(_currentSkinPath)
                Logger.Info($"[XFL] 3D 预览已应用模型体型：{savedType}")
            End If
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 3D 预览导航完成回调异常")
        End Try
    End Sub

    Private Sub SetModelType(type As String)
        Try
            If type = _currentModelType Then Return
            _currentModelType = type
            Settings.Set("Player3DModelType", type)
            ApplyModelTypeVisual(type)
            InjectModelType(type)
            Logger.Info($"[XFL] 3D 预览切换模型体型：{If(type = "slim", "纤细", "粗壮")}")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 切换模型体型失败")
        End Try
    End Sub

    Private Sub InjectModelType(type As String)
        Try
            If Not _ready OrElse WebView.CoreWebView2 Is Nothing Then Return
            Dim js As String = "window.__xflSetModelType('" & If(type = "slim", "slim", "classic") & "');"
            WebView.CoreWebView2.ExecuteScriptAsync(js)
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 注入模型体型失败")
        End Try
    End Sub

    Private Sub ApplyModelTypeVisual(type As String)
        Try
            Dim isSlim As Boolean = (type = "slim")
            If BtnModelClassic IsNot Nothing Then
                If isSlim Then
                    BtnModelClassic.Background = Brushes.Transparent
                Else
                    BtnModelClassic.SetResourceReference(Border.BackgroundProperty, "ColorBrush3")
                End If
            End If
            If LabModelClassic IsNot Nothing Then
                If isSlim Then
                    LabModelClassic.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushTextSecondary")
                    LabModelClassic.FontWeight = FontWeights.Normal
                Else
                    LabModelClassic.Foreground = Brushes.White
                    LabModelClassic.FontWeight = FontWeights.SemiBold
                End If
            End If
            If BtnModelSlim IsNot Nothing Then
                If isSlim Then
                    BtnModelSlim.SetResourceReference(Border.BackgroundProperty, "ColorBrush3")
                Else
                    BtnModelSlim.Background = Brushes.Transparent
                End If
            End If
            If LabModelSlim IsNot Nothing Then
                If isSlim Then
                    LabModelSlim.Foreground = Brushes.White
                    LabModelSlim.FontWeight = FontWeights.SemiBold
                Else
                    LabModelSlim.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushTextSecondary")
                    LabModelSlim.FontWeight = FontWeights.Normal
                End If
            End If
        Catch
        End Try
    End Sub

    Private Sub LoadSkinList()
        Try
            PanSkins.Children.Clear()

            If Not String.IsNullOrWhiteSpace(_accountSkinPath) AndAlso IO.File.Exists(_accountSkinPath) Then
                AddSkinItem("账号皮肤（正版）", _accountSkinPath)
            End If
            Dim dirs As String() = {
                IO.Path.Combine(Paths.Base, "XFL", "Skins"),
                IO.Path.Combine(Paths.Base, ".minecraft", "skin")}
            Dim seen As New HashSet(Of String)
            For Each d As String In dirs
                If Not IO.Directory.Exists(d) Then
                    Try : IO.Directory.CreateDirectory(d) : Catch : End Try
                End If
                Try
                    For Each f As String In IO.Directory.EnumerateFiles(d, "*.png", IO.SearchOption.TopDirectoryOnly)
                        Dim name As String = IO.Path.GetFileNameWithoutExtension(f)
                        If seen.Contains(name) Then Continue For
                        seen.Add(name)
                        AddSkinItem(name, f)
                    Next
                Catch
                End Try
            Next

            Dim saved As String = Settings.Get(Of String)("Player3DSkin")
            If String.IsNullOrWhiteSpace(saved) Then Return
            Dim sel As Border = PanSkins.Children.OfType(Of Border)().FirstOrDefault(Function(b) CStr(b.Tag) = saved)
            If sel IsNot Nothing Then
                sel.Tag = "sel"
                _currentSkinPath = saved
                UpdateCurrentSkin(saved)
            End If
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 构建皮肤列表失败")
        End Try
    End Sub

    Private Sub AddSkinItem(name As String, path As String)
        Try
            Dim row As New Border With {
                .CornerRadius = New CornerRadius(6),
                .Padding = New Thickness(8, 6, 8, 6),
                .Margin = New Thickness(0, 0, 0, 4),
                .Cursor = Cursors.Hand,
                .Tag = path}
            row.SetResourceReference(Border.BackgroundProperty, "ColorBrushSurfaceSunken")
            Dim sp As New StackPanel With {.Orientation = Orientation.Horizontal}
            Dim img As New Image With {.Width = 24, .Height = 24, .Stretch = Stretch.Uniform, .Margin = New Thickness(0, 0, 6, 0)}
            Try
                img.Source = New Windows.Media.Imaging.BitmapImage(New Uri(path, UriKind.Absolute))
            Catch
            End Try
            Dim lab As New TextBlock With {
                .Text = name, .FontSize = 12.5, .VerticalAlignment = VerticalAlignment.Center}
            lab.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushTextPrimary")
            sp.Children.Add(img)
            sp.Children.Add(lab)
            row.Child = sp
            AddHandler row.MouseLeftButtonUp,
                Sub(s2 As Object, e2 As MouseButtonEventArgs)
                    Try
                        e2.Handled = True
                        SelectSkin(CStr(CType(s2, Border).Tag))
                    Catch
                    End Try
                End Sub
            PanSkins.Children.Add(row)
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 添加皮肤项失败：" & name)
        End Try
    End Sub

    Private Sub SelectSkin(path As String)
        Try
            If String.IsNullOrWhiteSpace(path) Then Return
            _currentSkinPath = path
            Settings.Set("Player3DSkin", path)
            ApplySkin(path)
            UpdateCurrentSkin(path)

            Dim prev As Border = PanSkins.Children.OfType(Of Border)().FirstOrDefault(Function(b) CStr(b.Tag) = "sel")
            If prev IsNot Nothing Then prev.Tag = CStr(prev.Tag)
            For Each b As Border In PanSkins.Children.OfType(Of Border)()
                b.Opacity = If(CStr(b.Tag) = path, 1.0, 0.55)
            Next
            Logger.Info($"[XFL] 3D 预览切换皮肤：{IO.Path.GetFileName(path)}")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 切换皮肤失败")
        End Try
    End Sub

    Private Sub UpdateCurrentSkin(path As String)
        Try
            ImgCurrentSkin.Source = New Windows.Media.Imaging.BitmapImage(New Uri(path, UriKind.Absolute))
            LabNoSkin.Visibility = Visibility.Collapsed
        Catch
            LabNoSkin.Visibility = Visibility.Visible
        End Try
    End Sub

    Private Sub ApplySkin(path As String)
        Try
            If Not _ready OrElse WebView.CoreWebView2 Is Nothing Then
                Logger.Info("[XFL] 3D 预览内核尚未就绪，记住待载皮肤")
                Return
            End If
            If Not IO.File.Exists(path) Then Return
            Dim bytes As Byte() = IO.File.ReadAllBytes(path)
            Dim b64 As String = System.Convert.ToBase64String(bytes)
            Dim js As String = "window.__xflSetSkin('data:image/png;base64," & b64 & "');"
            WebView.CoreWebView2.ExecuteScriptAsync(js)
            Logger.Info($"[XFL] 3D 预览已应用皮肤：{IO.Path.GetFileName(path)}（{bytes.Length} 字节）")
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 应用皮肤到 3D 模型失败")
        End Try
    End Sub

    Private Sub BtnUploadSkin_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            Dim dir As String = IO.Path.Combine(Paths.Base, "XFL", "Skins")
            Try : IO.Directory.CreateDirectory(dir) : Catch : End Try
            Dim dlg As New Windows.Forms.OpenFileDialog With {
                .Filter = "PNG 图片|*.png|所有文件|*.*",
                .Title = "选择皮肤 PNG"}
            If dlg.ShowDialog() <> Windows.Forms.DialogResult.OK Then Return
            Dim dst As String = IO.Path.Combine(dir, IO.Path.GetFileName(dlg.FileName))
            IO.File.Copy(dlg.FileName, dst, True)

            SelectSkin(dst)
            LoadSkinList()

            If Not _isPremium Then
                Hint("已应用到本地预览（离线账号无法上传皮肤到账号）", HintType.Blue)
                Return
            End If

            Dim choice As Integer = ConfirmAccountChange(
                "已应用到本地预览。" & vbCrLf & vbCrLf &
                "是否把这张皮肤上传到你的账号？" & vbCrLf &
                $"体型：{If(_currentModelType = "slim", "纤细（Slim）", "粗壮（Classic）")}" & vbCrLf & vbCrLf &
                "上传后官网、游戏内与其它启动器都会同步生效。",
                "上传皮肤到账号", "上传到账号", "仅本地预览")
            If choice = 1 Then PushSkinToAccount(dst)
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 上传皮肤失败")
        End Try
    End Sub

    Friend Sub DumpUiLayout()
        Try
            Logger.Info($"[XFL][UI] ===== 更改档案面板布局诊断 =====")
            Logger.Info($"[XFL][UI] 正版={_isPremium}  账号披风集合={_accountCapes.Count} 件  WebView已就绪={_ready}")
            Logger.Info($"[XFL][UI] PanCapes 子项数={PanCapes.Children.Count}  高={PanCapes.ActualHeight:F1}  可见={PanCapes.Visibility}")
            Logger.Info($"[XFL][UI] PanCapeList 高={PanCapeList.ActualHeight:F1}  宽={PanCapeList.ActualWidth:F1}  可见={PanCapeList.Visibility}")
            Logger.Info($"[XFL][UI] LabCapeHint 文本={LabCapeHint.Text}  可见={LabCapeHint.Visibility}  高={LabCapeHint.ActualHeight:F1}")
            Logger.Info($"[XFL][UI] PanSkins 子项数={PanSkins.Children.Count}  高={PanSkins.ActualHeight:F1}")
            Dim sv As DependencyObject = VisualTreeHelper.GetParent(PanCapeList)
            Dim depth As Integer = 0
            Do While sv IsNot Nothing AndAlso depth < 8
                Dim s2 As ScrollViewer = TryCast(sv, ScrollViewer)
                If s2 IsNot Nothing Then
                    Logger.Info($"[XFL][UI] 祖先 ScrollViewer(层{depth}): 内容高={s2.ExtentHeight:F1} 视口高={s2.ViewportHeight:F1} 可滚={s2.ScrollableHeight:F1} 偏移={s2.VerticalOffset:F1}")
                End If
                sv = VisualTreeHelper.GetParent(sv)
                depth += 1
            Loop
            Try
                If FrmMain IsNot Nothing Then
                    Dim p1 As Point = PanCapeList.TransformToAncestor(FrmMain).Transform(New Point(0, 0))
                    Logger.Info($"[XFL][UI] PanCapeList 窗口内位置 x={p1.X:F0} y={p1.Y:F0}")
                    Dim p2 As Point = PanCapes.TransformToAncestor(FrmMain).Transform(New Point(0, 0))
                    Logger.Info($"[XFL][UI] PanCapes 窗口内位置 x={p2.X:F0} y={p2.Y:F0}")
                    Logger.Info($"[XFL][UI] 窗口尺寸 {FrmMain.ActualWidth:F0} x {FrmMain.ActualHeight:F0}")
                End If
            Catch ex As Exception
                Logger.Info("[XFL][UI] 取绝对位置失败：" & ex.Message)
            End Try
            Logger.Info($"[XFL][UI] ===== 诊断结束 =====")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 面板布局诊断失败")
        End Try
    End Sub

    Private Function GetAccessToken() As String
        Try
            If McLoginMsLoader IsNot Nothing AndAlso McLoginMsLoader.State = LoadState.Finished Then
                Dim t As String = McLoginMsLoader.Output.AccessToken
                If Not String.IsNullOrWhiteSpace(t) Then Return t
            End If
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 读取登录凭据失败")
        End Try
        Try
            Return Settings.Get(Of String)("CacheMsV2Access")
        Catch
            Return ""
        End Try
    End Function

    Private Function ConfirmAccountChange(caption As String, title As String,
                                          Optional btn1 As String = "设为账号",
                                          Optional btn2 As String = "仅本地预览") As Integer

        Dim restoreLater As Boolean = False
        Try
            If WebView IsNot Nothing AndAlso WebView.Visibility = Visibility.Visible Then
                WebView.Visibility = Visibility.Collapsed
                restoreLater = True
                Logger.Info("[XFL] 弹窗期间临时隐藏 3D 窗口（避免遮挡确认框）")
            End If
        Catch
        End Try
        Try
            Return MyMsgBox(caption, title, btn1, btn2, "取消", ForceWait:=True)
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 账号变更确认弹窗失败")
            Return 3
        Finally
            If restoreLater Then
                Try
                    UpdateWebViewVisibility()
                Catch
                End Try
            End If
        End Try
    End Function

    Private Function PickActiveSkinUrl(resultJson As String) As String
        Try
            Dim o As JObject = resultJson.DeserializeJson()
            If o Is Nothing Then Return ""
            If o("""errorMessage""") IsNot Nothing Then Throw New Exception(o("""errorMessage""").ToString)
            Dim arr = o("skins")
            If arr Is Nothing Then Return ""
            For Each sk In arr
                If sk("state")?.ToString = "ACTIVE" Then Return If(sk("url")?.ToString, "")
            Next
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 解析皮肤上传返回值失败")
        End Try
        Return ""
    End Function

    Private Sub PushSkinToAccount(path As String)
        RunInNewThread(
            Sub()
                Try
                    Dim token As String = GetAccessToken()
                    If token = "" Then
                        RunInUi(Sub() Hint("没有可用的登录凭据，请先重新登录正版账号", HintType.Red))
                        Return
                    End If
                    Dim modelVariant As String = If(_currentModelType = "slim", "slim", "classic")
                    Logger.Info($"[XFL] 正在上传皮肤到账号：{IO.Path.GetFileName(path)}（{modelVariant}）")
                    Dim result As String = NetRequestByClientRetry(
                        "https://api.minecraftservices.com/minecraft/profile/skins", HttpMethod.Post,
                        Content:=New Net.Http.MultipartFormDataContent From {
                            {New Net.Http.StringContent(modelVariant), "variant"},
                            {New Net.Http.ByteArrayContent(FileUtils.ReadAsBytes(path)), "file", PathUtils.GetLastPart(path)}
                        },
                        Headers:={{"Authorization", "Bearer " & token}, {"Accept", "*/*"}, {"User-Agent", "MojangSharp/0.1"}})

                    Dim newUrl As String = PickActiveSkinUrl(result)
                    If newUrl <> "" Then
                        Try
                            MySkin.ReloadCache(newUrl)
                        Catch
                        End Try
                    End If
                    RunInUi(Sub() Hint("皮肤已上传到账号（官网/游戏内都会同步生效）", HintType.Green))
                    Logger.Info("[XFL] 皮肤上传成功")
                Catch ex As Exception
                    Logger.Error(ex, "[XFL] 上传皮肤到账号失败")
                    RunInUi(Sub() Hint("上传皮肤失败：" & ex.Message, HintType.Red))
                End Try
            End Sub, "XFL PushSkin")
    End Sub

    Private Sub PushCapeToAccount(capeName As String, capeId As String)
        RunInNewThread(
            Sub()
                Try
                    Dim token As String = GetAccessToken()
                    If token = "" Then
                        RunInUi(Sub() Hint("没有可用的登录凭据，请先重新登录正版账号", HintType.Red))
                        Return
                    End If
                    Dim removeCape As Boolean = String.IsNullOrWhiteSpace(capeId)
                    Dim body As String = If(removeCape, "", New JObject(New JProperty("capeId", capeId)).ToString(0))
                    Logger.Info($"[XFL] 正在{(If(removeCape, "取下", "设置"))}账号披风：{capeName}")
                    Dim result As String = NetRequestByClientRetry(
                        "https://api.minecraftservices.com/minecraft/profile/capes/active",
                        If(removeCape, HttpMethod.Delete, HttpMethod.Put),
                        Content:=body, ContentType:="application/json",
                        Headers:={{"Authorization", "Bearer " & token}})
                    If result IsNot Nothing AndAlso result.Contains("""errorMessage""") Then
                        Throw New Exception(result.DeserializeJson()("errorMessage").ToString)
                    End If

                    UpdateProfileCapeState(If(removeCape, "", capeId))
                    RunInUi(
                        Sub()
                            Try
                                Hint(If(removeCape,
                                        "已从账号上取下披风（官网/游戏内都会同步生效）",
                                        $"已将「{capeName}」设为账号披风（官网/游戏内都会同步生效）"), HintType.Green)
                                LoadCapeList()
                            Catch
                            End Try
                        End Sub)
                    Logger.Info($"[XFL] 账号披风修改成功：{capeName}")
                Catch ex As Exception
                    Logger.Error(ex, "[XFL] 修改账号披风失败")
                    RunInUi(Sub() Hint("修改账号披风失败：" & ex.Message, HintType.Red))
                End Try
            End Sub, "XFL PushCape")
    End Sub

    Private Sub UpdateProfileCapeState(activeCapeId As String)
        Try
            SyncLock _assetLock
                For Each c In _accountCapes
                    c.Active = (c.Id = activeCapeId AndAlso activeCapeId <> "")
                Next
            End SyncLock
            Dim json As String = GetAccountProfileJson()
            If String.IsNullOrWhiteSpace(json) Then Return
            Dim profile As JObject = json.DeserializeJson()
            Dim arr = profile("capes")
            If arr Is Nothing Then Return
            For Each c In arr
                c("state") = If(c("id")?.ToString = activeCapeId AndAlso activeCapeId <> "", "ACTIVE", "INACTIVE")
            Next
            Dim newJson As String = profile.ToString(0)
            Try
                McLoginMsLoader.Output.ProfileJson = newJson
            Catch
            End Try
            Try
                Settings.Set("CacheMsV2ProfileJson", newJson)
            Catch
            End Try
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 更新本地档案披风状态失败")
        End Try
    End Sub

    Public Shared Sub RequestAccountSync(Optional force As Boolean = False)
        Try
            If FrmMain Is Nothing Then Return
            Dim pv As PlayerPreview3D = FrmMain.PlayerPreview
            If pv IsNot Nothing Then pv.SyncAccount(force)
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 请求同步账号档案失败")
        End Try
    End Sub

    Private Function GetAccountProfileJson() As String
        Try
            If McLoginMsLoader IsNot Nothing AndAlso McLoginMsLoader.State = LoadState.Finished Then
                Dim p As String = McLoginMsLoader.Output.ProfileJson
                If Not String.IsNullOrWhiteSpace(p) Then Return p
            End If
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 读取登录档案失败，改用缓存")
        End Try
        Try
            Return Settings.Get(Of String)("CacheMsV2ProfileJson")
        Catch
            Return ""
        End Try
    End Function

    Private Function AssetDir() As String
        Dim d As String = IO.Path.Combine(Paths.Base, "XFL", "AccountAssets")
        IO.Directory.CreateDirectory(d)
        Return d
    End Function

    Private Function DownloadAsset(url As String, tag As String) As String
        Try
            If String.IsNullOrWhiteSpace(url) Then Return ""

            If url.Contains("minecraft.net/") Then url = url.Replace("http://", "https://")
            Dim dir As String = AssetDir()
            Dim f As String = IO.Path.Combine(dir, tag & "_" & url.GetStableHashCode() & ".png")
            If IO.File.Exists(f) AndAlso New IO.FileInfo(f).Length > 0 Then Return f
            Dim tmp As String = f & ".PCLDownloading"
            NetDownloadByClient(url, tmp)
            FileUtils.Move(tmp, f)
            Logger.Info($"[XFL] 账号资源下载成功（{tag}）：{IO.Path.GetFileName(f)}")
            Return f
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 账号资源下载失败：" & tag & " " & url)
            Return ""
        End Try
    End Function

    Public Sub SyncAccount(Optional force As Boolean = False)
        RunInNewThread(
            Sub()
                Try
                    Dim profileJson As String = GetAccountProfileJson()
                    If String.IsNullOrWhiteSpace(profileJson) Then
                        Logger.Info("[XFL] 3D 预览：没有可同步的账号档案（未登录或档案为空）")
                        Return
                    End If
                    Dim profile As JObject
                    Try
                        profile = profileJson.DeserializeJson()
                    Catch ex As Exception
                        Logger.Warn(ex, "[XFL] 账号档案不是合法 JSON，跳过同步")
                        Return
                    End Try

                    Dim skinUrl As String = ""
                    Dim skinVariant As String = "CLASSIC"
                    Try
                        Dim skins = profile("skins")
                        If skins IsNot Nothing AndAlso skins.Count > 0 Then
                            Dim sel As JToken = Nothing
                            For Each s In skins
                                If s("state")?.ToString = "ACTIVE" Then
                                    sel = s
                                    Exit For
                                End If
                            Next
                            If sel Is Nothing Then sel = skins(0)
                            skinUrl = If(sel("url")?.ToString, "")
                            skinVariant = If(sel("variant")?.ToString, "CLASSIC")
                        End If
                    Catch ex As Exception
                        Logger.Warn(ex, "[XFL] 解析账号皮肤失败")
                    End Try

                    Dim capes As New List(Of AccountCape)
                    Try
                        Dim arr = profile("capes")
                        If arr IsNot Nothing Then
                            For Each c In arr
                                capes.Add(New AccountCape With {
                                    .Id = If(c("id")?.ToString, ""),
                                    .CapeAlias = If(c("alias")?.ToString, "Cape"),
                                    .Url = If(c("url")?.ToString, ""),
                                    .Active = (c("state")?.ToString = "ACTIVE")})
                            Next
                        End If
                    Catch ex As Exception
                        Logger.Warn(ex, "[XFL] 解析账号披风失败")
                    End Try

                    Dim sb As New System.Text.StringBuilder
                    sb.Append(skinUrl).Append("|").Append(skinVariant)
                    For Each c In capes
                        sb.Append("|").Append(c.CapeAlias).Append("=").Append(c.Url).Append(c.Active)
                    Next
                    Dim sig As String = sb.ToString
                    SyncLock _assetLock
                        If Not force AndAlso sig = _accountSig Then
                            Logger.Info("[XFL] 3D 预览：账号档案无变化，跳过同步")
                            Return
                        End If
                        _accountSig = sig
                        _accountCapes = capes
                    End SyncLock
                    Logger.Info($"[XFL] 3D 预览账号档案：皮肤={If(skinUrl = "", "无", "有")}，体型={skinVariant}，披风 {capes.Count} 件")

                    If skinUrl <> "" Then
                        Dim f As String = DownloadAsset(skinUrl, "skin")
                        If f <> "" Then
                            RunInUi(
                                Sub()
                                    Try
                                        _currentSkinPath = f
                                        _accountSkinPath = f
                                        ApplySkin(f)
                                        UpdateCurrentSkin(f)
                                        LoadSkinList()
                                    Catch
                                    End Try
                                End Sub)
                        End If
                    End If

                    Dim mt As String = If(skinVariant.ToUpper = "SLIM", "slim", "classic")
                    RunInUi(
                        Sub()
                            Try
                                _currentModelType = mt
                                ApplyModelTypeVisual(mt)
                                InjectModelType(mt)

                                If Not _isPremium Then
                                    _isPremium = True
                                    Logger.Info("[XFL] 3D 预览账号状态已纠正为：正版（已取到账号档案）")
                                    LoadCapeList()
                                End If
                            Catch
                            End Try
                        End Sub)

                    Dim activeCape As AccountCape = Nothing
                    For Each c In capes
                        If c.Active AndAlso c.Url <> "" Then
                            activeCape = c
                            Exit For
                        End If
                    Next
                    If activeCape IsNot Nothing Then
                        Dim cf As String = DownloadAsset(activeCape.Url, "cape")
                        If cf <> "" Then
                            RunInUi(
                                Sub()
                                    Try
                                        ApplyCapeFile(cf, activeCape.CapeAlias)
                                    Catch
                                    End Try
                                End Sub)
                        End If
                    End If

                    RunInUi(
                        Sub()
                            Try
                                LoadCapeList()

                                If Environment.GetEnvironmentVariable("XFL_TEST_DUMPUI") = "1" Then
                                    RunInNewThread(
                                        Sub()
                                            Thread.Sleep(20000)
                                            RunInUi(
                                                Sub()
                                                    Try
                                                        DumpUiLayout()
                                                    Catch
                                                    End Try
                                                End Sub)
                                        End Sub)
                                End If

                                Dim testCape As String = Environment.GetEnvironmentVariable("XFL_TEST_CAPE")
                                If String.IsNullOrWhiteSpace(testCape) Then Return
                                Dim idx As Integer
                                If Not Integer.TryParse(testCape, idx) Then Return

                                Dim delayMs As Integer = 6000
                                Integer.TryParse(Environment.GetEnvironmentVariable("XFL_TEST_CAPE_DELAY"), delayMs)
                                Logger.Info($"[XFL] 测试钩子：将在 {delayMs} ms 后选择披风 #{idx}")
                                RunInNewThread(
                                    Sub()
                                        Thread.Sleep(delayMs)
                                        RunInUi(
                                            Sub()
                                                Try
                                                    If idx = 0 Then
                                                        Logger.Info("[XFL] 测试钩子：自动选择「无披风」")
                                                        SelectCape("无披风", "", "")
                                                        Return
                                                    End If
                                                    Dim list As New List(Of AccountCape)(_accountCapes)
                                                    If idx >= 1 AndAlso idx <= list.Count Then
                                                        Dim c As AccountCape = list(idx - 1)
                                                        Logger.Info($"[XFL] 测试钩子：自动选择披风 #{idx} = {c.CapeAlias}｜id={c.Id}")
                                                        SelectCape(c.CapeAlias, c.Url, c.Id)
                                                    Else
                                                        Logger.Info($"[XFL] 测试钩子：索引 {idx} 越界（共 {list.Count} 件）")
                                                    End If
                                                Catch ex As Exception
                                                    Logger.Warn(ex, "[XFL] 测试钩子执行失败")
                                                End Try
                                            End Sub)
                                    End Sub)
                            Catch ex As Exception
                                Logger.Warn(ex, "[XFL] 测试钩子触发披风选择失败")
                            End Try
                        End Sub)
                Catch ex As Exception
                    Logger.Error(ex, "[XFL] 同步账号档案失败")
                End Try
            End Sub, "XFL SyncAccount")
    End Sub

    Private Sub ApplyCapeFile(path As String, capeName As String)
        Try
            If String.IsNullOrWhiteSpace(path) OrElse Not IO.File.Exists(path) Then Return
            Dim bytes As Byte() = IO.File.ReadAllBytes(path)
            InjectCape(System.Convert.ToBase64String(bytes))
            _currentCapeName = capeName
            Logger.Info($"[XFL] 3D 预览披风已应用：{capeName}（{bytes.Length} 字节）")
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 应用披风文件失败")
        End Try
    End Sub

    Private Sub LoadCapeThumb(img As System.Windows.Controls.Image, url As String)
        RunInNewThread(
            Sub()
                Try
                    Dim f As String = DownloadAsset(url, "cape")
                    If f = "" Then Return
                    Dim bmp As New BitmapImage()
                    bmp.BeginInit()
                    bmp.CacheOption = BitmapCacheOption.OnLoad
                    bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache
                    bmp.UriSource = New Uri(f, UriKind.Absolute)
                    bmp.EndInit()
                    bmp.Freeze()
                    Dim rect As System.Windows.Int32Rect
                    If bmp.PixelWidth >= 22 AndAlso bmp.PixelHeight >= 17 Then

                        rect = New System.Windows.Int32Rect(1, 1, 10, 16)
                    Else
                        rect = New System.Windows.Int32Rect(0, 0, Math.Max(1, bmp.PixelWidth), Math.Max(1, bmp.PixelHeight))
                    End If
                    Dim crop As New CroppedBitmap(bmp, rect)
                    crop.Freeze()
                    RunInUi(Sub() img.Source = crop)
                Catch ex As Exception
                    Logger.Warn(ex, "[XFL] 加载披风图案失败")
                End Try
            End Sub)
    End Sub

    Private Sub AddCapeRow(capeName As String, url As String, active As Boolean, capeId As String)
        Try
            Dim row As New Border With {
                .CornerRadius = New CornerRadius(6),
                .Padding = New Thickness(6, 4, 8, 4),
                .Margin = New Thickness(0, 0, 0, 4),
                .Cursor = Cursors.Hand,
                .Tag = url}
            row.SetResourceReference(Border.BackgroundProperty, "ColorBrushSurfaceSunken")
            Dim sp As New StackPanel With {.Orientation = Orientation.Horizontal}
            Dim img As New System.Windows.Controls.Image With {
                .Width = 18, .Height = 26, .Stretch = Stretch.Uniform,
                .VerticalAlignment = VerticalAlignment.Center,
                .Margin = New Thickness(0, 0, 6, 0)}
            System.Windows.Media.RenderOptions.SetBitmapScalingMode(img, System.Windows.Media.BitmapScalingMode.NearestNeighbor)
            Dim lab As New TextBlock With {
                .Text = capeName, .FontSize = 12,
                .VerticalAlignment = VerticalAlignment.Center,
                .TextTrimming = TextTrimming.CharacterEllipsis}
            lab.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushTextPrimary")
            If active Then lab.FontWeight = FontWeights.SemiBold
            sp.Children.Add(img)
            sp.Children.Add(lab)
            row.Child = sp
            AddHandler row.MouseLeftButtonUp,
                Sub(s2 As Object, e2 As MouseButtonEventArgs)
                    Try
                        e2.Handled = True
                        SelectCape(capeName, url, capeId)
                    Catch
                    End Try
                End Sub
            PanCapes.Children.Add(row)
            If url <> "" Then LoadCapeThumb(img, url)
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 添加披风项失败")
        End Try
    End Sub

    Private Sub LoadCapeList()
        Try
            PanCapes.Children.Clear()

            Dim acc As List(Of AccountCape)
            SyncLock _assetLock
                acc = New List(Of AccountCape)(_accountCapes)
            End SyncLock
            If acc.Count > 0 Then

                LabCapeHint.Text = "共 " & acc.Count & " 件可用披风，选择后点「设为账号」才会写入账号"
                LabCapeHint.Visibility = Visibility.Visible
                AddCapeRow("无披风", "", False, "")
                For Each c In acc
                    AddCapeRow(If(c.CapeAlias = "", "Cape", c.CapeAlias), c.Url, c.Active, c.Id)
                Next
                Return
            End If

            LabCapeHint.Text = If(_isPremium OrElse GetAccessToken() <> "",
                                  "正在读取账号披风…",
                                  "登录正版账号后可选择披风")
            LabCapeHint.Visibility = Visibility.Visible
            Dim tip As New TextBlock With {
                .Text = If(_isPremium, "正在读取账号披风…", "登录正版账号后可选择披风"),
                .FontSize = 11.5, .TextWrapping = TextWrapping.Wrap,
                .Margin = New Thickness(2, 2, 2, 2)}
            tip.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushTextTertiary")
            PanCapes.Children.Add(tip)
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 构建披风列表失败")
        End Try
    End Sub

    Private Sub SelectCape(capeName As String, url As String, capeId As String)
        Try

            If Not _isPremium AndAlso GetAccessToken() = "" Then
                Hint("离线账号不可使用披风", HintType.Red)
                Return
            End If
            Logger.Info($"[XFL] 选择了披风：{capeName}｜id={If(String.IsNullOrWhiteSpace(capeId), "(无)", capeId)}｜url={If(url = "", "(无)", "已提供")}")
            Dim removeCape As Boolean = String.IsNullOrWhiteSpace(url)

            Dim caption As String = If(removeCape,
                "确定要从账号上取下披风吗？" & vbCrLf & vbCrLf & "官网、游戏内与其它启动器都会同步生效。",
                $"确定把「{capeName}」设为你的账号披风吗？" & vbCrLf & vbCrLf & "官网、游戏内与其它启动器都会同步生效。")
            Dim choice As Integer = ConfirmAccountChange(caption, "更改账号披风")
            If choice = 3 Then Return

            If removeCape Then
                _currentCapeName = ""
                InjectCape("")
                Logger.Info("[XFL] 3D 预览已移除披风")
            Else
                _currentCapeName = capeName
                RunInNewThread(
                    Sub()
                        Try
                            Dim f As String = DownloadAsset(url, "cape")
                            If f = "" Then
                                RunInUi(Sub() Hint("披风图案下载失败", HintType.Red))
                                Return
                            End If
                            RunInUi(Sub() ApplyCapeFile(f, capeName))
                        Catch ex As Exception
                            RunInUi(Sub() Hint("披风下载失败：" & ex.Message, HintType.Red))
                        End Try
                    End Sub)
            End If

            If choice = 1 Then PushCapeToAccount(capeName, capeId)
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 切换披风失败")
        End Try
    End Sub

    Private Sub InjectCape(b64 As String)
        Try
            If Not _ready OrElse WebView.CoreWebView2 Is Nothing Then Return

            Dim js As String = If(String.IsNullOrWhiteSpace(b64),
                                  "window.__xflSetCape('');",
                                  "window.__xflSetCape('data:image/png;base64," & b64 & "');")
            WebView.CoreWebView2.ExecuteScriptAsync(js)
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 注入披风到 3D 模型失败")
        End Try
    End Sub

End Class
