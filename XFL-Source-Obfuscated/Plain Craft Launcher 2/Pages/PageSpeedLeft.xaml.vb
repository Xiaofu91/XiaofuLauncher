Public Class PageSpeedLeft
    Private Const WatcherInterval As Integer = 300

    Private Const MaxTasksPerTick As Integer = 24

    Private Const MaxCards As Integer = 40

    Private _taskCursor As Integer = 0
    Private _isWatching As Boolean = False

    Private _idleSince As Long = 0

    Private _hadTasks As Boolean = False

    Private IsLoad As Boolean = False

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Try
            If BtnCancelAll IsNot Nothing Then
                RemoveHandler BtnCancelAll.MouseLeftButtonDown, AddressOf CancelAll_Click
                AddHandler BtnCancelAll.MouseLeftButtonDown, AddressOf CancelAll_Click
            End If
        Catch
        End Try

        Try
            _hadTasks = LoaderTaskbar.Any(Function(l) l IsNot Nothing AndAlso l.Show)
        Catch
            _hadTasks = False
        End Try

        Watcher()

        RunInNewThread(
            Sub()
                Threading.Thread.Sleep(1200)
                RunInUi(Sub()
                            Try
                                TryReturnToHome()
                            Catch
                            End Try
                        End Sub)
            End Sub)

        If IsLoad Then Return
        IsLoad = True

        Dim timer As New Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 0, WatcherInterval)}
        AddHandler timer.Tick, AddressOf Watcher
        timer.Start()

        If Not ModeDebug Then
            RowDefinitions(12).Height = New GridLength(0)
            RowDefinitions(13).Height = New GridLength(0)
            RowDefinitions(14).Height = New GridLength(0)
            RowDefinitions(15).Height = New GridLength(0)
        End If

    End Sub

    Private ReadOnly RightCards As New Dictionary(Of String, MyCard)
    Private Sub Watcher()
        If Not FrmMain.PageCurrent = FormMain.PageType.DownloadManager Then Return

        If _isWatching Then Return
        _isWatching = True
        Try

#Region "更新左边栏"
            If Not LoaderTaskbar.Any() Then

                LabProgress.Text = "100 %"
                LabSpeed.Text = "0 B/s"
                LabFile.Text = "0"
                LabThread.Text = "0 / " & NetTaskThreadLimit
            Else

                Dim Tasks = LoaderTaskbar.Where(Function(l) l.Show).ToList()
                Dim RawPercent As Double = If(Tasks.Any, Tasks.Select(Function(l) l.Progress).Average().Clamp(0, 1), 1)
                Dim PredictText As String = Math.Floor(RawPercent * 100) & "." & Math.Floor((RawPercent * 100 - Math.Floor(RawPercent * 100)) * 100).ToString.EnsureLength("0", 2) & " %"
                LabProgress.Text = If(RawPercent > 0.999999, "100 %", PredictText)
                LabSpeed.Text = StringUtils.FormatByteSize(NetManager.Speed) & "/s"
                LabFile.Text = If(NetManager.FileRemain < 0, "0*", NetManager.FileRemain)
                LabThread.Text = NetTaskThreadCount & " / " & NetTaskThreadLimit
            End If
#End Region

        Catch ex As Exception
            Logger.Error(ex, "下载管理左栏监视出错")
        End Try
        If FrmSpeedRight Is Nothing OrElse FrmSpeedRight.PanMain Is Nothing Then
            _isWatching = False
            Return
        End If
        Try

            Dim all As List(Of LoaderBase) = LoaderTaskbar.ToList

            If Not _hadTasks AndAlso all.Any(Function(l) l IsNot Nothing AndAlso l.Show) Then
                _hadTasks = True
                ShowEmptyHint(False)
            End If
            If all.Count = 0 Then
                _taskCursor = 0
            Else
                all = all.OrderByDescending(Function(l) If(l Is Nothing, False, l.Show)).ToList()
                If _taskCursor >= all.Count Then _taskCursor = 0
                Dim count As Integer = Math.Min(MaxTasksPerTick, all.Count)
                For i As Integer = 0 To count - 1
                    Dim idx As Integer = (_taskCursor + i) Mod all.Count
                    Try
                        TaskRefresh(all(idx))
                    Catch exOne As Exception
                        Logger.Warn(exOne, $"刷新单个下载任务失败（已跳过）：{all(idx).Name}")
                    End Try
                Next
                _taskCursor = (_taskCursor + count) Mod all.Count
            End If
        Catch ex As Exception
            Logger.Error(ex, "下载管理右栏监视出错")
        Finally
            _isWatching = False
        End Try

        Try
            TryReturnToHome()
        Catch
        End Try
    End Sub
    Public Sub TaskRefresh(Loader As LoaderBase)
        If Loader Is Nothing OrElse Not Loader.Show Then Return
        Try

            Dim LoaderList As List(Of LoaderBase) = CType(Loader, Object).GetLoaderList()
            If RightCards.ContainsKey(Loader.Name) Then

                Dim Card As Grid = RightCards(Loader.Name)
                Dim NewValue As Double = Loader.Progress + Loader.State
                If Val(Card.Tag) = NewValue Then Return
                Card.Tag = NewValue
                If Card.Children.Count <= 3 Then
                    Logger.Warn($"元素不足的卡片：{Loader.Name}")
                    Return
                End If
                Card = Card.Children(3)
                Try
                    Select Case Loader.State
                        Case LoadState.Failed
#Region "失败，更新卡片"
                            Card.RowDefinitions.Clear()
                            Card.Children.Clear()
                            Card.Children.Add(GetObjectFromXML("<Path xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" Stretch=""Uniform"" Tag=""Failed"" Data=""F1 M2.5,0 L0,2.5 7.5,10 0,17.5 2.5,20 10,12.5 17.5,20 20,17.5 12.5,10 20,2.5 17.5,0 10,7.5 2.5,0Z"" Height=""15"" Width=""15"" HorizontalAlignment=""Center"" Grid.Column=""0"" Grid.Row=""0"" Fill=""{DynamicResource ColorBrush3}"" Margin=""0,1,0,0"" VerticalAlignment=""Top""/>"))
                            Dim Tb As TextBlock = GetObjectFromXML("<TextBlock xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" TextWrapping=""Wrap"" HorizontalAlignment=""Left"" ToolTip=""{StaticResource LangSpeedClickCopyError}"" Grid.Column=""1"" Grid.Row=""0"" Margin=""0,0,0,5"" />")
                            Tb.Text = Loader.Error.GetDisplay(True)
                            AddHandler Tb.MouseLeftButtonDown,
                            Sub(sender As TextBlock, e As EventArgs)
                                ClipboardSet(sender.Text, False)
                                Hint(GetLang("LangSpeedHintClickCopyError"), HintType.Green)
                            End Sub
                            Card.Children.Add(Tb)
#End Region
                        Case LoadState.Finished, LoadState.Canceled
#Region "完成或中断，销毁卡片并返回"
                            AniDispose(CType(Card.Parent, MyCard), True, AddressOf TryReturnToHome)
#End Region
                        Case LoadState.Loading, LoadState.Waiting
#Region "进度不同，更新卡片"
                            Try
                                If Card.Children.Count < LoaderList.Count * 2 Then
                                    Logger.Warn($"刷新下载管理卡片 {Loader.Name} 失败：卡片中仅有 {Card.Children.Count} 个子项，要求至少有 {LoaderList.Count * 2} 个子项")
                                    Exit Try
                                End If
                                Dim Row As Integer = 0
                                For Each SubTask In LoaderList
                                    Select Case SubTask.State
                                        Case LoadState.Waiting
                                            If CType(Card.Children(Row * 2), FrameworkElement).Tag <> "Waiting" Then
                                                Card.Children.RemoveAt(Row * 2)
                                                Card.Children.Insert(Row * 2, GetObjectFromXML("<Path xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:local=""clr-namespace:PCL;assembly=Xiaofu Launcher"" Stretch=""Uniform"" Tag=""Waiting"" Data=""F1 M5,0 a5,5 360 1 0 0,0.0001 m15,0 a5,5 360 1 0 0,0.0001 m15,0 a5,5 360 1 0 0,0.0001 Z"" Width=""18"" HorizontalAlignment=""Center"" Grid.Column=""0"" Grid.Row=""" & Row & """ Fill=""{DynamicResource ColorBrush3}"" Margin=""0,7,0,0"" VerticalAlignment=""Top"" Height=""6""/>"))
                                            End If
                                        Case LoadState.Loading
                                            If CType(Card.Children(Row * 2), FrameworkElement).Tag <> "Loading" Then
                                                Card.Children.RemoveAt(Row * 2)
                                                Card.Children.Insert(Row * 2, GetObjectFromXML("<TextBlock xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:local=""clr-namespace:PCL;assembly=Xiaofu Launcher"" Text=""" & Math.Floor(SubTask.Progress * 100) & "%"" Tag=""Loading"" HorizontalAlignment=""Center"" Grid.Column=""0"" Grid.Row=""" & Row & """ Foreground=""{DynamicResource ColorBrush3}""/>"))
                                            Else
                                                CType(Card.Children(Row * 2), TextBlock).Text = Math.Floor(SubTask.Progress * 100) & "%"
                                            End If
                                        Case LoadState.Finished
                                            If CType(Card.Children(Row * 2), FrameworkElement).Tag <> "Finished" Then
                                                Card.Children.RemoveAt(Row * 2)
                                                Card.Children.Insert(Row * 2, GetObjectFromXML("<Path xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:local=""clr-namespace:PCL;assembly=Xiaofu Launcher"" Stretch=""Uniform"" Tag=""Finished"" Data=""F1 M 23.7501,33.25L 34.8334,44.3333L 52.2499,22.1668L 56.9999,26.9168L 34.8334,53.8333L 19.0001,38L 23.7501,33.25 Z"" Height=""16"" Width=""15"" HorizontalAlignment=""Center"" Grid.Column=""0"" Grid.Row=""" & Row & """ Fill=""{DynamicResource ColorBrush3}"" Margin=""0,3,0,0"" VerticalAlignment=""Top""/>"))
                                            End If
                                    End Select
                                    Row += 1
                                Next
                            Catch ex As Exception
                                Logger.Error(ex, $"刷新下载管理卡片 {Loader.Name} 失败")
                            End Try
#End Region
                    End Select
                Catch ex As Exception
                    Logger.Error(ex, $"更新下载管理显示失败（{Loader.State}）")
                End Try
            ElseIf Not (Loader.State = LoadState.Canceled OrElse Loader.State = LoadState.Finished) Then

                If RightCards.Count >= MaxCards Then Return
                Try
#Region "没有卡片且未中断或完成，添加新的卡片"
                    Dim CardXAML As String = "
                        <local:MyCard xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:local=""clr-namespace:PCL;assembly=Xiaofu Launcher""
                            Tag=""" & (Loader.Progress + Loader.State) & """ Title=""" & StringUtils.XmlEscape(Loader.Name) & """ Margin=""0,0,0,15"">
                            <Grid Margin=""14,40,15,10"">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width=""50""/>
                                    <ColumnDefinition/>
                                </Grid.ColumnDefinitions>
                                <Grid.RowDefinitions>"
                    For Each SubTask In LoaderList
                        CardXAML += "<RowDefinition Height=""26""/>"
                    Next
                    CardXAML += "</Grid.RowDefinitions>"
                    Dim Row As Integer = 0
                    For Each SubTask In LoaderList
                        Select Case SubTask.State
                            Case LoadState.Waiting
                                CardXAML += "<Path Stretch=""Uniform"" Tag=""Waiting"" Data=""F1 M5,0 a5,5 360 1 0 0,0.0001 m15,0 a5,5 360 1 0 0,0.0001 m15,0 a5,5 360 1 0 0,0.0001 Z"" Width=""18"" HorizontalAlignment=""Center"" Grid.Column=""0"" Grid.Row=""" & Row & """ Fill=""{DynamicResource ColorBrush3}"" Margin=""0,7,0,0"" VerticalAlignment=""Top"" Height=""6""/>"
                            Case LoadState.Loading
                                CardXAML += "<TextBlock Text=""" & Math.Floor(SubTask.Progress * 100) & "%"" Tag=""Loading"" HorizontalAlignment=""Center"" Grid.Column=""0"" Grid.Row=""" & Row & """ Foreground=""{DynamicResource ColorBrush3}"" />"
                            Case LoadState.Finished
                                CardXAML += "<Path Stretch=""Uniform"" Tag=""Finished"" Data=""F1 M 23.7501,33.25L 34.8334,44.3333L 52.2499,22.1668L 56.9999,26.9168L 34.8334,53.8333L 19.0001,38L 23.7501,33.25 Z"" Height=""16"" Width=""15"" HorizontalAlignment=""Center"" Grid.Column=""0"" Grid.Row=""" & Row & """ Fill=""{DynamicResource ColorBrush3}"" Margin=""0,3,0,0"" VerticalAlignment=""Top""/>"
                            Case Else
                                CardXAML += "<Path Stretch=""Uniform"" Tag=""Failed"" Data=""F1 M2.5,0 L0,2.5 7.5,10 0,17.5 2.5,20 10,12.5 17.5,20 20,17.5 12.5,10 20,2.5 17.5,0 10,7.5 2.5,0Z"" Height=""15"" Width=""15"" HorizontalAlignment=""Center"" Grid.Column=""0"" Grid.Row=""" & Row & """ Fill=""{DynamicResource ColorBrush3}"" Margin=""0,1,0,0"" VerticalAlignment=""Top""/>"
                        End Select
                        CardXAML += "<TextBlock Text=""" & StringUtils.XmlEscape(SubTask.Name) & """ HorizontalAlignment=""Left"" Grid.Column=""1"" Grid.Row=""" & Row & """/>"
                        Row += 1
                    Next
                    CardXAML += "</Grid></local:MyCard>"

                    Dim Card As MyCard
                    Try
                        Card = GetObjectFromXML(CardXAML)
                    Catch ex As Exception

                        Logger.Warn(ex, "新建下载管理卡片失败（已跳过）")
                        Logger.Info($"出错的卡片内容：{vbCrLf}{StringUtils.XmlEscape(CardXAML)}")
                        Return
                    End Try
                    FrmSpeedRight.PanMain.Children.Insert(0, Card)
                    RightCards.Add(Loader.Name, Card)
                    Logger.Info($"新建下载管理卡片：{Loader.Name}")

                    Dim Cancel As New MyIconButton With {.Name = "BtnCancel", .Logo = "F1 M2,0 L0,2 8,10 0,18 2,20 10,12 18,20 20,18 12,10 20,2 18,0 10,8 2,0Z", .Height = 24, .Margin = New Thickness(0, 7, 7, 0), .LogoScale = 1.25, .HorizontalAlignment = HorizontalAlignment.Right, .VerticalAlignment = VerticalAlignment.Top, .Theme = MyIconButton.Themes.Red, .ToolTip = "取消此任务"}
                    Card.Children.Add(Cancel)
                    AddHandler Cancel.Click,
                    Sub(sender As MyIconButton, e As EventArgs)
                        AniDispose(sender, False)
                        AniDispose(Card, True, Sub() If FrmSpeedRight.PanMain.Children.Count = 0 AndAlso FrmMain.PageCurrent = FormMain.PageType.DownloadManager Then FrmMain.PageBack())
                        RightCards.Remove(Loader.Name)
                        LoaderTaskbar.Remove(Loader)
                        Logger.Info($"关闭下载管理卡片：{Loader.Name}，且移出任务列表")
                        RunInThread(Sub() Loader.Cancel())
                    End Sub

                    If Loader.State = LoadState.Failed Then
                        Card.Tag = Nothing
                        TaskRefresh(Loader)
                    End If
#End Region
                Catch ex As Exception
                    Logger.Error(ex, "添加下载管理卡片失败")
                End Try
            End If
        Catch ex As Exception
            Logger.Error(ex, "刷新下载管理显示失败")
        End Try
    End Sub

    Friend Sub CancelAll_Click(sender As Object, e As MouseButtonEventArgs)
        Try
            Dim all As List(Of LoaderBase) = LoaderTaskbar.ToList()
            If all.Count = 0 Then
                Hint("当前没有进行中的下载任务", HintType.Blue)
                Return
            End If
            Dim n As Integer = 0
            For Each l As LoaderBase In all
                Try
                    l.Show = False
                    RunInThread(Sub() l.Cancel())
                    n += 1
                Catch exOne As Exception
                    Logger.Warn(exOne, "[XFL] 取消单个任务失败")
                End Try
            Next
            Try
                For Each k As String In RightCards.Keys.ToList()
                    Try
                        FrmSpeedRight.PanMain.Children.Remove(RightCards(k))
                    Catch
                    End Try
                Next
                RightCards.Clear()
            Catch
            End Try
            Try
                LoaderTaskbar.Clear()
            Catch
            End Try
            Hint($"已取消 {n} 个下载任务", HintType.Blue)
            Logger.Info($"[XFL] 全部取消：{n} 个任务")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 全部取消失败")
        End Try
    End Sub

    Public Sub TaskRemove(Loader As Object)
        If RightCards.ContainsKey(Loader.Name) Then
            RunInUiWait(
            Sub()

                Dim Card As Grid = RightCards(Loader.Name)
                FrmSpeedRight.PanMain.Children.Remove(Card)
                RightCards.Remove(Loader.Name)
                Logger.Info($"移除下载管理卡片：{Loader.Name}")
            End Sub)
        End If
    End Sub

    Private Sub TryReturnToHome()

        If FrmSpeedRight Is Nothing OrElse FrmSpeedRight.PanMain Is Nothing Then Return
        If Not FrmMain.PageCurrent = FormMain.PageType.DownloadManager Then Return
        If Not _hadTasks Then
            ShowEmptyHint(True)
            Return
        End If
        If FrmSpeedRight.PanMain.Children.Count > 0 Then
            ShowEmptyHint(False)
            Return
        End If
        Dim hasVisible As Boolean = LoaderTaskbar.Any(Function(l) l IsNot Nothing AndAlso l.Show)
        If hasVisible Then
            ShowEmptyHint(False)
            _idleSince = 0
            Return
        End If

        Dim nowTick As Long = GetTimeMs()
        If _idleSince = 0 Then
            _idleSince = nowTick
            ShowEmptyHint(True)
            Return
        End If
        If nowTick - _idleSince < 1500 Then Return
        Logger.Info("[XFL] 下载全部完成，自动关闭下载管理页")
        _idleSince = 0
        FrmMain.PageBack()
    End Sub

    Private _emptyHint As Border = Nothing
    Private Sub ShowEmptyHint(show As Boolean)
        Try
            If FrmSpeedRight Is Nothing OrElse FrmSpeedRight.PanMain Is Nothing Then Return
            If Not show Then
                If _emptyHint IsNot Nothing Then
                    FrmSpeedRight.PanMain.Children.Remove(_emptyHint)
                    _emptyHint = Nothing
                End If
                Return
            End If
            If _emptyHint IsNot Nothing Then Return
            Dim box As New Border With {
                .CornerRadius = New CornerRadius(12),
                .Padding = New Thickness(28, 34, 28, 34),
                .Margin = New Thickness(0, 20, 0, 0),
                .HorizontalAlignment = HorizontalAlignment.Stretch}
            box.SetResourceReference(Border.BackgroundProperty, "ColorBrushSurfaceCard")
            box.SetResourceReference(Border.BorderBrushProperty, "ColorBrushBorderNormal")
            box.BorderThickness = New Thickness(1)
            Dim sp As New StackPanel With {.HorizontalAlignment = HorizontalAlignment.Center}
            Dim icon As New TextBlock With {.Text = "⬇", .FontSize = 34, .HorizontalAlignment = HorizontalAlignment.Center}
            icon.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush3")
            Dim t1 As New TextBlock With {
                .Text = "当前没有下载任务", .FontSize = 15, .FontWeight = FontWeights.SemiBold,
                .HorizontalAlignment = HorizontalAlignment.Center, .Margin = New Thickness(0, 12, 0, 0)}
            t1.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushTextPrimary")
            Dim t2 As New TextBlock With {
                .Text = "去「下载」页选择游戏或整合包，开始下载后这里会显示进度。",
                .FontSize = 12.5, .HorizontalAlignment = HorizontalAlignment.Center,
                .Margin = New Thickness(0, 8, 0, 0), .TextWrapping = TextWrapping.Wrap}
            t2.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushTextTertiary")
            sp.Children.Add(icon)
            sp.Children.Add(t1)
            sp.Children.Add(t2)
            box.Child = sp
            FrmSpeedRight.PanMain.Children.Insert(0, box)
            _emptyHint = box
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 显示下载空状态失败")
        End Try
    End Sub

End Class
