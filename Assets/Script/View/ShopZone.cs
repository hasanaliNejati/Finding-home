using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Script.View
{
    public class ShopZone : MonoBehaviour
    {
        [Header("Shop Settings")]
        [Tooltip("The card type that can be used as currency (e.g., Coin)")]
        [SerializeField] private CardDataSo currencyCardType;
        
        [Tooltip("The card that will be created when purchase is complete")]
        [SerializeField] private CardDataSo rewardCardType;
        
        [Tooltip("The cost to purchase the reward card")]
        [SerializeField] private int cost = 10;
        
        [Tooltip("Random offset range for spawning reward cards")]
        [SerializeField] private float spawnOffsetRange = 1.5f;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI counterText;
        [SerializeField] private Image rewardIcon;
        [SerializeField] private Slider progressSlider;

        private int currentProgress = 0;

        private void Start()
        {
            ResetProgress();
            UpdateUI();
        }

        /// <summary>
        /// Called when a card is dropped on this shop zone
        /// </summary>
        public bool TryPurchase(CardView cardView)
        {
            if (cardView == null || cardView.thisCard == null)
                return false;

            if (currencyCardType == null || rewardCardType == null)
            {
                Debug.LogError("[ShopZone] Currency or reward card type is not set!");
                return false;
            }

            Card card = cardView.thisCard;
            
            // Get all cards in the stack
            List<Card> cardsInStack = card.GetAllTopCardsInGroup();
            
            // Filter only currency cards and calculate total value
            List<Card> currencyCards = new List<Card>();
            int totalValue = 0;
            
            foreach (var c in cardsInStack)
            {
                if (c.Type == currencyCardType.type)
                {
                    currencyCards.Add(c);
                    totalValue += c.Value;
                }
            }

            // If no valid currency cards, reject
            if (currencyCards.Count == 0)
            {
                Debug.Log($"[ShopZone] No {currencyCardType.type} cards found in stack.");
                return false;
            }

            Vector3 spawnCenter = cardView.transform.position;

            // Remove currency cards from the game
            foreach (var c in currencyCards)
            {
                // Detach from group first
                DetachCard(c);
                GamePlayManager.Instance.RemoveCard(c.Id);
            }

            // Add to progress
            currentProgress += totalValue;
            
            Debug.Log($"[ShopZone] Added {totalValue} to progress. Current: {currentProgress}/{cost}");

            // Check if we've reached the cost
            while (currentProgress >= cost)
            {
                currentProgress -= cost;
                
                // Create reward card
                var newCard = CardFactory.CreateCard(rewardCardType, 0);
                
                // Random offset on X-Z plane for top-down view
                Vector3 randomOffset = new Vector3(
                    Random.Range(-spawnOffsetRange, spawnOffsetRange),
                    0f,
                    Random.Range(-spawnOffsetRange, spawnOffsetRange)
                );
                
                Vector3 spawnPosition = transform.position + randomOffset;
                GamePlayManager.Instance.AddCard(newCard, spawnPosition);
                
                Debug.Log($"[ShopZone] Purchased {rewardCardType.type}!");
            }

            UpdateUI();
            return true;
        }

        private void DetachCard(Card card)
        {
            if (card.TopCardId != 0)
            {
                var top = GamePlayManager.Instance.GetCardById(card.TopCardId);
                if (top != null)
                {
                    top.BottomCardId = card.BottomCardId;
                }
            }

            if (card.BottomCardId != 0)
            {
                var bottom = GamePlayManager.Instance.GetCardById(card.BottomCardId);
                if (bottom != null)
                {
                    bottom.TopCardId = card.TopCardId;
                }
            }

            card.TopCardId = 0;
            card.BottomCardId = 0;
        }

        private void ResetProgress()
        {
            currentProgress = 0;
        }

        private void UpdateUI()
        {
            int remaining = cost - currentProgress;
            
            if (counterText != null)
            {
                counterText.text = remaining.ToString();
            }

            if (progressSlider != null)
            {
                progressSlider.value = (float)currentProgress / cost;
            }

            if (rewardIcon != null && rewardCardType != null && rewardCardType.sprite != null)
            {
                rewardIcon.sprite = rewardCardType.sprite;
            }
        }

        // For debugging in editor
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                UpdateUI();
            }
        }
    }
}

