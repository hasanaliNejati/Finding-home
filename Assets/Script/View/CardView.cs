using System;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

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
        private Vector3 dragOffset; // Offset between card center and mouse click point
        private Vector3 dragTargetPosition; // Target position for smooth lerp movement
        private bool needsToReachTarget = false; // آیا کارت باید به dragTargetPosition برسد (حتی بعد از پایان درگ)
        private float normalVisualCardY = 0f; // ارتفاع عادی VisualCard
        protected float normalVisualCardZ = 0f; // زاویه عادی VisualCard (چرخش Z) - protected برای دسترسی از top cards
        private Tweener dragHeightTweener; // برای متوقف کردن انیمیشن ارتفاع در صورت نیاز

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
            
            // ذخیره ارتفاع و زاویه عادی VisualCard
            if (viewData.visualCard != null)
            {
                normalVisualCardY = viewData.visualCard.localPosition.y;
                normalVisualCardZ = viewData.visualCard.localEulerAngles.z;
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
            
            // Reset VisualCard Y و Z به 0
            if (viewData.visualCard != null)
            {
                Vector3 visualPos = viewData.visualCard.localPosition;
                visualPos.y = 0;
                viewData.visualCard.localPosition = visualPos;
                
                Vector3 visualRot = viewData.visualCard.localEulerAngles;
                visualRot.z = 0;
                viewData.visualCard.localEulerAngles = visualRot;
                
                // ذخیره ارتفاع و زاویه عادی
                normalVisualCardY = 0f;
                normalVisualCardZ = 0f;
            }
            
            // انیمیشن حرکت خطی (X, Z) - CardView خودش
            transform.DOMove(targetPosition, viewData.moveDuration)
                .SetEase(Ease.Linear);
            
            // انیمیشن پرش (Y از 0 به jumpHeight به 0) - VisualCard با حالت Bounce چندباره
            if (viewData.visualCard != null)
            {
                Sequence bounceSequence = DOTween.Sequence();
                
                float currentHeight = viewData.jumpHeight;
                float currentDuration = viewData.jumpDuration;
                
                
                // اجرای چندباره انیمیشن با کاهش ارتفاع و زمان
                for (int i = 0; i < viewData.bounceCount; i++)
                {
                    bounceSequence.Append(viewData.visualCard.DOPunchPosition(
                        new Vector3(0, currentHeight, 0),
                        currentDuration,
                        vibrato: viewData.vibrato,
                        elasticity: viewData.elasticity)
                        .SetEase(Ease.OutBounce));
                    
                    // کاهش ارتفاع و زمان برای bounce بعدی
                    currentHeight *= viewData.heightReduction;
                    currentDuration *= viewData.durationReduction;
                }
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
            
            // Initialize drag target position to current position for smooth start
            dragTargetPosition = transform.position;
            
            // Calculate offset between card position and mouse world position
            Vector3 mouseWorldPos = GetMouseWorldPosition(eventData.position);
            dragOffset = transform.position - mouseWorldPos;
            
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
            
            // بالا بردن VisualCard هنگام drag
            if (viewData.visualCard != null)
            {
                // متوقف کردن انیمیشن قبلی اگر وجود داشت
                if (dragHeightTweener != null && dragHeightTweener.IsActive())
                {
                    dragHeightTweener.Kill();
                }
                
                // ذخیره ارتفاع عادی
                normalVisualCardY = viewData.visualCard.localPosition.y;
                
                // بالا بردن VisualCard
                dragHeightTweener = viewData.visualCard.DOLocalMoveY(
                    viewData.dragHeight, 
                    viewData.dragHeightDuration)
                    .SetEase(Ease.OutQuad);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (mainCamera == null) return;

            // Convert screen point to world point on Y=0 plane
            Vector3 mouseWorldPos = GetMouseWorldPosition(eventData.position);
            
            // Apply the offset to maintain the same grab point
            Vector3 newPosition = mouseWorldPos + dragOffset;
            
            // Keep Y position fixed, only allow X and Z movement (top-down view)
            newPosition.y = transform.position.y;
            
            // Store target position for smooth lerp movement (will be applied in UpdateView)
            dragTargetPosition = newPosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            
            // کارت باید به dragTargetPosition برسد حتی بعد از پایان درگ
            needsToReachTarget = true;
            
            // Reset isDragging for top cards too
            var topViews = TopCardViews();
            foreach (var topView in topViews)
            {
                topView.isDragging = false;
                topView.needsToReachTarget = false; // top cards نیازی به رسیدن به dragTargetPosition ندارند
                
                // برگرداندن زاویه VisualCard top cards به حالت عادی
                if (topView.viewData.visualCard != null)
                {
                    Vector3 targetRot = topView.viewData.visualCard.localEulerAngles;
                    targetRot.z = topView.normalVisualCardZ;
                    topView.viewData.visualCard.DOLocalRotate(targetRot, viewData.dragHeightDownDuration)
                        .SetEase(Ease.InCubic);
                }
            }
            
            // Restore alpha and raycasts
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
            
            // برگرداندن VisualCard به ارتفاع و زاویه عادی
            if (viewData.visualCard != null)
            {
                // متوقف کردن انیمیشن قبلی اگر وجود داشت
                if (dragHeightTweener != null && dragHeightTweener.IsActive())
                {
                    dragHeightTweener.Kill();
                }
                
                // برگرداندن VisualCard به ارتفاع عادی (سریع‌تر)
                dragHeightTweener = viewData.visualCard.DOLocalMoveY(
                    normalVisualCardY, 
                    viewData.dragHeightDownDuration)
                    .SetEase(Ease.InCubic);
                
                // برگرداندن زاویه به حالت عادی (اما بعد از رسیدن به هدف)
                // این در UpdateView انجام می‌شود وقتی به dragTargetPosition رسید
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
            
            // اگر کارت باید به dragTargetPosition برسد (حتی بعد از پایان درگ)
            if (needsToReachTarget)
            {
                Vector3 direction = dragTargetPosition - transform.position;
                direction.y = 0; // فقط در صفحه XZ
                float distance = direction.magnitude;
                
                // اگر هنوز به هدف نرسیده، ادامه حرکت
                if (distance > 0.01f)
                {
                    transform.position = Vector3.Lerp(
                        transform.position,
                        dragTargetPosition,
                        Time.deltaTime * viewData.speedLerp);
                    
                    // چرخش VisualCard به سمت نقطه هدف (بر اساس فاصله)
                    if (viewData.visualCard != null)
                    {
                        if (distance > 0.01f)
                        {
                            float targetAngle = 0f;
                            
                            // اگر فاصله از threshold کمتر بود، زاویه 0 است
                            if (distance > viewData.rotationZeroThreshold)
                            {
                                // محاسبه زاویه در صفحه XZ (نسبت به محور Z مثبت) - معکوس شده
                                float baseAngle = -Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                                
                                // محاسبه ضریب بر اساس فاصله (از threshold تا maxDistance)
                                float normalizedDistance = Mathf.Clamp01(
                                    (distance - viewData.rotationZeroThreshold) / 
                                    (viewData.rotationMaxDistance - viewData.rotationZeroThreshold));
                                
                                // اعمال ضریب به زاویه (هر چه نزدیک‌تر، زاویه کمتر)
                                targetAngle = baseAngle * normalizedDistance;
                                
                                // محدود کردن زاویه به حداکثر مقدار تعریف شده
                                targetAngle = Mathf.Clamp(targetAngle, -viewData.maxDragRotationAngle, viewData.maxDragRotationAngle);
                            }
                            
                            // اعمال چرخش نرم با Lerp
                            float currentZ = viewData.visualCard.localEulerAngles.z;
                            // تبدیل به محدوده -180 تا 180 برای محاسبه صحیح
                            if (currentZ > 180f) currentZ -= 360f;
                            
                            float newZ = Mathf.LerpAngle(currentZ, targetAngle, Time.deltaTime * viewData.speedLerp);
                            Vector3 newRotation = viewData.visualCard.localEulerAngles;
                            newRotation.z = newZ;
                            viewData.visualCard.localEulerAngles = newRotation;
                        }
                    }
                }
                else
                {
                    // به هدف رسید، دیگر نیازی به ادامه حرکت نیست
                    needsToReachTarget = false;
                    transform.position = dragTargetPosition; // مطمئن شو که دقیقاً در موقعیت هدف است
                    thisCard.Position = transform.position;
                    
                    // برگرداندن زاویه به حالت عادی
                    if (viewData.visualCard != null)
                    {
                        Vector3 targetRot = viewData.visualCard.localEulerAngles;
                        targetRot.z = normalVisualCardZ;
                        viewData.visualCard.DOLocalRotate(targetRot, viewData.dragHeightDownDuration)
                            .SetEase(Ease.InCubic);
                    }
                }
            }
            
            // Smooth lerp movement when dragging
            if (isDragging)
            {
                // اگر این یک top card است که bottom card آن در حال درگ است، به سمت bottom card حرکت کن
                // در غیر این صورت، به dragTargetPosition حرکت کن (کارت اصلی)
                if (isTopCardBeingDragged)
                {
                    // top card باید به سمت bottom card حرکت کند
                    Vector3 targetPos = bottomView.transform.position + viewData.groupOffset;
                    transform.position = Vector3.Lerp(
                        transform.position,
                        targetPos,
                        Time.deltaTime * viewData.speedLerp);
                    
                    // چرخش VisualCard به سمت bottom card (بر اساس فاصله) - برای top cards
                    if (viewData.visualCard != null)
                    {
                        // محاسبه بردار جهت در صفحه XZ
                        Vector3 direction = targetPos - transform.position;
                        direction.y = 0; // فقط در صفحه XZ
                        float distance = direction.magnitude;
                        
                        // اگر فاصله کافی وجود دارد، زاویه را محاسبه کن
                        if (distance > 0.01f)
                        {
                            float targetAngle = 0f;
                            
                            // اگر فاصله از threshold کمتر بود، زاویه 0 است
                            if (distance > viewData.rotationZeroThreshold)
                            {
                                // محاسبه زاویه در صفحه XZ (نسبت به محور Z مثبت) - معکوس شده
                                float baseAngle = -Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                                
                                // محاسبه ضریب بر اساس فاصله (از threshold تا maxDistance)
                                float normalizedDistance = Mathf.Clamp01(
                                    (distance - viewData.rotationZeroThreshold) / 
                                    (viewData.rotationMaxDistance - viewData.rotationZeroThreshold));
                                
                                // اعمال ضریب به زاویه (هر چه نزدیک‌تر، زاویه کمتر)
                                targetAngle = baseAngle * normalizedDistance;
                                
                                // محدود کردن زاویه به حداکثر مقدار تعریف شده
                                targetAngle = Mathf.Clamp(targetAngle, -viewData.maxDragRotationAngle, viewData.maxDragRotationAngle);
                            }
                            
                            // اعمال چرخش نرم با Lerp
                            float currentZ = viewData.visualCard.localEulerAngles.z;
                            // تبدیل به محدوده -180 تا 180 برای محاسبه صحیح
                            if (currentZ > 180f) currentZ -= 360f;
                            
                            float newZ = Mathf.LerpAngle(currentZ, targetAngle, Time.deltaTime * viewData.speedLerp);
                            Vector3 newRotation = viewData.visualCard.localEulerAngles;
                            newRotation.z = newZ;
                            viewData.visualCard.localEulerAngles = newRotation;
                        }
                    }
                }
                else
                {
                    // کارت اصلی به dragTargetPosition حرکت می‌کند
                    transform.position = Vector3.Lerp(
                        transform.position,
                        dragTargetPosition,
                        Time.deltaTime * viewData.speedLerp);
                    
                    // چرخش VisualCard به سمت نقطه هدف (بر اساس فاصله) - فقط برای کارت اصلی
                    if (viewData.visualCard != null)
                    {
                        // محاسبه بردار جهت در صفحه XZ
                        Vector3 direction = dragTargetPosition - transform.position;
                        direction.y = 0; // فقط در صفحه XZ
                        float distance = direction.magnitude;
                        
                        // اگر فاصله کافی وجود دارد، زاویه را محاسبه کن
                        if (distance > 0.01f)
                        {
                            float targetAngle = 0f;
                            
                            // اگر فاصله از threshold کمتر بود، زاویه 0 است
                            if (distance > viewData.rotationZeroThreshold)
                            {
                                // محاسبه زاویه در صفحه XZ (نسبت به محور Z مثبت) - معکوس شده
                                float baseAngle = -Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                                
                                // محاسبه ضریب بر اساس فاصله (از threshold تا maxDistance)
                                float normalizedDistance = Mathf.Clamp01(
                                    (distance - viewData.rotationZeroThreshold) / 
                                    (viewData.rotationMaxDistance - viewData.rotationZeroThreshold));
                                
                                // اعمال ضریب به زاویه (هر چه نزدیک‌تر، زاویه کمتر)
                                targetAngle = baseAngle * normalizedDistance;
                                
                                // محدود کردن زاویه به حداکثر مقدار تعریف شده
                                targetAngle = Mathf.Clamp(targetAngle, -viewData.maxDragRotationAngle, viewData.maxDragRotationAngle);
                            }
                            
                            // اعمال چرخش نرم با Lerp
                            float currentZ = viewData.visualCard.localEulerAngles.z;
                            // تبدیل به محدوده -180 تا 180 برای محاسبه صحیح
                            if (currentZ > 180f) currentZ -= 360f;
                            
                            float newZ = Mathf.LerpAngle(currentZ, targetAngle, Time.deltaTime * viewData.speedLerp);
                            Vector3 newRotation = viewData.visualCard.localEulerAngles;
                            newRotation.z = newZ;
                            viewData.visualCard.localEulerAngles = newRotation;
                        }
                    }
                }
            }

            bool isFollowingBottom = false;
            // اگر bottom card وجود دارد و کارت در حال درگ نیست، به سمت bottom card حرکت کن
            if (bottom != null && !isDragging)
            {
                if (bottomView != null)
                {
                    transform.position = Vector3.Lerp(
                        transform.position,
                        bottomView.transform.position + viewData.groupOffset,
                        Time.deltaTime * viewData.speedLerp);
                    isFollowingBottom = true;
                    
                    // اگر bottom card در حال drag است، VisualCard را بالا ببر
                    if (bottomView.isDragging && viewData.visualCard != null && !isDragging)
                    {
                        float currentY = viewData.visualCard.localPosition.y;
                        if (Mathf.Abs(currentY - viewData.dragHeight) > 0.01f)
                        {
                            // متوقف کردن انیمیشن قبلی اگر وجود داشت
                            if (dragHeightTweener != null && dragHeightTweener.IsActive())
                            {
                                dragHeightTweener.Kill();
                            }
                            
                            // ذخیره ارتفاع عادی
                            normalVisualCardY = currentY;
                            
                            // بالا بردن VisualCard
                            dragHeightTweener = viewData.visualCard.DOLocalMoveY(
                                viewData.dragHeight, 
                                viewData.dragHeightDuration)
                                .SetEase(Ease.OutQuad);
                        }
                    }
                    // اگر bottom card drag نیست و VisualCard بالا است، برگردان
                    else if (!bottomView.isDragging && viewData.visualCard != null && !isDragging)
                    {
                        float currentY = viewData.visualCard.localPosition.y;
                        if (Mathf.Abs(currentY - normalVisualCardY) > 0.01f)
                        {
                            // متوقف کردن انیمیشن قبلی اگر وجود داشت
                            if (dragHeightTweener != null && dragHeightTweener.IsActive())
                            {
                                dragHeightTweener.Kill();
                            }
                            
                            // برگرداندن VisualCard به ارتفاع عادی (سریع‌تر)
                            dragHeightTweener = viewData.visualCard.DOLocalMoveY(
                                normalVisualCardY, 
                                viewData.dragHeightDownDuration)
                                .SetEase(Ease.InCubic);
                        }
                    }
                }
            }
            else
            {
                // اگر bottom نداریم و VisualCard بالا است، برگردان به ارتفاع عادی
                if (viewData.visualCard != null && !isDragging)
                {
                    float currentY = viewData.visualCard.localPosition.y;
                    if (Mathf.Abs(currentY - normalVisualCardY) > 0.01f)
                    {
                        // متوقف کردن انیمیشن قبلی اگر وجود داشت
                        if (dragHeightTweener != null && dragHeightTweener.IsActive())
                        {
                            dragHeightTweener.Kill();
                        }
                        
                        // برگرداندن VisualCard به ارتفاع عادی (سریع‌تر)
                        dragHeightTweener = viewData.visualCard.DOLocalMoveY(
                            normalVisualCardY, 
                            viewData.dragHeightDownDuration)
                            .SetEase(Ease.InCubic);
                    }
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