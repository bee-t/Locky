using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using Locky.Platforms.Android.Overlay;

namespace Locky.Platforms.Android.Services;

[Service(
    Name = "com.locky.touchguard.LockyForegroundService",
    Exported = false,
    ForegroundServiceType = ForegroundService.TypeSpecialUse)]
public sealed class LockyForegroundService : Service
{
    public const string ActionStart = "com.locky.touchguard.ACTION_START";
    public const string ActionStop = "com.locky.touchguard.ACTION_STOP";
    public const string ActionToggleLock = "com.locky.touchguard.ACTION_TOGGLE_LOCK";

    private const string ChannelId = "locky_overlay_channel";
    private const int NotificationId = 4102;

    private static LockyForegroundService? _instance;

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnCreate()
    {
        base.OnCreate();
        _instance = this;
        EnsureNotificationChannel();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        string? action = intent?.Action;

        if (action == ActionStop)
        {
            FloatingOverlayController.Instance.Stop();
            StopForeground(StopForegroundFlags.Remove);
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        if (action == ActionToggleLock)
        {
            bool currentlyLocked = FloatingOverlayController.Instance.IsLocked;
            FloatingOverlayController.Instance.SetLocked(!currentlyLocked);
            UpdateNotificationState();
            return StartCommandResult.Sticky;
        }

        try
        {
            StartForegroundWithNotification();
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Warn("Locky", $"StartForeground warning: {ex.Message}");
        }

        FloatingOverlayController.Instance.Start();
        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        _instance = null;
        base.OnDestroy();
    }

    public static void UpdateNotificationState()
    {
        _instance?.UpdateNotificationInternal();
    }

    private void StartForegroundWithNotification()
    {
        var notification = BuildNotification();
        if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake)
        {
            StartForeground(NotificationId, notification, ForegroundService.TypeSpecialUse);
        }
        else
        {
            StartForeground(NotificationId, notification);
        }
    }

    private void UpdateNotificationInternal()
    {
        try
        {
            var manager = GetSystemService(NotificationService)?.JavaCast<NotificationManager>();
            manager?.Notify(NotificationId, BuildNotification());
        }
        catch { }
    }

    private Notification BuildNotification()
    {
        bool isLocked = FloatingOverlayController.Instance.IsLocked;

        var openAppIntent = new Intent(this, typeof(MainActivity));
        openAppIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        var openAppPending = PendingIntent.GetActivity(
            this, 0, openAppIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var toggleIntent = new Intent(this, typeof(LockyForegroundService));
        toggleIntent.SetAction(ActionToggleLock);
        var togglePending = PendingIntent.GetService(
            this, 1, toggleIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var stopIntent = new Intent(this, typeof(LockyForegroundService));
        stopIntent.SetAction(ActionStop);
        var stopPending = PendingIntent.GetService(
            this, 2, stopIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        string title = isLocked
            ? "Locky: Screen Touch Locked"
            : "Locky: Floating Button Ready";

        string text = isLocked
            ? "Double-tap or hold floating button 1s to unlock (or tap Unlock below)."
            : "Tap the floating green-dot button during video calls to lock touches.";

        var builder = new NotificationCompat.Builder(this, ChannelId)
            .SetSmallIcon(global::Android.Resource.Drawable.IcLockIdleLock)
            .SetContentTitle(title)!
            .SetContentText(text)!
            .SetOngoing(true)!
            .SetSilent(true)!
            .SetPriority(NotificationCompat.PriorityLow)!
            .SetContentIntent(openAppPending)!
            .AddAction(0, isLocked ? "Emergency Unlock" : "Lock Now", togglePending)!
            .AddAction(0, "Stop Locky", stopPending)!;

        return builder.Build()!;
    }

    private void EnsureNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        try
        {
            var manager = GetSystemService(NotificationService)?.JavaCast<NotificationManager>();
            if (manager == null)
                return;

            var channel = new NotificationChannel(
                ChannelId,
                "Locky Floating Button",
                NotificationImportance.Low)
            {
                Description = "Keeps the floating touch-lock button active over video calls"
            };
            channel.SetSound(null, null);
            channel.EnableVibration(false);
            manager.CreateNotificationChannel(channel);
        }
        catch { }
    }
}
