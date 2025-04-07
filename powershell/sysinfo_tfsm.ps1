# Parse-SystemInfo.ps1
# Parse Windows system information using TextFSM

# Check PowerShell version and select appropriate DLL
$psVersion = $PSVersionTable.PSVersion.Major
$dllPath = ""

if ($psVersion -ge 7) {
    # PowerShell 7+ uses .NET Core/.NET 5+
    $dllPath = "..\TextFSMLibrary\bin\Debug\net8.0\TextFSMLibrary.dll"
    Write-Host "Using .NET 8.0 DLL for PowerShell $psVersion" -ForegroundColor Yellow
} else {
    # PowerShell 5.1 and below uses .NET Framework
    $dllPath = "..\TextFSMLibrary\bin\Debug\net472\TextFSMLibrary.dll"
    Write-Host "Using .NET Framework 4.7.2 DLL for PowerShell $psVersion" -ForegroundColor Yellow
}

# Load the TextFSM assembly
try {
    Add-Type -Path $dllPath
    Write-Host "TextFSM library loaded successfully" -ForegroundColor Green
}
catch {
    Write-Host "Error loading TextFSM library: $_" -ForegroundColor Red
    exit
}

# Read raw system info from file
$sysInfoPath = "raw_systeminfo.txt"
if (-not (Test-Path $sysInfoPath)) {
    Write-Host "System info file not found at: $sysInfoPath" -ForegroundColor Red
    Write-Host "Running systeminfo command to collect data..." -ForegroundColor Yellow
    systeminfo | Out-File -FilePath $sysInfoPath
    Write-Host "Created system info file." -ForegroundColor Green
}

$sysInfoOutput = Get-Content -Path $sysInfoPath -Raw

# Debug: Show the first few lines of system info
Write-Host "DEBUG: First 300 characters of system info:" -ForegroundColor Cyan
Write-Host ($sysInfoOutput.Substring(0, [Math]::Min(300, $sysInfoOutput.Length))) -ForegroundColor Gray

# Define TextFSM template for Windows systeminfo output
# IMPORTANT: Using single quotes for the here-string to prevent PowerShell variable expansion
$templateText = @'
Value HOSTNAME (\S+)
Value OS_NAME (.+)
Value OS_VERSION (.+)
Value OS_MANUFACTURER (.+)
Value SYSTEM_BOOT_TIME (.+)
Value SYSTEM_MANUFACTURER (.+)
Value SYSTEM_MODEL (.+)
Value PROCESSOR_INFO (.+)

Start
  ^Host Name:\s+${HOSTNAME}
  ^OS Name:\s+${OS_NAME}
  ^OS Version:\s+${OS_VERSION}
  ^OS Manufacturer:\s+${OS_MANUFACTURER}
  ^System Boot Time:\s+${SYSTEM_BOOT_TIME}
  ^System Manufacturer:\s+${SYSTEM_MANUFACTURER}
  ^System Model:\s+${SYSTEM_MODEL}
  ^Processor\(s\):\s+${PROCESSOR_INFO}
'@

# Save the template to a file
$templateText | Out-File -FilePath "systeminfo.textfsm"
Write-Host "Created TextFSM template file: systeminfo.textfsm" -ForegroundColor Green

# Debug: Show the template
Write-Host "DEBUG: TextFSM Template:" -ForegroundColor Cyan
Write-Host $templateText -ForegroundColor Gray

# Parse with TextFSM
try {
    # Get JSON directly from helper (with pretty printing)
    Write-Host "DEBUG: Calling ParseTextToJson..." -ForegroundColor Cyan
    $jsonString = [TextFSM.PowerShellHelper]::ParseTextToJson($templateText, $sysInfoOutput, $true)
    
    # Debug: Output JSON string length
    Write-Host "DEBUG: JSON string length: $($jsonString.Length)" -ForegroundColor Cyan
    
    # Check for errors
    if ($jsonString -match '"Error":') {
        Write-Host "DEBUG: Error found in JSON response" -ForegroundColor Red
        Write-Host $jsonString -ForegroundColor Red
        throw "Failed to parse: $jsonString"
    }
    
    # Save raw JSON output
    $jsonString | Out-File -FilePath "systeminfo_parsed.json"
    Write-Host "Results saved to systeminfo_parsed.json" -ForegroundColor Green
    
    # Convert to PowerShell objects for display
    $results = $jsonString | ConvertFrom-Json
    
    # Debug: Show count of results
    Write-Host "DEBUG: Number of results: $($results.Count)" -ForegroundColor Cyan
    
    # Display results
    Write-Host "`nParsed System Information:" -ForegroundColor Cyan
    $results | Format-Table -AutoSize
}
catch {
    Write-Host "Error parsing with TextFSM: $_" -ForegroundColor Red
    exit 1
}