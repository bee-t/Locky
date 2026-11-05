using Android.Content;
using Android.Graphics;
using Android.Views;
using Paint = Android.Graphics.Paint;
using RectF = Android.Graphics.RectF;
using Color = Android.Graphics.Color;

namespace Locky.Platforms.Android.Overlay;

/// <summary>
/// Minimalist native custom View for the floating Locky indicator.
/// Displays only a compact Green (Unlocked) or Red (Locked) indicator dot.
/// Pre-allocates all Paint and RectF instances for zero GC allocations in OnDraw.
/// </summary>
public sealed class FloatingLockButtonView : global::Android.Views.View
{
    private readonly Paint _haloPaint;
    private readonly Paint _borderPaint;
    private readonly Paint _dotGlowPaint;
    private readonly Paint _dotPaint;
    private readonly Paint _specularPaint;
    private readonly Paint _progressPaint;
    private readonly RectF _progressRect = new();

    private bool _isLocked;
    private float _holdProgress; // 0f..1f during 1s parent unlock hold
    private float _shieldPulse;  // 0f..1f brief flash when a toddler taps the locked screen

    public event Func<MotionEvent, bool>? TouchIntercepted;

    public FloatingLockButtonView(Context context) : base(context)
    {
        SetWillNotDraw(false);
        Clickable = true;
        Focusable = false;

        _haloPaint = new Paint(PaintFlags.AntiAlias)
        {
            Color = new Color(12, 16, 24, 165)
        };
        _haloPaint.SetStyle(Paint.Style.Fill);

        _borderPaint = new Paint(PaintFlags.AntiAlias)
        {
            StrokeWidth = 2f
        };
        _borderPaint.SetStyle(Paint.Style.Stroke);

        _dotGlowPaint = new Paint(PaintFlags.AntiAlias);
        _dotGlowPaint.SetStyle(Paint.Style.Fill);

        _dotPaint = new Paint(PaintFlags.AntiAlias);
        _dotPaint.SetStyle(Paint.Style.Fill);

        _specularPaint = new Paint(PaintFlags.AntiAlias)
        {
            Color = new Color((byte)255, (byte)255, (byte)255, (byte)95)
        };
        _specularPaint.SetStyle(Paint.Style.Fill);

        _progressPaint = new Paint(PaintFlags.AntiAlias)
        {
            Color = new Color((byte)34, (byte)197, (byte)94, (byte)255), // Emerald #22C55E
            StrokeCap = Paint.Cap.Round
        };
        _progressPaint.SetStyle(Paint.Style.Stroke);
    }

    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (_isLocked != value)
            {
                _isLocked = value;
                _holdProgress = 0f;
                Invalidate();
            }
        }
    }

    public float HoldProgress
    {
        get => _holdProgress;
        set
        {
            float clamped = Math.Clamp(value, 0f, 1f);
            if (Math.Abs(_holdProgress - clamped) > 0.005f)
            {
                _holdProgress = clamped;
                Invalidate();
            }
        }
    }

    public void TriggerShieldPulse()
    {
        _shieldPulse = 1f;
        Invalidate();
        PostDelayed(() =>
        {
            _shieldPulse = 0f;
            Invalidate();
        }, 160);
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e != null && TouchIntercepted != null)
        {
            return TouchIntercepted.Invoke(e);
        }
        return base.OnTouchEvent(e);
    }

    protected override void OnDraw(Canvas? canvas)
    {
        if (canvas == null)
            return;

        base.OnDraw(canvas);

        float w = Width;
        float h = Height;
        if (w <= 0 || h <= 0)
            return;

        float cx = w * 0.5f;
        float cy = h * 0.5f;
        float maxR = Math.Min(w, h) * 0.5f;

        // 1. Subtle dark contrast ring so the dot stays visible over bright or dark video feeds
        float haloR = maxR * 0.78f;
        canvas.DrawCircle(cx, cy, haloR, _haloPaint);

        // 2. Thin outer ring (flashes briefly if toddler touches locked screen)
        float ringStroke = Math.Max(1.5f, maxR * 0.09f);
        _borderPaint.StrokeWidth = ringStroke;
        if (_isLocked)
        {
            byte ringAlpha = _shieldPulse > 0.1f ? (byte)245 : (byte)130;
            _borderPaint.Color = new Color((byte)239, (byte)68, (byte)68, ringAlpha); // Red #EF4444
        }
        else
        {
            _borderPaint.Color = new Color((byte)34, (byte)197, (byte)94, (byte)120); // Green #22C55E
        }
        canvas.DrawCircle(cx, cy, haloR, _borderPaint);

        // 3. Soft glow around the core indicator dot
        float dotR = maxR * 0.44f;
        if (_isLocked)
        {
            _dotGlowPaint.Color = new Color((byte)239, (byte)68, (byte)68, (byte)95);
            _dotPaint.Color = new Color((byte)239, (byte)68, (byte)68, (byte)255); // Red #EF4444
        }
        else
        {
            _dotGlowPaint.Color = new Color((byte)34, (byte)197, (byte)94, (byte)90);
            _dotPaint.Color = new Color((byte)34, (byte)197, (byte)94, (byte)255); // Green #22C55E
        }
        canvas.DrawCircle(cx, cy, dotR * 1.45f, _dotGlowPaint);

        // 4. Core Green or Red Indicator Dot
        canvas.DrawCircle(cx, cy, dotR, _dotPaint);

        // Tiny subtle LED highlight
        canvas.DrawCircle(cx - dotR * 0.28f, cy - dotR * 0.28f, Math.Max(1.2f, dotR * 0.24f), _specularPaint);

        // 5. 1-Second Hold Unlock Progress Ring (sweeps around the outer edge when holding to unlock)
        if (_isLocked && _holdProgress > 0.01f)
        {
            float progStroke = Math.Max(2.5f, maxR * 0.16f);
            _progressPaint.StrokeWidth = progStroke;
            float inset = progStroke * 0.65f;
            _progressRect.Set(inset, inset, w - inset, h - inset);
            canvas.DrawArc(_progressRect, -90f, 360f * _holdProgress, false, _progressPaint);
        }
    }
}
