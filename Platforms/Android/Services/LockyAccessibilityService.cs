using Android.AccessibilityServices;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Views.Accessibility;
using Locky.Platforms.Android.Overlay;

namespace Locky.Platforms.Android.Services;

/// <summary>
/// Optional ultra-lightweight AccessibilityService that elevates Locky's touch shield
/// to TYPE_ACCESSIBILITY_OVERLAY so top notification pull-downs and edge back swipes
/// are also blocked while locked during video calls.
/// Zero screen content inspection; zero background CPU usage.
/// </summary>
[Service(
    Name = "com.locky.touchguard.LockyAccessibilityService",
    Label = "Locky Video Call Touch Shield",
    Permission = "android.permission.BIND_ACCESSIBILITY_SERVICE",
    Exported = true)]
[IntentFilter(new[] { "android.accessibilityservice.AccessibilityService" })]
[MetaData("android.accessibilityservice", Resource = "@xml/accessibility_service_config")]
public sealed class LockyAccessibilityService : AccessibilityService
{
    public static LockyAccessibilityService? Instance { get; private set; }

    protected override void OnServiceConnected()
    {
        base.OnServiceConnected();
        Instance = this;
        FloatingOverlayController.Instance.OnAccessibilityServiceChanged();
    }

    public override void OnAccessibilityEvent(AccessibilityEvent? e)
    {
        // Intentionally no-op: Locky never reads or inspects screen content.
    }

    public override void OnInterrupt()
    {
    }

    protected override bool OnKeyEvent(KeyEvent? e)
    {
        // When screen is locked, block accidental Back / Home / AppSwitch hardware or gesture keys
        if (e != null && FloatingOverlayController.Instance.IsLocked)
        {
            if (e.KeyCode == Keycode.Back ||
                e.KeyCode == Keycode.Home ||
                e.KeyCode == Keycode.AppSwitch)
            {
                return true;
            }
        }
        return base.OnKeyEvent(e);
    }

    public override bool OnUnbind(Intent? intent)
    {
        Instance = null;
        FloatingOverlayController.Instance.OnAccessibilityServiceChanged();
        return base.OnUnbind(intent);
    }

    public override void OnDestroy()
    {
        Instance = null;
        FloatingOverlayController.Instance.OnAccessibilityServiceChanged();
        base.OnDestroy();
    }
}
