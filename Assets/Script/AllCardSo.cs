using System.Collections.Generic;
using UnityEngine;

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
            return FindCombinationMatch(combination)?.Item1;
        }
        public CardDataSo GetCardDataByCreateBy(List<string> combination)
        {
            return FindCombinationMatch(combination)?.Item2;
        }
        
        (Combination,CardDataSo)? FindCombinationMatch(List<string> combination)
        {
            foreach (var cardData in allCardData)
            {
                foreach (var createBy in cardData.CreateBy)
                {
                    List<string> createByTypes = new List<string>();
                    foreach (var part in createBy.Parts)
                    {
                        for (int i = 0; i < part.Count; i++)
                        {
                            createByTypes.Add(part.CardData.type);    
                        }
                    }
                    
                    if (createByTypes.Count == combination.Count)
                    {
                        bool isMatch = true;
                        foreach (var type in combination)
                        {
                            if (!createByTypes.Contains(type))
                            {
                                isMatch = false;
                                break;
                            }
                        }

                        if (isMatch)
                        {
                            return (createBy, cardData);
                        }
                    }
                }
            }

            return null;
        }
    }
}