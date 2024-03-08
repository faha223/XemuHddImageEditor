using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace FatX.Net
{
    public class PathMatcher
    {
        private const string AlphaUpper = "A-Z";
        private const string AlphaLower = "a-z";
        private const string Digits = "0-9";
        private const string Whitespace = "\\s";
        private const string NonWordCharactersExceptPeriod = "!#\\$%&'\\(\\)\\-\\.@\\[\\]\\^_`\\{\\}\\~";
        private const string Period = "\\.";

        private static Regex driveLetterRegex = new Regex("^([A-Z])\\:", RegexOptions.Compiled);
        private static Regex matchEverythingRegex = new Regex(".*", RegexOptions.Compiled);

        private Regex _regex { get; init; }

        public string QueryString { get; init; }

        public PathMatcher(string queryString)
        {
            QueryString = queryString;
            _regex = BuildRegex(queryString);
        }

        public bool IsMatch(string fullname)
        {
            fullname = driveLetterRegex.Replace(fullname, $"{Path.DirectorySeparatorChar}$1");
            return _regex.IsMatch(fullname);
        }

        private static Regex BuildRegex(string query)
        {
            var path = query;
            if(path == "**" || path == "*")
                return matchEverythingRegex;
            
            var pathSeparator = Escape(Path.DirectorySeparatorChar);
            var validChars = AlphaUpper + AlphaLower + Digits + NonWordCharactersExceptPeriod + Whitespace;
            
            path = Escape(path)
                .Replace("**", $"[{validChars + Period + pathSeparator}]<0+>")
                .Replace("*\\.*", $"[{validChars}]+\\.[{validChars}]+")
                .Replace("*", $"[{validChars + Period}]+")
                .Replace("<0+>", "*");

            return new Regex(path, RegexOptions.Compiled);
        }

        public static string Escape(string str)
        {
            var sb = new StringBuilder();
            foreach(var c in str)
            {
                sb.Append(Escape(c));
            }
            return sb.ToString();
        }

        public static string Escape(char c)
        {
            switch(c)
            {
                case '[':
                case ']':
                case '(':
                case ')':
                case '{':
                case '}':
                case '\\':
                case '/':
                case '.':
                // case '*': Skip Asterisk, this is a wildcard character
                case '+':
                case '-':
                case ':':
                    return "\\" + c.ToString();
                default:
                    return c.ToString();
            }
        }
    }
}
