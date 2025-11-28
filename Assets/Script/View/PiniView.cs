using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Script.View
{
    public class PiniView : CardView
    {
        [Header("Pini UI - Health")]
        [SerializeField] private Image healthFillImage;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Color healthColor = new Color(0.2f, 0.8f, 0.2f); // Green

        [Header("Pini UI - Food")]
        [SerializeField] private Image foodFillImage;
        [SerializeField] private TextMeshProUGUI foodText;
        [SerializeField] private Color normalFoodColor = new Color(1f, 0.7f, 0.2f); // Orange
        [SerializeField] private Color hungryFoodColor = new Color(1f, 0.2f, 0.2f); // Red

        [Header("Hunger Visual")]
        [SerializeField] private Image cardBackground;
        [SerializeField] private Color normalBackgroundColor = Color.white;
        [SerializeField] private Color hungryBackgroundColor = new Color(1f, 0.8f, 0.8f); // Light red

        private PiniCard piniCard;

        public override void Init(Card card, CardDataSo cardDataSo)
        {
            piniCard = card as PiniCard;
            base.Init(card, cardDataSo);
            
            // Set health bar color
            if (healthFillImage != null)
                healthFillImage.color = healthColor;
                
            UpdatePiniUI();
        }

        public override void Refresh(Card card, CardDataSo cardDataSo)
        {
            piniCard = card as PiniCard;
            base.Refresh(card, cardDataSo);
            UpdatePiniUI();
        }

        protected override void UpdateView()
        {
            base.UpdateView();
            
            if (piniCard == null)
                return;

            UpdatePiniUI();
        }

        private void UpdatePiniUI()
        {
            if (piniCard == null)
                return;

            // Update health fill
            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = piniCard.CurrentHealth / piniCard.MaxHealth;
            }
            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(piniCard.CurrentHealth)}/{piniCard.MaxHealth}";
            }

            // Update food fill
            if (foodFillImage != null)
            {
                foodFillImage.fillAmount = piniCard.CurrentFood / piniCard.MaxFood;
            }
            if (foodText != null)
            {
                foodText.text = $"{Mathf.CeilToInt(piniCard.CurrentFood)}/{piniCard.MaxFood}";
            }

            // Update visual based on hunger state
            UpdateHungerVisual();

            // Show eating progress
            if (piniCard.IsEating && viewData.progressSlider != null)
            {
                viewData.progressSlider.gameObject.SetActive(true);
                viewData.progressSlider.value = piniCard.EatingProgress / piniCard.EatingDuration;
            }
        }

        private void UpdateHungerVisual()
        {
            bool isHungry = piniCard.IsHungry;

            // Change food bar color based on hunger
            if (foodFillImage != null)
            {
                foodFillImage.color = isHungry ? hungryFoodColor : normalFoodColor;
            }

            // Change background color
            if (cardBackground != null)
            {
                cardBackground.color = isHungry ? hungryBackgroundColor : normalBackgroundColor;
            }
        }
    }
}

