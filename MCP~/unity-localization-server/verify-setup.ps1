# Unity Localization MCP Server - Quick Setup Verification

Write-Host "🔍 Unity Localization MCP Server - Setup Verification" -ForegroundColor Cyan
Write-Host ""

# Check if server files exist
$serverPath = "E:\ED\01-Yperea\Yperea-Design\YpereaDesign.Unity\Packages\com.realitycollective.uxmanager\MCP\unity-localization-server"
$distPath = Join-Path $serverPath "dist\index.js"

if (Test-Path $distPath) {
    Write-Host "✅ Server compiled successfully" -ForegroundColor Green
    Write-Host "   Location: $distPath" -ForegroundColor Gray
} else {
    Write-Host "❌ Server not compiled" -ForegroundColor Red
    Write-Host "   Run: cd '$serverPath' && npm run build" -ForegroundColor Yellow
    exit 1
}

# Check if MCP settings exist
$mcpSettingsPath = "$env:APPDATA\Code\User\globalStorage\saoudrizwan.claude-dev\settings\cline_mcp_settings.json"

if (Test-Path $mcpSettingsPath) {
    Write-Host "✅ MCP settings file exists" -ForegroundColor Green
    Write-Host "   Location: $mcpSettingsPath" -ForegroundColor Gray
    
    $settings = Get-Content $mcpSettingsPath -Raw | ConvertFrom-Json
    if ($settings.mcpServers.'unity-localization') {
        Write-Host "✅ unity-localization server registered" -ForegroundColor Green
    } else {
        Write-Host "❌ unity-localization server not registered" -ForegroundColor Red
    }
} else {
    Write-Host "❌ MCP settings file not found" -ForegroundColor Red
}

Write-Host ""
Write-Host "📝 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Restart VS Code to load the MCP server"
Write-Host "2. Test the server using the available tools:"
Write-Host "   - validate_localization"
Write-Host "   - get_localization_status"
Write-Host "   - add_localization_key"
Write-Host ""
Write-Host "📚 Documentation: Packages/com.realitycollective.uxmanager/MCP/unity-localization-server/README.md"
