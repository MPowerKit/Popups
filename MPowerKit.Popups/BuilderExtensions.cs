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