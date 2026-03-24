namespace Dagon.Gameplay
{
    public interface IBossHealthDisplayOverride
    {
        float DisplayedCurrentHealth { get; }
        float DisplayedMaxHealth { get; }
        bool IsBossHealthVisible { get; }
    }
}
