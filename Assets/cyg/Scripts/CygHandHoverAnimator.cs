using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cyg.UI
{
    [DisallowMultipleComponent]
    public sealed class CygHandHoverAnimator : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private Transform slotRoot;
        [SerializeField] private string cardNamePrefix = "Card";
        [SerializeField] private GameObject cardInfoPanel;
        [SerializeField] private bool showCardInfoPanelOnHover = true;
        [SerializeField, Min(0f)] private float cardInfoHideDelay = 0.2f;

        [Header("Hover Motion")]
        [SerializeField, Min(1f)] private float hoverScale = 1.12f;
        [SerializeField, Min(0f)] private float hoverLift = 55f;
        [SerializeField, Min(0f)] private float neighborPush = 42f;
        [SerializeField, Range(0f, 1f)] private float pushFalloff = 0.55f;
        [SerializeField, Min(1f)] private float animationSpeed = 16f;

        [Header("Layering")]
        [SerializeField] private int normalSortingOrder = 1;
        [SerializeField] private int hoverSortingOrder = 20;

        private readonly List<CardVisual> cards = new();
        private Canvas rootCanvas;
        private Camera eventCamera;
        private int hoveredIndex = -1;
        private bool cardInfoPanelVisible;
        private float cardInfoHideTimer;

        private void Awake()
        {
            RefreshCards();
        }

        private void OnEnable()
        {
            RefreshCards();
        }

        private void OnDisable()
        {
            hoveredIndex = -1;
            SetCardInfoPanelVisible(false);
        }

        private void Update()
        {
            if (cards.Count == 0)
            {
                RefreshCards();
            }

            UpdateHoveredIndex();
            AnimateCards();
            SetCardInfoPanelVisible(showCardInfoPanelOnHover && hoveredIndex >= 0);
        }

        public void RefreshCards()
        {
            rootCanvas = GetComponentInParent<Canvas>();
            eventCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            if (slotRoot == null)
            {
                slotRoot = transform;
            }

            if (cardInfoPanel == null)
            {
                cardInfoPanel = FindInRoot("Panel_CardInfo");
            }

            cards.Clear();

            for (int i = 0; i < slotRoot.childCount; i++)
            {
                RectTransform card = FindCardVisual(slotRoot.GetChild(i));
                if (card == null)
                {
                    continue;
                }

                Canvas cardCanvas = card.GetComponent<Canvas>();
                if (cardCanvas == null)
                {
                    cardCanvas = card.gameObject.AddComponent<Canvas>();
                }

                cardCanvas.overrideSorting = true;
                cardCanvas.sortingOrder = normalSortingOrder;

                cards.Add(new CardVisual(card, cardCanvas));
            }
        }

        public void ClearHover()
        {
            hoveredIndex = -1;
        }

        public void SetCardInfoPanel(GameObject panel)
        {
            cardInfoPanel = panel;
            cardInfoPanelVisible = panel != null && panel.activeSelf;
            cardInfoHideTimer = 0f;
        }

        private RectTransform FindCardVisual(Transform slot)
        {
            for (int i = 0; i < slot.childCount; i++)
            {
                Transform child = slot.GetChild(i);
                if (child.name.StartsWith(cardNamePrefix))
                {
                    return child as RectTransform;
                }
            }

            return slot.childCount > 0 ? slot.GetChild(0) as RectTransform : null;
        }

        private GameObject FindInRoot(string objectName)
        {
            Transform searchRoot = rootCanvas != null ? rootCanvas.transform : transform.root;
            Transform[] children = searchRoot.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == objectName)
                {
                    return children[i].gameObject;
                }
            }

            return null;
        }

        private void UpdateHoveredIndex()
        {
            if (!TryGetPointerPosition(out Vector2 pointerPosition))
            {
                hoveredIndex = -1;
                return;
            }

            int nextHoveredIndex = -1;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < cards.Count; i++)
            {
                RectTransform rectTransform = cards[i].RectTransform;
                if (!rectTransform.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pointerPosition, eventCamera))
                {
                    continue;
                }

                Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(eventCamera, rectTransform.position);
                float distance = (screenCenter - pointerPosition).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nextHoveredIndex = i;
                }
            }

            hoveredIndex = nextHoveredIndex;
        }

        private bool TryGetPointerPosition(out Vector2 pointerPosition)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                pointerPosition = Mouse.current.position.ReadValue();
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            pointerPosition = Input.mousePosition;
            return true;
#else
            pointerPosition = default;
            return false;
#endif
        }

        private void AnimateCards()
        {
            float t = 1f - Mathf.Exp(-animationSpeed * Time.unscaledDeltaTime);

            for (int i = 0; i < cards.Count; i++)
            {
                CardVisual card = cards[i];
                Vector2 targetPosition = card.BasePosition;
                Vector3 targetScale = card.BaseScale;
                int sortingOrder = normalSortingOrder;

                if (hoveredIndex >= 0)
                {
                    int distanceFromHover = i - hoveredIndex;
                    if (distanceFromHover == 0)
                    {
                        targetPosition += Vector2.up * hoverLift;
                        targetScale = card.BaseScale * hoverScale;
                        sortingOrder = hoverSortingOrder;
                    }
                    else
                    {
                        float direction = Mathf.Sign(distanceFromHover);
                        float falloff = Mathf.Pow(pushFalloff, Mathf.Abs(distanceFromHover) - 1);
                        targetPosition += Vector2.right * (direction * neighborPush * falloff);
                    }
                }

                card.Canvas.sortingOrder = sortingOrder;
                card.RectTransform.anchoredPosition = Vector2.Lerp(card.RectTransform.anchoredPosition, targetPosition, t);
                card.RectTransform.localScale = Vector3.Lerp(card.RectTransform.localScale, targetScale, t);
            }
        }

        private void SetCardInfoPanelVisible(bool shouldShow)
        {
            if (cardInfoPanel == null)
            {
                return;
            }

            if (shouldShow)
            {
                cardInfoHideTimer = cardInfoHideDelay;
                if (!cardInfoPanelVisible)
                {
                    cardInfoPanel.SetActive(true);
                    cardInfoPanelVisible = true;
                }

                return;
            }

            if (!cardInfoPanelVisible)
            {
                return;
            }

            cardInfoHideTimer -= Time.unscaledDeltaTime;
            if (cardInfoHideTimer > 0f)
            {
                return;
            }

            cardInfoPanel.SetActive(false);
            cardInfoPanelVisible = false;
        }

        private readonly struct CardVisual
        {
            public CardVisual(RectTransform rectTransform, Canvas canvas)
            {
                RectTransform = rectTransform;
                Canvas = canvas;
                BasePosition = rectTransform.anchoredPosition;
                BaseScale = rectTransform.localScale;
            }

            public RectTransform RectTransform { get; }
            public Canvas Canvas { get; }
            public Vector2 BasePosition { get; }
            public Vector3 BaseScale { get; }
        }
    }
}
