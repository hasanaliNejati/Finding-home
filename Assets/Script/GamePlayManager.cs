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

        public void AddCargoCard()
        {
            allCardSo.allCardData.ForEach(cardDataSo =>
            {
                if (cardDataSo is CargoDataSo cargoDataSo)
                {
                    CargoCard cargoCard = new CargoCard(cargoDataSo, cardIdCounter++);
                    Cards.Add(cargoCard.Id, cargoCard);
                    GenerateVisualCard(cargoCard);
                }
            });
            RefreshDeletedCards();
        }

        public void AddCard(string type)
        {
            var cardData = allCardSo.GetCardDataByType(type);
            if (cardData != null)
            {
                Card card = new Card(cardData, cardIdCounter++);
                Cards.Add(card.Id, card);
                GenerateVisualCard(card);
                RefreshDeletedCards();

            }
            else
            {
                Debug.LogError($"Card type {type} not found in AllCardSo.");
            }
        }

        public void AddCard(Card card, Vector3 pos)
        {
            card.Position = pos;
            card.Id = cardIdCounter++;
            Cards.Add(card.Id, card);
            GenerateVisualCard(card);
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

        public void GenerateVisualCard(Card card)
        {
            var cardData = allCardSo.GetCardDataByType(card.Type);
            var cardView = Instantiate(cardData.cardViewPrefab, cardParentTransform);
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
        
        public Combination GetCombinationByCreateBy(List<string> combination)
        {
            Debug.Log("GetCombinationByCreateBy called");
            return allCardSo.GetCombinationByCreateBy(combination);
        }
        public CardDataSo GetCardDataByCreateBy(List<string> combination)
        {
            return allCardSo.GetCardDataByCreateBy(combination);
        }


        private void Update()
        {
            foreach (var card in Cards.ToList())
            {
                card.Value.UpdateTick();
            }
        }
    }
}