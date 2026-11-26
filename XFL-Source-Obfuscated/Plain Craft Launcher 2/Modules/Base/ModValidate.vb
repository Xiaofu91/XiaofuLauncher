
Public Module ModValidate

    Public Function Validate(Text As String, ValidateRules As IEnumerable(Of Validate)) As String
        Dim Result As String = ""
        For Each ValidateRule As Validate In ValidateRules
            Result = ValidateRule.Validate(Text)
            If Result Is Nothing Then Return ""
            If Result <> "" Then Return Result
        Next
        Return Result
    End Function

End Module

Public MustInherit Class Validate

    Public MustOverride Function Validate(Str As String) As String
End Class

Public Class ValidateNullable
    Inherits Validate
    Public Sub New()
    End Sub
    Public Overrides Function Validate(Str As String) As String
        If IsNothing(Str) OrElse String.IsNullOrEmpty(Str) Then Return Nothing
        Return ""
    End Function
End Class

Public Class ValidateNullOrEmpty
    Inherits Validate
    Public Sub New()
    End Sub
    Public Overrides Function Validate(Str As String) As String
        If IsNothing(Str) OrElse String.IsNullOrEmpty(Str) Then Return GetLang("LangModValidateNoEmptyInput")
        Return ""
    End Function
End Class

Public Class ValidateNullOrWhiteSpace
    Inherits Validate
    Public Sub New()
    End Sub
    Public Overrides Function Validate(Str As String) As String
        If IsNothing(Str) OrElse String.IsNullOrWhiteSpace(Str) Then Return GetLang("LangModValidateNoEmptyInput")
        Return ""
    End Function
End Class

Public Class ValidateFunc
    Inherits Validate
    Private ReadOnly _func As Func(Of String, String)
    Public Sub New(F As Func(Of String, String))
        _func = F
    End Sub
    Public Overrides Function Validate(Str As String) As String
        Return _func(Str)
    End Function
End Class

Public Class ValidateRegex
    Inherits Validate
    Public Property Regex As String
    Public Property ErrorDescription As String = GetLang("LangModValidateRegexError")
    Public Sub New()
    End Sub
    Public Sub New(Regex As String, Optional ErrorDescription As String = "正则检查失败！")
        If ErrorDescription.Equals("正则检查失败！") Then ErrorDescription = GetLang("LangModValidateRegexError")
        Me.Regex = Regex
        Me.ErrorDescription = ErrorDescription
    End Sub
    Public Overrides Function Validate(Str As String) As String
        If Not Str.RegexCheck(Regex) Then Return ErrorDescription
        Return ""
    End Function
End Class

Public Class ValidateHttp
    Inherits Validate
    Public Sub New()
    End Sub
    Public Overrides Function Validate(Str As String) As String
        If Str.EndsWithF("/") Then Str = Str.Substring(0, Str.Length - 1)
        If Not Str.RegexCheck("^(http[s]?)\://") Then Return GetLang("LangModValidateIncorrectUrl")
        Return ""
    End Function
End Class

Public Class ValidateInteger
    Inherits Validate
    Public Property Min As Integer
    Public Property Max As Integer = Integer.MaxValue
    Public Sub New()
    End Sub
    Public Sub New(Min As Integer, Max As Integer)
        Me.Min = Min
        Me.Max = Max
    End Sub
    Public Overrides Function Validate(Str As String) As String
        If Str.Length > 9 Then Return GetLang("LangModValidateNumTooLong")
        Dim Valed As Integer
        If Not Integer.TryParse(Str, Valed) Then Return GetLang("LangModValidateNumInt")
        If Valed > Max Then Return GetLang("LangModValidateNumNoGreater", Max)
        If Valed < Min Then Return GetLang("LangModValidateNumNoLess", Min)
        Return ""
    End Function
End Class

Public Class ValidateLength
    Inherits Validate
    Public Property Min As Integer = 0
    Public Property Max As Integer = Integer.MaxValue
    Public Sub New()
    End Sub
    Public Sub New(Min As Integer, Optional Max As Integer = Integer.MaxValue)
        Me.Min = Min
        Me.Max = Max
    End Sub
    Public Overrides Function Validate(Str As String) As String
        If Len(Str) <> Max AndAlso Max = Min Then Return GetLang("LangModValidateLen", Max)
        If Len(Str) > Max Then Return GetLang("LangModValidateLenMax", Max)
        If Len(Str) < Min Then Return GetLang("LangModValidateLenMin", Min)
        Return ""
    End Function
End Class

Public Class ValidateExcept
    Inherits Validate
    Public Property Excepts As New ObjectModel.Collection(Of String)
    Public Property ErrorMessage As String
    Public Sub New()
        ErrorMessage = GetLang("LangModValidateNoContain")
    End Sub
    Public Sub New(Excepts As ObjectModel.Collection(Of String), Optional ErrorMessage As String = "输入内容不能包含 %")
        If ErrorMessage.Equals("输入内容不能包含 %") Then ErrorMessage = GetLang("LangModValidateNoContain")
        Me.Excepts = Excepts
        Me.ErrorMessage = ErrorMessage
    End Sub
    Public Sub New(Excepts As IEnumerable(Of String), Optional ErrorMessage As String = "输入内容不能包含 %")
        Me.New(
            New ObjectModel.Collection(Of String)(New List(Of String)(Excepts)),
            If(ErrorMessage.Equals("输入内容不能包含 %"), GetLang("LangModValidateNoContain"), ErrorMessage))
    End Sub
    Public Sub New(Excepts As IEnumerable(Of Char), Optional ErrorMessage As String = "输入内容不能包含 %")
        Me.New(
            New ObjectModel.Collection(Of String)(Excepts.Select(Function(c) c.ToString).ToList),
            ErrorMessage)
    End Sub
    Public Overrides Function Validate(Str As String) As String
        For Each Ch As String In Excepts
            If Str.IndexOfF(Ch, True) >= 0 Then
                If IsNothing(ErrorMessage) Then ErrorMessage = ""
                Return ErrorMessage.Replace("%", Ch)
            End If
        Next
        Return ""
    End Function

End Class

Public Class ValidateExceptSame
    Inherits Validate
    Public Property Excepts As New ObjectModel.Collection(Of String)
    Public Property ErrorMessage As String
    Public Property IgnoreCase As Boolean = False
    Public Sub New()
    End Sub
    Public Sub New(Excepts As ObjectModel.Collection(Of String), Optional ErrorMessage As String = "输入内容不能为 %", Optional IgnoreCase As Boolean = False)
        If ErrorMessage.Equals("输入内容不能为 %") Then ErrorMessage = GetLang("LangModValidateNoContent")
        Me.Excepts = Excepts
        Me.ErrorMessage = ErrorMessage
        Me.IgnoreCase = IgnoreCase
    End Sub
    Public Sub New(Excepts As IEnumerable(Of String), Optional ErrorMessage As String = "输入内容不能为 %", Optional IgnoreCase As Boolean = False)
        Me.New(
            New ObjectModel.Collection(Of String)(New List(Of String)(Excepts)),
            If(ErrorMessage.Equals("输入内容不能为 %"), GetLang("LangModValidateNoContent"), ErrorMessage), IgnoreCase)
    End Sub
    Public Sub New(Except As String, Optional ErrorMessage As String = "输入内容不能为 %", Optional IgnoreCase As Boolean = False)
        Me.New(
            New ObjectModel.Collection(Of String) From {Except},
            ErrorMessage, IgnoreCase)
    End Sub
    Public Overrides Function Validate(Str As String) As String
        If Str Is Nothing Then Return ErrorMessage.Replace("%", "null")
        For Each Ch As String In Excepts
            If IgnoreCase Then
                If Str.Lower = Ch.Lower Then Return ErrorMessage.Replace("%", Ch)
            Else
                If Str.Equals(Ch) Then Return ErrorMessage.Replace("%", Ch)
            End If
        Next
        Return ""
    End Function

End Class

Public Class ValidateSame
    Inherits Validate
    Public Property Required As String
    Public Property ErrorMessage As String
    Public Property IgnoreCase As Boolean = False
    Public Sub New()
    End Sub
    Public Sub New(Required As String, Optional ErrorMessage As String = "输入内容有误", Optional IgnoreCase As Boolean = False)
        Me.Required = Required
        Me.ErrorMessage = ErrorMessage
        Me.IgnoreCase = IgnoreCase
    End Sub
    Public Overrides Function Validate(Str As String) As String
        Return If(String.Compare(Str, Required, IgnoreCase) = 0, "", ErrorMessage)
    End Function

End Class

Public Class ValidateFolderName
    Inherits Validate
    Public Property Folder As String
    Public Property UseMinecraftCharCheck As Boolean = True
    Public Property IgnoreCase As Boolean = True
    Private ReadOnly PathIgnore As IEnumerable(Of String)
    Public Sub New()
    End Sub
    Public Sub New(Folder As String, Optional UseMinecraftCharCheck As Boolean = True, Optional IgnoreCase As Boolean = True)
        Me.Folder = Folder
        Me.IgnoreCase = IgnoreCase
        Me.UseMinecraftCharCheck = UseMinecraftCharCheck
        On Error Resume Next
        PathIgnore = DirectoryUtils.EnumerateDirectories(Folder)
    End Sub
    Public Overrides Function Validate(Str As String) As String
        Try

            Dim LengthCheck As String = New ValidateNullOrWhiteSpace().Validate(Str)
            If LengthCheck <> "" Then Return LengthCheck

            If Str.StartsWithF(" ") Then Return GetLang("LangModValidateNoStartWithSpaceFolderName")
            If Str.EndsWithF(" ") Then Return GetLang("LangModValidateNoEndWithSpaceFolderName")

            LengthCheck = New ValidateLength(1, 100).Validate(Str)
            If LengthCheck <> "" Then Return LengthCheck

            If Str.EndsWithF(".") Then Return GetLang("LangModValidateNoEndWithDotFolderName")

            Dim CharactCheck As String = New ValidateExcept(Path.GetInvalidFileNameChars() & If(UseMinecraftCharCheck, "!;", ""), GetLang("LangModValidateNoEndWithSpecialCharFolderName")).Validate(Str)
            If CharactCheck <> "" Then Return CharactCheck

            Dim InvalidStrCheck As String = New ValidateExceptSame({"CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"}, GetLang("LangModValidateNoSpacialFolderName"), True).Validate(Str)
            If InvalidStrCheck <> "" Then Return InvalidStrCheck

            If Str.RegexCheck(".{2,}~\d") Then Return GetLang("LangModValidateFileNoSpecialName")

            Dim Arr As New List(Of String)
            If PathIgnore IsNot Nothing Then
                For Each FolderPath In PathIgnore
                    Arr.Add(PathUtils.GetLastPart(FolderPath))
                Next
            End If
            Dim SameNameCheck = New ValidateExceptSame(Arr, GetLang("LangModValidateNoSameFolderName"), IgnoreCase).Validate(Str)
            If Not SameNameCheck = "" Then Return SameNameCheck
            Return ""
        Catch ex As Exception
            Logger.Warn(ex, "检查文件夹名出错")
            Return "错误：" & ex.Message
        End Try
    End Function
End Class

Public Class ValidateFileName
    Inherits Validate
    Public Property Name As String
    Public Property UseMinecraftCharCheck As Boolean = True
    Public Property IgnoreCase As Boolean = True
    Public Property ParentFolder As String = Nothing
    Public Property RequireParentFolderExists = True
    Public Sub New()
    End Sub
    Public Sub New(Name As String, Optional UseMinecraftCharCheck As Boolean = True, Optional IgnoreCase As Boolean = True)
        Me.Name = Name
        Me.IgnoreCase = IgnoreCase
        Me.UseMinecraftCharCheck = UseMinecraftCharCheck
    End Sub
    Public Overrides Function Validate(Str As String) As String
        Try

            Dim LengthCheck As String = New ValidateNullOrWhiteSpace().Validate(Str)
            If Not LengthCheck = "" Then Return LengthCheck

            If Str.StartsWithF(" ") Then Return GetLang("LangModValidateFileNoStartWithSpace")
            If Str.EndsWithF(" ") Then Return GetLang("LangModValidateFileNoEndWithSpace")

            LengthCheck = New ValidateLength(1, 253).Validate(Str & If(ParentFolder, ""))
            If LengthCheck <> "" Then Return LengthCheck

            If Str.EndsWithF(".") Then Return GetLang("LangModValidateFileNoEndWithDot")

            Dim CharactCheck As String = New ValidateExcept(Path.GetInvalidFileNameChars() & If(UseMinecraftCharCheck, "!;", ""), GetLang("LangModValidateFileNoContain")).Validate(Str)
            If CharactCheck <> "" Then Return CharactCheck

            Dim InvalidStrCheck As String = New ValidateExceptSame({"CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"}, GetLang("LangModValidateFileNoContent"), True).Validate(Str)
            If InvalidStrCheck <> "" Then Return InvalidStrCheck

            If Str.RegexCheck(".{2,}~\d") Then Return GetLang("LangModValidateFileNoSpecialName")

            If ParentFolder IsNot Nothing Then
                Dim DirInfo = DirectoryUtils.GetInfo(ParentFolder)
                If DirInfo.Exists Then
                    Dim SameNameCheck = New ValidateExceptSame(DirInfo.EnumerateFiles("*").Select(Function(f) f.Name),
                                                               GetLang("LangModValidateFileNoSameName"), IgnoreCase).Validate(Str)
                    If Not SameNameCheck = "" Then Return SameNameCheck
                Else
                    If RequireParentFolderExists Then Return GetLang("LangModValidateFileNoParentFolder", ParentFolder)
                End If
            End If
            Return ""
        Catch ex As Exception
            Logger.Warn(ex, "检查文件名出错")
            Return "错误：" & ex.Message
        End Try
    End Function
End Class

Public Class ValidateFolderPath
    Inherits Validate
    Public Property UseMinecraftCharCheck As Boolean = True
    Public Sub New()
    End Sub
    Public Sub New(UseMinecraftCharCheck As Boolean)
        Me.UseMinecraftCharCheck = UseMinecraftCharCheck
    End Sub
    Public Overrides Function Validate(Str As String) As String

        Str = Str.Replace("/", "\")
        If Not Str.TrimEnd("\").EndsWithF(":") Then Str = Str.TrimEnd("\")

        Dim LengthCheck As String = New ValidateNullOrWhiteSpace().Validate(Str)
        If LengthCheck <> "" Then Return LengthCheck

        LengthCheck = New ValidateLength(1, 254).Validate(Str)
        If LengthCheck <> "" Then Return LengthCheck

        If Str.StartsWithF("\\Mac\") Then GoTo Fin
        For Each Drive In Environment.GetLogicalDrives().Where(Function(p) DirectoryUtils.Exists(p))
            Dim DriveName As String = Drive.Upper.First & ":\"
            If Str.Upper = DriveName Then Return ""
            If Str.StartsWithF(DriveName, True) Then GoTo Fin
        Next
        Return GetLang("LangModValidateFileHeadUriIncorrect")
Fin:

        For i = If(Str.StartsWithF("\\Mac\"), 2, 1) To Str.Split("\").Count - 1
            Dim SubStr As String = Str.Split("\")(i)

            Dim SubLengthCheck As String = New ValidateNullOrWhiteSpace().Validate(SubStr)
            If Not SubLengthCheck = "" Then Return GetLang("LangModValidateFolderUriIncorrect")

            Dim CharactCheck As String = New ValidateExcept(Path.GetInvalidFileNameChars() & If(UseMinecraftCharCheck, "!;", ""), GetLang("LangModValidateInvalidChar")).Validate(SubStr)
            If Not CharactCheck = "" Then Return CharactCheck

            If SubStr.StartsWithF(" ") Then Return GetLang("LangModValidateNoStartWithSpaceFolderName")
            If SubStr.EndsWithF(" ") Then Return GetLang("LangModValidateNoEndWithSpaceFolderName")

            If SubStr.EndsWithF(".") Then Return GetLang("LangModValidateNoEndWithDotFolderName")

            Dim InvalidStrCheck As String = New ValidateExceptSame({"CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"}, GetLang("LangModValidateNoSpacialFolderName")).Validate(SubStr)
            If Not InvalidStrCheck = "" Then Return InvalidStrCheck

            If SubStr.RegexCheck(".{2,}~\d") Then Return GetLang("LangModValidateFileNoSpecialName")
        Next
        Return ""
    End Function
End Class
