using System;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Script.View
{
    [RequireComponent(typeof(CardViewData))]
    public class CardView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        // Data component that holds all serialized fields
        protected CardViewData viewData;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas parentCanvas;

        private Vector2 startPosition;
        private Transform startParent;

        [SerializeReference] public Card thisCard;


        private void Awake()
        {
            // Get the data component - this will always exist due to RequireComponent
            viewData = GetComponent<CardViewData>();
            
            rectTransform = GetComponent<RectTransform>();

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            parentCanvas = GetComponentInParent<Canvas>();
        }

        public virtual void Init(Card card, CardDataSo cardDataSo)
        {
            thisCard = card;
            ApplyData(cardDataSo);
            transform.position = card.Position;
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
            startPosition = rectTransform.position; 
            startParent = transform.parent;
            
            Card bottomCard = GamePlayManager.Instance.GetCardById(thisCard.BottomCardId);
            if (bottomCard != null)
            {
                GamePlayManager.Instance.GetCardViewByCard(bottomCard).thisCard.RemoveFromGroup(thisCard);
            }

            transform.SetAsLastSibling();
            foreach (var tops in TopCardViews())
            {
                tops.transform.SetAsLastSibling();
            }
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.8f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    rectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var worldPoint))
            {
                // Keep Y position fixed, only allow X and Z movement (top-down view)
                Vector3 currentPosition = rectTransform.position;
                rectTransform.position = new Vector3(worldPoint.x, currentPosition.y, worldPoint.z);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
            thisCard.Position = transform.position;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                eventData.pressEventCamera,
                rectTransform.position);

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
            {
                position = screenPoint
            }, results);

            var topCards = TopCardViews();
            foreach (var hit in results)
            {
                // Check for SellZone first
                var sellZone = hit.gameObject.GetComponent<SellZone>();
                if (sellZone != null)
                {
                    sellZone.SellCard(this);
                    return; // Card is sold, exit early
                }
                
                // Check for ShopZone
                var shopZone = hit.gameObject.GetComponent<ShopZone>();
                if (shopZone != null)
                {
                    if (shopZone.TryPurchase(this))
                    {
                        return; // Purchase processed, exit early
                    }
                    // If purchase failed (wrong card type), continue checking other targets
                }
                
                var slot = hit.gameObject.GetComponent<CardView>();
                if (slot != null && slot != this && !topCards.Contains(slot))
                {
                    if(GamePlayManager.Instance.GetCardById(slot.thisCard.TopCardId) != null)
                        continue;
                    
                    // Try to add to group - may fail if Pini restrictions apply
                    if (slot.thisCard.AddToGroup(thisCard))
                    {
                        break; // Successfully merged
                    }
                    // If merge failed, continue checking other slots
                }
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
            if (bottom != null)
            {
                var bottomView = GamePlayManager.Instance.GetCardViewByCard(bottom);
                transform.position = Vector3.Lerp(
                    transform.position,
                    bottomView.transform.position + viewData.groupOffset,
                    Time.deltaTime * viewData.speedLerp);
            }
            
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
    }
}