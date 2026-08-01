using System.Text;

namespace EasyMorph.TestTask.Common
{
    /// <summary>
    /// Writes parsing errors to <c>errors.txt</c>
    /// <para>If the log file cannot be created or written, errors are written to the console instead.</para>
    /// </summary>
    public class Logger : IDisposable
    {
        private StreamWriter _errorWriter;
        private string _errorFile;

        public Logger(string workDir)
        {
            _errorFile = Path.Combine(workDir, "errors.txt");
            try
            {
                File.Delete(_errorFile);
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }
        }

        public void LogError(string message)
        {
            try
            {
                _errorWriter ??= new StreamWriter(_errorFile, false, Encoding.UTF8);
                _errorWriter.WriteLine(message);
            }
            catch (IOException)
            {
                Console.WriteLine(message);
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine(message);
            }
        }

        public void Dispose()
        {
            _errorWriter?.Dispose();
            _errorWriter = null;
        }
    }
}