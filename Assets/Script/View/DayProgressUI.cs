using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Script.View
{
    public class DayProgressUI : MonoBehaviour
    {
        [SerializeField] private Image dayProgressSlider;
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private Button speed1xButton;
        [SerializeField] private Button speed2xButton;
        [SerializeField] private Button speed3xButton;
        [SerializeField] private Button pauseButton;

        private DayManager dayManager;

        private void Start()
        {
            dayManager = DayManager.Instance;
            
            if (dayManager == null)
            {
                Debug.LogError("DayManager instance not found!");
                return;
            }

            // Setup button listeners
            if (speed1xButton != null)
                speed1xButton.onClick.AddListener(() => dayManager.SetTimeSpeed1x());
            
            if (speed2xButton != null)
                speed2xButton.onClick.AddListener(() => dayManager.SetTimeSpeed2x());
            
            if (speed3xButton != null)
                speed3xButton.onClick.AddListener(() => dayManager.SetTimeSpeed3x());
            
            if (pauseButton != null)
                pauseButton.onClick.AddListener(() => dayManager.PauseTime());

            // Subscribe to day progress changes
            dayManager.OnDayProgressChanged += UpdateProgressSlider;
            dayManager.OnDayComplete += UpdateDayText;
            dayManager.OnTimeMultiplierChanged += UpdateSpeedText;

            // Initialize UI
            UpdateProgressSlider(dayManager.DayProgress);
            UpdateDayText(dayManager.CurrentDay);
            UpdateSpeedText(dayManager.TimeMultiplier);
        }

        private void Update()
        {
            if (dayManager != null)
            {
                UpdateProgressSlider(dayManager.DayProgress);
            }
        }

        private void UpdateProgressSlider(float progress)
        {
            if (dayProgressSlider != null)
            {
                dayProgressSlider.fillAmount = progress;
            }
        }

        private void UpdateDayText(int day)
        {
            if (dayText != null)
            {
                dayText.text = $"Day {day}";
            }
        }

        private void UpdateSpeedText(float multiplier)
        {
            if (speedText != null)
            {
                if (multiplier == 0f)
                {
                    speedText.text = "Paused";
                }
                else
                {
                    speedText.text = $"x{multiplier}";
                }
            }
        }

        private void OnDestroy()
        {
            if (dayManager != null)
            {
                dayManager.OnDayProgressChanged -= UpdateProgressSlider;
                dayManager.OnDayComplete -= UpdateDayText;
                dayManager.OnTimeMultiplierChanged -= UpdateSpeedText;
            }
        }
    }
}

