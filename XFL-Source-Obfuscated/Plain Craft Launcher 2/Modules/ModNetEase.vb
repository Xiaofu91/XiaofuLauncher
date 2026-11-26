
Imports Newtonsoft.Json.Linq

Module ModNetEase

    Public Class MusicHit
        Public Property Id As Long
        Public Property Name As String
        Public Property Artist As String
        Public Property Album As String
        Public Property Duration As Integer
        Public ReadOnly Property Display As String
            Get
                Dim d As String = ""
                If Duration > 0 Then d = "  [" & (Duration \ 60000) & ":" & ((Duration \ 1000) Mod 60).ToString("00") & "]"
                Return $"{Name} - {Artist}{d}"
            End Get
        End Property
    End Class

    Private Sub EnsureTls12()
        Try
            Net.ServicePointManager.SecurityProtocol = Net.ServicePointManager.SecurityProtocol Or Net.SecurityProtocolType.Tls12 Or Net.SecurityProtocolType.Tls11 Or Net.SecurityProtocolType.Tls
        Catch
        End Try
    End Sub

    Private Function MusicHttp() As Net.Http.HttpClient
        EnsureTls12()
        Dim handler As New Net.Http.HttpClientHandler With {
            .UseProxy = True,
            .AutomaticDecompression = Net.DecompressionMethods.GZip Or Net.DecompressionMethods.Deflate}
        Dim client As New Net.Http.HttpClient(handler)
        client.Timeout = TimeSpan.FromSeconds(15)

        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0")
        client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://music.163.com/")
        client.DefaultRequestHeaders.TryAddWithoutValidation("Origin", "https://music.163.com")
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*")
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8")
        Return client
    End Function

    Private Function FetchSearch(kw As String, limit As Integer) As String
        EnsureTls12()
        Dim attempts As New List(Of Func(Of String))

        attempts.Add(Function()
                         Using c = MusicHttp()
                             Dim url = $"https://music.163.com/api/search/get/web?s={Uri.EscapeDataString(kw)}&type=1&offset=0&limit={limit}"
                             Return c.GetStringAsync(url).GetAwaiter().GetResult()
                         End Using
                     End Function)

        attempts.Add(Function()
                         Using c = MusicHttp()
                             Dim url = "https://music.163.com/api/search/get/web"
                             Dim content As New Net.Http.FormUrlEncodedContent(New Dictionary(Of String, String) From {
                                 {"s", kw}, {"type", "1"}, {"offset", "0"}, {"limit", CStr(limit)}, {"csrf_token", ""}})
                             Dim resp = c.PostAsync(url, content).GetAwaiter().GetResult()
                             Return resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                         End Using
                     End Function)

        attempts.Add(Function()
                         Using c = MusicHttp()
                             Dim url = $"https://interface.music.163.com/api/search/get/web?s={Uri.EscapeDataString(kw)}&type=1&offset=0&limit={limit}"
                             Return c.GetStringAsync(url).GetAwaiter().GetResult()
                         End Using
                     End Function)

        Dim lastErr As Exception = Nothing
        For i As Integer = 0 To attempts.Count - 1
            For retry As Integer = 1 To 2
                Try
                    Dim txt As String = attempts(i)()
                    If Not String.IsNullOrWhiteSpace(txt) AndAlso txt.Contains(Chr(34) & "songs" & Chr(34)) Then
                        If i > 0 OrElse retry > 1 Then
                            Logger.Info($"[XFL] 网易云搜索成功（方式 {i + 1}.{retry}）")
                        End If
                        Return txt
                    End If
                Catch ex As Exception
                    lastErr = ex
                    Logger.Warn($"[XFL] 网易云搜索方式 {i + 1}.{retry} 失败：{ex.Message}")
                    Threading.Thread.Sleep(400)
                End Try
            Next
        Next
        If lastErr IsNot Nothing Then Throw lastErr
        Return ""
    End Function

    Public Function MusicSearch(keyword As String, Optional limit As Integer = 20) As List(Of MusicHit)
        Dim hits As New List(Of MusicHit)
        Dim kw As String = If(keyword, "").Trim()
        If kw.Length = 0 Then Return hits
        Try
            Dim txt As String = FetchSearch(kw, limit)
            If txt.Length > 0 Then
                Dim root As JObject = JObject.Parse(txt)
                Dim arr As JToken = root("result")?("songs")
                If arr Is Nothing Then Return hits
                For Each it As JToken In arr
                    Dim artists As String = ""
                    Dim ar As JToken = If(it("artists"), it("ar"))
                    If ar IsNot Nothing AndAlso ar.Type = JTokenType.Array Then
                        Dim names As New List(Of String)
                        For Each a As JToken In ar
                            If a("name") IsNot Nothing Then names.Add(a("name").ToString())
                        Next
                        artists = String.Join(" / ", names.ToArray())
                    End If
                    Dim album As String = ""
                    If it("album")?("name") IsNot Nothing Then
                        album = it("album")("name").ToString()
                    ElseIf it("al")?("name") IsNot Nothing Then
                        album = it("al")("name").ToString()
                    End If
                    Dim sid As Long = 0
                    If it("id") IsNot Nothing Then Long.TryParse(it("id").ToString(), sid)
                    Dim dur As Integer = 0
                    If it("duration") IsNot Nothing Then Integer.TryParse(it("duration").ToString(), dur)
                    hits.Add(New MusicHit With {
                        .Id = sid,
                        .Name = If(it("name")?.ToString(), ""),
                        .Artist = artists,
                        .Album = album,
                        .Duration = dur})
                Next
            End If
            Logger.Info($"[XFL] 网易云搜索「{If(kw.Length > 20, kw.Substring(0, 20) & "…", kw)}」得到 {hits.Count} 条")
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 网易云搜索失败")
        End Try
        Return hits
    End Function

    Public Function MusicLyric(songId As Long) As String
        Try
            EnsureTls12()
            Using client As Net.Http.HttpClient = MusicHttp()
                Dim url As String = $"https://music.163.com/api/song/lyric?id={songId}&lv=1&kv=1&tv=-1"
                Dim txt As String = client.GetStringAsync(url).GetAwaiter().GetResult()
                Dim root As JObject = JObject.Parse(txt)
                Return If(root("lrc")?("lyric")?.ToString(), "")
            End Using
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 获取歌词失败")
            Return ""
        End Try
    End Function

    Public Function SongWebUrl(songId As Long) As String
        Return $"https://music.163.com/#/song?id={songId}"
    End Function

    Public Function SearchWebUrl(keyword As String) As String
        Return $"https://music.163.com/#/search/m/?s={Uri.EscapeDataString(If(keyword, ""))}&type=1"
    End Function

    Public Function ParseSearch(jsonText As String, limit As Integer) As List(Of MusicHit)
        Dim hits As New List(Of MusicHit)
        Try
            Dim root As JObject = JObject.Parse(jsonText)
            Dim arr As JToken = root("result")?("songs")
            If arr Is Nothing Then Return hits
            For Each it As JToken In arr
                Dim artists As String = ""
                Dim ar As JToken = If(it("artists"), it("ar"))
                If ar IsNot Nothing AndAlso ar.Type = JTokenType.Array Then
                    Dim names As New List(Of String)
                    For Each a As JToken In ar
                        If a("name") IsNot Nothing Then names.Add(a("name").ToString())
                    Next
                    artists = String.Join(" / ", names.ToArray())
                End If
                Dim album As String = ""
                If it("album")?("name") IsNot Nothing Then
                    album = it("album")("name").ToString()
                ElseIf it("al")?("name") IsNot Nothing Then
                    album = it("al")("name").ToString()
                End If
                Dim sid As Long = 0
                If it("id") IsNot Nothing Then Long.TryParse(it("id").ToString(), sid)
                Dim dur As Integer = 0
                If it("duration") IsNot Nothing Then Integer.TryParse(it("duration").ToString(), dur)
                hits.Add(New MusicHit With {
                    .Id = sid,
                    .Name = If(it("name")?.ToString(), ""),
                    .Artist = artists,
                    .Album = album,
                    .Duration = dur})
                If hits.Count >= limit Then Exit For
            Next
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 解析搜索结果失败")
        End Try
        Return hits
    End Function

End Module