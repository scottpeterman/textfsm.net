# .NET Library with Tests Project Setup

This README explains how to set up a .NET library project with a separate test project using the .NET CLI and Visual Studio Code.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (version 8.0 or later)
- [Visual Studio Code](https://code.visualstudio.com/)
- [C# Extension for VS Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)

## Project Structure

The project follows this structure:

```
YourSolution/
├── YourLibrary/
│   ├── YourLibrary.csproj
│   └── YourClass.cs
├── YourLibrary.Tests/
│   ├── YourLibrary.Tests.csproj
│   └── Program.cs
└── YourSolution.sln
```

## Setting Up the Project

### 1. Create a New Solution

```bash
mkdir YourSolution
cd YourSolution
dotnet new sln -n YourSolution
```

### 2. Create the Library Project

```bash
# Create the class library project
dotnet new classlib -n YourLibrary

# Add it to the solution
dotnet sln add YourLibrary/YourLibrary.csproj
```

### 3. Create the Test Project

```bash
# Create a console application for testing
dotnet new console -n YourLibrary.Tests

# Add it to the solution
dotnet sln add YourLibrary.Tests/YourLibrary.Tests.csproj

# Add a reference to the library project
dotnet add YourLibrary.Tests/YourLibrary.Tests.csproj reference YourLibrary/YourLibrary.csproj
```

## Setting Up VS Code for Debugging

### 1. Open the Project in VS Code

```bash
code .
```

### 2. Create `.vscode` Directory

If it doesn't exist already, create a `.vscode` directory in the solution root:

```bash
mkdir -p .vscode
```

### 3. Create `launch.json` Configuration

Create a file `.vscode/launch.json` with the following content:

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET Core Launch (console)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/YourLibrary.Tests/bin/Debug/net8.0/YourLibrary.Tests.dll",
            "args": [],
            "cwd": "${workspaceFolder}/YourLibrary.Tests",
            "console": "internalConsole",
            "stopAtEntry": false
        }
    ]
}
```

Make sure to replace `net8.0` with the appropriate target framework if you're using a different version.

### 4. Create `tasks.json` Configuration

Create a file `.vscode/tasks.json` with the following content:

```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build",
            "command": "dotnet",
            "type": "process",
            "args": [
                "build",
                "${workspaceFolder}/YourSolution.sln",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "problemMatcher": "$msCompile"
        }
    ]
}
```

## Building and Running the Project

### Building the Solution

```bash
dotnet build
```

### Running the Tests

```bash
cd YourLibrary.Tests
dotnet run
```

## Debugging with VS Code

1. Open your code file in VS Code
2. Set breakpoints by clicking in the gutter (left margin) of the code editor
3. Press F5 to start debugging
4. Use VS Code's Debug toolbar to step through code, inspect variables, etc.

## Common Issues and Solutions

### DLL Not Found When Debugging

If you see an error like "program 'path/to/dll' does not exist" when trying to debug:

1. Check if your `launch.json` points to the correct target framework version (e.g., `net8.0` instead of `net6.0`)
2. Make sure you've built the project before debugging
3. Verify the path in `launch.json` matches your actual build output directory

### Intellisense Not Working

If code completion or other IntelliSense features aren't working:

1. Reload VS Code: `Ctrl+Shift+P` > "Developer: Reload Window"
2. Check if OmniSharp is running (should show in status bar)
3. Verify you have the C# extension installed

## Example Library Implementation

Here's a simple example of what your library class might look like:

```csharp
namespace YourLibrary
{
    public class Calculator
    {
        public int Add(int a, int b)
        {
            return a + b;
        }
        
        public int Subtract(int a, int b)
        {
            return a - b;
        }
    }
}
```

## Example Test Program

And here's an example test program:

```csharp
using YourLibrary;

class Program
{
    static void Main()
    {
        Console.WriteLine("Testing calculator implementation...");
        
        var calculator = new Calculator();
        
        // Test addition
        var sum = calculator.Add(5, 3);
        Console.WriteLine($"5 + 3 = {sum}");
        
        // Test subtraction
        var difference = calculator.Subtract(10, 4);
        Console.WriteLine($"10 - 4 = {difference}");
        
        Console.WriteLine("Tests completed successfully!");
    }
}
```

## Next Steps

- Consider adding a proper testing framework like xUnit, NUnit, or MSTest
- Set up continuous integration with GitHub Actions or Azure DevOps
- Add code coverage reporting