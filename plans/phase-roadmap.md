# Phase 路线图 — FWTCG Unity 移植
> 生成于 2026-03-29。每个 Phase 是可独立运行的最小 demo。
> 规则：每个功能必须归属某个 Phase，或标注 OUT OF SCOPE。不允许模糊推迟。

---

## 已完成

| Phase | 可玩内容 | 核心交付 |
|-------|---------|---------|
| DEV-1 | 双人对战基础框架 | GameState / TurnManager / CombatSystem / ScoreManager / 简化卡组 |
| DEV-2 | 完整卡组 + 入场效果 + 开局流程 | CardData扩展 / EntryEffectSystem / DeathwishSystem / StartupFlowUI |
| DEV-3 | 可打出10张非反应法术 | SpellSystem / 目标选择 / CardView视觉区分 |
| DEV-4 | 反应牌系统（含法力扣除） | ReactiveSystem / ReactiveWindowUI / AI施法 / 9张反应牌 |

---

## DEV-5 — 符文回收 + 装备系统
**可玩 demo**：玩家可回收符文换取符能；打出装备牌附着到单位获得属性加成；AI 会自动横置符文

| 功能 | 类别 |
|------|------|
| 符文回收获得符能（recycle → addSch，打通按钮逻辑）| 符文系统 |
| 增益指示物系统（buffToken +1/+1，永久，不可叠加）| 单位状态 |
| 装备卡附着机制（装备从手牌打出→选择己方单位→附着，给该单位+stat）| 卡牌系统 |
| dorans_blade（+2战力，费用1摧破符能）| 装备效果 |
| trinity_force（+2战力，据守额外+1分，费用1摧破符能）| 装备效果 |
| guardian_angel（+1战力，死亡保护一次，费用1翠意符能）| 装备效果 |
| zhonya（待命：死亡保护一次，费用0）| 装备效果 |
| AI 符文操作（AI 回合自动横置符文补充法力）| AI |
| 待确认符文操作队列（pendingRunes）| **OUT OF SCOPE** — 改为即时执行，无需中间状态 |

---

## DEV-6 — 剩余法术 + 单位入场效果 + 关键词完整化
**可玩 demo**：两个卡组所有卡牌效果全部可触发；强攻/坚守在战斗中生效；时间扭曲给额外回合

| 功能 | 类别 |
|------|------|
| 强攻关键词（StrongAtk：战斗中+1 atk）| 关键词 |
| 坚守关键词（Standby：防守时+1 def）| 关键词 |
| 壁垒关键词（Barrier：必须先承受致命伤害）| 关键词（已有字段，实现战斗逻辑）|
| 回响关键词机制（本回合已打出过牌 → 额外效果触发）| 关键词 |
| furnace_blast（回响：1点伤害×3单位）| 卡莎法术 |
| divine_ray（回响+2炽烈符能，2点伤害×2次）| 卡莎法术 |
| time_warp（额外回合：extraTurnPending=true）| 卡莎法术 |
| noxus_recruit 入场（鼓舞：下一个进场盟友+1战力）| 卡莎单位 |
| bad_poro 征服触发（征服该战场时获得+1炽烈符能）| 卡莎单位 |
| rengar 入场（获得强攻+1炽烈符能）| 卡莎单位 |
| kaisa_hero 入场（征服触发：+1炽烈符能）| 卡莎单位 |
| yi_hero 入场（游走+急速+1摧破符能）| 易大师单位 |
| sandshoal_deserter 入场（法盾：法术无法将其选为目标）| 易大师单位 |
| balance_resolve 费用-2条件效果（手牌目标选择UI：选1张牌降费2）| 卡莎法术 |
| 溢出伤害检查（压制关键词）| **OUT OF SCOPE** — 传奇不可被摧毁，溢出伤害无处落点 |
| 强力判定 atk >= 5（reckoner_arena战场用）| DEV-8 |

---

## DEV-7 — 传奇系统
**可玩 demo**：传奇卡显示在传奇区；卡莎/易大师传奇技能可使用（被动/触发/主动）；进化升级可触发
> 注：传奇不参与战斗，不可被摧毁，无HP概念。胜利条件仍为纯得分制（规则167.3 / 633）。

| 功能 | 类别 |
|------|------|
| 传奇卡数据结构（abilities数组：被动/触发/主动技能定义）| 数据 |
| 卡莎传奇卡（kaisa_hero_legend，含虚空感知主动+进化被动）| 数据 |
| 易大师传奇卡（masteryi_legend，含独影剑鸣被动）| 数据 |
| 传奇初始化（游戏开始时置于传奇区，不进牌库）| 启动流程 |
| 英雄牌单独提取（游戏开始时从牌库抽出，放入英雄区，不进手牌）| 启动流程 |
| 传奇不占基地/战场槽位（独立传奇区显示）| 区域 |
| 英雄区域（独立于基地/战场，英雄牌在此等待打出）| 区域 |
| 卡莎传奇：虚空感知（主动·反应，休眠自身，获得+1炽烈符能）| 卡莎技能 |
| 卡莎传奇：进化被动（己方盟友持有4种不同关键词 → 传奇升级+3/+3）| 卡莎技能 |
| 技能本回合使用限制（resetLegendAbilitiesForTurn，每回合一次）| 核心逻辑 |
| 易大师传奇：独影剑鸣被动（该战场防守方仅1名单位时+2战力）| 易大师技能 |
| checkLegendPassives 战后触发| 核心逻辑 |
| triggerLegendEvent 事件接口| 核心逻辑 |
| 主动技能激活入口（玩家点击传奇区面板按钮）| UI |
| AI 传奇技能决策（被动自动/主动按优先级）| AI |
| 传奇升级动画（level 1→2 特效）| **DEV-10**（依赖VFX系统）|

---

## DEV-8 — 战场卡牌特殊能力
**可玩 demo**：两块战场（各方从牌池随机选）的特殊效果真正改变游戏策略

| 功能 | 类别 |
|------|------|
| 19张战场卡完整数据（id/name/描述/效果标签）| 数据 |
| 战场牌池分配（卡莎池3张 / 易大师池3张，各随机选1）| 数据 |
| void_gate — 被动：法术造成伤害额外+1 | 战场效果 |
| ascending_stairs — 被动：据守时额外+1分 | 战场效果 |
| forgotten_monument — 被动：第3回合前无据守得分 | 战场效果 |
| dreaming_tree — 被动：每回合首次法术结算后抽1张 | 战场效果 |
| reckoner_arena — 被动：战力≥5的单位自动获得强攻+坚守 | 战场效果 |
| back_alley_bar — 被动：单位移动离开此战场时+1战力 | 战场效果 |
| vile_throat_nest — 被动：禁止此处单位撤回基地 | 战场效果 |
| rockfall_path — 被动：禁止玩家直接将单位打出到此战场 | 战场效果 |
| altar_unity — 据守触发：召唤1/1新兵到该战场 | 战场效果 |
| strength_obelisk — 据守触发：额外召出1张符文 | 战场效果 |
| star_peak — 据守触发：召出1枚休眠符文（玩家选择类型）| 战场效果 |
| thunder_rune — 征服触发：回收1张符文（玩家选择）| 战场效果 |
| trifarian_warcamp — 据守触发：控制者获得增益指示物 | 战场效果 |
| bandle_tree — 据守触发：场上≥3种符文特性则获得+1法力 | 战场效果 |
| aspirant_climb — 据守触发：支付1法力，基地所有单位+1战力 | 战场效果 |
| hirana — 征服触发：消耗1个增益指示物，抽1张牌 | 战场效果 |
| sunken_temple — 防守失败触发：支付2法力，抽1张牌 | 战场效果 |
| zaun_undercity — 征服触发：弃1张牌，抽1张牌 | 战场效果 |
| reaver_row — 征服触发：从废牌堆捞≤2费单位进场 | **OUT OF SCOPE** — 需要复杂的废牌堆浏览+选择UI，收益低 |

---

## DEV-9 — UI 完整改版
**可玩 demo**：完整视觉布局，告别纯文字列表；所有交互有视觉反馈

| 功能 | 类别 |
|------|------|
| 主战场布局（1920×1080基准，Scale With Screen Size，支持4K）| 布局 |
| 双战场区域 UI（BF1 / BF2，明确分区）| 布局 |
| 玩家基地区域 UI | 布局 |
| 敌方基地区域 UI | 布局 |
| 手牌区域 UI（横排，最多显示全部手牌）| 布局 |
| 传奇区域 UI（玩家/敌方独立英雄区，HP显示）| 布局 |
| 符文区域 UI（显示已召出符文，横置状态）| 布局 |
| 得分显示 UI（X/8）| 布局 |
| 法力/符能显示 UI | 布局 |
| 回合/阶段显示 UI | 布局 |
| 消息/日志区域 UI（滚动列表）| 布局 |
| 结束回合按钮（重新美化）| 布局 |
| 完整卡牌 Prefab（图片/名称/费用/战力/关键词/描述文字）| 卡牌 |
| 卡牌状态视觉（休眠变暗 / 眩晕标记 / 增益指示物标记）| 卡牌 |
| 费用不足红色高亮（selFailUid）| 卡牌 |
| 传奇卡特殊显示（HP独立显示，金色边框）| 卡牌 |
| 装备卡显示（附着在单位上的小图标）| 卡牌 |
| 游戏结束界面（最终得分 + 胜负原因 + 再来一局按钮）| 特殊界面 |
| 横幅提示（showBanner：回合开始 / 得分事件）| 特殊界面 |
| 对决界面（法术对决期间弹出提示框）| 特殊界面 |
| UI 引用改造（移除 GameObject.Find，改为 Inspector 直接引用）| 技术债 |
| 卡牌背面 Prefab | **OUT OF SCOPE** — 单人 vs AI 模式，对手手牌不可见即可，无需独立背面Prefab |

---

## DEV-10 — 动画 + 交互体验
**可玩 demo**：所有操作有流畅动画反馈，视觉体验接近完成品

| 功能 | 类别 |
|------|------|
| 悬停放大 + 发光效果 | 卡牌交互 |
| 可出牌提示（轻微上移 + 发光边框）| 卡牌交互 |
| 目标选择高亮（合法目标闪烁 / 非法目标变灰）| 卡牌交互 |
| 单位点击选中视觉（选中高亮 / 取消）| 战场交互 |
| 战场点击视觉反馈 | 战场交互 |
| 战斗动画（单位冲向目标，回弹）| 战斗 |
| 伤害数字飘出（受击时显示数字）| VFX |
| 单位阵亡特效 | VFX |
| 法术施放特效（光效）| VFX |
| 征服/得分特效（横幅 + 粒子）| VFX |
| 对决横幅动画（showDuelBanner）| VFX |
| 掷硬币翻转动画（1800ms）| VFX（解决技术债）|
| 传奇升级动画（level 1→2，+3/+3 特效）| VFX |
| 3D 倾斜效果（鼠标悬停时 3D rotation）| **OUT OF SCOPE** — 视觉噱头，实现复杂，不影响核心体验 |
| 拖拽出牌（漩涡/传送门动画）| **OUT OF SCOPE** — 点击出牌已满足需求，拖拽实现成本高 |
| 确认移动按钮（可选，已有点击流程）| **OUT OF SCOPE** — 当前点击流程已足够清晰 |

---

## DEV-11 — AI 升级 + 游戏完整性
**可玩 demo**：AI 有挑战性；有倒计时压力；反应牌双方均可使用；游戏体验完整封闭

| 功能 | 类别 |
|------|------|
| 回合倒计时（30秒，时间到自动结束玩家回合）| 游戏逻辑 |
| 软加权开局手牌（67%触发，优先抽≤2费单位）| 启动流程 |
| 局面评分（aiBoardScore：战场价值 + 手牌 + 得分差）| AI |
| AI 出牌决策优化（选最高价值目标，不盲目出牌）| AI |
| AI 反应窗口处理（玩家施法时AI也能打出反应牌）| AI（解决技术债）|
| 反应牌目标选择UI（玩家打出反应牌时手动选目标）| UI（解决技术债）|
| AI 符文回收决策（何时横置 vs 回收）| AI |
| AI 延迟链（_aiNextAction）| **OUT OF SCOPE** — 当前 async/await 模式已满足需求，延迟链过度设计 |

---

## 完整功能 × Phase 对照表

### 一、核心游戏逻辑

| 功能 | Phase |
|------|-------|
| G 全局状态对象 | ✅ DEV-1 |
| 回合系统六阶段 | ✅ DEV-1 |
| 回合倒计时（30秒）| DEV-11 |
| 胜利判定（8分/平局）| ✅ DEV-1 |
| 得分系统 | ✅ DEV-1 |
| 双战场区域 | ✅ DEV-1 |
| 战场控制权 | ✅ DEV-1 |
| 六阶段回合流程 | ✅ DEV-1 |
| atk=HP 规则 | ✅ DEV-1 |
| 增益指示物系统（buffToken）| DEV-5 |
| 休眠/眩晕状态 | ✅ DEV-1 |
| 临时战力加成（TempAtkBonus）| ✅ DEV-4（已实现字段）|
| 绝念触发系统 | ✅ DEV-2 |
| 单位移动 | ✅ DEV-1 |
| 战斗触发（自动争夺）| ✅ DEV-1 |
| 伤害分配（顺序吸收）| ✅ DEV-1 |
| 强攻关键词 | DEV-6 |
| 坚守关键词 | DEV-6 |
| 壁垒关键词（战斗伤害优先）| DEV-6 |
| 溢出伤害→传奇 | DEV-7 |
| 战斗结果判定 | ✅ DEV-1 |
| 战后清理 | ✅ DEV-1 |
| 眩晕战力=0 | ✅ DEV-1 |
| 征服得分条件 | ✅ DEV-1 |
| 空战场征服 | ✅ DEV-1 |
| 最后一分规则 | ✅ DEV-1 |
| 召回行动 | ✅ DEV-1 |
| 反应/迅捷处理 | ✅ DEV-4 |
| 反应窗口 | ✅ DEV-4 |
| 待确认符文队列（pendingRunes）| **OUT OF SCOPE** |

### 二、卡牌数据系统

| 功能 | Phase |
|------|-------|
| CardData ScriptableObject | ✅ DEV-1 |
| 关键词枚举（全套）| ✅ DEV-2 |
| 装备卡数据结构（附着机制）| DEV-5 |
| 传奇卡数据结构（abilities数组）| DEV-7 |
| 卡莎主卡组（19张）| ✅ DEV-2 |
| 易大师主卡组（10张）| ✅ DEV-2 |
| 卡莎传奇卡 | DEV-7 |
| 易大师传奇卡 | DEV-7 |
| 19张战场卡数据 | DEV-8 |
| 战场牌池分配 | DEV-8 |
| 符文横置获得法力 | ✅ DEV-1 |
| 符文回收获得符能 | DEV-5 |
| 符能费用检查与扣除 | ✅ DEV-1 |
| 回合结束符能清零 | ✅ DEV-1 |
| 符文区上限12张 | ✅ DEV-1 |

### 三、法术/卡牌效果

| 功能 | Phase |
|------|-------|
| SpellSystem 主入口 | ✅ DEV-3 |
| void_seek | ✅ DEV-3 |
| evolve_day | ✅ DEV-3 |
| starburst | ✅ DEV-3 |
| hex_ray（迅捷）| ✅ DEV-3 |
| stardrop | ✅ DEV-3 |
| akasi_storm | ✅ DEV-3 |
| rally_call | ✅ DEV-3 |
| balance_resolve（基础）| ✅ DEV-3 |
| balance_resolve 费用-2条件 | DEV-6 |
| slam | ✅ DEV-3 |
| strike_ask_later | ✅ DEV-3 |
| furnace_blast（回响）| DEV-6 |
| divine_ray（回响）| DEV-6 |
| time_warp（额外回合）| DEV-6 |
| swindle（反应）| ✅ DEV-4 |
| retreat_rune（反应）| ✅ DEV-4 |
| guilty_pleasure（反应）| ✅ DEV-4 |
| smoke_bomb（反应）| ✅ DEV-4 |
| scoff（反应）| ✅ DEV-4 |
| duel_stance（反应）| ✅ DEV-4 |
| well_trained（反应）| ✅ DEV-4 |
| wind_wall（反应）| ✅ DEV-4 |
| flash_counter（反应）| ✅ DEV-4 |
| noxus_recruit 入场 | DEV-6 |
| alert_sentinel 绝念 | ✅ DEV-2 |
| yordel_instructor 入场 | ✅ DEV-2 |
| bad_poro 征服触发 | DEV-6 |
| rengar 入场 | DEV-6 |
| kaisa_hero 入场 | DEV-6 |
| darius 入场 | ✅ DEV-2 |
| thousand_tail 入场 | ✅ DEV-2 |
| foresight_mech 预知 | ✅ DEV-2 |
| yi_hero 入场 | DEV-6 |
| jax 入场 | ✅ DEV-2 |
| tiyana_warden 被动 | ✅ DEV-2 |
| wailing_poro 绝念 | ✅ DEV-2 |
| sandshoal_deserter 法盾 | DEV-6 |
| dorans_blade | DEV-5 |
| trinity_force | DEV-5 |
| guardian_angel | DEV-5 |
| zhonya | DEV-5 |

### 四、UI 系统

| 功能 | Phase |
|------|-------|
| 主战场布局（1920×1080）| DEV-9 |
| 双战场/基地/手牌/传奇/符文区域 UI | DEV-9 |
| 得分/法力/符能/回合/消息显示 UI | DEV-9 |
| 完整卡牌 Prefab | DEV-9 |
| 卡牌背面 Prefab | **OUT OF SCOPE** |
| 传奇卡显示（HP）| DEV-9 |
| 装备卡显示（附着图标）| DEV-9 |
| 卡牌状态视觉（休眠/眩晕/增益）| DEV-9 |
| 费用不足红色高亮 | DEV-9 |
| 悬停放大 + 发光 | DEV-10 |
| 可出牌提示 | DEV-10 |
| 3D 倾斜效果 | **OUT OF SCOPE** |
| 拖拽出牌 | **OUT OF SCOPE** |
| 目标选择高亮 | DEV-10 |
| 战斗动画 | DEV-10 |
| 游戏结束界面 | DEV-9 |
| 横幅提示 | DEV-9 |
| 对决界面 | DEV-9 |
| 掷硬币动画 | DEV-10 |

### 五、AI 系统

| 功能 | Phase |
|------|-------|
| AI 移动多单位 | ✅ DEV-1 |
| AI 施法（非反应）| ✅ DEV-4 |
| AI 符文横置 | DEV-5 |
| AI 符文回收决策 | DEV-11 |
| AI 反应窗口处理 | DEV-11 |
| 局面评分 | DEV-11 |
| AI 出牌决策优化 | DEV-11 |
| AI 延迟链 | **OUT OF SCOPE** |

### 六、传奇系统

| 功能 | Phase |
|------|-------|
| 传奇卡数据结构（abilities数组）| DEV-7 |
| 传奇初始化 | DEV-7 |
| 传奇不占基地/战场槽位 | DEV-7 |
| 卡莎：虚空感知（主动·反应）| DEV-7 |
| 卡莎：进化被动 | DEV-7 |
| 卡莎：升级动画 | DEV-10 |
| 技能使用限制 | DEV-7 |
| 易大师：独影剑鸣被动 | DEV-7 |
| checkLegendPassives | DEV-7 |
| triggerLegendEvent | DEV-7 |
| AI 传奇技能决策 | DEV-7 |

### 七、战场卡牌特殊能力（19张）

| 功能 | Phase |
|------|-------|
| void_gate | DEV-8 |
| ascending_stairs | DEV-8 |
| forgotten_monument | DEV-8 |
| dreaming_tree | DEV-8 |
| reckoner_arena | DEV-8 |
| back_alley_bar | DEV-8 |
| vile_throat_nest | DEV-8 |
| rockfall_path | DEV-8 |
| altar_unity | DEV-8 |
| strength_obelisk | DEV-8 |
| star_peak | DEV-8 |
| thunder_rune | DEV-8 |
| trifarian_warcamp | DEV-8 |
| bandle_tree | DEV-8 |
| aspirant_climb | DEV-8 |
| hirana | DEV-8 |
| sunken_temple | DEV-8 |
| zaun_undercity | DEV-8 |
| reaver_row | **OUT OF SCOPE** — 废牌堆浏览+选择UI成本过高 |

### 八、启动流程

| 功能 | Phase |
|------|-------|
| 随机阵营分配 | ✅ DEV-1 |
| 符文牌堆初始化 | ✅ DEV-1 |
| 初始手牌 | ✅ DEV-1 |
| 掷硬币先手 | ✅ DEV-2 |
| 战场随机选择 | ✅ DEV-2 |
| 梦想手牌调度（换牌）| ✅ DEV-2 |
| 卡组初始化（英雄卡提取）| DEV-7 |
| 软加权开局手牌（67%）| DEV-11 |
| 传奇初始化 | DEV-7 |

---

## OUT OF SCOPE 汇总

| 功能 | 原因 |
|------|------|
| 待确认符文操作队列（pendingRunes）| 改为即时执行，无需中间状态 |
| 卡牌背面 Prefab | 单人 vs AI 模式，隐藏对手手牌不显示即可 |
| 3D 倾斜效果（hover tilt）| 视觉噱头，实现复杂，不影响核心体验 |
| 拖拽出牌（dragAnim.js）| 点击出牌已满足需求；拖拽实现成本高 |
| AI 延迟链（_aiNextAction）| 当前 async/await 已满足，延迟链属过度设计 |
| reaver_row 战场能力 | 废牌堆浏览+单位选择UI复杂度过高，功能边际收益低 |
| 确认移动按钮 | 当前点击流程已足够清晰 |

---

*最后更新：2026-03-29*
