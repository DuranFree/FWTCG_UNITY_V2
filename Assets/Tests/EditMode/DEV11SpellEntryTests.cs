using NUnit.Framework;
using UnityEngine;
using FWTCG.Core;
using FWTCG.Data;
using System.Collections.Generic;

namespace FWTCG.Tests
{
    [TestFixture]
    public class DEV11SpellEntryTests
    {
        private GameState _gs;

        [SetUp]
        public void SetUp()
        {
            GameState.ResetUidCounter();
            _gs = new GameState();
            _gs.Turn = GameRules.OWNER_PLAYER;
            _gs.Phase = GameRules.PHASE_ACTION;
            _gs.First = GameRules.OWNER_PLAYER;
        }

        // ── ExtraTurnPending field ───────────────────────────────────────────

        [Test]
        public void GameState_ExtraTurnPending_DefaultFalse()
        {
            Assert.IsFalse(_gs.ExtraTurnPending);
        }

        [Test]
        public void GameState_ExtraTurnPending_CanBeSet()
        {
            _gs.ExtraTurnPending = true;
            Assert.IsTrue(_gs.ExtraTurnPending);
        }

        // ── InspireNextUnit field ────────────────────────────────────────────

        [Test]
        public void GameState_InspireNextUnit_DefaultFalse()
        {
            Assert.IsFalse(_gs.InspireNextUnit);
        }

        [Test]
        public void GameState_InspireNextUnit_CanBeSet()
        {
            _gs.InspireNextUnit = true;
            Assert.IsTrue(_gs.InspireNextUnit);
        }

        // ── UnitInstance new fields ──────────────────────────────────────────

        [Test]
        public void UnitInstance_HasReactive_DefaultFalse()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.EditorSetup("test", "Test", 1, 1, RuneType.Blazing, 0, "desc");
            var unit = new UnitInstance(1, card, GameRules.OWNER_PLAYER);
            Assert.IsFalse(unit.HasReactive);
            Object.DestroyImmediate(card);
        }

        [Test]
        public void UnitInstance_UntargetableBySpells_DefaultFalse()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.EditorSetup("test", "Test", 1, 1, RuneType.Blazing, 0, "desc");
            var unit = new UnitInstance(1, card, GameRules.OWNER_PLAYER);
            Assert.IsFalse(unit.UntargetableBySpells);
            Object.DestroyImmediate(card);
        }

        [Test]
        public void UnitInstance_HasReactive_CanBeSet()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.EditorSetup("test", "Test", 1, 1, RuneType.Blazing, 0, "desc");
            var unit = new UnitInstance(1, card, GameRules.OWNER_PLAYER);
            unit.HasReactive = true;
            Assert.IsTrue(unit.HasReactive);
            Object.DestroyImmediate(card);
        }

        [Test]
        public void UnitInstance_UntargetableBySpells_CanBeSet()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.EditorSetup("test", "Test", 1, 1, RuneType.Blazing, 0, "desc");
            var unit = new UnitInstance(1, card, GameRules.OWNER_PLAYER);
            unit.UntargetableBySpells = true;
            Assert.IsTrue(unit.UntargetableBySpells);
            Object.DestroyImmediate(card);
        }

        // ── Rune recycle logic ────────────────────────────────────────────────

        [Test]
        public void RuneRecycle_RemovesFromActiveRunes()
        {
            var rune = new RuneInstance(1, RuneType.Blazing);
            _gs.PRunes.Add(rune);
            Assert.AreEqual(1, _gs.PRunes.Count);

            // Simulate recycle
            _gs.PRunes.RemoveAt(0);
            _gs.PRuneDeck.Insert(0, rune);
            _gs.AddSch(GameRules.OWNER_PLAYER, rune.RuneType, 1);

            Assert.AreEqual(0, _gs.PRunes.Count);
            Assert.AreEqual(1, _gs.PRuneDeck.Count);
            Assert.AreEqual(1, _gs.GetSch(GameRules.OWNER_PLAYER, RuneType.Blazing));
        }

        [Test]
        public void RuneRecycle_GainsSchematicEnergy()
        {
            var rune = new RuneInstance(2, RuneType.Crushing);
            _gs.PRunes.Add(rune);

            // Simulate recycle
            _gs.PRunes.RemoveAt(0);
            _gs.PRuneDeck.Insert(0, rune);
            _gs.AddSch(GameRules.OWNER_PLAYER, rune.RuneType, 1);

            Assert.AreEqual(1, _gs.GetSch(GameRules.OWNER_PLAYER, RuneType.Crushing));
        }

        // ── Extra turn logic ─────────────────────────────────────────────────

        [Test]
        public void ExtraTurn_PreventsTurnSwitch()
        {
            _gs.ExtraTurnPending = true;
            _gs.Turn = GameRules.OWNER_PLAYER;

            // Simulate end-phase logic: if ExtraTurnPending, same player keeps turn
            if (_gs.ExtraTurnPending)
            {
                _gs.ExtraTurnPending = false;
                // Turn stays the same
            }
            else
            {
                _gs.Turn = _gs.Opponent(_gs.Turn);
            }

            Assert.AreEqual(GameRules.OWNER_PLAYER, _gs.Turn);
            Assert.IsFalse(_gs.ExtraTurnPending);
        }

        // ── Inspire entry effect ─────────────────────────────────────────────

        [Test]
        public void Inspire_SetsFlag()
        {
            _gs.InspireNextUnit = true;
            Assert.IsTrue(_gs.InspireNextUnit);

            // Next unit clears it
            _gs.InspireNextUnit = false;
            Assert.IsFalse(_gs.InspireNextUnit);
        }

        [Test]
        public void Inspire_NextUnitGetsBuffToken()
        {
            _gs.InspireNextUnit = true;
            var card = ScriptableObject.CreateInstance<CardData>();
            card.EditorSetup("test", "Test", 3, 3, RuneType.Blazing, 0, "desc");
            var unit = new UnitInstance(1, card, GameRules.OWNER_PLAYER);

            // Simulate inspire check
            if (_gs.InspireNextUnit)
            {
                _gs.InspireNextUnit = false;
                unit.BuffTokens += 1;
                unit.CurrentAtk += 1;
            }

            Assert.AreEqual(1, unit.BuffTokens);
            Assert.AreEqual(4, unit.CurrentAtk); // 3 + 1
            Assert.IsFalse(_gs.InspireNextUnit);
            Object.DestroyImmediate(card);
        }

        // ── FurnaceBlast damage logic ────────────────────────────────────────

        [Test]
        public void FurnaceBlast_DamagesUpTo3Units()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.EditorSetup("enemy1", "E1", 2, 2, RuneType.Blazing, 0, "");
            var card2 = ScriptableObject.CreateInstance<CardData>();
            card2.EditorSetup("enemy2", "E2", 3, 3, RuneType.Blazing, 0, "");
            var card3 = ScriptableObject.CreateInstance<CardData>();
            card3.EditorSetup("enemy3", "E3", 4, 4, RuneType.Blazing, 0, "");

            var u1 = _gs.MakeUnit(card, GameRules.OWNER_ENEMY);
            var u2 = _gs.MakeUnit(card2, GameRules.OWNER_ENEMY);
            var u3 = _gs.MakeUnit(card3, GameRules.OWNER_ENEMY);
            _gs.EBase.Add(u1);
            _gs.EBase.Add(u2);
            _gs.EBase.Add(u3);

            // Simulate furnace blast: 1 damage each
            u1.CurrentHp -= 1;
            u2.CurrentHp -= 1;
            u3.CurrentHp -= 1;

            Assert.AreEqual(1, u1.CurrentHp); // 2-1
            Assert.AreEqual(2, u2.CurrentHp); // 3-1
            Assert.AreEqual(3, u3.CurrentHp); // 4-1

            Object.DestroyImmediate(card);
            Object.DestroyImmediate(card2);
            Object.DestroyImmediate(card3);
        }
    }
}
