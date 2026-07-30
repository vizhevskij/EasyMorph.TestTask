namespace EasyMorph.TestTask
{
    public sealed class ProgressBar
    {
        private readonly int _total;
        private readonly int _stars;

        private int _current;
        private int _printed;

        public ProgressBar(int total, int stars = 50)
        {
            _total = total;
            _stars = stars;

            Console.Write('[');
        }

        public void Increment()
        {
            _current++;

            var shouldPrint = _current * _stars / _total;

            while (_printed < shouldPrint)
            {
                Console.Write('*');
                _printed++;
            }
        }

        public void Finish()
        {
            while (_printed < _stars)
            {
                Console.Write('*');
                _printed++;
            }

            Console.WriteLine("] Done");
        }
    }
}
