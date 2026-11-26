
Friend Module ModXflAiExec

    Friend Const Tag As String = "[XFL][AI执行]"

    Friend _pickerDialog As MyMsgMulti = Nothing

    Friend _pickerPicked As List(Of String) = Nothing

    Friend _pickerDone As System.Threading.ManualResetEventSlim = Nothing

    Friend _pickerPickedFlag As Boolean = False
    Friend Const ProtoFence As String = "```xfl-act"

    Private _versionsCache As List(Of String) = Nothing
    Private ReadOnly _verLock As New Object()

    Friend ReadOnly Property Enabled As Boolean
        Get
            Try
                Return Settings.Get(Of Boolean)("AiExecEnabled")
            Catch
                Return False
            End Try
        End Get
    End Property

    Friend Function ProtocolPrompt() As String
        Return "你可以执行启动器操作。若用户要求你做事（启动、下载、跳转），" & vbCrLf &
               "请在回答最后追加一个代码块，语言标记为 xfl-act，内容为一个 JSON 对象：" & vbCrLf &
               "```xfl-act" & vbCrLf &
               "{""actions"":[{""act"":""launch"",""instance"":""1.20.1-Forge""}]}" & vbCrLf &
               "```" & vbCrLf &
               "可用动作（act 取值）：" & vbCrLf &
               "· launch：启动已安装版本，参数 instance（版本名，可模糊）" & vbCrLf &
               "· download_version：下载游戏版本，参数 version（如 1.20.1）、loader（forge/fabric/neoforge/vanilla）、loader_version（可空）、name（实例名，可空）" & vbCrLf &
               "· download_resource：搜索并下载社区资源。参数 type（mod/resourcepack/shader/datapack）、query（名称关键词）、game_version（可空）、loader（可空）；" & vbCrLf &               "  当用户只描述用途（如「优化模组」「让游戏更流畅」「加点建造类的」）而没给具体名字时，" & vbCrLf &               "  用 category 参数指定 Modrinth 官方分类（见分类说明），query 留空；" & vbCrLf &               "  此时启动器会列出该分类下适配的模组让你勾选，再下载你选中的。" & vbCrLf &
               "· open_page：页面跳转，参数 page（launch/download/setup/login/link/music/browser）" & vbCrLf &
               "只输出确实需要的动作；不确定参数就留空让启动器取默认值；不要编造版本号。"
    End Function

    Friend Function AvailableVersions() As List(Of String)
        SyncLock _verLock
            Try
                If _versionsCache IsNot Nothing Then Return _versionsCache
                Dim js As JObject = Nothing
                For Each u As String In New String() {
                        "https://launchermeta.mojang.com/mc/game/version_manifest.json",
                        "https://bmclapi2.bangbang93.com/mc/game/version_manifest.json"}
                    Try
                        Dim rawTxt As String = NetRequestByClientRetry(u, RequireJson:=True)
                        If String.IsNullOrWhiteSpace(rawTxt) Then
                            Logger.Warn($"{Tag} 版本清单返回空内容：{u}")
                            Continue For
                        End If
                        js = JObject.Parse(rawTxt)
                        If js IsNot Nothing Then Exit For
                    Catch ex As Exception
                        Logger.Warn(ex, $"{Tag} 版本清单请求失败：{u}")
                    End Try
                Next
                Dim list As New List(Of String)
                If js IsNot Nothing Then
                    Dim arr As JToken = js("versions")
                    If arr IsNot Nothing AndAlso arr.Type = JTokenType.Array Then
                        For Each v As JToken In CType(arr, JArray)
                            Try
                                If v("type")?.ToString = "release" Then list.Add(v("id")?.ToString)
                            Catch
                            End Try
                        Next
                    End If
                End If
                _versionsCache = list
                Logger.Info($"{Tag} 真实版本清单：{list.Count} 个正式版，最新 {If(list.Count > 0, list(0), "未知")}")
                Return list
            Catch ex As Exception
                Logger.Warn(ex, $"{Tag} 获取版本清单失败")
                Return New List(Of String)
            End Try
        End SyncLock
    End Function

    Friend Function VersionsHint() As String
        Try
            Dim rel As List(Of String) = AvailableVersions()
            If rel Is Nothing OrElse rel.Count = 0 Then Return ""
            Dim top As List(Of String) = rel.Take(15).ToList()
            Return "【真实版本清单】启动器当前可下载的 Minecraft 正式版（官方清单，最新在前）：" &
                   String.Join("、", top) & "。" & vbCrLf &
                   "规则：用户提到的版本若在清单里，就原样使用清单写法；若不在清单里，" &
                   "不要猜、不要替换成别的版本号，直接说「没有这个版本」并列出清单里相近的几项。"
        Catch
            Return ""
        End Try
    End Function

    Friend Function IsKnownVersion(ver As String) As Boolean
        Try
            If String.IsNullOrWhiteSpace(ver) Then Return False
            Dim rel As List(Of String) = AvailableVersions()
            If rel Is Nothing OrElse rel.Count = 0 Then Return True
            Return rel.Any(Function(x As String) String.Equals(x, ver.Trim(), StringComparison.OrdinalIgnoreCase))
        Catch
            Return True
        End Try
    End Function

    Friend Function ExtractActions(reply As String) As List(Of JObject)
        Dim Result As New List(Of JObject)
        Try
            If String.IsNullOrWhiteSpace(reply) Then Return Result
            Dim mm As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(
                reply, "```xfl-act\s*(\{[\s\S]*?\})\s*```", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            If Not mm.Success Then Return Result
            Dim js As JObject = JObject.Parse(mm.Groups(1).Value)
            Dim arr As JToken = js("actions")
            If arr Is Nothing OrElse arr.Type <> JTokenType.Array Then Return Result
            For Each it As JToken In CType(arr, JArray)
                If it.Type = JTokenType.Object Then Result.Add(CType(it, JObject))
            Next
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 解析动作失败")
        End Try
        Return Result
    End Function

    Friend Function StripActions(reply As String) As String
        Try
            If String.IsNullOrWhiteSpace(reply) Then Return reply
            Return System.Text.RegularExpressions.Regex.Replace(
                reply, "```xfl-act[\s\S]*?```", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim()
        Catch
            Return reply
        End Try
    End Function

    Friend Function VersionMismatchWarning(userText As String, actions As List(Of JObject)) As String
        Try
            If String.IsNullOrWhiteSpace(userText) OrElse actions Is Nothing Then Return ""
            Dim mentioned As New List(Of String)
            For Each mm As System.Text.RegularExpressions.Match In
                    System.Text.RegularExpressions.Regex.Matches(userText, "\d+\.\d+(?:\.\d+)?")
                If Not mentioned.Contains(mm.Value) Then mentioned.Add(mm.Value)
            Next
            If mentioned.Count = 0 Then Return ""
            Dim targets As New List(Of String)
            For Each a As JObject In actions
                Dim act As String = SafeStr(a, "act").ToLower
                If act = "download_version" Then
                    targets.Add(SafeStr(a, "version"))
                ElseIf act = "launch" Then
                    targets.Add(SafeStr(a, "instance"))
                End If
            Next
            targets = targets.Where(Function(s As String) s <> "").ToList()
            If targets.Count = 0 Then Return ""

            For Each m As String In mentioned
                For Each tg As String In targets
                    If tg.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0 Then Return ""
                Next
            Next
            Dim rel As List(Of String) = AvailableVersions()
            Dim cand As String = ""
            If rel IsNot Nothing AndAlso rel.Count > 0 Then
                cand = "。当前可下载的最新正式版：" & String.Join("、", rel.Take(6))
            End If
            Dim msg As String = $"你原话说的是「{String.Join("、", mentioned)}」，" &
                                $"但下面要操作的是「{String.Join("、", targets)}」，两者不一致{cand}。"
            Logger.Info($"{Tag} 版本不一致提醒：{msg}")
            Return msg
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 版本核对失败")
            Return ""
        End Try
    End Function

    Friend Function SearchModProject(query As String, gv As String, loader As String,
                                    rType As String) As Tuple(Of String, String, ResourcePlatforms, String)
        Try
            If String.IsNullOrWhiteSpace(query) Then Return Nothing
            Dim candidates As New List(Of Tuple(Of String, String, ResourcePlatforms, String))

            Try
                Dim facets As New List(Of String)
                facets.Add($"[""project_type:{rType}""]")
                If gv <> "" Then facets.Add($"[""versions:{gv}""]")
                If loader <> "" AndAlso rType = "mod" Then facets.Add($"[""categories:{loader}""]")

                Dim url As String = "https://api.modrinth.com/v2/search?limit=10&index=relevance&query=" &
                                    StringUtils.UrlEscape(query) & "&facets=" &
                                    StringUtils.UrlEscape("[" & String.Join(",", facets) & "]")
                Dim js As JToken = DlModRequest(url)
                Dim hits As JToken = js("hits")
                If hits IsNot Nothing AndAlso hits.Type = JTokenType.Array Then
                    For Each it As JToken In CType(hits, JArray)
                        Try
                            Dim h As JObject = CType(it, JObject)
                            Dim pid As String = SafeStr(h, "project_id")
                            If pid = "" Then pid = SafeStr(h, "slug")
                            If pid = "" Then Continue For
                            Dim title As String = SafeStr(h, "title")
                            Dim desc As String = SafeStr(h, "description")
                            If Not NameLooksRelated(query, title) Then Continue For
                            candidates.Add(Tuple.Create(pid, title, ResourcePlatforms.Modrinth, desc))
                        Catch
                        End Try
                    Next
                End If
            Catch ex As Exception
                Logger.Warn(ex, $"{Tag} Modrinth 搜索失败")
            End Try

            If Not candidates.Any() Then
                Try
                    Dim cfUrl As String = "https://api.curseforge.com/v1/mods/search?gameId=432&pageSize=20&searchFilter=" &
                                        StringUtils.UrlEscape(query)
                    Dim cfJs As JToken = DlModRequest(cfUrl)
                    Dim cfData As JToken = cfJs("data")
                    If cfData IsNot Nothing AndAlso cfData.Type = JTokenType.Array Then
                        For Each it As JToken In CType(cfData, JArray)
                            Try
                                Dim c As JObject = CType(it, JObject)
                                Dim pid As String = SafeStr(c, "id")
                                If pid = "" Then Continue For
                                Dim title As String = SafeStr(c, "name")
                                Dim desc As String = SafeStr(c, "summary")
                                If Not NameLooksRelated(query, title) Then Continue For
                                candidates.Add(Tuple.Create(pid, title, ResourcePlatforms.CurseForge, desc))
                            Catch
                            End Try
                        Next
                    End If
                Catch ex As Exception
                    Logger.Warn(ex, $"{Tag} CurseForge 搜索失败")
                End Try
            End If
            If candidates.Count = 0 Then
                Logger.Info($"{Tag} 未找到与「{query}」匹配的资源")
                Return Nothing
            End If
            Logger.Info($"{Tag} 搜到 {candidates.Count} 个候选，首个：{candidates(0).Item2}")
            Return candidates(0)
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 资源搜索失败")
            Return Nothing
        End Try
    End Function

    Friend Function NameLooksRelated(query As String, title As String) As Boolean
        Try
            If String.IsNullOrWhiteSpace(query) OrElse String.IsNullOrWhiteSpace(title) Then Return False
            Dim q As String = query.Trim.ToLower
            Dim t As String = title.Trim.ToLower
            If t.Contains(q) OrElse q.Contains(t) Then Return True

            Dim q2 As String = q.Replace(" ", "").Replace("-", "").Replace("_", "").Replace(".", "")
            Dim t2 As String = t.Replace(" ", "").Replace("-", "").Replace("_", "").Replace(".", "")
            If q2.Length < 2 Then Return False
            Return t2.Contains(q2)
        Catch
            Return False
        End Try
    End Function

    Friend Function Describe(actions As List(Of JObject)) As String
        Dim sb As New System.Text.StringBuilder
        Dim i As Integer = 0
        For Each a As JObject In actions
            i += 1
            Dim act As String = SafeStr(a, "act").ToLower
            Select Case act
                Case "launch"
                    sb.AppendLine($"{i}. 启动版本：{SafeStr(a, "instance")}")
                Case "download_version"
                    Dim loader As String = SafeStr(a, "loader").ToLower
                    Dim extra As String = If(loader = "" OrElse loader = "vanilla", "原版", loader)
                    Dim lv As String = SafeStr(a, "loader_version")
                    If lv <> "" Then extra &= " " & lv
                    sb.AppendLine($"{i}. 下载游戏版本：{SafeStr(a, "version")}（{extra}）")
                Case "download_resource"
                    Dim t As String = SafeStr(a, "type").ToLower
                    Dim tn As String = If(t = "resourcepack", "资源包", If(t = "shader", "光影", If(t = "datapack", "数据包", "模组")))
                    sb.AppendLine($"{i}. 搜索并下载{tn}：{SafeStr(a, "query")}")
                Case "open_page"
                    sb.AppendLine($"{i}. 打开页面：{SafeStr(a, "page")}")
                Case Else
                    sb.AppendLine($"{i}. 不支持的动作：{act}")
            End Select
        Next
        Return sb.ToString().TrimEnd()
    End Function

    Friend Sub Execute(actions As List(Of JObject), OnLog As Action(Of String))
        RunInNewThread(
            Sub()
                For Each a As JObject In actions
                    Try
                        Dim act As String = SafeStr(a, "act").ToLower
                        Select Case act
                            Case "launch"
                                DoLaunch(SafeStr(a, "instance"), OnLog)
                            Case "download_version"
                                DoDownloadVersion(a, OnLog)
                            Case "download_resource"
                                DoDownloadResource(a, OnLog)
                            Case "open_page"
                                DoOpenPage(SafeStr(a, "page"), OnLog)
                            Case Else
                                Report(OnLog, $"跳过不支持的动作：{act}")
                        End Select
                    Catch ex As Exception
                        Logger.Warn(ex, $"{Tag} 执行动作失败")
                        Report(OnLog, "执行失败：" & ex.Message)
                    End Try
                Next
            End Sub, "XFL AI Exec")
    End Sub

    Private Sub Report(OnLog As Action(Of String), msg As String)
        Logger.Info($"{Tag} {msg}")
        Try
            RunInUi(Sub() OnLog?.Invoke(msg))
        Catch
        End Try
    End Sub

    Private Sub DoLaunch(name As String, OnLog As Action(Of String))
        Dim inst As McInstance = FindInstance(name)
        If inst Is Nothing Then
            Report(OnLog, $"没有找到版本「{name}」，已跳过启动")
            Return
        End If
        Report(OnLog, $"正在启动：{inst.Name}")
        RunInUi(Sub()
                    Try
                        McLaunchStart(New McLaunchOptions With {.Instance = inst})
                    Catch ex As Exception
                        Logger.Warn(ex, "[XFL][AI执行] 启动失败")
                    End Try
                End Sub)
    End Sub

    Friend Function FindInstance(name As String) As McInstance
        Try
            Dim all As New List(Of McInstance)
            For Each kv As KeyValuePair(Of McInstanceCardType, List(Of McInstance)) In McInstanceList
                If kv.Value IsNot Nothing Then all.AddRange(kv.Value)
            Next
            If all.Count = 0 Then Return Nothing
            If String.IsNullOrWhiteSpace(name) Then Return all(0)
            Dim exact As McInstance = all.FirstOrDefault(Function(i As McInstance) String.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase))
            If exact IsNot Nothing Then Return exact
            Dim key As String = name.ToLower
            Return all.FirstOrDefault(Function(i As McInstance) i.Name.ToLower.Contains(key))
        Catch ex As Exception
            Logger.Warn(ex, "[XFL][AI执行] 查找实例失败")
            Return Nothing
        End Try
    End Function

    Friend Function ResolveLoaderVersion(loader As String, mcVer As String) As String
        Try
            Select Case loader.ToLower
                Case "fabric", "quilt"
                    Dim url As String = "https://meta.fabricmc.net/v2/versions/loader/" & mcVer
                    Dim rawTxt As String = ""
                    Try
                        rawTxt = NetRequestByClientRetry(url, RequireJson:=True)
                    Catch ex As Exception
                        Logger.Warn(ex, $"{Tag} Fabric meta 请求失败，改用镜像")
                        rawTxt = NetRequestByClientRetry(
                            "https://bmclapi2.bangbang93.com/fabric-meta/v2/versions/loader/" & mcVer, RequireJson:=True)
                    End Try
                    If String.IsNullOrWhiteSpace(rawTxt) Then Return Nothing
                    Dim arr As JArray = JArray.Parse(rawTxt)
                    If arr Is Nothing OrElse arr.Count = 0 Then Return Nothing

                    For Each it As JToken In arr
                        Try
                            Dim ld As JToken = it("loader")
                            If ld Is Nothing Then Continue For
                            Dim ver As String = ld("version")?.ToString
                            If String.IsNullOrWhiteSpace(ver) Then Continue For
                            Dim stable As Boolean = False
                            Try
                                stable = ld("stable")?.ToObject(Of Boolean)
                            Catch
                            End Try
                            If stable Then Return ver
                        Catch
                        End Try
                    Next

                    For Each it As JToken In arr
                        Try
                            Dim ver As String = it("loader")?("version")?.ToString
                            If Not String.IsNullOrWhiteSpace(ver) Then Return ver
                        Catch
                        End Try
                    Next
                    Return Nothing
                Case "neoforge"

                    Return ResolveForgeVersion("neoforge", mcVer)
                Case "forge"
                    Return ResolveForgeVersion("forge", mcVer)
                Case Else
                    Return Nothing
            End Select
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 解析加载器版本失败：{loader} {mcVer}")
            Return Nothing
        End Try
    End Function

    Friend Function ResolveForgeVersion(loader As String, mcVer As String) As String
        Try
            If String.IsNullOrWhiteSpace(mcVer) Then Return Nothing
            If loader.ToLower = "forge" Then Return ResolveForgeBuild(mcVer)
            If loader.ToLower = "neoforge" Then Return ResolveNeoForgeBuild(mcVer)
            Return Nothing
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 解析 {loader} 构建号失败：{mcVer}")
            Return Nothing
        End Try
    End Function

    Private Function ResolveNeoForgeBuild(mcVer As String) As String
        Try
            Dim files As New List(Of JObject)

            For Each pkg In New String() {"neoforge", "forge"}
                Try
                    Dim rawTxt As String = NetRequestByClientRetry(
                        "https://bmclapi2.bangbang93.com/neoforge/meta/api/maven/details/releases/net/neoforged/" & pkg,
                        RequireJson:=True)
                    If String.IsNullOrWhiteSpace(rawTxt) Then Continue For
                    Dim js As JObject = JObject.Parse(rawTxt)
                    Dim fj As JToken = js("files")
                    If fj Is Nothing OrElse fj.Type <> JTokenType.Array Then Continue For
                    For Each f As JToken In CType(fj, JArray)
                        Try
                            If f("name") IsNot Nothing Then files.Add(CType(f, JObject))
                        Catch
                        End Try
                    Next
                Catch ex As Exception
                    Logger.Warn(ex, $"{Tag} NeoForge 包 {pkg} 拉取失败")
                End Try
            Next
            If files.Count = 0 Then Return Nothing
            Dim Best As String = Nothing
            Dim BestKey As String = ""
            For Each f As JObject In files
                Try
                    Dim nm As String = f("name")?.ToString
                    If String.IsNullOrWhiteSpace(nm) Then Continue For
                    If nm.StartsWith("maven-metadata") Then Continue For
                    Dim targetMc As String = NeoForgeTargetMc(nm)
                    If targetMc = "" Then Continue For

                    If Not McVersionPrefixMatch(targetMc, mcVer.Trim()) Then Continue For

                    Dim verPart As String = nm.BeforeFirst("+").BeforeFirst("-")
                    Dim isBeta As Boolean = nm.ContainsIgnoreCase("beta") OrElse nm.ContainsIgnoreCase("alpha")
                    Dim key As String = VerSortKey(verPart)
                    If isBeta Then key = "0" & key Else key = "1" & key
                    If key > BestKey Then
                        BestKey = key
                        Best = nm
                    End If
                Catch
                End Try
            Next
            If Best Is Nothing Then
                Logger.Info($"{Tag} NeoForge 未找到适配 {mcVer} 的构建")
                Return Nothing
            End If
            Logger.Info($"{Tag} NeoForge {mcVer} → {Best}")
            Return Best
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} NeoForge 构建解析失败")
            Return Nothing
        End Try
    End Function

    Private Function McVersionPrefixMatch(target As String, want As String) As Boolean
        Try
            If String.IsNullOrWhiteSpace(target) OrElse String.IsNullOrWhiteSpace(want) Then Return False
            If target.Equals(want, StringComparison.OrdinalIgnoreCase) Then Return True

            Dim tSeg As String() = target.Split("."c)
            Dim wSeg As String() = want.Split("."c)
            If tSeg.Length < 2 OrElse wSeg.Length < 2 Then Return False
            Return tSeg(0).Equals(wSeg(0), StringComparison.OrdinalIgnoreCase) AndAlso
                   tSeg(1).Equals(wSeg(1), StringComparison.OrdinalIgnoreCase)
        Catch
            Return False
        End Try
    End Function

    Private Function NeoForgeTargetMc(nm As String) As String
        Try
            If nm.Contains("-") AndAlso nm.StartsWith("1.") Then

                Return nm.BeforeFirst("-")
            End If
            Dim segs As String() = nm.BeforeFirst("+").BeforeFirst("-").Split("."c)
            If segs.Length < 2 Then Return ""

            Dim mc As String = segs(0) & "." & segs(1)

            If segs(2) = "0" AndAlso segs.Length >= 3 Then

            End If
            Return mc
        Catch
            Return ""
        End Try
    End Function

    Private Function VerSortKey(ver As String) As String
        Try
            Dim sb As New System.Text.StringBuilder
            For Each seg As String In ver.Split("."c)
                Dim digits As String = ""
                For Each ch As Char In seg
                    If ch >= "0"c AndAlso ch <= "9"c Then digits &= ch Else Exit For
                Next
                If digits = "" Then digits = "0"
                sb.Append(digits.PadLeft(6, "0"c))
            Next
            Return sb.ToString()
        Catch
            Return ""
        End Try
    End Function

    Private Function ResolveForgeBuild(mcVer As String) As String
        Try
            Dim rawTxt As String = NetRequestByClientRetry(
                "https://bmclapi2.bangbang93.com/forge/minecraft/" & mcVer, RequireJson:=True)
            If String.IsNullOrWhiteSpace(rawTxt) Then Return Nothing
            Dim arr As JArray = JArray.Parse(rawTxt)
            If arr Is Nothing OrElse arr.Count = 0 Then Return Nothing
            Dim Best As String = Nothing
            Dim BestKey As String = ""
            For Each it As JToken In arr
                Try
                    Dim v As String = it("version")?.ToString
                    If String.IsNullOrWhiteSpace(v) Then Continue For

                    Dim rec As Boolean = False
                    Try
                        rec = it("recommended")?.ToObject(Of Boolean)
                    Catch
                    End Try
                    Dim key As String = VerSortKey(v)
                    If rec Then key = "9" & key
                    If key > BestKey Then
                        BestKey = key
                        Best = v
                    End If
                Catch
                End Try
            Next
            If Best Is Nothing Then
                Logger.Info($"{Tag} Forge 未找到适配 {mcVer} 的构建")
                Return Nothing
            End If
            Logger.Info($"{Tag} Forge {mcVer} → {Best}")
            Return Best
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} Forge 构建解析失败")
            Return Nothing
        End Try
    End Function

    Friend Function ExpandTagCandidates(tag As String, mcVer As String, loader As String,
                                       OnNote As Action(Of String)) As List(Of Tuple(Of String, String))
        Dim Result As New List(Of Tuple(Of String, String))
        Try
            Dim cands As List(Of ModXflCatalogEntry) = ModXflCatalog.ByTag(tag)
            If cands Is Nothing OrElse cands.Count = 0 Then Return Result
            Dim used As New HashSet(Of String)
            For Each c In cands
                Try
                    If Not ModXflCatalog.LoaderOk(c, loader) Then
                        OnNote?.Invoke($"{c.Title} 不支持 {loader}，跳过")
                        Continue For
                    End If

                    Dim versions As List(Of ResourceVersion) = Nothing
                    Try
                        versions = ResourceVersion.FromProjectId(c.ProjectId, ResourcePlatforms.Modrinth, LoadDependencies:=False)
                    Catch ex1 As Exception
                        Logger.Warn(ex1, $"{Tag} 取模组版本失败：{c.ProjectId}")
                    End Try
                    If versions Is Nothing OrElse Not versions.Any() Then
                        OnNote?.Invoke($"{c.Title} 没有可用版本，跳过")
                        Continue For
                    End If
                    Dim ok As Boolean = False
                    If String.IsNullOrWhiteSpace(mcVer) Then
                        ok = True
                    Else
                        For Each v In versions
                            If v.GameVersions IsNot Nothing AndAlso
                               v.GameVersions.Any(Function(g As String) g.Equals(mcVer.Trim(), StringComparison.OrdinalIgnoreCase)) Then
                                ok = True
                                Exit For
                            End If
                        Next
                    End If
                    If Not ok Then
                        OnNote?.Invoke($"{c.Title} 没有适配 {mcVer} 的版本，跳过")
                        Continue For
                    End If
                    If Not used.Add(c.ProjectId) Then Continue For
                    Result.Add(Tuple.Create(c.Title, c.ProjectId))
                Catch exEach As Exception
                    Logger.Warn(exEach, $"{Tag} 处理候选失败：{c.ProjectId}")
                End Try
            Next
            Logger.Info($"{Tag} 标签 {tag} → {Result.Count} 个可用模组")
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 展开标签候选失败")
        End Try
        Return Result
    End Function

    Private Sub DoDownloadVersion(a As JObject, OnLog As Action(Of String))
        Dim ver As String = SafeStr(a, "version")
        If ver = "" Then
            Report(OnLog, "缺少 version 参数，已跳过下载")
            Return
        End If

        If Not IsKnownVersion(ver) Then
            Dim rel As List(Of String) = AvailableVersions()
            Dim tip As String = ""
            If rel IsNot Nothing AndAlso rel.Count > 0 Then
                tip = "（当前最新正式版：" & rel(0) & "；可选：" & String.Join("、", rel.Take(8)) & "）"
            End If
            Report(OnLog, $"没有「{ver}」这个版本，已取消下载。{tip}")
            Return
        End If
        Dim loader As String = SafeStr(a, "loader").ToLower
        Dim lv As String = SafeStr(a, "loader_version")

        Dim isVanilla As Boolean = (loader = "" OrElse loader = "vanilla" OrElse loader = "none")
        If Not isVanilla AndAlso String.IsNullOrWhiteSpace(lv) Then
            Report(OnLog, $"正在查询 {ver} 可用的 {loader} 加载器版本…")
            lv = ResolveLoaderVersion(loader, ver)
            If String.IsNullOrWhiteSpace(lv) Then
                Report(OnLog, $"无法自动确定 {ver} 对应的 {loader} 加载器版本，已中止安装。" &
                              "（可在「下载 → 安装整合包/加载器」里手动选择具体构建号）")
                Return
            End If
            Report(OnLog, $"{loader} 加载器版本：{lv}")
        End If
        Dim name As String = SafeStr(a, "name")
        If name = "" Then
            name = ver & If(isVanilla, "", "-" & loader)
        End If
        Dim req As New McInstallRequest With {
            .NewInstanceName = name,
            .VersionFolder = McFolderSelected & "versions\" & name & "\",
            .MinecraftName = ver}
        Select Case loader
            Case "forge" : req.ForgeVersion = lv
            Case "fabric" : req.FabricVersion = lv
            Case "quilt" : req.FabricVersion = lv
            Case "neoforge" : req.NeoForgeVersion = lv
        End Select

        If Not isVanilla AndAlso lv = "" Then
            Report(OnLog, $"缺少 {loader} 加载器版本，已中止（避免只装原版却挂个 {loader} 名字）")
            Return
        End If
        Report(OnLog, $"正在下载：{ver}（{If(loader = "", "原版", loader)}）→ 实例 {name}")
        RunInUi(Sub()
                    Try
                        McInstall(req)
                    Catch ex As Exception
                        Logger.Warn(ex, "[XFL][AI执行] 下载版本失败")
                    End Try
                End Sub)
    End Sub

    Private Sub DoDownloadResource(a As JObject, OnLog As Action(Of String))
        Dim catName As String = SafeStr(a, "category").ToLower
        If catName <> "" Then
            Dim gv3 As String = SafeStr(a, "game_version")
            Dim ld3 As String = SafeStr(a, "loader").ToLower
            Report(OnLog, $"正在按分类「{catName}」查找适配的模组…")
            Dim catList As New List(Of String)
            catList.Add(catName)

            If String.IsNullOrWhiteSpace(gv3) Then
                Dim curInst As McInstance = If(McInstanceSelected, Nothing)
                If curInst IsNot Nothing AndAlso curInst.Version IsNot Nothing Then
                    gv3 = curInst.Version.VanillaName
                End If
            End If
            Dim cands As List(Of ModXflModInfo) = ModXflMods.ByCategory(catList, gv3, ld3, 12)
            If cands Is Nothing OrElse cands.Count = 0 Then
                Report(OnLog, $"分类「{catName}」下没有找到适配当前版本/加载器的模组，已跳过。")
                Return
            End If

            ModXflCn.EnsureCnNames(cands)
            Report(OnLog, $"找到 {cands.Count} 个候选模组，请勾选要下载的：")
            Dim pickedIds As List(Of String) = ShowModPicker(
                $"选择要下载的模组（分类：{catName}）",
                $"共 {cands.Count} 个候选，已按下载量排序。勾选后点「下载所选」；不勾选则不会下载任何模组。",
                cands)
            If pickedIds Is Nothing OrElse pickedIds.Count = 0 Then
                Report(OnLog, "没有勾选任何模组，已取消。")
                Return
            End If
            Report(OnLog, $"已选择 {pickedIds.Count} 个模组，开始下载…")
            For Each pid In pickedIds
                Dim nm As String = pid
                For Each c In cands
                    If c.ProjectId = pid Then nm = c.Title : Exit For
                Next
                DownloadOneMod(pid, nm, gv3, ld3, OnLog)
            Next
            Return
        End If
        Dim tagName As String = SafeStr(a, "tag").ToLower
        If tagName <> "" Then
            Dim gv2 As String = SafeStr(a, "game_version")
            Dim ld2 As String = SafeStr(a, "loader").ToLower
            Report(OnLog, $"正在按用途「{tagName}」挑选适合的模组…")

            Dim catIds As New List(Of String)
            For Each ce In ModXflCatalog.ByTag(tagName)
                If catIds.Contains(ce.ProjectId) Then Continue For
                catIds.Add(ce.ProjectId)
            Next
            If catIds.Count = 0 Then
                Report(OnLog, $"没有适合「{tagName}」的模组，已跳过。")
                Return
            End If
            If String.IsNullOrWhiteSpace(gv2) Then
                Dim curInst2 As McInstance = If(McInstanceSelected, Nothing)
                If curInst2 IsNot Nothing AndAlso curInst2.Version IsNot Nothing Then
                    gv2 = curInst2.Version.VanillaName
                End If
            End If
            Dim cands2 As List(Of ModXflModInfo) = ModXflMods.ByCategory(catIds, gv2, ld2, 12)
            If cands2 Is Nothing OrElse cands2.Count = 0 Then
                Report(OnLog, $"没有找到适合「{tagName}」且兼容当前版本/加载器的模组，已跳过。")
                Return
            End If

            ModXflCn.EnsureCnNames(cands2)
            Report(OnLog, $"找到 {cands2.Count} 个候选模组，请勾选要下载的：")
            Dim pickedIds2 As List(Of String) = ShowModPicker(
                $"选择要下载的模组（用途：{tagName}）",
                $"共 {cands2.Count} 个候选，已按下载量排序。勾选后点「下载所选」；不勾选则不会下载任何模组。",
                cands2)
            If pickedIds2 Is Nothing OrElse pickedIds2.Count = 0 Then
                Report(OnLog, "没有勾选任何模组，已取消。")
                Return
            End If
            Report(OnLog, $"已选择 {pickedIds2.Count} 个模组，开始下载…")
            For Each pid2 In pickedIds2
                Dim nm2 As String = pid2
                For Each c2 In cands2
                    If c2.ProjectId = pid2 Then nm2 = c2.Title : Exit For
                Next
                DownloadOneMod(pid2, nm2, gv2, ld2, OnLog)
            Next
            Return
        End If
        Dim query As String = SafeStr(a, "query")
        Dim typeStr As String = SafeStr(a, "type").ToLower
        Dim gv As String = SafeStr(a, "game_version")
        Dim loader As String = SafeStr(a, "loader").ToLower
        Dim pidGiven As String = SafeStr(a, "mod_id")
        If query = "" AndAlso pidGiven = "" Then
            Report(OnLog, "缺少 query（模组名）参数，已跳过下载")
            Return
        End If
        Dim pt As String = If(typeStr = "resourcepack", "resourcepack",
                              If(typeStr = "shader", "shader", If(typeStr = "datapack", "datapack", "mod")))

        Dim platform As ResourcePlatforms = ResourcePlatforms.Modrinth
        Dim projectId As String = pidGiven
        Dim title As String = ""
        If projectId = "" Then
            Dim found As Tuple(Of String, String, ResourcePlatforms, String) = SearchModProject(query, gv, loader, pt)
            If found Is Nothing Then
                Report(OnLog, $"没找到与「{query}」匹配的资源（已尝试 Modrinth 与 CurseForge），已跳过。" &
                              "可以换个更准确的名称，或直接给我模组 ID。")
                Return
            End If
            projectId = found.Item1
            title = found.Item2
            platform = found.Item3
            If Not String.IsNullOrWhiteSpace(found.Item4) Then
                Report(OnLog, $"匹配到「{title}」：{ShortenSafe(found.Item4, 60)}")
            End If
        End If
        Report(OnLog, $"正在获取「{If(title = "", projectId, title)}」的版本列表…")
        Dim versions As List(Of ResourceVersion) = Nothing
        Try
            versions = ResourceVersion.FromProjectId(projectId, platform, LoadDependencies:=False)
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 获取项目版本失败：{projectId}")
        End Try
        If versions Is Nothing OrElse Not versions.Any() Then
            Report(OnLog, $"「{If(title = "", projectId, title)}」没有可用版本，已跳过")
            Return
        End If
        Dim pick As ResourceVersion = PickVersion(versions, gv, loader)
        If pick Is Nothing Then
            Report(OnLog, $"「{If(title = "", projectId, title)}」没有兼容版本，已跳过")
            Return
        End If
        Dim inst As McInstance = If(McInstanceSelected, FirstInstance())
        If inst Is Nothing Then
            Report(OnLog, "没有可用游戏实例，无法确定安装位置")
            Return
        End If
        Dim sub2 As String = If(pt = "resourcepack", "resourcepacks",
                               If(pt = "shader", "shaderpacks", If(pt = "datapack", "datapacks", "mods")))
        Dim folder As String = IO.Path.Combine(inst.PathIndie, sub2)
        Try : IO.Directory.CreateDirectory(folder) : Catch : End Try
        Dim file As String = IO.Path.Combine(folder, pick.FileName)
        Dim loaders As New List(Of LoaderBase)
        loaders.Add(New LoaderDownload(pick.Display,
                    {pick.ToNetFile(file, ResourceVersion.DownloadReason.Standalone, gv, pick.ModLoaders)}) With {.ProgressWeight = 6, .Block = True})

        If platform = ResourcePlatforms.Modrinth Then
            Try
                Dim deps As List(Of ResourceVersion) = ModXflDep.CollectMissing(pick, ResourcePlatforms.Modrinth, folder)
                For Each d As ResourceVersion In deps
                    Dim dp As String = IO.Path.Combine(folder, d.FileName)
                    If IO.File.Exists(dp) Then Continue For
                    loaders.Add(New LoaderDownload($"前置：{d.Display}",
                                {d.ToNetFile(dp, ResourceVersion.DownloadReason.Dependency, gv, pick.ModLoaders)}) With {.ProgressWeight = 3, .Block = True})
                Next
            Catch ex As Exception
                Logger.Warn(ex, "[XFL][AI执行] 补前置失败")
            End Try
        End If
        Report(OnLog, $"正在下载：{pick.Display}（{pick.FileName}）→ {inst.Name}\{sub2}")
        RunInUi(Sub()
                    Try
                        Dim combo As New LoaderCombo(Of Integer)($"AI 下载：{If(title = "", pick.Display, title)}", loaders)
                        combo.Start(1)
                        LoaderTaskbarAdd(combo)
                        FrmMain.BtnExtraDownload.ShowRefresh()
                        FrmMain.BtnExtraDownload.Ribble()
                    Catch ex As Exception
                        Logger.Warn(ex, "[XFL][AI执行] 启动下载失败")
                    End Try
                End Sub)
    End Sub

    Private Sub DownloadOneMod(projectId As String, title As String, gv As String, loader As String,
                              OnLog As Action(Of String))
        Try
            Dim versions As List(Of ResourceVersion) = ResourceVersion.FromProjectId(projectId, ResourcePlatforms.Modrinth, LoadDependencies:=False)
            If versions Is Nothing OrElse Not versions.Any() Then
                Report(OnLog, $"「{title}」没有可用版本，已跳过")
                Return
            End If
            Dim pick As ResourceVersion = PickVersion(versions, gv, loader)
            If pick Is Nothing Then
                Report(OnLog, $"「{title}」没有兼容版本，已跳过")
                Return
            End If
            Dim inst As McInstance = If(McInstanceSelected, FirstInstance())
            If inst Is Nothing Then
                Report(OnLog, "没有可用游戏实例，无法确定安装位置")
                Return
            End If
            Dim folder As String = IO.Path.Combine(inst.PathIndie, "mods")
            Try : IO.Directory.CreateDirectory(folder) : Catch : End Try
            Dim file As String = IO.Path.Combine(folder, pick.FileName)
            Dim loaders As New List(Of LoaderBase)
            loaders.Add(New LoaderDownload(pick.Display,
                        {pick.ToNetFile(file, ResourceVersion.DownloadReason.Standalone, gv, pick.ModLoaders)}) With {.ProgressWeight = 6, .Block = True})
            Try
                Dim deps As List(Of ResourceVersion) = ModXflDep.CollectMissing(pick, ResourcePlatforms.Modrinth, folder)
                For Each d As ResourceVersion In deps
                    Dim dp As String = IO.Path.Combine(folder, d.FileName)
                    If IO.File.Exists(dp) Then Continue For
                    loaders.Add(New LoaderDownload($"前置：{d.Display}",
                                {d.ToNetFile(dp, ResourceVersion.DownloadReason.Dependency, gv, pick.ModLoaders)}) With {.ProgressWeight = 3, .Block = True})
                Next
            Catch ex As Exception
                Logger.Warn(ex, "[XFL][AI执行] 补前置失败")
            End Try
            Report(OnLog, $"正在下载：{pick.Display}（{pick.FileName}）→ {inst.Name}\mods")
            RunInUi(Sub()
                        Try
                            Dim combo As New LoaderCombo(Of Integer)($"AI 下载：{If(title = "", pick.Display, title)}", loaders)
                            combo.Start(1)
                            LoaderTaskbarAdd(combo)
                            FrmMain.BtnExtraDownload.ShowRefresh()
                            FrmMain.BtnExtraDownload.Ribble()
                        Catch ex As Exception
                            Logger.Warn(ex, "[XFL][AI执行] 启动下载失败")
                        End Try
                    End Sub)
        Catch ex As Exception
            Logger.Warn(ex, "[XFL][AI执行] 下载模组失败")
            Report(OnLog, $"「{title}」下载失败：{ex.Message}")
        End Try
    End Sub

    Friend Function ShowModPicker(title As String, hint As String,
                                   cands As List(Of ModXflModInfo),
                                   Optional autoSelectAll As Boolean = False,
                                   Optional autoConfirm As Boolean = False) As List(Of String)
        Dim Result As New List(Of String)
        Try
            If cands Is Nothing OrElse cands.Count = 0 Then Return Result

            ModXflAiExec._pickerPicked = New List(Of String)
            ModXflAiExec._pickerPickedFlag = False
            Dim DlgReady As New System.Threading.ManualResetEventSlim(False)

            RunInUi(Sub()
                        Try
                            If FrmMain IsNot Nothing Then FrmMain.DragStop()
                            Dim Items As New List(Of MyListItem)
                            For Each c In cands
                                Try

                                    Dim shown As String = c.Title
                                    If Not String.IsNullOrWhiteSpace(c.CnName) Then
                                        shown = c.CnName
                                        If Not shown.Equals(c.Title, StringComparison.OrdinalIgnoreCase) AndAlso
                                           Not shown.Contains(c.Title) Then
                                            shown &= " (" & c.Title & ")"
                                        End If
                                    End If
                                    Dim it As New MyListItem With {
                                        .Title = shown,
                                        .Info = If(String.IsNullOrWhiteSpace(c.Description), "", Left(c.Description, 60)),
                                        .Type = MyListItem.CheckType.CheckBox,
                                        .Height = 40,
                                        .MinHeight = 34,
                                        .Tag = c.ProjectId}
                                    Items.Add(it)
                                Catch ex As Exception
                                    Logger.Warn(ex, $"{Tag} 构建候选项失败")
                                End Try
                            Next
                            If Items.Count = 0 Then
                                Logger.Warn($"{Tag} 没有可显示的候选项")
                                Exit Sub
                            End If
                            Dim Dlg As New MyMsgMulti(title, hint, Items, "下载所选", "取消", autoSelectAll, autoConfirm)
                            Dlg.OnConfirm = Sub(picked As List(Of MyListItem))
                                                   For Each it3 In picked
                                                       If it3 IsNot Nothing AndAlso it3.Tag IsNot Nothing Then
                                                           ModXflAiExec._pickerPicked.Add(it3.Tag.ToString)
                                                       End If
                                                   Next
                                                   Logger.Info($"{Tag} 回调收到 {ModXflAiExec._pickerPicked.Count} 个勾选")
                                                   ModXflAiExec._pickerPickedFlag = True
                                               End Sub
                            ModXflAiExec._pickerDialog = Dlg
                            FrmMain.PanMsg.Children.Add(Dlg)
                        Catch ex As Exception
                            Logger.Warn(ex, $"{Tag} 创建勾选弹窗失败")
                        Finally
                            Try : DlgReady.Set() : Catch : End Try
                        End Try
                    End Sub)

            DlgReady.Wait(10000)

            Dim waited As Integer = 0
            While Not ModXflAiExec._pickerPickedFlag AndAlso waited < 300000
                Threading.Thread.Sleep(100)
                waited += 100
            End While
            If Not ModXflAiExec._pickerPickedFlag Then
                Logger.Warn($"{Tag} 勾选等待超时或用户取消")
            Else
                If ModXflAiExec._pickerPicked IsNot Nothing Then Result.AddRange(ModXflAiExec._pickerPicked)
            End If
            Logger.Info($"{Tag} 勾选结果：{Result.Count} 个")
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 多选流程失败")
        End Try
        Return Result
    End Function

    Private Function ShortenSafe(s As String, n As Integer) As String
        Try
            If String.IsNullOrEmpty(s) Then Return ""
            If s.Length <= n Then Return s
            Return s.Substring(0, n) & "…"
        Catch
            Return s
        End Try
    End Function

    Private Function FirstInstance() As McInstance
        Try
            For Each kv As KeyValuePair(Of McInstanceCardType, List(Of McInstance)) In McInstanceList
                If kv.Value IsNot Nothing AndAlso kv.Value.Any() Then Return kv.Value(0)
            Next
        Catch
        End Try
        Return Nothing
    End Function

    Private Function PickVersion(versions As List(Of ResourceVersion), gv As String, loader As String) As ResourceVersion
        Try
            Dim filtered As List(Of ResourceVersion) = versions.Where(
                Function(v As ResourceVersion)
                    Dim ok As Boolean = True
                    If gv <> "" Then ok = v.GameVersions.Any(Function(x As String) x.Equals(gv, StringComparison.OrdinalIgnoreCase))
                    If ok AndAlso loader <> "" AndAlso v.ModLoaders <> ModLoaders.None Then
                        ok = v.ModLoaders.ToString.ToLower.Contains(loader)
                    End If
                    Return ok
                End Function).ToList()
            If Not filtered.Any() Then filtered = versions
            Dim ordered As List(Of ResourceVersion) = filtered _
                .OrderByDescending(Function(v As ResourceVersion) v.ReleaseType = ResourceVersion.ReleaseTypes.Release) _
                .ThenByDescending(Function(v As ResourceVersion) v.ReleaseDate) _
                .ToList()
            Return ordered.FirstOrDefault()
        Catch ex As Exception
            Logger.Warn(ex, "[XFL][AI执行] 选择版本失败")
            Return Nothing
        End Try
    End Function

    Private Sub DoOpenPage(page As String, OnLog As Action(Of String))
        Dim pt As FormMain.PageType
        Select Case page.ToLower
            Case "download", "下载" : pt = FormMain.PageType.Download
            Case "setup", "settings", "设置" : pt = FormMain.PageType.Setup
            Case "login", "登录" : pt = FormMain.PageType.Login
            Case "link", "联机" : pt = FormMain.PageType.Link
            Case "music", "音乐" : pt = FormMain.PageType.Music
            Case "browser", "浏览器" : pt = FormMain.PageType.Browser
            Case Else : pt = FormMain.PageType.Launch
        End Select
        Report(OnLog, $"打开页面：{pt}")
        RunInUi(Sub()
                    Try
                        FrmMain.PageChange(New FormMain.PageStackData With {.Page = pt})
                    Catch ex As Exception
                        Logger.Warn(ex, "[XFL][AI执行] 页面跳转失败")
                    End Try
                End Sub)
    End Sub

    Private Function SafeStr(o As JObject, key As String) As String
        Try
            Dim tok As JToken = o(key)
            If tok Is Nothing Then Return ""
            Return tok.ToString().Trim()
        Catch
            Return ""
        End Try
    End Function

End Module
