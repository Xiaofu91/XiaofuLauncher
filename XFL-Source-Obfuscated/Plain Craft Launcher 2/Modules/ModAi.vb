
'等一律返回 HTTP 402 FREE_LIMIT（需付费 Key）。模型列表用 FetchModels() 在线获取。

Public Module ModAi

    Public Class AiMessage
        Public Property Role As String
        Public Property Content As String
        Public Sub New(role As String, content As String)
            Me.Role = role
            Me.Content = content
        End Sub
    End Class

    Public Class AiRateLimitException
        Inherits Exception
        Public ReadOnly WaitSeconds As Integer
        Public Sub New(waitSec As Integer, code As String, raw As String)
            MyBase.New($"免费接口限速（{code}）：{raw}")
            Me.WaitSeconds = waitSec
        End Sub
    End Class

    Public Class SearchHit
        Public Property Title As String
        Public Property Url As String
        Public Property Snippet As String
        Public Property Article As String
    End Class

    Public Class AiResponse
        Public Property Content As String
        Public Property Reasoning As String
    End Class

    Public ReadOnly AiHistory As New List(Of AiMessage)

    Public Sub AiClearHistory()
        AiHistory.Clear()
    End Sub

    Public Sub AiSendAsync(UserText As String,
                           OnStatus As Action(Of String),
                           OnThink As Action(Of String),
                           OnReply As Action(Of String),
                           OnError As Action(Of String),
                           Optional OnPartial As Action(Of String) = Nothing)
        AiHistory.Add(New AiMessage("user", UserText))
        RunInNewThread(
            Sub()
                Try

                    Dim searchBlock As String = ""
                    Dim useSearch As Boolean = False
                    Try
                        useSearch = Settings.Get(Of Boolean)("AiWebSearch")
                    Catch
                    End Try
                    If useSearch AndAlso Not NeedsSearch(UserText) Then
                        useSearch = False
                        Logger.Info("[XFL] 本条为寒暄/极短消息，跳过联网搜索")
                    End If
                    If useSearch Then
                        RunInUi(Sub() OnStatus("🔍 正在联网搜索…"))

                        Dim terms As New List(Of String)
                        Dim q As String = BuildSearchQuery(UserText, terms)
                        Dim hits As List(Of SearchHit) = AiWebSearchCached(q, terms)
                        If hits IsNot Nothing AndAlso hits.Count > 0 Then
                            searchBlock = BuildSearchBlock(hits)
                            RunInUi(Sub() OnStatus($"🔍 已找到 {hits.Count} 条相关资料，正在思考…"))
                        Else

                            RunInUi(Sub() OnStatus("🔍 未找到相关资料，将直接作答…"))
                            Logger.Info($"[XFL] 联网搜索未命中相关资料（查询：{Shorten(q, 40)}），本次不注入资料")
                        End If
                    End If

                    Dim resp As AiResponse = Nothing
                    Dim attempt As Integer = 0
                    Do
                        Try

                            Dim useStream As Boolean = True
                            Try
                                useStream = Settings.Get(Of Boolean)("AiStream")
                            Catch
                            End Try
                            If useStream AndAlso OnPartial IsNot Nothing Then
                                Try
                                    resp = AiRequestStream(searchBlock, OnPartial)
                                Catch exS As AiRateLimitException
                                    Throw exS
                                Catch exS As Exception

                                    Logger.Warn(exS, "[XFL] 流式请求失败，回落到非流式")
                                    resp = AiRequest(searchBlock)
                                End Try
                            Else
                                resp = AiRequest(searchBlock)
                            End If
                            Exit Do
                        Catch rl As AiRateLimitException
                            attempt += 1
                            If attempt > 2 Then Throw
                            Dim total As Integer = Math.Max(rl.WaitSeconds, 5) + 2
                            If attempt > 1 Then total += 5
                            For remain As Integer = total To 1 Step -1
                                Dim sec As Integer = remain
                                RunInUi(Sub() OnStatus($"⏳ 免费接口限速（每 {rl.WaitSeconds} 秒 1 次），{sec} 秒后自动重试（第 {attempt + 1} 次）…"))
                                Threading.Thread.Sleep(1000)
                            Next
                            RunInUi(Sub() OnStatus("🔁 正在重试…"))
                        End Try
                    Loop
                    If resp Is Nothing Then Throw New Exception("请求未返回结果")
                    AiHistory.Add(New AiMessage("assistant", resp.Content))
                    If Not String.IsNullOrWhiteSpace(resp.Reasoning) Then
                        RunInUi(Sub() OnThink(resp.Reasoning))
                    End If
                    RunInUi(Sub() OnReply(resp.Content))
                Catch ex As Exception

                    Try
                        If AiHistory.Count > 0 AndAlso AiHistory(AiHistory.Count - 1).Role = "user" Then
                            AiHistory.RemoveAt(AiHistory.Count - 1)
                        End If
                    Catch
                    End Try
                    Dim msg As String = ex.Message
                    If msg Is Nothing Then msg = ex.GetType().Name
                    RunInUi(Sub() OnError(msg))
                End Try
            End Sub)
    End Sub

#Region "模型请求"

    Public Function FetchModels() As List(Of String)
        Dim result As New List(Of String)
        Try
            Dim modelsUrl As String = DeriveModelsUrl(Settings.Get(Of String)("AiApiUrl"))
            If String.IsNullOrWhiteSpace(modelsUrl) Then Return result
            Dim handler As New Net.Http.HttpClientHandler With {.UseProxy = True, .AutomaticDecompression = Net.DecompressionMethods.GZip}
            Using client As New Net.Http.HttpClient(handler)
                client.Timeout = TimeSpan.FromSeconds(20)
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "XiaofuLauncher/2.13")
                Dim req As New Net.Http.HttpRequestMessage(Net.Http.HttpMethod.Get, modelsUrl)
                Dim key As String = Settings.Get(Of String)("AiApiKey")
                If Not String.IsNullOrWhiteSpace(key) Then
                    req.Headers.TryAddWithoutValidation("Authorization", "Bearer " & key.Trim())
                End If
                Dim resp As Net.Http.HttpResponseMessage = client.SendAsync(req).GetAwaiter().GetResult()
                Dim text As String = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                If Not resp.IsSuccessStatusCode Then Return result
                Dim arr As JArray = JArray.Parse(text)
                For Each item As JToken In arr
                    If item.Type = JTokenType.String Then
                        result.Add(item.ToString())
                    Else
                        Dim name As JToken = item("name")
                        If name Is Nothing Then name = item("id")
                        If name IsNot Nothing Then result.Add(name.ToString())
                    End If
                Next
            End Using
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 获取模型列表失败")
        End Try
        Return result
    End Function

    Private Function DeriveModelsUrl(apiUrl As String) As String
        If String.IsNullOrWhiteSpace(apiUrl) Then Return ""
        Try
            Dim u As String = apiUrl.TrimEnd("/"c)
            For Each tail As String In New String() {"/chat/completions", "/completions", "/openai"}
                If u.EndsWith(tail, StringComparison.OrdinalIgnoreCase) Then
                    u = u.Substring(0, u.Length - tail.Length)
                    Exit For
                End If
            Next
            If u.Contains("/") AndAlso Not u.EndsWith("/models", StringComparison.OrdinalIgnoreCase) Then
                Dim idx As Integer = u.LastIndexOf("/"c)
                If idx > 8 Then u = u.Substring(0, idx)
            End If
            Return u.TrimEnd("/"c) & "/models"
        Catch
            Return ""
        End Try
    End Function

    Private Function AiRequestStream(searchBlock As String, OnPartial As Action(Of String)) As AiResponse
        Dim url As String = Settings.Get(Of String)("AiApiUrl")
        If String.IsNullOrWhiteSpace(url) Then Throw New Exception("未配置 AI 接口地址，请在 设置 → 其他 → AI 对话 中填写")
        Dim key As String = Settings.Get(Of String)("AiApiKey")
        Dim model As String = Settings.Get(Of String)("AiModel")
        If String.IsNullOrWhiteSpace(model) Then model = "openai-fast"

        Dim body As JObject = BuildRequestBody(searchBlock, model, True)
        Dim handler As New Net.Http.HttpClientHandler With {.UseProxy = True, .AutomaticDecompression = Net.DecompressionMethods.GZip}
        Using client As New Net.Http.HttpClient(handler)
            client.Timeout = TimeSpan.FromSeconds(180)
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "XiaofuLauncher/2.13")
            Dim req As New Net.Http.HttpRequestMessage(Net.Http.HttpMethod.Post, url)
            req.Content = New Net.Http.StringContent(body.ToString(Newtonsoft.Json.Formatting.None), System.Text.Encoding.UTF8, "application/json")
            If Not String.IsNullOrWhiteSpace(key) Then
                req.Headers.TryAddWithoutValidation("Authorization", "Bearer " & key.Trim())
            End If
            Dim resp As Net.Http.HttpResponseMessage =
                client.SendAsync(req, Net.Http.HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult()
            If Not resp.IsSuccessStatusCode Then
                Dim errText As String = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                ThrowBuildHttpError(resp, errText)
            End If

            Dim sbContent As New Text.StringBuilder
            Dim sbReason As New Text.StringBuilder
            Dim sw As Stopwatch = System.Diagnostics.Stopwatch.StartNew()
            Dim firstLogged As Boolean = False
            Using stream As IO.Stream = resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult()
                Using reader As New IO.StreamReader(stream, System.Text.Encoding.UTF8)
                    Dim lastPush As Integer = Environment.TickCount
                    While Not reader.EndOfStream
                        Dim line As String = reader.ReadLine()
                        If line Is Nothing Then Exit While
                        line = line.Trim()
                        If line.Length = 0 OrElse Not line.StartsWith("data:") Then Continue While
                        Dim payload As String = line.Substring(5).Trim()
                        If payload = "[DONE]" Then Exit While
                        Try
                            Dim j As JObject = JObject.Parse(payload)
                            Dim delta As JToken = j("choices")(0)?("delta")
                            If delta IsNot Nothing Then
                                Dim rc As String = If(delta("reasoning_content")?.ToString(),
                                                      If(delta("reasoning")?.ToString(), ""))
                                If rc.Length > 0 Then sbReason.Append(rc)
                                Dim ct As String = delta("content")?.ToString()
                                If Not String.IsNullOrEmpty(ct) Then
                                    sbContent.Append(ct)

                                    If Not firstLogged Then
                                        firstLogged = True
                                        Logger.Info($"[XFL] 流式首字到达：{sw.ElapsedMilliseconds}ms")
                                    End If

                                    If OnPartial IsNot Nothing AndAlso Environment.TickCount - lastPush >= 120 Then
                                        lastPush = Environment.TickCount
                                        Dim snap As String = sbContent.ToString()
                                        RunInUi(Sub() OnPartial(snap))
                                    End If
                                End If
                            End If
                        Catch

                        End Try
                    End While
                End Using
            End Using
            Dim final As String = sbContent.ToString()
            If OnPartial IsNot Nothing AndAlso final.Length > 0 Then
                Dim snapF As String = final
                RunInUi(Sub() OnPartial(snapF))
            End If
            Logger.Info($"[XFL] 流式完成：总耗时 {sw.ElapsedMilliseconds}ms，正文 {final.Length} 字，思考 {sbReason.Length} 字")
            If final.Length = 0 AndAlso sbReason.Length = 0 Then Throw New Exception("接口返回内容为空")
            Return New AiResponse With {.Content = final, .Reasoning = sbReason.ToString()}
        End Using
    End Function

    Friend Function BuildRequestBody(searchBlock As String, model As String, stream As Boolean) As JObject
        Dim msgs As New JArray
        Dim sysPrompt As String = Settings.Get(Of String)("AiSystemPrompt")
        If Not String.IsNullOrWhiteSpace(sysPrompt) Then
            msgs.Add(New JObject From {{"role", "system"}, {"content", sysPrompt}})
        End If

        msgs.Add(New JObject From {{"role", "system"}, {"content", AnswerStylePrompt()}})

        Try
            Dim verHint As String = ModXflAiExec.VersionsHint()
            If Not String.IsNullOrWhiteSpace(verHint) Then
                msgs.Add(New JObject From {{"role", "system"}, {"content", verHint}})
            End If
        Catch
        End Try

        Try
            Dim tagHint As String = ModXflCatalog.TagsHint()
            If Not String.IsNullOrWhiteSpace(tagHint) Then
                msgs.Add(New JObject From {{"role", "system"}, {"content", tagHint}})
            End If
        Catch
        End Try

        Try
            Dim catHint As String = ModXflCats.CatsHint()
            If Not String.IsNullOrWhiteSpace(catHint) Then
                msgs.Add(New JObject From {{"role", "system"}, {"content", catHint}})
            End If
        Catch
        End Try
        Try
            If ModXflAiExec.Enabled Then
                msgs.Add(New JObject From {{"role", "system"}, {"content", ModXflAiExec.ProtocolPrompt()}})
            End If
        Catch
        End Try
        If Not String.IsNullOrWhiteSpace(searchBlock) Then
            msgs.Add(New JObject From {{"role", "system"}, {"content", searchBlock}})
        End If
        For Each m As AiMessage In AiHistory
            msgs.Add(New JObject From {{"role", m.Role}, {"content", m.Content}})
        Next
        Dim temp As Double = 0.3
        Try
            Dim t As Double = Settings.Get(Of Double)("AiTemperature")
            If t > 0 Then temp = t
        Catch
        End Try
        Return New JObject From {
            {"model", model},
            {"messages", msgs},
            {"stream", stream},
            {"temperature", temp}}
    End Function

    Private Function AnswerStylePrompt() As String
        Return "回答要求：" & vbCrLf &
               "1) 直接回答用户的问题，不要复述提问、不要寒暄铺垫；" & vbCrLf &
               "2) 用中文，条理清晰：要点用「· 」分点，或分成 2–3 个短段落；" & vbCrLf &
               "3) 尽量给出有用信息：先说你确定的内容（原理、做法、思路、相关常识等），" & vbCrLf &
               "   只有在关键事实（版本号、日期、价格、链接等）没把握时，才在该处简短标注「（此点不确定）」，" & vbCrLf &
               "   绝对不要只回一句「不确定」就把问题打回去，也不要编造具体细节；" & vbCrLf &
               "4) 若提供了联网资料且确实相关，可引用并用【来源N】标注；若与问题无关，直接忽略它们；" & vbCrLf &
               "5) 篇幅：简单问题 1–3 句，需要展开的问题 100–400 字，不要啰嗦重复。"
    End Function

    Private Sub ThrowBuildHttpError(resp As Net.Http.HttpResponseMessage, respText As String)
        Dim waitSec As Integer = 0
        Dim code As String = ""
        Dim detail As String = Shorten(respText, 300)
        Try
            Dim ej As JObject = JObject.Parse(respText)
            code = If(ej("error")?("code")?.ToString(), "")
            Dim emsg As String = If(ej("error")?("message")?.ToString(), "")
            If String.IsNullOrWhiteSpace(code) Then code = If(ej("code")?.ToString(), "")
            Dim probe As String = emsg & " " & detail
            For Each rule As String In New String() {"(\d+)\s*second", "retry\s*after\s*(\d+)\s*s"}
                Dim mm As System.Text.RegularExpressions.Match =
                    System.Text.RegularExpressions.Regex.Match(probe, rule, System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                If mm.Success Then waitSec = Math.Max(waitSec, CInt(mm.Groups(1).Value))
            Next
        Catch
        End Try
        If waitSec > 0 OrElse code.ToUpper.Contains("FREE_LIMIT") OrElse CInt(resp.StatusCode) = 429 Then
            If waitSec <= 0 Then waitSec = 45
            Throw New AiRateLimitException(waitSec, code, detail)
        End If
        If CInt(resp.StatusCode) = 404 Then
            Throw New Exception("该模型在当前接口不可用（404）。请点「设置 → 其他 → AI 对话 → 获取模型列表」选用免费可用模型（如 openai-fast）。")
        End If
        Throw New Exception($"接口返回 HTTP {CInt(resp.StatusCode)}：{detail}")
    End Sub

    Friend Function AiTranslateRaw(Prompt As String, Optional TimeoutSec As Integer = 60) As String
        Dim url As String = Settings.Get(Of String)("AiApiUrl")
        If String.IsNullOrWhiteSpace(url) Then Throw New Exception("未配置 AI 接口地址")
        Dim key As String = Settings.Get(Of String)("AiApiKey")
        Dim model As String = Settings.Get(Of String)("AiModel")
        If String.IsNullOrWhiteSpace(model) Then model = "openai-fast"

        Dim msgs As New JArray
        msgs.Add(New JObject From {{"role", "system"}, {"content", "你是一个翻译与格式化助手。只输出被要求的内容，不要任何解释、前后缀或代码块标记。"}})
        msgs.Add(New JObject From {{"role", "user"}, {"content", Prompt}})
        Dim body As New JObject From {
            {"model", model},
            {"messages", msgs},
            {"stream", False},
            {"temperature", 0.1}}

        Dim handler As New Net.Http.HttpClientHandler With {.UseProxy = True, .AutomaticDecompression = Net.DecompressionMethods.GZip}
        Using client As New Net.Http.HttpClient(handler)
            client.Timeout = TimeSpan.FromSeconds(TimeoutSec)
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "XiaofuLauncher/2.13")
            Dim req As New Net.Http.HttpRequestMessage(Net.Http.HttpMethod.Post, url)
            req.Content = New Net.Http.StringContent(body.ToString(Newtonsoft.Json.Formatting.None), System.Text.Encoding.UTF8, "application/json")
            If Not String.IsNullOrWhiteSpace(key) Then
                req.Headers.TryAddWithoutValidation("Authorization", "Bearer " & key.Trim())
            End If
            Dim resp As Net.Http.HttpResponseMessage = client.SendAsync(req).GetAwaiter().GetResult()
            Dim respText As String = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            If Not resp.IsSuccessStatusCode Then
                Throw New Exception($"翻译接口返回 HTTP {CInt(resp.StatusCode)}：{Shorten(respText, 120)}")
            End If
            If String.IsNullOrWhiteSpace(respText) Then Throw New Exception("翻译接口返回内容为空")
            Dim parsed As AiResponse = ParseOpenAi(respText)
            If String.IsNullOrWhiteSpace(parsed.Content) AndAlso Not respText.TrimStart().StartsWith("{") Then
                parsed.Content = respText.Trim()
            End If
            Return If(parsed.Content, "").Trim()
        End Using
    End Function

    Private Function AiRequest(searchBlock As String) As AiResponse
        Dim url As String = Settings.Get(Of String)("AiApiUrl")
        If String.IsNullOrWhiteSpace(url) Then Throw New Exception("未配置 AI 接口地址，请在 设置 → 其他 → AI 对话 中填写")
        Dim key As String = Settings.Get(Of String)("AiApiKey")
        Dim model As String = Settings.Get(Of String)("AiModel")
        If String.IsNullOrWhiteSpace(model) Then model = "openai-fast"

        Dim body As JObject = BuildRequestBody(searchBlock, model, False)

        Dim handler As New Net.Http.HttpClientHandler With {.UseProxy = True, .AutomaticDecompression = Net.DecompressionMethods.GZip}
        Using client As New Net.Http.HttpClient(handler)
            client.Timeout = TimeSpan.FromSeconds(120)
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "XiaofuLauncher/2.13")
            Dim req As New Net.Http.HttpRequestMessage(Net.Http.HttpMethod.Post, url)
            req.Content = New Net.Http.StringContent(body.ToString(Newtonsoft.Json.Formatting.None), System.Text.Encoding.UTF8, "application/json")
            If Not String.IsNullOrWhiteSpace(key) Then
                req.Headers.TryAddWithoutValidation("Authorization", "Bearer " & key.Trim())
            End If

            Dim resp As Net.Http.HttpResponseMessage = client.SendAsync(req).GetAwaiter().GetResult()
            Dim respText As String = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            If Not resp.IsSuccessStatusCode Then
                ThrowBuildHttpError(resp, respText)
            End If
            If String.IsNullOrWhiteSpace(respText) Then Throw New Exception("接口返回内容为空")

            Dim parsed As AiResponse = ParseOpenAi(respText)
            If String.IsNullOrWhiteSpace(parsed.Content) Then

                If Not respText.TrimStart().StartsWith("{") Then
                    parsed.Content = respText.Trim()
                    Return parsed
                End If
                Throw New Exception("接口返回格式无法识别：" & Shorten(respText, 200))
            End If
            parsed.Content = parsed.Content.Trim()
            Return parsed
        End Using
    End Function

    Private Function ParseOpenAi(text As String) As AiResponse
        Dim r As New AiResponse
        Try
            Dim j As JObject = JObject.Parse(text)
            Dim msg As JToken = j("choices")?.First?("message")
            If msg Is Nothing Then
                Dim alt As JToken = j("message")?("content")
                If alt IsNot Nothing Then r.Content = alt.ToString()
                Return r
            End If
            r.Content = ExtractText(msg("content"))
            r.Reasoning = ExtractText(msg("reasoning"))
            If String.IsNullOrWhiteSpace(r.Reasoning) Then r.Reasoning = ExtractText(msg("reasoning_content"))
            If String.IsNullOrWhiteSpace(r.Reasoning) Then r.Reasoning = ExtractText(msg("thinking"))
        Catch
        End Try
        Return r
    End Function

    Private Function ExtractText(node As JToken) As String
        If node Is Nothing Then Return ""
        If node.Type = JTokenType.Array Then
            Dim sb As New Text.StringBuilder
            For Each part As JToken In node
                Dim t As JToken = part("text")
                If t Is Nothing Then t = part("content")
                If t IsNot Nothing Then sb.Append(t.ToString())
            Next
            Return sb.ToString()
        End If
        Return node.ToString()
    End Function

#End Region

#Region "联网搜索（自实现，抓取 Bing 结果页）"

    Private Class SearchCacheEntry
        Public Hits As List(Of SearchHit)
        Public Stamped As Integer
    End Class

    Private ReadOnly SearchCache As New Dictionary(Of String, SearchCacheEntry)
    Private ReadOnly SearchCacheLock As New Object
    Private Const SearchCacheMs As Integer = 600000

    Friend Function NeedsSearch(text As String) As Boolean
        Dim s As String = If(text, "").Trim()
        If s.Length = 0 Then Return False
        Dim plain As String = s.Replace("！", "").Replace("!", "").Replace("。", "").Replace("？", "").Replace("?", "").Replace("～", "").Replace("~", "").Trim()
        For Each g As String In {"你好", "您好", "谢谢", "多谢", "再见", "早上好", "中午好", "晚上好", "哈喽", "在吗", "测试", "你是谁", "你能做什么", "hi", "hello", "ok", "好的"}
            If String.Equals(plain, g, StringComparison.OrdinalIgnoreCase) Then Return False
        Next
        If s.Length <= 3 Then Return False
        Return True
    End Function

    Private Function FetchArticle(url As String, maxChars As Integer) As String
        Try
            If String.IsNullOrWhiteSpace(url) Then Return ""
            If Not (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) OrElse
                    url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) Then Return ""
            Dim handler As New Net.Http.HttpClientHandler With {.UseProxy = True, .AutomaticDecompression = Net.DecompressionMethods.GZip}
            Using client As New Net.Http.HttpClient(handler)
                client.Timeout = TimeSpan.FromSeconds(5)
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36")
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8")
                Dim html As String = client.GetStringAsync(url).GetAwaiter().GetResult()
                If String.IsNullOrWhiteSpace(html) Then Return ""
                Dim body As String = html

                body = System.Text.RegularExpressions.Regex.Replace(body, "<script[\s\S]*?</script>", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                body = System.Text.RegularExpressions.Regex.Replace(body, "<style[\s\S]*?</style>", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                body = System.Text.RegularExpressions.Regex.Replace(body, "<!--[\s\S]*?-->", " ")
                body = System.Text.RegularExpressions.Regex.Replace(body, "<(nav|header|footer|aside)[\s\S]*?</\1>", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                body = CleanHtml(body)

                If body.Length > maxChars Then body = body.Substring(0, maxChars)
                Return body.Trim()
            End Using
        Catch ex As Exception

            Logger.Info($"[XFL] 正文抓取失败（{ex.GetType().Name}）：{Shorten(url, 50)}")
            Return ""
        End Try
    End Function

    Friend Function AiWebSearchCached(query As String, terms As List(Of String)) As List(Of SearchHit)
        Dim key As String = query.Trim().ToLowerInvariant()
        SyncLock SearchCacheLock
            Dim ce As SearchCacheEntry = Nothing
            If SearchCache.TryGetValue(key, ce) AndAlso ce IsNot Nothing Then
                If Environment.TickCount - ce.Stamped < SearchCacheMs AndAlso ce.Stamped <= Environment.TickCount Then
                    Logger.Info($"[XFL] 搜索命中缓存（{ce.Hits.Count} 条）：{Shorten(key, 24)}")
                    Return ce.Hits
                End If
                SearchCache.Remove(key)
            End If
        End SyncLock
        Dim hits As List(Of SearchHit) = AiWebSearch(query, terms)

        Dim got As Integer = 0
        Dim fetchSw As Stopwatch = System.Diagnostics.Stopwatch.StartNew()
        For idx As Integer = 0 To Math.Min(2, hits.Count - 1)
            If fetchSw.ElapsedMilliseconds > 6000 Then Exit For
            Dim art As String = FetchArticle(hits(idx).Url, 1200)
            If art.Length > 200 Then
                hits(idx).Article = art
                got += 1
                Logger.Info($"[XFL] 已抓取来源{idx + 1} 网页正文 {art.Length} 字：{Shorten(hits(idx).Title, 30)}")
                Exit For
            Else
                Logger.Info($"[XFL] 来源{idx + 1} 正文过短（{art.Length} 字），改试下一条")
            End If
        Next
        If got = 0 Then Logger.Info("[XFL] 所有候选网页均未抓到有效正文，仅用搜索摘要")
        SyncLock SearchCacheLock
            SearchCache(key) = New SearchCacheEntry With {.Hits = hits, .Stamped = Environment.TickCount}

            If SearchCache.Count > 40 Then SearchCache.Clear()
        End SyncLock
        Return hits

    End Function

    Public Function AiWebSearch(query As String, Optional terms As List(Of String) = Nothing) As List(Of SearchHit)
        Dim hits As New List(Of SearchHit)
        If String.IsNullOrWhiteSpace(query) Then Return hits
        Dim rawCount As Integer = 0
        Try
            Dim handler As New Net.Http.HttpClientHandler With {.UseProxy = True, .AutomaticDecompression = Net.DecompressionMethods.GZip}
            Using client As New Net.Http.HttpClient(handler)
                client.Timeout = TimeSpan.FromSeconds(20)
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36")
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8")
                Dim url As String = "https://www.bing.com/search?setlang=zh-CN&q=" & Uri.EscapeDataString(query)
                Dim html As String = client.GetStringAsync(url).GetAwaiter().GetResult()
                If String.IsNullOrWhiteSpace(html) Then Return hits

                Dim blocks As System.Text.RegularExpressions.MatchCollection = System.Text.RegularExpressions.Regex.Matches(html, "<li class=""b_algo"".*?</li>", System.Text.RegularExpressions.RegexOptions.Singleline)
                Dim seen As New HashSet(Of String)
                For Each b As System.Text.RegularExpressions.Match In blocks
                    If hits.Count >= 6 Then Exit For
                    Dim seg As String = b.Value

                    Dim mh As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(seg, "<h2[^>]*>\s*<a[^>]*href=""([^""]+)""[^>]*>(.*?)</a>", System.Text.RegularExpressions.RegexOptions.Singleline)
                    If Not mh.Success Then Continue For
                    Dim href As String = mh.Groups(1).Value
                    Dim title As String = CleanHtml(mh.Groups(2).Value)
                    If String.IsNullOrWhiteSpace(title) Then Continue For

                    Dim snippet As String = ""
                    Dim paras As New List(Of String)
                    For Each mp As System.Text.RegularExpressions.Match In
                        System.Text.RegularExpressions.Regex.Matches(seg, "<p[^>]*>(.*?)</p>", System.Text.RegularExpressions.RegexOptions.Singleline)
                        Dim one As String = CleanHtml(mp.Groups(1).Value)
                        If one.Length >= 12 AndAlso Not paras.Contains(one) Then paras.Add(one)
                        If paras.Count >= 3 Then Exit For
                    Next
                    If paras.Count > 0 Then snippet = String.Join(" ", paras.ToArray())
                    rawCount += 1

                    If Not IsRelevantHit(title, snippet, terms) Then Continue For

                    Dim host As String = ""
                    Try
                        host = New Uri(href).Host
                    Catch
                    End Try
                    Dim dkey As String = Shorten(title, 24) & "|" & host
                    If Not seen.Add(dkey) Then Continue For
                    hits.Add(New SearchHit With {.Title = title, .Url = href, .Snippet = snippet})
                Next
                Logger.Info($"[XFL] 联网搜索「{Shorten(query, 30)}」命中 {rawCount} 条，其中相关 {hits.Count} 条")
            End Using
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 联网搜索失败")
        End Try
        Return hits
    End Function

    Private ReadOnly SearchJunk As String() = {
        "请问", "请帮我", "帮我", "麻烦", "能不能", "可不可以", "可以吗", "有没有", "是否",
        "是多少", "是什么", "怎么样", "怎么办", "如何", "为什么", "哪些", "哪个", "哪里",
        "告诉我", "我想知道", "我想", "我知道", "现在", "目前", "最新的", "最新", "最近的", "最近",
        "已经", "一下", "谢谢", "的话", "了吗", "多少", "什么"}

    Friend Function BuildSearchQuery(userText As String, ByRef terms As List(Of String)) As String
        Dim raw As String = If(userText, "").Trim()
        terms = New List(Of String)
        If raw.Length = 0 Then Return ""
        Dim cleaned As String = raw

        For Each junk As String In SearchJunk
            cleaned = cleaned.Replace(junk, " ")
        Next

        Dim seps As Char() = (" " & ControlChars.Tab & ControlChars.Cr & ControlChars.Lf &
                              "，,。？?！!、；;：:（）()“”/").ToCharArray()
        Dim parts As String() = cleaned.Split(seps, StringSplitOptions.RemoveEmptyEntries)
        Dim sb As New Text.StringBuilder
        For Each part As String In parts
            Dim seg As String = part.Trim()
            If seg.Length = 0 Then Continue For

            If seg.Length < 2 Then Continue For
            terms.Add(seg)
            If sb.Length > 0 Then sb.Append(" "c)
            sb.Append(seg)
            If sb.Length >= 48 Then Exit For
        Next
        Dim q As String = sb.ToString().Trim()

        Dim ordered As New List(Of String)
        For Each tt As String In terms
            If HasLatin(tt) Then ordered.Add(tt)
        Next
        For Each tt As String In terms
            If Not HasLatin(tt) Then ordered.Add(tt)
        Next
        If ordered.Count > 0 Then q = String.Join(" ", ordered.ToArray())

        If q.Length < 2 Then
            q = Shorten(raw, 40)
            terms.Clear()
            terms.Add(q)
        End If
        Return q
    End Function

    Private Function IsRelevantHit(title As String, snippet As String, terms As List(Of String)) As Boolean
        If terms Is Nothing OrElse terms.Count = 0 Then Return True
        Dim blob As String = (If(title, "") & " " & If(snippet, "")).ToLowerInvariant()

        Dim strong As New List(Of String)
        Dim cn As New List(Of String)
        Dim weak As New List(Of String)
        For Each t As String In terms
            Dim k As String = If(t, "").Trim()
            If k.Length < 2 Then Continue For
            If HasLatin(k) Then
                strong.Add(k)
            ElseIf IsNumericLike(k) Then
                weak.Add(k)
            Else
                cn.Add(k)
            End If
        Next
        Dim keys As List(Of String) = If(strong.Count > 0, strong, If(cn.Count > 0, cn, weak))
        For Each t As String In keys
            Dim key As String = t.ToLowerInvariant()
            If blob.Contains(key) Then Return True

            Dim syn As String() = Nothing
            If SearchSynonyms.TryGetValue(key, syn) Then
                For Each s As String In syn
                    If blob.Contains(s) Then Return True
                Next
            End If
        Next

        If strong.Count = 0 AndAlso cn.Count > 0 AndAlso weak.Count > 0 Then
            Dim cnHit As Boolean = cn.Any(Function(x) blob.Contains(x.ToLowerInvariant()))
            Dim wkHit As Boolean = weak.Any(Function(x) blob.Contains(System.Text.RegularExpressions.Regex.Match(x, "\d{2,}").Value))
            If cnHit AndAlso wkHit Then Return True
        End If
        Return False
    End Function

    Private Function IsNumericLike(s As String) As Boolean
        Dim hasDigit As Boolean = False
        For Each ch As Char In If(s, "")
            If ch >= "0"c AndAlso ch <= "9"c Then
                hasDigit = True
            ElseIf ch = "年"c OrElse ch = "月"c OrElse ch = "日"c OrElse ch = "."c OrElse ch = "-"c OrElse ch = "/"c Then
                Continue For
            Else
                Return False
            End If
        Next
        Return hasDigit
    End Function

    Private ReadOnly SearchSynonyms As New Dictionary(Of String, String()) From {
        {"minecraft", New String() {"我的世界", "mc", "minecraft"}},
        {"我的世界", New String() {"minecraft", "mc"}},
        {"mc", New String() {"minecraft", "我的世界"}},
        {"java", New String() {"java版", "java 版"}},
        {"bedrock", New String() {"基岩", "基岩版"}},
        {"基岩", New String() {"bedrock"}},
        {"optifine", New String() {"高清修复", "of"}},
        {"fabric", New String() {"fabric", "fabricmc"}},
        {"forge", New String() {"forge", "neoforge"}},
        {"mod", New String() {"mod", "模组", "插件"}},
        {"mods", New String() {"mod", "模组"}},
        {"模组", New String() {"mod", "mods"}},
        {"光影", New String() {"shader", "shaders"}},
        {"shader", New String() {"光影", "shader"}},
        {"启动器", New String() {"launcher"}},
        {"launcher", New String() {"启动器"}},
        {"版本", New String() {"version"}},
        {"version", New String() {"版本"}},
        {"登录", New String() {"login", "sign in"}},
        {"下载", New String() {"download"}},
        {"download", New String() {"下载"}}}

    Private Function HasLatin(s As String) As Boolean
        For Each ch As Char In If(s, "")
            If (ch >= "a"c AndAlso ch <= "z"c) OrElse (ch >= "A"c AndAlso ch <= "Z"c) Then Return True
        Next
        Return False
    End Function

    Private Function HasLatinOrDigit(s As String) As Boolean
        For Each ch As Char In If(s, "")
            If (ch >= "a"c AndAlso ch <= "z"c) OrElse (ch >= "A"c AndAlso ch <= "Z"c) OrElse (ch >= "0"c AndAlso ch <= "9"c) Then Return True
        Next
        Return False
    End Function

    Private Function BuildSearchBlock(hits As List(Of SearchHit)) As String
        Dim sb As New Text.StringBuilder
        sb.AppendLine("下面是刚刚联网搜索到的网页摘要，仅供参考：")
        sb.AppendLine("· 只有在确实与用户问题相关时才引用，并用【来源N】标注；")
        sb.AppendLine("· 如果这些资料与问题无关或不足以回答，请直接忽略它们，用你自己的知识回答；")
        sb.AppendLine("  不要复述、不要提及资料里的无关内容，也不要因为资料存在就改变回答主题；")
        sb.AppendLine("· 不确定的信息要说明「可能不准确」。")
        sb.AppendLine()
        Dim i As Integer = 1
        Dim total As Integer = sb.Length
        For Each h As SearchHit In hits
            If total > 2200 Then Exit For
            Dim one As New Text.StringBuilder
            one.AppendLine($"【来源{i}】{h.Title}")
            If Not String.IsNullOrWhiteSpace(h.Article) Then
                one.AppendLine($"正文：{Shorten(h.Article, 900)}")
            ElseIf Not String.IsNullOrWhiteSpace(h.Snippet) Then
                one.AppendLine($"摘要：{Shorten(h.Snippet, 200)}")
            End If
            one.AppendLine($"链接：{h.Url}")
            one.AppendLine()
            sb.Append(one)
            total += one.Length
            i += 1
        Next
        Return sb.ToString()
    End Function

    Private Function CleanHtml(s As String) As String
        If String.IsNullOrEmpty(s) Then Return ""
        Dim t As String = System.Text.RegularExpressions.Regex.Replace(s, "<[^>]+>", "")
        t = Net.WebUtility.HtmlDecode(t)
        t = System.Text.RegularExpressions.Regex.Replace(t, "\s+", " ")
        Return t.Trim()
    End Function

#End Region

    Private Function Shorten(s As String, n As Integer) As String
        If String.IsNullOrEmpty(s) Then Return ""
        If s.Length <= n Then Return s
        Return s.Substring(0, n) & "…"
    End Function

End Module