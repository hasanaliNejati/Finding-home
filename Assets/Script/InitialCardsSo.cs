using System.Collections.Generic;
using UnityEngine;

namespace Script
{
    [CreateAssetMenu(fileName = "InitialCards", menuName = "FindingHome/InitialCardsSo", order = 4)]
    public class InitialCardsSo : ScriptableObject
    {
        [System.Serializable]
        public class InitialCardEntry
        {
            [Tooltip("Card data to spawn (drag CardDataSo here)")]
            public CardDataSo cardData;
            
            [Tooltip("Position where the card should spawn")]
            public Vector3 position;
        }

        [Tooltip("List of cards to spawn at the start of the game")]
        public List<InitialCardEntry> initialCards = new List<InitialCardEntry>();
    }
}

