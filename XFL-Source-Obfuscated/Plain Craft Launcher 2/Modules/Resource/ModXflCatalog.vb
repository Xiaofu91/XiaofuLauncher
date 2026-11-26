
Public Class ModXflCatalogEntry

    Public Tag As String

    Public Title As String

    Public ProjectId As String

    Public Loaders As String

    Public Sub New(tag As String, title As String, projectId As String, Optional loaders As String = "")
        Me.Tag = tag
        Me.Title = title
        Me.ProjectId = projectId
        Me.Loaders = loaders
    End Sub
End Class

Public Module ModXflCatalog

    Friend Const Tag As String = "[XFL][模组库]"

    Private ReadOnly _catalog As New List(Of ModXflCatalogEntry)

    Friend ReadOnly Property Catalog As List(Of ModXflCatalogEntry)
        Get
            Return _catalog
        End Get
    End Property

    Friend Sub Init()
        If _catalog.Count > 0 Then Return
        Try
            _catalog.Add(New ModXflCatalogEntry("performance", "Sodium（渲染优化，帧数提升最明显）", "AANobbMI", "fabric,neoforge,quilt"))
            _catalog.Add(New ModXflCatalogEntry("performance", "Lithium（物理引擎优化）", "gvQqBUqZ", "fabric,neoforge,quilt"))
            _catalog.Add(New ModXflCatalogEntry("performance", "FerriteCore（降低内存占用）", "uXXizFIs", "fabric,forge,neoforge,quilt"))
            _catalog.Add(New ModXflCatalogEntry("performance", "ImmediatelyFast（Paper 性能优化）", "5ZwdcRci", "fabric,forge,neoforge,quilt"))
            _catalog.Add(New ModXflCatalogEntry("performance", "ModernFix（修复大量原版性能问题）", "nmDcB62a", "fabric,forge,neoforge"))
            _catalog.Add(New ModXflCatalogEntry("performance", "Entity Culling（剔除不可见实体）", "NNAgCjsB", "fabric,forge,neoforge,quilt"))
            _catalog.Add(New ModXflCatalogEntry("performance", "Krypton（优化网络）", "fQEb0iXm", "fabric"))
            _catalog.Add(New ModXflCatalogEntry("performance", "ScalableLux（光照优化）", "Ps1zyz6x", "fabric,neoforge"))
            _catalog.Add(New ModXflCatalogEntry("performance", "Alternate Current（电与红石优化）", "r0v8vy1s", "fabric,forge,neoforge,quilt"))
            _catalog.Add(New ModXflCatalogEntry("performance", "Fast Suite（合成加速）", "roccmqou", "forge,neoforge"))
            _catalog.Add(New ModXflCatalogEntry("performance", "LazyDFU（启动加速）", "hvFnDODi", "fabric"))
            _catalog.Add(New ModXflCatalogEntry("performance", "ThreadTweak（线程优化）", "vSEH1ERy", "fabric,quilt"))
            _catalog.Add(New ModXflCatalogEntry("performance", "Better Clouds（云朵优化）", "5srFLIaK", "fabric,forge,neoforge"))
            _catalog.Add(New ModXflCatalogEntry("shader", "Iris Shaders（光影，Fabric/NeoForge）", "YL57xq9U", "fabric,neoforge,quilt"))
            _catalog.Add(New ModXflCatalogEntry("shader", "Oculus（光影，Forge/NeoForge）", "GchcoXML", "forge,neoforge"))
            _catalog.Add(New ModXflCatalogEntry("shader", "Complementary Shaders（配套光影）", "YnXJmi7F", "fabric,neoforge"))
            _catalog.Add(New ModXflCatalogEntry("shader", "Continuity（连接纹理）", "1IjD5062", "fabric,forge,neoforge,quilt"))
            _catalog.Add(New ModXflCatalogEntry("shader", "Indium（光影兼容层）", "Orvt0mRa", "fabric,quilt"))
            _catalog.Add(New ModXflCatalogEntry("shader", "Distant Horizons（远景）", "uCdwusMi", "fabric,forge,neoforge"))
            _catalog.Add(New ModXflCatalogEntry("hud", "Just Enough Items（物品查询）", "u6dRKJwZ", "fabric,forge,neoforge"))
            _catalog.Add(New ModXflCatalogEntry("hud", "Jade（方块与实体信息）", "nvQzSEkH", "fabric,forge,neoforge,quilt"))
            _catalog.Add(New ModXflCatalogEntry("hud", "Just Enough Professions（职业信息）", "kB56GtWA", "fabric,forge,neoforge"))
            _catalog.Add(New ModXflCatalogEntry("hud", "AppleSkin（饥饿值显示）", "Q5IXwQJO", "forge,neoforge"))
            _catalog.Add(New ModXflCatalogEntry("hud", "Better F3（调试信息）", "V8TerSIX", "forge"))
            _catalog.Add(New ModXflCatalogEntry("hud", "Mod Menu（模组设置界面）", "mOgUt4GM", "fabric,quilt"))
            _catalog.Add(New ModXflCatalogEntry("hud", "Inventory Profiles（背包配置）", "Dxtrb61j", "fabric"))
            _catalog.Add(New ModXflCatalogEntry("hud", "Cloth Config（配置界面库）", "9s6osm5g", "fabric,forge,neoforge"))
            _catalog.Add(New ModXflCatalogEntry("map", "Waystones（传送石）", "LOpKHB2A", "fabric,forge,neoforge"))
            _catalog.Add(New ModXflCatalogEntry("map", "JourneyMap（小地图）", "lfHFW1mp", "fabric,forge,neoforge,quilt"))
            _catalog.Add(New ModXflCatalogEntry("map", "XaeroPlus（小地图）", "EnPUzSTg", "fabric,forge,neoforge"))
            _catalog.Add(New ModXflCatalogEntry("build", "Chime（随机建筑装饰）", "ubxU84eR", "fabric,quilt"))
            _catalog.Add(New ModXflCatalogEntry("build", "Create（自动化机械）", "LNytGWDc", "forge,neoforge"))
            _catalog.Add(New ModXflCatalogEntry("build", "World Editor（世界编辑）", "uRd37jgq", "fabric,forge,neoforge,quilt"))
            _catalog.Add(New ModXflCatalogEntry("visual", "Vanilla Tweaks（原版细节优化）", "2sTRh8zD", "fabric"))
            _catalog.Add(New ModXflCatalogEntry("visual", "Better Leaves（树叶优化）", "7XS8ewY9", "fabric"))
            Logger.Info($"{Tag} 模组知识库已载入：{_catalog.Count} 条")
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 模组知识库载入失败")
        End Try
    End Sub

    Friend Function TagsHint() As String
        Init()
        Dim sb As New System.Text.StringBuilder
        sb.AppendLine("【模组用途标签】当用户用描述性说法要模组时（而不是直接给出模组名），" &
                      "请选一个最贴切的标签，并把动作写成 " & Chr(34) & "act" & Chr(34) & ":" & Chr(34) &
                      "download_resource" & Chr(34) & "、" & Chr(34) & "tag" & Chr(34) & ":" &
                      Chr(34) & "<标签>" & Chr(34) & "（此时 query 留空）。可用标签：")
        sb.AppendLine("· performance —— 优化性能、让游戏更流畅、卡顿、掉帧、内存占用高、加载慢")
        sb.AppendLine("· shader —— 光影、画质、材质、画面增强")
        sb.AppendLine("· hud —— 物品查询、方块信息、职业信息、调试信息、背包整理、设置界面")
        sb.AppendLine("· map —— 地图、小地图、传送、旅行、导航")
        sb.AppendLine("· build —— 建造、装饰、机械、自动化、编辑世界")
        sb.AppendLine("· visual —— 美化、材质、原版细节")
        sb.AppendLine("（标签只是用途指引；启动器会挑出真正适合该 MC 版本与加载器的模组并列出给你确认。）")
        Return sb.ToString()
    End Function

    Friend Function ByTag(tag As String) As List(Of ModXflCatalogEntry)
        Init()
        Dim Result As New List(Of ModXflCatalogEntry)
        Try
            If String.IsNullOrWhiteSpace(tag) Then Return Result
            For Each e In _catalog
                If e.Tag.Equals(tag.Trim(), StringComparison.OrdinalIgnoreCase) Then Result.Add(e)
            Next
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 按标签取模组失败")
        End Try
        Return Result
    End Function

    Friend Function LoaderOk(entry As ModXflCatalogEntry, loader As String) As Boolean
        Try
            If entry Is Nothing Then Return False
            If String.IsNullOrWhiteSpace(entry.Loaders) OrElse String.IsNullOrWhiteSpace(loader) Then Return True
            Return entry.Loaders.ToLower.Contains(loader.Trim().ToLower)
        Catch
            Return True
        End Try
    End Function

End Module
