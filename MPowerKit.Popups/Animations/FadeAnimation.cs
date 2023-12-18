using MPowerKit.Popups.Animations.Base;

namespace MPowerKit.Popups.Animations;

public class FadeAnimation : BaseAnimation
{
    private double _defaultOpacity;

#if IOS || MACCATALYST
    private Color? _backgroundColor;
#endif

    public bool HasBackgroundAnimation { get; set; } = true;

    public override void Preparing(View content, PopupPage page)
    {
        if (HasBackgroundAnimation)
        {
            _defaultOpacity = page.Opacity;
            page.Opacity = 0;

#if IOS || MACCATALYST
            _backgroundColor = page.BackgroundColor;

            page.BackgroundColor = GetColor(0);
#endif
        }
        else if (content is not null)
        {
            _defaultOpacity = content.Opacity;
            content.Opacity = 0;
        }
    }

    public override void Disposing(View content, PopupPage page)
    {
        if (HasBackgroundAnimation)
        {
            page.Opacity = _defaultOpacity;
#if IOS || MACCATALYST
            page.BackgroundColor = _backgroundColor;
#endif
        }
        else if (content is not null)
        {
            content.Opacity = _defaultOpacity;
        }
    }

    public override Task Appearing(View content, PopupPage page)
    {
        if (double.IsNaN(_defaultOpacity)) _defaultOpacity = 1;

        List<Task> tasks = [];

        if (HasBackgroundAnimation)
        {
#if IOS || MACCATALYST
            var tcs = new TaskCompletionSource();

            page.Animate("backgroundFadeIn",
                d =>
                {
                    page.BackgroundColor = GetColor((float)d);
                }, 0, _backgroundColor.Alpha, length: (uint)DurationIn.TotalMilliseconds, easing: EasingIn,
                finished: (d, b) =>
                {
                    tcs.SetResult();
                });

            tasks.Add(tcs.Task);
#endif
            tasks.Add(page.FadeTo(_defaultOpacity, (uint)DurationIn.TotalMilliseconds, EasingIn));
        }
        if (content is not null)
        {
            tasks.Add(content.FadeTo(_defaultOpacity, (uint)DurationIn.TotalMilliseconds, EasingIn));
        }

        return Task.WhenAll(tasks);
    }

    public override Task Disappearing(View content, PopupPage page)
    {
        List<Task> tasks = [];

        if (HasBackgroundAnimation)
        {
#if IOS || MACCATALYST
            var tcs = new TaskCompletionSource();

            _backgroundColor = page.BackgroundColor;

            page.Animate("backgroundFadeOut",
                d =>
                {
                    page.BackgroundColor = GetColor((float)d);
                }, _backgroundColor.Alpha, 0, length: (uint)DurationOut.TotalMilliseconds, easing: EasingOut,
                finished: (d, b) =>
                {
                    tcs.SetResult();
                });

            tasks.Add(tcs.Task);
#endif
            tasks.Add(page.FadeTo(0, (uint)DurationOut.TotalMilliseconds, EasingOut));
        }
        if (content is not null)
        {
            tasks.Add(content.FadeTo(0, (uint)DurationOut.TotalMilliseconds, EasingOut));
        }

        return Task.WhenAll(tasks);
    }

#if IOS || MACCATALYST
    private Color? GetColor(float transparent)
    {
        return _backgroundColor?.WithAlpha(transparent);
    }
#endif
}