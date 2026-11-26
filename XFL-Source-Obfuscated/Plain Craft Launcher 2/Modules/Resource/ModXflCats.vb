
Public Class ModXflCatEntry

    Public CatId As String

    Public Title As String

    Public Keywords As String

    Public Sub New(catId As String, title As String, keywords As String)
        Me.CatId = catId
        Me.Title = title
        Me.Keywords = keywords
    End Sub
End Class

Public Module ModXflCats

    Friend Const Tag As String = "[XFL][分类]"

    Private ReadOnly _cats As New List(Of ModXflCatEntry)

    Friend ReadOnly Property Cats As List(Of ModXflCatEntry)
        Get
            Return _cats
        End Get
    End Property

    Friend Sub Init()
        If _cats.Count > 0 Then Return
        Try
            _cats.Add(New ModXflCatEntry("optimization", "优化性能", "优化 性能 流畅 卡顿 掉帧 内存 fps 加载慢 稳定"))
            _cats.Add(New ModXflCatEntry("decoration", "装饰美化", "装饰 美化 好看 装饰方块 家具 景观 颜色 材质"))
            _cats.Add(New ModXflCatEntry("technology", "科技机械", "科技 机械 自动化 电力 机器 工业 红石 电路"))
            _cats.Add(New ModXflCatEntry("worldgen", "世界生成", "世界生成 地形 生物群系 矿物 结构 地牢 地形"))
            _cats.Add(New ModXflCatEntry("social", "社交联机", "社交 联机 聊天 语音 好友 队伍 多人 消息"))
            _cats.Add(New ModXflCatEntry("library", "前置库", "前置 库 api 依赖 工具库"))
            _cats.Add(New ModXflCatEntry("gameplay", "玩法增强", "玩法 游戏性 机制 增强 挑战 生存"))
            _cats.Add(New ModXflCatEntry("adventure", "冒险探索", "冒险 探索 地牢 任务 副本 遗迹 boss"))
            _cats.Add(New ModXflCatEntry("combat", "战斗玩法", "战斗 武器 装备 技能 伤害 pvp 武器附魔"))
            _cats.Add(New ModXflCatEntry("magic", "魔法", "魔法 附魔 咒语 法师 奥术 法术"))
            _cats.Add(New ModXflCatEntry("economy", "经济", "经济 商店 交易 货币 银行 村民"))
            _cats.Add(New ModXflCatEntry("food", "食物农业", "食物 农业  farming 种植 食物 作物 烹饪"))
            _cats.Add(New ModXflCatEntry("building", "建造", "建造 房子 装饰 建筑 放置 家具"))
            _cats.Add(New ModXflCatEntry("utility", "实用工具", "工具 实用 辅助 便利 小工具"))
            _cats.Add(New ModXflCatEntry("misc", "杂项", "杂项 其他 通用"))
            _cats.Add(New ModXflCatEntry("realistic", "写实增强", "写实 真实 沉浸 拟真"))
            _cats.Add(New ModXflCatEntry("adjustment", "视觉调整", "视觉 调整 界面 主题 字体 着色"))
            _cats.Add(New ModXflCatEntry("animatic", "动画", "动画 动画化 挥手 表情 动作"))
            _cats.Add(New ModXflCatEntry("audio", "音效", "音效 声音 音乐 氛围"))
            _cats.Add(New ModXflCatEntry("storage", "仓储整理", "仓储 整理 仓库 背包 箱子 存储"))
            _cats.Add(New ModXflCatEntry("screen", "界面显示", "界面 显示 hud 状态栏 指示器"))
            _cats.Add(New ModXflCatEntry("survival", "生存辅助", "生存 辅助 难度 饥饿 经验"))
            _cats.Add(New ModXflCatEntry("tools", "工具", "工具 工具类 效率 挖掘"))
            _cats.Add(New ModXflCatEntry("textures", "材质", "材质 贴图 纹理 皮肤 动画 资源包"))
            Logger.Info($"{Tag} 官方分类映射已载入：{_cats.Count} 个")
        Catch ex As Exception
            Logger.Warn(ex, $"{Tag} 分类映射载入失败")
        End Try
    End Sub

    Friend Function ById(catId As String) As ModXflCatEntry
        Init()
        Try
            If String.IsNullOrWhiteSpace(catId) Then Return Nothing
            For Each c In _cats
                If c.CatId.Equals(catId.Trim(), StringComparison.OrdinalIgnoreCase) Then Return c
            Next
        Catch
        End Try
        Return Nothing
    End Function

    Friend Function CatsHint() As String
        Init()
        Dim sb As New System.Text.StringBuilder
        sb.AppendLine("【Minecraft 模组分类】用户描述想要哪类模组时，请选一个最贴切的分类，")
        sb.AppendLine("并把动作写成 " & Chr(34) & "act" & Chr(34) & ":" & Chr(34) & "download_resource" & Chr(34) & "、")
        sb.AppendLine(Chr(34) & "category" & Chr(34) & ":" & Chr(34) & "<分类ID>" & Chr(34) & "（此时 query 留空）。可用分类：")
        For Each c In _cats
            sb.AppendLine("· " & c.CatId & " —— " & c.Title & "（" & c.Keywords & "）")
        Next
        sb.AppendLine("若用户已经直接给出模组名，就用 query 而不是 category。")
        sb.AppendLine("（分类只用来圈定范围，启动器会列出该分类下适配当前版本的模组让你勾选。）")
        Return sb.ToString()
    End Function

End Module
