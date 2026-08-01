namespace EasyMorph.TestTask.Common
{
    /// <summary>
    /// Displays a simple console progress bar.
    /// </summary>
    public class ProgressBar: IDisposable
    {
        private const int Stars = 50;

        private readonly int _total;        

        private int _current;
        private int _printed;

        public ProgressBar(int total, string prompt)
        {
            _total = total;
            Console.Write($"{prompt} [");
        }

        public void Increment()
        {
            _current++;
            var shouldPrint = _current * Stars / _total;
            while (_printed < shouldPrint)
            {
                Console.Write('*');
                _printed++;
            }
        }

        public void Dispose()
        {
            while (_printed < Stars)
            {
                Console.Write('*');
                _printed++;
            }
            Console.WriteLine("] Done");
        }
    }
}