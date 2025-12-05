using Script;
using UnityEngine;

[System.Serializable]
public class EnergyCard : Card
{
    public EnergyCard(EnergyDataSo energyData, int id) : base(energyData, id)
    {
        // Energy cards are ready to use
    }

    /// <summary>
    /// Called when this energy card is used in a combination
    /// Spawns a pollution card at the energy card's position
    /// </summary>
    public void OnEnergyUsed()
    {
        var energyData = Data as EnergyDataSo;
        if (energyData == null || energyData.pollutionCard == null)
        {
            Debug.LogWarning($"EnergyCard {Type} does not have a pollution card configured.");
            return;
        }

    }
}





