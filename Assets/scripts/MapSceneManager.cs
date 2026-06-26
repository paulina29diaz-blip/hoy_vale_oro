using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Manages the mapa scene:
/// - Cleans stray scripts from candado objects
/// - Connects each candado to its nivel button
/// - Provides keyboard shortcuts and progress reset for testing
/// </summary>
public class MapSceneManager : MonoBehaviour
{
    void Awake()
    {
        // Ensure EventSystem exists
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // --- CANDADO CLEANUP ---
        // Remove any stray BotonNivel from candado objects (Editor artifacts)
        // and disable their raycast so they never intercept clicks
        string[] candadoNames = { "candado_2", "candado_3", "candado_4", "candado_5", "candado_6" };
        foreach (string cn in candadoNames)
        {
            GameObject cGO = GameObject.Find(cn);
            if (cGO == null) continue;

            // Destroy every BotonNivel on the candado
            foreach (var b in cGO.GetComponents<BotonNivel>())
                Destroy(b);

            // Block all raycasts on the candado
            foreach (var img in cGO.GetComponents<Image>())
                img.raycastTarget = false;
        }

        // --- CONNECT CANDADOS TO NIVEL BUTTONS ---
        ConectarCandado("nivel_2", "candado_2");
        ConectarCandado("nivel_3", "candado_3");
        ConectarCandado("nivel_4", "candado_4");
        ConectarCandado("nivel_5", "candado_5");
        ConectarCandado("nivel_6", "candado_6");
    }

    void Start()
    {
        // Diálogo introductorio: se muestra solo la primera vez que se entra al mapa
        IntroDialogo.IntentarMostrar();
    }

    private void ConectarCandado(string nivelName, string candadoName)
    {
        GameObject nivelGO   = GameObject.Find(nivelName);
        GameObject candadoGO = GameObject.Find(candadoName);

        if (nivelGO == null)
        {
            Debug.LogWarning($"[MapSceneManager] '{nivelName}' no encontrado.");
            return;
        }

        BotonNivel btn = nivelGO.GetComponent<BotonNivel>();
        if (btn == null)
        {
            Debug.LogWarning($"[MapSceneManager] '{nivelName}' no tiene BotonNivel.");
            return;
        }

        if (candadoGO != null)
        {
            btn.candadoObject = candadoGO;

            // Render candado below the nivel button so nivel receives clicks first
            int nivelIdx = nivelGO.transform.GetSiblingIndex();
            if (candadoGO.transform.GetSiblingIndex() > nivelIdx)
                candadoGO.transform.SetSiblingIndex(Mathf.Max(0, nivelIdx - 1));

            Debug.Log($"[MapSceneManager] Candado '{candadoName}' conectado a '{nivelName}'.");
        }
    }

    void Update()
    {
        // Keyboard shortcuts for testing
        if (Input.GetKeyDown(KeyCode.R))
        {
            BorrarProgreso();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void BorrarProgreso()
    {
        PlayerPrefs.DeleteKey("nivel_1_ypf_completado");
        PlayerPrefs.DeleteKey("nivel_2_quilmes_completado");
        PlayerPrefs.DeleteKey("nivel_3_avellaneda_completado");
        PlayerPrefs.DeleteKey("nivel_4_caminito_completado");
        PlayerPrefs.DeleteKey("nivel_5_uade_completado");
        PlayerPrefs.DeleteKey("nivel_6_obelisco_completado");
        PlayerPrefs.Save();
        Debug.Log("[MapSceneManager] Progreso borrado.");
    }
}
