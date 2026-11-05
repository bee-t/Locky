using Locky.Services;
using Locky.Views;

namespace Locky;

public class App : Application
{
    private readonly ILockyPlatformService _platformService;

    public App(ILockyPlatformService platformService)
    {
        _platformService = platformService;
        UserAppTheme = AppTheme.Dark;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new NavigationPage(new MainPage(_platformService))
        {
            BarBackgroundColor = Color.FromArgb("#0B0F19"),
            BarTextColor = Colors.White
        })
        {
            Title = "Locky"
        };
    }
}
