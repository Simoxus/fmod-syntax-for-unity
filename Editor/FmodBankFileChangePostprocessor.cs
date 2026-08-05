using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Watches for changes to built FMOD bank files (e.g. in StreamingAssets) and automatically
    /// regenerates FMOD-Syntax code when their *content* actually changes (not just on reimport).
    /// </summary>
    public class FmodBankFileChangePostprocessor : AssetPostprocessor
    {
        private const string BankExtension = ".bank";

        private static string HashCacheFilePath =>
            Application.dataPath.GetParentDirectory() + "/UserSettings/FMOD-Syntax/bankHashes.json";

        [Serializable]
        private class HashEntry
        {
            public string path;
            public string hash;
        }

        [Serializable]
        private class HashCache
        {
            public List<HashEntry> entries = new List<HashEntry>();
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            string[] candidatePaths = importedAssets
                .Concat(movedAssets)
                .Where(path => path.EndsWith(BankExtension, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            string[] deletedBankPaths = deletedAssets
                .Where(path => path.EndsWith(BankExtension, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (candidatePaths.Length == 0 && deletedBankPaths.Length == 0)
                return;

            Dictionary<string, string> cachedHashes = LoadHashCache();
            bool anyContentActuallyChanged = false;

            foreach (string deletedPath in deletedBankPaths)
            {
                if (cachedHashes.Remove(deletedPath))
                    anyContentActuallyChanged = true;
            }

            foreach (string path in candidatePaths)
            {
                string absolutePath = path.GetAbsolutePath();
                if (!File.Exists(absolutePath))
                    continue;

                string newHash = ComputeFileHash(absolutePath);

                bool hadPreviousHash = cachedHashes.TryGetValue(path, out string previousHash);
                if (!hadPreviousHash || previousHash != newHash)
                {
                    cachedHashes[path] = newHash;
                    anyContentActuallyChanged = true;
                }
            }

            if (!anyContentActuallyChanged)
                return;

            SaveHashCache(cachedHashes);

            FmodCodeGenerator.GenerateCode();
        }

        private static string ComputeFileHash(string absolutePath)
        {
            using (MD5 md5 = MD5.Create())
            using (FileStream stream = File.OpenRead(absolutePath))
            {
                byte[] hashBytes = md5.ComputeHash(stream);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private static Dictionary<string, string> LoadHashCache()
        {
            string filePath = HashCacheFilePath;
            Dictionary<string, string> result = new Dictionary<string, string>();

            if (!File.Exists(filePath))
                return result;

            try
            {
                string json = File.ReadAllText(filePath);
                HashCache cache = JsonUtility.FromJson<HashCache>(json);
                if (cache?.entries != null)
                {
                    foreach (HashEntry entry in cache.entries)
                        result[entry.path] = entry.hash;
                }
            }
            catch (Exception)
            {
            }

            return result;
        }

        private static void SaveHashCache(Dictionary<string, string> hashes)
        {
            HashCache cache = new HashCache
            {
                entries = hashes.Select(kvp => new HashEntry { path = kvp.Key, hash = kvp.Value }).ToList()
            };

            string json = JsonUtility.ToJson(cache, true);
            string filePath = HashCacheFilePath;

            string directory = Path.GetDirectoryName(filePath);
            Directory.CreateDirectory(directory);

            File.WriteAllText(filePath, json);
        }
    }
}