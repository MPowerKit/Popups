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

    public override async ValueTask Appearing(View content, PopupPage page)
    {
        if (double.IsNaN(_defaultOpacity)) _defaultOpacity = 1;

        if (HasBackgroundAnimation)
        {
            List<Task> tasks = [];
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

            await Task.WhenAll(tasks);
        }
        if (content is not null)
        {
            await content.FadeTo(_defaultOpacity, (uint)DurationIn.TotalMilliseconds, EasingIn);
        }
    }

    public override async ValueTask Disappearing(View content, PopupPage page)
    {
        if (HasBackgroundAnimation)
        {
            List<Task> tasks = [];
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

            await Task.WhenAll(tasks);
        }
        if (content is not null)
        {
            await content.FadeTo(0, (uint)DurationOut.TotalMilliseconds, EasingOut);
        }
    }

#if IOS || MACCATALYST
    private Color? GetColor(float transparent)
    {
        return _backgroundColor?.WithAlpha(transparent);
    }
#endif
}