namespace MPowerKit.Popups.Animations;

public class FadeAnimation : FadeBackgroundAnimation
{
    private double _defaultOpacity;

    public override void Preparing(View? content, PopupPage page)
    {
        base.Preparing(content, page);

        if (content is null) return;

        _defaultOpacity = content.Opacity;
        content.Opacity = 0;
    }

    public override void Disposing(View? content, PopupPage page)
    {
        base.Disposing(content, page);

        if (content is null) return;

        content.Opacity = _defaultOpacity;
    }

    public override Task Appearing(View? content, PopupPage page)
    {
        List<Task> tasks = [base.Appearing(content, page)];

        if (double.IsNaN(_defaultOpacity)) _defaultOpacity = 1;

        if (content is not null)
        {
            tasks.Add(content.FadeTo(_defaultOpacity, (uint)DurationIn.TotalMilliseconds, EasingIn));
        }

        return Task.WhenAll(tasks);
    }

    public override Task Disappearing(View? content, PopupPage page)
    {
        List<Task> tasks = [base.Disappearing(content, page)];

        if (content is not null)
        {
            tasks.Add(content.FadeTo(0, (uint)DurationOut.TotalMilliseconds, EasingOut));
        }

        return Task.WhenAll(tasks);
    }
}