'XFL 批75: 启动器热更新（GitHub Release + 镜像站自动测速选源）
'
'设计（已与用户确认）：
'  · 只更新 exe —— 下载 Release 里的 XiaofuLauncher.exe，替换自身后重启；runtimes 等依赖不动。
'  · 启动自动检查 + 设置页手动「检查更新」。
'  · 多个源（GitHub 直连 + 镜像）并行测速，挑最快的下载，失败自动换下一个。
'  · 下载完成弹窗，点「立即更新」后：写一个临时 .bat 等主进程退出 → 覆盖 exe → 重新启动 → 删自己。
'
'版本来源：GitHub /releases/latest 的 tag（如 v0.75）；本机版本取程序集版本（2.13.0.1）。
'比较方式：tag 里的数字按段比较，比本机 AssemblyVersion 大才算「有新版」。
'
'镜像测速：对每个源的 exe 地址发 HEAD/Range 请求，记录耗时，取最快的一个；
'全失败则退回 GitHub 直连，直连也失败就提示用户检查网络/代理。

Imports System.Diagnostics
Imports System.IO
Imports System.Net
Imports System.Net.Http
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks

Module ModXflUpdate

    '---------- 常量 ----------

    Private Const RepoOwner As String = "Xiaofu91"
    Private Const RepoName As String = "XiaofuLauncher"
    Private Const AssetName As String = "XiaofuLauncher.exe"

    ''' <summary>GitHub Release 的 API（查最新版本号）。</summary>
    Private ReadOnly ApiLatest As String =
        $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest"

    ''' <summary>直接下载最新 release 的 exe（GitHub 官方地址，latest 会重定向）。</summary>
    Private ReadOnly DirectExe As String =
        $"https://github.com/{RepoOwner}/{RepoName}/releases/latest/download/{AssetName}"

    ''' <summary>
    ''' 镜像前缀（拼在 GitHub 原地址前）。gh-proxy.com 实测最快，其余兜底。
    ''' </summary>
    Private ReadOnly Mirrors As String() = {
        "https://gh-proxy.com/",
        "https://ghproxy.net/",
        "https://ghfast.top/",
        "https://github.moeyy.xyz/"
    }

    '---------- 运行状态 ----------

    ''' <summary>正在检查/下载，避免重复触发。</summary>
    Friend _updating As Boolean = False
    ''' <summary>已检查过（启动自动检查只跑一次）。</summary>
    Private _checkedOnce As Boolean = False
    ''' <summary>远端最新版本（如 0.75），未知为 Nothing。</summary>
    Private _latestVer As Version = Nothing
    ''' <summary>远端 release tag（如 v0.75）。</summary>
    Private _latestTag As String = ""
    ''' <summary>下载到的临时文件。</summary>
    Private _downloadedPath As String = ""

    Private ReadOnly Tag As String = "[XFL][更新]"
    Private _netReady As Boolean = False

    ''' <summary>初始化网络：强制 TLS 1.2+，并使用系统代理（本机 GitHub 直连常失败，靠镜像与代理）。</summary>
    Private Sub EnsureNet()
        If _netReady Then Return
        _netReady = True
        Try
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 Or CType(12288, SecurityProtocolType) '12288 = Tls13，该框架枚举里可能没有
        Catch
        End Try
        Try
            ServicePointManager.DefaultConnectionLimit = 16
            Dim wp As IWebProxy = WebRequest.GetSystemWebProxy()
            If wp IsNot Nothing Then
                Dim probe As Uri = wp.GetProxy(New Uri("https://github.com"))
                If probe IsNot Nothing AndAlso probe.Host <> "github.com" Then
                    WebRequest.DefaultWebProxy = wp
                    Logger.Info($"{Tag} 使用系统代理 {probe.Host}:{probe.Port}")
                End If
            End If
        Catch
        End Try
    End Sub

    '---------- 对外入口 ----------

    ''' <summary>XFL: 启动后静默检查（后台线程，不阻塞界面）。</summary>
    Friend Sub CheckOnStartup()
        EnsureNet()
        Try
            If _checkedOnce Then Return
            _checkedOnce = True
            If Not Settings.Get(Of Boolean)("XflUpdateAutoCheck") Then
                Logger.Info($"{Tag} 自动检查已关闭，跳过")
                Return
            End If
            Task.Run(Sub()
                         Try
                             Threading.Thread.Sleep(2500) '等主界面画完再联网
                             CheckInternal(silent:=True)
                         Catch ex As Exception
                             Logger.Warn(ex, $"{Tag} 启动检查异常")
                         End Try
                     End Sub)
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} CheckOnStartup 异常")
        End Try
    End Sub

    ''' <summary>XFL: 设置页「检查更新」按钮 —— 用户主动触发，结果一定要有反馈。</summary>
    Friend Sub CheckByUser()
        EnsureNet()
        If _updating Then
            MyMsgBox("正在检查或下载更新，请稍候。", "检查更新")
            Return
        End If
        _updating = True
        RunInUi(Sub() Task.Run(Sub()
                                   Try
                                       CheckInternal(silent:=False)
                                   Catch ex As Exception
                                       Logger.Warn(ex, $"{Tag} 检查异常")
                                       RunInUi(Sub() MyMsgBox("检查更新失败：" & ex.Message, "检查更新", IsWarn:=True))
                                   Finally
                                       _updating = False
                                   End Try
                               End Sub))
    End Sub

    '---------- 核心流程 ----------

    ''' <summary>查版本 → 比较 → 提示 → 下载。silent=True 表示启动自动检查（无新版不打扰）。</summary>
    Private Sub CheckInternal(silent As Boolean)
        Logger.Info($"{Tag} 开始检查更新（silent={silent}）")
        Dim cur As Version = CurrentVersion()
        Logger.Info($"{Tag} 本机版本 {cur}")

        '① 取远端 tag（API 走镜像也行，但 API 一般直连就通；失败时按「未知」处理，仍可下载最新 exe）
        Dim tagName As String = ""
        Dim remote As Version = Nothing
        Try
            tagName = FetchLatestTag()
            remote = ParseVersion(tagName)
            Logger.Info($"{Tag} 远端 {tagName} → {If(remote Is Nothing, "无法解析", remote.ToString())}")
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 获取远端版本失败")
        End Try

        '② 有明确版本且不比本机新 → 结束
        If remote IsNot Nothing AndAlso remote <= cur Then
            Logger.Info($"{Tag} 已是最新（{cur} >= {remote}）")
            If Not silent Then
                RunInUi(Sub() MyMsgBox($"当前已是最新版本（{cur}）。", "检查更新"))
            End If
            Return
        End If

        _latestTag = tagName
        _latestVer = remote
        Dim desc As String = If(remote Is Nothing, "发现新版本（版本号未知）", $"发现新版本 {remote}")

        '③ 问用户（启动自动检查时也问，但只在确实有新版本时）
        Dim ans As Integer = -1
        RunInUiWait(Sub()
                        ans = MyMsgBox($"{desc}，当前版本 {cur}。" & vbCrLf & vbCrLf &
                                       "是否现在下载并更新？更新会重启启动器。" & vbCrLf &
                                       "（只替换主程序，不影响存档与设置）",
                                       "发现更新", "立即更新", "稍后", "", False, True)
                    End Sub)
        If ans <> 1 Then
            Logger.Info($"{Tag} 用户选择稍后")
            Return
        End If

        '④ 测速选源 + 下载
        Dim url As String = PickFastestSource()
        If String.IsNullOrEmpty(url) Then
            RunInUi(Sub() MyMsgBox("所有下载源都连接失败。" & vbCrLf &
                                   "请检查网络，或开启系统代理后重试。", "下载失败", IsWarn:=True))
            Return
        End If
        DownloadAndApply(url)
    End Sub

    ''' <summary>取 releases/latest 的 tag_name。走直连，失败再用镜像。</summary>
    Private Function FetchLatestTag() As String
        Dim urls As New List(Of String) From {ApiLatest}
        For Each m In Mirrors
            urls.Add(m & ApiLatest)
        Next
        For Each u In urls
            Try
                Dim json As String = HttpGetString(u, 8000)
                If String.IsNullOrWhiteSpace(json) Then Continue For
                Dim mc As Match = Regex.Match(json, """tag_name""\s*:\s*""([^""]+)""")
                If mc.Success Then Return mc.Groups(1).Value
            Catch ex As Exception
                Logger.Warn($"{Tag} 版本接口失败 {u} —— {ex.Message}")
            End Try
        Next
        Return ""
    End Function

    ''' <summary>把 tag（v0.75 / 0.75 / v0.75.1）解析成 Version。</summary>
    Private Function ParseVersion(tag As String) As Version
        If String.IsNullOrWhiteSpace(tag) Then Return Nothing
        Dim mc As Match = Regex.Match(tag, "(\d+(?:\.\d+){0,3})")
        If Not mc.Success Then Return Nothing
        Dim v As Version = Nothing
        If Version.TryParse(mc.Groups(1).Value, v) Then Return v
        Return Nothing
    End Function

    ''' <summary>
    ''' XFL 自己的版本号 —— **不**用程序集版本（那是 PCL 的 2.13.0.1，永远大于 Release 的 0.74）。
    ''' 每次发版改这里：与 GitHub Release 的 tag（v0.75）对应。
    ''' ⚠ 发版流程里必须同步改这个常量，否则客户端认为已是最新。
    ''' </summary>
    Private Const XflVersion As String = "0.76"

    ''' <summary>本机 XFL 版本（用于与 Release tag 比较）。</summary>
    Private Function CurrentVersion() As Version
        Try
            Dim v As Version = Nothing
            If Version.TryParse(XflVersion, v) Then Return v
            Return New Version(0, 0, 0, 0)
        Catch
            Return New Version(0, 0, 0, 0)
        End Try
    End Function

    ''' <summary>XFL: 当前版本号（显示用，如 "0.75"）。</summary>
    Friend Function VersionText() As String
        Return XflVersion
    End Function

    ''' <summary>并行测速，返回最快的可用下载地址；全失败返回空。</summary>
    Private Function PickFastestSource() As String
        Dim candidates As New List(Of String) From {DirectExe}
        For Each m In Mirrors
            candidates.Add(m & DirectExe)
        Next
        Logger.Info($"{Tag} 开始测速，共 {candidates.Count} 个源")

        Dim best As String = ""
        Dim bestMs As Long = Long.MaxValue
        Dim results As New List(Of String)

        For Each u In candidates
            Try
                Dim sw As New Diagnostics.Stopwatch()
                sw.Start()
                Dim ok As Boolean = ProbeUrl(u, 6000)
                sw.Stop()
                If ok Then
                    results.Add($"{sw.ElapsedMilliseconds}ms {ShortHost(u)}")
                    If sw.ElapsedMilliseconds < bestMs Then
                        bestMs = sw.ElapsedMilliseconds
                        best = u
                    End If
                Else
                    results.Add($"失败 {ShortHost(u)}")
                End If
            Catch ex As Exception
                results.Add($"异常 {ShortHost(u)}")
                Logger.Warn($"{Tag} 测速异常 {ShortHost(u)} —— {ex.Message}")
            End Try
        Next

        Logger.Info($"{Tag} 测速结果：{String.Join(" / ", results)}")
        If String.IsNullOrEmpty(best) Then Return ""
        Logger.Info($"{Tag} 选用 {ShortHost(best)}（{bestMs}ms）")
        Return best
    End Function

    Private Function ShortHost(u As String) As String
        Try
            Return New Uri(u).Host
        Catch
            Return u
        End Try
    End Function

    ''' <summary>探测地址可用性（Range 只取 1 字节，快）。</summary>
    Private Function ProbeUrl(url As String, timeoutMs As Integer) As Boolean
        Try
            Dim req As HttpWebRequest = DirectCast(WebRequest.Create(url), HttpWebRequest)
            req.Method = "GET"
            req.Timeout = timeoutMs
            req.ReadWriteTimeout = timeoutMs
            req.AllowAutoRedirect = True
            req.UserAgent = "XFL-Updater"
            req.AddRange(0, 0) '只取第 1 个字节
            Using resp As HttpWebResponse = DirectCast(req.GetResponse(), HttpWebResponse)
                Return resp.StatusCode = HttpStatusCode.OK OrElse
                       resp.StatusCode = HttpStatusCode.PartialContent
            End Using
        Catch
            Return False
        End Try
    End Function

    Private Function HttpGetString(url As String, timeoutMs As Integer) As String
        Dim req As HttpWebRequest = DirectCast(WebRequest.Create(url), HttpWebRequest)
        req.Method = "GET"
        req.Timeout = timeoutMs
        req.ReadWriteTimeout = timeoutMs
        req.AllowAutoRedirect = True
        req.UserAgent = "XFL-Updater"
        req.Accept = "application/vnd.github+json"
        Using resp As HttpWebResponse = DirectCast(req.GetResponse(), HttpWebResponse)
            Using sr As New StreamReader(resp.GetResponseStream())
                Return sr.ReadToEnd()
            End Using
        End Using
    End Function

    '---------- 下载与替换 ----------

    Private Sub DownloadAndApply(url As String)
        Try
            'XFL 批75：下载目录放在程序目录（%TEMP% 常被安全软件"受控文件夹访问"拦截）
            Dim dlDir As String = IO.Path.Combine(IO.Path.GetDirectoryName(
                Reflection.Assembly.GetExecutingAssembly().Location), "XFL", "Update")
            Try
                IO.Directory.CreateDirectory(dlDir)
            Catch
                dlDir = IO.Path.GetTempPath()
            End Try
            Dim tmp As String = IO.Path.Combine(dlDir, "XFL_update_" & Guid.NewGuid().ToString("N") & ".exe")
            Logger.Info($"{Tag} 开始下载 {ShortHost(url)} → {tmp}")

            Using wc As New WebClient()
                wc.Headers.Add("User-Agent", "XFL-Updater")
                AddHandler wc.DownloadProgressChanged,
                    Sub(s, e)
                        Try
                            RunInUi(Sub() UpdateProgress(e.ProgressPercentage))
                        Catch
                        End Try
                    End Sub
                wc.DownloadFile(url, tmp)
            End Using

            Dim fi As New FileInfo(tmp)
            If Not fi.Exists OrElse fi.Length < 1024 * 1024 Then
                Throw New Exception($"下载的文件异常（{If(fi.Exists, fi.Length.ToString(), "0")} 字节）")
            End If
            Logger.Info($"{Tag} 下载完成 {Math.Round(fi.Length / 1024 / 1024, 2)} MB")
            _downloadedPath = tmp

            RunInUi(Sub()
                        Dim a As Integer = MyMsgBox(
                            $"新版本已下载完成（{Math.Round(fi.Length / 1024 / 1024, 2)} MB）。" & vbCrLf & vbCrLf &
                            "点击「立即更新」将关闭启动器、替换主程序并自动重新打开。",
                            "更新已就绪", "立即更新", "稍后更新", "", False, True)
                        If a = 1 Then ApplyUpdate()
                    End Sub)
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 下载失败")
            RunInUi(Sub() MyMsgBox("下载更新失败：" & ex.Message, "下载失败", IsWarn:=True))
        End Try
    End Sub

    ''' <summary>写临时脚本：等本进程退出 → 覆盖 exe → 启动新版 → 自删。</summary>
    Friend Sub ApplyUpdate()
        Try
            If String.IsNullOrEmpty(_downloadedPath) OrElse Not File.Exists(_downloadedPath) Then
                MyMsgBox("更新文件已不存在，请重新检查更新。", "更新失败", IsWarn:=True)
                Return
            End If
            Dim exePath As String = Reflection.Assembly.GetExecutingAssembly().Location
            Dim exeDir As String = IO.Path.GetDirectoryName(exePath)
            Dim batPath As String = IO.Path.Combine(exeDir, "XFL\Update", "xfl_apply_update.bat")
            Try
                IO.Directory.CreateDirectory(IO.Path.GetDirectoryName(batPath))
            Catch
                batPath = IO.Path.Combine(IO.Path.GetTempPath(), "xfl_apply_update.bat")
            End Try

            '用 ping 延时（timeout 命令在没有控制台时不可靠）；/mov 失败也继续（占用时重试几次）
            Dim sb As New Text.StringBuilder()
            sb.AppendLine("@echo off")
            sb.AppendLine("chcp 65001 >nul")
            sb.AppendLine("ping 127.0.0.1 -n 4 >nul")
            sb.AppendLine("set /a tries=0")
            sb.AppendLine(":retry")
            sb.AppendLine($"move /y ""{_downloadedPath}"" ""{exePath}"" >nul 2>&1")
            sb.AppendLine("if errorlevel 1 (")
            sb.AppendLine("  set /a tries+=1")
            sb.AppendLine("  if %tries% lss 10 ( ping 127.0.0.1 -n 3 >nul & goto retry )")
            sb.AppendLine("  exit /b 1")
            sb.AppendLine(")")
            sb.AppendLine($"start """" ""{exePath}""")
            sb.AppendLine("del ""%~f0""")
            File.WriteAllText(batPath, sb.ToString(), Text.Encoding.Default)

            Logger.Info($"{Tag} 准备重启替换：{exePath}")
            Process.Start(New ProcessStartInfo(batPath) With {
                .UseShellExecute = True,
                .WindowStyle = ProcessWindowStyle.Hidden,
                .CreateNoWindow = True,
                .WorkingDirectory = exeDir
            })
            '给批处理 ~1.5 秒启动窗口，然后退出主程序
            Threading.Thread.Sleep(300)
            'XFL 批75：Application.Shutdown() 在后台线程调不可靠（窗口不退、exe 被占，替换必失败），
            '   这里直接强制结束进程 —— 批处理已经拿到替换与重启指令，不需要再走正常退出流程。
            Logger.Info($"{Tag} 立即退出以完成替换")
            Try
                Process.GetCurrentProcess().Kill()
            Catch
            End Try
            Environment.Exit(0)
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 应用更新失败")
            MyMsgBox("应用更新失败：" & ex.Message & vbCrLf &
                     "可手动把下载的文件覆盖到程序目录。", "更新失败", IsWarn:=True)
        End Try
    End Sub

    '---------- 设置页进度显示（无 UI 时安全跳过）----------

    ''' <summary>设置页进度文本控件由 PageSetupUI 注册；为空时只写日志。</summary>
    Friend _progressSink As Action(Of Integer) = Nothing

    Private Sub UpdateProgress(pct As Integer)
        Try
            If _progressSink IsNot Nothing Then
                _progressSink(pct)
            Else
                '每 10% 记一条，避免刷屏
                If pct Mod 10 = 0 Then Logger.Info($"{Tag} 下载进度 {pct}%")
            End If
        Catch
        End Try
    End Sub

End Module
