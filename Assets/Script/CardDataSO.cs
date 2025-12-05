using System.Collections.Generic;
using Script.View;
using UnityEngine;

namespace Script
{
    
    [CreateAssetMenu(fileName = "CardData", menuName = "FindingHome/CardDataSo", order = 1)]
    public class CardDataSo : ScriptableObject
    {
        public string type;
        public CardCategory category = CardCategory.None;
        public Sprite sprite;
        public CardView cardViewPrefab;
        public int value;
        public List<Combination> CreateBy = new List<Combination>();

        [Tooltip("How many successful combinations this card can participate in before it is destroyed. -1 means infinite uses.")]
        public int maxCombinationUses = 1;

        [Tooltip("Optional upgrade data that replaces this card after it participates in a combination.")]
        public CardDataSo upgradeTarget;

        [Tooltip("Optional card to spawn when this card is removed/destroyed after a combination.")]
        public CardDataSo removalCard;
    }
}