
Imports System.Net
Imports System.Net.Sockets

Module ModLanDetect

    Public Class LanRoom
        Public Property WorldName As String
        Public Property Port As Integer
        Public Property SourceIp As String
        Public Property Stamped As Integer
    End Class

    Private ReadOnly RoomLock As New Object
    Private ReadOnly Rooms As New Dictionary(Of Integer, LanRoom)
    Private _udp As UdpClient = Nothing
    Private _running As Boolean = False

    Public Sub StartWatch()
        If _running Then Return
        _running = True
        RunInNewThread(
            Sub()
                Try
                    _udp = New UdpClient()
                    _udp.ExclusiveAddressUse = False
                    _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, True)
                    _udp.Client.Bind(New IPEndPoint(IPAddress.Any, 4445))
                    _udp.JoinMulticastGroup(IPAddress.Parse("224.0.2.60"))
                    Logger.Info("[XFL] 已开始自动识别局域网房间（UDP 4445 多播）")
                Catch ex As Exception
                    Logger.Warn(ex, "[XFL] 加入局域网多播失败，将只支持手动输入端口")
                    _running = False
                    Return
                End Try
                Try
                    Dim remote As New IPEndPoint(IPAddress.Any, 0)
                    While _running
                        Dim data As Byte() = _udp.Receive(remote)
                        Dim text As String = System.Text.Encoding.UTF8.GetString(data)
                        ParseBroadcast(text, remote.Address.ToString())
                    End While
                Catch

                Finally
                    Try
                        _udp.Close()
                    Catch
                    End Try
                    _udp = Nothing
                End Try
            End Sub)
    End Sub

    Private Sub ParseBroadcast(text As String, ip As String)
        Try

            Dim mWorld As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(text, "\[MOTD\](.*?)\[/MOTD\]", System.Text.RegularExpressions.RegexOptions.Singleline)
            Dim mPort As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(text, "\[AD\](.*?)\[/AD\]")
            If Not mPort.Success Then Return
            Dim port As Integer = 0
            If Not Integer.TryParse(mPort.Groups(1).Value, port) Then Return
            SyncLock RoomLock
                Rooms(port) = New LanRoom With {
                    .WorldName = If(mWorld.Success, mWorld.Groups(1).Value, "（未命名世界）"),
                    .Port = port,
                    .SourceIp = ip,
                    .Stamped = Environment.TickCount}

                For Each k As Integer In Rooms.Keys.ToList()
                    If Math.Abs(Environment.TickCount - Rooms(k).Stamped) > 10000 Then Rooms.Remove(k)
                Next
            End SyncLock
            Logger.Info($"[XFL] 发现局域网房间：{If(mWorld.Success, mWorld.Groups(1).Value, "?")}  端口 {port}  来自 {ip}")
        Catch ex As Exception
            Logger.Warn(ex, "[XFL] 解析局域网广播失败")
        End Try
    End Sub

    Public Function GetRooms() As List(Of LanRoom)
        Dim list As New List(Of LanRoom)
        SyncLock RoomLock
            For Each kv In Rooms
                If Math.Abs(Environment.TickCount - kv.Value.Stamped) <= 10000 Then list.Add(kv.Value)
            Next
        End SyncLock
        Return list.OrderByDescending(Function(r) r.Stamped).ToList()
    End Function

    Public Function LatestRoom() As LanRoom
        Dim rs As List(Of LanRoom) = GetRooms()
        If rs.Count = 0 Then Return Nothing
        Dim local As LanRoom = rs.FirstOrDefault(Function(r) r.SourceIp = "127.0.0.1" OrElse r.SourceIp.EndsWith(".1"))
        Return If(local, rs(0))
    End Function

    Public Sub StopWatch()
        _running = False
        If _udp IsNot Nothing Then
            Try
                _udp.Close()
            Catch
            End Try
        End If
        _udp = Nothing
    End Sub

End Module