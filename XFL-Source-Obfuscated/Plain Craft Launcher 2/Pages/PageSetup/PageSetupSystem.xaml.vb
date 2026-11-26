Public Class PageSetupSystem

#Region "语言"
    Private Sub SelectCurrentLanguage()
        For i As Integer = 0 To ComboLanguage.Items.Count - 1
            Dim item As MyComboBoxItem = CType(ComboLanguage.Items(i), MyComboBoxItem)
            If item.Tag.Equals(Lang) Then
                ComboLanguage.SelectedIndex = i
                Exit For
            End If
        Next
    End Sub

    Private Sub RefreshLang() Handles ComboLanguage.SelectionChanged
        If Not IsLoaded Or Not ComboLanguage.IsLoaded Then Exit Sub
        Dim TargetLang As String = CType(ComboLanguage.SelectedItem, MyComboBoxItem).Tag
        If TargetLang.Equals(Lang) Then Exit Sub
        If HasRunningMinecraft OrElse McLaunchLoader.State = LoadState.Loading Then
            Hint(GetLang("LangPageSetupSystemHintCloseGameBeforeChangeLanguage"))
            SelectCurrentLanguage()
            Exit Sub
        End If
        If HasDownloadingTask() Then
            Hint(GetLang("LangPageSetupSystemHintFinishDownloadTaskBeforeChangeLanguage"))
            SelectCurrentLanguage()
            Exit Sub
        End If
        Lang = TargetLang
        Settings.Set("SystemLang", Lang)
        Application.Current.Resources.MergedDictionaries(1) = New ResourceDictionary With {.Source = New Uri("pack://application:,,,/Resources/Language/" & Lang & ".xaml", UriKind.RelativeOrAbsolute)}
        If Lang.Equals("zh-MEME") Then MyMsgBox(GetLang("LangPageSetupSystemDialogContentEntertainment"), IsWarn:=True)
        MyMsgBox(GetLang("LangPageSetupSystemDialogContentLanguageRestart"), ForceWait:=True)
        Process.Start(New ProcessStartInfo(PathExe))
        FormMain.EndProgramForce()
    End Sub

    Private Sub HelpTranslate(sender As Object, e As EventArgs) Handles BtnHelpTranslate.Click
        OpenWebsite("https://translate.PCLc.cc/engage/PCL-language/")
    End Sub
#End Region

    Private Shadows IsLoaded As Boolean = False

    Private Sub LoadAiSettings()
        Try
            If TextAiUrl Is Nothing Then Return
            TextAiUrl.Text = Settings.Get(Of String)("AiApiUrl")
            TextAiKey.Text = Settings.Get(Of String)("AiApiKey")
            TextAiModel.Text = Settings.Get(Of String)("AiModel")
        Catch ex As Exception
            Logger.Error(ex, "[XFL] AI 配置载入失败")
        End Try
    End Sub

    Private Sub BtnAiModelRefresh_Click(sender As Object, e As RoutedEventArgs)
        Try
            If BtnAiModelRefresh IsNot Nothing Then BtnAiModelRefresh.IsEnabled = False
            If HintAiInfo IsNot Nothing Then HintAiInfo.Text = "正在获取模型列表…"

            Settings.Set("AiApiUrl", If(TextAiUrl.Text, "").Trim())
            Settings.Set("AiApiKey", If(TextAiKey.Text, "").Trim())
            RunInNewThread(
                Sub()
                    Dim models As List(Of String) = ModAi.FetchModels()
                    RunInUi(
                        Sub()
                            Try
                                If models IsNot Nothing AndAlso models.Count > 0 Then
                                    TextAiModel.Text = models(0)
                                    HintAiInfo.Text = "已获取到 " & models.Count & " 个模型：" & String.Join("、", models) &
                                                      "。已自动填入第一个，可自行改成其它名字。"
                                    Logger.Info("[XFL] 模型列表：" & String.Join(", ", models))
                                Else
                                    HintAiInfo.Text = "未能获取模型列表（接口不支持 /models 或网络不可达）。免费公开接口可用的推理模型一般填 openai-fast。"
                                End If
                            Catch ex As Exception
                                HintAiInfo.Text = "回填模型列表失败：" & ex.Message
                            Finally
                                If BtnAiModelRefresh IsNot Nothing Then BtnAiModelRefresh.IsEnabled = True
                            End Try
                        End Sub)
                End Sub)
        Catch ex As Exception
            Hint("获取模型列表失败：" & ex.Message, HintType.Red)
            If BtnAiModelRefresh IsNot Nothing Then BtnAiModelRefresh.IsEnabled = True
        End Try
    End Sub

    Private Sub BtnAiSave_Click(sender As Object, e As RoutedEventArgs)
        Try
            Settings.Set("AiApiUrl", If(TextAiUrl.Text, "").Trim())
            Settings.Set("AiApiKey", If(TextAiKey.Text, "").Trim())
            Settings.Set("AiModel", If(TextAiModel.Text, "").Trim())
            Hint("AI 设置已保存！", HintType.Green)
            Logger.Info("[XFL] AI 配置已保存：" & Settings.Get(Of String)("AiApiUrl"))
        Catch ex As Exception
            Hint("保存 AI 设置失败：" & ex.Message, HintType.Red)
        End Try
    End Sub

    Private Sub BtnAiClear_Click(sender As Object, e As RoutedEventArgs)
        Try
            ModAi.AiClearHistory()
            If FrmLaunchLeft IsNot Nothing Then FrmLaunchLeft.ClearAiChat()
            Hint("AI 对话记录已清空！", HintType.Green)
        Catch ex As Exception
            Hint("清空失败：" & ex.Message, HintType.Red)
        End Try
    End Sub

    Private Sub RefreshTaskbarAlign() Handles ComboTaskbarAlign.SelectionChanged
        Try
            Dim idx As Integer = ComboTaskbarAlign.SelectedIndex
            If idx >= 0 Then Settings.Set("UiTaskbarAlign", idx)
        Catch ex As Exception
            Logger.Error(ex, "[XFL] 任务栏对齐设置保存失败")
        End Try
    End Sub

    Private Sub PageSetupSystem_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        Try
            ComboTaskbarAlign.SelectedIndex = Settings.Get(Of Integer)("UiTaskbarAlign")
        Catch ex As Exception
            ComboTaskbarAlign.SelectedIndex = 1
        End Try

        LoadAiSettings()
        If BtnAiSave IsNot Nothing Then AddHandler BtnAiSave.Click, AddressOf BtnAiSave_Click
        If BtnAiModelRefresh IsNot Nothing Then AddHandler BtnAiModelRefresh.Click, AddressOf BtnAiModelRefresh_Click
        If BtnAiClear IsNot Nothing Then AddHandler BtnAiClear.Click, AddressOf BtnAiClear_Click

        PanBack.ScrollToHome()

        If BuildType = BuildTypes.Release Then
            PanDonate.Visibility = Visibility.Collapsed
        Else
            PanDonate.Visibility = Visibility.Visible
            ItemSystemUpdateDownload.Content = GetLang("LangPageSetupSystemSystemLaunchUpdateE")
        End If

        SelectCurrentLanguage()

        If IsLoaded Then Return
        IsLoaded = True

        AniControlEnabled += 1
        Reload()
        SliderLoad()
        AniControlEnabled -= 1

    End Sub
    Public Sub Reload()
        SettingService.RefreshSettings(Me)
    End Sub
    Public Sub Reset()
        Try
            SettingService.ResetSettings(Me)
            Logger.Info(GetLang("LangPageSetupSystemLaunchResetSuccess"))
            Hint(GetLang("LangPageSetupSystemLaunchResetSuccess"), HintType.Green, False)
        Catch ex As Exception
            Logger.Error(ex, GetLang("LangPageSetupSystemLaunchResetFail"), LogBehavior.Alert)
        End Try
        Reload()
    End Sub

    Private Sub SliderLoad()
        SliderDownloadThread.GetHintText = Function(v) v + 1
        SliderDownloadSpeed.GetHintText =
        Function(v)
            Select Case v
                Case Is <= 14
                    Return (v + 1) * 0.1 & " M/s"
                Case Is <= 31
                    Return (v - 11) * 0.5 & " M/s"
                Case Is <= 41
                    Return (v - 21) & " M/s"
                Case Else
                    Return GetLang("LangPageSetupSystemDownloadSpeedUnlimited")
            End Select
        End Function
        SliderDebugAnim.GetHintText = Function(v) If(v > 29, GetLang("LangPageSetupSystemDebugAnimSpeedDisable"), (v / 10 + 0.1) & "x")
    End Sub
    Private Sub SliderDownloadThread_PreviewChange(sender As Object, e As RouteEventArgs) Handles SliderDownloadThread.PreviewChange
        If SliderDownloadThread.Value < 100 Then Return
        If Not Settings.Get(Of Boolean)("HintDownloadThread") Then
            Settings.Set("HintDownloadThread", True)
            MyMsgBox(GetLang("LangPageSetupSystemDownloadSpeedDialogThreadTooMuchContent"), GetLang("LangDialogTitleWarning"), GetLang("LangDialogBtnIC"), IsWarn:=True)
        End If
    End Sub

    Private Sub BtnSystemIdentify_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSystemIdentify.Click
        PageOtherAbout.CopyIdentify()
    End Sub
    Private Sub BtnSystemUnlock_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSystemUnlock.Click
        InputPotatoCode(False)
    End Sub

    Private Sub CheckDebugMode_Change() Handles CheckDebugMode.Change
        If AniControlEnabled = 0 Then Hint(GetLang("LangPageSetupSystemDebugNeedRestart"),, False)
    End Sub

    Private Sub ComboSystemActivity_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboSystemActivity.SelectionChanged
        If AniControlEnabled <> 0 Then Return
        If ComboSystemActivity.SelectedIndex <> 2 Then Return
        If MyMsgBox(GetLang("LangPageSetupSystemLaunchDialogAnnouncementSilentContent"), GetLang("LangDialogTitleWarning"), GetLang("LangPageSetupSystemLaunchDialogAnnouncementBtnConfirm"), GetLang("LangDialogBtnCancel"), IsWarn:=True) = 2 Then
            If e.RemovedItems.Count > 0 Then ComboSystemActivity.SelectedItem = e.RemovedItems(0)
        End If
    End Sub
    Private Sub ComboSystemUpdate_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboSystemUpdate.SelectionChanged
        If AniControlEnabled <> 0 Then Return
        If ComboSystemUpdate.SelectedIndex <> 3 Then Return
        If MyMsgBox(GetLang("LangPageSetupSystemLaunchDialogAnnouncementDisableContent"), GetLang("LangDialogTitleWarning"), GetLang("LangPageSetupSystemLaunchDialogAnnouncementBtnConfirm"), GetLang("LangDialogBtnCancel"), IsWarn:=True) = 2 Then
            If e.RemovedItems.Count > 0 Then ComboSystemUpdate.SelectedItem = e.RemovedItems(0)
        End If
    End Sub
    Private Sub BtnSystemUpdate_Click(sender As Object, e As EventArgs) Handles BtnSystemUpdate.Click
        UpdateCheckByButton()
    End Sub

    Public Shared Function IsLauncherNewest() As Boolean?
        Try
            If ModSecret.LatestReleaseInfoJson Is Nothing Then Return Nothing
            Return ModSecret.LatestVersion = VersionBaseName
        Catch ex As Exception
            Logger.Error(ex, GetLang("LangPageSetupSystemSystemLaunchUpdateFail"))
            Return Nothing
        End Try
    End Function

#Region "导出 / 导入设置"

    Private Sub BtnSystemSettingExp_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSystemSettingExp.Click
        Hint(GetLang("LangPageSetupSystemInDev"))
    End Sub
    Private Sub BtnSystemSettingImp_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSystemSettingImp.Click
        Hint(GetLang("LangPageSetupSystemInDev"))
    End Sub

#End Region

End Class
