using NUnit.Framework;
using UnityEngine;
using FWTCG.Core;
using FWTCG.Data;

namespace FWTCG.Tests
{
    /// <summary>
    /// DEV-25b rule fix tests:
    ///   - Rule 139.2: EffectiveAtk floor is 0, not 1
    ///   - Rule 722: only units with Roam keyword can move between battlefields
    /// </summary>
    [TestFixture]
    public class DEV25bRuleFixTests
    {
        private CardData MakeCard(string id, int atk = 2, CardKeyword kw = CardKeyword.None)
        {
            var cd = ScriptableObject.CreateInstance<CardData>();
            cd.EditorSetup(id, id, 1, atk, RuneType.Blazing, 0, "", keywords: kw);
            return cd;
        }

        private UnitInstance MakeUnit(string id, int atk = 2, CardKeyword kw = CardKeyword.None,
            string owner = GameRules.OWNER_PLAYER)
        {
            return new UnitInstance(1, MakeCard(id, atk, kw), owner);
        }

        // ── Rule 139.2: EffectiveAtk floor = 0 ─────────────────────────────

        [Test]
        public void EffectiveAtk_NormalUnit_ReturnsBaseAtk()
        {
            var u = MakeUnit("normal", atk: 3);
            Assert.AreEqual(3, u.EffectiveAtk());
        }

        [Test]
        public void EffectiveAtk_TempBonusMakesNegative_ReturnsZero()
        {
            var u = MakeUnit("weak", atk: 1);
            u.TempAtkBonus = -5;
            Assert.AreEqual(0, u.EffectiveAtk(), "Rule 139.2: power < 0 must be treated as 0");
        }

        [Test]
        public void EffectiveAtk_TempBonusExactlyNegatesAtk_ReturnsZero()
        {
            var u = MakeUnit("zero", atk: 2);
            u.TempAtkBonus = -2;
            Assert.AreEqual(0, u.EffectiveAtk());
        }

        [Test]
        public void EffectiveAtk_TempBonusPartialReduction_ReturnsPositive()
        {
            var u = MakeUnit("reduced", atk: 3);
            u.TempAtkBonus = -1;
            Assert.AreEqual(2, u.EffectiveAtk());
        }

        [Test]
        public void EffectiveAtk_StunnedUnit_ReturnsZeroRegardlessOfAtk()
        {
            var u = MakeUnit("stunned", atk: 5);
            u.Stunned = true;
            Assert.AreEqual(0, u.EffectiveAtk());
        }

        [Test]
        public void EffectiveAtk_StunnedWithNegativeTemp_StillReturnsZero()
        {
            var u = MakeUnit("stunnedneg", atk: 3);
            u.Stunned = true;
            u.TempAtkBonus = -10;
            Assert.AreEqual(0, u.EffectiveAtk());
        }

        // ── Rule 722: Roam keyword required for BF-to-BF move ──────────────

        [Test]
        public void CardKeyword_RoamDefined()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(CardKeyword), "Roam"),
                "CardKeyword.Roam must be defined");
        }

        [Test]
        public void HasKeyword_UnitWithRoam_ReturnsTrue()
        {
            var u = MakeUnit("roamer", kw: CardKeyword.Roam);
            Assert.IsTrue(u.CardData.HasKeyword(CardKeyword.Roam));
        }

        [Test]
        public void HasKeyword_UnitWithoutRoam_ReturnsFalse()
        {
            var u = MakeUnit("stationary");
            Assert.IsFalse(u.CardData.HasKeyword(CardKeyword.Roam));
        }

        [Test]
        public void HasKeyword_RoamNotGrantedByOtherKeywords()
        {
            var u = MakeUnit("haste_only", kw: CardKeyword.Haste);
            Assert.IsFalse(u.CardData.HasKeyword(CardKeyword.Roam),
                "Haste must not imply Roam");
        }

        [Test]
        public void HasKeyword_UnitWithRoamAndOtherKeywords_HasBoth()
        {
            var u = MakeUnit("roamhaste", kw: CardKeyword.Roam | CardKeyword.Haste);
            Assert.IsTrue(u.CardData.HasKeyword(CardKeyword.Roam));
            Assert.IsTrue(u.CardData.HasKeyword(CardKeyword.Haste));
        }
    }
}
