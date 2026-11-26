
Friend Module ModXflDep

    Private Const Tag As String = "[XFL][前置]"

    Friend Function CollectMissing(RootFile As ResourceVersion, Platform As ResourcePlatforms,
                                   TargetFolder As String) As List(Of ResourceVersion)
        Dim Result As New List(Of ResourceVersion)
        Try
            If RootFile Is Nothing Then Return Result
            If RootFile.ResourceType <> ResourceTypes.Mod Then Return Result
            If String.IsNullOrWhiteSpace(TargetFolder) Then Return Result

            Dim Installed As New HashSet(Of String)
            Try
                If IO.Directory.Exists(TargetFolder) Then
                    For Each F As String In IO.Directory.EnumerateFiles(TargetFolder)
                        Installed.Add(IO.Path.GetFileName(F).ToLower)
                    Next
                End If
            Catch
            End Try
            Dim Visited As New HashSet(Of String)
            Dim Queue As New Queue(Of ResourceVersion)
            Queue.Enqueue(RootFile)
            Dim Guard As Integer = 0
            While Queue.Count > 0 AndAlso Guard < 64
                Guard += 1
                Dim Current As ResourceVersion = Queue.Dequeue()
                Dim DepIds As New List(Of String)
                Try
                    For Each D As String In Current.Dependencies
                        If Not String.IsNullOrWhiteSpace(D) AndAlso Not DepIds.Contains(D) Then DepIds.Add(D)
                    Next
                Catch
                End Try
                If Not DepIds.Any() Then Continue While
                For Each DepId As String In DepIds
                    Try
                        If Not Visited.Add(DepId) Then Continue For
                        Dim Candidates As List(Of ResourceVersion) = Nothing
                        Try
                            Candidates = ResourceVersion.FromProjectId(DepId, Platform, LoadDependencies:=False)
                        Catch ex1 As Exception
                            Logger.Warn(ex1, $"{Tag} 获取前置版本失败：{DepId}")
                            Continue For
                        End Try
                        If Candidates Is Nothing OrElse Not Candidates.Any() Then Continue For
                        Dim Pick As ResourceVersion = ChooseCompatible(Candidates, Current)
                        If Pick Is Nothing Then Continue For
                        If AlreadyInstalled(Installed, Pick) Then
                            Logger.Info($"{Tag} 前置已存在，跳过：{Pick.FileName}")
                            Continue For
                        End If
                        Result.Add(Pick)
                        Queue.Enqueue(Pick)
                        Logger.Info($"{Tag} 需补前置：{Pick.Display}（{Pick.FileName}）")
                    Catch exEach As Exception
                        Logger.Warn(exEach, $"{Tag} 处理前置失败：{DepId}")
                    End Try
                Next
            End While
            If Result.Count > 0 Then
                Logger.Info($"{Tag} 本次共需补下载 {Result.Count} 个前置")
            End If
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 收集前置失败")
        End Try
        Return Result
    End Function

    Private Function ChooseCompatible(Candidates As List(Of ResourceVersion),
                                      Current As ResourceVersion) As ResourceVersion
        Try
            Dim WantVersions As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each V As String In Current.GameVersions
                WantVersions.Add(V)
            Next
            Dim WantLoaders As ModLoaders = Current.ModLoaders
            Dim Filtered As List(Of ResourceVersion) = Candidates.Where(
                Function(c As ResourceVersion)
                    Dim verOk As Boolean = WantVersions.Count = 0
                    If Not verOk Then
                        For Each V As String In c.GameVersions
                            If WantVersions.Contains(V) Then
                                verOk = True
                                Exit For
                            End If
                        Next
                    End If
                    If Not verOk Then Return False
                    Dim ldOk As Boolean = WantLoaders = ModLoaders.None OrElse c.ModLoaders = ModLoaders.None
                    If Not ldOk Then ldOk = WantLoaders.Flags.Intersect(c.ModLoaders.Flags).Any()
                    Return ldOk
                End Function).ToList()
            If Not Filtered.Any() Then Filtered = Candidates
            Dim Ordered As List(Of ResourceVersion) = Filtered _
                .OrderByDescending(Function(c As ResourceVersion) c.ReleaseType = ResourceVersion.ReleaseTypes.Release) _
                .ThenByDescending(Function(c As ResourceVersion) c.ReleaseDate) _
                .ToList()
            Return Ordered.FirstOrDefault()
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 选择兼容版本失败")
            Return Nothing
        End Try
    End Function

    Private Function AlreadyInstalled(Installed As HashSet(Of String), Pick As ResourceVersion) As Boolean
        Try
            Dim Name As String = If(Pick.FileName, "").ToLower
            If String.IsNullOrWhiteSpace(Name) Then Return False
            If Installed.Contains(Name) Then Return True
            Dim Stem As String = IO.Path.GetFileNameWithoutExtension(Name)
            If Stem.Length < 3 Then Return False
            For Each F As String In Installed
                If F.EndsWithF(".jar") AndAlso F.Contains(Stem) Then Return True
            Next
        Catch
        End Try
        Return False
    End Function

End Module
