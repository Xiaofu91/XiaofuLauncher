
Friend Class XflWebViewKeeper

    Private _owner As FrameworkElement
    Private _wv As UIElement
    Private _tag As String
    Private _expected As Boolean = False
    Private _suspended As Boolean = False
    Private _lastApplied As Nullable(Of Boolean) = Nothing

    Public Sub New(owner As FrameworkElement, wv As UIElement, tag As String)
        _owner = owner
        _wv = wv
        _tag = tag
    End Sub

    Public Sub BindControl(wv As UIElement)
        _wv = wv
        _lastApplied = Nothing
        HookTracking()
    End Sub

    Public Sub SetExpected(show As Boolean)
        _expected = show
        Update()
    End Sub

    Public ReadOnly Property Expected As Boolean
        Get
            Return _expected
        End Get
    End Property

    Public Sub SetSuspended(suspend As Boolean)
        _suspended = suspend
        Update()
    End Sub

    Public Sub HookTracking()
        Try
            If _owner Is Nothing OrElse _tracked Then Return
            _tracked = True
            AddHandler _owner.IsVisibleChanged, Sub(s, e) Update()
            AddHandler _owner.SizeChanged, Sub(s, e) Update()

            TryAttachPanMsg()
            Logger.Info($"[XFL][空域] {_tag} 已挂状态监听")
            Update()
        Catch ex As Exception
            Logger.Warn(ex, $"[XFL][空域] {_tag} 挂监听失败")
        End Try
    End Sub
    Private _tracked As Boolean = False

    Public Sub Update()
        Try
            If _wv Is Nothing Then Return

            If Not _panMsgHooked Then TryAttachPanMsg()
            Dim show As Boolean = _expected AndAlso Not _suspended
            If show AndAlso _owner IsNot Nothing Then
                show = _owner.IsVisible AndAlso _owner.ActualWidth > 1 AndAlso _owner.ActualHeight > 1
            End If
            If show AndAlso HasModalOverlay() Then show = False
            If _lastApplied.HasValue AndAlso _lastApplied.Value = show Then Return
            _lastApplied = show
            _wv.Visibility = If(show, Windows.Visibility.Visible, Windows.Visibility.Collapsed)
            Logger.Info($"[XFL][空域] {_tag} {(If(show, "显示", "隐藏"))}(期望={_expected} 压制={_suspended} 实际可见={If(_owner IsNot Nothing, _owner.IsVisible, False)})")
        Catch ex As Exception
            Logger.Warn(ex, $"[XFL][空域] {_tag} 切换失败")
        End Try
    End Sub

    Private Function HasModalOverlay() As Boolean
        Try
            If FrmMain Is Nothing Then Return False
            If FrmMain.PanMsg IsNot Nothing AndAlso FrmMain.PanMsg.IsVisible Then Return True
        Catch
        End Try
        Return False
    End Function

    Private _panMsgHooked As Boolean = False

    Private Sub TryAttachPanMsg()
        If _panMsgHooked Then Return
        Try
            If FrmMain IsNot Nothing AndAlso FrmMain.PanMsg IsNot Nothing Then
                AddHandler FrmMain.PanMsg.IsVisibleChanged,
                    Sub(s As Object, e As DependencyPropertyChangedEventArgs) Update()
                _panMsgHooked = True
            End If
        Catch
        End Try
    End Sub
End Class
