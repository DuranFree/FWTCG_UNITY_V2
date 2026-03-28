using System;
using UnityEngine;
using FWTCG.Core;

namespace FWTCG.Systems
{
    /// <summary>
    /// Handles all scoring logic including the last-point restriction rule.
    ///
    /// Last-point rule: A player can only earn their 8th (WIN_SCORE) point via
    /// a "conquer" score type AND they must have conquered ALL battlefields
    /// during the current turn. Otherwise the point is denied and they draw 1
    /// card instead.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static event Action<string> OnGameOver;
        public static event Action<string> OnScoreChanged;

        /// <summary>
        /// Attempts to add points to a player. Applies the last-point restriction.
        /// Returns true if the point(s) were actually awarded.
        /// </summary>
        /// <param name="who">Owner string ("player" or "enemy")</param>
        /// <param name="pts">Points to add</param>
        /// <param name="type">Score type: "hold", "conquer", or "burnout"</param>
        /// <param name="bfId">Battlefield ID (null for non-battlefield scores)</param>
        /// <param name="gs">Current game state</param>
        public bool AddScore(string who, int pts, string type, int? bfId, GameState gs)
        {
            if (gs.GameOver) return false;

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

        // ── Private helpers ────────────────────────────────────────────────────

        private string DisplayName(string owner) =>
            owner == GameRules.OWNER_PLAYER ? "玩家" : "AI";
    }
}
