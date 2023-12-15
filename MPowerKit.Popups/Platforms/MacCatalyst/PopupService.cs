using CoreGraphics;

using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

using UIKit;

namespace MPowerKit.Popups;

public partial class PopupService
{
    protected readonly List<UIWindow> Windows = [];

    protected virtual partial void AttachToWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow)
    {
        var window = parentWindow.Handler.PlatformView as UIWindow
            ?? throw new InvalidOperationException("Window not found");

        var handler = pageHandler as PageHandler;

        var connectedScene = UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIWindowScene>()
            .FirstOrDefault(x => x.ActivationState == UISceneActivationState.ForegroundActive);
        var popupWindow = connectedScene is not null
            ? new PopupWindow(connectedScene)
            : new PopupWindow();

        Windows.Add(popupWindow);

        popupWindow.RootViewController = handler.ViewController;

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

        handler.ViewController.View.AddGestureRecognizer(gr);

        popupWindow.MakeKeyAndVisible();
    }

    protected virtual partial void DetachFromWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow)
    {
        var window = parentWindow.Handler.PlatformView as UIWindow
            ?? throw new InvalidOperationException("Window not found");

        var handler = pageHandler as PageHandler;

        var popupWindow = handler.ViewController.View.Window;

        foreach (var gr in handler.ViewController.View.GestureRecognizers.ToList())
        {
            handler.ViewController.View.RemoveGestureRecognizer(gr);
        }

        popupWindow.RootViewController.DismissViewController(false, null);
        popupWindow.RootViewController.Dispose();
        popupWindow.Hidden = true;
        popupWindow.WindowScene = null;
        popupWindow.Dispose();

        Windows.Remove(popupWindow);

        window.MakeKeyAndVisible();
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
            var viewcontroller = RootViewController as PageViewController;
            var page = viewcontroller.CurrentView as PopupPage;

            var hitTestResult = base.HitTest(point, uievent);

            if (uievent.Type != UIEventType.Hover && hitTestResult is not UITextField)
            {
                RootViewController.View.EndEditing(true);
            }

            return hitTestResult == viewcontroller.CurrentPlatformView
                && page.BackgroundInputTransparent
                ? null
                : hitTestResult;
        }
    }
}