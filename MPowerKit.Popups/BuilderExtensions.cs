#if ANDROID
using Microsoft.Maui.LifecycleEvents;

using MPowerKit.Popups.Interfaces;
#endif

namespace MPowerKit.Popups;

public static class BuilderExtensions
{
    public static MauiAppBuilder UseMPowerKitPopups(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton(_ => PopupService.Current);
#if ANDROID
        builder
            .ConfigureLifecycleEvents(static lifecycle =>
            {
                lifecycle.AddAndroid(static d =>
                {
                    d.OnBackPressed(static activity =>
                    {
                        var popupService = IPlatformApplication.Current!.Services.GetService<IPopupService>();

                        if (popupService is null || popupService.PopupStack.Count == 0) return false;

                        try
                        {
                            if (popupService.PopupStack[^1].SendBackButtonPressed()) return true;

                            popupService.HidePopupAsync(popupService.PopupStack[^1], true);
                            return true;
                        }
                        catch
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