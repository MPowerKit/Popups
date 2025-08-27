using Android.Views;

namespace MPowerKit.Popups;

public class KeyboardListener : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
{
    private readonly ViewGroup _decorView;

    public bool KeyboardVisible { get; private set; }

    public KeyboardListener(ViewGroup decorView)
    {
        _decorView = decorView;
    }

    public void OnGlobalLayout()
    {
        var view = _decorView.FindViewById(Android.Resource.Id.Content);
        if (view is null) return;

        var r = new Android.Graphics.Rect();
        view!.GetWindowVisibleDisplayFrame(r);
        int screenHeight = view.RootView!.Height;

        // r.bottom is the position above soft keypad or device button.
        // if keypad is shown, the r.bottom is smaller than that before.
        int keypadHeight = screenHeight - r.Bottom;

        if (keypadHeight > screenHeight * 0.15)
        {
            if (!KeyboardVisible)
            {
                KeyboardVisible = true;
            }
        }
        else if (KeyboardVisible)
        {
            KeyboardVisible = false;
        }
    }
}