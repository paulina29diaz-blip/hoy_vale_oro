using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Attached to each nivel_X button on the mapa scene.
/// The prerequisite chain is hardcoded by sceneName — no external initialization needed.
/// </summary>
public class BotonNivel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Transition Settings")]
    public string sceneName;          // set in Unity Inspector (already serialized in scene)
    public float scaleFactor = 1.10f;
    public float transitionSpeed = 12f;

    // Injected by MapSceneManager (not serialized — connected at runtime)
    [System.NonSerialized] public GameObject candadoObject;

    private Vector3 originalScale;
    private bool isHovered = false;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    void Start()
    {
        AplicarVisualCandado();
    }

    void Update()
    {
        // Keep candado in sync every frame (catches the moment it unlocks)
        AplicarVisualCandado();

        // Hover scale only when unlocked
        bool locked = EstaBlockeado();
        Vector3 target = (isHovered && !locked) ? originalScale * scaleFactor : originalScale;
        transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * transitionSpeed);
    }

    // -----------------------------------------------------------------------
    // Core logic: prerequisite derived from sceneName (already in scene file)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the scene that must be completed before this one, or null if always unlocked.
    /// </summary>
    private string ObtenerEscenaRequerida()
    {
        switch (sceneName)
        {
            case "nivel_2_quilmes":    return "nivel_1_ypf";
            case "nivel_3_avellaneda": return "nivel_2_quilmes";
            case "nivel_4_caminito":   return "nivel_3_avellaneda";
            case "nivel_5_uade":       return "nivel_4_caminito";
            case "nivel_6_obelisco":   return "nivel_5_uade";
            default:                   return null; // nivel_1_ypf — always available
        }
    }

    private bool EstaBlockeado()
    {
        string req = ObtenerEscenaRequerida();
        if (string.IsNullOrEmpty(req)) return false;
        return PlayerPrefs.GetInt(req + "_completado", 0) == 0;
    }

    private void AplicarVisualCandado()
    {
        if (candadoObject != null)
            candadoObject.SetActive(EstaBlockeado());
    }

    // -----------------------------------------------------------------------
    // Pointer events
    // -----------------------------------------------------------------------

    public void OnPointerEnter(PointerEventData e) => isHovered = true;
    public void OnPointerExit(PointerEventData e)  => isHovered = false;

    public void OnPointerClick(PointerEventData e)
    {
        if (EstaBlockeado())
        {
            Debug.Log($"[BotonNivel] '{gameObject.name}' bloqueado. Requiere completar '{ObtenerEscenaRequerida()}'.");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning($"[BotonNivel] sceneName vacío en '{gameObject.name}'.");
            return;
        }

        Debug.Log($"[BotonNivel] Cargando escena '{sceneName}'");
        Time.timeScale = 1f;
        Movimiento.ResetearPosicion();
        NPCInteraction.ResetearTratosDeNivel(sceneName);
        SceneManager.LoadScene(sceneName);
    }
}
