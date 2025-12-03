using System.Collections.Generic;
using UnityEngine;

namespace Script
{
    [CreateAssetMenu(fileName = "Mine", menuName = "FindingHome/MineDataSo", order = 3)]
    public class MineDataSo : CardDataSo
    {
        [Header("Mining Settings")]
        [Tooltip("Time in seconds between each card generation")]
        public float miningInterval = 5f;
        
        [Tooltip("List of possible cards to generate with their ratios")]
        public List<CargoCardRatioItem> mineCardRatioItems = new List<CargoCardRatioItem>();
    }
}


