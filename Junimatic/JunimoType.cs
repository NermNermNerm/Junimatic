namespace NermNermNerm.Junimatic
{
    public enum JunimoType
    {
        /* Careful with the ordering, there's code in GameMachine.IsCompatibleWithJunimo that depends on the order */

        MiningProcessing,
        Animals,
        CropProcessing,
        Fishing,
        Forestry
    };
}
