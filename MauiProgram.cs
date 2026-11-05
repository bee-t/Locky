using Locky.Services;
using Locky.Views;
using Microsoft.Extensions.Logging;

namespace Locky;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

#if ANDROID
        builder.Services.AddSingleton<ILockyPlatformService, Locky.Platforms.Android.Services.AndroidLockyPlatformService>();
#endif
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
