using System;
using System.Collections.Generic;
using System.IO;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Useful utility methods for the FMOD Syntax.
    /// </summary>
    public static class FmodSyntaxUtilities
    {
        private static readonly Dictionary<string, string> replacements = new Dictionary<string, string>()
        {
            { "base", "@base" },
        };

        private static readonly Dictionary<string, string> substitutions = new Dictionary<string, string>()
        {
            { "-", "_" },
            { " ", "" },
            { ".", "" },
            { "(", "" },
            { ")", "" },
            { "&", "And" },
        };

        public static string Filter(string name, bool performReplacements = true)
        {
            if (performReplacements)
            {
                foreach (KeyValuePair<string, string> replacement in replacements)
                {
                    if (name == replacement.Key)
                        return replacement.Value;
                }
            }

            foreach (KeyValuePair<string, string> substitution in substitutions)
            {
                name = name.Replace(substitution.Key, substitution.Value);
            }

            // Collapse underscores deterministically ourselves (capitalizing the letter right after each
            // underscore, then dropping the underscore) instead of relying on ToPascalCasing/ToCamelCasing to
            // do it consistently. Those get called on the string at different points with different casing
            // already applied (e.g. once per path segment, once on the joined file name), and in practice that
            // produces two DIFFERENT spellings for the same event ("FootstepsRunPlayback" vs "Footsteps_runPlayback")
            // depending on which code path computed it - one class gets generated, the other reference can't find it.
            // Doing this here, as the very last step of every Filter() call, guarantees every call site agrees.
            name = CollapseUnderscores(name);

            name = EnsureValidIdentifier(name);

            return name;
        }

        /// <summary>
        /// Removes underscores from a name while capitalizing the character that immediately followed each one,
        /// e.g. "Footsteps_run" becomes "FootstepsRun". Leaves strings with no underscore completely untouched
        /// (including the casing of the very first character), so it's safe to run on already-camelCased
        /// argument names without breaking them.
        /// </summary>
        private static string CollapseUnderscores(string name)
        {
            if (string.IsNullOrEmpty(name) || name.IndexOf('_') == -1)
                return name;

            System.Text.StringBuilder sb = new System.Text.StringBuilder(name.Length);
            bool capitalizeNext = false;
            foreach (char c in name)
            {
                if (c == '_')
                {
                    capitalizeNext = true;
                    continue;
                }

                if (capitalizeNext)
                {
                    sb.Append(char.ToUpperInvariant(c));
                    capitalizeNext = false;
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Guarantees the given string is safe to use as a C# identifier (class name, field name, etc).
        /// Strips any character that isn't a letter, digit or underscore, and if what's left starts with
        /// a digit (e.g. an FMOD event literally named "096_Yummers"), prefixes it with an underscore so
        /// it doesn't produce "Invalid token" / "Identifier expected" compiler errors.
        /// </summary>
        public static string EnsureValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "_";

            System.Text.StringBuilder sb = new System.Text.StringBuilder(name.Length);
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
                // Anything else (symbols, punctuation, whitespace we didn't already catch) just gets dropped.
            }

            string result = sb.ToString();

            if (string.IsNullOrEmpty(result))
                return "_";

            // C# identifiers can't start with a digit.
            if (char.IsDigit(result[0]))
                result = "_" + result;

            return result;
        }

        public static string GetDisplayNameFromPath(string path)
        {
            return Path.GetFileName(path);
        }

        public static string GetFilteredNameFromPath(string path)
        {
            string name = Path.GetFileName(path).ToPascalCasing();
            name = Filter(name);
            return name;
        }

        public static string GetFilteredNameFromPathLowerCase(string path)
        {
            string name = GetFilteredNameFromPath(path).ToCamelCasing();
            name = Filter(name);
            return name;
        }

        public static string GetFilteredPath(string path, bool stripSpecialCharacters)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            const char separator = '/';

            // Events start with something like event:/ , so get rid of that first.
            path = path.Substring(path.IndexOf(separator, StringComparison.Ordinal) + 1);

            if (stripSpecialCharacters)
            {
                // Clean up every section a bit.
                string[] pathSections = path.Split(separator);
                for (int i = 0; i < pathSections.Length; i++)
                {
                    pathSections[i] = pathSections[i].ToPascalCasing();
                    pathSections[i] = Filter(pathSections[i], false);
                }

                path = string.Join(separator, pathSections);
            }

            return path;
        }
    }
}