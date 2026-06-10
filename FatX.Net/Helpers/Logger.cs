namespace FatX.Net.Helpers
{
    public static class Logger
    {
        private static void WriteToLog(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        public static void Debug(string message)
        {
            WriteToLog($"[DEBUG] {message}");
        }

        public static void Error(string message)
        {
            WriteToLog($"[ERROR] {message}");
        }

        public static void Warning(string message)
        {
            WriteToLog($"[WARNING] {message}");
        }

        public static void Verbose(string message)
        {
            WriteToLog($"[VERBOSE] {message}");
        }

        public static void Information(string message)
        {
            WriteToLog($"[INFO] {message}");
        }
    }

}
