namespace MPowerKit.Popups.Animations;

public class MoveAnimation : FadeBackgroundAnimation
{
    private double _defaultTranslationX;
    private double _defaultTranslationY;

    public MoveAnimationOptions PositionIn { get; set; }
    public MoveAnimationOptions PositionOut { get; set; }

    public MoveAnimation() : this(MoveAnimationOptions.Bottom, MoveAnimationOptions.Bottom) { }

    public MoveAnimation(MoveAnimationOptions positionIn, MoveAnimationOptions positionOut)
    {
        PositionIn = positionIn;
        PositionOut = positionOut;

        DurationIn = DurationOut = TimeSpan.FromMilliseconds(300);
        EasingIn = Easing.SinOut;
        EasingOut = Easing.SinIn;
    }

    public override void Preparing(View content, PopupPage page)
    {
        base.Preparing(content, page);

        if (content is null) return;

        UpdateDefaultTranslations(content);
    }

    public override void Disposing(View content, PopupPage page)
    {
        base.Disposing(content, page);

        if (content is null) return;

        content.TranslationX = _defaultTranslationX;
        content.TranslationY = _defaultTranslationY;
    }

    public override Task Appearing(View content, PopupPage page)
    {
        List<Task> taskList = [base.Appearing(content, page)];

        if (content is not null)
        {
            var topOffset = GetTopOffset(content, page);
            var leftOffset = GetLeftOffset(content, page);

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

            taskList.Add(content.TranslateTo(_defaultTranslationX, _defaultTranslationY, (uint)DurationIn.TotalMilliseconds, EasingIn));
        }

        return Task.WhenAll(taskList);
    }

    public override Task Disappearing(View content, PopupPage page)
    {
        List<Task> taskList = [base.Disappearing(content, page)];

        if (content is not null)
        {
            UpdateDefaultTranslations(content);

            var topOffset = GetTopOffset(content, page);
            var leftOffset = GetLeftOffset(content, page);

            var translationX = _defaultTranslationX;
            var translationY = _defaultTranslationY;

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

        return Task.WhenAll(taskList);
    }

    private void UpdateDefaultTranslations(View content)
    {
        _defaultTranslationX = content.TranslationX;
        _defaultTranslationY = content.TranslationY;
    }
}