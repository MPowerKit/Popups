namespace MPowerKit.Popups.Interfaces;

public interface IPopupService
{
    Task ShowPopupAsync(PopupPage page, bool animated = true);
    Task ShowPopupAsync(PopupPage page, Window? parentWindow, bool animated = true);
}