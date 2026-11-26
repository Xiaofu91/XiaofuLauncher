
Imports System.Threading.Tasks

Friend Module ModSecret

    Public Const VersionBranchMain As String = "OpenSource"

    Public Const RegFolder As String = "PCLang"
#If Not Release Then

    Public OAuthClientId As String = If(Environment.GetEnvironmentVariable("PCL_MS_CLIENT_ID"), "")

    Public CurseForgeAPIKey As String = If(Environment.GetEnvironmentVariable("PCL_CURSEFORGE_API_KEY"), "")
#End If
#If Release Then

    Public OAuthClientId As String = If(Environment.GetEnvironmentVariable("PCL_MS_CLIENT_ID"), "c36a9fb6-4f2a-41ff-90bd-ae7cc92031eb")
    Public Const CurseForgeAPIKey As String = ""
#End If

    Public Const ClsBaseUrl As String = ""

#Region "网络鉴权"

    Friend Function SecretCdnSign(UrlWithMark As String) As String
        If Not UrlWithMark.EndsWithF("{CDN}") Then Return UrlWithMark
        UrlWithMark = Uri.EscapeUriString(UrlWithMark)
        Return UrlWithMark.Replace("{CDN}", "")
    End Function

    Friend Sub SecretHeadersSign(Url As String, ByRef Req As HttpRequestMessage, Optional SimulateBrowserHeaders As Boolean = False)
        If Not Req.Headers.UserAgent.Any Then
            If Url.Contains("baidupcs.com") OrElse Url.Contains("baidu.com") Then
                Req.Headers.Add("User-Agent", "LogStatistic")
            ElseIf SimulateBrowserHeaders Then
                Req.Headers.Add("User-Agent", $"XFL/{VersionBaseName}.{CInt(BuildType)} Mozilla/5.0 AppleWebKit/537.36 Chrome/63.0.3239.132 Safari/537.36")
            Else
                Req.Headers.Add("User-Agent", $"XFL/{VersionBaseName}.{CInt(BuildType)}")
            End If
        End If
        If Not SimulateBrowserHeaders Then Req.Headers.Add("Referer", $"http://{VersionCode}.open.XFL.server/")
        If Url.Contains("api.curseforge.com") OrElse Url.Contains("forgecdn.net") Then Req.Headers.Add("x-api-key", CurseForgeAPIKey)
    End Sub

#End Region

#Region "主题"

    Public Color1 As New MyColor(52, 61, 74)
    Public Color2 As New MyColor(11, 91, 203)
    Public Color3 As New MyColor(19, 112, 243)
    Public Color4 As New MyColor(72, 144, 245)
    Public Color5 As New MyColor(150, 192, 249)
    Public Color6 As New MyColor(213, 230, 253)
    Public Color7 As New MyColor(222, 236, 253)
    Public Color8 As New MyColor(234, 242, 254)
    Public ColorBg0 As New MyColor(150, 192, 249)
    Public ColorBg1 As New MyColor(190, Color7)
    Public ColorGray1 As New MyColor(64, 64, 64)
    Public ColorGray2 As New MyColor(115, 115, 115)
    Public ColorGray3 As New MyColor(140, 140, 140)
    Public ColorGray4 As New MyColor(166, 166, 166)
    Public ColorGray5 As New MyColor(204, 204, 204)
    Public ColorGray6 As New MyColor(235, 235, 235)
    Public ColorGray7 As New MyColor(240, 240, 240)
    Public ColorGray8 As New MyColor(245, 245, 245)
    Public ColorSemiTransparent As New MyColor(1, Color8)

    Public ThemeNow As Integer = -1
    Public ColorHue As Integer = 210, ColorSat As Integer = 85, ColorLightAdjust As Integer = 0, ColorHueTopbarDelta As OneOf(Of Integer, Integer()) = 0
    Public ThemeDontClick As Integer = 0

    Public Sub ThemeRefresh(Optional NewTheme As Integer = -1)
        Try
            If ThemeNow = NewTheme AndAlso NewTheme >= 0 Then Return
            If NewTheme >= 0 Then ThemeNow = NewTheme

            If NewTheme >= 0 Then
                ThemeDarkMode = False
                Select Case NewTheme
                    Case 0
                        ColorHue = 210 : ColorSat = 85 : ColorLightAdjust = 0
                    Case 1
                        ColorHue = 180 : ColorSat = 80 : ColorLightAdjust = 0
                    Case 2
                        ColorHue = 145 : ColorSat = 72 : ColorLightAdjust = 0
                    Case 3
                        ColorHue = 45 : ColorSat = 90 : ColorLightAdjust = 0
                    Case 4
                        ColorHue = 25 : ColorSat = 55 : ColorLightAdjust = 0
                    Case 5
                        ColorHue = 210 : ColorSat = 4 : ColorLightAdjust = 0
                    Case 6
                        ColorHue = 330 : ColorSat = 80 : ColorLightAdjust = 0
                    Case 12
                        ColorHue = 280 : ColorSat = 85 : ColorLightAdjust = 0
                    Case 13
                        ColorHue = 40 : ColorSat = 95 : ColorLightAdjust = 2
                    Case 20
                        ColorHue = 30 : ColorSat = 78 : ColorLightAdjust = 0
                    Case 21
                        ColorHue = 168 : ColorSat = 62 : ColorLightAdjust = 0
                    Case 22
                        ColorHue = 210 : ColorSat = 6 : ColorLightAdjust = 0
                    Case 15
                        ColorHue = 210 : ColorSat = 85 : ColorLightAdjust = 0
                        ThemeDarkMode = True
                    Case 23
                        ColorHue = 340 : ColorSat = 70 : ColorLightAdjust = 0
                    Case 24
                        ColorHue = 200 : ColorSat = 80 : ColorLightAdjust = 0
                    Case 25
                        ColorHue = 120 : ColorSat = 60 : ColorLightAdjust = 0
                    Case 26
                        ColorHue = 270 : ColorSat = 75 : ColorLightAdjust = 0
                    Case 27
                        ColorHue = 155 : ColorSat = 55 : ColorLightAdjust = 0
                    Case 28
                        ColorHue = 15 : ColorSat = 85 : ColorLightAdjust = 0
                    Case Else
                        ColorHue = 210 : ColorSat = 85 : ColorLightAdjust = 0
                End Select
            Else

                Try
                    ColorHue = Settings.Get(Of Integer)("UiLauncherHue")
                    ColorSat = Settings.Get(Of Integer)("UiLauncherSat")
                    ColorLightAdjust = Settings.Get(Of Integer)("UiLauncherLight")
                Catch
                End Try
            End If

            ThemeApplyTokens()
            ThemeRefreshMain()
            ThemeResetAnimations()

            RunInUi(
                Sub()
                    Try
                        If FrmLaunchLeft IsNot Nothing Then FrmLaunchLeft.RefreshThemeStyle()
                    Catch
                    End Try
                End Sub, True)
        Catch ex As Exception
            Logger.Error(ex, "刷新主题颜色失败", LogBehavior.Toast)
        End Try
    End Sub

    Friend ThemeDarkMode As Boolean = False

    Private Sub SetThemeBrushC(Key As String, C As MyColor)
        Application.Current.Resources(Key) = New SolidColorBrush(C)
        If Key.StartsWith("ColorBrush") Then
            Dim ObjKey2 As String = Key.Replace("ColorBrush", "ColorObject")
            If Application.Current.Resources.Contains(ObjKey2) Then
                Application.Current.Resources(ObjKey2) = CType(C, Color)
            End If
        End If
    End Sub

    Private Sub SetThemeBrush(Key As String, Hex As String)
        Dim C As New MyColor(Hex)
        Application.Current.Resources(Key) = New SolidColorBrush(C)
        If Key.StartsWith("ColorBrush") Then
            Dim ObjKey As String = Key.Replace("ColorBrush", "ColorObject")
            If Application.Current.Resources.Contains(ObjKey) Then
                Application.Current.Resources(ObjKey) = CType(C, Color)
            End If
        End If
    End Sub

    Friend Sub ThemeApplyTokens()

        Dim MM As Func(Of Double, Double) = Function(L As Double) If(ThemeDarkMode, 100.0 - L, L)

        Dim MB As Func(Of Double, Double) = Function(L As Double) If(ThemeDarkMode, L + 12.0, L)
        Color1 = New MyColor().FromHSL2(ColorHue, ColorSat * 0.2, MM(25 + ColorLightAdjust * 0.3))
        Color2 = New MyColor().FromHSL2(ColorHue, ColorSat, MB(45 + ColorLightAdjust))
        Color3 = New MyColor().FromHSL2(ColorHue, ColorSat, MB(55 + ColorLightAdjust))
        Color4 = New MyColor().FromHSL2(ColorHue, ColorSat, MB(65 + ColorLightAdjust))
        Color5 = New MyColor().FromHSL2(ColorHue, ColorSat, MB(80 + ColorLightAdjust * 0.4))
        Color6 = New MyColor().FromHSL2(ColorHue, ColorSat, MM(91 + ColorLightAdjust * 0.1))
        Color7 = New MyColor().FromHSL2(ColorHue, ColorSat, MM(95))
        Color8 = New MyColor().FromHSL2(ColorHue, ColorSat, MM(97))
        ColorBg0 = Color4 * 0.4 + Color5 * 0.4 + New MyColor().FromHSL2(ColorHue, 0, MM(65)) * 0.2
        ColorBg1 = New MyColor(190, Color7)
        ColorSemiTransparent = New MyColor(1, Color8)

        SetThemeBrushC("ColorBrush1", Color1)
        SetThemeBrushC("ColorBrush2", Color2)
        SetThemeBrushC("ColorBrush3", Color3)
        SetThemeBrushC("ColorBrush4", Color4)
        SetThemeBrushC("ColorBrush5", Color5)
        SetThemeBrushC("ColorBrush6", Color6)
        SetThemeBrushC("ColorBrush7", Color7)
        SetThemeBrushC("ColorBrush8", Color8)
        SetThemeBrushC("ColorBrushBg0", ColorBg0)
        SetThemeBrushC("ColorBrushBg1", ColorBg1)

        Dim GrayL As Double() = {25, 45, 55, 65, 80, 92, 94, 96}
        Dim GrayC(7) As MyColor
        For i As Integer = 0 To 7
            GrayC(i) = New MyColor().FromHSL2(ColorHue, 0, MM(GrayL(i)))
            SetThemeBrushC("ColorBrushGray" & (i + 1), GrayC(i))
        Next

        If ThemeDarkMode Then
            SetThemeBrush("ColorBrushSurfaceWindow", "#1B1F24")
            SetThemeBrush("ColorBrushSurfaceCard", "#21262C")
            SetThemeBrush("ColorBrushSurfaceInput", "#171B20")
            SetThemeBrush("ColorBrushSurfaceDialog", "#262C33")
            SetThemeBrush("ColorBrushSurfaceNav", "#1B1F24")
            SetThemeBrush("ColorBrushSurfaceHover", "#2A3138")
            SetThemeBrush("ColorBrushSurfaceSunken", "#14181C")
            SetThemeBrush("ColorBrushTextPrimary", "#E6E9EC")
            SetThemeBrush("ColorBrushTextSecondary", "#A6ADB4")
            SetThemeBrush("ColorBrushTextTertiary", "#858C93")
            SetThemeBrush("ColorBrushTextDisabled", "#6A7178")
            SetThemeBrushC("ColorBrushBorderStrong", GrayC(4))
            SetThemeBrush("ColorBrushBorderNormal", "#333A42")
            SetThemeBrush("ColorBrushBorderSubtle", "#2A3037")
            SetThemeBrush("ColorBrushBubbleAnswer", "#262C33")
            SetThemeBrush("ColorBrushBubbleAnswerText", "#E6E9EC")
            SetThemeBrush("ColorBrushBubbleThink", "#22303C")
            SetThemeBrush("ColorBrushBubbleThinkBorder", "#33465A")
            SetThemeBrush("ColorBrushBubbleThinkText", "#B4BCC4")
        Else
            SetThemeBrush("ColorBrushSurfaceWindow", "#EEF2F7")
            SetThemeBrush("ColorBrushSurfaceCard", "#FFFFFF")
            SetThemeBrush("ColorBrushSurfaceInput", "#FFFFFF")
            SetThemeBrush("ColorBrushSurfaceDialog", "#FBFBFB")
            SetThemeBrush("ColorBrushSurfaceNav", "#FFFFFF")
            SetThemeBrushC("ColorBrushSurfaceHover", Color7)
            SetThemeBrushC("ColorBrushSurfaceSunken", GrayC(7))
            SetThemeBrush("ColorBrushTextPrimary", "#1F2329")
            SetThemeBrush("ColorBrushTextSecondary", "#5B626B")
            SetThemeBrush("ColorBrushTextTertiary", "#737373")
            SetThemeBrush("ColorBrushTextDisabled", "#A6A6A6")
            SetThemeBrushC("ColorBrushBorderStrong", Color5)
            SetThemeBrushC("ColorBrushBorderNormal", GrayC(5))
            SetThemeBrushC("ColorBrushBorderSubtle", GrayC(6))
            SetThemeBrush("ColorBrushBubbleAnswer", "#F2F4F7")
            SetThemeBrush("ColorBrushBubbleAnswerText", "#1F2329")
            SetThemeBrushC("ColorBrushBubbleThink", Color8)
            SetThemeBrushC("ColorBrushBubbleThinkBorder", Color6)
            SetThemeBrush("ColorBrushBubbleThinkText", "#5B626B")
        End If

        SetThemeBrushC("ColorBrushSurfaceBar", Color2)

        SetThemeBrush("ColorBrushTextOnBrand", "#FFFFFF")
        SetThemeBrush("ColorBrushSuccess", "#0A7C52")
        SetThemeBrush("ColorBrushSuccessBack", "#E6F7F0")
        SetThemeBrush("ColorBrushWarning", "#8A5200")
        SetThemeBrush("ColorBrushWarningBack", "#FDF3E3")
        SetThemeBrush("ColorBrushError", "#B71C10")
        SetThemeBrush("ColorBrushErrorBack", "#FDECEC")
        SetThemeBrush("ColorBrushScrim", "#66000000")
    End Sub

    Friend Sub ThemeResetAnimations()
        RunInUi(
        Sub()
            Try
                If FrmMain Is Nothing OrElse Not FrmMain.IsLoaded Then Return
                For Each Btn As MyButton In EnumerateVisual(Of MyButton)(FrmMain)
                    Try
                        AniStop("MyButton Color " & Btn.Uuid)
                        AniStop("MyButton Background " & Btn.Uuid)
                        AniStop("MyButton Scale " & Btn.Uuid)
                        Btn.RefreshColor()
                    Catch
                    End Try
                Next
            Catch ex As Exception
                Logger.Error(ex, "重置主题动画失败")
            End Try
        End Sub, True)
    End Sub

    Private Iterator Function EnumerateVisual(Of T As DependencyObject)(Root As DependencyObject) As IEnumerable(Of T)
        If Root Is Nothing Then Return
        Dim Me1 As T = TryCast(Root, T)
        If Me1 IsNot Nothing Then Yield Me1
        Dim N As Integer = VisualTreeHelper.GetChildrenCount(Root)
        For i As Integer = 0 To N - 1
            For Each Child As T In EnumerateVisual(Of T)(VisualTreeHelper.GetChild(Root, i))
                Yield Child
            Next
        Next
    End Function

    Public Sub ThemeRefreshMain()
        RunInUi(
        Sub()
            If Not FrmMain.IsLoaded Then Return

            Dim Brush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
            Dim Deltas = ColorHueTopbarDelta.Switch(Function(d) New Integer() {-d, 0, d}, Function(d) d)
            Brush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor().FromHSL2(ColorHue + Deltas(0), ColorSat, 48 + ColorLightAdjust)})
            Brush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor().FromHSL2(ColorHue + Deltas(1), ColorSat, 54 + ColorLightAdjust)})
            Brush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor().FromHSL2(ColorHue + Deltas(2), ColorSat, 48 + ColorLightAdjust)})
            FrmMain.PanTitle.Background = Brush
            FrmMain.PanTitle.Background.Freeze()

            FrmMain.PanForm.SetResourceReference(Panel.BackgroundProperty, "ColorBrushSurfaceWindow")
        End Sub)
    End Sub
    Friend Sub ThemeCheckAll(EffectSetup As Boolean)
    End Sub
    Friend Function ThemeCheckOne(Id As Integer) As Boolean
        Return True
    End Function
    Friend Function ThemeUnlock(Id As Integer, Optional ShowDoubleHint As Boolean = True, Optional UnlockHint As String = Nothing) As Boolean
        Return False
    End Function

#End Region

#Region "更新"

    Public IsUpdateStarted As Boolean = False
    Public IsUpdateWaitingRestart As Boolean = False
    Public LatestVersion As String = VersionBaseName
    Public LatestReleaseInfoJson As JObject = Nothing
    Private LatestUpdateTime As DateTime? = Nothing
    Public Sub UpdateCheckByButton()
        Hint(GetLang("LangModSecretUpdateChecking"))
        If IsUpdateStarted Then
            Exit Sub
        End If
        RunInNewThread(Sub()
                           Try
                               UpdateLatestVersionInfo()
                               NoticeUserUpdate()
                           Catch ex As Exception
                               Logger.Error(ex, "[Update] 获取启动器更新信息失败")
                               Hint(GetLang("LangModSecretHintUpdateCheckFail"), HintType.Red)
                           End Try
                       End Sub)
    End Sub
    Public Sub UpdateLatestVersionInfo()

        If LatestUpdateTime IsNot Nothing AndAlso
           (DateTime.Now - LatestUpdateTime.Value).TotalMilliseconds < ServerLoader.ReloadTimeout Then
            Return
        End If

        Dim latestJson As JObject = Nothing
        Dim configJson As JObject = Nothing
        Dim releaseEx As Exception = Nothing
        Dim t1 = Task.Run(Sub()
                              Try
                                  latestJson = NetRequestByClientRetry("https://api.github.com/repos/PCL-Community/PCL2-Language/releases/latest", SimulateBrowserHeaders:=True, RequireJson:=True).DeserializeJson()
                              Catch ex As Exception
                                  releaseEx = ex
                              End Try
                          End Sub)
        Dim t2 = Task.Run(Sub()
                              Try
                                  configJson = NetRequestByClientRetry("https://github.com/PCL-Community/PCL2-Language/raw/refs/heads/main/remote_config/ServerConfig.json", RequireJson:=True).DeserializeJson()
                              Catch ex As Exception
                                  Logger.Warn(ex, "获取 ServerConfig 失败，将使用旧配置")
                              End Try
                          End Sub)
        Task.WaitAll({t1, t2})

        If releaseEx IsNot Nothing Then Throw releaseEx
        If latestJson IsNot Nothing Then
            LatestReleaseInfoJson = latestJson
            LatestVersion = latestJson("tag_name").ToString().TrimStart("v"c)
        End If
        If configJson IsNot Nothing Then
            ServerConfig = configJson
        End If
        LatestUpdateTime = DateTime.Now
    End Sub
    Public Sub NoticeUserUpdate()
        If Not LatestVersion = VersionBaseName Then
            If MyMsgBox(GetLang("LangModSecretUpdateDialogContent", VersionBaseName, LatestVersion), GetLang("LangModSecretUpdateDialogTitle"), GetLang("LangModSecretUpdateBtnGoUpdate"), GetLang("LangDialogBtnCancel")) = 1 Then
                UpdateStart(LatestVersion, False)
            End If
        Else
            Hint(GetLang("LangModSecretUpdateLatest", VersionBaseName), HintType.Green)
        End If
    End Sub
    Public Sub UpdateStart(VersionStr As String, Slient As Boolean, Optional ReceivedKey As String = Nothing, Optional ForceValidated As Boolean = False)
        Dim Tag As String = If(LatestReleaseInfoJson IsNot Nothing, LatestReleaseInfoJson("tag_name")?.ToString, Nothing)
        If String.IsNullOrEmpty(Tag) Then Tag = "v" & VersionStr
        Dim DlLink As String = "https://github.com/PCL-Community/PCL2-Language/releases/download/" & Tag & "/XFL_Lang.exe"
        Dim DlTargetPath As String = Path.Combine(Paths.Base, "XFL\Xiaofu Launcher.exe")
        RunInNewThread(Sub()
                           Try

                               Dim Loaders As New List(Of LoaderBase)

                               Dim Address As New List(Of String)
                               Address.Add(DlLink)
                               Address.Add($"https://cdn.crashmc.com/{DlLink}")
                               Loaders.Add(New LoaderDownload(GetLang("LangModSecretUpdateTaskDownload"), New List(Of NetFile) From {New NetFile(Address.ToArray, DlTargetPath, New FileChecker With {.MinSize = 1024 * 64})}) With {.ProgressWeight = 15})

                               Loaders.Add(New LoaderTask(Of Integer, Integer)(GetLang("LangModSecretUpdateTaskVerify"), Sub()
                                                                                                  Dim asset = LatestReleaseInfoJson?("assets")?(0)
                                                                                                  Dim digestStr = If(asset IsNot Nothing, asset("digest")?.ToString, Nothing)
                                                                                                  If digestStr IsNot Nothing AndAlso digestStr.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) Then
                                                                                                      Dim expected = digestStr.Substring(7).Trim().ToLowerInvariant()
                                                                                                      Dim actual = CryptographyUtils.ComputeFileHash(DlTargetPath, CryptographyUtils.HashMethod.Sha256)
                                                                                                      If expected <> actual Then
                                                                                                          Throw New Exception(GetLang("LangModSecretUpdateShaMismatch", expected, actual))
                                                                                                      End If
                                                                                                  End If
                                                                                              End Sub) With {.ProgressWeight = 1})
                               If Not Slient Then
                                   Loaders.Add(New LoaderTask(Of Integer, Integer)(GetLang("LangModSecretUpdateTaskReplace"), Sub() UpdateRestart(True)))
                               End If

                               Dim Loader As New LoaderCombo(Of JObject)(GetLang("LangModSecretUpdateTaskName"), Loaders)
                               Loader.Start()
                               If Slient Then
                                   IsUpdateWaitingRestart = True
                               Else
                                   LoaderTaskbarAdd(Loader)
                                   FrmMain.BtnExtraDownload.ShowRefresh()
                                   FrmMain.BtnExtraDownload.Ribble()
                               End If
                           Catch ex As Exception
                               Logger.Error(ex, "[Update] 下载启动器更新文件失败")
                               Hint(GetLang("LangModSecretHintUpdateDownloadFail"), HintType.Red)
                           End Try
                       End Sub)
    End Sub
    Public Sub UpdateRestart(TriggerRestartAndByEnd As Boolean)
        Try
            Dim fileName As String = Paths.Base & "XFL\Xiaofu Launcher.exe"
            If Not File.Exists(fileName) Then
                Logger.Info("[System] 更新失败：未找到更新文件")
                Exit Sub
            End If

            Dim text As String = String.Concat(New String() {"--update ", Process.GetCurrentProcess().Id, " """, PathExe, """ """, fileName, """ ", TriggerRestartAndByEnd})
            Logger.Info("[System] 更新程序启动，参数：" + text)
            Process.Start(New ProcessStartInfo(fileName) With {.WindowStyle = ProcessWindowStyle.Hidden, .CreateNoWindow = True, .Arguments = text})
            If TriggerRestartAndByEnd Then
                FrmMain.EndProgram(False)
                Logger.Info("[System] 已由于更新强制结束程序")
            End If
        Catch ex As UnauthorizedAccessException
            Logger.Trace(ex, "自动更新时触发 Win32 错误，疑似被拦截")
            If MyMsgBox(GetLang("LangModSecretUpdateFailContent", vbCrLf, ModBase.PathExe), GetLang("LangModSecretUpdateFailTitle"), GetLang("LangModSecretUpdateFailBtnHelp"), GetLang("LangDialogBtnOK"), "", True, True, False, Nothing, Nothing, Nothing) = 1 Then
                CustomEvent.Raise(CustomEvent.EventType.打开帮助, "启动器/Microsoft Defender 添加排除项.json")
            End If
        End Try
    End Sub
    Public Sub UpdateReplace(ProcessId As Integer, OldFileName As String, NewFileName As String, TriggerRestart As Boolean)
        Try
            Dim ps = Process.GetProcessById(ProcessId)
            If Not ps.HasExited Then
                ps.Kill()
            End If
        Catch ex As Exception
        End Try
        Dim ex2 As Exception = Nothing
        Dim num As Integer = 0
        Do
            Try
                If File.Exists(OldFileName) Then
                    File.Delete(OldFileName)
                End If
                If Not File.Exists(OldFileName) Then
                    Exit Try
                End If
            Catch ex3 As Exception
                ex2 = ex3
            Finally
                Thread.Sleep(500)
            End Try
            num += 1
        Loop While num <= 4
        If (Not File.Exists(OldFileName)) AndAlso File.Exists(NewFileName) Then
            Try
                FileUtils.Copy(NewFileName, OldFileName)
            Catch ex4 As UnauthorizedAccessException
                MsgBox(GetLang("LangModSecretUpdateFailPermissionContent", ex4.GetDisplay(True)), MsgBoxStyle.Critical, GetLang("LangModSecretUpdateFailTitle"))
            Catch ex5 As Exception
                MsgBox(GetLang("LangModSecretUpdateFailCopyContent", ex5.GetDisplay(True)), MsgBoxStyle.Critical, GetLang("LangModSecretUpdateFailTitle"))
                Return
            End Try
            If TriggerRestart Then
                Try
                    Process.Start(OldFileName)
                Catch ex6 As Exception
                    MsgBox(GetLang("LangModSecretUpdateFailRestartContent", ex6.GetDisplay(True)), MsgBoxStyle.Critical, GetLang("LangModSecretUpdateFailTitle"))
                End Try
            End If
            Return
        End If
        If TypeOf ex2 Is UnauthorizedAccessException Then
            Dim UpdateFailTips As String = String.Concat(
                                 If((PathExe.StartsWithF(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), False) OrElse PathExe.StartsWithF(Environment.GetFolderPath(Environment.SpecialFolder.Personal), False)),
                                 GetLang("LangModSecretUpdateFailTipMoveFromDesktop") & vbCrLf, ""),
                                 If(PathExe.StartsWithF("C", True),
                                 GetLang("LangModSecretUpdateFailTipMoveFromC") & vbCrLf, ""),
                                 GetLang("LangModSecretUpdateFailTipRunAsAdmin") & vbCrLf & GetLang("LangModSecretUpdateFailTipManualCopy") & vbCrLf & vbCrLf)
            MsgBox(GetLang("LangModSecretUpdateFailPermissionTryContent", UpdateFailTips, ex2.GetDisplay(True)), MsgBoxStyle.Critical, GetLang("LangModSecretUpdateFailTitle"))
            Return
        End If

    End Sub

    Friend Sub DownloadLatestPCL(Optional LoaderToSyncProgress As LoaderBase = Nothing)

    End Sub

#End Region

#Region "联网配置"

    Public ServerConfig As JObject

    Public ServerLoader As New LoaderTask(Of Integer, Integer)("XFL 配置更新", AddressOf LoadOnlineInfo, Priority:=ThreadPriority.BelowNormal) With
        {.ReloadTimeout = 1000 * 60 * 60}

    Private Sub LoadOnlineInfo()
        Select Case Settings.Get(Of Integer)("SystemSystemUpdate")
            Case 0
                UpdateLatestVersionInfo()
                If VersionBaseName <> LatestVersion Then
                    UpdateStart(LatestVersion, True)
                End If
            Case 1
                UpdateLatestVersionInfo()
                NoticeUserUpdate()
            Case 2, 3
                Exit Sub
        End Select
    End Sub
#End Region

#Region "赞助等级"

    Public ReadOnly Property CurrentRank As DonationRank
        Get
            Return DonationRank.None
        End Get
    End Property

    Public Sub InputPotatoCode(IsUpdating As Boolean)
    End Sub
    Friend Sub GeneratePotatoCode()
    End Sub

    Friend Function GetIdentify() As String
        Return "0000-0000-0000-0000"
    End Function

#End Region

End Module
