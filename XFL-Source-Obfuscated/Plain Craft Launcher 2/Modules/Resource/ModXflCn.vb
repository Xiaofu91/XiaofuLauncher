
Public Module ModXflCn

    Friend Const Tag As String = "[XFL][翻译]"

    Private ReadOnly _localMap As New Dictionary(Of String, String)
    Private _localBuilded As Boolean = False
    Private ReadOnly _localLock As New Object()

    Private ReadOnly _aiCache As New Dictionary(Of String, String)
    Private ReadOnly _aiLock As New Object()

    Friend Sub BuildLocalMap()
        SyncLock _localLock
            If _localBuilded Then Return
            _localBuilded = True
            Try
                Dim sw As New System.Diagnostics.Stopwatch
                sw.Start()
                Dim all = WikiEntry.All.Value
                For Each e In all
                    Try
                        Dim mr As String = e.Slugs.GetOrDefault(ResourcePlatforms.Modrinth)
                        If String.IsNullOrWhiteSpace(mr) Then Continue For
                        Dim cn As String = e.ChineseName
                        If String.IsNullOrWhiteSpace(cn) Then Continue For
                        If cn.Contains("*") Then
                            Dim en As String = e.Slugs.Values.First().Replace("-", " ").Capitalize()
                            cn = cn.Replace("*", " (" & en & ")")
                        End If
                        Dim key As String = mr.Trim.ToLower
                        If Not _localMap.ContainsKey(key) Then _localMap(key) = cn
                    Catch
                    End Try
                Next
                sw.Stop()
                Logger.Info($"{Tag} 本地译名库已载入：{_localMap.Count} 条，耗时 {sw.ElapsedMilliseconds} ms")
            Catch ex As Exception
                Logger.Warn(ex, $"{Tag} 本地译名库载入失败")
            End Try
        End SyncLock
    End Sub

    Friend Function Local(slug As String) As String
        Try
            BuildLocalMap()
            If String.IsNullOrWhiteSpace(slug) Then Return ""
            SyncLock _localLock
                Dim cn As String = Nothing
                If _localMap.TryGetValue(slug.Trim.ToLower, cn) Then Return cn
            End SyncLock
            Return ""
        Catch
            Return ""
        End Try
    End Function

    Friend Function AiCached(slug As String) As String
        Try
            If String.IsNullOrWhiteSpace(slug) Then Return ""
            SyncLock _aiLock
                Dim cn As String = Nothing
                If _aiCache.TryGetValue(slug.Trim.ToLower, cn) Then Return cn
            End SyncLock
            Return ""
        Catch
            Return ""
        End Try
    End Function

    Friend Sub SetAiCached(slug As String, cn As String)
        Try
            If String.IsNullOrWhiteSpace(slug) OrElse String.IsNullOrWhiteSpace(cn) Then Return
            SyncLock _aiLock
                _aiCache(slug.Trim.ToLower) = cn
            End SyncLock
        Catch
        End Try
    End Sub

    Friend Sub EnsureCnNames(cands As List(Of ModXflModInfo))
        Try
            If cands Is Nothing OrElse cands.Count = 0 Then Return
            BuildLocalMap()
            Dim missing As New List(Of ModXflModInfo)
            Dim hitLocal As Integer = 0
            For Each c In cands
                If c Is Nothing Then Continue For

                Dim cn As String = ""
                If Not String.IsNullOrWhiteSpace(c.Slug) Then cn = Local(c.Slug)
                If String.IsNullOrWhiteSpace(cn) Then cn = Local(c.ProjectId)
                If String.IsNullOrWhiteSpace(cn) Then cn = AiCached(c.Slug)
                If String.IsNullOrWhiteSpace(cn) Then cn = AiCached(c.ProjectId)
                If String.IsNullOrWhiteSpace(cn) Then
                    missing.Add(c)
                Else
                    c.CnName = cn
                    hitLocal += 1
                End If
            Next
            If missing.Count = 0 Then
                Logger.Info($"{Tag} 本地译名全部命中（{hitLocal}/{cands.Count}）")
                Return
            End If
            Logger.Info($"{Tag} 本地命中 {hitLocal}/{cands.Count}，需 AI 翻译 {missing.Count} 个")
            FillMissingByAi(missing)
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 补中文名失败")
        End Try
    End Sub

    Private Sub FillMissingByAi(missing As List(Of ModXflModInfo))
        Try
            If missing Is Nothing OrElse missing.Count = 0 Then Return
            Dim lines As New List(Of String)
            For Each m In missing
                Dim t As String = If(m.Title, "").Replace("|", " ").Replace(vbCrLf, " ").Replace(vbLf, " ")
                Dim key As String = If(String.IsNullOrWhiteSpace(m.Slug), m.ProjectId, m.Slug)
                lines.Add(key & "|" & t)
            Next
            Dim prompt As String =
                "你是 Minecraft 模组翻译助手。下面每行是 Modrinth 模组的 slug|英文标题。" & vbCrLf &
                "请为每个模组给出简洁、常用的中文译名（保留必要的英文缩写，如 JEI、FPS）。" & vbCrLf &
                "只输出一个 JSON 对象：键为 slug（原样），值为中文译名。不要输出任何解释。" & vbCrLf &
                String.Join(vbCrLf, lines)
            Dim txt As String = ""
            Try
                txt = ModAi.AiTranslateRaw(prompt, 60)
            Catch ex As Exception
                Logger.Warn(ex, $"{Tag} AI 翻译失败，保留英文原名")
                Return
            End Try
            If String.IsNullOrWhiteSpace(txt) Then Return
            Dim json As JObject = Nothing
            Try
                json = JObject.Parse(txt)
            Catch
                Try
                    Dim i0 As Integer = txt.IndexOf("{")
                    Dim i1 As Integer = txt.LastIndexOf("}")
                    If i0 >= 0 AndAlso i1 > i0 Then json = JObject.Parse(txt.Substring(i0, i1 - i0 + 1))
                Catch
                End Try
            End Try
            If json Is Nothing Then
                Logger.Warn($"{Tag} AI 翻译返回无法解析")
                Return
            End If
            Dim got As Integer = 0
            For Each m In missing
                Try
                    Dim cn As String = ""
                    If Not String.IsNullOrWhiteSpace(m.Slug) Then cn = If(json(m.Slug)?.ToString, "").Trim
                    If cn = "" Then cn = If(json(m.ProjectId)?.ToString, "").Trim
                    If cn = "" Then cn = If(json(m.Title)?.ToString, "").Trim
                    If cn <> "" Then

                        If Not cn.Equals(m.Title, StringComparison.OrdinalIgnoreCase) Then m.CnName = cn
                        If Not String.IsNullOrWhiteSpace(m.Slug) Then SetAiCached(m.Slug, cn)
                        SetAiCached(m.ProjectId, cn)
                        got += 1
                    End If
                Catch
                End Try
            Next
            Logger.Info($"{Tag} AI 翻译完成 {got}/{missing.Count} 个")
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 批量翻译流程失败")
        End Try
    End Sub

End Module
