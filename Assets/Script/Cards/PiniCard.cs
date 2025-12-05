using Script;
using UnityEngine;

[System.Serializable]
public class PiniCard : Card
{
    public CardDataSo DeathDropCard;

    public PiniCard(PiniDataSo piniData, int id) : base(piniData, id)
    {
        DeathDropCard = piniData.deathDropCard;
    }

    /// <summary>
    /// Check if Pini can accept another card on top (when Pini is bottom)
    /// </summary>
    public bool CanMergeWith(Card other)
    {
        if (other?.Data == null)
            return false;
        return true;
    }

    /// <summary>
    /// Check if Pini can be placed on another card (when Pini is being dragged)
    /// </summary>
    public bool CanBePlacedOn(Card other)
    {
        if (other?.Data == null)
            return false;
        return true;
    }

    public override void UpdateTick()
    {
        // Base update for normal combinations
        base.UpdateTick();
    }

    /// <summary>
    /// Called when Pini dies (not enough food at end of day)
    /// </summary>
    public void Die()
    {
        // Detach from any connected cards
        if (TopCardId != 0)
        {
            var top = GamePlayManager.Instance.GetCardById(TopCardId);
            if (top != null)
                top.BottomCardId = 0;
        }
        if (BottomCardId != 0)
        {
            var bottom = GamePlayManager.Instance.GetCardById(BottomCardId);
            if (bottom != null)
                bottom.TopCardId = 0;
        }

        // Spawn death drop card if configured
        if (DeathDropCard != null)
        {
            var dropCard = CardFactory.CreateCard(DeathDropCard, 0);
            Vector3 spawnPos = (Vector3)this.Position;
            GamePlayManager.Instance.AddCard(dropCard, spawnPos);
        }

        // Remove Pini
        GamePlayManager.Instance.RemoveCard(this.Id);
    }


    /// <summary>
    /// Pini should never be destroyed after a combination
    /// </summary>
    public override void CombinedComplete()
    {
        // Don't call base - Pini should never be destroyed
        // Just reset process state and check for new combinations
        ProcessTime = 0;
        TargetProcessTime = 0;
        CheckCombination();
        GamePlayManager.Instance.RefreshVisualCard(this);
    }
}

