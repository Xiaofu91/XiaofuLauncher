Public Class PageOtherLeft

    Private IsLoad As Boolean = False
    Private IsPageSwitched As Boolean = False
    Private Sub PageOtherLeft_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        Dim IsHiddenPage As Boolean = False
        If ItemHelp.Checked AndAlso Settings.Get(Of Boolean)("UiHiddenOtherHelp") Then IsHiddenPage = True
        If ItemAbout.Checked AndAlso Settings.Get(Of Boolean)("UiHiddenOtherAbout") Then IsHiddenPage = True
        If ItemTest.Checked AndAlso Settings.Get(Of Boolean)("UiHiddenOtherTest") Then IsHiddenPage = True
        If PageSetupUI.HiddenForceShow Then IsHiddenPage = False

        If IsLoad AndAlso Not IsHiddenPage Then Return
        IsLoad = True

        PageSetupUI.HiddenRefresh()

        If IsPageSwitched Then Return
        If Not Settings.Get(Of Boolean)("UiHiddenOtherHelp") Then
            ItemHelp.SetChecked(True, False, False)
        ElseIf Not Settings.Get(Of Boolean)("UiHiddenOtherAbout") Then
            ItemAbout.SetChecked(True, False, False)
        Else
            ItemTest.SetChecked(True, False, False)
        End If
    End Sub
    Private Sub PageOtherLeft_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
        IsPageSwitched = False
    End Sub

#Region "页面切换"

    Public PageID As FormMain.PageSubType
    Public Sub New()
        InitializeComponent()

        If Not Settings.Get(Of Boolean)("UiHiddenOtherHelp") Then
            PageID = FormMain.PageSubType.OtherHelp
        ElseIf Not Settings.Get(Of Boolean)("UiHiddenOtherAbout") Then
            PageID = FormMain.PageSubType.OtherAbout
        Else
            PageID = FormMain.PageSubType.OtherTest
        End If
    End Sub

    Private Sub PageCheck(sender As FrameworkElement, e As RouteEventArgs) Handles ItemAbout.Check, ItemHelp.Check, ItemTest.Check

        If sender.Tag IsNot Nothing Then PageChange(Val(sender.Tag))
    End Sub

    Public Function PageGet(Optional ID As FormMain.PageSubType = -1)
        If ID = -1 Then ID = PageID
        Select Case ID
            Case FormMain.PageSubType.OtherHelp
                If FrmOtherHelp Is Nothing Then FrmOtherHelp = New PageOtherHelp
                Return FrmOtherHelp
            Case FormMain.PageSubType.OtherAbout
                If FrmOtherAbout Is Nothing Then FrmOtherAbout = New PageOtherAbout
                Return FrmOtherAbout
            Case FormMain.PageSubType.OtherTest
                If FrmOtherTest Is Nothing Then FrmOtherTest = New PageOtherTest
                Return FrmOtherTest
            Case Else
                Throw New Exception("未知的更多子页面种类：" & ID)
        End Select
    End Function

    Public Sub PageChange(ID As FormMain.PageSubType)
        If PageID = ID Then Return
        AniControlEnabled += 1
        IsPageSwitched = True
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

    Public Sub Refresh(sender As Object, e As EventArgs)
        Select Case Val(sender.Tag)
            Case FormMain.PageSubType.OtherHelp
                RefreshHelp()
                ItemHelp.Checked = True
        End Select
        Hint(GetLang("LangPageOtherRefreshing"), Log:=False)
    End Sub
    Public Shared Sub RefreshHelp()
        Settings.Set("SystemHelpVersion", 0)
        FrmOtherHelp.PageLoaderRestart()
        FrmOtherHelp.SearchBox.Text = ""
    End Sub

    Private Sub TryFeedback(sender As Object, e As RouteEventArgs) Handles ItemFeedback.Changed
        If Not ItemFeedback.Checked Then Return
        TryFeedback()
        e.Handled = True
    End Sub
    Public Shared Sub TryFeedback()
        If False.Equals(PageSetupSystem.IsLauncherNewest) Then Return
        Select Case MyMsgBox(GetLang("LangPageOtherDialogFeedbackContent"),
                    GetLang("LangPageOtherDialogFeedbackTitle"), GetLang("LangPageOtherDialogFeedbackBtn1"), GetLang("LangPageOtherDialogFeedbackBtn2"), GetLang("LangDialogBtnCancel"))
            Case 1
                Feedback(True, False)
            Case 2
                OpenWebsite("https://github.com/PCL-Community/PCL2-Language/issues/new/choose")
        End Select
    End Sub
    Private Sub TryVote(sender As Object, e As RouteEventArgs) Handles ItemVote.Changed
        If Not ItemVote.Checked Then Return
        TryVote()
        e.Handled = True
    End Sub
    Public Shared Sub TryVote()
        If MyMsgBox(GetLang("LangPageOtherDialogVoteContent"),
                    GetLang("LangPageOtherDialogVoteTitle"), GetLang("LangSelectOpen"), GetLang("LangDialogBtnCancel")) = 2 Then Return
        OpenWebsite("https://github.com/Meloong-Git/XFL/discussions/categories/%E5%8A%9F%E8%83%BD%E6%8A%95%E7%A5%A8?discussions_q=category%3A%E5%8A%9F%E8%83%BD%E6%8A%95%E7%A5%A8+sort%3Adate_created")
    End Sub

End Class
