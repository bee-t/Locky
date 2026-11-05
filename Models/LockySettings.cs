using Microsoft.Maui.Storage;

namespace Locky.Models;

public static class LockySettings
{
    public static event Action? Changed;

    public static int ButtonSizeDp
    {
        get => Preferences.Default.Get(nameof(ButtonSizeDp), 40);
        set
        {
            Preferences.Default.Set(nameof(ButtonSizeDp), Math.Clamp(value, 30, 64));
            Changed?.Invoke();
        }
    }

    public static float IdleOpacity
    {
        get => Preferences.Default.Get(nameof(IdleOpacity), 0.78f);
        set
        {
            Preferences.Default.Set(nameof(IdleOpacity), Math.Clamp(value, 0.35f, 1.0f));
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Interval in seconds between automatic random position hops when locked.
    /// 0 means only hop when single-tapped by a toddler (zero periodic timer).
    /// </summary>
    public static int FloatIntervalSeconds
    {
        get => Preferences.Default.Get(nameof(FloatIntervalSeconds), 4);
        set
        {
            Preferences.Default.Set(nameof(FloatIntervalSeconds), Math.Max(0, value));
            Changed?.Invoke();
        }
    }

    public static bool KeepScreenOnWhileLocked
    {
        get => Preferences.Default.Get(nameof(KeepScreenOnWhileLocked), true);
        set
        {
            Preferences.Default.Set(nameof(KeepScreenOnWhileLocked), value);
            Changed?.Invoke();
        }
    }

    public static bool HapticFeedback
    {
        get => Preferences.Default.Get(nameof(HapticFeedback), true);
        set
        {
            Preferences.Default.Set(nameof(HapticFeedback), value);
            Changed?.Invoke();
        }
    }
}
