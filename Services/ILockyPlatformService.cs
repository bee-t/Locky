namespace Locky.Services;

public interface ILockyPlatformService
{
    bool HasOverlayPermission { get; }
    bool IsAccessibilityShieldEnabled { get; }
    bool IsFloatingButtonRunning { get; }
    bool IsScreenLocked { get; }

    event Action? StateChanged;

    void RequestOverlayPermission();
    void OpenAccessibilitySettings();
    bool StartFloatingButton();
    void StopFloatingButton();
    void SetLocked(bool locked);
    void RefreshSettings();
}
