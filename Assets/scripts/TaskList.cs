using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// HUD permanente en la esquina superior derecha que muestra los 5 objetos
/// necesarios para llegar al Obelisco. Se tacha cuando el jugador lo tiene
/// en el inventario. Cliqueando el header se colapsa / expande.
/// </summary>
public class TaskList : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Singleton
    // -----------------------------------------------------------------------
    public static TaskList Instance { get; private set; }

    // -----------------------------------------------------------------------
    // Datos
    // -----------------------------------------------------------------------
    private static readonly string[] ITEM_KEYS = {
        "caja de herramientas", "bateria", "mapa", "anafe", "guantes"
    };

    private static readonly string[] ITEM_LABELS = {
        "Caja de herramientas", "Batería para el auto", "Mapa", "Anafe", "Guantes"
    };

    private static readonly string[] ESCENAS_OCULTAS = {
        "menu", "videointro", "mapa", "dialogo", "dialogos", "escenatrueque", "Intropikirrin"
    };

    // -----------------------------------------------------------------------
    // UI refs
    // -----------------------------------------------------------------------
    private Canvas              canvas;
    private GameObject          panelBody;      // parte colapsable (ítems + línea)
    private TextMeshProUGUI     txtFlecha;      // ▲ / ▼ en el header
    private TextMeshProUGUI[]   txtItems;
    private bool[]              estadoAnterior;
    private bool                expandido = true;

    // Anchos del panel según estado
    private RectTransform       panelRT;
    private const float PANEL_Y_EXPANDIDO  = 0.72f;
    private const float PANEL_Y_COLAPSADO  = 0.92f;   // solo muestra el header

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
        SceneManager.sceneLoaded += OnSceneLoaded;
        ActualizarVisibilidad(SceneManager.GetActiveScene().name);
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode _) => ActualizarVisibilidad(scene.name);

    void Update() => RefrescarEstado();

    // -----------------------------------------------------------------------
    // Construcción de UI
    // -----------------------------------------------------------------------
    private void BuildUI()
    {
        // Canvas
        GameObject cgo = new GameObject("TaskListCanvas");
        cgo.transform.SetParent(transform, false);
        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler cs = cgo.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();

        // Panel principal
        GameObject panel = Rect(cgo.transform, "Panel", 0.73f, PANEL_Y_EXPANDIDO, 0.99f, 0.99f);
        panelRT = panel.GetComponent<RectTransform>();
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.04f, 0.03f, 0.85f);
        Outline borde = panel.AddComponent<Outline>();
        borde.effectColor    = new Color(0.6f, 0.5f, 0.2f, 0.7f);
        borde.effectDistance = new Vector2(1.5f, -1.5f);

        // ── Header cliqueable ─────────────────────────────────────────────
        GameObject header = Rect(panel.transform, "Header", 0f, 0.84f, 1f, 1f);
        // Fondo header ligeramente más claro
        Image headerImg = header.AddComponent<Image>();
        headerImg.color = new Color(0.12f, 0.10f, 0.05f, 0.95f);

        // Cursor hint (cambia el puntero a mano al pasar)
        Button headerBtn = header.AddComponent<Button>();
        ColorBlock cb = headerBtn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1f, 0.95f, 0.75f, 1f);  // se ilumina al hover
        cb.pressedColor     = new Color(0.8f, 0.7f, 0.4f, 1f);
        headerBtn.colors    = cb;
        headerBtn.onClick.AddListener(ToggleExpandido);

        // Ícono clipboard + texto
        TextMeshProUGUI txtTitulo = TMP(header.transform, "Titulo", 0.04f, 0.1f, 0.78f, 0.9f);
        txtTitulo.text      = "📋 OBJETIVOS";
        txtTitulo.fontSize  = 15f;
        txtTitulo.fontStyle = FontStyles.Bold;
        txtTitulo.color     = new Color(1f, 0.82f, 0.15f);
        txtTitulo.alignment = TextAlignmentOptions.MidlineLeft;

        // Flecha ▼ / ▲ en el lado derecho del header (indica que es colapsable)
        txtFlecha = TMP(header.transform, "Flecha", 0.78f, 0.1f, 0.97f, 0.9f);
        txtFlecha.text      = "▲";
        txtFlecha.fontSize  = 14f;
        txtFlecha.color     = new Color(1f, 0.82f, 0.15f, 0.8f);
        txtFlecha.alignment = TextAlignmentOptions.MidlineRight;

        // ── Cuerpo colapsable (línea divisora + ítems) ────────────────────
        panelBody = Rect(panel.transform, "Body", 0f, 0f, 1f, 0.84f);

        // Línea divisora
        GameObject linea = Rect(panelBody.transform, "Linea", 0.03f, 0.955f, 0.97f, 0.97f);
        linea.AddComponent<Image>().color = new Color(0.6f, 0.5f, 0.2f, 0.4f);

        // Ítems
        txtItems       = new TextMeshProUGUI[ITEM_LABELS.Length];
        estadoAnterior = new bool[ITEM_LABELS.Length];
        float itemH = 1f / ITEM_LABELS.Length;

        for (int i = 0; i < ITEM_LABELS.Length; i++)
        {
            float yMax = 0.93f - i * itemH;
            float yMin = yMax - itemH + 0.01f;
            txtItems[i] = TMP(panelBody.transform, "Item_" + i, 0.05f, yMin, 0.97f, yMax);
            txtItems[i].fontSize           = 13f;
            txtItems[i].enableWordWrapping = false;
            txtItems[i].overflowMode       = TextOverflowModes.Ellipsis;
            estadoAnterior[i]              = false;
            ActualizarItem(i, false);
        }
    }

    // -----------------------------------------------------------------------
    // Toggle colapsar / expandir
    // -----------------------------------------------------------------------
    private void ToggleExpandido()
    {
        expandido = !expandido;

        // Mostrar/ocultar el cuerpo
        panelBody.SetActive(expandido);

        // Cambiar flecha
        txtFlecha.text = expandido ? "▲" : "▼";

        // Ajustar altura del panel (solo el anchorMin.y cambia)
        if (panelRT != null)
        {
            panelRT.anchorMin = new Vector2(0.73f, expandido ? PANEL_Y_EXPANDIDO : PANEL_Y_COLAPSADO);
        }
    }

    // -----------------------------------------------------------------------
    // Lógica de estado de ítems
    // -----------------------------------------------------------------------
    private void RefrescarEstado()
    {
        if (InventoryManager.Instance == null) return;
        for (int i = 0; i < ITEM_KEYS.Length; i++)
        {
            bool tiene = InventoryManager.Instance.HasItem(ITEM_KEYS[i]);
            if (tiene != estadoAnterior[i])
            {
                estadoAnterior[i] = tiene;
                ActualizarItem(i, tiene);
            }
        }
    }

    private void ActualizarItem(int idx, bool tiene)
    {
        if (txtItems == null || idx >= txtItems.Length || txtItems[idx] == null) return;
        if (tiene)
        {
            txtItems[idx].text  = "<s><color=#7FBF7F>" + ITEM_LABELS[idx] + "</color></s> ✓";
            txtItems[idx].color = new Color(0.5f, 0.85f, 0.5f, 0.9f);
        }
        else
        {
            txtItems[idx].text  = "◻ " + ITEM_LABELS[idx];
            txtItems[idx].color = new Color(0.92f, 0.88f, 0.72f, 1f);
        }
    }

    private void ActualizarVisibilidad(string sceneName)
    {
        if (canvas == null) return;
        bool esNivel = true;
        foreach (string s in ESCENAS_OCULTAS)
            if (sceneName == s) { esNivel = false; break; }
        canvas.gameObject.SetActive(esNivel);
    }

    // -----------------------------------------------------------------------
    // Helpers UI
    // -----------------------------------------------------------------------
    private static GameObject Rect(Transform parent, string name,
        float ax, float ay, float bx, float by)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(ax, ay);
        rt.anchorMax = new Vector2(bx, by);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    private static TextMeshProUGUI TMP(Transform parent, string name,
        float ax, float ay, float bx, float by)
    {
        GameObject go = Rect(parent, name, ax, ay, bx, by);
        return go.AddComponent<TextMeshProUGUI>();
    }
}
