Imports System.Collections.ObjectModel
Imports System.Text.RegularExpressions

Public Module ModModpack

    Public Sub ModpackInstall()
        Dim File As String = Dialogs.SelectFile(GetLang("LangModModpackSelectTitleModpack"), False, filter:={({"rar", "zip", "mrpack"}, GetLang("LangModModpackSelectModpack"))}).FirstOrDefault()
        If String.IsNullOrEmpty(File) Then Return
        RunInThread(
        Sub()
            Try
                ModpackInstall(File)
            Catch ex As Exception
                ex.ThrowIfCanceled()
                Logger.Error(ex, GetLang("LangModModpackExceptionManualInstallFail"), LogBehavior.Alert)
            End Try
        End Sub)
    End Sub

    Public Function ModpackInstall(File As String, Optional InstanceName As String = Nothing, Optional Logo As String = Nothing) As LoaderCombo(Of String)
        Logger.Info($"整合包安装请求：{If(File, "null")}")
        Dim Archive As ZipArchive = Nothing
        Dim ArchiveBaseFolder As String = ""
        Try

            Dim TargetFolder As String = $"{McFolderSelected}versions\{InstanceName}\"
            If TargetFolder.Contains("!") OrElse TargetFolder.Contains(";") Then Hint(GetLang("LangModModpackPathNoExclamationOrSemicolon", TargetFolder), HintType.Red) : Throw New OperationCanceledException

            Dim PackType As Integer = -1
            Try
                Archive = FileUtils.OpenZip(File)

                If Archive.GetEntry("mcbbs.packmeta") IsNot Nothing Then PackType = 3 : Exit Try
                If Archive.GetEntry("mmc-pack.json") IsNot Nothing Then PackType = 2 : Exit Try
                If Archive.GetEntry("modrinth.index.json") IsNot Nothing Then PackType = 4 : Exit Try
                If Archive.GetEntry("manifest.json") IsNot Nothing Then
                    Dim Json As JObject = Archive.GetEntry("manifest.json").Open().ReadString(Encoding.UTF8).DeserializeJson()
                    If Json("addons") Is Nothing Then
                        PackType = 0 : Exit Try
                    Else
                        PackType = 3 : Exit Try
                    End If
                End If
                If Archive.GetEntry("modpack.json") IsNot Nothing Then PackType = 1 : Exit Try
                If Archive.GetEntry("modpack.zip") IsNot Nothing OrElse Archive.GetEntry("modpack.mrpack") IsNot Nothing Then PackType = 9 : Exit Try

                For Each Entry In Archive.Entries
                    Dim PathSplits As String() = Entry.FullName.Split("/")
                    ArchiveBaseFolder = PathSplits(0) & "/"

                    If PathSplits.Count <> 2 Then Continue For

                    If PathSplits(1) = "mcbbs.packmeta" Then PackType = 3 : Exit Try
                    If PathSplits(1) = "mmc-pack.json" Then PackType = 2 : Exit Try
                    If PathSplits(1) = "modrinth.index.json" Then PackType = 4 : Exit Try
                    If PathSplits(1) = "manifest.json" Then
                        Dim Json As JObject = Entry.Open().ReadString(Encoding.UTF8).DeserializeJson()
                        If Json("addons") Is Nothing Then
                            PackType = 0 : Exit Try
                        Else
                            PackType = 3 : ArchiveBaseFolder = "overrides/" : Exit Try
                        End If
                    End If
                    If PathSplits(1) = "modpack.json" Then PackType = 1 : Exit Try
                    If PathSplits(1) = "modpack.zip" OrElse PathSplits(1) = "modpack.mrpack" Then PackType = 9 : Exit Try
                Next
            Catch ex As Exception
                If ex.GetDisplay(True).Contains("Error.WinIOError") Then
                    Throw New Exception(GetLang("LangModModpackExceptionOpenFileFail"), ex)
                ElseIf File.EndsWithF(".rar", True) Then
                    Throw New Exception(GetLang("LangModModpackExceptionRarFormat"), ex)
                Else
                    Throw New Exception(GetLang("LangModModpackExceptionReadFileFail"), ex)
                End If
            End Try

            Select Case PackType
                Case 0
                    Logger.Info("整合包种类：CurseForge")
                    Return InstallPackCurseForge(File, Archive, ArchiveBaseFolder, InstanceName, Logo)
                Case 1
                    Logger.Info("整合包种类：HMCL")
                    Return InstallPackHMCL(File, Archive, ArchiveBaseFolder)
                Case 2
                    Logger.Info("整合包种类：MMC")
                    Return InstallPackMMC(File, Archive, ArchiveBaseFolder)
                Case 3
                    Logger.Info("整合包种类：MCBBS")
                    Return InstallPackMCBBS(File, Archive, ArchiveBaseFolder, InstanceName)
                Case 4
                    Logger.Info("整合包种类：Modrinth")
                    Return InstallPackModrinth(File, Archive, ArchiveBaseFolder, InstanceName, Logo)
                Case 9
                    Logger.Info("整合包种类：带启动器的压缩包")
                    Return InstallPackLauncherPack(File, Archive, ArchiveBaseFolder)
                Case Else
                    Logger.Info("整合包种类：未能识别，假定为压缩包")
                    Return InstallPackCompress(File, Archive)
            End Select
        Finally
            If Archive IsNot Nothing Then Archive.Dispose()
        End Try
    End Function

    Private Sub ExtractModpackFiles(InstallTemp As String, FileAddress As String, Loader As LoaderBase, ProgressIncrement As Double)

        Dim RetryCount As Integer = 1
        Dim InitialProgress = Loader.Progress
Retry:
        Try
            Loader.Progress = InitialProgress
            DirectoryUtils.Delete(InstallTemp)
            Dim RawProgress As Double = Loader.Progress
            FileUtils.ExtractToDirectory(FileAddress, InstallTemp, progressHandler:=Sub(Percentage) Loader.Progress = RawProgress + Percentage * ProgressIncrement)
        Catch ex As Exception
            Logger.Warn(ex, $"第 {RetryCount} 次解压尝试失败")

            If RetryCount < 5 Then
                Thread.Sleep(RetryCount * 2000)
                If Loader IsNot Nothing AndAlso Loader.LoadingState <> MyLoading.MyLoadingState.Run Then Return
                RetryCount += 1
                GoTo Retry
            Else
                Throw New Exception("解压整合包文件失败", ex)
            End If
        End Try
        Loader.Progress = InitialProgress + ProgressIncrement
    End Sub

    Private Sub CopyOverrideDirectory(OverridesFolder As String, VersionFolder As String, Loader As LoaderBase)
        If Not OverridesFolder.EndsWithF("\") Then OverridesFolder += "\"
        If Not VersionFolder.EndsWithF("\") Then VersionFolder += "\"

        If DirectoryUtils.Exists(OverridesFolder) Then
            Logger.Info($"处理整合包覆写文件夹：{OverridesFolder} → {VersionFolder}")
            DirectoryUtils.Copy(OverridesFolder, VersionFolder)
        Else
            Logger.Info($"整合包中没有覆写文件夹：{OverridesFolder}")
        End If

        Dim OverridesIni As String = $"{OverridesFolder}XFL\Setup.ini"
        Dim VersionIni As String = $"{VersionFolder}XFL\Setup.ini"
        If FileUtils.Exists(OverridesIni) Then
            WriteIni(OverridesIni, "VersionArgumentIndie", 1)
            WriteIni(OverridesIni, "VersionArgumentIndieV2", True)
            WriteIni(OverridesIni, "IsStar", False)
            FileUtils.Copy(OverridesIni, VersionIni)
        Else
            WriteIni(VersionIni, "VersionArgumentIndie", 1)
            WriteIni(VersionIni, "VersionArgumentIndieV2", True)
        End If
        IniClearCache(VersionIni)
    End Sub

    Private Sub NotifyIncompatibleLoader(LoaderName As String)
        If MyMsgBox(GetLang("LangModModpackDialogContentIncompatibleLoader", LoaderName),
            GetLang("LangModModpackDialogTitleIncompatibleLoader"), GetLang("LangDialogBtnCancel"), GetLang("LangModModpackDialogBtnSkipLoader", LoaderName)) = 1 Then Throw New OperationCanceledException
    End Sub

#Region "不同类型整合包的安装方法"

    Private Function InstallPackCurseForge(FileAddress As String, Archive As ZipArchive, ArchiveBaseFolder As String,
                                           Optional InstanceName As String = Nothing, Optional Logo As String = Nothing) As LoaderCombo(Of String)

        Dim Json As JObject
        Try
            Json = Archive.GetEntry(ArchiveBaseFolder & "manifest.json").Open().ReadString().DeserializeJson()
        Catch ex As Exception
            Throw New Exception(GetLang("LangModModpackExceptionCurseforgeModpackError"), ex)
        End Try
        If Json("minecraft") Is Nothing OrElse Json("minecraft")("version") Is Nothing Then Throw New Exception(GetLang("LangModModpackCurseforgeNoGameInfo"))

        If InstanceName Is Nothing Then
            InstanceName = If(Json("name"), "")
            Dim Validate As New ValidateFolderName(McFolderSelected & "versions")
            If Validate.Validate(InstanceName) <> "" Then InstanceName = ""
            If InstanceName = "" Then InstanceName = MyMsgBoxInput(GetLang("LangModModpackInputInstanceName"), "", "", New Collection(Of Validate) From {Validate})
            If String.IsNullOrEmpty(InstanceName) Then Throw New OperationCanceledException
        End If

        Dim TargetVersion As New McVersion
        TargetVersion.VanillaName = Json("minecraft")("version").ToString
        For Each Entry In If(Json("minecraft")("modLoaders"), {})
            Dim Id As String = If(Entry("id"), "").ToString.Lower
            If Id.StartsWithF("forge-") Then

                If Id.Contains("recommended") Then Throw New Exception(GetLang("LangModModpackExceptionModpackTooOld"))
                Logger.Info($"整合包 Forge 版本：{Id}")
                TargetVersion.Forge = Id.Replace("forge-", "")
            ElseIf Id.StartsWithF("neoforge-") Then

                Logger.Info($"整合包 NeoForge 版本：{Id}")
                TargetVersion.NeoForge = Id.Replace("neoforge-", "")
            ElseIf Id.StartsWithF("fabric-") Then

                Logger.Info($"整合包 Fabric 版本：{Id}")
                TargetVersion.Fabric = Id.Replace("fabric-", "")
            Else

                NotifyIncompatibleLoader(Id)
            End If
        Next

        Dim InstallTemp As String = RequestTaskTempFolder()
        Dim InstallLoaders As New List(Of LoaderBase)
        Dim OverrideHome As String = If(Json("overrides"), "")
        If OverrideHome <> "" Then
            InstallLoaders.Add(New LoaderTask(Of String, Integer)(GetLang("LangModModpackTaskExtraModpack"),
            Sub(Task As LoaderTask(Of String, Integer))
                ExtractModpackFiles(InstallTemp, FileAddress, Task, 0.6)
                CopyOverrideDirectory(
                    InstallTemp & ArchiveBaseFolder & If(OverrideHome = "." OrElse OverrideHome = "./", "", OverrideHome),
                    $"{McFolderSelected}versions\{InstanceName}", Task)
            End Sub) With {
            .ProgressWeight = FileUtils.GetInfo(FileAddress).Length / 1024 / 1024 / 6, .Block = False})
        End If

        Dim ModList As New List(Of Integer)
        Dim ModOptionalList As New List(Of Integer)
        For Each ModEntry In If(Json("files"), {})
            If ModEntry("projectID") Is Nothing OrElse ModEntry("fileID") Is Nothing Then
                Hint(GetLang("LangModModpackHintModInfoMissing", ModEntry.ToString))
                Continue For
            End If
            ModList.Add(ModEntry("fileID"))
            If ModEntry("required") IsNot Nothing AndAlso Not ModEntry("required").ToObject(Of Boolean) Then ModOptionalList.Add(ModEntry("fileID"))
        Next
        If ModList.Any Then
            Dim ModDownloadLoaders As New List(Of LoaderBase)

            ModDownloadLoaders.Add(New LoaderTask(Of Integer, JArray)(GetLang("LangModModpackTaskGetModDownloadInfo"),
            Sub(Task As LoaderTask(Of Integer, JArray))
                Task.Output = DlModRequest("https://api.curseforge.com/v1/mods/files", HttpMethod.Post, "{""fileIds"": [" & ModList.Join(",") & "]}", "application/json")("data")
                If ModList.Count = Task.Output.Count Then Return

                Dim MissingIds = ModList.Except(Task.Output.Select(Function(j) j("id").ToObject(Of Integer))).ToList
                Logger.Info($"缺失的 fileIds：{MissingIds.Join("、")}")

                Dim Missings = MissingIds.Select(Of (ProjectId As Integer, FileId As Integer))(
                    Function(id) (CType(Json("files").AsEnumerable.First(Function(f) f("fileID").ToObject(Of Integer) = id), JObject)("projectID").ToObject(Of Integer), id)).ToList
                Logger.Info($"缺失项：{Missings.Join("、")}")

                Dim Projects As New List(Of ResourceProject)
                Try
                    Projects = ResourceProject.FromProjectIds(Missings.Select(Function(e) e.ProjectId.ToString), ResourcePlatforms.CurseForge).ToList
                Catch ex As Exception
                    Logger.Warn(ex, "获取缺失的工程信息失败")
                End Try
                Dim MissingDesc As New List(Of String)
                For Each Missing In Missings
                    Dim Project = Projects.FirstOrDefault(Function(p) p.Id = Missing.ProjectId)
                    If Project Is Nothing Then
                        MissingDesc.Add($"未知 CurseForge 项目 (项目 ID {Missing.ProjectId}，文件 ID {Missing.FileId})")
                    Else
                        MissingDesc.Add($"{Project.TranslatedName} (文件 ID {Missing.FileId})")
                    End If
                Next
                Dim MissingDisp = $"- {MissingDesc.Join(vbCrLf + "- ")}"
                If MyMsgBox(GetLang("LangModModpackDialogContentOnlineModDeleted", MissingDisp),
                    GetLang("LangModModpackDialogTitleOnlineModDeleted"), GetLang("LangDialogBtnContinue"), GetLang("LangDialogBtnCancel"), IsWarn:=True) = 2 Then
                    Task.Cancel()
                End If
            End Sub) With {.ProgressWeight = ModList.Count / 10})

            ModDownloadLoaders.Add(New LoaderTask(Of JArray, List(Of NetFile))(GetLang("LangModModpackTaskInitModDownloadInfo"),
            Sub(Task As LoaderTask(Of JArray, List(Of NetFile)))
                Dim FileList As New Dictionary(Of Integer, NetFile)
                For Each ModJson In Task.Input
                    Dim Id As Integer = ModJson("id").ToObject(Of Integer)

                    If FileList.ContainsKey(Id) Then Continue For

                    If ModOptionalList.Contains(Id) Then
                        If MyMsgBox(GetLang("LangModModpackDialogContentDownloadOptionalFile", ModJson("displayName").ToString),
                                        GetLang("LangModModpackDialogTitleDownloadOptionalFile"), GetLang("LangDialogBtnYes"), GetLang("LangDialogBtnNo")) = 2 Then
                            Continue For
                        End If
                    End If

                    Dim TargetFolder As String, Type As ResourceTypes
                    If ModJson("modules").Any Then
                        Dim ModuleNames = CType(ModJson("modules"), JArray).Select(Function(l) l("name").ToString).ToList
                        If ModuleNames.Contains("META-INF") OrElse ModuleNames.Contains("mcmod.info") OrElse
                           ModJson?("FileName")?.ToString.EndsWithF(".jar", True) Then
                            TargetFolder = "mods" : Type = ResourceTypes.Mod
                        ElseIf ModuleNames.Contains("pack.mcmeta") Then
                            TargetFolder = "resourcepacks" : Type = ResourceTypes.ResourcePack
                        Else
                            TargetFolder = "shaderpacks" : Type = ResourceTypes.Shader
                        End If
                    Else
                        TargetFolder = "mods" : Type = ResourceTypes.Mod
                    End If

                    Dim File = ResourceVersion.FromPlatformJson(ModJson, Type)
                    If Not File.DownloadAvailable Then Continue For

                    FileList.Add(Id, File.ToNetFile($"{McFolderSelected}versions\{InstanceName}\{TargetFolder}\",
                        ResourceVersion.DownloadReason.ModPack, TargetVersion.VanillaName, TargetVersion.ModLoaders))
                    Task.Progress += 1 / (1 + ModList.Count)
                Next
                Task.Output = FileList.Values.ToList
            End Sub) With {.ProgressWeight = ModList.Count / 200, .Show = False})

            ModDownloadLoaders.Add(New LoaderDownload(GetLang("LangModModpackTaskDownloadMod"), New List(Of NetFile)) With {.ProgressWeight = ModList.Count * 1.5})

            InstallLoaders.Add(New LoaderCombo(Of Integer)(GetLang("LangModModpackTaskDownloadModMainLoader"), ModDownloadLoaders) With
                {.Show = False, .ProgressWeight = ModDownloadLoaders.Sum(Function(l) l.ProgressWeight)})
        End If

        Dim Request As New McInstallRequest With {
            .NewInstanceName = InstanceName,
            .VersionFolder = $"{McFolderSelected}versions\{InstanceName}\",
            .MinecraftName = TargetVersion.VanillaName,
            .ForgeVersion = TargetVersion.Forge,
            .NeoForgeVersion = TargetVersion.NeoForge,
            .FabricVersion = TargetVersion.Fabric
        }
        Dim MergeLoaders As List(Of LoaderBase) = McInstallLoader(Request)

        Dim Loaders As New List(Of LoaderBase)
        Loaders.Add(New LoaderCombo(Of String)(GetLang("LangModModpackTaskInstallModpack"), InstallLoaders) With {.Show = False, .Block = False, .ProgressWeight = InstallLoaders.Sum(Function(l) l.ProgressWeight)})
        Loaders.Add(New LoaderCombo(Of String)(GetLang("LangModModpackTaskInstallGame"), MergeLoaders) With {.Show = False, .ProgressWeight = MergeLoaders.Sum(Function(l) l.ProgressWeight)})
        Loaders.Add(New LoaderTask(Of String, String)(GetLang("LangModModpackTaskOrganizeFiles"),
        Sub(Task As LoaderTask(Of String, String))

            Dim VersionFolder As String = $"{McFolderSelected}versions\{InstanceName}\"
            If Logo IsNot Nothing AndAlso FileUtils.Exists(Logo) Then
                FileUtils.Copy(Logo, VersionFolder & "XFL\Logo.png")
                WriteIni(VersionFolder & "XFL\Setup.ini", "Logo", "XFL\Logo.png")
                WriteIni(VersionFolder & "XFL\Setup.ini", "LogoCustom", "True")
                Logger.Info($"已设置整合包 Logo：{Logo}")
            End If

            For Each Target As String In {VersionFolder & "原始整合包.zip", VersionFolder & "原始整合包.mrpack"}
                If FileUtils.Exists(Target) Then
                    Logger.Info($"删除原始整合包文件：{Target}")
                    FileUtils.Delete(Target)
                End If
            Next
            If FileUtils.Exists(FileAddress) AndAlso PathUtils.GetFileNameWithoutExtension(FileAddress) = "modpack" Then
                Logger.Info($"删除安装整合包文件：{FileAddress}")
                FileUtils.Delete(FileAddress)
            End If
        End Sub) With {.ProgressWeight = 0.1, .Show = False})

        Dim LoaderName As String = GetLang("LangModModpackTaskCurseForgeModpackInstall") & InstanceName & " "
        If LoaderTaskbar.Any(Function(l) l.Name = LoaderName) Then
            Hint(GetLang("LangModModpackHintInstalling"), HintType.Red)
            Throw New OperationCanceledException
        End If

        Dim Loader As New LoaderCombo(Of String)(LoaderName, Loaders) With {.OnStateChanged = AddressOf McInstallState}
        Loader.Start(Request.VersionFolder)
        LoaderTaskbarAdd(Loader)
        FrmMain.BtnExtraDownload.ShowRefresh()
        RunInUi(Sub() FrmMain.PageChange(FormMain.PageType.DownloadManager))
        Return Loader
    End Function

    Private Function InstallPackModrinth(FileAddress As String, Archive As ZipArchive, ArchiveBaseFolder As String, Optional InstanceName As String = Nothing, Optional Logo As String = Nothing) As LoaderCombo(Of String)

        Dim Json As JObject
        Try
            Json = Archive.GetEntry(ArchiveBaseFolder & "modrinth.index.json").Open().ReadString().DeserializeJson()
        Catch ex As Exception
            Throw New Exception(GetLang("LangModModpackExceptionModrinthModpackError"), ex)
        End Try
        If Json("dependencies") Is Nothing OrElse Json("dependencies")("minecraft") Is Nothing Then Throw New Exception(GetLang("LangModModpackModrinthNoGameInfo"))

        Dim TargetVersion As New McVersion
        For Each Entry As JProperty In If(Json("dependencies"), {})
            Select Case Entry.Name.Lower
                Case "minecraft"
                    TargetVersion.VanillaName = Entry.Value.ToString
                Case "forge"
                    TargetVersion.Forge = Entry.Value.ToString
                    Logger.Info($"整合包 Forge 版本：{TargetVersion.Forge}")
                Case "neoforge", "neo-forge"
                    TargetVersion.NeoForge = Entry.Value.ToString
                    Logger.Info($"整合包 NeoForge 版本：{TargetVersion.NeoForge}")
                Case "fabric-loader"
                    TargetVersion.Fabric = Entry.Value.ToString
                    Logger.Info($"整合包 Fabric 版本：{TargetVersion.Fabric}")
                Case Else

                    NotifyIncompatibleLoader(Entry.Name)
            End Select
        Next

        If InstanceName Is Nothing Then
            InstanceName = If(Json("name"), "")
            Dim Validate As New ValidateFolderName(McFolderSelected & "versions")
            If Validate.Validate(InstanceName) <> "" Then InstanceName = ""
            If InstanceName = "" Then InstanceName = MyMsgBoxInput(GetLang("LangModModpackInputInstanceName"), "", "", New Collection(Of Validate) From {Validate})
            If String.IsNullOrEmpty(InstanceName) Then Throw New OperationCanceledException
        End If

        Dim InstallTemp As String = RequestTaskTempFolder()
        Dim InstallLoaders As New List(Of LoaderBase)
        InstallLoaders.Add(New LoaderTask(Of String, Integer)(GetLang("LangModModpackTaskExtraModpack"),
        Sub(Task As LoaderTask(Of String, Integer))
            ExtractModpackFiles(InstallTemp, FileAddress, Task, 0.5)
            CopyOverrideDirectory(
                InstallTemp & ArchiveBaseFolder & "overrides",
                McFolderSelected & "versions\" & InstanceName, Task)
            CopyOverrideDirectory(
                InstallTemp & ArchiveBaseFolder & "client-overrides",
                McFolderSelected & "versions\" & InstanceName, Task)
        End Sub) With {.ProgressWeight = FileUtils.GetInfo(FileAddress).Length / 1024 / 1024 / 6, .Block = False})

        Dim FileList As New List(Of NetFile)
        For Each File In If(Json("files"), {})

            If File("env") IsNot Nothing Then
                Select Case File("env")("client").ToString
                    Case "optional"
                        If MyMsgBox(GetLang("LangModModpackDialogContentDownloadOptionalFile", PathUtils.GetLastPart(File("path").ToString)),
                                    GetLang("LangModModpackDialogTitleDownloadOptionalFile"), GetLang("LangDialogBtnYes"), GetLang("LangDialogBtnNo")) = 2 Then
                            Continue For
                        End If
                    Case "unsupported"
                        Continue For
                End Select
            End If

            Dim Urls = File("downloads").SelectMany(Function(t) ResourceVersion.ParseCurseForgeDownloadUrls(t.ToString)).ToList
            Urls.AddRange(Urls.Select(Function(u) DlSourceModGet(u)).ToList)
            Urls = ResourceVersion.ParseModrinthTrackArguments(Urls,
                ResourceVersion.DownloadReason.ModPack, TargetVersion.VanillaName, TargetVersion.ModLoaders).ToList
            Urls = Urls.Distinct.ToList()
            Dim TargetPath As String = $"{McFolderSelected}versions\{InstanceName}\{File("path")}"
            If Not Path.GetFullPath(TargetPath).StartsWithF(Path.GetFullPath($"{McFolderSelected}versions\{InstanceName}\")) Then
                MyMsgBox(GetLang("LangModModpackDialogContentPathOutOfRange", Path.GetFullPath(TargetPath), TargetPath), GetLang("LangModModpackDialogTitlePathInvalid"), IsWarn:=True)
                Throw New OperationCanceledException
            End If
            FileList.Add(New NetFile(Urls, TargetPath,
                New FileChecker With {.ActualSize = File("fileSize").ToObject(Of Long), .Hash = File("hashes")("sha1").ToString}, True))
        Next
        If FileList.Any Then
            InstallLoaders.Add(New LoaderDownload(GetLang("LangModModpackTaskDownloadExtraFile"), FileList) With {.ProgressWeight = FileList.Count * 1.5})
        End If

        Dim Request As New McInstallRequest With {
            .NewInstanceName = InstanceName,
            .VersionFolder = $"{McFolderSelected}versions\{InstanceName}\",
            .MinecraftName = TargetVersion.VanillaName,
            .ForgeVersion = TargetVersion.Forge,
            .NeoForgeVersion = TargetVersion.NeoForge,
            .FabricVersion = TargetVersion.Fabric
        }
        Dim MergeLoaders As List(Of LoaderBase) = McInstallLoader(Request)

        Dim Loaders As New List(Of LoaderBase)
        Loaders.Add(New LoaderCombo(Of String)(GetLang("LangModModpackTaskInstallModpack"), InstallLoaders) With {.Show = False, .Block = False, .ProgressWeight = InstallLoaders.Sum(Function(l) l.ProgressWeight)})
        Loaders.Add(New LoaderCombo(Of String)(GetLang("LangModModpackTaskInstallGame"), MergeLoaders) With {.Show = False, .ProgressWeight = MergeLoaders.Sum(Function(l) l.ProgressWeight)})
        Loaders.Add(New LoaderTask(Of String, String)(GetLang("LangModModpackTaskOrganizeFiles"),
        Sub(Task As LoaderTask(Of String, String))

            Dim VersionFolder As String = $"{McFolderSelected}versions\{InstanceName}\"
            If Logo IsNot Nothing AndAlso FileUtils.Exists(Logo) Then
                FileUtils.Copy(Logo, VersionFolder & "XFL\Logo.png")
                WriteIni(VersionFolder & "XFL\Setup.ini", "Logo", "XFL\Logo.png")
                WriteIni(VersionFolder & "XFL\Setup.ini", "LogoCustom", "True")
                Logger.Info($"已设置整合包 Logo：{Logo}")
            End If

            For Each Target As String In {VersionFolder & "原始整合包.zip", VersionFolder & "原始整合包.mrpack"}
                If FileUtils.Exists(Target) Then
                    Logger.Info($"删除原始整合包文件：{Target}")
                    FileUtils.Delete(Target)
                End If
            Next
            If FileUtils.Exists(FileAddress) AndAlso PathUtils.GetFileNameWithoutExtension(FileAddress) = "modpack" Then
                Logger.Info($"删除安装整合包文件：{FileAddress}")
                FileUtils.Delete(FileAddress)
            End If
        End Sub) With {.ProgressWeight = 0.1, .Show = False})

        Dim LoaderName As String = GetLang("LangModModpackTaskModrinthModpackInstall") & InstanceName & " "
        If LoaderTaskbar.Any(Function(l) l.Name = LoaderName) Then
            Hint(GetLang("LangModModpackHintInstalling"), HintType.Red)
            Throw New OperationCanceledException
        End If

        Dim Loader As New LoaderCombo(Of String)(LoaderName, Loaders) With {.OnStateChanged = AddressOf McInstallState}
        Loader.Start(Request.VersionFolder)
        LoaderTaskbarAdd(Loader)
        FrmMain.BtnExtraDownload.ShowRefresh()
        RunInUi(Sub() FrmMain.PageChange(FormMain.PageType.DownloadManager))
        Return Loader
    End Function

    Private Function InstallPackHMCL(FileAddress As String, Archive As ZipArchive, ArchiveBaseFolder As String) As LoaderCombo(Of String)

        Dim Json As JObject
        Try
            Json = Archive.GetEntry(ArchiveBaseFolder & "modpack.json").Open().ReadString(Encoding.UTF8).DeserializeJson()
        Catch ex As Exception
            Throw New Exception(GetLang("LangModModpackExceptionHMCLModpackError"), ex)
        End Try

        Dim InstanceName As String = If(Json("name"), "")
        Dim Validate As New ValidateFolderName(McFolderSelected & "versions")
        If Validate.Validate(InstanceName) <> "" Then InstanceName = ""
        If InstanceName = "" Then InstanceName = MyMsgBoxInput(GetLang("LangModModpackInputInstanceName"), "", "", New Collection(Of Validate) From {Validate})
        If String.IsNullOrEmpty(InstanceName) Then Throw New OperationCanceledException

        Dim InstallTemp As String = RequestTaskTempFolder()
        Dim InstallLoaders As New List(Of LoaderBase)
        InstallLoaders.Add(New LoaderTask(Of String, Integer)(GetLang("LangModModpackTaskExtraModpack"),
        Sub(Task As LoaderTask(Of String, Integer))
            ExtractModpackFiles(InstallTemp, FileAddress, Task, 0.6)
            CopyOverrideDirectory(
                InstallTemp & ArchiveBaseFolder & "minecraft",
                McFolderSelected & "versions\" & InstanceName, Task)
        End Sub) With {.ProgressWeight = FileUtils.GetInfo(FileAddress).Length / 1024 / 1024 / 6, .Block = False})

        If Json("gameVersion") Is Nothing Then Throw New Exception(GetLang("LangModModpackHMCLNoGameInfo"))
        Dim Request As New McInstallRequest With {
            .NewInstanceName = InstanceName,
            .VersionFolder = $"{McFolderSelected}versions\{InstanceName}\",
            .MinecraftName = Json("gameVersion").ToString
        }
        Dim MergeLoaders As List(Of LoaderBase) = McInstallLoader(Request)

        Dim Loaders As New List(Of LoaderBase) From {
            New LoaderCombo(Of String)(GetLang("LangModModpackTaskInstallModpack"), InstallLoaders) With {.Show = False, .Block = False, .ProgressWeight = InstallLoaders.Sum(Function(l) l.ProgressWeight)},
            New LoaderCombo(Of String)(GetLang("LangModModpackTaskInstallGame"), MergeLoaders) With {.Show = False, .ProgressWeight = MergeLoaders.Sum(Function(l) l.ProgressWeight)}
        }

        Dim LoaderName As String = GetLang("LangModModpackTaskHMCLModpackInstall") & InstanceName & " "
        If LoaderTaskbar.Any(Function(l) l.Name = LoaderName) Then
            Hint(GetLang("LangModModpackHintInstalling"), HintType.Red)
            Throw New OperationCanceledException
        End If

        Dim Loader As New LoaderCombo(Of String)(LoaderName, Loaders) With {.OnStateChanged = AddressOf McInstallState}
        Loader.Start(Request.VersionFolder)
        LoaderTaskbarAdd(Loader)
        FrmMain.BtnExtraDownload.ShowRefresh()
        RunInUi(Sub() FrmMain.PageChange(FormMain.PageType.DownloadManager))
        Return Loader
    End Function

    Private Function InstallPackMMC(FileAddress As String, Archive As ZipArchive, ArchiveBaseFolder As String) As LoaderCombo(Of String)

        Dim PackJson As JObject, PackInstance As String
        Try
            PackJson = Archive.GetEntry(ArchiveBaseFolder & "mmc-pack.json").Open().ReadString(Encoding.UTF8).DeserializeJson()
            PackInstance = Archive.GetEntry(ArchiveBaseFolder & "instance.cfg").Open().ReadString(Encoding.UTF8)
        Catch ex As Exception
            Throw New Exception(GetLang("LangModModpackExceptionMMCModpackError"), ex)
        End Try

        Dim InstanceName As String = If(PackInstance.RegexSeek("(?<=\nname\=)[^\n]+"), "")
        Dim Validate As New ValidateFolderName(McFolderSelected & "versions")
        If Validate.Validate(InstanceName) <> "" Then InstanceName = ""
        If InstanceName = "" Then InstanceName = MyMsgBoxInput(GetLang("LangModModpackInputInstanceName"), "", "", New Collection(Of Validate) From {Validate})
        If String.IsNullOrEmpty(InstanceName) Then Throw New OperationCanceledException

        Dim InstallTemp As String = RequestTaskTempFolder()
        Dim SetupFile As String = $"{McFolderSelected}versions\{InstanceName}\XFL\Setup.ini"
        Dim InstallLoaders As New List(Of LoaderBase)
        InstallLoaders.Add(New LoaderTask(Of String, Integer)(GetLang("LangModModpackTaskExtraModpack"),
        Sub(Task As LoaderTask(Of String, Integer))
            ExtractModpackFiles(InstallTemp, FileAddress, Task, 0.55)
            CopyOverrideDirectory(
                InstallTemp & ArchiveBaseFolder & ".minecraft",
                McFolderSelected & "versions\" & InstanceName, Task)

            Try
                Dim MmcSetupFile As String = InstallTemp & ArchiveBaseFolder & "instance.cfg"
                If FileUtils.Exists(MmcSetupFile) Then

                    Dim Lines As New List(Of String)
                    For Each Line In FileUtils.ReadAsLines(MmcSetupFile, True)
                        If Not Line.Contains("=") Then Continue For
                        Lines.Add(Line.BeforeFirst("=") & ":" & Line.AfterFirst("="))
                    Next
                    FileUtils.Write(MmcSetupFile, Lines.Join(vbCrLf))

                    If ReadIni(MmcSetupFile, "OverrideCommands", False) Then
                        Dim PreLaunchCommand As String = ReadIni(MmcSetupFile, "PreLaunchCommand")
                        If PreLaunchCommand <> "" Then
                            PreLaunchCommand = PreLaunchCommand.Replace("\""", """").
                                Replace("$INST_JAVA", "{java}java.exe").
                                Replace("$INST_MC_DIR\", "{minecraft}").Replace("$INST_MC_DIR", "{minecraft}").
                                Replace("$INST_DIR\", "{verpath}").Replace("$INST_DIR", "{verpath}").
                                Replace("$INST_ID", "{name}").Replace("$INST_NAME", "{name}")
                            WriteIni(SetupFile, "VersionAdvanceRun", PreLaunchCommand)
                            Logger.Info($"迁移 MultiMC 版本独立设置：启动前执行命令：{PreLaunchCommand}")
                        End If
                    End If
                    If ReadIni(MmcSetupFile, "JoinServerOnLaunch", False) Then
                        Dim ServerAddress As String = ReadIni(MmcSetupFile, "JoinServerOnLaunchAddress").Replace("\""", """")
                        WriteIni(SetupFile, "VersionServerEnter", ServerAddress)
                        Logger.Info($"迁移 MultiMC 版本独立设置：自动进入服务器：{ServerAddress}")
                    End If
                    Dim Logo As String = ReadIni(MmcSetupFile, "iconKey", "")
                    If Logo <> "" AndAlso FileUtils.Exists($"{InstallTemp}{ArchiveBaseFolder}{Logo}.png") Then
                        WriteIni(SetupFile, "LogoCustom", True)
                        WriteIni(SetupFile, "Logo", "XFL\Logo.png")
                        FileUtils.Copy($"{InstallTemp}{ArchiveBaseFolder}{Logo}.png", $"{McFolderSelected}versions\{InstanceName}\XFL\Logo.png")
                        Logger.Info($"迁移 MultiMC 版本独立设置：版本图标（{Logo}.png）")
                    End If

                    Dim JvmArgs As String = ReadIni(MmcSetupFile, "JvmArgs", "")
                    If JvmArgs <> "" Then
                        If ReadIni(MmcSetupFile, "OverrideJavaArgs", False) Then
                            WriteIni(SetupFile, "VersionAdvanceJvm", JvmArgs)
                            Logger.Info($"迁移 MultiMC 版本独立设置：JVM 参数（覆盖）：{JvmArgs}")
                        Else
                            JvmArgs += " " & Settings.Get(Of String)("LaunchAdvanceJvm")
                            WriteIni(SetupFile, "VersionAdvanceJvm", JvmArgs)
                            Logger.Info($"迁移 MultiMC 版本独立设置：JVM 参数（追加）：{JvmArgs}")
                        End If
                    End If
                End If
            Catch ex As Exception
                Logger.Warn(ex, $"读取 MMC 配置文件失败（{InstallTemp}{ArchiveBaseFolder}instance.cfg）")
            End Try
        End Sub) With {.ProgressWeight = FileUtils.GetInfo(FileAddress).Length / 1024 / 1024 / 6, .Block = False})

        If PackJson("components") Is Nothing Then Throw New Exception(GetLang("LangModModpackMMCNoGameInfo"))
        Dim Request As New McInstallRequest With {.NewInstanceName = InstanceName, .VersionFolder = $"{McFolderSelected}versions\{InstanceName}\"}
        For Each Component As JObject In PackJson("components")
            If Not Component.ContainsKey("uid") Then Continue For
            Dim UID As String = Component("uid").ToString
            Select Case UID
                Case "net.minecraft"
                    Request.MinecraftName = Component("version")
                Case "net.minecraftforge"
                    Request.ForgeVersion = Component("version")
                Case "net.neoforged"
                    Request.NeoForgeVersion = Component("version")
                Case "net.fabricmc.fabric-loader"
                    Request.FabricVersion = Component("version")

                Case Else
                    If UID.StartsWithF("org.lwjgl") Then
                        Logger.Info($"已跳过 LWJGL 项：{UID}")
                    Else
                        NotifyIncompatibleLoader(UID)
                    End If
            End Select
        Next

        Dim MergeLoaders As List(Of LoaderBase) = McInstallLoader(Request)

        Dim Loaders As New List(Of LoaderBase)
        Loaders.Add(New LoaderCombo(Of String)(GetLang("LangModModpackTaskInstallModpack"), InstallLoaders) With {.Show = False, .Block = False, .ProgressWeight = InstallLoaders.Sum(Function(l) l.ProgressWeight)})
        Loaders.Add(New LoaderCombo(Of String)(GetLang("LangModModpackTaskInstallGame"), MergeLoaders) With {.Show = False, .ProgressWeight = MergeLoaders.Sum(Function(l) l.ProgressWeight)})

        Dim LoaderName As String = GetLang("LangModModpackTaskMMCModpackInstall") & InstanceName & " "
        If LoaderTaskbar.Any(Function(l) l.Name = LoaderName) Then
            Hint(GetLang("LangModModpackHintInstalling"), HintType.Red)
            Throw New OperationCanceledException
        End If

        Dim Loader As New LoaderCombo(Of String)(LoaderName, Loaders) With {.OnStateChanged = AddressOf McInstallState}
        Loader.Start(Request.VersionFolder)
        LoaderTaskbarAdd(Loader)
        FrmMain.BtnExtraDownload.ShowRefresh()
        RunInUi(Sub() FrmMain.PageChange(FormMain.PageType.DownloadManager))
        Return Loader
    End Function

    Private Function InstallPackMCBBS(FileAddress As String, Archive As ZipArchive, ArchiveBaseFolder As String,
                                      Optional InstanceName As String = Nothing) As LoaderCombo(Of String)

        Dim Json As JObject
        Try
            Dim Entry = If(Archive.GetEntry(ArchiveBaseFolder & "mcbbs.packmeta"), Archive.GetEntry(ArchiveBaseFolder & "manifest.json"))
            Json = Entry.Open().ReadString(Encoding.UTF8).DeserializeJson()
        Catch ex As Exception
            Throw New Exception(GetLang("LangModModpackExceptionMCBBSModpackError"), ex)
        End Try

        If InstanceName Is Nothing Then
            InstanceName = If(Json("name"), "")
            Dim Validate As New ValidateFolderName(McFolderSelected & "versions")
            If Validate.Validate(InstanceName) <> "" Then InstanceName = ""
            If InstanceName = "" Then InstanceName = MyMsgBoxInput(GetLang("LangModModpackInputInstanceName"), "", "", New Collection(Of Validate) From {Validate})
            If String.IsNullOrEmpty(InstanceName) Then Throw New OperationCanceledException
        End If

        Dim InstallTemp As String = RequestTaskTempFolder()
        Dim SetupFile As String = $"{McFolderSelected}versions\{InstanceName}\XFL\Setup.ini"
        Dim InstallLoaders As New List(Of LoaderBase)
        InstallLoaders.Add(New LoaderTask(Of String, Integer)(GetLang("LangModModpackTaskExtraModpack"),
        Sub(Task As LoaderTask(Of String, Integer))
            ExtractModpackFiles(InstallTemp, FileAddress, Task, 0.6)
            CopyOverrideDirectory(
                InstallTemp & ArchiveBaseFolder & "overrides",
                McFolderSelected & "versions\" & InstanceName, Task)

            If Json("launchInfo") IsNot Nothing Then
                Dim LaunchInfo As JObject = Json("launchInfo")
                If LaunchInfo.ContainsKey("javaArgument") Then WriteIni(SetupFile, "VersionAdvanceJvm", String.Join(" ", LaunchInfo("javaArgument")))
                If LaunchInfo.ContainsKey("launchArgument") Then WriteIni(SetupFile, "VersionAdvanceGame", String.Join(" ", LaunchInfo("launchArgument")))
            End If
        End Sub) With {.ProgressWeight = FileUtils.GetInfo(FileAddress).Length / 1024 / 1024 / 6, .Block = False})

        If Json("addons") Is Nothing Then Throw New Exception(GetLang("LangModModpackMCBBSNoGameInfoAddons"))
        Dim Addons As New Dictionary(Of String, String)
        For Each Entry In Json("addons")
            Addons.Add(Entry("id"), Entry("version"))
        Next
        If Not Addons.ContainsKey("game") Then Throw New Exception(GetLang("LangModModpackMCBBSNoGameInfo"))
        If Addons.ContainsKey("quilt") Then
            Hint(GetLang("LangModModpackHintQuiltNotSupport"), HintType.Red)
            Throw New OperationCanceledException
        End If
        Dim Request As New McInstallRequest With {
            .NewInstanceName = InstanceName,
            .VersionFolder = $"{McFolderSelected}versions\{InstanceName}\",
            .MinecraftName = Addons("game"),
            .OptiFineVersion = If(Addons.ContainsKey("optifine"), Addons("optifine"), Nothing),
            .ForgeVersion = If(Addons.ContainsKey("forge"), Addons("forge"), Nothing),
            .NeoForgeVersion = If(Addons.ContainsKey("neoforge"), Addons("neoforge"), Nothing),
            .FabricVersion = If(Addons.ContainsKey("fabric"), Addons("fabric"), Nothing)
        }
        Dim MergeLoaders As List(Of LoaderBase) = McInstallLoader(Request)

        Dim Loaders As New List(Of LoaderBase)
        Loaders.Add(New LoaderCombo(Of String)(GetLang("LangModModpackTaskInstallModpack"), InstallLoaders) With {.Show = False, .Block = False, .ProgressWeight = InstallLoaders.Sum(Function(l) l.ProgressWeight)})
        Loaders.Add(New LoaderCombo(Of String)(GetLang("LangModModpackTaskInstallGame"), MergeLoaders) With {.Show = False, .ProgressWeight = MergeLoaders.Sum(Function(l) l.ProgressWeight)})

        Dim LoaderName As String = GetLang("LangModModpackTaskMCBBSModpackInstall") & InstanceName & " "
        If LoaderTaskbar.Any(Function(l) l.Name = LoaderName) Then
            Hint(GetLang("LangModModpackHintInstalling"), HintType.Red)
            Throw New OperationCanceledException
        End If

        Dim Loader As New LoaderCombo(Of String)(LoaderName, Loaders) With {.OnStateChanged = AddressOf McInstallState}

        Loader.Start(Request.VersionFolder)
        LoaderTaskbarAdd(Loader)
        FrmMain.BtnExtraDownload.ShowRefresh()
        RunInUi(Sub() FrmMain.PageChange(FormMain.PageType.DownloadManager))
        Return Loader
    End Function

    Private Function InstallPackLauncherPack(FileAddress As String, Archive As ZipArchive, ArchiveBaseFolder As String) As LoaderCombo(Of String)

        MyMsgBox(GetLang("LangModModpackDialogContentInstallTip"), GetLang("LangModModpackDialogTitleInstall"), GetLang("LangDialogBtnContinue"), ForceWait:=True)
        Dim TargetFolder As String = Dialogs.SelectFolder(GetLang("LangModModpackSelectInstallFolder"), False).FirstOrDefault
        If TargetFolder Is Nothing Then Throw New OperationCanceledException
        If Not DirectoryUtils.IsEmpty(TargetFolder) Then Hint(GetLang("LangModModpackFolderShouldBeEmpty"), HintType.Red) : Throw New OperationCanceledException

        Dim Loader As New LoaderCombo(Of String)(GetLang("LangModModpackTaskExtractArchive"), {
            New LoaderTask(Of String, Integer)(GetLang("LangModModpackTaskExtractArchive"),
            Sub(Task As LoaderTask(Of String, Integer))
                ExtractModpackFiles(TargetFolder, FileAddress, Task, 0.9)
                OpenExplorer(TargetFolder)
                Thread.Sleep(400)

                Dim Launcher As String = Nothing
                For Each ExeFile In DirectoryUtils.EnumerateFiles(TargetFolder, searchPattern:="*.exe")
                    Dim Info = FileVersionInfo.GetVersionInfo(ExeFile)
                    Logger.Info($"文件 {ExeFile} 的产品名标识为 {Info.ProductName}")
                    If Info.ProductName = "Xiaofu Launcher" Then
                        Launcher = ExeFile
                        Logger.Info($"发现整合包附带的 XFL 启动器：{ExeFile}")
                    ElseIf (Info.ProductName.ContainsIgnoreCase("Launcher") OrElse Info.ProductName.Contains("启动")) AndAlso
                        Not Info.ProductName = "Xiaofu Launcher Admin Manager" Then
                        If Launcher Is Nothing Then
                            Launcher = ExeFile
                            Logger.Info($"发现整合包附带的疑似第三方启动器：{ExeFile}")
                        End If
                    End If
                Next
                Task.Progress = 0.95

                If Launcher IsNot Nothing Then
                    Logger.Info($"找到压缩包中附带的启动器：{Launcher}")
                    If MyMsgBox(GetLang("LangModModpackDialogContentAnotherLauncher", Launcher), GetLang("LangModModpackDialogTitleAnotherLauncher"), GetLang("LangModModpackDialogBtnAnotherLauncher1"), GetLang("LangModModpackDialogBtnAnotherLauncher2")) = 1 Then
                        StartProcess(Launcher, "--wait")
                        Logger.Info("为换用整合包中的启动器启动，强制结束程序")
                        FrmMain.EndProgram(False)
                        Return
                    End If
                Else
                    Logger.Info("未找到压缩包中附带的启动器")
                End If

                Dim InstanceName As String = PathUtils.GetLastPart(TargetFolder)
                DirectoryUtils.Create(TargetFolder & ".minecraft\")
                PageSelectLeft.AddFolder(
                    TargetFolder & ".minecraft\" & ArchiveBaseFolder.Replace("/", "\").TrimStart("\"),
                    InstanceName, False)

                Dim ModpackFile = DirectoryUtils.EnumerateFiles(TargetFolder, True, "modpack.*").First
                Logger.Info($"调用 modpack 文件继续安装：{ModpackFile}")
                ModpackInstall(ModpackFile)
            End Sub)
        })
        Loader.Start(TargetFolder)
        LoaderTaskbarAdd(Loader)
        FrmMain.BtnExtraDownload.ShowRefresh()
        FrmMain.BtnExtraDownload.Ribble()
        Return Loader
    End Function

    Private Function InstallPackCompress(FileAddress As String, Archive As ZipArchive) As LoaderCombo(Of String)

        Dim Match As Match = Nothing
        Dim Regex As New Regex("^.*\/(?=versions\/(?<ver>[^\/]+)\/(\k<ver>)\.json$)", RegexOptions.IgnoreCase)
        For Each Entry In Archive.Entries
            Dim EntryMatch = Regex.Match("/" & Entry.FullName)
            If EntryMatch.Success Then
                Match = EntryMatch
                Exit For
            End If
        Next
        If Match Is Nothing Then Throw New Exception(GetLang("LangModModpackExceptionNoModpackFound"))
        Dim ArchiveBaseFolder As String = Match.Value.Replace("/", "\").TrimStart("\")
        Logger.Info($"检测到压缩包的 .minecraft 根目录：{ArchiveBaseFolder}，命中的版本名：{Match.Groups(1).Value}")

        MyMsgBox(GetLang("LangModModpackDialogContentInstallTip"), GetLang("LangModModpackDialogTitleInstall"), GetLang("LangDialogBtnContinue"), ForceWait:=True)
        Dim TargetFolder As String = Dialogs.SelectFolder(GetLang("LangModModpackSelectInstallFolder"), False).FirstOrDefault
        If TargetFolder Is Nothing Then Throw New OperationCanceledException
        If TargetFolder.Contains("!") OrElse TargetFolder.Contains(";") Then Hint(GetLang("LangModModpackFolderNoExclamationOrSemicolon"), HintType.Red) : Throw New OperationCanceledException
        If Not DirectoryUtils.IsEmpty(TargetFolder) Then Hint(GetLang("LangModModpackFolderShouldBeEmpty"), HintType.Red) : Throw New OperationCanceledException

        Dim Loader As New LoaderCombo(Of String)(GetLang("LangModModpackTaskExtractArchive"), {
            New LoaderTask(Of String, Integer)(GetLang("LangModModpackTaskExtractArchive"),
            Sub(Task As LoaderTask(Of String, Integer))
                ExtractModpackFiles(TargetFolder, FileAddress, Task, 0.95)

                PageSelectLeft.AddFolder(TargetFolder & ArchiveBaseFolder, PathUtils.GetLastPart(TargetFolder), False)
                Thread.Sleep(400)
                RunInUi(Sub() FrmMain.PageChange(FormMain.PageType.InstanceSelect))
            End Sub)
        }) With {.OnStateChanged = AddressOf McInstallState}
        Loader.Start(TargetFolder)
        LoaderTaskbarAdd(Loader)
        FrmMain.BtnExtraDownload.ShowRefresh()
        FrmMain.BtnExtraDownload.Ribble()
        Return Loader
    End Function

#End Region

End Module
