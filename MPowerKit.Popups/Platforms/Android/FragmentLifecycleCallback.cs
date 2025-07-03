using Android.OS;

using Microsoft.Maui.Platform;

using DialogFragment = AndroidX.Fragment.App.DialogFragment;
using Fragment = AndroidX.Fragment.App.Fragment;
using FragmentManager = AndroidX.Fragment.App.FragmentManager;
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;

namespace MPowerKit.Popups;

public class FragmentLifecycleCallback : FragmentManager.FragmentLifecycleCallbacks
{
    private readonly List<View> _attachedViews = [];
    private readonly Func<FragmentManager> _getFragmentManager;

    public FragmentLifecycleCallback(Func<FragmentManager> getFragmentManager)
    {
        _getFragmentManager = getFragmentManager;
    }

    public virtual void AttachView(View view)
    {
        var dv = GetTopFragmentDecorView();
        if (dv is null) return;

        dv.AddView(view);
        _attachedViews.Add(view);
    }

    public virtual void DetachView(View view)
    {
        _attachedViews.Remove(view);
        view.RemoveFromParent();
    }

    public override void OnFragmentCreated(FragmentManager fm, Fragment f, Bundle? savedInstanceState)
    {
        base.OnFragmentCreated(fm, f, savedInstanceState);

        var dv = GetTopFragmentDecorView();
        if (dv is null) return;

        foreach (var view in _attachedViews)
        {
            view.RemoveFromParent();
            dv.AddView(view);
        }
    }

    public override void OnFragmentDestroyed(FragmentManager fm, Fragment f)
    {
        base.OnFragmentDestroyed(fm, f);

        var dv = GetTopFragmentDecorView();
        if (dv is null) return;

        foreach (var view in _attachedViews)
        {
            view.RemoveFromParent();
            dv.AddView(view);
        }
    }

    protected virtual ViewGroup? GetTopFragmentDecorView()
    {
        var fragments = _getFragmentManager?.Invoke()?.Fragments;

        Fragment? topFragment = null;
        if (fragments?.Count > 0)
        {
            topFragment = fragments[^1];

            if (topFragment is DialogFragment dialogFragment)
            {
                return dialogFragment.Dialog?.Window?.DecorView as ViewGroup;
            }
        }

        var activity = topFragment?.Activity ?? Platform.CurrentActivity;

        return activity?.Window?.DecorView as ViewGroup;
    }
}
