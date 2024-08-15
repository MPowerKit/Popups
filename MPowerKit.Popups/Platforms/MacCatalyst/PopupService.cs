using CoreGraphics;

using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

using UIKit;

namespace MPowerKit.Popups;

public partial class PopupService
{
    protected static readonly List<UIWindow> PrevKeyWindows = [];

    protected readonly List<UIWindow> Windows = [];

    protected virtual partial void AttachToWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow)
    {
        var window = parentWindow.Handler.PlatformView as UIWindow
            ?? throw new InvalidOperationException("Window not found");

        var handler = (pageHandler as IPlatformViewHandler)!;

        var gr = new UITapGestureRecognizer(e =>
        {
            var loc = e.LocationOfTouch(0, e.View);
            var view = e.View.HitTest(loc, null);

            if (handler.ViewController is PageViewController pc && pc.CurrentPlatformView == view)
            {
                page.SendBackgroundClick();
            }
        })
        { CancelsTouchesInView = false };

        handler.ViewController!.View!.AddGestureRecognizer(gr);

        var prevKeyWindow = GetKeyWindow();

        AddToVisualTree(handler);

        StorePrevKeyWindow(prevKeyWindow);
    }

    protected virtual void AddToVisualTree(IPlatformViewHandler handler)
    {
        var connectedScene = GetActiveScene();
        PopupWindow popupWindow = connectedScene is not null
            ? new(connectedScene)
            : new();

        Windows.Add(popupWindow);

        popupWindow.RootViewController = handler.ViewController;

        popupWindow.MakeKeyAndVisible();
    }

    protected virtual partial void DetachFromWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow)
    {
        var window = parentWindow.Handler.PlatformView as UIWindow
            ?? throw new InvalidOperationException("Window not found");

        var handler = (pageHandler as PageHandler)!;

        var view = handler.ViewController!.View!;

        foreach (var gr in view.GestureRecognizers!.ToList())
        {
            view.RemoveGestureRecognizer(gr);
        }

        RemoveFromVisualTree(handler);

        (RestorePrevKeyWindow() ?? window).MakeKeyWindow();
    }

    protected virtual void RemoveFromVisualTree(IPlatformViewHandler handler)
    {
        var view = handler.ViewController!.View!;

        var popupWindow = view.Window;

        popupWindow.RootViewController!.DismissViewController(false, null);
        popupWindow.RootViewController.Dispose();
        popupWindow.Hidden = true;
        popupWindow.WindowScene = null;
        popupWindow.Dispose();

        Windows.Remove(popupWindow);
    }

    protected virtual void StorePrevKeyWindow(UIWindow? window)
    {
        if (window is null) return;

        PrevKeyWindows.Remove(window);

        PrevKeyWindows.Add(window);
    }

    protected virtual UIWindow? RestorePrevKeyWindow()
    {
        if (PrevKeyWindows.Count == 0) return null;

        var window = PrevKeyWindows[^1];

        PrevKeyWindows.Remove(window);

        return window;
    }

    public class PopupWindow : UIWindow
    {
        public PopupWindow(IntPtr handle) : base(handle)
        {
        }

        public PopupWindow()
        {
        }

        public PopupWindow(UIWindowScene uiWindowScene) : base(uiWindowScene)
        {
        }

        public override UIView? HitTest(CGPoint point, UIEvent? uievent)
        {
            var viewcontroller = (RootViewController as PageViewController)!;
            var page = (viewcontroller.CurrentView as PopupPage)!;

            var hitTestResult = base.HitTest(point, uievent);

            if (uievent?.Type is not UIEventType.Hover && hitTestResult is not UITextField)
            {
                viewcontroller.View!.EndEditing(true);
            }

            return hitTestResult == viewcontroller.CurrentPlatformView
                && page.BackgroundInputTransparent
                ? null
                : hitTestResult;
        }
    }

    public static UIWindow? GetKeyWindow()
    {
        var window = GetActiveScene()?.Windows.FirstOrDefault(w => w.IsKeyWindow);

        return window;
    }

    public static UIWindowScene? GetActiveScene()
    {
        var connectedScene = UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIWindowScene>()
            .FirstOrDefault(x => x.ActivationState == UISceneActivationState.ForegroundActive);

        return connectedScene;
    }
}