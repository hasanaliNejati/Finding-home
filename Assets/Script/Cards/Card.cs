using System.Collections.Generic;
using NUnit.Framework;
using Script;
using UnityEngine;

[System.Serializable]
public class Card
{
    public Card(CardDataSo cardDataSo, int id)
    {
        Id = id;
        ApplyData(cardDataSo);
    }

    public CardDataSo Data { get; private set; }
    public string Type;
    public int Id;
    public int TopCardId;
    public int BottomCardId;
    public int CombinationUses;
    public int Value;

    public Vec3 Position;

    public float ProcessTime;
    public float TargetProcessTime;

    public virtual bool Done()
    {
        return false;
    }

    public bool AddToGroup(Card topCard)
    {
        // Check if this card (bottom) is a PiniCard and can merge with topCard
        if (this is PiniCard pini)
        {
            if (!pini.CanMergeWith(topCard))
                return false;
        }

        // Check if topCard is a hungry PiniCard - can only merge with food
        if (topCard is PiniCard topPini)
        {
            if (!topPini.CanBePlacedOn(this))
                return false;
        }

        // Check if topCard is being placed on a PiniCard
        // Also check the entire stack for any PiniCard that might reject this merge
        var currentCard = this;
        while (currentCard != null)
        {
            if (currentCard is PiniCard piniInStack && !piniInStack.CanMergeWith(topCard))
                return false;
            currentCard = GamePlayManager.Instance.GetCardById(currentCard.BottomCardId);
        }

        topCard.BottomCardId = this.Id;
        this.TopCardId = topCard.Id;

        // If this (bottom) is a PiniCard and topCard is food, start eating
        if (this is PiniCard piniCard && topCard.Data?.category == Script.CardCategory.Food)
        {
            piniCard.OnCardAddedOnTop(topCard);
        }
        // If topCard is a PiniCard and this (bottom) is food, start eating
        else if (topCard is PiniCard _topPini && this.Data?.category == Script.CardCategory.Food)
        {
            _topPini.OnFoodBelow(this);
            
        }
        else
        {
            CheckCombination();
        }

        return true;
    }

    public void RemoveFromGroup(Card topCard)
    {
        topCard.BottomCardId = 0;
        this.TopCardId = 0;
        CheckCombination();
    }

    public void CheckCombination()
    {
        var bottom = GamePlayManager.Instance.GetCardById(BottomCardId);
        if (bottom != null)
        {
            TargetProcessTime = 0;
            bottom.CheckCombination();
            return;
        }

        var combination = GamePlayManager.Instance.GetCombinationByCreateBy(GetAllTopTypesInGroup());

        if (combination != null)
        {
            TargetProcessTime = combination.duration;
        }
        else
        {
            TargetProcessTime = 0;
        }
    }

    public List<string> GetAllTopTypesInGroup()
    {
        List<string> types = new List<string>();
        types.Add(this.Type);

        var top = GamePlayManager.Instance.GetCardById(TopCardId);
        if (top != null)
        {
            types.AddRange(top.GetAllTopTypesInGroup());
        }

        return types;
    }
    public List<Card> GetAllTopCardsInGroup()
    {
        List<Card> cards = new List<Card>();
        cards.Add(this);

        var top = GamePlayManager.Instance.GetCardById(TopCardId);
        if (top != null)
        {
            cards.AddRange(top.GetAllTopCardsInGroup());
        }

        return cards;
    }

    public virtual void UpdateTick()
    {
        if (TargetProcessTime > 0)
        {
            ProcessTime += UnityEngine.Time.deltaTime;
            if (ProcessTime > TargetProcessTime)
            {
                ProcessTime = 0;
                ProcessComplete();
            }
        }
        else
        {
            ProcessTime = 0;
        }
    }

    public virtual void ProcessComplete()
    {
        var result = GamePlayManager.Instance.GetCardDataByCreateBy(GetAllTopTypesInGroup());
        if (result == null)
        {
            Debug.LogWarning($"Combination result not found for stack: {string.Join(",", GetAllTopTypesInGroup())}");
            TargetProcessTime = 0;
            ProcessTime = 0;
            CheckCombination();
            return;
        }

        var newCard = CardFactory.CreateCard(result, 0);
        
        // Add random offset to spawn position (X-Z plane for top-down view)
        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-1.5f, 1.5f),
            0f,
            UnityEngine.Random.Range(-1.5f, 1.5f)
        );
        Vector3 spawnPosition = (Vector3)this.Position + randomOffset;
        
        GamePlayManager.Instance.AddCard(newCard, spawnPosition);

        foreach (var item in GetAllTopCardsInGroup())
        {
            item.CombinedComplete();
        }
    }
    
    public virtual void CombinedComplete()
    {
        CombinationUses++;
        HandleUpgradeIfNeeded();

        if (ShouldDestroyAfterCombination())
        {
            // Only detach from neighbors if this card is being destroyed
            // This keeps surviving cards connected for the next process
            DetachFromNeighbors();
            GamePlayManager.Instance.RemoveCard(Id);
        }
        else
        {
            ProcessTime = 0;
            TargetProcessTime = 0;
            CheckCombination();
            GamePlayManager.Instance.RefreshVisualCard(this);
        }
    }

    void ApplyData(CardDataSo cardDataSo)
    {
        Data = cardDataSo;
        Type = cardDataSo.type;
        Value = cardDataSo.value;
    }

    void HandleUpgradeIfNeeded()
    {
        if (Data == null || Data.upgradeTarget == null)
            return;

        ApplyData(Data.upgradeTarget);
    }

    bool ShouldDestroyAfterCombination()
    {
        if (Data == null)
            return true;

        if (Data.maxCombinationUses < 0)
            return false;

        return CombinationUses >= Mathf.Max(1, Data.maxCombinationUses);
    }

    void DetachFromNeighbors()
    {
        if (TopCardId != 0)
        {
            var top = GamePlayManager.Instance.GetCardById(TopCardId);
            if (top != null)
            {
                top.BottomCardId = BottomCardId;
            }
        }

        if (BottomCardId != 0)
        {
            var bottom = GamePlayManager.Instance.GetCardById(BottomCardId);
            if (bottom != null)
            {
                bottom.TopCardId = TopCardId;
            }
        }

        TopCardId = 0;
        BottomCardId = 0;
    }
    
}