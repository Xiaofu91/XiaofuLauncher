
Imports System.Diagnostics

Module ModDependency

    Public Enum DepStatus
        Unknown = 0
        Checking = 1
        Installed = 2
        Installing = 3
        Failed = 4
        NotApplicable = 5
    End Enum

    Public Property WebView2Status As DepStatus = DepStatus.Unknown
    Public Property EasyTierStatus As DepStatus = DepStatus.Unknown

    Private Const Wv2BootstrapperUrl As String = "https://go.microsoft.com/fwlink/p/?LinkId=2124703"
    Private Const Tag As String = "[XFL][依赖]"

    Public Function IsWebView2Installed() As Boolean
        Try
            Return Not String.IsNullOrWhiteSpace(ModWebView.RuntimeVersion())
        Catch
            Return False
        End Try
    End Function

    Public Sub CheckAll()
        Try
            WebView2Status = If(IsWebView2Installed(), DepStatus.Installed, DepStatus.Checking)
            EasyTierStatus = If(ModEasyTier.IsReady(), DepStatus.Installed, DepStatus.Checking)
            Logger.Info($"{Tag} 状态检查：WebView2={WebView2Status} EasyTier={EasyTierStatus}")
        Catch ex As Exception
            Logger.Warn(ex, Tag & "状态检查失败")
        End Try
    End Sub

    Public Sub EnsureAllAsync(OnProgress As Action(Of String, Double, String), OnDone As Action(Of Boolean, String))
        RunInNewThread(
            Sub()
                Try
                    CheckAll()

                    If Not IsWebView2Installed() Then
                        RunInUi(Sub() OnProgress("webview2", 0.0, "正在下载 WebView2 运行时…"))
                        Dim okWv As Boolean = InstallWebView2(
                            Sub(pct As Double, msg As String)
                                RunInUi(Sub() OnProgress("webview2", pct, msg))
                            End Sub)
                        WebView2Status = If(okWv, DepStatus.Installed, DepStatus.Failed)
                        If Not okWv Then
                            RunInUi(Sub() OnDone(False, "WebView2 运行时安装失败，请查看日志或手动安装"))
                            Return
                        End If
                    Else
                        WebView2Status = DepStatus.Installed
                        RunInUi(Sub() OnProgress("webview2", 1.0, "WebView2 已就绪"))
                    End If

                    If Not ModEasyTier.IsReady() Then
                        EasyTierStatus = DepStatus.Checking
                        RunInUi(Sub() OnProgress("easytier", 0.0, "联机组件将在首次使用联机时下载"))
                    Else
                        EasyTierStatus = DepStatus.Installed
                    End If

                    RunInUi(Sub() OnDone(True, "依赖检查完成"))
                Catch ex As Exception
                    Logger.Error(ex, Tag & "依赖检查/安装异常")
                    RunInUi(Sub() OnDone(False, ex.Message))
                End Try
            End Sub)
    End Sub

    Private Function InstallWebView2(OnProgress As Action(Of Double, String)) As Boolean
        Try
            Dim tmpDir As String = IO.Path.Combine(IO.Path.GetTempPath(), "XiaofuLauncher")
            IO.Directory.CreateDirectory(tmpDir)
            Dim exePath As String = IO.Path.Combine(tmpDir, "MicrosoftEdgeWebview2Setup.exe")

            Logger.Info(Tag & "开始下载 WebView2 Bootstrapper…")
            Using client As New Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromMinutes(5)
                Using resp = client.GetAsync(Wv2BootstrapperUrl, Net.Http.HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult()
                    If Not resp.IsSuccessStatusCode Then
                        Logger.Error(Tag & "WebView2 下载失败：HTTP " & CInt(resp.StatusCode))
                        Return False
                    End If
                    Dim total As Long = If(resp.Content.Headers.ContentLength, -1L)
                    Using src = resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult(),
                          dst As IO.Stream = IO.File.Create(exePath)
                        Dim buf(81919) As Byte
                        Dim readTotal As Long = 0
                        Dim lastPush As Integer = 0
                        Do
                            Dim n As Integer = src.Read(buf, 0, buf.Length)
                            If n <= 0 Then Exit Do
                            dst.Write(buf, 0, n)
                            readTotal += n
                            If total > 0 AndAlso Environment.TickCount - lastPush > 300 Then
                                lastPush = Environment.TickCount
                                Dim pct As Double = readTotal / total
                                RunInUi(Sub() OnProgress(pct * 0.8, $"正在下载 WebView2 运行时… {readTotal / 1048576.0:F1} MB"))
                            End If
                        Loop
                    End Using
                End Using
            End Using
            Logger.Info(Tag & "WebView2 Bootstrapper 下载完成，开始静默安装…")
            RunInUi(Sub() OnProgress(0.85, "正在安装 WebView2 运行时…"))

            Dim psi As New ProcessStartInfo With {
                .FileName = exePath,
                .Arguments = "/silent /install",
                .UseShellExecute = False,
                .CreateNoWindow = True,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True}
            Using p As Process = Process.Start(psi)
                p.WaitForExit(300000)
                Dim code As Integer = p.ExitCode
                Logger.Info(Tag & "WebView2 安装程序退出码：" & code)

                If code = 0 OrElse code = 1638 Then
                    RunInUi(Sub() OnProgress(1.0, "WebView2 运行时已就绪"))
                    Logger.Info(Tag & "WebView2 安装成功")
                    Return True
                ElseIf code = 1618 Then
                    Logger.Warn(Tag & "另一个安装正在进行，稍后重试可恢复")
                    Return False
                Else
                    Logger.Error(Tag & "WebView2 安装失败，退出码 " & code)
                    Return False
                End If
            End Using
        Catch ex As Exception
            Logger.Error(ex, Tag & "安装 WebView2 异常")
            Return False
        End Try
    End Function

End Module