
Imports System.Text

Module ModLinkCode

    Private Const TagFull As String = "XFL2"
    Private ReadOnly Alphabet As Char() = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray()

    Public Function ShortCode(roomName As String, password As String) As String
        Try
            Dim payload As String = Normalize(roomName) & vbTab & Normalize(password)
            Using md5 As Security.Cryptography.MD5 = Security.Cryptography.MD5.Create()
                Dim hash As Byte() = md5.ComputeHash(Encoding.UTF8.GetBytes(payload))
                Dim sb As New System.Text.StringBuilder
                For i As Integer = 0 To 5
                    sb.Append(Alphabet(hash(i) Mod 32))
                Next
                Return sb.ToString()
            End Using
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 生成短码失败")
            Return "------"
        End Try
    End Function

    Public Function EncodeShare(roomName As String, password As String) As String
        Try
            Dim payload As String = Normalize(roomName) & vbTab & Normalize(password)
            Dim bytes As Byte() = Encoding.UTF8.GetBytes(payload)
            Dim sb As New System.Text.StringBuilder
            Dim buffer As Integer = 0
            Dim bits As Integer = 0
            For Each b As Byte In bytes
                buffer = (buffer << 8) Or b
                bits += 8
                While bits >= 5
                    bits -= 5
                    sb.Append(Alphabet((buffer >> bits) And 31))
                End While
            Next
            If bits > 0 Then sb.Append(Alphabet((buffer << (5 - bits)) And 31))
            Return TagFull & "-" & sb.ToString()
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 生成分享码失败")
            Return ""
        End Try
    End Function

    Public Function DecodeShare(code As String, ByRef roomName As String, ByRef password As String) As Boolean
        roomName = ""
        password = ""
        Try
            Dim raw As String = If(code, "").Trim()
            Dim s As String = raw.ToUpperInvariant()
            If Not s.StartsWith(TagFull) Then
                If raw.Length > 0 Then
                    roomName = raw
                    password = ""
                    Return True
                End If
                Return False
            End If
            s = s.Substring(TagFull.Length).Replace("-"c, "").Replace(" "c, "")

            Dim buf As Integer = 0
            Dim bits As Integer = 0
            Dim outBytes As New List(Of Byte)
            For Each ch As Char In s
                Dim idx As Integer = Array.IndexOf(Alphabet, ch)
                If idx < 0 Then Continue For
                buf = (buf << 5) Or idx
                bits += 5
                If bits >= 8 Then
                    bits -= 8
                    outBytes.Add(CByte((buf >> bits) And 255))
                End If
            Next
            If outBytes.Count = 0 Then Return False
            Dim txt As String = Encoding.UTF8.GetString(outBytes.ToArray())
            Dim parts As String() = txt.Split(vbTab)
            If parts.Length < 1 OrElse parts(0).Trim().Length = 0 Then Return False
            roomName = parts(0).Trim()
            password = If(parts.Length >= 2, parts(1).Trim(), "")
            Return True
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 解析分享码失败")
            Return False
        End Try
    End Function

    Public Function Normalize(s As String) As String
        Dim t As String = If(s, "").Trim().ToUpperInvariant()
        Dim sb As New System.Text.StringBuilder
        For Each ch As Char In t
            If Char.IsWhiteSpace(ch) Then Continue For
            sb.Append(ch)
        Next
        Return sb.ToString()
    End Function

End Module