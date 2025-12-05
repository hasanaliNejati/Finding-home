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

            // Hide food and health UI elements since we removed the hunger system
            if (healthFillImage != null)
                healthFillImage.gameObject.SetActive(false);
            if (healthText != null)
                healthText.gameObject.SetActive(false);
            if (foodFillImage != null)
                foodFillImage.gameObject.SetActive(false);
            if (foodText != null)
                foodText.gameObject.SetActive(false);
        }
    }
}

