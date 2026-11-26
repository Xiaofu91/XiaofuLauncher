Public Module ModMinecraft

#Region "文件夹"

    Public Property McFolderSelected As String
        Get
            If _McFolderSelected Is Nothing Then SetMcFolder(Settings.Get(Of String)("LaunchFolderSelect"))
            Return _McFolderSelected
        End Get
        Set(Value As String)
            SetMcFolder(Value)
        End Set
    End Property
    Private Sub SetMcFolder(Value As String)
        If _McFolderSelected = Value Then Return
        _McFolderSelected = Value.Replace("$", Paths.Base)
        _McFolderSelected = PathUtils.AddSlashSuffix(PathUtils.ForCompare(_McFolderSelected))
        Settings.Set("LaunchFolderSelect", Value.Replace(Paths.Base, "$"))
        Logger.Info($"当前选择的 Minecraft 文件夹：{_McFolderSelected}")
    End Sub
    Private _McFolderSelected As String

    Public McFolderList As New List(Of McFolder)

    Public Class McFolder
        Public Name As String

        Public Location As String
        Public Type As Types
        Public Enum Types
            Vanilla
            RenamedVanilla
            Custom
        End Enum

        Public Overrides Function Equals(obj As Object) As Boolean
            If Not (TypeOf obj Is McFolder) Then Return False
            Dim Folder = DirectCast(obj, McFolder)
            Return Name = Folder.Name AndAlso Location = Folder.Location AndAlso Type = Folder.Type
        End Function
        Public Overrides Function ToString() As String
            Return Location
        End Function
    End Class

    Public McFolderListLoader As New LoaderTask(Of Integer, Integer)("Minecraft Folder List", AddressOf McFolderListLoadSub, Priority:=ThreadPriority.AboveNormal)
    Private Sub McFolderListLoadSub()
        Try

            Dim CacheMcFolderList = New List(Of McFolder)

#Region "读取默认（Original）文件夹，即当前、官启文件夹，可能没有结果"

            Try
                If DirectoryUtils.Exists(Paths.Base & "versions\") Then CacheMcFolderList.Add(New McFolder With {.Name = GetLang("LangModMinecraftCurrentFolder"), .Location = Paths.Base, .Type = McFolder.Types.Vanilla})
                For Each Folder As DirectoryInfo In DirectoryUtils.GetInfo(Paths.Base).GetDirectories
                    If DirectoryUtils.Exists(Folder.FullName & "versions\") OrElse Folder.Name = ".minecraft" Then
                        CacheMcFolderList.Add(New McFolder With {.Name = GetLang("LangModMinecraftCurrentFolder"), .Location = PathUtils.AddSlashSuffix(PathUtils.ForCompare(Folder.FullName)), .Type = McFolder.Types.Vanilla})
                    End If
                Next
            Catch ex As Exception
                Logger.Warn(ex, "扫描 XFL 所在文件夹中是否有 MC 文件夹失败")
            End Try

            Dim MojangPath As String = Paths.AppData & ".minecraft\"
            If (Not CacheMcFolderList.Any OrElse MojangPath <> CacheMcFolderList(0).Location) AndAlso
                DirectoryUtils.Exists(MojangPath & "versions\") Then
                CacheMcFolderList.Add(New McFolder With {.Name = GetLang("LangModMinecraftOfficialFolder"), .Location = MojangPath, .Type = McFolder.Types.Vanilla})
            End If

#End Region

#Region "读取自定义（Custom）文件夹，可能没有结果"

            For Each Folder As String In Settings.Get(Of String)("LaunchFolders").ToString.Split("|"c, True)
                If Folder = "" Then Continue For
                If Not Folder.Contains(">") OrElse Not Folder.EndsWithF("\") Then
                    Hint(GetLang("LangModMinecraftInvalidMcFolder", Folder), HintType.Red)
                    Continue For
                End If
                Dim FolderName As String = Folder.BeforeFirst(">"), FolderPath As String = Folder.AfterFirst(">")
                Try
                    CheckPermissionWithException(FolderPath)

                    Dim Renamed As Boolean = False
                    For Each OriginalFolder As McFolder In CacheMcFolderList
                        If OriginalFolder.Location = FolderPath Then
                            OriginalFolder.Name = FolderName
                            OriginalFolder.Type = McFolder.Types.RenamedVanilla
                            Renamed = True
                        End If
                    Next
                    If Not Renamed Then CacheMcFolderList.Add(New McFolder With {.Name = FolderName, .Location = PathUtils.AddSlashSuffix(PathUtils.ForCompare(FolderPath)), .Type = McFolder.Types.Custom})
                Catch ex As Exception
                    MyMsgBox(GetLang("LangModMinecraftInvalidMcFolder", FolderPath, ex.GetDisplay(False)), GetLang("LangModMinecraftDialogTitleInvalidMcFolder"), IsWarn:=True)
                    Logger.Warn(ex, $"无法访问 Minecraft 文件夹 {FolderPath}")
                End Try
            Next

            Dim NewSetup As New List(Of String)
            For Each Folder As McFolder In CacheMcFolderList
                If Not Folder.Type = McFolder.Types.Vanilla Then NewSetup.Add(Folder.Name & ">" & Folder.Location)
            Next
            If Not NewSetup.Any() Then NewSetup.Add("")
            Settings.Set("LaunchFolders", NewSetup.Join("|"c))

#End Region

            If Not CacheMcFolderList.Any() Then
                DirectoryUtils.Create(Paths.Base & ".minecraft\versions\")
                CacheMcFolderList.Add(New McFolder With {.Name = GetLang("LangModMinecraftCurrentFolder"), .Location = Paths.Base & ".minecraft\", .Type = McFolder.Types.Vanilla})
            End If

            For Each Folder As McFolder In CacheMcFolderList
                McFolderLauncherProfilesJsonCreate(Folder.Location)
            Next
            If Settings.Get(Of Boolean)("SystemDebugDelay") Then Thread.Sleep(RandomInteger(200, 2000))

            McFolderList = CacheMcFolderList

        Catch ex As Exception
            Logger.Error(ex, "加载 Minecraft 文件夹列表失败")
        End Try
    End Sub

    Public Sub McFolderLauncherProfilesJsonCreate(Folder As String)
        Try
            If FileUtils.Exists(Folder & "launcher_profiles.json") Then Return
            Dim ResultJson As String =
"{
    ""profiles"":  {
        ""XFL"": {
            ""icon"": ""Grass"",
            ""name"": ""XFL"",
            ""lastVersionId"": ""latest-release"",
            ""type"": ""latest-release"",
            ""lastUsed"": """ & Date.Now.ToString("yyyy'-'MM'-'dd") & "T" & Date.Now.ToString("HH':'mm':'ss") & ".0000Z""
        }
    },
    ""selectedProfile"": ""XFL"",
    ""clientToken"": ""23323323323323323323323323323333""
}"
            FileUtils.Write(Folder & "launcher_profiles.json", ResultJson, encoding:=Encoding.GetEncoding("GB18030"))
            Logger.Info($"已创建 launcher_profiles.json：{Folder}")
        Catch ex As Exception
            Logger.Error(ex, $"创建 launcher_profiles.json 失败（{Folder}）")
        End Try
    End Sub

#End Region

#Region "版本处理"

    Private _McInstanceSelected As McInstance

    Public Property McInstanceSelected As McInstance
        Get
            Return _McInstanceSelected
        End Get
        Set(value As McInstance)
            Static _McInstanceSelectedLast As Object = 0
            If ReferenceEquals(_McInstanceSelectedLast, value) Then Return
            _McInstanceSelected = value
            _McInstanceSelectedLast = value

            Logger.Info($"当前选择的 Minecraft 版本：{If(value?.PathVersion, "无")}")
            WriteIni(McFolderSelected & "XFL.ini", "Version", If(value?.Name, ""))
            If value Is Nothing Then Return

            PageDownloadResourceDetail.CachedFolder.Clear()

            If AniControlEnabled = 0 AndAlso
               Settings.Get(Of String)("VersionServerNide", Instance:=value) <> Settings.Get(Of String)("CacheNideServer") AndAlso
               Settings.Get(Of Integer)("VersionServerLogin", Instance:=value) = 3 Then
                Settings.Set("CacheNideAccess", "")
                Logger.Info("服务器改变，要求重新登录统一通行证")
            End If
            If Settings.Get(Of Integer)("VersionServerLogin", Instance:=value) = 3 Then
                Settings.Set("CacheNideServer", Settings.Get(Of String)("VersionServerNide", Instance:=value))
            End If

            If AniControlEnabled = 0 AndAlso
               Settings.Get(Of String)("VersionServerAuthServer", Instance:=value) <> Settings.Get(Of String)("CacheAuthServerServer") AndAlso
               Settings.Get(Of Integer)("VersionServerLogin", Instance:=value) = 4 Then
                Settings.Set("CacheAuthAccess", "")
                Logger.Info("服务器改变，要求重新登录 Authlib-Injector")
            End If
            If Settings.Get(Of Integer)("VersionServerLogin", Instance:=value) = 4 Then
                Settings.Set("CacheAuthServerServer", Settings.Get(Of String)("VersionServerAuthServer", Instance:=value))
                Settings.Set("CacheAuthServerName", Settings.Get(Of String)("VersionServerAuthName", Instance:=value))
                Settings.Set("CacheAuthServerRegister", Settings.Get(Of String)("VersionServerAuthRegister", Instance:=value))
            End If
        End Set
    End Property

    Public Class McInstance

        Public ReadOnly Property PathVersion As String

        Public ReadOnly Property PathIndie As String
            Get
                If Not Settings.HasSaved("VersionArgumentIndieV2", Instance:=Me) Then
                    If Not IsLoaded Then Load()

                    Dim ShouldBeIndie =
                    Function() As Boolean

                        If Settings.HasSaved("VersionArgumentIndie", Instance:=Me) AndAlso Settings.Get(Of Integer)("VersionArgumentIndie", Instance:=Me) > 0 Then
                            Logger.Info($"版本隔离初始化（{Name}）：从老的版本独立设置中迁移")
                            Return Settings.Get(Of Integer)("VersionArgumentIndie", Instance:=Me) = 1
                        End If

                        Dim ModFolder = DirectoryUtils.GetInfo(PathVersion & "mods\")
                        Dim SaveFolder = DirectoryUtils.GetInfo(PathVersion & "saves\")
                        If (ModFolder.Exists AndAlso ModFolder.EnumerateFiles.Any) OrElse (SaveFolder.Exists AndAlso SaveFolder.EnumerateDirectories.Any) Then
                            Logger.Info($"版本隔离初始化（{Name}）：版本文件夹下存在 mods 或 saves 文件夹，自动开启")
                            Return True
                        End If

                        Dim IsRelease As Boolean = State <> McInstanceState.Fool AndAlso State <> McInstanceState.Old AndAlso State <> McInstanceState.Snapshot
                        Logger.Info($"版本隔离初始化（{Name}）：从全局默认设置中（{Settings.Get(Of Integer)("LaunchArgumentIndieV2")}）判断，State {State}，IsRelease {IsRelease}，Modable {Modable}")
                        Select Case Settings.Get(Of Integer)("LaunchArgumentIndieV2")
                            Case 0
                                Return False
                            Case 1
                                Return Modable
                            Case 2
                                Return Not IsRelease
                            Case 3
                                Return Not IsRelease OrElse Modable
                            Case Else
                                Return True
                        End Select
                    End Function
                    Settings.Set("VersionArgumentIndieV2", ShouldBeIndie(), Instance:=Me)
                End If
                Return If(Settings.Get(Of Boolean)("VersionArgumentIndieV2", Instance:=Me), PathVersion, McFolderSelected)
            End Get
        End Property

        Public ReadOnly Property Name As String
            Get
                If _Name Is Nothing AndAlso Not PathVersion = "" Then _Name = PathUtils.GetLastPart(PathVersion)
                Return _Name
            End Get
        End Property
        Private _Name As String = Nothing

        Public Info As String = GetLang("LangModMinecraftNotLoaded")

        Public State As McInstanceState = McInstanceState.Error

        Public Logo As String

        Public IsStar As Boolean = False

        Public DisplayType As McInstanceCardType = McInstanceCardType.Auto

        Public ReadOnly Property Modable As Boolean
            Get
                If Not IsLoaded Then Load()
                Return Version.HasFabric OrElse Version.HasForgelike OrElse Version.HasLiteLoader OrElse
                    DisplayType = McInstanceCardType.API
            End Get
        End Property

        Public Property Version As McVersion
            Get
                If _Version IsNot Nothing Then Return _Version
                _Version = New McVersion
#Region "获取游戏版本"
                Try

                    Try
                        If JsonObject("releaseTime") Is Nothing Then
                            ReleaseTime = New Date(1970, 1, 1, 15, 0, 0)
                        Else
                            ReleaseTime = JsonObject("releaseTime").ToObject(Of Date)
                        End If
                        If ReleaseTime.Year > 2000 AndAlso ReleaseTime.Year < 2013 Then
                            _Version.VanillaName = "Old"
                            GoTo VersionSearchFinish
                        End If
                    Catch
                        ReleaseTime = New Date(1970, 1, 1, 15, 0, 0)
                    End Try

                    If If(JsonObject("type"), "") = "pending" Then
                        _Version.VanillaName = "pending"
                        GoTo VersionSearchFinish
                    End If

                    If JsonObject("clientVersion") IsNot Nothing Then
                        _Version.VanillaName = JsonObject("clientVersion")
                        GoTo VersionSearchFinish
                    End If

                    If JsonObject("patches") IsNot Nothing Then
                        For Each Patch As JObject In JsonObject("patches")
                            If If(Patch("id"), "").ToString = "game" AndAlso Patch("version") IsNot Nothing Then
                                _Version.VanillaName = Patch("version").ToString
                                GoTo VersionSearchFinish
                            End If
                        Next
                    End If

                    If JsonObject("arguments") IsNot Nothing AndAlso JsonObject("arguments")("game") IsNot Nothing Then
                        Dim Mark As Boolean = False
                        For Each Argument In JsonObject("arguments")("game")
                            If Mark Then
                                _Version.VanillaName = Argument.ToString
                                GoTo VersionSearchFinish
                            End If
                            If Argument.ToString = "--fml.mcVersion" Then Mark = True
                        Next
                    End If

                    If Not InheritName = "" Then
                        _Version.VanillaName = If(JsonObject("jar"), "").ToString
                        If _Version.VanillaName = "" Then _Version.VanillaName = InheritName
                        GoTo VersionSearchFinish
                    End If

                    Dim Regex As String = If(JsonObject("downloads"), "").ToString.RegexSeek("(?<=launcher.mojang.com/mc/game/)[^/]*")
                    If Regex IsNot Nothing Then
                        _Version.VanillaName = Regex
                        GoTo VersionSearchFinish
                    End If

                    Dim LibrariesString As String = JsonObject("libraries").ToString
                    Regex = If(LibrariesString.RegexSeek("(?<=net.minecraftforge:forge:)[0-9]{1,2}.[0-9+.]+"), LibrariesString.RegexSeek("(?<=net.minecraftforge:fmlloader:)[0-9]{1,2}.[0-9+.]+"))
                    If Regex IsNot Nothing Then
                        _Version.VanillaName = Regex
                        GoTo VersionSearchFinish
                    End If

                    Regex = LibrariesString.RegexSeek("(?<=optifine:OptiFine:)[0-9]{1,2}.[0-9+.]+")
                    If Regex IsNot Nothing Then
                        _Version.VanillaName = Regex
                        GoTo VersionSearchFinish
                    End If

                    Regex = LibrariesString.RegexSeek("(?<=((fabricmc)|(quiltmc)):intermediary:)[^""]*")
                    If Regex IsNot Nothing Then
                        _Version.VanillaName = Regex
                        GoTo VersionSearchFinish
                    End If

                    If JsonObject("jar") IsNot Nothing Then
                        _Version.VanillaName = JsonObject("jar").ToString
                        GoTo VersionSearchFinish
                    End If

                    If JsonVersion?("name") IsNot Nothing Then
                        Dim JsonVerName As String = JsonVersion("name").ToString
                        If JsonVerName.Length < 32 Then
                            _Version.VanillaName = JsonVerName
                            Logger.Info($"从版本 jar 中的 version.json 获取到版本号：{JsonVerName}")
                            GoTo VersionSearchFinish
                        End If
                    End If

                    Static Pattern = "(([1-9][0-9]w[0-9]{2}[a-g])|((1|[2-9][0-9])\.[0-9]+(\.[0-9]+)?(-(pre|rc|snapshot-?)[1-9]*| Pre-Release( [1-9])?)?))(_unobfuscated)?"
                    Regex = JsonObject("id").ToString.RegexSeek(Pattern, RegularExpressions.RegexOptions.IgnoreCase)
                    If Regex IsNot Nothing Then
                        _Version.VanillaName = Regex
                        GoTo VersionSearchFinish
                    End If

                    Logger.Info($"无法完全确认 MC 版本号的版本：{Name}")
                    _Version.Reliable = False

                    Regex = Name.RegexSeek(Pattern, RegularExpressions.RegexOptions.IgnoreCase)
                    If Regex IsNot Nothing Then
                        _Version.VanillaName = Regex
                        GoTo VersionSearchFinish
                    End If

                    Dim JsonRaw As JObject = JsonObject.DeepClone()
                    JsonRaw.Remove("libraries")
                    Dim JsonRawText As String = JsonRaw.ToString
                    Regex = JsonRawText.RegexSeek(Pattern, RegularExpressions.RegexOptions.IgnoreCase)
                    If Regex IsNot Nothing Then
                        _Version.VanillaName = Regex
                        GoTo VersionSearchFinish
                    End If

                    _Version.VanillaName = "Unknown"
                    Info = GetLang("LangModMinecraftUnableGetVersion")
                Catch ex As Exception
                    Logger.Warn(ex, "识别 Minecraft 版本时出错")
                    _Version.VanillaName = "Unknown"
                    Info = GetLang("LangModMinecraftGetVersionFail", ex.Message)
                End Try
#End Region
VersionSearchFinish:

                _Version.VanillaName = _Version.VanillaName.Replace("_unobfuscated", "").Replace(" Unobfuscated", "")
                Return _Version
            End Get
            Set(value As McVersion)
                _Version = value
            End Set
        End Property
        Private _Version As McVersion = Nothing

        Public ReleaseTime As New Date(1970, 1, 1, 15, 0, 0)

        Public Function GetJsonPath() As String
            Dim JsonPath As String = PathVersion & Name & ".json"
            If Not FileUtils.Exists(JsonPath) Then

                JsonPath = Nothing
                For Each JsonCandidatePath In DirectoryUtils.EnumerateFiles(PathVersion, searchPattern:="*.json")
                    Try
                        Dim JsonCandidate As JObject = FileUtils.ReadAsJson(JsonCandidatePath)
                        If Not JsonCandidate.ContainsKey("mainClass") Then Continue For
                        If Not JsonCandidate.ContainsKey("type") Then Continue For
                        If Not JsonCandidate.ContainsKey("id") Then Continue For
                    Catch
                        Continue For
                    End Try
                    JsonPath = JsonCandidatePath
                    Exit For
                Next
                If JsonPath Is Nothing Then
                    Throw New Exception($"未找到版本 JSON 文件：{PathVersion}{Name}.json")
                Else
                    Logger.Warn($"未找到同名版本 JSON，自动换用 {JsonPath}")
                End If
            End If
            Return JsonPath
        End Function

        Public Property JsonText As String
            Get

                Dim FastJsonCheck =
                Function(Json As String) As Boolean
                    Dim TrimedJson As String = Json.Trim()
                    Return TrimedJson.StartsWithF("{") AndAlso TrimedJson.EndsWithF("}")
                End Function
                If _JsonText Is Nothing Then
                    Dim JsonPath As String = GetJsonPath()
                    _JsonText = If(FileUtils.TryReadAsString(JsonPath), "")

                    If Not FastJsonCheck(_JsonText) Then
                        If RunInUi() Then
                            Logger.Warn("版本 JSON 文件为空或有误，由于代码在主线程运行，将不再进行重试")
                            _JsonText.DeserializeJson()
                        Else
                            Logger.Warn($"版本 JSON 文件为空或有误，将在 2s 后重试读取（{JsonPath}）")
                            Thread.Sleep(2000)
                            _JsonText = FileUtils.ReadAsString(JsonPath)
                            If Not FastJsonCheck(_JsonText) Then _JsonText.DeserializeJson()
                        End If
                    End If
                End If
                Return _JsonText
            End Get
            Set(value As String)
                _JsonText = value
            End Set
        End Property
        Private _JsonText As String = Nothing

        Public Property JsonObject As JObject
            Get
                If _JsonObject Is Nothing Then
                    Dim Text As String = JsonText
                    Try
                        _JsonObject = Text.DeserializeJson()

                        If _JsonObject.ContainsKey("patches") AndAlso Not _JsonObject.ContainsKey("time") Then
                            IsHmclFormatJson = True

                            Dim CurrentObject As JObject = Nothing
                            Dim SubjsonList As New List(Of JObject)
                            For Each Subjson As JObject In _JsonObject("patches")
                                SubjsonList.Add(Subjson)
                            Next
                            SubjsonList = SubjsonList.SortByComparison(
                                Function(Left, Right) Val(If(Left("priority"), "0").ToString) < Val(If(Right("priority"), "0").ToString))
                            For Each Subjson As JObject In SubjsonList
                                Dim Id As String = Subjson("id")
                                If Id IsNot Nothing Then

                                    Logger.Info($"合并 HMCL 分支项：{Id}")
                                    If CurrentObject IsNot Nothing Then
                                        CurrentObject.Merge(Subjson)
                                    Else
                                        CurrentObject = Subjson
                                    End If
                                Else
                                    Logger.Info("存在为空的 HMCL 分支项")
                                End If
                            Next
                            _JsonObject = CurrentObject

                            _JsonObject("id") = Name
                            If _JsonObject.ContainsKey("inheritsFrom") Then _JsonObject.Remove("inheritsFrom")
                        End If

                        Dim InstanceNameInheritFrom = Nothing
                        Try
                            InstanceNameInheritFrom = If(_JsonObject("inheritsFrom") Is Nothing, "", _JsonObject("inheritsFrom").ToString)
                            If InstanceNameInheritFrom = Name Then
                                Logger.Warn($"自引用的继承版本：{Name}")
                                InstanceNameInheritFrom = ""
                                Exit Try
                            End If
Recheck:
                            If InstanceNameInheritFrom <> "" Then
                                Dim InheritInstance As New McInstance(InstanceNameInheritFrom)

                                If InheritInstance.InheritName = InstanceNameInheritFrom Then Throw New Exception(GetLang("LangModMinecraftExceptionDependencyNestingAppears", InstanceNameInheritFrom))
                                InstanceNameInheritFrom = InheritInstance.InheritName

                                Dim CurrentLib As JArray = _JsonObject("libraries").DeepClone()
                                For Each LibToken In InheritInstance.JsonObject("libraries")
                                    CurrentLib.Add(LibToken)
                                Next

                                InheritInstance.JsonObject.Merge(_JsonObject)
                                _JsonObject = InheritInstance.JsonObject
                                _JsonObject("libraries") = CurrentLib
                                GoTo Recheck
                            End If
                        Catch ex As Exception
                            Logger.Warn(ex, $"合并版本依赖项 JSON 失败（{If(InstanceNameInheritFrom, "null")}）")
                        End Try
                    Catch ex As Exception
                        Throw New Exception("初始化版本 JSON 时失败（" & If(Name, "null") & "）", ex)
                    End Try
                End If
                Return _JsonObject
            End Get
            Set(value As JObject)
                _JsonObject = value
            End Set
        End Property
        Private _JsonObject As JObject = Nothing

        Public ReadOnly Property IsOldJson As Boolean
            Get
                Return JsonObject("minecraftArguments") IsNot Nothing AndAlso JsonObject("minecraftArguments") <> ""
            End Get
        End Property

        Public Property IsHmclFormatJson As Boolean = False

        Public ReadOnly Property JsonVersion As JObject
            Get
                Static JsonVersionInited As Boolean = False
                If Not JsonVersionInited Then
                    JsonVersionInited = True
                    Try
                        If Not FileUtils.Exists($"{PathVersion}{Name}.jar") Then Exit Try
                        Using JarArchive = FileUtils.OpenZip(PathVersion & Name & ".jar")
                            Dim VersionJson As ZipArchiveEntry = JarArchive.GetEntry("version.json")
                            If VersionJson Is Nothing Then Exit Try
                            Using VersionJsonStream As New StreamReader(VersionJson.Open)
                                _JsonVersion = VersionJsonStream.ReadToEnd().DeserializeJson()
                            End Using
                        End Using
                    Catch ex As Exception
                        Logger.Warn(ex, $"从版本 jar 中读取 version.json 失败（{PathVersion}{Name}.jar）")
                    End Try
                End If
                Return _JsonVersion
            End Get
        End Property
        Private _JsonVersion As JObject = Nothing

        Public ReadOnly Property InheritName As String
            Get
                If _InheritName Is Nothing Then
                    _InheritName = If(JsonObject("inheritsFrom"), "").ToString

                    If JsonText.Contains("liteloader") AndAlso Version.VanillaName <> Name AndAlso Not JsonText.Contains("logging") Then
                        If If(JsonObject("jar"), Version.VanillaName).ToString = Version.VanillaName Then _InheritName = Version.VanillaName
                    End If

                    If IsHmclFormatJson Then _InheritName = ""
                End If
                Return _InheritName
            End Get
        End Property
        Private _InheritName As String = Nothing

        Public Config As JsonConfigProvider
        Public Sub ResetSettingsCache()
            IniClearCache(PathIndie & "options.txt")
            IniClearCache(PathVersion & "XFL\Setup.ini")
            Config.DiscardCache()
            For Each Entry In Settings.Entries.Values
                If Entry.Source = Settings.Sources.Instance Then Entry.ValueCache.Remove(PathVersion)
            Next
        End Sub

        Public Sub New(NameOrFullPath As String)
            NameOrFullPath = PathUtils.RemoveExtendedPrefix(NameOrFullPath)
            PathVersion = PathUtils.AddSlashSuffix(If(NameOrFullPath.Contains(":"), "", McFolderSelected & "versions\") & NameOrFullPath)
            Config = New JsonConfigProvider(Path.Combine(PathVersion, "XFL\config.json"))
        End Sub

        Public Function Check() As Boolean

            If Not DirectoryUtils.Exists(PathVersion) Then
                State = McInstanceState.Error
                Info = GetLang("LangModMinecraftCheckStatusInstanceNotFound") & " " & Name
                Return False
            End If

            Try
                If JsonObject("mainClass") Is Nothing Then Throw New Exception("版本 JSON 中没有 mainClass 项！")
            Catch ex As Exception
                Logger.Warn(ex, $"版本 JSON 可用性检查失败（{PathVersion}）")
                JsonText = ""
                JsonObject = Nothing
                Info = ex.Message
                State = McInstanceState.Error
                Return False
            End Try

            Try
                If String.IsNullOrEmpty(Version.VanillaName) Then Throw New Exception("无法获取版本号，结果为空")
            Catch ex As Exception
                Logger.Warn(ex, $"版本号获取失败（{Name}）")
                State = McInstanceState.Error
                Info = "版本号获取失败：" & ex.GetDisplay(False)
                Return False
            End Try

            Try
                If Not InheritName = "" Then
                    If Not FileUtils.Exists($"{PathUtils.RemoveLastPart(PathVersion)}\{InheritName}\{InheritName}.json") Then
                        State = McInstanceState.Error
                        Info = GetLang("LangModMinecraftCheckStatusNeedDependency") & InheritName
                        Return False
                    End If
                End If
            Catch ex As Exception
                Logger.Warn(ex, $"依赖版本检查出错（{Name}）")
                State = McInstanceState.Error
                Info = GetLang("LangModMinecraftCheckStatusUnknownError") & ex.GetDisplay(False)
                Return False
            End Try

            State = McInstanceState.Original
            Return True
        End Function

        Public Function Load() As McInstance
            Try

                If Not Check() Then GoTo ExitDataLoad
#Region "确定版本分类"
                Select Case Version.VanillaName
                    Case "Unknown"
                        State = McInstanceState.Error
                    Case "Old"
                        State = McInstanceState.Old
                    Case Else
                        Dim RealJson As String = If(JsonObject, JsonText).ToString

                        Dim FixedReleaseTime = ReleaseTime.ToUniversalTime().AddHours(2)
                        Dim JsonType = If(JsonObject("type"), "").ToString
                        If JsonType = "fool" OrElse
                           (FixedReleaseTime.Month = 4 AndAlso FixedReleaseTime.Day = 1 AndAlso JsonType = "snapshot") OrElse
                           GetMcFoolName(Version.VanillaName) <> "" Then
                            State = McInstanceState.Fool
                        ElseIf IsSnapshot() Then
                            State = McInstanceState.Snapshot
                        End If

                        If RealJson.Contains("optifine") Then
                            State = McInstanceState.OptiFine
                            Version.OptiFine = If(RealJson.RegexSeek("(?<=HD_U_)[^"":/]+"), "未知版本")
                        End If

                        If RealJson.Contains("liteloader") Then
                            State = McInstanceState.LiteLoader
                            Version.HasLiteLoader = True
                        End If

                        If RealJson.Contains("net.fabricmc:fabric-loader") OrElse RealJson.Contains("org.quiltmc:quilt-loader") Then
                            State = McInstanceState.Fabric
                            Version.Fabric = If(RealJson.RegexSeek("(?<=(net.fabricmc:fabric-loader:)|(org.quiltmc:quilt-loader:))[0-9\.]+(\+build.[0-9]+)?"), "未知版本").Replace("+build", "")
                        ElseIf RealJson.Contains("minecraftforge") AndAlso Not RealJson.Contains("net.neoforge") Then
                            State = McInstanceState.Forge
                            Version.Forge = RealJson.RegexSeek("(?<=forge:[0-9\.]+(_pre[0-9]*)?\-)[0-9\.]+")
                            If Version.Forge Is Nothing Then Version.Forge = RealJson.RegexSeek("(?<=net\.minecraftforge:minecraftforge:)[0-9\.]+")
                            If Version.Forge Is Nothing Then Version.Forge = If(RealJson.RegexSeek("(?<=net\.minecraftforge:fmlloader:[0-9\.]+-)[0-9\.]+"), "未知版本")
                        ElseIf RealJson.Contains("net.neoforge") Then

                            State = McInstanceState.NeoForge
                            Version.NeoForge = If(RealJson.RegexSeek("(?<=orgeVersion"",[^""]*?"")[^""]+(?="",)"), "未知版本")
                        End If
                End Select
#End Region
ExitDataLoad:

                Logo = ReadIni(PathVersion & "XFL\Setup.ini", "Logo", "")
                If Logo = "" OrElse Not CType(ReadIni(PathVersion & "XFL\Setup.ini", "LogoCustom", False), Boolean) Then
                    Select Case State
                        Case McInstanceState.Original
                            Logo = PathImage & "Blocks/Grass.png"
                        Case McInstanceState.Snapshot
                            Logo = PathImage & "Blocks/CommandBlock.png"
                        Case McInstanceState.Old
                            Logo = PathImage & "Blocks/CobbleStone.png"
                        Case McInstanceState.Forge
                            Logo = PathImage & "Blocks/Anvil.png"
                        Case McInstanceState.NeoForge
                            Logo = PathImage & "Blocks/NeoForge.png"
                        Case McInstanceState.Fabric
                            Logo = PathImage & "Blocks/Fabric.png"
                        Case McInstanceState.OptiFine
                            Logo = PathImage & "Blocks/GrassPath.png"
                        Case McInstanceState.LiteLoader
                            Logo = PathImage & "Blocks/Egg.png"
                        Case McInstanceState.Fool
                            Logo = PathImage & "Blocks/GoldBlock.png"
                        Case Else
                            Logo = PathImage & "Blocks/RedstoneBlock.png"
                    End Select
                End If

                Dim CustomInfo As String = ReadIni(PathVersion & "XFL\Setup.ini", "CustomInfo")
                If CustomInfo <> "" Then
                    Info = CustomInfo
                Else
                    Info = VersionDisplayName()
                    If State = McInstanceState.Fool Then
                        Info = GetMcFoolName(Version.VanillaName)
                    ElseIf State <> McInstanceState.Error Then
                        If Settings.Get(Of Integer)("VersionServerLogin", Instance:=Me) = 3 Then Info += ", 统一通行证验证"
                        If Settings.Get(Of Integer)("VersionServerLogin", Instance:=Me) = 4 Then Info += ", Authlib 验证"
                    End If
                End If

                IsStar = ReadIni(PathVersion & "XFL\Setup.ini", "IsStar", False)

                DisplayType = ReadIni(PathVersion & "XFL\Setup.ini", "DisplayType", McInstanceCardType.Auto)

                If DirectoryUtils.Exists(PathVersion) Then
                    WriteIni(PathVersion & "XFL\Setup.ini", "State", State)
                    WriteIni(PathVersion & "XFL\Setup.ini", "Info", Info)
                    WriteIni(PathVersion & "XFL\Setup.ini", "Logo", Logo)
                End If
                If State <> McInstanceState.Error AndAlso Version.Reliable Then
                    WriteIni(PathVersion & "XFL\Setup.ini", "ReleaseTime", ReleaseTime.ToString("yyyy'-'MM'-'dd HH':'mm"))
                    WriteIni(PathVersion & "XFL\Setup.ini", "VersionFabric", Version.Fabric)
                    WriteIni(PathVersion & "XFL\Setup.ini", "VersionOptiFine", Version.OptiFine)
                    WriteIni(PathVersion & "XFL\Setup.ini", "VersionLiteLoader", Version.HasLiteLoader)
                    WriteIni(PathVersion & "XFL\Setup.ini", "VersionForge", Version.Forge)
                    WriteIni(PathVersion & "XFL\Setup.ini", "VersionNeoForge", Version.NeoForge)
                    WriteIni(PathVersion & "XFL\Setup.ini", "VersionVanillaName", Version.VanillaName)
                    WriteIni(PathVersion & "XFL\Setup.ini", "VersionVanilla", Version.Vanilla.ToString)
                End If
            Catch ex As Exception
                Info = ex.GetDisplay(False)
                Logo = PathImage & "Blocks/RedstoneBlock.png"
                State = McInstanceState.Error
                Logger.Warn(ex, $"加载版本失败（{Name}）")
            Finally
                IsLoaded = True
            End Try
            Return Me
        End Function
        Private Function IsSnapshot() As Boolean
            Dim jsonType As String = If(JsonObject("type"), "").ToString
            Return {"w", "snapshot", "rc", "pre", "experimental", "-"}.Any(Function(s) Version.VanillaName.ContainsIgnoreCase(s)) OrElse
                Name.ContainsIgnoreCase("combat") OrElse
                jsonType = "snapshot" OrElse jsonType = "pending" OrElse jsonType = "fool"
        End Function

        Public IsLoaded As Boolean = False

        Public Function VersionDisplayName() As String

            Dim ModLoaderInfo As String = ""
            If Version.HasForge Then ModLoaderInfo += ", Forge" & If(Version.Forge = GetLang("LangDownloadUnknown"), "", " " & Version.Forge)
            If Version.HasNeoForge Then ModLoaderInfo += ", NeoForge" & If(Version.NeoForge = GetLang("LangDownloadUnknown"), "", " " & Version.NeoForge)
            If Version.HasFabric Then ModLoaderInfo += ", Fabric" & If(Version.Fabric = GetLang("LangDownloadUnknown"), "", " " & Version.Fabric)
            If Version.HasOptiFine Then ModLoaderInfo += ", OptiFine" & If(Version.OptiFine = GetLang("LangDownloadUnknown"), "", " " & Version.OptiFine.Replace("-", " ").Replace("_", " "))
            If Version.HasLiteLoader Then ModLoaderInfo += ", LiteLoader"

            Dim Info As String
            Select Case State
                Case McInstanceState.Snapshot, McInstanceState.Original, McInstanceState.Forge, McInstanceState.NeoForge, McInstanceState.Fabric, McInstanceState.OptiFine, McInstanceState.LiteLoader
                    If Version.VanillaName.ContainsIgnoreCase("pre") Then
                        Info = GetLang("LangModMinecraftVersionPre") & " " & Version.VanillaName
                    ElseIf Version.VanillaName.ContainsIgnoreCase("rc") Then
                        Info = GetLang("LangModMinecraftVersionRC") & " " & Version.VanillaName
                    ElseIf Version.VanillaName.ContainsIgnoreCase("experimental") Then
                        Info = "实验性快照" & Version.VanillaName
                    ElseIf Version.VanillaName = "pending" Then
                        Info = GetLang("LangModMinecraftVersionExperimental")
                    ElseIf IsSnapshot() Then
                        Info = If(Version.Reliable, GetLang("LangModMinecraftVersionSnapshot") & " " & Version.VanillaName.Replace("-snapshot", ""), GetLang("LangModMinecraftVersionSnapshot"))
                    Else
                        Info = If(Version.Reliable,  GetLang("LangModMinecraftVersionStable") & " " & Version.VanillaName, GetLang("LangModMinecraftVersionStable"))
                    End If
                Case McInstanceState.Old
                    Info = GetLang("LangModMinecraftVersionOld")
                Case McInstanceState.Fool
                    Info = "愚人节版本 " & Version.VanillaName
                Case McInstanceState.Error
                    Return Me.Info
                Case Else
                    Return GetLang("LangModMinecraftUnknownError")
            End Select
            Return (Info & ModLoaderInfo).Replace("_", "-")
        End Function

        Public Function ToListItem(Optional ContentHandler As Action(Of MyListItem, EventArgs) = Nothing) As MyVirtualizingElement(Of MyListItem)
            Return New MyVirtualizingElement(Of MyListItem)(
            Function()
                Dim NewItem As New MyListItem With {
                    .Info = Info, .Height = 42, .Tag = Me, .SnapsToDevicePixels = True, .Type = MyListItem.CheckType.Clickable,
                    .ContentHandler = ContentHandler
                }

                NewItem.Inlines.Clear()
                NewItem.Inlines.Add(New Run(Name))
                If ReadIni(PathVersion & "XFL\Setup.ini", "CustomInfo") <> "" Then
                    NewItem.Inlines.Add(New Run("  |  " & VersionDisplayName()) With {.Foreground = New MyColor(215, 215, 215), .FontSize = 12})
                End If

                Try
                    If Logo.EndsWithF("XFL\Logo.png") Then
                        NewItem.Logo = PathVersion & "XFL\Logo.png"
                    Else
                        NewItem.Logo = Logo
                    End If
                Catch ex As Exception
                    Logger.Error(ex, "加载版本图标失败", LogBehavior.Toast)
                    NewItem.Logo = "pack://application:,,,/images/Blocks/RedstoneBlock.png"
                End Try
                Return NewItem
            End Function) With {.Height = 42}
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim version = TryCast(obj, McInstance)
            Return version IsNot Nothing AndAlso PathVersion = version.PathVersion
        End Function
        Public Shared Operator =(a As McInstance, b As McInstance) As Boolean
            If a Is Nothing AndAlso b Is Nothing Then Return True
            If a Is Nothing OrElse b Is Nothing Then Return False
            Return a.PathVersion = b.PathVersion
        End Operator
        Public Shared Operator <>(a As McInstance, b As McInstance) As Boolean
            Return Not (a = b)
        End Operator
    End Class
    Public Enum McInstanceState
        [Error]
        Original
        Snapshot
        Fool
        OptiFine
        Old
        Forge
        NeoForge
        LiteLoader
        Fabric
    End Enum

    Public Class McVersion

        Public Property VanillaName As String
            Get
                Return _VanillaName
            End Get
            Set(value As String)
                If _VanillaName = value Then Return
                _VanillaName = value
                Vanilla = NameToVersion(_VanillaName)
            End Set
        End Property
        Private _VanillaName As String

        Public Vanilla As Version

        Public Reliable As Boolean = True

        Public ReadOnly Property Vaild As Boolean
            Get
                Return Vanilla.Major < 1000
            End Get
        End Property

        Public ReadOnly Property Drop As Integer
            Get
                Return If(Vaild, Vanilla.Major * 10 + Vanilla.Minor, 209)
            End Get
        End Property

        Public ReadOnly Property HasOptiFine As Boolean
            Get
                Return OptiFine IsNot Nothing
            End Get
        End Property

        Public OptiFine As String = Nothing

        Public ReadOnly Property OptiFineCode As Integer
            Get
                If OptiFine Is Nothing OrElse OptiFine = "未知版本" Then Return 0

                Dim Result As Integer = Asc(OptiFine.Upper.First) - Asc("A"c) + 1

                Result *= 100
                Result += Val(Right(OptiFine, OptiFine.Length - 1).RegexSeek("[0-9]+"))

                Result *= 100
                If OptiFine.ContainsIgnoreCase("pre") Then Result += 50
                If OptiFine.ContainsIgnoreCase("pre") OrElse OptiFine.ContainsIgnoreCase("beta") Then
                    If Val(Right(OptiFine, 1)) = 0 AndAlso Right(OptiFine, 1) <> "0" Then
                        Result += 1
                    Else
                        Result += Val(OptiFine.Lower.RegexSeek("(?<=((pre)|(beta)))[0-9]+"))
                    End If
                Else
                    Result += 99
                End If
                Return Result
            End Get
        End Property

        Public ReadOnly Property HasForge As Boolean
            Get
                Return Forge IsNot Nothing
            End Get
        End Property

        Public ReadOnly Property HasNeoForge As Boolean
            Get
                Return NeoForge IsNot Nothing
            End Get
        End Property

        Public ReadOnly Property HasForgelike As Boolean
            Get
                Return HasForge OrElse HasNeoForge
            End Get
        End Property

        Public Forge As String = Nothing

        Public NeoForge As String = Nothing

        Public ReadOnly Property ForgelikeCode As Integer
            Get
                If Not HasForgelike Then Return 0
                If (Forge Is Nothing OrElse Forge = "未知版本") AndAlso
                   (NeoForge Is Nothing OrElse NeoForge = "未知版本") Then Return 0
                Dim Segments = If(HasForge, Forge, NeoForge).RegexSearch("\d+").ToList
                Select Case Segments.Count
                    Case Is > 4
                        Return Val(Segments(0)) * 1000000 + Val(Segments(1)) * 10000 + Val(Segments(3))
                    Case 3
                        Return Val(Segments(0)) * 1000000 + Val(Segments(1)) * 10000 + Val(Segments(2))
                    Case 2
                        Return Val(Segments(0)) * 1000000 + Val(Segments(1)) * 10000
                    Case Else
                        Return Val(Segments(0)) * 1000000
                End Select
            End Get
        End Property

        Public ReadOnly Property HasFabric As Boolean
            Get
                Return Fabric IsNot Nothing
            End Get
        End Property

        Public Fabric As String = Nothing

        Public HasLiteLoader As Boolean = False

        Public ReadOnly Property ModLoaders As ModLoaders
            Get
                Dim Result As ModLoaders = ModLoaders.None
                If HasForge Then Result = Result Or ModLoaders.Forge
                If HasNeoForge Then Result = Result Or ModLoaders.NeoForge
                If HasFabric Then Result = Result Or ModLoaders.Fabric
                If HasLiteLoader Then Result = Result Or ModLoaders.LiteLoader
                Return Result
            End Get
        End Property

        Public Shared Function IsFormatFit(Version As String) As Boolean
            If Version Is Nothing Then Return False
            If Version.RegexCheck("^1\.\d") Then Return True
            If Val(Version.RegexSeek("^[2-9]\d\.\d+")) >= 26 Then Return True
            Return False
        End Function

        Public Shared Function VersionToDrop(VanillaName As String) As Integer
            If VanillaName Is Nothing Then Return 209
            Dim Version = NameToVersion(VanillaName)
            If Version.Major >= 1000 Then Return 209
            Return Version.Major * 10 + Version.Minor
        End Function

        Public Shared Function DropToVersion(Drop As Integer) As String
            If Drop >= 250 Then
                Return $"{Drop \ 10}.{Drop Mod 10}"
            ElseIf Drop = 209 Then
                Logger.Warn(New Exception("尝试将旧快照版 Drop 序数转换为版本字符串，这不应该发生！"))
                Return "1.20"
            Else
                Return $"1.{Drop \ 10}"
            End If
        End Function

        Public Shared Function NameToVersion(Name As String) As Version
            Name = Name.Lower.Replace("_unobfuscated", "").Replace(" Unobfuscated", "")

            If Name.StartsWithF("2.0") Then Return NameToVersion("1.5.1")
            If Name = "15w14a" Then Return NameToVersion("1.8.3")
            If Name.Contains(".rv-pre") Then Return NameToVersion("1.9.2")
            If Name.Contains("shareware") Then Return NameToVersion("1.13.2")
            If Name.StartsWithF("20w14") AndAlso Name <> "20w14a" Then Return NameToVersion("1.15.2")
            If Name.Contains("oneblockatatime") Then Return NameToVersion("1.18.2")
            If Name.Contains("23w13a") AndAlso Name <> "20w13a" Then Return NameToVersion("1.19.4")
            If Name = "24w14potato" Then Return NameToVersion("1.20.4")
            If Name = "25w14craftmine" Then Return NameToVersion("1.21.4")
            If Name = "26w14a" Then Return NameToVersion("26.1.1")

            If Name.StartsWithF("1.") Then
                Dim Segments = Name.Split(" _-.".ToCharArray)
                Return New Version(
                    Val(If(Segments.Count >= 2, Segments(1), "0")),
                    0,
                    Val(If(Segments.Count >= 3, Segments(2), "0")))
            ElseIf Name.RegexCheck("^[2-9][0-9]\.") Then
                Dim Segments = Name.Split(" _-.".ToCharArray)
                Return New Version(
                    Val(Segments(0)),
                    Val(If(Segments.Count >= 2, Segments(1), "0")),
                    Val(If(Segments.Count >= 3, Segments(2), "0")))
            Else
                Return New Version(9999, 0, 0)
            End If
        End Function

    End Class

    Public Function GetMcFoolName(Name As String) As String
        Name = Name.Lower
        If Name.StartsWithF("2.0") Then
            Return "2013 | " & GetLang("LangModMinecraftFoolName2.0")
        ElseIf Name = "15w14a" Then
            Return "2015 | " & GetLang("LangModMinecraftFoolName15w14a")
        ElseIf Name = "1.rv-pre1" Then
            Return "2016 | " & GetLang("LangModMinecraftFoolName1.rv-pre1")
        ElseIf Name = "3d shareware v1.34" Then
            Return "2019 | " & GetLang("LangModMinecraftFoolName3dshareware")
        ElseIf Name.StartsWithF("20w14inf") OrElse Name = "20w14∞" Then
            Return "2020 | " & GetLang("LangModMinecraftFoolName20w14inf")
        ElseIf Name = "22w13oneblockatatime" Then
            Return "2022 | " & GetLang("LangModMinecraftFoolName22w13oneblockatatime")
        ElseIf Name = "23w13a_or_b" Then
            Return "2023 | " & GetLang("LangModMinecraftFoolName23w13ab")
        ElseIf Name = "24w14potato" Then
            Return "2024 | " & GetLang("LangModMinecraftFoolName24w14potato")
        ElseIf Name = "25w14craftmine" Then
            Return "2025 | " & GetLang("LangModMinecraftFoolName25w14craftmine")
        ElseIf Name = "26w14a" Then
            Return "2026 | " & GetLang("LangModMinecraftFoolName26w14a")
        Else
            Return ""
        End If
    End Function

    Public McInstanceList As New Dictionary(Of McInstanceCardType, List(Of McInstance))

#End Region

#Region "版本列表加载"

    Public McInstanceListForceRefresh As Boolean = False

    Private IsFirstMcInstanceListLoad As Boolean = True

    Public McInstanceListLoader As New LoaderTask(Of String, Integer)("Minecraft Instance List", AddressOf InitMcInstanceList) With {.ReloadTimeout = 1}
    Private Sub InitMcInstanceList(Loader As LoaderTask(Of String, Integer))

        Dim PathMc As String = Loader.Input
        Try

            McInstanceList = New Dictionary(Of McInstanceCardType, List(Of McInstance))

            Dim FolderList As New List(Of String)
            If DirectoryUtils.Exists(PathMc & "versions") Then
                Try
                    For Each Folder As DirectoryInfo In DirectoryUtils.GetInfo(PathMc & "versions").GetDirectories
                        FolderList.Add(Folder.Name)
                    Next
                Catch ex As Exception
                    Throw New Exception(GetLang("LangModMinecraftExceptionGetFolderListFail", PathMc), ex)
                End Try
            End If

            If Not FolderList.Any() Then
                WriteIni(PathMc & "XFL.ini", "InstanceCache", "")
                GoTo OnLoaded
            End If

            Dim FolderListCheck As Integer = (Versions.McInstanceCacheVersion & "#" & FolderList.Join("#"c)).GetStableHashCode() Mod (Integer.MaxValue - 1)
            If Not McInstanceListForceRefresh AndAlso Val(ReadIni(PathMc & "XFL.ini", "InstanceCache")) = FolderListCheck Then

                Dim Result = InitMcInstanceListWithCache(PathMc)
                If Result Is Nothing Then
                    GoTo Reload
                Else
                    McInstanceList = Result
                End If
            Else

Reload:
                McInstanceListForceRefresh = False
                Logger.Info("文件夹列表变更，重载所有版本")
                WriteIni(PathMc & "XFL.ini", "InstanceCache", FolderListCheck)
                McInstanceList = InitMcInstanceListWithoutCache(PathMc)
            End If
            IsFirstMcInstanceListLoad = False

OnLoaded:
            If Loader.IsCanceled Then Return
            If McInstanceList.Any(Function(v) v.Key <> McInstanceCardType.Error) Then

                Dim SavedSelection As String = ReadIni(PathMc & "XFL.ini", "Version")
                If SavedSelection <> "" Then
                    For Each Card As KeyValuePair(Of McInstanceCardType, List(Of McInstance)) In McInstanceList
                        For Each Instance As McInstance In Card.Value
                            If Instance.Name = SavedSelection AndAlso Not Instance.State = McInstanceState.Error Then

                                McInstanceSelected = Instance
                                Logger.Info($"选择该文件夹储存的 Minecraft 版本：{McInstanceSelected.PathVersion}")
                                Return
                            End If
                        Next
                    Next
                End If
                If Not McInstanceList.First.Value(0).State = McInstanceState.Error Then

                    McInstanceSelected = McInstanceList.First.Value(0)
                    Logger.Info($"自动选择 Minecraft 版本：{McInstanceSelected.PathVersion}")
                End If
            Else
                McInstanceSelected = Nothing
                Logger.Info("未找到可用 Minecraft 版本")
            End If
            If Settings.Get(Of Boolean)("SystemDebugDelay") Then Thread.Sleep(RandomInteger(200, 3000))
        Catch ex As Exception
            If Loader.IsCanceled OrElse ex.IsCanceled Then Return
            WriteIni(PathMc & "XFL.ini", "InstanceCache", "")
            Logger.Error(ex, "加载 .minecraft 版本列表失败")
        End Try
    End Sub

    Private Function InitMcInstanceListWithCache(Folder As String) As Dictionary(Of McInstanceCardType, List(Of McInstance))
        Dim Results As New Dictionary(Of McInstanceCardType, List(Of McInstance))
        Try
            Dim CardCount As Integer = ReadIni(Folder & "XFL.ini", "CardCount", -1)
            If CardCount = -1 Then Return Nothing
            For i = 0 To CardCount - 1
                Dim CardType As McInstanceCardType = ReadIni(Folder & "XFL.ini", "CardKey" & (i + 1), ":")
                Dim InstanceList As New List(Of McInstance)

                For Each VersionName As String In ReadIni(Folder & "XFL.ini", "CardValue" & (i + 1), ":").Split(":")
                    If VersionName = "" Then Continue For
                    Dim FolderVersions As String = $"{Folder}versions\{VersionName}\"
                    If FileUtils.Exists(FolderVersions & ".PCLignore") Then
                        If IsFirstMcInstanceListLoad Then
                            Logger.Info($"清理残留的忽略项目：{FolderVersions}")
                            FileUtils.Delete(FolderVersions & ".PCLignore")
                        Else
                            Logger.Info($"跳过要求忽略的项目：{FolderVersions}")
                            Continue For
                        End If
                    End If
                    Try

                        Dim Instance As New McInstance(FolderVersions)
                        InstanceList.Add(Instance)
                        Instance.Info = ReadIni(Instance.PathVersion & "XFL\Setup.ini", "CustomInfo", "")
                        If Instance.Info = "" Then Instance.Info = ReadIni(Instance.PathVersion & "XFL\Setup.ini", "Info", Instance.Info)
                        Instance.Logo = ReadIni(Instance.PathVersion & "XFL\Setup.ini", "Logo", Instance.Logo)
                        Instance.ReleaseTime = ReadIni(Instance.PathVersion & "XFL\Setup.ini", "ReleaseTime", Instance.ReleaseTime)
                        Instance.State = ReadIni(Instance.PathVersion & "XFL\Setup.ini", "State", Instance.State)
                        Instance.IsStar = ReadIni(Instance.PathVersion & "XFL\Setup.ini", "IsStar", False)
                        Instance.DisplayType = ReadIni(Folder & "XFL\Setup.ini", "DisplayType", McInstanceCardType.Auto)
                        If Instance.State <> McInstanceState.Error AndAlso
                           ReadIni(Instance.PathVersion & "XFL\Setup.ini", "VersionVanillaName", "Unknown") <> "Unknown" Then

                            Dim VersionInfo As New McVersion With {
                                .Fabric = ReadIni(Instance.PathVersion & "XFL\Setup.ini", "VersionFabric", Nothing),
                                .Forge = ReadIni(Instance.PathVersion & "XFL\Setup.ini", "VersionForge", Nothing),
                                .NeoForge = ReadIni(Instance.PathVersion & "XFL\Setup.ini", "VersionNeoForge", Nothing),
                                .OptiFine = ReadIni(Instance.PathVersion & "XFL\Setup.ini", "VersionOptiFine", Nothing),
                                .HasLiteLoader = ReadIni(Instance.PathVersion & "XFL\Setup.ini", "VersionLiteLoader", False),
                                .VanillaName = ReadIni(Instance.PathVersion & "XFL\Setup.ini", "VersionVanillaName", "Unknown")
                            }
                            Instance.Version = VersionInfo
                        Else
                            Instance.Load()
                        End If

                        If Instance.State = McInstanceState.Error Then

                            Dim OldDesc As String = Instance.Info
                            Instance.State = McInstanceState.Original
                            Instance.Check()

                            Dim CustomInfo As String = ReadIni(Instance.PathVersion & "XFL\Setup.ini", "CustomInfo")
                            If Instance.State = McInstanceState.Original OrElse (CustomInfo = "" AndAlso Not OldDesc = Instance.Info) Then
                                Logger.Info($"版本 {Instance.Name} 的错误状态已变更，新的状态为：{Instance.Info}")
                                Return Nothing
                            End If
                        End If

                        If Instance.Logo = "" Then
                            Logger.Info($"版本 {Instance.Name} 未被加载")
                            Return Nothing
                        End If

                    Catch ex As Exception
                        Logger.Warn(ex, $"读取版本加载缓存失败（{VersionName}）")
                        Return Nothing
                    End Try
                Next

                If InstanceList.Any Then Results.Add(CardType, InstanceList)
            Next
            Return Results
        Catch ex As Exception
            Logger.Warn(ex, "读取版本缓存失败")
            Return Nothing
        End Try
    End Function
    Private Function InitMcInstanceListWithoutCache(Folder As String) As Dictionary(Of McInstanceCardType, List(Of McInstance))
        Dim InstanceList As New List(Of McInstance)

#Region "循环加载每个版本的信息"
        For Each VersionFolderInfo As DirectoryInfo In DirectoryUtils.GetInfo(Folder & "versions").GetDirectories
            If Not VersionFolderInfo.Exists OrElse Not VersionFolderInfo.EnumerateFiles.Any Then
                Logger.Info($"跳过空文件夹：{VersionFolderInfo.FullName}")
                Continue For
            End If
            If (VersionFolderInfo.Name = "cache" OrElse VersionFolderInfo.Name = "BLClient" OrElse VersionFolderInfo.Name = "XFL") AndAlso
                Not FileUtils.Exists(VersionFolderInfo.FullName & "\" & VersionFolderInfo.Name & ".json") Then
                Logger.Info($"跳过可能不是版本文件夹的项目：{VersionFolderInfo.FullName}")
                Continue For
            End If
            Dim VersionFolder As String = VersionFolderInfo.FullName & "\"
            If FileUtils.Exists(VersionFolder & ".PCLignore") Then
                If IsFirstMcInstanceListLoad Then
                    Logger.Info($"清理残留的忽略项目：{VersionFolder}")
                    FileUtils.Delete(VersionFolder & ".PCLignore")
                Else
                    Logger.Info($"跳过要求忽略的项目：{VersionFolder}")
                    Continue For
                End If
            End If
            Dim Instance As New McInstance(VersionFolder)
            InstanceList.Add(Instance)
            Instance.Load()
        Next
#End Region

        Dim Results As New Dictionary(Of McInstanceCardType, List(Of McInstance))

#Region "将版本分类到各个卡片"
        Try

            Dim InstanceListOriginal As New Dictionary(Of McInstanceCardType, List(Of McInstance))

            Dim InstanceStar As New List(Of McInstance)
            For Each Instance As McInstance In InstanceList.ToList
                If Not Instance.IsStar Then Continue For
                If Instance.DisplayType = McInstanceCardType.Hidden Then Continue For
                InstanceStar.Add(Instance)
                InstanceList.Remove(Instance)
            Next
            If InstanceStar.Any Then InstanceListOriginal.Add(McInstanceCardType.Star, InstanceStar)

            McInstanceFilter(InstanceList, InstanceListOriginal, {McInstanceState.Error}, McInstanceCardType.Error)
            McInstanceFilter(InstanceList, InstanceListOriginal, {McInstanceState.Fool}, McInstanceCardType.Fool)

            McInstanceFilter(InstanceList, InstanceListOriginal, {McInstanceState.Forge, McInstanceState.NeoForge, McInstanceState.LiteLoader, McInstanceState.Fabric}, McInstanceCardType.API)

            Dim InstanceUseful As New List(Of McInstance)
            Dim InstanceRubbish As New List(Of McInstance)
            McInstanceFilter(InstanceList, {McInstanceState.Old}, InstanceRubbish)

            Dim LargestInstance As McInstance = InstanceList.
                Where(Function(v) v.State = McInstanceState.Original OrElse v.State = McInstanceState.Snapshot).
                MaxBy(Function(v) v.ReleaseTime)
            If LargestInstance IsNot Nothing AndAlso LargestInstance.State = McInstanceState.Snapshot Then
                InstanceUseful.Add(LargestInstance)
                InstanceList.Remove(LargestInstance)
            End If

            McInstanceFilter(InstanceList, {McInstanceState.Snapshot}, InstanceRubbish)

            Dim NewerInstance As New Dictionary(Of String, McInstance)
            Dim ExistDrops As New List(Of Integer)
            For Each Instance As McInstance In InstanceList
                If Not Instance.Version.Vaild Then Continue For
                If Not ExistDrops.Contains(Instance.Version.Drop) Then ExistDrops.Add(Instance.Version.Drop)
                Dim Key As String = Instance.Version.Drop & "-" & Instance.State
                If Not NewerInstance.ContainsKey(Key) Then
                    NewerInstance.Add(Key, Instance)
                    Continue For
                End If
                If Instance.Version.HasOptiFine Then
                    If Instance.Version.OptiFineCode > NewerInstance(Key).Version.OptiFineCode Then NewerInstance(Key) = Instance
                Else
                    If Instance.ReleaseTime > NewerInstance(Key).ReleaseTime Then NewerInstance(Key) = Instance
                End If
            Next

            For Each Drop As Integer In ExistDrops
                If NewerInstance.ContainsKey(Drop & "-" & McInstanceState.OptiFine) AndAlso NewerInstance.ContainsKey(Drop & "-" & McInstanceState.Original) Then

                    Dim VanillaInstance As McInstance = NewerInstance(Drop & "-" & McInstanceState.Original)
                    Dim OptiFineInstance As McInstance = NewerInstance(Drop & "-" & McInstanceState.OptiFine)
                    If VanillaInstance.Version.Drop > OptiFineInstance.Version.Drop Then

                        InstanceUseful.Add(VanillaInstance)
                        InstanceList.Remove(VanillaInstance)
                    End If
                    InstanceUseful.Add(OptiFineInstance)
                    InstanceList.Remove(OptiFineInstance)
                ElseIf NewerInstance.ContainsKey(Drop & "-" & McInstanceState.OptiFine) Then

                    InstanceUseful.Add(NewerInstance(Drop & "-" & McInstanceState.OptiFine))
                    InstanceList.Remove(NewerInstance(Drop & "-" & McInstanceState.OptiFine))
                ElseIf NewerInstance.ContainsKey(Drop & "-" & McInstanceState.Original) Then

                    InstanceUseful.Add(NewerInstance(Drop & "-" & McInstanceState.Original))
                    InstanceList.Remove(NewerInstance(Drop & "-" & McInstanceState.Original))
                End If
            Next

            InstanceRubbish.AddRange(InstanceList)
            If InstanceUseful.Any Then InstanceListOriginal.Add(McInstanceCardType.OriginalLike, InstanceUseful)
            If InstanceRubbish.Any Then InstanceListOriginal.Add(McInstanceCardType.Rubbish, InstanceRubbish)

            For Each InstancePair In InstanceListOriginal
                For Each Instance As McInstance In InstancePair.Value
                    Dim RealType = If(Instance.DisplayType = 0 OrElse InstancePair.Key = McInstanceCardType.Star, InstancePair.Key, Instance.DisplayType)
                    If Not Results.ContainsKey(RealType) Then Results.Add(RealType, New List(Of McInstance))
                    Results(RealType).Add(Instance)
                Next
            Next

        Catch ex As Exception
            Results.Clear()
            Logger.Error(ex, "分类版本列表失败")
        End Try
#End Region
#Region "对卡片与版本进行排序"

        Dim SortedInstanceList As New Dictionary(Of McInstanceCardType, List(Of McInstance))
        For Each SortRule As String In {McInstanceCardType.Star, McInstanceCardType.API, McInstanceCardType.OriginalLike, McInstanceCardType.Rubbish, McInstanceCardType.Fool, McInstanceCardType.Error, McInstanceCardType.Hidden}
            If Results.ContainsKey(SortRule) Then SortedInstanceList.Add(SortRule, Results(SortRule))
        Next
        Results = SortedInstanceList

        For Each CardType In {McInstanceCardType.Star, McInstanceCardType.API, McInstanceCardType.OriginalLike, McInstanceCardType.Rubbish, McInstanceCardType.Fool}
            If Not Results.ContainsKey(CardType) Then Continue For
            Dim GetComponentCode =
            Function(Instance As McInstance) As Integer
                If Instance.Version.ForgelikeCode > 0 Then Return Instance.Version.ForgelikeCode
                If Instance.Version.HasOptiFine Then Return Instance.Version.OptiFineCode
                Return 0
            End Function
            Results(CardType) = Results(CardType).SortByComparison(
            Function(Left As McInstance, Right As McInstance)

                If (Left.ReleaseTime.Year >= 2000 OrElse Right.ReleaseTime.Year >= 2000) AndAlso
                    Left.ReleaseTime <> Right.ReleaseTime Then Return Left.ReleaseTime > Right.ReleaseTime

                If Left.Version.HasFabric <> Right.Version.HasFabric Then Return Left.Version.HasFabric
                If Left.Version.HasNeoForge <> Right.Version.HasNeoForge Then Return Left.Version.HasNeoForge
                If Left.Version.HasForge <> Right.Version.HasForge Then Return Left.Version.HasForge
                If Left.Version.HasOptiFine <> Right.Version.HasOptiFine Then Return Left.Version.HasOptiFine
                If Left.Version.HasLiteLoader <> Right.Version.HasLiteLoader Then Return Left.Version.HasLiteLoader

                If GetComponentCode(Left) <> GetComponentCode(Right) Then Return GetComponentCode(Left) > GetComponentCode(Right)

                Return Left.Name > Right.Name
            End Function)
        Next

#End Region
#Region "保存卡片缓存"
        WriteIni(Folder & "XFL.ini", "CardCount", Results.Count)
        For i = 0 To Results.Count - 1
            WriteIni(Folder & "XFL.ini", "CardKey" & (i + 1), Results.Keys(i))
            Dim Value As String = ""
            For Each Instance As McInstance In Results.Values(i)
                Value += Instance.Name & ":"
            Next
            WriteIni(Folder & "XFL.ini", "CardValue" & (i + 1), Value)
        Next
#End Region
        Return Results
    End Function

    Private Sub McInstanceFilter(ByRef InstanceList As List(Of McInstance), ByRef Target As Dictionary(Of McInstanceCardType, List(Of McInstance)), Formula As McInstanceState(), CardType As McInstanceCardType)
        Dim KeepList = InstanceList.Where(Function(v) Formula.Contains(v.State)).ToList

        If KeepList.Any Then
            Target.Add(CardType, KeepList)
            InstanceList = InstanceList.Except(KeepList).ToList()
        End If
    End Sub

    Private Sub McInstanceFilter(ByRef InstanceList As List(Of McInstance), Formula As McInstanceState(), ByRef KeepList As List(Of McInstance))
        KeepList.AddRange(InstanceList.Where(Function(v) Formula.Contains(v.State)))

        If KeepList.Any Then
            InstanceList = InstanceList.Except(KeepList).ToList()
        End If
    End Sub
    Public Enum McInstanceCardType
        Star = -1
        Auto = 0
        Hidden = 1
        API = 2
        OriginalLike = 3
        Rubbish = 4
        Fool = 5
        [Error] = 6
    End Enum

#End Region

#Region "皮肤"

    Public Structure McSkinInfo
        Public IsSlim As Boolean
        Public LocalFile As String
        Public IsVaild As Boolean
    End Structure

    Public Function McSkinSelect() As McSkinInfo
        Dim FileName As String = Dialogs.SelectFile(GetLang("LangModMinecraftSelectTitleSkin"), False, filter:={({"png", "jpg", "jpeg", "webp"}, GetLang("LangModMinecraftSelectTypeSkin"))}).FirstOrDefault()

        If String.IsNullOrEmpty(FileName) Then Return New McSkinInfo With {.IsVaild = False}
        Try
            Dim Image As New MyBitmap(FileName)
            If Image.Pic.Width <> 64 OrElse Not (Image.Pic.Height = 32 OrElse Image.Pic.Height = 64) Then
                Hint(GetLang("LangModMinecraftSkinSizeErrorA"), HintType.Red)
                Return New McSkinInfo With {.IsVaild = False}
            End If
            Dim FileInfo = FileUtils.GetInfo(FileName)
            If FileInfo.Length > 24 * 1024 Then
                Hint(GetLang("LangModMinecraftSkinSizeErrorB") & " " & Math.Round(FileInfo.Length / 1024, 2) & " KB", HintType.Red)
                Return New McSkinInfo With {.IsVaild = False}
            End If
        Catch ex As Exception
            Logger.Error(ex, GetLang("LangModMinecraftSkinErrorFile"), LogBehavior.Toast)
            Return New McSkinInfo With {.IsVaild = False}
        End Try

        Dim IsSlim As Integer = MyMsgBox(GetLang("LangModMinecraftSkinDialogSkinTypeContent"), GetLang("LangModMinecraftSkinDialogSkinTypeTitle"), GetLang("LangModMinecraftSkinDialogSkinTypeSteve"), GetLang("LangModMinecraftSkinDialogSkinTypeAlex"), GetLang("LangModMinecraftSkinDialogSkinTypeIDK"), HighLight:=False)
        If IsSlim = 3 Then
            Hint(GetLang("LangModMinecraftSkinSkinTypeIDK"))
            Return New McSkinInfo With {.IsVaild = False}
        End If

        Return New McSkinInfo With {.IsVaild = True, .IsSlim = IsSlim = 2, .LocalFile = FileName}
    End Function

    Public Function McSkinGetAddress(UUID As String, Type As String) As String
        If UUID = "" Then Throw New Exception(GetLang("LangModMinecraftExceptionEmptyUuid"))
        If UUID.StartsWithF("00000") AndAlso Type <> "Auth" Then Throw New Exception(GetLang("LangModMinecraftExceptionUuidNoOnlineProfile", UUID))

        Dim CacheSkinAddress As String = ReadIni(PathTemp & "Cache\Skin\Index" & Type & ".ini", UUID)
        If Not CacheSkinAddress = "" Then Return CacheSkinAddress

        Dim Url As String
        Select Case Type
            Case "Mojang", "Ms"
                Url = "https://sessionserver.mojang.com/session/minecraft/profile/"
            Case "Nide"
                Url = "https://auth.mc-user.com:233/" & If(McInstanceSelected Is Nothing, Settings.Get(Of String)("CacheNideServer"), Settings.Get(Of String)("VersionServerNide", Instance:=McInstanceSelected)) & "/sessionserver/session/minecraft/profile/"
            Case "Auth"
                Url = If(McInstanceSelected Is Nothing, Settings.Get(Of String)("CacheAuthServerServer"), Settings.Get(Of String)("VersionServerAuthServer", Instance:=McInstanceSelected)) & "/sessionserver/session/minecraft/profile/"
            Case Else
                Throw New ArgumentException(GetLang("LangModMinecraftExceptionSkinTypeInvalid", If(Type, "null")))
        End Select
        Dim SkinString = NetRequestByClientRetry(Url & UUID, RequireJson:=True)
        If SkinString = "" Then Throw New Exception(GetLang("LangModMinecraftExceptionEmptySkin"))

        Dim SkinValue As String
        Try
            For Each SkinProperty In SkinString.DeserializeJson()("properties")
                If SkinProperty("name") = "textures" Then
                    SkinValue = SkinProperty("value")
                    Exit Try
                End If
            Next
            Throw New Exception(GetLang("LangModMinecraftExceptionSkinPropertyNotFound"))
        Catch ex As Exception
            Logger.Warn(ex, $"无法完成解析的皮肤返回值，可能是未设置自定义皮肤的用户：{SkinString}")
            Throw New Exception(GetLang("LangModMinecraftExceptionSkinNoData"), ex)
        End Try
        SkinString = Encoding.UTF8.GetString(Convert.FromBase64String(SkinValue))
        Dim SkinJson As JObject = SkinString.Lower.DeserializeJson()
        If SkinJson("textures") Is Nothing OrElse SkinJson("textures")("skin") Is Nothing OrElse SkinJson("textures")("skin")("url") Is Nothing Then
            Throw New Exception(GetLang("LangModMinecraftExceptionSkinNotSet"))
        Else
            Dim SkinUrl As String = SkinJson("textures")("skin")("url").ToString
            SkinValue = If(SkinUrl.Contains("minecraft.net/"), SkinUrl.Replace("http://", "https://"), SkinUrl)
        End If

        WriteIni(PathTemp & "Cache\Skin\Index" & Type & ".ini", UUID, SkinValue)
        Logger.Info($"UUID {UUID} 对应的皮肤文件为 {SkinValue}")
        Return SkinValue
    End Function

    Private ReadOnly McSkinDownloadLock As New Object

    Public Function McSkinDownload(Address As String) As String
        Dim SkinName As String = PathUtils.GetLastPart(Address)
        Dim FileAddress As String = PathTemp & "Cache\Skin\" & Address.GetStableHashCode() & ".png"
        SyncLock McSkinDownloadLock
            If Not FileUtils.Exists(FileAddress) Then
                NetDownloadByClient(Address, FileAddress & ".PCLDownloading")
                FileUtils.Move(FileAddress & ".PCLDownloading", FileAddress)
                Logger.Info($"皮肤下载成功：{FileAddress}")
            End If
            Return FileAddress
        End SyncLock
    End Function

    Public Function McSkinSex(Uuid As String) As String
        If Not Uuid.Length = 32 Then Return "Steve"
        Dim a = Integer.Parse(Uuid(7), Globalization.NumberStyles.AllowHexSpecifier)
        Dim b = Integer.Parse(Uuid(15), Globalization.NumberStyles.AllowHexSpecifier)
        Dim c = Integer.Parse(Uuid(23), Globalization.NumberStyles.AllowHexSpecifier)
        Dim d = Integer.Parse(Uuid(31), Globalization.NumberStyles.AllowHexSpecifier)
        Return If((a Xor b Xor c Xor d) Mod 2, "Alex", "Steve")

    End Function

#End Region

#Region "支持库文件（Library）"

    Public Class McLibEntry

        Public LocalPath As String

        Public Size As Long = 0

        Public IsNatives As Boolean = False

        Public SHA1 As String = Nothing

        Public Property Url As String
            Get
                Return _Url
            End Get
            Set(value As String)

                _Url = If(String.IsNullOrWhiteSpace(value), Nothing, value)
            End Set
        End Property
        Private _Url As String

        Public ReadOnly Property Name As String
            Get
                If OriginalName Is Nothing Then Return Nothing
                Dim Splited As New List(Of String)(OriginalName.Split(":"))
                Splited.RemoveAt(2)
                Return Splited.Join(":"c)
            End Get
        End Property

        Public OriginalName As String

        Public Overrides Function ToString() As String
            Return If(IsNatives, "[Native] ", "") & StringUtils.FormatByteSize(Size) & " | " & LocalPath
        End Function
    End Class

    Public Function McJsonRuleCheck(RuleToken As JToken) As Boolean
        If RuleToken Is Nothing Then Return True

        Dim Required As Boolean = False
        For Each Rule As JToken In RuleToken

            Dim IsRightRule As Boolean = True
            If Rule("os") IsNot Nothing Then
                If Rule("os")("name") IsNot Nothing Then
                    Dim OsName As String = Rule("os")("name").ToString
                    If OsName = "unknown" Then
                    ElseIf OsName = "windows" Then
                        If Rule("os")("version") IsNot Nothing Then
                            IsRightRule = IsRightRule AndAlso OSVersion.RegexCheck(Rule("os")("version").ToString)
                        End If
                    Else
                        IsRightRule = False
                    End If
                End If
                If Rule("os")("arch") IsNot Nothing Then
                    IsRightRule = IsRightRule AndAlso Rule("os")("arch").ToString <> "x86" AndAlso Not Rule("os")("arch").Contains("arm")
                End If
            End If
            If Not IsNothing(Rule("features")) Then
                IsRightRule = IsRightRule AndAlso IsNothing(Rule("features")("is_demo_user"))
                If CType(Rule("features"), JObject).Children.Any(Function(j As JProperty) j.Name.Contains("quick_play")) Then
                    IsRightRule = False
                End If
            End If

            If Rule("action").ToString = "allow" Then
                If IsRightRule Then Required = True
            Else
                If IsRightRule Then Required = False
            End If

        Next
        Return Required
    End Function
    Private OSVersion As String = My.Computer.Info.OSVersion

    Public Function McLibListGet(Instance As McInstance, IncludeMainJar As Boolean) As List(Of McLibEntry)

        Logger.Info($"获取支持库列表：{Instance.Name}")
        Dim Result = McLibListGetWithJson(Instance.JsonObject)

        If IncludeMainJar Then
            Dim RealInstance As McInstance
            Dim RequiredJar As String = Instance.JsonObject("jar")?.ToString
            If Instance.IsHmclFormatJson OrElse RequiredJar Is Nothing Then

                Dim OriginalInstance As McInstance = Instance

                If Not (Instance.Version.HasForgelike AndAlso Instance.Version.Vanilla.Major >= 17) Then
                    Do Until OriginalInstance.InheritName = ""
                        If OriginalInstance.InheritName = OriginalInstance.Name Then Exit Do
                        OriginalInstance = New McInstance(McFolderSelected & "versions\" & OriginalInstance.InheritName & "\")
                    Loop
                End If

                RealInstance = New McInstance(OriginalInstance.PathVersion)
            Else

                RealInstance = New McInstance(RequiredJar)
            End If
            Dim ClientUrl As String, ClientSHA1 As String

            If Not FileUtils.Exists(RealInstance.PathVersion & RealInstance.Name & ".json") Then
                RealInstance = Instance
                Logger.Warn($"可能缺少前置版本 {RealInstance.Name}，找不到对应的 json 文件")
            End If

            If RealInstance.JsonObject("downloads") IsNot Nothing AndAlso RealInstance.JsonObject("downloads")("client") IsNot Nothing Then
                ClientUrl = RealInstance.JsonObject("downloads")("client")("url")
                ClientSHA1 = RealInstance.JsonObject("downloads")("client")("sha1")
            Else
                ClientUrl = Nothing
                ClientSHA1 = Nothing
            End If

            Result.Add(New McLibEntry With {.LocalPath = RealInstance.PathVersion & RealInstance.Name & ".jar", .Size = 0, .IsNatives = False, .Url = ClientUrl, .SHA1 = ClientSHA1})
        End If

        Return Result
    End Function

    Public Function McLibListGetWithJson(JsonObject As JObject, Optional KeepSameNameDifferentVersionResult As Boolean = False, Optional CustomMcFolder As String = Nothing) As List(Of McLibEntry)
        CustomMcFolder = If(CustomMcFolder, McFolderSelected)

        Dim BasicArray As New List(Of McLibEntry)
        For Each Library As JObject In CType(JsonObject("libraries"), JArray).Children

            For i = Library.Properties.Count - 1 To 0 Step -1
                If Library.Properties(i).Value.Type = JTokenType.Null Then Library.Remove(Library.Properties(i).Name)
            Next

            If Not McJsonRuleCheck(Library("rules")) Then Continue For

            Dim RootUrl As String = Library("url")
            If RootUrl IsNot Nothing Then
                RootUrl += McLibGet(Library("name"), False, True, CustomMcFolder).Replace("\", "/")
            End If

            If Library("natives") Is Nothing Then
                Dim LocalPath As String
                LocalPath = McLibGet(Library("name"), CustomMcFolder:=CustomMcFolder)
                Try
                    If Library("downloads") IsNot Nothing AndAlso Library("downloads")("artifact") IsNot Nothing Then
                        BasicArray.Add(New McLibEntry With {
                            .OriginalName = Library("name"),
                            .Url = If(Library("downloads")("artifact")("url"), RootUrl),
                            .LocalPath = If(Library("downloads")("artifact")("path") Is Nothing, McLibGet(Library("name"),
                                CustomMcFolder:=CustomMcFolder), CustomMcFolder & "libraries\" & Library("downloads")("artifact")("path").ToString.Replace("/", "\")),
                            .Size = Val(Library("downloads")("artifact")("size").ToString),
                            .IsNatives = False,
                            .SHA1 = Library("downloads")("artifact")("sha1")?.ToString})
                    Else
                        BasicArray.Add(New McLibEntry With {.OriginalName = Library("name"), .Url = RootUrl, .LocalPath = LocalPath, .Size = 0, .IsNatives = False, .SHA1 = Nothing})
                    End If
                Catch ex As Exception
                    Logger.Warn(ex, $"处理实际支持库列表失败（无 Natives，{If(Library("name"), "Nothing")}）")
                    BasicArray.Add(New McLibEntry With {.OriginalName = Library("name"), .Url = RootUrl, .LocalPath = LocalPath, .Size = 0, .IsNatives = False, .SHA1 = Nothing})
                End Try
            ElseIf Library("natives")("windows") IsNot Nothing Then
                Dim NativeName As String = Library("natives")("windows")
                Try
                    If Library("downloads") IsNot Nothing AndAlso Library("downloads")("classifiers") IsNot Nothing AndAlso Library("downloads")("classifiers")(NativeName) IsNot Nothing Then
                        Dim NativeData = Library("downloads")("classifiers")(NativeName)
                        BasicArray.Add(New McLibEntry With {
                             .OriginalName = Library("name"),
                             .Url = If(NativeData("url"), RootUrl),
                             .LocalPath = If(NativeData("path") Is Nothing,
                                 McLibGet(Library("name"), CustomMcFolder:=CustomMcFolder).
                                    Replace(".jar", "-" & NativeName & ".jar").
                                    Replace("${arch}", If(Environment.Is64BitOperatingSystem, "64", "32")),
                                    $"{CustomMcFolder}libraries\{NativeData("path").ToString.Replace("/", "\")}"),
                             .Size = Val(NativeData("size").ToString),
                             .IsNatives = True,
                             .SHA1 = NativeData("sha1").ToString})
                    Else
                        BasicArray.Add(
                        New McLibEntry With {.OriginalName = Library("name"), .Url = RootUrl, .Size = 0, .IsNatives = True, .SHA1 = Nothing,
                            .LocalPath = McLibGet(Library("name"), CustomMcFolder:=CustomMcFolder).
                                 Replace(".jar", $"-{NativeName}.jar").
                                 Replace("${arch}", If(Environment.Is64BitOperatingSystem, "64", "32"))
                        })
                    End If
                Catch ex As Exception
                    Logger.Warn(ex, $"处理实际支持库列表失败（有 Natives，{If(Library("name"), "Nothing")}）")
                    BasicArray.Add(
                        New McLibEntry With {.OriginalName = Library("name"), .Url = RootUrl, .Size = 0, .IsNatives = True, .SHA1 = Nothing,
                             .LocalPath = McLibGet(Library("name"), CustomMcFolder:=CustomMcFolder).
                                 Replace(".jar", $"-{NativeName}.jar").
                                 Replace("${arch}", If(Environment.Is64BitOperatingSystem, "64", "32"))
                        })
                End Try
            End If

        Next

        Dim ResultArray As New Dictionary(Of String, McLibEntry)
        Dim GetVersion =
        Function(Token As McLibEntry) As String

            'D:\Minecraft\test\libraries\org\apache\commons\commons-collections4\4.2\commons-collections4-4.2.jar

            Return PathUtils.GetLastPart(PathUtils.RemoveLastPart(Token.LocalPath))
        End Function
        For i = 0 To BasicArray.Count - 1
            Dim Key As String = BasicArray(i).Name & BasicArray(i).IsNatives.ToString
            If ResultArray.ContainsKey(Key) Then
                Dim BasicArrayVersion As String = GetVersion(BasicArray(i))
                Dim ResultArrayVersion As String = GetVersion(ResultArray(Key))
                If BasicArrayVersion <> ResultArrayVersion AndAlso KeepSameNameDifferentVersionResult Then
                    Logger.Info($"发现疑似重复的支持库：{BasicArray(i)} ({BasicArrayVersion}) 与 {ResultArray(Key)} ({ResultArrayVersion})")
                    ResultArray.Add(Key & GetUuid(), BasicArray(i))
                Else
                    Logger.Info($"发现重复的支持库：{BasicArray(i)} ({BasicArrayVersion}) 与 {ResultArray(Key)} ({ResultArrayVersion})，已忽略其中之一")
                    If CompareVersionGE(BasicArrayVersion, ResultArrayVersion) Then
                        ResultArray(Key) = BasicArray(i)
                    End If
                End If
            Else
                ResultArray.Add(Key, BasicArray(i))
            End If
        Next
        Return ResultArray.Values.ToList
    End Function

    Public Function McLibNetFilesFromInstance(Instance As McInstance) As List(Of NetFile)
        If Not Instance.IsLoaded Then Instance.Load()
        Dim Result As New List(Of NetFile)

        Try
            Dim MainJar As NetFile = DlClientJarGet(Instance, True)
            If MainJar IsNot Nothing Then Result.Add(MainJar)
        Catch ex As Exception
            Logger.Warn(ex, "版本缺失主 jar 文件所必须的信息")
        End Try

        Result.AddRange(McLibNetFilesFromTokens(McLibListGet(Instance, False)))

        If Settings.Get(Of Integer)("VersionServerLogin", Instance:=Instance) = 3 Then
            Dim TargetFile As String = PathPure & "nide8auth.jar"
            Try

                Logger.Info("开始获取统一通行证下载信息")
                Dim DownloadInfo As JObject = NetRequestByClientRetry("https://auth.mc-user.com:233/" & Settings.Get(Of String)("VersionServerNide", Instance:=Instance), RequireJson:=True).DeserializeJson()
                Result.Add(New NetFile(
                    Urls:={"https://login.mc-user.com:233/index/jar"},
                    LocalPath:=TargetFile,
                    Checker:=New FileChecker With {.Hash = DownloadInfo("jarHash").ToString}))
            Catch ex As Exception
                If FileUtils.Exists(TargetFile) Then
                    Logger.Warn(ex, "获取统一通行证下载信息失败")
                Else
                    Throw New Exception("获取统一通行证下载信息失败", ex)
                End If
            End Try
        End If

        If Settings.Get(Of Integer)("VersionServerLogin", Instance:=Instance) = 4 Then
            Dim TargetFile = PathPure & "authlib-injector.jar"
            Try
                Logger.Info("开始获取 Authlib-Injector 下载信息")
                Dim DownloadInfo As JObject = NetRequestByClientRetry(
                        "https://authlib-injector.yushi.moe/artifact/latest.json",
                        BackupUrl:="https://bmclapi2.bangbang93.com/mirrors/authlib-injector/artifact/latest.json", RequireJson:=True).DeserializeJson()
                Dim DownloadAddress As String = DownloadInfo("download_url").ToString.
                        Replace("bmclapi2.bangbang93.com/mirrors/authlib-injector", "authlib-injector.yushi.moe")
                Result.Add(New NetFile({
                    DownloadAddress,
                    DownloadAddress.Replace("authlib-injector.yushi.moe", "bmclapi2.bangbang93.com/mirrors/authlib-injector")
                }, TargetFile, New FileChecker With {.Hash = DownloadInfo("checksums")("sha256").ToString}))
            Catch ex As Exception
                If FileUtils.Exists(TargetFile) Then
                    Logger.Warn(ex, "获取 Authlib-Injector 下载信息失败")
                Else
                    Throw New Exception("获取 Authlib-Injector 下载信息失败", ex)
                End If
            End Try
        End If

        If ShouldIgnoreFileCheck(Instance) Then
            Logger.Warn("设置要求尽量忽略文件检查，这可能会保留有误的文件")
            Result = Result.Where(
            Function(f)
                If FileUtils.Exists(f.LocalPath) Then
                    Logger.Warn($"跳过下载的支持库文件：{f.LocalPath}")
                    Return False
                Else
                    Return True
                End If
            End Function).ToList
        End If

        Return Result
    End Function

    Public Function McLibNetFilesFromTokens(Libs As List(Of McLibEntry), Optional CustomMcFolder As String = Nothing) As List(Of NetFile)
        CustomMcFolder = If(CustomMcFolder, McFolderSelected)
        Dim Result As New List(Of NetFile)

        For Each Token As McLibEntry In Libs

            Dim Checker As FileChecker
            If Token.Name.ContainsIgnoreCase("labymod") Then
                Checker = New FileChecker
            Else
                Checker = New FileChecker With {.ActualSize = If(Token.Size = 0, -1, Token.Size), .Hash = Token.SHA1}
            End If

            Dim Urls As New List(Of String)
            If Token.Url Is Nothing AndAlso Token.Name = "net.minecraftforge:forge:universal" Then
                Token.Url = "https://maven.minecraftforge.net" & Token.LocalPath.Replace(CustomMcFolder & "libraries", "").Replace("\", "/")
            End If
            If Token.Url IsNot Nothing Then

                Urls.Add(Token.Url)
                If Token.Url.Contains("launcher.mojang.com/v1/objects") OrElse Token.Url.Contains("client.txt") OrElse
                   Token.Url.Contains(".tsrg") Then
                    Urls.AddRange(DlSourceLauncherOrMetaGet(Token.Url))
                End If
                If Token.Url.Contains("maven") Then
                    Dim BmclapiUrl As String =
                        Token.Url.Replace(Token.Url.Substring(0, Token.Url.IndexOfF("maven")), "https://bmclapi2.bangbang93.com/").Replace("maven.fabricmc.net", "maven").Replace("maven.minecraftforge.net", "maven").Replace("maven.neoforged.net/releases", "maven")
                    If DlSourcePreferMojang Then
                        Urls.Add(BmclapiUrl)
                    Else
                        Urls.Insert(0, BmclapiUrl)
                    End If
                End If
            End If
            If Token.LocalPath.Contains("transformer-discovery-service") Then

                If Not FileUtils.Exists(Token.LocalPath) Then ExtractResources(Token.LocalPath, "Transformer")
                Logger.Warn("已自动释放 Transformer Discovery Service")
                Continue For
            ElseIf Token.LocalPath.Contains("optifine\OptiFine") Then

                Dim OptiFineBase As String = Token.LocalPath.Replace(CustomMcFolder & "libraries\optifine\OptiFine\", "").Split("_")(0) & "/" & PathUtils.GetLastPart(Token.LocalPath).Replace("-", "_")
                OptiFineBase = "/maven/com/optifine/" & OptiFineBase
                If OptiFineBase.Contains("_pre") Then OptiFineBase = OptiFineBase.Replace("com/optifine/", "com/optifine/preview_")
                Urls.Add("https://bmclapi2.bangbang93.com" & OptiFineBase)
            ElseIf Urls.Count <= 2 Then

                Urls.AddRange(DlSourceLibraryGet("https://libraries.minecraft.net" & Token.LocalPath.Replace(CustomMcFolder & "libraries", "").Replace("\", "/")))
            End If
            Result.Add(New NetFile(Urls.Distinct, Token.LocalPath, Checker))
        Next

        Return Result.DistinctBy(Function(a) a.LocalPath).ToList
    End Function

    Public Function McLibGet(Original As String, Optional WithHead As Boolean = True, Optional IgnoreLiteLoader As Boolean = False, Optional CustomMcFolder As String = Nothing) As String
        CustomMcFolder = If(CustomMcFolder, McFolderSelected)

        Dim Splited = Original.Split(":")
        Dim Result = If(WithHead, CustomMcFolder & "libraries\", "") &
            $"{Splited(0).Replace(".", "\")}\{Splited(1)}\{Splited(2)}\{Splited.Skip(1).Join("-")}.jar"
        Return Result
    End Function

    Public Function ShouldIgnoreFileCheck(Instance As McInstance)
        Return Settings.Get(Of Boolean)("VersionAdvanceAssetsV2", Instance:=Instance) OrElse (Settings.Get(Of Integer)("VersionAdvanceAssets", Instance:=Instance) = 2)
    End Function

#End Region

#Region "资源文件（Assets）"

    Public Function McAssetsGetIndex(Instance As McInstance, Optional ReturnLegacyOnError As Boolean = False, Optional CheckURLEmpty As Boolean = False) As JToken
        Dim AssetsName As String
        Try
            Do While True
                Dim Index As JToken = Instance.JsonObject("assetIndex")
                If Index IsNot Nothing AndAlso Index("id") IsNot Nothing Then Return Index
                If Instance.JsonObject("assets") IsNot Nothing Then AssetsName = Instance.JsonObject("assets").ToString
                If CheckURLEmpty AndAlso Index("url") IsNot Nothing Then Return Index

                If Instance.InheritName = "" Then Exit Do
                Instance = New McInstance(McFolderSelected & "versions\" & Instance.InheritName)
            Loop
        Catch
        End Try

        If ReturnLegacyOnError Then

            Logger.Info("无法获取资源文件索引下载地址，使用默认的 legacy 下载地址")
            Return "{
                ""id"": ""legacy"",
                ""sha1"": ""c0fd82e8ce9fbc93119e40d96d5a4e62cfa3f729"",
                ""size"": 134284,
                ""url"": ""https://launchermeta.mojang.com/mc-staging/assets/legacy/c0fd82e8ce9fbc93119e40d96d5a4e62cfa3f729/legacy.json"",
                ""totalSize"": 111220701
            }".DeserializeJson()

        Else
            Throw New Exception(GetLang("LangModMinecraftExceptionNoAssetsIndexInfo"))
        End If
    End Function

    Public Function McAssetsGetIndexName(Instance As McInstance) As String
        Try
            Do While True
                If Instance.JsonObject("assetIndex") IsNot Nothing AndAlso Instance.JsonObject("assetIndex")("id") IsNot Nothing Then
                    Return Instance.JsonObject("assetIndex")("id").ToString
                End If
                If Instance.JsonObject("assets") IsNot Nothing Then
                    Return Instance.JsonObject("assets").ToString
                End If
                If Instance.InheritName = "" Then Exit Do
                Instance = New McInstance(McFolderSelected & "versions\" & Instance.InheritName)
            Loop
        Catch ex As Exception
            Logger.Warn(ex, "获取资源文件索引名失败")
        End Try
        Return "legacy"
    End Function

    Private Structure McAssetsToken

        Public LocalPath As String

        Public SourcePath As String

        Public Size As Long

        Public Hash As String

        Public Overrides Function ToString() As String
            Return StringUtils.FormatByteSize(Size) & " | " & LocalPath
        End Function
    End Structure

    Private Function McAssetsListGet(Instance As McInstance) As List(Of McAssetsToken)
        Dim IndexName = McAssetsGetIndexName(Instance)
        Try

            If Not FileUtils.Exists($"{McFolderSelected}assets\indexes\{IndexName}.json") Then Throw New FileNotFoundException(GetLang("LangModMinecraftExceptionAssetsIndexFileNotFound"), McFolderSelected & "assets\indexes\" & IndexName & ".json")
            Dim Result As New List(Of McAssetsToken)
            Dim Json As JObject = FileUtils.ReadAsJson($"{McFolderSelected}assets\indexes\{IndexName}.json")

            For Each File As JProperty In Json("objects").Children
                Dim LocalPath As String
                If Json("map_to_resources") IsNot Nothing AndAlso Json("map_to_resources").ToObject(Of Boolean) Then

                    LocalPath = $"{Instance.PathIndie}resources\{File.Name}"
                ElseIf Json("virtual") IsNot Nothing AndAlso Json("virtual").ToObject(Of Boolean) Then

                    LocalPath = $"{McFolderSelected}assets\virtual\legacy\{File.Name}"
                Else

                    LocalPath = $"{McFolderSelected}assets\objects\{Left(File.Value()("hash").ToString, 2)}\{File.Value()("hash")}"
                End If
                Result.Add(New McAssetsToken With {
                    .LocalPath = LocalPath.Replace("/", "\"),
                    .SourcePath = File.Name,
                    .Hash = File.Value()("hash").ToString,
                    .Size = File.Value()("size").ToString
                })
            Next
            Return Result

        Catch ex As Exception
            Logger.Warn(ex, $"获取资源文件列表失败：{IndexName}")
            Throw
        End Try
    End Function

    Public Function McAssetsFixList(Instance As McInstance, CheckHash As Boolean, Optional ByRef ProgressFeed As LoaderBase = Nothing) As List(Of NetFile)

        If CheckHash Then
            Return McAssetsListGet(Instance).
                Select(Function(Token As McAssetsToken) New NetFile(
                    DlSourceAssetsGet($"https://resources.download.minecraft.net/{Left(Token.Hash, 2)}/{Token.Hash}"),
                    LocalPath:=Token.LocalPath,
                    Checker:=New FileChecker With {.ActualSize = If(Token.Size = 0, -1, Token.Size), .Hash = Token.Hash})).ToList
        End If

        Dim Result As New List(Of NetFile)
        Dim AssetsList As List(Of McAssetsToken)
        Try
            AssetsList = McAssetsListGet(Instance)
            Dim Token As McAssetsToken
            If ProgressFeed IsNot Nothing Then ProgressFeed.Progress = 0.04
            For i = 0 To AssetsList.Count - 1

                Token = AssetsList(i)
                If ProgressFeed IsNot Nothing Then ProgressFeed.Progress = 0.05 + 0.94 * i / AssetsList.Count

                Dim File = FileUtils.GetInfo(Token.LocalPath)
                If File.Exists AndAlso (Token.Size = 0 OrElse Token.Size = File.Length) Then Continue For

                Result.Add(New NetFile(DlSourceAssetsGet($"https://resources.download.minecraft.net/{Left(Token.Hash, 2)}/{Token.Hash}"), Token.LocalPath, New FileChecker With {.ActualSize = If(Token.Size = 0, -1, Token.Size), .Hash = Token.Hash}))
            Next
        Catch ex As Exception
            Logger.Warn(ex, "获取版本缺失的资源文件下载列表失败")
        End Try
        If ProgressFeed IsNot Nothing Then ProgressFeed.Progress = 0.99
        Return Result
    End Function

#End Region

    Public Sub McDownloadClientUpdateHint(VersionName As String, Json As JObject)
        Try

            Dim Version As JToken = Nothing
            For Each Token In Json("versions")
                If Token("id") IsNot Nothing AndAlso Token("id").ToString = VersionName Then
                    Version = Token
                    Exit For
                End If
            Next

            If Version Is Nothing Then Return
            Dim Time As Date = Version("releaseTime")
            Dim MsgBoxText As String = GetLang("LangModMinecraftDialogNewVersionContentA") & $"{VersionName}{vbCrLf}" &
                If((Date.Now - Time).TotalDays > 1, GetLang("LangModMinecraftDialogNewVersionContentB") & Time.ToString, GetLang("LangModMinecraftDialogNewVersionContentC") & StringUtils.FormatTimeSpan(Time - Date.Now, False))
            Dim MsgResult = MyMsgBox(MsgBoxText, GetLang("LangModMinecraftDialogNewVersionTitle"), GetLang("LangDialogBtnOK"), GetLang("LangModMinecraftDialogNewVersionBtnDownload"), If((Date.Now - Time).TotalHours > 3, GetLang("LangModMinecraftDialogNewVersionBtnLog"), ""),
                Button3Action:=Sub() McUpdateLogShow(Version))

            If MsgResult = 2 Then

                RunInUi(
                Sub()
                    PageDownloadInstall.VersionWaitingSelect = VersionName
                    FrmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadInstall)
                End Sub)
            End If

        Catch ex As Exception
            Logger.Error(ex, $"Minecraft 更新提示发送失败（{If(VersionName, "Nothing")}）")
        End Try
    End Sub

    Public Function CompareVersionGE(Left As String, Right As String) As Boolean
        Return CompareVersion(Left, Right) >= 0
    End Function

    Public Function CompareVersion(Left As String, Right As String) As Integer
        If Left = "未知版本" OrElse Right = "未知版本" Then
            If Left = "未知版本" AndAlso Right <> "未知版本" Then Return 1
            If Left = "未知版本" AndAlso Right = "未知版本" Then Return 0
            If Left <> "未知版本" AndAlso Right = "未知版本" Then Return -1
        End If
        Left = Left.Lower
        Right = Right.Lower
        Dim Lefts = Left.Replace("快照", "snapshot").Replace("预览版", "pre").RegexSearch("[a-z]+|[0-9]+").ToList
        Dim Rights = Right.Replace("快照", "snapshot").Replace("预览版", "pre").RegexSearch("[a-z]+|[0-9]+").ToList
        Dim i As Integer = 0
        While True

            If Lefts.Count - 1 < i AndAlso Rights.Count - 1 < i Then
                If Left > Right Then Return 1
                If Left < Right Then Return -1
                Return 0
            End If

            Dim LeftValue As String = If(Lefts.Count - 1 < i, 0, Lefts(i))
            Dim RightValue As String = If(Rights.Count - 1 < i, 0, Rights(i))
            If LeftValue = RightValue Then GoTo NextEntry
            If LeftValue = "rc" Then LeftValue = -1
            If LeftValue = "pre" Then LeftValue = -2
            If LeftValue = "snapshot" Then LeftValue = -3
            If LeftValue = "experimental" Then LeftValue = -4
            Dim LeftValValue = Val(LeftValue)
            If RightValue = "rc" Then RightValue = -1
            If RightValue = "pre" Then RightValue = -2
            If RightValue = "snapshot" Then RightValue = -3
            If RightValue = "experimental" Then RightValue = -4
            Dim RightValValue = Val(RightValue)
            If LeftValValue = 0 AndAlso RightValValue = 0 Then

                If LeftValue > RightValue Then
                    Return 1
                ElseIf LeftValue < RightValue Then
                    Return -1
                End If
            Else

                If LeftValValue > RightValValue Then
                    Return 1
                ElseIf LeftValValue < RightValValue Then
                    Return -1
                End If
            End If
NextEntry:
            i += 1
        End While
        Return 0
    End Function

    Public Class VersionComparer
        Implements IComparer(Of String)
        Public Function Compare(x As String, y As String) As Integer Implements IComparer(Of String).Compare
            Return CompareVersion(x, y)
        End Function
    End Class

    Public Function FilterAccessToken(Raw As String, FilterChar As Char) As String

        If Raw.Contains("accessToken ") Then
            For Each Token In Raw.RegexSearch("(?<=accessToken ([^ ]{5}))[^ ]+(?=[^ ]{5})")
                Raw = Raw.Replace(Token, New String(FilterChar, Token.Count))
            Next
        End If

        Dim AccessToken As String = McLoginLoader.Output.AccessToken
        If AccessToken IsNot Nothing AndAlso AccessToken.Length >= 10 AndAlso Raw.ContainsIgnoreCase(AccessToken) AndAlso
            McLoginLoader.Output.Uuid <> McLoginLoader.Output.AccessToken Then
            Raw = Raw.Replace(AccessToken, Left(AccessToken, 5) & New String(FilterChar, AccessToken.Length - 10) & Right(AccessToken, 5))
        End If
        Return Raw
    End Function

    Public Function FilterUserName(Raw As String, FilterChar As Char) As String
        If Raw.Contains(":\Users\") Then
            For Each Token In Raw.RegexSearch("(?<=:\\Users\\)[^\\]+")
                Raw = Raw.Replace("\Users\" & Token, "\Users\" & New String(FilterChar, Token.Count))
            Next
        End If
        If Raw.Contains(":/Users/") Then
            For Each Token In Raw.RegexSearch("(?<=:/Users/)[^/]+")
                Raw = Raw.Replace("/Users/" & Token, "/Users/" & New String(FilterChar, Token.Count))
            Next
        End If
        Return Raw
    End Function

End Module
