
Imports System.Windows.Threading

Partial Public Class PageLinkXfl

    Private _timer As DispatcherTimer = Nothing
    Private _logLines As New List(Of String)

    Public Sub New()
        InitializeComponent()
        AddHandler Me.Loaded, AddressOf Page_Loaded
        AddHandler Me.Unloaded, AddressOf Page_Unloaded
        AddHandler BtnCompDownload.Click, AddressOf BtnCompDownload_Click
        AddHandler BtnCreate.Click, AddressOf BtnCreate_Click
        AddHandler BtnStopHost.Click, AddressOf BtnStopHost_Click
        AddHandler BtnCopyCode.Click, AddressOf BtnCopyCode_Click
        AddHandler BtnJoin.Click, AddressOf BtnJoin_Click
        AddHandler BtnLeave.Click, AddressOf BtnLeave_Click
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Try

            ModLanDetect.StartWatch()
            InitDefaults()
            RefreshComponentState()

            _timer = New DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(1500)}
            AddHandler _timer.Tick, AddressOf Timer_Tick
            _timer.Start()
            AddLog("页面已就绪，正在监听局域网房间…")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 联机页加载失败")
        End Try
    End Sub

    Private Sub Page_Unloaded(sender As Object, e As RoutedEventArgs)
        Try
            If _timer IsNot Nothing Then
                _timer.Stop()
                _timer = Nothing
            End If
        Catch
        End Try
    End Sub

    Private Sub InitDefaults()
        Try
            If String.IsNullOrWhiteSpace(TxtRoomName.Text) Then
                Dim saved As String = Settings.Get(Of String)("LinkRoomName")
                TxtRoomName.Text = If(String.IsNullOrWhiteSpace(saved), Environment.UserName & "的世界", saved)
            End If
            TxtRoomPort.Text = "25565"
        Catch
        End Try
    End Sub

    Private Sub RefreshComponentState()
        Try
            If ModEasyTier.IsReady() Then
                LabCompStatus.Text = "联机组件：已就绪 ✓"
                LabCompTip.Text = "无需再下载，可直接创建 / 加入房间"
                BtnCompDownload.Visibility = Visibility.Collapsed
                BarComp.Visibility = Visibility.Collapsed
            Else
                LabCompStatus.Text = "联机组件：未安装"
                LabCompTip.Text = "首次使用需要下载联机组件（约 31 * 1024 * 1024，只需一次）"
                BtnCompDownload.Visibility = Visibility.Visible
            End If
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 刷新联机组件状态失败")
        End Try
    End Sub

    Private Sub BtnCompDownload_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            BtnCompDownload.Visibility = Visibility.Collapsed
            BarComp.Visibility = Visibility.Visible
            BarComp.Value = 0
            AddLog("开始下载联机组件…")
            ModEasyTier.EnsureAsync(
                Sub(pct As Double, msg As String)
                    BarComp.Value = Math.Max(0, Math.Min(1, pct))
                    LabCompTip.Text = msg
                End Sub,
                Sub(ok As Boolean, msg As String)
                    RefreshComponentState()
                    If ok Then
                        LabCompTip.Text = "联机组件已就绪 ✓"
                        AddLog("联机组件下载完成")
                    Else
                        LabCompTip.Text = "下载失败，可手动放置组件"
                        AddLog(msg)
                        BtnCompDownload.Visibility = Visibility.Visible
                    End If
                End Sub)
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 下载联机组件失败")
            AddLog("下载失败：" & ex.Message)
        End Try
    End Sub

    Private Sub Timer_Tick(sender As Object, e As EventArgs)
        Try
            RefreshLanRooms()
            RefreshRunState()
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 联机页定时刷新失败")
        End Try
    End Sub

    Private Sub RefreshLanRooms()
        Try
            Dim rooms As List(Of ModLanDetect.LanRoom) = ModLanDetect.GetRooms()
            If rooms.Count = 0 Then
                LabLanStatus.Text = "正在监听…（在游戏里点「对局域网开放」后会自动出现）"
                PanLanRooms.Children.Clear()
                Return
            End If
            LabLanStatus.Text = $"发现 {rooms.Count} 个局域网房间（点击可自动填入端口）："
            PanLanRooms.Children.Clear()
            Dim accent As Brush = TryCast(Application.Current.Resources("ColorBrush3"), Brush)
            For Each r As ModLanDetect.LanRoom In rooms
                Dim row As New Border With {
                    .CornerRadius = New CornerRadius(8),
                    .Padding = New Thickness(12, 8, 12, 8),
                    .Margin = New Thickness(0, 0, 0, 6),
                    .Cursor = Cursors.Hand,
                    .Tag = r.Port}
                row.SetResourceReference(Border.BackgroundProperty, "ColorBrushSurfaceSunken")
                Dim sp As New StackPanel With {.Orientation = Orientation.Horizontal}
                Dim t1 As New TextBlock With {
                    .Text = $"🌐 {r.WorldName}", .FontSize = 13.5, .FontWeight = FontWeights.SemiBold}
                t1.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushTextPrimary")
                Dim t2 As New TextBlock With {
                    .Text = $"   端口 {r.Port}   ·   {r.SourceIp}", .FontSize = 12.5}
                t2.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushTextTertiary")
                sp.Children.Add(t1)
                sp.Children.Add(t2)
                row.Child = sp
                AddHandler row.MouseLeftButtonUp,
                    Sub(s2 As Object, e2 As MouseButtonEventArgs)
                        Try
                            TxtRoomPort.Text = CStr(CType(s2, Border).Tag)
                            If String.IsNullOrWhiteSpace(TxtRoomName.Text) Then TxtRoomName.Text = r.WorldName
                            AddLog($"已选用房间「{r.WorldName}」，端口 {r.Port}")
                        Catch
                        End Try
                    End Sub
                PanLanRooms.Children.Add(row)
            Next
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 刷新局域网房间失败")
        End Try
    End Sub

    Private Sub RefreshRunState()
        Try
            If ModEasyTier.IsRunning Then
                If String.IsNullOrWhiteSpace(LabHostInfo.Text) OrElse LabHostInfo.Text.StartsWith("正在") Then
                    If Not String.IsNullOrWhiteSpace(ModEasyTier.LocalVirtualIp) Then
                        Dim port As String = If(String.IsNullOrWhiteSpace(TxtRoomPort.Text), "25565", TxtRoomPort.Text.Trim())
                        LabHostInfo.Text = $"联机 IP：{ModEasyTier.LocalVirtualIp}  游戏内「直接连接」填：{ModEasyTier.LocalVirtualIp}:{port}"
                    Else
                        LabHostInfo.Text = "正在组网，稍候会显示虚拟 IP…"
                    End If
                End If
            End If
        Catch
        End Try
    End Sub

    Private Sub BtnCreate_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            If Not ModEasyTier.IsReady() Then
                AddLog("请先下载联机组件")
                LabCompTip.Text = "请先点右侧「下载联机组件」"
                Return
            End If
            Dim room As String = If(TxtRoomName.Text, "").Trim()
            If room.Length = 0 Then
                AddLog("请先填写房间名")
                Return
            End If
            Dim pass As String = If(TxtRoomPass.Text, "").Trim()
            If pass.Length = 0 Then pass = "xfl"
            Settings.Set("LinkRoomName", room)

            Dim netName As String = "xfl-" & ModLinkCode.Normalize(room)
            AddLog($"正在创建房间「{room}」…")
            Dim ok As Boolean = ModEasyTier.StartNetwork(netName, pass)
            If Not ok Then
                AddLog("启动联机组件失败，详见下方日志")
                Return
            End If
            Dim shortCode As String = ModLinkCode.ShortCode(room, pass)
            Dim share As String = ModLinkCode.EncodeShare(room, pass)
            LabShortCode.Text = shortCode
            LabShareCode.Text = share
            PanMyCode.Visibility = Visibility.Visible
            LabHostInfo.Text = "正在组网，稍候会显示虚拟 IP…"
            AddLog($"房间已创建。短码 {shortCode}")
            AddLog("朋友可用「分享码」一键进房，也可手输房间名+密码。")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 创建房间失败")
            AddLog("创建失败：" & ex.Message)
        End Try
    End Sub

    Private Sub BtnStopHost_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            ModEasyTier.StopNetwork()
            PanMyCode.Visibility = Visibility.Collapsed
            LabHostInfo.Text = ""
            AddLog("房间已关闭")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 关闭房间失败")
        End Try
    End Sub

    Private Sub BtnCopyCode_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            Dim share As String = If(LabShareCode.Text, "").Trim()
            Dim shortCode As String = If(LabShortCode.Text, "------").Trim()
            Dim room As String = If(TxtRoomName.Text, "").Trim()
            Dim pass As String = If(TxtRoomPass.Text, "").Trim()
            If pass.Length = 0 Then pass = "xfl"
            If share.Length = 0 OrElse share = "—" Then
                AddLog("请先创建房间")
                Return
            End If
            Dim text As String = $"【XFL 联机】房间：{room}（密码：{pass}，短码：{shortCode}）" & vbCrLf &
                                 "把下面整段发给我，在我的启动器「联机」页粘贴即可进房：" & vbCrLf & share
            Clipboard.SetText(text)
            AddLog("已复制邀请信息（含房间名、密码、短码与分享码）")
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 复制邀请信息失败")
        End Try
    End Sub

    Private Sub BtnJoin_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            If Not ModEasyTier.IsReady() Then
                AddLog("请先下载联机组件")
                LabJoinStatus.Text = "请先下载联机组件"
                Return
            End If
            Dim code As String = If(TxtJoinCode.Text, "").Trim()
            If code.Length = 0 Then
                LabJoinStatus.Text = "请先粘贴联机码"
                Return
            End If
            Dim room As String = ""
            Dim pass As String = ""

            Dim shareMatch As Text.RegularExpressions.Match =
                System.Text.RegularExpressions.Regex.Match(code, "XFL2-[A-Za-z2-7\-]+")
            Dim toParse As String = If(shareMatch.Success, shareMatch.Value, code)
            If Not ModLinkCode.DecodeShare(toParse, room, pass) Then
                LabJoinStatus.Text = "联机码无法识别，请检查是否完整"
                Return
            End If
            If pass.Length = 0 Then pass = "xfl"
            Dim netName As String = "xfl-" & ModLinkCode.Normalize(room)
            LabJoinStatus.Text = $"正在加入房间「{room}」…"
            AddLog($"解析联机码成功：房间「{room}」，正在组网…")
            If ModEasyTier.StartNetwork(netName, If(String.IsNullOrWhiteSpace(pass), "xfl", pass)) Then
                LabJoinStatus.Text = $"已加入房间「{room}」。" & vbCrLf &
                                     "现在回到游戏，多人游戏 →「直接连接」，输入房主显示的联机 IP:端口 即可。" & vbCrLf &
                                     "（房主的虚拟 IP 会在对方的启动器上显示，让他发给你）"
                AddLog("加入成功，等待组网完成…")
            Else
                LabJoinStatus.Text = "组网启动失败，详见下方日志"
                AddLog("组网启动失败")
            End If
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 加入房间失败")
            LabJoinStatus.Text = "加入失败：" & ex.Message
        End Try
    End Sub

    Private Sub BtnLeave_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            ModEasyTier.StopNetwork()
            LabJoinStatus.Text = "已退出房间"
            AddLog("已退出房间")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 退出房间失败")
        End Try
    End Sub

    Private Sub AddLog(msg As String)
        Try
            _logLines.Add($"[{DateTime.Now:HH:mm:ss}] {msg}")
            If _logLines.Count > 40 Then _logLines.RemoveAt(0)
            LabLog.Text = String.Join(vbCrLf, _logLines.ToArray())
            Logger.Info("[XFL][联机] " & msg)
        Catch
        End Try
    End Sub

    Public Sub OnPageHide()
        Try
            If _timer IsNot Nothing Then _timer.Stop()
        Catch
        End Try
    End Sub

End Class