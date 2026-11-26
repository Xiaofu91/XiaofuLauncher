Public Class PageSetupUI

    Private Sub PageSetupUI_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        PanBack.ScrollToHome()
        ThemeCheckAll(True)
        If ThemeDontClick <> 0 Then
            Dim NewText As String = Nothing
            Select Case ThemeDontClick
                Case 1
                    NewText = GetLang("LangThemeRealWhite")
                Case 2
                    NewText = GetLang("LangThemeRealFunny")
                Case Else
                    NewText = "？？？"
            End Select
            For Each Control In PanLauncherTheme.Children
                If (TypeOf Control Is MyRadioBox) AndAlso CType(Control, MyRadioBox).IsEnabled Then CType(Control, MyRadioBox).Text = NewText
            Next
        End If

        AniControlEnabled += 1
        Refresh()
        AniControlEnabled -= 1

        Static Reloaded As Boolean = False
        If Reloaded Then Return
        Reloaded = True

        SliderLoad()

    End Sub
    Public Sub Refresh()
        Try
            SettingService.RefreshSettings(Me)
            BackgroundRefresh(False, False)

            CheckLogoLeft.Visibility = If(RadioLogoType0.Checked, Visibility.Visible, Visibility.Collapsed)
            PanLogoText.Visibility = If(RadioLogoType2.Checked, Visibility.Visible, Visibility.Collapsed)
            PanLogoChange.Visibility = If(RadioLogoType3.Checked, Visibility.Visible, Visibility.Collapsed)

            MusicRefreshUI()

            OnMainPageTypeChanged()
        Catch ex As NullReferenceException
            Logger.Error(ex, GetLang("LangHintThemeSetIncorrect"), LogBehavior.Alert)
            Reset()
        Catch ex As Exception
            Logger.Error(ex, GetLang("LangHintThemeSetLoadFail"))
        End Try
    End Sub
    Public Sub Reset()
        Try
            SettingService.ResetSettings(Me)
            Logger.Info(GetLang("LangHintThemeSetInit"))
            Hint(GetLang("LangHintThemeSetInit"), HintType.Green, False)
        Catch ex As Exception
            Logger.Error(ex, GetLang("LangHintThemeSetInitFail"), LogBehavior.Alert)
        End Try
        Refresh()
    End Sub

    Private Sub BtnUIBgOpen_Click(sender As Object, e As EventArgs) Handles BtnBackgroundOpen.Click
        OpenExplorer(Paths.Base & "XFL\Pictures\")
    End Sub
    Private Sub BtnBackgroundRefresh_Click(sender As Object, e As EventArgs) Handles BtnBackgroundRefresh.Click
        BackgroundRefresh(True, True)
    End Sub
    Public Sub BackgroundRefreshUI(Show As Boolean, Count As Integer)
        If IsNothing(PanBackgroundOpacity) Then Return
        If Show Then
            PanBackgroundOpacity.Visibility = Visibility.Visible
            PanBackgroundBlur.Visibility = Visibility.Visible
            PanBackgroundSuit.Visibility = Visibility.Visible
            BtnBackgroundClear.Visibility = Visibility.Visible
            CardBackground.Title = GetLang("LangBackgroundPicCount", Count)
        Else
            PanBackgroundOpacity.Visibility = Visibility.Collapsed
            PanBackgroundBlur.Visibility = Visibility.Collapsed
            PanBackgroundSuit.Visibility = Visibility.Collapsed
            BtnBackgroundClear.Visibility = Visibility.Collapsed
            CardBackground.Title = GetLang("LangBackgroundPic")
        End If
        CardBackground.TriggerForceResize()
    End Sub
    Private Sub BtnBackgroundClear_Click(sender As Object, e As EventArgs) Handles BtnBackgroundClear.Click
        If MyMsgBox(GetLang("LangDialogDeleteAllBackgroundPicContent"), GetLang("LangDialogTitleWarning"),, GetLang("LangDialogBtnCancel"), IsWarn:=True) = 1 Then
            DirectoryUtils.Delete(Paths.Base & "XFL\Pictures")
            BackgroundRefresh(False, True)
            Hint(GetLang("LangHintBackgroundPicDeleted"), HintType.Green)
        End If
    End Sub

    Public Shared Sub BackgroundRefresh(IsHint As Boolean, Refresh As Boolean)
        Try

            DirectoryUtils.Create(Paths.Base & "XFL\Pictures\")
            Dim Pic As New List(Of String)
            For Each File In DirectoryUtils.EnumerateFiles(Paths.Base & "XFL\Pictures\", True)
                Dim Extension As String = PathUtils.GetExtension(File)
                If Extension <> "ini" AndAlso Extension <> "db" Then Pic.Add(File)
            Next

            If Not Pic.Any() Then
                If Refresh Then
                    If FrmMain.ImgBack.Visibility = Visibility.Collapsed Then
                        If IsHint Then Hint(GetLang("LangHintBackgroundPicNotFound"), HintType.Red)
                    Else
                        FrmMain.ImgBack.Visibility = Visibility.Collapsed
                        If IsHint Then Hint(GetLang("LangHintBackgroundPicDeleted"), HintType.Green)
                    End If
                End If
                If Not IsNothing(FrmSetupUI) Then FrmSetupUI.BackgroundRefreshUI(False, 0)
            Else
                If Refresh Then
                    Dim Address As String = RandomOne(Pic)
                    Try
                        Logger.Info($"加载背景图片：{Address}")
                        FrmMain.ImgBack.Background = New MyBitmap(Address)
                        FrmMain.ImgBack.Visibility = Visibility.Visible
                        FrmMain.UpdateBackgroundAndTitleBar()
                        If IsHint Then Hint(GetLang("LangHintBackgroundPicRefreshed") & PathUtils.GetLastPart(Address), HintType.Green, False)
                    Catch ex As Exception
                        If ex.Message.Contains("参数无效") Then
                            Logger.Error(GetLang("LangDialogBackgroundPicRefreshFailA") & Address, LogBehavior.Alert)
                        Else
                            Logger.Error(ex, GetLang("LangDialogBackgroundPicRefreshFailB", Address), LogBehavior.Alert)
                        End If
                    End Try
                End If
                If Not IsNothing(FrmSetupUI) Then FrmSetupUI.BackgroundRefreshUI(True, Pic.Count)
            End If

        Catch ex As Exception
            Logger.Error(ex, GetLang("LangDialogBackgroundPicRefreshFailUnknown"))
        End Try
    End Sub

    Private Sub BtnLogoChange_Click(sender As Object, e As EventArgs) Handles BtnLogoChange.Click
        Dim FileName As String = Dialogs.SelectFile(GetLang("LangDialogSelectImage"), False, filter:={({"png", "jpg", "jpeg", "gif", "webp"}, GetLang("LangDialogImageFileFilter"))}).FirstOrDefault()
        If String.IsNullOrEmpty(FileName) Then Return
        Dim TargetPath As String = Paths.Base & "XFL\Logo.png"
        Try

            FileUtils.Copy(FileName, TargetPath)

            FrmMain.ImageTitleLogo.Source = Nothing
            FrmMain.ImageTitleLogo.Source = TargetPath
        Catch ex As Exception
            If ex.Message.Contains("参数无效") Then
                Logger.Error(GetLang("LangDialogTitlePicChangeFailA"), LogBehavior.Alert)
            Else
                Logger.Error(ex, GetLang("LangDialogTitlePicChangeFailB"), LogBehavior.Alert)
            End If
            FrmMain.ImageTitleLogo.Source = Nothing
        End Try
    End Sub
    Private Sub RadioLogoType3_Check(sender As Object, e As RouteEventArgs) Handles RadioLogoType3.PreviewCheck
        If Not (AniControlEnabled = 0 AndAlso e.RaiseByMouse) Then Return
Refresh:

        Dim TargetPath As String = Paths.Base & "XFL\Logo.png"
        If FileUtils.Exists(TargetPath) Then
            Try
                FrmMain.ImageTitleLogo.Source = Nothing
                FrmMain.ImageTitleLogo.Source = TargetPath
            Catch ex As Exception
                If ex.Message.Contains("参数无效") Then
                    Logger.Error(GetLang("LangDialogTitlePicChangeFailC"), LogBehavior.Alert)
                Else
                    Logger.Error(ex, GetLang("LangDialogTitlePicChangeFailD"), LogBehavior.Alert)
                End If
                FrmMain.ImageTitleLogo.Source = Nothing
                e.Handled = True
                Try
                    FileUtils.Delete(TargetPath)
                Catch exx As Exception
                    Logger.Error(exx, GetLang("LangDialogTitlePicChangeFailE"), LogBehavior.Alert)
                End Try
            End Try
            Return
        End If

        Dim FileName As String = Dialogs.SelectFile(GetLang("LangDialogSelectImage"), False, filter:={({"png", "jpeg", "jpg", "gif", "webp"}, GetLang("LangDialogImageFileFilter"))}).FirstOrDefault()
        If String.IsNullOrEmpty(FileName) Then
            FrmMain.ImageTitleLogo.Source = Nothing
            e.Handled = True
        Else
            Try
                FileUtils.Copy(FileName, TargetPath)
                GoTo Refresh
            Catch ex As Exception
                Logger.Error(ex, GetLang("LangDialogTitlePicChangeFailF"), LogBehavior.Alert)
            End Try
        End If
    End Sub
    Private Sub BtnLogoDelete_Click(sender As Object, e As EventArgs) Handles BtnLogoDelete.Click
        Try
            FileUtils.Delete(Paths.Base & "XFL\Logo.png")
            RadioLogoType1.SetChecked(True, True)
            Hint(GetLang("LangHintTitlePicEmptied"), HintType.Green)
        Catch ex As Exception
            Logger.Error(ex, GetLang("LangDialogTitlePicEmptyFail"), LogBehavior.Alert)
        End Try
    End Sub

    Private Sub BtnMusicOpen_Click(sender As Object, e As EventArgs) Handles BtnMusicOpen.Click
        OpenExplorer(Paths.Base & "XFL\Musics\")
    End Sub
    Private Sub BtnMusicRefresh_Click(sender As Object, e As EventArgs) Handles BtnMusicRefresh.Click
        MusicRefreshPlay(True)
    End Sub
    Public Sub MusicRefreshUI()
        If PanBackgroundOpacity Is Nothing Then Return
        If MusicAllList.Any Then
            PanMusicVolume.Visibility = Visibility.Visible
            PanMusicDetail.Visibility = Visibility.Visible
            BtnMusicClear.Visibility = Visibility.Visible
            CardMusic.Title = GetLang("LangBackgroundMusicCount", DirectoryUtils.EnumerateFiles(Paths.Base & "XFL\Musics\", True).Count(Function(f) Not {"ini", "jpg", "txt", "cfg", "lrc", "db", "png"}.Contains(PathUtils.GetExtension(f))))
        Else
            PanMusicVolume.Visibility = Visibility.Collapsed
            PanMusicDetail.Visibility = Visibility.Collapsed
            BtnMusicClear.Visibility = Visibility.Collapsed
            CardMusic.Title = GetLang("LangBackgroundMusic")
        End If
        CardMusic.TriggerForceResize()
    End Sub
    Private Sub BtnMusicClear_Click(sender As Object, e As EventArgs) Handles BtnMusicClear.Click
        If MyMsgBox(GetLang("LangDialogBackgroundMusicDeleteContent"), GetLang("LangDialogTitleWarning"),, GetLang("LangDialogBtnCancel"), IsWarn:=True) = 1 Then
            RunInThread(
            Sub()
                Hint(GetLang("LangHintBackgroundMusicDeleting"))

                MusicNAudio = Nothing
                MusicWaitingList = New List(Of String)
                MusicAllList = New List(Of String)
                Thread.Sleep(200)

                Try
                    DirectoryUtils.Delete(Paths.Base & "XFL\Musics")
                    Hint(GetLang("LangHintBackgroundMusicDeleted"), HintType.Green)
                Catch ex As Exception
                    Logger.Error(ex, GetLang("LangHintBackgroundMusicDeleteFail"), LogBehavior.Alert)
                End Try
                Try
                    DirectoryUtils.Create(Paths.Base & "XFL\Musics\")
                    RunInUi(Sub() MusicRefreshPlay(False))
                Catch ex As Exception
                    Logger.Error(ex, GetLang("LangHintBackgroundMusicCreateFolderFail"), LogBehavior.Alert)
                End Try
            End Sub)
        End If
    End Sub
    Private Sub CheckMusicStart_Change() Handles CheckMusicStart.Change
        If AniControlEnabled <> 0 Then Return
        If CheckMusicStart.Checked Then CheckMusicStop.Checked = False
    End Sub
    Private Sub CheckMusicStop_Change() Handles CheckMusicStop.Change
        If AniControlEnabled <> 0 Then Return
        If CheckMusicStop.Checked Then CheckMusicStart.Checked = False
    End Sub

    Private Sub BtnCustomFile_Click(sender As Object, e As EventArgs) Handles BtnCustomFile.Click
        Try
            If FileUtils.Exists(Paths.Base & "XFL\Custom.xaml") Then
                If MyMsgBox(GetLang("LangDialogCustomPageOverwriteConfirmationContent"), GetLang("LangDialogCustomHomePageReplaceTitle"), GetLang("LangDialogBtnContinue"), GetLang("LangDialogBtnCancel"), IsWarn:=True) = 2 Then Return
            End If
            ExtractResources(Paths.Base & "XFL\Custom.xaml", "Custom")
            Hint(GetLang("LangHintCustomPageOverwriteSuccess"), HintType.Green)
            OpenExplorer(Paths.Base & "XFL\Custom.xaml")
        Catch ex As Exception
            Logger.Error(ex, GetLang("LangDialogCustomPageOverwriteFail"))
        End Try
    End Sub
    Private Sub BtnCustomRefresh_Click() Handles BtnCustomRefresh.Click
        FrmLaunchRight.ForceRefresh()
        Hint(GetLang("LangHintCustomPageRefreshed"), HintType.Green)
    End Sub
    Private Sub BtnCustomTutorial_Click(sender As Object, e As EventArgs) Handles BtnCustomTutorial.Click
        MyMsgBox(GetLang("LangDialogCustomPageTeachContent"), GetLang("LangDialogCustomPageTeachTitle"))
    End Sub
    Public Shared Sub OnMainPageTypeChanged()
        If FrmSetupUI Is Nothing Then Return
        Select Case CInt(Settings.Get(Of Integer)("UiCustomType"))
            Case 0
                FrmSetupUI.PanCustomPreset.Visibility = Visibility.Collapsed
                FrmSetupUI.PanCustomLocal.Visibility = Visibility.Collapsed
                FrmSetupUI.PanCustomNet.Visibility = Visibility.Collapsed
                FrmSetupUI.HintCustom.Visibility = Visibility.Collapsed
                FrmSetupUI.HintCustomWarn.Visibility = Visibility.Collapsed
            Case 1
                FrmSetupUI.PanCustomPreset.Visibility = Visibility.Collapsed
                FrmSetupUI.PanCustomLocal.Visibility = Visibility.Visible
                FrmSetupUI.PanCustomNet.Visibility = Visibility.Collapsed
                FrmSetupUI.HintCustom.Visibility = Visibility.Visible
                FrmSetupUI.HintCustomWarn.Visibility = If(Settings.Get(Of Boolean)("HintCustomWarn"), Visibility.Collapsed, Visibility.Visible)
                FrmSetupUI.HintCustom.Text = GetLang("LangSetHomePageTipLocal")
                CustomEventService.SetEventType(FrmSetupUI.HintCustom, CustomEvent.EventType.None)
            Case 2
                FrmSetupUI.PanCustomPreset.Visibility = Visibility.Collapsed
                FrmSetupUI.PanCustomLocal.Visibility = Visibility.Collapsed
                FrmSetupUI.PanCustomNet.Visibility = Visibility.Visible
                FrmSetupUI.HintCustom.Visibility = Visibility.Visible
                FrmSetupUI.HintCustomWarn.Visibility = If(Settings.Get(Of Boolean)("HintCustomWarn"), Visibility.Collapsed, Visibility.Visible)
                FrmSetupUI.HintCustom.Text = GetLang("LangSetHomePageTipOnline")
                CustomEventService.SetEventType(FrmSetupUI.HintCustom, CustomEvent.EventType.打开网页)
                CustomEventService.SetEventData(FrmSetupUI.HintCustom, "https://github.com/Meloong-Git/XFL/discussions/2528")
            Case 3
                FrmSetupUI.PanCustomPreset.Visibility = Visibility.Visible
                FrmSetupUI.PanCustomLocal.Visibility = Visibility.Collapsed
                FrmSetupUI.PanCustomNet.Visibility = Visibility.Collapsed
                FrmSetupUI.HintCustom.Visibility = Visibility.Collapsed
                FrmSetupUI.HintCustomWarn.Visibility = Visibility.Collapsed
        End Select
        FrmSetupUI.CardCustom.TriggerForceResize()
    End Sub

    Private Sub LabLauncherTheme5Unlock_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)
    End Sub

    Private Sub BtnWinHeight_Change(sender As Object, raiseByMouse As Boolean) Handles BtnWinHeight720.Check,
            BtnWinHeight480.Check
        Try
            Dim card As MyRadioButton = TryCast(sender, MyRadioButton)
            If card Is Nothing OrElse Not card.Checked Then Return
            Dim mode As Integer = If(card.Name = "BtnWinHeight480", 1, 0)
            FormMain.ApplyWindowSizeMode(mode)
            Logger.Info($"[XFL] 用户切换窗口高度模式为 {mode}")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 切换窗口高度失败")
        End Try
    End Sub

    Friend Sub XflSyncWinHeight()
        Try
            Dim mode As Integer = Settings.Get(Of Integer)("WindowHeightMode")
            BtnWinHeight720.Checked = (mode = 0)
            BtnWinHeight480.Checked = (mode = 1)
        Catch
        End Try
    End Sub

    Private Sub XflTheme_Change(sender As Object, e As RouteEventArgs) Handles RadioXflThemeAmber.Changed,
            RadioXflThemeTeal.Changed,
            RadioXflThemeGraphite.Changed,
            RadioXflThemeDark.Changed,
            RadioXflThemeSakura.Changed,
            RadioXflThemeOcean.Changed,
            RadioXflThemeForest.Changed,
            RadioXflThemeViolet.Changed,
            RadioXflThemeMint.Changed,
            RadioXflThemeSunset.Changed
        Dim Card As MyRadioBox = TryCast(sender, MyRadioBox)
        If Card Is Nothing OrElse Not Card.Checked Then Return
        Dim Id As Integer = 20
        Select Case Card.Name
            Case "RadioXflThemeAmber" : Id = 20
            Case "RadioXflThemeTeal" : Id = 21
            Case "RadioXflThemeGraphite" : Id = 22
            Case "RadioXflThemeDark" : Id = 15
            Case "RadioXflThemeSakura" : Id = 23
            Case "RadioXflThemeOcean" : Id = 24
            Case "RadioXflThemeForest" : Id = 25
            Case "RadioXflThemeViolet" : Id = 26
            Case "RadioXflThemeMint" : Id = 27
            Case "RadioXflThemeSunset" : Id = 28
        End Select
        Try
            Settings.Set("UiLauncherTheme", Id)
            ThemeRefresh(Id)
            Logger.Info($"[XFL] 已切换到 XFL 主题 {Id}")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 切换 XFL 主题失败")
        End Try
    End Sub

    Friend Sub XflSyncThemeCards()
        Try
            Dim Id As Integer = Settings.Get(Of Integer)("UiLauncherTheme")
            RadioXflThemeAmber.Checked = (Id = 20)
            RadioXflThemeTeal.Checked = (Id = 21)
            RadioXflThemeGraphite.Checked = (Id = 22)
            RadioXflThemeDark.Checked = (Id = 15)
            RadioXflThemeSakura.Checked = (Id = 23)
            RadioXflThemeOcean.Checked = (Id = 24)
            RadioXflThemeForest.Checked = (Id = 25)
            RadioXflThemeViolet.Checked = (Id = 26)
            RadioXflThemeMint.Checked = (Id = 27)
            RadioXflThemeSunset.Checked = (Id = 28)
        Catch
        End Try
    End Sub

    Private Sub HSL_Change() Handles SliderLauncherHue.Change, SliderLauncherLight.Change, SliderLauncherSat.Change, SliderLauncherDelta.Change
        If AniControlEnabled <> 0 OrElse SliderLauncherSat Is Nothing OrElse Not SliderLauncherSat.IsLoaded Then Return
        ThemeRefresh()
    End Sub

#Region "功能隐藏"

    Private Shared _HiddenForceShow As Boolean = False

    Public Shared Property HiddenForceShow As Boolean
        Get
            Return _HiddenForceShow
        End Get
        Set(value As Boolean)
            _HiddenForceShow = value
            HiddenRefresh()
        End Set
    End Property

    Public Shared Sub HiddenRefresh() Handles Me.Loaded
        If FrmMain.PanTitleSelect Is Nothing OrElse Not FrmMain.PanTitleSelect.IsLoaded Then Return
        Try

            If Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenPageDownload") AndAlso Settings.Get(Of Boolean)("UiHiddenPageLink") AndAlso Settings.Get(Of Boolean)("UiHiddenPageSetup") AndAlso Settings.Get(Of Boolean)("UiHiddenPageOther") Then

                FrmMain.PanTitleSelect.Visibility = Visibility.Collapsed
                FrmMain.PanTaskbar.Visibility = Visibility.Collapsed
            Else

                FrmMain.PanTitleSelect.Visibility = Visibility.Collapsed
                FrmMain.PanTaskbar.Visibility = Visibility.Visible
                FrmMain.BtnTitleSelect1.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenPageDownload"), Visibility.Collapsed, Visibility.Visible)
                FrmMain.BtnTaskSelect1.Visibility = FrmMain.BtnTitleSelect1.Visibility
                FrmMain.BtnTitleSelect2.Visibility = Visibility.Collapsed
                FrmMain.BtnTitleSelect3.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenPageSetup"), Visibility.Collapsed, Visibility.Visible)
                FrmMain.BtnTaskSelect3.Visibility = FrmMain.BtnTitleSelect3.Visibility
                FrmMain.BtnTitleSelect4.Visibility = Visibility.Collapsed
                FrmMain.BtnTaskSelect4.Visibility = Visibility.Collapsed
            End If

            FrmLaunchLeft.RefreshButtonsUI()
            If FrmSetupUI IsNot Nothing Then
                FrmSetupUI.CardSwitch.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenFunctionHidden"), Visibility.Collapsed, Visibility.Visible)
            End If

            If FrmSetupLeft IsNot Nothing Then
                FrmSetupLeft.ItemLaunch.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenSetupLaunch"), Visibility.Collapsed, Visibility.Visible)
                FrmSetupLeft.ItemUI.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenSetupUi"), Visibility.Collapsed, Visibility.Visible)
                FrmSetupLeft.ItemLink.Visibility = Visibility.Collapsed
                FrmSetupLeft.ItemSystem.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenSetupSystem"), Visibility.Collapsed, Visibility.Visible)

                Dim AvaliableCount As Integer = 0
                If Not Settings.Get(Of Boolean)("UiHiddenSetupLaunch") Then AvaliableCount += 1
                If Not Settings.Get(Of Boolean)("UiHiddenSetupUi") Then AvaliableCount += 1

                If Not Settings.Get(Of Boolean)("UiHiddenSetupSystem") Then AvaliableCount += 1
                FrmSetupLeft.PanItem.Visibility = If(AvaliableCount < 2 AndAlso Not HiddenForceShow, Visibility.Collapsed, Visibility.Visible)
            End If

            Dim OtherAvaliableCount As Integer = 0
            If Not Settings.Get(Of Boolean)("UiHiddenOtherHelp") Then OtherAvaliableCount += 1
            If Not Settings.Get(Of Boolean)("UiHiddenOtherAbout") Then OtherAvaliableCount += 1
            If Not Settings.Get(Of Boolean)("UiHiddenOtherTest") Then OtherAvaliableCount += 1
            If Not Settings.Get(Of Boolean)("UiHiddenOtherFeedback") Then OtherAvaliableCount += 1
            If Not Settings.Get(Of Boolean)("UiHiddenOtherVote") Then OtherAvaliableCount += 1
            If FrmOtherLeft IsNot Nothing Then
                FrmOtherLeft.ItemHelp.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenOtherHelp"), Visibility.Collapsed, Visibility.Visible)
                FrmOtherLeft.ItemFeedback.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenOtherFeedback"), Visibility.Collapsed, Visibility.Visible)
                FrmOtherLeft.ItemVote.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenOtherVote"), Visibility.Collapsed, Visibility.Visible)
                FrmOtherLeft.ItemAbout.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenOtherAbout"), Visibility.Collapsed, Visibility.Visible)
                FrmOtherLeft.ItemTest.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenOtherTest"), Visibility.Collapsed, Visibility.Visible)

                FrmOtherLeft.PanItem.Visibility = If(OtherAvaliableCount < 2 AndAlso Not HiddenForceShow, Visibility.Collapsed, Visibility.Visible)
            End If
            If OtherAvaliableCount = 1 AndAlso Not HiddenForceShow Then
                If Not Settings.Get(Of Boolean)("UiHiddenOtherHelp") Then
                    FrmMain.BtnTitleSelect4.Text = GetLang("LangOtherHelp")
                ElseIf Not Settings.Get(Of Boolean)("UiHiddenOtherAbout") Then
                    FrmMain.BtnTitleSelect4.Text = GetLang("LangOtherAbout")
                Else
                    FrmMain.BtnTitleSelect4.Text = GetLang("LangOtherTool")
                End If
            Else
                FrmMain.BtnTitleSelect4.Text = GetLang("LangOtherMore")
            End If

            If FrmMain.PageCurrent = FormMain.PageType.InstanceSelect Then FrmSelectRight.BtnEmptyDownload_Loaded()
            If FrmMain.PageCurrent = FormMain.PageType.Launch Then FrmLaunchLeft.RefreshButtonsUI()
            If FrmMain.PageCurrent = FormMain.PageType.InstanceSetup AndAlso FrmInstanceModDisabled IsNot Nothing Then FrmInstanceModDisabled.BtnDownload_Loaded()

            If FrmSetupUI IsNot Nothing Then FrmSetupUI.CardSwitch.Title = If(HiddenForceShow, GetLang("LangHiddenModeTitleA"), GetLang("LangHiddenModeTitleB"))
        Catch ex As Exception
            Logger.Error(ex, GetLang("LangHiddenModeFail"))
        End Try
    End Sub

    Private Sub HiddenSetupMain() Handles CheckHiddenPageSetup.Change

        If CheckHiddenPageSetup.Checked Then

            CheckHiddenSetupLaunch.Checked = True
            CheckHiddenSetupSystem.Checked = True
            CheckHiddenSetupLink.Checked = True
            CheckHiddenSetupUI.Checked = True
        Else

            If Settings.Get(Of Boolean)("UiHiddenSetupLaunch") AndAlso Settings.Get(Of Boolean)("UiHiddenSetupUi") AndAlso Settings.Get(Of Boolean)("UiHiddenSetupSystem") AndAlso Settings.Get(Of Boolean)("UiHiddenSetupLink") Then
                CheckHiddenSetupLaunch.Checked = False
                CheckHiddenSetupSystem.Checked = False
                CheckHiddenSetupLink.Checked = False
                CheckHiddenSetupUI.Checked = False
            End If
        End If
    End Sub
    Private Sub HiddenSetupSub() Handles CheckHiddenSetupLaunch.Change, CheckHiddenSetupSystem.Change, CheckHiddenSetupLink.Change, CheckHiddenSetupUI.Change

        If Settings.Get(Of Boolean)("UiHiddenSetupLaunch") AndAlso Settings.Get(Of Boolean)("UiHiddenSetupUi") AndAlso Settings.Get(Of Boolean)("UiHiddenSetupSystem") AndAlso Settings.Get(Of Boolean)("UiHiddenSetupLink") Then

            CheckHiddenPageSetup.Checked = True
        Else

            CheckHiddenPageSetup.Checked = False
        End If
    End Sub
    Private Sub HiddenOtherMain() Handles CheckHiddenPageOther.Change

        If CheckHiddenPageOther.Checked Then

            CheckHiddenOtherAbout.Checked = True
            CheckHiddenOtherTest.Checked = True
            CheckHiddenOtherFeedback.Checked = True
            CheckHiddenOtherVote.Checked = True
            CheckHiddenOtherHelp.Checked = True
        Else

            If Settings.Get(Of Boolean)("UiHiddenOtherHelp") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherAbout") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherTest") AndAlso
                Settings.Get(Of Boolean)("UiHiddenOtherVote") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherFeedback") Then
                CheckHiddenOtherAbout.Checked = False
                CheckHiddenOtherTest.Checked = False
                CheckHiddenOtherFeedback.Checked = False
                CheckHiddenOtherVote.Checked = False
                CheckHiddenOtherHelp.Checked = False
            End If
        End If
    End Sub
    Private Sub HiddenOtherSub(sender As Object, user As Boolean) Handles CheckHiddenOtherHelp.Change, CheckHiddenOtherAbout.Change, CheckHiddenOtherTest.Change

        If Settings.Get(Of Boolean)("UiHiddenOtherHelp") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherAbout") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherTest") Then

            CheckHiddenPageOther.Checked = True
        Else

            CheckHiddenPageOther.Checked = False
        End If

        If Not user Then Return
        If Settings.Get(Of Boolean)("UiHiddenOtherHelp") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherAbout") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherTest") Then
            CheckHiddenOtherFeedback.Checked = True
            CheckHiddenOtherVote.Checked = True
        End If
    End Sub
    Private Sub HiddenOtherNet(sender As Object, user As Boolean) Handles CheckHiddenOtherFeedback.Change, CheckHiddenOtherVote.Change

        If Not user Then Return
        If Settings.Get(Of Boolean)("UiHiddenOtherHelp") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherAbout") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherTest") AndAlso
            (Not Settings.Get(Of Boolean)("UiHiddenOtherFeedback") OrElse Not Settings.Get(Of Boolean)("UiHiddenOtherVote")) Then
            CheckHiddenOtherAbout.Checked = False
            CheckHiddenOtherTest.Checked = False
            CheckHiddenOtherHelp.Checked = False
        End If
    End Sub

    Private Sub HiddenHint(sender As Object, user As Boolean) Handles CheckHiddenFunctionHidden.Change, CheckHiddenPageSetup.Change, CheckHiddenSetupUI.Change
        If AniControlEnabled = 0 AndAlso sender.Checked Then Hint(GetLang("LangHintHiddenModeTip"))
    End Sub

#End Region

    Private Sub SliderLoad()
        SliderMusicVolume.GetHintText = Function(v) Math.Ceiling(v * 0.1) & "%"
        SliderLauncherTransparent.GetHintText = Function(v) Math.Round(40 + v * 0.1) & "%"
        SliderLauncherHue.GetHintText = Function(v) v & "°"
        SliderLauncherSat.GetHintText = Function(v) v & "%"
        SliderLauncherDelta.GetHintText =
        Function(Value As Integer) As String
            If Value > 90 Then
                Return "+" & (Value - 90)
            ElseIf Value = 90 Then
                Return 0
            Else
                Return Value - 90
            End If
        End Function
        SliderLauncherLight.GetHintText =
        Function(Value As Integer) As String
            If Value > 20 Then
                Return "+" & (Value - 20)
            ElseIf Value = 20 Then
                Return 0
            Else
                Return Value - 20
            End If
        End Function
        SliderBackgroundOpacity.GetHintText = Function(v) Math.Round(v * 0.1) & "%"
        SliderBackgroundBlur.GetHintText = Function(v) v & " " & GetLang("LangSetupUIBackgroundPicPixel")
    End Sub

End Class
