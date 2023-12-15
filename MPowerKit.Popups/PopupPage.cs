using System.Windows.Input;

using MPowerKit.Popups.Animations;
using MPowerKit.Popups.Interfaces;

namespace MPowerKit.Popups;

public static class AnimationHelper
{
    public static bool SystemAnimationsEnabled
    {
        get
        {
#if ANDROID
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                return Android.Animation.ValueAnimator.AreAnimatorsEnabled();
            }
#endif

            return true;
        }
    }
}

public class RoutedEventArgs : EventArgs
{
    public bool Handled { get; set; }
}

public class PopupPage : ContentPage
{
    public event EventHandler<RoutedEventArgs>? BackgroundClicked;

    public PopupPage()
    {
        BackgroundColor = Color.FromArgb("#50000000");
    }

    protected override bool OnBackButtonPressed()
    {
        return false;
    }

    internal void PreparingAnimation()
    {
        if (!IsAnimationEnabled) return;

        Animation?.Preparing(Content, this);
    }

    internal void DisposingAnimation()
    {
        if (!IsAnimationEnabled) return;

        Animation?.Disposing(Content, this);
    }

    internal async Task AppearingAnimation()
    {
        OnAppearingAnimationBegin();
        await OnAppearingAnimationBeginAsync();

        if (IsAnimationEnabled && Animation is not null)
        {
            await Animation.Appearing(Content, this);
        }

        OnAppearingAnimationEnd();
        await OnAppearingAnimationEndAsync();
    }

    internal async Task DisappearingAnimation()
    {
        OnDisappearingAnimationBegin();
        await OnDisappearingAnimationBeginAsync();

        if (IsAnimationEnabled && Animation is not null)
        {
            await Animation.Disappearing(Content, this);
        }

        OnDisappearingAnimationEnd();
        await OnDisappearingAnimationEndAsync();
    }

    protected virtual void OnAppearingAnimationBegin()
    {
    }

    protected virtual void OnAppearingAnimationEnd()
    {
    }

    protected virtual void OnDisappearingAnimationBegin()
    {
    }

    protected virtual void OnDisappearingAnimationEnd()
    {
    }

    protected virtual Task OnAppearingAnimationBeginAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnAppearingAnimationEndAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnDisappearingAnimationBeginAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnDisappearingAnimationEndAsync()
    {
        return Task.CompletedTask;
    }

    public virtual void OnBackgroundCliked()
    {

    }

    public virtual void SendBackgroundClick()
    {
        OnBackgroundCliked();

        BackgroundClicked?.Invoke(this, new RoutedEventArgs());

        if (BackgroundClickedCommand?.CanExecute(BackgroundClickedCommandParameter) is true)
        {
            BackgroundClickedCommand.Execute(BackgroundClickedCommandParameter);
        }
    }

    #region IsAnimationEnabled
    public bool IsAnimationEnabled
    {
        get { return (bool)GetValue(IsAnimationEnabledProperty); }
        set { SetValue(IsAnimationEnabledProperty, value); }
    }

    public static readonly BindableProperty IsAnimationEnabledProperty =
        BindableProperty.Create(
            nameof(IsAnimationEnabled),
            typeof(bool),
            typeof(PopupPage),
            true);
    #endregion

    #region HasSystemPadding
    public bool HasSystemPadding
    {
        get { return (bool)GetValue(HasSystemPaddingProperty); }
        set { SetValue(HasSystemPaddingProperty, value); }
    }

    public static readonly BindableProperty HasSystemPaddingProperty =
        BindableProperty.Create(
            nameof(HasSystemPadding),
            typeof(bool),
            typeof(PopupPage),
            true
            );
    #endregion

    #region Animation
    public IPopupAnimation Animation
    {
        get { return (IPopupAnimation)GetValue(AnimationProperty); }
        set { SetValue(AnimationProperty, value); }
    }

    public static readonly BindableProperty AnimationProperty =
        BindableProperty.Create(
            nameof(Animation),
            typeof(IPopupAnimation),
            typeof(PopupPage),
            new ScaleAnimation()
            );
    #endregion

    #region CloseOnBackgroundClick
    public bool CloseOnBackgroundClick
    {
        get { return (bool)GetValue(CloseOnBackgroundClickProperty); }
        set { SetValue(CloseOnBackgroundClickProperty, value); }
    }

    public static readonly BindableProperty CloseOnBackgroundClickProperty =
        BindableProperty.Create(
            nameof(CloseOnBackgroundClick),
            typeof(bool),
            typeof(PopupPage)
            );
    #endregion

    #region BackgroundInputTransparent
    public bool BackgroundInputTransparent
    {
        get { return (bool)GetValue(BackgroundInputTransparentProperty); }
        set { SetValue(BackgroundInputTransparentProperty, value); }
    }

    public static readonly BindableProperty BackgroundInputTransparentProperty =
        BindableProperty.Create(
            nameof(BackgroundInputTransparent),
            typeof(bool),
            typeof(PopupPage)
            );
    #endregion

    #region BackgroundClickedCommand
    public ICommand BackgroundClickedCommand
    {
        get { return (ICommand)GetValue(BackgroundClickedCommandProperty); }
        set { SetValue(BackgroundClickedCommandProperty, value); }
    }

    public static readonly BindableProperty BackgroundClickedCommandProperty =
        BindableProperty.Create(
            nameof(BackgroundClickedCommand),
            typeof(ICommand),
            typeof(PopupPage)
            );
    #endregion

    #region BackgroundClickedCommandParameter
    public object BackgroundClickedCommandParameter
    {
        get { return (object)GetValue(BackgroundClickedCommandParameterProperty); }
        set { SetValue(BackgroundClickedCommandParameterProperty, value); }
    }

    public static readonly BindableProperty BackgroundClickedCommandParameterProperty =
        BindableProperty.Create(
            nameof(BackgroundClickedCommandParameter),
            typeof(object),
            typeof(PopupPage)
            );
    #endregion

    #region DisableAndroidAccessibilityHandling
    public bool DisableAndroidAccessibilityHandling
    {
        get { return (bool)GetValue(DisableAndroidAccessibilityHandlingProperty); }
        set { SetValue(DisableAndroidAccessibilityHandlingProperty, value); }
    }

    public static readonly BindableProperty DisableAndroidAccessibilityHandlingProperty =
        BindableProperty.Create(
            nameof(DisableAndroidAccessibilityHandling),
            typeof(bool),
            typeof(PopupPage)
            );
    #endregion
}