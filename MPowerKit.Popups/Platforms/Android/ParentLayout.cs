using Android.Content;
using Android.Views;

using AndroidX.ConstraintLayout.Widget;

using Microsoft.Maui.Platform;

using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;

namespace MPowerKit.Popups;

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