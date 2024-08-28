using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Windows.UI.ViewManagement;

namespace MPowerKit.Popups;

public partial class PopupService
{
    protected virtual partial void AttachToWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow)
    {
        var uiWindow = parentWindow.Handler.PlatformView as MauiWinUIWindow;

        var content = uiWindow?.Content as Panel
            ?? throw new InvalidOperationException("Window not found");

        var handler = (pageHandler as IPlatformViewHandler)!;

        var inputPane = InputPaneInterop.GetForWindow(uiWindow!.WindowHandle);

        handler.PlatformView!.PointerPressed += (s, e) =>
        {
            if (inputPane.Visible)
            {
                inputPane.TryHide();
                var element = FocusManager.GetFocusedElement(handler.PlatformView.XamlRoot) as Control;
                element?.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
                return;
            }

            if ((e.OriginalSource as Microsoft.UI.Xaml.FrameworkElement) != handler.PlatformView)
            {
                e.Handled = true;
                return;
            }

            page.SendBackgroundClick();

            e.Handled = !page.BackgroundInputTransparent;
        };

        AddToVisualTree(page, handler, content);
    }

    protected virtual void AddToVisualTree(PopupPage page, IPlatformViewHandler handler, Panel windowContent)
    {
        var platform = handler.PlatformView!;

        if (page.BackgroundInputTransparent)
        {
            platform.Margin = new Microsoft.UI.Xaml.Thickness(
               page.Content.Margin.Left,
               page.Content.Margin.Top,
               page.Content.Margin.Right,
               page.Content.Margin.Bottom);
            page.Content.Margin = new Thickness(0);

            PageContentSizeChanged(page, EventArgs.Empty);

            page.Content.SizeChanged += PageContentSizeChanged;

            void PageContentSizeChanged(object? sender, EventArgs args)
            {
                var measured = (page.Content as IView).Measure(double.PositiveInfinity, double.PositiveInfinity);

                platform.HorizontalAlignment = page.Content.HorizontalOptions.ToPlatformHorizontal();
                platform.VerticalAlignment = page.Content.VerticalOptions.ToPlatformVertical();

                if (platform.HorizontalAlignment is not Microsoft.UI.Xaml.HorizontalAlignment.Stretch)
                {
                    platform.Width = measured.Width;
                }
                if (platform.VerticalAlignment is not Microsoft.UI.Xaml.VerticalAlignment.Stretch)
                {
                    platform.Height = measured.Height;
                }
            }
        }

        windowContent.Children.Add(platform);
    }

    protected virtual partial void DetachFromWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow)
    {
        var uiWindow = parentWindow.Handler.PlatformView as MauiWinUIWindow;

        var content = uiWindow?.Content as Panel
            ?? throw new InvalidOperationException("Window not found");

        var handler = (pageHandler as IPlatformViewHandler)!;

        RemoveFromVisualTree(page, handler, content);
    }

    protected virtual void RemoveFromVisualTree(PopupPage page, IPlatformViewHandler handler, Panel windowContent)
    {
        windowContent.Children.Remove(handler.PlatformView);
    }
}

public static class WinUIExtensions
{
    public static Microsoft.UI.Xaml.HorizontalAlignment ToPlatformHorizontal(this LayoutOptions options)
    {
        int i = 0;
        return i switch
        {
            _ when options == LayoutOptions.Start => Microsoft.UI.Xaml.HorizontalAlignment.Left,
            _ when options == LayoutOptions.Center => Microsoft.UI.Xaml.HorizontalAlignment.Center,
            _ when options == LayoutOptions.End => Microsoft.UI.Xaml.HorizontalAlignment.Right,
            _ when options == LayoutOptions.Fill => Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
            _ => throw new ArgumentOutOfRangeException(nameof(options), options, null)
        };
    }

    public static Microsoft.UI.Xaml.VerticalAlignment ToPlatformVertical(this LayoutOptions options)
    {
        int i = 0;
        return i switch
        {
            _ when options == LayoutOptions.Start => Microsoft.UI.Xaml.VerticalAlignment.Top,
            _ when options == LayoutOptions.Center => Microsoft.UI.Xaml.VerticalAlignment.Center,
            _ when options == LayoutOptions.End => Microsoft.UI.Xaml.VerticalAlignment.Bottom,
            _ when options == LayoutOptions.Fill => Microsoft.UI.Xaml.VerticalAlignment.Stretch,
            _ => throw new ArgumentOutOfRangeException(nameof(options), options, null)
        };
    }
}