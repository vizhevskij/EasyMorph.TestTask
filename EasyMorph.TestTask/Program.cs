namespace EasyMorph.TestTask
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var outputCode = 0;
            if (args.Length == 0)
            {
                Console.WriteLine(Help);
                outputCode = 1;
            }
            else
            {
                switch (args[0].ToLowerInvariant())
                {
                    case "generate":
                        outputCode = Generate(args);
                        break;
                    case "parse":
                        outputCode = Parse(args);
                        break;
                    case "help":
                    case "--help":
                        Console.WriteLine(Help);
                        break;
                    default:
                        Console.WriteLine(Help);
                        break;
                }
            }
            ReportMemory();
            return outputCode;
        }

        private static int Generate(string[] args)
        {
            var workDir = DateTime.Now.ToString("yyyy_MM_dd__HH_mm");
            var stores = 50;
            var targetSize = 10_000;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--help":
                    case "-h":
                        Console.WriteLine(GenerateHelp);
                        return 0;
                    case "--work-dir":
                        workDir = args[++i];
                        break;
                    case "--stores":
                        stores = int.Parse(args[++i]);
                        if (stores < 1 || stores > 100)
                        {
                            Console.WriteLine("Stores must be between 1 and 100.");
                            return 2;
                        }
                        break;
                    case "--target-size":
                        targetSize = int.Parse(args[++i]);
                        if (targetSize < 100 || targetSize > 10_000_000)
                        {
                            Console.WriteLine("Target size must be between 100 and 10000000 KB.");
                            return 3;
                        }
                        break;
                }
            }

            var generator = new Generator();
            generator.Run(workDir, stores, targetSize);
            return 0;
        }

        private static int Parse(string[] args)
        {
            if (args.Contains("--help"))
            {
                Console.WriteLine(ParseHelp);
                return 0;
            }

            string workDir = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--help":
                    case "-h":
                        Console.WriteLine(ParseHelp);
                        return 0;
                    case "--work-dir":
                        workDir = args[++i];
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(workDir))
            {
                Console.WriteLine("--work-dir is required.");
                return 4;
            }
            if (!Directory.Exists(workDir))
            {
                Console.WriteLine($"Directory specified by --work-dir does not exist: '{workDir}'.");
                return 5;
            }

            var parser = new Parser(workDir);
            parser.Run();
            return 0;
        }

        private static void ReportMemory()
        {
            using var p = System.Diagnostics.Process.GetCurrentProcess();
            Console.WriteLine($"Peak working set: {p.PeakWorkingSet64 / 1024 / 1024} MB");
            Console.WriteLine($"Total allocated: {GC.GetTotalAllocatedBytes() / 1024 / 1024} MB");
        }

        #region Help constans
        const string Help = """
Sales Data Generator

Usage:
    EasyMorph.TestTask <command> [options]

Commands:
    generate    Generate test input data.
    parse       Parse generated data and print summary.

Run "EasyMorph.TestTask <command> --help" for command-specific help.

Examples:
    EasyMorph.TestTask generate
    EasyMorph.TestTask generate --stores 50 --size 10000
    EasyMorph.TestTask generate --work-dir D:\TestData

    EasyMorph.TestTask parse --work-dir D:\TestData
""";

        const string GenerateHelp = """
Usage:
    EasyMorph.TestTask generate [options]

Options:

    --work-dir <directory>
        Output directory.
        Default: yyyy_MM_dd__HH_mm

    --stores <count>
        Number of stores.
        Range: 1..100
        Default: 50

    --size <kilobytes>
        Approximate total size of generated XML files.
        Range: 100..10000000 KB
        Default: 10000 KB

    -h, --help
        Show this help.
""";

        const string ParseHelp = """
   Usage:
    SalesGenerator parse [options]

Options:

    --work-dir <directory>
        Working directory containing generated XML files.
        Required.

    -h, --help
        Show this help.               
""";
        #endregion
    }
}