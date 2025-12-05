using System;
using System.Collections.Generic;
using System.Linq;
using Script.View;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.WSA;

namespace Script
{
    public class GamePlayManager : MonoBehaviour
    {
        public static GamePlayManager Instance { get; private set; }

        [SerializeReference] public Dictionary<int, Card> Cards = new Dictionary<int, Card>();
        [SerializeReference] public Dictionary<Card, CardView> CardViews = new Dictionary<Card, CardView>();
        [SerializeField] private int cardIdCounter = 1;

        [SerializeField] private AllCardSo allCardSo;
        [SerializeField] private Transform cardParentTransform;
        [SerializeField] private InitialCardsSo initialCardsSo;

        [Header("Game Stats")]
        [SerializeField] private int maxCardLimit = 20;
        [SerializeField] private int foodPerCreature = 2;
        [SerializeField] private int maxPollutionCount = 15;

        [Header("Card Discovery")]
        [SerializeReference] private HashSet<string> obtainedCardTypes = new HashSet<string>();

        public event Action OnGameOver;
        public event Action<string> OnCardObtained;

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

        private void Start()
        {
            // Spawn initial cards if configured
            if (initialCardsSo != null && initialCardsSo.initialCards != null)
            {
                SpawnInitialCards();
            }
        }

        /// <summary>
        /// Spawn all initial cards defined in InitialCardsSo
        /// </summary>
        private void SpawnInitialCards()
        {
            foreach (var entry in initialCardsSo.initialCards)
            {
                if (entry.cardData == null)
                {
                    Debug.LogWarning("Initial card entry has null cardData, skipping.");
                    continue;
                }

                var card = CardFactory.CreateCard(entry.cardData, cardIdCounter++);
                AddCard(card, entry.position,entry.position);
            }
        }

        public void AddCargoCard(Vector3 spawnPos)
        {
            allCardSo.allCardData.ForEach(cardDataSo =>
            {
                if (cardDataSo is CargoDataSo cargoDataSo)
                {
                    var cargoCard = CardFactory.CreateCard(cargoDataSo, cardIdCounter++);
                    Cards.Add(cargoCard.Id, cargoCard);
                    MarkCardAsObtained(cargoCard);
                    GenerateVisualCard(cargoCard, spawnPos);
                }
            });
            RefreshDeletedCards();
        }

        public void AddCard(string type,Vector3 spownPos)
        {
            var cardData = allCardSo.GetCardDataByType(type);
            if (cardData != null)
            {
                Card card = CardFactory.CreateCard(cardData, cardIdCounter++);
                Cards.Add(card.Id, card);
                MarkCardAsObtained(card);
                GenerateVisualCard(card,spownPos);
                RefreshDeletedCards();

            }
            else
            {
                Debug.LogError($"Card type {type} not found in AllCardSo.");
            }
        }

        public void AddCard(Card card, Vector3 pos,Vector3 spawnFromPos)
        {
            card.Position = pos;
            card.Id = cardIdCounter++;
            Cards.Add(card.Id, card);
            MarkCardAsObtained(card);
            GenerateVisualCard(card,spawnFromPos);
            RefreshDeletedCards();

        }

        public void RemoveCard(int cardId)
        {
            if (Cards.ContainsKey(cardId))
            {
                RemoveVisualCard(Cards[cardId]);
                Cards.Remove(cardId);
                
            }
            else
            {
                Debug.LogError($"Card with ID {cardId} not found.");
            }
        }


        
        public void GenerateVisualCard(Card card, Vector3? startPosition)
        {
            var cardData = card.Data ?? allCardSo.GetCardDataByType(card.Type);
            var cardView = Instantiate(cardData.cardViewPrefab, cardParentTransform);
            
            // اگر موقعیت شروع مشخص شده، کارت را در آن موقعیت قرار بده
            if (startPosition.HasValue)
            {
                cardView.transform.position = startPosition.Value;
            }
            
            cardView.Init(card, cardData);
            CardViews.Add(card, cardView);
        }
        public void RemoveVisualCard(Card card)
        {
            if (CardViews.ContainsKey(card))
            {
                var cardView = CardViews[card];
                Destroy(cardView.gameObject);
                CardViews.Remove(card);
            }
            else
            {
                Debug.LogError($"Visual representation for card {card.Id} not found.");
            }
        }

        public void RefreshDeletedCards()
        {
            var cardsToRemove = new List<int>();
            foreach (var card in Cards)
            {
                if (card.Value.Done())
                {
                    cardsToRemove.Add(card.Key);
                }
            }
            foreach (var cardId in cardsToRemove)
            {
                RemoveCard(cardId);
            }
        }

        public Card GetCardById(int id)
        {
            return Cards.GetValueOrDefault(id);
        }
        
        public CardView GetCardViewByCard(Card card)
        {
            return CardViews.GetValueOrDefault(card);
        }
        
        public int GetNextCardId()
        {
            return cardIdCounter++;
        }
        
        public Combination GetCombinationByCreateBy(List<string> combination)
        {
            Debug.Log("GetCombinationByCreateBy called");
            return allCardSo.GetCombinationByCreateBy(combination);
        }
        
        public Combination GetCombinationByCreateBy(List<string> combination, List<Card> cards)
        {
            return allCardSo.GetCombinationByCreateBy(combination, cards);
        }
        
        public CardDataSo GetCardDataByCreateBy(List<string> combination)
        {
            return allCardSo.GetCardDataByCreateBy(combination);
        }
        
        public CardDataSo GetCardDataByCreateBy(List<string> combination, List<Card> cards)
        {
            return allCardSo.GetCardDataByCreateBy(combination, cards);
        }


        private void Update()
        {
            foreach (var card in Cards.ToList())
            {
                card.Value.UpdateTick();
            }
        }

        public void RefreshVisualCard(Card card)
        {
            if (card == null)
                return;

            if (CardViews.TryGetValue(card, out var cardView))
            {
                var cardData = card.Data ?? allCardSo.GetCardDataByType(card.Type);
                cardView.Refresh(card, cardData);
            }
        }

        // Game Stats Methods
        public int GetCurrentCardCount()
        {
            return Cards.Count;
        }

        public int GetMaxCardLimit()
        {
            return maxCardLimit;
        }

        public int GetCurrentFoodCount()
        {
            int foodCount = 0;
            foreach (var card in Cards.Values)
            {
                if (card.Data?.category == CardCategory.Food)
                {
                    foodCount++;
                }
            }
            return foodCount;
        }

        public int GetRequiredFoodCount()
        {
            int creatureCount = 0;
            foreach (var card in Cards.Values)
            {
                if (card.Data?.category == CardCategory.Creature)
                {
                    creatureCount++;
                }
            }
            return creatureCount * foodPerCreature;
        }

        public float GetHealthStatus()
        {
            int pollutionCount = 0;
            foreach (var card in Cards.Values)
            {
                if (card.Data?.category == CardCategory.Pollution)
                {
                    pollutionCount++;
                }
            }
            if (maxPollutionCount <= 0)
                return 0f;
            
            return Mathf.Clamp01((float)pollutionCount / maxPollutionCount);
        }

        public int GetPollutionCount()
        {
            int pollutionCount = 0;
            foreach (var card in Cards.Values)
            {
                if (card.Data?.category == CardCategory.Pollution)
                {
                    pollutionCount++;
                }
            }
            return pollutionCount;
        }

        public int GetMaxPollutionCount()
        {
            return maxPollutionCount;
        }

        public int GetPenguinCount()
        {
            int penguinCount = 0;
            foreach (var card in Cards.Values)
            {
                if (card is PiniCard && card.Data?.category == CardCategory.Creature)
                {
                    penguinCount++;
                }
            }
            return penguinCount;
        }

        public bool AreAllPenguinsDead()
        {
            return GetPenguinCount() == 0;
        }

        public void ShowGameOverPanel()
        {
            OnGameOver?.Invoke();
        }

        public static Vector3 RandomVectorOffset()
        {
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-1.5f, 1.5f),
                0f,
                UnityEngine.Random.Range(-1.5f, 1.5f)
            );
            return randomOffset;
        }

        /// <summary>
        /// Mark a card type as obtained (even if it's later removed)
        /// </summary>
        private void MarkCardAsObtained(Card card)
        {
            Debug.Log("MarkCardAsObtained called");
            if (card != null && !string.IsNullOrEmpty(card.Type))
            {
                
                // Only trigger event if this is a new card type
                bool isNewCard = !obtainedCardTypes.Contains(card.Type);
                if (isNewCard)
                    Debug.Log(card.Type + " is obtained");
                obtainedCardTypes.Add(card.Type);
                
                if (isNewCard)
                {
                    OnCardObtained?.Invoke(card.Type);
                }
            }
        }

        /// <summary>
        /// Check if a card type has ever been obtained
        /// </summary>
        public bool HasObtainedCardType(string cardType)
        {
            return obtainedCardTypes.Contains(cardType);
        }

        /// <summary>
        /// Check if a card can be shown in the card list (all ingredients have been obtained)
        /// </summary>
        public bool CanShowCardInList(CardDataSo cardData)
        {
            if (cardData == null)
                return false;

            // If card has no crafting requirements, don't show it
            if (cardData.CreateBy == null || cardData.CreateBy.Count == 0)
                return false;

            // Check if at least one combination has all ingredients obtained
            foreach (var combination in cardData.CreateBy)
            {
                if (combination == null || combination.Parts == null || combination.Parts.Count == 0)
                {
                    // No requirements, don't show
                    continue;
                }

                bool allIngredientsObtained = true;
                foreach (var part in combination.Parts)
                {
                    if (part.CardData == null)
                        continue;

                    // Check if this ingredient has been obtained
                    if (!HasObtainedCardType(part.CardData.type))
                    {
                        allIngredientsObtained = false;
                        break;
                    }
                }

                // If all ingredients for this combination are obtained, can show the card
                if (allIngredientsObtained)
                    return true;
            }

            // No combination has all ingredients obtained
            return false;
        }
    }
}