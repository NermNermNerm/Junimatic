namespace NermNermNerm.Junimatic
{
    public enum JunimoType
    {
        // Be careful with the ordering, there's code in GameMachine.IsCompatibleWithJunimo that depends on the order.
        // Do not change the names - mods that add custom machines can be relying on these names.

        Mining,
        Animals,
        Crops,
        Fishing,
        Forestry
    };
}
