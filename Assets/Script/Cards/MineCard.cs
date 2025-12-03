using System.Collections.Generic;
using Script;
using UnityEngine;

namespace Script
{
    
    public class MineCard : Card
    {
        
        private float miningTimer = 0f;
        private float miningInterval;
        private List<CargoCardRatioItem> mineCardRatioItems;
        private bool hasCreatureOnTop = false;

        public MineCard(MineDataSo mineData, int id) : base(mineData, id)
        {
            miningInterval = mineData.miningInterval;
            mineCardRatioItems = mineData.mineCardRatioItems;
            
            // Debug: Check if mine is configured correctly
            if (mineCardRatioItems == null || mineCardRatioItems.Count == 0)
            {
                Debug.LogWarning($"[MineCard] Mine {Type} has no mineCardRatioItems configured! It won't be able to generate cards.");
            }
            else
            {
                Debug.Log($"[MineCard] Mine {Type} initialized with {mineCardRatioItems.Count} card types, interval: {miningInterval}s");
            }
        }

        /// <summary>
        /// Check if there is a Creature (PiniCard) on top or bottom of this mine
        /// </summary>
        private bool HasCreatureOnTop()
        {
            // Check if there's a Creature on top of the mine
            if (TopCardId != 0)
            {
                var topCard = GamePlayManager.Instance.GetCardById(TopCardId);
                if (topCard != null)
                {
                    // Check if top card is a Creature (PiniCard)
                    if (topCard is PiniCard)
                        return true;

                    // Also check cards further up the stack
                    var currentCard = topCard;
                    while (currentCard != null)
                    {
                        if (currentCard is PiniCard)
                            return true;
                        
                        if (currentCard.TopCardId == 0)
                            break;
                            
                        currentCard = GamePlayManager.Instance.GetCardById(currentCard.TopCardId);
                    }
                }
            }

            // Check if mine is on top of a Creature (bottom card)
            if (BottomCardId != 0)
            {
                var bottomCard = GamePlayManager.Instance.GetCardById(BottomCardId);
                if (bottomCard != null)
                {
                    // Check if bottom card is a Creature (PiniCard)
                    if (bottomCard is PiniCard)
                        return true;

                    // Also check cards further down the stack
                    var currentCard = bottomCard;
                    while (currentCard != null)
                    {
                        if (currentCard is PiniCard)
                            return true;
                        
                        if (currentCard.BottomCardId == 0)
                            break;
                            
                        currentCard = GamePlayManager.Instance.GetCardById(currentCard.BottomCardId);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Generate a random card based on ratio items
        /// </summary>
        private Card GenerateRandomCard()
        {
            if (mineCardRatioItems == null || mineCardRatioItems.Count == 0)
            {
                Debug.LogWarning($"MineCard {Type} does not have any mineCardRatioItems configured.");
                return null;
            }

            // Calculate total weight
            float totalWeight = 0;
            foreach (var item in mineCardRatioItems)
            {
                totalWeight += item.ratio;
            }

            if (totalWeight <= 0)
            {
                Debug.LogWarning($"MineCard {Type} has invalid ratio weights.");
                return null;
            }

            // Select random card based on ratio
            float randomValue = Random.Range(0f, totalWeight);
            float current = 0;

            foreach (var item in mineCardRatioItems)
            {
                current += item.ratio;
                if (randomValue <= current)
                {
                    return CardFactory.CreateCard(item.cardData, 0);
                }
            }

            // Fallback (shouldn't reach here)
            return CardFactory.CreateCard(mineCardRatioItems[0].cardData, 0);
        }

        /// <summary>
        /// Spawn a generated card near the mine
        /// </summary>
        private void SpawnMinedCard()
        {
            var newCard = GenerateRandomCard();
            if (newCard == null)
            {
                Debug.LogWarning($"[MineCard] Failed to generate card for mine {Type}");
                return;
            }

            // Spawn card around mine on X-Z plane (top-down view)
            // Generate a random angle and distance to ensure card spawns around the mine
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float randomDistance = Random.Range(1.5f, 2.5f); // Distance between 1.5 and 2.5 units
            
            Vector3 randomOffset = new Vector3(
                Mathf.Cos(randomAngle) * randomDistance,
                0f,
                Mathf.Sin(randomAngle) * randomDistance
            );
            
            Vector3 spawnPosition = (Vector3)this.Position + randomOffset;
            newCard.Position = spawnPosition;
            
            // Use mine position as start position for animation (card will animate from mine to spawn position)
            Vector3 startPosition = (Vector3)this.Position;
            
            newCard.Id = GamePlayManager.Instance.GetNextCardId();
            GamePlayManager.Instance.Cards.Add(newCard.Id, newCard);
            GamePlayManager.Instance.GenerateVisualCard(newCard, startPosition);
            GamePlayManager.Instance.RefreshDeletedCards();
            
            Debug.Log($"[MineCard] Successfully spawned {newCard.Type} from mine {Type} at position {spawnPosition}");
        }

        public override void UpdateTick()
        {
            base.UpdateTick();

            // Check if there's a creature on top
            bool currentlyHasCreature = HasCreatureOnTop();
            
            // If creature was just placed, reset timer
            if (currentlyHasCreature && !hasCreatureOnTop)
            {
                miningTimer = 0f;
                TargetProcessTime = miningInterval;
                ProcessTime = 0f;
                Debug.Log($"[MineCard] Creature detected on mine {Type}, starting mining...");
            }
            
            hasCreatureOnTop = currentlyHasCreature;

            // Only mine if there's a creature on top
            if (hasCreatureOnTop)
            {
                miningTimer += Time.deltaTime;
                
                // Update ProcessTime for progress slider display
                ProcessTime = miningTimer;
                TargetProcessTime = miningInterval;
                
                // Refresh visual to update progress slider
                GamePlayManager.Instance.RefreshVisualCard(this);
                
                if (miningTimer >= miningInterval)
                {
                    miningTimer = 0f;
                    ProcessTime = 0f;
                    TargetProcessTime = 0f;
                    Debug.Log($"[MineCard] Mining interval reached for {Type}, spawning card...");
                    SpawnMinedCard();
                    // Refresh visual to hide progress slider
                    GamePlayManager.Instance.RefreshVisualCard(this);
                }
            }
            else
            {
                // Reset timer when no creature is on top
                if (miningTimer > 0 || TargetProcessTime > 0)
                {
                    miningTimer = 0f;
                    ProcessTime = 0f;
                    TargetProcessTime = 0f;
                    Debug.Log($"[MineCard] No creature on mine {Type}, resetting timer.");
                    // Refresh visual to hide progress slider
                    GamePlayManager.Instance.RefreshVisualCard(this);
                }
            }
        }
    }
}

