using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Script.View
{
    /// <summary>
    /// UI component that displays a list of all game cards and shows crafting requirements when clicked
    /// </summary>
    public class CardListUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AllCardSo allCardSo;
        [SerializeField] private Transform cardListContainer;
        [SerializeField] private CardListItem cardListItemPrefab;
        [SerializeField] private TextMeshProUGUI requirementsText;
        [SerializeField] private Button closeButton;
        
        [Header("UI Settings")]
        [Tooltip("Text to show when a card has no crafting requirements")]
        [SerializeField] private string noRequirementsText = "This card cannot be crafted";
        
        private List<CardDataSo> allCards = new List<CardDataSo>();
        private CardDataSo selectedCard = null;

        private void Start()
        {
            if (allCardSo == null)
            {
                Debug.LogError("[CardListUI] AllCardSo reference is missing!");
                return;
            }

            if (cardListContainer == null)
            {
                Debug.LogError("[CardListUI] Card list container is missing!");
                return;
            }

            if (cardListItemPrefab == null)
            {
                Debug.LogError("[CardListUI] Card list item prefab is missing!");
                return;
            }

            if (requirementsText == null)
            {
                Debug.LogError("[CardListUI] Requirements text is missing!");
                return;
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePanel);
            }

            LoadAllCards();
            PopulateCardList();
        }

        /// <summary>
        /// Load all cards from AllCardSo
        /// </summary>
        private void LoadAllCards()
        {
            if (allCardSo != null && allCardSo.allCardData != null)
            {
                allCards = new List<CardDataSo>(allCardSo.allCardData);
                Debug.Log($"[CardListUI] Loaded {allCards.Count} cards");
            }
        }

        /// <summary>
        /// Populate the UI list with all cards
        /// </summary>
        private void PopulateCardList()
        {
            // Clear existing items
            foreach (Transform child in cardListContainer)
            {
                Destroy(child.gameObject);
            }

            // Create UI items for each card
            foreach (var cardData in allCards)
            {
                if (cardData == null)
                    continue;

                CardListItem listItem = Instantiate(cardListItemPrefab, cardListContainer);
                SetupCardListItem(listItem, cardData);
            }
        }

        /// <summary>
        /// Setup a card list item with card data and click handler
        /// </summary>
        private void SetupCardListItem(CardListItem listItem, CardDataSo cardData)
        {
            // Initialize the list item
            listItem.Initialize(cardData, this);
        }

        /// <summary>
        /// Called when a card item is clicked in the list
        /// </summary>
        public void OnCardItemClicked(CardDataSo cardData)
        {
            selectedCard = cardData;
            UpdateRequirementsText(cardData);
        }

        /// <summary>
        /// Update the requirements text to show how the selected card is crafted
        /// </summary>
        private void UpdateRequirementsText(CardDataSo cardData)
        {
            if (requirementsText == null)
                return;

            if (cardData == null)
            {
                requirementsText.text = "";
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<b>{cardData.type}</b>");
            sb.AppendLine();

            if (cardData.CreateBy == null || cardData.CreateBy.Count == 0)
            {
                sb.AppendLine(noRequirementsText);
            }
            else
            {
                // Show all possible combinations
                for (int i = 0; i < cardData.CreateBy.Count; i++)
                {
                    var combination = cardData.CreateBy[i];
                    
                    if (i > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine("OR");
                        sb.AppendLine();
                    }

                    if (combination.Parts == null || combination.Parts.Count == 0)
                    {
                        sb.AppendLine("No requirements");
                    }
                    else
                    {
                        // Show time first
                        if (combination.duration > 0)
                        {
                            sb.AppendLine($"Time: {combination.duration} seconds");
                        }
                        
                        // Show each part on a new line
                        foreach (var part in combination.Parts)
                        {
                            if (part.CardData == null)
                                continue;

                            string partName = part.CardData.type;
                            
                            // If it accepts any from category, add category info
                            if (part.acceptAnyFromCategory && part.CardData.category != CardCategory.None)
                            {
                                partName = $"Any {GetCategoryName(part.CardData.category)}";
                            }

                            if (part.Count > 1)
                            {
                                sb.AppendLine($"{part.Count}x {partName}");
                            }
                            else
                            {
                                sb.AppendLine(partName);
                            }
                        }
                    }
                }
            }

            requirementsText.text = sb.ToString();
        }

        /// <summary>
        /// Get English name for card category
        /// </summary>
        private string GetCategoryName(CardCategory category)
        {
            switch (category)
            {
                case CardCategory.Food:
                    return "Food";
                case CardCategory.RawMaterial:
                    return "Raw Material";
                case CardCategory.CraftedMaterial:
                    return "Crafted Material";
                case CardCategory.HarvestingMaterial:
                    return "Harvesting Material";
                case CardCategory.Creature:
                    return "Creature";
                case CardCategory.Energy:
                    return "Energy";
                case CardCategory.Pollution:
                    return "Pollution";
                case CardCategory.Special:
                    return "Special";
                default:
                    return category.ToString();
            }
        }

        /// <summary>
        /// Close the panel
        /// </summary>
        private void ClosePanel()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Open the panel
        /// </summary>
        public void OpenPanel()
        {
            gameObject.SetActive(true);
            if (selectedCard != null)
            {
                UpdateRequirementsText(selectedCard);
            }
        }
    }
}

