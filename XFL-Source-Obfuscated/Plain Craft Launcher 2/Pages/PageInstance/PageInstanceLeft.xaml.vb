Public Class PageInstanceLeft
    Implements IRefreshable

    Public Shared Instance As McInstance = Nothing

    Public Sub RefreshModDisabled() Handles Me.Loaded
        If Instance IsNot Nothing AndAlso Instance.Modable Then
            ItemMod.Visibility = Visibility.Visible
            ItemModDisabled.Visibility = Visibility.Collapsed
        Else
            ItemMod.Visibility = Visibility.Collapsed
            ItemModDisabled.Visibility = Visibility.Visible
        End If
    End Sub

#Region "页面切换"

    Public PageID As FormMain.PageSubType = FormMain.PageSubType.Default

    Private Sub PageCheck(sender As FrameworkElement, e As RouteEventArgs) Handles ItemOverall.Check, ItemMod.Check, ItemModDisabled.Check, ItemSetup.Check, ItemExport.Check

        If sender.Tag IsNot Nothing Then PageChange(Val(sender.Tag))
    End Sub

    Public Function PageGet(Optional ID As FormMain.PageSubType = -1)
        If ID = -1 Then ID = PageID
        Select Case ID
            Case FormMain.PageSubType.InstanceOverall
                If FrmInstanceOverall Is Nothing Then FrmInstanceOverall = New PageInstanceOverall
                Return FrmInstanceOverall
            Case FormMain.PageSubType.InstanceMod
                If FrmInstanceMod Is Nothing Then FrmInstanceMod = New PageInstanceMod
                Return FrmInstanceMod
            Case FormMain.PageSubType.InstanceModDisabled
                If FrmInstanceModDisabled Is Nothing Then FrmInstanceModDisabled = New PageInstanceModDisabled
                Return FrmInstanceModDisabled
            Case FormMain.PageSubType.InstanceSetup
                If IsNothing(FrmInstanceSetup) Then FrmInstanceSetup = New PageInstanceSetup
                Return FrmInstanceSetup
            Case FormMain.PageSubType.InstanceExport
                If FrmInstanceExport Is Nothing Then FrmInstanceExport = New PageInstanceExport
                Return FrmInstanceExport
            Case Else
                Throw New Exception("未知的版本设置子页面种类：" & ID)
        End Select
    End Function

    Public Sub PageChange(ID As FormMain.PageSubType)
        If PageID = ID Then Return
        AniControlEnabled += 1
        Try
            PageChangeRun(PageGet(ID))
            PageID = ID
        Catch ex As Exception
            Logger.Error(ex, $"切换分页面失败（ID {ID}）")
        Finally
            AniControlEnabled -= 1
        End Try
    End Sub
    Private Shared Sub PageChangeRun(Target As MyPageRight)
        AniStop("FrmMain PageChangeRight")
        If Target.Parent IsNot Nothing Then Target.SetValue(ContentPresenter.ContentProperty, Nothing)
        FrmMain.PageRight = Target
        CType(FrmMain.PanMainRight.Child, MyPageRight).PageOnExit()
        AniStart({
            AaCode(Sub()
                       CType(FrmMain.PanMainRight.Child, MyPageRight).PageOnForceExit()
                       FrmMain.PanMainRight.Child = FrmMain.PageRight
                       FrmMain.PageRight.Opacity = 0
                   End Sub, 130),
            AaCode(Sub()

                       FrmMain.PageRight.Opacity = 1
                       FrmMain.PageRight.PageOnEnter()
                   End Sub, 30, True)
        }, "PageLeft PageChange")
    End Sub

#End Region

    Public Sub Refresh_Click(sender As Object, e As EventArgs)
        Refresh(Val(sender.Tag))
    End Sub
    Public Sub Refresh() Implements IRefreshable.Refresh
        Refresh(FrmMain.PageCurrentSub)
    End Sub
    Public Sub Refresh(SubType As FormMain.PageSubType)
        Select Case SubType
            Case FormMain.PageSubType.InstanceMod
                PageInstanceMod.Refresh()
                ItemMod.Checked = True
            Case FormMain.PageSubType.InstanceExport
                If FrmInstanceExport IsNot Nothing Then FrmInstanceExport.RefreshAll()
                ItemExport.Checked = True
        End Select
    End Sub

    Public Sub Reset(sender As Object, e As EventArgs)
        If MyMsgBox(GetLang("LangPageVersionLeftSettingDialogIndependentSetContent"), GetLang("LangPageVersionLeftSettingDialogIndependentSetTitle"),, GetLang("LangDialogBtnCancel"), IsWarn:=True) = 1 Then
            If IsNothing(FrmInstanceSetup) Then FrmInstanceSetup = New PageInstanceSetup
            FrmInstanceSetup.Reset()
            ItemSetup.Checked = True
        End If
    End Sub

    Public Shared CurrentJavaWorker As New RedoableWorker(Of Java)(Function(c, p) SelectOrDownloadJava(PageInstanceLeft.Instance, False, c, p))
    Public Shared Sub ReloadCurrentJava() Handles Me.Loaded
        If AniControlEnabled <> 0 Then Return
        If PageInstanceLeft.Instance Is Nothing Then Return
        CurrentJavaWorker.Start()
    End Sub

End Class
