using FMODUnity;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    [InitializeOnLoad]
    public static class FmodBankFileChangePostprocessor
    {
        private static DateTime lastSeenCacheTime = DateTime.MinValue;

        private static string EditorPrefsCacheTimeKey =>
            "FMODSyntax.LastHandledCacheTime." + Application.dataPath.GetHashCode();

        private static string EditorPrefsFingerprintKey =>
            "FMODSyntax.LastHandledFingerprint." + Application.dataPath.GetHashCode();

        static FmodBankFileChangePostprocessor()
        {
            lastSeenCacheTime = LoadPersistedCacheTime();
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (!EventManager.IsInitialized) return;

            DateTime currentCacheTime = EventManager.CacheTime;

            if (currentCacheTime == DateTime.MinValue || currentCacheTime == lastSeenCacheTime) return;

            lastSeenCacheTime = currentCacheTime;
            SavePersistedCacheTime(currentCacheTime);

            string fingerprint = ComputeFingerprint();
            string previousFingerprint = EditorPrefs.GetString(EditorPrefsFingerprintKey, string.Empty);

            if (fingerprint == previousFingerprint) return;

            EditorPrefs.SetString(EditorPrefsFingerprintKey, fingerprint);

            RegenerateCode();
        }

        private static string ComputeFingerprint()
        {
            StringBuilder builder = new StringBuilder();

            foreach (var eventRef in EventManager.Events.OrderBy(e => e.Path, StringComparer.Ordinal))
            {
                builder.Append("EVT|").Append(eventRef.Path).Append('|').Append(eventRef.Guid)
                    .Append('|').Append(eventRef.IsOneShot).Append('|').Append(eventRef.Is3D)
                    .Append('|').Append(eventRef.IsStream).Append('\n');

                foreach (var param in eventRef.LocalParameters.OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    AppendParameter(builder, param);
                }
            }

            foreach (var param in EventManager.Parameters.OrderBy(p => p.Name, StringComparer.Ordinal))
            {
                AppendParameter(builder, param);
            }

            foreach (var bank in EventManager.Banks.OrderBy(b => b.Path, StringComparer.Ordinal))
            {
                builder.Append("BANK|").Append(bank.Path).Append('\n');
            }

            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
                return Convert.ToBase64String(hash);
            }
        }

        private static void AppendParameter(StringBuilder builder, EditorParamRef param)
        {
            builder.Append("PARAM|").Append(param.Name).Append('|').Append(param.ID.data1)
                .Append('|').Append(param.ID.data2).Append('|').Append(param.Type)
                .Append('|').Append(param.Min).Append('|').Append(param.Max)
                .Append('|').Append(param.Default).Append('|').Append(param.IsGlobal);

            if (param.Labels != null)
                builder.Append('|').Append(string.Join(",", param.Labels));

            builder.Append('\n');
        }

        private static void RegenerateCode()
        {
            try
            {
                FmodCodeGenerator.GenerateCode();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static DateTime LoadPersistedCacheTime()
        {
            string stored = EditorPrefs.GetString(EditorPrefsCacheTimeKey, string.Empty);
            if (string.IsNullOrEmpty(stored)) return DateTime.MinValue;

            if (long.TryParse(stored, out long ticks))
            {
                try { return new DateTime(ticks); }
                catch (ArgumentOutOfRangeException) { return DateTime.MinValue; }
            }

            return DateTime.MinValue;
        }

        private static void SavePersistedCacheTime(DateTime time)
        {
            EditorPrefs.SetString(EditorPrefsCacheTimeKey, time.Ticks.ToString());
        }
    }
}