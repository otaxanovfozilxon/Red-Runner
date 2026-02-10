using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Luxodd.Game.Scripts.HelpersAndUtils
{
    public static class StringExtensions
    {
        public static string ToPascalCaseStyle(this string str)
        {
            // Avoid LINQ .Select() with Thread.CurrentThread.CurrentCulture — problematic in IL2CPP WebGL
            var parts = str.Split('_');
            var sb = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (string.IsNullOrEmpty(part)) continue;
                sb.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                    sb.Append(part, 1, part.Length - 1);
            }
            return sb.ToString();
        }


        private static readonly Regex LowerUpperBoundary =
            new Regex(@"(?<=[\p{Ll}\p{Nd}])(?=\p{Lu})", RegexOptions.Compiled);

        private static readonly Regex WordMatcher =
            new Regex(@"[\p{L}\p{Nd}]+", RegexOptions.Compiled);

        public static string ToPascalCase(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;


            var normalized = LowerUpperBoundary.Replace(value.Trim(), " ");
            // Avoid .Cast<Match>().Select() — LINQ generic state machines crash IL2CPP WebGL
            var matches = WordMatcher.Matches(normalized);

            var sb = new StringBuilder();
            for (int i = 0; i < matches.Count; i++)
            {
                var part = matches[i].Value;
                var lower = part.ToLowerInvariant();
                sb.Append(char.ToUpperInvariant(lower[0]));
                if (lower.Length > 1)
                    sb.Append(lower, 1, lower.Length - 1);
            }

            return sb.ToString();
        }
    }
}
