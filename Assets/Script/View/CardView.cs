using System;
using System.Collections.Generic;
using DG.Tweening;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Script.View
{
    [RequireComponent(typeof(CardViewData))]
    public class CardView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        // Data component that holds all serialized fields
        protected CardViewData viewData;

        private Vector3 startPosition;
        private Transform startParent;
        private Camera mainCamera;
        private CanvasGroup canvasGroup;
        private bool isDragging = false;

        [SerializeReference] public Card thisCard;



        private void Awake()
        {
            // Get the data component - this will always exist due to RequireComponent
            viewData = GetComponent<CardViewData>();
            
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[CardView] Main camera not found!");
            }

            // Setup canvas group for alpha control
            if (viewData.worldCanvas != null)
            {
                canvasGroup = viewData.worldCanvas.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = viewData.worldCanvas.gameObject.AddComponent<CanvasGroup>();
                }

                // Ensure canvas is in World Space mode
                if (viewData.worldCanvas.renderMode != RenderMode.WorldSpace)
                {
                    Debug.LogWarning($"[CardView] Canvas on {gameObject.name} should be in World Space mode!");
                }

                // Ensure there's a GraphicRaycaster for UI events
                if (viewData.worldCanvas.GetComponent<GraphicRaycaster>() == null)
                {
                    viewData.worldCanvas.gameObject.AddComponent<GraphicRaycaster>();
                }
            }
            

        }

        public virtual void Init(Card card, CardDataSo cardDataSo)
        {
            thisCard = card;
            ApplyData(cardDataSo);
            
            // ذخیره موقعیت شروع (جایی که Instantiate شده)
            Vector3 startPosition = transform.position;
            
            // Target position از card
            Vector3 targetPosition = card.Position;
            // Y رو ثابت نگه دار (از startPosition)
            targetPosition.y = startPosition.y;
            
            // تنظیم موقعیت اولیه
            transform.position = startPosition;
            transform.DOMove(targetPosition, 0.5f);
            
            // Reset VisualCard Y و Z به 0
            if (viewData.visualCard != null)
            {
                Vector3 visualPos = viewData.visualCard.localPosition;
                visualPos.y = 0;
                viewData.visualCard.localPosition = visualPos;
                
                Vector3 visualRot = viewData.visualCard.localEulerAngles;
                visualRot.z = 0;
                viewData.visualCard.localEulerAngles = visualRot;

            }
            

            
        }

        public virtual void Refresh(Card card, CardDataSo cardDataSo)
        {
            thisCard = card;
            ApplyData(cardDataSo);
        }

        void ApplyData(CardDataSo cardDataSo)
        {
            if (cardDataSo == null)
                return;

            if (viewData.spriteRenderer != null)
                viewData.spriteRenderer.sprite = cardDataSo.sprite;
            
            if (viewData.nameText != null)
                viewData.nameText.text = cardDataSo.type;
            
            if (viewData.valueText != null)
                viewData.valueText.text = cardDataSo.value.ToString();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("Clicked: " + (viewData.nameText != null ? viewData.nameText.text : "Card"));
            OnClick();
        }

        public virtual void OnClick()
        {
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            startPosition = transform.position;
            startParent = transform.parent;
            

            
            // Calculate offset between card position and mouse world position
            Vector3 mouseWorldPos = GetMouseWorldPosition(eventData.position);
            transform.position = mouseWorldPos;
            
            Card bottomCard = GamePlayManager.Instance.GetCardById(thisCard.BottomCardId);
            if (bottomCard != null)
            {
                GamePlayManager.Instance.GetCardViewByCard(bottomCard).thisCard.RemoveFromGroup(thisCard);
            }

            // Increase sorting order for dragged card and its stack
            UpdateCanvasSortingOrder(10000);
            
            // Mark top cards as being dragged too (so they don't update their sorting based on position)
            var topViews = TopCardViews();
            foreach (var topView in topViews)
            {
                topView.isDragging = true;
                topView.UpdateCanvasSortingOrder(10000);
            }

            // Make card semi-transparent while dragging
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.8f;
                canvasGroup.blocksRaycasts = false; // Don't block raycasts to other cards
            }
            
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (mainCamera == null) return;

            // Convert screen point to world point on Y=0 plane
            Vector3 mouseWorldPos = GetMouseWorldPosition(eventData.position);
            transform.position = mouseWorldPos;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            

            
            // Reset isDragging for top cards too
            var topViews = TopCardViews();
            foreach (var topView in topViews)
            {
                topView.isDragging = false;
            }
            
            // Restore alpha and raycasts
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
            
            // برگرداندن VisualCard به ارتفاع و زاویه عادی (بدون انیمیشن)
            if (viewData.visualCard != null)
            {
                Vector3 visualPos = viewData.visualCard.localPosition;
                visualPos.y = 0;
                viewData.visualCard.localPosition = visualPos;
                
                Vector3 visualRot = viewData.visualCard.localEulerAngles;
                visualRot.z = 0;
                viewData.visualCard.localEulerAngles = visualRot;
            }
            
            thisCard.Position = transform.position;

            // UI Raycast to find what we dropped on
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                if (result.gameObject == null) continue;

                // Check for SellZone first (check in parent hierarchy too)
                var sellZone = result.gameObject.GetComponentInParent<SellZone>();
                if (sellZone != null)
                {
                    sellZone.SellCard(this);
                    return; // Card is sold, exit early
                }
                
                // Check for ShopZone (check in parent hierarchy too)
                var shopZone = result.gameObject.GetComponentInParent<ShopZone>();
                if (shopZone != null)
                {
                    if (shopZone.TryPurchase(this))
                    {
                        return; // Purchase processed, exit early
                    }
                    // If purchase failed, continue checking other targets
                }
                
                // Check for CardView in parent hierarchy (since CardView is on parent of Canvas)
                var slot = result.gameObject.GetComponentInParent<CardView>();
                if (slot != null && slot != this && !topViews.Contains(slot))
                {
                    if(GamePlayManager.Instance.GetCardById(slot.thisCard.TopCardId) != null)
                        continue;
                    
                    // Try to add to group - may fail if Pini restrictions apply
                    if (slot.thisCard.AddToGroup(thisCard))
                    {
                        break; // Successfully merged
                    }
                }
            }

            // Reset sorting order after drag (for this card and top cards)
            UpdateCanvasSortingOrderBasedOnPosition();
            foreach (var topView in topViews)
            {
                topView.UpdateCanvasSortingOrderBasedOnPosition();
            }
        }

        List<CardView> TopCardViews()
        {
            var result = new List<CardView>(); 
            Card topCard = GamePlayManager.Instance.GetCardById(thisCard.TopCardId);
            if (topCard == null)
                return result;
            int counter = 0;
            while (topCard != null && counter++ < 100)
            {
                result.Add(GamePlayManager.Instance.GetCardViewByCard(topCard));
                topCard = GamePlayManager.Instance.GetCardById(topCard.TopCardId);
            }
            return result;
        }

        private void Update()
        {
            UpdateView();
        }

        protected virtual void UpdateView()
        {
            if (thisCard == null)
                return;

            var bottom = GamePlayManager.Instance.GetCardById(thisCard.BottomCardId);
            var bottomView = bottom != null ? GamePlayManager.Instance.GetCardViewByCard(bottom) : null;
            bool isTopCardBeingDragged = isDragging && bottomView != null && bottomView.isDragging;
            
            // Instant movement when dragging
            if (isDragging)
            {
                // اگر این یک top card است که bottom card آن در حال درگ است، به سمت bottom card حرکت کن
                if (isTopCardBeingDragged)
                {
                    // top card باید به سمت bottom card حرکت کند (instant)
                    Vector3 targetPos = bottomView.transform.position + viewData.groupOffset;
                    transform.position = targetPos;
                }
                else
                {
                    // کارت اصلی موقعیتش در OnDrag تنظیم می‌شود
                }
                
                // VisualCard همیشه در زاویه 0 است (بدون انیمیشن)
                if (viewData.visualCard != null)
                {
                    Vector3 newRotation = viewData.visualCard.localEulerAngles;
                    newRotation.z = 0;
                    viewData.visualCard.localEulerAngles = newRotation;
                }
            }

            bool isFollowingBottom = false;
            // اگر bottom card وجود دارد و کارت در حال درگ نیست، به سمت bottom card حرکت کن (instant)
            if (bottom != null && !isDragging)
            {
                if (bottomView != null)
                {
                    transform.position = bottomView.transform.position + viewData.groupOffset;
                    isFollowingBottom = true;
                }
            }
            else
            {
                // VisualCard همیشه در ارتفاع 0 است (بدون انیمیشن)
                if (viewData.visualCard != null && !isDragging)
                {
                    Vector3 visualPos = viewData.visualCard.localPosition;
                    visualPos.y = 0;
                    viewData.visualCard.localPosition = visualPos;
                }
            }

            // Update canvas sorting order based on Z position for proper depth ordering
            if (!isDragging)
            {
                UpdateCanvasSortingOrderBasedOnPosition();
            }
            
            // Update progress slider
            if(thisCard.TargetProcessTime > 0)
            {
                if (viewData.progressSlider != null)
                {
                    viewData.progressSlider.gameObject.SetActive(true);
                    viewData.progressSlider.value = Mathf.Clamp01(thisCard.ProcessTime / thisCard.TargetProcessTime);
                }
            }
            else
            {
                if (viewData.progressSlider != null)
                    viewData.progressSlider.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Updates canvas sorting order based on Z position - cards closer to camera (higher Z) appear in front
        /// </summary>
        private void UpdateCanvasSortingOrderBasedOnPosition()
        {
            if (viewData.worldCanvas == null) return;

            // Convert Z position to sorting order
            // Multiply by -sortingOrderMultiplier so that higher Z = higher sorting order (closer to camera)
            int sortingOrder = viewData.baseSortingOrder + Mathf.RoundToInt(-transform.position.z * viewData.sortingOrderMultiplier);
            
            //UpdateCanvasSortingOrder(sortingOrder);
            UpdateCanvasSortingOrder(sortingOrder);

        }

        /// <summary>
        /// Sets the sorting order for the canvas
        /// </summary>
        public void UpdateCanvasSortingOrder(int sortingOrder)
        {
            if (viewData.worldCanvas != null)
            {
                viewData.worldCanvas.sortingOrder = sortingOrder;
            }
        }

        /// <summary>
        /// Gets the mouse position in world coordinates on the game plane (Y = 0)
        /// </summary>
        private Vector3 GetMouseWorldPosition(Vector2 screenPosition)
        {
            if (mainCamera == null) return Vector3.zero;
            
            // Create a ray from camera through screen position
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            
            // Find intersection with Y=0 plane (ground plane for top-down view)
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            
            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }
            
            return transform.position;
        }
    }
}