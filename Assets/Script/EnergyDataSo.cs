using UnityEngine;

namespace Script
{
    [CreateAssetMenu(fileName = "EnergyData", menuName = "FindingHome/EnergyDataSo", order = 2)]
    public class EnergyDataSo : CardDataSo
    {
        [Header("Energy Settings")]
        [Tooltip("Card to spawn when this energy is used (typically Pollution card)")]
        public CardDataSo pollutionCard;
    }
}


