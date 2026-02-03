# Localization Service

The UX Manager includes a comprehensive localization service for managing multi-language support in your application.

---

## Overview

The LocalizationService provides:

- Multi-language support with automatic fallback to default locale
- Two loading strategies: **Resources** (bundled) or **Addressables** (on-demand)
- Standard string keys and enum localization
- Format string support with parameters
- Culture-aware date/time/number formatting
- Runtime locale switching with UI refresh events

**Key Concepts:**

- **Catalog**: JSON file containing translations for one locale (e.g., `en-US.json`, `es-ES.json`)
- **Keys**: String identifiers for translations (e.g., "welcome_message", "button_continue")
- **Locale Code**: Standard locale identifier (e.g., "en-US", "es-ES", "fr-FR", "ja-JP")
- **Loading Strategy**: How catalogs are loaded (Resources or Addressables)

---

## Setting up with Resources

Resources strategy bundles all locale catalogs with your application. Simple setup, no external dependencies.

**When to use:**

- Small projects with few locales (2-4 languages)
- No need for remote updates
- Simpler deployment workflow

**Setup Steps:**

1. **Create LocalizationServiceProfile**
   - Right-click in Project → Create → Reality Collective → UX Manager → Localization Service Profile
   - Name it `LocalizationServiceProfile`

2. **Configure Profile:**
   - **Default Locale**: Set to your primary language (e.g., "en-US")
   - **Loading Strategy**: Set to `Resources`
   - **Catalog Path**: Set to `Assets/Resources/Localization/Catalogs`
   - **Supported Locales**: Add all locale codes you'll support (e.g., "en-US", "es-ES", "fr-FR")

3. **Create Catalog Directory:**

   ```
   Assets/
     Resources/
       Localization/
         Catalogs/
           en-US.json
           es-ES.json
           fr-FR.json
   ```

   **CRITICAL:** Catalogs MUST be in a `Resources` folder to be included in builds.

4. **Add Localization Keys to Profile:**
   - In the profile Inspector, expand `Localization Keys`
   - Add each key with its display name (e.g., key: "welcome_message" → displayName: "WelcomeMessage")
   - This generates `LocalizationKeys.g.cs` with typed constants

5. **Register Service:**
   - Add LocalizationServiceProfile to your ServiceProvidersProfile
   - Service will initialize automatically on app start

**That's it!** Your catalogs are now bundled with the app and loaded from Resources.

---

## Setting up with Addressables

Addressables strategy enables on-demand loading and remote catalog updates. More complex setup but better for production.

**When to use:**

- Multiple locales (5+ languages)
- Need remote catalog updates without app redeployment
- Memory optimization (load only active locale)
- Production apps with frequent translation updates

**Prerequisites:**

1. Install `com.unity.addressables` package (Window → Package Manager → Unity Registry → Addressables)
2. Initialize Addressables: Window → Asset Management → Addressables → Groups → Create Addressables Settings

**Setup Steps:**

1. **Create LocalizationServiceProfile**
   - Right-click in Project → Create → Reality Collective → UX Manager → Localization Service Profile

2. **Configure Profile:**
   - **Default Locale**: Set to your primary language (e.g., "en-US")
   - **Loading Strategy**: Set to `Addressables`
   - **Catalog Path**: Leave empty `""` for direct addressing (recommended)
   - **Supported Locales**: Add all locale codes (e.g., "en-US", "es-ES", "fr-FR")

3. **Create Catalog Files:**
   Create JSON files anywhere in your project (they'll be marked as Addressable, location doesn't matter):

   ```
   Assets/
     Localization/
       Catalogs/
         en-US.json
         es-ES.json
         fr-FR.json
   ```

4. **Create Addressable Group (Recommended):**
   - Window → Asset Management → Addressables → Groups
   - Click "Create New Group" → Name: "Localization Catalogs"

5. **Mark Catalogs as Addressable:**
   For EACH catalog file:
   - Select catalog in Project window (e.g., `en-US.json`)
   - In Inspector, check **"Addressable"**
   - **CRITICAL:** Set Addressable Name to match locale code EXACTLY:
     - `en-US.json` → Addressable Name: `en-US`
     - `es-ES.json` → Addressable Name: `es-ES`
     - `fr-FR.json` → Addressable Name: `fr-FR`
   - Assign to "Localization Catalogs" group

6. **Add Localization Keys to Profile:**
   - Same as Resources setup (add keys with display names)

7. **Build Addressables Content:**
   - Window → Asset Management → Addressables → Groups
   - Click "Build → New Build → Default Build Script"

8. **Register Service:**
   - Add LocalizationServiceProfile to your ServiceProvidersProfile

**Addressing Options:**

The service supports two addressing patterns based on `Catalog Path`:

- **Direct (Recommended):** `Catalog Path = ""` → Loads by locale code directly ("en-US", "es-ES")
- **Path-based:** `Catalog Path = "Localization/Catalogs"` → Loads as "Localization/Catalogs/en-US"
- **Label-based:** `Catalog Path = "locale"` → Loads as "locale_en-US", "locale_es-ES"

**Remote Content Distribution (Optional):**

1. Select "Localization Catalogs" group in Addressables Groups window
2. Enable "Build Remote Catalog"
3. Set "Build & Load Paths" to remote server URL
4. Update catalogs on CDN without rebuilding app

**Troubleshooting:**

- **"Failed to load catalog"**: Addressable Name doesn't match locale code exactly
- **"InvalidKeyException"**: Catalog not marked as Addressable or not built
- **Still using Resources**: Loading Strategy not set to Addressables in profile

---

## Creating a Catalog

Localization catalogs are JSON files, one per locale. Each catalog contains translations for all keys in that language.

### Basic Structure

```json
{
  "locale": "en-US",
  "version": "1.0.0",
  "keys": {
    "welcome_message": "Welcome to our app!",
    "button_continue": "Continue"
  }
}
```

**Required Fields:**

- `locale`: Locale code matching filename (e.g., "en-US")
- `version`: Catalog version for tracking updates
- `keys`: Dictionary of all translations

### Keys

Standard string keys provide simple text translations.

**Naming Convention:** `screenname_element` or `category_item`

**Examples:**

```json
{
  "keys": {
    "mainscreen_title": "Home",
    "mainscreen_time": "Time",
    "mainscreen_view": "View",
    "shopscreen_buy_button": "Purchase",
    "shopscreen_buy_format": "Buy for {0}",
    "settings_language": "Language",
    "settings_notifications": "Notifications"
  }
}
```

**Format Strings:**

Use `{0}`, `{1}`, etc. for dynamic values:

```json
{
  "keys": {
    "greeting_format": "Hello, {0}!",
    "items_count": "You have {0} items",
    "purchase_confirmation": "Purchased {0} for {1}",
    "session_duration": "{0} minutes remaining"
  }
}
```

**Guidelines:**

- Use lowercase with underscores: `button_continue`, not `buttonContinue`
- Prefix with screen/category: `shopscreen_title`, not just `title`
- Keep keys consistent across ALL locale catalogs
- Use format strings instead of string concatenation

### Enums

Enum values can be localized using a special key format: `enum_{EnumTypeName}_{EnumValue}`

**Example Enums:**

```csharp
public enum SessionType { Breathing, Meditation, Yoga }
public enum Difficulty { Beginner, Intermediate, Advanced }
public enum CardType { Silent, Guided, Group }
```

**Catalog Keys:**

```json
{
  "keys": {
    "enum_SessionType_Breathing": "Breathing Exercise",
    "enum_SessionType_Meditation": "Meditation",
    "enum_SessionType_Yoga": "Yoga Session",
    "enum_Difficulty_Beginner": "Beginner",
    "enum_Difficulty_Intermediate": "Intermediate", 
    "enum_Difficulty_Advanced": "Advanced",
    "enum_CardType_Silent": "Silent Session",
    "enum_CardType_Guided": "Guided Session",
    "enum_CardType_Group": "Group Session"
  }
}
```

**Pattern:** For enum `MyEnum.Value` → key is `enum_MyEnum_Value`

**Complete Example:**

```json
{
  "locale": "en-US",
  "version": "1.0.0",
  "keys": {
    "app_title": "Meditation App",
    "mainscreen_welcome": "Welcome back!",
    "greeting_format": "Hello, {0}!",
    "session_duration": "{0} minutes",
    "items_purchased": "{0} of {1} purchased",
    "enum_SessionType_Breathing": "Breathing",
    "enum_SessionType_Meditation": "Meditation",
    "enum_Difficulty_Beginner": "Beginner",
    "enum_Difficulty_Advanced": "Advanced"
  }
}
```

---

## Reading Localization

### Accessing the Service

```csharp
using RealityCollective.ServiceFramework.Services;
using RealityCollective.UXManager.Interfaces.Localization;

private ILocalizationService localizationService;

private void Awake()
{
    localizationService = ServiceManager.Instance?.GetService<ILocalizationService>();
}
```

### Keys

**Simple String Lookup:**

```csharp
// Using generated constants (recommended)
string title = localizationService.GetString(LocalizationKeys.AppTitle);
string welcome = localizationService.GetString(LocalizationKeys.MainscreenWelcome);
```

**Format Strings with Parameters:**

```csharp
// Single parameter
string greeting = localizationService.GetString(LocalizationKeys.GreetingFormat, userName);
// Catalog: "greeting_format": "Hello, {0}!" → Result: "Hello, John!"

string duration = localizationService.GetString(LocalizationKeys.SessionDuration, 30);
// Catalog: "session_duration": "{0} minutes" → Result: "30 minutes"

// Multiple parameters
string purchased = localizationService.GetString(LocalizationKeys.ItemsPurchased, 5, 10);
// Catalog: "items_purchased": "{0} of {1} purchased" → Result: "5 of 10 purchased"
```

**Safe Retrieval with Fallback:**

```csharp
if (localizationService.TryGetString(LocalizationKeys.OptionalMessage, out string message, "Default"))
{
    Debug.Log($"Found: {message}");
}
else
{
    Debug.Log($"Using fallback: {message}");
}
```

### Enums

**Getting Enum Display Names:**

```csharp
public enum SessionType { Breathing, Meditation, Yoga }

SessionType currentType = SessionType.Meditation;
string displayName = localizationService.GetEnumDisplayName(currentType);
// Looks up: "enum_SessionType_Meditation" → Result: "Meditation"
```

**Populating Dropdowns:**

```csharp
public enum Difficulty { Beginner, Intermediate, Advanced }

// Get all localized names for dropdown
var difficulties = System.Enum.GetValues(typeof(Difficulty));
foreach (Difficulty level in difficulties)
{
    string localizedName = localizationService.GetEnumDisplayName(level);
    dropdown.AddOption(localizedName);
}
```

### Best Practices

**1. Always Use Generated Constants**

```csharp
// ✅ GOOD: Type-safe, refactorable
string text = localizationService.GetString(LocalizationKeys.WelcomeMessage);

// ❌ BAD: String literal, prone to typos
string text = localizationService.GetString("welcome_message");
```

**2. Subscribe to Locale Changes**

```csharp
private void OnEnable()
{
    if (localizationService != null)
    {
        localizationService.OnLocaleChanged += HandleLocaleChanged;
    }
}

private void OnDisable()
{
    if (localizationService != null)
    {
        localizationService.OnLocaleChanged -= HandleLocaleChanged;
    }
}

private void HandleLocaleChanged(string newLocale)
{
    // Refresh all UI text
    titleLabel.text = localizationService.GetString(LocalizationKeys.ScreenTitle);
    buttonLabel.text = localizationService.GetString(LocalizationKeys.ButtonContinue);
}
```

**3. Use Format Strings, Not Concatenation**

```csharp
// ✅ GOOD: Translators can reorder parameters
// Catalog: "greeting_format": "Welcome, {0}!"
string text = localizationService.GetString(LocalizationKeys.GreetingFormat, userName);

// ❌ BAD: Can't be localized properly (word order varies by language)
string text = "Welcome, " + userName + "!";
```

**4. Use CultureInfo for Formatting**

```csharp
var culture = localizationService.GetCurrentCultureInfo();

// Date/time formatting
DateTime date = DateTime.Now;
string formattedDate = date.ToString("D", culture);  // Long date
string formattedTime = date.ToString("t", culture);  // Short time

// Number/currency formatting
decimal price = 1234.56m;
string formattedPrice = price.ToString("C", culture); // $1,234.56 or 1.234,56€
```

**5. Localize All Enum Values**

```csharp
// ✅ GOOD: Enum values localized
string typeName = localizationService.GetEnumDisplayName(SessionType.Meditation);

// ❌ BAD: Hard-coded English
string typeName = SessionType.Meditation.ToString(); // Always "Meditation"
```

**6. Test with Pseudo-Locale**

Add a "test" locale with bracketed text to catch missing translations:

```json
{
  "locale": "test",
  "keys": {
    "welcome_message": "[Ŵëłçömê Mëššågê]",
    "button_continue": "[Çöñţîñûë]"
  }
}
```

**7. Keep Catalogs Synchronized**

Ensure all locale catalogs have the SAME keys. Missing keys fall back to default locale, causing mixed languages in UI.

---

## API Reference

### LocalizationService

Complete reference for `ILocalizationService` interface.

## Properties

### `string CurrentLocale { get; }`

Gets the currently active locale code.

**Example:**

- Set the default locale (e.g., "en-US")
- Add supported locale codes (e.g., "es-ES", "fr-FR", "ja-JP")

1. **Choose a loading strategy:**
   - **Resources** (Default): Catalogs stored in `Assets/Resources/Localization/Catalogs/`
   - **Addressables** (Optional): On-demand loading with remote update support

2. **Create catalog JSON files** for each locale (see "Catalog Structure" below)

3. **Add localization keys** to the profile's `Localization Keys` list for code generation

4. **Use in code:**

   ```csharp
   string text = LocalizationService.GetString(LocalizationKeys.WelcomeMessage);
   ```

## Catalog Structure

Localization catalogs are JSON files stored per locale. Each catalog follows this structure:

```json
{
  "locale": "en-US",
  "version": "1.0.0",
  "keys": {
    "welcome_message": "Welcome to our app!",
    "button_continue": "Continue",
    "greeting_format": "Hello, {0}!",
    "items_count": "You have {0} items",
    "enum_CardType_Silent": "Silent Session",
    "enum_CardType_Guided": "Guided Session",
    "enum_CardType_Group": "Group Session"
  }
}
```

**Required Fields:**

- `locale`: The locale code (e.g., "en-US", "es-ES")
- `version`: Catalog version for tracking updates
- `keys`: Dictionary of key-value pairs for translations

**Key Types:**

1. **Standard String Keys** (e.g., "welcome_message")
   - Simple text translations
   - Convention: `screenname_element` (e.g., "mainscreen_title", "shopscreen_buy_button")

2. **Format String Keys** (e.g., "greeting_format")
   - Support `string.Format()` parameters using `{0}`, `{1}`, etc.
   - Use for dynamic text with placeholders
   - Example: "You have {0} items in your cart"

3. **Enum Display Name Keys** (e.g., "enum_CardType_Silent")
   - Format: `enum_{EnumTypeName}_{EnumValue}`
   - Used to localize enum values
   - Example: For `CardType.Silent` → key is "enum_CardType_Silent"

**Example with all key types:**

```json
{
  "locale": "en-US",
  "version": "1.0.0",
  "keys": {
    "app_title": "Meditation App",
    "welcome_user": "Welcome back, {0}!",
    "session_duration": "{0} minutes remaining",
    "items_purchased": "You've purchased {0} out of {1} items",
    "enum_SessionType_Breathing": "Breathing Exercise",
    "enum_SessionType_Meditation": "Meditation",
    "enum_SessionType_Yoga": "Yoga Session",
    "enum_Difficulty_Beginner": "Beginner",
    "enum_Difficulty_Intermediate": "Intermediate",
    "enum_Difficulty_Advanced": "Advanced"
  }
}
```

## Using Localization in Code

**Accessing the Service:**

```csharp
using RealityCollective.ServiceFramework.Services;
using RealityCollective.UXManager.Interfaces.Localization;

// Get service instance
private ILocalizationService localizationService;
localizationService = ServiceManager.Instance?.GetService<ILocalizationService>();
```

**Getting Standard Strings:**

```csharp
// Simple string lookup (using generated constants)
string title = localizationService.GetString(LocalizationKeys.AppTitle);

// With format parameters
string greeting = localizationService.GetString(LocalizationKeys.WelcomeUser, userName);
string duration = localizationService.GetString(LocalizationKeys.SessionDuration, minutesRemaining);

// Multiple parameters
string purchased = localizationService.GetString(LocalizationKeys.ItemsPurchased, 5, 10);
// Result: "You've purchased 5 out of 10 items"
```

**Getting Enum Display Names:**

```csharp
// For enum values
public enum SessionType { Breathing, Meditation, Yoga }
public enum Difficulty { Beginner, Intermediate, Advanced }

SessionType currentType = SessionType.Meditation;
Difficulty currentLevel = Difficulty.Intermediate;

// Get localized enum display name
string typeName = localizationService.GetEnumDisplayName(currentType);
// Result: "Meditation" (from "enum_SessionType_Meditation" key)

string levelName = localizationService.GetEnumDisplayName(currentLevel);
// Result: "Intermediate" (from "enum_Difficulty_Intermediate" key)
```

**Safe String Retrieval:**

```csharp
// Try pattern with fallback
if (localizationService.TryGetString(LocalizationKeys.OptionalMessage, out string message, "Default Text"))
{
    // Key was found in catalog
    Debug.Log($"Found: {message}");
}
else
{
    // Key not found, fallback used
    Debug.Log($"Fallback used: {message}");
}
```

**Changing Locale at Runtime:**

```csharp
// Switch to Spanish
localizationService.SetLocale("es-ES");

// Listen for locale changes to update UI
localizationService.OnLocaleChanged += OnLocaleChangedHandler;

private void OnLocaleChangedHandler(string newLocale)
{
    Debug.Log($"Locale changed to: {newLocale}");
    // Refresh all UI text elements
    RefreshAllText();
}
```

**Culture-Aware Formatting:**

```csharp
// Get CultureInfo for date/time/number formatting
var cultureInfo = localizationService.GetCurrentCultureInfo();

// Use for date formatting
DateTime now = DateTime.Now;
string formattedDate = now.ToString("D", cultureInfo); // Long date pattern
string formattedTime = now.ToString("t", cultureInfo); // Short time pattern

// Use for number formatting
decimal price = 1234.56m;
string formattedPrice = price.ToString("C", cultureInfo); // Currency format
```

## Loading Strategies

**Resources (Default)**

- Catalogs bundled with the application
- All locales loaded into memory
- No external dependencies required
- Ideal for smaller projects with few locales

**Addressables (Optional)**

- Requires `com.unity.addressables` package
- On-demand locale loading (reduces memory usage)
- Remote catalog updates without app redeployment
- Better for production apps with multiple locales
- Automatically enabled via version define when Addressables is installed

### Addressables Setup (Step-by-Step)

**Prerequisites:**

1. Install `com.unity.addressables` package via Package Manager (Window → Package Manager → Unity Registry → Addressables)
2. Initialize Addressables: Window → Asset Management → Addressables → Groups → Create Addressables Settings (if not already done)

**Configuration Steps:**

1. **Create an Addressable Group for Localization** (recommended for organization):
   - Window → Asset Management → Addressables → Groups
   - Click "Create New Group" → Name it "Localization Catalogs"

2. **Mark each catalog JSON file as Addressable:**
   - Select your catalog file (e.g., `en-US.json`) in the Project window
   - In the Inspector, check "Addressable"
   - **CRITICAL:** Set the Addressable Name to match the locale code exactly
     - For `en-US.json` → Addressable Name: `en-US`
     - For `es-ES.json` → Addressable Name: `es-ES`
     - For `fr-FR.json` → Addressable Name: `fr-FR`
   - Assign to the "Localization Catalogs" group
   - Repeat for ALL locale catalog files

3. **Configure LocalizationServiceProfile:**
   - Open your LocalizationServiceProfile asset
   - Set `Loading Strategy` to `Addressables`
   - Set `Catalog Path` to one of these options:

     **Option A: Direct Addressing (Recommended)**
     - Leave `Catalog Path` empty or set to just the locale code pattern
     - The service will load each catalog by its exact Addressable Name (e.g., "en-US")
     - Example: `Catalog Path = ""` → Loads by locale code directly

     **Option B: Group/Label-Based Addressing**
     - Set `Catalog Path` to a label you've assigned to all catalog assets
     - Example: `Catalog Path = "locale"` → Loads as "locale_en-US", "locale_es-ES", etc.
     - You must add this label to each catalog asset in Addressables Groups window

4. **Verify Configuration:**
   - Window → Asset Management → Addressables → Groups
   - Ensure all catalog JSON files appear in your group
   - Verify Addressable Names match locale codes exactly
   - Check that all catalogs have the same label (if using Option B)

5. **Build Addressables Content:**
   - Window → Asset Management → Addressables → Groups
   - Click "Build → New Build → Default Build Script"
   - This creates the required catalog and asset bundles

6. **Test in Play Mode:**
   - Enter Play Mode and check Console for LocalizationService initialization logs
   - Should see "Loaded X keys from '{locale}' (Addressables)"
   - If errors occur, verify Addressable Names match supported locale codes

**Addressing Scheme Details:**

The service supports two addressing patterns based on your `Catalog Path` setting:

- **Path-based** (contains "/"): Builds address as `{CatalogPath}/{localeCode}`
  - Example: `Catalog Path = "Localization/Catalogs"` → Loads "Localization/Catalogs/en-US"
  - Use this if organizing catalogs in folder structure within Addressables
  
- **Direct/Label-based** (no "/"): Loads by `{localeCode}` directly, or `{CatalogPath}_{localeCode}` if path specified
  - Example: `Catalog Path = ""` → Loads "en-US" directly (Recommended)
  - Example: `Catalog Path = "locale"` → Loads "locale_en-US"
  - Use this for flat addressing scheme

**Troubleshooting:**

- **"Failed to load catalog from Addressables"**: Addressable Name doesn't match locale code
- **"InvalidKeyException"**: Catalog not marked as Addressable or not built
- **Service still using Resources**: `Loading Strategy` not set to Addressables in profile
- **Keys not found**: Catalog JSON format incorrect or missing "keys" object

**Remote Content Distribution (Optional):**

To enable remote catalog updates:

1. In Addressables Groups window, select your "Localization Catalogs" group
2. Enable "Build Remote Catalog"
3. Set "Build & Load Paths" to a remote server URL
4. Configure your CDN/hosting for the built bundles
5. Update catalogs remotely without rebuilding the app

**Note:** The Addressables implementation uses conditional compilation (`#if UNITY_ADDRESSABLES`), so the service works perfectly without the Addressables package installed.

---

## API Reference

### LocalizationService

Complete reference for `ILocalizationService` interface.

## Properties

### `string CurrentLocale { get; }`

Gets the currently active locale code.

**Example:**

```csharp
string locale = localizationService.CurrentLocale; // "en-US"
```

---

### `string DefaultLocale { get; }`

Gets the default/fallback locale code configured in the profile.

**Example:**

```csharp
string defaultLocale = localizationService.DefaultLocale; // "en-US"
```

---

### `IReadOnlyList<string> SupportedLocales { get; }`

Gets all supported locale codes configured in the profile.

**Example:**

```csharp
var locales = localizationService.SupportedLocales;
// Returns: ["en-US", "es-ES", "fr-FR", "test"]

foreach (string locale in locales)
{
    Debug.Log($"Supported: {locale}");
}
```

---

## Methods

### `string GetString(string key, params object[] parameters)`

Retrieves a localized string by key with optional format parameters.

**Parameters:**

- `key`: Localization key (e.g., "mainscreen_title")
- `parameters`: Optional parameters for `string.Format()`

**Returns:**

- Localized string from current locale
- Falls back to default locale if key not found in current locale
- Returns the key itself if not found in any locale

**Example:**

```csharp
// Simple string
string title = localizationService.GetString(LocalizationKeys.AppTitle);

// With format parameters
string greeting = localizationService.GetString(LocalizationKeys.WelcomeUser, "John");
// Catalog: "welcome_user": "Hello, {0}!" → Result: "Hello, John!"

// Multiple parameters
string message = localizationService.GetString(LocalizationKeys.ItemsCount, 5, 10);
// Catalog: "items_count": "{0} of {1} items" → Result: "5 of 10 items"
```

---

### `bool TryGetString(string key, out string value, string fallback = "")`

Attempts to retrieve a localized string, returning success status.

**Parameters:**

- `key`: Localization key
- `value`: Output parameter for the retrieved string
- `fallback`: Fallback string if key not found (default: empty string)

**Returns:**

- `true` if key found in current or default locale
- `false` if key not found (fallback used)

**Example:**

```csharp
if (localizationService.TryGetString("optional_key", out string text, "Default Text"))
{
    Debug.Log($"Found in catalog: {text}");
}
else
{
    Debug.Log($"Using fallback: {text}");
}
```

---

### `string GetEnumDisplayName(Enum value)`

Gets a localized display name for an enum value.

**Parameters:**

- `value`: Enum value to localize

**Returns:**

- Localized string for the enum value
- Key format used: `enum_{EnumTypeName}_{EnumValue}`

**Example:**

```csharp
public enum CardType { Silent, Guided, Group }

CardType type = CardType.Guided;
string displayName = localizationService.GetEnumDisplayName(type);
// Looks up key: "enum_CardType_Guided"
// Catalog: "enum_CardType_Guided": "Guided Session"
// Result: "Guided Session"
```

**Catalog example for enums:**

```json
{
  "keys": {
    "enum_CardType_Silent": "Silent Session",
    "enum_CardType_Guided": "Guided Session",
    "enum_CardType_Group": "Group Session"
  }
}
```

---

### `void SetLocale(string localeCode)`

Changes the active locale and reloads catalog data.

**Parameters:**

- `localeCode`: Locale code to switch to (must be in `SupportedLocales`)

**Behavior:**

- Validates locale is supported
- Loads new locale catalog
- Falls back to default locale if loading fails
- Fires `OnLocaleChanged` event on success
- Clears cached `CultureInfo`

**Example:**

```csharp
// Switch to Spanish
localizationService.SetLocale("es-ES");

// Switch based on user preference
string userLocale = PlayerPrefs.GetString("preferred_locale", "en-US");
localizationService.SetLocale(userLocale);
```

---

### `System.Globalization.CultureInfo GetCurrentCultureInfo()`

Gets the `CultureInfo` for the current locale, used for culture-aware formatting.

**Returns:**

- `CultureInfo` for current locale
- Falls back to system default culture if locale invalid

**Usage:**

- Date/time formatting
- Number formatting (currency, decimals, percentages)
- Comparison and sorting rules

**Example:**

```csharp
var cultureInfo = localizationService.GetCurrentCultureInfo();

// Date formatting
DateTime date = DateTime.Now;
string longDate = date.ToString("D", cultureInfo);  // Full date
string shortDate = date.ToString("d", cultureInfo); // Short date
string time = date.ToString("t", cultureInfo);      // Short time

// Number formatting
decimal price = 1234.56m;
string currency = price.ToString("C", cultureInfo);    // Currency: $1,234.56 or 1.234,56€
string number = price.ToString("N2", cultureInfo);     // Number: 1,234.56 or 1.234,56
string percent = 0.85m.ToString("P", cultureInfo);     // Percent: 85% or 85 %

// Custom formatting
var nfi = cultureInfo.NumberFormat;
Debug.Log($"Currency symbol: {nfi.CurrencySymbol}");
Debug.Log($"Decimal separator: {nfi.NumberDecimalSeparator}");
```

**Important:** This is the CENTRAL POINT for device locale discovery. DO NOT directly access `Application.systemLanguage` in UI code - always use this method.

---

## Events

### `event Action<string> OnLocaleChanged`

Fired when the locale successfully changes via `SetLocale()`.

**Event Parameter:**

- `string`: The new locale code

**Usage:**
Subscribe to refresh UI text when user changes language at runtime.

**Example:**

```csharp
private void Awake()
{
    localizationService.OnLocaleChanged += HandleLocaleChanged;
}

private void OnDestroy()
{
    if (localizationService != null)
    {
        localizationService.OnLocaleChanged -= HandleLocaleChanged;
    }
}

private void HandleLocaleChanged(string newLocale)
{
    Debug.Log($"Locale changed to: {newLocale}");
    
    // Refresh all text in UI
    titleLabel.text = localizationService.GetString(LocalizationKeys.ScreenTitle);
    buttonLabel.text = localizationService.GetString(LocalizationKeys.ButtonContinue);
    
    // Refresh date/time displays
    UpdateDateTimeFormatting();
}
```

---

### Best Practices

1. **Always use generated constants** (`LocalizationKeys.*`) instead of raw strings
2. **Subscribe to `OnLocaleChanged`** in UI components to update text dynamically
3. **Use `GetCurrentCultureInfo()`** for date/time/number formatting instead of system culture
4. **Define enum keys in catalogs** for user-facing enum values
5. **Use format parameters** (`{0}`, `{1}`) for dynamic text instead of string concatenation
6. **Test with pseudo-locale** ("test" locale) to catch missing translations
7. **Keep keys consistent** across all locale catalogs (same keys in en-US, es-ES, fr-FR, etc.)
