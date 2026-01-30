# Unity Localization MCP Server

A Model Context Protocol (MCP) server for managing Unity localization keys, translations, and catalogs. This server provides atomic operations for adding, updating, and validating localization data across all Unity localization files.

**For detailed architecture documentation, see [ARCHITECTURE.md](ARCHITECTURE.md) - covers design, operation maps, and technical implementation details.**

## Key Features

- ✅ **Configuration Detection**: Automatically discovers LocalizationService configuration from ServiceProvidersProfile
- ✅ **No Hardcoded Paths**: Uses actual catalog paths and settings from Unity project
- ✅ **Atomic Operations**: Add/update keys across profile + all catalogs + generated file in one operation
- ✅ **Validation**: Check consistency across all localization files
- ✅ **Batch Operations**: Add multiple keys at once
- ✅ **Auto-generation**: Automatically regenerates LocalizationKeys.g.cs
- ✅ **Status Reports**: Get comprehensive overview of localization state
- ✅ **Profile Updates**: Updates both `key` and `displayName` in profile entries

## How It Works

The server directly accesses your Unity project's LocalizationServiceProfile:

1. **Finds LocalizationServiceProfile.asset** - Directly locates your localization configuration
2. **Loads Configuration** - Reads catalog paths, supported locales, and settings
3. **Uses Real Configuration** - All operations use your project's actual localization setup

### What It Validates

- ✅ LocalizationServiceProfile.asset exists and is accessible
- ✅ All configured catalogs exist
- ✅ Profile has both `key` and `displayName` for each entry

If any requirement is missing, the server reports a clear error and stops.

## Quick Start

### Step 1: Install Dependencies

Navigate to the server directory and install/build:

```bash
cd Packages/com.realitycollective.uxmanager/MCP~/unity-localization-server
npm install
npm run build
```

Verify `dist/index.js` exists after building.

### Step 2: Configure VS Code MCP Settings

Add the unity-localization server to your VS Code MCP configuration file.

**File location by OS:**

- Windows: `%APPDATA%\Code\User\mcp.json`
- macOS: `~/Library/Application Support/Code/User/mcp.json`
- Linux: `~/.config/Code/User/mcp.json`

**Configuration to add:**

Open the file and add this entry to the `"servers"` object:

```json
"unity-localization": {
  "type": "stdio",
  "command": "node",
  "args": [
    "/path/to/YourProject/Packages/com.realitycollective.uxmanager/MCP~/unity-localization-server/dist/index.js"
  ],
  "env": {
    "UNITY_PROJECT_ROOT": "/path/to/YourProject",
    "LOCALIZATION_PROFILE_PATH": "Assets/ServiceProvidersProfile/LocalizationServiceProfile.asset"
  }
}
```

**⚠️ Important:**

- Replace `/path/to/YourProject` with your actual Unity project root (use absolute paths)
- Verify the path to `dist/index.js` is correct before saving
- Set `UNITY_PROJECT_ROOT` to your Unity project root directory
- `LOCALIZATION_PROFILE_PATH` is optional - defaults to `Assets/ServiceProvidersProfile/LocalizationServiceProfile.asset`. Only set if your profile is in a non-standard location.

### Step 3: Restart VS Code

1. Close VS Code completely
2. Reopen VS Code
3. Wait 2-3 seconds for the Localization MCP server to initialize

You should see this in the MCP server output:

```
[info] Connection state: Running
[info] Discovered 9 tools
```

### Step 4: Test the Server (Health Check)

In any VS Code chat or editor context, run the health check tool to verify everything is configured:

1. Call the `unity_localization_health_check` tool with empty parameters: `{}`
2. Expected response: `🟢 Status: OPERATIONAL` with all 6 checks passing

This verifies:

- ✅ LocalizationServiceProfile.asset is accessible
- ✅ LocalizationServiceProfile.asset is readable/writable
- ✅ All configured locale catalogs exist
- ✅ LocalizationKeys.g.cs file is accessible
- ✅ All profile keys are consistent across catalogs

**If health check fails**, see [Troubleshooting section](#troubleshooting) for diagnostic steps.

### Using the MCP Service

- **Add key**: Use `unity_localization_add_key` tool (single key with all translations at once)
- **Add multiple keys**: Use `unity_localization_batch_add_keys` for 3+ keys efficiently
- **Validate key**: Use `unity_localization_check_key_health` after adding to verify configuration and code usage
- **Update translation**: Use `unity_localization_update_key` to fix typos or modify existing translations
- **System check**: Use `unity_localization_validate` before releases

### Update Copilot Instructions

Edit `.github/copilot-instructions.md`:

1. **Add health check as first validation**: Update `AGENT VALIDATION CHECKLIST` section to include:
   - `1. **CHECK LOCALIZATION MCP HEALTH FIRST** - Before any localization work, run health_check tool`

2. **Document all MCP tools**: Add section listing all 9 available tools (`unity_localization_add_key`, `unity_localization_batch_add_keys`, `unity_localization_update_key`, `unity_localization_check_key_health`, `unity_localization_validate`, `unity_localization_get_status`, `unity_localization_get_key_info`, `unity_localization_remove_key`, `unity_localization_health_check`)

3. **Replace manual workflow with MCP workflow**: Update localization section to show MCP tool usage instead of manual file editing

---

## Expected Code Patterns

The server generates and expects proper C# localization patterns:

### ILocalizationService Usage

```csharp
// ✅ CORRECT: Use ILocalizationService and generated keys
private ILocalizationService _localizationService;
protected ILocalizationService LocalizationService 
    => _localizationService ??= ServiceManager.Instance?.GetService<ILocalizationService>();

// In UI generation
label.text = LocalizationService.GetString(LocalizationKeys.MyKey);

// ❌ NEVER use raw strings
label.text = LocalizationService.GetString("mykey"); // DON'T DO THIS
```

### Locale Change Handling

Components should handle locale changes:

```csharp
public override void HandleLocaleChanged(string newLocale)
{
    // Re-localize all text elements
    titleLabel.text = LocalizationService.GetString(LocalizationKeys.MyTitle);
    buttonText.text = LocalizationService.GetString(LocalizationKeys.MyButton);
}
```

### Custom Component Pattern

For custom VisualElements that display localized text:

```csharp
public class MyComponent : VisualElement
{
    private ILocalizationService localizationService;
    
    public ILocalizationService LocalizationService
    {
        get => localizationService;
        set => localizationService = value;
    }
    
    private string GetLocalizedText(string localizationKey)
    {
        if (localizationService == null)
        {
            return localizationKey; // Fallback
        }
        return localizationService.GetString(localizationKey);
    }
}

## Installation

### 1. Install Dependencies

Navigate to the MCP server directory and install dependencies:

```bash
cd Packages/com.realitycollective.uxmanager/MCP/unity-localization-server
npm install
npm run build
```

### 2. Configure VS Code

Add the server to your VS Code MCP settings. Open your VS Code settings and add:

**For Windows (PowerShell):**
Edit `%APPDATA%\Code\User\globalStorage\saoudrizwan.claude-dev\settings\cline_mcp_settings.json`:

```json
{
  "mcpServers": {
    "unity-localization": {
      "command": "node",
      "args": [
        "E:\\ED\\01-Yperea\\Yperea-Design\\YpereaDesign.Unity\\Packages\\com.realitycollective.uxmanager\\MCP\\unity-localization-server\\dist\\index.js"
      ],
      "env": {
        "UNITY_PROJECT_ROOT": "E:\\ED\\01-Yperea\\Yperea-Design\\YpereaDesign.Unity"
      }
    }
  }
}
```

**For macOS/Linux:**
Edit `~/.vscode/extensions/saoudrizwan.claude-dev-*/settings/cline_mcp_settings.json`:

```json
{
  "mcpServers": {
    "unity-localization": {
      "command": "node",
      "args": [
        "/path/to/YpereaDesign.Unity/Packages/com.realitycollective.uxmanager/MCP/unity-localization-server/dist/index.js"
      ],
      "env": {
        "UNITY_PROJECT_ROOT": "/path/to/YpereaDesign.Unity"
      }
    }
  }
}
```

### 3. Restart VS Code

After adding the configuration, restart VS Code to load the MCP server.

## Available Tools

### `unity_localization_add_key`

Add a new localization key with translations to all catalogs.

**Parameters:**

- `key` (string): Localization key (e.g., "enum_SocialPlatform_Instagram")
- `displayName` (string): PascalCase display name (e.g., "EnumSocialPlatformInstagram")
- `translations` (object): Translations for each locale

**Example:**

```typescript
{
  "key": "enum_SocialPlatform_Instagram",
  "displayName": "EnumSocialPlatformInstagram",
  "translations": {
    "en-US": "Instagram",
    "es-ES": "Instagram",
    "fr-FR": "Instagram",
    "test": "[Ĩṇşţàĝṛàṃ]"
  }
}
```

### `unity_localization_batch_add_keys`

Add multiple localization keys at once.

**Parameters:**

- `keys` (array): Array of key objects (same structure as `unity_localization_add_key`)

**Example:**

```typescript
{
  "keys": [
    {
      "key": "enum_SocialPlatform_Instagram",
      "displayName": "EnumSocialPlatformInstagram",
      "translations": {
        "en-US": "Instagram",
        "es-ES": "Instagram",
        "fr-FR": "Instagram",
        "test": "[Ĩṇşţàĝṛàṃ]"
      }
    },
    {
      "key": "enum_SocialPlatform_Twitter",
      "displayName": "EnumSocialPlatformTwitter",
      "translations": {
        "en-US": "Twitter",
        "es-ES": "Twitter",
        "fr-FR": "Twitter",
        "test": "[Ţẁĩţţḗṛ]"
      }
    }
  ]
}
```

### `unity_localization_update_key`

Update translations for an existing key.

**Parameters:**

- `key` (string): Localization key to update
- `translations` (object): New translations

### `unity_localization_validate`

Validate localization consistency across all files.

**Checks:**

- All catalogs have the same keys
- Profile keys match catalog keys
- No orphaned keys
- Naming conventions are followed

### `unity_localization_get_status`

Get comprehensive status of the localization system.

**Parameters:**

- `verbose` (boolean, optional): Include detailed key listing

### `unity_localization_get_key_info`

Get detailed information about a specific localization key.

**Parameters:**

- `key` (string): Localization key to query

### `unity_localization_remove_key`

Remove a localization key from all files.

**Parameters:**

- `key` (string): Localization key to remove

## Usage Examples

### Adding Social Platform Keys (The Session Example)

Instead of manually editing 7 files, use one tool call:

```typescript
// Using unity_localization_batch_add_keys
{
  "keys": [
    {
      "key": "enum_SocialPlatform_Instagram",
      "displayName": "EnumSocialPlatformInstagram",
      "translations": {
        "en-US": "Instagram",
        "es-ES": "Instagram",
        "fr-FR": "Instagram",
        "test": "[Ĩṇşţàĝṛàṃ]"
      }
    },
    // ... other platforms
  ]
}
```

This single operation:

1. ✅ Adds keys to LocalizationServiceProfile.asset
2. ✅ Updates en-US.json, es-ES.json, fr-FR.json, test.json
3. ✅ Regenerates LocalizationKeys.g.cs
4. ✅ Validates consistency

### Validating Before Release

```typescript
// Check for any localization issues
unity_localization_validate({})
```

### Getting Status

```typescript
// Quick status
unity_localization_get_status({ "verbose": false })

// Detailed status with all keys
unity_localization_get_status({ "verbose": true })
```

### `unity_localization_check_key_health`

Comprehensive health check of a localization key. Validates configuration, C# usage patterns, and translations.

**What It Checks:**

✅ **Configuration Health**

- Key exists in profile
- DisplayName is valid and PascalCase
- Key exists in all configured locales
- No missing catalogs

✅ **Translation Health**

- All translations present (no missing locales)
- All translations non-empty
- No typos or malformed entries

✅ **Code Usage Health**

- Scans entire `Assets/` directory for C# files
- Detects proper usage: `LocalizationKeys.DisplayName`
- Detects improper usage: `GetString("raw_key")`
- Identifies unused keys
- Flags raw string usage that should use constants

**Parameters:**

- `key` (string): The localization key to health check

**Example:**

```typescript
{
  "key": "enum_SocialPlatform_Instagram"
}
```

**Report Output:**

The health report shows:

```
🏥 **Key Health Report: enum_SocialPlatform_Instagram**

✅ **Status: HEALTHY**

**📋 Configuration:**
- ✅ Key exists in profile
- ✅ DisplayName: EnumSocialPlatformInstagram
- ✅ PascalCase format: Valid

**🗂️ Catalogs:**
- ✅ Present in all locales

**📝 Translations:**
- ✅ All translations present and non-empty

**💻 Code Usage:**
- ✅ Proper usage found: 3 occurrence(s)
  Files: SocialLinkContainer.cs
```

**When to Use:**

1. **After adding a new key** - Verify it's properly configured and used
2. **Before release** - Check all keys are used and complete
3. **Refactoring** - Identify raw string usage that should be constants
4. **Cleanup** - Find unused keys that can be removed

**Possible Status Values:**

- ✅ **HEALTHY** - Fully configured and properly used
- ⚠️ **HEALTHY BUT UNUSED** - Valid configuration but no C# usage found
- ❌ **ISSUES DETECTED** - Configuration or usage problems requiring action

**Common Issues & Fixes:**

| Issue | Recommendation |
|-------|---|
| Key not in profile | Add to LocalizationServiceProfile.asset |
| Missing displayName | Add PascalCase display name to profile |
| Missing from locales | Add translations to all catalog JSON files |
| Empty translations | Fill in translation values |
| Raw string usage | Replace `GetString("key")` with `LocalizationKeys.ConstantName` |
| No usage found | Either use the key in code or remove if obsolete |

### `health_check`

Server health check - validates MCP server startup, configuration access, and file accessibility.

**When to use:**

- **Before performing any localization tasks** - Verify everything is configured
- **Troubleshooting** - Diagnose configuration issues
- **Setup verification** - Confirm installation is complete

**No parameters required:**

```typescript
{}
```

**Health Report Output:**

```
⚕️ **MCP Server Health Report**

🟢 **Status: OPERATIONAL**

**Checks:**

✅ **Configuration Detection**
   Found configuration at: LocalizationServiceProfile.asset

✅ **Profile Access**
   Profile accessible with 42 keys

✅ **Catalog Access**
   All 4 catalogs accessible: ✓ en-US, ✓ es-ES, ✓ fr-FR, ✓ test

✅ **Generated Keys File**
   Keys file accessible: LocalizationKeys.g.cs

✅ **Service Discovery**
   ILocalizationService discovered via GUID-based lookup

✅ **Configuration Consistency**
   All profile keys present in all catalogs

**Summary:**
- ✅ Passed: 6/6
- ✅ **Server5/5 ready for localization tasks**. You can safely call any localization tool.
```

**Possible Status Indicators:**

- 🟢 **OPERATIONAL** - All checks passed, ready for operations
- 🔴 **ISSUES DETECTED** - One or more checks failed, needs troubleshooting

**Common Health Check Scenarios:**

| Scenario | Status | Action |
|----------|--------|--------|
| Fresh installation | 🟢 OPERATIONAL | Ready to use! |
| Profile file moved | 🔴 ISSUES | Reconfigure LOCALIZATION_PROFILE_PATH env var |
| Catalog file deleted | 🔴 ISSUES | Restore deleted catalog or create new one |
| Keys file inaccessible | 🟡 WARNING | Will be auto-created on first operation |

**Troubleshooting Failed Checks:**

**❌ Configuration Detection Failed**

- Verify ServiceProvidersProfile.asset exists in Assets/ServiceProvidersProfile/
- Verify LocalizationServiceProfile.asset exists in Assets/ServiceProvidersProfile/ (or your custom path)
- Check UNITY_PROJECT_ROOT environment variable points to project root
- Set LOCALIZATION_PROFILE_PATH if profile is in a non-standard location
**❌ Profile Access Failed**

- Verify LocalizationServiceProfile.asset exists and is readable
- Check file permissions (should be writable for add/update operations)
- Verify asset isn't corrupted (try reimporting)

**❌ Catalog Access Failed**

- Verify catalogs folder exists at configured path (usually Assets/UX/Localization/Catalogs/)
- Check all required locale files exist (en-US.json, es-ES.json, etc.)
- Verify files are readable

**❌ Service Discovery Failed**

- Verify ILocalizationService GUID in ServiceProvidersProfile is valid
- Check sLocalizationServiceProfile.asset is valid and properly formatted
- Check file isn't corrupted (try reimporting in Unity)
- Verify LOCALIZATION_PROFILE_PATH points to the correct file
## File Structure

```
unity-localization-server/
├── src/
│   ├── index.ts                  # MCP server entry point
│   └── localization-manager.ts   # Core localization logic
├── dist/                          # Compiled JavaScript
├── package.json
├── tsconfig.json
└── README.md
```

## Development

### Building

```bash
npm run build
```

### Watch Mode

```bash
npm run watch
```

## Naming Conventions

### Keys

- Lowercase with underscores
- Format: `category_subcategory_name`
- Examples:
  - `enum_SocialPlatform_Instagram`
  - `mainscreen_time`
  - `shopscreen_buy_format`

### Display Names

- PascalCase (no underscores)
- Generated constant: `LocalizationKeys.{DisplayName}`
- Examples:
  - `EnumSocialPlatformInstagram`
  - `MainscreenTime`
  - `ShopscreenBuyFormat`

## Error Handling

The server validates:

- ✅ Key naming conventions (lowercase_with_underscores)
- ✅ Display name format (PascalCase)
- ✅ All locales have translations
- ✅ No duplicate keys
- ✅ Consistent key sets across catalogs

## Troubleshooting

### Server Not Appearing in VS Code

1. Check the path in `cline_mcp_settings.json` is correct (use absolute paths)
2. Verify `UNITY_PROJECT_ROOT` environment variable points to Unity project root
3. Run `npm run build` to ensure compiled files exist
4. Restart VS Code completely

### Permission Errors

Ensure VS Code has write permissions to:

- `Assets/ServiceProvidersProfile/LocalizationServiceProfile.asset`
- `Assets/UX/Localization/Catalogs/*.json`
- `Assets/ServiceProvidersProfile/LocalizationKeys.g.cs`

## License

MIT License - See LICENSE in the Reality Collective UX Manager package.
