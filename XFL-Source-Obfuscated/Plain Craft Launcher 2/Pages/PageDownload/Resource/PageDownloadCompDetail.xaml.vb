Imports System.Collections.ObjectModel

Public Class PageDownloadResourceDetail
    Private ResourceItem As MyResourceItem = Nothing

    Private TargetResourceType As ResourceTypes
    Private Project As ResourceProject
    Private TargetVersion As String
    Private TargetLoaders As ModLoaders
    Public Sub LoadPageArguments() Handles Me.Loaded
        Project = FrmMain.PageCurrent.Additional(0)
        TargetVersion = FrmMain.PageCurrent.Additional(2)
        TargetLoaders = CType(FrmMain.PageCurrent.Additional(3), ModLoaders)
        If TargetLoaders <> ModLoaders.None AndAlso Not TargetLoaders.Flags.IsSingle Then TargetLoaders = TargetLoaders.Flags.First()
        TargetResourceType = FrmMain.PageCurrent.Additional(4)
    End Sub

#Region "加载器"

    Private ResourceFileLoader As New LoaderTask(Of Integer, List(Of ResourceVersion))(
        "Resource File",
        Sub(Task)
            LoadPageArguments()
            Dim Result = ResourceVersion.FromProjectId(Project.Id, Project.Platform)
            If Task.IsCanceled Then Return
            Task.Output = Result
        End Sub)

    Private Sub PageDownloadResourceDetail_Inited(sender As Object, e As EventArgs) Handles Me.Initialized
        LoadPageArguments()
        PageLoaderInit(Load, PanLoad, PanMain, CardIntro, ResourceFileLoader, AddressOf Load_OnFinish)
    End Sub

    Private Sub Load_State(sender As Object, state As MyLoading.MyLoadingState, oldState As MyLoading.MyLoadingState) Handles Load.StateChanged
        Select Case ResourceFileLoader.State
            Case LoadState.Failed
                Dim ErrorMessage As String = ""
                If ResourceFileLoader.Error IsNot Nothing Then ErrorMessage = ResourceFileLoader.Error.Message
                If ErrorMessage.Contains("不是有效的 json 文件") Then
                    Logger.Warn("下载的文件 json 列表损坏，已自动重试")
                    PageLoaderRestart()
                End If
        End Select
    End Sub

    Private Class CardSorter
        Implements IComparer(Of String)
        Public Topmost As String = ""
        Public Function Compare(x As String, y As String) As Integer Implements IComparer(Of String).Compare

            If x = y Then Return 0

            If x = Topmost Then Return -1
            If y = Topmost Then Return 1

            Dim IsXSpecial As Boolean = Not x.Contains(".")
            Dim IsYSpecial As Boolean = Not y.Contains(".")
            If IsXSpecial AndAlso IsYSpecial Then Return x.CompareTo(y)
            If IsXSpecial Then Return 1
            If IsYSpecial Then Return -1

            Dim VersionCodeSort = -CompareVersion(x.Replace(x.BeforeFirst(" ") & " ", ""), y.Replace(y.BeforeFirst(" ") & " ", ""))
            If VersionCodeSort <> 0 Then Return VersionCodeSort

            Return -CompareVersion(x, y)
        End Function
        Public Sub New(Optional Topmost As String = "")
            Me.Topmost = If(Topmost, "")
        End Sub
    End Class

    Private VersionFilter As String
    Private GroupedDrop As Boolean
    Private GroupedOld As Boolean

    Private Function GetResults() As List(Of ResourceVersion)
        Dim Results As List(Of ResourceVersion) = ResourceFileLoader.Output
        Select Case TargetResourceType
            Case ResourceTypes.Any
                Results = Results.Where(Function(r) r.ResourceType <> ResourceTypes.Plugin).ToList
            Case ResourceTypes.Shader, ResourceTypes.ResourcePack

            Case Else
                Results = Results.Where(Function(r) r.ResourceType = TargetResourceType).ToList
        End Select
        Return Results
    End Function
    Private Sub Load_OnFinish()
        Dim Results = GetResults()

        Dim Filters As List(Of String) = Nothing
        Dim UpdateFilters =
        Sub()
            Filters = Results.SelectMany(Function(v) v.GameVersions).Select(Function(v) GetGroupedVersionName(v, GroupedDrop, GroupedOld)).
                Distinct.OrderByDescending(Function(s) s, New VersionComparer).ToList
        End Sub

        GroupedDrop = False : GroupedOld = False
        UpdateFilters()
        If Filters.Count < 9 Then GoTo GroupDone
        GroupedDrop = True : GroupedOld = False
        UpdateFilters()
        If Filters.Count < 9 Then GoTo GroupDone
        GroupedDrop = False : GroupedOld = True
        UpdateFilters()
        If Filters.Count < 9 Then GoTo GroupDone
        GroupedDrop = True : GroupedOld = True
        UpdateFilters()
GroupDone:

        PanFilter.Children.Clear()
        If Filters.Count < 2 Then
            CardFilter.Visibility = Visibility.Collapsed
            VersionFilter = Nothing
        Else
            CardFilter.Visibility = Visibility.Visible
            Filters.Insert(0, "全部")

            For Each Version As String In Filters
                Dim NewButton As New MyRadioButton With {
                    .Text = Version, .Margin = New Thickness(2, 0, 2, 0), .ColorType = MyRadioButton.ColorState.Highlight}
                NewButton.LabText.Margin = New Thickness(-2, 0, 10, 0)
                AddHandler NewButton.Check,
                Sub(sender As MyRadioButton, raiseByMouse As Boolean)
                    VersionFilter = If(sender.Text = "全部", Nothing, sender.Text)
                    UpdateFilterResult()
                End Sub
                PanFilter.Children.Add(NewButton)
            Next

            Dim ToCheck As MyRadioButton = Nothing
            If TargetVersion <> "" Then
                Dim TargetFile = Results.FirstOrDefault(Function(v) v.GameVersions.Contains(TargetVersion))
                If TargetFile IsNot Nothing Then
                    Dim TargetGroup = GetGroupedVersionName(TargetVersion, GroupedDrop, GroupedOld)
                    For Each Button As MyRadioButton In PanFilter.Children
                        If Button.Text <> TargetGroup Then Continue For
                        ToCheck = Button
                        Exit For
                    Next
                End If
            End If
            If ToCheck Is Nothing Then ToCheck = PanFilter.Children(0)
            ToCheck.Checked = True
        End If

        UpdateFilterResult()
    End Sub
    Private Sub UpdateFilterResult()
        Dim Results = GetResults()

        Dim TargetCardName As String = If(TargetVersion <> "" OrElse TargetLoaders <> ModLoaders.None,
            GetLang("LangDownloadCompSelectedVersion") & $"{If(TargetLoaders <> ModLoaders.None, TargetLoaders.ToString + " ", "")}{TargetVersion}", "")

        Dim Dict As New SortedDictionary(Of String, List(Of ResourceVersion))(New CardSorter(TargetCardName))
        Dict.Add("其他", New List(Of ResourceVersion))
        For Each Version As ResourceVersion In Results
            For Each GameVersion In Version.GameVersions

                If VersionFilter IsNot Nothing AndAlso
                    GetGroupedVersionName(GameVersion, GroupedDrop, GroupedOld) <> VersionFilter Then Continue For

                Dim VerName As String = GetGroupedVersionName(GameVersion, False, False)

                Dim Loaders As New List(Of String)
                If Project.ModLoaders.Flags.Count > 1 AndAlso
                    Version.ResourceType = ResourceTypes.Mod AndAlso
                    McVersion.IsFormatFit(VerName) Then
                    For Each Loader In Version.ModLoaders.Flags
                        If Loader = ModLoaders.Quilt AndAlso Settings.Get(Of Boolean)("ToolDownloadIgnoreQuilt") Then Continue For
                        Loaders.Add(Loader.ToString & " ")
                    Next
                End If
                If Not Loaders.Any() Then Loaders.Add("")

                For Each Loader In Loaders
                    Dim TargetCard As String = Loader & VerName
                    If Not Dict.ContainsKey(TargetCard) Then Dict.Add(TargetCard, New List(Of ResourceVersion))
                    If Not Dict(TargetCard).Contains(Version) Then Dict(TargetCard).Add(Version)
                Next
            Next
        Next

        If TargetCardName <> "" AndAlso (VersionFilter Is Nothing OrElse GetGroupedVersionName(TargetVersion, GroupedDrop, GroupedOld).StartsWithF(VersionFilter)) Then
            Dict.Add(TargetCardName, New List(Of ResourceVersion))
            For Each Version As ResourceVersion In Results
                If Version.GameVersions.Contains(TargetVersion) AndAlso
                   (TargetLoaders = ModLoaders.None OrElse TargetLoaders.Flags.Intersect(Version.ModLoaders.Flags).Any) Then

                    If VersionFilter IsNot Nothing AndAlso
                        Not Version.GameVersions.Any(Function(v) GetGroupedVersionName(v, GroupedDrop, GroupedOld) = VersionFilter) Then Continue For
                    If Not Dict(TargetCardName).Contains(Version) Then Dict(TargetCardName).Add(Version)
                End If
            Next
        End If

        Try
            PanResults.Children.Clear()
            For Each Pair As KeyValuePair(Of String, List(Of ResourceVersion)) In Dict
                If Not Pair.Value.Any() Then Continue For
                If Pair.Key = TargetCardName.Replace("（所选版本）", "") Then Continue For

                Dim NewCard As New MyCard With {.Title = Pair.Key, .Margin = New Thickness(0, 0, 0, 15), .SwapType = If(TargetResourceType = ResourceTypes.ModPack, 9, 8)}
                Dim NewStack As New StackPanel With {.Margin = New Thickness(20, MyCard.SwapedHeight, 18, 0), .VerticalAlignment = VerticalAlignment.Top, .RenderTransform = New TranslateTransform(0, 0), .Tag = Pair.Value}
                NewCard.Children.Add(NewStack)
                NewCard.SwapControl = NewStack
                PanResults.Children.Add(NewCard)

                If Pair.Key = TargetCardName OrElse
                   (FrmMain.PageCurrent.Additional IsNot Nothing AndAlso
                   CType(FrmMain.PageCurrent.Additional(1), List(Of String)).Contains(NewCard.Title)) Then
                    MyCard.StackInstall(NewStack, If(TargetResourceType = ResourceTypes.ModPack, 9, 8), Pair.Key)
                Else
                    NewCard.IsSwapped = True
                End If

                If Pair.Key = "其他" Then
                    NewStack.Children.Add(New MyHint With {.Text = GetLang("LangDownloadCompApiSlow"), .Theme = MyHint.Themes.Yellow, .Margin = New Thickness(5, 0, 0, 8)})
                End If
            Next

            If PanResults.Children.Count = 1 Then
                CType(PanResults.Children(0), MyCard).IsSwapped = False
            End If

            If Project.Types = ResourceTypes.ModOrDataPack AndAlso (TargetResourceType = ResourceTypes.Mod OrElse TargetResourceType = ResourceTypes.DataPack) Then
                HintAlternative.Visibility = Visibility.Visible
                HintAlternative.Text = If(TargetResourceType = ResourceTypes.Mod,
                    GetLang("LangDownloadCompHintAlternativeMod"), GetLang("LangDownloadCompHintAlternativeDataPack"))
            Else
                HintAlternative.Visibility = Visibility.Collapsed
            End If
        Catch ex As Exception
            Logger.Error(ex, "可视化工程下载列表出错")
        End Try
    End Sub
    Private Function GetGroupedVersionName(Name As String, GroupedByDrop As Boolean, FoldOld As Boolean) As String
        Dim Drop = McVersion.VersionToDrop(Name)
        If Name Is Nothing Then
            Return "其他"
        ElseIf Name.Contains("w") OrElse Drop = 209 Then
            Return "快照版"
        ElseIf Not McVersion.IsFormatFit(Name) OrElse (FoldOld AndAlso Drop < 120) Then
            Return "远古版"
        ElseIf GroupedByDrop Then
            Return McVersion.DropToVersion(Drop)
        Else
            Return Name
        End If
    End Function

#End Region

    Private IsFirstInit As Boolean = True
    Public Sub Init() Handles Me.PageEnter
        AniControlEnabled += 1
        Project = FrmMain.PageCurrent.Additional(0)
        PanBack.ScrollToHome()

        If IsFirstInit Then

            IsFirstInit = False
        Else
            PageLoaderRestart(IsForceRestart:=True)
        End If

        If ResourceItem IsNot Nothing Then PanIntro.Children.Remove(ResourceItem)
        ResourceItem = Project.ToResourceItem(True, True).Init()
        ResourceItem.CanInteraction = False
        ResourceItem.Margin = New Thickness(-7, -7, 0, 8)
        PanIntro.Children.Insert(0, ResourceItem)

        BtnIntroWeb.Text = If(Project.Platform = ResourcePlatforms.CurseForge, GetLang("LangDownloadCompToCurseForge"), GetLang("LangDownloadCompToModrinth"))
        BtnIntroWiki.Visibility = If(Project.WikiId = 0, Visibility.Collapsed, Visibility.Visible)

        AniControlEnabled -= 1
    End Sub

    Public Sub Install_Click(sender As MyListItem, e As EventArgs)
        Try

            Dim File As ResourceVersion = sender.Tag
            Dim LoaderName As String = $"{Project.Platform} {GetLang("LangDownloadCompDownloadModpack")}{Project.TranslatedName} "

            Dim PackName As String = Project.TranslatedName.Replace(".zip", "").Replace(".rar", "").Replace(".mrpack", "").Replace("\", "＼").Replace("/", "／").Replace("|", "｜").Replace(":", "：").Replace("<", "＜").Replace(">", "＞").Replace("*", "＊").Replace("?", "？").Replace("""", "").Replace("： ", "：")
            Dim Validate As New ValidateFolderName(McFolderSelected & "versions")
            If Validate.Validate(PackName) <> "" Then PackName = ""
            Dim InstanceName As String = MyMsgBoxInput(GetLang("LangDownloadCompInputInstanceName"), "", PackName, New Collection(Of Validate) From {Validate})
            If String.IsNullOrEmpty(InstanceName) Then Return

            Dim Loaders As New List(Of LoaderBase)
            Dim Target As String = $"{McFolderSelected}versions\{InstanceName}\原始整合包.{If(Project.Platform = ResourcePlatforms.CurseForge, "zip", "mrpack")}"
            Dim LogoFileAddress As String = ResourceItem.PathLogo.ActualSource
            Loaders.Add(New LoaderDownload(GetLang("LangDownloadCompTaskDownloadModpackFile"), {
                File.ToNetFile(Target, ResourceVersion.DownloadReason.Standalone, File.GameVersions.FirstOrDefault, File.ModLoaders)
            }) With {.ProgressWeight = 10, .Block = True})
            Loaders.Add(New LoaderTask(Of Integer, Integer)(GetLang("LangDownloadCompTaskPrepareInstallModpack"),
            Sub() ModpackInstall(Target, InstanceName, If(FileUtils.Exists(LogoFileAddress), LogoFileAddress, Nothing))) With {.ProgressWeight = 0.1})

            Dim Loader As New LoaderCombo(Of String)(LoaderName, Loaders) With {.OnStateChanged =
            Sub(MyLoader)
                Select Case MyLoader.State
                    Case LoadState.Failed
                        Hint(MyLoader.Name & GetLang("LangPageVersionOverallCompleteFileFail") & MyLoader.Error.GetDisplay(False), HintType.Red)
                    Case LoadState.Canceled
                        Hint(MyLoader.Name & GetLang("LangTaskAbort"), HintType.Blue)
                    Case LoadState.Loading
                        Return
                End Select
                McInstallFailedClearFolder(MyLoader)
            End Sub}
            Loader.Start(McFolderSelected & "versions\" & InstanceName & "\")
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()

        Catch ex As Exception
            Logger.Error(ex, "下载资源整合包失败")
        End Try
    End Sub

    Public Shared CachedFolder As New Dictionary(Of ResourceTypes, String)
    Public Sub Save_Click(sender As Object, e As EventArgs)
        Dim File As ResourceVersion = If(TypeOf sender Is MyListItem, sender, sender.Parent).Tag
        RunInNewThread(
        Sub()
            Try
                Dim Desc As String = Nothing
                Select Case File.ResourceType
                    Case ResourceTypes.ModPack : Desc = GetLang("LangDownloadPageLeftModpack")
                    Case ResourceTypes.Mod : Desc = GetLang("LangDownloadPageLeftMods")
                    Case ResourceTypes.ResourcePack : Desc = GetLang("LangDownloadPageLeftResourcepack")
                    Case ResourceTypes.Shader : Desc = GetLang("LangDownloadPageLeftShader")
                    Case ResourceTypes.DataPack : Desc = GetLang("LangDownloadPageLeftDatapack")
                End Select

                Dim DefaultFolder As String = Nothing
                If File.ResourceType <> ResourceTypes.ModPack Then
                    Dim SubFolder As String = Nothing
                    Select Case File.ResourceType
                        Case ResourceTypes.Mod : SubFolder = "mods\"
                        Case ResourceTypes.ResourcePack : SubFolder = "resourcepacks\"
                        Case ResourceTypes.Shader : SubFolder = "shaderpacks\"
                        Case ResourceTypes.DataPack : SubFolder = ""
                    End Select
                    Dim IsInstanceSuitable As Func(Of McInstance, Boolean) = Nothing

                    Dim AllowedLoaders As ModLoaders = ModLoaders.None
                    If File.ModLoaders <> ModLoaders.None Then
                        AllowedLoaders = File.ModLoaders
                    ElseIf Project.ModLoaders <> ModLoaders.None Then
                        AllowedLoaders = Project.ModLoaders
                    End If
                    Logger.Info($"{Desc}要求的加载器种类：{AllowedLoaders}")

                    IsInstanceSuitable =
                    Function(Instance As McInstance)
                        If Not Instance.IsLoaded Then Instance.Load()

                        If File.ResourceType = ResourceTypes.Mod OrElse File.ResourceType = ResourceTypes.DataPack Then
                            If Not File.GameVersions.Any(
                            Function(FileVersion)
                                Dim FileIsSnapshot As Boolean = FileVersion.Contains("预览版") OrElse Not FileVersion.Contains(".")
                                Dim InstanceIsSnapshot As Boolean = Instance.Version.VanillaName.Contains("snapshot") OrElse Not Instance.Version.VanillaName.Contains(".")
                                If FileIsSnapshot <> InstanceIsSnapshot Then

                                    Return False
                                ElseIf Not FileIsSnapshot AndAlso Not InstanceIsSnapshot Then

                                    Return FileVersion = Instance.Version.VanillaName
                                Else

                                    Dim FileIsNewFormat As Boolean = FileVersion.Contains(".") AndAlso Val(FileVersion.BeforeFirst(".")) > 1
                                    Dim InstanceIsNewFormat As Boolean = Instance.Version.Drop > 250
                                    If FileIsNewFormat <> InstanceIsNewFormat Then

                                        Return False
                                    ElseIf FileIsNewFormat AndAlso InstanceIsNewFormat Then

                                        Return FileVersion.BeforeFirst(" ") = Instance.Version.VanillaName.BeforeFirst("-")
                                    Else

                                        If FileVersion.Contains("w") AndAlso Instance.Version.VanillaName.Contains("w") Then

                                            Return FileVersion = Instance.Version.VanillaName
                                        Else

                                            Return True
                                        End If
                                    End If
                                End If
                            End Function) Then Return False
                        End If

                        Return AllowedLoaders = ModLoaders.None OrElse AllowedLoaders.Flags.Intersect(Instance.Version.ModLoaders.Flags).Any
                    End Function

                    If CachedFolder.ContainsKey(File.ResourceType) Then
                        DefaultFolder = CachedFolder(File.ResourceType)
                        Logger.Info($"使用上次下载时的文件夹作为默认下载位置：{DefaultFolder}")
                    ElseIf McInstanceSelected IsNot Nothing AndAlso IsInstanceSuitable(McInstanceSelected) Then
                        DefaultFolder = $"{McInstanceSelected.PathIndie}{SubFolder}"
                        DirectoryUtils.Create(DefaultFolder)
                        Logger.Info($"使用当前版本作为默认下载位置：{DefaultFolder}")
                    Else

                        Dim NeedLoad As Boolean = McInstanceListLoader.State <> LoadState.Finished
                        If NeedLoad Then
                            Hint(GetLang("LangDownloadCompSearchingSuitableInstance"))
                            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\", WaitForExit:=True)
                        End If
                        Dim SuitableInstanceList = McInstanceList.Values.SelectMany(Function(l) l).Where(Function(v) IsInstanceSuitable(v)).
                            Select(Function(v) DirectoryUtils.GetInfo($"{v.PathIndie}{SubFolder}"))
                        If SuitableInstanceList.Any Then
                            Dim SelectedInstance = SuitableInstanceList.
                                OrderByDescending(Function(Dir) If(Dir.Exists, Dir.LastWriteTimeUtc, Date.MinValue)).
                                ThenByDescending(Function(Dir) If(Dir.Exists, Dir.GetFiles().Length, -1)).
                                First()
                            DefaultFolder = PathUtils.RemoveExtendedPrefix(SelectedInstance.FullName)
                            DirectoryUtils.Create(DefaultFolder)
                            Logger.Info($"使用适合的游戏版本作为默认下载位置：{DefaultFolder}")
                        Else
                            DefaultFolder = McFolderSelected
                            If NeedLoad Then
                                Hint(GetLang("LangDownloadCompNoSuitableInstance"))
                            Else
                                Logger.Info("由于当前版本不兼容，使用当前的 MC 文件夹作为默认下载位置")
                            End If
                        End If
                    End If
                End If

                Dim DepMissing As List(Of ResourceVersion) = Nothing
                Try
                    If File.ResourceType = ResourceTypes.Mod Then
                        DepMissing = ModXflDep.CollectMissing(File, Project.Platform, DefaultFolder)
                    End If
                Catch exDep As Exception
                    Logger.Warn(exDep, "[XFL][前置] 解析前置失败")
                End Try

                Dim FileName As String
                If Project.TranslatedName = Project.RawName Then
                    FileName = File.FileName
                Else
                    Dim ChineseName As String = Project.TranslatedName.BeforeFirst(" (").BeforeFirst(" - ").
                        Replace("\", "＼").Replace("/", "／").Replace("|", "｜").Replace(":", "：").Replace("<", "＜").Replace(">", "＞").Replace("*", "＊").Replace("?", "？").Replace("""", "").Replace("： ", "：")
                    Select Case Settings.Get(Of Integer)("ToolDownloadTranslateV2")
                        Case 0
                            FileName = $"【{ChineseName}】{File.FileName}"
                        Case 1
                            FileName = $"[{ChineseName}] {File.FileName}"
                        Case 2
                            FileName = $"{ChineseName}-{File.FileName}"
                        Case 3
                            FileName = $"{File.FileName}-{ChineseName}"
                        Case Else
                            FileName = File.FileName
                    End Select
                End If
                If File.ResourceType = ResourceTypes.Mod Then FileName = FileName.Replace("~", "-")
                RunInUi(
                Sub()

                    Dim Target As String
                    Dim Ext As String = If(File.ResourceType = ResourceTypes.Mod,
                        If(File.FileName.EndsWithF(".litemod"), "litemod", "jar"),
                        If(File.FileName.EndsWithF(".mrpack"), "mrpack", "zip"))
                    Target = Dialogs.SaveFile(GetLang("LangSaveAs"), FileName, DefaultFolder, {(Ext, Desc & GetLang("LangSaveAsFile"))})
                    If Target Is Nothing Then Return

                    Dim LoaderName As String = Desc & GetLang("LangDownloadCompTaskDownloadFileDetail") & PathUtils.GetFileNameWithoutExtension(Target) & " "
                    If Target <> DefaultFolder Then CachedFolder(File.ResourceType) = PathUtils.RemoveLastPart(Target)
                    Dim Loaders As New List(Of LoaderBase)
                    Loaders.Add(New LoaderDownload(GetLang("LangDownloadCompTaskDownloadFile"), {
                        File.ToNetFile(Target, If(TargetResourceType = ResourceTypes.Any, ResourceVersion.DownloadReason.Dependency, ResourceVersion.DownloadReason.Standalone))
                    }) With {.ProgressWeight = 6, .Block = True})

                    Try
                        If DepMissing IsNot Nothing AndAlso DepMissing.Count > 0 Then
                            Dim TargetDir As String = IO.Path.GetDirectoryName(Target)
                            For Each M As ResourceVersion In DepMissing
                                Dim Mp As String = IO.Path.Combine(TargetDir, M.FileName)
                                If IO.File.Exists(Mp) Then Continue For
                                Loaders.Add(New LoaderDownload($"前置：{M.Display}", {
                                    M.ToNetFile(Mp, ResourceVersion.DownloadReason.Dependency, File.GameVersions.FirstOrDefault, File.ModLoaders)
                                }) With {.ProgressWeight = 3, .Block = True})
                            Next
                            Logger.Info($"[XFL][前置] 已追加 {DepMissing.Count} 个前置下载")
                        End If
                    Catch exDep2 As Exception
                        Logger.Warn(exDep2, "[XFL][前置] 追加前置下载失败")
                    End Try

                    Dim Loader As New LoaderCombo(Of Integer)(LoaderName, Loaders) With {.OnStateChanged = AddressOf LoaderStateChangedHintOnly}
                    Loader.Start(1)
                    LoaderTaskbarAdd(Loader)
                    FrmMain.BtnExtraDownload.ShowRefresh()
                    FrmMain.BtnExtraDownload.Ribble()
                End Sub)
            Catch ex As Exception
                Logger.Error(ex, "保存资源文件失败")
            End Try
        End Sub, "Download Resource Detail Save")
    End Sub

    Private Sub BtnIntroWeb_Click(sender As Object, e As EventArgs) Handles BtnIntroWeb.Click
        OpenWebsite(Project.Website)
    End Sub
    Private Sub BtnIntroWiki_Click(sender As Object, e As EventArgs) Handles BtnIntroWiki.Click
        OpenWebsite("https://www.mcmod.cn/class/" & Project.WikiId & ".html")
    End Sub
    Private Sub BtnIntroCopy_Click(sender As Object, e As EventArgs) Handles BtnIntroCopy.Click
        ClipboardSet(ResourceItem.LabTitle.Text & ResourceItem.LabTitleRaw.Text)
    End Sub

    Private Sub HintAlternative_Click() Handles HintAlternative.Click
        TargetResourceType = If(TargetResourceType = ResourceTypes.Mod, ResourceTypes.DataPack, ResourceTypes.Mod)
        FrmMain.PageCurrent.Additional(4) = TargetResourceType
        PageLoaderRestart(IsForceRestart:=True)
    End Sub

    Public Shared Sub ResourceFilesCardPreload(Stack As StackPanel, Files As List(Of ResourceVersion))

        Dim Deps As List(Of String) = Files.SelectMany(Function(f) f.Dependencies).Distinct.ToList()
        Deps.Sort()
        If Not Deps.Any() Then Return
        Deps = Deps.Where(
        Function(dep)
            If Not ResourceProject.Cache.ContainsKey(dep) Then Logger.Warn($"未找到 ID {dep} 的前置信息")
            Return ResourceProject.Cache.ContainsKey(dep)
        End Function).ToList

        Stack.Children.Add(New TextBlock With {.Text = GetLang("LangModCompModDependent"), .FontSize = 14, .HorizontalAlignment = HorizontalAlignment.Left, .Margin = New Thickness(6, 2, 0, 5)})

        For Each Dep In Deps
            Dim Item = ResourceProject.Cache(Dep).ToResourceItem(False, False)
            Stack.Children.Add(Item)
        Next

        Stack.Children.Add(New TextBlock With {.Text = GetLang("LangModCompModAlternateVersion"), .FontSize = 14, .HorizontalAlignment = HorizontalAlignment.Left, .Margin = New Thickness(6, 12, 0, 5)})
    End Sub

End Class
