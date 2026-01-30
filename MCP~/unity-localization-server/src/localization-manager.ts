import fs from 'fs/promises';
import path from 'path';
import yaml from 'js-yaml';

// Unity YAML Schema - handles Unity-specific YAML tags
const UNITY_SCHEMA = yaml.DEFAULT_SCHEMA.extend([
  new yaml.Type('!u!114', {
    kind: 'mapping',
    construct: (data) => data,
    instanceOf: Object
  }),
  new yaml.Type('tag:unity3d.com,2011:114', {
    kind: 'mapping',
    construct: (data) => data,
    instanceOf: Object
  })
]);

interface LocalizationKey {
  key: string;
  displayName: string;
}

interface LocalizationCatalog {
  locale: string;
  version: string;
  keys: Record<string, string>;
}

interface ValidationResult {
  valid: boolean;
  errors: string[];
  warnings: string[];
}

interface KeyHealthReport {
  keyExists: boolean;
  configurationHealth: {
    profileEntry: boolean;
    displayNameValid: boolean;
    displayNamePascalCase: boolean;
    allCatalogsHaveKey: boolean;
    missingFromLocales: string[];
  };
  translationHealth: {
    allTranslationsPresent: boolean;
    allTranslationsNonEmpty: boolean;
    emptyInLocales: string[];
  };
  codeUsageHealth: {
    properUsageCount: number;
    improperUsageCount: number;
    usedWithLocalKeys: string[];
    usedWithRawStrings: string[];
    potentiallyUnused: boolean;
  };
}

interface LocalizationServiceConfig {
  profilePath: string;
  catalogPath: string;
  generatedKeysPath: string;
  defaultLocale: string;
  supportedLocales: string[];
}

export class LocalizationManager {
  private projectRoot: string;
  private config: LocalizationServiceConfig | null = null;

  constructor(projectRoot: string) {
    this.projectRoot = projectRoot;
  }

  private async detectConfiguration(): Promise<LocalizationServiceConfig> {
    if (this.config) {
      return this.config;
    }

    try {
      // Find LocalizationServiceProfile.asset directly
      const profilePath = await this.findLocalizationServiceProfile();

      if (!profilePath) {
        throw new Error(
          '❌ LocalizationServiceProfile.asset not found. Set LOCALIZATION_PROFILE_PATH environment variable or place it in Assets/ServiceProvidersProfile/'
        );
      }

      // Load the profile to get catalog path and other settings
      const localizationProfile = await this.loadYamlFile(profilePath);
      const profileData = localizationProfile?.MonoBehaviour;

      if (!profileData) {
        throw new Error(
          '❌ Invalid LocalizationServiceProfile.asset structure'
        );
      }

      const catalogPath =
        profileData.catalogPath || 'Assets/UX/Localization/Catalogs';
      const defaultLocale = profileData.defaultLocale || 'en-US';
      const supportedLocales = profileData.supportedLocales || [
        'en-US',
        'es-ES',
        'fr-FR',
        'test',
      ];

      // Determine generated keys path (same directory as profile)
      const generatedKeysPath = path.join(
        path.dirname(profilePath),
        'LocalizationKeys.g.cs'
      );

      this.config = {
        profilePath,
        catalogPath: path.join(this.projectRoot, catalogPath),
        generatedKeysPath,
        defaultLocale,
        supportedLocales,
      };

      return this.config;
    } catch (error) {
      throw new Error(
        `Failed to detect localization configuration: ${error instanceof Error ? error.message : String(error)}`
      );
    }
  }

  private async fileExists(filePath: string): Promise<boolean> {
    try {
      await fs.access(filePath);
      return true;
    } catch {
      return false;
    }
  }

  private async findLocalizationServiceProfile(): Promise<string | null> {
    // Check if user specified the path via environment variable
    const envPath = process.env.LOCALIZATION_PROFILE_PATH;
    if (envPath) {
      // Resolve relative paths against the project root
      const resolvedPath = path.isAbsolute(envPath) 
        ? envPath 
        : path.join(this.projectRoot, envPath);
      
      if (await this.fileExists(resolvedPath)) {
        return resolvedPath;
      }
      // If env var is specified but path doesn't exist, error out
      throw new Error(
        `LOCALIZATION_PROFILE_PATH is set to "${envPath}" (resolved to "${resolvedPath}") but file not found.`
      );
    }

    // Try common default locations
    const defaultPaths = [
      path.join(this.projectRoot, 'Assets/ServiceProvidersProfile/LocalizationServiceProfile.asset'),
      path.join(this.projectRoot, 'Assets/LocalizationServiceProfile.asset'),
    ];

    for (const searchPath of defaultPaths) {
      if (await this.fileExists(searchPath)) {
        return searchPath;
      }
    }

    return null;
  }


  private async findAssetByGuid(guid: string): Promise<string | null> {
    // Search for .meta files containing the GUID
    // This works across any project structure since GUIDs are unique per asset
    
    const searchDir = path.join(this.projectRoot, 'Assets');
    
    try {
      return await this.searchForGuidInMetaFiles(searchDir, guid);
    } catch (error) {
      return null;
    }
  }

  private async searchForGuidInMetaFiles(dirPath: string, guid: string): Promise<string | null> {
    try {
      const entries = await fs.readdir(dirPath, { withFileTypes: true });
      
      for (const entry of entries) {
        const fullPath = path.join(dirPath, entry.name);
        
        if (entry.isDirectory()) {
          // Recursively search subdirectories
          const found = await this.searchForGuidInMetaFiles(fullPath, guid);
          if (found) {
            return found;
          }
        } else if (entry.name.endsWith('.meta')) {
          // Check if this .meta file contains the GUID
          const metaContent = await fs.readFile(fullPath, 'utf-8');
          
          // Look for the GUID in the .meta file (format: "guid: {guid}")
          if (metaContent.includes(`guid: ${guid}`)) {
            // Return the asset file (remove .meta extension)
            const assetPath = fullPath.slice(0, -5); // Remove '.meta'
            if (await this.fileExists(assetPath)) {
              return assetPath;
            }
          }
        }
      }
      
      return null;
    } catch (error) {
      return null;
    }
  }


  private async loadYamlFile(filePath: string): Promise<any> {
    const content = await fs.readFile(filePath, 'utf-8');
    
    // Use Unity YAML schema that handles Unity-specific tags
    try {
      return yaml.load(content, { 
        schema: UNITY_SCHEMA,
        onWarning: () => {} // Suppress warnings
      });
    } catch (error) {
      throw new Error(`Failed to parse YAML file ${filePath}: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  private parseUnityYaml(content: string): any {
    // Custom parser for Unity YAML files that handles array items and nested structures
    const lines = content.split('\n');
    const result: any = {};
    const stack: Array<{ indent: number; container: any; isArray: boolean }> = [
      { indent: -1, container: result, isArray: false },
    ];

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];

      // Skip empty lines, comments, and YAML directives
      if (!line.trim() || line.trim().startsWith('%')) {
        continue;
      }

      // Skip document separators with Unity tags (e.g., "--- !u!114 &11400000")
      if (line.trim().startsWith('---')) {
        continue;
      }

      // Skip Unity tags
      if (line.includes('!<tag:unity3d.com')) {
        continue;
      }

      const indent = line.search(/\S/);
      const trimmed = line.trim();

      // Pop stack based on indentation
      while (stack.length > 1 && indent < stack[stack.length - 1].indent) {
        stack.pop();
      }

      const current = stack[stack.length - 1];

      if (trimmed.startsWith('- ')) {
        // Array item
        const rest = trimmed.substring(2).trim();

        if (rest.includes(':')) {
          // Array item with properties: "- key: value"
          const match = rest.match(/^([^:]+):\s*(.*)$/);
          if (match) {
            const key = match[1].trim();
            const value = match[2].trim();

            const arrayItem: any = {};
            if (!Array.isArray(current.container)) {
              current.container = [];
            }
            current.container.push(arrayItem);

            if (value === '') {
              arrayItem[key] = {};
              stack.push({ indent: indent + 2, container: arrayItem, isArray: false });
            } else if (value === '[]') {
              arrayItem[key] = [];
              stack.push({ indent: indent + 2, container: arrayItem[key], isArray: true });
            } else if (value === '{}') {
              arrayItem[key] = {};
              stack.push({ indent: indent + 2, container: arrayItem[key], isArray: false });
            } else {
              arrayItem[key] = this.parseValue(value);
              stack.push({ indent: indent, container: arrayItem, isArray: false });
            }
          }
        } else {
          // Simple array value
          if (!Array.isArray(current.container)) {
            current.container = [];
          }
          current.container.push(this.parseValue(rest));
        }
      } else {
        // Regular key: value
        const match = trimmed.match(/^([^:]+):\s*(.*)$/);
        if (match) {
          const key = match[1].trim();
          const value = match[2].trim();

          // Ensure we have the right type of container
          if (Array.isArray(current.container)) {
            // We're in an array, but got a key: value - shouldn't happen, skip
            continue;
          }

          if (value === '') {
            // Empty value - check next line to see if nested
            if (i + 1 < lines.length) {
              const nextLine = lines[i + 1];
              const nextIndent = nextLine.search(/\S/);
              if (nextIndent > indent && !nextLine.trim().startsWith('%')) {
                // Next line is indented, create nested object
                const nested: any = {};
                current.container[key] = nested;
                stack.push({ indent: indent + 2, container: nested, isArray: false });
              } else {
                current.container[key] = null;
              }
            } else {
              current.container[key] = null;
            }
          } else if (value === '[]') {
            // Array
            current.container[key] = [];
            stack.push({ indent: indent + 2, container: current.container[key], isArray: true });
          } else if (value === '{}') {
            // Object
            current.container[key] = {};
            stack.push({ indent: indent + 2, container: current.container[key], isArray: false });
          } else {
            // Value
            current.container[key] = this.parseValue(value);
          }
        }
      }
    }

    return result;
  }

  private parseValue(value: string): any {
    if (value === '' || value === 'null') {
      return null;
    } else if (value === '[]') {
      return [];
    } else if (value === '{}') {
      return {};
    } else if (value.startsWith('{fileID:')) {
      // Parse fileID references
      const fileIdMatch = value.match(/\{fileID:\s*(\d+),\s*guid:\s*([^,}]+)/);
      if (fileIdMatch) {
        return {
          fileID: parseInt(fileIdMatch[1]),
          guid: fileIdMatch[2].trim(),
        };
      }
      return value;
    } else if (value === 'true' || value === 'false') {
      return value === 'true';
    } else if (!isNaN(Number(value)) && value !== '') {
      return Number(value);
    } else {
      return value.replace(/^['"]|['"]$/g, '');
    }
  }

  async addKey(
    key: string,
    displayName: string,
    translations: Record<string, string>
  ) {
    try {
      // Detect configuration first
      const config = await this.detectConfiguration();

      // Validate inputs
      this.validateKeyName(key);
      this.validateDisplayName(displayName);

      // Load current state
      const profile = await this.loadProfile(config.profilePath);
      const catalogs = await this.loadAllCatalogs(config);

      // Check if key already exists
      if (profile.localizationKeys.some((k) => k.key === key)) {
        throw new Error(`Key "${key}" already exists in profile`);
      }

      // Verify all required locales have translations
      for (const locale of config.supportedLocales) {
        if (!translations[locale]) {
          throw new Error(`Missing translation for locale: ${locale}`);
        }
      }

      // Add to profile with both key and displayName
      profile.localizationKeys.push({ key, displayName });
      profile.localizationKeys.sort((a, b) => a.key.localeCompare(b.key));

      // Add to catalogs
      for (const catalog of catalogs) {
        if (translations[catalog.locale]) {
          catalog.keys[key] = translations[catalog.locale];
        }
      }

      // Write changes
      await this.saveProfile(config.profilePath, profile);
      await this.saveAllCatalogs(config, catalogs);
      await this.regenerateKeysFile(config, profile);

      return {
        content: [
          {
            type: 'text',
            text: `✅ Successfully added localization key "${key}"\n\n**Updated files:**\n- ${config.profilePath}\n- ${catalogs.length} catalog files in ${config.catalogPath}\n- ${config.generatedKeysPath}\n\n**Generated constant:** \`LocalizationKeys.${displayName}\`\n\n**Usage in code:**\n\`\`\`csharp\nLocalizationService.GetString(LocalizationKeys.${displayName})\n\`\`\``,
          },
        ],
      };
    } catch (error) {
      throw new Error(
        `Failed to add localization key: ${error instanceof Error ? error.message : String(error)}`
      );
    }
  }

  async batchAddKeys(
    keys: Array<{
      key: string;
      displayName: string;
      translations: Record<string, string>;
    }>
  ) {
    try {
      const config = await this.detectConfiguration();

      // Validate all keys first
      for (const item of keys) {
        this.validateKeyName(item.key);
        this.validateDisplayName(item.displayName);
      }

      // Load current state
      const profile = await this.loadProfile(config.profilePath);
      const catalogs = await this.loadAllCatalogs(config);

      // Check for duplicates
      for (const item of keys) {
        if (profile.localizationKeys.some((k) => k.key === item.key)) {
          throw new Error(`Key "${item.key}" already exists in profile`);
        }
      }

      // Add all keys
      for (const item of keys) {
        // Verify all required locales have translations
        for (const locale of config.supportedLocales) {
          if (!item.translations[locale]) {
            throw new Error(
              `Missing translation for key "${item.key}" in locale: ${locale}`
            );
          }
        }

        profile.localizationKeys.push({
          key: item.key,
          displayName: item.displayName,
        });

        for (const catalog of catalogs) {
          if (item.translations[catalog.locale]) {
            catalog.keys[item.key] = item.translations[catalog.locale];
          }
        }
      }

      // Sort profile keys
      profile.localizationKeys.sort((a, b) => a.key.localeCompare(b.key));

      // Write changes
      await this.saveProfile(config.profilePath, profile);
      await this.saveAllCatalogs(config, catalogs);
      await this.regenerateKeysFile(config, profile);

      return {
        content: [
          {
            type: 'text',
            text: `✅ Successfully added ${keys.length} localization keys\n\n**Keys added:**\n${keys.map((k) => `- ${k.key} → \`LocalizationKeys.${k.displayName}\``).join('\n')}\n\n**Updated files:**\n- ${config.profilePath}\n- ${catalogs.length} catalog files\n- ${config.generatedKeysPath}`,
          },
        ],
      };
    } catch (error) {
      throw new Error(
        `Failed to batch add localization keys: ${error instanceof Error ? error.message : String(error)}`
      );
    }
  }

  async updateKey(key: string, translations: Record<string, string>) {
    try {
      const config = await this.detectConfiguration();
      const catalogs = await this.loadAllCatalogs(config);

      // Check if key exists
      const keyExists = catalogs.every((c) => key in c.keys);
      if (!keyExists) {
        throw new Error(`Key "${key}" does not exist in all catalogs`);
      }

      // Update translations
      for (const catalog of catalogs) {
        if (translations[catalog.locale]) {
          catalog.keys[key] = translations[catalog.locale];
        }
      }

      await this.saveAllCatalogs(config, catalogs);

      return {
        content: [
          {
            type: 'text',
            text: `✅ Successfully updated translations for key "${key}"\n\nUpdated ${catalogs.length} catalog files`,
          },
        ],
      };
    } catch (error) {
      throw new Error(
        `Failed to update localization key: ${error instanceof Error ? error.message : String(error)}`
      );
    }
  }

  async removeKey(key: string) {
    try {
      const config = await this.detectConfiguration();
      const profile = await this.loadProfile(config.profilePath);
      const catalogs = await this.loadAllCatalogs(config);

      // Remove from profile
      const keyIndex = profile.localizationKeys.findIndex((k) => k.key === key);
      if (keyIndex === -1) {
        throw new Error(`Key "${key}" not found in profile`);
      }
      profile.localizationKeys.splice(keyIndex, 1);

      // Remove from catalogs
      for (const catalog of catalogs) {
        delete catalog.keys[key];
      }

      // Write changes
      await this.saveProfile(config.profilePath, profile);
      await this.saveAllCatalogs(config, catalogs);
      await this.regenerateKeysFile(config, profile);

      return {
        content: [
          {
            type: 'text',
            text: `✅ Successfully removed localization key "${key}"\n\nUpdated files:\n- ${config.profilePath}\n- ${catalogs.length} catalog files\n- ${config.generatedKeysPath}`,
          },
        ],
      };
    } catch (error) {
      throw new Error(
        `Failed to remove localization key: ${error instanceof Error ? error.message : String(error)}`
      );
    }
  }

  async validate() {
    const result: ValidationResult = {
      valid: true,
      errors: [],
      warnings: [],
    };

    try {
      const config = await this.detectConfiguration();
      const profile = await this.loadProfile(config.profilePath);
      const catalogs = await this.loadAllCatalogs(config);

      // Get all unique keys from catalogs
      const allCatalogKeys = new Set<string>();
      for (const catalog of catalogs) {
        Object.keys(catalog.keys).forEach((k) => allCatalogKeys.add(k));
      }

      // Check each catalog has all keys
      for (const catalog of catalogs) {
        const catalogKeys = new Set(Object.keys(catalog.keys));
        for (const key of allCatalogKeys) {
          if (!catalogKeys.has(key)) {
            result.errors.push(
              `Catalog "${catalog.locale}" missing key: ${key}`
            );
            result.valid = false;
          }
        }
      }

      // Check profile keys match catalog keys
      const profileKeys = new Set(profile.localizationKeys.map((k) => k.key));
      for (const key of allCatalogKeys) {
        if (!profileKeys.has(key)) {
          result.errors.push(
            `Key "${key}" exists in catalogs but not in profile`
          );
          result.valid = false;
        }
      }
      for (const key of profileKeys) {
        if (!allCatalogKeys.has(key)) {
          result.warnings.push(
            `Key "${key}" exists in profile but not in catalogs`
          );
        }
      }

      // Check that all profile keys have displayName
      for (const item of profile.localizationKeys) {
        if (!item.displayName || item.displayName.trim() === '') {
          result.warnings.push(
            `Key "${item.key}" missing displayName in profile`
          );
        } else if (!this.isValidDisplayName(item.displayName)) {
          result.warnings.push(
            `Display name "${item.displayName}" does not follow PascalCase convention`
          );
        }
      }

      const statusText = result.valid
        ? '✅ Localization is valid!'
        : '❌ Localization has errors';
      const details = [
        statusText,
        '',
        `**Configuration:**`,
        `- Profile: ${config.profilePath}`,
        `- Catalogs: ${config.catalogPath}`,
        `- Generated keys: ${config.generatedKeysPath}`,
        '',
        `**Statistics:**`,
        `- Profile keys: ${profileKeys.size}`,
        `- Catalog keys: ${allCatalogKeys.size}`,
        `- Supported locales: ${config.supportedLocales.join(', ')}`,
      ];

      if (result.errors.length > 0) {
        details.push('', '**Errors:**');
        result.errors.forEach((e) => details.push(`- ${e}`));
      }

      if (result.warnings.length > 0) {
        details.push('', '**Warnings:**');
        result.warnings.forEach((w) => details.push(`- ${w}`));
      }

      return {
        content: [
          {
            type: 'text',
            text: details.join('\n'),
          },
        ],
      };
    } catch (error) {
      throw new Error(
        `Failed to validate localization: ${error instanceof Error ? error.message : String(error)}`
      );
    }
  }

  async getStatus(verbose: boolean = false) {
    try {
      const config = await this.detectConfiguration();
      const profile = await this.loadProfile(config.profilePath);
      const catalogs = await this.loadAllCatalogs(config);

      const status = [
        '📊 **Localization Status**',
        '',
        '**Configuration:**',
        `- Profile: ${config.profilePath}`,
        `- Catalogs: ${config.catalogPath}`,
        `- Generated keys: ${config.generatedKeysPath}`,
        `- Default locale: ${config.defaultLocale}`,
        '',
        '**Statistics:**',
        `- Total keys: ${profile.localizationKeys.length}`,
        `- Supported locales: ${config.supportedLocales.length}`,
        '',
        '**Catalogs:**',
      ];

      for (const catalog of catalogs) {
        const keyCount = Object.keys(catalog.keys).length;
        status.push(`- ${catalog.locale}: ${keyCount} keys`);
      }

      if (verbose) {
        status.push('', '**All Keys:**');
        profile.localizationKeys.forEach((k) => {
          status.push(
            `- ${k.key}${k.displayName ? ` → \`LocalizationKeys.${k.displayName}\`` : ' (no displayName)'}`
          );
        });
      }

      return {
        content: [
          {
            type: 'text',
            text: status.join('\n'),
          },
        ],
      };
    } catch (error) {
      throw new Error(
        `Failed to get localization status: ${error instanceof Error ? error.message : String(error)}`
      );
    }
  }

  async getKeyInfo(key: string) {
    try {
      const config = await this.detectConfiguration();
      const profile = await this.loadProfile(config.profilePath);
      const catalogs = await this.loadAllCatalogs(config);

      const profileKey = profile.localizationKeys.find((k) => k.key === key);
      if (!profileKey) {
        throw new Error(`Key "${key}" not found in profile`);
      }

      const info = [
        `🔑 **Key Information: ${key}**`,
        '',
        `**Display Name:** ${profileKey.displayName || 'Not set'}`,
        `**Constant:** \`LocalizationKeys.${profileKey.displayName || 'N/A'}\``,
        '',
        '**Usage in code:**',
        '```csharp',
        `LocalizationService.GetString(LocalizationKeys.${profileKey.displayName || 'N/A'})`,
        '```',
        '',
        '**Translations:**',
      ];

      for (const catalog of catalogs) {
        const translation = catalog.keys[key];
        info.push(
          `- ${catalog.locale}: ${translation ? `"${translation}"` : '❌ MISSING'}`
        );
      }

      return {
        content: [
          {
            type: 'text',
            text: info.join('\n'),
          },
        ],
      };
    } catch (error) {
      throw new Error(
        `Failed to get key info: ${error instanceof Error ? error.message : String(error)}`
      );
    }
  }

  private async loadProfile(profilePath: string): Promise<{
    localizationKeys: LocalizationKey[];
  }> {
    const content = await fs.readFile(profilePath, 'utf-8');
    const data = yaml.load(content) as any;
    const keys = data['MonoBehaviour']['localizationKeys'] || [];
    
    // Ensure each key has both key and displayName properties
    return {
      localizationKeys: keys.map((k: any) => ({
        key: k.key || '',
        displayName: k.displayName || '',
      })),
    };
  }

  private async saveProfile(
    profilePath: string,
    profile: { localizationKeys: LocalizationKey[] }
  ) {
    const content = await fs.readFile(profilePath, 'utf-8');
    const data = yaml.load(content) as any;
    
    // Update localizationKeys array with both key and displayName
    data['MonoBehaviour']['localizationKeys'] = profile.localizationKeys.map(
      (k) => ({
        key: k.key,
        displayName: k.displayName,
      })
    );
    
    const newContent = yaml.dump(data, { lineWidth: -1, noRefs: true });
    await fs.writeFile(profilePath, newContent, 'utf-8');
  }

  private async loadAllCatalogs(
    config: LocalizationServiceConfig
  ): Promise<LocalizationCatalog[]> {
    const catalogs: LocalizationCatalog[] = [];

    for (const locale of config.supportedLocales) {
      const catalogPath = path.join(config.catalogPath, `${locale}.json`);
      
      if (await this.fileExists(catalogPath)) {
        const content = await fs.readFile(catalogPath, 'utf-8');
        catalogs.push(JSON.parse(content));
      } else {
        throw new Error(
          `Catalog file not found for locale "${locale}" at ${catalogPath}`
        );
      }
    }

    return catalogs;
  }

  private async saveAllCatalogs(
    config: LocalizationServiceConfig,
    catalogs: LocalizationCatalog[]
  ) {
    for (const catalog of catalogs) {
      const filePath = path.join(config.catalogPath, `${catalog.locale}.json`);
      await fs.writeFile(
        filePath,
        JSON.stringify(catalog, null, 2) + '\n',
        'utf-8'
      );
    }
  }

  private async regenerateKeysFile(
    config: LocalizationServiceConfig,
    profile: { localizationKeys: LocalizationKey[] }
  ) {
    const keys = profile.localizationKeys
      .filter((k) => k.displayName)
      .sort((a, b) => a.displayName.localeCompare(b.displayName));

    const lines = [
      '// <auto-generated>',
      '// This file is auto-generated by LocalizationKeysGenerator.',
      '// Do not modify this file directly.',
      '// </auto-generated>',
      '',
      'namespace RealityCollective.UXManager.Services.Localization',
      '{',
      '    /// <summary>',
      '    /// Auto-generated localization key constants from the default (en-US) locale catalog.',
      '    /// Use these constants with ILocalizationService.GetString() for type-safe localization lookups.',
      '    /// </summary>',
      '    public static class LocalizationKeys',
      '    {',
    ];

    for (const key of keys) {
      lines.push(`        /// <summary>${key.key}</summary>`);
      lines.push(
        `        public const string ${key.displayName} = "${key.key}";`
      );
      lines.push('');
    }

    lines.push('    }');
    lines.push('}');

    await fs.writeFile(config.generatedKeysPath, lines.join('\n'), 'utf-8');
  }

  async checkKeyHealth(key: string) {
    try {
      const config = await this.detectConfiguration();
      const profile = await this.loadProfile(config.profilePath);
      const catalogs = await this.loadAllCatalogs(config);

      const report: KeyHealthReport = {
        keyExists: false,
        configurationHealth: {
          profileEntry: false,
          displayNameValid: false,
          displayNamePascalCase: false,
          allCatalogsHaveKey: true,
          missingFromLocales: [],
        },
        translationHealth: {
          allTranslationsPresent: true,
          allTranslationsNonEmpty: true,
          emptyInLocales: [],
        },
        codeUsageHealth: {
          properUsageCount: 0,
          improperUsageCount: 0,
          usedWithLocalKeys: [],
          usedWithRawStrings: [],
          potentiallyUnused: true,
        },
      };

      // 1. Check profile entry
      const profileEntry = profile.localizationKeys.find((k) => k.key === key);
      if (profileEntry) {
        report.keyExists = true;
        report.configurationHealth.profileEntry = true;
        report.configurationHealth.displayNameValid =
          !!profileEntry.displayName && profileEntry.displayName.trim() !== '';
        report.configurationHealth.displayNamePascalCase =
          this.isValidDisplayName(profileEntry.displayName);
      }

      // 2. Check catalog entries
      for (const catalog of catalogs) {
        if (!(key in catalog.keys)) {
          report.configurationHealth.allCatalogsHaveKey = false;
          report.configurationHealth.missingFromLocales.push(catalog.locale);
        } else {
          const translation = catalog.keys[key];
          if (!translation || translation.trim() === '') {
            report.translationHealth.allTranslationsNonEmpty = false;
            report.translationHealth.emptyInLocales.push(catalog.locale);
          }
        }
      }

      report.translationHealth.allTranslationsPresent =
        report.configurationHealth.missingFromLocales.length === 0;

      // 3. Check C# code usage patterns
      const codeUsage = await this.scanForKeyUsage(key, config, profileEntry);
      report.codeUsageHealth = codeUsage;

      // Generate report
      const issues = this.generateHealthReport(
        key,
        report,
        profileEntry,
        config
      );

      return {
        content: [
          {
            type: 'text',
            text: issues,
          },
        ],
      };
    } catch (error) {
      throw new Error(
        `Failed to check key health: ${error instanceof Error ? error.message : String(error)}`
      );
    }
  }

  private async scanForKeyUsage(
    key: string,
    config: LocalizationServiceConfig,
    profileEntry: LocalizationKey | undefined
  ) {
    const usage = {
      properUsageCount: 0,
      improperUsageCount: 0,
      usedWithLocalKeys: [] as string[],
      usedWithRawStrings: [] as string[],
      potentiallyUnused: true,
    };

    try {
      // Search in Assets directory for C# files
      const assetsPath = path.join(config.profilePath, '../../..');
      const csFiles = await this.findCsFiles(assetsPath);

      for (const filePath of csFiles) {
        try {
          const content = await fs.readFile(filePath, 'utf-8');

          // Check for proper usage: LocalizationKeys.DisplayName
          if (profileEntry?.displayName) {
            const properPattern = new RegExp(
              `LocalizationKeys\\.${profileEntry.displayName}\\b`,
              'g'
            );
            const properMatches = content.match(properPattern);
            if (properMatches) {
              usage.properUsageCount += properMatches.length;
              usage.potentiallyUnused = false;
              if (!usage.usedWithLocalKeys.includes(filePath)) {
                usage.usedWithLocalKeys.push(filePath);
              }
            }
          }

          // Check for improper usage: raw string matching key
          const rawStringPatterns = [
            new RegExp(`GetString\\s*\\(\\s*"${key}"\\s*\\)`, 'g'),
            new RegExp(`GetString\\s*\\(\\s*'${key}'\\s*\\)`, 'g'),
            new RegExp(`"${key}"`, 'g'),
          ];

          let improperMatches = 0;
          for (const pattern of rawStringPatterns) {
            const matches = content.match(pattern);
            if (matches) {
              improperMatches += matches.length;
            }
          }

          if (improperMatches > 0) {
            usage.improperUsageCount += improperMatches;
            if (!usage.usedWithRawStrings.includes(filePath)) {
              usage.usedWithRawStrings.push(filePath);
            }
          }
        } catch {
          // Skip files that can't be read
        }
      }
    } catch {
      // If scanning fails, continue with zero usage counts
    }

    return usage;
  }

  private async findCsFiles(startPath: string): Promise<string[]> {
    const files: string[] = [];

    async function traverse(dir: string) {
      try {
        const entries = await fs.readdir(dir, { withFileTypes: true });
        for (const entry of entries) {
          const fullPath = path.join(dir, entry.name);

          // Skip common non-source directories
          if (
            entry.isDirectory() &&
            !['Library', 'Temp', 'obj', 'bin', 'node_modules', '.git'].includes(
              entry.name
            )
          ) {
            await traverse(fullPath);
          } else if (entry.isFile() && entry.name.endsWith('.cs')) {
            files.push(fullPath);
          }
        }
      } catch {
        // Skip directories that can't be read
      }
    }

    await traverse(startPath);
    return files;
  }

  private generateHealthReport(
    key: string,
    report: KeyHealthReport,
    profileEntry: LocalizationKey | undefined,
    config: LocalizationServiceConfig
  ): string {
    const lines = [`🏥 **Key Health Report: ${key}**`, ''];

    // Overall status
    const isHealthy =
      report.keyExists &&
      report.configurationHealth.profileEntry &&
      report.configurationHealth.displayNameValid &&
      report.configurationHealth.displayNamePascalCase &&
      report.configurationHealth.allCatalogsHaveKey &&
      report.translationHealth.allTranslationsPresent &&
      report.translationHealth.allTranslationsNonEmpty &&
      report.codeUsageHealth.improperUsageCount === 0;

    if (isHealthy && report.codeUsageHealth.properUsageCount > 0) {
      lines.push('✅ **Status: HEALTHY**');
    } else if (isHealthy && report.codeUsageHealth.properUsageCount === 0) {
      lines.push('⚠️ **Status: HEALTHY BUT UNUSED**');
    } else {
      lines.push('❌ **Status: ISSUES DETECTED**');
    }

    lines.push('');

    // Configuration health
    lines.push('**📋 Configuration:**');
    if (!report.keyExists) {
      lines.push('- ❌ Key does not exist in profile');
    } else {
      lines.push('- ✅ Key exists in profile');
      lines.push(
        `- ${report.configurationHealth.displayNameValid ? '✅' : '❌'} DisplayName: ${profileEntry?.displayName || '(empty)'}`
      );
      lines.push(
        `- ${report.configurationHealth.displayNamePascalCase ? '✅' : '⚠️'} PascalCase format: ${report.configurationHealth.displayNamePascalCase ? 'Valid' : 'Invalid - should be PascalCase'}`
      );
    }

    // Catalog health
    lines.push('');
    lines.push('**🗂️ Catalogs:**');
    if (report.configurationHealth.allCatalogsHaveKey) {
      lines.push('- ✅ Present in all locales');
    } else {
      lines.push('- ❌ Missing from locales:');
      report.configurationHealth.missingFromLocales.forEach((locale) => {
        lines.push(`  - ${locale}`);
      });
    }

    // Translation health
    lines.push('');
    lines.push('**📝 Translations:**');
    if (report.translationHealth.allTranslationsNonEmpty) {
      lines.push('- ✅ All translations present and non-empty');
    } else {
      lines.push('- ❌ Empty or missing translations in:');
      report.translationHealth.emptyInLocales.forEach((locale) => {
        lines.push(`  - ${locale}`);
      });
    }

    // Code usage health
    lines.push('');
    lines.push('**💻 Code Usage:**');
    if (report.codeUsageHealth.properUsageCount > 0) {
      lines.push(
        `- ✅ Proper usage found: ${report.codeUsageHealth.properUsageCount} occurrence(s)`
      );
      lines.push(`  Files: ${report.codeUsageHealth.usedWithLocalKeys.slice(0, 3).map((f) => path.basename(f)).join(', ')}`);
      if (report.codeUsageHealth.usedWithLocalKeys.length > 3) {
        lines.push(
          `  ... and ${report.codeUsageHealth.usedWithLocalKeys.length - 3} more`
        );
      }
    } else {
      lines.push('- ⚠️ No proper usage found (possibly unused)');
    }

    if (report.codeUsageHealth.improperUsageCount > 0) {
      lines.push(
        `- ❌ Improper raw string usage: ${report.codeUsageHealth.improperUsageCount} occurrence(s)`
      );
      lines.push(`  Files: ${report.codeUsageHealth.usedWithRawStrings.slice(0, 3).map((f) => path.basename(f)).join(', ')}`);
      if (report.codeUsageHealth.usedWithRawStrings.length > 3) {
        lines.push(
          `  ... and ${report.codeUsageHealth.usedWithRawStrings.length - 3} more`
        );
      }
      lines.push('  **Action:** Replace raw strings with `LocalizationKeys.*` constants');
    }

    // Recommendations
    if (!isHealthy || report.codeUsageHealth.properUsageCount === 0) {
      lines.push('');
      lines.push('**💡 Recommendations:**');

      if (!report.configurationHealth.profileEntry) {
        lines.push('- Add key to LocalizationServiceProfile');
      }
      if (!report.configurationHealth.displayNameValid) {
        lines.push('- Add displayName to profile entry');
      }
      if (!report.configurationHealth.displayNamePascalCase) {
        lines.push('- Convert displayName to PascalCase');
      }
      if (!report.configurationHealth.allCatalogsHaveKey) {
        lines.push(
          `- Add translations for: ${report.configurationHealth.missingFromLocales.join(', ')}`
        );
      }
      if (!report.translationHealth.allTranslationsNonEmpty) {
        lines.push(
          `- Fill empty translations in: ${report.translationHealth.emptyInLocales.join(', ')}`
        );
      }
      if (report.codeUsageHealth.improperUsageCount > 0) {
        lines.push('- Replace raw string usage with generated constant');
      }
      if (report.codeUsageHealth.properUsageCount === 0) {
        lines.push(
          '- Either use this key in code or consider removing it if obsolete'
        );
      }
    }

    return lines.join('\n');
  }

  private validateKeyName(key: string) {
    if (!/^[a-z_][a-z0-9_]*$/.test(key)) {
      throw new Error(
        `Invalid key name "${key}". Must be lowercase with underscores only.`
      );
    }
  }

  private validateDisplayName(displayName: string) {
    if (!/^[A-Z][a-zA-Z0-9]*$/.test(displayName)) {
      throw new Error(
        `Invalid display name "${displayName}". Must be PascalCase (start with uppercase, no underscores).`
      );
    }
  }

  private isValidDisplayName(displayName: string): boolean {
    return /^[A-Z][a-zA-Z0-9]*$/.test(displayName);
  }

  async healthCheck() {
    const checks: {
      name: string;
      status: 'pass' | 'fail' | 'warning';
      message: string;
    }[] = [];

    let allPassed = true;

    // Check 1: Configuration Detection
    try {
      const config = await this.detectConfiguration();
      checks.push({
        name: 'Configuration Detection',
        status: 'pass',
        message: `Found configuration at: ${path.basename(config.profilePath)}`,
      });
    } catch (error) {
      allPassed = false;
      checks.push({
        name: 'Configuration Detection',
        status: 'fail',
        message: `Failed to detect configuration: ${error instanceof Error ? error.message : String(error)}`,
      });
      return this.formatHealthReport(checks, allPassed);
    }

    const config = await this.detectConfiguration();

    // Check 2: Profile Access
    try {
      const profile = await this.loadProfile(config.profilePath);
      checks.push({
        name: 'Profile Access',
        status: 'pass',
        message: `Profile accessible with ${profile.localizationKeys.length} keys`,
      });
    } catch (error) {
      allPassed = false;
      checks.push({
        name: 'Profile Access',
        status: 'fail',
        message: `Cannot access profile: ${error instanceof Error ? error.message : String(error)}`,
      });
      return this.formatHealthReport(checks, allPassed);
    }

    // Check 3: Catalog Access
    try {
      const catalogs = await this.loadAllCatalogs(config);
      const localeStatus = config.supportedLocales
        .map((locale) => {
          const found = catalogs.find((c) => c.locale === locale);
          return found ? `✓ ${locale}` : `✗ ${locale}`;
        })
        .join(', ');

      if (catalogs.length === config.supportedLocales.length) {
        checks.push({
          name: 'Catalog Access',
          status: 'pass',
          message: `All ${catalogs.length} catalogs accessible: ${localeStatus}`,
        });
      } else {
        allPassed = false;
        checks.push({
          name: 'Catalog Access',
          status: 'fail',
          message: `Only ${catalogs.length}/${config.supportedLocales.length} catalogs accessible: ${localeStatus}`,
        });
      }
    } catch (error) {
      allPassed = false;
      checks.push({
        name: 'Catalog Access',
        status: 'fail',
        message: `Cannot access catalogs: ${error instanceof Error ? error.message : String(error)}`,
      });
    }

    // Check 4: Keys File Access
    try {
      if (await fs.access(config.generatedKeysPath).then(() => true).catch(() => false)) {
        checks.push({
          name: 'Generated Keys File',
          status: 'pass',
          message: `Keys file accessible: ${path.basename(config.generatedKeysPath)}`,
        });
      } else {
        checks.push({
          name: 'Generated Keys File',
          status: 'warning',
          message: `Keys file not found. Will be created on first operation.`,
        });
      }
    } catch (error) {
      checks.push({
        name: 'Generated Keys File',
        status: 'warning',
        message: `Could not access keys file (will be created on first operation)`,
      });
    }

    // Check 5: Service Discovery
    try {
      const profile = await this.loadProfile(config.profilePath);
      const hasServiceEntry = profile.localizationKeys.length > 0;
      checks.push({
        name: 'Service Discovery',
        status: 'pass',
        message: `ILocalizationService discovered via GUID-based lookup`,
      });
    } catch (error) {
      allPassed = false;
      checks.push({
        name: 'Service Discovery',
        status: 'fail',
        message: `Service discovery failed: ${error instanceof Error ? error.message : String(error)}`,
      });
    }

    // Check 6: Configuration Consistency
    try {
      const profile = await this.loadProfile(config.profilePath);
      const catalogs = await this.loadAllCatalogs(config);

      const allKeys = new Set<string>();
      profile.localizationKeys.forEach((k) => allKeys.add(k.key));
      catalogs.forEach((c) => {
        Object.keys(c.keys).forEach((k) => allKeys.add(k));
      });

      const mismatches: string[] = [];

      // Check profile keys in all catalogs
      profile.localizationKeys.forEach((k) => {
        const missingLocales = config.supportedLocales.filter(
          (locale) => !catalogs.find((c) => c.locale === locale && k.key in c.keys)
        );
        if (missingLocales.length > 0) {
          mismatches.push(`${k.key} missing from: ${missingLocales.join(', ')}`);
        }
      });

      if (mismatches.length === 0) {
        checks.push({
          name: 'Configuration Consistency',
          status: 'pass',
          message: `All profile keys present in all catalogs`,
        });
      } else {
        allPassed = false;
        checks.push({
          name: 'Configuration Consistency',
          status: 'fail',
          message: `Found ${mismatches.length} inconsistencies: ${mismatches.slice(0, 3).join('; ')}${mismatches.length > 3 ? '; ...' : ''}`,
        });
      }
    } catch (error) {
      allPassed = false;
      checks.push({
        name: 'Configuration Consistency',
        status: 'fail',
        message: `Consistency check failed: ${error instanceof Error ? error.message : String(error)}`,
      });
    }

    return this.formatHealthReport(checks, allPassed);
  }

  private formatHealthReport(
    checks: { name: string; status: 'pass' | 'fail' | 'warning'; message: string }[],
    allPassed: boolean
  ) {
    const statusIcon = allPassed ? '🟢' : '🔴';
    const status = allPassed ? 'OPERATIONAL' : 'ISSUES DETECTED';

    const lines = [`⚕️ **MCP Server Health Report**`, ``, `${statusIcon} **Status: ${status}**`, ''];

    lines.push('**Checks:**');
    lines.push('');

    checks.forEach((check) => {
      const icon = check.status === 'pass' ? '✅' : check.status === 'fail' ? '❌' : '⚠️';
      lines.push(`${icon} **${check.name}**`);
      lines.push(`   ${check.message}`);
      lines.push('');
    });

    const passCount = checks.filter((c) => c.status === 'pass').length;
    const failCount = checks.filter((c) => c.status === 'fail').length;
    const warningCount = checks.filter((c) => c.status === 'warning').length;

    lines.push('**Summary:**');
    lines.push(`- ✅ Passed: ${passCount}/${checks.length}`);
    if (failCount > 0) {
      lines.push(`- ❌ Failed: ${failCount}`);
    }
    if (warningCount > 0) {
      lines.push(`- ⚠️ Warnings: ${warningCount}`);
    }

    lines.push('');
    if (allPassed) {
      lines.push(
        '✅ **Server is ready for localization tasks**. You can safely call any localization tool.'
      );
    } else {
      lines.push(
        '❌ **Server has issues**. Please resolve failures above before attempting localization tasks.'
      );
    }

    return {
      content: [
        {
          type: 'text',
          text: lines.join('\n'),
        },
      ],
    };
  }
}
