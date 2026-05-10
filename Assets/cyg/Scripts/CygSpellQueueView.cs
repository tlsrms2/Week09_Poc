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
        [SerializeField] private string emptyTotalText = "";

        private static readonly CardType[] displayOrder =
        {
            CardType.Attack,
            CardType.Defense,
            CardType.Utility_NextTurnDraw
        };

        private readonly List<QueuedSpell> queuedSpells = new();

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
            queuedSpells.Clear();
            RefreshView();
        }

        private void HandleBlockPlaced(CardData card, int originX, int originY)
        {
            if (card != null)
                queuedSpells.Add(new QueuedSpell(card, originX, originY));

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
                totalText.SetText(BuildTotalText());
            }
        }

        private string BuildSpellListText()
        {
            if (queuedSpells.Count == 0)
                return emptyListText;

            var builder = new StringBuilder(256);
            for (int i = 0; i < queuedSpells.Count; i++)
            {
                QueuedSpell queuedSpell = queuedSpells[i];
                CardData card = queuedSpell.Card;
                string cardName = card != null ? card.CardName : "Unknown";
                builder.Append(i + 1);
                builder.Append(". ");
                builder.Append(cardName);
                builder.Append("  ");
                builder.Append(card != null ? GetQueueLabel(card.Type) : "효과");
                builder.Append(" ");
                builder.Append(GetEffectivePower(queuedSpell));

                if (i < queuedSpells.Count - 1)
                    builder.AppendLine();
            }

            return builder.ToString();
        }

        private string BuildTotalText()
        {
            if (queuedSpells.Count == 0)
                return emptyTotalText;

            var builder = new StringBuilder(96);

            foreach (CardType type in displayOrder)
            {
                int value = GetQueuedTotal(type);
                if (value <= 0)
                    continue;

                if (builder.Length > 0)
                    builder.AppendLine();

                builder.Append(GetTotalLabel(type));
                builder.Append(" ");
                builder.Append(value);
            }

            return builder.Length > 0 ? builder.ToString() : emptyTotalText;
        }

        private int GetQueuedTotal(CardType type)
        {
            int total = 0;
            for (int i = 0; i < queuedSpells.Count; i++)
            {
                QueuedSpell queuedSpell = queuedSpells[i];
                CardData card = queuedSpell.Card;
                if (card == null || card.Type != type)
                    continue;

                total += GetEffectivePower(queuedSpell);
            }

            return total;
        }

        private int GetEffectivePower(QueuedSpell queuedSpell)
        {
            CardData card = queuedSpell.Card;
            if (card == null)
                return 0;

            if (gridManager == null)
                return card.BasePower;

            int overlapBonus = 0;
            foreach (var (col, row, _) in card.GetOccupiedCells())
            {
                int gx = queuedSpell.OriginX + col;
                int gy = queuedSpell.OriginY + row;
                if (gx < 0 || gx >= GridManager.GridSize || gy < 0 || gy >= GridManager.GridSize)
                    continue;

                overlapBonus += Mathf.Max(0, gridManager.GetOverlapCount(gx, gy) - 1);
            }

            return card.BasePower * (1 + overlapBonus);
        }

        private static string GetQueueLabel(CardType type)
        {
            return type switch
            {
                CardType.Attack => "공격",
                CardType.Defense => "방어",
                CardType.Utility_NextTurnDraw => "다음 턴 드로우",
                _ => type.ToString()
            };
        }

        private static string GetTotalLabel(CardType type)
        {
            return type switch
            {
                CardType.Attack => "최종 공격",
                CardType.Defense => "최종 방어",
                CardType.Utility_NextTurnDraw => "다음 턴 드로우",
                _ => type.ToString()
            };
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

        private readonly struct QueuedSpell
        {
            public QueuedSpell(CardData card, int originX, int originY)
            {
                Card = card;
                OriginX = originX;
                OriginY = originY;
            }

            public CardData Card { get; }
            public int OriginX { get; }
            public int OriginY { get; }
        }

    }
}
