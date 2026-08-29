#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    /// <summary>Aligns Kotlin's legacy JDK split artifacts after IAP and EDM generate Gradle.</summary>
    public sealed class HillAdventurerAndroidDependencyPostprocessor :
        IPostGenerateGradleAndroidProject
    {
        private const string Marker = "HILL_ADVENTURER_KOTLIN_ALIGNMENT";

        public int callbackOrder => 10000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            DirectoryInfo unityLibrary = new DirectoryInfo(path);
            string buildGradle = unityLibrary.Parent != null
                ? Path.Combine(unityLibrary.Parent.FullName, "build.gradle")
                : string.Empty;
            if (!File.Exists(buildGradle))
                throw new FileNotFoundException("Generated root build.gradle was not found",
                    buildGradle);

            string contents = File.ReadAllText(buildGradle);
            if (contents.IndexOf(Marker, StringComparison.Ordinal) >= 0) return;

            contents += @"

// HILL_ADVENTURER_KOTLIN_ALIGNMENT
// Games v2 -> coroutines 1.6.4 requests the old 1.6.21 JDK split jars, while
// Unity IAP / Billing 9 resolves the merged kotlin-stdlib 1.8.22.
subprojects {
    configurations.configureEach {
        resolutionStrategy {
            force 'org.jetbrains.kotlin:kotlin-stdlib-jdk7:1.8.22'
            force 'org.jetbrains.kotlin:kotlin-stdlib-jdk8:1.8.22'
        }
    }
}
";
            File.WriteAllText(buildGradle, contents);
            Debug.Log("[Android Dependencies] Kotlin JDK compatibility artifacts aligned to 1.8.22");
        }
    }
}
#endif
