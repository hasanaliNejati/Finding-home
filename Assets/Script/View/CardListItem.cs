using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Script.View
{
    /// <summary>
    /// UI component for a single card item in the card list
    /// </summary>
    public class CardListItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button button;
        [SerializeField] private Image cardImage;
        [SerializeField] private TextMeshProUGUI cardNameText;
        
        private CardDataSo cardData;
        private CardListUI parentListUI;

        private void Awake()
        {
            // Auto-find components if not assigned
            if (button == null)
                button = GetComponent<Button>();
            
            if (button == null)
                button = GetComponentInChildren<Button>();

            if (cardImage == null)
                cardImage = GetComponentInChildren<Image>();

            if (cardNameText == null)
                cardNameText = GetComponentInChildren<TextMeshProUGUI>();

            // Setup button click
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        /// <summary>
        /// Initialize this list item with card data
        /// </summary>
        public void Initialize(CardDataSo cardData, CardListUI parentListUI)
        {
            this.cardData = cardData;
            this.parentListUI = parentListUI;
            
            UpdateVisuals();
        }

        /// <summary>
        /// Update the visual elements of this list item
        /// </summary>
        private void UpdateVisuals()
        {
            if (cardData == null)
                return;

            // Set card name
            if (cardNameText != null)
            {
                cardNameText.text = cardData.type;
            }

            // Set card sprite
            if (cardImage != null && cardData.sprite != null)
            {
                cardImage.sprite = cardData.sprite;
            }
        }

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        private void OnButtonClicked()
        {
            if (cardData != null && parentListUI != null)
            {
                parentListUI.OnCardItemClicked(cardData);
            }
        }

        /// <summary>
        /// Get the card data associated with this item
        /// </summary>
        public CardDataSo GetCardData()
        {
            return cardData;
        }
    }
}




