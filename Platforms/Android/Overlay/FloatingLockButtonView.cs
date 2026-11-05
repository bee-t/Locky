using Android.Content;
using Android.Graphics;
using Android.Views;
using Paint = Android.Graphics.Paint;
using Path = Android.Graphics.Path;
using RectF = Android.Graphics.RectF;
using Color = Android.Graphics.Color;

namespace Locky.Platforms.Android.Overlay;

/// <summary>
/// Ultra-lightweight native custom View for the floating Locky button.
/// Pre-allocates all Paint and RectF instances to guarantee zero GC allocations during OnDraw.
/// </summary>
public sealed class FloatingLockButtonView : global::Android.Views.View
{
    private readonly Paint _bgPaint;
    private readonly Paint _borderPaint;
    private readonly Paint _progressPaint;
    private readonly Paint _iconStrokePaint;
    private readonly Paint _iconFillPaint;
    private readonly Paint _dotPaint;
    private readonly Paint _dotGlowPaint;
    private readonly RectF _boundsRect = new();
    private readonly RectF _progressRect = new();
    private readonly RectF _bodyRect = new();
    private readonly RectF _shackleRect = new();
    private readonly Path _shacklePath = new();

    private bool _isLocked;
    private float _holdProgress; // 0f..1f during 1s parent unlock hold
    private float _shieldPulse;  // 0f..1f brief flash when a toddler taps the locked screen

    public event Func<MotionEvent, bool>? TouchIntercepted;

    public FloatingLockButtonView(Context context) : base(context)
    {
        SetWillNotDraw(false);
        Clickable = true;
        Focusable = false;

        _bgPaint = new Paint(PaintFlags.AntiAlias)
        {
            Color = new Color(15, 20, 30, 235)
        };
        _bgPaint.SetStyle(Paint.Style.Fill);

        _borderPaint = new Paint(PaintFlags.AntiAlias)
        {
            StrokeWidth = 3f
        };
        _borderPaint.SetStyle(Paint.Style.Stroke);

        _progressPaint = new Paint(PaintFlags.AntiAlias)
        {
            Color = new Color(34, 197, 94, 255), // Emerald #22C55E
            StrokeCap = Paint.Cap.Round
        };
        _progressPaint.SetStyle(Paint.Style.Stroke);

        _iconStrokePaint = new Paint(PaintFlags.AntiAlias)
        {
            Color = new Color(248, 250, 252, 248),
            StrokeCap = Paint.Cap.Round,
            StrokeJoin = Paint.Join.Round
        };
        _iconStrokePaint.SetStyle(Paint.Style.Stroke);

        _iconFillPaint = new Paint(PaintFlags.AntiAlias)
        {
            Color = new Color(248, 250, 252, 248)
        };
        _iconFillPaint.SetStyle(Paint.Style.Fill);

        _dotPaint = new Paint(PaintFlags.AntiAlias);
        _dotPaint.SetStyle(Paint.Style.Fill);

        _dotGlowPaint = new Paint(PaintFlags.AntiAlias);
        _dotGlowPaint.SetStyle(Paint.Style.Fill);
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
        }, 180);
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
        float radius = Math.Min(w, h) * 0.5f;
        float strokeW = Math.Max(2.5f, radius * 0.085f);

        // 1. Background Circle
        _boundsRect.Set(strokeW, strokeW, w - strokeW, h - strokeW);
        canvas.DrawOval(_boundsRect, _bgPaint);

        // 2. Subtle Rim (Green tint when unlocked, Red tint when locked)
        _borderPaint.StrokeWidth = strokeW;
        if (_isLocked)
        {
            byte alpha = _shieldPulse > 0.1f ? (byte)255 : (byte)185;
            _borderPaint.Color = new Color((byte)239, (byte)68, (byte)68, alpha); // Crimson #EF4444
        }
        else
        {
            _borderPaint.Color = new Color((byte)34, (byte)197, (byte)94, (byte)175); // Emerald #22C55E
        }
        canvas.DrawOval(_boundsRect, _borderPaint);

        // 3. 1-Second Hold Unlock Progress Arc
        if (_isLocked && _holdProgress > 0.01f)
        {
            float progStroke = Math.Max(3.5f, radius * 0.12f);
            _progressPaint.StrokeWidth = progStroke;
            float inset = progStroke * 0.65f;
            _progressRect.Set(inset, inset, w - inset, h - inset);
            canvas.DrawArc(_progressRect, -90f, 360f * _holdProgress, false, _progressPaint);
        }

        // 4. Padlock Icon (shifted slightly upward to leave room for the Red & Green indicator dots below)
        float iconCenterY = cy - (radius * 0.10f);
        float bodyW = radius * 0.64f;
        float bodyH = radius * 0.50f;
        float bodyLeft = cx - bodyW * 0.5f;
        float bodyTop = iconCenterY - bodyH * 0.15f;
        float bodyRight = cx + bodyW * 0.5f;
        float bodyBottom = bodyTop + bodyH;
        float cornerR = Math.Max(2f, radius * 0.10f);

        float iconStroke = Math.Max(2.2f, radius * 0.095f);
        _iconStrokePaint.StrokeWidth = iconStroke;
        _iconStrokePaint.Color = _isLocked
            ? new Color((byte)254, (byte)242, (byte)242, (byte)250)
            : new Color((byte)240, (byte)253, (byte)244, (byte)250);

        // Padlock body outline
        _bodyRect.Set(bodyLeft, bodyTop, bodyRight, bodyBottom);
        canvas.DrawRoundRect(_bodyRect, cornerR, cornerR, _iconStrokePaint);

        // Keyhole dot inside padlock body
        _iconFillPaint.Color = _isLocked
            ? new Color((byte)239, (byte)68, (byte)68, (byte)250)
            : new Color((byte)34, (byte)197, (byte)94, (byte)250);
        canvas.DrawCircle(cx, (bodyTop + bodyBottom) * 0.5f, Math.Max(1.8f, radius * 0.07f), _iconFillPaint);

        // Padlock shackle
        float shackleW = bodyW * 0.62f;
        float shackleH = bodyH * 0.72f;
        float shackleLeft = cx - shackleW * 0.5f;
        float shackleRight = cx + shackleW * 0.5f;

        _shacklePath.Reset();
        if (_isLocked)
        {
            // Closed shackle
            float arcTop = bodyTop - shackleH * 0.85f;
            float arcBottom = bodyTop + shackleH * 0.15f;
            _shackleRect.Set(shackleLeft, arcTop, shackleRight, arcBottom);
            _shacklePath.MoveTo(shackleLeft, bodyTop);
            _shacklePath.ArcTo(_shackleRect, 180f, 180f, false);
            _shacklePath.LineTo(shackleRight, bodyTop);
            canvas.DrawPath(_shacklePath, _iconStrokePaint);
        }
        else
        {
            // Open shackle (lifted & unlatched on the left so it's clearly unlocked)
            float lift = shackleH * 0.28f;
            float arcTop = bodyTop - shackleH * 0.85f - lift;
            float arcBottom = bodyTop + shackleH * 0.15f - lift;
            _shackleRect.Set(shackleLeft, arcTop, shackleRight, arcBottom);
            _shacklePath.MoveTo(shackleLeft, bodyTop - lift * 1.15f);
            _shacklePath.ArcTo(_shackleRect, 180f, 180f, false);
            _shacklePath.LineTo(shackleRight, bodyTop);
            canvas.DrawPath(_shacklePath, _iconStrokePaint);
        }

        // 5. Small Green & Red Indicator Dots inside the bottom of the button
        float dotsY = bodyBottom + radius * 0.24f;
        float dotSpacing = radius * 0.22f;
        float greenDotX = cx - dotSpacing;
        float redDotX = cx + dotSpacing;
        float activeDotR = Math.Max(2.8f, radius * 0.115f);
        float inactiveDotR = Math.Max(2.0f, radius * 0.08f);

        if (!_isLocked)
        {
            // Unlocked: Bright glowing Green dot + dim Red dot
            _dotGlowPaint.Color = new Color((byte)34, (byte)197, (byte)94, (byte)110);
            canvas.DrawCircle(greenDotX, dotsY, activeDotR * 1.8f, _dotGlowPaint);

            _dotPaint.Color = new Color((byte)34, (byte)197, (byte)94, (byte)255);
            canvas.DrawCircle(greenDotX, dotsY, activeDotR, _dotPaint);

            _dotPaint.Color = new Color((byte)239, (byte)68, (byte)68, (byte)90);
            canvas.DrawCircle(redDotX, dotsY, inactiveDotR, _dotPaint);
        }
        else
        {
            // Locked: Dim Green dot + Bright glowing Red dot
            _dotPaint.Color = new Color((byte)34, (byte)197, (byte)94, (byte)85);
            canvas.DrawCircle(greenDotX, dotsY, inactiveDotR, _dotPaint);

            _dotGlowPaint.Color = new Color((byte)239, (byte)68, (byte)68, (byte)120);
            canvas.DrawCircle(redDotX, dotsY, activeDotR * 1.85f, _dotGlowPaint);

            _dotPaint.Color = new Color((byte)239, (byte)68, (byte)68, (byte)255);
            canvas.DrawCircle(redDotX, dotsY, activeDotR, _dotPaint);
        }
    }
}
