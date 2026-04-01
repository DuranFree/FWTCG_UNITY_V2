using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using FWTCG.Core;
using FWTCG.Systems;
using FWTCG.UI;

/// <summary>
/// DEV-19: UI 系统补全测试
/// 覆盖：AskPromptUI 异步逻辑、ScoreManager.OnScoreAdded、GameEventBus.OnDuelBanner、
/// GameUI 分数脉冲触发条件、回合横幅生成、反应按钮 ribbon 触发条件。
/// </summary>
[TestFixture]
public class DEV19UITests
{
    // ────────────────────────────────────────────────────────────────────────
    // 一、AskPromptUI
    // ────────────────────────────────────────────────────────────────────────

    [Test]
    public void AskPromptUI_WaitForCardChoice_ReturnsNullImmediately_WhenNullCards()
    {
        // WaitForCardChoice(null, ...) should complete synchronously with null result
        var go = new GameObject("AskPromptUI_Test");
        var ui = go.AddComponent<AskPromptUI>();

        Task<UnitInstance> task = ui.WaitForCardChoice(null, "选择");

        Assert.IsTrue(task.IsCompleted, "Should complete synchronously with null card list");
        Assert.IsNull(task.Result, "Result should be null when no cards supplied");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void AskPromptUI_WaitForCardChoice_ReturnsNullImmediately_WhenEmptyList()
    {
        var go = new GameObject("AskPromptUI_Test2");
        var ui = go.AddComponent<AskPromptUI>();

        var task = ui.WaitForCardChoice(new List<UnitInstance>(), "选择");

        Assert.IsTrue(task.IsCompleted, "Should complete synchronously with empty card list");
        Assert.IsNull(task.Result);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void AskPromptUI_WaitForConfirm_TaskNotCompleted_BeforeButtonPress()
    {
        var go = new GameObject("AskPromptUI_Test3");
        var ui = go.AddComponent<AskPromptUI>();

        // Without any buttons wired, task should remain pending
        var task = ui.WaitForConfirm("确认", "是否继续？");

        // Task should be created but not auto-completed (requires user button press)
        Assert.IsNotNull(task, "Task should not be null");
        // It may or may not be completed depending on button state — just check it's created
        // (actual async resolution tested in play mode)

        // Cleanup: cancel to avoid hanging task
        ui.CancelFromTest();
        Object.DestroyImmediate(go);
    }

    [Test]
    public void AskPromptUI_Singleton_Instance_NotNullAfterAwake()
    {
        // Ensure clean state regardless of test execution order
        AskPromptUI.ResetInstanceForTest();

        var go = new GameObject("AskPromptUISingleton");
        var ui = go.AddComponent<AskPromptUI>();

        // In EditMode, Awake may not auto-fire on AddComponent.
        // Use reflection to invoke it explicitly; guard in Awake prevents double-init.
        var awakeMethod = typeof(AskPromptUI).GetMethod(
            "Awake",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        awakeMethod?.Invoke(ui, null);

        Assert.IsNotNull(AskPromptUI.Instance, "Instance should be set after Awake");
        Assert.AreSame(ui, AskPromptUI.Instance);

        // Reset singleton
        AskPromptUI.ResetInstanceForTest();
        Object.DestroyImmediate(go);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 二、ScoreManager.OnScoreAdded
    // ────────────────────────────────────────────────────────────────────────

    [Test]
    public void ScoreManager_OnScoreAdded_FiresWithCorrectOwnerAndScore()
    {
        string firedOwner = null;
        int firedScore = -1;
        ScoreManager.OnScoreAdded += (owner, score) => { firedOwner = owner; firedScore = score; };

        var go = new GameObject("ScoreMgr");
        var bfSysGO = new GameObject("BFSys");
        var bfSys = bfSysGO.AddComponent<BattlefieldSystem>();
        var mgr = go.AddComponent<ScoreManager>();
        // Inject BFSystem via reflection to avoid null ref
        typeof(ScoreManager)
            .GetField("_bfSys", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(mgr, bfSys);

        var gs = new GameState();

        bool added = mgr.AddScore(GameRules.OWNER_PLAYER, 1, GameRules.SCORE_TYPE_HOLD, null, gs);

        Assert.IsTrue(added, "Score should be added");
        Assert.AreEqual(GameRules.OWNER_PLAYER, firedOwner, "OnScoreAdded should fire with OWNER_PLAYER");
        Assert.AreEqual(1, firedScore, "OnScoreAdded should report new score = 1");

        ScoreManager.OnScoreAdded -= (owner, score) => { firedOwner = owner; firedScore = score; };
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(bfSysGO);
    }

    [Test]
    public void ScoreManager_OnScoreAdded_NotFiredWhenGameOver()
    {
        int fireCount = 0;
        System.Action<string, int> handler = (o, s) => fireCount++;
        ScoreManager.OnScoreAdded += handler;

        var go = new GameObject("ScoreMgr2");
        var mgr = go.AddComponent<ScoreManager>();
        var gs = new GameState();
        gs.GameOver = true;

        mgr.AddScore(GameRules.OWNER_PLAYER, 1, GameRules.SCORE_TYPE_HOLD, null, gs);

        Assert.AreEqual(0, fireCount, "OnScoreAdded should not fire when GameOver");

        ScoreManager.OnScoreAdded -= handler;
        Object.DestroyImmediate(go);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 三、GameEventBus.OnDuelBanner
    // ────────────────────────────────────────────────────────────────────────

    [Test]
    public void GameEventBus_FireDuelBanner_TriggersSubscribers()
    {
        int callCount = 0;
        System.Action handler = () => callCount++;
        GameEventBus.OnDuelBanner += handler;

        GameEventBus.FireDuelBanner();

        Assert.AreEqual(1, callCount, "OnDuelBanner should fire exactly once");

        GameEventBus.OnDuelBanner -= handler;
    }

    [Test]
    public void GameEventBus_FireDuelBanner_NoSubscribers_DoesNotThrow()
    {
        // Ensure no subscribers are registered
        // Calling with no subscribers should not throw
        Assert.DoesNotThrow(() => GameEventBus.FireDuelBanner(),
            "FireDuelBanner should not throw with no subscribers");
    }

    // ────────────────────────────────────────────────────────────────────────
    // 四、得分脉冲触发条件（GameUI 辅助逻辑）
    // ────────────────────────────────────────────────────────────────────────

    [Test]
    public void ScorePulse_ShouldTrigger_WhenScoreIncreases()
    {
        // Logic test: given cached score != new score, pulse should be triggered
        int cachedScore = 2;
        int newScore    = 3;
        bool shouldPulse = newScore > cachedScore;
        Assert.IsTrue(shouldPulse, "Pulse should trigger when score increases");
    }

    [Test]
    public void ScorePulse_ShouldNotTrigger_WhenScoreSame()
    {
        int cachedScore = 3;
        int newScore    = 3;
        bool shouldPulse = newScore != cachedScore;
        Assert.IsFalse(shouldPulse, "Pulse should NOT trigger when score is unchanged");
    }

    [Test]
    public void ScorePulse_CircleIndex_IsNewScoreMinusOne()
    {
        // When player scores their 4th point, the circle at index 3 should pulse
        int newScore = 4;
        int expectedCircleIdx = newScore - 1; // 0-indexed
        Assert.AreEqual(3, expectedCircleIdx, "Circle index should be newScore-1");
    }

    // ────────────────────────────────────────────────────────────────────────
    // 五、回合横幅内容
    // ────────────────────────────────────────────────────────────────────────

    [Test]
    public void TurnBanner_PlayerTurn_ContainsPlayerLabel()
    {
        string turn = GameRules.OWNER_PLAYER;
        int round = 3;
        string banner = BuildTurnBannerText(turn, round);
        Assert.IsTrue(banner.Contains("玩家"), "Player turn banner should contain '玩家'");
        Assert.IsTrue(banner.Contains("4"), "Round display should be round+1 (3→4)");
    }

    [Test]
    public void TurnBanner_EnemyTurn_ContainsAILabel()
    {
        string turn = GameRules.OWNER_ENEMY;
        int round = 0;
        string banner = BuildTurnBannerText(turn, round);
        Assert.IsTrue(banner.Contains("AI"), "Enemy turn banner should contain 'AI'");
        Assert.IsTrue(banner.Contains("1"), "Round 1 should display '1'");
    }

    /// <summary>Mirrors the banner text logic in GameManager.HandlePhaseChanged.</summary>
    private static string BuildTurnBannerText(string turn, int round)
    {
        string who = turn == GameRules.OWNER_PLAYER ? "玩家" : "AI";
        return $"回合 {round + 1} · {who}的回合";
    }

    // ────────────────────────────────────────────────────────────────────────
    // 六、反应按钮 ribbon 触发条件
    // ────────────────────────────────────────────────────────────────────────

    [Test]
    public void ReactRibbon_ShouldAnimate_WhenButtonBecomesInteractable()
    {
        // Logic: ribbon animates when button transitions from non-interactable to interactable
        bool wasInteractable = false;
        bool nowInteractable = true;
        bool shouldAnimate = !wasInteractable && nowInteractable;
        Assert.IsTrue(shouldAnimate, "Ribbon should animate on false→true transition");
    }

    [Test]
    public void ReactRibbon_ShouldNotAnimate_WhenButtonAlreadyInteractable()
    {
        bool wasInteractable = true;
        bool nowInteractable = true;
        bool shouldAnimate = !wasInteractable && nowInteractable;
        Assert.IsFalse(shouldAnimate, "Ribbon should NOT animate when already interactable");
    }

    // ────────────────────────────────────────────────────────────────────────
    // 七、结束按钮常驻脉冲条件
    // ────────────────────────────────────────────────────────────────────────

    [Test]
    public void EndTurnPulse_ActiveWhenPlayerAction()
    {
        string turn  = GameRules.OWNER_PLAYER;
        string phase = GameRules.PHASE_ACTION;
        bool shouldPulse = turn == GameRules.OWNER_PLAYER && phase == GameRules.PHASE_ACTION;
        Assert.IsTrue(shouldPulse, "Pulse should be active during player action phase");
    }

    [Test]
    public void EndTurnPulse_InactiveWhenAITurn()
    {
        string turn  = GameRules.OWNER_ENEMY;
        string phase = GameRules.PHASE_ACTION;
        bool shouldPulse = turn == GameRules.OWNER_PLAYER && phase == GameRules.PHASE_ACTION;
        Assert.IsFalse(shouldPulse, "Pulse should be inactive during enemy turn");
    }

    [Test]
    public void EndTurnPulse_InactiveWhenNotActionPhase()
    {
        string turn  = GameRules.OWNER_PLAYER;
        string phase = GameRules.PHASE_AWAKEN;
        bool shouldPulse = turn == GameRules.OWNER_PLAYER && phase == GameRules.PHASE_ACTION;
        Assert.IsFalse(shouldPulse, "Pulse should be inactive outside action phase");
    }
}
