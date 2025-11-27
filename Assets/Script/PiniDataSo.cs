using UnityEngine;

namespace Script
{
    [CreateAssetMenu(fileName = "PiniData", menuName = "FindingHome/PiniDataSo", order = 2)]
    public class PiniDataSo : CardDataSo
    {
        [Header("Pini Stats")]
        [Tooltip("Maximum health points")]
        public int maxHealth = 100;

        [Tooltip("Maximum food level")]
        public int maxFood = 5;

        [Tooltip("How much food decreases per second")]
        public float foodDecreaseRate = 0.1f;

        [Tooltip("How much health decreases per second when hungry")]
        public float healthDecreaseRate = 1f;

        [Tooltip("How much food is restored when eating")]
        public int foodPerMeal = 1;

        [Tooltip("Duration of eating process in seconds")]
        public float eatingDuration = 1f;

        [Header("Death")]
        [Tooltip("Card to spawn when Pini dies (e.g. bones, ghost, etc.)")]
        public CardDataSo deathDropCard;
    }
}

