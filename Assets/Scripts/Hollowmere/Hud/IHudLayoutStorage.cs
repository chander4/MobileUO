namespace Hollowmere.Hud
{
    /// <summary>
    /// Storage boundary for full HUD layout profiles. Implementations may be local,
    /// account-backed, or cloud-backed; callers depend only on this contract.
    /// </summary>
    public interface IHudLayoutStorage
    {
        bool TryLoad(string profileId, out HudLayoutProfile profile, out string error);
        void Save(HudLayoutProfile profile);
        void Delete(string profileId);
    }
}
