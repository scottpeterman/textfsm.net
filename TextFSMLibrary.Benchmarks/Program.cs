using System;
using System.IO;
using System.Threading.Tasks;

namespace TextFSMLibrary.Benchmarks
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("TextFSM Benchmarking Tool");
            Console.WriteLine("========================");

            string solutionPath = Directory.GetCurrentDirectory();
            
            // Find the solution directory
            while (!File.Exists(Path.Combine(solutionPath, "TextFSMSolution.sln")) && 
                   Directory.GetParent(solutionPath) != null)
            {
                solutionPath = Directory.GetParent(solutionPath).FullName;
            }

            // Default paths relative to solution directory
            string templateDir = Path.Combine(solutionPath, "templates");
            string testDir = Path.Combine(solutionPath, "tests");
            string vendor = "cisco_ios";
            string templatePrefix = "cisco_ios";
            int maxWorkers = Environment.ProcessorCount;
            bool parallel = true;

            // Parse command line arguments
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--templates" && i + 1 < args.Length)
                {
                    templateDir = args[++i];
                }
                else if (args[i] == "--tests" && i + 1 < args.Length)
                {
                    testDir = args[++i];
                }
                else if (args[i] == "--vendor" && i + 1 < args.Length)
                {
                    vendor = args[++i];
                }
                else if (args[i] == "--prefix" && i + 1 < args.Length)
                {
                    templatePrefix = args[++i];
                }
                else if (args[i] == "--workers" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out int workers))
                    {
                        maxWorkers = workers;
                    }
                }
                else if (args[i] == "--sequential")
                {
                    parallel = false;
                }
                else if (args[i] == "--help")
                {
                    ShowHelp();
                    return;
                }
            }

            // Validate directories
            if (!Directory.Exists(templateDir))
            {
                Console.WriteLine($"Template directory not found: {templateDir}");
                Console.WriteLine("Use --templates to specify the correct directory.");
                return;
            }

            if (!Directory.Exists(testDir))
            {
                Console.WriteLine($"Test directory not found: {testDir}");
                Console.WriteLine("Use --tests to specify the correct directory.");
                return;
            }

            Console.WriteLine($"Template directory: {templateDir}");
            Console.WriteLine($"Test directory: {testDir}");
            Console.WriteLine($"Vendor: {vendor}");
            Console.WriteLine($"Template prefix: {templatePrefix}");
            Console.WriteLine($"Processing mode: {(parallel ? "Parallel" : "Sequential")}");
            if (parallel)
            {
                Console.WriteLine($"Max workers: {maxWorkers}");
            }
            Console.WriteLine();

            // Run the benchmarker
            var benchmarker = new TextFSMBenchmarker(
                templateDir,
                testDir,
                vendor,
                templatePrefix,
                maxWorkers,
                parallel);

            await benchmarker.RunAsync();

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void ShowHelp()
        {
            Console.WriteLine("TextFSM Benchmarking Tool - Command Line Options");
            Console.WriteLine("==============================================");
            Console.WriteLine("--templates <path>  : Path to template directory");
            Console.WriteLine("--tests <path>      : Path to test data directory");
            Console.WriteLine("--vendor <name>     : Vendor name (default: cisco_ios)");
            Console.WriteLine("--prefix <prefix>   : Template file prefix (default: cisco_ios)");
            Console.WriteLine("--workers <number>  : Number of parallel workers (default: number of CPU cores)");
            Console.WriteLine("--sequential        : Run tests sequentially instead of in parallel");
            Console.WriteLine("--help              : Show this help message");
        }
    }
}