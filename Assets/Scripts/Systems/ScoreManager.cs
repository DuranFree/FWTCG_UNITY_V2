using System;
using System.Collections.Generic;
using UnityEngine;
using FWTCG.Core;

namespace FWTCG.Systems
{
    /// <summary>
    /// Handles all scoring logic including the last-point rule.
    ///
    /// Last-point rule (#2): To earn the winning point via conquest,
    /// the player must have conquered ALL battlefields (2) in the current turn.
    /// Conquering only 1 battlefield denies the point and awards 1 card draw instead.
    /// Hold and burnout scores bypass this restriction.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static event Action<string> OnGameOver;
        public static event Action<string> OnScoreChanged;

        /// <summary>
        /// Attempts to add points to a player. Applies the last-point restriction.
        /// Returns true if the point(s) were actually awarded.
        /// </summary>
        public bool AddScore(string who, int pts, string type, int? bfId, GameState gs)
        {
            if (gs.GameOver) return false;

            // #2: Last-point rule for conquest scoring
            if (type == GameRules.SCORE_TYPE_CONQUER)
            {
                int currentScore = gs.GetScore(who);
                if (currentScore + pts >= GameRules.WIN_SCORE)
                {
                    // Check if ALL battlefields have been conquered this turn
                    HashSet<int> conqueredBFs = new HashSet<int>(gs.BFConqueredThisTurn);
                    if (bfId.HasValue) conqueredBFs.Add(bfId.Value);

                    if (conqueredBFs.Count < GameRules.BATTLEFIELD_COUNT)
                    {
                        // Deny winning point, draw 1 card instead
                        TurnManager.BroadcastMessage_Static(
                            $"[最后一分] {DisplayName(who)} 未征服所有战场，得分无效，改为抽1张牌");

                        // Still track that this BF was conquered
                        if (bfId.HasValue && !gs.BFConqueredThisTurn.Contains(bfId.Value))
                            gs.BFConqueredThisTurn.Add(bfId.Value);

                        DrawCardAsReward(who, gs);
                        return false;
                    }
                    // All BFs conquered — allow winning point through
                }
            }

            // Award the score
            gs.AddScore(who, pts);

            // Track battlefield scoring
            if (bfId.HasValue)
            {
                if (type == GameRules.SCORE_TYPE_HOLD && !gs.BFScoredThisTurn.Contains(bfId.Value))
                {
                    gs.BFScoredThisTurn.Add(bfId.Value);
                }
                if (type == GameRules.SCORE_TYPE_CONQUER && !gs.BFConqueredThisTurn.Contains(bfId.Value))
                {
                    gs.BFConqueredThisTurn.Add(bfId.Value);
                }
            }

            string msg = $"[得分] {DisplayName(who)} +{pts} ({type}) → {gs.GetScore(who)}/{GameRules.WIN_SCORE}";
            TurnManager.BroadcastMessage_Static(msg);
            OnScoreChanged?.Invoke(msg);

            CheckWin(gs);
            return true;
        }

        /// <summary>
        /// Checks if WIN_SCORE has been reached and fires OnGameOver if so.
        /// </summary>
        public void CheckWin(GameState gs)
        {
            if (gs.GameOver) return;

            string winner = null;
            if (gs.PScore >= GameRules.WIN_SCORE) winner = GameRules.OWNER_PLAYER;
            else if (gs.EScore >= GameRules.WIN_SCORE) winner = GameRules.OWNER_ENEMY;

            if (winner != null)
            {
                gs.GameOver = true;
                string msg = winner == GameRules.OWNER_PLAYER
                    ? $"玩家获胜！({gs.PScore}/{GameRules.WIN_SCORE})"
                    : $"AI获胜！({gs.EScore}/{GameRules.WIN_SCORE})";

                OnGameOver?.Invoke(msg);
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Draws 1 card for the last-point denial reward.
        /// If deck is empty, performs burnout (shuffle discard → draw, opponent +1).
        /// </summary>
        private void DrawCardAsReward(string who, GameState gs)
        {
            List<UnitInstance> deck = gs.GetDeck(who);
            List<UnitInstance> hand = gs.GetHand(who);
            List<UnitInstance> discard = gs.GetDiscard(who);

            if (deck.Count == 0 && discard.Count > 0)
            {
                // Burnout: shuffle discard into deck
                ShuffleDiscard(deck, discard);
                string opponent = gs.Opponent(who);
                TurnManager.BroadcastMessage_Static($"[燃尽] {DisplayName(who)} 牌库耗尽，洗牌！对手 +1分");
                AddScore(opponent, 1, GameRules.SCORE_TYPE_BURNOUT, null, gs);
            }

            if (deck.Count > 0)
            {
                UnitInstance drawn = deck[0];
                deck.RemoveAt(0);
                hand.Add(drawn);
                TurnManager.BroadcastMessage_Static($"[奖励] {DisplayName(who)} 抽到 {drawn.UnitName}");
            }
        }

        private void ShuffleDiscard(List<UnitInstance> deck, List<UnitInstance> discard)
        {
            for (int i = discard.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                UnitInstance temp = discard[i];
                discard[i] = discard[j];
                discard[j] = temp;
            }
            deck.AddRange(discard);
            discard.Clear();
        }

        private string DisplayName(string owner) =>
            owner == GameRules.OWNER_PLAYER ? "玩家" : "AI";
    }
}
