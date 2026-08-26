namespace Hollowmere.Hud
{
    /// <summary>
    /// Renderer bridge for one HUD widget. Receives resolved layout only; does not
    /// participate in profile selection, storage, or edit mode.
    /// </summary>
    public interface IHudWidgetPresenter : System.IDisposable
    {
        string WidgetId { get; }

        HudSize GetIntrinsicSize();

        void Apply(HudResolvedWidgetLayout layout);
    }

    /// <summary>
    /// Optional per-frame hook for presenters that must react to gameplay state
    /// without polling from the host's layout path.
    /// </summary>
    public interface IHudWidgetPresenterTick
    {
        void Tick();
    }
}
