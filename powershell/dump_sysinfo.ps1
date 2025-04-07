# Output-SystemInfo.ps1
# Outputs Windows system information for TextFSM template development

# Get system info using PowerShell
Write-Host "Collecting system information..." -ForegroundColor Yellow
$sysInfoOutput = systeminfo | Out-String

# Save the raw output to a file
$sysInfoOutput | Out-File -FilePath "raw_systeminfo.txt"

Write-Host "System information saved to raw_systeminfo.txt" -ForegroundColor Green

# Display the first few lines as a preview
Write-Host "`nPreview of system information:" -ForegroundColor Cyan
$sysInfoOutput.Split("`n") | Select-Object -First 20