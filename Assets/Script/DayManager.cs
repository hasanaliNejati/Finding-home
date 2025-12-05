using System;
using System.Collections.Generic;
using UnityEngine;

namespace Script
{
    public class DayManager : MonoBehaviour
    {
        public static DayManager Instance { get; private set; }

        [SerializeField] private float dayDuration = 60f; // Duration of one day in seconds
        [SerializeField] private float timeMultiplier = 1f; // 1x, 2x, 3x, or 0 for pause
        
        private float currentDayTime = 0f;
        private int currentDay = 1;

        public float DayDuration => dayDuration;
        public float CurrentDayTime => currentDayTime;
        public float DayProgress => currentDayTime / dayDuration; // 0 to 1
        public int CurrentDay => currentDay;
        public float TimeMultiplier => timeMultiplier;

        public event Action<int> OnDayComplete;
        public event Action<float> OnDayProgressChanged;
        public event Action<float> OnTimeMultiplierChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Update day time based on multiplier (not using Time.timeScale)
            if (timeMultiplier > 0)
            {
                currentDayTime += Time.deltaTime * timeMultiplier;
                
                // Check if day is complete
                if (currentDayTime >= dayDuration)
                {
                    currentDayTime = 0f;
                    currentDay++;
                    DistributeFoodToPenguins();
                    OnDayComplete?.Invoke(currentDay);
                }
                
                OnDayProgressChanged?.Invoke(DayProgress);
            }
        }

        public void SetTimeMultiplier(float multiplier)
        {
            float newMultiplier = Mathf.Clamp(multiplier, 0f, 3f);
            if (timeMultiplier != newMultiplier)
            {
                timeMultiplier = newMultiplier;
                OnTimeMultiplierChanged?.Invoke(timeMultiplier);
            }
        }

        public void PauseTime()
        {
            if (timeMultiplier != 0f)
            {
                timeMultiplier = 0f;
                OnTimeMultiplierChanged?.Invoke(timeMultiplier);
            }
        }

        public void ResumeTime()
        {
            if (timeMultiplier == 0f)
            {
                timeMultiplier = 1f;
                OnTimeMultiplierChanged?.Invoke(timeMultiplier);
            }
        }

        public void SetTimeSpeed1x()
        {
            if (timeMultiplier != 1f)
            {
                timeMultiplier = 1f;
                OnTimeMultiplierChanged?.Invoke(timeMultiplier);
            }
        }

        public void SetTimeSpeed2x()
        {
            if (timeMultiplier != 2f)
            {
                timeMultiplier = 2f;
                OnTimeMultiplierChanged?.Invoke(timeMultiplier);
            }
        }

        public void SetTimeSpeed3x()
        {
            if (timeMultiplier != 3f)
            {
                timeMultiplier = 3f;
                OnTimeMultiplierChanged?.Invoke(timeMultiplier);
            }
        }

        public void ResetDay()
        {
            currentDayTime = 0f;
            OnDayProgressChanged?.Invoke(0f);
        }

        /// <summary>
        /// Distribute food to penguins at the end of each day
        /// Each penguin needs 2 food. If not enough food, penguin dies.
        /// </summary>
        private void DistributeFoodToPenguins()
        {
            if (GamePlayManager.Instance == null)
                return;

            // Get all penguins (creatures)
            List<PiniCard> penguins = new List<PiniCard>();
            foreach (var card in GamePlayManager.Instance.Cards.Values)
            {
                if (card is PiniCard pini && card.Data?.category == CardCategory.Creature)
                {
                    penguins.Add(pini);
                }
            }

            // Get all food cards (create a copy of IDs to avoid modification during iteration)
            List<int> foodCardIds = new List<int>();
            foreach (var card in GamePlayManager.Instance.Cards.Values)
            {
                if (card.Data?.category == CardCategory.Food)
                {
                    foodCardIds.Add(card.Id);
                }
            }

            // Distribute food to each penguin (each needs 2 food)
            const int foodPerPenguin = 2;
            int foodIndex = 0;

            foreach (var penguin in penguins)
            {
                int foodGiven = 0;
                
                // Try to give 2 food to this penguin
                while (foodGiven < foodPerPenguin && foodIndex < foodCardIds.Count)
                {
                    int foodCardId = foodCardIds[foodIndex];
                    var foodCard = GamePlayManager.Instance.GetCardById(foodCardId);
                    
                    // Check if food card still exists (might have been removed)
                    if (foodCard == null)
                    {
                        foodIndex++;
                        continue;
                    }
                    
                    // Consume the food card
                    foodCard.CombinationUses++;
                    if (foodCard.Data.maxCombinationUses >= 0 && 
                        foodCard.CombinationUses >= foodCard.Data.maxCombinationUses)
                    {
                        GamePlayManager.Instance.RemoveCard(foodCard.Id);
                    }
                    else
                    {
                        GamePlayManager.Instance.RefreshVisualCard(foodCard);
                    }
                    
                    foodGiven++;
                    foodIndex++;
                }

                // If penguin didn't get enough food, it dies
                if (foodGiven < foodPerPenguin)
                {
                    penguin.Die();
                }
            }

            // Check if all penguins are dead (game over)
            if (GamePlayManager.Instance != null && GamePlayManager.Instance.AreAllPenguinsDead())
            {
                GamePlayManager.Instance.ShowGameOverPanel();
            }
        }
    }
}

