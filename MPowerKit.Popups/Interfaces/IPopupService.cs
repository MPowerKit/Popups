namespace MPowerKit.Popups.Interfaces;

public interface IPopupService
{
    IReadOnlyList<PopupPage> PopupStack { get; }
    ValueTask ShowPopupAsync(PopupPage page, bool animated = true);
    ValueTask ShowPopupAsync(PopupPage page, Window? attachToWindow, bool animated = true);
    ValueTask HidePopupAsync(bool animated = true);
    ValueTask HidePopupAsync(PopupPage page, bool animated = true);
}