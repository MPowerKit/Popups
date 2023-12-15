using Microsoft.Maui.Platform;

using MPowerKit.Popups.Interfaces;

namespace MPowerKit.Popups;

public partial class PopupService : IPopupService
{
    private static PopupService? _instance;

    public static IPopupService Current => _instance ??= new PopupService();

    public static List<PopupPage> PopupStack { get; } = [];

    public virtual Task ShowPopupAsync(PopupPage page, bool animated = true)
    {
        if (PopupStack.Contains(page))
        {
            throw new InvalidOperationException("This popup already presented");
        }

        var window = Application.Current?.Windows.FirstOrDefault();

        return ShowPopupAsync(page, window, animated);
    }

    public virtual async Task ShowPopupAsync(PopupPage page, Window? attachToWindow, bool animated = true)
    {
        if (PopupStack.Contains(page))
        {
            throw new InvalidOperationException("This popup already presented");
        }
        if (attachToWindow is null)
        {
            throw new InvalidOperationException("Parent window not found");
        }

        animated = animated && AnimationHelper.SystemAnimationsEnabled;

        var pageHandler = page.ToHandler(attachToWindow.Handler.MauiContext!);

        page.Parent = attachToWindow;

        page.RequestClose += async (s, e) =>
        {
            try
            {
                await HidePopupAsync(page, animated);
            }
            catch { }
        };

        if (animated)
        {
            page.PreparingAnimation();
        }

        AttachToWindow(page, pageHandler, attachToWindow!);
        page.SendAppearing();

        PopupStack.Add(page);

        if (animated)
        {
            await page.AppearingAnimation();
        }
    }

    public virtual Task HidePopupAsync(bool animated = true)
    {
        if (PopupStack.Count == 0)
        {
            throw new InvalidOperationException("Popup stack is empty");
        }

        var page = PopupStack[^1];

        return HidePopupAsync(page, animated);
    }

    public virtual Task HidePopupAsync(PopupPage page, bool animated = true)
    {
        return HidePopupAsync(page, page.Window, animated);
    }

    protected virtual async Task HidePopupAsync(PopupPage page, Window parentWindow, bool animated = true)
    {
        if (!PopupStack.Contains(page))
        {
            throw new InvalidOperationException("Popup stack does not contain chosen page");
        }
        if (parentWindow is null)
        {
            throw new InvalidOperationException("Parent window not found");
        }

        animated = animated && AnimationHelper.SystemAnimationsEnabled;

        if (animated)
        {
            await page.DisappearingAnimation();
        }

        page.SendDisappearing();
        DetachFromWindow(page, page.Handler, parentWindow);

        page.Parent = null;
        PopupStack.Remove(page);

        if (animated)
        {
            page.DisposingAnimation();
        }
    }

    protected virtual partial void AttachToWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow);
    protected virtual partial void DetachFromWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow);
}