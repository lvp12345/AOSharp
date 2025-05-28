using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp
{
    internal class BinaryPatternReplacer
    {
        private readonly byte[] findPattern;
        private readonly bool[] findWildcards;
        private readonly byte[] replacePattern;
        private readonly bool[] replaceWildcards;
        private readonly int patternLength;

        public BinaryPatternReplacer(string findPatternHex, string replacePatternHex)
        {
            (findPattern, findWildcards) = ParsePattern(findPatternHex);
            (replacePattern, replaceWildcards) = ParsePattern(replacePatternHex);

            if (findPattern.Length != replacePattern.Length)
            {
                throw new ArgumentException("Find and replace patterns must have the same length");
            }

            patternLength = findPattern.Length;
        }

        private (byte[] pattern, bool[] wildcards) ParsePattern(string hexPattern)
        {
            var parts = hexPattern.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var pattern = new byte[parts.Length];
            var wildcards = new bool[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "??")
                {
                    wildcards[i] = true;
                    pattern[i] = 0; // Value doesn't matter for wildcards
                }
                else
                {
                    pattern[i] = Convert.ToByte(parts[i], 16);
                    wildcards[i] = false;
                }
            }

            return (pattern, wildcards);
        }

        public List<long> FindAndReplace(string filePath, bool performReplace = true)
        {
            var matches = new List<long>();
            const int bufferSize = 4096;

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                byte[] buffer = new byte[bufferSize + patternLength - 1];
                int bytesRead;
                long currentPosition = 0;
                int overlap = 0;

                while ((bytesRead = stream.Read(buffer, overlap, bufferSize)) > 0)
                {
                    int totalBytes = bytesRead + overlap;

                    // Search for pattern in current buffer
                    for (int i = 0; i <= totalBytes - patternLength; i++)
                    {
                        if (IsMatch(buffer, i))
                        {
                            long matchPosition = currentPosition + i;
                            matches.Add(matchPosition);

                            if (performReplace)
                            {
                                // Replace the pattern
                                ReplacePattern(stream, buffer, i, matchPosition);
                            }

                            // Skip past this match to avoid overlapping matches
                            i += patternLength - 1;
                        }
                    }

                    // Handle potential pattern spanning buffers
                    if (totalBytes >= patternLength)
                    {
                        // Copy last (patternLength - 1) bytes to beginning of buffer
                        Array.Copy(buffer, totalBytes - (patternLength - 1), buffer, 0, patternLength - 1);
                        overlap = patternLength - 1;
                        currentPosition += totalBytes - overlap;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return matches;
        }

        private bool IsMatch(byte[] buffer, int offset)
        {
            for (int i = 0; i < patternLength; i++)
            {
                if (!findWildcards[i] && buffer[offset + i] != findPattern[i])
                {
                    return false;
                }
            }
            return true;
        }

        private void ReplacePattern(FileStream stream, byte[] buffer, int bufferOffset, long filePosition)
        {
            // Prepare replacement bytes
            byte[] replacementBytes = new byte[patternLength];

            for (int i = 0; i < patternLength; i++)
            {
                if (replaceWildcards[i])
                {
                    // Keep original byte
                    replacementBytes[i] = buffer[bufferOffset + i];
                }
                else
                {
                    // Use replacement byte
                    replacementBytes[i] = replacePattern[i];
                }
            }

            // Save current position
            long savedPosition = stream.Position;

            // Seek to match position and write replacement
            stream.Seek(filePosition, SeekOrigin.Begin);
            stream.Write(replacementBytes, 0, patternLength);

            // Restore position
            stream.Seek(savedPosition, SeekOrigin.Begin);
        }
    }

}
