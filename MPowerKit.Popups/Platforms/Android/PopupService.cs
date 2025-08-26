using Android.App;
using Android.Views;

using Microsoft.Maui.Platform;

using static Android.Views.View;

using FragmentManager = AndroidX.Fragment.App.FragmentManager;
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
using Window = Microsoft.Maui.Controls.Window;

namespace MPowerKit.Popups;

public partial class PopupService
{
    protected FragmentLifecycleCallback? Observer { get; set; }
    private WeakReference<FragmentManager>? _fmReference;

    protected FragmentManager? FragmentManager
    {
        get
        {
            if (_fmReference is null) return null;

            _fmReference.TryGetTarget(out var fm);
            return fm;
        }
        set
        {
            var oldFm = FragmentManager;

            if (value == oldFm) return;

            if (value != oldFm && oldFm != null)
            {
                oldFm.UnregisterFragmentLifecycleCallbacks(Observer!);
                Observer = null;
            }

            if (value is null)
            {
                _fmReference = null;
                return;
            }

            _fmReference = new(value);
            var observer = new FragmentLifecycleCallback(() => value);
            Observer = observer;
            value.RegisterFragmentLifecycleCallbacks(observer, true);
        }
    }

    protected virtual partial void AttachToWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow)
    {
        HandleAccessibility(true, page, parentWindow.Page);

        var activity = parentWindow.Handler.PlatformView as Activity
            ?? throw new InvalidOperationException("Activity not found");

        var dv = activity.Window?.DecorView as ViewGroup
            ?? throw new InvalidOperationException("DecorView of Activity not found");

        var handler = (pageHandler as IPlatformViewHandler)!;

        var pv = handler.PlatformView!;

        void attachedHandler(object? s, ViewAttachedToWindowEventArgs e)
        {
            dv.Context!.HideKeyboard(dv);
        }
        pv.ViewAttachedToWindow += attachedHandler;

        void detachedHandler(object? s, ViewDetachedFromWindowEventArgs e)
        {
            dv.Context!.HideKeyboard(dv);
        }
        pv.ViewDetachedFromWindow += detachedHandler;

        var keyboardListener = new KeyboardListener();
        var viewTreeObserver = pv.ViewTreeObserver!;
        viewTreeObserver.AddOnGlobalLayoutListener(keyboardListener);

        void touchHandler(object? s, View.TouchEventArgs e)
        {
            var view = (s as ViewGroup)!;

            if (page.Content is not null && view.ChildCount > 0)
            {
                var child = view.GetChildAt(0)!;

                var rawx = e.Event!.GetX();
                var rawy = e.Event.GetY();
                var childx = child.GetX();
                var childy = child.GetY();

                if (rawx >= childx && rawx <= (child.Width + childx)
                    && rawy >= childy && rawy <= (child.Height + childy))
                {
                    if (keyboardListener.KeyboardVisible)
                    {
                        view.Context!.HideKeyboard(view);
                        view.FindFocus()?.ClearFocus();
                    }

                    e.Handled = true;
                    return;
                }
            }

            if (e.Event!.Action is MotionEventActions.Down)
            {
                if (!page.BackgroundInputTransparent && keyboardListener.KeyboardVisible)
                {
                    view.Context!.HideKeyboard(view);
                    view.FindFocus()?.ClearFocus();
                    e.Handled = true;
                    return;
                }
            }

            if (e.Event!.Action is MotionEventActions.Up)
            {
                page.SendBackgroundClick();
            }

            e.Handled = !page.BackgroundInputTransparent;
        }
        pv.Touch += touchHandler;

        var action = new DisposableAction(() =>
        {
            pv.ViewAttachedToWindow -= attachedHandler;
            pv.ViewDetachedFromWindow -= detachedHandler;
            pv.Touch -= touchHandler;
            
            if (viewTreeObserver.IsAlive)
                viewTreeObserver.RemoveOnGlobalLayoutListener(keyboardListener);
        });
        page.SetValue(DisposableActionAttached.DisposableActionProperty, action);

        AddToVisualTree(page, handler, dv, 10000);
    }

    protected virtual void AddToVisualTree(PopupPage page, IPlatformViewHandler handler, ViewGroup decorView, float elevation)
    {
        var view = !page.HasSystemPadding
            ? handler.PlatformView!
            : new ParentLayout(decorView.Context!, decorView, page);
        view.Elevation = elevation;

#if NET9_0_OR_GREATER
        FragmentManager = AndroidExtensions.GetFragmentManager(handler.MauiContext!);
        Observer!.AttachView(view);
#else
        decorView.AddView(view);
#endif
    }

    protected virtual partial void DetachFromWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow)
    {
        var handler = (pageHandler as IPlatformViewHandler)!;

        HandleAccessibility(false, page, parentWindow.Page);

        var action = page.GetValue(DisposableActionAttached.DisposableActionProperty) as DisposableAction;
        action?.Dispose();

        RemoveFromVisualTree(page, handler);
    }

    protected virtual void RemoveFromVisualTree(PopupPage page, IPlatformViewHandler handler)
    {
        View view;
        if (page.HasSystemPadding)
        {
            var layout = (handler.PlatformView!.Parent as ParentLayout)!;
            layout.RemoveGlobalLayoutListener();

            view = layout;
        }
        else
        {
            view = handler.PlatformView!;
        }
#if NET9_0_OR_GREATER
        Observer?.DetachView(view);
#else
        view.RemoveFromParent();
#endif
    }

    //! important keeps reference to pages that accessibility has applied to. This is so accessibility can be removed properly when popup is removed. #https://github.com/LuckyDucko/Mopups/issues/93
    protected Dictionary<PopupPage, Dictionary<ViewGroup, (ImportantForAccessibility, DescendantFocusability)>> AccessibilityViews = [];
    protected virtual void HandleAccessibility(bool showPopup, PopupPage popupPage, Page? mainPage)
    {
        if (!popupPage.EnableAndroidAccessibilityHandling || mainPage is null) return;

        if (showPopup)
        {
            Dictionary<ViewGroup, (ImportantForAccessibility, DescendantFocusability)> accessViews = [];

            // store previous accessibility settings
            if (mainPage.Handler?.PlatformView is ViewGroup pageView
                && pageView.ImportantForAccessibility is not ImportantForAccessibility.NoHideDescendants)
            {
                accessViews[pageView] = (pageView.ImportantForAccessibility, pageView.DescendantFocusability);
            }

            if (mainPage.Navigation.NavigationStack.Count > 0
                && mainPage.Navigation?.NavigationStack[^1]?.Handler?.PlatformView is ViewGroup navStackView
                && navStackView.ImportantForAccessibility is not ImportantForAccessibility.NoHideDescendants)
            {
                accessViews[navStackView] = (navStackView.ImportantForAccessibility, navStackView.DescendantFocusability);
            }

            if (mainPage.Navigation!.ModalStack.Count > 0
                && mainPage.Navigation?.ModalStack[^1]?.Handler?.PlatformView is ViewGroup modalStackView
                && modalStackView.ImportantForAccessibility is not ImportantForAccessibility.NoHideDescendants)
            {
                accessViews[modalStackView] = (modalStackView.ImportantForAccessibility, modalStackView.DescendantFocusability);
            }

            if (accessViews.Count > 0)
            {
                AccessibilityViews[popupPage] = accessViews;
            }
        }

        if (AccessibilityViews.TryGetValue(popupPage, out var views))
        {
            foreach (var view in views)
            {
                if (showPopup)
                {
                    view.Key.ImportantForAccessibility = ImportantForAccessibility.NoHideDescendants;
                    view.Key.DescendantFocusability = DescendantFocusability.BlockDescendants;
                    view.Key.ClearFocus();
                }
                else
                {
                    view.Key.ImportantForAccessibility = view.Value.Item1;
                    view.Key.DescendantFocusability = view.Value.Item2;
                }
            }

            if (!showPopup)
            {
                AccessibilityViews.Remove(popupPage);
            }
        }
    }
}