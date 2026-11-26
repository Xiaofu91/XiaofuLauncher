
Imports System.Diagnostics
Imports System.IO
Imports System.IO.Compression

Module ModEasyTier

    Public ReadOnly Property EasyTierDir As String
        Get
            Dim d As String = IO.Path.Combine(Paths.AppDataThenName, "EasyTier")
            Try
                Directory.CreateDirectory(d)
            Catch
            End Try
            Return d
        End Get
    End Property

    Public ReadOnly Property CoreExe As String
        Get
            Return IO.Path.Combine(EasyTierDir, "easytier-core.exe")
        End Get
    End Property

    Public ReadOnly Property CliExe As String
        Get
            Return IO.Path.Combine(EasyTierDir, "easytier-cli.exe")
        End Get
    End Property

    Public Function IsReady() As Boolean
        Try
            If Not File.Exists(CoreExe) Then Return False
            Dim fi As New FileInfo(CoreExe)
            Return fi.Length > 1024 * 1024
        Catch
            Return False
        End Try
    End Function

    Private ReadOnly Mirrors As String() = {
        "https://gh-proxy.com/",
        "https://ghproxy.net/",
        ""}

    Private Const EtVersion As String = "v2.6.4"

    Private Function AssetUrl(arch As String) As String
        Return $"https://github.com/EasyTier/EasyTier/releases/download/{EtVersion}/easytier-windows-{arch}-{EtVersion}.zip"
    End Function

    Public Function CurrentArch() As String

        Try
            Dim manual As String = Settings.Get(Of String)("LinkArch")
            If Not String.IsNullOrWhiteSpace(manual) Then Return manual.Trim()
        Catch
        End Try
        If IntPtr.Size = 4 Then Return "i686"
        Return "x86_64"
    End Function

    Public Sub EnsureAsync(OnProgress As Action(Of Double, String), OnDone As Action(Of Boolean, String))
        RunInNewThread(
            Sub()
                Dim zipPath As String = IO.Path.Combine(EasyTierDir, "EasyTier.zip")
                Dim arch As String = CurrentArch()
                Dim asset As String = AssetUrl(arch)
                Dim lastErr As String = ""
                Dim ok As Boolean = False
                For Each prefix As String In Mirrors
                    Dim url As String = prefix & asset
                    Try
                        RunInUi(Sub() OnProgress(0.02, $"正在下载联机组件（{If(prefix = "", "GitHub 直连", New Uri(prefix).Host)}）…"))
                        Dim handler As New Net.Http.HttpClientHandler With {.UseProxy = True, .AutomaticDecompression = Net.DecompressionMethods.GZip}
                        Using client As New Net.Http.HttpClient(handler)
                            client.Timeout = TimeSpan.FromMinutes(10)
                            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "XiaofuLauncher/2.13")
                            Using resp As Net.Http.HttpResponseMessage =
                                client.GetAsync(url, Net.Http.HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult()
                                If Not resp.IsSuccessStatusCode Then
                                    lastErr = $"HTTP {CInt(resp.StatusCode)}"
                                    Continue For
                                End If
                                Dim total As Long = If(resp.Content.Headers.ContentLength, -1L)
                                Using src As IO.Stream = resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult(),
                                      dst As IO.Stream = File.Create(zipPath)
                                    Dim buf(81919) As Byte
                                    Dim readTotal As Long = 0
                                    Dim lastPush As Integer = 0
                                    Do
                                        Dim n As Integer = src.Read(buf, 0, buf.Length)
                                        If n <= 0 Then Exit Do
                                        dst.Write(buf, 0, n)
                                        readTotal += n
                                        If total > 0 AndAlso Environment.TickCount - lastPush > 250 Then
                                            lastPush = Environment.TickCount
                                            Dim pct As Double = readTotal / total
                                            Dim mb As Double = readTotal / 1048576.0
                                            Dim tmb As Double = total / 1048576.0
                                            RunInUi(Sub() OnProgress(0.02 + pct * 0.8, $"正在下载联机组件… {mb:F1} / {tmb:F1} MB"))
                                        End If
                                    Loop
                                End Using
                            End Using
                        End Using
                        If File.Exists(zipPath) Then
                            Dim zi As New FileInfo(zipPath)
                            If zi.Length > 2 * 1024 * 1024 Then
                                ok = True
                                Exit For
                            End If
                        End If
                    Catch ex As Exception
                        lastErr = ex.Message
                        Logger.Warn(ex, $"[XFL] 联机组件下载失败（{prefix}）")
                        Try
                            File.Delete(zipPath)
                        Catch
                        End Try
                    End Try
                Next

                If Not ok Then
                    RunInUi(Sub() OnDone(False, "下载失败：" & lastErr & vbCrLf &
                                                 "可手动把 easytier-windows-" & arch & " 压缩包解压到：" & vbCrLf & EasyTierDir))
                    Return
                End If

                Try
                    RunInUi(Sub() OnProgress(0.85, "正在解压联机组件…"))
                    Dim tmp As String = IO.Path.Combine(EasyTierDir, "extract_tmp")
                    Try
                        Directory.Delete(tmp, True)
                    Catch
                    End Try
                    Directory.CreateDirectory(tmp)
                    ZipFile.ExtractToDirectory(zipPath, tmp)

                    Dim core As String = Directory.EnumerateFiles(tmp, "easytier-core.exe", SearchOption.AllDirectories).FirstOrDefault()
                    If core Is Nothing Then
                        RunInUi(Sub() OnDone(False, "压缩包里没找到 easytier-core.exe"))
                        Return
                    End If
                    Dim srcDir As String = IO.Path.GetDirectoryName(core)
                    For Each f As String In Directory.EnumerateFiles(srcDir)
                        File.Copy(f, IO.Path.Combine(EasyTierDir, IO.Path.GetFileName(f)), True)
                    Next
                    Try
                        File.Delete(zipPath)
                    Catch
                    End Try
                    Try
                        Directory.Delete(tmp, True)
                    Catch
                    End Try
                    RunInUi(Sub() OnProgress(1.0, "联机组件已就绪"))
                    RunInUi(Sub() OnDone(IsReady(), "组件就绪"))
                Catch ex As Exception
                    Logger.Error(ex, "[XFL] 联机组件解压失败")
                    RunInUi(Sub() OnDone(False, "解压失败：" & ex.Message))
                End Try
            End Sub)
    End Sub

    Private _proc As Process = Nothing

    Public ReadOnly Property IsRunning As Boolean
        Get
            Try
                Return _proc IsNot Nothing AndAlso Not _proc.HasExited
            Catch
                Return False
            End Try
        End Get
    End Property

    Public Property LocalVirtualIp As String = ""

    Public Function StartNetwork(networkName As String, secret As String, Optional peers As String = "") As Boolean
        Try
            StopNetwork()
            If Not IsReady() Then Return False
            If String.IsNullOrWhiteSpace(networkName) Then Return False
            If String.IsNullOrWhiteSpace(secret) Then secret = "xfl"

            Dim args As New System.Text.StringBuilder
            args.Append($"-n ""{networkName}"" -s ""{secret}"" ")
            args.Append("--relay-all-peer-rpc --disable-p2p=false ")
            If Not String.IsNullOrWhiteSpace(peers) Then
                For Each p As String In peers.Split({","c, ";"c, " "c}, StringSplitOptions.RemoveEmptyEntries)
                    args.Append($"-p ""{p.Trim()}"" ")
                Next
            End If

            Dim psi As New ProcessStartInfo With {
                .FileName = CoreExe,
                .Arguments = args.ToString().Trim(),
                .WorkingDirectory = EasyTierDir,
                .UseShellExecute = False,
                .CreateNoWindow = True,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True}
            _proc = Process.Start(psi)
            AddHandler _proc.OutputDataReceived, AddressOf OnCoreOutput
            AddHandler _proc.ErrorDataReceived, AddressOf OnCoreOutput
            _proc.BeginOutputReadLine()
            _proc.BeginErrorReadLine()
            Logger.Info($"[XFL] 联机组件已启动：网络={networkName} 参数={args.ToString().Trim()}")

            RunInNewThread(
                Sub()
                    Threading.Thread.Sleep(6000)
                    RefreshVirtualIp()
                End Sub)
            Return True
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 启动联机组网失败")
            Return False
        End Try
    End Function

    Private Sub OnCoreOutput(sender As Object, e As DataReceivedEventArgs)
        Try
            If String.IsNullOrWhiteSpace(e.Data) Then Return
            Logger.Info("[EasyTier] " & e.Data.Trim())
        Catch
        End Try
    End Sub

    Public Sub RefreshVirtualIp()
        Try
            If Not IsRunning Then Return
            Dim psi As New ProcessStartInfo With {
                .FileName = CliExe,
                .Arguments = "peer",
                .WorkingDirectory = EasyTierDir,
                .UseShellExecute = False,
                .CreateNoWindow = True,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True}
            Using p As Process = Process.Start(psi)
                Dim outp As String = p.StandardOutput.ReadToEnd()
                p.WaitForExit(8000)

                Dim m As System.Text.RegularExpressions.Match =
                    System.Text.RegularExpressions.Regex.Match(outp, "\b(10\.\d{1,3}\.\d{1,3}\.\d{1,3}|172\.(1[6-9]|2\d|3[01])\.\d{1,3}\.\d{1,3})\b")
                If m.Success Then
                    LocalVirtualIp = m.Groups(1).Value
                    Logger.Info($"[XFL] 本机虚拟局域网 IP = {LocalVirtualIp}")
                End If
            End Using
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 读取虚拟 IP 失败")
        End Try
    End Sub

    Public Sub StopNetwork()
        Try
            If _proc IsNot Nothing AndAlso Not _proc.HasExited Then
                _proc.Kill()
                _proc.WaitForExit(3000)
                Logger.Info("[XFL] 联机组件已停止")
            End If
        Catch
        Finally
            _proc = Nothing
            LocalVirtualIp = ""
        End Try
    End Sub

End Module