using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FWTCG.Core;

namespace FWTCG.UI
{
    /// <summary>
    /// Attached to a Card prefab. Displays unit data and handles click events.
    /// Left-click: gameplay action (play/select/target).
    /// Right-click: show card detail popup.
    /// DEV-8: visual states (stunned overlay, buff token, cost dimming).
    /// </summary>
    public class CardView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _costText;
        [SerializeField] private Text _atkText;
        [SerializeField] private Text _descText;
        [SerializeField] private Image _cardBg;
        [SerializeField] private Image _artImage;
        [SerializeField] private Button _clickButton;

        // DEV-8: visual state overlays
        [SerializeField] private Image _stunnedOverlay;
        [SerializeField] private GameObject _buffTokenIcon;
        [SerializeField] private Text _buffTokenText;

        // DEV-10: schematic cost display
        [SerializeField] private Text _schCostText;
        [SerializeField] private Image _schCostBg;

        // DEV-10: exhausted overlay (gray dimming)
        [SerializeField] private Image _exhaustedOverlay;

        private UnitInstance _unit;
        private bool _isPlayerCard;
        private Action<UnitInstance> _onClick;
        private Action<UnitInstance> _onRightClick;

        private bool _selected;
        private bool _faceDown;
        private bool _costInsufficient;
        private Coroutine _stunPulse;
        private CardGlow _cardGlow;

        private void Awake()
        {
            // Auto-wire SerializeField refs by child name if Inspector connections were lost
            if (_nameText == null)         _nameText         = FindDeepText("CardName");
            if (_costText == null)         _costText         = FindDeepText("CostText");
            if (_atkText == null)          _atkText          = FindDeepText("AtkText");
            if (_descText == null)         _descText         = FindDeepText("DescText");
            if (_artImage == null)         _artImage         = FindDeepImage("ArtImage");
            if (_cardBg == null)           _cardBg           = GetComponent<Image>();
            if (_clickButton == null)      _clickButton      = GetComponent<Button>();
            if (_stunnedOverlay == null)   _stunnedOverlay   = FindDeepImage("StunnedOverlay");
            if (_buffTokenIcon == null)    { var t = FindDeep("BuffTokenIcon"); if (t) _buffTokenIcon = t.gameObject; }
            if (_buffTokenText == null)    _buffTokenText    = FindDeepText("BuffText");
            if (_schCostText == null)      _schCostText      = FindDeepText("SchCostText");
            if (_schCostBg == null)        _schCostBg        = FindDeepImage("SchCostBg");
            if (_exhaustedOverlay == null) _exhaustedOverlay = FindDeepImage("ExhaustedOverlay");

            if (_clickButton != null)
                _clickButton.onClick.AddListener(HandleClick);

            // Init glow controller — uses the material already assigned to _cardBg by SceneBuilder
            _cardGlow = GetComponent<CardGlow>();
            if (_cardGlow != null && _cardBg != null && _cardBg.material != null
                && _cardBg.material != Canvas.GetDefaultCanvasMaterial())
            {
                _cardGlow.Init(_cardBg, _cardBg.material);
            }
        }

        // ── Auto-wire helpers ─────────────────────────────────────────────────

        private Transform FindDeep(string childName)
        {
            return FindDeepIn(transform, childName);
        }

        private static Transform FindDeepIn(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var result = FindDeepIn(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }

        private Text FindDeepText(string childName)
        {
            var t = FindDeep(childName);
            return t != null ? t.GetComponent<Text>() : null;
        }

        private Image FindDeepImage(string childName)
        {
            var t = FindDeep(childName);
            return t != null ? t.GetComponent<Image>() : null;
        }

        private void OnDestroy()
        {
            if (_clickButton != null)
                _clickButton.onClick.RemoveListener(HandleClick);
            if (_stunPulse != null)
                StopCoroutine(_stunPulse);
        }

        public void Setup(UnitInstance unit, bool isPlayerCard, Action<UnitInstance> onClick,
                          Action<UnitInstance> onRightClick = null)
        {
            _unit = unit;
            _isPlayerCard = isPlayerCard;
            _onClick = onClick;
            _onRightClick = onRightClick;
            _costInsufficient = false;

            Refresh();

            if (_clickButton != null)
                _clickButton.interactable = onClick != null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (_unit != null && _onRightClick != null && !_faceDown)
                    _onRightClick(_unit);
            }
        }

        public void SetFaceDown(bool faceDown)
        {
            _faceDown = faceDown;
            RefreshFaceDown();
        }

        private void RefreshFaceDown()
        {
            bool hide = _faceDown;
            if (_nameText != null) _nameText.gameObject.SetActive(!hide);
            if (_costText != null) _costText.gameObject.SetActive(!hide);
            if (_atkText  != null) _atkText.gameObject.SetActive(!hide);
            if (_descText != null) _descText.gameObject.SetActive(!hide);
            if (_artImage != null) _artImage.enabled = !hide;
            if (_cardBg   != null) _cardBg.color = hide ? GameColors.CardFaceDown
                : (_selected ? GameColors.CardSelected
                : (_unit != null && _unit.Exhausted ? GameColors.CardExhausted
                : (_isPlayerCard ? GameColors.CardPlayer : GameColors.CardEnemy)));
            if (_clickButton != null) _clickButton.interactable = !hide;
            if (_stunnedOverlay != null) _stunnedOverlay.gameObject.SetActive(false);
            if (_buffTokenIcon != null) _buffTokenIcon.SetActive(false);
        }

        public void Refresh()
        {
            if (_faceDown) { RefreshFaceDown(); return; }
            if (_unit == null) return;

            if (_nameText != null)
                _nameText.text = _unit.UnitName;

            if (_costText != null)
                _costText.text = _unit.CardData.Cost.ToString();

            if (_atkText != null)
            {
                if (_unit.CardData.IsSpell)
                {
                    _atkText.text = "法";
                }
                else
                {
                    _atkText.text = $"{_unit.CurrentAtk}";
                    if (_unit.CurrentHp != _unit.CurrentAtk)
                        _atkText.text = $"{_unit.CurrentHp}/{_unit.CurrentAtk}";
                }
            }

            if (_descText != null)
                _descText.text = _unit.CardData.Description;

            if (_artImage != null && _unit.CardData.ArtSprite != null)
            {
                _artImage.sprite = _unit.CardData.ArtSprite;
                _artImage.enabled = true;
            }
            else if (_artImage != null)
            {
                _artImage.enabled = false;
            }

            // Background color
            if (_cardBg != null)
            {
                Color baseColor;
                if (_unit.CardData.IsSpell)
                    baseColor = _isPlayerCard ? GameColors.CardSpellPlayer : GameColors.CardSpellEnemy;
                else
                    baseColor = _isPlayerCard ? GameColors.CardPlayer : GameColors.CardEnemy;

                if (_selected)
                    baseColor = GameColors.CardSelected;
                else if (_unit.Exhausted)
                    baseColor = GameColors.CardExhausted;

                // Cost insufficient dimming
                if (_costInsufficient)
                    baseColor *= GameColors.CostDimFactor;

                _cardBg.color = baseColor;
            }

            // Stunned overlay
            if (_stunnedOverlay != null)
            {
                bool stunned = _unit.Stunned;
                _stunnedOverlay.gameObject.SetActive(stunned);
                if (stunned && _stunPulse == null)
                    _stunPulse = StartCoroutine(StunPulseRoutine());
                else if (!stunned && _stunPulse != null)
                {
                    StopCoroutine(_stunPulse);
                    _stunPulse = null;
                }
            }

            // Exhausted overlay (gray dim)
            if (_exhaustedOverlay != null)
                _exhaustedOverlay.gameObject.SetActive(_unit.Exhausted && !_unit.Stunned);

            // Glow border (playable = affordable + not exhausted for hand cards)
            if (_cardGlow != null)
            {
                if (_isPlayerCard && !_unit.Exhausted && !_costInsufficient)
                    _cardGlow.SetPlayable(true);
                else
                    _cardGlow.SetPlayable(false);
            }

            // Buff token indicator
            if (_buffTokenIcon != null)
            {
                bool hasBuff = _unit.BuffTokens > 0;
                _buffTokenIcon.SetActive(hasBuff);
                if (hasBuff && _buffTokenText != null)
                    _buffTokenText.text = $"+{_unit.BuffTokens}";
            }

            // Schematic (rune) cost display
            if (_schCostText != null && _schCostBg != null)
            {
                int schCost = _unit.CardData.RuneCost;
                if (schCost > 0)
                {
                    _schCostText.gameObject.SetActive(true);
                    _schCostBg.gameObject.SetActive(true);
                    string rtShort = "";
                    switch (_unit.CardData.RuneType)
                    {
                        case Data.RuneType.Blazing: rtShort = "炽"; break;
                        case Data.RuneType.Radiant: rtShort = "灵"; break;
                        case Data.RuneType.Verdant: rtShort = "翠"; break;
                        case Data.RuneType.Crushing: rtShort = "摧"; break;
                        default: rtShort = "符"; break;
                    }
                    _schCostText.text = $"{rtShort}×{schCost}";
                    _schCostBg.color = GameColors.GetRuneColor(_unit.CardData.RuneType);
                }
                else
                {
                    _schCostText.gameObject.SetActive(false);
                    _schCostBg.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Mark card as too expensive (dims the card).
        /// Called by GameUI for hand cards when mana is insufficient.
        /// </summary>
        public void SetCostInsufficient(bool insufficient)
        {
            _costInsufficient = insufficient;
            Refresh();
        }

        public UnitInstance Unit => _unit;

        public void SetSelected(bool selected)
        {
            _selected = selected;
            Refresh();
        }

        private void HandleClick()
        {
            if (_unit != null && _onClick != null)
                _onClick(_unit);
        }

        private IEnumerator StunPulseRoutine()
        {
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime * 2f; // 2 Hz pulse
                float alpha = Mathf.Lerp(0.15f, 0.45f, (Mathf.Sin(t) + 1f) * 0.5f);
                if (_stunnedOverlay != null)
                {
                    var c = GameColors.StunnedOverlay;
                    c.a = alpha;
                    _stunnedOverlay.color = c;
                }
                yield return null;
            }
        }
    }
}
