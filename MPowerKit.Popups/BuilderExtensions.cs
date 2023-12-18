using Microsoft.Maui.LifecycleEvents;

using MPowerKit.Popups.Interfaces;

namespace MPowerKit.Popups;

public static class BuilderExtensions
{
    public static MauiAppBuilder UseMPowerKitPopups(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton(PopupService.Current);
#if ANDROID
        builder
            .ConfigureLifecycleEvents(lifecycle =>
            {
                lifecycle.AddAndroid(d =>
                {
                    d.OnBackPressed(activity =>
                    {
                        var popupService = Application.Current.Handler.MauiContext.Services.GetService<IPopupService>();

                        if (popupService is null || PopupService.Current.PopupStack.Count == 0) return false;

                        try
                        {
                            if (PopupService.Current.PopupStack[^1].SendBackButtonPressed()) return true;

                            popupService.HidePopupAsync(PopupService.Current.PopupStack[^1], true);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            return false;
                        }
                    });
                });
            });
#endif
        return builder;
    }
}