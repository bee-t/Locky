using Android.Content;
using Android.Views;

namespace Locky.Platforms.Android.Overlay;

/// <summary>
/// Full-screen 100% transparent touch-absorbing shield placed over the video call when Locked.
/// Consumes all touch and key events while using zero GPU/CPU drawing overhead.
/// </summary>
public sealed class TouchShieldOverlayView : global::Android.Views.View
{
    public event Action? BlockedTouchOccurred;

    public TouchShieldOverlayView(Context context) : base(context)
    {
        Clickable = true;
        Focusable = false;
        SetBackgroundColor(global::Android.Graphics.Color.Transparent);
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e != null && e.ActionMasked == MotionEventActions.Down)
        {
            BlockedTouchOccurred?.Invoke();
        }
        // Always consume all touches so underlying video call buttons cannot be pressed by a toddler
        return true;
    }
}
