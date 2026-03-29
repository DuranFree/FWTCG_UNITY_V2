# 技术债清单
> 发现时追加，解决后删除
> 格式：- [ ] <描述> — 原因：<why deferred> — Phase <number>

- ✅ TMP Essential Resources 问题 — 已解决：全项目切换至 Legacy UnityEngine.UI.Text，TMP 彻底废弃 — Phase DEV-1
- ✅ CardData ScriptableObject 字段扩展 — DEV-2 已添加 keywords/effectId/isEquipment/equipAtkBonus/equipRuneType/equipRuneCost — Phase DEV-2
- [ ] StartupFlowUI 掷硬币无动画 — DEV-2 仅显示文字，动画效果（1800ms翻转）推迟到 DEV-3 — Phase DEV-2
- [ ] 卡牌图片未导入 — tempPic/cards/ 中文件名无法与卡牌ID对应，需用户手动重命名或提供映射表 — Phase DEV-2
- [ ] UI 引用在 batch mode 下通过 GameObject.Find 连线，运行时若场景结构变化会失效 — Phase DEV-1
