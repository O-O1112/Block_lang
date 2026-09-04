using System;
using System.Text;

namespace BlockEngine
{
    internal static class SecurityLimits
    {
        internal const int MaxCapturedOutputChars = 1024 * 1024;
        internal const int MaxConcurrentRequests = 4;
        internal const int MaxImportFiles = 256;
        internal const long MaxImportedBytes = 32L * 1024L * 1024L;
        internal const long MaxScriptBytes = 32L * 1024L * 1024L;
        internal const long MaxJsonBytes = 4L * 1024L * 1024L;
        internal const int RequestReadTimeoutSeconds = 30;
        internal const int MinimumApiTokenLength = 32;
        internal const uint ChildProcessMemoryLimitBytes = 512u * 1024u * 1024u;
        internal const uint ChildJobMemoryLimitBytes = 1024u * 1024u * 1024u;

        internal static bool SecureEquals(string left, string right)
        {
            if (left == null || right == null) return false;
            int length = Math.Max(left.Length, right.Length);
            int difference = left.Length ^ right.Length;
            for (int i = 0; i < length; i++)
            {
                char leftChar = i < left.Length ? left[i] : '\0';
                char rightChar = i < right.Length ? right[i] : '\0';
                difference |= leftChar ^ rightChar;
            }
            return difference == 0;
        }

        internal static void AppendOutput(StringBuilder builder, string text)
        {
            if (builder == null || string.IsNullOrEmpty(text)) return;
            lock (builder)
            {
                if (builder.Length >= MaxCapturedOutputChars) return;
                int remaining = MaxCapturedOutputChars - builder.Length;
                if (text.Length <= remaining)
                {
                    builder.Append(text);
                    return;
                }

                builder.Append(text, 0, remaining);
                const string marker = "\n[output truncated]\n";
                if (builder.Length >= marker.Length)
                {
                    builder.Remove(builder.Length - marker.Length, marker.Length);
                }
                builder.Append(marker);
            }
        }
    }
}
