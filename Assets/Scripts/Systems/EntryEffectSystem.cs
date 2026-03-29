using System.Collections.Generic;
using UnityEngine;
using FWTCG.Core;
using FWTCG.Data;

namespace FWTCG.Systems
{
    /// <summary>
    /// Handles unit entry (onSummon) effects when a unit moves from hand to base.
    /// Effects are identified by CardData.EffectId.
    /// </summary>
    public class EntryEffectSystem : MonoBehaviour
    {
        public static event System.Action<string> OnEffectLog;

        /// <summary>
        /// Trigger the entry effect of a unit that just entered the base.
        /// Call this after paying costs and placing the unit in base.
        /// </summary>
        public void OnUnitEntered(UnitInstance unit, string owner, GameState gs)
        {
            string effectId = unit.CardData.EffectId;
            if (string.IsNullOrEmpty(effectId)) return;

            switch (effectId)
            {
                case "yordel_instructor_enter":
                    DrawCards(owner, 1, gs);
                    break;

                case "darius_second_card":
                    // If player has played more than 1 card this turn, +2 atk and un-exhaust
                    if (gs.CardsPlayedThisTurn > 1)
                    {
                        unit.TempAtkBonus += 2;
                        unit.Exhausted = false;
                        Log($"[入场] {unit.UnitName} — 本回合已出牌，获得+2战力并变为活跃");
                    }
                    break;

                case "thousand_tail_enter":
                    // All enemy units -3 power (min 1)
                    string enemy = gs.Opponent(owner);
                    int debuffed = 0;
                    foreach (UnitInstance u in AllUnitsFor(enemy, gs))
                    {
                        int newAtk = Mathf.Max(1, u.CurrentAtk - 3);
                        u.CurrentAtk = newAtk;
                        debuffed++;
                    }
                    Log($"[入场] {unit.UnitName} — 所有敌方单位-3战力（共{debuffed}个）");
                    break;

                case "foresight_mech_enter":
                    // View top deck card (AI: just log; player: TODO prompt in DEV-4)
                    List<UnitInstance> deck = gs.GetDeck(owner);
                    if (deck.Count > 0)
                        Log($"[预知] {unit.UnitName} — 牌库顶：{deck[0].UnitName}（ATK:{deck[0].CardData.Atk} 费用:{deck[0].CardData.Cost}）");
                    else
                        Log($"[预知] {unit.UnitName} — 牌库为空");
                    break;

                case "jax_enter":
                    // Hand equipment cards gain Reactive keyword (tracked via flag in DEV-3+)
                    Log($"[入场] {unit.UnitName} — 手牌装备获得反应关键词（DEV-3实现）");
                    break;

                case "tiyana_enter":
                    // Passive: opponent can't gain hold score while Tiyana is in play
                    // Handled in ScoreManager — just log the entry
                    Log($"[入场] {unit.UnitName} — 被动启动：对手无法获得据守分");
                    gs.TiyanasInPlay[owner] = true;
                    break;
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void DrawCards(string owner, int count, GameState gs)
        {
            List<UnitInstance> deck = gs.GetDeck(owner);
            List<UnitInstance> hand = gs.GetHand(owner);
            List<UnitInstance> discard = gs.GetDiscard(owner);
            int drawn = 0;

            for (int i = 0; i < count; i++)
            {
                if (deck.Count == 0)
                {
                    if (discard.Count == 0) break;
                    // Simple shuffle back (no burnout for entry draws)
                    deck.AddRange(discard);
                    discard.Clear();
                }
                if (deck.Count == 0) break;
                hand.Add(deck[0]);
                deck.RemoveAt(0);
                drawn++;
            }

            Log($"[效果] 摸{drawn}张牌（手牌 {gs.GetHand(owner).Count}）");
        }

        private List<UnitInstance> AllUnitsFor(string owner, GameState gs)
        {
            var result = new List<UnitInstance>(gs.GetBase(owner));
            for (int i = 0; i < GameRules.BATTLEFIELD_COUNT; i++)
            {
                List<UnitInstance> bfUnits = owner == GameRules.OWNER_PLAYER
                    ? gs.BF[i].PlayerUnits
                    : gs.BF[i].EnemyUnits;
                result.AddRange(bfUnits);
            }
            return result;
        }

        private void Log(string msg)
        {
            Debug.Log(msg);
            OnEffectLog?.Invoke(msg);
        }
    }
}
