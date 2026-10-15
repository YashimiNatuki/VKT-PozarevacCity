#if UNITY_EDITOR && UNITY_ANDROID
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.Android;
using UnityEngine;

namespace Pozarevac.CityGame.Editor
{
    // Explicit, generated-project-only use of the host's existing certificate roots.
    public sealed class CityAndroidBuildTrust : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 100;
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-vktWindowsRootTrust") < 0) return;
            if (Application.platform != RuntimePlatform.WindowsEditor)
                throw new InvalidOperationException("Windows-ROOT trust requires the Windows Editor.");
            string file = Path.Combine(Directory.GetParent(Path.GetFullPath(path)).FullName, "gradle.properties");
            if (!File.Exists(file)) throw new FileNotFoundException("Generated Gradle properties are missing.", file);
            string text = File.ReadAllText(file);
            const string flags = " -Djavax.net.ssl.trustStoreType=Windows-ROOT -Djavax.net.ssl.trustStore=NONE";
            if (!Regex.IsMatch(text, @"(?m)^org\.gradle\.jvmargs="))
                throw new InvalidOperationException("Generated Gradle JVM options are missing.");
            text = Regex.Replace(text, @"(?m)^org\.gradle\.jvmargs=([^\r\n]*)", match =>
                match.Value.Contains("javax.net.ssl.trustStoreType") ? match.Value : match.Value + flags);
            text = Regex.Replace(text, @"(?m)^org\.gradle\.daemon=[^\r\n]*\r?\n?", "");
            text += "\norg.gradle.daemon=false\n";
            File.WriteAllText(file, text, new UTF8Encoding(false));
            Debug.Log("VKT_ANDROID_TRUST: Existing Windows trusted roots selected for the generated Gradle build; TLS validation remains enabled.");
        }
    }
}
#endif
