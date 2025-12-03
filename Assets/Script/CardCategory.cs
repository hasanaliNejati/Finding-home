namespace Script
{
    public enum CardCategory
    {
        None,
        Food,               // غذا (Fish, FishCan, ...)
        RawMaterial,        // مواد خام (Wood, Stone, Ice, ...)
        CraftedMaterial,    // ساخته شده (Hammer, Ship, SolarPanel, ...)
        HarvestingMaterial, // منبع برداشت (Tree, Rock, FishHole, ...)
        Creature,           // موجودات زنده (Pini)
        Energy,             // انرژی (CleanEnergy, OilEnergy, ...)
        Pollution,          // آلودگی
        Special             // خاص (Cargo, Coin, ...)
    }
}

