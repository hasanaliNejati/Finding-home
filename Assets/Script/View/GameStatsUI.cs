using UnityEngine;
using TMPro;

namespace Script.View
{
    public class GameStatsUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI cardLimitText;
        [SerializeField] private TextMeshProUGUI foodRequirementText;
        [SerializeField] private TextMeshProUGUI healthStatusText;

        private GamePlayManager gamePlayManager;

        private void Start()
        {
            gamePlayManager = GamePlayManager.Instance;
            
            if (gamePlayManager == null)
            {
                Debug.LogError("GamePlayManager instance not found!");
                return;
            }
        }

        private void Update()
        {
            if (gamePlayManager != null)
            {
                UpdateStats();
            }
        }

        private void UpdateStats()
        {
            // Update card limit text
            if (cardLimitText != null)
            {
                int currentCount = gamePlayManager.GetCurrentCardCount();
                int maxLimit = gamePlayManager.GetMaxCardLimit();
                cardLimitText.text = $"Cards: {currentCount}/{maxLimit}";
            }

            // Update food requirement text
            if (foodRequirementText != null)
            {
                int currentFood = gamePlayManager.GetCurrentFoodCount();
                int requiredFood = gamePlayManager.GetRequiredFoodCount();
                foodRequirementText.text = $"Food: {currentFood}/{requiredFood}";
            }

            // Update health status text
            if (healthStatusText != null)
            {
                float healthStatus = gamePlayManager.GetHealthStatus();
                int pollutionCount = gamePlayManager.GetPollutionCount();
                int maxPollution = gamePlayManager.GetMaxPollutionCount();
                healthStatusText.text = $"Health: {pollutionCount}/{maxPollution} ({(healthStatus * 100f):F0}%)";
            }
        }
    }
}

