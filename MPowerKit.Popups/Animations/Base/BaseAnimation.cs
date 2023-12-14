using System.ComponentModel;
using System.Globalization;
using System.Reflection;

using MPowerKit.Popups.Interfaces;

namespace MPowerKit.Popups.Animations.Base;

public class EasingTypeConverter : TypeConverter
{
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not null)
        {
            var fieldInfo = typeof(Easing).GetRuntimeFields()?
                .FirstOrDefault(fi => fi.IsStatic && fi.Name == value.ToString());

            var fieldValue = fieldInfo?.GetValue(null);
            if (fieldValue is not null) return (Easing)fieldValue;
        }

        throw new InvalidOperationException($"Cannot convert \"{value}\" into {typeof(Easing)}");
    }
}

public abstract class BaseAnimation : IPopupAnimation
{
    private static readonly TimeSpan _defaultDuration = TimeSpan.FromMilliseconds(200);

    public TimeSpan DurationIn { get; set; } = _defaultDuration;

    public TimeSpan DurationOut { get; set; } = _defaultDuration;

    [TypeConverter(typeof(EasingTypeConverter))]
    public Easing EasingIn { get; set; } = Easing.Linear;

    [TypeConverter(typeof(EasingTypeConverter))]
    public Easing EasingOut { get; set; } = Easing.Linear;

    public abstract void Preparing(View content, PopupPage page);

    public abstract void Disposing(View content, PopupPage page);

    public abstract Task Appearing(View content, PopupPage page);

    public abstract Task Disappearing(View content, PopupPage page);

    protected virtual double GetTopOffset(View content, Page page)
    {
        return (content.Height + page.Height) / 2.0;
    }

    protected virtual double GetLeftOffset(View content, Page page)
    {
        return (content.Width + page.Width) / 2.0;
    }

    protected virtual void HidePage(Page page)
    {
        page.Opacity = 0;
    }

    protected virtual void ShowPage(Page page)
    {
        page.Dispatcher.Dispatch(() => page.Opacity = 1);
    }
}