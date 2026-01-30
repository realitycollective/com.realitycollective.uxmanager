/**
 * Standalone YAML parser test script
 * Test Unity YAML parsing WITHOUT rebuilding the entire MCP server
 * 
 * Usage: node test-yaml-parser.js
 */

const fs = require('fs');
const path = require('path');
const yaml = require('js-yaml');

// Define Unity YAML schema with custom tag handlers
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

function testYAMLParser() {
  console.log('🧪 Testing Unity YAML Parser\n');
  
  const projectRoot = process.env.UNITY_PROJECT_ROOT || path.resolve(__dirname, '../../../../../');
  const profilePath = path.join(projectRoot, 'Assets/ServiceProvidersProfile/LocalizationServiceProfile.asset');
  
  console.log(`📁 Project Root: ${projectRoot}`);
  console.log(`📄 Profile Path: ${profilePath}\n`);
  
  try {
    // Check file exists
    if (!fs.existsSync(profilePath)) {
      console.error(`❌ File not found: ${profilePath}`);
      process.exit(1);
    }
    
    console.log('✅ File exists\n');
    
    // Read file
    const content = fs.readFileSync(profilePath, 'utf8');
    console.log(`✅ File read successfully (${content.length} bytes)\n`);
    
    // Show first 500 chars
    console.log('📝 File preview (first 500 chars):');
    console.log('─'.repeat(60));
    console.log(content.substring(0, 500));
    console.log('─'.repeat(60));
    console.log('');
    
    // Try parsing with DEFAULT schema (will fail)
    console.log('🔴 Attempting parse with DEFAULT schema (expected to fail)...');
    try {
      yaml.load(content);
      console.log('✅ Parsed successfully with DEFAULT schema');
    } catch (error) {
      console.log(`❌ Failed with DEFAULT schema: ${error.message}\n`);
    }
    
    // Try parsing with UNITY schema
    console.log('🟢 Attempting parse with UNITY schema...');
    try {
      const data = yaml.load(content, { schema: UNITY_SCHEMA });
      console.log('✅ Parsed successfully with UNITY schema\n');
      
      // Extract localization data
      if (data && data.MonoBehaviour) {
        const behavior = data.MonoBehaviour;
        console.log('📊 Extracted data:');
        console.log(`   - Default Locale: ${behavior.defaultLocale}`);
        console.log(`   - Catalog Path: ${behavior.catalogPath}`);
        console.log(`   - Supported Locales: ${behavior.supportedLocales?.join(', ')}`);
        console.log(`   - Localization Keys: ${behavior.localizationKeys?.length || 0}`);
        
        if (behavior.localizationKeys && behavior.localizationKeys.length > 0) {
          console.log('\n📋 Sample keys (first 5):');
          behavior.localizationKeys.slice(0, 5).forEach((key, i) => {
            console.log(`   ${i + 1}. ${key.key} → ${key.displayName}`);
          });
        }
        
        console.log('\n✅ All data extracted successfully');
        console.log('\n🎉 YAML parser test PASSED!');
        process.exit(0);
      } else {
        console.error('❌ Parsed but no MonoBehaviour data found');
        console.log('Data structure:', JSON.stringify(data, null, 2).substring(0, 500));
        process.exit(1);
      }
    } catch (error) {
      console.error(`❌ Failed with UNITY schema: ${error.message}`);
      console.error('Stack:', error.stack);
      process.exit(1);
    }
    
  } catch (error) {
    console.error(`❌ Unexpected error: ${error.message}`);
    console.error('Stack:', error.stack);
    process.exit(1);
  }
}

// Run the test
testYAMLParser();
