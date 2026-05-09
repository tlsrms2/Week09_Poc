using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

        [Header("Fan Layout")]
        [SerializeField] private bool useFanLayout = true;
        [SerializeField, Min(0f)] private float fanEdgeDrop = 28f;
        [SerializeField, Min(0f)] private float fanMaxRotation = 8f;
        [SerializeField, Min(0f)] private float hoverRotationEase = 0.85f;

        private readonly List<CardVisual> cards = new();
        private readonly HashSet<Transform> inactiveCards = new();
        private readonly Dictionary<Transform, CardStaticPose> cardStaticPoses = new();
        private readonly Dictionary<Transform, CardPose> inactiveCardPoses = new();
        private Canvas rootCanvas;
        private Camera eventCamera;
        private int hoveredIndex = -1;
        private bool cardInfoPanelVisible;
        private float cardInfoHideTimer;
        private int cachedChildCount = -1;
        private Coroutine refreshRoutine;

        private void Awake()
        {
            CacheCanvasReferences();
        }

        private void OnEnable()
        {
            RequestRefreshCards();
        }

        private void OnDisable()
        {
            if (refreshRoutine != null)
            {
                StopCoroutine(refreshRoutine);
                refreshRoutine = null;
            }

            hoveredIndex = -1;
            SetCardInfoPanelVisible(false);
        }

        private void Update()
        {
            if (slotRoot != null && (cards.Count == 0 || cachedChildCount != slotRoot.childCount))
            {
                RequestRefreshCards();
            }

            if (HasInvalidCardReferences())
            {
                RequestRefreshCards();
                return;
            }

            bool isDraggingCard = IsAnyCardDragging();
            if (isDraggingCard)
            {
                hoveredIndex = -1;
            }
            else
            {
                UpdateHoveredIndex();
            }

            AnimateCards(isDraggingCard);
            SetCardInfoPanelVisible(showCardInfoPanelOnHover && (hoveredIndex >= 0 || isDraggingCard));
        }

        public void RefreshCards()
        {
            RequestRefreshCards();
        }

        private void RequestRefreshCards()
        {
            if (!isActiveAndEnabled)
            {
                RefreshCardsNow();
                return;
            }

            if (refreshRoutine != null)
            {
                return;
            }

            refreshRoutine = StartCoroutine(RefreshCardsAfterLayout());
        }

        private IEnumerator RefreshCardsAfterLayout()
        {
            yield return null;

            Canvas.ForceUpdateCanvases();
            if (slotRoot is RectTransform slotRootRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(slotRootRect);
            }

            RefreshCardsNow();
            refreshRoutine = null;
        }

        private void RefreshCardsNow()
        {
            CacheCanvasReferences();

            if (slotRoot == null)
            {
                slotRoot = transform;
            }

            if (cardInfoPanel == null)
            {
                cardInfoPanel = FindInRoot("Panel_CardInfo");
            }

            cards.Clear();
            cachedChildCount = slotRoot.childCount;

            for (int i = 0; i < slotRoot.childCount; i++)
            {
                RectTransform card = FindCardVisual(slotRoot.GetChild(i));
                if (card == null)
                {
                    continue;
                }

                if (!cardStaticPoses.TryGetValue(card, out CardStaticPose staticPose))
                {
                    staticPose = new CardStaticPose(card.localScale, card.localRotation);
                    cardStaticPoses.Add(card, staticPose);
                }

                cards.Add(new CardVisual(card, staticPose));
            }
        }

        private void CacheCanvasReferences()
        {
            rootCanvas = GetComponentInParent<Canvas>();
            eventCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;
        }

        public void ClearHover()
        {
            hoveredIndex = -1;
        }

        public void MarkCardInactive(GameObject cardObject)
        {
            if (cardObject != null)
            {
                MarkCardInactive(cardObject.transform);
            }
        }

        public void MarkCardInactive(Transform cardTransform)
        {
            if (cardTransform == null)
            {
                return;
            }

            inactiveCards.Add(cardTransform);

            if (cardTransform is RectTransform rectTransform)
            {
                inactiveCardPoses[cardTransform] = new CardPose(
                    rectTransform.anchoredPosition,
                    rectTransform.localScale,
                    rectTransform.localRotation
                );
            }

            if (cardTransform.TryGetComponent(out CanvasGroup canvasGroup))
            {
                canvasGroup.alpha = 0.35f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            cardTransform.gameObject.SetActive(true);
            ClearHover();
            RequestRefreshCards();
        }

        public void RestoreCardActive(GameObject cardObject)
        {
            if (cardObject != null)
            {
                RestoreCardActive(cardObject.transform);
            }
        }

        public void RestoreCardActive(Transform cardTransform)
        {
            if (cardTransform == null)
            {
                return;
            }

            inactiveCards.Remove(cardTransform);
            inactiveCardPoses.Remove(cardTransform);

            if (cardTransform.TryGetComponent(out CanvasGroup canvasGroup))
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            RequestRefreshCards();
        }

        public void SetCardInfoPanel(GameObject panel)
        {
            cardInfoPanel = panel;
            cardInfoPanelVisible = panel != null && panel.activeSelf;
            cardInfoHideTimer = 0f;
        }

        private RectTransform FindCardVisual(Transform slot)
        {
            if (slot.name.StartsWith(cardNamePrefix) || slot.name.StartsWith("PF_Card") || slot.GetComponent<CanvasGroup>() != null)
            {
                return slot as RectTransform;
            }

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
                if (!IsHoverTargetAvailable(rectTransform) || IsMarkedInactive(rectTransform))
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

        private void AnimateCards(bool suppressHover)
        {
            float t = 1f - Mathf.Exp(-animationSpeed * Time.unscaledDeltaTime);

            for (int i = 0; i < cards.Count; i++)
            {
                CardVisual card = cards[i];
                if (!IsHoverTargetAvailable(card.RectTransform))
                {
                    continue;
                }

                if (TryApplyInactivePose(card.RectTransform))
                {
                    continue;
                }

                float fanAmount = GetFanAmount(i);
                Vector2 targetPosition = card.BasePosition + GetFanOffset(fanAmount);
                Vector3 targetScale = card.BaseScale;
                Quaternion targetRotation = GetFanRotation(card.BaseRotation, fanAmount);

                if (!suppressHover && hoveredIndex >= 0)
                {
                    int distanceFromHover = i - hoveredIndex;
                    if (distanceFromHover == 0)
                    {
                        targetPosition += Vector2.up * hoverLift;
                        targetScale = card.BaseScale * hoverScale;
                        targetRotation = Quaternion.Slerp(targetRotation, card.BaseRotation, hoverRotationEase);
                    }
                    else
                    {
                        float direction = Mathf.Sign(distanceFromHover);
                        float falloff = Mathf.Pow(pushFalloff, Mathf.Abs(distanceFromHover) - 1);
                        targetPosition += Vector2.right * (direction * neighborPush * falloff);
                    }
                }

                card.RectTransform.anchoredPosition = Vector2.Lerp(card.RectTransform.anchoredPosition, targetPosition, t);
                card.RectTransform.localScale = Vector3.Lerp(card.RectTransform.localScale, targetScale, t);
                card.RectTransform.localRotation = Quaternion.Slerp(card.RectTransform.localRotation, targetRotation, t);
            }
        }

        private bool HasInvalidCardReferences()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].RectTransform == null)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsAnyCardDragging()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                RectTransform rectTransform = cards[i].RectTransform;
                if (!IsHoverTargetAvailable(rectTransform) || IsMarkedInactive(rectTransform))
                {
                    continue;
                }

                CanvasGroup canvasGroup = rectTransform.GetComponent<CanvasGroup>();
                if (canvasGroup != null && (!canvasGroup.blocksRaycasts || canvasGroup.alpha < 0.99f))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsHoverTargetAvailable(RectTransform rectTransform)
        {
            return rectTransform != null && rectTransform.gameObject.activeInHierarchy;
        }

        private bool IsMarkedInactive(RectTransform rectTransform)
        {
            return rectTransform != null && inactiveCards.Contains(rectTransform);
        }

        private bool TryApplyInactivePose(RectTransform rectTransform)
        {
            if (rectTransform == null || !inactiveCardPoses.TryGetValue(rectTransform, out CardPose pose))
            {
                return false;
            }

            float t = 1f - Mathf.Exp(-animationSpeed * Time.unscaledDeltaTime);
            rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, pose.Position, t);
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, pose.Scale, t);
            rectTransform.localRotation = Quaternion.Slerp(rectTransform.localRotation, pose.Rotation, t);
            return true;
        }

        private float GetFanAmount(int index)
        {
            if (!useFanLayout || cards.Count <= 1)
            {
                return 0f;
            }

            return Mathf.Lerp(-1f, 1f, index / (float)(cards.Count - 1));
        }

        private Vector2 GetFanOffset(float fanAmount)
        {
            if (!useFanLayout)
            {
                return Vector2.zero;
            }

            return Vector2.down * (fanEdgeDrop * fanAmount * fanAmount);
        }

        private Quaternion GetFanRotation(Quaternion baseRotation, float fanAmount)
        {
            if (!useFanLayout)
            {
                return baseRotation;
            }

            return baseRotation * Quaternion.Euler(0f, 0f, -fanAmount * fanMaxRotation);
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
            public CardVisual(RectTransform rectTransform, CardStaticPose staticPose)
            {
                RectTransform = rectTransform;
                BasePosition = rectTransform.anchoredPosition;
                BaseScale = staticPose.Scale;
                BaseRotation = staticPose.Rotation;
            }

            public RectTransform RectTransform { get; }
            public Vector2 BasePosition { get; }
            public Vector3 BaseScale { get; }
            public Quaternion BaseRotation { get; }
        }

        private readonly struct CardStaticPose
        {
            public CardStaticPose(Vector3 scale, Quaternion rotation)
            {
                Scale = scale;
                Rotation = rotation;
            }

            public Vector3 Scale { get; }
            public Quaternion Rotation { get; }
        }

        private readonly struct CardPose
        {
            public CardPose(Vector2 position, Vector3 scale, Quaternion rotation)
            {
                Position = position;
                Scale = scale;
                Rotation = rotation;
            }

            public Vector2 Position { get; }
            public Vector3 Scale { get; }
            public Quaternion Rotation { get; }
        }
    }
}
