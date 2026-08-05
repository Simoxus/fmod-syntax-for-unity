using System;
using System.IO;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    public static class StringExtensions
    {
        private const string HungarianPrefix = "m_";
        private const char Underscore = '_';
        public const char DefaultSeparator = ' ';

        private static readonly char[] DirectorySeparators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        private static bool IsExcludedSymbol(char symbol, char wordSeparator = DefaultSeparator)
        {
            return char.IsWhiteSpace(symbol) || char.IsPunctuation(symbol) || symbol == wordSeparator;
        }

        public static string ToCamelCasing(this string text, char wordSeparator = DefaultSeparator)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Split the text up into separate words first then fix spaces and change capitalization.
            text = text.ToHumanReadable(wordSeparator);

            System.Text.StringBuilder result = new System.Text.StringBuilder(text.Length);
            System.Text.StringBuilder currentWord = new System.Text.StringBuilder();
            bool isFirstWord = true;

            for (int i = 0; i <= text.Length; i++)
            {
                bool atEnd = i == text.Length;
                char c = atEnd ? '\0' : text[i];
                bool isSeparator = !atEnd && (char.IsWhiteSpace(c) || c == wordSeparator);

                if (atEnd || isSeparator)
                {
                    if (currentWord.Length > 0)
                    {
                        result.Append(FormatWord(currentWord.ToString(), isFirstWord));
                        isFirstWord = false;
                        currentWord.Clear();
                    }

                    // Non-whitespace separators (e.g. a literal underscore passed in as wordSeparator) are
                    // preserved in the output, matching the previous behavior.
                    if (isSeparator && !char.IsWhiteSpace(c))
                        result.Append(c);

                    continue;
                }

                currentWord.Append(c);
            }

            return result.ToString();
        }

        /// <summary>
        /// Formats a single word for camel/Pascal casing. Acronym-like words (e.g. "UI", "SCPs" — a run of
        /// two or more uppercase letters, optionally followed by a trailing lowercase "s" for a plural) are
        /// preserved exactly as-is instead of being lowercased, so folder/class names like "UI" and "SCPs"
        /// don't get mangled into "Ui" and "ScPs". Non-acronym words get standard title-casing, except the
        /// very first word is lowercased entirely to preserve existing camelCase behavior.
        /// </summary>
        private static string FormatWord(string word, bool isFirstWord)
        {
            if (IsAcronym(word))
                return word;

            char firstChar = isFirstWord ? char.ToLower(word[0]) : char.ToUpper(word[0]);
            return word.Length == 1 ? firstChar.ToString() : firstChar + word.Substring(1).ToLower();
        }

        /// <summary>
        /// Returns true if the word is a run of 2+ uppercase letters, optionally with one trailing
        /// lowercase "s" (to allow plural acronyms like "SCPs").
        /// </summary>
        private static bool IsAcronym(string word)
        {
            if (string.IsNullOrEmpty(word))
                return false;

            int upperCount = 0;
            for (int i = 0; i < word.Length; i++)
            {
                char c = word[i];
                bool isLastChar = i == word.Length - 1;

                if (isLastChar && c == 's' && upperCount >= 2)
                    continue;

                if (!char.IsUpper(c))
                    return false;

                upperCount++;
            }

            return upperCount >= 2;
        }

        public static string ToPascalCasing(this string text, char wordSeparator = DefaultSeparator)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Capitalize the first letter.
            string camelCasingText = text.ToCamelCasing(wordSeparator);
            if (camelCasingText.Length == 1)
                return camelCasingText.ToUpper();

            return char.ToUpper(camelCasingText[0]) + camelCasingText.Substring(1);
        }

        /// <summary>
        /// Gets the human readable version of programmer text, like a variable name.
        /// </summary>
        /// <param name="programmerText">The programmer text.</param>
        /// <returns>The human readable equivalent of the programmer text.</returns>
        public static string ToHumanReadable(
            this string programmerText,
            char wordSeparator = DefaultSeparator)
        {
            if (string.IsNullOrEmpty(programmerText))
                return programmerText;

            bool wasLetter = false;
            bool wasUpperCase = false;
            bool addedSpace = false;
            string result = "";

            // First remove the m_ prefix if it exists.
            if (programmerText.StartsWith(HungarianPrefix))
                programmerText = programmerText.Substring(HungarianPrefix.Length);

            // Deal with any miscellanneous spaces.
            if (wordSeparator != DefaultSeparator)
                programmerText = programmerText.Replace(DefaultSeparator, wordSeparator);

            // Deal with any miscellanneous underscores.
            if (wordSeparator != Underscore)
                programmerText = programmerText.Replace(Underscore, wordSeparator);

            // Go through the original string and copy it with some modifications.
            for (int i = 0; i < programmerText.Length; i++)
            {
                // If there was a change in caps add spaces.
                if ((wasUpperCase != char.IsUpper(programmerText[i])
                     || (wasLetter != char.IsLetter(programmerText[i])))
                    && i > 0 && !addedSpace
                    && !(IsExcludedSymbol(programmerText[i], wordSeparator) ||
                         IsExcludedSymbol(programmerText[i - 1], wordSeparator)))
                {
                    if (wasUpperCase && i > 1 && !IsExcludedSymbol(programmerText[i - 1], wordSeparator)
                        && !IsExcludedSymbol(result[result.Length - 2], wordSeparator))
                    {
                        if (wasLetter && char.IsLetter(programmerText[i]))
                            result = result.Insert(result.Length - 1, wordSeparator.ToString());
                        else
                            result += wordSeparator;
                        addedSpace = true;
                    }

                    // Lower case to upper case.
                    if (!wasUpperCase)
                    {
                        result += wordSeparator;
                        addedSpace = true;
                    }
                }
                else
                {
                    // No case change.
                    addedSpace = false;
                }

                // Add the character.
                result += programmerText[i];

                // Capitalize the first character.
                if (i == 0)
                    result = result.ToUpper();

                // Remember things about the previous letter.
                wasLetter = char.IsLetter(programmerText[i]);
                wasUpperCase = char.IsUpper(programmerText[i]);
            }

            return result;
        }

        public static string RemovePrefix(this string name, string prefix)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(prefix))
                return name;

            if (!name.StartsWith(prefix))
                return name;

            return name.Substring(prefix.Length);
        }

        public static string RemoveSuffix(this string name, string suffix)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(suffix))
                return name;

            if (!name.EndsWith(suffix))
                return name;

            return name.Substring(0, name.Length - suffix.Length);
        }

        /// <summary>
        /// Converts the slashes to be consistent.
        /// </summary>
        public static string ToUnityPath(this string name)
        {
            return name.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private const string AssetsFolder = "Assets";

        public static string RemoveAssetsPrefix(this string path)
        {
            return path.RemovePrefix(AssetsFolder + Path.AltDirectorySeparatorChar);
        }

        public static string GetAbsolutePath(this string projectPath)
        {
            string absolutePath = projectPath.ToUnityPath().RemoveAssetsPrefix();
            return Application.dataPath + Path.AltDirectorySeparatorChar + absolutePath;
        }

        public static string GetWhitespacePreceding(this string text, int index, bool includingNewLines)
        {
            string whitespacePreceding = string.Empty;
            if (index > 0)
            {
                for (int i = index - 1; i >= 0; i--)
                {
                    char c = text[i];
                    if (c == '\n' && !includingNewLines)
                        break;
                    if (char.IsWhiteSpace(c))
                        whitespacePreceding = text[i] + whitespacePreceding;
                    else
                        break;
                }
            }

            return whitespacePreceding;
        }

        public static string GetSection(this string text, string from, string to)
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                return string.Empty;

            int fromIndex = text.IndexOf(from, StringComparison.Ordinal);
            if (fromIndex == -1)
                return string.Empty;
            fromIndex += from.Length;

            int toIndex = text.IndexOf(to, fromIndex, StringComparison.Ordinal);
            if (toIndex == -1)
                return string.Empty;

            return text.Substring(fromIndex, toIndex - fromIndex);
        }

        public static string GetParentDirectory(this string path)
        {
            int lastDirectorySeparator = path.LastIndexOfAny(DirectorySeparators);
            if (lastDirectorySeparator == -1)
                return path;

            return path.Substring(0, lastDirectorySeparator);
        }
    }
}