using System.Collections.Generic;
using UnityEngine;

namespace Script
{
    [CreateAssetMenu(fileName = "Cargo", menuName = "FindingHome/CargoSo", order = 3)]
    public class CargoDataSo : CardDataSo
    {
        public int minimumCargoCount = 1;
        public int maximumCargoCount = 5;
        public List<CargoCardRatioItem> cargoCardRatioItems = new List<CargoCardRatioItem>();
        
        public int GetCargoCount()
        {
            return Random.Range(minimumCargoCount, maximumCargoCount + 1);
        }
    }
}