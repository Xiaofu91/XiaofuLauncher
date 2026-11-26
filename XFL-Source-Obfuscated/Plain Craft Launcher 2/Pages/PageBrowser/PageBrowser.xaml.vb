
Imports Microsoft.Web.WebView2.Core

Partial Public Class PageBrowser

    Private _initStarted As Boolean = False
    Private _ready As Boolean = False

    Private ReadOnly Property HomeUrl As String
        Get
            Try
                Dim u As String = Settings.Get(Of String)("BrowserHome")
                If Not String.IsNullOrWhiteSpace(u) Then Return u.Trim()
            Catch
            End Try
            Return "https://www.bing.com"
        End Get
    End Property

    Public Sub New()
        InitializeComponent()

        Try
            Dim dir As String = IO.Path.Combine(Paths.Base, "XFL", "WebView2Browser")
            IO.Directory.CreateDirectory(dir)
            WebView.CreationProperties = New Microsoft.Web.WebView2.Wpf.CoreWebView2CreationProperties With {.UserDataFolder = dir}
            Logger.Info($"[XFL] 浏览器页使用独立数据目录：{dir}")
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 浏览器页初始化失败")
        End Try

        Try
            _keeper = New XflWebViewKeeper(Me, WebView, "浏览器页")
            _keeper.HookTracking()
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 浏览器页空域管理器建立失败")
        End Try
        AddHandler Me.Loaded, AddressOf BrowserPage_Loaded
        AddHandler BtnBack.Click, AddressOf BtnBack_Click
        AddHandler BtnForward.Click, AddressOf BtnForward_Click
        AddHandler BtnRefresh.Click, AddressOf BtnRefresh_Click
        AddHandler BtnHome.Click, AddressOf BtnHome_Click
        AddHandler BtnGo.Click, AddressOf BtnGo_Click
        AddHandler TxtUrl.KeyDown, AddressOf TxtUrl_KeyDown
        AddHandler BtnSmallWin.Click, AddressOf BtnSmallWin_Click
    End Sub

    Private Sub BrowserPage_Loaded(sender As Object, e As RoutedEventArgs)
        Try
            InitBrowserCore()
            UpdateNavButtons()
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 浏览器页加载失败")
        End Try
    End Sub

    Private Sub InitBrowserCore()
        If _initStarted Then Return
        _initStarted = True
        Try

            Dim ver As String = ""
            Try
                ver = CoreWebView2Environment.GetAvailableBrowserVersionString()
            Catch
            End Try
            If String.IsNullOrWhiteSpace(ver) Then
                LabBrowserHint.Text = "未检测到「Microsoft Edge WebView2 运行时」，内置浏览器无法启动。" & vbCrLf & vbCrLf &
                                      "解决办法（任选其一）：" & vbCrLf &
                                      "· 安装 Microsoft Edge 浏览器（Windows 一般已自带）" & vbCrLf &
                                      "· 或安装微软官方免费的 WebView2 运行时：" & vbCrLf &
                                      "  https://developer.microsoft.com/microsoft-edge/webview2/"
                Ctl.Visibility = Visibility.Collapsed
                LabBrowserHint.Visibility = Visibility.Visible
                Logger.Warn("[XFL] 未检测到 WebView2 运行时，内置浏览器不可用")
                Return
            End If
            Logger.Info($"[XFL] WebView2 运行时版本 {ver}，开始初始化浏览器内核")

            LabBrowserHint.Text = "正在加载浏览器内核…"
            AddHandler WebView.CoreWebView2InitializationCompleted, AddressOf WebView_CoreWebView2InitializationCompleted

            Try
                Ctl.EnsureCoreWebView2Async()
            Catch ex As Exception
                Logger.Error(ex, "[XFL] 浏览器内核初始化失败")
                LabBrowserHint.Text = "浏览器内核初始化失败：" & ex.Message
            End Try
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 浏览器内核初始化失败")
            LabBrowserHint.Text = "浏览器内核初始化失败：" & ex.Message
        End Try
    End Sub

    Private _wvRetried As Integer = 0

    Private _webViewNew As Microsoft.Web.WebView2.Wpf.WebView2 = Nothing

    Private _keeper As XflWebViewKeeper = Nothing

    Private ReadOnly Property Ctl As Microsoft.Web.WebView2.Wpf.WebView2
        Get
            Return If(_webViewNew, WebView)
        End Get
    End Property

    Private Sub WebView_CoreWebView2InitializationCompleted(sender As Object, e As CoreWebView2InitializationCompletedEventArgs)
        Try
            If Not e.IsSuccess Then
                Dim msg As String = If(e.InitializationException IsNot Nothing, e.InitializationException.Message, "未知错误")
                Logger.Error("[XFL] WebView2 初始化未成功：" & msg)

                If msg.Contains("0x800700AA") OrElse msg.Contains("资源在使用中") Then
                    If _wvRetried < 2 Then
                        _wvRetried += 1
                        LabBrowserHint.Text = $"浏览器内核初始化异常，正在自动修复（第 {_wvRetried}/2 次）…"
                        RunInNewThread(
                            Sub()
                                Try

                                    Dim altDir As String = IO.Path.Combine(
                                        Paths.Base, "XFL", "WebView2_r" & Date.Now.ToString("HHmmss"))
                                    IO.Directory.CreateDirectory(altDir)
                                    RunInUi(
                                        Sub()
                                            Try

                                                Dim host As Windows.Controls.Panel = TryCast(WebView.Parent, Windows.Controls.Panel)
                                                If host IsNot Nothing Then host.Children.Remove(WebView)
                                                Try : WebView.Dispose() : Catch : End Try

                                                Dim nv As New Microsoft.Web.WebView2.Wpf.WebView2 With {
                                                    .Visibility = Visibility.Collapsed,
                                                    .CreationProperties = New Microsoft.Web.WebView2.Wpf.CoreWebView2CreationProperties With {.UserDataFolder = altDir}}
                                                If host IsNot Nothing Then host.Children.Add(nv)
                                                _webViewNew = nv

                                                If _keeper IsNot Nothing Then _keeper.BindControl(nv)
                                                Logger.Warn($"[XFL] 浏览器已重建视图，使用新数据目录：{altDir}")
                                                InitBrowserCore()
                                            Catch ex3 As Exception
                                                Logger.Error(ex3, "[XFL] 重建浏览器视图失败")
                                            End Try
                                        End Sub)
                                Catch ex As Exception
                                    Logger.Error(ex, "[XFL] 浏览器初始化重试失败")
                                End Try
                            End Sub)
                        Return
                    End If
                End If
                LabBrowserHint.Text = "浏览器内核初始化失败：" & msg
                Ctl.Visibility = Visibility.Collapsed
                LabBrowserHint.Visibility = Visibility.Visible
                Return
            End If
            _ready = True

            If _keeper IsNot Nothing Then
                _keeper.SetExpected(True)
            Else
                Ctl.Visibility = Visibility.Visible
            End If
            LabBrowserHint.Visibility = Visibility.Collapsed

            Dim core As CoreWebView2 = Ctl.CoreWebView2

            AddHandler core.SourceChanged, AddressOf Core_SourceChanged
            AddHandler core.HistoryChanged, AddressOf Core_HistoryChanged
            AddHandler core.NavigationCompleted, AddressOf Core_NavigationCompleted

            AddHandler core.NewWindowRequested,
                Sub(s2 As Object, e2 As CoreWebView2NewWindowRequestedEventArgs)
                    Try
                        e2.Handled = True
                        Core_NavigateTo(e2.Uri)
                    Catch
                    End Try
                End Sub
            If Not String.IsNullOrWhiteSpace(_pendingUrl) Then
                Logger.Info("[XFL] 浏览器内核就绪，打开指定地址：" & _pendingUrl)
                Dim pu As String = _pendingUrl
                _pendingUrl = ""
                Core_NavigateTo(pu)
            Else
                Logger.Info("[XFL] 浏览器内核就绪，打开主页：" & HomeUrl)
                Core_NavigateTo(HomeUrl)
            End If
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 浏览器内核就绪回调失败")
        End Try
    End Sub

    Private Sub Core_SourceChanged(sender As Object, e As Object)
        RunInUi(Sub()
                    Try
                        If Not String.IsNullOrWhiteSpace(WebView.Source?.ToString) Then TxtUrl.Text = WebView.Source.ToString()
                        UpdateNavButtons()
                    Catch
                    End Try
                End Sub, True)
    End Sub

    Private Sub Core_HistoryChanged(sender As Object, e As Object)
        RunInUi(Sub()
                    Try
                        UpdateNavButtons()
                    Catch
                    End Try
                End Sub, True)
    End Sub

    Private Sub Core_NavigationCompleted(sender As Object, e As CoreWebView2NavigationCompletedEventArgs)
        RunInUi(Sub()
                    Try
                        UpdateNavButtons()
                    Catch
                    End Try
                End Sub, True)
    End Sub

    Private Sub UpdateNavButtons()
        Try
            If Not _ready Then Return
            Dim core As CoreWebView2 = WebView.CoreWebView2
            If core Is Nothing Then Return
            BtnBack.Opacity = If(core.CanGoBack, 1.0, 0.4)
            BtnForward.Opacity = If(core.CanGoForward, 1.0, 0.4)
        Catch
        End Try
    End Sub

    Private Function NormalizeInput(text As String) As String
        Dim s As String = If(text, "").Trim()
        If s.Length = 0 Then Return HomeUrl
        If s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) OrElse
           s.StartsWith("https://", StringComparison.OrdinalIgnoreCase) OrElse
           s.StartsWith("about:", StringComparison.OrdinalIgnoreCase) OrElse
           s.StartsWith("edge:", StringComparison.OrdinalIgnoreCase) Then Return s

        If System.Text.RegularExpressions.Regex.IsMatch(s, "^[A-Za-z0-9\-\.]+\.[A-Za-z]{2,}(/.*)?$") Then Return "https://" & s

        Return "https://www.bing.com/search?q=" & Uri.EscapeDataString(s)
    End Function

    Public Sub NavigateTo(url As String)
        Try
            If String.IsNullOrWhiteSpace(url) Then Return

            _pendingUrl = url
            If _ready Then
                _pendingUrl = ""
                Core_NavigateTo(url)
            End If
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 浏览器页导航失败")
        End Try
    End Sub

    Private _pendingUrl As String = ""

    Private Sub Core_NavigateTo(url As String)
        Try
            If Not _ready OrElse WebView.CoreWebView2 Is Nothing Then Return
            WebView.CoreWebView2.Navigate(url)
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 浏览器导航失败：" & url)
        End Try
    End Sub

    Private Sub BtnBack_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            If _ready AndAlso WebView.CoreWebView2 IsNot Nothing AndAlso WebView.CoreWebView2.CanGoBack Then WebView.CoreWebView2.GoBack()
        Catch
        End Try
    End Sub

    Private Sub BtnForward_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            If _ready AndAlso WebView.CoreWebView2 IsNot Nothing AndAlso WebView.CoreWebView2.CanGoForward Then WebView.CoreWebView2.GoForward()
        Catch
        End Try
    End Sub

    Private Sub BtnRefresh_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            If _ready AndAlso WebView.CoreWebView2 IsNot Nothing Then WebView.CoreWebView2.Reload()
        Catch
        End Try
    End Sub

    Private Sub BtnHome_Click(sender As Object, e As MouseButtonEventArgs)
        Core_NavigateTo(HomeUrl)
    End Sub

    Private Sub BtnGo_Click(sender As Object, e As MouseButtonEventArgs)
        Core_NavigateTo(NormalizeInput(TxtUrl.Text))
    End Sub

    Private Sub TxtUrl_KeyDown(sender As Object, e As KeyEventArgs)
        Try
            If e.Key = Key.Enter Then
                e.Handled = True
                Core_NavigateTo(NormalizeInput(TxtUrl.Text))
            End If
        Catch
        End Try
    End Sub

    Private Sub BtnSmallWin_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            If Not _ready Then
                MsgBox("小窗需要浏览器内核就绪后再用。", MsgBoxStyle.Information, "小窗模式")
                Return
            End If
            FormWebSmall.ShowSmall(CurrentPageUrl())
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 打开小窗失败")
        End Try
    End Sub

    Private Function CurrentPageUrl() As String
        Try
            If WebView IsNot Nothing AndAlso WebView.Source IsNot Nothing Then Return WebView.Source.ToString()
        Catch
        End Try
        Return HomeUrl
    End Function

    Public Sub OnPageHide()
        Try
            If TxtUrl IsNot Nothing Then TxtUrl.Text = TxtUrl.Text
        Catch
        End Try
    End Sub

End Class