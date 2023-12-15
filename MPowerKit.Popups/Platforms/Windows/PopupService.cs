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

        var handler = pageHandler as IPlatformViewHandler;

        var inputPane = InputPaneInterop.GetForWindow(uiWindow.WindowHandle);

        handler.PlatformView.PointerPressed += (s, e) =>
        {
            if (inputPane.Visible)
            {
                inputPane.TryHide();
                var element = FocusManager.GetFocusedElement(handler.PlatformView.XamlRoot) as Control;
                element?.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
                return;
            }

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