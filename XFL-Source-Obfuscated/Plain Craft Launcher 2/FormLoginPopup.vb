
Partial Public Class FormMain

    Private IsLoginPageShown As Boolean = False

    Public Sub ShowLoginPage(Show As Boolean)
        Try
            If PanLoginPage Is Nothing Then Return
            IsLoginPageShown = Show
            PanLoginPage.Visibility = If(Show, Visibility.Visible, Visibility.Collapsed)
            If Show Then
                If FrmLaunchLeft IsNot Nothing Then FrmLaunchLeft.MigrateLoginUIToPopup()

                Try
                    If PlayerPreview IsNot Nothing Then

                        Dim isPremium As Boolean = (ModLaunch.McLoginMsLoader.State = LoadState.Finished)
                        If Not isPremium Then
                            Try
                                Dim tk As String = Settings.Get(Of String)("CacheMsV2Access")
                                isPremium = Not String.IsNullOrWhiteSpace(tk)
                            Catch
                            End Try
                        End If
                        PlayerPreview.Premium = isPremium
                        Logger.Info($"[XFL] 3D 预览账号状态：{(If(PlayerPreview.Premium, "正版", "离线"))}（登录加载={ModLaunch.McLoginMsLoader.State}）")

                        PlayerPreview3D.RequestAccountSync()
                    End If
                Catch ex As Exception
                    Logger.Warn(ex, "[XFL] 设置 3D 预览账号状态失败")
                End Try

                Dim tmr As New Threading.DispatcherTimer With {.Interval = TimeSpan.FromSeconds(2)}
                AddHandler tmr.Tick,
                    Sub(s2, e2)
                        Try
                            tmr.Stop()
                            If FrmLaunchLeft Is Nothing OrElse FrmLaunchLeft.PanLogin Is Nothing Then
                                Logger.Info("[XFL] 登录诊断：PanLogin 为空")
                                Return
                            End If
                            Dim pl = FrmLaunchLeft.PanLogin
                            Dim info As String = $"登录诊断：页面={PanLoginPage.Visibility}，宿主子数={PanLoginHost.Children.Count}，PanLogin={pl.ActualWidth}x{pl.ActualHeight} 子数={pl.Children.Count} 可见={pl.Visibility}"
                            If pl.Children.Count > 0 Then
                                Dim ch = pl.Children(0)
                                info &= $"，首子类型={ch.GetType().Name} 尺寸={CType(ch, FrameworkElement).ActualWidth}x{CType(ch, FrameworkElement).ActualHeight} 可见={CType(ch, FrameworkElement).Visibility}"
                            End If
                            Logger.Info("[XFL] " & info)
                        Catch ex As Exception
                            Logger.Error(ex, "[XFL] 登录诊断失败")
                        End Try
                    End Sub
                tmr.Start()
            End If
            Logger.Info($"[XFL] 登录页：{If(Show, "显示", "隐藏")}")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 登录页切换失败")
        End Try
    End Sub

End Class
