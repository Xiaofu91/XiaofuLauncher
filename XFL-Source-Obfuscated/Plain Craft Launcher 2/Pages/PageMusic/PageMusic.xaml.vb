
Imports Microsoft.Web.WebView2.Core

Partial Public Class PageMusic

    Private _initStarted As Boolean = False
    Private _ready As Boolean = False

    Private _keeper As XflWebViewKeeper = Nothing
    Private _searching As Boolean = False
    Private _searchRetry As Integer = 0
    Private _currentSongId As Long = 0

    Private ReadOnly Property HomeUrl As String
        Get
            Return OutchainUrl(0)
        End Get
    End Property

    Private Const DefaultSongId As Long = 186016

    Private Function OutchainUrl(songId As Long, Optional autoPlay As Boolean = True) As String
        If songId <= 0 Then Return ""
        Dim a As Integer = If(autoPlay, 1, 0)
        Return $"https://music.163.com/outchain/player?type=2&id={songId}&auto={a}&height=66"
    End Function

    Public Sub New()
        InitializeComponent()

        AddHandler Me.Loaded, AddressOf MusicPage_Loaded
        AddHandler BtnMusicSearch.Click, AddressOf BtnMusicSearch_Click
        AddHandler TxtMusicSearch.KeyDown, AddressOf TxtMusicSearch_KeyDown
        AddHandler BtnMusicHome.Click, AddressOf BtnMusicHome_Click
        AddHandler BtnMusicMine.Click, AddressOf BtnMusicMine_Click
        AddHandler BtnMusicRefresh.Click, AddressOf BtnMusicRefresh_Click
        AddHandler BtnMusicBrowser.Click, AddressOf BtnMusicBrowser_Click
        AddHandler BtnMusicSmall.Click, AddressOf BtnMusicSmall_Click

        Try
            _keeper = New XflWebViewKeeper(Me, WebView, "音乐页")
            _keeper.HookTracking()
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 音乐页空域管理器建立失败")
        End Try

        Dispatcher.BeginInvoke(New Action(
            Sub()
                Try
                    InitCore()

                    Try
                        Dim kw As String = Environment.GetEnvironmentVariable("XFL_TEST_MUSIC")
                        If Not String.IsNullOrWhiteSpace(kw) Then
                            RunInNewThread(
                                Sub()
                                    Threading.Thread.Sleep(6000)
                                    RunInUi(Sub()
                                                Try
                                                    TxtMusicSearch.Text = kw
                                                    DoSearch()
                                                Catch ex2 As Exception
                                                    Logger.Error(ex2, "[XFL] 音乐自检失败")
                                                End Try
                                            End Sub)
                                End Sub)
                        End If
                    Catch
                    End Try
                Catch ex As Exception
                    Logger.Error(ex, "[XFL] 音乐页延迟初始化失败")
                End Try
            End Sub), Windows.Threading.DispatcherPriority.ApplicationIdle)
    End Sub

    Private Sub MusicPage_Loaded(sender As Object, e As RoutedEventArgs)
        Try
            InitCore()

            Try
                Dim kw As String = Environment.GetEnvironmentVariable("XFL_TEST_MUSIC")
                If Not String.IsNullOrWhiteSpace(kw) Then
                    RunInNewThread(
                        Sub()
                            Threading.Thread.Sleep(3000)
                            RunInUi(Sub()
                                        Try
                                            TxtMusicSearch.Text = kw
                                            DoSearch()
                                        Catch ex2 As Exception
                                            Logger.Error(ex2, "[XFL] 音乐自检失败")
                                        End Try
                                    End Sub)
                        End Sub)
                End If
            Catch
            End Try
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 网易云音乐页加载失败")
        End Try
    End Sub

    Private Sub InitCore()
        Logger.Info("[XFL] 音乐页 InitCore 进入")
        If _initStarted Then Return
        _initStarted = True
        Try
            Dim ver As String = ""
            Try
                ver = CoreWebView2Environment.GetAvailableBrowserVersionString()
            Catch
            End Try
            Logger.Info($"[XFL] 音乐页 InitCore：运行时 ver={If(ver, "（空）")}")
            If String.IsNullOrWhiteSpace(ver) Then
                LabMusicHint.Text = "未检测到「Microsoft Edge WebView2 运行时」，无法显示网易云网页播放器。" & vbCrLf & vbCrLf &
                                    "解决办法：安装 Microsoft Edge 浏览器，或安装微软官方免费的 WebView2 运行时：" & vbCrLf &
                                    "https://developer.microsoft.com/microsoft-edge/webview2/"
                Return
            End If
            LabMusicHint.Text = "正在加载网易云音乐…"
            AddHandler WebView.CoreWebView2InitializationCompleted, AddressOf WebView_CoreWebView2InitializationCompleted

            Try
                Dim dir As String = IO.Path.Combine(Paths.Base, "XFL", "WebView2Music")
                IO.Directory.CreateDirectory(dir)
                WebView.CreationProperties = New Microsoft.Web.WebView2.Wpf.CoreWebView2CreationProperties With {.UserDataFolder = dir}
                Logger.Info($"[XFL] 音乐页使用独立数据目录：{dir}")
            Catch ex As Exception
                Logger.Warn(ex, "[XFL] 设置音乐数据目录失败，将回退默认")
            End Try
            Try
                WebView.EnsureCoreWebView2Async()
            Catch ex As Exception
                Logger.Error(ex, "[XFL] 网易云音乐内核初始化失败")
            End Try
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 网易云音乐内核初始化失败")
            LabMusicHint.Text = "初始化失败：" & ex.Message
        End Try
    End Sub

    Private Sub WebView_CoreWebView2InitializationCompleted(sender As Object, e As CoreWebView2InitializationCompletedEventArgs)
        Try
            If Not e.IsSuccess Then
                LabMusicHint.Text = "网易云音乐初始化失败：" & If(e.InitializationException IsNot Nothing, e.InitializationException.Message, "未知错误")
                Return
            End If
            _ready = True

            If _keeper IsNot Nothing Then
                _keeper.SetExpected(True)
            Else
                WebView.Visibility = Visibility.Visible
            End If
            AddHandler WebView.CoreWebView2.NewWindowRequested,
                Sub(s2 As Object, e2 As CoreWebView2NewWindowRequestedEventArgs)
                    Try
                        e2.Handled = True
                        Core_NavigateTo(e2.Uri)
                    Catch
                    End Try
                End Sub
            Logger.Info("[XFL] 网易云音乐内核就绪（outchain 播放条模式），等待点歌")

            Try
                Core_NavigateTo(OutchainUrl(DefaultSongId, False))
                Logger.Info("[XFL] 已加载同源占位播放条（提供 fetch 所需的 origin）")
            Catch ex As Exception
                Logger.Warn(ex, "[XFL] 加载占位播放条失败")
            End Try

            If _currentSongId <= 0 Then
                LabMusicHint.Visibility = Visibility.Visible
            End If
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 网易云音乐就绪回调失败")
        End Try
    End Sub

    Private Sub Core_NavigateTo(url As String)
        Try
            If String.IsNullOrWhiteSpace(url) Then Return
            If Not _ready OrElse WebView.CoreWebView2 Is Nothing Then Return
            WebView.CoreWebView2.Navigate(url)
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 音乐页导航失败：" & url)
        End Try
    End Sub

    Public Function CurrentUrl() As String
        Try
            If _ready AndAlso WebView.Source IsNot Nothing Then Return WebView.Source.ToString()
        Catch
        End Try
        Return HomeUrl
    End Function

    Private Sub TxtMusicSearch_KeyDown(sender As Object, e As KeyEventArgs)
        If e.Key = Key.Enter Then
            e.Handled = True
            DoSearch()
        End If
    End Sub

    Private Sub BtnMusicSearch_Click(sender As Object, e As MouseButtonEventArgs)
        DoSearch()
    End Sub

    Private Sub DoSearch()
        If _searching Then Return
        Dim kw As String = If(TxtMusicSearch.Text, "").Trim()
        If kw.Length = 0 Then
            ShowResults("请先输入歌名 / 歌手 / 专辑关键词", Nothing)
            Return
        End If
        _searching = True
        ShowResults($"正在搜索「{kw}」…", Nothing)

        If _ready AndAlso WebView.CoreWebView2 IsNot Nothing Then
            Try
                Dim q As String = JsQuote(kw)
                Dim kick As String =
                    "window.__xflSearchResult=null;" &
                    "(function(){var k=" & q & ";" &
                    "var u='https://music.163.com/api/search/get/web?s='+encodeURIComponent(k)+'&type=1&offset=0&limit=20';" &
                    "fetch(u,{method:'GET',credentials:'include'})" &
                    " .then(function(r){return r.text();})" &
                    " .then(function(t){window.__xflSearchResult=t;})" &
                    " .catch(function(e){window.__xflSearchResult='FETCH_ERR:'+e.message;});" &
                    "})();"
                WebView.CoreWebView2.ExecuteScriptAsync(kick)
                Logger.Info("[XFL] 已在内核中发起搜索请求（fetch），开始轮询结果")
                PollWebViewSearch(kw, 0)
            Catch ex As Exception
                Logger.Warn(ex, "[XFL] WebView fetch 调用失败，回退 .NET 直连")
                FallbackDotNetSearch(kw)
            End Try
        Else
            Logger.Info("[XFL] 内核未就绪，改用 .NET 直连搜索")
            FallbackDotNetSearch(kw)
        End If
    End Sub

    Private Sub ShowResults(tip As String, hits As List(Of ModNetEase.MusicHit))
        Try
            If hits IsNot Nothing AndAlso hits.Count > 0 Then _searchRetry = 0
            If LabMusicEmpty IsNot Nothing Then LabMusicEmpty.Visibility = Visibility.Collapsed
            PanMusicResults.Children.Clear()
            LabMusicResultTip.Text = tip
            If hits IsNot Nothing Then
                Dim accent As Brush = TryCast(Application.Current.Resources("ColorBrush3"), Brush)
                For Each h As ModNetEase.MusicHit In hits
                    Dim row As New Border With {
                        .CornerRadius = New CornerRadius(8),
                        .Padding = New Thickness(10, 6, 10, 6),
                        .Margin = New Thickness(0, 0, 0, 4),
                        .Cursor = Cursors.Hand,
                        .Tag = h.Id}
                    row.SetResourceReference(Border.BackgroundProperty, "ColorBrushSurfaceSunken")
                    Dim sp As New StackPanel With {.Orientation = Orientation.Horizontal}
                    Dim lab As New TextBlock With {
                        .Text = h.Display, .FontSize = 13.5, .TextTrimming = TextTrimming.CharacterEllipsis,
                        .VerticalAlignment = VerticalAlignment.Center}
                    lab.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushTextPrimary")
                    Dim labAlbum As New TextBlock With {
                        .Text = If(String.IsNullOrWhiteSpace(h.Album), "", "   · " & h.Album),
                        .FontSize = 12, .VerticalAlignment = VerticalAlignment.Center}
                    labAlbum.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushTextTertiary")
                    sp.Children.Add(lab)
                    sp.Children.Add(labAlbum)
                    row.Child = sp
                    AddHandler row.MouseLeftButtonUp,
                        Sub(s2 As Object, e2 As MouseButtonEventArgs)
                            Try
                                Dim sid As Long = CLng(CType(s2, Border).Tag)
                                PlaySong(sid)
                            Catch
                            End Try
                        End Sub
                    PanMusicResults.Children.Add(row)
                Next
            End If
            PanMusicResultBox.Visibility = Visibility.Visible
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 音乐结果渲染失败")
        End Try
    End Sub

    Private Sub BtnMusicHome_Click(sender As Object, e As MouseButtonEventArgs)
        Try

            Logger.Info("[XFL] 发现音乐：转内置浏览器打开")
            FrmMain.OpenUrlInBrowser("https://music.163.com/")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 打开发现音乐失败")
        End Try
    End Sub

    Private Sub BtnMusicMine_Click(sender As Object, e As MouseButtonEventArgs)
        Try

            Logger.Info("[XFL] 我的音乐：转内置浏览器打开")
            FrmMain.OpenUrlInBrowser("https://music.163.com/#/my/")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 打开我的音乐失败")
        End Try
    End Sub

    Private Sub BtnMusicRefresh_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            If _ready AndAlso WebView.CoreWebView2 IsNot Nothing Then WebView.CoreWebView2.Reload()
        Catch
        End Try
    End Sub

    Private Sub BtnMusicBrowser_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            If FrmMain IsNot Nothing Then FrmMain.OpenUrlInBrowser(CurrentUrl())
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 转交浏览器页失败")
        End Try
    End Sub

    Private Sub BtnMusicSmall_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            If Not _ready Then
                MsgBox("小窗需要浏览器内核就绪后再用。", MsgBoxStyle.Information, "小窗模式")
                Return
            End If
            FormWebSmall.ShowSmall(CurrentUrl())
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 打开音乐小窗失败")
        End Try
    End Sub

    Public Sub OnPageHide()
        Try
            PanMusicResultBox.Visibility = Visibility.Collapsed
        Catch
        End Try
    End Sub

    Private Sub PlaySong(songId As Long)
        Try
            If songId <= 0 Then Return
            _currentSongId = songId
            If Not _ready Then
                InitCore()
                Logger.Info($"[XFL] 内核尚未就绪，已记住待播曲目 id={songId}")
                Return
            End If
            Dim url As String = OutchainUrl(songId)
            If url.Length = 0 Then Return
            LabMusicHint.Visibility = Visibility.Collapsed
            Core_NavigateTo(url)
            Logger.Info($"[XFL] 播放条切歌 id={songId}")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 播放歌曲失败")
        End Try
    End Sub

    Private Function JsQuote(s As String) As String

        Dim t As String = If(s, "")
        t = t.Replace("\", "\\")
        t = t.Replace("'", "\'")
        t = t.Replace(Chr(34), "\""")
        t = t.Replace(vbCr, "").Replace(vbLf, "\n")
        Return "'" & t & "'"
    End Function

    Private Sub FallbackDotNetSearch(kw As String)
        RunInNewThread(
            Sub()
                Try
                    Dim hits As List(Of ModNetEase.MusicHit) = ModNetEase.MusicSearch(kw, 20)
                    RunInUi(Sub()
                                _searching = False
                                If hits.Count = 0 Then
                                    ShowResults($"没有搜到「{kw}」（可点「发现音乐」在网页版里搜索）", Nothing)
                                Else
                                    ShowResults($"「{kw}」找到 {hits.Count} 条，点一下即刻播放：", hits)
                                End If
                            End Sub, True)
                Catch ex As Exception
                    Logger.Error(ex, "[XFL] 网易云搜索出错")

                    If _searchRetry < 1 Then
                        _searchRetry += 1
                        RunInUi(Sub() ShowResults($"网络似乎被拦截，3 秒后自动重试…（{_searchRetry}/1）", Nothing))
                        RunInNewThread(
                            Sub()
                                Threading.Thread.Sleep(3000)
                                RunInUi(Sub()
                                            _searching = False
                                            DoSearch()
                                        End Sub)
                            End Sub)
                        Return
                    End If
                    _searchRetry = 0
                    RunInUi(Sub()
                                _searching = False
                                ShowResults("搜索失败：网易云接口暂时不可达。" & vbCrLf &
                                            "可能原因：本机网络被网易云风控临时限制（连接能建立但 TLS 握手被重置）。" & vbCrLf &
                                            "建议：① 稍等几分钟再搜；② 点「发现音乐」用内置浏览器打开网页版搜索；③ 换网络（如手机热点）重试。",
                                            Nothing)
                            End Sub, True)
                End Try
            End Sub)
    End Sub

    Private Sub PollWebViewSearch(kw As String, attempt As Integer)
        Try
            If Not _ready OrElse WebView.CoreWebView2 Is Nothing Then
                FallbackDotNetSearch(kw)
                Return
            End If
            If attempt > 48 Then
                Logger.Warn("[XFL] 内核搜索超时（12s），回退 .NET 直连")
                FallbackDotNetSearch(kw)
                Return
            End If
            Dim task As Threading.Tasks.Task(Of String) = WebView.CoreWebView2.ExecuteScriptAsync("window.__xflSearchResult")
            task.ContinueWith(
                Sub(ta As Threading.Tasks.Task(Of String))
                    Try
                        Dim raw As String = If(ta.IsCompleted AndAlso Not ta.IsFaulted, ta.Result, "null")
                        If String.IsNullOrWhiteSpace(raw) OrElse raw.Trim() = "null" Then
                            RunInNewThread(
                                Sub()
                                    Threading.Thread.Sleep(250)
                                    RunInUi(Sub() PollWebViewSearch(kw, attempt + 1))
                                End Sub)
                            Return
                        End If

                        Dim body As String = raw
                        Try
                            body = Newtonsoft.Json.Linq.JToken.Parse(raw).ToString()
                        Catch
                        End Try
                        If body.StartsWith("FETCH_ERR:") Then
                            Logger.Warn("[XFL] 内核 fetch 失败：" & body)
                            RunInUi(Sub() FallbackDotNetSearch(kw))
                            Return
                        End If
                        Dim hits As List(Of ModNetEase.MusicHit) = ModNetEase.ParseSearch(body, 20)
                        Logger.Info($"[XFL] 内核 fetch 搜索完成，解析到 {hits.Count} 条")
                        RunInUi(Sub()
                                    _searching = False
                                    If hits.Count = 0 Then
                                        ShowResults($"没有搜到「{kw}」", Nothing)
                                    Else
                                        ShowResults($"「{kw}」找到 {hits.Count} 条，点一下即刻播放：", hits)
                                    End If
                                End Sub, True)
                    Catch ex As Exception
                        Logger.Warn(ex, "[XFL] 轮询搜索结果异常")
                    End Try
                End Sub)
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 轮询启动失败")
            FallbackDotNetSearch(kw)
        End Try
    End Sub

End Class