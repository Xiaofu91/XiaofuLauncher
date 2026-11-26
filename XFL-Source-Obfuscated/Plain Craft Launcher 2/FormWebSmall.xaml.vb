
Imports Microsoft.Web.WebView2.Core

Public Class FormWebSmall

    Private Shared _instance As FormWebSmall = Nothing

    Private _initStarted As Boolean = False
    Private _ready As Boolean = False
    Private _pendingUrl As String = ""

    Public Shared Sub ShowSmall(url As String, Optional autoPlayHint As Boolean = False)
        Try
            If _instance Is Nothing OrElse Not _instance.IsLoaded Then
                _instance = New FormWebSmall
                _instance.Show()
            Else
                _instance.Activate()
            End If
            _instance.Navigate(url)
            Logger.Info("[XFL] 小窗模式已打开：" & If(url, ""))
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 打开小窗失败")
        End Try
    End Sub

    Public Sub New()
        InitializeComponent()

        Try
            Dim dir As String = IO.Path.Combine(Paths.Base, "XFL", "WebView2Small")
            IO.Directory.CreateDirectory(dir)
            WebView.CreationProperties = New Microsoft.Web.WebView2.Wpf.CoreWebView2CreationProperties With {.UserDataFolder = dir}
            Logger.Info($"[XFL] 小窗使用独立数据目录：{dir}")
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 小窗设置数据目录失败")
        End Try
        AddHandler Me.Loaded, AddressOf Small_Loaded
        AddHandler Me.Closed, AddressOf Small_Closed
        AddHandler BtnSmallBack.Click, AddressOf BtnSmallBack_Click
        AddHandler BtnSmallTop.Click, AddressOf BtnSmallTop_Click
        AddHandler BtnSmallBrowser.Click, AddressOf BtnSmallBrowser_Click
    End Sub

    Private Sub Small_Loaded(sender As Object, e As EventArgs)
        Try
            InitCore()
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 小窗加载失败")
        End Try
    End Sub

    Private Sub Small_Closed(sender As Object, e As EventArgs)
        Try
            If WebView IsNot Nothing Then WebView.Dispose()
        Catch
        End Try
        _instance = Nothing
    End Sub

    Public Sub Navigate(url As String)
        Try
            If String.IsNullOrWhiteSpace(url) Then Return
            _pendingUrl = url
            LabSmallUrl.Text = url
            If _ready Then
                _pendingUrl = ""
                WebView.CoreWebView2.Navigate(url)
            End If
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 小窗导航失败")
        End Try
    End Sub

    Public Function CurrentUrl() As String
        Try
            If _ready AndAlso WebView.Source IsNot Nothing Then Return WebView.Source.ToString()
        Catch
        End Try
        Return _pendingUrl
    End Function

    Private Sub InitCore()
        If _initStarted Then Return
        _initStarted = True
        Try
            Dim ver As String = ModWebView.RuntimeVersion()
            If String.IsNullOrWhiteSpace(ver) Then
                LabSmallHint.Text = "未检测到「Microsoft Edge WebView2 运行时」，小窗无法显示网页。" & vbCrLf &
                                    "可安装 Microsoft Edge 浏览器，或安装微软官方免费的 WebView2 运行时。"
                Return
            End If
            LabSmallHint.Text = "正在准备小窗…"
            AddHandler WebView.CoreWebView2InitializationCompleted, AddressOf WebView_CoreWebView2InitializationCompleted

            Try
                WebView.EnsureCoreWebView2Async()
            Catch ex As Exception
                Logger.Error(ex, "[XFL] 小窗内核初始化失败")
                LabSmallHint.Text = "小窗初始化失败：" & ex.Message
            End Try
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 小窗初始化异常")
        End Try
    End Sub

    Private Sub WebView_CoreWebView2InitializationCompleted(sender As Object, e As CoreWebView2InitializationCompletedEventArgs)
        Try
            If Not e.IsSuccess Then
                LabSmallHint.Text = "小窗内核初始化失败：" & If(e.InitializationException IsNot Nothing, e.InitializationException.Message, "未知错误")
                Return
            End If
            _ready = True
            WebView.Visibility = Visibility.Visible
            LabSmallHint.Visibility = Visibility.Collapsed
            AddHandler WebView.CoreWebView2.SourceChanged,
                Sub(s2 As Object, e2 As Object)
                    RunInUi(Sub()
                                Try
                                    LabSmallUrl.Text = If(WebView.Source?.ToString(), "")
                                Catch
                                End Try
                            End Sub, True)
                End Sub

            AddHandler WebView.CoreWebView2.NewWindowRequested,
                Sub(s2 As Object, e2 As CoreWebView2NewWindowRequestedEventArgs)
                    Try
                        e2.Handled = True
                        WebView.CoreWebView2.Navigate(e2.Uri)
                    Catch
                    End Try
                End Sub
            Logger.Info("[XFL] 小窗内核就绪")
            If Not String.IsNullOrWhiteSpace(_pendingUrl) Then
                Dim u As String = _pendingUrl
                _pendingUrl = ""
                WebView.CoreWebView2.Navigate(u)
            End If
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 小窗就绪回调失败")
        End Try
    End Sub

    Private Sub BtnSmallBack_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            Dim u As String = CurrentUrl()
            Close()
            If FrmMain IsNot Nothing Then FrmMain.OpenUrlInBrowser(u)
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 小窗回到主界面失败")
        End Try
    End Sub

    Private Sub BtnSmallBrowser_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            Dim u As String = CurrentUrl()
            Close()
            If FrmMain IsNot Nothing Then FrmMain.OpenUrlInBrowser(u)
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 小窗转交浏览器失败")
        End Try
    End Sub

    Private Sub BtnSmallTop_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            Topmost = Not Topmost
            BtnSmallTop.Text = If(Topmost, "取消置顶", "保持置顶")
            Logger.Info($"[XFL] 小窗置顶已{(If(Topmost, "开启", "关闭"))}")
        Catch
        End Try
    End Sub

End Class