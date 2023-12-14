using Microsoft.UI.Xaml.Controls;

namespace MPowerKit.Popups;

public partial class PopupService
{
    protected virtual partial void AttachToWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow)
    {
        var content = (parentWindow.Handler.PlatformView as MauiWinUIWindow)?.Content as Panel
            ?? throw new InvalidOperationException("Window not found");

        var handler = pageHandler as IPlatformViewHandler;

        handler.PlatformView.PointerPressed += (s, e) =>
        {
            if (e.OriginalSource != handler.PlatformView)
            {
                e.Handled = true;
                return;
            }

            page.SendBackgroundClick();

            e.Handled = !page.BackgroundInputTransparent;
        };

        content.Children.Add(handler.PlatformView);
    }

    protected virtual partial void DetachFromWindow(PopupPage page, IViewHandler pageHandler, Window parentWindow)
    {
        var handler = pageHandler as IPlatformViewHandler;

        (handler.PlatformView.Parent as Panel)?.Children.Remove(handler.PlatformView);
    }
}