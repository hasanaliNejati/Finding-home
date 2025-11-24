using System.Collections.Generic;
using Script.View;
using UnityEngine;

namespace Script
{
    
    [CreateAssetMenu(fileName = "CardData", menuName = "FindingHome/CardDataSo", order = 1)]
    public class CardDataSo : ScriptableObject
    {
        public string type;
        public Sprite sprite;
        public CardView cardViewPrefab;
        public List<Combination> CreateBy = new List<Combination>();
        

    }
}