# 深度扫描结果 — FWTCG Unity 移植
> Phase 1 生成，之后不再修改
> 扫描日期：2026-03-28

---

## 深度扫描 1 — 硬编码数值和隐藏游戏规则

| 数值 | 来源 | 说明 |
|------|------|------|
| WIN_SCORE = 8 | engine.js:138 | 率先达到8分获胜 |
| 平局条件 | engine.js:203 | 双方同时达到8分 |
| 手牌上限 = 7 | engine.js:101,110,129,181 / combat.js:286 | 任何增加手牌操作的前置检查 |
| 战场单位槽位 = 2 | combat.js:52-56 | 每个战场每方最多2名单位 |
| 强力判定 atk >= 5 | engine.js:20 / combat.js:142-143 | 清算人竞技场自动赋予强攻/坚守 |
| 回合倒计时 = 30秒 | engine.js:63 | 玩家每回合限时 |
| 后手首回合符文 = 3 | engine.js:338-340 | 后手第一回合获得3张符文（之后每回合2张）|
| 普通回合符文 = 2 | engine.js:338-340 | 每回合正常获得2张符文 |
| 先手概率 = 50% | main.js:32,81 | Math.random() < 0.5 |
| 初始手牌 = 4张 | main.js:56-59 | 双方各抽4张 |
| 开局软加权触发率 = 67% | main.js:14 | Math.random() >= 0.33 时不触发 |
| 最后1分受限规则 | engine.js:173-185 | 得第8分须本回合主动征服所有战场 |
| 燃尽惩罚 = +1分 | engine.js:367 | 牌库耗尽时对手得1分 |
| 御衡守念费用减免 | spell.js:13-17 | 对手距胜利≤3分时费用-2（最低0）|
| 坚守加成 = +1 | combat.js:147 | guardBonus || 1 |
| 强攻加成 = +1 | combat.js:147 | strongAtkBonus || 1 |
| AI延迟 = 700ms | engine.js:254 / ai.js | aiAction()启动延迟 |
| 回合切换延迟 = 600ms | engine.js:398-399 | setTimeout startTurn |
| 唤醒延迟 = 700ms | engine.js:224 | runPhase('awaken') |
| 阶段切换延迟 = 650ms | engine.js:234,238,244 | 各阶段间隔 |
| 符文牌堆 炽烈×7 灵光×5 | main.js:44 | 卡莎虚空符文堆（共12张）|
| 符文牌堆 翠意×6 摧破×6 | main.js:45 | 易大师伊欧尼亚符文堆（共12张）|

---

## 深度扫描 2 — 配置和数据文件

### 卡牌数据结构（ScriptableObject 字段参考）
```
id: string           卡牌唯一标识符
name: string         显示名称
region: string       地域（void/ionia/noxus/demacia等）
type: string         类型（follower/spell/equipment/champion）
cost: int            基础法力费用
atk: int             战力（同时是HP，atk=HP规则）
hp: int              血量（仅传奇独立HP，其余等于atk）
keywords: string[]   关键词列表
text: string         效果文字
emoji: string        表情符号
effect: string       效果标识符
schType: string      符能类型要求
schCost: int         符能费用
```

### 卡莎卡组（KAISA_MAIN）— 40张
**单位（19张）：** noxus_recruit×2, alert_sentinel×3, yordel_instructor×3, bad_poro×2, rengar×2, kaisa_hero×1, darius×1, thousand_tail×3, foresight_mech×2
**法术（21张）：** swindle×3, void_seek×1, evolve_day×1, retreat_rune×2, furnace_blast×2, guilty_pleasure×1, starburst×1, hex_ray×2, time_warp×2, stardrop×3, smoke_bomb×1, divine_ray×1, akasi_storm×1

### 易大师卡组（MASTERYI_MAIN）— 30张
**单位（8张）：** yi_hero×1, jax×2, tiyana_warden×2, wailing_poro×3
**法术（22张）：** scoff×3, duel_stance×2, well_trained×3, wind_wall×2, rally_call×3, balance_resolve×3, flash_counter×2, slam×2, strike_ask_later×2
**装备（8张）：** zhonya×1, trinity_force×2, guardian_angel×2, dorans_blade×3, sandshoal_deserter×2

### 传奇卡
**卡莎（kaisa）：** cost:5 / atk:5 / hp:14 / 关键词：迅捷攻击 / 主动技能：反应-休眠自身获得符能 / 被动：盟友4种关键词→升级+3/+3
**易大师（masteryi）：** cost:5 / atk:5 / hp:12 / 被动：防守单位仅1名时+2战力

### 战场卡牌（20张）
altar_unity, aspirant_climb, back_alley_bar, bandle_tree, hirana, reaver_row, reckoner_arena, dreaming_tree, vile_throat_nest, rockfall_path, sunken_temple, trifarian_warcamp, void_gate, zaun_undercity, strength_obelisk, star_peak, thunder_rune, ascending_stairs, forgotten_monument（共19个，不含分配逻辑）

### 战场牌池分配
- 卡莎牌池：star_peak, void_gate, strength_obelisk
- 易大师牌池：thunder_rune, ascending_stairs, forgotten_monument
- 战场选择：各方从己方牌池随机抽1个

---

## 深度扫描 3 — 用户交互流程链

### 流程1：出牌流程（跨越3+文件）
```
点击手牌(ui.js) → 费用检查 getCannotPlayReasons(spell.js) → 目标选择 getSpellTargets(spell.js) → 费用扣除(spell.js) → applySpell结算(spell.js) → triggerLegendEvent(legend.js) → render(ui.js)
```

### 流程2：单位移动与战斗流程（跨越3+文件）
```
onUnitClick(combat.js) → 休眠/眩晕检查 → G.selUnits追加 → onBFClick(combat.js) → 槽位检查 → G.pendingMove设置 → confirmMove(combat.js) → moveUnit(combat.js) → 战场能力触发(combat.js) → startSpellDuel(spell.js) → 反应窗口(engine.js) → triggerCombat(combat.js) → cleanDead → checkLegendPassives(legend.js) → checkWin(engine.js) → render(ui.js)
```

### 流程3：拖拽出牌流程（跨越2+文件）
```
startDrag覆写(dragAnim.js) → 漩涡视觉效果 → 检测放置目标(dragAnim.js) → 资源检查(spell.js) → 单位/法术/装备分发执行(spell.js/combat.js) → 动画结束(dragAnim.js) → render(ui.js)
```

### 流程4：符文横置与符能获取（跨越2文件）
```
点击符文(ui.js) → tapRune/tapAllRunes(hint.js) → G.pendingRunes追加 → confirmRunes(hint.js) → r.tapped=true / addSch(engine.js) → G.pMana++ → 结束阶段 resetSch(engine.js) → render(ui.js)
```

### 流程5：回合结束流程（跨越3文件）
```
playerEndTurn(engine.js) → clearTurnTimer → doEndPhase(engine.js) → 眩晕清除 → currentHp=currentAtk → tb归零 → resetSch → extraTurnPending检查 → startTurn('enemy')(engine.js) → 展示banner → runPhase→aiAction(ai.js)
```

### 流程6：游戏启动/掷硬币流程（跨越2文件）
```
startGame(main.js) → 随机选阵营 → 初始化卡组/传奇/符文牌堆 → seedPlayerOpeningHand软加权 → 各抽4张手牌 → showCoinFlip(main.js/ui.js) → G.first随机确定 → 硬币动画1800ms → showBFSelect → confirmBFSelect → showMulligan → 玩家选≤2张换牌 → startTurn(G.first)(engine.js)
```

---

## 关键移植陷阱

1. **atk = HP 规则**：`currentHp` 和 `currentAtk` 必须始终同步，`dealDamage()` 是唯一入口
2. **dragAnim.js 覆写陷阱**：`startDrag()` 在 spell.js 中的实现是死代码，实际由 dragAnim.js 完全覆写
3. **async/await 流程**：`doStart()`, `doDraw()`, `askPrompt()` 都是异步的，Unity 需用协程或 async/await 实现
4. **反应窗口系统**：`reactionWindowOpen` 冻结 AI 行动，允许玩家在 AI 行动间响应，是最复杂的交互机制之一
5. **最后1分受限规则**：容易遗漏，`bfConqueredThisTurn` 专门追踪本回合主动征服情况
