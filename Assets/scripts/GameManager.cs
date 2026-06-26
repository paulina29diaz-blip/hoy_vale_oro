using UnityEngine;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        // Crear la TaskList persistente si no existe aún
        if (TaskList.Instance == null)
        {
            GameObject go = new GameObject("TaskList");
            go.AddComponent<TaskList>();
        }
    }

    void Update() { }
}
