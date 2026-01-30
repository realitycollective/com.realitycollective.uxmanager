#!/usr/bin/env node
import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  Tool,
} from '@modelcontextprotocol/sdk/types.js';
import { LocalizationManager } from './localization-manager.js';

const UNITY_PROJECT_ROOT = process.env.UNITY_PROJECT_ROOT || process.cwd();

// Initialize the MCP server
const server = new Server(
  {
    name: 'unity-localization-server',
    version: '1.0.0',
  },
  {
    capabilities: {
      tools: {},
    },
  }
);

// Initialize localization manager
const localizationManager = new LocalizationManager(UNITY_PROJECT_ROOT);

// Define available tools
const tools: Tool[] = [
  {
    name: 'unity_localization_add_key',
    description: 'Add a new localization key with translations to all catalogs and update the profile. Atomically updates profile, all catalogs, and regenerates LocalizationKeys.g.cs',
    inputSchema: {
      type: 'object',
      properties: {
        key: {
          type: 'string',
          description: 'Localization key (e.g., "enum_SocialPlatform_Instagram")',
        },
        displayName: {
          type: 'string',
          description: 'Display name for the profile (PascalCase, e.g., "EnumSocialPlatformInstagram")',
        },
        translations: {
          type: 'object',
          description: 'Translations for each locale',
          additionalProperties: { type: 'string' },
        },
      },
      required: ['key', 'displayName', 'translations'],
    },
  },
  {
    name: 'unity_localization_batch_add_keys',
    description: 'Add multiple localization keys at once',
    inputSchema: {
      type: 'object',
      properties: {
        keys: {
          type: 'array',
          items: {
            type: 'object',
            properties: {
              key: { type: 'string' },
              displayName: { type: 'string' },
              translations: {
                type: 'object',
                additionalProperties: { type: 'string' },
              },
            },
            required: ['key', 'displayName', 'translations'],
          },
        },
      },
      required: ['keys'],
    },
  },
  {
    name: 'unity_localization_update_key',
    description: 'Update translations for an existing localization key across all catalogs',
    inputSchema: {
      type: 'object',
      properties: {
        key: {
          type: 'string',
          description: 'Localization key to update',
        },
        translations: {
          type: 'object',
          description: 'New translations for each locale',
          additionalProperties: { type: 'string' },
        },
      },
      required: ['key', 'translations'],
    },
  },
  {
    name: 'unity_localization_validate',
    description: 'Validate localization consistency across all files. Checks: all catalogs have same keys, profile keys match catalogs, no orphaned keys, naming conventions followed',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'unity_localization_get_status',
    description: 'Get comprehensive status of localization system including key counts, missing translations, and inconsistencies',
    inputSchema: {
      type: 'object',
      properties: {
        verbose: {
          type: 'boolean',
          description: 'Include detailed information',
          default: false,
        },
      },
    },
  },
  {
    name: 'unity_localization_get_key_info',
    description: 'Get detailed information about a specific localization key',
    inputSchema: {
      type: 'object',
      properties: {
        key: {
          type: 'string',
          description: 'Localization key to query',
        },
      },
      required: ['key'],
    },
  },
  {
    name: 'unity_localization_remove_key',
    description: 'Remove a localization key from all catalogs and profile',
    inputSchema: {
      type: 'object',
      properties: {
        key: {
          type: 'string',
          description: 'Localization key to remove',
        },
      },
      required: ['key'],
    },
  },
  {
    name: 'unity_localization_check_key_health',
    description: 'Comprehensive health check of a localization key. Validates configuration, C# usage patterns, and translations. Checks for: key existence, proper displayName, translations completeness, proper usage patterns (LocalizationKeys.*), missing keys, and improper raw string usage.',
    inputSchema: {
      type: 'object',
      properties: {
        key: {
          type: 'string',
          description: 'Localization key to health check',
        },
      },
      required: ['key'],
    },
  },
  {
    name: 'unity_localization_health_check',
    description: 'Server health check. Validates MCP server startup, configuration access, file accessibility, and service discovery. Run this before performing localization tasks to ensure everything is configured correctly.',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
];

// Handle list_tools request
server.setRequestHandler(ListToolsRequestSchema, async () => {
  return { tools };
});

// Handle call_tool request
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

  try {
    if (!args) {
      throw new Error('Missing arguments');
    }

    switch (name) {
      case 'unity_localization_add_key':
        return await localizationManager.addKey(
          args.key as string,
          args.displayName as string,
          args.translations as Record<string, string>
        );

      case 'unity_localization_batch_add_keys':
        return await localizationManager.batchAddKeys(
          args.keys as Array<{
            key: string;
            displayName: string;
            translations: Record<string, string>;
          }>
        );

      case 'unity_localization_update_key':
        return await localizationManager.updateKey(
          args.key as string,
          args.translations as Record<string, string>
        );

      case 'unity_localization_validate':
        return await localizationManager.validate();

      case 'unity_localization_get_status':
        return await localizationManager.getStatus(args.verbose as boolean);

      case 'unity_localization_get_key_info':
        return await localizationManager.getKeyInfo(args.key as string);

      case 'unity_localization_remove_key':
        return await localizationManager.removeKey(args.key as string);

      case 'unity_localization_check_key_health':
        return await localizationManager.checkKeyHealth(args.key as string);

      case 'unity_localization_health_check':
        return await localizationManager.healthCheck();

      default:
        throw new Error(`Unknown tool: ${name}`);
    }
  } catch (error) {
    return {
      content: [
        {
          type: 'text',
          text: `Error: ${error instanceof Error ? error.message : String(error)}`,
        },
      ],
      isError: true,
    };
  }
});

// Start the server
async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error('Unity Localization MCP Server running on stdio');
}

main().catch((error) => {
  console.error('Fatal error:', error);
  process.exit(1);
});
