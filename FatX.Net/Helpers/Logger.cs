namespace FatX.Net.Helpers
{
    internal static class Logger
    {
        public static void Debug(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] {message}");
        }

        public static void Error(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] {message}");
        }

        public static void Warning(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[WARNING] {message}");
        }

        public static void Verbose(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[VERBOSE] {message}");
        }

        public static void Information(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[INFO] {message}");
        }
    }

}
