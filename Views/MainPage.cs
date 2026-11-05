using Locky.Models;
using Locky.Services;
using Microsoft.Maui.Controls.Shapes;

namespace Locky.Views;

public sealed class MainPage : ContentPage
{
    private readonly ILockyPlatformService _platform;

    private readonly Border _heroBadge;
    private readonly Label _heroBadgeLabel;
    private readonly Label _heroTitleLabel;
    private readonly Label _heroSubtitleLabel;
    private readonly Button _primaryToggleButton;
    private readonly Button _quickLockButton;

    private readonly Label _overlayPermStatusLabel;
    private readonly Button _overlayPermButton;
    private readonly Label _accessPermStatusLabel;
    private readonly Button _accessPermButton;

    private readonly Button _sizeMiniBtn;
    private readonly Button _sizeCompactBtn;
    private readonly Button _sizeStandardBtn;

    private readonly Button _opacitySubtleBtn;
    private readonly Button _opacityBalancedBtn;
    private readonly Button _opacitySolidBtn;

    private readonly Button _float4sBtn;
    private readonly Button _float8sBtn;
    private readonly Button _floatTapOnlyBtn;

    private readonly Switch _keepAwakeSwitch;

    public MainPage(ILockyPlatformService platform)
    {
        _platform = platform;
        Title = "Locky";
        BackgroundColor = Color.FromArgb("#0B0F19");
        NavigationPage.SetHasNavigationBar(this, false);

        // Hero status elements
        _heroBadgeLabel = new Label
        {
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#22C55E")
        };

        _heroBadge = new Border
        {
            StrokeThickness = 1,
            Stroke = Color.FromArgb("#3322C55E"),
            BackgroundColor = Color.FromArgb("#1A22C55E"),
            Padding = new Thickness(12, 5),
            HorizontalOptions = LayoutOptions.Start,
            StrokeShape = new RoundRectangle { CornerRadius = 999 },
            Content = _heroBadgeLabel
        };

        _heroTitleLabel = new Label
        {
            Text = "Locky Touch Shield",
            FontSize = 24,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#F8FAFC")
        };

        _heroSubtitleLabel = new Label
        {
            FontSize = 14,
            TextColor = Color.FromArgb("#94A3B8"),
            LineHeight = 1.25
        };

        _primaryToggleButton = new Button
        {
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 14,
            HeightRequest = 52
        };
        _primaryToggleButton.Clicked += OnPrimaryToggleClicked;

        _quickLockButton = new Button
        {
            Text = "Test Lock Screen Now",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 44,
            BackgroundColor = Color.FromArgb("#1E293B"),
            TextColor = Color.FromArgb("#E2E8F0")
        };
        _quickLockButton.Clicked += (_, _) =>
        {
            if (_platform.IsFloatingButtonRunning)
            {
                _platform.SetLocked(!_platform.IsScreenLocked);
            }
        };

        // Permission controls
        _overlayPermStatusLabel = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold };
        _overlayPermButton = new Button
        {
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 10,
            HeightRequest = 38,
            Padding = new Thickness(14, 0)
        };
        _overlayPermButton.Clicked += (_, _) => _platform.RequestOverlayPermission();

        _accessPermStatusLabel = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold };
        _accessPermButton = new Button
        {
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 10,
            HeightRequest = 38,
            Padding = new Thickness(14, 0)
        };
        _accessPermButton.Clicked += (_, _) => _platform.OpenAccessibilitySettings();

        // Size selector buttons
        _sizeMiniBtn = CreateChipButton("Tiny (34dp)", () => LockySettings.ButtonSizeDp = 34);
        _sizeCompactBtn = CreateChipButton("Compact (40dp)", () => LockySettings.ButtonSizeDp = 40);
        _sizeStandardBtn = CreateChipButton("Medium (48dp)", () => LockySettings.ButtonSizeDp = 48);

        // Opacity selector buttons
        _opacitySubtleBtn = CreateChipButton("Subtle (55%)", () => LockySettings.IdleOpacity = 0.55f);
        _opacityBalancedBtn = CreateChipButton("Balanced (78%)", () => LockySettings.IdleOpacity = 0.78f);
        _opacitySolidBtn = CreateChipButton("Solid (95%)", () => LockySettings.IdleOpacity = 0.95f);

        // Float interval buttons
        _float4sBtn = CreateChipButton("Every 4s + Tap", () => LockySettings.FloatIntervalSeconds = 4);
        _float8sBtn = CreateChipButton("Every 8s + Tap", () => LockySettings.FloatIntervalSeconds = 8);
        _floatTapOnlyBtn = CreateChipButton("On Tap Only", () => LockySettings.FloatIntervalSeconds = 0);

        _keepAwakeSwitch = new Switch
        {
            IsToggled = LockySettings.KeepScreenOnWhileLocked,
            OnColor = Color.FromArgb("#22C55E"),
            ThumbColor = Colors.White
        };
        _keepAwakeSwitch.Toggled += (_, e) => LockySettings.KeepScreenOnWhileLocked = e.Value;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(18, 22, 18, 32),
                Spacing = 16,
                Children =
                {
                    BuildHeroCard(),
                    BuildPermissionsCard(),
                    BuildCustomizationCard(),
                    BuildHowItWorksCard()
                }
            }
        };

        _platform.StateChanged += () => MainThread.BeginInvokeOnMainThread(RefreshUiState);
        LockySettings.Changed += () => MainThread.BeginInvokeOnMainThread(RefreshUiState);

#if ANDROID
        MainActivity.Resumed += () => MainThread.BeginInvokeOnMainThread(() =>
        {
            TryAutoStartIfPermitted();
            RefreshUiState();
        });
#endif
    }

    private bool _userExplicitlyStopped;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        TryAutoStartIfPermitted();
        RefreshUiState();
    }

    private void TryAutoStartIfPermitted()
    {
        if (!_userExplicitlyStopped && !_platform.IsFloatingButtonRunning && _platform.HasOverlayPermission)
        {
            _platform.StartFloatingButton();
        }
    }

    private View BuildHeroCard()
    {
        return CreateCard(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                _heroBadge,
                _heroTitleLabel,
                _heroSubtitleLabel,
                new BoxView { HeightRequest = 2, Color = Colors.Transparent },
                _primaryToggleButton,
                _quickLockButton
            }
        });
    }

    private View BuildPermissionsCard()
    {
        return CreateCard(new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                new Label
                {
                    Text = "1. Setup & Permissions",
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#F8FAFC")
                },
                BuildPermissionRow(
                    title: "Display Over Other Apps (Required)",
                    subtitle: "Lets the small lock/unlock button float over WhatsApp, Meet, Zoom, or FaceTime.",
                    statusLabel: _overlayPermStatusLabel,
                    actionButton: _overlayPermButton),
                new BoxView { HeightRequest = 1, Color = Color.FromArgb("#1E293B") },
                BuildPermissionRow(
                    title: "Full Edge & Swipe Shield (Optional)",
                    subtitle: "Enable 'Locky Video Call Touch Shield' in Accessibility to also block top notification pull-downs & back/home swipes while locked.",
                    statusLabel: _accessPermStatusLabel,
                    actionButton: _accessPermButton)
            }
        });
    }

    private static View BuildPermissionRow(string title, string subtitle, Label statusLabel, Button actionButton)
    {
        var textCol = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label
                {
                    Text = title,
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#E2E8F0")
                },
                statusLabel,
                new Label
                {
                    Text = subtitle,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#94A3B8"),
                    LineHeight = 1.2
                }
            }
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };
        grid.Add(textCol, 0, 0);
        actionButton.VerticalOptions = LayoutOptions.Center;
        grid.Add(actionButton, 1, 0);
        return grid;
    }

    private View BuildCustomizationCard()
    {
        var keepAwakeRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12,
            Children =
            {
                new VerticalStackLayout
                {
                    Spacing = 2,
                    Children =
                    {
                        new Label
                        {
                            Text = "Keep Screen Awake While Locked",
                            FontSize = 14,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#E2E8F0")
                        },
                        new Label
                        {
                            Text = "Prevents screen dimming while touch lock is active.",
                            FontSize = 12,
                            TextColor = Color.FromArgb("#94A3B8")
                        }
                    }
                }
            }
        };
        _keepAwakeSwitch.VerticalOptions = LayoutOptions.Center;
        keepAwakeRow.Add(_keepAwakeSwitch, 1, 0);

        return CreateCard(new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                new Label
                {
                    Text = "2. Floating Button Preferences",
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#F8FAFC")
                },
                BuildChipGroup("Button Size (Shrinks slightly more when Locked)", _sizeMiniBtn, _sizeCompactBtn, _sizeStandardBtn),
                BuildChipGroup("Idle Visibility", _opacitySubtleBtn, _opacityBalancedBtn, _opacitySolidBtn),
                BuildChipGroup("Random Floating Motion (When Locked)", _float4sBtn, _float8sBtn, _floatTapOnlyBtn),
                new BoxView { HeightRequest = 1, Color = Color.FromArgb("#1E293B") },
                keepAwakeRow
            }
        });
    }

    private static View BuildChipGroup(string label, Button b1, Button b2, Button b3)
    {
        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 8
        };
        row.Add(b1, 0, 0);
        row.Add(b2, 1, 0);
        row.Add(b3, 2, 0);

        return new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label
                {
                    Text = label,
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#CBD5E1")
                },
                row
            }
        };
    }

    private static View BuildHowItWorksCard()
    {
        return CreateCard(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label
                {
                    Text = "How It Works on Video Calls",
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#F8FAFC")
                },
                BuildBullet("🟢", "Unlocked (Green Dot): Drag the small floating button anywhere on screen. Tap it once during a video call to lock the screen."),
                BuildBullet("🔴", "Locked (Red Dot): All toddler taps on the video call are blocked while the video keeps playing. Single-tapping the tiny button makes it hop away to a random spot."),
                BuildBullet("🔓", "Parent Unlock: Double-tap the floating button OR press-and-hold it in place for 1 second (until the green ring fills) to unlock.")
            }
        });
    }

    private static View BuildBullet(string icon, string text)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(26)),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 6
        };
        grid.Add(new Label { Text = icon, FontSize = 15, VerticalOptions = LayoutOptions.Start }, 0, 0);
        grid.Add(new Label { Text = text, FontSize = 13, TextColor = Color.FromArgb("#94A3B8"), LineHeight = 1.25 }, 1, 0);
        return grid;
    }

    private static Border CreateCard(View content)
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb("#131A2A"),
            Stroke = Color.FromArgb("#1E293B"),
            StrokeThickness = 1,
            Padding = new Thickness(16),
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = content
        };
    }

    private static Button CreateChipButton(string text, Action onClick)
    {
        var btn = new Button
        {
            Text = text,
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 10,
            HeightRequest = 38,
            Padding = new Thickness(6, 0)
        };
        btn.Clicked += (_, _) => onClick();
        return btn;
    }

    private void OnPrimaryToggleClicked(object? sender, EventArgs e)
    {
        if (!_platform.HasOverlayPermission)
        {
            _platform.RequestOverlayPermission();
            return;
        }

        if (_platform.IsFloatingButtonRunning)
        {
            _userExplicitlyStopped = true;
            _platform.StopFloatingButton();
        }
        else
        {
            _userExplicitlyStopped = false;
            _platform.StartFloatingButton();
        }
        RefreshUiState();
    }

    private void RefreshUiState()
    {
        bool hasOverlay = _platform.HasOverlayPermission;
        bool hasAccess = _platform.IsAccessibilityShieldEnabled;
        bool isRunning = _platform.IsFloatingButtonRunning;
        bool isLocked = _platform.IsScreenLocked;

        // 1. Hero Card state
        if (!hasOverlay)
        {
            _heroBadgeLabel.Text = "PERMISSION NEEDED";
            _heroBadgeLabel.TextColor = Color.FromArgb("#F59E0B");
            _heroBadge.BackgroundColor = Color.FromArgb("#1AF59E0B");
            _heroBadge.Stroke = Color.FromArgb("#44F59E0B");

            _heroSubtitleLabel.Text = "Grant 'Display over other apps' below so Locky can float its small lock/unlock button over your video calls. 100% offline & near-zero battery use.";
            _primaryToggleButton.Text = "Grant Overlay Permission";
            _primaryToggleButton.BackgroundColor = Color.FromArgb("#F59E0B");
            _primaryToggleButton.TextColor = Color.FromArgb("#0B0F19");
            _quickLockButton.IsVisible = false;
        }
        else if (!isRunning)
        {
            _heroBadgeLabel.Text = "STOPPED • READY TO LAUNCH";
            _heroBadgeLabel.TextColor = Color.FromArgb("#94A3B8");
            _heroBadge.BackgroundColor = Color.FromArgb("#1E293B");
            _heroBadge.Stroke = Color.FromArgb("#334155");

            _heroSubtitleLabel.Text = "Start the floating button before or during a video call with grandparents, family, or friends.";
            _primaryToggleButton.Text = "Start Floating Lock Button";
            _primaryToggleButton.BackgroundColor = Color.FromArgb("#22C55E");
            _primaryToggleButton.TextColor = Color.FromArgb("#052E16");
            _quickLockButton.IsVisible = false;
        }
        else
        {
            if (isLocked)
            {
                _heroBadgeLabel.Text = "🔴 SCREEN LOCKED • SHIELD ACTIVE";
                _heroBadgeLabel.TextColor = Color.FromArgb("#EF4444");
                _heroBadge.BackgroundColor = Color.FromArgb("#1AEF4444");
                _heroBadge.Stroke = Color.FromArgb("#44EF4444");
                _heroSubtitleLabel.Text = "Touch shield is active! Double-tap or hold the floating button for 1 second to unlock.";
                _quickLockButton.Text = "Unlock Screen Now";
            }
            else
            {
                _heroBadgeLabel.Text = "🟢 FLOATING BUTTON ACTIVE (UNLOCKED)";
                _heroBadgeLabel.TextColor = Color.FromArgb("#22C55E");
                _heroBadge.BackgroundColor = Color.FromArgb("#1A22C55E");
                _heroBadge.Stroke = Color.FromArgb("#4422C55E");
                _heroSubtitleLabel.Text = "The floating button is on screen! Switch to your video call and tap the button whenever your toddler grabs the phone.";
                _quickLockButton.Text = "Lock Screen Now";
            }

            _primaryToggleButton.Text = "Stop Floating Button";
            _primaryToggleButton.BackgroundColor = Color.FromArgb("#EF4444");
            _primaryToggleButton.TextColor = Colors.White;
            _quickLockButton.IsVisible = true;
        }

        // 2. Permission status rows
        if (hasOverlay)
        {
            _overlayPermStatusLabel.Text = "✓ Granted";
            _overlayPermStatusLabel.TextColor = Color.FromArgb("#22C55E");
            _overlayPermButton.Text = "Granted";
            _overlayPermButton.BackgroundColor = Color.FromArgb("#1E293B");
            _overlayPermButton.TextColor = Color.FromArgb("#94A3B8");
        }
        else
        {
            _overlayPermStatusLabel.Text = "• Required to float button";
            _overlayPermStatusLabel.TextColor = Color.FromArgb("#F59E0B");
            _overlayPermButton.Text = "Allow";
            _overlayPermButton.BackgroundColor = Color.FromArgb("#F59E0B");
            _overlayPermButton.TextColor = Color.FromArgb("#0B0F19");
        }

        if (hasAccess)
        {
            _accessPermStatusLabel.Text = "✓ Full Edge & Swipe Lock Active";
            _accessPermStatusLabel.TextColor = Color.FromArgb("#22C55E");
            _accessPermButton.Text = "Settings";
            _accessPermButton.BackgroundColor = Color.FromArgb("#1E293B");
            _accessPermButton.TextColor = Color.FromArgb("#94A3B8");
        }
        else
        {
            _accessPermStatusLabel.Text = "• Optional (Recommended for toddlers)";
            _accessPermStatusLabel.TextColor = Color.FromArgb("#38BDF8");
            _accessPermButton.Text = "Enable";
            _accessPermButton.BackgroundColor = Color.FromArgb("#1E293B");
            _accessPermButton.TextColor = Color.FromArgb("#38BDF8");
        }

        // 3. Chip selections
        int sizeDp = LockySettings.ButtonSizeDp;
        StyleChip(_sizeMiniBtn, sizeDp <= 36);
        StyleChip(_sizeCompactBtn, sizeDp > 36 && sizeDp <= 44);
        StyleChip(_sizeStandardBtn, sizeDp > 44);

        float opacity = LockySettings.IdleOpacity;
        StyleChip(_opacitySubtleBtn, opacity < 0.65f);
        StyleChip(_opacityBalancedBtn, opacity >= 0.65f && opacity < 0.88f);
        StyleChip(_opacitySolidBtn, opacity >= 0.88f);

        int interval = LockySettings.FloatIntervalSeconds;
        StyleChip(_float4sBtn, interval > 0 && interval <= 5);
        StyleChip(_float8sBtn, interval > 5);
        StyleChip(_floatTapOnlyBtn, interval == 0);

        if (_keepAwakeSwitch.IsToggled != LockySettings.KeepScreenOnWhileLocked)
        {
            _keepAwakeSwitch.IsToggled = LockySettings.KeepScreenOnWhileLocked;
        }
    }

    private static void StyleChip(Button btn, bool selected)
    {
        if (selected)
        {
            btn.BackgroundColor = Color.FromArgb("#22C55E");
            btn.TextColor = Color.FromArgb("#052E16");
        }
        else
        {
            btn.BackgroundColor = Color.FromArgb("#1E293B");
            btn.TextColor = Color.FromArgb("#CBD5E1");
        }
    }
}
