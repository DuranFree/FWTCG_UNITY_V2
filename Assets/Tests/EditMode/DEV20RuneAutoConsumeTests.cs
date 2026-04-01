using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using FWTCG.Core;
using FWTCG.Data;

/// <summary>
/// DEV-20: RuneAutoConsume.Compute logic tests.
/// Covers: free (no deficit), mana-only deficit, sch-only deficit, both deficits,
/// mixed-type runes, unaffordable case, null inputs, empty rune list, edge values.
/// </summary>
[TestFixture]
public class DEV20RuneAutoConsumeTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CardData MakeCard(int cost, int runeCost, RuneType runeType)
    {
        var cd = ScriptableObject.CreateInstance<CardData>();
#if UNITY_EDITOR
        cd.EditorSetup("test", "TestCard", cost, 2, runeType, runeCost, "");
#endif
        return cd;
    }

    private static UnitInstance MakeUnit(int cost, int runeCost, RuneType runeType)
    {
        var cd = MakeCard(cost, runeCost, runeType);
        return new UnitInstance(1, cd, GameRules.OWNER_PLAYER);
    }

    private static RuneInstance MakeRune(RuneType rt, bool tapped = false)
    {
        var r = new RuneInstance(GameState.NextUid(), rt);
        r.Tapped = tapped;
        return r;
    }

    // ── 1. Already affordable — no ops needed ─────────────────────────────────

    [Test]
    public void Compute_CanAfford_WhenManaAndSchSufficient_NoOps()
    {
        var gs = new GameState { PMana = 3 };
        gs.AddSch(GameRules.OWNER_PLAYER, RuneType.Blazing, 1);
        var unit = MakeUnit(2, 1, RuneType.Blazing);

        var plan = RuneAutoConsume.Compute(unit, gs, GameRules.OWNER_PLAYER);

        Assert.IsTrue(plan.CanAfford, "Should afford with sufficient mana+sch");
        Assert.IsFalse(plan.NeedsOps, "No rune ops needed when already affordable");
        Assert.AreEqual(0, plan.TapCount);
        Assert.AreEqual(0, plan.RecycleCount);
    }

    // ── 2. Mana deficit covered by tapping runes ──────────────────────────────

    [Test]
    public void Compute_TapsRunes_WhenManaDeficit()
    {
        var gs = new GameState { PMana = 1 };
        gs.PRunes.Add(MakeRune(RuneType.Blazing));
        gs.PRunes.Add(MakeRune(RuneType.Radiant));
        var unit = MakeUnit(3, 0, RuneType.Blazing); // needs 2 more mana

        var plan = RuneAutoConsume.Compute(unit, gs, GameRules.OWNER_PLAYER);

        Assert.IsTrue(plan.CanAfford);
        Assert.AreEqual(2, plan.TapCount, "Should tap 2 runes for 2 missing mana");
        Assert.AreEqual(0, plan.RecycleCount);
    }

    // ── 3. Sch deficit covered by recycling matching runes ────────────────────

    [Test]
    public void Compute_RecyclesRunes_WhenSchDeficit()
    {
        var gs = new GameState { PMana = 5 };
        gs.PRunes.Add(MakeRune(RuneType.Blazing));
        gs.PRunes.Add(MakeRune(RuneType.Blazing));
        var unit = MakeUnit(2, 2, RuneType.Blazing); // needs 2 sch

        var plan = RuneAutoConsume.Compute(unit, gs, GameRules.OWNER_PLAYER);

        Assert.IsTrue(plan.CanAfford);
        Assert.AreEqual(2, plan.RecycleCount, "Should recycle 2 Blazing runes for sch");
        Assert.AreEqual(0, plan.TapCount);
    }

    // ── 4. Both deficits — recycle first, then tap ────────────────────────────

    [Test]
    public void Compute_BothDeficits_RecycleThenTap()
    {
        var gs = new GameState { PMana = 0 };
        // 1 Blazing (for sch recycle) + 1 Radiant (for mana tap)
        gs.PRunes.Add(MakeRune(RuneType.Blazing));
        gs.PRunes.Add(MakeRune(RuneType.Radiant));
        var unit = MakeUnit(1, 1, RuneType.Blazing);

        var plan = RuneAutoConsume.Compute(unit, gs, GameRules.OWNER_PLAYER);

        Assert.IsTrue(plan.CanAfford);
        Assert.AreEqual(1, plan.RecycleCount, "1 Blazing recycled for sch");
        Assert.AreEqual(1, plan.TapCount, "1 Radiant tapped for mana");
        // Ensure they don't overlap
        Assert.AreEqual(0, plan.RecycleIndices[0], "Blazing at index 0 recycled");
        Assert.AreEqual(1, plan.TapIndices[0], "Radiant at index 1 tapped");
    }

    // ── 5. Unaffordable — insufficient runes ─────────────────────────────────

    [Test]
    public void Compute_CannotAfford_WhenRunesInsufficient()
    {
        var gs = new GameState { PMana = 0 };
        gs.PRunes.Add(MakeRune(RuneType.Blazing)); // only 1 rune, need 3 mana
        var unit = MakeUnit(3, 0, RuneType.Blazing);

        var plan = RuneAutoConsume.Compute(unit, gs, GameRules.OWNER_PLAYER);

        Assert.IsFalse(plan.CanAfford, "Should not afford with insufficient runes");
    }

    // ── 6. Tapped runes are skipped ───────────────────────────────────────────

    [Test]
    public void Compute_SkipsTappedRunes()
    {
        var gs = new GameState { PMana = 0 };
        gs.PRunes.Add(MakeRune(RuneType.Blazing, tapped: true)); // already tapped
        gs.PRunes.Add(MakeRune(RuneType.Radiant, tapped: false));
        var unit = MakeUnit(1, 0, RuneType.Blazing);

        var plan = RuneAutoConsume.Compute(unit, gs, GameRules.OWNER_PLAYER);

        Assert.IsTrue(plan.CanAfford);
        Assert.AreEqual(1, plan.TapCount);
        Assert.AreEqual(1, plan.TapIndices[0], "Should tap Radiant at index 1, not tapped Blazing at 0");
    }

    // ── 7. Wrong rune type not used for sch ───────────────────────────────────

    [Test]
    public void Compute_WrongRuneType_NotUsedForSch()
    {
        var gs = new GameState { PMana = 5 };
        gs.PRunes.Add(MakeRune(RuneType.Radiant)); // wrong type for Blazing cost
        var unit = MakeUnit(0, 1, RuneType.Blazing); // needs 1 Blazing sch

        var plan = RuneAutoConsume.Compute(unit, gs, GameRules.OWNER_PLAYER);

        Assert.IsFalse(plan.CanAfford, "Radiant rune cannot cover Blazing sch deficit");
        Assert.AreEqual(0, plan.RecycleCount, "Wrong-type rune should not be recycled for sch");
    }

    // ── 8. Zero-cost card — always affordable ─────────────────────────────────

    [Test]
    public void Compute_ZeroCostCard_AlwaysAffordable()
    {
        var gs = new GameState { PMana = 0 };
        var unit = MakeUnit(0, 0, RuneType.Blazing);

        var plan = RuneAutoConsume.Compute(unit, gs, GameRules.OWNER_PLAYER);

        Assert.IsTrue(plan.CanAfford);
        Assert.IsFalse(plan.NeedsOps);
    }

    // ── 9. Null card returns CanAfford=false ──────────────────────────────────

    [Test]
    public void Compute_NullCard_ReturnsFalse()
    {
        var gs = new GameState();
        var plan = RuneAutoConsume.Compute(null, gs, GameRules.OWNER_PLAYER);
        Assert.IsFalse(plan.CanAfford);
    }

    // ── 10. Null GameState returns CanAfford=false ────────────────────────────

    [Test]
    public void Compute_NullGameState_ReturnsFalse()
    {
        var unit = MakeUnit(1, 0, RuneType.Blazing);
        var plan = RuneAutoConsume.Compute(unit, null, GameRules.OWNER_PLAYER);
        Assert.IsFalse(plan.CanAfford);
    }

    // ── 11. Empty rune list — only existing mana/sch checked ─────────────────

    [Test]
    public void Compute_EmptyRunes_AffordableIfCurrentResourcesSuffice()
    {
        var gs = new GameState { PMana = 3 };
        var unit = MakeUnit(2, 0, RuneType.Blazing);

        var plan = RuneAutoConsume.Compute(unit, gs, GameRules.OWNER_PLAYER);

        Assert.IsTrue(plan.CanAfford);
        Assert.AreEqual(0, plan.TapCount);
    }

    // ── 12. Exactly enough runes to cover deficit ─────────────────────────────

    [Test]
    public void Compute_ExactlyEnoughRunes_CanAfford()
    {
        var gs = new GameState { PMana = 0 };
        gs.PRunes.Add(MakeRune(RuneType.Verdant));
        var unit = MakeUnit(1, 0, RuneType.Blazing);

        var plan = RuneAutoConsume.Compute(unit, gs, GameRules.OWNER_PLAYER);

        Assert.IsTrue(plan.CanAfford);
        Assert.AreEqual(1, plan.TapCount);
    }

    // ── 13. BuildConfirmText — non-empty for NeedsOps plan ───────────────────

    [Test]
    public void BuildConfirmText_ContainsManaAndSchInfo()
    {
        var gs = new GameState { PMana = 0 };
        gs.PRunes.Add(MakeRune(RuneType.Blazing));
        gs.PRunes.Add(MakeRune(RuneType.Blazing));
        var unit = MakeUnit(1, 1, RuneType.Blazing);

        var plan = RuneAutoConsume.Compute(unit, gs, GameRules.OWNER_PLAYER);
        string text = plan.BuildConfirmText(unit);

        Assert.IsNotEmpty(text, "Confirm text should not be empty");
        StringAssert.Contains("横置", text, "Should mention tap operation");
        StringAssert.Contains("回收", text, "Should mention recycle operation");
    }
}
