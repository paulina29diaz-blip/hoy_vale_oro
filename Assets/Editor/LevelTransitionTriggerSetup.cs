#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class LevelTransitionTriggerSetup
{
    [MenuItem("Tools/Configurar Trigger de Salida Nivel 1")]
    public static void SetupTrigger()
    {
        string scenePath = "Assets/Scenes/nivel_1_ypf.unity";

        if (!System.IO.File.Exists(scenePath))
        {
            EditorUtility.DisplayDialog("Error", "No se encontró la escena 'nivel_1_ypf.unity' en la ruta especificada.", "OK");
            return;
        }

        // Save currently open scenes
        string previousScene = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.SaveOpenScenes();

        // Open level 1 scene
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // 1. Clean up any previous standalone LevelTransitionTrigger objects
        GameObject oldTrigger = GameObject.Find("LevelTransitionTrigger");
        if (oldTrigger != null)
        {
            Debug.Log("[LevelTransitionTriggerSetup] Removing old standalone trigger object.");
            Object.DestroyImmediate(oldTrigger);
        }

        // 2. Configure Square (2) -> loads "mapa"
        GameObject sq2Obj = GameObject.Find("Square (2)");
        bool sq2Found = sq2Obj != null;
        if (sq2Found)
        {
            ConfigureTrigger(sq2Obj, "mapa");
        }

        // 3. Configure Square (3) or square (3) -> loads "menu"
        GameObject sq3Obj = GameObject.Find("Square (3)");
        if (sq3Obj == null) sq3Obj = GameObject.Find("square (3)");
        bool sq3Found = sq3Obj != null;
        if (sq3Found)
        {
            ConfigureTrigger(sq3Obj, "menu");
        }

        if (!sq2Found && !sq3Found)
        {
            EditorUtility.DisplayDialog("Error", "No se encontró ni 'Square (2)' ni 'Square (3)'/'square (3)' en la escena.", "OK");
            return;
        }

        // Mark scene dirty and save
        EditorSceneManager.MarkSceneDirty(scene);
        bool success = EditorSceneManager.SaveScene(scene, scenePath);

        if (success)
        {
            string msg = "Configuración completada con éxito en 'nivel_1_ypf.unity':\n\n";
            if (sq2Found) msg += $"• 'Square (2)' configurado para transicionar a 'mapa'\n";
            if (sq3Found) msg += $"• '{sq3Obj.name}' configurado para transicionar a 'menu'\n";
            EditorUtility.DisplayDialog("Configuración Exitosa", msg, "OK");
        }
        else
        {
            Debug.LogError("[LevelTransitionTriggerSetup] Error al intentar guardar la escena.");
            EditorUtility.DisplayDialog("Error", "Ocurrió un error al guardar la escena. Revisa la consola para más detalles.", "OK");
        }

        // Restore previous scene if applicable
        if (!string.IsNullOrEmpty(previousScene) && previousScene != scenePath)
        {
            EditorSceneManager.OpenScene(previousScene);
        }
        else
        {
            EditorSceneManager.OpenScene(scenePath);
        }
    }

    private static void ConfigureTrigger(GameObject go, string sceneName)
    {
        BoxCollider2D col = go.GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 6.0f);
        }

        LevelTransitionTrigger script = go.GetComponent<LevelTransitionTrigger>();
        if (script == null)
        {
            script = go.AddComponent<LevelTransitionTrigger>();
        }
        script.targetSceneName = sceneName;
        Debug.Log($"[LevelTransitionTriggerSetup] {go.name} configurado con éxito para cargar la escena '{sceneName}'.");
    }
}
#endif
