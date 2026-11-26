Imports System.Reflection
Imports System.Windows.Threading
Imports Microsoft.Win32

Public Class Application

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        Try

            XflSelfUnblock()

            MeloongCore.Main.Init("XFL")
            MeloongCore.Wpf.Main.Init()
            Logger.Instance = New XFLLogger With {.logFolder = Paths.BaseThenName, .MinLevel = If(ModeDebug, LogLevel.Trace, LogLevel.Info)}

            Try
                Dim exeLoc As String = Reflection.Assembly.GetEntryAssembly()?.Location
                Logger.Info($"[XFL] 网络下载标记自解除完成，当前仍有标记：{If(HasZoneIdentifier(exeLoc), "是", "否")}")

                RunInNewThread(
                    Sub()
                        Try
                            Threading.Thread.Sleep(8000)
                            XflSelfUnblock()
                            Logger.Info($"[XFL] 网络下载标记二次自解除完成，当前仍有标记：{If(HasZoneIdentifier(exeLoc), "是", "否")}")
                        Catch
                        End Try
                    End Sub)
            Catch
            End Try

            Settings.Set("SystemLang", Lang)

            Try
                Application.Current.Resources.MergedDictionaries(1) = New ResourceDictionary With {.Source = New Uri("pack://application:,,,/Resources/Language/" & Lang & ".xaml", UriKind.RelativeOrAbsolute)}
            Catch ex As Exception
                MsgBox("无法找到语言资源：" & Lang & vbCrLf & "Language resource cannot be found:" & Lang, MsgBoxStyle.Critical)
                Lang = GetDefaultLang()
            End Try

            Dim LaunchFont As FontFamily
            Select Case Lang
                Case "zh-TW", "zh-HK", "lzh", "zh-MARS"
                    LaunchFont = New FontFamily(New Uri("pack://application:,,,/"), "./Resources/#XFL English, Segoe UI, Microsoft JhengHei UI")
                Case "ja-JP"
                    LaunchFont = New FontFamily(New Uri("pack://application:,,,/"), "./Resources/#XFL English, Segoe UI, Yu Gothic UI, Microsoft YaHei UI")
                Case "ko-KR"
                    LaunchFont = New FontFamily(New Uri("pack://application:,,,/"), "./Resources/#XFL English, Segoe UI, Malgun Gothic, Microsoft YaHei UI")
                Case "en-US", "en-GB", "zh-CN", "zh-MEME"
                    LaunchFont = New FontFamily(New Uri("pack://application:,,,/"), "./Resources/#XFL English, Segoe UI, Microsoft YaHei UI")
                Case Else
                    LaunchFont = New FontFamily(New Uri("pack://application:,,,/"), "Segoe UI, ./Resources/#XFL English, Microsoft YaHei UI")
            End Select
            SwitchApplicationFont(LaunchFont)

            Thread.CurrentThread.Priority = ThreadPriority.Highest

            If BuildType = BuildTypes.Debug Then
                Try
                    ModDevelop.Start()
                Catch ex As Exception
                    Logger.Error(ex, "开发者模式测试出错", LogBehavior.Alert)
                End Try
            End If

            Try
                Using ndpKey As RegistryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\")
                    If ndpKey?.GetValue("Release") IsNot Nothing AndAlso CInt(ndpKey.GetValue("Release")) < 528040 Then OldEnvironmentAssert()
                End Using
            Catch ex As Exception
                Logger.Warn(ex, "检查 .NET Framework 版本失败")
            End Try

            Try
                Dim _unused As New FormattedText("", Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Fonts.SystemTypefaces.First, 96, New MyColor, DPI)
            Catch ex As UriFormatException
                Environment.SetEnvironmentVariable("windir", Environment.GetEnvironmentVariable("SystemRoot"), EnvironmentVariableTarget.User)
                Dim _unused As New FormattedText("", Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Fonts.SystemTypefaces.First, 96, New MyColor, DPI)
            End Try

            If e.Args.Length > 0 Then
                If e.Args(0) = "--update" Then

                    UpdateReplace(e.Args(1), e.Args(2).Trim(""""), e.Args(3).Trim(""""), e.Args(4))
                    Environment.Exit(ProcessReturnValues.TaskDone)
                ElseIf e.Args(0) = "--gpu" Then

                    Try
                        SetGPUPreference(e.Args(1).Trim(""""))
                        Environment.Exit(ProcessReturnValues.TaskDone)
                    Catch ex As Exception
                        Environment.Exit(ProcessReturnValues.Fail)
                    End Try
                ElseIf e.Args(0).StartsWithF("--memory") Then

                    Dim Ram = My.Computer.Info.AvailablePhysicalMemory
                    Try
                        PageOtherTest.MemoryOptimizeInternal(False)
                    Catch ex As Exception
                        MsgBox(ex.Message, MsgBoxStyle.Critical, GetLang("LangApplicationDialogContentMemReduceFail"))
                        Environment.Exit(-1)
                    End Try
                    If My.Computer.Info.AvailablePhysicalMemory < Ram Then
                        Environment.Exit(0)
                    Else
                        Environment.Exit((My.Computer.Info.AvailablePhysicalMemory - Ram) / 1024)
                    End If
                End If
            End If

            Try
                DirectoryUtils.Create(Paths.Base & "XFL\Pictures\")
                DirectoryUtils.Create(Paths.Base & "XFL\Musics\")
                CheckPermissionWithException(Paths.Base & "XFL\")
            Catch ex As Exception
                MsgBox(GetLang("LangApplicationDialogContentNoPermission", Paths.Base,
                                If(Paths.Base.StartsWithF("C:", True), GetLang("LangApplicationDialogNoPermissionMoveSuffixC"), GetLang("LangApplicationDialogNoPermissionMoveSuffixDefault"))),
                       MsgBoxStyle.Critical, GetLang("LangModSecretPermissionError"))
                Environment.[Exit](ProcessReturnValues.Cancel)
            End Try
RetryCacheCheck:
            Try
                DirectoryUtils.Create(PathTemp)
                CheckPermissionWithException(PathTemp)
            Catch ex As Exception
                If PathTemp = Path.GetTempPath() & "XFL\" Then
                    MyMsgBox(GetLang("LangApplicationDialogContentCacheFolderUnavailable", ex.GetDisplay(True)), GetLang("LangApplicationDialogTitleCacheFolderUnavailable"))
                Else
                    MyMsgBox(GetLang("LangApplicationDialogContentCustomCacheFolderUnavailable", ex.GetDisplay(True)), GetLang("LangApplicationDialogTitleCacheFolderUnavailable"))
                    Settings.Set("SystemSystemCache", "")
                    PathTemp = Path.GetTempPath() & "XFL\"
                    GoTo RetryCacheCheck
                End If
            End Try
            DirectoryUtils.Create(PathTemp & "Cache\")
            DirectoryUtils.Create(Paths.AppDataThenName)

            WaitingMutex(e)

            ToolTipService.InitialShowDelayProperty.OverrideMetadata(GetType(DependencyObject), New FrameworkPropertyMetadata(100))

            ServicePointManager.Expect100Continue = False
            ServicePointManager.DefaultConnectionLimit = 10000
            ServicePointManager.UseNagleAlgorithm = False
            ServicePointManager.EnableDnsRoundRobin = True
            ServicePointManager.ReusePort = True
            If Environment.OSVersion.Version < New Version(6, 2) Then
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 Or SecurityProtocolType.Tls Or SecurityProtocolType.Tls11 Or SecurityProtocolType.Tls12
            End If

            If Settings.Get(Of Boolean)("UiLauncherLogo") Then

                FrmStart = New SplashScreen("Images\Splash.png")
                FrmStart.Show(False, True)
            End If

            Logger.Info($"程序版本：{VersionDisplay} ({VersionCode}{If(CommitHash = "", "", $"，#{CommitHash}")})")
            If BuildType = BuildTypes.Snapshot Then
                Logger.Info($"识别码：{Identify}{If(ThemeCheckOne(9), "，已解锁反馈主题", "，未解锁反馈主题")}")
            Else
                Logger.Info($"识别码：{Identify}")
            End If
            Logger.Info($"程序路径：{PathExe}")
            Logger.Info($"系统编码：{Encoding.Default.HeaderName} ({Encoding.Default.CodePage}, GBK={IsGBKEncoding})")
            Logger.Info($"管理员权限：{WindowsUtils.HasAdminRole()}")
            Logger.Info("[Location] 启动器语言：" & Lang)
            Logger.Info("[Location] 当前系统环境是否为中国大陆：" & IsLocationZH())

            If Paths.Base.Contains(Path.GetTempPath()) OrElse Paths.Base.Contains("AppData\Local\Temp\") Then
                MyMsgBox(GetLang("LangApplicationDialogContentUnzipLauncher"), GetLang("LangApplicationDialogTitleUnzipLauncher"), GetLang("LangDialogBtnOK"), IsWarn:=True, ForceWait:=True)
                FormMain.EndProgramForce(ProcessReturnValues.Cancel)
            End If
            If Not Environment.Is64BitOperatingSystem Then
                MyMsgBox(GetLang("LangApplicationDialogContent32BitWarn"), GetLang("LangApplicationDialogTitle32BitWarn"), GetLang("LangDialogBtnOK"), IsWarn:=True, ForceWait:=True)
                FormMain.EndProgramForce(ProcessReturnValues.Cancel)
            End If
            SetUnmanagedDll()

            Logger.Info($"第一阶段加载用时：{GetTimeMs() - ApplicationStartTick} ms")
            ApplicationStartTick = GetTimeMs()
            AniControlEnabled += 1
        Catch ex As Exception
            Dim FilePath As String = Nothing
            Try
                FilePath = PathExe
            Catch
            End Try
            MsgBox(ex.GetDisplay(True) & vbCrLf & GetLang("LangApplicationDialogPathPrefix") & If(String.IsNullOrEmpty(FilePath), GetLang("LangApplicationDialogPathGetFail"), FilePath), MsgBoxStyle.Critical, GetLang("LangApplicationDialogTitleInitError"))
            FormMain.EndProgramForce(ProcessReturnValues.Exception)
        End Try
    End Sub

    Private Shared Sub WaitingMutex(e As StartupEventArgs)
        If BuildType = BuildTypes.Debug Then Return
        Try
            Dim MutexCreatedNew As Boolean
            PclMutex = New Mutex(True, "PCL_SingletonMutex", MutexCreatedNew)
            If MutexCreatedNew Then Return
            Logger.Warn("已有一个 XFL 实例正在运行")

            If e.Args.Length > 0 AndAlso e.Args(0) = "--wait" Then
                Try
                    If PclMutex.WaitOne(10000) Then Return
                Catch Ignored As AbandonedMutexException
                    Return
                End Try
            End If

            Dim WindowHwnd As IntPtr = FindWindow(Nothing, "Xiaofu Launcher　")
            If WindowHwnd = IntPtr.Zero Then WindowHwnd = FindWindow(Nothing, "Xiaofu Launcher　")
            If WindowHwnd <> IntPtr.Zero Then ShowWindowToTop(WindowHwnd)

            Beep()
            Environment.[Exit](ProcessReturnValues.Cancel)
        Catch ex As Exception
            Logger.Warn(ex, "单例检查失败")
        End Try
    End Sub
    Private Shared PclMutex As Mutex

    Private Sub Application_SessionEnding(sender As Object, e As SessionEndingCancelEventArgs) Handles Me.SessionEnding
        FrmMain?.EndProgram(False)
    End Sub

    Private Sub Application_DispatcherUnhandledException(sender As Object, e As DispatcherUnhandledExceptionEventArgs) Handles Me.DispatcherUnhandledException
        On Error Resume Next

        If FrmMain?.PageLeft IsNot Nothing AndAlso TypeOf FrmMain.PageLeft Is IDispatcherUnhandledException Then
            CType(FrmMain.PageLeft, IDispatcherUnhandledException).DispatcherUnhandledException(sender, e)
            If e.Handled Then Return
        End If
        If FrmMain?.PageRight IsNot Nothing AndAlso TypeOf FrmMain.PageRight Is IDispatcherUnhandledException Then
            CType(FrmMain.PageRight, IDispatcherUnhandledException).DispatcherUnhandledException(sender, e)
            If e.Handled Then Return
        End If

        e.Handled = True
        If IsProgramEnding Then Return
        FeedbackInfo()
        Dim Detail As String = e.Exception.GetDisplay(True)
        If Detail.Contains("System.Windows.Threading.Dispatcher.Invoke") OrElse Detail.Contains("MS.Internal.AppModel.ITaskbarList.HrInit") OrElse Detail.Contains("未能加载文件或程序集") OrElse
           Detail.Contains(".NET Framework") Then
            OldEnvironmentAssert(e.Exception)
        Else
            Logger.Error(e.Exception, "程序出现未知错误", LogBehavior.AlertThenCrash)
        End If
    End Sub
    Private Shared Sub OldEnvironmentAssert(Optional ex As Exception = Nothing)
        OpenWebsite("https://dotnet.microsoft.com/zh-cn/download/dotnet-framework/thank-you/net48-offline-installer")
        If ex Is Nothing Then
            Logger.Error($"你的 .NET Framework 版本过低或损坏，请下载并重新安装 .NET Framework 4.8！{vbCrLf}若安装失败，可以先卸载高版本的 .NET Framework 然后再试。", LogBehavior.AlertThenCrash)
        Else
            Logger.Error(ex, $"你的 .NET Framework 版本过低或损坏，请下载并重新安装 .NET Framework 4.8！{vbCrLf}若安装失败，可以先卸载高版本的 .NET Framework 然后再试。", LogBehavior.AlertThenCrash)
        End If
    End Sub

    Private Declare Function SetDllDirectory Lib "kernel32" Alias "SetDllDirectoryA" (lpPathName As String) As Boolean
    Private Shared Sub SetUnmanagedDll()
        SetDllDirectory(PathPure.TrimEnd("\"c))
        Try
            ExtractResources(PathPure & "libwebp.dll", "libwebp64")
        Catch ex As Exception
            Logger.Warn(ex, "写入 libwebp.dll 失败")
        End Try
    End Sub

    Private Sub MyIconButton_Click(sender As Object, e As EventArgs)
        Select Case Settings.Get(Of McLoginType)("LoginType")
            Case McLoginType.Ms

                Dim MsJson As JObject = Settings.Get(Of String)("LoginMsJson").DeserializeJson()
                MsJson.Remove(sender.Tag)
                Settings.Set("LoginMsJson", MsJson.ToString(Newtonsoft.Json.Formatting.None))
                If FrmLoginMs.ComboAccounts.SelectedItem Is sender.Parent Then FrmLoginMs.ComboAccounts.SelectedIndex = 0
                FrmLoginMs.ComboAccounts.Items.Remove(sender.Parent)
            Case McLoginType.Legacy

                Dim Names As New List(Of String)
                Names.AddRange(Settings.Get(Of String)("LoginLegacyName").ToString.Split("¨"))
                Names.Remove(sender.Tag)
                Settings.Set("LoginLegacyName", Names.Join("¨"c))
                FrmLoginLegacy.ComboName.ItemsSource = Names
                FrmLoginLegacy.ComboName.Text = If(Names.Any, Names(0), "")
            Case Else

                Dim Token As String = Settings.Get(Of McLoginType)("LoginType").ToString()
                Dim Dict As New Dictionary(Of String, String)
                Dim Names As New List(Of String)
                Dim Passs As New List(Of String)
                If Not Settings.Get(Of String)("Login" & Token & "Email") = "" Then Names.AddRange(Settings.Get(Of String)("Login" & Token & "Email").Split("¨"))
                If Not Settings.Get(Of String)("Login" & Token & "Pass") = "" Then Passs.AddRange(Settings.Get(Of String)("Login" & Token & "Pass").Split("¨"))
                For i = 0 To Names.Count - 1
                    Dict.Add(Names(i), Passs(i))
                Next
                Dict.Remove(sender.Tag)
                Settings.Set("Login" & Token & "Email", Dict.Keys.Join("¨"c))
                Settings.Set("Login" & Token & "Pass", Dict.Values.Join("¨"c))
                Select Case Token
                    Case "Nide"
                        FrmLoginNide.ComboName.ItemsSource = Dict.Keys
                        FrmLoginNide.ComboName.Text = If(Dict.Keys.Any, Dict.Keys(0), "")
                        FrmLoginNide.TextPass.Password = If(Dict.Values.Any, Dict.Values(0), "")
                    Case "Auth"
                        FrmLoginAuth.ComboName.ItemsSource = Dict.Keys
                        FrmLoginAuth.ComboName.Text = If(Dict.Keys.Any, Dict.Keys(0), "")
                        FrmLoginAuth.TextPass.Password = If(Dict.Values.Any, Dict.Values(0), "")
                End Select
        End Select
    End Sub

    Public Shared ShowingTooltips As New List(Of ToolTip)
    Private Sub OnTooltipOpened(sender As ToolTip, e As RoutedEventArgs)
        ShowingTooltips.Add(sender)
    End Sub
    Private Sub OnToolTipClosed(sender As ToolTip, e As RoutedEventArgs)
        ShowingTooltips.Remove(sender)
    End Sub

End Class
