
Public Class ModXflModInfo

    Public ProjectId As String

    Public Title As String

    Public Description As String

    Public Categories As List(Of String) = New List(Of String)

    Public GameVersions As List(Of String) = New List(Of String)

    Public Loaders As List(Of String) = New List(Of String)

    Public Downloads As Long = 0

    Public CnName As String = ""

    Public Slug As String = ""
End Class

Public Module ModXflMods

    Friend Const Tag As String = "[XFL][候选]"

    Private Const MaxParallel As Integer = 6

    Private ReadOnly _cache As New Dictionary(Of String, ModXflModInfo)
    Private ReadOnly _cacheLock As New Object()

    Private ReadOnly _throttle As New System.Threading.SemaphoreSlim(MaxParallel, MaxParallel)

    Friend Function GetInfo(projectId As String) As ModXflModInfo
        Try
            If String.IsNullOrWhiteSpace(projectId) Then Return Nothing

            _throttle.Wait()
            SyncLock _cacheLock
                If _cache.ContainsKey(projectId) Then Return _cache(projectId)
            End SyncLock
            Dim rawTxt As String = NetRequestByClientRetry(
                "https://api.modrinth.com/v2/project/" & projectId, RequireJson:=True)
            If String.IsNullOrWhiteSpace(rawTxt) Then Return Nothing
            Dim js As JObject = JObject.Parse(rawTxt)
            Dim info As New ModXflModInfo With {
                .ProjectId = projectId,
                .Title = js("title")?.ToString,
                .Description = js("description")?.ToString}
            Try
                info.Downloads = CLng(js("downloads")?.ToObject(Of Double))
            Catch
            End Try
            Try
                For Each c In CType(js("categories"), JArray)
                    info.Categories.Add(c.ToString)
                Next
            Catch
            End Try
            Try
                For Each g In CType(js("game_versions"), JArray)
                    info.GameVersions.Add(g.ToString)
                Next
            Catch
            End Try
            Try
                For Each l In CType(js("loaders"), JArray)
                    info.Loaders.Add(l.ToString)
                Next
            Catch
            End Try
            SyncLock _cacheLock
                _cache(projectId) = info
            End SyncLock
            Return info
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 获取项目信息失败：{projectId}")
            Return Nothing
        Finally
            Try : _throttle.Release() : Catch : End Try
        End Try
    End Function

    Friend Function GetInfos(projectIds As List(Of String)) As List(Of ModXflModInfo)
        Dim Result As New List(Of ModXflModInfo)
        Try
            If projectIds Is Nothing OrElse projectIds.Count = 0 Then Return Result

            Dim Need As New List(Of String)
            For Each pid As String In projectIds
                If String.IsNullOrWhiteSpace(pid) Then Continue For
                SyncLock _cacheLock
                    If _cache.ContainsKey(pid) Then
                        Result.Add(_cache(pid))
                    Else
                        Need.Add(pid)
                    End If
                End SyncLock
            Next
            If Need.Count = 0 Then Return Result

            Dim Tasks As New List(Of Task(Of ModXflModInfo))
            For Each pid As String In Need
                Tasks.Add(Task.Factory.StartNew(Of ModXflModInfo)(Function() GetInfo(pid)))
            Next
            Try
                Dim GotArr As ModXflModInfo() = Task.WhenAll(Tasks.ToArray()).GetAwaiter().GetResult()
            Dim Got As New List(Of ModXflModInfo)
            For Each g0 In GotArr
                If g0 IsNot Nothing Then Got.Add(g0)
            Next
                For Each g In Got
                    If g IsNot Nothing Then Result.Add(g)
                Next
            Catch ex As Exception
                Logger.Warn(ex, $"{Tag} 并发获取项目信息失败")
            End Try
            Logger.Info($"{Tag} 并发取 {Need.Count} 个项目，命中 {Result.Count} 个（缓存 {projectIds.Count - Need.Count}）")
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 批量获取失败")
        End Try
        Return Result
    End Function

    Friend Function ByCategory(catIds As List(Of String), mcVer As String, loader As String,
                                   limitPerCat As Integer) As List(Of ModXflModInfo)
        Dim Result As New List(Of ModXflModInfo)
        Try
            If catIds Is Nothing OrElse catIds.Count = 0 Then Return Result
            Dim Tasks As New List(Of Task(Of List(Of ModXflModInfo)))
            For Each cid As String In catIds
                If String.IsNullOrWhiteSpace(cid) Then Continue For
                Tasks.Add(Task.Factory.StartNew(Of List(Of ModXflModInfo))(Function() SearchCategory(cid, mcVer, loader, limitPerCat)))
            Next
            If Tasks.Count = 0 Then Return Result
            Dim AllArr As List(Of ModXflModInfo)() = Task.WhenAll(Tasks.ToArray()).GetAwaiter().GetResult()
            Dim Seen As New HashSet(Of String)
            For Each lst In AllArr
                If lst Is Nothing Then Continue For
                For Each info In lst
                    If info Is Nothing Then Continue For
                    If Not Seen.Add(info.ProjectId) Then Continue For
                    If Not Compatible(info, mcVer, loader) Then Continue For
                    Result.Add(info)
                Next
            Next

            Dim Ordered As List(Of ModXflModInfo) = Result.OrderByDescending(
                Function(i As ModXflModInfo) i.Downloads).ToList()
            Logger.Info($"{Tag} 分类 {String.Join(",", catIds)} → {Ordered.Count} 个兼容模组")
            Return Ordered
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 按分类拉取失败")
            Return Result
        End Try
    End Function

    Private Function SearchCategory(catId As String, mcVer As String, loader As String,
                                     limitPerCat As Integer) As List(Of ModXflModInfo)
        Dim R As New List(Of ModXflModInfo)
        Try
            _throttle.Wait()
            Dim facets As New List(Of String)
            facets.Add($"[""categories:{catId}""]")
            facets.Add($"[""project_type:mod""]")
            If Not String.IsNullOrWhiteSpace(mcVer) Then facets.Add($"[""versions:{mcVer}""]")
            If Not String.IsNullOrWhiteSpace(loader) Then facets.Add($"[""categories:{loader}""]")
            Dim url As String = "https://api.modrinth.com/v2/search?limit=" & Math.Max(10, limitPerCat) &
                                "&index=downloads&facets=" & StringUtils.UrlEscape("[" & String.Join(",", facets) & "]")
            Dim rawTxt As String = NetRequestByClientRetry(url, RequireJson:=True)
            If String.IsNullOrWhiteSpace(rawTxt) Then Return R
            Dim js As JObject = JObject.Parse(rawTxt)
            Dim hits As JToken = js("hits")
            If hits Is Nothing OrElse hits.Type <> JTokenType.Array Then Return R
            For Each h In CType(hits, JArray)
                Try
                    Dim ho As JObject = CType(h, JObject)
                    Dim pid As String = ho("project_id")?.ToString
                    If String.IsNullOrWhiteSpace(pid) Then Continue For

                    Dim info As New ModXflModInfo With {
                        .ProjectId = pid,
                        .Slug = ho("slug")?.ToString,
                        .Title = ho("title")?.ToString,
                        .Description = ho("description")?.ToString}
                    Try
                        info.Downloads = CLng(ho("downloads")?.ToObject(Of Double))
                    Catch
                    End Try
                    Try
                        For Each c In CType(ho("categories"), JArray)
                            info.Categories.Add(c.ToString)
                        Next
                    Catch
                    End Try
                    Try
                        For Each g In CType(ho("game_versions"), JArray)
                            info.GameVersions.Add(g.ToString)
                        Next
                    Catch
                    End Try
                    Try
                        For Each l In CType(ho("loaders"), JArray)
                            info.Loaders.Add(l.ToString)
                        Next
                    Catch
                    End Try
                    R.Add(info)
                Catch
                End Try
            Next
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 分类 {catId} 搜索失败")
        Finally
            Try : _throttle.Release() : Catch : End Try
        End Try
        Return R
    End Function

    Friend Function Compatible(info As ModXflModInfo, mcVer As String, loader As String) As Boolean
        Try
            If info Is Nothing Then Return False
            If Not String.IsNullOrWhiteSpace(loader) AndAlso info.Loaders.Any() Then
                If Not info.Loaders.Any(Function(l As String) l.Equals(loader.Trim(), StringComparison.OrdinalIgnoreCase)) Then
                    Return False
                End If
            End If
            If Not String.IsNullOrWhiteSpace(mcVer) AndAlso info.GameVersions.Any() Then
                If Not info.GameVersions.Any(Function(g As String) g.Equals(mcVer.Trim(), StringComparison.OrdinalIgnoreCase)) Then
                    Return False
                End If
            End If
            Return True
        Catch
            Return True
        End Try
    End Function

End Module
