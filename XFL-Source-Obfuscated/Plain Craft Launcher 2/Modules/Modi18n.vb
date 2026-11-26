Imports System.Globalization
Imports System.Windows.Forms

Module Modi18n

    Public Function GetLang(Key As String, ParamArray Param As String()) As String
        Try
            If String.IsNullOrWhiteSpace(Key) Then Throw New Exception("Key 值未提供;No key value provided")
            Return String.Format(Application.Current.FindResource(Key), Param)
        Catch ex As FormatException
            Logger.Error(ex, $"[Location] 格式化文本失败：{Key};传入参数：{Param.Join(",")}", LogBehavior.Alert)
            Return Application.Current.FindResource(Key)
        Catch ex As ResourceReferenceKeyNotFoundException
            Logger.Error(ex, $"[Location] 找不到对应的语言资源：{Key}")
            Return Key
        Catch ex As Exception
            Logger.Error(ex, $"[Location] 获取语言资源失败：{Key}（{ex.Message}）", LogBehavior.Alert)
            Return Key
        End Try
    End Function

    Public Function GetLangByWord(Word As String) As String
        If Lang = "zh-CN" Then Return Word
        Select Case Word
            Case "正式版"
                Return GetLang("LangDownloadRelease")
            Case "预览版", "快照版本"
                Return GetLang("LangDownloadBeta")
            Case "远古版", "远古版本"
                Return GetLang("LangDownloadAncientVersion")
            Case "愚人节版"
                Return GetLang("LangDownloadAprilFool")
            Case "未知版本"
                Return GetLang("LangDownloadUnknown")
            Case Else

                Return Word
        End Select
    End Function

    Public Function GetLocalTimeFormat(Time As DateTime) As String
        Select Case Lang
            Case "ja-JP", "ko-KR", "lzh", "zh-CN", "zh-HK", "zh-MARS", "zh-MEME", "zh-TW"
                Return Time.ToString("yyyy'/'MM'/'dd HH':'mm")
            Case "en-US"
                Return Time.ToString("MM'/'dd'/'yyyy HH':'mm")
            Case Else
                Return Time.ToString("dd'/'MM'/'yyyy HH':'mm")
        End Select
    End Function

    Public Sub SwitchApplicationFont(Font As FontFamily)
        Try
            Application.Current.Resources("LaunchFontFamily") = Font
        Catch ex As Exception
            Logger.Error(ex, "[Location] 切换字体失败，这可能导致界面显示异常", LogBehavior.Alert)
        End Try
    End Sub

    Private _IsLocationZH As Integer = -1

    Public Function IsLocationZH() As Boolean

        If Not _IsLocationZH.Equals(-1) Then Return _IsLocationZH.Equals(1)
        Dim IsZH As Boolean = CultureInfo.CurrentCulture.Name.Equals("zh-CN")
        IsZH = IsZH And CultureInfo.CurrentUICulture.Name.Equals("zh-CN")
        IsZH = IsZH And RegionInfo.CurrentRegion.ISOCurrencySymbol.Equals("CNY")
        IsZH = IsZH And TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Equals(New TimeSpan(8, 0, 0))
        IsZH = IsZH And InputLanguage.InstalledInputLanguages.OfType(Of InputLanguage).ToList().Any(Function(i) i.Culture.Name.Equals("zh-CN"))
        _IsLocationZH = If(IsZH, 1, 0)
        Return IsZH
    End Function

    Public Function GetDefaultLang() As String
        Dim CurrentCulture As String = CultureInfo.CurrentUICulture.Name
        Dim PrefixMap As New Dictionary(Of String, String) From {
            {"el-", "el-GR"},
            {"es-", "es-ES"},
            {"fr-", "fr-FR"},
            {"ja-", "ja-JP"},
            {"ko-", "ko-KR"},
            {"ru-", "ru-RU"},
            {"sk-", "sk-SK"}
        }

        For Each prefixPair In PrefixMap
            If CurrentCulture.StartsWith(prefixPair.Key) Then
                Return prefixPair.Value
            End If
        Next

        Select Case CurrentCulture
            Case "en-GB", "en-NZ", "en-AU", "en-CA"
                Return "en-GB"
            Case "zh-CN", "zh-SG", "zh-Hans"
                Return "zh-CN"
            Case "zh-HK", "zh-MO"
                Return "zh-HK"
            Case "zh-TW", "zh-Hant"
                Return "zh-TW"
        End Select

        Return "en-US"
    End Function

    Public Function GetLocationNum(Num As Int32) As String
        Select Case Lang
            Case "ja-JP", "ko-KR", "lzh", "zh-CN", "zh-HK", "zh-MARS", "zh-MEME", "zh-TW"
                Return If(Num > 1000000000000, Math.Round(Num / 1000000000000, 2) & " " & GetLang("LangModCompModDigit3"),
                If(Num > 100000000, Math.Round(Num / 100000000, 2) & " " & GetLang("LangModCompModDigit2"),
                If(Num > 100000, Math.Round(Num / 10000, 0) & " " & GetLang("LangModCompModDigit1"), Num.ToString("N0") & " ")))
            Case Else
                Return If(Num > 1000000000, Math.Round(Num / 1000000000, 2) & GetLang("LangModCompModDigit3"),
                If(Num > 1000000, Math.Round(Num / 1000000, 2) & GetLang("LangModCompModDigit2"),
                If(Num > 10000, Math.Round(Num / 1000, 0) & GetLang("LangModCompModDigit1"), Num.ToString("N0"))))
        End Select
    End Function

    Public Function GetLangByNumIsPlural(Count As Int32, Key As String, ParamArray Param As String()) As String
        If Count <= 1 Then
            Return GetLang(Key, Param)
        Else
            Return GetLang(Key & "P", Param)
        End If
    End Function

    Public Function GetLocalizedEnum(Items As IEnumerable(Of String), IsConjunctive As Boolean) As String
        If Items Is Nothing Then Return ""

        Dim arr = Items.Where(Function(x) x IsNot Nothing).ToArray()
        Dim cnt = arr.Length

        Dim coordinator As String = If(IsConjunctive, GetLang("LangConjunctiveCoordinator"), GetLang("LangDisjunctiveCoordinator"))
        Dim separator As String = GetLang("LangSeparator")
        Dim useSerialSeparator As Boolean = (Lang = "en-US" OrElse Lang = "en-UK")

        If cnt = 0 Then Return ""
        If cnt = 1 Then Return arr(0)
        If cnt = 2 Then Return $"{arr(0)} {coordinator} {arr(1)}"

        Dim head As String = String.Join(separator, arr.Take(cnt - 1))
        If useSerialSeparator Then
            Return head & separator & coordinator & " " & arr(cnt - 1)
        Else
            Return head & " " & coordinator & " " & arr(cnt - 1)
        End If
    End Function

End Module
