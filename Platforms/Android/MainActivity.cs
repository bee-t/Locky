using Android.App;
using Android.Content.PM;
using Android.OS;
using Locky.Platforms.Android.Overlay;

namespace Locky;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTask,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    public static event Action? Resumed;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop && Window != null)
        {
            Window.SetStatusBarColor(global::Android.Graphics.Color.ParseColor("#0B0F19"));
            Window.SetNavigationBarColor(global::Android.Graphics.Color.ParseColor("#0B0F19"));
        }
    }

    protected override void OnResume()
    {
        base.OnResume();
        FloatingOverlayController.Instance.RefreshAppearance();
        Resumed?.Invoke();
    }
}
