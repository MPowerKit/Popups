using Microsoft.Maui.Platform;

namespace MPowerKit.Popups;

public partial class PopupService
{
    protected virtual partial void AttachToWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow)
    {
        HandleAccessibility(true, page.DisableAndroidAccessibilityHandling, parentWindow);

        var dv = (parentWindow.Handler.PlatformView as Android.App.Activity)?.Window?.DecorView as Android.Views.ViewGroup
            ?? throw new InvalidOperationException("DecorView of Activity not found");

        var handler = pageHandler as IPlatformViewHandler;

        handler.PlatformView.ViewAttachedToWindow += (s, e) =>
        {
            dv.Context.HideKeyboard(dv);
        };

        handler.PlatformView.ViewDetachedFromWindow += (s, e) =>
        {
            dv.Context.HideKeyboard(dv);
        };

        handler.PlatformView.Touch += (s, e) =>
        {
            var view = s as Android.Views.ViewGroup;

            if (page.Content is not null && view.ChildCount > 0)
            {
                var child = view.GetChildAt(0);

                var rawx = e.Event.RawX;
                var rawy = e.Event.RawY;
                var childx = child.GetX();
                var childy = child.GetY();

                if (rawx >= childx && rawx <= (child.Width + childx)
                    && rawy >= childy && rawy <= (child.Height + childy))
                {
                    e.Handled = true;
                    return;
                }
            }

            page.SendBackgroundClick();

            e.Handled = !page.BackgroundInputTransparent;
        };

        dv.AddView(handler.PlatformView);
    }

    protected virtual partial void DetachFromWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow)
    {
        var handler = pageHandler as IPlatformViewHandler;

        HandleAccessibility(false, page.DisableAndroidAccessibilityHandling, parentWindow);

        handler.PlatformView.RemoveFromParent();
    }

    //! important keeps reference to pages that accessibility has applied to. This is so accessibility can be removed properly when popup is removed. #https://github.com/LuckyDucko/Mopups/issues/93
    private readonly List<Android.Views.View?> _accessibilityViews = [];
    void HandleAccessibility(bool showPopup, bool disableAccessibilityHandling, Window window)
    {
        if (disableAccessibilityHandling) return;

        if (showPopup)
        {
            var mainPage = window.Page;
            if (mainPage is null) return;

            _accessibilityViews.Add(mainPage.Handler?.PlatformView as Android.Views.View);

            if (mainPage.Navigation.NavigationStack.Count > 0)
            {
                _accessibilityViews.Add(mainPage.Navigation?.NavigationStack[^1]?.Handler?.PlatformView as Android.Views.View);
            }

            if (mainPage.Navigation.ModalStack.Count > 0)
            {
                _accessibilityViews.Add(mainPage.Navigation?.ModalStack[^1]?.Handler?.PlatformView as Android.Views.View);
            }
        }

        foreach (var view in _accessibilityViews)
        {
            if (view is null) continue;

            // Screen reader
            view.ImportantForAccessibility = showPopup
                ? Android.Views.ImportantForAccessibility.NoHideDescendants
                : Android.Views.ImportantForAccessibility.Auto;

            // Keyboard navigation
            ((Android.Views.ViewGroup)view).DescendantFocusability = showPopup
                ? Android.Views.DescendantFocusability.BlockDescendants
                : Android.Views.DescendantFocusability.AfterDescendants;
            view.ClearFocus();
        }
    }
}