using Microsoft.Maui.Platform;

using MPowerKit.Popups.Interfaces;

namespace MPowerKit.Popups;

public partial class PopupService : IPopupService
{
    private static PopupService? _instance;

    public static IPopupService Current => _instance ??= new PopupService();

    protected List<PopupPage> InternalPopupStack { get; } = [];
    public IReadOnlyList<PopupPage> PopupStack => InternalPopupStack;

    public virtual ValueTask ShowPopupAsync(PopupPage page, bool animated = true)
    {
        if (PopupStack.Contains(page))
        {
            throw new InvalidOperationException("This popup already presented");
        }

        var window = Application.Current?.Windows[0];

        return ShowPopupAsync(page, window, animated);
    }

    public virtual async ValueTask ShowPopupAsync(PopupPage page, Window? attachToWindow, bool animated = true)
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

        attachToWindow.AddLogicalChild(page);

        page.BackgroundClicked += async (s, e) =>
        {
            if (!page.CloseOnBackgroundClick) return;

            try
            {
                if (e.Handled) return;
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

        InternalPopupStack.Add(page);

        if (animated)
        {
            //HACK: animation needs dispatcher to get page size ready
            await page.Dispatcher.DispatchAsync(page.AppearingAnimation);
        }
    }

    public virtual ValueTask HidePopupAsync(bool animated = true)
    {
        if (PopupStack.Count == 0)
        {
            throw new InvalidOperationException("Popup stack is empty");
        }

        var page = PopupStack[^1];

        return HidePopupAsync(page, animated);
    }

    public virtual ValueTask HidePopupAsync(PopupPage page, bool animated = true)
    {
        return HidePopupAsync(page, page.Window, animated);
    }

    protected virtual async ValueTask HidePopupAsync(PopupPage page, Window parentWindow, bool animated = true)
    {
        if (page.IsClosing) return;

        if (!PopupStack.Contains(page))
        {
            throw new InvalidOperationException("Popup stack does not contain chosen page");
        }
        if (parentWindow is null)
        {
            throw new InvalidOperationException("Parent window not found");
        }

        page.IsClosing = true;

        animated = animated && AnimationHelper.SystemAnimationsEnabled;

        if (animated)
        {
            await page.DisappearingAnimation();
        }

        page.SendDisappearing();
        DetachFromWindow(page, page.Handler!, parentWindow);

        parentWindow.RemoveLogicalChild(page);
        InternalPopupStack.Remove(page);

        if (animated)
        {
            page.DisposingAnimation();
        }

        page.BindingContext = null;
        page.Behaviors.Clear();
#if NET9_0_OR_GREATER
        page.DisconnectHandlers();
#endif
    }

    protected virtual partial void AttachToWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow);
    protected virtual partial void DetachFromWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow);
}