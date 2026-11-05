# Google Play Console Store Listing & Release Guide for **Locky**

---

## 1. Copy-Paste Store Listing Fields

### Package Name (`ApplicationId`)
```text
com.locky.touchguard
```

### App Name (Max 30 characters)
```text
Locky: Video Call Touch Lock
```

### Short Description (Max 80 characters)
```text
Floating touch lock for toddler-proof family video calls. 100% offline & ad-free
```

### Full Description (Max 4000 characters)
```text
Locky is an ultra-lightweight, 100% offline floating screen-touch lock built for parents during video calls with grandparents, family, and friends.

Toddlers love grabbing the phone during video calls—accidentally hanging up, muting the microphone, flipping the camera, or swiping out of the call. Locky solves this with a tiny, non-intrusive floating indicator dot that blocks accidental screen touches while your video call continues uninterrupted.

★ MINIMAL GREEN & RED DOT INDICATOR ★
• Unlocked Mode (Green Dot): A tiny floating green indicator dot rests unobtrusively on your screen and can be freely dragged anywhere without covering faces or controls.
• 1-Tap Touch Shield (Red Dot): Tap the green dot once during a video call to lock all screen touches. The indicator turns red and activates a 100% transparent full-screen touch shield—keeping your video call completely visible while toddler pokes and swipes are absorbed.

★ SMART TODDLER EVASION & EASY PARENT UNLOCK ★
• Toddler Tap-Dodge: While locked, the tiny red dot gently hops to random positions on the screen—and if a toddler single-taps it, it immediately jumps away to a new spot out of reach.
• Effortless Parent Unlock: Double-tap the red dot OR press-and-hold it in place for 1 second (until the green progress ring completes) to unlock immediately.
• Emergency Notification Unlock: You can also unlock or stop Locky at any time from its silent status notification.

★ CUSTOMIZABLE & ULTRA-LOW BATTERY ★
• Choose your indicator size (Tiny 18dp, Small 24dp, Medium 32dp), idle transparency (55%, 78%, 95%), and random hop interval (Every 4s, Every 8s, or On Tap Only).
• Optional Accessibility Edge Shield to also block top notification pull-downs and Back/Home navigation swipes while locked.
• 100% Offline & Private: Locky does not request Internet permission, contains zero ads, and collects zero data.
```

---

## 2. Generated Graphics Ready to Upload (`PlayStoreAssets/`)

| Play Console Slot | File Path | Dimensions |
| :--- | :--- | :--- |
| **App Icon** | `PlayStoreAssets/icon-512x512.png` | `512 × 512 px` |
| **Feature Graphic** | `PlayStoreAssets/feature-graphic-1024x500.png` | `1024 × 500 px` |
| **Phone Screenshots (16:9)** | `PlayStoreAssets/Screenshots/Phone/01-video-call-unlocked-1920x1080.png`<br>`PlayStoreAssets/Screenshots/Phone/02-video-call-locked-shield-1920x1080.png`<br>`PlayStoreAssets/Screenshots/Phone/03-toddler-tap-dodge-1920x1080.png`<br>`PlayStoreAssets/Screenshots/Phone/04-parent-hold-unlock-1920x1080.png`<br>`PlayStoreAssets/Screenshots/Phone/05-locky-dashboard-1920x1080.png` | `1920 × 1080 px` |
| **7-inch Tablet Screenshots** | `PlayStoreAssets/Screenshots/Tablet-7in/01-video-call-locked-shield-1920x1200.png`<br>`PlayStoreAssets/Screenshots/Tablet-7in/02-locky-dashboard-1920x1200.png` | `1920 × 1200 px` |
| **10-inch Tablet Screenshots** | `PlayStoreAssets/Screenshots/Tablet-10in/01-video-call-locked-shield-2560x1600.png`<br>`PlayStoreAssets/Screenshots/Tablet-10in/02-toddler-tap-dodge-2560x1600.png` | `2560 × 1600 px` |

---

## 3. Signed Release `.aab` Bundle & Keystore Details

- **Signed `.aab` Path:**
  `bin\Release\net9.0-android\android-arm64\publish\com.locky.touchguard-Signed.aab`
- **Keystore File:** `locky.keystore`
- **Key Alias:** `locky`
- **Keystore / Key Password:** `LockyKey2026!`
- **Privacy Policy URL (GitHub Pages):** `https://bee-t.github.io/Locky/`
