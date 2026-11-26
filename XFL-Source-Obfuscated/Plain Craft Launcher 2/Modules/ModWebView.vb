
Imports Microsoft.Web.WebView2.Core

Module ModWebView

    Private _env As CoreWebView2Environment = Nothing
    Private _creating As Boolean = False

    Private _envRetried As Boolean = False
    Private ReadOnly _waiters As New List(Of Action(Of CoreWebView2Environment))
    Private ReadOnly _lock As New Object

    Private Function DataDir() As String
        Try
            Dim d As String = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                              "XiaofuLauncher", "WebView2")
            IO.Directory.CreateDirectory(d)
            Return d
        Catch
            Try
                Dim d2 As String = IO.Path.Combine(Paths.Base, "XFL", "WebView2")
                IO.Directory.CreateDirectory(d2)
                Return d2
            Catch
                Return ""
            End Try
        End Try
    End Function

    Public Sub EnsureEnvAsync(OnReady As Action(Of CoreWebView2Environment))
        If OnReady Is Nothing Then Return
        SyncLock _lock
            If _env IsNot Nothing Then
                RunInUi(Sub() OnReady(_env))
                Return
            End If
            _waiters.Add(OnReady)
            If _creating Then Return
            _creating = True
        End SyncLock

        Try
            Dim dir As String = DataDir()

            Dim task As Threading.Tasks.Task(Of CoreWebView2Environment) =
                Threading.Tasks.Task.Run(
                    Function()
                        Try

                            Dim result As CoreWebView2Environment = Nothing
                            Dim err As Exception = Nothing
                            Dim sta As New Threading.Thread(
                                Sub()
                                    Try
                                        result = If(dir.Length > 0,
                                                    CoreWebView2Environment.CreateAsync(Nothing, dir).GetAwaiter().GetResult(),
                                                    CoreWebView2Environment.CreateAsync(Nothing, Nothing).GetAwaiter().GetResult())
                                    Catch ex As Exception
                                        err = ex
                                    End Try
                                End Sub)
                            sta.SetApartmentState(Threading.ApartmentState.STA)
                            sta.Start()
                            sta.Join()
                            If err IsNot Nothing Then Throw err
                            Return result
                        Catch ex As Exception

                            Logger.Warn(ex, "[XFL] CreateAsync 内部失败（将回退默认环境）")
                            Return Nothing
                        End Try
                    End Function)
            If Not task.Wait(20000) Then
                Logger.Error("[XFL] 创建 WebView2 共享环境超时（20s）")
            End If
            Dim env As CoreWebView2Environment = Nothing
            Try
                If task.IsCompleted AndAlso Not task.IsFaulted AndAlso task.Result IsNot Nothing Then env = task.Result
            Catch ex0 As Exception
                Logger.Warn(ex0, "[XFL] CreateAsync 抛异常")
            End Try
            If env Is Nothing AndAlso Not _envRetried Then

                _envRetried = True
                Dim staleDir As String = ""
                Try : staleDir = DataDir() : Catch : End Try
                If staleDir.Length > 0 Then
                    Try
                        Logger.Warn($"[XFL] WebView2 数据目录可能损坏，尝试重建：{staleDir}")
                        Threading.Thread.Sleep(600)
                        If IO.Directory.Exists(staleDir) Then IO.Directory.Delete(staleDir, True)
                    Catch ex2 As Exception
                        Logger.Warn(ex2, "[XFL] 重建数据目录失败（可能有进程占用）")
                    End Try
                End If
                Dim waitersNow As List(Of Action(Of CoreWebView2Environment))
                SyncLock _lock
                    _creating = False
                    waitersNow = _waiters.ToList()
                    _waiters.Clear()
                End SyncLock
                RunInUi(
                    Sub()
                        For Each a As Action(Of CoreWebView2Environment) In waitersNow
                            Try
                                EnsureEnvAsync(a)
                            Catch
                            End Try
                        Next
                    End Sub)
                Return
            End If
            If env Is Nothing Then Logger.Warn("[XFL] 创建 WebView2 共享环境失败，将回退默认环境")
            If env IsNot Nothing Then Logger.Info("[XFL] 共享环境创建成功")
            Dim list As List(Of Action(Of CoreWebView2Environment))
            SyncLock _lock
                If env IsNot Nothing Then _env = env
                list = _waiters.ToList()
                _waiters.Clear()
                _creating = False
            End SyncLock
            RunInUi(
                Sub()
                    For Each a As Action(Of CoreWebView2Environment) In list
                        Try
                            a(env)
                        Catch ex As Exception
                            Logger.Error(ex, "[XFL] WebView2 环境回调失败")
                        End Try
                    Next
                End Sub)
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 创建 WebView2 共享环境异常")
            SyncLock _lock
                _waiters.Clear()
                _creating = False
            End SyncLock
            RunInUi(Sub() OnReady(Nothing))
        End Try
    End Sub

    Public Function RuntimeVersion() As String
        Try
            Return CoreWebView2Environment.GetAvailableBrowserVersionString()
        Catch
            Return ""
        End Try
    End Function

End Module