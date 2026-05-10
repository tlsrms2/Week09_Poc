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

        [Header("Overflow Fit")]
        [SerializeField] private bool keepCardsInsideSlotRoot = true;
        [SerializeField, Min(0f)] private float fitHorizontalPadding = 0f;

        [Header("Hover Layer")]
        [SerializeField] private bool bringHoveredCardToFront = true;
        [SerializeField] private int hoveredSortingOrder = 5000;
        [SerializeField] private bool routeRaycastsToHoveredCard = true;

        private readonly List<CardVisual> cards = new();
        private readonly HashSet<Transform> inactiveCards = new();
        private readonly Dictionary<Transform, CardStaticPose> cardStaticPoses = new();
        private readonly Dictionary<Transform, CardSortingState> cardSortingStates = new();
        private readonly Dictionary<Transform, bool> cardRaycastStates = new();
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
            ResetCardSorting();
            ResetCardRaycasts();
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
            ApplyHoverLayering(isDraggingCard);
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

            PrepareHoverLayerComponents();
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

            PrepareHoverLayerComponents();

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

            PruneMissingSortingStates();
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
            RestoreCardSorting(cardTransform);
            RestoreCardRaycast(cardTransform);

            if (cardTransform.TryGetComponent(out CanvasGroup canvasGroup))
            {
                canvasGroup.alpha = 0.35f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            cardTransform.gameObject.SetActive(true);
            ClearHover();
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
            RestoreCardSorting(cardTransform);
            RestoreCardRaycast(cardTransform);

            if (cardTransform.TryGetComponent(out CanvasGroup canvasGroup))
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }
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

        private void PrepareHoverLayerComponents()
        {
            if (!bringHoveredCardToFront || slotRoot == null)
            {
                return;
            }

            for (int i = 0; i < slotRoot.childCount; i++)
            {
                RectTransform card = FindCardVisual(slotRoot.GetChild(i));
                if (card == null)
                {
                    continue;
                }

                EnsureHoverLayerComponents(card);
            }
        }

        private void EnsureHoverLayerComponents(RectTransform card)
        {
            if (card == null)
            {
                return;
            }

            if (!card.TryGetComponent(out Canvas canvas))
            {
                canvas = card.gameObject.AddComponent<Canvas>();
            }

            if (!card.TryGetComponent(out GraphicRaycaster _))
            {
                card.gameObject.AddComponent<GraphicRaycaster>();
            }

            if (canvas.overrideSorting)
            {
                return;
            }

            if (rootCanvas != null)
            {
                canvas.sortingLayerID = rootCanvas.sortingLayerID;
            }

            canvas.sortingOrder = 0;
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

                bool isInactive = IsMarkedInactive(card.RectTransform);

                float fanAmount = GetFanAmount(i);
                Vector2 targetPosition = GetFittedBasePosition(i) + GetFanOffset(fanAmount);
                Vector3 targetScale = card.BaseScale;
                Quaternion targetRotation = GetFanRotation(card.BaseRotation, fanAmount);

                if (!suppressHover && !isInactive && hoveredIndex >= 0)
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

                targetPosition = ClampInsideSlotRoot(card.RectTransform, targetPosition, targetScale);

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
                if (canvasGroup != null && canvasGroup.alpha < 0.99f)
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

        private Vector2 GetFittedBasePosition(int index)
        {
            if (index < 0 || index >= cards.Count)
            {
                return Vector2.zero;
            }

            if (!keepCardsInsideSlotRoot)
            {
                return cards[index].BasePosition;
            }

            if (!TryGetParentLocalHorizontalBounds(out float left, out float right))
            {
                return cards[index].BasePosition;
            }

            float availableWidth = right - left;
            if (availableWidth <= 0f)
            {
                return cards[index].BasePosition;
            }

            float baseCenter = GetBaseContentCenterLocal();
            float fullWidth = GetScaledContentWidthLocal(baseCenter, 1f, out float fullMin, out float fullMax);
            if (fullWidth <= availableWidth && fullMin >= left && fullMax <= right)
            {
                return cards[index].BasePosition;
            }

            float low = 0f;
            float high = 1f;
            for (int i = 0; i < 14; i++)
            {
                float mid = (low + high) * 0.5f;
                float width = GetScaledContentWidthLocal(baseCenter, mid, out _, out _);
                if (width <= availableWidth)
                    low = mid;
                else
                    high = mid;
            }

            float fitScale = low;
            GetScaledContentWidthLocal(baseCenter, fitScale, out float scaledMin, out float scaledMax);
            float availableCenter = (left + right) * 0.5f;
            float scaledCenter = (scaledMin + scaledMax) * 0.5f;
            float offset = availableCenter - scaledCenter;

            CardVisual currentCard = cards[index];
            float basePivotLocalX = GetPivotLocalX(currentCard.RectTransform, currentCard.BasePosition);
            float fittedPivotLocalX = baseCenter + (basePivotLocalX - baseCenter) * fitScale + offset;

            Vector2 position = cards[index].BasePosition;
            position.x = GetAnchoredXFromPivotLocal(currentCard.RectTransform, fittedPivotLocalX);
            return position;
        }

        private float GetBaseContentCenterLocal()
        {
            float min = float.MaxValue;
            float max = float.MinValue;

            for (int i = 0; i < cards.Count; i++)
            {
                CardVisual card = cards[i];
                float pivotLocalX = GetPivotLocalX(card.RectTransform, card.BasePosition);
                float leftExtent = GetLeftExtent(card.RectTransform, card.BaseScale);
                float rightExtent = GetRightExtent(card.RectTransform, card.BaseScale);
                min = Mathf.Min(min, pivotLocalX - leftExtent);
                max = Mathf.Max(max, pivotLocalX + rightExtent);
            }

            return (min + max) * 0.5f;
        }

        private float GetScaledContentWidthLocal(float baseCenter, float spacingScale, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;

            for (int i = 0; i < cards.Count; i++)
            {
                CardVisual card = cards[i];
                float basePivotLocalX = GetPivotLocalX(card.RectTransform, card.BasePosition);
                float pivotLocalX = baseCenter + (basePivotLocalX - baseCenter) * spacingScale;
                float leftExtent = GetLeftExtent(card.RectTransform, card.BaseScale);
                float rightExtent = GetRightExtent(card.RectTransform, card.BaseScale);
                min = Mathf.Min(min, pivotLocalX - leftExtent);
                max = Mathf.Max(max, pivotLocalX + rightExtent);
            }

            return max - min;
        }

        private Vector2 ClampInsideSlotRoot(RectTransform card, Vector2 targetPosition, Vector3 targetScale)
        {
            if (!keepCardsInsideSlotRoot || card == null || !TryGetParentLocalHorizontalBounds(out float left, out float right))
            {
                return targetPosition;
            }

            float leftExtent = GetLeftExtent(card, targetScale);
            float rightExtent = GetRightExtent(card, targetScale);
            if (right - left <= leftExtent + rightExtent)
            {
                targetPosition.x = GetAnchoredXFromPivotLocal(card, (left + right) * 0.5f);
                return targetPosition;
            }

            float pivotLocalX = GetPivotLocalX(card, targetPosition);
            pivotLocalX = Mathf.Clamp(pivotLocalX, left + leftExtent, right - rightExtent);
            targetPosition.x = GetAnchoredXFromPivotLocal(card, pivotLocalX);
            return targetPosition;
        }

        private bool TryGetParentLocalHorizontalBounds(out float left, out float right)
        {
            left = 0f;
            right = 0f;

            RectTransform slotRootRect = slotRoot as RectTransform;
            if (slotRootRect == null)
            {
                return false;
            }

            Rect rect = slotRootRect.rect;
            left = rect.xMin + fitHorizontalPadding;
            right = rect.xMax - fitHorizontalPadding;
            return right > left;
        }

        private float GetPivotLocalX(RectTransform rectTransform, Vector2 anchoredPosition)
        {
            return GetAnchorReferenceX(rectTransform) + anchoredPosition.x;
        }

        private float GetAnchoredXFromPivotLocal(RectTransform rectTransform, float pivotLocalX)
        {
            return pivotLocalX - GetAnchorReferenceX(rectTransform);
        }

        private float GetAnchorReferenceX(RectTransform rectTransform)
        {
            if (slotRoot is not RectTransform slotRootRect || rectTransform == null)
            {
                return 0f;
            }

            Rect parentRect = slotRootRect.rect;
            float normalizedReference = Mathf.Lerp(rectTransform.anchorMin.x, rectTransform.anchorMax.x, rectTransform.pivot.x);
            return parentRect.xMin + parentRect.width * normalizedReference;
        }

        private static float GetLeftExtent(RectTransform rectTransform, Vector3 scale)
        {
            if (rectTransform == null)
            {
                return 0f;
            }

            return rectTransform.rect.width * Mathf.Abs(scale.x) * rectTransform.pivot.x;
        }

        private static float GetRightExtent(RectTransform rectTransform, Vector3 scale)
        {
            if (rectTransform == null)
            {
                return 0f;
            }

            return rectTransform.rect.width * Mathf.Abs(scale.x) * (1f - rectTransform.pivot.x);
        }

        private void ApplyHoverLayering(bool suppressHover)
        {
            if (!bringHoveredCardToFront)
            {
                ResetCardSorting();
                ResetCardRaycasts();
                return;
            }

            Transform hoveredCard = null;
            if (!suppressHover && hoveredIndex >= 0 && hoveredIndex < cards.Count)
            {
                hoveredCard = cards[hoveredIndex].RectTransform;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                RectTransform card = cards[i].RectTransform;
                if (card == null)
                {
                    continue;
                }

                if (card == hoveredCard && IsHoverTargetAvailable(card) && !IsMarkedInactive(card))
                {
                    RaiseCardSorting(card);
                    ApplyCardRaycast(card, true);
                }
                else if (hoveredCard == null || suppressHover)
                {
                    RestoreCardSorting(card);
                    RestoreCardRaycast(card);
                }
                else
                {
                    RestoreCardSorting(card);
                    ApplyCardRaycast(card, false);
                }
            }
        }

        private void RaiseCardSorting(RectTransform card)
        {
            EnsureHoverLayerComponents(card);
            Canvas canvas = card.GetComponent<Canvas>();
            if (canvas == null)
                return;

            if (!cardSortingStates.ContainsKey(card))
            {
                cardSortingStates.Add(card, new CardSortingState(canvas.overrideSorting, canvas.sortingLayerID, canvas.sortingOrder));
            }

            canvas.overrideSorting = true;
            if (rootCanvas != null)
            {
                canvas.sortingLayerID = rootCanvas.sortingLayerID;
                canvas.sortingOrder = rootCanvas.sortingOrder + hoveredSortingOrder;
            }
            else
            {
                canvas.sortingOrder = hoveredSortingOrder;
            }
        }

        private void RestoreCardSorting(Transform card)
        {
            if (card == null || !cardSortingStates.TryGetValue(card, out CardSortingState state))
            {
                return;
            }

            Canvas canvas = card.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = state.OverrideSorting;
                canvas.sortingLayerID = state.SortingLayerId;
                canvas.sortingOrder = state.SortingOrder;
            }

            cardSortingStates.Remove(card);
        }

        private void ResetCardSorting()
        {
            foreach (var entry in cardSortingStates)
            {
                if (entry.Key == null)
                {
                    continue;
                }

                Canvas canvas = entry.Key.GetComponent<Canvas>();
                if (canvas == null)
                {
                    continue;
                }

                canvas.overrideSorting = entry.Value.OverrideSorting;
                canvas.sortingLayerID = entry.Value.SortingLayerId;
                canvas.sortingOrder = entry.Value.SortingOrder;
            }

            cardSortingStates.Clear();
        }

        private void ApplyCardRaycast(RectTransform card, bool shouldReceiveRaycasts)
        {
            if (!routeRaycastsToHoveredCard || card == null || IsMarkedInactive(card))
            {
                return;
            }

            CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
            if (canvasGroup == null || canvasGroup.alpha < 0.99f)
            {
                return;
            }

            if (!cardRaycastStates.ContainsKey(card))
            {
                cardRaycastStates.Add(card, canvasGroup.blocksRaycasts);
            }

            canvasGroup.blocksRaycasts = shouldReceiveRaycasts;
        }

        private void RestoreCardRaycast(Transform card)
        {
            if (card == null || !cardRaycastStates.TryGetValue(card, out bool blocksRaycasts))
            {
                return;
            }

            CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = blocksRaycasts;
            }

            cardRaycastStates.Remove(card);
        }

        private void ResetCardRaycasts()
        {
            foreach (var entry in cardRaycastStates)
            {
                if (entry.Key == null)
                {
                    continue;
                }

                CanvasGroup canvasGroup = entry.Key.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = entry.Value;
                }
            }

            cardRaycastStates.Clear();
        }

        private void PruneMissingSortingStates()
        {
            List<Transform> missingCards = null;
            foreach (Transform card in cardSortingStates.Keys)
            {
                if (card != null && ContainsCard(card))
                {
                    continue;
                }

                missingCards ??= new List<Transform>();
                missingCards.Add(card);
            }

            if (missingCards == null)
            {
                return;
            }

            for (int i = 0; i < missingCards.Count; i++)
            {
                cardSortingStates.Remove(missingCards[i]);
            }
        }

        private bool ContainsCard(Transform card)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].RectTransform == card)
                {
                    return true;
                }
            }

            return false;
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

        private readonly struct CardSortingState
        {
            public CardSortingState(bool overrideSorting, int sortingLayerId, int sortingOrder)
            {
                OverrideSorting = overrideSorting;
                SortingLayerId = sortingLayerId;
                SortingOrder = sortingOrder;
            }

            public bool OverrideSorting { get; }
            public int SortingLayerId { get; }
            public int SortingOrder { get; }
        }
    }
}
