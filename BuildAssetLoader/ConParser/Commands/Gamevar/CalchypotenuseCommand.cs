namespace BuildAssetLoader.Con
{
    // calchypotenuse <returnvar> <x> <y>
    public sealed record CalchypotenuseCommand(string ReturnVar, string X, string Y) : Command(CommandList.calchypotenuse);
}

