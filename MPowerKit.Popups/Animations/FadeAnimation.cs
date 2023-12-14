using MPowerKit.Popups.Animations.Base;

namespace MPowerKit.Popups.Animations;

public class FadeAnimation : BaseAnimation
{
    private double _defaultOpacity;

    public bool HasBackgroundAnimation { get; set; } = true;

    public override void Preparing(View content, PopupPage page)
    {
        if (HasBackgroundAnimation)
        {
            _defaultOpacity = page.Opacity;
            page.Opacity = 0;
        }
        else if (content is not null)
        {
            _defaultOpacity = content.Opacity;
            content.Opacity = 0;
        }
    }

    public override void Disposing(View content, PopupPage page)
    {
        if (!HasBackgroundAnimation && content is null) return;

        page.Opacity = _defaultOpacity;
    }

    public override Task Appearing(View content, PopupPage page)
    {
        if (double.IsNaN(_defaultOpacity)) _defaultOpacity = 1;

        if (HasBackgroundAnimation)
        {
            return page.FadeTo(_defaultOpacity, (uint)DurationIn.TotalMilliseconds, EasingIn);
        }
        if (content is not null)
        {
            return content.FadeTo(_defaultOpacity, (uint)DurationIn.TotalMilliseconds, EasingIn);
        }

        return Task.CompletedTask;
    }

    public override Task Disappearing(View content, PopupPage page)
    {
        if (HasBackgroundAnimation)
        {
            return page.FadeTo(0, (uint)DurationOut.TotalMilliseconds, EasingOut);
        }
        if (content is not null)
        {
            return content.FadeTo(0, (uint)DurationOut.TotalMilliseconds, EasingOut);
        }

        return Task.CompletedTask;
    }
}