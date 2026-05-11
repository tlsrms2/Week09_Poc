using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Cyg.UI
{
    [DisallowMultipleComponent]
    public sealed class CygSpellQueueView : MonoBehaviour
    {
        [Header("Runtime Source")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private bool findGridManagerOnEnable = true;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI spellListText;
        [SerializeField] private TextMeshProUGUI totalText;
        [SerializeField] private bool autoCreateMissingText = true;

        [Header("Display")]
        [SerializeField] private TMP_FontAsset sharedFont;
        [SerializeField] private string emptyListText = "등록된 주문 없음";
        [SerializeField] private string title = "등록 주문";
        [SerializeField] private string attackLabel = "최종 공격";
        [SerializeField] private string defenseLabel = "최종 방어";
        [SerializeField] private Color activeSpellColor = new Color(1f, 0.85f, 0.1f);  // 결산 중인 카드 색상

        private readonly List<CardData> registeredCards = new();
        private int resolvingIndex = -1;
        private int resolvedCount = 0;
        private int instantOverlapDamage = 0;
        private int instantOverlapDefense = 0;

        private void Awake()
        {
            if (autoCreateMissingText)
                EnsureTextObjects();
        }

        private void OnEnable()
        {
            GameEvents.OnBlockPlaced            += HandleBlockPlaced;
            GameEvents.OnDrawPhaseStarted       += HandleDrawPhaseStarted;
            GameEvents.OnResolutionPhaseStarted += HandleResolutionPhaseStarted;
            GameEvents.OnResolutionResult       += HandleResolutionResult;
            GameEvents.OnResolutionComplete     += HandleResolutionComplete;
            GameEvents.OnOverlapEffectTriggered += HandleOverlapEffectTriggered;

            if (findGridManagerOnEnable && gridManager == null)
                gridManager = FindAnyObjectByType<GridManager>();

            RefreshView();
        }

        private void OnDisable()
        {
            GameEvents.OnBlockPlaced            -= HandleBlockPlaced;
            GameEvents.OnDrawPhaseStarted       -= HandleDrawPhaseStarted;
            GameEvents.OnResolutionPhaseStarted -= HandleResolutionPhaseStarted;
            GameEvents.OnResolutionResult       -= HandleResolutionResult;
            GameEvents.OnResolutionComplete     -= HandleResolutionComplete;
            GameEvents.OnOverlapEffectTriggered -= HandleOverlapEffectTriggered;
        }

        public void ClearQueue()
        {
            registeredCards.Clear();
            RefreshView();
        }

        private void HandleBlockPlaced(CardData card, int _, int __)
        {
            if (card != null)
                registeredCards.Add(card);

            RefreshView();
        }

        private void HandleDrawPhaseStarted(int _)
        {
            instantOverlapDamage = 0;
            instantOverlapDefense = 0;
            ClearQueue();
        }

        private void HandleOverlapEffectTriggered(ResolutionResult result)
        {
            instantOverlapDamage  += result.damage;
            instantOverlapDefense += result.defense;
            RefreshView();
        }

        private void HandleResolutionPhaseStarted()
        {
            resolvedCount = 0;
            resolvingIndex = 0;
            RefreshView();
        }

        private void HandleResolutionResult(ResolutionResult _)
        {
            resolvingIndex = resolvedCount;
            resolvedCount++;
            RefreshView();
        }

        private void HandleResolutionComplete()
        {
            resolvingIndex = -1;
            resolvedCount = 0;
            RefreshView();
        }

        private void RefreshView()
        {
            if (autoCreateMissingText)
                EnsureTextObjects();

            if (titleText != null)
            {
                ApplySharedFont(titleText);
                titleText.SetText(title);
            }

            if (spellListText != null)
            {
                ApplySharedFont(spellListText);
                spellListText.SetText(BuildSpellListText());
            }

            if (totalText != null)
            {
                ApplySharedFont(totalText);
                ResolutionResult r = gridManager != null ? gridManager.GetPreview() : default;
                string atkText = instantOverlapDamage  > 0 ? $"{r.damage} (+{instantOverlapDamage} 즉시)"  : r.damage.ToString();
                string defText = instantOverlapDefense > 0 ? $"{r.defense} (+{instantOverlapDefense} 즉시)" : r.defense.ToString();
                totalText.SetText($"{attackLabel} {atkText}\n{defenseLabel} {defText}");
            }
        }

        private string BuildSpellListText()
        {
            if (registeredCards.Count == 0)
                return emptyListText;

            var blockPreviews = gridManager != null ? gridManager.GetPlacedBlockPreviews() : null;
            string hexColor = ColorUtility.ToHtmlStringRGB(activeSpellColor);
            var builder = new StringBuilder(256);
            for (int i = 0; i < registeredCards.Count; i++)
            {
                CardData card = registeredCards[i];
                bool isResolving = i == resolvingIndex;
                int bonusDmg = (blockPreviews != null && i < blockPreviews.Length) ? blockPreviews[i].bonusDamage  : 0;
                int bonusDef = (blockPreviews != null && i < blockPreviews.Length) ? blockPreviews[i].bonusDefense : 0;

                if (isResolving) builder.Append($"<color=#{hexColor}>");

                builder.Append(i + 1);
                builder.Append(". ");
                builder.Append(card != null ? card.CardName : "Unknown");

                if (card != null)
                {
                    string desc = BuildDescriptionWithBonus(card, bonusDmg, bonusDef);
                    if (!string.IsNullOrWhiteSpace(desc))
                    {
                        builder.Append("  ");
                        builder.Append(desc);
                    }
                    else
                    {
                        int attackCount  = 0;
                        int defenseCount = 0;
                        foreach (var e in card.Effects)
                        {
                            if (e.effectType == CardType.Attack || e.effectType == CardType.Drain) attackCount++;
                            if (e.effectType == CardType.Defense) defenseCount++;
                        }
                        int perAttack  = attackCount  > 0 ? bonusDmg / attackCount  : 0;
                        int perDefense = defenseCount > 0 ? bonusDef / defenseCount : 0;

                        foreach (var effect in card.Effects)
                        {
                            int power = effect.power;
                            if (effect.effectType == CardType.Attack || effect.effectType == CardType.Drain) power += perAttack;
                            if (effect.effectType == CardType.Defense) power += perDefense;
                            builder.Append("  ");
                            builder.Append(GetEffectLabel(effect.effectType));
                            builder.Append(" ");
                            builder.Append(power);
                        }
                    }
                }

                if (isResolving) builder.Append("</color>");

                if (i < registeredCards.Count - 1)
                    builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string BuildDescriptionWithBonus(CardData card, int bonusDmg, int bonusDef)
        {
            string text = card.Description;
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var effects = card.Effects;
            int attackCount  = 0;
            int defenseCount = 0;
            foreach (var e in effects)
            {
                if (e.effectType == CardType.Attack || e.effectType == CardType.Drain) attackCount++;
                if (e.effectType == CardType.Defense) defenseCount++;
            }
            int perAttack  = attackCount  > 0 ? bonusDmg / attackCount  : 0;
            int perDefense = defenseCount > 0 ? bonusDef / defenseCount : 0;

            for (int i = 0; i < effects.Count; i++)
            {
                int power = effects[i].power;
                var type  = effects[i].effectType;
                if (type == CardType.Attack || type == CardType.Drain) power += perAttack;
                if (type == CardType.Defense)                          power += perDefense;
                text = text.Replace("{" + i + "}", power.ToString());
            }
            return text;
        }

        private static string GetEffectLabel(CardType type) => type switch
        {
            CardType.Attack  => "공격",
            CardType.Defense => "방어",
            CardType.Heal    => "회복",
            CardType.Drain   => "흡수",
            CardType.Draw               => "드로우(다음턴)",
            CardType.DrawNow            => "드로우(즉시)",
            CardType.OverlapBoostAttack    => "겹침공격증가",
            CardType.OverlapBoostDefense   => "겹침방어증가",
            CardType.BoostResolutionDamage  => "결산공격증가",
            CardType.BoostResolutionDefense => "결산방어증가",
            _                              => type.ToString(),
        };

        private void EnsureTextObjects()
        {
            titleText ??= FindChildText("Text_Title");
            spellListText ??= FindChildText("Text_SpellList");
            totalText ??= FindChildText("Text_Total");

            titleText ??= CreateText("Text_Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(-36f, 56f), 26f, FontStyles.Bold, TextAlignmentOptions.Center);
            spellListText ??= CreateText("Text_SpellList", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -24f), new Vector2(-44f, -170f), 20f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            totalText ??= CreateText("Text_Total", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(-36f, 92f), 24f, FontStyles.Bold, TextAlignmentOptions.BottomRight);
        }

        private TextMeshProUGUI FindChildText(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        private TextMeshProUGUI CreateText(
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(transform, false);

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var text = textObject.GetComponent<TextMeshProUGUI>();
            ApplySharedFont(text);
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = Color.black;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private void ApplySharedFont(TextMeshProUGUI text)
        {
            if (text != null && sharedFont != null)
                text.font = sharedFont;
        }

    }
}
