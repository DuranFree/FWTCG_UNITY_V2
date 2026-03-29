using NUnit.Framework;
using System.Collections.Generic;
using FWTCG.Core;
using FWTCG.Data;
using FWTCG.Systems;

namespace FWTCG.Tests
{
    /// <summary>
    /// DEV-5: Legend system tests
    /// — LegendInstance data integrity
    /// — Kaisa active (虚空感知): usage, cooldown, reset
    /// — Kaisa passive (进化): evolution condition
    /// — Masteryi passive (独影剑鸣): lone defender buff
    /// </summary>
    public class DEV5LegendTests
    {
        private GameState _gs;
        private LegendSystem _legendSys;

        [SetUp]
        public void SetUp()
        {
            GameState.ResetUidCounter();
            _gs = new GameState();
            _gs.Round = 0;
            _gs.Turn  = GameRules.OWNER_PLAYER;
            _gs.Phase = GameRules.PHASE_ACTION;

            _legendSys = new UnityEngine.GameObject("LegendSys")
                .AddComponent<LegendSystem>();

            _gs.PLegend = _legendSys.CreateLegend(LegendSystem.KAISA_LEGEND_ID, GameRules.OWNER_PLAYER);
            _gs.ELegend = _legendSys.CreateLegend(LegendSystem.YI_LEGEND_ID,    GameRules.OWNER_ENEMY);
        }

        // ── LegendInstance data ───────────────────────────────────────────────

        [Test]
        public void PLegend_InitializesCorrectly()
        {
            Assert.AreEqual(LegendSystem.KAISA_LEGEND_ID, _gs.PLegend.Id);
            Assert.AreEqual(1, _gs.PLegend.Level);
            Assert.IsFalse(_gs.PLegend.Exhausted);
            Assert.IsFalse(_gs.PLegend.AbilityUsedThisTurn);
            Assert.AreEqual(GameRules.OWNER_PLAYER, _gs.PLegend.Owner);
        }

        [Test]
        public void ELegend_InitializesCorrectly()
        {
            Assert.AreEqual(LegendSystem.YI_LEGEND_ID, _gs.ELegend.Id);
            Assert.AreEqual(1, _gs.ELegend.Level);
            Assert.AreEqual(GameRules.OWNER_ENEMY, _gs.ELegend.Owner);
        }

        [Test]
        public void GetLegend_ReturnsCorrectInstance()
        {
            Assert.AreEqual(_gs.PLegend, _gs.GetLegend(GameRules.OWNER_PLAYER));
            Assert.AreEqual(_gs.ELegend, _gs.GetLegend(GameRules.OWNER_ENEMY));
        }

        // ── Kaisa active: 虚空感知 ────────────────────────────────────────────

        [Test]
        public void KaisaActive_AddsBlazingSch()
        {
            int before = _gs.GetSch(GameRules.OWNER_PLAYER, RuneType.Blazing);
            bool ok = _legendSys.UseKaisaActive(GameRules.OWNER_PLAYER, _gs);
            Assert.IsTrue(ok);
            Assert.AreEqual(before + 1, _gs.GetSch(GameRules.OWNER_PLAYER, RuneType.Blazing));
        }

        [Test]
        public void KaisaActive_ExhaustsLegend()
        {
            _legendSys.UseKaisaActive(GameRules.OWNER_PLAYER, _gs);
            Assert.IsTrue(_gs.PLegend.Exhausted);
            Assert.IsTrue(_gs.PLegend.AbilityUsedThisTurn);
        }

        [Test]
        public void KaisaActive_CannotUseAgainSameTurn()
        {
            _legendSys.UseKaisaActive(GameRules.OWNER_PLAYER, _gs);
            bool secondUse = _legendSys.UseKaisaActive(GameRules.OWNER_PLAYER, _gs);
            Assert.IsFalse(secondUse);
            Assert.AreEqual(1, _gs.GetSch(GameRules.OWNER_PLAYER, RuneType.Blazing));
        }

        [Test]
        public void KaisaActive_ResetForTurnClearsFlags()
        {
            _legendSys.UseKaisaActive(GameRules.OWNER_PLAYER, _gs);
            _legendSys.ResetForTurn(GameRules.OWNER_PLAYER, _gs);
            Assert.IsFalse(_gs.PLegend.AbilityUsedThisTurn);
            Assert.IsFalse(_gs.PLegend.Exhausted);
        }

        [Test]
        public void KaisaActive_CanUseAgainAfterReset()
        {
            _legendSys.UseKaisaActive(GameRules.OWNER_PLAYER, _gs);
            _legendSys.ResetForTurn(GameRules.OWNER_PLAYER, _gs);

            bool ok = _legendSys.UseKaisaActive(GameRules.OWNER_PLAYER, _gs);
            Assert.IsTrue(ok);
            Assert.AreEqual(2, _gs.GetSch(GameRules.OWNER_PLAYER, RuneType.Blazing));
        }

        // ── Kaisa passive: 进化 ───────────────────────────────────────────────

        private UnitInstance MakeUnit(CardKeyword kw)
        {
            var data = UnityEngine.ScriptableObject.CreateInstance<CardData>();
            data.EditorSetup("test_" + kw, "TestUnit", 1, 1, RuneType.Blazing, 0, "", kw, "");
            return new UnitInstance(GameState.NextUid(), data, GameRules.OWNER_PLAYER);
        }

        [Test]
        public void KaisaEvolution_NoTriggerWithFewKeywords()
        {
            _gs.PBase.Add(MakeUnit(CardKeyword.Haste));
            _gs.PBase.Add(MakeUnit(CardKeyword.Barrier));
            _gs.PBase.Add(MakeUnit(CardKeyword.SpellShield));

            _legendSys.CheckKaisaEvolution(GameRules.OWNER_PLAYER, _gs);
            Assert.AreEqual(1, _gs.PLegend.Level);
        }

        [Test]
        public void KaisaEvolution_TriggersAtFourDistinctKeywords()
        {
            _gs.PBase.Add(MakeUnit(CardKeyword.Haste));
            _gs.PBase.Add(MakeUnit(CardKeyword.Barrier));
            _gs.PBase.Add(MakeUnit(CardKeyword.SpellShield));
            _gs.PBase.Add(MakeUnit(CardKeyword.Deathwish));

            _legendSys.CheckKaisaEvolution(GameRules.OWNER_PLAYER, _gs);
            Assert.AreEqual(2, _gs.PLegend.Level);
        }

        [Test]
        public void KaisaEvolution_DoesNotTriggerTwice()
        {
            _gs.PBase.Add(MakeUnit(CardKeyword.Haste));
            _gs.PBase.Add(MakeUnit(CardKeyword.Barrier));
            _gs.PBase.Add(MakeUnit(CardKeyword.SpellShield));
            _gs.PBase.Add(MakeUnit(CardKeyword.Deathwish));

            _legendSys.CheckKaisaEvolution(GameRules.OWNER_PLAYER, _gs);
            Assert.AreEqual(2, _gs.PLegend.Level);

            _legendSys.CheckKaisaEvolution(GameRules.OWNER_PLAYER, _gs);
            Assert.AreEqual(2, _gs.PLegend.Level); // still 2, not 3
        }

        [Test]
        public void KaisaEvolution_SameKeywordDoesNotCountTwice()
        {
            _gs.PBase.Add(MakeUnit(CardKeyword.Haste));
            _gs.PBase.Add(MakeUnit(CardKeyword.Haste)); // duplicate
            _gs.PBase.Add(MakeUnit(CardKeyword.Barrier));
            _gs.PBase.Add(MakeUnit(CardKeyword.SpellShield));

            _legendSys.CheckKaisaEvolution(GameRules.OWNER_PLAYER, _gs);
            Assert.AreEqual(1, _gs.PLegend.Level); // still only 3 distinct keywords
        }

        // ── Masteryi passive: 独影剑鸣 ─────────────────────────────────────────

        private UnitInstance MakeBasicUnit(string owner)
        {
            var data = UnityEngine.ScriptableObject.CreateInstance<CardData>();
            data.EditorSetup("basic", "BasicUnit", 1, 3, RuneType.Verdant, 0, "", CardKeyword.None, "");
            return new UnitInstance(GameState.NextUid(), data, owner);
        }

        [Test]
        public void MasteryiPassive_BuffsLoneDefender()
        {
            var defUnit = MakeBasicUnit(GameRules.OWNER_ENEMY);
            _gs.BF[0].EnemyUnits.Add(defUnit);
            _gs.BF[0].PlayerUnits.Add(MakeBasicUnit(GameRules.OWNER_PLAYER));

            int before = defUnit.TempAtkBonus;
            _legendSys.TryApplyMasteryiPassive(0, GameRules.OWNER_PLAYER, _gs);
            Assert.AreEqual(before + 2, defUnit.TempAtkBonus);
        }

        [Test]
        public void MasteryiPassive_NoBuff_WhenTwoDefenders()
        {
            var def1 = MakeBasicUnit(GameRules.OWNER_ENEMY);
            var def2 = MakeBasicUnit(GameRules.OWNER_ENEMY);
            _gs.BF[0].EnemyUnits.Add(def1);
            _gs.BF[0].EnemyUnits.Add(def2);

            _legendSys.TryApplyMasteryiPassive(0, GameRules.OWNER_PLAYER, _gs);
            Assert.AreEqual(0, def1.TempAtkBonus);
            Assert.AreEqual(0, def2.TempAtkBonus);
        }
    }
}
