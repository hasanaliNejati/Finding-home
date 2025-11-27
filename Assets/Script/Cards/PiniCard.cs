using Script;
using UnityEngine;

[System.Serializable]
public class PiniCard : Card
{
    public int MaxHealth;
    public int MaxFood;
    public float CurrentHealth;
    public float CurrentFood;
    public float FoodDecreaseRate;
    public float HealthDecreaseRate;
    public int FoodPerMeal;
    public float EatingDuration;
    public CardDataSo DeathDropCard;

    // State for eating process
    public bool IsEating;
    public float EatingProgress;
    public int FoodCardId; // The card being eaten

    public PiniCard(PiniDataSo piniData, int id) : base(piniData, id)
    {
        MaxHealth = piniData.maxHealth;
        MaxFood = piniData.maxFood;
        FoodDecreaseRate = piniData.foodDecreaseRate;
        HealthDecreaseRate = piniData.healthDecreaseRate;
        FoodPerMeal = piniData.foodPerMeal;
        EatingDuration = piniData.eatingDuration;
        DeathDropCard = piniData.deathDropCard;

        // Start with full stats
        CurrentHealth = MaxHealth;
        CurrentFood = MaxFood;
    }

    public bool IsHungry => CurrentFood <= 0;
    public bool IsFull => CurrentFood >= MaxFood;
    public bool IsDead => CurrentHealth <= 0;

    /// <summary>
    /// Check if Pini can accept another card on top (when Pini is bottom)
    /// </summary>
    public bool CanMergeWith(Card other)
    {
        if (other?.Data == null)
            return false;

        bool isFood = other.Data.category == CardCategory.Food;

        // If full, can't eat more food
        if (IsFull && isFood)
            return false;

        // If hungry, can only merge with food
        if (IsHungry && !isFood)
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

        // If hungry, can only be placed on food
        if (IsHungry && other.Data.category != CardCategory.Food)
            return false;

        return true;
    }

    public override void UpdateTick()
    {
        // Handle eating process
        if (IsEating)
        {
            EatingProgress += Time.deltaTime;
            if (EatingProgress >= EatingDuration)
            {
                CompleteEating();
            }
            return; // Don't decrease food/health while eating
        }

        // Decrease food over time
        if (CurrentFood > 0)
        {
            float previousFood = CurrentFood;
            CurrentFood -= FoodDecreaseRate * Time.deltaTime;
            if (CurrentFood < 0)
                CurrentFood = 0;

            // Just became hungry - detach from non-food cards
            if (previousFood > 0 && CurrentFood <= 0)
            {
                DetachFromNonFoodCards();
            }
        }
        else
        {
            // Hungry! Decrease health
            CurrentHealth -= HealthDecreaseRate * Time.deltaTime;
            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                Die();
                return;
            }
        }

        // Base update for normal combinations
        base.UpdateTick();
    }

    /// <summary>
    /// Detach from all non-food cards when becoming hungry
    /// </summary>
    private void DetachFromNonFoodCards()
    {
        // Check top card - if not food, detach
        if (TopCardId != 0)
        {
            var topCard = GamePlayManager.Instance.GetCardById(TopCardId);
            if (topCard != null && topCard.Data?.category != CardCategory.Food)
            {
                topCard.BottomCardId = 0;
                this.TopCardId = 0;
            }
        }

        // Check bottom card - if not food, detach
        if (BottomCardId != 0)
        {
            var bottomCard = GamePlayManager.Instance.GetCardById(BottomCardId);
            if (bottomCard != null && bottomCard.Data?.category != CardCategory.Food)
            {
                bottomCard.TopCardId = 0;
                this.BottomCardId = 0;
            }
        }

        // Reset any ongoing combination process
        TargetProcessTime = 0;
        ProcessTime = 0;
    }

    /// <summary>
    /// Called when Pini dies (health reaches 0)
    /// </summary>
    private void Die()
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
    /// Start eating a food card
    /// </summary>
    public void StartEating(Card foodCard)
    {
        if (foodCard == null || IsFull)
            return;

        IsEating = true;
        EatingProgress = 0;
        FoodCardId = foodCard.Id;

        // Set process time for visual feedback
        TargetProcessTime = EatingDuration;
        ProcessTime = 0;
    }

    /// <summary>
    /// Complete the eating process
    /// </summary>
    private void CompleteEating()
    {
        IsEating = false;
        EatingProgress = 0;
        TargetProcessTime = 0;
        ProcessTime = 0;

        // Restore food
        CurrentFood = Mathf.Min(CurrentFood + FoodPerMeal, MaxFood);

        // Handle the food card
        var foodCard = GamePlayManager.Instance.GetCardById(FoodCardId);
        if (foodCard != null)
        {
            // Detach food card from Pini (food is on top of Pini)
            if (foodCard.BottomCardId == this.Id)
            {
                foodCard.BottomCardId = 0;
                this.TopCardId = 0;
            }
            // Detach food card from Pini (Pini is on top of food)
            else if (this.BottomCardId == foodCard.Id)
            {
                this.BottomCardId = 0;
                foodCard.TopCardId = 0;
            }

            // Consume the food card (reduce uses or destroy)
            foodCard.CombinationUses++;
            if (foodCard.Data.maxCombinationUses >= 0 && 
                foodCard.CombinationUses >= foodCard.Data.maxCombinationUses)
            {
                GamePlayManager.Instance.RemoveCard(foodCard.Id);
            }
            else
            {
                // Food has uses left, refresh its visual
                GamePlayManager.Instance.RefreshVisualCard(foodCard);
            }
        }

        FoodCardId = 0;

        // Refresh Pini's visual
        GamePlayManager.Instance.RefreshVisualCard(this);
        
        // Check for new combinations
        CheckCombination();
    }

    /// <summary>
    /// Called when another card is added on top of Pini
    /// </summary>
    public void OnCardAddedOnTop(Card topCard)
    {
        if (topCard?.Data == null)
            return;

        // If it's food and we're not full, start eating
        if (topCard.Data.category == CardCategory.Food && !IsFull)
        {
            StartEating(topCard);
        }
    }

    /// <summary>
    /// Called when Pini is placed on top of a food card
    /// </summary>
    public void OnFoodBelow(Card foodCard)
    {
        if (foodCard?.Data == null)
            return;

        // If it's food and we're not full, start eating
        if (foodCard.Data.category == CardCategory.Food && !IsFull)
        {
            StartEating(foodCard);
        }
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

