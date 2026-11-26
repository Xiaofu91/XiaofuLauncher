
Imports Microsoft.Web.WebView2.Core
Imports Microsoft.Web.WebView2.Wpf

Module ModDeepSeek

    Public Const HomeUrl As String = "https://chat.deepseek.com/"
    Public Const LoginUrl As String = "https://chat.deepseek.com/sign_in"

    Private _view As WebView2 = Nothing
    Private _keeper As XflWebViewKeeper = Nothing
    Private _host As Windows.Controls.Panel = Nothing
    Private _inited As Boolean = False
    Private _lastLogin As Boolean = False

    Private Const MaxInitRetry As Integer = 3
    Private _retryCount As Integer = 0
    Private _pendingReady As Action(Of Boolean) = Nothing

    Private Function DsDataDir() As String

        Try
            Dim d As String = IO.Path.Combine(Paths.Base, "XFL", "WebView2DeepSeek")
            IO.Directory.CreateDirectory(d)
            Return d
        Catch
        End Try
        Try
            Dim d2 As String = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                              "XiaofuLauncher", "WebView2DeepSeek")
            IO.Directory.CreateDirectory(d2)
            Return d2
        Catch
            Return ""
        End Try
    End Function

    Public ReadOnly Property IsReady As Boolean
        Get
            Try
                Return _view IsNot Nothing AndAlso _view.CoreWebView2 IsNot Nothing
            Catch
                Return False
            End Try
        End Get
    End Property

    Public ReadOnly Property LastLoginState As Boolean
        Get
            Return _lastLogin
        End Get
    End Property

    Public Sub EnsureView(host As Windows.Controls.Panel, OnReady As Action(Of Boolean))
        If host Is Nothing Then
            If OnReady IsNot Nothing Then OnReady(False)
            Return
        End If

        If _view IsNot Nothing AndAlso _inited Then
            Try
                If Not ReferenceEquals(_host, host) Then
                    If _view.Parent IsNot Nothing Then CType(_view.Parent, Windows.Controls.Panel).Children.Remove(_view)
                    host.Children.Clear()
                    host.Children.Add(_view)
                    _host = host

                    If _keeper IsNot Nothing Then
                        _keeper = New XflWebViewKeeper(host, _view, "DeepSeek页")
                        _keeper.HookTracking()
                    End If
                End If
                If _keeper IsNot Nothing Then
                    _keeper.SetExpected(True)
                Else
                    _view.Visibility = Windows.Visibility.Visible
                End If
                If OnReady IsNot Nothing Then OnReady(True)
                Return
            Catch ex As Exception
                Logger.Warn(ex, "[XFL][DeepSeek] 复用已有视图失败，将重建")
            End Try
        End If

        Dim ver As String = ModWebView.RuntimeVersion()
        If String.IsNullOrWhiteSpace(ver) Then
            Logger.Warn("[XFL][DeepSeek] 未检测到 WebView2 运行时，无法使用 DeepSeek 账号登录")
            If OnReady IsNot Nothing Then OnReady(False)
            Return
        End If
        Logger.Info($"[XFL][DeepSeek] WebView2 运行时版本 {ver}，开始创建 DeepSeek 视图")

        Try
            _host = host
            _pendingReady = OnReady
            host.Children.Clear()
            Dim wv As New WebView2()
            _view = wv

            Dim dir As String = DsDataDir()
            Try
                If dir.Length > 0 Then
                    wv.CreationProperties = New CoreWebView2CreationProperties() With {.UserDataFolder = dir}
                End If
            Catch ex As Exception
                Logger.Warn(ex, "[XFL][DeepSeek] 设置用户数据目录失败，将用默认目录")
            End Try
            host.Children.Add(wv)

            Try
                _keeper = New XflWebViewKeeper(host, wv, "DeepSeek页")
                _keeper.HookTracking()
            Catch exK As Exception
                Logger.Warn(exK, "[XFL][DeepSeek] 空域管理器建立失败")
            End Try

            AddHandler wv.CoreWebView2InitializationCompleted, AddressOf OnInitCompleted
            Logger.Info($"[XFL][DeepSeek] 用户数据目录：{(If(dir.Length > 0, dir, "（默认）"))}")
            Try

                wv.EnsureCoreWebView2Async()
            Catch ex As Exception
                Logger.Error(ex, "[XFL][DeepSeek] 内核初始化调用失败")
                If OnReady IsNot Nothing Then OnReady(False)
            End Try
        Catch ex As Exception
            Logger.Error(ex, "[XFL][DeepSeek] 创建视图失败")
            If OnReady IsNot Nothing Then OnReady(False)
        End Try
    End Sub

    Private Sub OnInitCompleted(sender As Object, e As CoreWebView2InitializationCompletedEventArgs)
        Try
            If Not e.IsSuccess Then
                Dim msg As String = If(e.InitializationException IsNot Nothing, e.InitializationException.Message, "未知错误")
                Logger.Error("[XFL][DeepSeek] 内核初始化未成功：" & msg)

                If _retryCount < MaxInitRetry Then
                    _retryCount += 1
                    Logger.Info($"[XFL][DeepSeek] 将在 1.2 秒后重试初始化（第 {_retryCount}/{MaxInitRetry} 次）")
                    RunInNewThread(
                        Sub()
                            Threading.Thread.Sleep(1200)
                            RunInUi(
                                Sub()
                                    Try
                                        ClearStaleLocks()
                                        ResetDataDir()
                                        DisposeView()
                                        If _host IsNot Nothing Then EnsureView(_host, _pendingReady)
                                    Catch ex As Exception
                                        Logger.Error(ex, "[XFL][DeepSeek] 重试初始化失败")
                                    End Try
                                End Sub)

                        End Sub)
                Else
                    Logger.Error("[XFL][DeepSeek] 已达最大重试次数，放弃初始化")
                    If _pendingReady IsNot Nothing Then
                        Dim cb As Action(Of Boolean) = _pendingReady
                        _pendingReady = Nothing
                        RunInUi(Sub() cb(False))
                    End If
                End If
                Return
            End If
            _inited = True
            _retryCount = 0

            If _keeper IsNot Nothing Then _keeper.SetExpected(True)
            Dim core As CoreWebView2 = _view.CoreWebView2
            AddHandler core.NavigationCompleted, AddressOf OnNavCompleted

            AddHandler core.NewWindowRequested,
                Sub(s2 As Object, e2 As CoreWebView2NewWindowRequestedEventArgs)
                    Try
                        e2.Handled = True
                        If e2.Uri IsNot Nothing Then core.Navigate(e2.Uri)
                    Catch
                    End Try
                End Sub

            Logger.Info("[XFL][DeepSeek] 内核就绪，开始导航到 " & HomeUrl)
            core.Navigate(HomeUrl)
        Catch ex As Exception
            Logger.Error(ex, "[XFL][DeepSeek] 初始化完成回调异常")
        End Try
    End Sub

    Private Sub OnNavCompleted(sender As Object, e As CoreWebView2NavigationCompletedEventArgs)
        Try
            Logger.Info($"[XFL][DeepSeek] 页面加载完成：Success={e.IsSuccess} Url={SafeUrl()}")
                        CheckLoginAsync(Nothing)
            If e.IsSuccess Then ApplyAdaptCss()
        Catch ex As Exception
            Logger.Warn(ex, "[XFL][DeepSeek] 导航完成回调异常")
        End Try
    End Sub

    Private Sub ApplyAdaptCss()
        Try
            If Not IsReady Then Return

            Dim css As String =
                "body, html { margin:0 !important; padding:0 !important; height:100% !important; }" & vbCrLf &
                "* { box-sizing:border-box !important; }" & vbCrLf &
                "header, .topbar, .nav-bar, .user-center { display:none !important; }" & vbCrLf &
                "main, .main-content, #main, .app-main, .chat-container, #chat-container, .conversation {" & vbCrLf &
                "    padding:14px !important; height:calc(100% - 14px) !important; max-height:100% !important; overflow:auto !important; }" & vbCrLf &
                "[class*='bubble'], [class*='message-item'], .ds-bubble, .markdown-body {" & vbCrLf &
                "    border-radius:14px !important; border:1px solid rgba(0,0,0,.08) !important; padding:12px 16px !important;" & vbCrLf &
                "    font-size:14px !important; line-height:1.7 !important; }" & vbCrLf &
                "textarea, input[type='text'], [contenteditable] {" & vbCrLf &
                "    border-radius:12px !important; border:1px solid rgba(0,0,0,.12) !important; padding:10px 14px !important; font-size:14px !important; }" & vbCrLf &
                "button { border-radius:10px !important; }"

            Logger.Info("[XFL][DeepSeek] 注入适配 CSS（页面脚本执行前）")
            Dim task As Threading.Tasks.Task = _view.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(css)
            task.ContinueWith(
                Sub(ta As Threading.Tasks.Task)
                    Try
                        Logger.Info("[XFL][DeepSeek] 适配 CSS 已注入")
                    Catch ex As Exception
                        Logger.Warn(ex, "[XFL][DeepSeek] 注入适配 CSS 失败")
                    End Try
                End Sub)
        Catch ex As Exception
            Logger.Warn(ex, "[XFL][DeepSeek] 适配 CSS 注入异常")
        End Try
    End Sub

    Private Function SafeUrl() As String
        Try
            If _view IsNot Nothing AndAlso _view.CoreWebView2 IsNot Nothing Then Return _view.CoreWebView2.Source
        Catch
        End Try
        Return ""
    End Function

    Public Sub CheckLoginAsync(OnResult As Action(Of Boolean))
        Try
            If Not IsReady Then
                _lastLogin = False
                If OnResult IsNot Nothing Then RunInUi(Sub() OnResult(False))
                Return
            End If

            Dim js As String =
                "(function(){try{" &
                "var h=(location.href||'');" &
                "if(/sign_in|signin|login/i.test(h))return 'NO';" &
                "var t=localStorage.getItem('userToken')||'';" &
                "if(t.length<40){t='';for(var i=0;i<localStorage.length;i++){var k=localStorage.key(i)||'';" &
                "if(/usertoken|user_token|auth_?token|accesstoken/i.test(k)){var v=localStorage.getItem(k)||'';" &
                "if(v.length>40){t=v;break;}}}}" &
                "if(t.length>40)return 'YES';" &
                "return 'NO';}catch(e){return 'ERR';}})()"
Dim task As Threading.Tasks.Task(Of String) = _view.CoreWebView2.ExecuteScriptAsync(js)
            task.ContinueWith(
                Sub(t As Threading.Tasks.Task(Of String))
                    Dim ok As Boolean = False
                    Try
                        Dim raw As String = If(t.IsCompleted AndAlso Not t.IsFaulted, t.Result, "")

                        ok = raw IsNot Nothing AndAlso raw.Contains("YES")
                    Catch
                    End Try
                    _lastLogin = ok
                    Logger.Info($"[XFL][DeepSeek] 登录态检测：{(If(ok, "已登录", "未登录"))}")
                    If OnResult IsNot Nothing Then RunInUi(Sub() OnResult(ok))
                End Sub)
        Catch ex As Exception
            Logger.Warn(ex, "[XFL][DeepSeek] 检测登录态失败")
            If OnResult IsNot Nothing Then RunInUi(Sub() OnResult(False))
        End Try
    End Sub

    Public Sub GoLogin()
        Try
            If IsReady Then
                Logger.Info("[XFL][DeepSeek] 导航到登录页")
                _view.CoreWebView2.Navigate(LoginUrl)
            End If
        Catch ex As Exception
            Logger.Warn(ex, "[XFL][DeepSeek] 打开登录页失败")
        End Try
    End Sub

    Public Sub Reload()
        Try
            If IsReady Then
                _view.CoreWebView2.Reload()
            End If
        Catch
        End Try
    End Sub

    Public Sub Logout()
        Try
            If Not IsReady Then Return
            Logger.Info("[XFL][DeepSeek] 用户请求退出登录，清理本地登录态")
            _view.CoreWebView2.ExecuteScriptAsync("(function(){try{localStorage.clear();sessionStorage.clear();}catch(e){}return 'ok';})()")
            _view.CoreWebView2.Navigate(LoginUrl)
            _lastLogin = False
        Catch ex As Exception
            Logger.Warn(ex, "[XFL][DeepSeek] 退出登录失败")
        End Try
    End Sub

    Private Sub DisposeView()
        Try
            If _view IsNot Nothing Then
                Try
                    If _host IsNot Nothing AndAlso _view.Parent IsNot Nothing Then
                        CType(_view.Parent, Windows.Controls.Panel).Children.Remove(_view)
                    End If
                Catch
                End Try
                Try
                    _view.Dispose()
                Catch
                End Try
            End If
        Catch
        Finally
            _view = Nothing
            _inited = False
            _keeper = Nothing
        End Try
    End Sub

    Public Sub SetVisible(show As Boolean)
        Try

            If _keeper IsNot Nothing Then
                _keeper.SetExpected(show)
            ElseIf _view IsNot Nothing Then
                _view.Visibility = If(show, Windows.Visibility.Visible, Windows.Visibility.Collapsed)
            End If
        Catch
        End Try
    End Sub

    Private Sub ClearStaleLocks()
        Try
            Dim dir As String = DsDataDir()
            If String.IsNullOrWhiteSpace(dir) OrElse Not IO.Directory.Exists(dir) Then Return
            Dim removed As New List(Of String)
            For Each f As String In IO.Directory.EnumerateFiles(dir, "Singleton*")
                Try
                    IO.File.Delete(f)
                    removed.Add(IO.Path.GetFileName(f))
                Catch
                End Try
            Next
            If removed.Count > 0 Then
                Logger.Warn($"[XFL][DeepSeek] 已清理残留锁文件：{String.Join(", ", removed)}")
            End If
        Catch ex As Exception
            Logger.Warn(ex, "[XFL][DeepSeek] 清理残留锁文件失败")
        End Try
    End Sub

    Private Sub ResetDataDir()
        Try
            Dim dir As String = DsDataDir()
            If String.IsNullOrWhiteSpace(dir) Then Return
            Logger.Warn($"[XFL][DeepSeek] 准备重建用户数据目录：{dir}")
            Try
                If IO.Directory.Exists(dir) Then IO.Directory.Delete(dir, True)
            Catch ex As Exception

                Logger.Warn("[XFL][DeepSeek] 目录被占用，尝试结束占用的 WebView2 进程后重试…")
                Try
                    KillWebviewOwningDir(dir)
                    Threading.Thread.Sleep(800)
                    If IO.Directory.Exists(dir) Then IO.Directory.Delete(dir, True)
                Catch ex2 As Exception
                    Logger.Warn(ex2, "[XFL][DeepSeek] 重试删除仍失败，将尝试就地重建")
                End Try
            End Try
            Try
                IO.Directory.CreateDirectory(dir)
                Logger.Warn("[XFL][DeepSeek] 用户数据目录已重建")
            Catch ex3 As Exception
                Logger.Warn(ex3, "[XFL][DeepSeek] 重建目录失败（下次启动会再试）")
            End Try
        Catch ex As Exception
            Logger.Warn(ex, "[XFL][DeepSeek] 重建数据目录失败")
        End Try
    End Sub

    Private Sub KillWebviewOwningDir(dir As String)
        Try
            Dim found As Integer = 0
            For Each proc As Process In Process.GetProcessesByName("msedgewebview2")
                Try
                    Dim cl As String = ""
                    Try
                        cl = proc.MainModule.FileName & " " & String.Join(" ", proc.StartInfo.Arguments)
                    Catch
                    End Try

                    If String.IsNullOrWhiteSpace(cl) OrElse Not cl.Contains(dir) Then
                        Try
                            Using searcher As New System.Management.ManagementObjectSearcher(
                                    "SELECT CommandLine FROM Win32_Process WHERE ProcessId = " & proc.Id)
                                For Each o As System.Management.ManagementObject In searcher.Get()
                                    cl = If(o("CommandLine"), "").ToString()
                                    Exit For
                                Next
                            End Using
                        Catch
                        End Try
                    End If
                    If cl IsNot Nothing AndAlso cl.Contains(dir) Then
                        proc.Kill()
                        found += 1
                    End If
                Catch
                End Try
            Next
            If found > 0 Then
                Logger.Warn($"[XFL][DeepSeek] 已结束 {found} 个占用数据目录的 WebView2 进程")
            End If
        Catch ex As Exception
            Logger.Warn(ex, "[XFL][DeepSeek] 清理占用进程失败")
        End Try
    End Sub

End Module