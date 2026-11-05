using Android.Content;
using Android.OS;
using Android.Provider;
using Locky.Platforms.Android.Overlay;
using Locky.Services;
using NetUri = Android.Net.Uri;

namespace Locky.Platforms.Android.Services;

public sealed class AndroidLockyPlatformService : ILockyPlatformService
{
    public event Action? StateChanged;

    public AndroidLockyPlatformService()
    {
        FloatingOverlayController.Instance.StateChanged += () => StateChanged?.Invoke();
    }

    public bool HasOverlayPermission
    {
        get
        {
            var context = global::Android.App.Application.Context;
            return Settings.CanDrawOverlays(context) || LockyAccessibilityService.Instance != null;
        }
    }

    public bool IsAccessibilityShieldEnabled
    {
        get
        {
            if (LockyAccessibilityService.Instance != null)
                return true;

            try
            {
                var context = global::Android.App.Application.Context;
                string? enabledServices = Settings.Secure.GetString(
                    context.ContentResolver,
                    Settings.Secure.EnabledAccessibilityServices);
                return !string.IsNullOrEmpty(enabledServices) &&
                       enabledServices.Contains(context.PackageName ?? "com.locky.touchguard", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }

    public bool IsFloatingButtonRunning => FloatingOverlayController.Instance.IsRunning;

    public bool IsScreenLocked => FloatingOverlayController.Instance.IsLocked;

    public void RequestOverlayPermission()
    {
        try
        {
            var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
            var intent = new Intent(
                Settings.ActionManageOverlayPermission,
                NetUri.Parse($"package:{context.PackageName}"));
            intent.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(intent);
        }
        catch
        {
            try
            {
                var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
                var fallback = new Intent(Settings.ActionManageOverlayPermission);
                fallback.AddFlags(ActivityFlags.NewTask);
                context.StartActivity(fallback);
            }
            catch { }
        }
    }

    public void OpenAccessibilitySettings()
    {
        try
        {
            var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
            var intent = new Intent(Settings.ActionAccessibilitySettings);
            intent.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(intent);
        }
        catch { }
    }

    public bool StartFloatingButton()
    {
        if (!HasOverlayPermission)
        {
            RequestOverlayPermission();
            return false;
        }

        // Attach the floating overlay window first on the UI thread
        bool started = FloatingOverlayController.Instance.Start();

        try
        {
            var context = global::Android.App.Application.Context;
            var serviceIntent = new Intent(context, typeof(LockyForegroundService));
            serviceIntent.SetAction(LockyForegroundService.ActionStart);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(serviceIntent);
            }
            else
            {
                context.StartService(serviceIntent);
            }
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Warn("Locky", $"ForegroundService start note: {ex.Message}");
        }

        StateChanged?.Invoke();
        return started;
    }

    public void StopFloatingButton()
    {
        try
        {
            var context = global::Android.App.Application.Context;
            var serviceIntent = new Intent(context, typeof(LockyForegroundService));
            serviceIntent.SetAction(LockyForegroundService.ActionStop);
            context.StartService(serviceIntent);
        }
        catch { }

        FloatingOverlayController.Instance.Stop();
        StateChanged?.Invoke();
    }

    public void SetLocked(bool locked)
    {
        if (!IsFloatingButtonRunning)
        {
            if (!StartFloatingButton())
                return;
        }
        FloatingOverlayController.Instance.SetLocked(locked);
        StateChanged?.Invoke();
    }

    public void RefreshSettings()
    {
        FloatingOverlayController.Instance.RefreshAppearance();
        StateChanged?.Invoke();
    }
}
