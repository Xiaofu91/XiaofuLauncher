
Public Module ModXflUnblock

    <Runtime.InteropServices.DllImport("kernel32.dll", CharSet:=Runtime.InteropServices.CharSet.Unicode, SetLastError:=True)>
    Private Function DeleteFileW(lpFileName As String) As Boolean
    End Function

    <Runtime.InteropServices.DllImport("kernel32.dll", CharSet:=Runtime.InteropServices.CharSet.Unicode, SetLastError:=True)>
    Private Function GetFileAttributesW(lpFileName As String) As UInteger
    End Function

    Private Const INVALID_FILE_ATTRIBUTES As UInteger = 4294967295UI

    Public Sub XflSelfUnblock()
        Try
            Dim exePath As String = Nothing
            Try
                exePath = Reflection.Assembly.GetEntryAssembly()?.Location
            Catch
            End Try
            If String.IsNullOrWhiteSpace(exePath) Then exePath = PathExe

            RemoveZoneIdentifier(exePath)

            Try
                Dim dir As String = IO.Path.GetDirectoryName(exePath)
                If Not String.IsNullOrWhiteSpace(dir) AndAlso IO.Directory.Exists(dir) Then
                    For Each f As String In IO.Directory.GetFiles(dir)
                        RemoveZoneIdentifier(f)
                    Next
                End If
            Catch
            End Try

            Try
                If IO.Directory.Exists(Paths.BaseThenName) Then
                    For Each f As String In IO.Directory.GetFiles(Paths.BaseThenName, "*.exe")
                        RemoveZoneIdentifier(f)
                    Next
                End If
            Catch
            End Try
        Catch ex As Exception

        End Try
    End Sub

    Public Sub RemoveZoneIdentifier(file As String)
        If String.IsNullOrWhiteSpace(file) Then Return
        Try
            DeleteFileW(file & ":Zone.Identifier")
        Catch

        End Try
    End Sub

    Public Function HasZoneIdentifier(file As String) As Boolean
        If String.IsNullOrWhiteSpace(file) Then Return False
        Try
            Return GetFileAttributesW(file & ":Zone.Identifier") <> INVALID_FILE_ATTRIBUTES
        Catch
            Return False
        End Try
    End Function

End Module