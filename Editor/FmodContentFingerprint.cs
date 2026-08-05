using FMODUnity;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Computes a stable fingerprint of the FMOD data that FmodCodeGenerator actually reads
    /// (events, their parameters, global parameters, banks) - deliberately excluding file
    /// timestamps/sizes, which change on every bank rebuild regardless of relevance to codegen.
    /// </summary>
    public static class FmodContentFingerprint
    {
        public static string Compute()
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
    }
}