using FMODUnity;
using System;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Watches FMODUnity.EventManager's cache and regenerates FMOD-Syntax code, but only when the
    /// underlying event/parameter/bank data has actually changed from what's recorded in the
    /// generated code's own metadata - not just because FMOD's cache timestamp moved (which happens
    /// on every bank rebuild, including no-op ones).
    /// </summary>
    [InitializeOnLoad]
    public static class FmodBankFileChangePostprocessor
    {
        private static DateTime lastSeenCacheTime = DateTime.MinValue;

        static FmodBankFileChangePostprocessor()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (!EventManager.IsInitialized) return;

            DateTime currentCacheTime = EventManager.CacheTime;

            // Cheap early-out: FMOD hasn't recached since we last looked, nothing to check.
            if (currentCacheTime == DateTime.MinValue || currentCacheTime == lastSeenCacheTime) return;

            lastSeenCacheTime = currentCacheTime;

            // The cache time itself bumps on every bank rebuild regardless of whether anything
            // codegen-relevant changed, so compare against the fingerprint already embedded in
            // the last generated output rather than trusting the timestamp alone.
            string currentFingerprint = FmodContentFingerprint.Compute();
            string previousFingerprint = FmodCodeGenerator.GetPreviousContentFingerprint();

            if (currentFingerprint == previousFingerprint) return;

            RegenerateCode();
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
    }
}