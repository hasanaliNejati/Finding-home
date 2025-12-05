using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Script.View
{
    public class SellZone : MonoBehaviour
    {
        [Header("Settings")] [Tooltip("The card type that will be given in exchange for sold cards")] [SerializeField]
        private CardDataSo rewardCardType;

        [Tooltip("Random offset range for spawning reward cards")] [SerializeField]
        private float spawnOffsetRange = 1.5f;

        [Header("UI (Optional)")] [SerializeField]
        private Image iconImage;

        [SerializeField] private TextMeshProUGUI labelText;

        private void Start()
        {
            if (labelText != null)
            {
                labelText.text = rewardCardType != null ? $"Sell for {rewardCardType.type}" : "Sell Zone";
            }

            if (iconImage != null && rewardCardType != null && rewardCardType.sprite != null)
            {
                iconImage.sprite = rewardCardType.sprite;
            }
        }

        /// <summary>
        /// Called when a card is dropped on this sell zone
        /// </summary>
        public void SellCard(CardView cardView)
        {
            if (cardView == null || cardView.thisCard == null)
                return;

            if (rewardCardType == null)
            {
                Debug.LogError("[SellZone] Reward card type is not set!");
                return;
            }

            Card card = cardView.thisCard;

            // Get all cards in the stack (this card and all cards on top)
            List<Card> cardsToSell = card.GetAllTopCardsInGroup();

            // Calculate total value
            int totalValue = 0;
            foreach (var c in cardsToSell)
            {
                totalValue += c.Value;
            }

            if (totalValue <= 0)
            {
                Debug.Log("[SellZone] Card has no value, cannot sell.");
                return;
            }

            Vector3 spawnCenter = cardView.transform.position;

            // Remove all cards in the stack
            foreach (var c in cardsToSell)
            {
                GamePlayManager.Instance.RemoveCard(c.Id);
            }

            // Random offset for the group (X-Z plane for top-down view)
            Vector3 randomOffset = new Vector3(
                Random.Range(-spawnOffsetRange, spawnOffsetRange),
                0f,
                Random.Range(-spawnOffsetRange ,  -spawnOffsetRange/2)
            );
            Vector3 spawnPosition = spawnCenter + randomOffset;

            // Create reward cards equal to total value and group them together
            Card bottomCard = null;
            for (int i = 0; i < totalValue; i++)
            {
                var newCard = CardFactory.CreateCard(rewardCardType, 0);
                GamePlayManager.Instance.AddCard(newCard, spawnPosition,transform.position);

                // Group cards together
                if (bottomCard != null)
                {
                    bottomCard.AddToGroup(newCard);
                }

                bottomCard = newCard;
            }

            Debug.Log($"[SellZone] Sold {cardsToSell.Count} card(s) for {totalValue} {rewardCardType.type}(s)");
        }
    }
}