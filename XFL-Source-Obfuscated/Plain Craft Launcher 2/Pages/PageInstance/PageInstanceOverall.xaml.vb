Public Class PageInstanceOverall

    Private IsLoad As Boolean = False
    Private Sub PageSetupLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        PanBack.ScrollToHome()

        Reload()

        If IsLoad Then Return
        IsLoad = True
        PanDisplay.TriggerForceResize()

    End Sub

    Public InstanceHeaderItem As MyListItem

    Private Sub Reload()
        AniControlEnabled += 1

        ComboDisplayType.SelectedIndex = ReadIni(PageInstanceLeft.Instance.PathVersion & "XFL\Setup.ini", "DisplayType", McInstanceCardType.Auto)
        BtnDisplayStar.Text = If(PageInstanceLeft.Instance.IsStar, GetLang("LangPageVersionOverallCancelFavorite"), GetLang("LangPageVersionOverallFavorite"))
        BtnFolderMods.Visibility = If(PageInstanceLeft.Instance.Modable, Visibility.Visible, Visibility.Collapsed)

        PanDisplayItem.Children.Clear()
        InstanceHeaderItem = PageInstanceLeft.Instance.ToListItem().Init()
        InstanceHeaderItem.IsHitTestVisible = False
        PanDisplayItem.Children.Add(InstanceHeaderItem)
        FrmMain.PageNameRefresh()

        ComboDisplayLogo.SelectedIndex = 0
        Dim Logo As String = ReadIni(PageInstanceLeft.Instance.PathVersion & "XFL\Setup.ini", "Logo", "")
        Dim LogoCustom As Boolean = ReadIni(PageInstanceLeft.Instance.PathVersion & "XFL\Setup.ini", "LogoCustom", "False")
        If LogoCustom Then
            For Each Selection As MyComboBoxItem In ComboDisplayLogo.Items
                If Selection.Tag = Logo OrElse (Selection.Tag = "XFL\Logo.png" AndAlso Logo.EndsWithF("XFL\Logo.png")) Then
                    ComboDisplayLogo.SelectedItem = Selection
                    Exit For
                End If
            Next
        End If

        AniControlEnabled -= 1
    End Sub

#Region "卡片：个性化"

    Private Sub ComboDisplayType_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboDisplayType.SelectionChanged
        If Not (IsLoad AndAlso AniControlEnabled = 0) Then Return
        If ComboDisplayType.SelectedIndex <> 1 Then

            Try

                WriteIni(PageInstanceLeft.Instance.PathVersion & "XFL\Setup.ini", "DisplayType", ComboDisplayType.SelectedIndex)
                PageInstanceLeft.Instance.DisplayType = ReadIni(PageInstanceLeft.Instance.PathVersion & "XFL\Setup.ini", "DisplayType", McInstanceCardType.Auto)
                FrmInstanceLeft.RefreshModDisabled()

                WriteIni(McFolderSelected & "XFL.ini", "InstanceCache", "")
                LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
            Catch ex As Exception
                Logger.Error(ex, $"修改版本分类失败（{PageInstanceLeft.Instance.Name}）")
            End Try
            Reload()
        Else

            Try
                If Not Settings.Get(Of Boolean)("HintHide") Then
                    If MyMsgBox(GetLang("LangPageVersionOverallDialogHideInstanceContent"), GetLang("LangPageVersionOverallDialogHideInstanceTitle"),, GetLang("LangDialogBtnCancel")) <> 1 Then
                        ComboDisplayType.SelectedIndex = 0
                        Return
                    End If
                    Settings.Set("HintHide", True)
                End If
                WriteIni(PageInstanceLeft.Instance.PathVersion & "XFL\Setup.ini", "DisplayType", McInstanceCardType.Hidden)
                WriteIni(McFolderSelected & "XFL.ini", "InstanceCache", "")
                LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
            Catch ex As Exception
                Logger.Error(ex, $"隐藏版本 {PageInstanceLeft.Instance.Name} 失败")
            End Try
        End If
    End Sub

    Private Sub BtnDisplayDesc_Click(sender As Object, e As EventArgs) Handles BtnDisplayDesc.Click
        Try
            Dim OldInfo As String = ReadIni(PageInstanceLeft.Instance.PathVersion & "XFL\Setup.ini", "CustomInfo")
            Dim NewInfo As String = MyMsgBoxInput(GetLang("LangPageVersionOverallDialogEditDescTitle"), GetLang("LangPageVersionOverallDialogEditDescContent"), OldInfo, New ObjectModel.Collection(Of Validate), GetLang("LangPageVersionOverallDialogEditDescHint"))
            If NewInfo IsNot Nothing AndAlso OldInfo <> NewInfo Then WriteIni(PageInstanceLeft.Instance.PathVersion & "XFL\Setup.ini", "CustomInfo", NewInfo)
            PageInstanceLeft.Instance = New McInstance(PageInstanceLeft.Instance.Name).Load()
            Reload()
            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
        Catch ex As Exception
            Logger.Error(ex, $"版本 {PageInstanceLeft.Instance.Name} 描述更改失败", LogBehavior.Alert)
        End Try
    End Sub

    Private Sub BtnDisplayRename_Click(sender As Object, e As EventArgs) Handles BtnDisplayRename.Click
        Try

            Dim OldName As String = PageInstanceLeft.Instance.Name
            Dim OldPath As String = PageInstanceLeft.Instance.PathVersion

            Dim NewName As String = MyMsgBoxInput(GetLang("LangPageVersionOverallDialogEditNameTitle"), "", OldName, New ObjectModel.Collection(Of Validate) From {
                New ValidateFolderName(McFolderSelected & "versions", IgnoreCase:=False)})
            If String.IsNullOrWhiteSpace(NewName) Then Return
            Dim NewPath As String = McFolderSelected & "versions\" & NewName & "\"

            Dim JsonObject As JObject
            Dim OldJsonPath As String = PageInstanceLeft.Instance.GetJsonPath()
            Try
                JsonObject = FileUtils.ReadAsJson(OldJsonPath)
            Catch ex As Exception
                Logger.Warn(ex, "在重命名读取 JSON 时失败")
                JsonObject = PageInstanceLeft.Instance.JsonObject
            End Try

            DirectoryUtils.Move(OldPath, NewPath)

            PageInstanceLeft.Instance.ResetSettingsCache()

            If DirectoryUtils.Exists($"{NewPath}{OldName}-natives") Then DirectoryUtils.Move($"{NewPath}{OldName}-natives", $"{NewPath}{NewName}-natives")
            If FileUtils.Exists($"{NewPath}{OldName}.jar") Then FileUtils.Move($"{NewPath}{OldName}.jar", $"{NewPath}{NewName}.jar")

            If FileUtils.Exists(NewPath & "XFL\Setup.ini") Then
                FileUtils.Write(NewPath & "XFL\Setup.ini", FileUtils.ReadAsString(NewPath & "XFL\Setup.ini").Replace(OldPath, NewPath))
            End If

            If ReadIni(McFolderSelected & "XFL.ini", "Version") = OldName Then
                WriteIni(McFolderSelected & "XFL.ini", "Version", NewName)
            End If

            Try
                JsonObject("id") = NewName
                FileUtils.Delete(NewPath & PathUtils.GetLastPart(OldJsonPath))
                FileUtils.Write(NewPath & NewName & ".json", JsonObject.ToString)
            Catch ex As Exception
                Logger.Warn(ex, "重命名版本 json 失败")
            End Try

            Hint(GetLang("LangPageVersionOverallHintEditNameSuccess"), HintType.Green)
            PageInstanceLeft.Instance = New McInstance(NewName).Load()
            If Not IsNothing(McInstanceSelected) AndAlso McInstanceSelected.Equals(PageInstanceLeft.Instance) Then WriteIni(McFolderSelected & "XFL.ini", "Version", NewName)
            Reload()
            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
        Catch ex As Exception
            Logger.Error(ex, GetLang("LangPageVersionOverallEditNameFail"), LogBehavior.Alert)
        End Try
    End Sub

    Private Sub ComboDisplayLogo_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboDisplayLogo.SelectionChanged
        If Not (IsLoad AndAlso AniControlEnabled = 0) Then Return

        Try
            If ComboDisplayLogo.SelectedItem Is ItemDisplayLogoCustom Then
                Dim FileName As String = Dialogs.SelectFile(GetLang("LangDialogSelectImage"), False, filter:={({"png", "jpeg", "jpg", "gif", "webp"}, GetLang("LangDialogImageFileFilter"))}).FirstOrDefault()
                If String.IsNullOrEmpty(FileName) Then
                    Reload()
                    Return
                End If
                FileUtils.Copy(FileName, PageInstanceLeft.Instance.PathVersion & "XFL\Logo.png")
            Else
                FileUtils.Delete(PageInstanceLeft.Instance.PathVersion & "XFL\Logo.png")
            End If
        Catch ex As Exception
            Logger.Error(ex, $"更改自定义版本图标失败（{PageInstanceLeft.Instance.Name}）")
        End Try

        Try
            Dim NewLogo As String = ComboDisplayLogo.SelectedItem.Tag
            WriteIni(PageInstanceLeft.Instance.PathVersion & "XFL\Setup.ini", "Logo", NewLogo)
            WriteIni(PageInstanceLeft.Instance.PathVersion & "XFL\Setup.ini", "LogoCustom", Not NewLogo = "")

            WriteIni(McFolderSelected & "XFL.ini", "InstanceCache", "")
            PageInstanceLeft.Instance = New McInstance(PageInstanceLeft.Instance.Name).Load()
            Reload()
            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
        Catch ex As Exception
            Logger.Error(ex, $"更改版本图标失败（{PageInstanceLeft.Instance.Name}）")
        End Try
    End Sub

    Private Sub BtnDisplayStar_Click(sender As Object, e As EventArgs) Handles BtnDisplayStar.Click
        Try
            WriteIni(PageInstanceLeft.Instance.PathVersion & "XFL\Setup.ini", "IsStar", Not PageInstanceLeft.Instance.IsStar)
            PageInstanceLeft.Instance = New McInstance(PageInstanceLeft.Instance.Name).Load()
            Reload()
            McInstanceListForceRefresh = True
            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
        Catch ex As Exception
            Logger.Error(ex, $"版本 {PageInstanceLeft.Instance.Name} 收藏状态更改失败", LogBehavior.Alert)
        End Try
    End Sub

#End Region

#Region "卡片：快捷方式"

    Private Sub BtnFolderVersion_Click() Handles BtnFolderVersion.Click
        OpenInstanceFolder(PageInstanceLeft.Instance)
    End Sub
    Public Shared Sub OpenInstanceFolder(Instance As McInstance)
        OpenExplorer(Instance.PathVersion)
    End Sub

    Private Sub BtnFolderSaves_Click() Handles BtnFolderSaves.Click
        Dim Folder As String = PageInstanceLeft.Instance.PathIndie & "saves\"
        DirectoryUtils.Create(Folder)
        OpenExplorer(Folder)
    End Sub

    Private Sub BtnFolderMods_Click() Handles BtnFolderMods.Click
        Dim Folder As String = PageInstanceLeft.Instance.PathIndie & "mods\"
        DirectoryUtils.Create(Folder)
        OpenExplorer(Folder)
    End Sub

#End Region

#Region "卡片：管理"

    Private Sub BtnManageScript_Click() Handles BtnManageScript.Click
        Try

            Dim SavePath As String = Dialogs.SaveFile(GetLang("LangPageVersionOverallSelectSaveCommandFile"), "启动 " & PageInstanceLeft.Instance.Name & ".bat", filter:={("bat", GetLang("LangPageVersionOverallBatchFileFilter"))})
            If SavePath Is Nothing Then Return

            If McLaunchLoader.State = LoadState.Loading Then
                Hint(GetLang("LangPageVersionOverallHintWaitForTaskOver"), HintType.Red)
                Return
            End If

            If McLaunchStart(New McLaunchOptions With {.SaveBatch = SavePath, .Instance = PageInstanceLeft.Instance}) Then
                If Settings.Get(Of McLoginType)("LoginType") = McLoginType.Legacy Then
                    Hint(GetLang("LangPageVersionOverallHintExportingCommandA"))
                Else
                    Hint(GetLang("LangPageVersionOverallHintExportingCommandB"))
                End If
            End If
        Catch ex As Exception
            Logger.Error(ex, GetLang("LangPageVersionOverallHintExportingCommandFail") & "（" & PageInstanceLeft.Instance.Name & "）", LogBehavior.Alert)
        End Try
    End Sub

    Private Sub BtnManageCheck_Click(sender As Object, e As EventArgs) Handles BtnManageCheck.Click
        Try

            If ShouldIgnoreFileCheck(PageInstanceLeft.Instance) Then
                Hint(GetLang("LangPageVersionOverallHintEnableInstanceAssetsCheck"), HintType.Blue)
                Return
            End If

            For Each OngoingLoader In LoaderTaskbar
                If OngoingLoader.Name <> PageInstanceLeft.Instance.Name & " " & GetLang("LangPageVersionOverallTaskCompleteFile") Then Continue For
                Hint(GetLang("LangPageVersionOverallCompleteFileInTask"), HintType.Red)
                Return
            Next

            Dim Loader As New LoaderCombo(Of String)(PageInstanceLeft.Instance.Name & " " & GetLang("LangPageVersionOverallTaskCompleteFile"), DlClientFix(PageInstanceLeft.Instance, True, False))
            Loader.OnStateChanged =
            Sub()
                Select Case Loader.State
                    Case LoadState.Finished
                        Hint(Loader.Name & GetLang("LangPageVersionOverallCompleteFileSuccess"), HintType.Green)
                    Case LoadState.Failed
                        Hint(Loader.Name & GetLang("LangPageVersionOverallCompleteFileFail") & Loader.Error.GetDisplay(False), HintType.Red)
                    Case LoadState.Canceled
                        Hint(Loader.Name & GetLang("LangTaskAbort"), HintType.Blue)
                End Select
            End Sub
            Loader.Start(PageInstanceLeft.Instance.Name)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()
        Catch ex As Exception
            Logger.Error(ex, $"尝试补全文件失败（{PageInstanceLeft.Instance.Name}）", LogBehavior.Alert)
        End Try
    End Sub

    Private Sub BtnManageDelete_Click(sender As Object, e As EventArgs) Handles BtnManageDelete.Click
        Try
            Dim IsShiftPressed As Boolean = My.Computer.Keyboard.ShiftKeyDown
            Dim IsHintIndie As Boolean = PageInstanceLeft.Instance.State <> McInstanceState.Error AndAlso PageInstanceLeft.Instance.PathIndie <> McFolderSelected
            Dim MsgContent = ""
            If IsShiftPressed Then
                MsgContent = GetLang("LangPageVersionOverallDialogDeleteA", PageInstanceLeft.Instance.Name)
            Else
                MsgContent = GetLang("LangPageVersionOverallDialogDeleteB", PageInstanceLeft.Instance.Name)
            End If
            Select Case MyMsgBox(MsgContent,
                        GetLang("LangPageVersionOverallDialogDeleteTitle"), , GetLang("LangDialogBtnCancel"),, IsHintIndie OrElse IsShiftPressed)
                Case 1
                    PageInstanceLeft.Instance.ResetSettingsCache()
                    DirectoryUtils.Delete(PageInstanceLeft.Instance.PathVersion, Not IsShiftPressed)
                    Hint( GetLang("LangPageVersionOverallHintDeleteSuccess", PageInstanceLeft.Instance.Name), HintType.Green)
                Case 2
                    Return
            End Select
            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
            FrmMain.PageBack()
        Catch ex As OperationCanceledException
            Logger.Warn(ex, $"删除版本 {PageInstanceLeft.Instance.Name} 被主动取消")
        Catch ex As Exception
            Logger.Error(ex, $"删除版本 {PageInstanceLeft.Instance.Name} 失败", LogBehavior.Alert)
        End Try
    End Sub

#End Region

End Class
