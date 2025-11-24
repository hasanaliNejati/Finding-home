using System.Collections.Generic;
using Script;

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
    }

    public void CheckCreate()
    {
        var bottom = GamePlayManager.Instance.GetCardById(BottomCardId);
        if (bottom != null)
        {
            bottom.CheckCreate();
            return;
        }

        var combination = GamePlayManager.Instance.GetCombinationByCreateBy(GetAllTypesInGroup());

        if (combination != null)
        {
            TargetProcessTime = combination.duration;
        }else
        {
            TargetProcessTime = 0;
        }
    }

    public List<string> GetAllTypesInGroup()
    {
        List<string> types = new List<string>();
        types.Add(this.Type);

        var bottom = GamePlayManager.Instance.GetCardById(BottomCardId);
        if (bottom != null)
        {
            types.AddRange(bottom.GetAllTypesInGroup());
        }

        return types;
    }

    public virtual void UpdateTick()
    {
        if (TargetProcessTime > 0)
        {
            ProcessTime += UnityEngine.Time.deltaTime;
            if(ProcessTime > TargetProcessTime)
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
         GamePlayManager.Instance.GetCombinationByCreateBy(GetAllTypesInGroup());
    }
}