# 已知 Bug 清单
> 发现时追加，修复后标记 ✅，不删除
> 格式：- [ ] <描述> — 发现于 Phase <number>

- ✅ TMP 文字不显示 — 已修复：全项目切换至 Legacy Text，不再依赖 TMP — 发现于 Phase DEV-1
- ✅ GameScene UI 无响应 — 已修复：SceneBuilder 添加 EventSystem + StandaloneInputModule — 发现于 Phase DEV-1
- ✅ 卡在7分无法获胜 — 已修复：删除错误的最后1分受限规则（规则不存在）— 发现于 Phase DEV-1
- ✅ 战场每方只能放2张单位 — 已修复：删除 MAX_BF_UNITS 限制（规则不存在）— 发现于 Phase DEV-1
- ✅ 手牌莫名丢弃 — 已修复：删除 MAX_HAND_SIZE = 7 限制（规则不存在）— 发现于 Phase DEV-1
- ✅ GameStateTests.cs 编译错误 — 已修复：移除对已删除常量 MAX_HAND_SIZE/MAX_BF_UNITS 的引用 — 发现于 Phase DEV-1
- ✅ Debug面板只显示2个按钮 — 已修复：VerticalLayoutGroup.childControlHeight 改为 true 使 preferredHeight 生效 — 发现于 DEV-3
- ✅ 法术目标无法点击 — 已修复：敌方单位 RefreshUnitList 改为传入 _onUnitClicked 而非 null — 发现于 DEV-3
- ✅ 日志文字被右边裁切 — 已修复：MessageText 的 horizontalOverflow 改为 Wrap — 发现于 DEV-3
- ✅ 符文牌右键回收无反应 — 已修复：EventTrigger从root移到RuneCircle（Button在circle上拦截事件）— 发现于 DEV-11
- ✅ 符文牌被上下拉伸 — 已修复：HLG关闭childControlHeight + LayoutElement固定46×46 — 发现于 DEV-11
- ✅ SpellShowcasePanel coroutine inactive 报错（Lazy Awake 陷阱）— 彻底修复：Awake 改用 CanvasGroup Hide()，不再 SetActive(false)，面板始终保持 active — 发现于 DEV-16b，根治于 DEV-16c
- ✅ SpellTargetPopup coroutine inactive 同类问题 — 已修复：同上 CanvasGroup 方案 — 发现于 DEV-16c
- ✅ CardView 重复 Unit 属性编译错误 — 已修复：移除行39的重复定义，保留行299原有属性 — 发现于 DEV-16b
- ✅ AI 不出单位牌（符能被提前耗尽）— 已修复：AiRecycleRunes 跳过本回合法力不足的卡牌，防止无意义的 sch 预消耗 — 发现于 DEV-16c
