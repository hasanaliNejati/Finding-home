namespace Script
{
    public static class CardFactory
    {
        public static Card CreateCard(CardDataSo data, int id)
        {
            if (data == null)
                return null;

            return data switch
            {
                PiniDataSo pini => new PiniCard(pini, id),
                CargoDataSo cargo => new CargoCard(cargo, id),
                EnergyDataSo energy => new EnergyCard(energy, id),
                MineDataSo mine => new MineCard(mine, id),
                _ => new Card(data, id)
            };
        }
    }
}










