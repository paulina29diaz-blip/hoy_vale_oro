#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public static class StandaloneBuilder
{
    [MenuItem("Tools/Compilar Juego para Mac (.app)")]
    public static void BuildMac()
    {
        string buildPath = "/Users/franciscofafian/Downloads/Facu/Videojuegos/Builds/Mac/HoyValeOro.app";
        string buildFolder = Path.GetDirectoryName(buildPath);

        if (!Directory.Exists(buildFolder))
        {
            Directory.CreateDirectory(buildFolder);
        }

        List<string> activeScenes = GetActiveScenes();
        if (activeScenes.Count == 0) return;

        Debug.Log($"[StandaloneBuilder] Iniciando compilación macOS en: {buildPath}");

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = activeScenes.ToArray();
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.StandaloneOSX;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[StandaloneBuilder] Compilación macOS exitosa.");
            EditorUtility.RevealInFinder(buildPath);
            EditorUtility.DisplayDialog("Compilación Completada", 
                $"El juego para Mac se compiló con éxito en:\n{buildPath}\n\nSe abrió la carpeta en Finder.", "OK");
        }
        else
        {
            Debug.LogError($"[StandaloneBuilder] La compilación macOS falló.");
            EditorUtility.DisplayDialog("Error de Compilación", "La compilación para macOS falló. Revisa la consola para más detalles.", "OK");
        }
    }

    [MenuItem("Tools/Compilar Juego para Windows (.exe)")]
    public static void BuildWindows()
    {
        string buildPath = "/Users/franciscofafian/Downloads/Facu/Videojuegos/juegofinal/HoyValeOro.exe";
        string buildFolder = Path.GetDirectoryName(buildPath);

        if (!Directory.Exists(buildFolder))
        {
            Directory.CreateDirectory(buildFolder);
        }

        List<string> activeScenes = GetActiveScenes();
        if (activeScenes.Count == 0) return;

        Debug.Log($"[StandaloneBuilder] Iniciando compilación Windows en: {buildPath}");

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = activeScenes.ToArray();
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[StandaloneBuilder] Compilación Windows exitosa.");
            EditorUtility.RevealInFinder(buildPath);
            EditorUtility.DisplayDialog("Compilación Completada", 
                $"El juego para Windows se compiló con éxito en:\n{buildPath}\n\nSe abrió la carpeta en Finder.", "OK");
        }
        else
        {
            Debug.LogError($"[StandaloneBuilder] La compilación Windows falló.");
            EditorUtility.DisplayDialog("Error de Compilación", "La compilación para Windows falló.\n\nAsegúrate de tener el módulo 'Windows Build Support' instalado en Unity Hub.", "OK");
        }
    }

    private static List<string> GetActiveScenes()
    {
        List<string> activeScenes = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                activeScenes.Add(scene.path);
            }
        }

        if (activeScenes.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No hay escenas habilitadas en Build Settings.", "OK");
        }
        return activeScenes;
    }
}
#endif
