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
        
        // Separation force velocity
        private Vector3 separationVelocity = Vector3.zero;

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
                        // Reset separation velocity for both cards when merging
                        // This prevents sudden movement when adding a card to a group
                        this.separationVelocity = Vector3.zero;
                        slot.separationVelocity = Vector3.zero;
                        
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
                    // Reset velocity when following bottom card (top cards don't have their own velocity)
                    separationVelocity = Vector3.zero;
                }
            }
            else
            {
                // Apply separation force for:
                // 1. Independent cards (not stacked, not dragging)
                // 2. Bottom cards of a stack (they move the whole group)
                bool isBottomCard = (bottom == null && thisCard.TopCardId != 0);
                bool isIndependentCard = (bottom == null && thisCard.TopCardId == 0);
                
                if (!isDragging && (isBottomCard || isIndependentCard))
                {
                    ApplySeparationForce();
                }
                
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

            // Use shared utility function to calculate sorting order based on Z position
            // استفاده از baseSortingOrder = 0 و multiplier مشترک برای یکسان بودن با دریا
            int sortingOrder = SortingOrderUtility.GetSortingOrderFromZ(
                transform.position.z,
                0, // baseSortingOrder را 0 قرار می‌دهیم تا با دریا یکسان باشد
                SortingOrderUtility.DefaultSortingOrderMultiplier // استفاده از multiplier مشترک
            );
            
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
        
        /// <summary>
        /// Applies separation force to push this card away from nearby cards
        /// Only applies force when cards are overlapping, stops movement when no overlap exists
        /// </summary>
        private void ApplySeparationForce()
        {
            if (GamePlayManager.Instance == null || viewData == null)
                return;
            
            Vector3 separationForceVector = Vector3.zero;
            Vector3 currentPos = transform.position;
            float cardRadius = viewData.cardRadius;
            float separationForce = viewData.separationForce;
            bool hasOverlap = false;
            
            // Check all other cards for overlap
            foreach (var cardEntry in GamePlayManager.Instance.Cards)
            {
                Card otherCard = cardEntry.Value;
                
                // Skip self
                if (otherCard.Id == thisCard.Id)
                    continue;
                
                // Skip cards that are in the same stack (top/bottom of this card)
                if (otherCard.Id == thisCard.BottomCardId || otherCard.Id == thisCard.TopCardId)
                    continue;
                
                // Skip cards that are being dragged
                var otherView = GamePlayManager.Instance.GetCardViewByCard(otherCard);
                if (otherView != null && otherView.isDragging)
                    continue;
                
                // For stacked cards, check against the bottom card of the other stack
                Card otherBottomCard = otherCard;
                CardView otherBottomView = otherView;
                
                // If other card is a top card, get its bottom card for position check
                if (otherCard.BottomCardId != 0)
                {
                    var tempBottom = GamePlayManager.Instance.GetCardById(otherCard.BottomCardId);
                    if (tempBottom != null)
                    {
                        otherBottomCard = tempBottom;
                        otherBottomView = GamePlayManager.Instance.GetCardViewByCard(tempBottom);
                    }
                }
                
                // Skip if checking against same bottom card (same stack)
                if (otherBottomCard.Id == thisCard.Id)
                    continue;
                
                // If this card is a top card, check against other card's bottom position
                if (thisCard.BottomCardId != 0)
                {
                    var thisBottom = GamePlayManager.Instance.GetCardById(thisCard.BottomCardId);
                    if (thisBottom != null && thisBottom.Id == otherBottomCard.Id)
                        continue; // Same stack
                }
                
                // Use bottom card's position for stacked cards
                Vector3 otherPos = (Vector3)otherBottomCard.Position;
                // Keep Y position same (only work on X-Z plane)
                otherPos.y = currentPos.y;
                
                // Calculate distance on X-Z plane (ignore Y)
                Vector3 diff = currentPos - otherPos;
                diff.y = 0; // Only work on X-Z plane
                float distance = diff.magnitude;
                
                // Calculate combined radius
                // Use bottom card's radius for stacked cards
                float otherRadius = otherBottomView != null && otherBottomView.viewData != null 
                    ? otherBottomView.viewData.cardRadius 
                    : cardRadius;
                float minDistance = cardRadius + otherRadius;
                
                // If cards are overlapping or too close
                if (distance < minDistance && distance > 0.001f)
                {
                    hasOverlap = true;
                    
                    // Calculate separation direction (normalized)
                    Vector3 direction = diff.normalized;
                    
                    // Calculate force strength (stronger when closer)
                    float overlapAmount = minDistance - distance;
                    float forceStrength = (overlapAmount / minDistance) * separationForce;
                    
                    // Add to separation force vector
                    separationForceVector += direction * forceStrength;
                }
            }
            
            // Only apply force if there's overlap
            if (hasOverlap)
            {
                // Apply force to velocity
                separationVelocity += separationForceVector * Time.deltaTime;
                
                // Clamp velocity to max speed
                if (separationVelocity.magnitude > viewData.maxSpeed)
                {
                    separationVelocity = separationVelocity.normalized * viewData.maxSpeed;
                }
            }
            else
            {
                // No overlap - apply damping to slow down and stop
                separationVelocity *= viewData.damping;
            }
            
            // Apply velocity to position
            if (separationVelocity.magnitude > 0.001f)
            {
                Vector3 newPosition = currentPos + separationVelocity * Time.deltaTime;
                // Keep Y position constant
                newPosition.y = currentPos.y;
                transform.position = newPosition;
                
                // Update card position data
                thisCard.Position = newPosition;
            }
            else
            {
                // Reset velocity when very small or no overlap
                separationVelocity = Vector3.zero;
            }
        }
        
        /// <summary>
        /// Draws gizmos to visualize card radius for debugging
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (viewData == null)
                return;
            
            // Draw card radius as a circle on X-Z plane
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position;
            float radius = viewData.cardRadius;
            
            // Draw circle on X-Z plane (top-down view)
            int segments = 32;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = (float)i / segments * 360f * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
            
            // Draw center point
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(center, 0.1f);
        }
        
        /// <summary>
        /// Draws gizmos for all cards (not just selected)
        /// </summary>
        private void OnDrawGizmos()
        {
            if (viewData == null)
                return;
            
            // Draw a subtle circle for all cards
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Semi-transparent yellow
            Vector3 center = transform.position;
            float radius = viewData.cardRadius;
            
            // Draw circle on X-Z plane
            int segments = 32;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = (float)i / segments * 360f * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
    }
}