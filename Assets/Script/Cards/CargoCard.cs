using System.Collections.Generic;
using UnityEngine;

namespace Script
{
    [System.Serializable]
    public class CargoCard : Card
    {
        public List<Card> rewardCards = new List<Card>();
        public CargoCard(CargoDataSo cardData, int id) : base(cardData, id)
        {
            cardData = cardData as CargoDataSo;

            int totalCards = cardData.GetCargoCount();
            var items = cardData.cargoCardRatioItems;

            float totalWeight = 0;
            foreach (var item in items)
                totalWeight += item.ratio;

            for (int i = 0; i < totalCards; i++)
            {
                float randomValue = Random.Range(0f, totalWeight);
                float current = 0;

                foreach (var item in items)
                {
                    current += item.ratio;
                    if (randomValue <= current)
                    {
                        rewardCards.Add(new Card(item.cardData, -1));
                        break;
                    }
                }
            }
        }


        public Card PopCard()
        {
            if (rewardCards.Count > 0)
            {
                Card card = rewardCards[0];
                rewardCards.RemoveAt(0);
                return card;
            }

            return null;
        }

        public override bool Done()
        {
            return rewardCards.Count == 0;
        }
    }
}