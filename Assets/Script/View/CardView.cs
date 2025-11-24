using System;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Script.View
{
    public class CardView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private Image spriteRenderer;
        [SerializeField] private TextMeshProUGUI nameText;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas parentCanvas;

        private Vector2 startPosition;
        private Transform startParent;

        [SerializeReference] public Card thisCard;
        [SerializeField] public float speedLerp = 10f;
        [SerializeField] public Slider progressSlider;


        public Vector3 GroupOffset = new Vector3(0, -0.1f, -0.1f);


        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            parentCanvas = GetComponentInParent<Canvas>();
        }

        public void Init(Card card, CardDataSo cardDataSo)
        {
            thisCard = card;
            spriteRenderer.sprite = cardDataSo.sprite;
            nameText.text = cardDataSo.type;
            transform.position = card.Position;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("Clicked: " + nameText.text);
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
                rectTransform.position = worldPoint;
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
                var slot = hit.gameObject.GetComponent<CardView>();
                if (slot != null && slot != this && !topCards.Contains(slot))
                {
                    if(GamePlayManager.Instance.GetCardById(slot.thisCard.TopCardId) != null)
                        continue;
                    slot.thisCard.AddToGroup(thisCard);
                    break;
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
            if (thisCard == null)
                return;
            var bottom = GamePlayManager.Instance.GetCardById(thisCard.BottomCardId);
            if (bottom != null)
            {
                var bottomView = GamePlayManager.Instance.GetCardViewByCard(bottom);
                transform.position = Vector3.Lerp(
                    transform.position,
                    bottomView.transform.position + GroupOffset,
                    Time.deltaTime * speedLerp);
            }
            
            if(thisCard.TargetProcessTime > 0)
            {
                progressSlider.gameObject.SetActive(true);
                progressSlider.value = Mathf.Clamp01(thisCard.ProcessTime / thisCard.TargetProcessTime);
            }
            else
            {
                progressSlider.gameObject.SetActive(false);
            }
        }
    }
}