﻿using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Views;

using AndroidX.Activity;
using AndroidX.ConstraintLayout.Widget;

using Microsoft.Maui.Platform;

using static Android.Views.View;

using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
using Window = Microsoft.Maui.Controls.Window;

namespace MPowerKit.Popups;

public partial class PopupService
{
    protected virtual partial void AttachToWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow)
    {
        HandleAccessibility(true, page, parentWindow.Page);

        var activity = parentWindow.Handler.PlatformView as Activity
            ?? throw new InvalidOperationException("Activity not found");

        var dv = activity.Window?.DecorView as ViewGroup
            ?? throw new InvalidOperationException("DecorView of Activity not found");

        var handler = (pageHandler as IPlatformViewHandler)!;

        void attachedHandler(object? s, ViewAttachedToWindowEventArgs e)
        {
            dv.Context!.HideKeyboard(dv);
        }
        handler.PlatformView!.ViewAttachedToWindow += attachedHandler;

        void detachedHandler(object? s, ViewDetachedFromWindowEventArgs e)
        {
            dv.Context!.HideKeyboard(dv);
        }
        handler.PlatformView.ViewDetachedFromWindow += detachedHandler;

        bool keyboardVisible = false;

        void globalLayoutHandler(object? s, EventArgs e)
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
        }
        handler.PlatformView!.ViewTreeObserver!.GlobalLayout += globalLayoutHandler;

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
                    if (keyboardVisible)
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
                if (!page.BackgroundInputTransparent && keyboardVisible)
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
        handler.PlatformView.Touch += touchHandler;

        var action = new DisposableAction(() =>
        {
            handler.PlatformView!.ViewAttachedToWindow -= attachedHandler;
            handler.PlatformView.ViewDetachedFromWindow -= detachedHandler;
            handler.PlatformView.ViewTreeObserver!.GlobalLayout -= globalLayoutHandler;
            handler.PlatformView.Touch -= touchHandler;
        });
        page.SetValue(DisposableActionAttached.DisposableActionProperty, action);

#if NET9_0_OR_GREATER
        AddToVisualTree(page, handler, dv, 10000, () => BackPressCallback(page, activity));
#else
        AddToVisualTree(page, handler, dv, 10000);
#endif
    }

    protected virtual async void BackPressCallback(PopupPage page, Activity activity)
    {
        try
        {
            if (page.SendBackButtonPressed()) return;

            await this.HidePopupAsync(page, true);
        }
        catch
        {
            activity.OnBackPressed();
        }
    }

    protected virtual void AddToVisualTree(PopupPage page, IPlatformViewHandler handler, ViewGroup decorView, float elevation
#if NET9_0_OR_GREATER
        , Action backPressCallback
#endif
        )
    {
        var view = !page.HasSystemPadding
            ? handler.PlatformView!
            : new ParentLayout(decorView.Context!, decorView, page);

#if NET9_0_OR_GREATER
        var d = new PopupComponentDialog(Platform.CurrentActivity!, Resource.Style.Maui_MainTheme_NoActionBar, backPressCallback);
        d.Window!.SetBackgroundDrawable(new ColorDrawable(Android.Graphics.Color.Transparent));
        d.Window!.AddFlags(WindowManagerFlags.LayoutNoLimits);
        d.SetContentView(view);
        d.Show();
        PagePopupDialogAttached.SetPopupDialog(page, d);
#else
        view.Elevation = elevation;
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
#if NET8_0
        View view;
#endif

        if (page.HasSystemPadding)
        {
            var layout = (handler.PlatformView!.Parent as ParentLayout)!;
            layout.RemoveGlobalLayoutListener();
#if NET8_0
            view = layout;
        }
        else
        {
            view = handler.PlatformView!;
        }

        view.RemoveFromParent();
#else
        }

        var d = PagePopupDialogAttached.GetPopupDialog(page);
        PagePopupDialogAttached.SetPopupDialog(page, null);
        d.Dismiss();
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

    public class PopupComponentDialog : ComponentDialog
    {
        public PopupComponentDialog(Context context, int themeResId, Action backPress) : base(context, themeResId)
        {
            this.OnBackPressedDispatcher.AddCallback(new CallBack(true, this, backPress));
        }

        public class CallBack : OnBackPressedCallback
        {
            private readonly WeakReference<ComponentDialog> _customComponentDialog;
            private readonly Action _backPress;

            public CallBack(bool enabled, ComponentDialog customComponentDialog, Action backPress) : base(enabled)
            {
                _customComponentDialog = new(customComponentDialog);
                _backPress = backPress;
            }

            public override void HandleOnBackPressed()
            {
                if (!_customComponentDialog.TryGetTarget(out var customComponentDialog) ||
                    customComponentDialog.Context.GetActivity() is not Activity activity)
                {
                    return;
                }

                _backPress?.Invoke();
            }
        }
    }

    public class ParentLayout : ConstraintLayout, ViewTreeObserver.IOnGlobalLayoutListener
    {
        private readonly ViewGroup _decorView;
        private readonly PopupPage _page;
        private readonly ViewGroup _platformView;
        private View? _top;
        private View? _bottom;
        private View? _left;
        private View? _right;
        private Android.Graphics.Rect? _prevInsets;

        public ParentLayout(Context context, ViewGroup decorView, PopupPage page) : base(context)
        {
            _decorView = decorView;
            _page = page;
            _platformView = (page.Handler!.PlatformView as ViewGroup)!;

            InitContent();

            _page.PropertyChanged += Page_PropertyChanged;

            _decorView.ViewTreeObserver!.AddOnGlobalLayoutListener(this);
        }

        public void RemoveGlobalLayoutListener()
        {
            _page.PropertyChanged -= Page_PropertyChanged;
            _decorView.ViewTreeObserver!.RemoveOnGlobalLayoutListener(this);
        }

        private void InitContent()
        {
            _top = new View(Context) { Id = View.GenerateViewId() };
            _bottom = new View(Context) { Id = View.GenerateViewId() };
            _left = new View(Context) { Id = View.GenerateViewId() };
            _right = new View(Context) { Id = View.GenerateViewId() };
            _platformView.Id = View.GenerateViewId();

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

            _prevInsets = insets;

            var topParams = new LayoutParams(LayoutParams.MatchParent, insets.Top);
            _top.LayoutParameters = topParams;

            var bottomParams = new LayoutParams(LayoutParams.MatchParent, insets.Bottom);
            _bottom.LayoutParameters = bottomParams;

            var leftParams = new LayoutParams(insets.Left, LayoutParams.MatchConstraint);
            _left.LayoutParameters = leftParams;

            var rightParams = new LayoutParams(insets.Right, LayoutParams.MatchConstraint);
            _right.LayoutParameters = rightParams;

            var centerParams = new LayoutParams(LayoutParams.MatchConstraint, LayoutParams.MatchConstraint);
            _platformView.LayoutParameters = centerParams;

            ParentLayout.ToggleViewVisiblility(_top, insets.Top);
            ParentLayout.ToggleViewVisiblility(_right, insets.Right);
            ParentLayout.ToggleViewVisiblility(_bottom, insets.Bottom);
            ParentLayout.ToggleViewVisiblility(_left, insets.Left);

            this.AddView(_top);
            this.AddView(_bottom);
            this.AddView(_left);
            this.AddView(_right);
            this.AddView(_platformView);

            var set = new ConstraintSet();
            set.Clone(this);

            set.Connect(_top.Id, ConstraintSet.Top, ConstraintSet.ParentId, ConstraintSet.Top);

            set.Connect(_bottom.Id, ConstraintSet.Bottom, ConstraintSet.ParentId, ConstraintSet.Bottom);

            set.Connect(_left.Id, ConstraintSet.Left, ConstraintSet.ParentId, ConstraintSet.Left);
            set.Connect(_left.Id, ConstraintSet.Top, _top.Id, ConstraintSet.Bottom);
            set.Connect(_left.Id, ConstraintSet.Bottom, _bottom.Id, ConstraintSet.Top);

            set.Connect(_right.Id, ConstraintSet.Right, ConstraintSet.ParentId, ConstraintSet.Right);
            set.Connect(_right.Id, ConstraintSet.Top, _top.Id, ConstraintSet.Bottom);
            set.Connect(_right.Id, ConstraintSet.Bottom, _bottom.Id, ConstraintSet.Top);

            set.Connect(_platformView.Id, ConstraintSet.Left, _left.Id, ConstraintSet.Right);
            set.Connect(_platformView.Id, ConstraintSet.Top, _top.Id, ConstraintSet.Bottom);
            set.Connect(_platformView.Id, ConstraintSet.Bottom, _bottom.Id, ConstraintSet.Top);
            set.Connect(_platformView.Id, ConstraintSet.Right, _right.Id, ConstraintSet.Left);

            set.ApplyTo(this);
        }

        private void Page_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Page.BackgroundColorProperty.PropertyName)
            {
                var color = _page.BackgroundColor.ToPlatform();

                _top!.SetBackgroundColor(color);
                _bottom!.SetBackgroundColor(color);
                _left!.SetBackgroundColor(color);
                _right!.SetBackgroundColor(color);
            }
            else if (e.PropertyName == Page.OpacityProperty.PropertyName)
            {
                var alpha = (float)_page.Opacity;

                _top!.Alpha = alpha;
                _bottom!.Alpha = alpha;
                _left!.Alpha = alpha;
                _right!.Alpha = alpha;
            }
        }

        public void OnGlobalLayout()
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

            if (_prevInsets!.Top == insets.Top && _prevInsets.Bottom == insets.Bottom
                && _prevInsets.Left == insets.Left && _prevInsets.Right == insets.Right) return;

            _prevInsets = insets;

            var topParams = _top!.LayoutParameters!;
            topParams.Height = insets.Top;
            _top.LayoutParameters = topParams;

            var bottomParams = _bottom!.LayoutParameters!;
            bottomParams.Height = insets.Bottom;
            _bottom.LayoutParameters = bottomParams;

            var leftParams = _left!.LayoutParameters!;
            leftParams.Width = insets.Left;
            _left.LayoutParameters = leftParams;

            var rightParams = _right!.LayoutParameters!;
            rightParams.Width = insets.Right;
            _right.LayoutParameters = rightParams;

            ToggleViewVisiblility(_top, insets.Top);
            ToggleViewVisiblility(_right, insets.Right);
            ToggleViewVisiblility(_bottom, insets.Bottom);
            ToggleViewVisiblility(_left, insets.Left);
        }

        private static void ToggleViewVisiblility(View view, int size)
        {
            view.Visibility = size > 0 ? ViewStates.Visible : ViewStates.Gone;
        }
    }
}

public static class PagePopupDialogAttached
{
    #region ServiceScope
    public static readonly BindableProperty PopupDialogProperty =
        BindableProperty.CreateAttached(
            "PopupDialog",
            typeof(Dialog),
            typeof(PagePopupDialogAttached),
            null);

    public static Dialog GetPopupDialog(BindableObject view) => (Dialog)view.GetValue(PopupDialogProperty);

    public static void SetPopupDialog(BindableObject view, Dialog value) => view.SetValue(PopupDialogProperty, value);
    #endregion
}