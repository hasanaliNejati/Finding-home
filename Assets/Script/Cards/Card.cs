using System.Collections.Generic;
using NUnit.Framework;
using Script;
using UnityEngine;

[System.Serializable]
public class Card
{
    public Card(CardDataSo cardDataSo, int id)
    {
        Type = cardDataSo.type;
        Id = id;
    }


    public string Type;
    public int Id;
    public int TopCardId;
    public int BottomCardId;

    public Vec3 Position;

    public float ProcessTime;
    public float TargetProcessTime;

    public virtual bool Done()
    {
        return false;
    }

    public void AddToGroup(Card topCard)
    {
        topCard.BottomCardId = this.Id;
        this.TopCardId = topCard.Id;
        CheckCombination();
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
            Debug.Log("null");
            TargetProcessTime = 0;
            bottom.CheckCombination();
            return;
        }

        Debug.Log("check combination");

        var combination = GamePlayManager.Instance.GetCombinationByCreateBy(GetAllTopTypesInGroup());

        if (combination != null)
        {
            Debug.Log("found combination for " + Type);
            TargetProcessTime = combination.duration;
        }
        else
        {
            Debug.Log("no combination found for " + Type);
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
        var newCard = new Card(result, 0);
        GamePlayManager.Instance.AddCard(newCard, this.Position);

        foreach (var item in GetAllTopCardsInGroup())
        {
            item.CombinedComplete();
        }
        
        

        //GamePlayManager.Instance.RemoveCard(this.Id);

    }
    
    public void CombinedComplete()
    {
        GamePlayManager.Instance.RemoveCard(Id);
    }
    
}