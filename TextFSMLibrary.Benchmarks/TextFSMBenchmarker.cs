using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TextFSM;

namespace TextFSMLibrary.Benchmarks
{
    public class TextFSMBenchmarker
    {
        // Configuration
        private readonly string _templateDir;
        private readonly string _testDir;
        private readonly string _vendor;
        private readonly string _templatePrefix;
        private readonly int _maxWorkers;
        private readonly bool _parallel;

        // Stats collection
        private class Stats
        {
            public int Total { get; set; }
            public int Success { get; set; }
            public int Failed { get; set; }
            public int Skipped { get; set; }
            public List<TemplateResult> Times { get; } = new List<TemplateResult>();
            public List<TemplateError> Errors { get; } = new List<TemplateError>();
        }

        private class TemplateResult
        {
            public string Template { get; set; }
            public string Command { get; set; }
            public double Time { get; set; }  // in milliseconds
            public int Records { get; set; }
        }

        private class TemplateError
        {
            public string Template { get; set; }
            public string Command { get; set; }
            public string Error { get; set; }
            public string StackTrace { get; set; }
        }

        private readonly Stats _stats = new Stats();

        public TextFSMBenchmarker(
            string templateDir,
            string testDir,
            string vendor = "cisco_ios",
            string templatePrefix = "cisco_ios",
            int maxWorkers = 4,
            bool parallel = true)
        {
            _templateDir = templateDir;
            _testDir = testDir;
            _vendor = vendor;
            _templatePrefix = templatePrefix;
            _maxWorkers = maxWorkers;
            _parallel = parallel;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("=== C# TextFSM Batch Testing ===");

            // Find templates
            var templates = FindTemplates();
            if (templates.Count == 0)
            {
                Console.WriteLine("No templates found. Exiting.");
                return;
            }

            // Process templates
            if (_parallel && _maxWorkers > 1)
            {
                // Parallel processing with TaskFactory
                var tasks = new List<Task>();
                var semaphore = new System.Threading.SemaphoreSlim(_maxWorkers);

                foreach (var template in templates)
                {
                    await semaphore.WaitAsync();
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            ProcessTemplate(template);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                await Task.WhenAll(tasks);
            }
            else
            {
                // Sequential processing
                foreach (var template in templates)
                {
                    ProcessTemplate(template);
                }
            }

            // Print summary
            PrintSummary();
        }

        private List<string> FindTemplates()
        {
            var pattern = $"{_templatePrefix}*.textfsm";
            var templates = Directory.GetFiles(_templateDir, pattern)
                .Select(Path.GetFileName)
                .ToList();

            Console.WriteLine($"Found {templates.Count} {_templatePrefix} templates");
            return templates;
        }

        private string FindRawFile(string commandName)
        {
            // Check in the most likely location first
            var dirPath = Path.Combine(_testDir, _vendor, commandName);
            if (Directory.Exists(dirPath))
            {
                // Get all files with .raw extension in the directory
                var rawFiles = Directory.GetFiles(dirPath, "*.raw");
                if (rawFiles.Length > 0)
                {
                    return rawFiles[0];
                }
            }

            // If the directory doesn't exist or has no .raw files, search more broadly
            var vendorDir = Path.Combine(_testDir, _vendor);
            try
            {
                var directories = Directory.GetDirectories(vendorDir)
                    .Select(d => new DirectoryInfo(d).Name);

                // Look for a directory that might match
                var possibleDirs = directories
                    .Where(d => d.Contains(commandName) || commandName.Contains(d))
                    .ToList();

                foreach (var directory in possibleDirs)
                {
                    dirPath = Path.Combine(vendorDir, directory);
                    var rawFiles = Directory.GetFiles(dirPath, "*.raw");
                    if (rawFiles.Length > 0)
                    {
                        return rawFiles[0];
                    }
                }
            }
            catch (Exception)
            {
                // Ignore directory search errors
            }

            return null;
        }

        private void ProcessTemplate(string templateFile)
        {
            lock (_stats)
            {
                _stats.Total++;
            }

            var templatePath = Path.Combine(_templateDir, templateFile);
            var commandName = Path.GetFileNameWithoutExtension(templateFile)
                .Replace(".textfsm", "")
                .Replace($"{_templatePrefix}_", "");

            Console.WriteLine($"\nTesting template: {templateFile}");

            try
            {
                // Find the raw file
                var rawFile = FindRawFile(commandName);
                if (string.IsNullOrEmpty(rawFile))
                {
                    Console.WriteLine($"[SKIP] {commandName}: No matching raw file found");
                    lock (_stats)
                    {
                        _stats.Skipped++;
                    }
                    return;
                }

                // Load template and raw data
                var templateContent = File.ReadAllText(templatePath);
                var rawContent = File.ReadAllText(rawFile);

                // Parse with TextFSM
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    // Create template and parse
                    var fsm = new TextFSM.TextFSM(templateContent);
                    var result = fsm.ParseText(rawContent);

                    // Calculate execution time
                    stopwatch.Stop();
                    var executionTimeMs = stopwatch.Elapsed.TotalMilliseconds;

                    // Record success
                    Console.WriteLine($"[PASS] {commandName}: Parsed {result.Count} records in {executionTimeMs:F2}ms");
                    lock (_stats)
                    {
                        _stats.Success++;
                        _stats.Times.Add(new TemplateResult
                        {
                            Template = templateFile,
                            Command = commandName,
                            Time = executionTimeMs,
                            Records = result.Count
                        });
                    }
                }
                catch (Exception e)
                {
                    // Record failure
                    var errorMsg = e.Message;
                    var stackTrace = e.StackTrace;
                    Console.WriteLine($"[FAIL] {commandName}: {errorMsg}");
                    lock (_stats)
                    {
                        _stats.Failed++;
                        _stats.Errors.Add(new TemplateError
                        {
                            Template = templateFile,
                            Command = commandName,
                            Error = errorMsg,
                            StackTrace = stackTrace
                        });
                    }
                }
            }
            catch (Exception e)
            {
                // File read error
                Console.WriteLine($"[ERROR] {commandName}: {e.Message}");
                lock (_stats)
                {
                    _stats.Failed++;
                    _stats.Errors.Add(new TemplateError
                    {
                        Template = templateFile,
                        Command = commandName,
                        Error = e.Message
                    });
                }
            }
        }

        private void PrintSummary()
        {
            Console.WriteLine(new string('=', 50));
            Console.WriteLine("C# TextFSM Testing Summary");
            Console.WriteLine(new string('=', 50));

            Console.WriteLine($"\nTotal templates tested: {_stats.Total}");
            double successRate = _stats.Total > 0 ? (_stats.Success / (double)_stats.Total) * 100 : 0;
            double failRate = _stats.Total > 0 ? (_stats.Failed / (double)_stats.Total) * 100 : 0;
            double skipRate = _stats.Total > 0 ? (_stats.Skipped / (double)_stats.Total) * 100 : 0;

            Console.WriteLine($"Successful: {_stats.Success} ({successRate:F1}%)");
            Console.WriteLine($"Failed: {_stats.Failed} ({failRate:F1}%)");
            Console.WriteLine($"Skipped: {_stats.Skipped} ({skipRate:F1}%)");

            if (_stats.Times.Count > 0)
            {
                // Calculate average time
                double totalTime = _stats.Times.Sum(item => item.Time);
                double avgTime = _stats.Times.Count > 0 ? totalTime / _stats.Times.Count : 0;

                Console.WriteLine($"\nAverage parse time: {avgTime:F2}ms");

                // Sort by time and get fastest/slowest
                var sortedTimes = _stats.Times.OrderBy(x => x.Time).ToList();

                Console.WriteLine("\nFastest templates:");
                foreach (var item in sortedTimes.Take(5))
                {
                    Console.WriteLine($"  {item.Command}: {item.Time:F2}ms ({item.Records} records)");
                }

                Console.WriteLine("\nSlowest templates:");
                foreach (var item in sortedTimes.Skip(Math.Max(0, sortedTimes.Count - 5)).Reverse())
                {
                    Console.WriteLine($"  {item.Command}: {item.Time:F2}ms ({item.Records} records)");
                }
            }

            if (_stats.Errors.Count > 0)
            {
                Console.WriteLine("\nFailed templates:");
                foreach (var item in _stats.Errors)
                {
                    Console.WriteLine($"  {item.Command}: {item.Error}");
                }
            }

            // Print overall assessment
            Console.WriteLine('\n' + new string('=', 50));
            if (successRate == 100)
            {
                Console.WriteLine("✅ All templates passed!");
            }
            else if (successRate >= 90)
            {
                Console.WriteLine("🟢 Most templates are working correctly.");
            }
            else if (successRate >= 75)
            {
                Console.WriteLine("🟡 The majority of templates are working, but some need attention.");
            }
            else
            {
                Console.WriteLine("🔴 Several templates need attention.");
            }
            Console.WriteLine(new string('=', 50));
        }
    }
}