using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controlador del panel de OPCIONES del menú principal.
/// Se auto-inicializa al cargar la escena "menu", busca el botón
/// "BotonOpciones" y le agrega el listener para abrir el panel.
/// </summary>
public class MenuOpcionesController : MonoBehaviour
{
    private Canvas      canvas;
    private GameObject  panelOpciones;
    private Slider      sliderVolumen;

    // -----------------------------------------------------------------------
    void Start()
    {
        // Solo actua en la escena menu
        if (SceneManager.GetActiveScene().name != "menu") return;

        BuildPanel();
        HookOpcionesButton();
    }

    // -----------------------------------------------------------------------
    // Hookear el boton OPCIONES que ya existe en la escena
    // -----------------------------------------------------------------------
    private void HookOpcionesButton()
    {
        GameObject btnGO = GameObject.Find("BotonOpciones");
        if (btnGO == null) { Debug.LogWarning("[Opciones] BotonOpciones no encontrado"); return; }

        Button btn = btnGO.GetComponent<Button>();
        if (btn == null) return;

        btn.onClick.AddListener(AbrirOpciones);
    }

    // -----------------------------------------------------------------------
    // Construir el panel de opciones (oculto al inicio)
    // -----------------------------------------------------------------------
    private void BuildPanel()
    {
        // Canvas propio para no interferir con la escena
        GameObject cgo = new GameObject("OpcionesCanvas");
        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler sc = cgo.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight  = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();

        // Fondo oscurecido
        GameObject fondo = MakeRect(cgo.transform, "Fondo", 0f, 0f, 1f, 1f);
        Image fondoImg = fondo.AddComponent<Image>();
        fondoImg.color = new Color(0f, 0f, 0f, 0.75f);

        // Panel central
        panelOpciones = MakeRect(cgo.transform, "Panel", 0.28f, 0.15f, 0.72f, 0.85f);
        Image panelImg = panelOpciones.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.06f, 0.04f, 0.97f);
        Outline borde = panelOpciones.AddComponent<Outline>();
        borde.effectColor    = new Color(0.65f, 0.48f, 0.12f, 1f);
        borde.effectDistance = new Vector2(3f, -3f);

        // Titulo
        MakeTxt(panelOpciones.transform, "Titulo", 0.04f, 0.88f, 0.96f, 0.99f,
            "OPCIONES", 36f, FontStyles.Bold, TextAlignmentOptions.Center,
            new Color(1f, 0.82f, 0.15f));

        // Linea separadora
        MakeImg(MakeRect(panelOpciones.transform, "Sep1", 0.04f, 0.865f, 0.96f, 0.875f),
            new Color(0.6f, 0.45f, 0.12f, 0.7f));

        // ── VOLUMEN ────────────────────────────────────────────────────────
        MakeTxt(panelOpciones.transform, "LblVol", 0.06f, 0.76f, 0.94f, 0.84f,
            "🔊  VOLUMEN", 22f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft,
            new Color(0.9f, 0.85f, 0.65f));

        // Track del slider
        GameObject sliderGO = MakeRect(panelOpciones.transform, "SliderVol",
            0.06f, 0.68f, 0.94f, 0.76f);
        sliderVolumen = sliderGO.AddComponent<Slider>();
        sliderVolumen.minValue = 0f;
        sliderVolumen.maxValue = 1f;
        sliderVolumen.value    = AudioListener.volume;

        // Fondo del track
        GameObject bgGO = MakeRect(sliderGO.transform, "Background", 0f, 0.25f, 1f, 0.75f);
        MakeImg(bgGO, new Color(0.18f, 0.15f, 0.08f, 1f));
        sliderVolumen.targetGraphic = bgGO.GetComponent<Image>();

        // Fill area
        GameObject fillArea = MakeRect(sliderGO.transform, "Fill Area", 0.02f, 0.2f, 0.98f, 0.8f);
        GameObject fill = MakeRect(fillArea.transform, "Fill", 0f, 0f, 0f, 1f);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.85f, 0.62f, 0.1f, 1f);
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0f, 1f);
        sliderVolumen.fillRect = fillRT;

        // Handle slide area
        GameObject handleArea = MakeRect(sliderGO.transform, "Handle Slide Area", 0f, 0f, 1f, 1f);
        GameObject handle = MakeRect(handleArea.transform, "Handle", 0f, 0f, 0f, 1f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(1f, 0.82f, 0.15f, 1f);
        RectTransform handleRT = handle.GetComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0f, 0f);
        handleRT.anchorMax = new Vector2(0f, 1f);
        handleRT.sizeDelta = new Vector2(28f, 0f);
        sliderVolumen.handleRect = handleRT;

        sliderVolumen.direction = Slider.Direction.LeftToRight;
        sliderVolumen.onValueChanged.AddListener(OnVolumenCambiado);

        // Valor de volumen en texto
        TextMeshProUGUI txtVol = MakeTxt(panelOpciones.transform, "TxtVol",
            0.40f, 0.60f, 0.60f, 0.68f,
            Mathf.RoundToInt(AudioListener.volume * 100) + "%",
            20f, FontStyles.Bold, TextAlignmentOptions.Center,
            new Color(1f, 0.82f, 0.3f));
        sliderVolumen.onValueChanged.AddListener(delegate(float v) {
            txtVol.text = Mathf.RoundToInt(v * 100) + "%";
        });

        // Linea separadora
        MakeImg(MakeRect(panelOpciones.transform, "Sep2", 0.04f, 0.575f, 0.96f, 0.585f),
            new Color(0.6f, 0.45f, 0.12f, 0.5f));

        // ── CONTROLES ─────────────────────────────────────────────────────
        MakeTxt(panelOpciones.transform, "LblCtrl", 0.06f, 0.51f, 0.94f, 0.57f,
            "🎮  CONTROLES", 22f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft,
            new Color(0.9f, 0.85f, 0.65f));

        string[,] controles = new string[,] {
            { "A / D", "Mover el auto" },
            { "F",     "Interactuar con NPCs" },
            { "Y",     "Abrir / cerrar inventario" },
        };
        float yTop = 0.49f;
        float rowH = 0.085f;
        for (int i = 0; i < 3; i++)
        {
            float y0 = yTop - i * rowH;
            // Fondo alternado
            if (i % 2 == 0)
                MakeImg(MakeRect(panelOpciones.transform, "RowBg" + i,
                    0.04f, y0 - rowH + 0.005f, 0.96f, y0),
                    new Color(1f, 1f, 1f, 0.03f));

            // Tecla
            GameObject keyBg = MakeRect(panelOpciones.transform, "Key" + i,
                0.06f, y0 - rowH + 0.01f, 0.28f, y0 - 0.01f);
            MakeImg(keyBg, new Color(0.25f, 0.20f, 0.08f, 1f));
            Outline ko = keyBg.AddComponent<Outline>();
            ko.effectColor    = new Color(0.7f, 0.55f, 0.15f, 0.8f);
            ko.effectDistance = new Vector2(1.5f, -1.5f);
            MakeTxt(keyBg.transform, "K", 0f, 0f, 1f, 1f,
                controles[i, 0], 18f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Color(1f, 0.82f, 0.15f));

            // Descripcion
            MakeTxt(panelOpciones.transform, "Desc" + i,
                0.30f, y0 - rowH + 0.01f, 0.96f, y0 - 0.01f,
                controles[i, 1], 18f, FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft,
                new Color(0.88f, 0.84f, 0.70f));
        }

        // Linea separadora
        MakeImg(MakeRect(panelOpciones.transform, "Sep3", 0.04f, 0.215f, 0.96f, 0.225f),
            new Color(0.6f, 0.45f, 0.12f, 0.5f));

        // ── BOTONES ───────────────────────────────────────────────────────
        // Boton CERRAR JUEGO
        GameObject btnCerrar = MakeRect(panelOpciones.transform, "BtnCerrar",
            0.06f, 0.07f, 0.46f, 0.17f);
        MakeImg(btnCerrar, new Color(0.40f, 0.08f, 0.08f, 1f));
        Outline oc = btnCerrar.AddComponent<Outline>();
        oc.effectColor    = new Color(0.85f, 0.2f, 0.2f, 0.8f);
        oc.effectDistance = new Vector2(2f, -2f);
        Button btnC = btnCerrar.AddComponent<Button>();
        btnC.onClick.AddListener(CerrarJuego);
        MakeTxt(btnCerrar.transform, "L", 0f, 0f, 1f, 1f,
            "CERRAR JUEGO", 18f, FontStyles.Bold, TextAlignmentOptions.Center,
            new Color(0.95f, 0.80f, 0.80f));

        // Boton VOLVER
        GameObject btnVolver = MakeRect(panelOpciones.transform, "BtnVolver",
            0.54f, 0.07f, 0.94f, 0.17f);
        MakeImg(btnVolver, new Color(0.15f, 0.22f, 0.10f, 1f));
        Outline ov = btnVolver.AddComponent<Outline>();
        ov.effectColor    = new Color(0.3f, 0.75f, 0.2f, 0.8f);
        ov.effectDistance = new Vector2(2f, -2f);
        Button btnV = btnVolver.AddComponent<Button>();
        btnV.onClick.AddListener(CerrarPanel);
        MakeTxt(btnVolver.transform, "L", 0f, 0f, 1f, 1f,
            "VOLVER", 18f, FontStyles.Bold, TextAlignmentOptions.Center,
            new Color(0.80f, 0.95f, 0.75f));

        // Boton X esquina
        GameObject btnX = MakeRect(panelOpciones.transform, "BtnX",
            0.88f, 0.91f, 0.98f, 0.99f);
        MakeImg(btnX, new Color(0.3f, 0.06f, 0.06f, 0.9f));
        Button bX = btnX.AddComponent<Button>();
        bX.onClick.AddListener(CerrarPanel);
        MakeTxt(btnX.transform, "X", 0f, 0f, 1f, 1f,
            "✕", 22f, FontStyles.Bold, TextAlignmentOptions.Center,
            Color.white);

        // Ocultar al inicio
        canvas.gameObject.SetActive(false);
    }

    // -----------------------------------------------------------------------
    // Callbacks
    // -----------------------------------------------------------------------
    private void AbrirOpciones()
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
            // Sincronizar slider con volumen actual
            if (sliderVolumen != null)
                sliderVolumen.value = AudioListener.volume;
        }
    }

    private void CerrarPanel()
    {
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    private void OnVolumenCambiado(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat("volumen", valor);
        PlayerPrefs.Save();
    }

    private void CerrarJuego()
    {
        PlayerPrefs.SetFloat("volumen", AudioListener.volume);
        PlayerPrefs.Save();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // -----------------------------------------------------------------------
    // Helpers UI
    // -----------------------------------------------------------------------
    private static GameObject MakeRect(Transform p, string n,
        float ax, float ay, float bx, float by)
    {
        GameObject go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(p, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(ax, ay);
        rt.anchorMax = new Vector2(bx, by);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    private static Image MakeImg(GameObject go, Color c)
    {
        Image img = go.AddComponent<Image>();
        img.color = c;
        return img;
    }

    private static TextMeshProUGUI MakeTxt(Transform p, string n,
        float ax, float ay, float bx, float by,
        string text, float fs, FontStyles style,
        TextAlignmentOptions align, Color col)
    {
        GameObject go = MakeRect(p, n, ax, ay, bx, by);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text               = text;
        t.fontSize           = fs;
        t.fontStyle          = style;
        t.alignment          = align;
        t.color              = col;
        t.enableWordWrapping = true;
        t.richText           = true;
        return t;
    }
}
