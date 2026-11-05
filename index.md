# Privacy Policy for Locky

**Effective Date:** November 5, 2026  
**Application Name:** Locky (`com.locky.touchguard`)

## 1. Overview
**Locky** is an offline floating screen-touch lock utility designed to prevent accidental screen taps by babies and toddlers during family video calls.

## 2. Zero Data Collection
**Locky does not collect, store, transmit, or share any personal or non-personal user data.**
- **100% Offline:** Locky does not request the `android.permission.INTERNET` permission and cannot connect to the internet.
- **No Analytics or Ads:** Locky contains zero third-party analytics, tracking SDKs, or advertisements.

## 3. Permissions & Optional Accessibility Service Disclosure
- **Display Over Other Apps (`android.permission.SYSTEM_ALERT_WINDOW`):** Used solely to display the floating lock/unlock button and transparent touch-blocking shield over the screen when activated by the user.
- **Optional Accessibility Service (`LockyAccessibilityService`):** If explicitly enabled by the user in Android Accessibility Settings, Locky uses an Accessibility overlay (`TYPE_ACCESSIBILITY_OVERLAY`) and hardware/gesture key filtering solely to prevent accidental status-bar pull-downs and Back/Home navigation swipes while the screen touch shield is locked. Locky sets `canRetrieveWindowContent="false"` and **never** reads, inspects, logs, or stores any screen content, text, or user interactions.

## 4. Contact Us
If you have any questions about this Privacy Policy, please contact the developer via the support email listed on the Google Play Store listing page for **Locky**.
