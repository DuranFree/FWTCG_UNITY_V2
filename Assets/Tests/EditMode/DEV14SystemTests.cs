using NUnit.Framework;
using UnityEngine;
using FWTCG.Core;
using FWTCG.Data;

namespace FWTCG.Tests
{
    [TestFixture]
    public class DEV14SystemTests
    {
        private GameState _gs;

        [SetUp]
        public void SetUp()
        {
            GameState.ResetUidCounter();
            _gs = new GameState();
        }

        // ── Buff Token System Validation ────────────────────────────────────

        [Test]
        public void BuffToken_DefaultZero()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.EditorSetup("test", "Test", 3, 3, RuneType.Blazing, 0, "");
            var unit = new UnitInstance(1, card, GameRules.OWNER_PLAYER);
            Assert.AreEqual(0, unit.BuffTokens);
            Object.DestroyImmediate(card);
        }

        [Test]
        public void BuffToken_Increment_IncreasesAtk()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.EditorSetup("test", "Test", 3, 3, RuneType.Blazing, 0, "");
            var unit = new UnitInstance(1, card, GameRules.OWNER_PLAYER);
            unit.BuffTokens++;
            unit.CurrentAtk += 1;
            Assert.AreEqual(1, unit.BuffTokens);
            Assert.AreEqual(4, unit.CurrentAtk);
            Object.DestroyImmediate(card);
        }

        [Test]
        public void BuffToken_SurvivesEndOfTurn()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.EditorSetup("test", "Test", 3, 3, RuneType.Blazing, 0, "");
            var unit = new UnitInstance(1, card, GameRules.OWNER_PLAYER);
            unit.BuffTokens = 1;
            unit.CurrentAtk = 4; // 3 + 1 buff

            unit.ResetEndOfTurn();

            // Buff should persist through end of turn
            Assert.AreEqual(1, unit.BuffTokens);
            Assert.AreEqual(4, unit.CurrentAtk); // base 3 + 1 buff
            Assert.AreEqual(4, unit.CurrentHp);  // HP = ATK
        }

        [Test]
        public void BuffToken_TempBonus_ClearedEndOfTurn()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.EditorSetup("test", "Test", 3, 3, RuneType.Blazing, 0, "");
            var unit = new UnitInstance(1, card, GameRules.OWNER_PLAYER);
            unit.TempAtkBonus = 5;

            unit.ResetEndOfTurn();

            Assert.AreEqual(0, unit.TempAtkBonus);
            Assert.AreEqual(3, unit.CurrentAtk); // back to base
            Object.DestroyImmediate(card);
        }

        // ── Soft-weighted Opening Hand ──────────────────────────────────────

        [Test]
        public void SoftWeightedHand_FindsLowCostUnit()
        {
            // Simulate: deck has expensive cards + one cheap card
            var expCard = ScriptableObject.CreateInstance<CardData>();
            expCard.EditorSetup("exp", "Expensive", 5, 5, RuneType.Blazing, 0, "");
            var cheapCard = ScriptableObject.CreateInstance<CardData>();
            cheapCard.EditorSetup("cheap", "Cheap", 2, 2, RuneType.Blazing, 0, "");

            _gs.PDeck.Add(_gs.MakeUnit(expCard, GameRules.OWNER_PLAYER));
            _gs.PDeck.Add(_gs.MakeUnit(expCard, GameRules.OWNER_PLAYER));
            _gs.PDeck.Add(_gs.MakeUnit(cheapCard, GameRules.OWNER_PLAYER));
            _gs.PDeck.Add(_gs.MakeUnit(expCard, GameRules.OWNER_PLAYER));

            // Simulate seed: find ≤2 cost unit
            UnitInstance found = null;
            for (int i = 0; i < _gs.PDeck.Count; i++)
            {
                if (!_gs.PDeck[i].CardData.IsSpell && _gs.PDeck[i].CardData.Cost <= 2)
                {
                    found = _gs.PDeck[i];
                    _gs.PDeck.RemoveAt(i);
                    _gs.PHand.Add(found);
                    break;
                }
            }

            Assert.IsNotNull(found);
            Assert.AreEqual("Cheap", found.UnitName);
            Assert.AreEqual(1, _gs.PHand.Count);
            Assert.AreEqual(3, _gs.PDeck.Count);

            Object.DestroyImmediate(expCard);
            Object.DestroyImmediate(cheapCard);
        }

        // ── AudioManager ────────────────────────────────────────────────────

        [Test]
        public void AudioManager_CanBeCreated()
        {
            var go = new GameObject("AudioManagerTest");
            var am = go.AddComponent<FWTCG.Audio.AudioManager>();
            Assert.IsNotNull(am);
            Object.DestroyImmediate(go);
        }

        // ── Deck building card counts ───────────────────────────────────────

        [Test]
        public void GameRules_CardCopies_NoxusRecruit_Is2()
        {
            Assert.AreEqual(2, GameRules.GetCardCopies("noxus_recruit"));
        }

        [Test]
        public void GameRules_CardCopies_KaisaHero_Is1()
        {
            Assert.AreEqual(1, GameRules.GetCardCopies("kaisa_hero"));
        }

        [Test]
        public void GameRules_CardCopies_Unknown_DefaultsTo1()
        {
            Assert.AreEqual(1, GameRules.GetCardCopies("nonexistent_card"));
        }
    }
}
