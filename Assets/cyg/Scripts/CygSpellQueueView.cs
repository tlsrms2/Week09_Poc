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

        private readonly List<CardData> registeredCards = new();

        private void Awake()
        {
            if (autoCreateMissingText)
                EnsureTextObjects();
        }

        private void OnEnable()
        {
            GameEvents.OnBlockPlaced += HandleBlockPlaced;
            GameEvents.OnDrawPhaseStarted += HandleDrawPhaseStarted;

            if (findGridManagerOnEnable && gridManager == null)
                gridManager = FindAnyObjectByType<GridManager>();

            RefreshView();
        }

        private void OnDisable()
        {
            GameEvents.OnBlockPlaced -= HandleBlockPlaced;
            GameEvents.OnDrawPhaseStarted -= HandleDrawPhaseStarted;
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
            ClearQueue();
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
                var (damage, defense) = gridManager != null ? gridManager.GetPreview() : (0, 0);
                totalText.SetText($"{attackLabel} {damage}\n{defenseLabel} {defense}");
            }
        }

        private string BuildSpellListText()
        {
            if (registeredCards.Count == 0)
                return emptyListText;

            var builder = new StringBuilder(256);
            for (int i = 0; i < registeredCards.Count; i++)
            {
                CardData card = registeredCards[i];
                string cardName = card != null ? card.CardName : "Unknown";
                builder.Append(i + 1);
                builder.Append(". ");
                builder.Append(cardName);
                builder.Append("  ");
                builder.Append(card != null && card.Type == CardType.Attack ? "공격" : "방어");
                builder.Append(" ");
                builder.Append(card != null ? card.BasePower : 0);

                if (i < registeredCards.Count - 1)
                    builder.AppendLine();
            }

            return builder.ToString();
        }

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
