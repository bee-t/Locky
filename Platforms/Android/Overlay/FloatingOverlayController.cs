using Android.Animation;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Locky.Models;
using Locky.Platforms.Android.Services;

namespace Locky.Platforms.Android.Overlay;

/// <summary>
/// Manages the floating Locky button and the full-screen transparent touch shield.
/// Designed for near-zero battery consumption: event-driven with zero continuous polling loops.
/// </summary>
public sealed class FloatingOverlayController : Java.Lang.Object
{
    public static FloatingOverlayController Instance { get; } = new();

    private readonly Handler _handler = new(Looper.MainLooper!);
    private readonly Random _rng = new();

    private IWindowManager? _windowManager;
    private bool _usingAccessibilityLayer;

    private FloatingLockButtonView? _buttonView;
    private WindowManagerLayoutParams? _buttonParams;

    private TouchShieldOverlayView? _shieldView;
    private WindowManagerLayoutParams? _shieldParams;

    private ValueAnimator? _holdAnimator;
    private ValueAnimator? _glideAnimator;

    private bool _isRunning;
    private bool _isLocked;
    private bool _isDragging;
    private bool _isPointerDown;
    private bool _pendingSingleTapDodge;

    private float _downRawX;
    private float _downRawY;
    private int _startParamX;
    private int _startParamY;
    private long _lastTapUpTimeMs;

    private Java.Lang.IRunnable? _periodicFloatRunnable;
    private Java.Lang.IRunnable? _singleTapDodgeRunnable;

    public event Action? StateChanged;

    private FloatingOverlayController()
    {
        LockySettings.Changed += OnSettingsChanged;
    }

    public bool IsRunning => _isRunning;
    public bool IsLocked => _isLocked;

    public bool Start()
    {
        Context appContext = global::Android.App.Application.Context;
        if (!Settings.CanDrawOverlays(appContext) && LockyAccessibilityService.Instance == null)
            return false;

        if (_isRunning && _buttonView != null && _windowManager != null)
        {
            RefreshAppearance();
            return true;
        }

        bool attached = AttachViews();
        _isRunning = attached;
        StateChanged?.Invoke();
        return attached;
    }

    public void Stop()
    {
        CancelTimersAndAnimators();
        DetachViews();
        _isLocked = false;
        _isRunning = false;
        StateChanged?.Invoke();
    }

    public void OnAccessibilityServiceChanged()
    {
        if (!_isRunning)
        {
            StateChanged?.Invoke();
            return;
        }

        bool canUseAccessibility = LockyAccessibilityService.Instance != null;
        if (canUseAccessibility != _usingAccessibilityLayer)
        {
            int savedX = _buttonParams?.X ?? -1;
            int savedY = _buttonParams?.Y ?? -1;
            bool wasLocked = _isLocked;

            DetachViews();
            _isRunning = AttachViews(savedX, savedY);
            if (_isRunning && wasLocked)
            {
                SetLocked(true, jumpImmediately: false);
            }
        }
        StateChanged?.Invoke();
    }

    public void SetLocked(bool locked, bool jumpImmediately = true)
    {
        if (!_isRunning || _buttonView == null || _buttonParams == null || _windowManager == null)
            return;

        CancelHoldAndDodge();

        _isLocked = locked;
        _buttonView.IsLocked = locked;

        UpdateShieldVisibility(locked);
        UpdateButtonSizeAndAlpha();

        TriggerSubtleHaptic(locked ? 35 : 55);

        if (locked)
        {
            if (jumpImmediately)
            {
                MoveButtonToRandomPosition(animate: true);
            }
            ScheduleNextPeriodicFloat();
        }
        else
        {
            StopPeriodicFloat();
        }

        LockyForegroundService.UpdateNotificationState();
        StateChanged?.Invoke();
    }

    public void RefreshAppearance()
    {
        if (!_isRunning || _buttonView == null || _buttonParams == null || _windowManager == null)
            return;

        UpdateButtonSizeAndAlpha();
        if (_isLocked)
        {
            UpdateShieldVisibility(true);
            ScheduleNextPeriodicFloat();
        }
    }

    private void OnSettingsChanged()
    {
        _handler.Post(RefreshAppearance);
    }

    private static IWindowManager? ResolveWindowManager(Context context)
    {
        var rawService = context.GetSystemService(Context.WindowService);
        if (rawService == null)
            return null;

        return rawService as IWindowManager ?? rawService.JavaCast<IWindowManager>();
    }

    private bool AttachViews(int initialX = -1, int initialY = -1)
    {
        var (screenW, screenH) = GetScreenDimensions();
        int sizePx = GetCurrentButtonSizePx();

        int startX = initialX >= 0 ? initialX : Math.Max(24, screenW - sizePx - dpToPx(18));
        int startY = initialY >= 0 ? initialY : Math.Clamp((int)(screenH * 0.35f), dpToPx(80), Math.Max(dpToPx(80), screenH - sizePx - dpToPx(120)));

        // Try Accessibility overlay layer first if the Accessibility Service is active
        if (LockyAccessibilityService.Instance != null)
        {
            if (TryAttachButtonWithContext(
                LockyAccessibilityService.Instance,
                WindowManagerTypes.AccessibilityOverlay,
                sizePx,
                startX,
                startY))
            {
                _usingAccessibilityLayer = true;
                return true;
            }
        }

        // Standard ApplicationOverlay layer
        Context appContext = global::Android.App.Application.Context;
        if (Settings.CanDrawOverlays(appContext))
        {
            if (TryAttachButtonWithContext(
                appContext,
                WindowManagerTypes.ApplicationOverlay,
                sizePx,
                startX,
                startY))
            {
                _usingAccessibilityLayer = false;
                return true;
            }
        }

        return false;
    }

    private bool TryAttachButtonWithContext(
        Context context,
        WindowManagerTypes overlayType,
        int sizePx,
        int startX,
        int startY)
    {
        try
        {
            var wm = ResolveWindowManager(context);
            if (wm == null)
                return false;

            var button = new FloatingLockButtonView(context)
            {
                IsLocked = _isLocked,
                Alpha = LockySettings.IdleOpacity
            };
            button.TouchIntercepted += OnButtonTouch;

            var flags = WindowManagerFlags.NotFocusable
                      | WindowManagerFlags.NotTouchModal
                      | WindowManagerFlags.LayoutInScreen;

            var layoutParams = new WindowManagerLayoutParams(
                sizePx,
                sizePx,
                overlayType,
                flags,
                Format.Translucent)
            {
                Gravity = GravityFlags.Top | GravityFlags.Left,
                X = startX,
                Y = startY
            };

            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                layoutParams.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }

            wm.AddView(button, layoutParams);

            _windowManager = wm;
            _buttonView = button;
            _buttonParams = layoutParams;
            return true;
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error("Locky", $"Failed to attach overlay ({overlayType}): {ex.Message}");
            return false;
        }
    }

    private void UpdateShieldVisibility(bool showShield)
    {
        if (_windowManager == null || _buttonView == null || _buttonParams == null)
            return;

        Context context = (_usingAccessibilityLayer && LockyAccessibilityService.Instance != null)
            ? LockyAccessibilityService.Instance
            : global::Android.App.Application.Context;

        WindowManagerTypes overlayType = _usingAccessibilityLayer
            ? WindowManagerTypes.AccessibilityOverlay
            : WindowManagerTypes.ApplicationOverlay;

        if (showShield)
        {
            var shieldFlags = WindowManagerFlags.NotFocusable
                            | WindowManagerFlags.NotTouchModal
                            | WindowManagerFlags.LayoutInScreen
                            | WindowManagerFlags.Fullscreen;

            if (LockySettings.KeepScreenOnWhileLocked)
            {
                shieldFlags |= WindowManagerFlags.KeepScreenOn;
            }

            if (_shieldView == null)
            {
                _shieldView = new TouchShieldOverlayView(context);
                _shieldView.BlockedTouchOccurred += () => _buttonView?.TriggerShieldPulse();

                _shieldParams = new WindowManagerLayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.MatchParent,
                    overlayType,
                    shieldFlags,
                    Format.Translucent)
                {
                    Gravity = GravityFlags.Top | GravityFlags.Left
                };

                if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
                {
                    _shieldParams.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
                }

                try
                {
                    _windowManager.AddView(_shieldView, _shieldParams);
                    // Re-add button view on top of shield view so the button always receives touches first
                    _windowManager.RemoveViewImmediate(_buttonView);
                    _windowManager.AddView(_buttonView, _buttonParams);
                }
                catch (Exception ex)
                {
                    global::Android.Util.Log.Error("Locky", $"Shield attach error: {ex.Message}");
                }
            }
            else if (_shieldParams != null)
            {
                _shieldParams.Flags = shieldFlags;
                try
                {
                    _windowManager.UpdateViewLayout(_shieldView, _shieldParams);
                }
                catch { }
            }
        }
        else
        {
            if (_shieldView != null)
            {
                try
                {
                    _windowManager.RemoveViewImmediate(_shieldView);
                }
                catch { }
                _shieldView = null;
                _shieldParams = null;
            }
        }
    }

    private void UpdateButtonSizeAndAlpha()
    {
        if (_buttonView == null || _buttonParams == null || _windowManager == null)
            return;

        int sizePx = GetCurrentButtonSizePx();
        _buttonParams.Width = sizePx;
        _buttonParams.Height = sizePx;
        _buttonView.Alpha = _isPointerDown ? 1.0f : LockySettings.IdleOpacity;

        try
        {
            _windowManager.UpdateViewLayout(_buttonView, _buttonParams);
            _buttonView.Invalidate();
        }
        catch { }
    }

    private bool OnButtonTouch(MotionEvent e)
    {
        if (_buttonView == null || _buttonParams == null || _windowManager == null)
            return false;

        int touchSlop = dpToPx(12);

        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
                _isPointerDown = true;
                _isDragging = false;
                _downRawX = e.RawX;
                _downRawY = e.RawY;
                _startParamX = _buttonParams.X;
                _startParamY = _buttonParams.Y;

                _glideAnimator?.Cancel();
                _buttonView.Alpha = 1.0f;

                if (_isLocked)
                {
                    StartOneSecondHoldAnimator();
                }
                return true;

            case MotionEventActions.Move:
                float dx = e.RawX - _downRawX;
                float dy = e.RawY - _downRawY;

                if (!_isDragging && (Math.Abs(dx) > touchSlop || Math.Abs(dy) > touchSlop))
                {
                    _isDragging = true;
                    CancelHoldOnly();
                    if (_singleTapDodgeRunnable != null)
                    {
                        _handler.RemoveCallbacks(_singleTapDodgeRunnable);
                        _pendingSingleTapDodge = false;
                    }
                }

                if (_isDragging)
                {
                    var (screenW, screenH) = GetScreenDimensions();
                    int sizePx = _buttonParams.Width;
                    _buttonParams.X = Math.Clamp(_startParamX + (int)dx, 0, Math.Max(0, screenW - sizePx));
                    _buttonParams.Y = Math.Clamp(_startParamY + (int)dy, 0, Math.Max(0, screenH - sizePx));
                    try
                    {
                        _windowManager.UpdateViewLayout(_buttonView, _buttonParams);
                    }
                    catch { }
                }
                return true;

            case MotionEventActions.Up:
            case MotionEventActions.Cancel:
                _isPointerDown = false;
                _buttonView.Alpha = LockySettings.IdleOpacity;

                bool wasDragging = _isDragging;
                _isDragging = false;
                CancelHoldOnly();

                if (wasDragging)
                {
                    if (_isLocked)
                    {
                        ScheduleNextPeriodicFloat();
                    }
                    return true;
                }

                if (!_isLocked)
                {
                    // Unlocked state -> Single tap locks screen and starts floating!
                    SetLocked(true, jumpImmediately: true);
                }
                else
                {
                    // Locked state -> Check for Double-Tap to unlock; otherwise dodge on single-tap!
                    long now = SystemClock.UptimeMillis();
                    if (_pendingSingleTapDodge && (now - _lastTapUpTimeMs) <= 350)
                    {
                        if (_singleTapDodgeRunnable != null)
                        {
                            _handler.RemoveCallbacks(_singleTapDodgeRunnable);
                        }
                        _pendingSingleTapDodge = false;
                        SetLocked(false);
                    }
                    else
                    {
                        _lastTapUpTimeMs = now;
                        _pendingSingleTapDodge = true;

                        if (_singleTapDodgeRunnable != null)
                        {
                            _handler.RemoveCallbacks(_singleTapDodgeRunnable);
                        }

                        _singleTapDodgeRunnable = new Java.Lang.Runnable(() =>
                        {
                            _pendingSingleTapDodge = false;
                            if (_isLocked && !_isPointerDown)
                            {
                                MoveButtonToRandomPosition(animate: true);
                                ScheduleNextPeriodicFloat();
                            }
                        });
                        _handler.PostDelayed(_singleTapDodgeRunnable, 350);
                    }
                }
                return true;
        }

        return false;
    }

    private void StartOneSecondHoldAnimator()
    {
        CancelHoldOnly();
        if (_buttonView == null)
            return;

        _holdAnimator = ValueAnimator.OfFloat(0f, 1f);
        if (_holdAnimator == null)
            return;

        _holdAnimator.SetDuration(1000);
        _holdAnimator.Update += (_, args) =>
        {
            if (_buttonView != null && args.Animation != null)
            {
                _buttonView.HoldProgress = args.Animation.AnimatedFraction;
            }
        };
        _holdAnimator.AnimationEnd += (_, _) =>
        {
            if (_isPointerDown && !_isDragging && _isLocked && (_buttonView?.HoldProgress ?? 0f) >= 0.95f)
            {
                _isPointerDown = false;
                SetLocked(false);
            }
        };
        _holdAnimator.Start();
    }

    private void CancelHoldOnly()
    {
        if (_holdAnimator != null)
        {
            _holdAnimator.RemoveAllListeners();
            _holdAnimator.Cancel();
            _holdAnimator = null;
        }
        if (_buttonView != null)
        {
            _buttonView.HoldProgress = 0f;
        }
    }

    private void CancelHoldAndDodge()
    {
        CancelHoldOnly();
        if (_singleTapDodgeRunnable != null)
        {
            _handler.RemoveCallbacks(_singleTapDodgeRunnable);
            _singleTapDodgeRunnable = null;
        }
        _pendingSingleTapDodge = false;
    }

    private void ScheduleNextPeriodicFloat()
    {
        StopPeriodicFloat();
        int intervalSec = LockySettings.FloatIntervalSeconds;
        if (!_isLocked || intervalSec <= 0)
            return;

        _periodicFloatRunnable = new Java.Lang.Runnable(() =>
        {
            if (!_isLocked || !_isRunning)
                return;

            if (!_isPointerDown && !_pendingSingleTapDodge)
            {
                MoveButtonToRandomPosition(animate: true);
            }
            ScheduleNextPeriodicFloat();
        });

        _handler.PostDelayed(_periodicFloatRunnable, intervalSec * 1000L);
    }

    private void StopPeriodicFloat()
    {
        if (_periodicFloatRunnable != null)
        {
            _handler.RemoveCallbacks(_periodicFloatRunnable);
            _periodicFloatRunnable = null;
        }
    }

    private void MoveButtonToRandomPosition(bool animate)
    {
        if (_buttonView == null || _buttonParams == null || _windowManager == null)
            return;

        var (screenW, screenH) = GetScreenDimensions();
        int sizePx = _buttonParams.Width;
        int marginX = dpToPx(18);
        int marginTop = dpToPx(52);
        int marginBottom = dpToPx(64);

        int maxX = Math.Max(marginX, screenW - sizePx - marginX);
        int maxY = Math.Max(marginTop, screenH - sizePx - marginBottom);

        int curX = _buttonParams.X;
        int curY = _buttonParams.Y;
        int targetX = curX;
        int targetY = curY;

        int minDistancePx = dpToPx(90);
        for (int attempt = 0; attempt < 8; attempt++)
        {
            targetX = _rng.Next(marginX, maxX + 1);
            targetY = _rng.Next(marginTop, maxY + 1);
            float dist = MathF.Sqrt((targetX - curX) * (targetX - curX) + (targetY - curY) * (targetY - curY));
            if (dist >= minDistancePx)
                break;
        }

        _glideAnimator?.Cancel();

        if (!animate)
        {
            _buttonParams.X = targetX;
            _buttonParams.Y = targetY;
            try
            {
                _windowManager.UpdateViewLayout(_buttonView, _buttonParams);
            }
            catch { }
            return;
        }

        int startX = curX;
        int startY = curY;
        _glideAnimator = ValueAnimator.OfFloat(0f, 1f);
        if (_glideAnimator == null)
            return;

        _glideAnimator.SetDuration(260);
        _glideAnimator.Update += (_, args) =>
        {
            if (_buttonView == null || _buttonParams == null || _windowManager == null || _isPointerDown)
                return;

            if (args.Animation != null)
            {
                float t = args.Animation.AnimatedFraction;
                float ease = 1f - MathF.Pow(1f - t, 3f);
                _buttonParams.X = startX + (int)((targetX - startX) * ease);
                _buttonParams.Y = startY + (int)((targetY - startY) * ease);
                try
                {
                    _windowManager.UpdateViewLayout(_buttonView, _buttonParams);
                }
                catch { }
            }
        };
        _glideAnimator.Start();
    }

    private void CancelTimersAndAnimators()
    {
        CancelHoldAndDodge();
        StopPeriodicFloat();
        _glideAnimator?.Cancel();
        _glideAnimator = null;
    }

    private void DetachViews()
    {
        if (_windowManager != null)
        {
            if (_buttonView != null)
            {
                try { _windowManager.RemoveViewImmediate(_buttonView); } catch { }
            }
            if (_shieldView != null)
            {
                try { _windowManager.RemoveViewImmediate(_shieldView); } catch { }
            }
        }
        _buttonView = null;
        _buttonParams = null;
        _shieldView = null;
        _shieldParams = null;
        _windowManager = null;
    }

    private int GetCurrentButtonSizePx()
    {
        int baseDp = LockySettings.ButtonSizeDp;
        int effectiveDp = _isLocked ? Math.Max(16, (int)(baseDp * 0.88f)) : baseDp;
        return dpToPx(effectiveDp);
    }

    private static (int width, int height) GetScreenDimensions()
    {
        var metrics = global::Android.App.Application.Context.Resources?.DisplayMetrics;
        if (metrics == null)
            return (1080, 2400);
        return (metrics.WidthPixels, metrics.HeightPixels);
    }

    private static int dpToPx(float dp)
    {
        var metrics = global::Android.App.Application.Context.Resources?.DisplayMetrics;
        if (metrics == null)
            return (int)(dp * 2.75f);
        return (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, metrics);
    }

    private static void TriggerSubtleHaptic(int durationMs)
    {
        if (!LockySettings.HapticFeedback)
            return;

        try
        {
            var context = global::Android.App.Application.Context;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                var vm = context.GetSystemService(Context.VibratorManagerService).JavaCast<VibratorManager>();
                vm?.DefaultVibrator?.Vibrate(VibrationEffect.CreateOneShot(durationMs, VibrationEffect.DefaultAmplitude));
            }
            else if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
#pragma warning disable CA1422
                var vib = context.GetSystemService(Context.VibratorService).JavaCast<Vibrator>();
                vib?.Vibrate(VibrationEffect.CreateOneShot(durationMs, VibrationEffect.DefaultAmplitude));
#pragma warning restore CA1422
            }
        }
        catch { }
    }
}
