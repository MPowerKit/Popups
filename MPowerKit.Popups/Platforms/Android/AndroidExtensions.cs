using AndroidX.Fragment.App;

using Microsoft.Maui.Platform;

namespace MPowerKit.Popups;

public static class AndroidExtensions
{
    public static FragmentManager GetFragmentManager(IMauiContext mauiContext)
    {
        var fragmentManager = mauiContext.Services.GetService<FragmentManager>();

        return fragmentManager
            ?? mauiContext.Context?.GetFragmentManager()
            ?? throw new InvalidOperationException("FragmentManager Not Found");
    }
}