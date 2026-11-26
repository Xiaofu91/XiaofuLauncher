
Public Module ModXflHorizon

    Friend Const Tag As String = "[XFL][Horizon]"

    Private Const RepoApi As String = "https://api.github.com/repos/horizon-rust/horizon"
    Private Const AssetWin64 As String = "horizon-windows-x64.zip"

    Public ReadOnly Property HorizonDir As String
        Get
            Dim d As String = IO.Path.Combine(Paths.AppDataThenName, "Horizon")
            Try : Directory.CreateDirectory(d) : Catch : End Try
            Return d
        End Get
    End Property

    Public ReadOnly Property VersionsDir As String
        Get
            Dim d As String = IO.Path.Combine(HorizonDir, "versions")
            Try : Directory.CreateDirectory(d) : Catch : End Try
            Return d
        End Get
    End Property

    Public ReadOnly Property HomeDir As String
        Get
            Dim d As String = IO.Path.Combine(HorizonDir, "home")
            Try : Directory.CreateDirectory(d) : Catch : End Try
            Return d
        End Get
    End Property

    Private ReadOnly Property ServerJarDir As String
        Get
            Dim d As String = IO.Path.Combine(HorizonDir, "server-jars")
            Try : Directory.CreateDirectory(d) : Catch : End Try
            Return d
        End Get
    End Property

    Private ReadOnly Property DlTmpDir As String
        Get
            Dim d As String = IO.Path.Combine(HorizonDir, "download")
            Try : Directory.CreateDirectory(d) : Catch : End Try
            Return d
        End Get
    End Property

    Public Structure HorizonStatus
        Public Installed As Boolean
        Public InstalledTag As String
        Public ClientExe As String
        Public Prepared As Boolean
        Public ResourcesDir As String
        Public CanLaunch As Boolean
    End Structure

    Private Function FindExeNear(root As String, exeName As String) As String
        Try
            Dim c1 As String = IO.Path.Combine(root, exeName)
            If File.Exists(c1) Then Return c1
            Dim c2 As String = IO.Path.Combine(root, "horizon-windows-x64", exeName)
            If File.Exists(c2) Then Return c2
            For Each d In Directory.GetDirectories(root)
                Dim c As String = IO.Path.Combine(d, exeName)
                If File.Exists(c) Then Return c
            Next
        Catch
        End Try
        Return Nothing
    End Function

    Public Function GetStatus() As HorizonStatus
        Dim s As New HorizonStatus
        Try
            If Not Directory.Exists(VersionsDir) Then Return s
            Dim best As String = Nothing
            For Each vdir In Directory.GetDirectories(VersionsDir)
                If FindExeNear(vdir, "horizon-client.exe") IsNot Nothing Then
                    If best Is Nothing OrElse String.Compare(vdir, best, StringComparison.Ordinal) > 0 Then best = vdir
                End If
            Next
            If best Is Nothing Then Return s
            s.Installed = True
            s.InstalledTag = IO.Path.GetFileName(best)
            s.ClientExe = FindExeNear(best, "horizon-client.exe")
            Dim resA As String = IO.Path.Combine(best, ".local", "horizon-client", "26.3")
            Dim resB As String = IO.Path.Combine(HorizonDir, "resources", s.InstalledTag)
            If Directory.Exists(resB) AndAlso Directory.EnumerateFiles(resB).Any() Then
                s.Prepared = True
                s.ResourcesDir = resB
            ElseIf Directory.Exists(resA) AndAlso Directory.EnumerateFiles(resA).Any() Then
                s.Prepared = True
                s.ResourcesDir = resA
            End If
            s.CanLaunch = s.Installed AndAlso s.Prepared AndAlso s.ClientExe IsNot Nothing
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 读取状态失败")
        End Try
        Return s
    End Function

    Public Function HasPremiumMs() As Boolean
        Try

            Dim lt As McLoginType = Settings.Get(Of McLoginType)("LoginType")
            If lt <> McLoginType.Ms Then Return False

            If McLoginLoader Is Nothing Then Return False
            If McLoginLoader.State <> LoadState.Finished Then Return False
            Dim t As String = If(McLoginLoader.Output.Type, "")
            Dim isMs As Boolean =
                t.Equals("Microsoft", StringComparison.OrdinalIgnoreCase) OrElse
                t.Equals("Ms", StringComparison.OrdinalIgnoreCase)
            If Not isMs Then Return False

            If String.IsNullOrWhiteSpace(McLoginLoader.Output.Name) Then Return False
            If String.IsNullOrWhiteSpace(McLoginLoader.Output.AccessToken) Then Return False
            Return True
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 正版状态检查失败")
            Return False
        End Try
    End Function

    Public Function FindVanilla263Json() As String
        Try
            Dim root As String = McFolderSelected
            If String.IsNullOrWhiteSpace(root) Then Return Nothing
            Dim vdir As String = IO.Path.Combine(root, "versions")
            If Not Directory.Exists(vdir) Then Return Nothing
            For Each d In Directory.GetDirectories(vdir)
                Dim nm As String = IO.Path.GetFileName(d)
                If nm = "26.3" OrElse nm.StartsWithF("26.3") Then
                    Dim js As String = IO.Path.Combine(d, nm & ".json")
                    If File.Exists(js) Then Return js
                End If
            Next
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 查找本地 26.3 失败")
        End Try
        Return Nothing
    End Function

    Private Function JavaMajor(home As String) As Integer
        Try
            Dim rel As String = IO.Path.Combine(home, "release")
            If File.Exists(rel) Then
                For Each line In File.ReadAllLines(rel)
                    If line.StartsWithF("JAVA_VERSION=") Then
                        Dim v As String = line.AfterFirst("=").Trim(""""c)
                        Dim parts As String() = v.Split("."c)
                        Dim maj As Integer
                        If parts.Length >= 1 AndAlso Integer.TryParse(parts(0), maj) Then Return maj
                    End If
                Next
            End If
        Catch
        End Try
        Return 0
    End Function

    Public Function FindJdk25() As String
        Try
            Dim cands As New List(Of String)
            Dim jh As String = Environment.GetEnvironmentVariable("JAVA_HOME")
            If Not String.IsNullOrWhiteSpace(jh) Then cands.Add(jh)
            Dim bases As New List(Of String)
            bases.Add("C:\Program Files\Java")
            bases.Add("C:\Program Files\Eclipse Adoptium")
            bases.Add("C:\Program Files\Microsoft")
            bases.Add("C:\Program Files\Zulu")
            bases.Add("C:\Program Files\BellSoft")
            bases.Add(IO.Path.Combine(Paths.AppDataThenName, "Java"))
            For Each jb In bases
                If Directory.Exists(jb) Then
                    For Each d In Directory.GetDirectories(jb)
                        cands.Add(d)
                    Next
                End If
            Next
            For Each c In cands
                Dim javac As String = IO.Path.Combine(c, "bin", "javac.exe")
                If File.Exists(javac) Then
                    If JavaMajor(c) >= 25 Then Return c
                End If
            Next
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 查找 JDK25 失败")
        End Try
        Return Nothing
    End Function

    Public Structure NightlyInfo
        Public Ok As Boolean
        Public VersionTag As String
        Public ZipUrl As String
        Public SumsUrl As String
        Public Size As Long
        Public ErrMsg As String
    End Structure

    Private Function MirrorUrls(raw As String) As List(Of String)
        Dim r As New List(Of String)
        If String.IsNullOrWhiteSpace(raw) Then Return r
        r.Add("https://gh-proxy.com/" & raw)
        r.Add("https://ghproxy.net/" & raw)
        r.Add(raw)
        Return r
    End Function

    Public Function QueryNightly() As NightlyInfo
        Dim r As New NightlyInfo
        Dim json As JObject = Nothing
        Dim lastErr As String = ""
        Dim apis As New List(Of String)
        apis.Add(RepoApi & "/releases/tags/nightly")
        apis.Add("https://mcp.playhorizon.dev/github/public/repos/horizon-rust/horizon/releases/tags/nightly")
        For Each api In apis
            Try
                Dim rawTxt As String = NetRequestByClientRetry(api, RequireJson:=True)
                If String.IsNullOrWhiteSpace(rawTxt) Then
                    lastErr = "空响应"
                Else
                    json = JObject.Parse(rawTxt)
                    If json IsNot Nothing AndAlso json("assets") IsNot Nothing Then Exit For
                    lastErr = "无 assets 字段"
                End If
            Catch ex As Exception
                lastErr = ex.Message
                Logger.Warn(ex, $"{Tag} nightly 查询失败：{api}")
            End Try
        Next
        If json Is Nothing Then
            r.Ok = False
            r.ErrMsg = If(String.IsNullOrWhiteSpace(lastErr), "无法获取版本信息", lastErr)
            Return r
        End If
        Try
            Dim nm As String = If(json("name")?.ToString, "nightly")
            Dim mm As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(nm, "\d{4}-\d{2}-\d{2}")
            r.VersionTag = "nightly-" & If(mm.Success, mm.Value, DateTime.Now.ToString("yyyy-MM-dd"))
            For Each a In CType(json("assets"), Newtonsoft.Json.Linq.JArray)
                Dim an As String = If(a("name")?.ToString, "")
                If an = AssetWin64 Then
                    r.ZipUrl = If(a("browser_download_url")?.ToString, "")
                    r.Size = CLng(If(a("size")?.ToObject(Of Double), 0))
                ElseIf an = "SHA256SUMS" Then
                    r.SumsUrl = If(a("browser_download_url")?.ToString, "")
                End If
            Next
            If String.IsNullOrWhiteSpace(r.ZipUrl) Then
                r.Ok = False
                r.ErrMsg = "未找到 " & AssetWin64
            Else
                r.Ok = True
            End If
        Catch ex As Exception
            r.Ok = False
            r.ErrMsg = ex.Message
            Logger.Warn(ex, $"{Tag} 解析 nightly 失败")
        End Try
        Return r
    End Function

    Private Function ParseSha(sumsText As String, fileName As String) As String
        Try
            Dim seps As Char() = {vbCr, vbLf}
            For Each line In sumsText.Split(seps, StringSplitOptions.RemoveEmptyEntries)
                Dim sp As Char() = {" "c}
                Dim parts As String() = line.Trim.Split(sp, StringSplitOptions.RemoveEmptyEntries)
                If parts.Length >= 2 Then
                    Dim fname As String = parts(parts.Length - 1).TrimStart("*"c)
                    If fname.Equals(fileName, StringComparison.OrdinalIgnoreCase) Then Return parts(0).ToLower
                End If
            Next
        Catch
        End Try
        Return ""
    End Function

    Private Function Sha256OfFile(path As String) As String
        Try
            Using sha = System.Security.Cryptography.SHA256.Create()
                Using fs As IO.FileStream = IO.File.OpenRead(path)
                    Dim bytes As Byte() = sha.ComputeHash(fs)
                    Dim sb As New System.Text.StringBuilder
                    For Each b In bytes
                        sb.Append(b.ToString("x2"))
                    Next
                    Return sb.ToString()
                End Using
            End Using
        Catch
            Return ""
        End Try
    End Function

    Private Function Sha1OfFile(path As String) As String
        Try
            Using sha = System.Security.Cryptography.SHA1.Create()
                Using fs As IO.FileStream = IO.File.OpenRead(path)
                    Dim bytes As Byte() = sha.ComputeHash(fs)
                    Dim sb As New System.Text.StringBuilder
                    For Each b In bytes
                        sb.Append(b.ToString("x2"))
                    Next
                    Return sb.ToString()
                End Using
            End Using
        Catch
            Return ""
        End Try
    End Function

    Public Function StartInstall(info As NightlyInfo) As Object
        Try
            If Not info.Ok Then
                Hint("无法安装：" & If(String.IsNullOrWhiteSpace(info.ErrMsg), "未知错误", info.ErrMsg), HintType.Red)
                Return Nothing
            End If
            Dim tag As String = info.VersionTag
            Dim destDir As String = IO.Path.Combine(VersionsDir, tag)
            Dim zipTmp As String = IO.Path.Combine(DlTmpDir, tag & ".zip")
            Dim sumsTmp As String = IO.Path.Combine(DlTmpDir, tag & ".SHA256SUMS")

            Dim zipUrls As List(Of String) = MirrorUrls(info.ZipUrl)
            Dim sumsUrls As List(Of String) = MirrorUrls(info.SumsUrl)

            Dim loaders As New List(Of LoaderBase)
            If sumsUrls.Count > 0 Then
                loaders.Add(New LoaderDownload("Horizon 校验清单", {
                    New NetFile(sumsUrls.ToArray, sumsTmp, New FileChecker With {.MinSize = 16})
                }) With {.ProgressWeight = 0.5, .Block = True})
            End If
            loaders.Add(New LoaderDownload("Horizon 游戏本体", {
                New NetFile(zipUrls.ToArray, zipTmp,
                    New FileChecker With {.MinSize = 1024 * 1024, .ActualSize = If(info.Size > 0, info.Size, -1)})
            }) With {.ProgressWeight = 20, .Block = True})

            Dim capturedSums As String = sumsTmp
            Dim capturedZip As String = zipTmp
            Dim capturedDest As String = destDir
            Dim capturedTag As String = tag
            loaders.Add(New LoaderTask(Of Integer, Integer)("校验并解压",
            Sub(Task As LoaderTask(Of Integer, Integer))
                Try
                    If File.Exists(capturedSums) Then
                        Dim want As String = ParseSha(File.ReadAllText(capturedSums), AssetWin64)
                        If want <> "" Then
                            Task.Progress = 0.1
                            Dim got As String = Sha256OfFile(capturedZip)
                            If got <> "" AndAlso Not got.Equals(want, StringComparison.OrdinalIgnoreCase) Then
                                Throw New Exception("SHA256 校验不匹配，可能镜像数据损坏，请重试或更换网络")
                            End If
                            Logger.Info($"{Tag} SHA256 校验通过")
                        End If
                    End If
                    If Directory.Exists(capturedDest) Then Directory.Delete(capturedDest, True)
                    Directory.CreateDirectory(capturedDest)
                    Task.Progress = 0.4
                    FileUtils.ExtractToDirectory(capturedZip, capturedDest,
                        Sub(Percentage) Task.Progress = 0.4 + Percentage * 0.5)
                    Task.Progress = 1
                    Try : File.Delete(capturedZip) : Catch : End Try
                    Logger.Info($"{Tag} 安装完成：{capturedDest}")
                Catch ex As Exception
                    Throw New Exception("解压/校验失败：" & ex.Message, ex)
                End Try
            End Sub) With {.ProgressWeight = 4})

            Dim combo As New LoaderCombo(Of String)("Horizon 安装 " & capturedTag, loaders)
            combo.Start(capturedTag)
            LoaderTaskbarAdd(combo)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()
            Return combo
        Catch ex As Exception
            Logger.Error(ex, $"{Tag} 启动安装失败")
            Hint("启动安装失败：" & ex.Message, HintType.Red)
            Return Nothing
        End Try
    End Function

    Public Function StartPrepare(st As HorizonStatus) As Object
        Try
            If Not st.Installed Then
                Hint("请先安装 Horizon", HintType.Red)
                Return Nothing
            End If
            If Not HasPremiumMs() Then
                MyMsgBox("Horizon 需要从你自己的 Minecraft Java Edition 26.3 构建游戏文件，这要求登录一个拥有 Java Edition 的微软账号（正版）。请先在「登录」页登录微软正版账号后再准备。", "需要正版账号", "我知道了")
                Return Nothing
            End If
            Dim vjson As String = FindVanilla263Json()
            If vjson Is Nothing Then
                MyMsgBox("Horizon 准备器需要本机已安装的 Minecraft 26.3（官方布局）。请先在「下载」页安装 26.3 并至少启动一次，再回来准备。", "缺少 26.3 安装", "我知道了")
                Return Nothing
            End If
            Dim jdk As String = FindJdk25()
            If jdk Is Nothing Then
                MyMsgBox("Horizon 准备器需要 JDK 25（含 javac）来运行 Mojang 的数据导出器。未检测到 JDK 25，请安装后重试（如 Adoptium Temurin 25）。", "缺少 JDK 25", "我知道了")
                Return Nothing
            End If

            Dim tag As String = st.InstalledTag
            Dim verDir As String = IO.Path.Combine(VersionsDir, tag)
            Dim prepExe As String = FindExeNear(verDir, "horizon-prepare.exe")
            If prepExe Is Nothing Then
                Hint("未找到 horizon-prepare.exe（安装可能不完整）", HintType.Red)
                Return Nothing
            End If
            Dim resDir As String = IO.Path.Combine(HorizonDir, "resources", tag)
            Dim jarPath As String = IO.Path.Combine(ServerJarDir, "26.3-server.jar")

            Dim serverUrl As String = ""
            Dim serverSha As String = ""
            Try
                Dim vj As JObject = JObject.Parse(File.ReadAllText(vjson))
                serverUrl = If(vj("downloads")?("server")?("url")?.ToString, "")
                serverSha = If(vj("downloads")?("server")?("sha1")?.ToString, "")
            Catch ex As Exception
                Logger.Warn(ex, $"{Tag} 解析 server 下载地址失败")
            End Try
            If serverUrl = "" Then
                Hint("26.3 的 version json 里没有 server 下载地址", HintType.Red)
                Return Nothing
            End If

            Dim loaders As New List(Of LoaderBase)
            Dim needJar As Boolean = True
            If File.Exists(jarPath) AndAlso serverSha <> "" AndAlso Sha1OfFile(jarPath) = serverSha Then needJar = False
            If needJar Then
                Dim chk As FileChecker = Nothing
                If serverSha <> "" Then
                    chk = New FileChecker With {.MinSize = 1024 * 1024, .Hash = serverSha}
                Else
                    chk = New FileChecker With {.MinSize = 1024 * 1024}
                End If
                loaders.Add(New LoaderDownload("下载 26.3 Server Jar", {
                    New NetFile({serverUrl}, jarPath, chk)
                }) With {.ProgressWeight = 6, .Block = True})
            End If

            Dim capturedPrep As String = prepExe
            Dim capturedJdk As String = jdk
            Dim capturedJson As String = vjson
            Dim capturedJar As String = jarPath
            Dim capturedRes As String = resDir
            Dim capturedHome As String = HomeDir
            loaders.Add(New LoaderTask(Of Integer, Integer)("构建游戏资源（horizon-prepare）",
            Sub(Task As LoaderTask(Of Integer, Integer))
                RunPrepare(capturedPrep, capturedJdk, capturedJson, capturedJar, capturedRes, capturedHome, Task)
            End Sub) With {.ProgressWeight = 12, .Block = True})

            Dim combo As New LoaderCombo(Of String)("Horizon 准备 " & tag, loaders)
            combo.Start(tag)
            LoaderTaskbarAdd(combo)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()
            Return combo
        Catch ex As Exception
            Logger.Error(ex, $"{Tag} 启动准备失败")
            Hint("启动准备失败：" & ex.Message, HintType.Red)
            Return Nothing
        End Try
    End Function

    Private Sub RunPrepare(prepExe As String, jdkHome As String, vjson As String, jarPath As String, resDir As String, homeDir As String, Task As LoaderTask(Of Integer, Integer))
        Dim mcRoot As String = Nothing
        Try
            Directory.CreateDirectory(resDir)
            Task.Progress = 0.05
            Dim mcVerDir As String = IO.Path.GetDirectoryName(vjson)
            mcRoot = IO.Path.GetFullPath(IO.Path.Combine(mcVerDir, "..", ".."))
            Dim psi As New ProcessStartInfo
            psi.FileName = prepExe
            psi.UseShellExecute = False
            psi.CreateNoWindow = True
            psi.RedirectStandardOutput = True
            psi.RedirectStandardError = True
            psi.WorkingDirectory = IO.Path.GetDirectoryName(prepExe)
            psi.EnvironmentVariables("JAVA_HOME") = jdkHome
            psi.EnvironmentVariables("HORIZON_HOME") = homeDir
            psi.Arguments = "--prepare --minecraft-dir """ & mcRoot & """ --server-jar """ & jarPath & """ --output """ & resDir & """"
            Dim proc As Process = Process.Start(psi)
            Dim outp As String = proc.StandardOutput.ReadToEnd()
            Dim errp As String = proc.StandardError.ReadToEnd()
            proc.WaitForExit()
            Task.Progress = 0.95
            Logger.Info($"{Tag} prepare 退出码={proc.ExitCode} 输出={outp.Substring(0, Math.Min(200, outp.Length))}")
            If proc.ExitCode <> 0 Then
                Dim tail As String = If(String.IsNullOrWhiteSpace(errp), outp, errp)
                If tail.Length > 400 Then tail = tail.Substring(0, 400)
                Throw New Exception("准备失败（退出码 " & proc.ExitCode & "）：" & tail)
            End If
            Try
                Dim psi2 As New ProcessStartInfo
                psi2.FileName = prepExe
                psi2.UseShellExecute = False
                psi2.CreateNoWindow = True
                psi2.RedirectStandardOutput = True
                psi2.RedirectStandardError = True
                psi2.WorkingDirectory = IO.Path.GetDirectoryName(prepExe)
                psi2.EnvironmentVariables("JAVA_HOME") = jdkHome
                psi2.EnvironmentVariables("HORIZON_HOME") = homeDir
                psi2.Arguments = "sounds --minecraft-dir """ & mcRoot & """ --output """ & resDir & """"
                Dim p2 As Process = Process.Start(psi2)
                p2.StandardOutput.ReadToEnd()
                p2.StandardError.ReadToEnd()
                p2.WaitForExit()
                Logger.Info($"{Tag} sounds 退出码={p2.ExitCode}")
            Catch exS As Exception
                Logger.Warn(exS, $"{Tag} 声音构建失败（不影响进入游戏，仅可能无声）")
            End Try
            Task.Progress = 1
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 准备流程失败")
            Throw
        End Try
    End Sub

    Public Function LaunchHorizon(st As HorizonStatus, Optional world As String = Nothing, Optional creative As Boolean = False) As Boolean
        Try
            If Not st.CanLaunch Then
                Hint("Horizon 尚未安装并准备完成", HintType.Red)
                Return False
            End If
            Dim client As String = st.ClientExe
            If client Is Nothing OrElse Not File.Exists(client) Then
                Hint("找不到 horizon-client.exe", HintType.Red)
                Return False
            End If
            Dim psi As New ProcessStartInfo
            psi.FileName = client
            psi.UseShellExecute = False
            psi.WorkingDirectory = IO.Path.GetDirectoryName(client)
            psi.EnvironmentVariables("HORIZON_HOME") = HomeDir
            Dim args As String = "--resources """ & st.ResourcesDir & """"
            If Not String.IsNullOrWhiteSpace(world) Then args = args & " --world """ & world & """"
            If creative Then args = args & " --creative"
            psi.Arguments = args
            Dim proc As Process = Process.Start(psi)
            Logger.Info($"{Tag} 已启动 Horizon（PID={proc.Id}）：{args}")
            Return True
        Catch ex As Exception
            Logger.Error(ex, $"{Tag} 启动失败")
            Hint("启动 Horizon 失败：" & ex.Message, HintType.Red)
            Return False
        End Try
    End Function

End Module
