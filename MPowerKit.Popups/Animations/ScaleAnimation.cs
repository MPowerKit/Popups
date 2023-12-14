namespace MPowerKit.Popups.Animations;

public class ScaleAnimation : FadeAnimation
{
    private double _defaultScale;
    private double _defaultOpacity;
    private double _defaultTranslationX;
    private double _defaultTranslationY;

    public double ScaleIn { get; set; } = 0.8;
    public double ScaleOut { get; set; } = 0.8;

    public MoveAnimationOptions PositionIn { get; set; }
    public MoveAnimationOptions PositionOut { get; set; }

    public ScaleAnimation() : this(MoveAnimationOptions.Center, MoveAnimationOptions.Center) { }

    public ScaleAnimation(MoveAnimationOptions positionIn, MoveAnimationOptions positionOut)
    {
        PositionIn = positionIn;
        PositionOut = positionOut;
        EasingIn = Easing.SinOut;
        EasingOut = Easing.SinIn;

        if (PositionIn is not MoveAnimationOptions.Center) DurationIn = TimeSpan.FromMilliseconds(500);
        if (PositionOut is not MoveAnimationOptions.Center) DurationOut = TimeSpan.FromMilliseconds(500);
    }

    public override void Preparing(View content, PopupPage page)
    {
        if (HasBackgroundAnimation) base.Preparing(content, page);

        HidePage(page);

        if (content == null) return;

        UpdateDefaultProperties(content);

        if (!HasBackgroundAnimation) content.Opacity = 0;
    }

    public override void Disposing(View content, PopupPage page)
    {
        if (HasBackgroundAnimation) base.Disposing(content, page);

        ShowPage(page);

        if (content == null) return;

        content.Scale = _defaultScale;
        content.Opacity = _defaultOpacity;
        content.TranslationX = _defaultTranslationX;
        content.TranslationY = _defaultTranslationY;
    }

    public override Task Appearing(View content, PopupPage page)
    {
        List<Task> taskList = [base.Appearing(content, page)];

        if (content is not null)
        {
            var topOffset = GetTopOffset(content, page) * ScaleIn;
            var leftOffset = GetLeftOffset(content, page) * ScaleIn;

            taskList.Add(Scale(content, EasingIn, ScaleIn, _defaultScale, true));

            if (PositionIn.HasFlag(MoveAnimationOptions.Top))
            {
                content.TranslationY = -topOffset;
            }
            else if (PositionIn.HasFlag(MoveAnimationOptions.Bottom))
            {
                content.TranslationY = topOffset;
            }

            if (PositionIn.HasFlag(MoveAnimationOptions.Left))
            {
                content.TranslationX = -leftOffset;
            }
            else if (PositionIn.HasFlag(MoveAnimationOptions.Right))
            {
                content.TranslationX = leftOffset;
            }

            if (PositionIn is not MoveAnimationOptions.Center)
            {
                taskList.Add(content.TranslateTo(_defaultTranslationX, _defaultTranslationY, (uint)DurationIn.TotalMilliseconds, EasingIn));
            }
        }

        ShowPage(page);

        return Task.WhenAll(taskList);
    }

    public async override Task Disappearing(View content, PopupPage page)
    {
        List<Task> taskList = [base.Disappearing(content, page)];

        if (content is not null)
        {
            UpdateDefaultProperties(content);

            var topOffset = GetTopOffset(content, page) * ScaleOut;
            var leftOffset = GetLeftOffset(content, page) * ScaleOut;

            var translationX = _defaultTranslationX;
            var translationY = _defaultTranslationY;

            taskList.Add(Scale(content, EasingOut, _defaultScale, ScaleOut, false));

            if (PositionOut.HasFlag(MoveAnimationOptions.Top))
            {
                translationY = -topOffset;
            }
            else if (PositionOut.HasFlag(MoveAnimationOptions.Bottom))
            {
                translationY = topOffset;
            }

            if (PositionOut == MoveAnimationOptions.Left)
            {
                translationX = -leftOffset;
            }
            else if (PositionOut == MoveAnimationOptions.Right)
            {
                translationX = leftOffset;
            }

            taskList.Add(content.TranslateTo(translationX, translationY, (uint)DurationOut.TotalMilliseconds, EasingOut));
        }

        await Task.WhenAll(taskList);

        HidePage(page);
    }

    private Task Scale(View content, Easing easing, double start, double end, bool isAppearing)
    {
        var task = new TaskCompletionSource();

        var duration = (uint)(isAppearing ? DurationIn.TotalMilliseconds : DurationOut.TotalMilliseconds);

        content.Animate("popIn",
            d =>
            {
                content.Scale = double.IsNaN(d) ? 1 : d;
            }, start, end, easing: easing, length: duration,
            finished: (d, b) =>
            {
                task.SetResult();
            });

        return task.Task;
    }

    private void UpdateDefaultProperties(View content)
    {
        _defaultScale = content.Scale;
        _defaultOpacity = content.Opacity;

        if (double.IsNaN(_defaultOpacity)) _defaultOpacity = 1;

        _defaultTranslationX = content.TranslationX;
        _defaultTranslationY = content.TranslationY;
    }
}