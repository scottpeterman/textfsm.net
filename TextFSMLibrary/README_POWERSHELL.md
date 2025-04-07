
# TextFSM for PowerShell - User Guide

## Introduction

TextFSM is a template-based text parsing engine that allows you to extract structured data from text outputs. This .NET implementation enables PowerShell users to parse command outputs, logs, and other text-based data into structured objects.

## Installation

1. Add the TextFSMLibrary.dll to your PowerShell project:
   - For PowerShell 5.1 and below: Use the .NET Framework 4.7.2 version
   - For PowerShell 7 and above: Use the .NET 8.0 version

2. Load the library in your PowerShell script:

```powershell
# Determine correct DLL version
$psVersion = $PSVersionTable.PSVersion.Major
$dllPath = if ($psVersion -ge 7) {
    "path\to\net8.0\TextFSMLibrary.dll"
} else {
    "path\to\net472\TextFSMLibrary.dll"
}

# Load the assembly
Add-Type -Path $dllPath
```

## Basic Usage

### 1. Create a TextFSM Template

TextFSM templates consist of two sections:
- **Value definitions**: Define the fields to extract
- **State definitions**: Define rules to match text patterns

Example template:

```
Value HOSTNAME (\S+)
Value OS_NAME (.+)
Value OS_VERSION (.+)

Start
  ^Host Name:\s+${HOSTNAME}
  ^OS Name:\s+${OS_NAME}
  ^OS Version:\s+${OS_VERSION}
```

**IMPORTANT**: When defining templates in PowerShell, use single quotes for here-strings to prevent variable expansion:

```powershell
$templateText = @'
Value HOSTNAME (\S+)
Value OS_NAME (.+)
Value OS_VERSION (.+)

Start
  ^Host Name:\s+${HOSTNAME}
  ^OS Name:\s+${OS_NAME}
  ^OS Version:\s+${OS_VERSION}
'@
```

### 2. Parse Text and Get Results

Use the PowerShellHelper class to parse text and get JSON results:

```powershell
# Parse text with template
$jsonString = [TextFSM.PowerShellHelper]::ParseTextToJson($templateText, $inputText, $true)

# Convert JSON to PowerShell objects
$results = $jsonString | ConvertFrom-Json

# Display results
$results | Format-Table
```

## Template Syntax

### Value Definitions

```
Value [Options] NAME (REGEX)
```

- `Options`: Optional modifiers like Required, Filldown, Key, List
- `NAME`: Name of the field (alphanumeric)
- `REGEX`: Regular expression pattern enclosed in parentheses

### State Definitions

```
StateName
  ^REGEX -> ACTION NewState
```

- `StateName`: Name of the state (Start is required)
- `REGEX`: Regular expression to match text lines
- `ACTION`: Optional action (Continue, Next, Error, Clear, Clearall, Record, NoRecord)
- `NewState`: Optional state to transition to

## Value Options

- **Required**: Value must be present or record is skipped
- **Filldown**: Value persists between records until changed
- **Fillup**: Value fills upward after being set
- **Key**: Creates composite keys to avoid duplicates
- **List**: Collects multiple matches into a list

## Complete Example

```powershell
# Load the TextFSM library
$dllPath = ".\TextFSMLibrary.dll"
Add-Type -Path $dllPath

# Read input text
$inputText = Get-Content -Path "systeminfo.txt" -Raw

# Define TextFSM template
$template = @'
Value HOSTNAME (\S+)
Value OS_NAME (.+)
Value OS_VERSION (.+)
Value OS_MANUFACTURER (.+)

Start
  ^Host Name:\s+${HOSTNAME}
  ^OS Name:\s+${OS_NAME}
  ^OS Version:\s+${OS_VERSION}
  ^OS Manufacturer:\s+${OS_MANUFACTURER}
'@

# Parse with TextFSM
$jsonString = [TextFSM.PowerShellHelper]::ParseTextToJson($template, $inputText, $true)

# Convert to PowerShell objects
$results = $jsonString | ConvertFrom-Json

# Display results
$results | Format-Table

# Export to CSV
$results | Export-Csv -Path "systeminfo_parsed.csv" -NoTypeInformation
```

## Advanced Usage

### Error Handling

Handle parsing errors by checking for Error property in the JSON output:

```powershell
$jsonString = [TextFSM.PowerShellHelper]::ParseTextToJson($template, $inputText, $true)

if ($jsonString -match '"Error":') {
    Write-Host "Error parsing with TextFSM:" -ForegroundColor Red
    $jsonString | ConvertFrom-Json | Select-Object Error, StackTrace
} else {
    # Process results normally
}
```

### Common TextFSM Template Patterns

#### Multi-line Records

```
Value INTERFACE (.+)
Value DESCRIPTION (.+)

Start
  ^interface ${INTERFACE} -> GetDescription

GetDescription
  ^\s+description ${DESCRIPTION} -> Record Start
  ^. -> Start
```

#### Handling Headers and Footers

```
Value ITEM (.+)
Value QUANTITY (\d+)

Start
  ^-+\s*-+ -> ItemList

ItemList
  ^${ITEM}\s+${QUANTITY} -> Record
  ^Total -> End
  ^-+\s*-+ -> End

End
```

## Troubleshooting

1. **Empty Results**:
   - Check if your regex patterns match the input text
   - Ensure template variable references use `${NAME}` syntax
   - Use single quotes for PowerShell here-strings to prevent variable expansion

2. **Parsing Errors**:
   - Verify your template syntax is correct
   - Check for proper state transitions
   - Examine error messages in the JSON response

3. **Line Ending Issues**:
   - Different platforms use different line endings (CRLF vs LF)
   - Use `Get-Content -Raw` to preserve original line endings

## Resources

For more information on TextFSM syntax and usage, refer to the original TextFSM documentation.
