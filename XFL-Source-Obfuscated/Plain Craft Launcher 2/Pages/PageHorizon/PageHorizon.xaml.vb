
Partial Public Class PageHorizon

    Private _timer As System.Windows.Threading.DispatcherTimer = Nothing
    Private _busy As Boolean = False

    Public Sub New()
        InitializeComponent()
        AddHandler Me.Loaded, AddressOf Page_Loaded
        AddHandler Me.Unloaded, AddressOf Page_Unloaded
        AddHandler BtnInstall.Click, AddressOf BtnInstall_Click
        AddHandler BtnCheckUpdate.Click, AddressOf BtnCheckUpdate_Click
        AddHandler BtnPrepare.Click, AddressOf BtnPrepare_Click
        AddHandler BtnLaunch.Click, AddressOf BtnLaunch_Click
        AddHandler BtnLaunchCreative.Click, AddressOf BtnLaunchCreative_Click
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Try
            RefreshEnv()
            RefreshInstall()

            _timer = New System.Windows.Threading.DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(1500)}
            AddHandler _timer.Tick, AddressOf Timer_Tick
            _timer.Start()
        Catch ex As Exception
            Logger.Error(ex, "[XFL] Horizon 页加载失败")
        End Try
    End Sub

    Private Sub Page_Unloaded(sender As Object, e As RoutedEventArgs)
        Try
            If _timer IsNot Nothing Then
                _timer.Stop()
                _timer = Nothing
            End If
        Catch ex As Exception
            Logger.Error(ex, "[XFL] Horizon 页卸载失败")
        End Try
    End Sub

    Public Sub OnPageHide()
        Try
            If _timer IsNot Nothing Then _timer.Stop()
        Catch
        End Try
    End Sub

    Private Sub Timer_Tick(sender As Object, e As EventArgs)
        Try

            Dim busy As Boolean = False
            Dim prog As Double = 0
            For Each l In LoaderTaskbar.ToList()
                If l IsNot Nothing AndAlso l.Name IsNot Nothing AndAlso l.Name.StartsWithF("Horizon ") Then
                    If l.State = LoadState.Loading OrElse l.State = LoadState.Waiting Then
                        busy = True
                        prog = Math.Max(prog, l.Progress)
                    End If
                End If
            Next
            _busy = busy
            BarInst.Visibility = If(busy, Visibility.Visible, Visibility.Collapsed)
            BarPrep.Visibility = If(busy, Visibility.Visible, Visibility.Collapsed)
            If busy Then
                BarInst.Value = prog
                BarPrep.Value = prog
            End If
            RefreshInstall()
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] Horizon 页刷新失败")
        End Try
    End Sub

    Private Sub RefreshEnv()
        Try

            Dim okMs As Boolean = ModXflHorizon.HasPremiumMs()
            SetEnv(LabEnvPremiumIcon, LabEnvPremium, okMs,
                "正版账号：已登录微软账号", "正版账号：未登录（准备阶段需要）")

            Dim v263 As String = ModXflHorizon.FindVanilla263Json()
            SetEnv(LabEnv263Icon, LabEnv263, v263 IsNot Nothing,
                "本地 26.3 安装：已就绪", "本地 26.3 安装：未找到（请先在下载页安装 26.3）")

            Dim jdk As String = ModXflHorizon.FindJdk25()
            SetEnv(LabEnvJdkIcon, LabEnvJdk, jdk IsNot Nothing,
                If(jdk IsNot Nothing, "JDK 25：已找到", "JDK 25：未找到"),
                "JDK 25：未找到（准备阶段需要，如 Adoptium Temurin 25）")
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] Horizon 环境检查失败")
        End Try
    End Sub

    Private Sub SetEnv(icon As Windows.Controls.TextBlock, lab As Windows.Controls.TextBlock, ok As Boolean, okText As String, badText As String)
        Try
            If icon Is Nothing OrElse lab Is Nothing Then Return
            lab.Text = If(ok, okText, badText)

            icon.SetResourceReference(Windows.Controls.TextBlock.ForegroundProperty, If(ok, "ColorBrushSuccess", "ColorBrushRedLight"))
            lab.SetResourceReference(Windows.Controls.TextBlock.ForegroundProperty, If(ok, "ColorBrushTextSecondary", "ColorBrushRedLight"))
        Catch
        End Try
    End Sub

    Private Sub RefreshInstall()
        Try
            Dim st = ModXflHorizon.GetStatus()
            If Not st.Installed Then
                LabInstStatus.Text = "Horizon：未安装"
                LabInstTip.Text = "首次使用需要下载游戏本体（约 76MB）"
                BtnInstall.Text = "下载并安装"
                BtnInstall.IsEnabled = Not _busy
                PanPrepare.Visibility = Visibility.Collapsed
                PanLaunch.Visibility = Visibility.Collapsed
            Else
                LabInstStatus.Text = "Horizon：已安装（" & st.InstalledTag & "）"
                If st.Prepared Then
                    LabInstTip.Text = "游戏资源已就绪，可以启动。"
                    BtnInstall.Text = "重新安装"
                    PanPrepare.Visibility = Visibility.Collapsed
                    PanLaunch.Visibility = Visibility.Visible
                    BtnLaunch.IsEnabled = Not _busy
                    BtnLaunchCreative.IsEnabled = Not _busy
                Else
                    LabInstTip.Text = "还需从本地 26.3 构建游戏资源才能启动。"
                    BtnInstall.Text = "重新安装"
                    PanPrepare.Visibility = Visibility.Visible
                    PanLaunch.Visibility = Visibility.Collapsed
                    BtnPrepare.IsEnabled = Not _busy
                End If
            End If
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] Horizon 安装状态刷新失败")
        End Try
    End Sub

    Private Sub BtnInstall_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            If _busy Then Return
            _busy = True
            BtnInstall.IsEnabled = False
            Hint("正在查询 Horizon 最新版本…", HintType.Blue)
            RunInNewThread(
            Sub()
                Dim info = ModXflHorizon.QueryNightly()
                RunInUi(
                Sub()
                    Try
                        If Not info.Ok Then
                            _busy = False
                            BtnInstall.IsEnabled = True
                            Hint("查询失败：" & info.ErrMsg, HintType.Red)
                            Return
                        End If
                        Dim combo = ModXflHorizon.StartInstall(info)
                        If combo Is Nothing Then
                            _busy = False
                            BtnInstall.IsEnabled = True
                        Else
                            Hint("已开始下载 Horizon " & info.VersionTag & "，可在下载管理查看进度", HintType.Green)
                        End If
                    Catch ex As Exception
                        _busy = False
                        Logger.Error(ex, "[XFL] Horizon 安装触发失败")
                    End Try
                End Sub)
            End Sub, "XFL Horizon Install")
        Catch ex As Exception
            _busy = False
            Logger.Error(ex, "[XFL] Horizon 安装按钮失败")
        End Try
    End Sub

    Private Sub BtnCheckUpdate_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            If _busy Then Return
            _busy = True
            BtnCheckUpdate.IsEnabled = False
            Hint("正在检查更新…", HintType.Blue)
            RunInNewThread(
            Sub()
                Dim info = ModXflHorizon.QueryNightly()
                RunInUi(
                Sub()
                    Try
                        BtnCheckUpdate.IsEnabled = True
                        _busy = False
                        If Not info.Ok Then
                            Hint("检查失败：" & info.ErrMsg, HintType.Red)
                            Return
                        End If
                        Dim st = ModXflHorizon.GetStatus()
                        If st.Installed AndAlso st.InstalledTag = info.VersionTag Then
                            Hint("已是最新版本（" & info.VersionTag & "）", HintType.Blue)
                        Else
                            If MyMsgBox("发现新版本：" & info.VersionTag & vbCrLf &
                                        (If(st.Installed, "当前已安装：" & st.InstalledTag & vbCrLf, "")) &
                                        "是否下载并安装？", "Horizon 更新", "下载", "取消") = 1 Then
                                _busy = True
                                Dim combo = ModXflHorizon.StartInstall(info)
                                If combo Is Nothing Then _busy = False
                            End If
                        End If
                    Catch ex As Exception
                        _busy = False
                        Logger.Error(ex, "[XFL] Horizon 更新检查失败")
                    End Try
                End Sub)
            End Sub, "XFL Horizon Update")
        Catch ex As Exception
            _busy = False
            Logger.Error(ex, "[XFL] Horizon 更新按钮失败")
        End Try
    End Sub

    Private Sub BtnPrepare_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            If _busy Then Return
            _busy = True
            BtnPrepare.IsEnabled = False
            Dim st = ModXflHorizon.GetStatus()
            RunInNewThread(
            Sub()
                Dim combo = ModXflHorizon.StartPrepare(st)
                RunInUi(
                Sub()
                    Try
                        If combo Is Nothing Then
                            _busy = False
                            BtnPrepare.IsEnabled = True
                        Else
                            Hint("已开始准备 Horizon 游戏资源，可在下载管理查看进度", HintType.Green)
                        End If
                    Catch ex As Exception
                        _busy = False
                        Logger.Error(ex, "[XFL] Horizon 准备触发失败")
                    End Try
                End Sub)
            End Sub, "XFL Horizon Prepare")
        Catch ex As Exception
            _busy = False
            Logger.Error(ex, "[XFL] Horizon 准备按钮失败")
        End Try
    End Sub

    Private Sub BtnLaunch_Click(sender As Object, e As MouseButtonEventArgs)
        DoLaunch(False)
    End Sub

    Private Sub BtnLaunchCreative_Click(sender As Object, e As MouseButtonEventArgs)
        DoLaunch(True)
    End Sub

    Private Sub DoLaunch(creative As Boolean)
        Try
            Dim st = ModXflHorizon.GetStatus()
            If Not st.CanLaunch Then
                Hint("Horizon 尚未安装并准备完成", HintType.Red)
                Return
            End If
            If ModXflHorizon.LaunchHorizon(st, Nothing, creative) Then
                Hint("已启动 Horizon" & If(creative, "（创造模式）", ""), HintType.Green)
            End If
        Catch ex As Exception
            Logger.Error(ex, "[XFL] Horizon 启动失败")
        End Try
    End Sub

End Class
