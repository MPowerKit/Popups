namespace MPowerKit.Popups.Interfaces;

public interface IPopupService
{
    Task ShowPopupAsync(PopupPage page, bool animated = true);
    Task ShowPopupAsync(PopupPage page, Window? attachToWindow, bool animated = true);
    Task HidePopupAsync(bool animated = true);
    Task HidePopupAsync(PopupPage page, bool animated = true);
}