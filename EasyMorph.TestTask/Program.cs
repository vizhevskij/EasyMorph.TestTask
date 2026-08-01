namespace EasyMorph.TestTask
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var outputCode = new CommandLineApplication().Run(args);
            ReportMemory();
            return outputCode;
        }       

        private static void ReportMemory()
        {
            using var p = System.Diagnostics.Process.GetCurrentProcess();
            Console.WriteLine($"Peak working set: {p.PeakWorkingSet64 / 1024 / 1024} MB");
            Console.WriteLine($"Total allocated: {GC.GetTotalAllocatedBytes() / 1024 / 1024} MB");
        }       
    }
}