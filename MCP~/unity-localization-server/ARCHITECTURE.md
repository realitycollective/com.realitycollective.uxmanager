# Unity Localization MCP Server - Architecture Documentation

**Version:** 1.0.0  
**Last Updated:** January 30, 2026  
**Status:** Production Ready

---

## Executive Summary

The **Unity Localization MCP Server** is a Model Context Protocol (MCP) server that provides atomic, configuration-aware localization operations for Unity projects using Reality Collective's service framework. It enables AI assistants and developers to safely manage localization keys, translations, and catalogs with built-in validation and health checking.

### Key Characteristics

- **Configuration-Aware:** Auto-discovers configuration from ServiceProvidersProfile using GUID-based service lookup
- **Atomic Operations:** All changes span profile + catalogs + code generation in single operations
- **Validation-First:** Pre-validation and post-operation consistency checking
- **MCP-Native:** Implements Model Context Protocol for seamless VS Code integration
- **Zero Hardcoding:** All paths and settings derived from actual Unity configuration
- **Production-Ready:** Comprehensive error handling, health checking, and reporting

### Architecture Style

```
┌─────────────────────────────────────────────────────────┐
│  VS Code / Cline (MCP Client)                           │
│  Invokes tools via MCP Protocol                         │
└──────────────────┬──────────────────────────────────────┘
                   │
                   │ MCP Protocol (JSON-RPC)
                   │
┌──────────────────▼──────────────────────────────────────┐
│  Unity Localization MCP Server (Node.js / TypeScript)   │
│  ┌────────────────────────────────────────────────────┐ │
│  │ Tool Registry & Handler Dispatch                  │ │
│  └─────────────────┬──────────────────────────────────┘ │
│                    │                                     │
│  ┌─────────────────▼──────────────────────────────────┐ │
│  │ LocalizationManager                                │ │
│  │ ├─ Configuration Detection                         │ │
│  │ ├─ Profile Operations                             │ │
│  │ ├─ Catalog Management                             │ │
│  │ ├─ Code Generation                                │ │
│  │ ├─ Validation & Health Checking                   │ │
│  │ └─ C# Code Analysis                               │ │
│  └─────────────────┬──────────────────────────────────┘ │
│                    │                                     │
└────────────────────┼─────────────────────────────────────┘
                     │
     ┌───────────────┼───────────────┐
     │               │               │
     ▼               ▼               ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ Unity Assets │ │  YAML Parser │ │   JSON I/O   │
│  File System │ │  (js-yaml)   │ │  (native)    │
└──────────────┘ └──────────────┘ └──────────────┘
```

---

## Part 1: How It Was Built

### Technology Stack

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| **Framework** | Model Context Protocol | 1.0+ | Server communication pattern |
| **Runtime** | Node.js | 20+ | JavaScript execution |
| **Language** | TypeScript | 5.3+ | Type-safe implementation |
| **YAML Parsing** | js-yaml | 4.1+ | Parse Unity `.asset` files |
| **File I/O** | Node.js fs/promises | native | Async file operations |
| **Build Tool** | TypeScript Compiler (tsc) | 5.3+ | Compile to JavaScript |
| **Package Manager** | npm | 9+ | Dependency management |

### Design Patterns Used

#### 1. **MCP Protocol Implementation**

- **Server Type:** Stdio-based (communicates via stdin/stdout)
- **Transport:** JSON-RPC 2.0 over stdio
- **Client Integration:** VS Code + Cline extension
- **Pattern:** Request-response with tool registry

```typescript
// Pattern: Tool definition + handler dispatch
const tools = [
  { name: 'tool_name', description: '...', inputSchema: {...} },
  // ... more tools
];

server.setRequestHandler(CallToolRequestSchema, async (request) => {
  switch (request.params.name) {
    case 'tool_name':
      return handler(request.params.arguments);
  }
});
```

#### 2. **Configuration Discovery (GUID-Based Lookup)**

- **Goal:** Find configuration without hardcoded paths
- **Method:** Traverse YAML assets using service GUID
- **Steps:**
  1. Parse ServiceProvidersProfile.asset
  2. Find ILocalizationService entry
  3. Extract service implementation's GUID
  4. Search for matching LocalizationServiceProfile.asset with same GUID
  5. Load actual configuration from profile

```
ServiceProvidersProfile.asset
    ↓ Find ILocalizationService
Localization Service Entry (with GUID)
    ↓ Extract GUID: "GUID-12345..."
LocalizationServiceProfile.asset
    ↓ Search by GUID
Profile Data (paths, supported locales, etc.)
```

#### 3. **Atomic Operations Pattern**

- **Principle:** All related updates happen together or not at all
- **Implementation:** Try all operations, rollback if any fail
- **Operations:** Add/update/remove span three file types simultaneously

```
Input: New localization key
    ↓
1. Validate input (naming, display name format)
2. Lock file writes (single operation)
3. Update Profile (add key + displayName entry)
4. Update All Catalogs (add key to each locale)
5. Regenerate Keys File (create C# constants)
6. Unlock / Return result

If step N fails → Return error, previous steps remain unchanged
```

#### 4. **Lazy Service Pattern**

- **Goal:** Defer expensive operations until needed
- **Implementation:** Detect configuration once, cache result
- **Benefit:** Repeated operations don't re-scan file system

```typescript
private config: LocalizationServiceConfig | null = null;

async ensureConfiguration() {
  if (this.config === null) {
    this.config = await this.detectConfiguration();
  }
  return this.config;
}
```

#### 5. **Validation Pipeline**

- **When:** Before operations and after completion
- **Levels:** Input → Operation → Output
- **Coverage:** Naming conventions, consistency, completeness

```
Input Validation
    ↓
Configuration Check
    ↓
File State Analysis
    ↓
Operation Execution
    ↓
Post-Operation Verification
    ↓
Consistency Check
    ↓
Result
```

### Project Structure

```
unity-localization-server/
├── src/
│   ├── index.ts              # MCP server entry point, tool definitions, handler dispatch
│   └── localization-manager.ts  # Core business logic, 1,270 lines
│
├── dist/                     # Compiled JavaScript (generated)
│   ├── index.js
│   ├── index.d.ts
│   ├── localization-manager.js
│   └── localization-manager.d.ts
│
├── package.json              # npm dependencies and build scripts
├── tsconfig.json             # TypeScript compiler configuration
│
├── README.md                 # User documentation
├── ARCHITECTURE.md           # This file
├── KEY_HEALTH_CHECK.md       # Feature documentation
├── HEALTH_CHECK_FEATURE.md   # Feature documentation
└── .gitignore                # Standard Node.js ignore rules
```

### Build Process

```
TypeScript Source (index.ts, localization-manager.ts)
    ↓
TypeScript Compiler (tsc)
    ↓ Compiled to ES2020 JavaScript
dist/index.js, dist/localization-manager.js
    ↓
npm run build (triggered on demand)
    ↓
dist/*.js ready for Node.js execution
    ↓
MCP Server launches with: node dist/index.js
    ↓
Listens on stdin/stdout for MCP protocol messages
```

---

## Part 2: Discrete Functions Provided

### Tool Overview

The server provides **9 atomic tools** organized by operation type:

#### **A. Core Localization Operations**

##### 1. `add_localization_key`

- **Purpose:** Add new localization key to profile and all catalogs
- **Input:** key, displayName, translations (by locale)
- **Output:** Success/error with status
- **Guarantees:** Atomic (all-or-nothing)

##### 2. `batch_add_localization_keys`

- **Purpose:** Add multiple keys in single operation
- **Input:** Array of {key, displayName, translations}
- **Output:** Success/error with count
- **Benefit:** More efficient than repeated add operations

##### 3. `update_localization_key`

- **Purpose:** Update translations for existing key
- **Input:** key, translations (by locale)
- **Output:** Success/error
- **Note:** Doesn't modify key or displayName (use remove + add instead)

##### 4. `remove_localization_key`

- **Purpose:** Remove key from all files
- **Input:** key
- **Output:** Success/error
- **Impact:** Removes from profile and all catalogs

#### **B. Validation & Analysis**

##### 5. `validate_localization`

- **Purpose:** Full system validation
- **Input:** None (optional verbose flag)
- **Output:** Validation report with all issues
- **Checks:**
  - All catalogs have identical key sets
  - Profile keys match catalog keys (no orphans)
  - Naming convention compliance
  - No duplicate keys
  - No empty translations

##### 6. `get_localization_status`

- **Purpose:** Get overview of localization state
- **Input:** verbose (boolean, default false)
- **Output:** Status report with key counts, locales, etc.
- **Verbose Mode:** Lists all keys and their status

##### 7. `get_key_info`

- **Purpose:** Get detailed info about specific key
- **Input:** key name
- **Output:** Key details (displayName, translations by locale, C# constant)

#### **C. Health & Quality**

##### 8. `check_key_health`

- **Purpose:** Comprehensive health check of single key
- **Input:** key name
- **Output:** Three-tier health report (config, translation, code usage)
- **Scans:** C# codebase for proper/improper usage patterns

##### 9. `health_check`

- **Purpose:** Server operational health validation
- **Input:** None
- **Output:** 6-point health report
- **Checks:** Configuration, file access, service discovery, consistency

### Tool Capability Matrix

| Tool | Creates | Updates | Deletes | Validates | Scans Code | Generates |
|------|---------|---------|---------|-----------|-----------|-----------|
| add_localization_key | ✅ | - | - | ✅ | - | ✅ |
| batch_add_localization_keys | ✅ | - | - | ✅ | - | ✅ |
| update_localization_key | - | ✅ | - | ✅ | - | ✅ |
| remove_localization_key | - | - | ✅ | ✅ | - | ✅ |
| validate_localization | - | - | - | ✅ | - | - |
| get_localization_status | - | - | - | ✅ | - | - |
| get_key_info | - | - | - | ✅ | ✅ | - |
| check_key_health | - | - | - | ✅ | ✅ | - |
| health_check | - | - | - | ✅ | - | - |

---

## Part 3: Detailed Operation Maps

### Operation: `unity_localization_add_key`

```
┌─────────────────────────────────────────────────────────────┐
│ Input: key, displayName, translations                      │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ 1. VALIDATE INPUT                                           │
│    ├─ key: lowercase_with_underscores                       │
│    ├─ displayName: PascalCase                               │
│    └─ translations: non-empty for all configured locales    │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ 2. AUTO-DETECT CONFIGURATION                                │
│    ├─ Find ServiceProvidersProfile.asset                    │
│    ├─ Locate ILocalizationService entry                     │
│    ├─ Extract GUID of service                               │
│    ├─ Find LocalizationServiceProfile with matching GUID    │
│    └─ Load: paths, supported locales, settings              │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ 3. LOAD CURRENT STATE                                       │
│    ├─ Parse profile YAML                                    │
│    ├─ Load all catalog JSON files                           │
│    └─ Cache in memory                                       │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ 4. CHECK FOR CONFLICTS                                      │
│    ├─ Key already in profile? → Error: duplicate            │
│    ├─ Key already in any catalog? → Error: duplicate        │
│    └─ Continue only if no conflicts                         │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ 5. UPDATE PROFILE                                           │
│    ├─ Add entry: { key: "...", displayName: "..." }         │
│    └─ Maintain order (usually alphabetical)                 │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ 6. UPDATE ALL CATALOGS                                      │
│    ├─ For each configured locale:                           │
│    │  ├─ Load catalog JSON                                  │
│    │  ├─ Add key: translation mapping                       │
│    │  ├─ Maintain order (usually alphabetical)              │
│    │  └─ Write back JSON                                    │
│    └─ All catalogs must be updated                          │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ 7. REGENERATE C# CONSTANTS                                  │
│    ├─ Load profile (now with new key)                       │
│    ├─ Sort keys alphabetically by displayName               │
│    ├─ Generate LocalizationKeys.g.cs with:                  │
│    │  └─ public const string {DisplayName} = "{key}"        │
│    └─ Write to configured path                              │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ 8. POST-OPERATION VALIDATION                                │
│    ├─ Verify key in profile                                 │
│    ├─ Verify key in all catalogs                            │
│    ├─ Verify key in generated file                          │
│    └─ Verify consistency                                    │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ Output: Success report with:                                │
│   - Key added to profile                                    │
│   - Translations added to N catalogs                        │
│   - C# constant generated                                   │
│   - Ready for use in code                                   │
└─────────────────────────────────────────────────────────────┘
```

### Operation: `check_key_health`

```
┌─────────────────────────────────────────────────────────────┐
│ Input: key                                                  │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ PHASE 1: CONFIGURATION HEALTH                               │
│                                                             │
│ ├─ Load profile                                             │
│ │  └─ Find entry with given key                            │
│ │     ├─ Exists? → ✅ profileEntry = true                  │
│ │     └─ Missing? → ❌ profileEntry = false                │
│ │                                                          │
│ ├─ If exists, validate displayName                         │
│ │  ├─ Non-empty? → ✅ displayNameValid = true              │
│ │  └─ Empty? → ❌ displayNameValid = false                 │
│ │                                                          │
│ └─ Check PascalCase format                                 │
│    ├─ Matches regex /^[A-Z][a-zA-Z0-9]*$/ ?               │
│    ├─ Yes → ✅ displayNamePascalCase = true               │
│    └─ No → ❌ displayNamePascalCase = false               │
│                                                             │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ PHASE 2: CATALOG HEALTH                                     │
│                                                             │
│ ├─ Load all catalogs                                        │
│ └─ For each configured locale:                             │
│    ├─ Key exists in this locale's catalog?                 │
│    │  ├─ Yes → ✅ allCatalogsHaveKey += 1                  │
│    │  └─ No → ❌ missingFromLocales += locale              │
│    │                                                       │
│    └─ Catalog key non-empty?                               │
│       ├─ Yes → ✅ translationPresent                       │
│       └─ No → ❌ emptyInLocales += locale                  │
│                                                             │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ PHASE 3: CODE USAGE HEALTH                                  │
│                                                             │
│ ├─ Recursive Directory Scan                                │
│ │  ├─ Start: Assets/ directory                             │
│ │  ├─ Skip: Library, Temp, obj, bin, node_modules, .git    │
│ │  └─ Find: All *.cs files                                 │
│ │                                                          │
│ ├─ For each .cs file:                                      │
│ │  │                                                       │
│ │  ├─ PROPER USAGE: LocalizationKeys.{DisplayName}        │
│ │  │  └─ Regex: LocalizationKeys\.{displayName}\b         │
│ │  │     ├─ Found → properUsageCount += matches           │
│ │  │     └─ usedWithLocalKeys += file path                │
│ │  │                                                      │
│ │  └─ IMPROPER USAGE: Raw string GetString("key")         │
│ │     └─ Regex: GetString\s*\(\s*["']{key}["']\s*\)       │
│ │        ├─ Found → improperUsageCount += matches         │
│ │        └─ usedWithRawStrings += file path               │
│ │                                                         │
│ └─ If properUsageCount == 0 → potentiallyUnused = true    │
│                                                             │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ PHASE 4: REPORT GENERATION                                  │
│                                                             │
│ Determine overall status:                                  │
│ ├─ All checks pass + proper usage → ✅ HEALTHY            │
│ ├─ All checks pass + no usage → ⚠️ HEALTHY BUT UNUSED     │
│ └─ Any check fails → ❌ ISSUES DETECTED                    │
│                                                             │
│ Generate markdown report with:                             │
│ ├─ Status indicator                                        │
│ ├─ Configuration details (pass/fail)                       │
│ ├─ Catalog coverage (pass/fail/warnings)                   │
│ ├─ Translation completeness (pass/fail)                    │
│ ├─ Code usage analysis (proper/improper/unused)           │
│ └─ Actionable recommendations                              │
│                                                             │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ Output: Comprehensive KeyHealthReport with three tiers:    │
│   1. configurationHealth (profile, displayName, catalogs)  │
│   2. translationHealth (completeness, non-empty)           │
│   3. codeUsageHealth (proper/improper/unused patterns)     │
└─────────────────────────────────────────────────────────────┘
```

### Operation: `unity_localization_health_check`

```
┌─────────────────────────────────────────────────────────────┐
│ Input: (none)                                               │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ CHECK 1: CONFIGURATION DETECTION                            │
│                                                             │
│ ├─ Find ServiceProvidersProfile.asset                       │
│ │  ├─ File exists? No → ❌ Fail                            │
│ │  └─ Parse YAML → OK → Continue                           │
│ │                                                          │
│ ├─ Find ILocalizationService entry                         │
│ │  ├─ Found in profile? No → ❌ Fail                       │
│ │  └─ Extract GUID → Continue                              │
│ │                                                          │
│ └─ Find LocalizationServiceProfile with GUID               │
│    ├─ Found? No → ❌ Fail                                  │
│    └─ Success → ✅ Configuration detected                  │
│                                                             │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ CHECK 2: PROFILE ACCESS                                     │
│                                                             │
│ ├─ Read LocalizationServiceProfile.asset                   │
│ │  ├─ File readable? No → ❌ Fail                          │
│ │  ├─ Parse valid YAML? No → ❌ Fail                       │
│ │  └─ Success → ✅ Profile accessible with N keys         │
│ │                                                          │
│ └─ Profile contains localizationKeys array                 │
│    └─ Success → ✅ Profile structure valid                 │
│                                                             │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ CHECK 3: CATALOG ACCESS                                     │
│                                                             │
│ ├─ For each configured locale (en-US, es-ES, etc.):       │
│ │  ├─ Catalog file exists? No → Mark missing              │
│ │  ├─ JSON valid? No → ❌ Fail                            │
│ │  └─ Can read? No → ❌ Fail                              │
│ │                                                          │
│ └─ All catalogs accessible?                                │
│    ├─ Yes → ✅ All N catalogs accessible                   │
│    └─ No → ❌ Missing locales: [...]                       │
│                                                             │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ CHECK 4: GENERATED KEYS FILE                                │
│                                                             │
│ ├─ File exists?                                             │
│ │  ├─ No → ⚠️ Warning (will be created on first operation) │
│ │  └─ Yes → Continue                                        │
│ │                                                          │
│ └─ File readable?                                           │
│    ├─ Yes → ✅ Keys file accessible                        │
│    └─ No → ❌ Fail                                         │
│                                                             │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ CHECK 5: SERVICE DISCOVERY                                  │
│                                                             │
│ ├─ Verify GUID lookup worked correctly                     │
│ │  └─ ServiceProvidersProfile GUID == LocalizationProfile  │
│ │                                                          │
│ └─ Service discoverable via ServiceManager?                │
│    ├─ Yes → ✅ Service discovery working                   │
│    └─ No → ❌ Fail                                         │
│                                                             │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ CHECK 6: CONFIGURATION CONSISTENCY                          │
│                                                             │
│ ├─ Load profile and all catalogs                            │
│ │                                                          │
│ ├─ Collect all keys from:                                  │
│ │  ├─ Profile localizationKeys                             │
│ │  └─ All catalog JSON files                               │
│ │                                                          │
│ ├─ For each profile key:                                   │
│ │  └─ Present in ALL catalogs?                             │
│ │     ├─ No → Missing from: [locales]                      │
│ │     └─ Collect mismatches                                │
│ │                                                          │
│ └─ Result:                                                  │
│    ├─ No mismatches → ✅ Consistency OK                    │
│    └─ Found issues → ❌ Fail with details                  │
│                                                             │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ AGGREGATE RESULTS                                           │
│                                                             │
│ Count: Passed / Failed / Warnings                           │
│                                                             │
│ Overall Status:                                             │
│ ├─ All checks pass → 🟢 OPERATIONAL                        │
│ └─ Any check fails → 🔴 ISSUES DETECTED                    │
│                                                             │
│ Generate report with:                                      │
│ ├─ Status indicator                                        │
│ ├─ All 6 check results                                     │
│ ├─ Summary statistics                                      │
│ └─ Recommendations for failed checks                       │
│                                                             │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│ Output: Health Report                                       │
│   🟢 OPERATIONAL - Server ready for operations              │
│   or                                                        │
│   🔴 ISSUES DETECTED - Review failures before proceeding    │
└─────────────────────────────────────────────────────────────┘
```

### File I/O Integration Map

```
LocalizationManager Methods ← → File System Operations

Configuration Discovery:
  detectConfiguration()
    ├─ fs.readFile() → ServiceProvidersProfile.asset (YAML)
    ├─ yaml.parse() → Extract service config
    ├─ fs.readFile() → LocalizationServiceProfile.asset (YAML)
    └─ Return: config { profilePath, catalogPath, generatedKeysPath, supportedLocales }

Profile Operations:
  loadProfile()
    ├─ fs.readFile() → LocalizationServiceProfile.asset
    ├─ yaml.parse() → Parse YAML
    └─ Return: profile { localizationKeys: [{key, displayName}, ...] }

  saveProfile()
    ├─ Prepare modified profile object
    ├─ yaml.dump() → Serialize to YAML string
    └─ fs.writeFile() → Write back to asset file

Catalog Operations:
  loadAllCatalogs()
    ├─ fs.readdir() → List all files in catalogPath
    ├─ Filter: only *.json files
    ├─ For each file:
    │  ├─ fs.readFile() → Catalog JSON
    │  └─ JSON.parse() → Parse catalog
    └─ Return: array of { locale, version, keys: {...} }

  saveCatalog()
    ├─ For each catalog to update:
    │  ├─ Update in-memory keys object
    │  ├─ JSON.stringify() → Serialize
    │  └─ fs.writeFile() → Write JSON

Code Generation:
  regenerateKeysFile()
    ├─ Load profile (now with all keys)
    ├─ Sort keys by displayName
    ├─ Generate TypeScript class with const strings:
    │  └─ public const string {DisplayName} = "{key}";
    ├─ Wrap in namespace
    └─ fs.writeFile() → Write LocalizationKeys.g.cs

Code Scanning:
  scanForKeyUsage()
    ├─ findCsFiles() → Recursively traverse Assets/
    │  └─ fs.readdir() with recursion
    ├─ For each *.cs file:
    │  ├─ fs.readFile() → Read source code
    │  └─ Regex search for patterns
    └─ Return: usage counts and file locations
```

### Configuration Discovery Data Flow

```
┌──────────────────────────┐
│ UNITY_PROJECT_ROOT       │
│ Environment Variable     │
└────────────┬─────────────┘
             │
             ▼
┌──────────────────────────────────────────┐
│ Assets/ServiceProvidersProfile/          │
│ ServiceProvidersProfile.asset (YAML)     │
│                                          │
│ Contains:                                │
│  - Services[] array                      │
│    - ServiceTypeName: ILocalizationService
│    - ServiceImplementation GUID          │
│                                          │
└────────────┬─────────────────────────────┘
             │ Parse & Extract
             ▼
┌──────────────────────────────────────────┐
│ LocalizationService Entry                │
│  - ServiceTypeName: ILocalizationService │
│  - ServiceImplementation: GUID-12345...  │
│                                          │
└────────────┬─────────────────────────────┘
             │ Extract GUID
             ▼
┌──────────────────────────────────────────┐
│ GUID: "GUID-12345-abcde-vwxyz"          │
│                                          │
│ Search for asset with this GUID         │
│                                          │
└────────────┬─────────────────────────────┘
             │ File System Search
             ▼
┌──────────────────────────────────────────┐
│ Assets/ServiceProvidersProfile/          │
│ LocalizationServiceProfile.asset (YAML)  │
│                                          │
│ Contains:                                │
│  - guid: "GUID-12345-abcde-vwxyz"       │
│  - class: LocalizationServiceProfile    │
│  - catalogPath: Assets/UX/Localization/… │
│  - profilePath: ...                      │
│  - generatedKeysPath: ...                │
│  - supportedLocales: [en-US, es-ES, ...] │
│                                          │
└────────────┬─────────────────────────────┘
             │ Load Config
             ▼
┌──────────────────────────────────────────┐
│ LocalizationServiceConfig                │
│ {                                        │
│   profilePath: full path                 │
│   catalogPath: full path                 │
│   generatedKeysPath: full path           │
│   supportedLocales: string[]             │
│ }                                        │
│                                          │
│ Ready for all operations!                │
│                                          │
└──────────────────────────────────────────┘
```

---

## Part 4: Executive Architecture Summary

### High-Level Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│ PRESENTATION LAYER                                          │
│ MCP Tool Interface                                          │
│ - 9 tools with defined schemas                             │
│ - JSON-RPC request/response                                 │
│ - Human-readable reports                                   │
└──────────────────┬──────────────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────────────┐
│ BUSINESS LOGIC LAYER                                        │
│ LocalizationManager                                         │
│ - Orchestrates operations                                  │
│ - Enforces validation rules                                │
│ - Manages atomic transactions                              │
│ - Generates reports                                        │
└──────────────────┬──────────────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────────────┐
│ DATA ACCESS LAYER                                           │
│ File I/O Operations                                        │
│ - Profile YAML parsing/writing                             │
│ - Catalog JSON read/write                                  │
│ - C# file generation                                       │
│ - Recursive directory scanning                             │
└──────────────────┬──────────────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────────────┐
│ INTEGRATION LAYER                                           │
│ Unity Project Files                                        │
│ - ServiceProvidersProfile.asset (YAML)                     │
│ - LocalizationServiceProfile.asset (YAML)                  │
│ - Catalog files (JSON)                                     │
│ - LocalizationKeys.g.cs (C#)                               │
│ - Source files (*.cs)                                      │
└─────────────────────────────────────────────────────────────┘
```

### Key Integration Points

#### **1. Service Framework Integration**

The server integrates with Reality Collective's service framework:

```
Unity Project
├── ServiceProvidersProfile
│   └─ Registers ILocalizationService
│      └─ Points to LocalizationServiceProfile via GUID
│
└── LocalizationServiceProfile
    ├─ Configures catalog paths
    ├─ Defines supported locales
    ├─ Specifies generated keys location
    └─ Contains all localization metadata
```

MCP Server reads this configuration and derives all paths/settings from it.

#### **2. C# Code Integration**

When keys are added/updated:

```
LocalizationKeys.g.cs (Generated)
│
├─ public const string EnumSocialPlatformInstagram = "enum_SocialPlatform_Instagram";
├─ public const string MainscreenTime = "mainscreen_time";
└─ ... (one per key)

Used in C# Code:
│
├─ LocalizationService.GetString(LocalizationKeys.EnumSocialPlatformInstagram)
├─ SocialLinkContainer.GetLocalizedPlatformName() uses constants
└─ All screens handle locale changes by re-reading constants
```

#### **3. Localization Catalog Integration**

```
en-US.json               es-ES.json              fr-FR.json
├─ "key1": "English"    ├─ "key1": "Español"    ├─ "key1": "Français"
├─ "key2": "English"    ├─ "key2": "Español"    ├─ "key2": "Français"
└─ ...                  └─ ...                  └─ ...

All synchronized by MCP server operations
└─ Atomic updates ensure consistency
```

### Data Flow for Complete Operation

```
User Request (VS Code)
    │
    ▼
MCP Protocol Handler (index.ts)
    │
    ├─ Parse request
    └─ Route to tool handler
        │
        ▼
LocalizationManager (localization-manager.ts)
    │
    ├─ detectConfiguration()
    │  └─ GUID-based lookup → Config object
    │
    ├─ Validation()
    │  ├─ Input validation
    │  ├─ State validation
    │  └─ Consistency checks
    │
    ├─ File Operations()
    │  ├─ Read/Parse: Profile YAML
    │  ├─ Read/Parse: Catalog JSON
    │  ├─ Modify data structures
    │  ├─ Write: Updated YAML
    │  ├─ Write: Updated JSON
    │  └─ Generate: C# code
    │
    ├─ Post-Validation()
    │  └─ Verify all changes applied
    │
    └─ Report Generation()
        └─ Format human-readable output
            │
            ▼
Return to MCP Handler
    │
    ├─ Serialize result
    └─ Return via MCP Protocol
        │
        ▼
VS Code / Cline Extension
    │
    └─ Display result to user
```

### Consistency Guarantees

The system maintains consistency through:

#### **1. Atomic Operations**

- All related changes happen together
- If any step fails, none are committed
- Example: Adding key touches 4+ files atomically

#### **2. Validation Checkpoints**

- **Pre-operation:** Input validation
- **During operation:** State validation
- **Post-operation:** Consistency verification
- **Periodic:** Full system validation tool

#### **3. Configuration Consistency**

- Profile keys must exist in all catalogs
- No orphaned keys in catalogs
- All generated constants match profile
- Naming conventions strictly enforced

#### **4. Error Recovery**

- Clear error messages indicate what went wrong
- Partial failures reported transparently
- User can verify state with `health_check` or `validate_localization`

### Scalability Characteristics

| Aspect | Limit | Notes |
|--------|-------|-------|
| **Keys** | 1,000+ | No practical limit; performance degrades gradually |
| **Locales** | 20+ | Limited by memory, not by design |
| **Catalog Size** | MB-range | JSON parsing handles large files efficiently |
| **Project Size** | GB-range | Code scanning skips non-source directories |
| **Concurrent Users** | Single | MCP protocol expects single client per instance |
| **Performance** | <3s/op | Most operations complete in <1s for typical projects |

### Security Considerations

The MCP server is designed for local development:

- ✅ **Runs locally** - No network communication
- ✅ **Read/write to project only** - Can't access system files outside project
- ✅ **No authentication needed** - Assumes trusted developer environment
- ✅ **No secrets stored** - All data is source code and config files
- ⚠️ **Full file system access** - Runs with permissions of Node.js process

**Best Practice:** Run in development environment only, never in production build pipelines.

### Extensibility Points

The server can be extended at these points:

#### **1. Add New Tools**

- Define tool schema in tools array
- Add handler case in CallToolRequestSchema
- Implement method in LocalizationManager

#### **2. Custom Validation**

- Extend `validateKeyName()` for stricter conventions
- Add locale-specific validation in `validateTranslations()`
- Implement domain-specific checks

#### **3. Alternative Backends**

- Replace file I/O with cloud storage
- Use different YAML/JSON parsers
- Implement database-backed storage

#### **4. IDE Integration**

- Could integrate with Unity editor plugins
- Could add JetBrains IDE support (ReSharper, Rider)
- Could integrate with other editors (Vim, Sublime)

---

## Summary: Key Architectural Principles

| Principle | Implementation |
|-----------|---|
| **Configuration-Aware** | GUID-based service discovery, no hardcoding |
| **Atomic Operations** | All-or-nothing updates across multiple files |
| **Fail-Safe** | Validation before and after each operation |
| **Self-Describing** | Health checks validate entire system state |
| **MCP-Native** | Standard protocol for IDE integration |
| **Zero External Dependencies** | Works offline, no network required |
| **Type-Safe** | TypeScript throughout, generates typed C# constants |
| **User-Friendly** | Clear error messages, actionable recommendations |
| **Maintainable** | Well-structured code, single responsibility methods |
| **Extensible** | Easy to add new tools and validation rules |

---

**Document Version:** 1.0.0  
**Created:** January 30, 2026  
**Status:** Production Ready  
**Maintenance:** This document should be updated when new tools are added or architecture changes are made.
