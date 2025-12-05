using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace Script
{
    [CreateAssetMenu(fileName = "AllCard", menuName = "FindingHome/AllCardSo", order = 2)]
    public class AllCardSo : ScriptableObject
    {
        [SerializeReference]
        public List<CardDataSo> allCardData = new List<CardDataSo>();
        
        public CardDataSo GetCardDataByType(string type)
        {
            foreach (var cardData in allCardData)
            {
                if (cardData.type == type)
                {
                    return cardData;
                }
            }

            return null;
        }
        
        public Combination GetCombinationByCreateBy(List<string> combination)
        {
            return FindCombinationMatch(combination, null)?.Item1;
        }
        
        public Combination GetCombinationByCreateBy(List<string> combination, List<Card> cards)
        {
            return FindCombinationMatch(combination, cards)?.Item1;
        }
        
        public CardDataSo GetCardDataByCreateBy(List<string> combination)
        {
            return GetCardDataByCreateByWithWeight(combination, null);
        }
        
        public CardDataSo GetCardDataByCreateBy(List<string> combination, List<Card> cards)
        {
            return GetCardDataByCreateByWithWeight(combination, cards);
        }
        
        CardDataSo GetCardDataByCreateByWithWeight(List<string> combination, List<Card> cards)
        {
            var matches = FindAllCombinationMatches(combination, cards);
            if (matches == null || matches.Count == 0)
            {
                Debug.Log("Null");
                return null;
            }
            
            if (matches.Count == 1)
            {
                return matches[0].Item2;
            }
            
            // Use weighted random selection
            return SelectWeightedRandom(matches);
        }
        
        (Combination, CardDataSo)? FindCombinationMatch(List<string> combination, List<Card> cards)
        {
            var matches = FindAllCombinationMatches(combination, cards);
            if (matches == null || matches.Count == 0)
            {
                return null;
            }
            
            // For backward compatibility, return first match
            return matches[0];
        }
        
        List<(Combination, CardDataSo)> FindAllCombinationMatches(List<string> combination, List<Card> cards)
        {
            var sortedCombination = combination.OrderBy(x => x).ToList();
            var matches = new List<(Combination, CardDataSo)>();

            foreach (var cardData in allCardData)
            {
                foreach (var createBy in cardData.CreateBy)
                {
                    if (MatchesCombination(createBy, sortedCombination, cards))
                    {
                        matches.Add((createBy, cardData));
                    }
                }
            }

            return matches;
        }
        
        CardDataSo SelectWeightedRandom(List<(Combination, CardDataSo)> matches)
        {
            // Calculate total weight
            int totalWeight = 0;
            foreach (var match in matches)
            {
                int weight = match.Item2.weight > 0 ? match.Item2.weight : 1;
                totalWeight += weight;
            }
            
            if (totalWeight <= 0)
            {
                // Fallback to equal probability if all weights are invalid
                return matches[Random.Range(0, matches.Count)].Item2;
            }
            
            // Generate random number between 0 and totalWeight
            int randomValue = Random.Range(0, totalWeight);
            
            // Select based on weighted probability
            int currentWeight = 0;
            foreach (var match in matches)
            {
                int weight = match.Item2.weight > 0 ? match.Item2.weight : 1;
                currentWeight += weight;
                
                if (randomValue < currentWeight)
                {
                    return match.Item2;
                }
            }
            
            // Fallback (should not reach here)
            return matches[matches.Count - 1].Item2;
        }
        
        bool MatchesCombination(Combination combination, List<string> sortedCardTypes, List<Card> cards)
        {
            // Build the required types/categories from combination parts
            List<string> requiredItems = new List<string>();
            Dictionary<string, CardCategory> categoryRequirements = new Dictionary<string, CardCategory>();
            
            foreach (var part in combination.Parts)
            {
                for (int i = 0; i < part.Count; i++)
                {
                    if (part.acceptAnyFromCategory && part.CardData != null)
                    {
                        // Use category identifier for category-based matching
                        string categoryKey = $"CATEGORY:{part.CardData.category}";
                        requiredItems.Add(categoryKey);
                        if (!categoryRequirements.ContainsKey(categoryKey))
                        {
                            categoryRequirements[categoryKey] = part.CardData.category;
                        }
                    }
                    else
                    {
                        // Use exact type for exact matching
                        requiredItems.Add(part.CardData.type);
                    }
                }
            }
            
            var sortedRequired = requiredItems.OrderBy(x => x).ToList();
            
            // If no category requirements, do simple type matching
            if (categoryRequirements.Count == 0)
            {
                return sortedRequired.SequenceEqual(sortedCardTypes);
            }
            
            // We have category requirements, need to match with cards
            if (cards == null || cards.Count != sortedCardTypes.Count)
            {
                // Can't do category matching without card objects
                return false;
            }
            
            // Build actual items from cards (types or category identifiers)
            List<string> actualItems = new List<string>();
            foreach (var card in cards)
            {
                // First check if exact type is in required items (for exact matches)
                if (sortedRequired.Contains(card.Type))
                {
                    actualItems.Add(card.Type);
                }
                // Otherwise, check if card's category matches any category requirement
                else if (card.Data != null)
                {
                    bool matchedCategory = false;
                    foreach (var kvp in categoryRequirements)
                    {
                        if (card.Data.category == kvp.Value)
                        {
                            actualItems.Add(kvp.Key);
                            matchedCategory = true;
                            break;
                        }
                    }
                    
                    if (!matchedCategory)
                    {
                        // Use exact type as fallback
                        actualItems.Add(card.Type);
                    }
                }
                else
                {
                    // Fallback to exact type
                    actualItems.Add(card.Type);
                }
            }
            
            var sortedActual = actualItems.OrderBy(x => x).ToList();
            return sortedRequired.SequenceEqual(sortedActual);
        }

    }
}