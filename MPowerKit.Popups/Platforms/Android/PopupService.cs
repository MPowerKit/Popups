using Android.Content;
using Android.Widget;

using Microsoft.Maui.Platform;

using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;

namespace MPowerKit.Popups;

public partial class PopupService
{
    protected virtual partial void AttachToWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow)
    {
        HandleAccessibility(true, page.DisableAndroidAccessibilityHandling, parentWindow);

        var activity = parentWindow.Handler.PlatformView as Android.App.Activity;

        var dv = activity?.Window?.DecorView as ViewGroup
            ?? throw new InvalidOperationException("DecorView of Activity not found");

        var handler = (pageHandler as IPlatformViewHandler)!;

        handler.PlatformView!.ViewAttachedToWindow += (s, e) =>
        {
            dv.Context!.HideKeyboard(dv);
        };

        handler.PlatformView.ViewDetachedFromWindow += (s, e) =>
        {
            dv.Context!.HideKeyboard(dv);
        };

        bool keyboardVisible = false;

        handler.PlatformView!.ViewTreeObserver!.GlobalLayout += (s, e) =>
        {
            var view = dv.FindViewById(Android.Resource.Id.Content);

            var r = new Android.Graphics.Rect();
            view!.GetWindowVisibleDisplayFrame(r);
            int screenHeight = view.RootView!.Height;

            // r.bottom is the position above soft keypad or device button.
            // if keypad is shown, the r.bottom is smaller than that before.
            int keypadHeight = screenHeight - r.Bottom;

            if (keypadHeight > screenHeight * 0.15)
            {
                if (!keyboardVisible)
                {
                    keyboardVisible = true;
                }
            }
            else
            {
                if (keyboardVisible)
                {
                    keyboardVisible = false;
                }
            }
        };

        handler.PlatformView.Touch += (s, e) =>
        {
            var view = (s as ViewGroup)!;

            if (page.Content is not null && view.ChildCount > 0)
            {
                var child = view.GetChildAt(0)!;

                var rawx = e.Event!.RawX;
                var rawy = e.Event.RawY;
                var childx = child.GetX();
                var childy = child.GetY();

                if (rawx >= childx && rawx <= (child.Width + childx)
                    && rawy >= childy && rawy <= (child.Height + childy))
                {
                    if (keyboardVisible)
                    {
                        view.Context!.HideKeyboard(view);
                        view.FindFocus()?.ClearFocus();
                    }

                    e.Handled = true;
                    return;
                }
            }

            if (e.Event!.Action is Android.Views.MotionEventActions.Down)
            {
                if (!page.BackgroundInputTransparent && keyboardVisible)
                {
                    view.Context!.HideKeyboard(view);
                    view.FindFocus()?.ClearFocus();
                    e.Handled = true;
                    return;
                }

                page.SendBackgroundClick();
            }

            e.Handled = !page.BackgroundInputTransparent;
        };

        if (page.HasSystemPadding)
        {
            var pl = new ParentLayout(dv.Context!, dv, page);

            dv.AddView(pl);
        }
        else dv.AddView(handler.PlatformView);
    }

    protected virtual partial void DetachFromWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow)
    {
        var handler = (pageHandler as IPlatformViewHandler)!;

        HandleAccessibility(false, page.DisableAndroidAccessibilityHandling, parentWindow);

        if (page.HasSystemPadding)
        {
            (handler.PlatformView!.Parent as ParentLayout)!.RemoveFromParent();
        }
        else handler.PlatformView!.RemoveFromParent();
    }

    //! important keeps reference to pages that accessibility has applied to. This is so accessibility can be removed properly when popup is removed. #https://github.com/LuckyDucko/Mopups/issues/93
    private readonly List<View?> _accessibilityViews = [];
    void HandleAccessibility(bool showPopup, bool disableAccessibilityHandling, Window window)
    {
        if (disableAccessibilityHandling) return;

        if (showPopup)
        {
            var mainPage = window.Page;
            if (mainPage is null) return;

            _accessibilityViews.Add(mainPage.Handler?.PlatformView as View);

            if (mainPage.Navigation.NavigationStack.Count > 0)
            {
                _accessibilityViews.Add(mainPage.Navigation?.NavigationStack[^1]?.Handler?.PlatformView as View);
            }

            if (mainPage.Navigation!.ModalStack.Count > 0)
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
            ((ViewGroup)view).DescendantFocusability = showPopup
                ? Android.Views.DescendantFocusability.BlockDescendants
                : Android.Views.DescendantFocusability.AfterDescendants;
            view.ClearFocus();
        }
    }

    public class ParentLayout : RelativeLayout
    {
        private readonly ViewGroup _decorView;
        private readonly PopupPage _page;
        private readonly ViewGroup _platformView;
        private View _top;
        private View _bottom;
        private View _left;
        private View _right;

        public ParentLayout(Context context, ViewGroup decorView, PopupPage page) : base(context)
        {
            _decorView = decorView;
            _page = page;
            _platformView = (page.Handler!.PlatformView as ViewGroup)!;
            InitContent();

            page.PropertyChanged += Page_PropertyChanged;

            _decorView.ViewTreeObserver!.GlobalLayout += OnGlobalLayout;
        }

        private void InitContent()
        {
            _top = new View(Context) { Id = View.GenerateViewId() };
            _bottom = new View(Context) { Id = View.GenerateViewId() };
            _left = new View(Context) { Id = View.GenerateViewId() };
            _right = new View(Context) { Id = View.GenerateViewId() };
            this.AddView(_top);
            this.AddView(_bottom);
            this.AddView(_left);
            this.AddView(_right);
            this.AddView(_platformView);

            var color = _page.BackgroundColor.ToPlatform();

            _top.SetBackgroundColor(color);
            _bottom.SetBackgroundColor(color);
            _left.SetBackgroundColor(color);
            _right.SetBackgroundColor(color);

            var alpha = (float)_page.Opacity;

            _top.Alpha = alpha;
            _bottom.Alpha = alpha;
            _left.Alpha = alpha;
            _right.Alpha = alpha;

            Android.Graphics.Rect insets;
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var ins = _decorView.RootWindowInsets!.GetInsetsIgnoringVisibility(Android.Views.WindowInsets.Type.SystemBars());
                insets = new Android.Graphics.Rect(ins.Left, ins.Top, ins.Right, ins.Bottom);
            }
            else
            {
                var ins = _decorView.RootWindowInsets!;
                insets = new Android.Graphics.Rect(ins.StableInsetLeft, ins.StableInsetTop, ins.StableInsetRight, ins.StableInsetBottom);
            }

            var topParams = new LayoutParams(ViewGroup.LayoutParams.MatchParent, insets.Top);
            topParams.AddRule(LayoutRules.AlignParentTop);
            _top.LayoutParameters = topParams;

            var bottomParams = new LayoutParams(ViewGroup.LayoutParams.MatchParent, insets.Bottom);
            bottomParams.AddRule(LayoutRules.AlignParentBottom);
            _bottom.LayoutParameters = bottomParams;

            var leftParams = new LayoutParams(insets.Left, ViewGroup.LayoutParams.MatchParent);
            leftParams.AddRule(LayoutRules.AlignParentLeft);
            leftParams.AddRule(LayoutRules.Below, _top.Id);
            leftParams.AddRule(LayoutRules.Above, _bottom.Id);
            _left.LayoutParameters = leftParams;

            var rightParams = new LayoutParams(insets.Right, ViewGroup.LayoutParams.MatchParent);
            rightParams.AddRule(LayoutRules.AlignParentRight);
            rightParams.AddRule(LayoutRules.Below, _top.Id);
            rightParams.AddRule(LayoutRules.Above, _bottom.Id);
            _right.LayoutParameters = rightParams;

            var centerParams = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            centerParams.AddRule(LayoutRules.RightOf, _left.Id);
            centerParams.AddRule(LayoutRules.LeftOf, _right.Id);
            centerParams.AddRule(LayoutRules.Below, _top.Id);
            centerParams.AddRule(LayoutRules.Above, _bottom.Id);
            _platformView.LayoutParameters = centerParams;
        }

        private void Page_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Page.BackgroundColorProperty.PropertyName)
            {
                var color = _page.BackgroundColor.ToPlatform();

                _top.SetBackgroundColor(color);
                _bottom.SetBackgroundColor(color);
                _left.SetBackgroundColor(color);
                _right.SetBackgroundColor(color);
            }
            else if (e.PropertyName == Page.OpacityProperty.PropertyName)
            {
                var alpha = (float)_page.Opacity;

                _top.Alpha = alpha;
                _bottom.Alpha = alpha;
                _left.Alpha = alpha;
                _right.Alpha = alpha;
            }
        }

        public void OnGlobalLayout(object? sender, EventArgs args)
        {
            Android.Graphics.Rect insets;
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var ins = _decorView.RootWindowInsets!.GetInsetsIgnoringVisibility(Android.Views.WindowInsets.Type.SystemBars());
                insets = new Android.Graphics.Rect(ins.Left, ins.Top, ins.Right, ins.Bottom);
            }
            else
            {
                var ins = _decorView.RootWindowInsets!;
                insets = new Android.Graphics.Rect(ins.StableInsetLeft, ins.StableInsetTop, ins.StableInsetRight, ins.StableInsetBottom);
            }

            var topParams = _top.LayoutParameters!;
            topParams.Height = insets.Top;
            _top.LayoutParameters = topParams;

            var bottomParams = _bottom.LayoutParameters!;
            bottomParams.Height = insets.Bottom;
            _bottom.LayoutParameters = bottomParams;

            var leftParams = _left.LayoutParameters!;
            leftParams.Width = insets.Left;
            _left.LayoutParameters = leftParams;

            var rightParams = _right.LayoutParameters!;
            rightParams.Width = insets.Right;
            _right.LayoutParameters = rightParams;
        }
    }
}