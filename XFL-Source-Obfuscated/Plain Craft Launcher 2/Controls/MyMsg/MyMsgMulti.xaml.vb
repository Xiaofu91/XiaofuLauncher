Public Class MyMsgMulti

    Public Shared SelectedItems As New List(Of Object)

    Public ResultItems As New List(Of MyListItem)

    Public OnConfirm As Action(Of List(Of MyListItem)) = Nothing

    Private ReadOnly Uuid As Integer = GetUuid()
    Private ReadOnly _items As New List(Of MyListItem)

    Private _autoSelectAll As Boolean = False
    Private _autoConfirm As Boolean = False

    Public Sub New(Title As String, Hint As String, Items As List(Of MyListItem),
                   Btn1Text As String, Btn2Text As String,
                   Optional autoSelectAll As Boolean = False,
                   Optional autoConfirm As Boolean = False)
        Try
            InitializeComponent()
            Btn1.Name = Btn1.Name & GetUuid()
            Btn2.Name = Btn2.Name & GetUuid()
            BtnAll.Name = BtnAll.Name & GetUuid()
            BtnNone.Name = BtnNone.Name & GetUuid()
            LabTitle.Text = Title
            LabHint.Text = Hint
            Btn1.Text = Btn1Text
            Btn2.Text = Btn2Text
            For Each it In Items
                If it Is Nothing Then Continue For
                it.Type = MyListItem.CheckType.CheckBox
                it.MinHeight = 34
                _items.Add(it)
                PanSelection.Children.Add(it)
            Next
            Btn1.IsEnabled = False
            _autoSelectAll = autoSelectAll
            _autoConfirm = autoConfirm
            AddHandler Btn1.Click, AddressOf Btn1_Click
            AddHandler Btn2.Click, AddressOf Btn2_Click
            AddHandler BtnAll.Click, AddressOf BtnAll_Click
            AddHandler BtnNone.Click, AddressOf BtnNone_Click
            For Each it In _items
                AddHandler it.Check, AddressOf Item_Check
            Next
        Catch ex As Exception
            Logger.Error(ex, "多选弹窗初始化失败")
        End Try
    End Sub

    Private Sub Item_Check(sender As Object, e As EventArgs)
        Try
            Dim n As Integer = _items.Where(Function(i As MyListItem) i.Checked).Count()
            Btn1.IsEnabled = n > 0
            Btn1.Text = If(n > 0, $"下载所选（{n}）", "下载所选")
        Catch
        End Try
    End Sub

    Private Sub BtnAll_Click(sender As Object, e As EventArgs)
        SelectAllItems()
    End Sub

    Public Sub SelectAllItems()
        Try
            For Each it In _items
                it.Checked = True
            Next
            Dim nSet As Integer = _items.Where(Function(i As MyListItem) i.Checked).Count()
            Logger.Info($"多选弹窗：SelectAllItems 后已勾选 {nSet}/{_items.Count}")
            Item_Check(Nothing, Nothing)
        Catch
        End Try
    End Sub

    Public Sub ConfirmSelection()
        Try
            Btn1_Click(Nothing, Nothing)
        Catch
        End Try
    End Sub

    Private Sub BtnNone_Click(sender As Object, e As EventArgs)
        Try
            For Each it In _items
                it.Checked = False
            Next
            Item_Check(Nothing, Nothing)
        Catch
        End Try
    End Sub

    Private Sub Btn1_Click(sender As Object, e As EventArgs)
        SelectedItems = New List(Of Object)
        ResultItems = New List(Of MyListItem)
        For Each it In _items
            If it.Checked Then
                SelectedItems.Add(it)
                ResultItems.Add(it)
            End If
        Next
        Logger.Info($"多选弹窗：Btn1_Click 收集到 {SelectedItems.Count}/{_items.Count} 项，ResultItems={ResultItems.Count}")

        Try
            If OnConfirm IsNot Nothing Then OnConfirm(ResultItems)
        Catch ex As Exception
            Logger.Warn(ex, "多选弹窗：确认回调异常")
        End Try
        Close()
    End Sub

    Private Sub Btn2_Click(sender As Object, e As EventArgs)
        SelectedItems = New List(Of Object)
        Close()
    End Sub

    Private Sub Load(sender As Object, e As EventArgs) Handles MyBase.Loaded
        Try
            Opacity = 0
            AniStart({
                AaOpacity(Me, 1, 120, 60),
                AaDouble(Sub(i) TransformPos.Y += i, -TransformPos.Y, 300, 60, New AniEaseOutBack(AniEasePower.Weak)),
                AaDouble(Sub(i) TransformRotate.Angle += i, -TransformRotate.Angle, 300, 60, New AniEaseOutFluent(AniEasePower.Weak))
            }, "MyMsgMulti " & Uuid)
            Logger.Info($"多选弹窗：{LabTitle.Text}（{_items.Count} 项）")

            If _autoSelectAll Then
                SelectAllItems()
                Logger.Info("多选弹窗：已自动全选")
            End If
            If _autoConfirm Then

                ConfirmSelection()
            End If
        Catch ex As Exception
            Logger.Error(ex, "多选弹窗加载失败")
        End Try
    End Sub

    Private Sub Close()
        Try

            Try
                Interop.ComponentDispatcher.PopModal()
            Catch
            End Try
            AniStart({
                AaOpacity(Me, 0, 160, 60),
                AaDouble(Sub(i) TransformPos.Y += i, 30, 200, 60, New AniEaseOutFluent(AniEasePower.Weak)),
                AaCode(Sub() FrmMain.PanMsg.Children.Remove(Me))
            }, "MyMsgMulti " & Uuid)
        Catch
            FrmMain.PanMsg.Children.Remove(Me)
        End Try
    End Sub

End Class
