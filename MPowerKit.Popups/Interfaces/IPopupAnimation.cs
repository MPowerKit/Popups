namespace MPowerKit.Popups.Interfaces;

public interface IPopupAnimation
{
    public TimeSpan DurationIn { get; set; }
    public TimeSpan DurationOut { get; set; }
    void Preparing(View? content, PopupPage page);
    void Disposing(View? content, PopupPage page);
    Task Appearing(View? content, PopupPage page);
    Task Disappearing(View? content, PopupPage page);
}