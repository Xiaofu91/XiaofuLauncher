Imports System.ComponentModel
Imports System.Windows.Interop

Public Class FormMain

#Region "基础"

    Private Sub ShowUpdateLog(LastVersion As Integer)
        Dim FeatureCount As Integer = 0, BugCount As Integer = 0
        Dim FeatureList As New List(Of KeyValuePair(Of Integer, String))

        If BuildType = BuildTypes.Release Then
            If LastVersion < 406 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "新增：重做 Java 管理与相关设置，允许调整 Java 优先级、指定 Java 版本范围等"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "优化：导出整合包时允许自动导出版本文件夹中的 Java"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "优化：重做弹出提示的样式以及动画，以更符合现代 UI 审美"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：可能无法安装 OptiFine 26.1.2"))
                FeatureCount += 35
                BugCount += 17
            End If
            If LastVersion < 404 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：Windows 7 无法正常联网"))
                FeatureCount += 1
                BugCount += 2
            End If
            If LastVersion < 402 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：升级框架到 .NET Framework 4.8"))
                FeatureCount += 35
                BugCount += 21
            End If
            If LastVersion < 398 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：下载游戏、整合包的稳定性"))
                FeatureCount += 1
                BugCount += 5
            End If
            If LastVersion < 391 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：会为部分 Forge / NeoForge 选择不支持的 Java"))
                BugCount += 4
            End If
            If LastVersion < 389 Then
                If LastVersion = 387 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：无法使用 Java 25+ 启动 Forge"))
                FeatureCount += 4
                BugCount += 3
            End If
            If LastVersion < 387 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "优化：降低 Minecraft 的内存占用"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：Minecraft 使用 Java 25+ 时的一个性能问题"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：无法下载 NeoForge 26.1"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：部分关键词搜不到资源，特别是中文 Mod 搜索经常没有结果"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：老版本 Windows 无法启动游戏"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：连接 Mojang 的服务可能失败，提示错误码 421"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：无法访问爱发电"))
                FeatureCount += 24
                BugCount += 19
            End If
            If LastVersion < 383 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：部分关键词搜不到资源，特别是中文 Mod 搜索经常没有结果"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：下载可能完全卡住，或是下载进度反复回退"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "新增：内存管理设置，可以选择 G1GC、ZGC 或分代 ZGC"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "优化：来自 Modrinth 的文件下载速度"))
                FeatureCount += 15
                BugCount += 16
            End If
            If LastVersion < 381 Then
                FeatureCount += 3
                BugCount += 13
            End If
            If LastVersion < 379 Then
                If LastVersion >= 376 Then
                    FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "删除：暂时隐藏联机入口……不过只是暂时关闭，它还会回来的！"))
                End If
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "新增：适配新的 Minecraft 版本号系统与 Unobfuscated 版本"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：巨幅优化各个下载页面和 Mod 管理页面的性能"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "优化：更换披风时会显示当前使用的披风"))
                FeatureCount += 33
                BugCount += 29
            End If
            If LastVersion < 376 Then

                FeatureCount += 32
                BugCount += 21
            End If
        Else

            If LastVersion < 407 Then
                If LastVersion = 405 Then
                    FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：使用部分主页预设时崩溃"))
                    FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：无法删除部分文件夹"))
                    FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：版本设置中指定的 Java 无法被保存"))
                    FeatureCount += 1
                    BugCount += 3
                End If
                FeatureCount += 2
            End If
            If LastVersion < 405 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "新增：重做 Java 管理与相关设置，允许调整 Java 优先级、指定 Java 版本范围等"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "优化：导出整合包时允许自动导出版本文件夹中的 Java"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "优化：重做弹出提示的样式以及动画，以更符合现代 UI 审美"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：可能无法安装 OptiFine 26.1.2"))
                FeatureCount += 35
                BugCount += 17
            End If
            If LastVersion < 403 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：Windows 7 无法正常联网"))
                FeatureCount += 1
                BugCount += 2
            End If
            If LastVersion < 401 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：可能无法更换披风"))
                FeatureCount += 8
                BugCount += 6
            End If
            If LastVersion < 399 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：升级框架到 .NET Framework 4.8"))
                FeatureCount += 28
                BugCount += 19
            End If
            If LastVersion < 397 Then
                If LastVersion >= 393 AndAlso LastVersion < 397 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：无法使用自定义隐藏主题"))
            End If
            If LastVersion < 395 Then
                If LastVersion = 393 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：部分个性化功能失效"))
            End If
            If LastVersion < 393 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：允许永久关闭启动页面的快照版提示"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：下载游戏、整合包的稳定性"))
                FeatureCount += 1
                BugCount += 5
            End If
            If LastVersion < 390 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：会为部分 Forge / NeoForge 选择不支持的 Java"))
                BugCount += 4
            End If
            If LastVersion < 388 Then
                If LastVersion = 386 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：无法使用 Java 25+ 启动 Forge"))
                FeatureCount += 4
                BugCount += 3
            End If
            If LastVersion < 386 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：Minecraft 使用 Java 25+ 时的一个性能问题"))
                If LastVersion = 384 OrElse LastVersion = 385 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：无法正常重命名游戏版本"))
                If LastVersion = 385 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：无法显示 Modrinth 整合包的版本列表"))
                BugCount += 1
            End If
            If LastVersion < 385 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：无法下载 NeoForge 26.1"))
                If LastVersion = 384 Then
                    FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：下载可能失败，提示下载管理刷新线程出错"))
                    FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：无法保存选择的 Minecraft 文件夹"))
                    FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：使用中文搜索 Mod 时，部分结果会忽略筛选条件"))
                End If
                FeatureCount += 3
                BugCount += 6
            End If
            If LastVersion < 384 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "优化：降低 Minecraft 的内存占用"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：部分关键词搜不到资源，特别是中文 Mod 搜索经常没有结果"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：老版本 Windows 无法启动游戏"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：连接 Mojang 的服务可能失败，提示错误码 421"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：无法访问爱发电"))
                FeatureCount += 21
                BugCount += 14
            End If
            If LastVersion < 382 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：下载可能完全卡住，或是下载进度反复回退"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "新增：内存管理设置，可以选择 G1GC、ZGC 或分代 ZGC"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "优化：来自 Modrinth 的文件下载速度"))
                FeatureCount += 15
                BugCount += 16
            End If
            If LastVersion < 380 Then
                FeatureCount += 3
                BugCount += 13
            End If
            If LastVersion < 378 Then
                If LastVersion >= 377 Then
                    FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：在版本列表中存在 OptiFine 时可能无法加载版本列表"))
                    BugCount += 2
                End If
            End If
            If LastVersion < 377 Then
                If LastVersion >= 373 Then
                    FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "删除：暂时隐藏联机入口……不过只是暂时关闭，它还会回来的！"))
                End If
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "新增：适配新的 Minecraft 版本号系统与 Unobfuscated 版本"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：巨幅优化各个下载页面和 Mod 管理页面的性能"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "优化：更换披风时会显示当前使用的披风"))
                FeatureCount += 33
                BugCount += 29
            End If
            If LastVersion < 375 Then

                FeatureCount += 16
                BugCount += 4
            End If
            If LastVersion < 374 Then

                FeatureCount += 9
                BugCount += 7
            End If
            If LastVersion < 373 Then

                FeatureCount += 7
                BugCount += 10
            End If
        End If

        Dim ContentList As New List(Of String)
        Dim SortedFeatures = FeatureList.OrderByDescending(Function(f) f.Key).ToList
        If Not SortedFeatures.Any() AndAlso FeatureCount = 0 AndAlso BugCount = 0 Then ContentList.Add("没有更新日志……")
        For i = 0 To Math.Min(9, SortedFeatures.Count - 1)
            ContentList.Add(SortedFeatures(i).Value)
        Next
        If SortedFeatures.Count > 10 Then FeatureCount += SortedFeatures.Count - 10
        If FeatureCount > 0 OrElse BugCount > 0 Then
            ContentList.Add(If(FeatureCount > 0, FeatureCount & " 项小调整与修改", "") &
                If(FeatureCount > 0 AndAlso BugCount > 0, GetLang("LangComma"), "") &
                If(BugCount > 0, "修复了 " & BugCount & " 个 Bug", "") &
                "，详见完整更新日志")
        End If
        Dim Content As String = "· " & ContentList.Join(vbCrLf & "· ")

        RunInNewThread(
        Sub()
            If MyMsgBox(Content, GetLang("LangDialogUpdateTo", VersionBaseName), GetLang("LangDialogBtnOK"), GetLang("LangDialogBtnUpdateFullLog")) = 2 Then
                OpenWebsite("https://meloong.com/afd/a/LTCat?tab=feed")
            End If
        End Sub, "UpdateLog Output")
    End Sub

    Private IsWindowLoadFinished As Boolean = False
    Public Sub New()
        ApplicationStartTick = GetTimeMs()

        FrmMain = Me
        FrmLaunchLeft = New PageLaunchLeft
        FrmLaunchRight = New PageLaunchRight

        Dim LastVersion As Integer = Settings.Get(Of Integer)("SystemLastVersionReg")
        If LastVersion < VersionCode Then

            UpgradeSub(LastVersion)
        ElseIf LastVersion > VersionCode Then

            DowngradeSub(LastVersion)
        End If

        If Not Settings.HasSaved("LaunchArgumentIndieV2") Then
            If Settings.HasSaved("LaunchArgumentIndie") Then
                Logger.Info("从老 XFL 迁移版本隔离")
                Settings.Set("LaunchArgumentIndieV2", Settings.Get(Of Integer)("LaunchArgumentIndie"))
            ElseIf HasIniKey("Setup", "LaunchVersionSelect") Then
                Logger.Info("从老 XFL 升级，但此前未调整版本隔离，使用老的版本隔离默认值")
                Settings.Set("LaunchArgumentIndieV2", Settings.GetDefault("LaunchArgumentIndie"))
            Else
                Logger.Info("全新的 XFL，使用新的版本隔离默认值")
                Settings.Set("LaunchArgumentIndieV2", Settings.GetDefault("LaunchArgumentIndieV2"))
            End If
        End If

        ThemeCheckAll(False)
        ThemeRefresh(Settings.Get(Of Integer)("UiLauncherTheme"))

        [AddHandler](DragDrop.DragEnterEvent, New DragEventHandler(AddressOf HandleDrag), handledEventsToo:=True)
        [AddHandler](DragDrop.DragOverEvent, New DragEventHandler(AddressOf HandleDrag), handledEventsToo:=True)

        InitializeComponent()
        Opacity = 0

        If WindowsUtils.HasAdminRole() Then
            Static Helper As New DragHelper
            AddHandler SourceInitialized,
            Sub()
                Dim WpfHelper As New WindowInteropHelper(Me)
                Helper.HwndIntPtrSource = HwndSource.FromHwnd(WpfHelper.Handle)
                Helper.AddHook()
            End Sub
            AddHandler Closing, Sub() Helper.RemoveDragHook()
            AddHandler Helper.DragDrop, Sub() FileDrag(Helper.DropFilePaths)
        End If

        If Not IsNothing(FrmLaunchLeft.Parent) Then FrmLaunchLeft.SetValue(ContentPresenter.ContentProperty, Nothing)
        If Not IsNothing(FrmLaunchRight.Parent) Then FrmLaunchRight.SetValue(ContentPresenter.ContentProperty, Nothing)
        PanMainLeft.Child = FrmLaunchLeft
        PageLeft = FrmLaunchLeft
        PanMainRight.Child = FrmLaunchRight
        PageRight = FrmLaunchRight
        FrmLaunchRight.PageState = MyPageRight.PageStates.ContentStay

        If BuildType = BuildTypes.Debug Then Hint(GetLang("LangHintDebugDeveloperWarning"), HintType.Red)
        If ModeDebug Then Hint(GetLang("LangHintDebugWarning"))

        McFolderListLoader.Start(0)

        Logger.Info($"第二阶段加载用时：{GetTimeMs() - ApplicationStartTick} ms")
    End Sub

    Private Shared _xflQuoteIndex As Integer = -1
    Private Shared ReadOnly _xflQuoteLock As New Object()
    Private Sub ApplyRandomQuote()
        Try
            If LabXflQuote Is Nothing Then Return
            SyncLock _xflQuoteLock
                If _xflQuoteIndex < 0 Then
                    Dim rnd As New Random()
                    _xflQuoteIndex = rnd.Next(0, XflQuotes.Quotes.Length)
                End If
            End SyncLock
            ShowQuote(XflQuotes.Quotes(_xflQuoteIndex), "启动随机")
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 设置顶栏名句失败")
        End Try
    End Sub

    Private Sub ShowQuote(raw As String, source As String)
        Try
            If LabXflQuote Is Nothing Then Return
            Dim cut As Integer = raw.LastIndexOf("——")
            LabXflQuote.Inlines.Clear()
            If cut > 0 Then
                LabXflQuote.Inlines.Add(New System.Windows.Documents.Run(raw.Substring(0, cut)))
                Dim runSrc As New System.Windows.Documents.Run(" " & raw.Substring(cut)) With {
                    .FontSize = 11.5,
                    .Foreground = New SolidColorBrush(Color.FromArgb(205, 255, 255, 255))}
                LabXflQuote.Inlines.Add(runSrc)
            Else
                LabXflQuote.Inlines.Add(New System.Windows.Documents.Run(raw))
            End If
            Logger.Info($"[XFL] 顶栏名句（{source}，第 {_xflQuoteIndex + 1}/{XflQuotes.Quotes.Length} 条）：{raw}")

            Try
                If source = "启动随机" Then
                    Dim mode As String = Environment.GetEnvironmentVariable("XFL_TEST_QUOTE")
                    If Not String.IsNullOrWhiteSpace(mode) Then
                        Dim tmrH As New System.Windows.Threading.DispatcherTimer() With {.Interval = TimeSpan.FromSeconds(6)}
                        AddHandler tmrH.Tick,
                            Sub()
                                tmrH.Stop()
                                Logger.Info($"[XFL][名句] 测试钩子触发：{mode}")
                                If mode = "next" Then
                                    ShowNextQuote()
                                ElseIf mode = "copy" Then
                                    CopyQuote()
                                End If
                            End Sub
                        tmrH.Start()
                    End If
                End If
            Catch
            End Try

            Try
                Dim tmrD As New System.Windows.Threading.DispatcherTimer() With {.Interval = TimeSpan.FromSeconds(3)}
                AddHandler tmrD.Tick,
                    Sub()
                        tmrD.Stop()
                        If source <> "启动随机" Then Return
                        Try
                            If Not Me.IsLoaded OrElse PanXflQuoteZone Is Nothing OrElse PanXflQuoteZone.ActualWidth < 1 Then Return
                            Dim pp As Point = PanXflQuoteZone.TransformToAncestor(Me).Transform(New Point(0, 0))
                            Logger.Info($"[XFL][名句] 命中区（动画后）x={pp.X:F0} y={pp.Y:F0} 宽={PanXflQuoteZone.ActualWidth:F0} 高={PanXflQuoteZone.ActualHeight:F0} 中心=({pp.X + PanXflQuoteZone.ActualWidth / 2:F0},{pp.Y + PanXflQuoteZone.ActualHeight / 2:F0})")
                            Dim dcCenter = PanXflQuoteZone.PointToScreen(New Point(PanXflQuoteZone.ActualWidth / 2, PanXflQuoteZone.ActualHeight / 2))
                            Logger.Info($"[XFL][名句] 命中区设备像素中心=({dcCenter.X:F0},{dcCenter.Y:F0})")
                        Catch
                        End Try
                    End Sub
                tmrD.Start()
            Catch exD As Exception
                Logger.Warn(exD, "[XFL][名句] 命中区定位失败")
            End Try
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 显示顶栏名句失败")
        End Try
    End Sub

    Private Sub ShowNextQuote()
        Try
            SyncLock _xflQuoteLock
                If XflQuotes.Quotes.Length < 2 Then Return
                Dim rnd As New Random()
                Dim idx As Integer = _xflQuoteIndex
                Do
                    idx = rnd.Next(0, XflQuotes.Quotes.Length)
                Loop While idx = _xflQuoteIndex
                _xflQuoteIndex = idx
            End SyncLock
            ShowQuote(XflQuotes.Quotes(_xflQuoteIndex), "单击切换")
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 切换顶栏名句失败")
        End Try
    End Sub

    Private _quoteClickTimer As System.Windows.Threading.DispatcherTimer = Nothing
    Private _quoteLastClickTick As Long = 0
    Private Sub LabXflQuote_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles LabXflQuote.MouseLeftButtonDown
        Try
            e.Handled = True
            Dim nowTick As Long = GetTimeMs()

            If _quoteLastClickTick > 0 AndAlso nowTick - _quoteLastClickTick < 300 Then
                If _quoteClickTimer IsNot Nothing Then _quoteClickTimer.Stop()
                _quoteLastClickTick = 0
                CopyQuote()
                Return
            End If
            Dim startPos As System.Drawing.Point = System.Windows.Forms.Cursor.Position

            Try
                DragMove()
            Catch
            End Try

            Dim endPos As System.Drawing.Point = System.Windows.Forms.Cursor.Position
            If Math.Abs(endPos.X - startPos.X) > 25 OrElse Math.Abs(endPos.Y - startPos.Y) > 25 Then Return

            _quoteLastClickTick = GetTimeMs()

            If _quoteClickTimer Is Nothing Then
                _quoteClickTimer = New System.Windows.Threading.DispatcherTimer() With {.Interval = TimeSpan.FromMilliseconds(250)}
                AddHandler _quoteClickTimer.Tick,
                    Sub()
                        _quoteClickTimer.Stop()
                        ShowNextQuote()
                        _quoteLastClickTick = 0
                    End Sub
            Else
                _quoteClickTimer.Stop()
            End If
            _quoteClickTimer.Start()
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 顶栏名句点击失败")
        End Try
    End Sub

    Private Sub CopyQuote()
        Try
            Dim raw As String = XflQuotes.Quotes(_xflQuoteIndex)
            System.Windows.Clipboard.SetText(raw)
            Hint("已复制名句到剪贴板：" & raw, HintType.Green)
            Logger.Info("[XFL] 顶栏名句已复制：" & raw)
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 复制顶栏名句失败")
            Hint("复制失败：" & ex.Message, HintType.Red)
        End Try
    End Sub

    Private Sub FormMain_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ApplicationStartTick = GetTimeMs()
        Handle = New WindowInteropHelper(Me).Handle

        UpdateBackgroundAndTitleBar()

        Try
            Dim testMode As String = Environment.GetEnvironmentVariable("XFL_TEST_AEXEC")
            If Not String.IsNullOrWhiteSpace(testMode) Then
                Dim tmrA As New System.Windows.Threading.DispatcherTimer() With {.Interval = TimeSpan.FromSeconds(5)}
                AddHandler tmrA.Tick,
                    Sub()
                        tmrA.Stop()
                        RunInNewThread(
                        Sub()
                            Try
                                Logger.Info("[XFL][测试] AEXEC 模式=" & testMode)
                                If testMode = "ver" Then
                                    Dim rel As List(Of String) = ModXflAiExec.AvailableVersions()
                                    Logger.Info("[XFL][测试] 清单条数=" & rel.Count & "，前6个=" & String.Join(",", rel.Take(6)))
                                    Logger.Info("[XFL][测试] IsKnown(26.3)=" & ModXflAiExec.IsKnownVersion("26.3") & "，IsKnown(99.99)=" & ModXflAiExec.IsKnownVersion("99.99"))
                                    Dim hint As String = ModXflAiExec.VersionsHint()
                                    Logger.Info("[XFL][测试] 注入提示长度=" & hint.Length & "，含26.3=" & hint.Contains("26.3"))
                                ElseIf testMode = "mismatch" Then
                                    Dim acts As New List(Of JObject)
                                    acts.Add(New JObject From {{"act", "download_version"}, {"version", "1.20.6"}})
                                    Dim w1 As String = ModXflAiExec.VersionMismatchWarning("帮我下载 26.3", acts)
                                    Logger.Info("[XFL][测试] 说26.3→要1.20.6：警告=" & If(w1 = "", "无", "有"))
                                    Logger.Info("[XFL][测试]   内容=" & If(w1.Length > 150, w1.Substring(0, 150), w1))
                                    acts.Clear()
                                    acts.Add(New JObject From {{"act", "download_version"}, {"version", "26.3"}})
                                    Dim w2 As String = ModXflAiExec.VersionMismatchWarning("帮我下载 26.3", acts)
                                    Logger.Info("[XFL][测试] 说26.3→要26.3：警告=" & If(w2 = "", "无", "有") & "（应为无）")
                                    acts.Clear()
                                    acts.Add(New JObject From {{"act", "download_version"}, {"version", "99.99"}})
                                    Dim w3 As String = ModXflAiExec.VersionMismatchWarning("帮我下载 1.21.1", acts)
                                    Logger.Info("[XFL][测试] 说1.21.1→要99.99：警告=" & If(w3 = "", "无", "有") & "（应为有）")
                                End If
                                Logger.Info("[XFL][测试] AEXEC 完成")
                            Catch ex As Exception
                                Logger.Warn(ex, "[XFL][测试] AEXEC 失败")
                            End Try
                        End Sub, "XFL Test AExec")
                    End Sub
                tmrA.Start()
            End If
        Catch exT As Exception
            Logger.Warn(exT, "[XFL][测试] AEXEC 钩子挂载失败")
        End Try

        Try
            If Environment.GetEnvironmentVariable("XFL_TEST_CANCEL") = "1" Then
                Dim tmrC As New System.Windows.Threading.DispatcherTimer() With {.Interval = TimeSpan.FromSeconds(3)}
                AddHandler tmrC.Tick,
                    Sub()
                        tmrC.Stop()
                        RunInNewThread(
                        Sub()
                            Try
                                Dim stamp As String = GetTimeMs().ToString()
                                Dim loaders As New List(Of LoaderBase)
                                For i As Integer = 1 To 60
                                    Dim fs As New List(Of String) From {"https://launchermeta.mojang.com/mc/game/version_manifest.json"}
                                    Dim f As New NetFile(fs, PathTemp & "Cache\xfl_cancel_test_" & stamp & "_" & i & ".json", New FileChecker)
                                    loaders.Add(New LoaderDownload("取消测试分片 " & i, {f}) With {.ProgressWeight = 1})
                                Next
                                Dim combo As New LoaderCombo(Of Integer)("XFL 取消测试", loaders) With {.OnStateChanged = AddressOf LoaderStateChangedHintOnly}
                                combo.Start(1)
                                LoaderTaskbarAdd(combo)
                                RunInUi(Sub() PageChange(New PageStackData With {.Page = PageType.DownloadManager}))
                                Threading.Thread.Sleep(5000)
                                Logger.Info("[XFL][测试] 取消前：任务数=" & LoaderTaskbar.Count & "，状态=" & combo.State)
                                RunInUi(Sub() FrmSpeedLeft.CancelAll_Click(Nothing, Nothing))
                                Threading.Thread.Sleep(2000)
                                Logger.Info("[XFL][测试] 取消后：任务数=" & LoaderTaskbar.Count & "，状态=" & combo.State)
                                Logger.Info("[XFL][测试] CANCEL 完成")
                            Catch ex As Exception
                                Logger.Warn(ex, "[XFL][测试] CANCEL 失败")
                            End Try
                        End Sub, "XFL Test Cancel")
                    End Sub
                tmrC.Start()
            End If
        Catch exCC As Exception
            Logger.Warn(exCC, "[XFL][测试] CANCEL 钩子挂载失败")
        End Try

        Try
            If Environment.GetEnvironmentVariable("XFL_TEST_CARET") = "1" Then
                Dim tmrK As New System.Windows.Threading.DispatcherTimer() With {.Interval = TimeSpan.FromSeconds(4)}
                AddHandler tmrK.Tick,
                    Sub()
                        tmrK.Stop()
                        Try
                            Dim ai = FrmLaunchLeft
                            If ai Is Nothing Then Return
                            Dim tb As Windows.Controls.TextBox = ai.TxtAiInput
                            Dim ph As Windows.Controls.TextBlock = ai.LabAiPlaceholder
                            If tb Is Nothing Then Return
                            Logger.Info("[XFL][光标] TxtAiInput: 尺寸=" & tb.ActualWidth & "x" & tb.ActualHeight & " Padding=" & tb.Padding.ToString & " BorderThickness=" & tb.BorderThickness.ToString & " FontSize=" & tb.FontSize & " VCA=" & tb.VerticalContentAlignment.ToString)
                            If ph IsNot Nothing Then
                                Dim pTb As Windows.Point = ph.TranslatePoint(New Windows.Point(0, 0), tb)
                                Logger.Info("[XFL][光标] 占位文字相对输入框: X=" & pTb.X & " Y=" & pTb.Y & " 字号=" & ph.FontSize & " 高=" & ph.ActualHeight)
                            End If

                            Dim host As Windows.FrameworkElement = Nothing
                            Try
                                host = TryCast(tb.Template.FindName("PART_ContentHost", tb), Windows.FrameworkElement)
                            Catch
                            End Try
                            If host IsNot Nothing Then
                                Dim hPos As Windows.Point = host.TranslatePoint(New Windows.Point(0, 0), tb)
                                Logger.Info("[XFL][光标] PART_ContentHost: 相对输入框 X=" & hPos.X & " Y=" & hPos.Y & " 尺寸=" & host.ActualWidth & "x" & host.ActualHeight & " Margin=" & host.Margin.ToString)

                                Try
                                    Dim dv As Windows.FrameworkElement = TryCast(host, Windows.Controls.ScrollViewer).Content
                                    If dv IsNot Nothing Then
                                        Dim dPos As Windows.Point = dv.TranslatePoint(New Windows.Point(0, 0), tb)
                                        Logger.Info("[XFL][光标] TextView: 相对输入框 X=" & dPos.X & " Y=" & dPos.Y & " 高=" & dv.ActualHeight & " Margin=" & dv.Margin.ToString)
                                    End If
                                Catch
                                End Try
                            End If

                            Try
                                Dim r0 As Windows.Rect = tb.GetRectFromCharacterIndex(0, False)
                                Logger.Info("[XFL][光标] 首个字符矩形: X=" & r0.X & " Y=" & r0.Y & " H=" & r0.Height)
                            Catch exR As Exception
                                Logger.Warn(exR, "[XFL][光标] 取字符矩形失败")
                            End Try

                            If tb.Text = "" Then tb.Text = "测"
                            Try
                                Dim r1 As Windows.Rect = tb.GetRectFromCharacterIndex(1, False)
                                Logger.Info("[XFL][光标] 光标矩形(文本后): X=" & r1.X & " Y=" & r1.Y & " H=" & r1.Height)
                            Catch
                            End Try
                            tb.Text = ""
                            Logger.Info("[XFL][光标] 诊断完成")
                        Catch ex As Exception
                            Logger.Warn(ex, "[XFL][光标] 诊断失败")
                        End Try
                    End Sub
                tmrK.Start()
            End If
        Catch exC As Exception
            Logger.Warn(exC, "[XFL][光标] 诊断钩子挂载失败")
        End Try

        Try
            If Environment.GetEnvironmentVariable("XFL_TEST_LOADER") = "1" Then
                Dim tmrL As New System.Windows.Threading.DispatcherTimer() With {.Interval = TimeSpan.FromSeconds(4)}
                AddHandler tmrL.Tick,
                    Sub()
                        tmrL.Stop()
                        RunInNewThread(
                        Sub()
                            Try
                                For Each pair In New String() {"26.3", "1.20.1", "99.99"}
                                    Dim mc As String = pair
                                    Dim v As String = ModXflAiExec.ResolveLoaderVersion("fabric", mc)
                                    Logger.Info("[XFL][测试] fabric(" & mc & ") = " & If(String.IsNullOrWhiteSpace(v), "<空>", v))
                                Next
                                Logger.Info("[XFL][测试] LOADER 完成")
                            Catch ex As Exception
                                Logger.Warn(ex, "[XFL][测试] LOADER 失败")
                            End Try
                        End Sub, "XFL Test Loader")
                    End Sub
                tmrL.Start()
            End If
        Catch exL As Exception
            Logger.Warn(exL, "[XFL][测试] LOADER 钩子挂载失败")
        End Try

        Try
            If Environment.GetEnvironmentVariable("XFL_TEST_FORGE") = "1" Then
                Dim tmrF As New System.Windows.Threading.DispatcherTimer() With {.Interval = TimeSpan.FromSeconds(4)}
                AddHandler tmrF.Tick,
                    Sub()
                        tmrF.Stop()
                        RunInNewThread(
                        Sub()
                            Try
                                For Each p In New String() {"neoforge|26.1", "neoforge|26.1.2", "neoforge|1.20.1", "forge|26.1", "forge|1.20.1", "fabric|26.1"}
                                    Dim parts As String() = p.Split("|"c)
                                    Dim v As String = ModXflAiExec.ResolveLoaderVersion(parts(0), parts(1))
                                    Logger.Info("[XFL][测试] " & parts(0) & "(" & parts(1) & ") = " & If(String.IsNullOrWhiteSpace(v), "<空>", v))
                                Next
                                Logger.Info("[XFL][测试] FORGE 完成")
                            Catch ex As Exception
                                Logger.Warn(ex, "[XFL][测试] FORGE 失败")
                            End Try
                        End Sub, "XFL Test Forge")
                    End Sub
                tmrF.Start()
            End If
        Catch exF As Exception
            Logger.Warn(exF, "[XFL][测试] FORGE 钩子挂载失败")
        End Try

        Try
            If Environment.GetEnvironmentVariable("XFL_TEST_MODSEARCH") = "1" Then
                Dim tmrM As New System.Windows.Threading.DispatcherTimer() With {.Interval = TimeSpan.FromSeconds(4)}
                AddHandler tmrM.Tick,
                    Sub()
                        tmrM.Stop()
                        RunInNewThread(
                        Sub()
                            Try
                                For Each q In New String() {"jei", "sodium", "不存在的模组xyz"}
                                    Dim r As Tuple(Of String, String, ResourcePlatforms, String) = ModXflAiExec.SearchModProject(q, "", "", "mod")
                                    If r Is Nothing Then
                                        Logger.Info("[XFL][测试] 搜索(" & q & ") = <未找到>")
                                    Else
                                        Logger.Info("[XFL][测试] 搜索(" & q & ") = " & r.Item2 & " | id=" & r.Item1 & " | 平台=" & r.Item3.ToString)
                                    End If
                                Next
                                Logger.Info("[XFL][测试] MODSEARCH 完成")
                            Catch ex As Exception
                                Logger.Warn(ex, "[XFL][测试] MODSEARCH 失败")
                            End Try
                        End Sub, "XFL Test ModSearch")
                    End Sub
                tmrM.Start()
            End If
        Catch exM As Exception
            Logger.Warn(exM, "[XFL][测试] MODSEARCH 钩子挂载失败")
        End Try

        ApplyRandomQuote()

        Try
            If Environment.GetEnvironmentVariable("XFL_TEST_DRAG") = "1" Then
                Dim tmrD As New System.Windows.Threading.DispatcherTimer() With {.Interval = TimeSpan.FromSeconds(5)}
                AddHandler tmrD.Tick,
                    Sub()
                        tmrD.Stop()
                        Try
                            Dim r As String = ModXflTaskbarDrag.SelfTest()
                            Logger.Info("[XFL][测试] DRAG 自检: " & r)
                            Logger.Info("[XFL][测试] DRAG 完成")
                        Catch ex As Exception
                            Logger.Warn(ex, "[XFL][测试] DRAG 失败")
                        End Try
                    End Sub
                tmrD.Start()
            End If
        Catch exD As Exception
            Logger.Warn(exD, "[XFL][测试] DRAG 钩子挂载失败")
        End Try

        Try
            If Environment.GetEnvironmentVariable("XFL_TEST_TB") = "1" Then
                Dim tmrB As New System.Windows.Threading.DispatcherTimer() With {.Interval = TimeSpan.FromSeconds(5)}
                AddHandler tmrB.Tick,
                    Sub()
                        tmrB.Stop()
                        Try
                            Dim sb As New System.Text.StringBuilder
                            For Each b In PanTaskbarStack.Children.OfType(Of MyRadioButton)()
                                sb.Append(If(b.Tag, "?").ToString & "(" & b.Name & ",vis=" & b.Visibility.ToString & ") ")
                            Next
                            Logger.Info("[XFL][测试] 任务栏顺序: " & sb.ToString())
                            Logger.Info("[XFL][测试] 保存的顺序设置: [" & Settings.Get(Of String)("XflTaskbarOrder") & "]")
                            Dim lt = Settings.Get(Of McLoginType)("LoginType")
                            Logger.Info("[XFL][测试] LoginType=" & lt.ToString())
                            Logger.Info("[XFL][测试] McLoginLoader.State=" & If(McLoginLoader Is Nothing, "Nothing", McLoginLoader.State.ToString()))
                            If McLoginLoader IsNot Nothing Then
                                Logger.Info("[XFL][测试] Output.Type=[" & If(McLoginLoader.Output.Type, "") & "] Name=[" & If(McLoginLoader.Output.Name, "") & "] TokenLen=" & If(McLoginLoader.Output.AccessToken, "").Length)
                            End If
                            Logger.Info("[XFL][测试] HasPremiumMs=" & ModXflHorizon.HasPremiumMs())
                            Logger.Info("[XFL][测试] TB 完成")
                        Catch ex As Exception
                            Logger.Warn(ex, "[XFL][测试] TB 失败")
                        End Try
                    End Sub
                tmrB.Start()
            End If
        Catch exB As Exception
            Logger.Warn(exB, "[XFL][测试] TB 钩子挂载失败")
        End Try

        Try
            If Environment.GetEnvironmentVariable("XFL_TEST_HORIZON") = "1" Then
                Dim tmrH As New System.Windows.Threading.DispatcherTimer() With {.Interval = TimeSpan.FromSeconds(4)}
                AddHandler tmrH.Tick,
                    Sub()
                        tmrH.Stop()
                        Try
                            PageChange(New PageStackData With {.Page = PageType.Horizon})
                            Logger.Info("[XFL][测试] 已切到 Horizon 页")
                            Dim st = ModXflHorizon.GetStatus()
                            Logger.Info("[XFL][测试] 状态 Installed=" & st.Installed & " Prepared=" & st.Prepared & " CanLaunch=" & st.CanLaunch & " tag=" & If(st.InstalledTag,"-"))
                            Logger.Info("[XFL][测试] 正版=" & ModXflHorizon.HasPremiumMs() & " 263=" & (ModXflHorizon.FindVanilla263Json() IsNot Nothing) & " JDK25=" & (ModXflHorizon.FindJdk25() IsNot Nothing))
                            Logger.Info("[XFL][测试] HORIZON 完成")
                            RunInNewThread(Sub()
                                Try
                                    Dim ni = ModXflHorizon.QueryNightly()
                                    Logger.Info("[XFL][测试] nightly Ok=" & ni.Ok & " tag=" & If(ni.VersionTag,"-") & " size=" & ni.Size & " err=" & If(ni.ErrMsg,"-"))
                                    Logger.Info("[XFL][测试] nightly zip=" & If(String.IsNullOrWhiteSpace(ni.ZipUrl),"-",ni.ZipUrl.Substring(0, Math.Min(70, ni.ZipUrl.Length))))
                                Catch ex As Exception
                                    Logger.Warn(ex, "[XFL][测试] nightly 查询异常")
                                End Try
                            End Sub, "XFL Test HorizonNightly")
                        Catch ex As Exception
                            Logger.Warn(ex, "[XFL][测试] HORIZON 失败")
                        End Try
                    End Sub
                tmrH.Start()
            End If
        Catch exH As Exception
            Logger.Warn(exH, "[XFL][测试] HORIZON 钩子挂载失败")
        End Try

        PageSetupUI.HiddenRefresh()
        PageSetupUI.BackgroundRefresh(False, True)
        MusicRefreshPlay(False, True)

        BtnExtraDownload.ShowCheck = AddressOf BtnExtraDownload_ShowCheck
        BtnExtraBack.ShowCheck = AddressOf BtnExtraBack_ShowCheck
        BtnExtraApril.ShowCheck = AddressOf BtnExtraApril_ShowCheck
        BtnExtraShutdown.ShowCheck = AddressOf BtnExtraShutdown_ShowCheck
        BtnExtraApril.ShowRefresh()
        InitXflTaskbar()

        Try
            Dim cid As String = If(OAuthClientId, "")
            Logger.Info($"[XFL] 微软登录 ClientId 已就绪：{If(String.IsNullOrWhiteSpace(cid), "【为空！正版登录会失败】", cid)}")
        Catch
        End Try

        Try
            Dim forceTheme As String = Environment.GetEnvironmentVariable("XFL_THEME")
            If Not String.IsNullOrWhiteSpace(forceTheme) Then
                ThemeRefresh(CInt(forceTheme))
                Logger.Info($"[XFL] 测试钩子：主题已强制为 {forceTheme}")
            End If
        Catch
        End Try

        Dim xflTestAi As String = Environment.GetEnvironmentVariable("XFL_TEST_AI")
        If xflTestAi = "1" OrElse xflTestAi = "2" Then
            RunInNewThread(
                Sub()
                    Try
                        Threading.Thread.Sleep(5000)
                        Logger.Info("[XFL] AI 自检：开始请求，接口=" & Settings.Get(Of String)("AiApiUrl"))

                        RunInUi(
                            Sub()
                                Try
                                    If FrmLaunchLeft IsNot Nothing Then
                                        FrmLaunchLeft.TxtAiInput.Text = If(Environment.GetEnvironmentVariable("XFL_TEST_AI_Q"),
                                                                  "2026年最新的 Minecraft 正式版本是多少？")
                                        FrmLaunchLeft.SendAiMessage()
                                    End If
                                Catch ex2 As Exception
                                    Logger.Error(ex2, "[XFL] AI 自检：UI 路径触发失败")
                                End Try
                            End Sub)

                If Environment.GetEnvironmentVariable("XFL_TEST_AI") = "2" Then
                    Threading.Thread.Sleep(6000)
                    Logger.Info("[XFL] AI 自检：发送第二条消息（预期触发 45 秒限速）")
                    RunInUi(
                        Sub()
                            Try
                                If FrmLaunchLeft IsNot Nothing Then

                                    FrmLaunchLeft.TxtAiInput.Text = If(Environment.GetEnvironmentVariable("XFL_TEST_AI_Q"),
                                                                      "再给我讲一句关于 Minecraft 的冷知识")
                                    FrmLaunchLeft.SendAiMessage()
                                End If
                            Catch ex3 As Exception
                                Logger.Error(ex3, "[XFL] AI 自检：第二条触发失败")
                            End Try
                        End Sub)
                End If
                If False Then ModAi.AiSendAsync("请用一句话介绍你自己",
                            Sub(status As String)
                                Logger.Info("[XFL] AI 自检：阶段=" & status)
                            End Sub,
                            Sub(thinking As String)
                                Logger.Info("[XFL] AI 自检：思考过程长度=" & If(thinking, "").Length)
                            End Sub,
                            Sub(reply As String)
                                Logger.Info("[XFL] AI 自检：成功，回复=" & reply)
                            End Sub,
                            Sub(err As String)
                                Logger.Error("[XFL] AI 自检：失败，" & err)
                            End Sub)
                    Catch ex As Exception
                        Logger.Error(ex, "[XFL] AI 自检：触发异常")
                    End Try
                End Sub)
        End If

        If Environment.GetEnvironmentVariable("XFL_TEST_MSGBOX") = "1" Then
            RunInNewThread(
                Sub()
                    Try
                        Threading.Thread.Sleep(6000)
                        RunInUi(Sub() MyMsgBox("这是弹窗层级测试", "测试弹窗"))
                        Logger.Info("[XFL] 测试钩子：已弹出层级测试消息框")
                    Catch ex As Exception
                        Logger.Error(ex, "[XFL] 测试钩子：消息框失败")
                    End Try
                End Sub)
        End If

        Try
            ModDependency.EnsureAllAsync(
                Sub(kind As String, pct As Double, msg As String)
                    Logger.Info($"[XFL][依赖] {kind} {pct:P0} {msg}")
                End Sub,
                Sub(okAll As Boolean, msg As String)
                    Logger.Info($"[XFL][依赖] 完成：{okAll} {msg}")
                End Sub)
        Catch ex As Exception
            Logger.Warn(ex, "[XFL][依赖] 启动检查失败")
        End Try

        Try
            Dim forcePage As String = Environment.GetEnvironmentVariable("XFL_START_PAGE")
            If Not String.IsNullOrWhiteSpace(forcePage) Then
                RunInNewThread(
                    Sub()
                        Try
                            Threading.Thread.Sleep(9000)
                            Dim pv As Integer = CInt(forcePage)
                            Logger.Info($"[XFL] 测试钩子：程序化跳转到页面 {pv}")
                            RunInUi(Sub() PageChange(CType(pv, PageType)))
                        Catch ex As Exception
                            Logger.Error(ex, "[XFL] 测试钩子：程序化跳转失败")
                        End Try
                    End Sub)
            End If
        Catch
        End Try

        Try
            Dim navSpec As String = Environment.GetEnvironmentVariable("XFL_TEST_NAV")
            If Not String.IsNullOrWhiteSpace(navSpec) Then
                Dim parts As String() = navSpec.Split(":"c)
                Dim navPage As Integer = CInt(parts(0))
                Dim navDelay As Integer = If(parts.Length > 1, CInt(parts(1)), 30000)
                RunInNewThread(
                    Sub()
                        Try
                            Threading.Thread.Sleep(navDelay)
                            Logger.Info($"[XFL] 测试钩子：{navDelay}ms 后切页到 {navPage}")
                            RunInUi(Sub() PageChange(CType(navPage, PageType)))
                        Catch ex As Exception
                            Logger.Error(ex, "[XFL] 测试钩子：延迟切页失败")
                        End Try
                    End Sub)
            End If
        Catch
        End Try

        Try
            If Environment.GetEnvironmentVariable("XFL_TEST_DLBADGE") = "1" Then
                RunInNewThread(
                    Sub()
                        Try
                            Threading.Thread.Sleep(12000)
                            RunInUi(
                                Sub()
                                    Try
                                        BtnExtraDownload.Progress = 0.45
                                        BtnExtraDownload.Show = True
                                        Logger.Info("[XFL] 测试钩子：已强制显示下载进度按钮")
                                    Catch ex2 As Exception
                                        Logger.Error(ex2, "[XFL] 测试钩子：显示下载进度按钮失败")
                                    End Try
                                End Sub)
                        Catch
                        End Try
                    End Sub)
            End If
        Catch
        End Try

        Try
            Dim ts As String = Environment.GetEnvironmentVariable("XFL_TEST_SMALL")
            If Not String.IsNullOrWhiteSpace(ts) Then
                RunInNewThread(
                    Sub()
                        Try
                            Threading.Thread.Sleep(13000)
                            Dim u As String = If(ts = "1", "https://www.bing.com", ts)
                            RunInUi(Sub()
                                        Try
                                            FormWebSmall.ShowSmall(u)
                                        Catch
                                        End Try
                                    End Sub)
                        Catch
                        End Try
                    End Sub)
            End If
        Catch
        End Try

        If Environment.GetEnvironmentVariable("XFL_START_LOGIN") = "1" Then
            RunInUi(Sub()
                        PageChangeActual(PageType.Login)
                        ShowLoginPage(True)
                    End Sub, True)
        End If

        Dim Resizer As New MyResizer(Me)
        Resizer.addResizerDown(ResizerB)
        Resizer.addResizerLeft(ResizerL)
        Resizer.addResizerLeftDown(ResizerLB)
        Resizer.addResizerLeftUp(ResizerLT)
        Resizer.addResizerRight(ResizerR)
        Resizer.addResizerRightDown(ResizerRB)
        Resizer.addResizerRightUp(ResizerRT)
        Resizer.addResizerUp(ResizerT)

        If RandomInteger(1, 1000) = 233 Then
            ShapeTitleLogo.Data = New GeometryConverter().ConvertFromString("M26,29 v-25 h6 a7,7 180 0 1 0,14 h-6 M83,6.5 a10,11.5 180 1 0 0,18 M48,2.5 v24.5 h13.5")
        End If

        ThemeRefreshMain()
        Try
            Height = Settings.Get(Of Integer)("WindowHeight")
            Width = Settings.Get(Of Integer)("WindowWidth")

            Dim wm As Integer = Settings.Get(Of Integer)("WindowHeightMode")
            If wm = 1 Then
                MinHeight = 480
                Height = 480
            End If
        Catch ex As Exception
            Logger.Error(ex, "读取窗口默认大小失败", LogBehavior.Toast)
            Height = MinHeight + 100
            Width = MinWidth + 100
        End Try

        Topmost = False
        If FrmStart IsNot Nothing Then FrmStart.Close(New TimeSpan(0, 0, 0, 0, 400 / AniSpeed))

        Top = (GetWPFSize(My.Computer.Screen.WorkingArea.Height) - Height) / 2
        Left = (GetWPFSize(My.Computer.Screen.WorkingArea.Width) - Width) / 2
        IsSizeSaveable = True
        ShowWindowToTop()
        Dim HwndSource As Interop.HwndSource = PresentationSource.FromVisual(Me)
        HwndSource.AddHook(New Interop.HwndSourceHook(AddressOf WndProc))
        AniStart({
            AaCode(Sub() AniControlEnabled -= 1, 50),
            AaOpacity(Me, Settings.Get(Of Integer)("UiLauncherTransparent") / 1000 + 0.4, 250, 100),
            AaDouble(Sub(i) TransformPos.Y += i, -TransformPos.Y, 600, 100, New AniEaseOutBack(AniEasePower.Weak)),
            AaDouble(Sub(i) TransformRotate.Angle += i, -TransformRotate.Angle, 500, 100, New AniEaseOutBack(AniEasePower.Weak)),
            AaCode(
            Sub()
                PanBack.RenderTransform = Nothing
                IsWindowLoadFinished = True
                Logger.Info($"DPI：{DPI}，系统版本：{Environment.OSVersion.VersionString}，XFL 位置：{PathExe}")
            End Sub, , True)
        }, "Form Show")

        AniStart()
        TimerMainStart()

        RunInNewThread(
        Sub()

            Const EulaVersion As Integer = 2
            If Settings.Get(Of Integer)("SystemEulaVersion") < EulaVersion Then
                Select Case MyMsgBox(
                    If(Settings.Get(Of Integer)("SystemEulaVersion") = 0,
                        GetLang("LangDialogPolicyContent"),
                        GetLang("LangDialogPolicyContentModified")),
                        GetLang("LangDialogPolicyTitle"), GetLang("LangDialogBtnAgree"), GetLang("LangDialogBtnDeny"), GetLang("LangDialogBtnPolicyContent"),
                        Button3Action:=Sub() OpenWebsite("https://shimo.im/docs/rGrd8pY8xWkt6ryW"))
                    Case 1
                        Settings.Set("SystemEulaVersion", EulaVersion)
                    Case 2
                        EndProgram(False)
                End Select
            End If

            Try
                JavaInit()
                Thread.Sleep(100)
                DlClientListMojangLoader.Start(1)
                RunCountSub()
                ServerLoader.Start()
                RunInNewThread(AddressOf TryClearTaskTemp, "TryClearTaskTemp", ThreadPriority.BelowNormal)
            Catch ex As Exception
                Logger.Error(ex, "初始化加载池运行失败")
            End Try

            Try
                FileUtils.Delete(Paths.Base & "XFL\Xiaofu Launcher.exe")
            Catch ex As Exception
                Logger.Warn(ex, "清理自动更新文件失败")
            End Try

            Telemetry("启动")

        End Sub, "初始化", ThreadPriority.Lowest)

        Logger.Info($"第三阶段加载用时：{GetTimeMs() - ApplicationStartTick} ms")

        Try
            If Environment.GetEnvironmentVariable("XFL_TEST_TAG") = "1" Then
                Dim tmrG As New System.Windows.Threading.DispatcherTimer() With {.Interval = TimeSpan.FromSeconds(4)}
                AddHandler tmrG.Tick,
                    Sub()
                        tmrG.Stop()
                        RunInNewThread(
                        Sub()
                            Try
                                Logger.Info("[XFL][测试] 知识库条目数 = " & ModXflCatalog.TagsHint().Length & " 字（标签说明已就绪）")
                                Dim r1 As List(Of Tuple(Of String, String)) = ModXflAiExec.ExpandTagCandidates("performance", "26.1", "neoforge", Nothing)
                                Logger.Info("[XFL][测试] performance@26.1/neoforge → " & r1.Count & " 个")
                                For Each x In r1
                                    Logger.Info("[XFL][测试]    · " & x.Item1 & " (" & x.Item2 & ")")
                                Next
                                Dim r2 As List(Of Tuple(Of String, String)) = ModXflAiExec.ExpandTagCandidates("performance", "26.3", "fabric", Nothing)
                                Logger.Info("[XFL][测试] performance@26.3/fabric → " & r2.Count & " 个")
                                For Each x In r2
                                    Logger.Info("[XFL][测试]    · " & x.Item1 & " (" & x.Item2 & ")")
                                Next
                                Dim r3 As List(Of Tuple(Of String, String)) = ModXflAiExec.ExpandTagCandidates("hud", "1.20.1", "forge", Nothing)
                                Logger.Info("[XFL][测试] hud@1.20.1/forge → " & r3.Count & " 个")
                                For Each x In r3
                                    Logger.Info("[XFL][测试]    · " & x.Item1 & " (" & x.Item2 & ")")
                                Next
                                Dim r4 As List(Of Tuple(Of String, String)) = ModXflAiExec.ExpandTagCandidates("nonexistent", "26.1", "fabric", Nothing)
                                Logger.Info("[XFL][测试] nonexistent → " & r4.Count & " 个（应为0）")
                                Logger.Info("[XFL][测试] TAG 完成")
                            Catch ex As Exception
                                Logger.Warn(ex, "[XFL][测试] TAG 失败")
                            End Try
                        End Sub, "XFL Test Tag")
                    End Sub
                tmrG.Start()
            End If
        Catch exG As Exception
            Logger.Warn(exG, "[XFL][测试] TAG 钩子挂载失败")
        End Try

        Try
            If Environment.GetEnvironmentVariable("XFL_TEST_PROMPT") = "1" Then
                Dim tmrP As New System.Windows.Threading.DispatcherTimer() With {.Interval = TimeSpan.FromSeconds(3)}
                AddHandler tmrP.Tick,
                    Sub()
                        tmrP.Stop()
                        RunInNewThread(
                        Sub()
                            Try
                                Dim tagHint As String = ModXflCatalog.TagsHint()
                                Logger.Info("[XFL][测试] 用途标签说明长度 = " & tagHint.Length)
                                Logger.Info("[XFL][测试] 含 performance 说明 = " & tagHint.Contains("performance"))
                                Logger.Info("[XFL][测试] 含 流畅 关键词 = " & tagHint.Contains("流畅"))
                                Logger.Info("[XFL][测试] ---- 标签说明全文 ----")
                                For Each ln In tagHint.Replace(Chr(13), "").Split(Chr(10))
                                    If String.IsNullOrWhiteSpace(ln) Then Continue For
                                    Logger.Info("[XFL][测试] " & ln)
                                Next

                                Dim proto As String = ModXflAiExec.ProtocolPrompt()
                                Logger.Info("[XFL][测试] 协议含 tag 参数 = " & proto.Contains("tag"))
                                Logger.Info("[XFL][测试] PROTOCOL 完成")
                            Catch ex As Exception
                                Logger.Warn(ex, "[XFL][测试] PROTOCOL 失败")
                            End Try
                        End Sub, "XFL Test Prompt")
                    End Sub
                tmrP.Start()
            End If
        Catch exP As Exception
            Logger.Warn(exP, "[XFL][测试] PROTOCOL 钩子挂载失败")
        End Try

        Try
            If Environment.GetEnvironmentVariable("XFL_TEST_PERF") = "1" Then
                Dim tmrX As New System.Windows.Threading.DispatcherTimer() With {.Interval = TimeSpan.FromSeconds(3)}
                AddHandler tmrX.Tick,
                    Sub()
                        tmrX.Stop()
                        RunInNewThread(
                        Sub()
                            Try

                                Dim t0 As Long = GetTimeMs()
                                Dim c1 As List(Of ModXflModInfo) = ModXflMods.ByCategory(New List(Of String)({"optimization"}), "", "", 20)
                                Dim e1 As Long = GetTimeMs() - t0
                                Logger.Info("[XFL][测试] optimization 分类 → " & c1.Count & " 个，耗时 " & e1 & " ms")
                                For Each x In c1
                                    Logger.Info("[XFL][测试]    · " & x.Title & " (" & x.ProjectId & ") 下载 " & x.Downloads)
                                Next

                                Dim t1 As Long = GetTimeMs()
                                Dim cats As New List(Of String)
                                cats.Add("optimization")
                                cats.Add("decoration")
                                cats.Add("technology")
                                cats.Add("worldgen")
                                Dim c2 As List(Of ModXflModInfo) = ModXflMods.ByCategory(cats, "", "", 15)
                                Dim e2 As Long = GetTimeMs() - t1
                                Logger.Info("[XFL][测试] 4 个分类并发 → " & c2.Count & " 个，耗时 " & e2 & " ms")

                                Dim t2 As Long = GetTimeMs()
                                Dim c3 As List(Of ModXflModInfo) = ModXflMods.ByCategory(New List(Of String)({"optimization"}), "26.1", "", 20)
                                Dim e3 As Long = GetTimeMs() - t2
                                Logger.Info("[XFL][测试] optimization@26.1 → " & c3.Count & " 个，耗时 " & e3 & " ms")

                                Dim t3 As Long = GetTimeMs()
                                Dim c4 As List(Of ModXflModInfo) = ModXflMods.ByCategory(New List(Of String)({"optimization"}), "", "", 20)
                                Dim e4 As Long = GetTimeMs() - t3
                                Logger.Info("[XFL][测试] 缓存命中后 → " & c4.Count & " 个，耗时 " & e4 & " ms")
                                Logger.Info("[XFL][测试] PERF 完成")
                            Catch ex As Exception
                                Logger.Warn(ex, "[XFL][测试] PERF 失败")
                            End Try
                        End Sub, "XFL Test Perf")
                    End Sub
                tmrX.Start()
            End If
        Catch exX As Exception
            Logger.Warn(exX, "[XFL][测试] PERF 钩子挂载失败")
        End Try

        Try
            If Environment.GetEnvironmentVariable("XFL_TEST_PICK") = "1" Then
                Dim tmrQ As New System.Windows.Threading.DispatcherTimer() With {.Interval = TimeSpan.FromSeconds(3)}
                AddHandler tmrQ.Tick,
                    Sub()
                        tmrQ.Stop()
                        RunInNewThread(
                        Sub()
                            Try
                                Dim cands As New List(Of ModXflModInfo)
                                For Each nm In New String() {"Sodium", "Lithium", "FerriteCore", "ImmediatelyFast"}
                                    cands.Add(New ModXflModInfo With {
                                        .ProjectId = "test-" & nm,
                                        .Title = nm & "（渲染优化）",
                                        .Description = "用于验证勾选窗的测试项"})
                                Next
                                Logger.Info("[XFL][测试] 打开勾选窗，候选 " & cands.Count & " 个")
                                Dim picked As List(Of String) = ModXflAiExec.ShowModPicker(
                                    "选择要下载的模组（分类：optimization）",
                                    "共 4 个候选。勾选后点「下载所选」。", cands, True, True)
                                Logger.Info("[XFL][测试] 勾选结果 = " & picked.Count & " 个：" & String.Join(",", picked))
                                Logger.Info("[XFL][测试] PICK 完成")
                            Catch ex As Exception
                                Logger.Warn(ex, "[XFL][测试] PICK 失败")
                            End Try
                        End Sub, "XFL Test Pick")
                    End Sub
                tmrQ.Start()
            End If
        Catch exQ As Exception
            Logger.Warn(exQ, "[XFL][测试] PICK 钩子挂载失败")
        End Try

        Try
            If Environment.GetEnvironmentVariable("XFL_TEST_CN") = "1" Then
                Dim tmrN As New System.Windows.Threading.DispatcherTimer() With {.Interval = TimeSpan.FromSeconds(3)}
                AddHandler tmrN.Tick,
                    Sub()
                        tmrN.Stop()
                        RunInNewThread(
                        Sub()
                            Try

                                Dim t0 As Long = GetTimeMs()
                                Dim cats1 As New List(Of String)
                                cats1.Add("optimization")
                                Dim c1 As List(Of ModXflModInfo) = ModXflMods.ByCategory(cats1, "26.1", "", 12)
                                Dim e1 As Long = GetTimeMs() - t0
                                Logger.Info("[XFL][测试] optimization@26.1 → " & c1.Count & " 个，耗时 " & e1 & " ms")

                                Dim t1 As Long = GetTimeMs()
                                ModXflCn.EnsureCnNames(c1)
                                Dim e2 As Long = GetTimeMs() - t1
                                Dim withCn As Integer = c1.Where(Function(x As ModXflModInfo) Not String.IsNullOrWhiteSpace(x.CnName)).Count()
                                Logger.Info("[XFL][测试] 补中文名耗时 " & e2 & " ms，命中 " & withCn & "/" & c1.Count)
                                For Each x In c1
                                    Dim shown As String = If(String.IsNullOrWhiteSpace(x.CnName), x.Title, x.CnName)
                                    If shown <> x.Title AndAlso Not shown.Contains(x.Title) Then shown &= " (" & x.Title & ")"
                                    Logger.Info("[XFL][测试]    · " & shown)
                                Next

                                Dim t2 As Long = GetTimeMs()
                                Dim cats2 As New List(Of String)
                                cats2.Add("optimization")
                                cats2.Add("decoration")
                                cats2.Add("technology")
                                cats2.Add("worldgen")
                                Dim c2 As List(Of ModXflModInfo) = ModXflMods.ByCategory(cats2, "", "", 12)
                                Dim e3 As Long = GetTimeMs() - t2
                                Logger.Info("[XFL][测试] 4 分类并发 → " & c2.Count & " 个，耗时 " & e3 & " ms")
                                Logger.Info("[XFL][测试] CN 完成")
                            Catch ex As Exception
                                Logger.Warn(ex, "[XFL][测试] CN 失败")
                            End Try
                        End Sub, "XFL Test Cn")
                    End Sub
                tmrN.Start()
            End If
        Catch exN As Exception
            Logger.Warn(exN, "[XFL][测试] CN 钩子挂载失败")
        End Try

    End Sub

    Private Sub RunCountSub()
        Settings.Set("SystemCount", Settings.Get(Of Integer)("SystemCount") + 1)
#If Not BETA Then
        If BuildType = BuildTypes.Snapshot Then
            Select Case Settings.Get(Of Integer)("SystemCount")
                Case 1
                    MyMsgBox(GetLang("LangFormMainSnapshotDialogContent"), GetLang("LangFormMainSnapshotDialogTitle"))
            End Select
            If Settings.Get(Of Integer)("SystemCount") >= 99 Then
                If ThemeUnlock(6, False) Then
                    MyMsgBox(GetLang("LangDialogHardcoreFanTheme"), GetLang("LangDialogTitleTip"))
                End If
            End If
        End If
#End If
    End Sub

    Private Sub UpgradeSub(LastVersionCode As Integer)
        Logger.Info($"版本号从 {LastVersionCode} 升高到 {VersionCode}")
        Settings.Set("SystemLastVersionReg", VersionCode)

        Dim HighestVersionCode As Integer
        Const SettingKey = If(BuildType = BuildTypes.Snapshot, "SystemHighestAlphaVersionReg", "SystemHighestBetaVersionReg")
        HighestVersionCode = Settings.Get(Of Integer)(SettingKey)
        If HighestVersionCode < VersionCode Then
            Settings.Set(SettingKey, VersionCode)
            Logger.Info($"最高版本号从 {HighestVersionCode} 升高到 {VersionCode}")
        End If

        If Settings.Get(Of Integer)("LaunchArgumentWindowType") = 5 Then Settings.Set("LaunchArgumentWindowType", 1)

        If HighestVersionCode <= 207 Then
            Dim UnlockedTheme As New List(Of String) From {"2"}
            UnlockedTheme.AddRange(New List(Of String)(Settings.Get(Of String)("UiLauncherThemeHide").ToString.Split("|")))
            UnlockedTheme.AddRange(New List(Of String)(Settings.Get(Of String)("UiLauncherThemeHide2").ToString.Split("|")))
            Settings.Set("UiLauncherThemeHide2", UnlockedTheme.Distinct.ToList.Join("|"c))
        End If

        If LastVersionCode <= 115 AndAlso Settings.Get(Of String)("UiLauncherThemeHide2").ToString.Split("|").Contains("13") Then
            Dim UnlockedTheme As New List(Of String)(Settings.Get(Of String)("UiLauncherThemeHide2").ToString.Split("|"))
            UnlockedTheme.Remove("13")
            Settings.Set("UiLauncherThemeHide2", UnlockedTheme.Join("|"c))
            MyMsgBox(GetLang("LangFormMainThemeUnlockContentA"), GetLang("LangFormMainThemeUnlockDialogTitle"))
        End If

        If LastVersionCode <= 152 AndAlso Settings.Get(Of String)("UiLauncherThemeHide2").ToString.Split("|").Contains("12") Then
            Dim UnlockedTheme As New List(Of String)(Settings.Get(Of String)("UiLauncherThemeHide2").ToString.Split("|"))
            UnlockedTheme.Remove("12")
            Settings.Set("UiLauncherThemeHide2", UnlockedTheme.Join("|"c))
            MyMsgBox(GetLang("LangFormMainThemeUnlockContentB"), GetLang("LangFormMainThemeUnlockDialogTitle"))
        End If

        If LastVersionCode <= 161 AndAlso FileUtils.Exists(Paths.Base & "XFL\CustomSkin.png") AndAlso Not FileUtils.Exists(Paths.AppDataThenName & "CustomSkin.png") Then
            FileUtils.Copy(Paths.Base & "XFL\CustomSkin.png", Paths.AppDataThenName & "CustomSkin.png")
            Logger.Info("已移动离线自定义皮肤 (162)")
        End If
        If LastVersionCode <= 263 AndAlso FileUtils.Exists(PathTemp & "CustomSkin.png") AndAlso Not FileUtils.Exists(Paths.AppDataThenName & "CustomSkin.png") Then
            FileUtils.Copy(PathTemp & "CustomSkin.png", Paths.AppDataThenName & "CustomSkin.png")
            Logger.Info("已移动离线自定义皮肤 (264)")
        End If

        If LastVersionCode <= 205 Then
            Settings.Set("UiHiddenOtherHelp", False)
            Logger.Info("已解除帮助页面的隐藏")
        End If

        If Not Settings.Get(Of Boolean)("CacheMsV2Migrated") Then
            Settings.Set("CacheMsV2Migrated", True)
            Settings.Set("CacheMsV2OAuthRefresh", Settings.Get(Of String)("CacheMsOAuthRefresh"))
            Settings.Set("CacheMsV2Access", Settings.Get(Of String)("CacheMsAccess"))
            Settings.Set("CacheMsV2ProfileJson", Settings.Get(Of String)("CacheMsProfileJson"))
            Settings.Set("CacheMsV2Uuid", Settings.Get(Of String)("CacheMsUuid"))
            Settings.Set("CacheMsV2Name", Settings.Get(Of String)("CacheMsName"))
            Logger.Info("已从老版本迁移微软登录结果")
        End If

        If Settings.HasSaved("ToolDownloadTranslate") AndAlso Not Settings.HasSaved("ToolDownloadTranslateV2") Then
            Settings.Set("ToolDownloadTranslateV2", Settings.Get(Of Integer)("ToolDownloadTranslate") + 1)
            Logger.Info("已从老版本迁移 Mod 命名设置")
        End If

        If LastVersionCode <= 381 AndAlso Settings.HasSaved("LaunchAdvanceJvm") AndAlso
           Settings.Get(Of String)("LaunchAdvanceJvm").ToString.Replace("-XX:+UseG1GC ", "").Replace("-XX:-UseAdaptiveSizePolicy ", "").Trim = Settings.GetDefault("LaunchAdvanceJvm") Then
            Settings.Reset("LaunchAdvanceJvm")
            Logger.Info("已重置 JVM 参数设置")
        End If

        If LastVersionCode <= 0 Then Return
        If HighestVersionCode >= VersionCode Then Return
        ShowUpdateLog(HighestVersionCode)
    End Sub
    Private Sub DowngradeSub(LastVersionCode As Integer)
        Logger.Info($"版本号从 {LastVersionCode} 降低到 {VersionCode}")
        Settings.Set("SystemLastVersionReg", VersionCode)
    End Sub

#End Region

#Region "自定义窗口"

    Private Sub FormMain_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        EndProgram(True)
        e.Cancel = True
    End Sub

    Public Sub EndProgram(SendWarning As Boolean)

        If HasDownloadingTask() Then
            If SendWarning AndAlso MyMsgBox(GetLang("LangDialogCloseOnDownloading"), GetLang("LangDialogTitleTip"), GetLang("LangDialogBtnOK"), GetLang("LangDialogBtnCancel")) = 2 Then Return
            RunInNewThread(
            Sub()
                Logger.Info("正在强行停止任务")
                For Each Task As LoaderBase In LoaderTaskbar.ToList()
                    Task.Cancel()
                Next
            End Sub, "强行停止下载任务")
        End If

        If FrmLinkMain?.TryExit(Not SendWarning, True) Then Return

        RunInUiWait(
        Sub()
            IsHitTestVisible = False
            If PanBack.RenderTransform Is Nothing Then
                Dim TransformPos As New TranslateTransform(0, 0)
                Dim TransformRotate As New RotateTransform(0)
                Dim TransformScale As New ScaleTransform(1, 1)
                PanBack.RenderTransform = New TransformGroup() With {.Children = New TransformCollection({TransformRotate, TransformPos, TransformScale})}
                AniStart({
                    AaOpacity(Me, -Opacity, 140, 40, New AniEaseOutFluent(AniEasePower.Weak)),
                    AaDouble(
                    Sub(i)
                        TransformScale.ScaleX += i
                        TransformScale.ScaleY += i
                    End Sub, 0.88 - TransformScale.ScaleX, 180),
                    AaDouble(Sub(i) TransformPos.Y += i, 20 - TransformPos.Y, 180, 0, New AniEaseOutFluent(AniEasePower.Weak)),
                    AaDouble(Sub(i) TransformRotate.Angle += i, 0.6 - TransformRotate.Angle, 180, 0, New AniEaseInoutFluent(AniEasePower.Weak)),
                    AaCode(
                    Sub()
                        IsHitTestVisible = False
                        Top = -10000
                        ShowInTaskbar = False
                    End Sub, 210),
                    AaCode(AddressOf EndProgramForce, 230)
                }, "Form Close")
            Else
                EndProgramForce()
            End If
            Logger.Info("收到关闭指令")
        End Sub)
    End Sub
    Private Shared IsLogShown As Boolean = False
    Public Shared Sub EndProgramForce(Optional ReturnCode As ProcessReturnValues = ProcessReturnValues.Success)
        On Error Resume Next
        IsProgramEnding = True
        AniControlEnabled += 1
        If IsUpdateWaitingRestart Then UpdateRestart(False)
        If ReturnCode = ProcessReturnValues.Exception Then
            If Not IsLogShown Then
                FeedbackInfo()
                Logger.Info(GetLang("LangCrashReport"))
                IsLogShown = True
                StartProcess(Paths.Base & "XFL\Log1.txt")
            End If
            Thread.Sleep(500)
        End If
        Logger.Info($"程序已退出，返回值：{ReturnCode}")
        ConfigUtils.SaveAll()
        Logger.Instance.Flush()
        If ReturnCode <> ProcessReturnValues.Success Then Environment.Exit(ReturnCode)
        Process.GetCurrentProcess.Kill()
    End Sub
    Private Sub BtnTitleClose_Click(sender As Object, e As RoutedEventArgs) Handles BtnTitleClose.Click
        EndProgram(True)
    End Sub

    Private Sub FormDragMove(sender As Object, e As MouseButtonEventArgs) Handles PanTitle.MouseLeftButtonDown, PanMsg.MouseLeftButtonDown
        On Error Resume Next
        If sender.IsMouseDirectlyOver Then DragMove()
    End Sub

    Public IsSizeSaveable As Boolean = False
    Private Sub FormMain_SizeChanged() Handles Me.SizeChanged, Me.Loaded
        If IsSizeSaveable Then
            Settings.Set("WindowHeight", Height)
            Settings.Set("WindowWidth", Width)
        End If
        RectForm.Rect = New Rect(0, 0, BorderForm.ActualWidth, BorderForm.ActualHeight)
        PanForm.Width = BorderForm.ActualWidth + 0.001
        PanForm.Height = BorderForm.ActualHeight + 0.001
        PanMain.Width = PanForm.Width
        PanMain.Height = Math.Max(0, PanForm.Height - PanTitle.ActualHeight - 60)
        If WindowState = WindowState.Maximized Then WindowState = WindowState.Normal
    End Sub

    Private Sub BtnTitleMin_Click() Handles BtnTitleMin.Click
        WindowState = WindowState.Minimized
    End Sub

    Public Shared Sub UpdateBackgroundAndTitleBar(Value)
        If FrmMain Is Nothing OrElse Not FrmMain.IsLoaded Then Return
        FrmMain.UpdateBackgroundAndTitleBar()
    End Sub
    Public Sub UpdateBackgroundAndTitleBar()
        Logger.Info("从设置更新背景图片与标题栏样式")

        ImgBack.Opacity = Settings.Get(Of Integer)("UiBackgroundOpacity") / 1000

        Dim BlurRadius As Double = Settings.Get(Of Integer)("UiBackgroundBlur") + 1
        If BlurRadius = 1 Then
            ImgBack.Effect = Nothing
        Else
            ImgBack.Effect = New Effects.BlurEffect With {.Radius = BlurRadius}
        End If
        ImgBack.Margin = New Thickness(-BlurRadius / 1.8)

        Dim BackgroundType As Integer = Settings.Get(Of Integer)("UiBackgroundSuit")
        If ImgBack.Background IsNot Nothing Then
            Dim Brush As ImageBrush = CType(ImgBack.Background, ImageBrush)
            If BackgroundType = 0 Then

                If Brush.ImageSource.Width < PanMain.ActualWidth / 2 AndAlso Brush.ImageSource.Height < PanMain.ActualHeight / 2 Then
                    BackgroundType = 4
                Else
                    BackgroundType = 2
                End If
            End If
            Brush.Stretch = Stretch.UniformToFill
            Brush.TileMode = TileMode.None
            Brush.Viewport = New Rect(0, 0, 1, 1)
            Brush.ViewportUnits = BrushMappingMode.RelativeToBoundingBox
            Brush.AlignmentX = AlignmentX.Center
            Brush.AlignmentY = AlignmentY.Center
            Select Case BackgroundType
                Case 1
                    Brush.Stretch = Stretch.None
                Case 3
                    Brush.Stretch = Stretch.Fill
                Case 4
                    Brush.Stretch = Stretch.None
                    Brush.TileMode = TileMode.Tile
                    Brush.Viewport = New Rect(0, 0, Brush.ImageSource.Width, Brush.ImageSource.Height)
                    Brush.ViewportUnits = BrushMappingMode.Absolute

                Case 2
                Case 5
                    Brush.AlignmentX = AlignmentX.Left
                    Brush.AlignmentY = AlignmentY.Top
                Case 6
                    Brush.AlignmentX = AlignmentX.Right
                    Brush.AlignmentY = AlignmentY.Top
                Case 7
                    Brush.AlignmentX = AlignmentX.Left
                    Brush.AlignmentY = AlignmentY.Bottom
                Case 8
                    Brush.AlignmentX = AlignmentX.Right
                    Brush.AlignmentY = AlignmentY.Bottom
                Case 9
                    Brush.AlignmentX = AlignmentX.Left
                    Brush.AlignmentY = AlignmentY.Center
                Case 10
                    Brush.AlignmentX = AlignmentX.Right
                    Brush.AlignmentY = AlignmentY.Center
                Case 11
                    Brush.AlignmentX = AlignmentX.Center
                    Brush.AlignmentY = AlignmentY.Top
                Case 12
                    Brush.AlignmentX = AlignmentX.Center
                    Brush.AlignmentY = AlignmentY.Bottom
            End Select
        End If

        Select Case Settings.Get(Of Integer)("UiLogoType")
            Case 0
                ShapeTitleLogo.Visibility = Visibility.Collapsed
                LabTitleLogo.Visibility = Visibility.Collapsed
                ImageTitleLogo.Visibility = Visibility.Collapsed
                If Not IsNothing(FrmSetupUI) Then
                    FrmSetupUI.CheckLogoLeft.Visibility = Visibility.Visible
                    FrmSetupUI.PanLogoText.Visibility = Visibility.Collapsed
                    FrmSetupUI.PanLogoChange.Visibility = Visibility.Collapsed
                End If
            Case 1
                ShapeTitleLogo.Visibility = Visibility.Visible
                LabTitleLogo.Visibility = Visibility.Collapsed
                ImageTitleLogo.Visibility = Visibility.Collapsed
                If Not IsNothing(FrmSetupUI) Then
                    FrmSetupUI.CheckLogoLeft.Visibility = Visibility.Collapsed
                    FrmSetupUI.PanLogoText.Visibility = Visibility.Collapsed
                    FrmSetupUI.PanLogoChange.Visibility = Visibility.Collapsed
                End If
            Case 2
                ShapeTitleLogo.Visibility = Visibility.Collapsed
                LabTitleLogo.Visibility = Visibility.Visible
                ImageTitleLogo.Visibility = Visibility.Collapsed
                If Not IsNothing(FrmSetupUI) Then
                    FrmSetupUI.CheckLogoLeft.Visibility = Visibility.Collapsed
                    FrmSetupUI.PanLogoText.Visibility = Visibility.Visible
                    FrmSetupUI.PanLogoChange.Visibility = Visibility.Collapsed
                End If
            Case 3
                ShapeTitleLogo.Visibility = Visibility.Collapsed
                LabTitleLogo.Visibility = Visibility.Collapsed
                ImageTitleLogo.Visibility = Visibility.Visible
                If Not IsNothing(FrmSetupUI) Then
                    FrmSetupUI.CheckLogoLeft.Visibility = Visibility.Collapsed
                    FrmSetupUI.PanLogoText.Visibility = Visibility.Collapsed
                    FrmSetupUI.PanLogoChange.Visibility = Visibility.Visible
                End If
                Try
                    ImageTitleLogo.Source = Paths.Base & "XFL\Logo.png"
                Catch ex As Exception
                    ImageTitleLogo.Source = Nothing
                    Logger.Error(ex, "显示标题栏图片失败", LogBehavior.Alert)
                End Try
        End Select
        FrmSetupUI?.CardLogo.TriggerForceResize()

        Dim LogoText As String = Settings.Get(Of String)("UiLogoText")
        If LogoText = "XFL" Then LogoText = "XFL 1"
        LabTitleLogo.Text = LogoText

        PanTitleMain.ColumnDefinitions(0).Width = New GridLength(If(Settings.Get(Of Boolean)("UiLogoLeft") AndAlso Settings.Get(Of Integer)("UiLogoType") = 0, 0, 1), GridUnitType.Star)
    End Sub

#End Region

#Region "窗体事件"

    Private Sub FormMain_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.IsRepeat Then Return

        If e.SystemKey = Key.LeftAlt OrElse e.SystemKey = Key.RightAlt Then e.Handled = True

        If PanMsg.Children.Any Then
            If e.Key = Key.Enter Then
                CType(PanMsg.Children(0), Object).Btn1_Click()
            ElseIf e.Key = Key.Escape Then
                Dim Msg As Object = PanMsg.Children(0)
                If TypeOf Msg IsNot MyMsgInput AndAlso TypeOf Msg IsNot MyMsgSelect AndAlso Msg.Btn3.Visibility = Visibility.Visible Then
                    Msg.Btn3_Click()
                ElseIf Msg.Btn2.Visibility = Visibility.Visible Then
                    Msg.Btn2_Click()
                Else
                    Msg.Btn1_Click()
                End If
            End If
            Return
        End If

        If e.Key = Key.Escape Then TriggerPageBack()

        If e.Key = Key.F11 AndAlso PageCurrent = FormMain.PageType.InstanceSelect Then
            FrmSelectRight.ShowHidden = Not FrmSelectRight.ShowHidden
            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
            Return
        End If

        If e.Key = Key.F12 Then
            PageSetupUI.HiddenForceShow = Not PageSetupUI.HiddenForceShow
            If PageSetupUI.HiddenForceShow Then
                Hint(GetLang("LangHintHideModeOff"), HintType.Green)
            Else
                Hint(GetLang("LangHintHideModeOn"), HintType.Green)
            End If
            PageSetupUI.HiddenRefresh()
            Return
        End If

        If e.Key = Key.F5 Then
            If TypeOf PageLeft Is IRefreshable Then CType(PageLeft, IRefreshable).Refresh()
            If TypeOf PageRight Is IRefreshable Then CType(PageRight, IRefreshable).Refresh()
            Return
        End If

        If e.Key = Key.Enter AndAlso PageCurrent = FormMain.PageType.Launch Then
            If IsAprilEnabled AndAlso Not IsAprilGiveup Then
                Hint(GetLang("LangHintIJustWantToStart"))
            Else
                FrmLaunchLeft.LaunchButtonClick()
            End If
        End If
    End Sub
    Private Sub FormMain_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseDown

        If FrmMain.PanMsg.Children.Count > 0 OrElse WaitingMyMsgBox.Any Then Return
        If e.ChangedButton = MouseButton.XButton1 OrElse e.ChangedButton = MouseButton.XButton2 Then TriggerPageBack()
    End Sub
    Private Sub TriggerPageBack()
        If PageCurrent = PageType.Download AndAlso PageCurrentSub = PageSubType.DownloadInstall AndAlso FrmDownloadInstall.IsInSelectPage Then
            FrmDownloadInstall.ExitSelectPage()
        Else
            PageBack()
        End If
    End Sub

    Private Sub FormMain_Activated() Handles Me.Activated

        Try
            If PageCurrent = PageType.InstanceSetup AndAlso PageCurrentSub = PageSubType.InstanceMod Then

                FrmInstanceMod.ReloadModList()
            ElseIf PageCurrent = PageType.InstanceSetup AndAlso (PageCurrentSub = PageSubType.InstanceSetup OrElse PageCurrentSub = PageSubType.InstanceExport) Then

                PageInstanceLeft.ReloadCurrentJava()
            ElseIf PageCurrent = PageType.InstanceSelect Then

                LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.RunOnUpdated, MaxDepth:=1, ExtraPath:="versions\")
            End If
        Catch ex As Exception
            Logger.Error(ex, "切回窗口时出错")
        End Try

        Return
        If PageLinkMain.LinkState <> PageLinkMain.LinkStates.Waiting Then Return
        If PageCurrent = PageType.Link Then Return
        Dim Code = ClipboardGetText() : If Code Is Nothing Then Return
        If Settings.Get(Of String)("LinkLastAutoJoinInviteCode") = Code Then Return
        If PageLinkMain.ValidateCodeFormat(Code) IsNot Nothing Then Return
        Settings.Set("LinkLastAutoJoinInviteCode", Code)
        RunInThread(
        Sub()
            If MyMsgBox(GetLang("LangFormMainJoinRoomDialogContent"), GetLang("LangFormMainJoinRoomDialogTitle"), GetLang("LangFormMainJoinRoomDialogBtnJoin"), GetLang("LangDialogBtnCancel")) = 2 Then Return
            RunInUi(
            Sub()
                PageLinkMain.Join(Code)
                ClipboardSet(Nothing, False)
            End Sub)
        End Sub)
    End Sub

    Private Sub HandleDrag(sender As Object, e As DragEventArgs)
        Try
            If e.Handled AndAlso (e.Effects <> DragDropEffects.None) Then Return
            e.Handled = True

            Static PrevData As IDataObject, PrevEffects As DragDropEffects
            If e.Data Is PrevData Then
                e.Effects = PrevEffects
                Return
            End If

            e.Effects = DragDropEffects.None
            If e.Data.GetDataPresent(DataFormats.Text) Then
                Dim Str As String = e.Data.GetData(DataFormats.Text)
                If Str.StartsWithF("authlib-injector:yggdrasil-server:") Then
                    e.Effects = DragDropEffects.Copy
                ElseIf Str.StartsWithF("file:///") Then
                    e.Effects = DragDropEffects.Copy
                End If
            ElseIf e.Data.GetDataPresent(DataFormats.FileDrop) Then
                Dim Files As String() = e.Data.GetData(DataFormats.FileDrop)
                If Files IsNot Nothing AndAlso Files.Length > 0 Then
                    e.Effects = DragDropEffects.Link
                End If
            End If
            PrevData = e.Data
            PrevEffects = e.Effects
            Logger.Info($"设置拖放类型：{e.Effects}")
        Catch ex As Exception
            Logger.Error(ex, "处理拖放时出错")
        End Try
    End Sub
    Private Sub FrmMain_Drop(sender As Object, e As DragEventArgs) Handles Me.Drop
        Try
            ShowWindowToTop()
            If e.Data.GetDataPresent(DataFormats.Text) Then

                Try
                    Dim Str As String = e.Data.GetData(DataFormats.Text)
                    Logger.Info($"接受文本拖拽：{Str}")
                    If Str.StartsWithF("authlib-injector:yggdrasil-server:") Then

                        e.Handled = True
                        e.Effects = DragDropEffects.Copy
                        Dim AuthlibServer As String = StringUtils.FormUrlUnescape(Str.Substring("authlib-injector:yggdrasil-server:".Length))
                        Logger.Info($"Authlib 拖拽：{AuthlibServer}")
                        If Not String.IsNullOrEmpty(New ValidateHttp().Validate(AuthlibServer)) Then
                            Hint(GetLang("LangHintAuthlibUrlIncorrect", AuthlibServer), HintType.Red)
                            Return
                        End If
                        Dim Target = If(PageCurrent = PageType.InstanceSetup, PageInstanceLeft.Instance, McInstanceSelected)
                        If Target Is Nothing Then
                            Hint(GetLang("LangHintAuthlibDownloadGame"), HintType.Red)
                            Return
                        End If
                        If AuthlibServer = "https://littleskin.cn/api/yggdrasil" Then

                            If MyMsgBox(GetLang("LangDialogAuthlibEnableLittleSkin", Target.Name), GetLang("LangDialogTitleAuthlibEnableLittleSkin"), GetLang("LangDialogBtnOK"), GetLang("LangDialogBtnCancel")) = 2 Then
                                Return
                            End If
                            Settings.Set("VersionServerLogin", 4, Instance:=Target)
                            Settings.Set("VersionServerAuthServer", "https://littleskin.cn/api/yggdrasil", Instance:=Target)
                            Settings.Set("VersionServerAuthRegister", "https://littleskin.cn/auth/register", Instance:=Target)
                            Settings.Set("VersionServerAuthName", "LittleSkin 登录", Instance:=Target)
                        Else

                            If MyMsgBox(GetLang("LangDialogAuthlibEnableLittleSkinServer", Target.Name, AuthlibServer), GetLang("LangDialogTitleAuthlibEnableLittleSkin"), GetLang("LangDialogBtnOK"), GetLang("LangDialogBtnCancel")) = 2 Then
                                Return
                            End If
                            Settings.Set("VersionServerLogin", 4, Instance:=Target)
                            Settings.Set("VersionServerAuthServer", AuthlibServer, Instance:=Target)
                            Settings.Set("VersionServerAuthRegister", AuthlibServer.Replace("api/yggdrasil", "auth/register"), Instance:=Target)
                            Settings.Set("VersionServerAuthName", "", Instance:=Target)
                        End If
                        If PageCurrent = PageType.InstanceSetup AndAlso PageCurrentSub = PageSubType.InstanceSetup Then

                            FrmInstanceSetup.Reload()
                        ElseIf PageCurrent = PageType.Launch Then

                            FrmLaunchLeft.RefreshPage(True, False)
                        End If
                    ElseIf Str.StartsWithF("file:///") Then

                        Dim FilePath = Net.WebUtility.UrlDecode(Str).Substring("file:///".Length).Replace("/", "\")
                        e.Handled = True
                        e.Effects = DragDropEffects.Copy
                        FileDrag(New List(Of String) From {FilePath})
                    End If
                Catch ex As Exception
                    Logger.Warn(ex, "无法接取文本拖拽事件")
                    Return
                End Try
            ElseIf e.Data.GetDataPresent(DataFormats.FileDrop) Then

                Dim FilePathRaw = e.Data.GetData(DataFormats.FileDrop)
                If FilePathRaw Is Nothing Then
                    Hint(GetLang("LangHintWindowDropExtract"), HintType.Red)
                    Return
                End If
                e.Handled = True
                e.Effects = DragDropEffects.Link
                FileDrag(CType(FilePathRaw, IEnumerable(Of String)))
            End If
        Catch ex As Exception
            Logger.Error(ex, "接取拖拽事件失败")
        End Try
    End Sub
    Private Sub FileDrag(FilePathList As IEnumerable(Of String))
        RunInNewThread(
        Sub()
            Dim FilePath As String = FilePathList.First
            Logger.Warn($"接受文件拖拽：{FilePath}{If(FilePathList.Any, $" 等 {FilePathList.Count} 个文件", "")}")

            If DirectoryUtils.Exists(FilePathList.First) AndAlso Not FileUtils.Exists(FilePathList.First) Then
                Hint(GetLang("LangHintWindowDropFolder"), HintType.Red)
                Return
            ElseIf Not FileUtils.Exists(FilePathList.First) Then
                Hint(GetLang("LangHintWindowDropFileNotFound", FilePathList.First), HintType.Red)
                Return
            End If

            Dim PathList As List(Of String) = FilePathList.ToList()
            If FilePathList.Count > 1 Then

                For Each File In PathList
                    If Not {"jar", "litemod", "disabled", "old"}.Contains(File.AfterLast(".").Lower) Then
                        Hint(GetLang("LangHintWindowDropFileOneFileAtATime"), HintType.Red)
                        Return
                    End If
                Next
            End If

            Dim Extension As String = FilePath.AfterLast(".").Lower
            If Extension = "xaml" Then
                Logger.Info("文件后缀为 XAML，作为主页加载")
                If FileUtils.Exists(Paths.Base & "XFL\Custom.xaml") Then
                    If MyMsgBox(GetLang("LangDialogCustomHomePageReplaceContent"), GetLang("LangDialogCustomHomePageReplaceTitle"), GetLang("LangDialogBtnCustomHomePageReplaceConfirm"), GetLang("LangDialogBtnCancel")) = 2 Then
                        Return
                    End If
                End If
                FileUtils.Copy(FilePath, Paths.Base & "XFL\Custom.xaml")
                RunInUi(
                Sub()
                    Settings.Set("UiCustomType", 1)
                    FrmLaunchRight.ForceRefresh()
                    Hint(GetLang("LangHintLoadedCustomHomePage"), HintType.Green)
                End Sub)
                Return
            End If

            If PageInstanceMod.InstallMods(PathList) Then Return

            If {"zip", "rar", "mrpack"}.Any(Function(t) t = Extension) Then
                Logger.Info("文件为压缩包，尝试作为整合包安装")
                Try
                    ModpackInstall(FilePath)
                    Return
                Catch ex As Exception
                    If ex.IsCanceled Then Return

                End Try
            End If

            If Extension = "rar" Then
                Hint(GetLang("LangHintRarNotSupport"))
                Return
            End If

            Try
                Logger.Info("尝试进行错误报告分析")
                Dim Analyzer As New CrashAnalyzer
                Analyzer.Import(FilePath)
                If Not Analyzer.Prepare() Then Exit Try
                Analyzer.Analyze()
                Analyzer.Output(True, New List(Of String))
                Return
            Catch ex As Exception
                Logger.Error(ex, "自主错误报告分析失败")
            End Try

            Hint(GetLang("LangHintWindowDropUnknown"))
        End Sub, "文件拖拽")
    End Sub

    Public IsSystemTimeChanged As Boolean = False
    Private Function WndProc(hwnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr, ByRef handled As Boolean) As IntPtr
        If msg = 30 Then
            Dim NowDate = Date.Now
            If NowDate.Date = ApplicationOpenTime.Date Then
                Logger.Info($"系统时间微调为：{NowDate.ToLongDateString} {NowDate.ToLongTimeString}")
                IsSystemTimeChanged = False
            Else
                Logger.Info($"系统时间修改为：{NowDate.ToLongDateString} {NowDate.ToLongTimeString}")
                IsSystemTimeChanged = True
            End If
        ElseIf msg = 400 * 16 + 2 Then
            Logger.Info($"收到置顶信息：{hwnd.ToInt64}")
            If Not IsWindowLoadFinished Then
                Logger.Info("窗口尚未加载完成，忽略置顶请求")
                Return IntPtr.Zero
            End If
            ShowWindowToTop()
            handled = True
        End If
        Return IntPtr.Zero
    End Function

    Private _Hidden As Boolean = False
    Public Property Hidden As Boolean
        Get
            Return _Hidden
        End Get
        Set(value As Boolean)
            If _Hidden = value Then Return
            _Hidden = value
            If value Then

                Left -= 10000
                ShowInTaskbar = False
                Visibility = Visibility.Hidden
                Logger.Info($"窗口已隐藏，位置：({Left},{Top})")
            Else

                If Left < -2000 Then Left += 10000
                ShowWindowToTop()
            End If
        End Set
    End Property

    Public Sub ShowWindowToTop()
        RunInUi(
        Sub()

            Visibility = Visibility.Visible
            ShowInTaskbar = True
            WindowState = WindowState.Normal
            Hidden = False
            Topmost = True
            Topmost = False
            SetForegroundWindow(Handle)
            Focus()
            Logger.Info($"窗口已置顶，位置：({Left}, {Top}), {Width} x {Height}")
        End Sub)
    End Sub

#End Region

#Region "切换页面"

    Public Enum PageType

        Launch = 0

        Download = 1

        Link = 2

        Setup = 3

        Other = 4

        InstanceSelect = 5

        DownloadManager = 6

        InstanceSetup = 7

        ResourceDetail = 8

        HelpDetail = 9

        Login = 10

        Browser = 11

        Music = 12

        LinkXfl = 13

        Horizon = 14
    End Enum

    Public Enum PageSubType
        [Default] = 0
        DownloadInstall = 0
        DownloadMod = 2
        DownloadPack = 3
        DownloadDataPack = 4
        DownloadResourcePack = 5
        DownloadShader = 6
        SetupLaunch = 0
        SetupLink = 1
        SetupUI = 2
        SetupSystem = 3
        LinkMain = 0
        OtherHelp = 0
        OtherAbout = 1
        OtherTest = 2
        InstanceOverall = 0
        InstanceSetup = 1
        InstanceMod = 2
        InstanceModDisabled = 3
        InstanceExport = 4
    End Enum

    Private Function PageNameGet(Stack As PageStackData) As String
        Select Case Stack.Page
            Case PageType.InstanceSelect
                Return GetLang("LangPageNameVersionChoose")
            Case PageType.DownloadManager
                Return GetLang("LangPageNameDownloadManagement")
            Case PageType.InstanceSetup
                Return GetLang("LangPageNameVersionConfiguration") & " - " & If(PageInstanceLeft.Instance Is Nothing, GetLang("LangPageNameVersionConfigurationUnknownVersion"), PageInstanceLeft.Instance.Name)
            Case PageType.ResourceDetail
                Return CType(Stack.Additional(0), ResourceProject).TranslatedName
            Case PageType.HelpDetail
                Return CType(Stack.Additional(0), HelpEntry).Title
            Case Else
                Return ""
        End Select
    End Function

    Public Sub PageNameRefresh(Type As PageStackData)
        LabTitleInner.Text = PageNameGet(Type)
    End Sub

    Public Sub PageNameRefresh()
        PageNameRefresh(PageCurrent)
    End Sub

    Public PageCurrent As PageStackData = PageType.Launch

    Public PageLast As PageStackData = PageType.Launch

    Public ReadOnly Property PageCurrentSub As PageSubType
        Get
            Select Case PageCurrent
                Case PageType.Download
                    If FrmDownloadLeft Is Nothing Then FrmDownloadLeft = New PageDownloadLeft
                    Return FrmDownloadLeft.PageID
                Case PageType.Setup
                    If FrmSetupLeft Is Nothing Then FrmSetupLeft = New PageSetupLeft
                    Return FrmSetupLeft.PageID
                Case PageType.Other
                    If FrmOtherLeft Is Nothing Then FrmOtherLeft = New PageOtherLeft
                    Return FrmOtherLeft.PageID
                Case PageType.InstanceSetup
                    If FrmInstanceLeft Is Nothing Then FrmInstanceLeft = New PageInstanceLeft
                    Return FrmInstanceLeft.PageID
                Case Else
                    Return 0
            End Select
        End Get
    End Property

    Public PageStack As New List(Of PageStackData)
    Public Class PageStackData

        Public Page As PageType
        Public Additional As Object

        Public Overrides Function Equals(other As Object) As Boolean
            If other Is Nothing Then Return False
            If TypeOf other Is PageStackData Then
                Dim PageOther As PageStackData = other
                If Page <> PageOther.Page Then Return False
                If Additional Is Nothing Then
                    Return PageOther.Additional Is Nothing
                Else
                    Return PageOther.Additional IsNot Nothing AndAlso Additional.Equals(PageOther.Additional)
                End If
            ElseIf TypeOf other Is Integer Then
                If Page <> other Then Return False
                Return Additional Is Nothing
            Else
                Return False
            End If
        End Function
        Public Shared Operator =(left As PageStackData, right As PageStackData) As Boolean
            Return EqualityComparer(Of PageStackData).Default.Equals(left, right)
        End Operator
        Public Shared Operator <>(left As PageStackData, right As PageStackData) As Boolean
            Return Not left = right
        End Operator
        Public Shared Widening Operator CType(Value As PageType) As PageStackData
            Return New PageStackData With {.Page = Value}
        End Operator
        Public Shared Widening Operator CType(Value As PageStackData) As PageType
            Return Value.Page
        End Operator
    End Class
    Public PageLeft As MyPageLeft, PageRight As MyPageRight

    Private IsChangingPage As Boolean = False

    Public Sub PageChange(Stack As PageStackData, Optional SubType As PageSubType = PageSubType.Default)
        If PageNameGet(Stack) = "" Then

            PageChangeExit()
            IsChangingPage = True

            SetNavChecked(PanTitleSelect, CInt(Stack.Page))
            SetNavChecked(PanTaskbarSelect, CInt(Stack.Page))
            IsChangingPage = False
            Select Case Stack.Page
                Case PageType.Download
                    If FrmDownloadLeft Is Nothing Then FrmDownloadLeft = New PageDownloadLeft
                    CType(FrmDownloadLeft.PanItem.Children(SubType), MyListItem).SetChecked(True, True, Stack = PageCurrent)
                Case PageType.Setup
                    If FrmSetupLeft Is Nothing Then FrmSetupLeft = New PageSetupLeft
                    CType(FrmSetupLeft.PanItem.Children(SubType), MyListItem).SetChecked(True, True, Stack = PageCurrent)
                Case PageType.Other
                    If FrmOtherLeft Is Nothing Then FrmOtherLeft = New PageOtherLeft
                    CType(FrmOtherLeft.PanItem.Children(SubType), MyListItem).SetChecked(True, True, Stack = PageCurrent)
            End Select
            PageChangeActual(Stack, SubType)
        Else

            Select Case Stack.Page
                Case PageType.InstanceSetup
                    If FrmInstanceLeft Is Nothing Then FrmInstanceLeft = New PageInstanceLeft
                    CType(FrmInstanceLeft.PanItem.Children(SubType), MyListItem).SetChecked(True, True, Stack = PageCurrent)
            End Select
            PageChangeActual(Stack, SubType)
        End If
    End Sub

    Private FrmBrowser As PageBrowser = Nothing
    Private Sub ShowBrowserPage(Show As Boolean)
        Try
            If PanBrowserPage Is Nothing OrElse PanBrowserHost Is Nothing Then Return
            If Show Then
                If FrmBrowser Is Nothing Then
                    FrmBrowser = New PageBrowser
                    PanBrowserHost.Children.Clear()
                    PanBrowserHost.Children.Add(FrmBrowser)
                End If
                PanBrowserPage.Visibility = Visibility.Visible
                Logger.Info("[XFL] 已打开内置浏览器页")
            Else
                PanBrowserPage.Visibility = Visibility.Collapsed
                If FrmBrowser IsNot Nothing Then FrmBrowser.OnPageHide()
            End If
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 浏览器页切换失败")
        End Try
    End Sub

    Private FrmMusic As PageMusic = Nothing
    Private Sub ShowMusicPage(Show As Boolean)
        Try
            If PanMusicPage Is Nothing OrElse PanMusicHost Is Nothing Then Return
            If Show Then
                If FrmMusic Is Nothing Then
                    FrmMusic = New PageMusic
                    PanMusicHost.Children.Clear()
                    PanMusicHost.Children.Add(FrmMusic)
                End If
                PanMusicPage.Visibility = Visibility.Visible
                Logger.Info("[XFL] 已打开网易云音乐页")
            Else
                PanMusicPage.Visibility = Visibility.Collapsed
                If FrmMusic IsNot Nothing Then FrmMusic.OnPageHide()
            End If
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 网易云音乐页切换失败")
        End Try
    End Sub

    Private FrmLinkXfl As PageLinkXfl = Nothing
    Private Sub ShowLinkXflPage(Show As Boolean)
        Try
            If PanLinkXflPage Is Nothing OrElse PanLinkXflHost Is Nothing Then Return
            If Show Then
                If FrmLinkXfl Is Nothing Then
                    FrmLinkXfl = New PageLinkXfl
                    PanLinkXflHost.Children.Clear()
                    PanLinkXflHost.Children.Add(FrmLinkXfl)
                End If
                PanLinkXflPage.Visibility = Visibility.Visible
                Logger.Info("[XFL] 已打开联机页")
            Else
                PanLinkXflPage.Visibility = Visibility.Collapsed
                If FrmLinkXfl IsNot Nothing Then FrmLinkXfl.OnPageHide()
            End If

        Catch ex As Exception
            Logger.Error(ex, "[XFL] 联机页切换失败")
        End Try
    End Sub

    Private FrmHorizon As PageHorizon = Nothing
    Private Sub ShowHorizonPage(Show As Boolean)
        Try
            If PanHorizonPage Is Nothing OrElse PanHorizonHost Is Nothing Then Return
            If Show Then
                If FrmHorizon Is Nothing Then
                    FrmHorizon = New PageHorizon
                    PanHorizonHost.Children.Clear()
                    PanHorizonHost.Children.Add(FrmHorizon)
                End If
                PanHorizonPage.Visibility = Visibility.Visible
                Logger.Info("[XFL] 已打开 Horizon 页")
            Else
                PanHorizonPage.Visibility = Visibility.Collapsed
                If FrmHorizon IsNot Nothing Then FrmHorizon.OnPageHide()
            End If
        Catch ex As Exception
            Logger.Error(ex, "[XFL] Horizon 页切换失败")
        End Try
    End Sub

    Private Sub AnimateXflPage(pane As FrameworkElement)
        Try
            If pane Is Nothing Then Return

            If TypeOf pane.RenderTransform IsNot ScaleTransform Then
                pane.RenderTransform = New ScaleTransform(0.96, 0.96)
                pane.RenderTransformOrigin = New Point(0.5, 0.5)
            End If
            pane.Opacity = 0
            AniStart({
                AaScaleTransform(pane, 1 - CType(pane.RenderTransform, ScaleTransform).ScaleX, 250,, New AniEaseOutBack(2)),
                AaOpacity(pane, 1, 100)
            }, "XflPage PageChange")
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 页面切换动画失败")
        End Try
    End Sub

    Private Sub AnimateXflPageHide(pane As FrameworkElement)
        Try
            If pane Is Nothing Then Return
            If pane.Visibility <> Visibility.Visible Then Return
            If TypeOf pane.RenderTransform IsNot ScaleTransform Then
                pane.RenderTransform = New ScaleTransform(1, 1)
                pane.RenderTransformOrigin = New Point(0.5, 0.5)
            End If
            AniStart({
                AaScaleTransform(pane, 0.95 - CType(pane.RenderTransform, ScaleTransform).ScaleX, 110,, New AniEaseInFluent(AniEasePower.Weak)),
                AaOpacity(pane, -pane.Opacity, 80, 30)
            }, "XflPage Hide")
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 页面隐藏动画失败")
        End Try
    End Sub

    Public Shared Sub ApplyWindowSizeMode(mode As Integer)
        Try
            If FrmMain Is Nothing Then Return
            Dim h As Integer = If(mode = 1, 480, 720)
            Settings.Set("WindowHeightMode", mode)

            FrmMain.MinHeight = 480
            FrmMain.Height = h
            ApplyWindowCompactLayout(mode = 1)
            Logger.Info($"[XFL] 窗口高度已切换为 {h}（模式 {mode}）")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 切换窗口尺寸失败")
        End Try
    End Sub

    Public Shared Sub ApplyWindowCompactLayout(compact As Boolean)
        Try
            If FrmLaunchLeft Is Nothing Then Return
            FrmLaunchLeft.SetCompactMode(compact)
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 应用紧凑布局失败")
        End Try
    End Sub

    Public Sub OpenUrlInBrowser(url As String)
        Try
            ShowBrowserPage(True)
            If FrmBrowser IsNot Nothing Then FrmBrowser.NavigateTo(url)
            SetNavChecked(PanTaskbarSelect, CInt(PageType.Browser))

            If BtnTaskSelectBrowser IsNot Nothing Then BtnTaskSelectBrowser.SetChecked(True, False, False)
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 转交浏览器页失败")
        End Try
    End Sub

    Private Sub BtnTitleSelect_Click(sender As MyRadioButton, raiseByMouse As Boolean) Handles BtnTitleSelect0.Check, BtnTitleSelect1.Check, BtnTitleSelect2.Check, BtnTitleSelect3.Check, BtnTitleSelect4.Check,
                                                                                                  BtnTaskSelect0.Check, BtnTaskSelect1.Check, BtnTaskSelect2.Check, BtnTaskSelect3.Check, BtnTaskSelect4.Check,
                                                                                                  BtnTaskSelectLogin.Check,
                                                                                                  BtnTaskSelectBrowser.Check,
                                                                                                  BtnTaskSelectMusic.Check,
                                                                                                  BtnTaskSelectLinkXfl.Check, BtnTaskSelectHorizon.Check
        If IsChangingPage Then Return
        PageChangeActual(Val(sender.Tag))
    End Sub

    Public Sub PageBack() Handles BtnTitleInner.Click
        If PageStack.Any() Then
            PageChangeActual(PageStack(0))
        Else
            PageChange(PageType.Launch)
        End If
    End Sub

    Private Sub PageChangeActual(Stack As PageStackData, Optional SubType As PageSubType = -1)
        If PageCurrent = Stack AndAlso (PageCurrentSub = SubType OrElse SubType = -1) Then Return
        AniControlEnabled += 1
        Try

#Region "子页面处理"
            Dim PageName As String = PageNameGet(Stack)
            If PageName = "" Then

                PageChangeExit()
            Else

                If PageStack.Any Then

                    AniStart({
                        AaOpacity(LabTitleInner, -LabTitleInner.Opacity, 130),
                        AaCode(Sub() LabTitleInner.Text = PageName,, True),
                        AaOpacity(LabTitleInner, 1, 150, 30)
                    }, "FrmMain Titlebar SubLayer")
                    If PageStack.Contains(Stack) Then

                        Do While PageStack.Contains(Stack)
                            PageStack.RemoveAt(0)
                        Loop
                    Else

                        PageStack.Insert(0, PageCurrent)
                    End If
                Else

                    PanTitleInner.Visibility = Visibility.Visible
                    PanTitleMain.IsHitTestVisible = False
                    PanTitleInner.IsHitTestVisible = True
                    PageNameRefresh(Stack)
                    AniStart({
                        AaOpacity(PanTitleMain, -PanTitleMain.Opacity, 150),
                        AaX(PanTitleMain, 12 - PanTitleMain.Margin.Left, 150,, New AniEaseInFluent(AniEasePower.Weak)),
                        AaOpacity(PanTitleInner, 1 - PanTitleInner.Opacity, 150, 200),
                        AaX(PanTitleInner, -PanTitleInner.Margin.Left, 350, 200, New AniEaseOutBack),
                        AaCode(Sub() PanTitleMain.Visibility = Visibility.Collapsed,, True)
                    }, "FrmMain Titlebar FirstLayer")
                    PageStack.Insert(0, PageCurrent)
                End If
            End If
#End Region

#Region "实际更改页面框架 UI"
            PageLast = PageCurrent
            PageCurrent = Stack
            Select Case Stack.Page
                Case PageType.Launch
                    PageChangeAnim(FrmLaunchLeft, FrmLaunchRight)
                Case PageType.Download
                    If FrmDownloadLeft Is Nothing Then FrmDownloadLeft = New PageDownloadLeft

                    PageChangeAnim(FrmDownloadLeft, FrmDownloadLeft.PageGet(SubType))
                Case PageType.Link
                    If FrmLinkMain Is Nothing Then FrmLinkMain = New PageLinkMain
                    PageChangeAnim(New MyPageLeft, FrmLinkMain)
                Case PageType.Setup
                    If FrmSetupLeft Is Nothing Then FrmSetupLeft = New PageSetupLeft
                    PageChangeAnim(FrmSetupLeft, FrmSetupLeft.PageGet(SubType))
                Case PageType.Other
                    If FrmOtherLeft Is Nothing Then FrmOtherLeft = New PageOtherLeft
                    PageChangeAnim(FrmOtherLeft, FrmOtherLeft.PageGet(SubType))
                Case PageType.InstanceSelect
                    If FrmSelectLeft Is Nothing Then FrmSelectLeft = New PageSelectLeft
                    If FrmSelectRight Is Nothing Then FrmSelectRight = New PageSelectRight
                    PageChangeAnim(FrmSelectLeft, FrmSelectRight)
                Case PageType.DownloadManager
                    If FrmSpeedLeft Is Nothing Then FrmSpeedLeft = New PageSpeedLeft
                    If FrmSpeedRight Is Nothing Then FrmSpeedRight = New PageSpeedRight
                    PageChangeAnim(FrmSpeedLeft, FrmSpeedRight)
                Case PageType.InstanceSetup
                    If FrmInstanceLeft Is Nothing Then FrmInstanceLeft = New PageInstanceLeft
                    PageChangeAnim(FrmInstanceLeft, FrmInstanceLeft.PageGet(SubType))
                Case PageType.ResourceDetail
                    If FrmDownloadResourceDetail Is Nothing Then FrmDownloadResourceDetail = New PageDownloadResourceDetail
                    PageChangeAnim(New MyPageLeft, FrmDownloadResourceDetail)
                Case PageType.HelpDetail
                    PageChangeAnim(New MyPageLeft, Stack.Additional(1))
                Case PageType.Login
                    ShowLoginPage(True)
                    AnimateXflPage(PanLoginPage)

                    Try
                        If PlayerPreview IsNot Nothing Then PlayerPreview.SetVisible(True)
                    Catch ex As Exception
                        Logger.Warn(ex, "[XFL] 显示 3D 预览空域失败")
                    End Try
                Case PageType.Browser
                    ShowBrowserPage(True)
                    AnimateXflPage(PanBrowserPage)
                Case PageType.Music
                    ShowMusicPage(True)
                    AnimateXflPage(PanMusicPage)
                Case PageType.LinkXfl
                    ShowLinkXflPage(True)
                    AnimateXflPage(PanLinkXflPage)
                Case PageType.Horizon
                    ShowHorizonPage(True)
                    AnimateXflPage(PanHorizonPage)
            End Select
#End Region

#Region "设置为最新状态"
            BtnExtraDownload.ShowRefresh()
            BtnExtraApril.ShowRefresh()
#End Region

            If Stack.Page <> PageType.Login Then
                AnimateXflPageHide(PanLoginPage)
                ShowLoginPage(False)

                Try
                    If PlayerPreview IsNot Nothing Then PlayerPreview.SetVisible(False)
                Catch ex As Exception
                    Logger.Warn(ex, "[XFL] 隐藏 3D 预览空域失败")
                End Try
            End If
            If Stack.Page <> PageType.Browser Then
                AnimateXflPageHide(PanBrowserPage)
                ShowBrowserPage(False)
            End If
            If Stack.Page <> PageType.Music Then
                AnimateXflPageHide(PanMusicPage)
                ShowMusicPage(False)
            End If

            Try
                If Stack.Page = PageType.Launch Then
                    If Settings.Get(Of String)("AiProvider") = "deepseek" Then ModDeepSeek.SetVisible(True)
                Else
                    ModDeepSeek.SetVisible(False)
                End If
            Catch ex As Exception
                Logger.Warn(ex, "[XFL] 同步 DeepSeek 空域可见性失败")
            End Try
            If Stack.Page <> PageType.LinkXfl Then ShowLinkXflPage(False)
            If Stack.Page <> PageType.Horizon Then ShowHorizonPage(False)
            Logger.Info($"切换主要页面：{Stack}, {SubType}")
        Catch ex As Exception
            Logger.Error(ex, GetLang("LangDialogFailedToChangePage", PageCurrent.Page))
        Finally
            AniControlEnabled -= 1
        End Try
    End Sub
    Private Sub PageChangeAnim(TargetLeft As FrameworkElement, TargetRight As FrameworkElement)
        AniStop("FrmMain LeftChange")
        AniStop("PageLeft PageChange")
        AniControlEnabled += 1

        If Not IsNothing(TargetLeft.Parent) Then TargetLeft.SetValue(ContentPresenter.ContentProperty, Nothing)
        If Not IsNothing(TargetRight) AndAlso Not IsNothing(TargetRight.Parent) Then TargetRight.SetValue(ContentPresenter.ContentProperty, Nothing)
        PageLeft = TargetLeft
        PageRight = TargetRight

        CType(PanMainLeft.Child, MyPageLeft).TriggerHideAnimation()
        CType(PanMainRight.Child, MyPageRight).PageOnExit()
        AniControlEnabled -= 1

        AniStart({
            AaCode(
            Sub()
                AniControlEnabled += 1

                PanMainLeft.Child = PageLeft
                PageLeft.Opacity = 0
                PanMainLeft.Background = Nothing
                AniControlEnabled -= 1
                RunInUi(Sub() PanMainLeft_Resize(PanMainLeft.ActualWidth), True)
            End Sub, 40),
            AaCode(
            Sub()

                PageLeft.Opacity = 1
                PageLeft.TriggerShowAnimation()
            End Sub, 30, True)
        }, "FrmMain PageChangeLeft")
        AniStart({
            AaCode(
            Sub()
                AniControlEnabled += 1
                CType(PanMainRight.Child, MyPageRight).PageOnForceExit()

                PanMainRight.Child = PageRight
                PageRight.Opacity = 0
                PanMainRight.Background = Nothing
                AniControlEnabled -= 1
                RunInUi(Sub() BtnExtraBack.ShowRefresh(), True)
            End Sub, 40),
            AaCode(
            Sub()

                PageRight.Opacity = 1
                PageRight.PageOnEnter()
            End Sub, 30, True)
        }, "FrmMain PageChangeRight")
    End Sub

    Private Sub PageChangeExit()
        If PageStack.Any Then

            PanTitleMain.Visibility = Visibility.Visible
            PanTitleMain.IsHitTestVisible = True
            PanTitleInner.IsHitTestVisible = False
            AniStart({
                AaOpacity(PanTitleInner, -PanTitleInner.Opacity, 150),
                AaX(PanTitleInner, -18 - PanTitleInner.Margin.Left, 150,, New AniEaseInFluent),
                AaOpacity(PanTitleMain, 1 - PanTitleMain.Opacity, 150, 200),
                AaX(PanTitleMain, -PanTitleMain.Margin.Left, 350, 200, New AniEaseOutBack(AniEasePower.Weak)),
                AaCode(Sub() PanTitleInner.Visibility = Visibility.Collapsed,, True)
            }, "FrmMain Titlebar FirstLayer")
            PageStack.Clear()
        Else

        End If
    End Sub

    Private Sub PanMainLeft_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles PanMainLeft.SizeChanged
        If Not e.WidthChanged Then Return
        PanMainLeft_Resize(e.NewSize.Width)
    End Sub
    Private Sub PanMainLeft_Resize(NewWidth As Double)
        Dim Delta As Double = NewWidth - RectLeftBackground.Width
        If Math.Abs(Delta) > 0.1 AndAlso AniControlEnabled = 0 Then
            If PanMain.Opacity < 0.1 Then PanMainLeft.IsHitTestVisible = False
            If NewWidth > 0 Then

                AniStart({
                    AaWidth(RectLeftBackground, NewWidth - RectLeftBackground.Width, 180,, New AniEaseOutFluent(AniEasePower.ExtraStrong)),
                    AaCode(Sub() PanMainLeft.IsHitTestVisible = True, 150)
                }, "FrmMain LeftChange", True)
            Else

                AniStart({
                    AaWidth(RectLeftBackground, -RectLeftBackground.Width, 180,, New AniEaseOutFluent),
                    AaCode(Sub() PanMainLeft.IsHitTestVisible = True, 150)
                }, "FrmMain LeftChange", True)
            End If
        Else
            RectLeftBackground.Width = NewWidth
            PanMainLeft.IsHitTestVisible = True
            AniStop("FrmMain LeftChange")
        End If
    End Sub

#End Region

#Region "控件拖动"

    Public Sub DragTick()
        If DragControl Is Nothing Then Return
        If Not Mouse.LeftButton = MouseButtonState.Pressed Then
            DragStop()
        End If
    End Sub

    Public Sub DragDoing() Handles PanBack.MouseMove
        If DragControl Is Nothing Then Return
        If Mouse.LeftButton = MouseButtonState.Pressed Then
            DragControl.DragDoing()
        Else
            DragStop()
        End If
    End Sub
    Public Sub DragStop()

        RunInUi(Sub()
                    If DragControl Is Nothing Then Return
                    Dim Control = DragControl
                    DragControl = Nothing
                    Control.DragStop()
                End Sub)
    End Sub

#End Region

#Region "附加按钮"

    Private Sub BtnExtraMusic_Click(sender As Object, e As EventArgs) Handles BtnExtraMusic.Click
        MusicControlPause()
    End Sub
    Private Sub BtnExtraMusic_RightClick(sender As Object, e As EventArgs) Handles BtnExtraMusic.RightClick
        MusicControlNext()
    End Sub

    Private Sub BtnExtraDownload_Click(sender As Object, e As EventArgs) Handles BtnExtraDownload.Click
        PageChange(PageType.DownloadManager)
    End Sub
    Private Function BtnExtraDownload_ShowCheck() As Boolean
        Return HasDownloadingTask() AndAlso Not PageCurrent = PageType.DownloadManager
    End Function

    Public Sub AprilGiveup() Handles BtnExtraApril.Click
        If IsAprilEnabled AndAlso Not IsAprilGiveup Then
            Hint("=D", HintType.Green)
            IsAprilGiveup = True
            FrmLaunchLeft.AprilScaleTrans.ScaleX = 1
            FrmLaunchLeft.AprilScaleTrans.ScaleY = 1
            BtnExtraApril.ShowRefresh()
        End If
    End Sub
    Public Function BtnExtraApril_ShowCheck() As Boolean
        Return IsAprilEnabled AndAlso Not IsAprilGiveup AndAlso PageCurrent = PageType.Launch
    End Function

    Public Sub BtnExtraShutdown_Click() Handles BtnExtraShutdown.Click
        Try
            If McLaunchLoaderReal IsNot Nothing Then McLaunchLoaderReal.Cancel()
            For Each Watcher In McWatcherList
                Watcher.Kill()
            Next
            Hint(GetLang("LangHintCloseMinecraftSuccess"), HintType.Green)
        Catch ex As Exception
            Logger.Error(ex, GetLang("LangHintCloseMinecraftFailed"))
        End Try
    End Sub
    Public Function BtnExtraShutdown_ShowCheck() As Boolean
        Return HasRunningMinecraft
    End Function

    Public Sub BackToTop() Handles BtnExtraBack.Click
        Dim RealScroll As MyScrollViewer = BtnExtraBack_GetRealChild()
        If RealScroll IsNot Nothing Then
            RealScroll.PerformVerticalOffsetDelta(-RealScroll.VerticalOffset)
        Else
            Logger.Error("" + GetLang("LangHintBackToTopFailed"), LogBehavior.Toast)
        End If
    End Sub
    Private Function BtnExtraBack_ShowCheck() As Boolean
        Dim RealScroll As MyScrollViewer = BtnExtraBack_GetRealChild()
        Return RealScroll IsNot Nothing AndAlso RealScroll.Visibility = Visibility.Visible AndAlso RealScroll.VerticalOffset > Height + If(BtnExtraBack.Show, 0, 700)
    End Function
    Private Function BtnExtraBack_GetRealChild() As MyScrollViewer
        If PanMainRight.Child Is Nothing OrElse TypeOf PanMainRight.Child IsNot MyPageRight Then Return Nothing
        Dim Page As MyPageRight = PanMainRight.Child
        Return Page.FindName(Page.PanScroll)
    End Function

#End Region

    Public lastMouseArg As MouseEventArgs = Nothing
    Private Sub FormMain_MouseMove(sender As Object, e As MouseEventArgs) Handles Me.MouseMove
        lastMouseArg = e
    End Sub

    Private Sub FormMain_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles Me.SizeChanged
        Try
            If IsWindowLoadFinished AndAlso FrmLaunchLeft IsNot Nothing Then
                FrmLaunchLeft.XflStretchToWindow()
            End If
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 主页宽度自适应失败")
        End Try
    End Sub

    Public Sub SetNavChecked(Panel As Panel, TagValue As Integer)
        Try
            If Panel Is Nothing Then Return
            Dim found As Integer = 0
            For Each btn As MyRadioButton In XflDescendants(Of MyRadioButton)(Panel)
                found += 1

                btn.SetChecked(Val(If(btn.Tag, "").ToString) = TagValue, False, False)
            Next
            Logger.Info($"[XFL] 导航高亮同步：面板={Panel.Name} 目标Tag={TagValue} 命中按钮={found}")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 导航勾选同步失败")
        End Try
    End Sub

    Private Iterator Function XflDescendants(Of T As DependencyObject)(Root As DependencyObject) As IEnumerable(Of T)
        If Root Is Nothing Then Return
        Dim hit As T = TryCast(Root, T)
        If hit IsNot Nothing Then Yield hit
        Dim n As Integer = VisualTreeHelper.GetChildrenCount(Root)
        For i As Integer = 0 To n - 1
            For Each child As T In XflDescendants(Of T)(VisualTreeHelper.GetChild(Root, i))
                Yield child
            Next
        Next
    End Function

    Public Shared Sub ApplyTaskbarAlignSetting(Value As Object)
        Try
            If FrmMain Is Nothing OrElse FrmMain.PanTaskbarSelect Is Nothing Then Return
            Dim align As Integer = If(Value Is Nothing, 1, CInt(Value))
            If align = 0 Then
                FrmMain.PanTaskbarStack.HorizontalAlignment = HorizontalAlignment.Center
                FrmMain.PanTaskbarStack.Margin = New Thickness(0)
            Else
                FrmMain.PanTaskbarStack.HorizontalAlignment = HorizontalAlignment.Left
                FrmMain.PanTaskbarStack.Margin = New Thickness(12, 0, 0, 0)
            End If
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 任务栏对齐设置失败")
        End Try
    End Sub

    Public Sub InitXflTaskbar()
        Try
            Dim cnt As Integer = 0
            For Each btn As MyRadioButton In XflDescendants(Of MyRadioButton)(PanTaskbarSelect)

                btn.MinHeight = 46
                btn.MaxHeight = 46
                btn.Width = 46
                btn.Height = 46
                btn.CornerRadius = New CornerRadius(13)

                btn.LogoMargin = New Thickness(15, 0, 15, 0)
                btn.TextMargin = New Thickness(0)

                btn.Margin = New Thickness(11, 0, 11, 0)
                cnt += 1
            Next
            ApplyTaskbarAlignSetting(Settings.Get(Of Integer)("UiTaskbarAlign"))
            Logger.Info($"[XFL] 任务栏图标圆角与对齐已应用（按钮 {cnt} 个）")

            ModXflTaskbarDrag.Hook()
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 任务栏初始化失败")
        End Try
    End Sub

End Class
