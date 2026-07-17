using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Minijuego de barra de nafta para npc1/4/7/10/13.
///
/// Fase 1: deja que DialogoManager construya su UI normal y luego llama a
///         DialogoManager.ActivarModoMinijuego() para modificar el texto
///         y los botones sin tocar el layout.
///
/// Fase 2: esconde el canvas del diálogo y muestra el minijuego de timing
///         con el marco barrajuego.png. La dificultad escala con el nivel.
/// </summary>
public class MinijuegoNafta : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Dificultad por nivel (valores hardcodeados para mayor control)
    // -----------------------------------------------------------------------
    //  Nivel | Velocidad | Zona verde
    //    1   |   0.55    |   0.18   ← fácil
    //    2   |   0.78    |   0.14
    //    3   |   1.08    |   0.10
    //    4   |   1.48    |   0.068
    //    5   |   1.95    |   0.042  ← bastante difícil
    //    6   |   2.50    |   0.026  ← muy difícil
    private static readonly float[] VELOCIDADES  = { 0.55f, 0.78f, 1.08f, 1.48f, 1.95f, 2.50f };
    private static readonly float[] ZONAS_VERDES = { 0.18f, 0.14f, 0.10f, 0.068f, 0.042f, 0.026f };

    private const float DELAY_INICIO    = 0.6f;
    private const float DELAY_RESULTADO = 3.0f;

    // Proporciones del hueco de barrajuego.png (1907x825, medidas pixel a pixel):
    private const float TRACK_X_INICIO = 0.245f;
    private const float TRACK_X_FIN    = 0.916f;
    private const float TRACK_Y_INICIO = 0.390f;
    private const float TRACK_Y_FIN    = 0.621f;

    // -----------------------------------------------------------------------
    // Estado
    // -----------------------------------------------------------------------
    public string escenaRetorno = "nivel_1_ypf";

    // Dificultad calculada en IniciarMinijuego()
    private float velocidadActual;
    private float zonaVerdeAncho;

    private float posIndicador = 0f;
    private float dirIndicador = 1f;
    private float zonaVerdeInicio;
    private bool  juegoActivo  = false;
    private bool  yaPresionado = false;

    // Vidas (solo nivel 5 tiene 3 vidas, el resto 1)
    private int   vidasTotales;
    private int   vidasRestantes;

    // -----------------------------------------------------------------------
    // Referencias UI del minijuego (fase 2)
    // -----------------------------------------------------------------------
    private RectTransform    indicadorRT;
    private TextMeshProUGUI  txtResultado;
    private TextMeshProUGUI  txtInstruccion;
    private TextMeshProUGUI  txtVidas;      // corazones ❤️
    private RectTransform    zonaVerdeRT;  // para repositioning entre intentos

    // -----------------------------------------------------------------------
    // Level 1 custom fields
    // -----------------------------------------------------------------------
    private bool isNivel1 = false;
    private float progressNivel1 = 0f;
    private float timerNivel1 = 12f;
    private bool isNivel1Active = false;
    private RectTransform fillNivel1RT;
    private TextMeshProUGUI txtTimerNivel1;
    private TextMeshProUGUI txtProgressPercentNivel1;

    // -----------------------------------------------------------------------
    // Level 2 custom fields
    // -----------------------------------------------------------------------
    private bool isNivel2 = false;
    private int aciertosNivel2 = 0;
    private float timerNivel2 = 15f;
    private bool isNivel2Active = false;
    private RectTransform fillNivel2RT;
    private TextMeshProUGUI txtTimerNivel2;
    private TextMeshProUGUI txtProgressPercentNivel2;
    private RectTransform trackNivel2RT;
    private RectTransform indicatorNivel2RT;
    private RectTransform greenZoneNivel2RT;
    private float posIndicadorNivel2 = 0f;
    private float dirIndicadorNivel2 = 1f;
    private float greenZoneInicioNivel2 = 0.3f;
    private float greenZoneAnchoNivel2 = 0.16f;
    private float velocidadNivel2 = 0.85f;
    private bool yaPresionadoNivel2 = false;

    // -----------------------------------------------------------------------
    // Level 3 custom fields
    // -----------------------------------------------------------------------
    private bool isNivel3 = false;
    private int aciertosNivel3 = 0;
    private float timerNivel3 = 15f;
    private bool isNivel3Active = false;
    private RectTransform fillNivel3RT;
    private TextMeshProUGUI txtTimerNivel3;
    private TextMeshProUGUI txtProgressPercentNivel3;
    private RectTransform trackNivel3RT;
    private RectTransform indicatorNivel3RT;
    private RectTransform greenZoneNivel3RT;
    private float posIndicadorNivel3 = 0f;
    private float greenZoneInicioNivel3 = 0.55f;
    private float greenZoneAnchoNivel3 = 0.16f;
    private float velocidadNivel3 = 1.3f;
    private bool yaPresionadoNivel3 = false;

    // -----------------------------------------------------------------------
    // Level 4 custom fields
    // -----------------------------------------------------------------------
    private bool isNivel4 = false;
    private int aciertosNivel4 = 0;
    private float timerNivel4 = 15f;
    private bool isNivel4Active = false;
    private RectTransform fillNivel4RT;
    private TextMeshProUGUI txtTimerNivel4;
    private TextMeshProUGUI txtProgressPercentNivel4;
    private float waitTimerNivel4 = 0f;
    private bool senalMostradaNivel4 = false;
    private float signalActiveTimeNivel4 = 0f;
    private bool feedbackActivoNivel4 = false;
    private bool spaceWasPressedNivel4 = false; // manual key tracking

    // -----------------------------------------------------------------------
    // Level 5 custom fields
    // -----------------------------------------------------------------------
    private bool isNivel5 = false;
    private bool isNivel5Active = false;
    private float timerNivel5 = 15f;
    private float holdTimeNivel5 = 0f;         // accumulated seconds inside green zone
    private const float HOLD_GOAL_NIVEL5 = 5f; // must hold 5 seconds total
    private RectTransform fillNivel5RT;
    private RectTransform indicadorNivel5RT;
    private RectTransform zonaVerdeNivel5RT;
    private TextMeshProUGUI txtTimerNivel5;
    private TextMeshProUGUI txtHoldNivel5;
    private float posIndicadorNivel5 = 0.5f;
    private float driftDirNivel5 = 1f;
    private float driftSpeedNivel5 = 0.65f;
    private float changeDriftNivel5Timer = 0f;
    private const float ZONA_CENTRO = 0.5f;
    private const float ZONA_HALF  = 0.11f;   // green zone half-width (22% total)
    private bool spaceHeldNivel5 = false;      // manual key tracking

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnEscenaCargada;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnEscenaCargada;
    }

    private void OnEscenaCargada(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "escenatrueque") return;
        SceneManager.sceneLoaded -= OnEscenaCargada;
        StartCoroutine(EsperarDialogoManager());
    }

    // =====================================================================
    // FASE 1: esperar a DialogoManager y modificar su UI
    // =====================================================================
    private IEnumerator EsperarDialogoManager()
    {
        yield return null; // frame 1: Start() corre
        yield return null; // frame 2: primer Update, UI completamente lista

        DialogoManager dm = FindAnyObjectByType<DialogoManager>();
        if (dm == null)
        {
            IniciarMinijuego();
            yield break;
        }

        int nivel = ExtraerNumeroNivel(escenaRetorno);

        dm.ActivarModoMinijuego(
            onContinuar: (nivel >= 1 && nivel <= 5) ? MostrarAdvertenciaIntro : IniciarMinijuego,
            onRechazar:  () =>
            {
                Time.timeScale = 1f;
                string destino = string.IsNullOrEmpty(escenaRetorno) ? "mapa" : escenaRetorno;
                Destroy(gameObject);
                SceneManager.LoadScene(destino);
            }
        );
    }

    private void MostrarAdvertenciaIntro()
    {
        int nivel = ExtraerNumeroNivel(escenaRetorno);
        string npcName = (nivel == 5) ? "Lucas y Gonza" : (nivel == 4) ? "Carla" : (nivel == 3) ? "Enrique" : (nivel == 2) ? "Sergio" : "Roxana";
        string distraidoText = (nivel == 1 || nivel == 4) ? "distraída" : "distraídos";

        // ── Canvas principal ─────────────────────────────────────────────
        GameObject canvasGO = new GameObject("IntroNivelCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300; // Above dialog canvas
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Fondo que opaque el trueque ──────────────────────────────────
        GameObject fondo = UIPanel(canvasGO.transform, "Fondo", new Color(0f, 0f, 0f, 0.80f));
        FullStretch(fondo.GetComponent<RectTransform>());

        // ── Panel Central ────────────────────────────────────────────────
        GameObject panel = UIPanel(canvasGO.transform, "Panel", new Color(0.12f, 0.12f, 0.14f, 0.96f));
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(700f, 320f);
        panelRT.anchoredPosition = Vector2.zero;

        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.8f, 0.8f, 0.6f);
        outline.effectDistance = new Vector2(2f, 2f);

        // ── Texto de Advertencia ─────────────────────────────────────────
        GameObject txtGO = new GameObject("TxtAdvertencia", typeof(RectTransform));
        txtGO.transform.SetParent(panel.transform, false);
        TextMeshProUGUI txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text = $"<color=#FFAA00>{npcName}</color> está {distraidoText} con el trueque.\n\nExtraé la nafta de su bidón antes de que termine el tiempo.";
        txt.fontSize = 26f;
        txt.fontStyle = FontStyles.Bold;
        txt.color = new Color(0.95f, 0.95f, 0.9f, 1f);
        txt.alignment = TextAlignmentOptions.Center;
        
        RectTransform txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = new Vector2(0.05f, 0.30f);
        txtRT.anchorMax = new Vector2(0.95f, 0.95f);
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;

        // ── Botón Continuar ──────────────────────────────────────────────
        GameObject btnGO = new GameObject("BtnContinuar");
        btnGO.transform.SetParent(panel.transform, false);
        RectTransform btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0f);
        btnRT.anchorMax = new Vector2(0.5f, 0f);
        btnRT.pivot = new Vector2(0.5f, 0f);
        btnRT.anchoredPosition = new Vector2(0f, 30f);
        btnRT.sizeDelta = new Vector2(200f, 48f);

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 0.9f, 1f); // Blue premium button
        Outline btnOutline = btnGO.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.08f, 0.08f, 0.08f, 0.8f);
        btnOutline.effectDistance = new Vector2(1f, 1f);

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.2f, 0.6f, 0.9f, 1f);
        cb.highlightedColor = new Color(0.3f, 0.7f, 1.0f, 1f);
        cb.pressedColor = new Color(0.15f, 0.5f, 0.8f, 1f);
        btn.colors = cb;

        GameObject btnTxtGO = new GameObject("Text");
        btnTxtGO.transform.SetParent(btnGO.transform, false);
        TextMeshProUGUI btnTxt = btnTxtGO.AddComponent<TextMeshProUGUI>();
        btnTxt.text = "CONTINUAR";
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.fontSize = 16f;
        btnTxt.color = Color.white;

        RectTransform btnTxtRT = btnTxtGO.GetComponent<RectTransform>();
        btnTxtRT.anchorMin = Vector2.zero;
        btnTxtRT.anchorMax = Vector2.one;
        btnTxtRT.offsetMin = btnTxtRT.offsetMax = Vector2.zero;

        btn.onClick.AddListener(() =>
        {
            Destroy(canvasGO);
            IniciarMinijuego();
        });
    }

    // =====================================================================
    // FASE 2: minijuego de timing
    // =====================================================================
    private void IniciarMinijuego()
    {
        // Calcular dificultad según el nivel actual
        int nivel = ExtraerNumeroNivel(escenaRetorno);
        int idx   = Mathf.Clamp(nivel - 1, 0, VELOCIDADES.Length - 1);

        velocidadActual = VELOCIDADES[idx];
        zonaVerdeAncho  = ZONAS_VERDES[idx];

        // Vidas: nivel 5 tiene 3 vidas, el resto 1
        vidasTotales   = (nivel == 5) ? 3 : 1;
        vidasRestantes = vidasTotales;

        // Ocultar la UI del diálogo
        Canvas[] todos = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in todos) c.gameObject.SetActive(false);

        BuildMinijuego();
    }

    /// <summary>
    /// Extrae el número de nivel del nombre de escena.
    /// "nivel_1_ypf" → 1,  "nivel_3_xxx" → 3, desconocido → 1
    /// </summary>
    private static int ExtraerNumeroNivel(string nombreEscena)
    {
        if (string.IsNullOrEmpty(nombreEscena)) return 1;
        // Buscar el primer dígito después de "nivel_"
        var match = System.Text.RegularExpressions.Regex.Match(
            nombreEscena.ToLower(), @"nivel_(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int n))
            return Mathf.Clamp(n, 1, 6);
        return 1;
    }

    private void BuildMinijuego()
    {
        int nivel = ExtraerNumeroNivel(escenaRetorno);
        if (nivel == 1)
        {
            BuildMinijuegoNivel1();
            return;
        }
        if (nivel == 2)
        {
            BuildMinijuegoNivel2();
            return;
        }
        if (nivel == 3)
        {
            BuildMinijuegoNivel3();
            return;
        }
        if (nivel == 4)
        {
            BuildMinijuegoNivel4();
            return;
        }
        if (nivel == 5)
        {
            BuildMinijuegoNivel5();
            return;
        }

        // ── Canvas principal ─────────────────────────────────────────────
        GameObject canvasGO = new GameObject("MinijuegoCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Fondo oscuro ─────────────────────────────────────────────────
        GameObject fondo = UIPanel(canvasGO.transform, "Fondo", new Color(0f, 0f, 0f, 0.88f));
        FullStretch(fondo.GetComponent<RectTransform>());

        // ── Marco barrajuego.png ─────────────────────────────────────────
        Sprite marcoSprite = Resources.Load<Sprite>("Sprites/barrajuego");
        if (marcoSprite == null)
            Debug.LogWarning("[MinijuegoNafta] No se encontró Sprites/barrajuego en Resources.");

        float marcoW = 1920f * 0.70f;
        float marcoH = marcoSprite != null
            ? marcoW * marcoSprite.rect.height / marcoSprite.rect.width
            : marcoW / 2.3115f;

        // ── Track de juego ───────────────────────────────────────────────
        float trackW = marcoW * (TRACK_X_FIN - TRACK_X_INICIO);
        float trackH = marcoH * (TRACK_Y_FIN - TRACK_Y_INICIO);
        float imageCenterY = (TRACK_Y_INICIO + TRACK_Y_FIN) * 0.5f;
        float trackCenterX = marcoW * ((TRACK_X_INICIO + TRACK_X_FIN) * 0.5f - 0.5f);
        float trackCenterY = marcoH * (0.5f - imageCenterY);

        GameObject trackContainer = new GameObject("TrackContainer", typeof(RectTransform));
        trackContainer.transform.SetParent(canvasGO.transform, false);
        RectTransform tcRT = trackContainer.GetComponent<RectTransform>();
        tcRT.anchorMin = tcRT.anchorMax = new Vector2(0.5f, 0.5f);
        tcRT.sizeDelta        = new Vector2(trackW, trackH);
        tcRT.anchoredPosition = new Vector2(trackCenterX, trackCenterY);

        Image maskImg = trackContainer.AddComponent<Image>();
        maskImg.color = Color.white;
        maskImg.raycastTarget = false;
        Mask mask = trackContainer.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Fondo oscuro del track
        GameObject trackBgGO = UIPanel(trackContainer.transform, "TrackBg", new Color(0.07f, 0.05f, 0.03f));
        FullStretch(trackBgGO.GetComponent<RectTransform>());

        // Zona verde (tamaño dinámico según dificultad)
        zonaVerdeInicio = Random.Range(0.10f, 1f - zonaVerdeAncho - 0.06f);
        GameObject zonaGO = UIPanel(trackContainer.transform, "ZonaVerde",
                                    new Color(0.15f, 0.85f, 0.25f, 0.80f));
        RectTransform zonaRT = zonaGO.GetComponent<RectTransform>();
        zonaVerdeRT = zonaRT;  // guardar referencia para reposicionar entre intentos
        zonaRT.anchorMin = new Vector2(zonaVerdeInicio, 0.06f);
        zonaRT.anchorMax = new Vector2(zonaVerdeInicio + zonaVerdeAncho, 0.94f);
        zonaRT.offsetMin = zonaRT.offsetMax = Vector2.zero;
        Outline zonaOutline = zonaGO.AddComponent<Outline>();
        zonaOutline.effectColor    = new Color(0.4f, 1f, 0.4f);
        zonaOutline.effectDistance = new Vector2(2f, 2f);

        // Indicador (barra amarilla)
        GameObject indGO = UIPanel(trackContainer.transform, "Indicador", new Color(1f, 0.88f, 0.1f));
        indicadorRT = indGO.GetComponent<RectTransform>();
        indicadorRT.anchorMin = new Vector2(0f, 0.03f);
        indicadorRT.anchorMax = new Vector2(0f, 0.97f);
        indicadorRT.sizeDelta        = new Vector2(5f, 0f);
        indicadorRT.anchoredPosition = new Vector2(2.5f, 0f);

        // Marco encima del track
        if (marcoSprite != null)
        {
            GameObject marcoGO = new GameObject("Marco", typeof(RectTransform), typeof(Image));
            marcoGO.transform.SetParent(canvasGO.transform, false);
            Image marcoImg = marcoGO.GetComponent<Image>();
            marcoImg.sprite         = marcoSprite;
            marcoImg.preserveAspect = true;
            marcoImg.raycastTarget  = false;
            RectTransform marcoRT   = marcoGO.GetComponent<RectTransform>();
            marcoRT.anchorMin = marcoRT.anchorMax = new Vector2(0.5f, 0.5f);
            marcoRT.sizeDelta        = new Vector2(marcoW, marcoH);
            marcoRT.anchoredPosition = Vector2.zero;
        }

        // Texto de instrucción (con indicador de dificultad)
        nivel = ExtraerNumeroNivel(escenaRetorno);
        string dificultadTag = nivel >= 5 ? " <color=#FF4444>⚡ DIFÍCIL</color>"
                             : nivel >= 3 ? " <color=#FFAA00>⚡ MEDIO</color>"
                             : "";
        GameObject txtInstGO = new GameObject("TxtInstruccion", typeof(RectTransform));
        txtInstGO.transform.SetParent(canvasGO.transform, false);
        txtInstruccion = txtInstGO.AddComponent<TextMeshProUGUI>();
        txtInstruccion.text      = $"Si detenés la barra en la zona verde... ¡le robás la nafta!{dificultadTag}    Presioná  ESPACIO";
        txtInstruccion.fontSize  = 28f;
        txtInstruccion.color     = new Color(0.88f, 0.82f, 0.65f);
        txtInstruccion.alignment = TextAlignmentOptions.Center;
        txtInstruccion.enableWordWrapping = true;
        txtInstruccion.richText  = true;
        RectTransform txtInstRT = txtInstGO.GetComponent<RectTransform>();
        txtInstRT.anchorMin = new Vector2(0.10f, 0.24f);
        txtInstRT.anchorMax = new Vector2(0.90f, 0.37f);
        txtInstRT.offsetMin = txtInstRT.offsetMax = Vector2.zero;

        // Texto de resultado
        GameObject txtResGO = new GameObject("TxtResultado", typeof(RectTransform));
        txtResGO.transform.SetParent(canvasGO.transform, false);
        txtResultado = txtResGO.AddComponent<TextMeshProUGUI>();
        txtResultado.text      = "";
        txtResultado.fontSize  = 52f;
        txtResultado.fontStyle = FontStyles.Bold;
        txtResultado.color     = Color.white;
        txtResultado.alignment = TextAlignmentOptions.Center;
        txtResultado.enableWordWrapping = true;
        txtResultado.richText  = true;
        RectTransform txtResRT = txtResGO.GetComponent<RectTransform>();
        txtResRT.anchorMin = new Vector2(0.10f, 0.60f);
        txtResRT.anchorMax = new Vector2(0.90f, 0.78f);
        txtResRT.offsetMin = txtResRT.offsetMax = Vector2.zero;

        // Corazones (solo visibles si hay más de 1 vida)
        if (vidasTotales > 1)
        {
            GameObject txtVidasGO = new GameObject("TxtVidas", typeof(RectTransform));
            txtVidasGO.transform.SetParent(canvasGO.transform, false);
            txtVidas = txtVidasGO.AddComponent<TextMeshProUGUI>();
            txtVidas.fontSize  = 42f;
            txtVidas.alignment = TextAlignmentOptions.Center;
            txtVidas.richText  = true;
            txtVidas.text      = GenerarCorazones();
            RectTransform txtVidasRT = txtVidasGO.GetComponent<RectTransform>();
            txtVidasRT.anchorMin = new Vector2(0.30f, 0.14f);
            txtVidasRT.anchorMax = new Vector2(0.70f, 0.23f);
            txtVidasRT.offsetMin = txtVidasRT.offsetMax = Vector2.zero;
        }

        StartCoroutine(ArrancarJuego());
    }

    private IEnumerator ArrancarJuego()
    {
        yield return new WaitForSeconds(DELAY_INICIO);
        juegoActivo = true;
    }

    // =====================================================================
    // UPDATE
    // =====================================================================
    void Update()
    {
        if (isNivel1)
        {
            UpdateNivel1();
            return;
        }
        if (isNivel2)
        {
            UpdateNivel2();
            return;
        }
        if (isNivel3)
        {
            UpdateNivel3();
            return;
        }
        if (isNivel4)
        {
            UpdateNivel4();
            return;
        }
        if (isNivel5)
        {
            UpdateNivel5();
            return;
        }

        if (!juegoActivo || yaPresionado) return;

        // Mover indicador a la velocidad del nivel actual
        posIndicador += dirIndicador * velocidadActual * Time.deltaTime;
        if (posIndicador >= 1f) { posIndicador = 1f; dirIndicador = -1f; }
        if (posIndicador <= 0f) { posIndicador = 0f; dirIndicador =  1f; }

        indicadorRT.anchorMin = new Vector2(posIndicador, indicadorRT.anchorMin.y);
        indicadorRT.anchorMax = new Vector2(posIndicador, indicadorRT.anchorMax.y);
        indicadorRT.anchoredPosition = new Vector2(2.5f, 0f);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            yaPresionado = true;
            juegoActivo  = false;
            StartCoroutine(MostrarResultado());
        }
    }

    // =====================================================================
    // LEVEL 1 SPECIFIC IMPLEMENTATION
    // =====================================================================
    private void BuildMinijuegoNivel1()
    {
        isNivel1 = true;
        progressNivel1 = 0f;
        timerNivel1 = 12f;
        isNivel1Active = false;

        // ── Canvas principal ─────────────────────────────────────────────
        GameObject canvasGO = new GameObject("MinijuegoCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Fondo oscuro ─────────────────────────────────────────────────
        GameObject fondo = UIPanel(canvasGO.transform, "Fondo", new Color(0f, 0f, 0f, 0.95f));
        FullStretch(fondo.GetComponent<RectTransform>());

        // ── Background Robonafta ─────────────────────────────────────────
        Sprite bgSprite = Resources.Load<Sprite>("Sprites/robonafta1");
        if (bgSprite != null)
        {
            GameObject bgGO = new GameObject("RoboNaftaBg", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(canvasGO.transform, false);
            Image bgImg = bgGO.GetComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.preserveAspect = true;
            bgImg.raycastTarget = false;
            
            RectTransform bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0.05f, 0.15f);
            bgRT.anchorMax = new Vector2(0.95f, 0.95f);
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        }

        // ── Progress Bar Container ───────────────────────────────────────
        GameObject barContainer = new GameObject("BarContainer", typeof(RectTransform));
        barContainer.transform.SetParent(canvasGO.transform, false);
        RectTransform bcRT = barContainer.GetComponent<RectTransform>();
        bcRT.anchorMin = bcRT.anchorMax = new Vector2(0.5f, 0f);
        bcRT.pivot = new Vector2(0.5f, 0f);
        bcRT.anchoredPosition = new Vector2(0f, 90f);
        bcRT.sizeDelta = new Vector2(800f, 40f);

        Image bgBarImg = barContainer.AddComponent<Image>();
        bgBarImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        Outline outlineBar = barContainer.AddComponent<Outline>();
        outlineBar.effectColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        outlineBar.effectDistance = new Vector2(2f, 2f);

        // Fill Image
        GameObject fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(barContainer.transform, false);
        Image fillImg = fillGO.GetComponent<Image>();
        fillImg.color = new Color(0.2f, 0.8f, 0.3f, 0.9f); // Green fill
        fillNivel1RT = fillGO.GetComponent<RectTransform>();
        fillNivel1RT.anchorMin = new Vector2(0f, 0f);
        fillNivel1RT.anchorMax = new Vector2(0f, 1f); // starts at 0%
        fillNivel1RT.offsetMin = fillNivel1RT.offsetMax = Vector2.zero;

        // ── Text Percent ───────────────────────────────────────────────
        GameObject txtPercentGO = new GameObject("TxtPercent", typeof(RectTransform));
        txtPercentGO.transform.SetParent(barContainer.transform, false);
        txtProgressPercentNivel1 = txtPercentGO.AddComponent<TextMeshProUGUI>();
        txtProgressPercentNivel1.text = "0%";
        txtProgressPercentNivel1.fontSize = 20f;
        txtProgressPercentNivel1.fontStyle = FontStyles.Bold;
        txtProgressPercentNivel1.color = Color.white;
        txtProgressPercentNivel1.alignment = TextAlignmentOptions.Center;
        
        RectTransform tpRT = txtPercentGO.GetComponent<RectTransform>();
        tpRT.anchorMin = Vector2.zero;
        tpRT.anchorMax = Vector2.one;
        tpRT.offsetMin = tpRT.offsetMax = Vector2.zero;

        // ── Timer Text ───────────────────────────────────────────────
        GameObject txtTimerGO = new GameObject("TxtTimer", typeof(RectTransform));
        txtTimerGO.transform.SetParent(canvasGO.transform, false);
        txtTimerNivel1 = txtTimerGO.AddComponent<TextMeshProUGUI>();
        txtTimerNivel1.text = "TIEMPO RESTANTE: 12.0s";
        txtTimerNivel1.fontSize = 32f;
        txtTimerNivel1.fontStyle = FontStyles.Bold;
        txtTimerNivel1.color = new Color(1f, 0.88f, 0.1f);
        txtTimerNivel1.alignment = TextAlignmentOptions.Center;

        RectTransform ttRT = txtTimerGO.GetComponent<RectTransform>();
        ttRT.anchorMin = new Vector2(0.5f, 0f);
        ttRT.anchorMax = new Vector2(0.5f, 0f);
        ttRT.pivot = new Vector2(0.5f, 0f);
        ttRT.anchoredPosition = new Vector2(0f, 140f);
        ttRT.sizeDelta = new Vector2(600f, 50f);

        // ── Instruction Text ─────────────────────────────────────────────
        GameObject txtInstGO = new GameObject("TxtInstruccion", typeof(RectTransform));
        txtInstGO.transform.SetParent(canvasGO.transform, false);
        txtInstruccion = txtInstGO.AddComponent<TextMeshProUGUI>();
        txtInstruccion.text = "¡PRESIONÁ <color=#FFAA00>ESPACIO</color> REPETIDAMENTE PARA LLENAR EL TANQUE!";
        txtInstruccion.fontSize = 24f;
        txtInstruccion.fontStyle = FontStyles.Bold;
        txtInstruccion.color = new Color(0.88f, 0.82f, 0.65f);
        txtInstruccion.alignment = TextAlignmentOptions.Center;

        RectTransform tiRT = txtInstGO.GetComponent<RectTransform>();
        tiRT.anchorMin = new Vector2(0.5f, 0f);
        tiRT.anchorMax = new Vector2(0.5f, 0f);
        tiRT.pivot = new Vector2(0.5f, 0f);
        tiRT.anchoredPosition = new Vector2(0f, 40f);
        tiRT.sizeDelta = new Vector2(1000f, 40f);

        // ── Text Resultado ───────────────────────────────────────────────
        GameObject txtResGO = new GameObject("TxtResultado", typeof(RectTransform));
        txtResGO.transform.SetParent(canvasGO.transform, false);
        txtResultado = txtResGO.AddComponent<TextMeshProUGUI>();
        txtResultado.text = "";
        txtResultado.fontSize = 56f;
        txtResultado.fontStyle = FontStyles.Bold;
        txtResultado.color = Color.white;
        txtResultado.alignment = TextAlignmentOptions.Center;

        RectTransform trRT = txtResGO.GetComponent<RectTransform>();
        trRT.anchorMin = new Vector2(0.1f, 0.45f);
        trRT.anchorMax = new Vector2(0.9f, 0.65f);
        trRT.offsetMin = trRT.offsetMax = Vector2.zero;

        StartCoroutine(ArrancarJuegoNivel1());
    }

    private IEnumerator ArrancarJuegoNivel1()
    {
        yield return new WaitForSeconds(DELAY_INICIO);
        isNivel1Active = true;
    }

    private void UpdateNivel1()
    {
        if (!isNivel1Active) return;

        // Decrement timer
        timerNivel1 -= Time.deltaTime;
        if (timerNivel1 <= 0f)
        {
            timerNivel1 = 0f;
            isNivel1Active = false;
            StartCoroutine(MostrarResultadoNivel1(false));
        }

        // Drain progress over time (constantly goes back a bit)
        progressNivel1 = Mathf.Max(0f, progressNivel1 - 0.12f * Time.deltaTime);

        // Advance progress on spacebar press
        if (Input.GetKeyDown(KeyCode.Space))
        {
            progressNivel1 = Mathf.Min(1f, progressNivel1 + 0.075f);
            if (progressNivel1 >= 1f)
            {
                isNivel1Active = false;
                StartCoroutine(MostrarResultadoNivel1(true));
            }
        }

        // Update UI elements
        if (fillNivel1RT != null)
        {
            fillNivel1RT.anchorMax = new Vector2(progressNivel1, 1f);
        }
        if (txtProgressPercentNivel1 != null)
        {
            txtProgressPercentNivel1.text = $"{(progressNivel1 * 100f):F0}%";
        }
        if (txtTimerNivel1 != null)
        {
            txtTimerNivel1.text = $"TIEMPO RESTANTE: {timerNivel1:F1}s";
        }
    }

    private IEnumerator MostrarResultadoNivel1(bool gano)
    {
        if (txtInstruccion != null) txtInstruccion.text = "";
        if (txtTimerNivel1 != null) txtTimerNivel1.text = "";

        if (gano)
        {
            InventoryManager.CurrentFuel = 1.0f;
            NPCInteraction.MarcarTratoAceptado(NPCInteraction.lastInteractedNPC);
            txtResultado.color = new Color(0.2f, 1f, 0.35f);
            txtResultado.text = "¡Le robaste la nafta!\n¡Tanque lleno al 100%!";
            yield return new WaitForSeconds(DELAY_RESULTADO);
            TerminarMinijuego();
        }
        else
        {
            txtResultado.color = new Color(1f, 0.25f, 0.2f);
            txtResultado.text = "¡Fallaste!\nNo conseguiste la nafta.";
            NPCInteraction.MarcarTratoAceptado(NPCInteraction.lastInteractedNPC);
            yield return new WaitForSeconds(DELAY_RESULTADO);
            TerminarMinijuego();
        }
    }

    // =====================================================================
    // RESULTADO
    // =====================================================================
    private IEnumerator MostrarResultado()
    {
        if (txtInstruccion != null) txtInstruccion.text = "";

        bool enZona = posIndicador >= zonaVerdeInicio &&
                      posIndicador <= zonaVerdeInicio + zonaVerdeAncho;

        if (enZona)
        {
            // ✅ GANO — marcar trato aceptado para que el NPC desaparezca al volver
            InventoryManager.CurrentFuel = 1.0f;
            NPCInteraction.MarcarTratoAceptado(NPCInteraction.lastInteractedNPC);
            txtResultado.color = new Color(0.2f, 1f, 0.35f);
            txtResultado.text  = "¡Le robaste la nafta!\n¡Tanque lleno al 100%!";
            yield return new WaitForSeconds(DELAY_RESULTADO);
            TerminarMinijuego();
        }
        else
        {
            // ❌ FALLÓ
            vidasRestantes--;

            if (vidasRestantes <= 0)
            {
                // Sin vidas — derrota final
                txtResultado.color = new Color(1f, 0.25f, 0.2f);
                txtResultado.text  = "¡Fallaste!\nNo conseguiste la nafta.";
                if (txtVidas != null) txtVidas.text = GenerarCorazones(); // todos vacíos

                // El NPC desaparece del nivel al quedarse sin vidas
                NPCInteraction.MarcarTratoAceptado(NPCInteraction.lastInteractedNPC);

                yield return new WaitForSeconds(DELAY_RESULTADO);
                TerminarMinijuego();
            }
            else
            {
                // Quedan vidas — mostrar flash breve y reiniciar intento
                txtResultado.color = new Color(1f, 0.35f, 0.2f);
                txtResultado.text  = $"¡Fallaste! \nQuedan {vidasRestantes} intento{(vidasRestantes == 1 ? "" : "s")}...";
                if (txtVidas != null) txtVidas.text = GenerarCorazones();

                yield return new WaitForSeconds(1.4f);

                // Reiniciar indicador y zona verde
                txtResultado.text = "";
                posIndicador      = 0f;
                dirIndicador      = 1f;
                zonaVerdeInicio   = Random.Range(0.10f, 1f - zonaVerdeAncho - 0.06f);

                // Actualizar posición de la zona verde en la UI
                if (zonaVerdeRT != null)
                {
                    zonaVerdeRT.anchorMin = new Vector2(zonaVerdeInicio, 0.06f);
                    zonaVerdeRT.anchorMax = new Vector2(zonaVerdeInicio + zonaVerdeAncho, 0.94f);
                    zonaVerdeRT.offsetMin = zonaVerdeRT.offsetMax = Vector2.zero;
                }

                // Restaurar instrucción y reanudar
                if (txtInstruccion != null)
                    txtInstruccion.text = "Si detenés la barra en la zona verde... ¡le robás la nafta!  <color=#FF4444>⚡ DIFÍCIL</color>    Presioná  ESPACIO";

                yield return new WaitForSeconds(0.4f);
                yaPresionado = false;
                juegoActivo  = true;
            }
        }
    }

    private void TerminarMinijuego()
    {
        Time.timeScale = 1f;
        string destino = string.IsNullOrEmpty(escenaRetorno) ? "mapa" : escenaRetorno;
        Destroy(gameObject);
        SceneManager.LoadScene(destino);
    }

    /// <summary>Genera la cadena de corazones según vidas restantes/totales.</summary>
    private string GenerarCorazones()
    {
        string lleno  = "<color=#FF3333>♥</color>";
        string vacio  = "<color=#555555>♥</color>";
        string result = "";
        for (int i = 0; i < vidasTotales; i++)
            result += (i < vidasRestantes ? lleno : vacio) + "  ";
        return result.TrimEnd();
    }

    // =====================================================================
    // LEVEL 2 SPECIFIC IMPLEMENTATION
    // =====================================================================
    private void BuildMinijuegoNivel2()
    {
        isNivel2 = true;
        aciertosNivel2 = 0;
        timerNivel2 = 15f;
        isNivel2Active = false;

        // ── Canvas principal ─────────────────────────────────────────────
        GameObject canvasGO = new GameObject("MinijuegoCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Fondo oscuro ─────────────────────────────────────────────────
        GameObject fondo = UIPanel(canvasGO.transform, "Fondo", new Color(0f, 0f, 0f, 0.95f));
        FullStretch(fondo.GetComponent<RectTransform>());

        // ── Background Robonafta 2 ───────────────────────────────────────
        Sprite bgSprite = Resources.Load<Sprite>("Sprites/robonafta2");
        if (bgSprite != null)
        {
            GameObject bgGO = new GameObject("RoboNaftaBg", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(canvasGO.transform, false);
            Image bgImg = bgGO.GetComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.preserveAspect = true;
            bgImg.raycastTarget = false;
            
            RectTransform bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0.05f, 0.15f);
            bgRT.anchorMax = new Vector2(0.95f, 0.95f);
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        }

        // ── Progress Bar Container (Bidón verde) ─────────────────────────
        GameObject barContainer = new GameObject("BarContainer", typeof(RectTransform));
        barContainer.transform.SetParent(canvasGO.transform, false);
        RectTransform bcRT = barContainer.GetComponent<RectTransform>();
        bcRT.anchorMin = bcRT.anchorMax = new Vector2(0.5f, 0f);
        bcRT.pivot = new Vector2(0.5f, 0f);
        bcRT.anchoredPosition = new Vector2(0f, 90f);
        bcRT.sizeDelta = new Vector2(800f, 40f);

        Image bgBarImg = barContainer.AddComponent<Image>();
        bgBarImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        Outline outlineBar = barContainer.AddComponent<Outline>();
        outlineBar.effectColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        outlineBar.effectDistance = new Vector2(2f, 2f);

        // Fill Image
        GameObject fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(barContainer.transform, false);
        Image fillImg = fillGO.GetComponent<Image>();
        fillImg.color = new Color(0.2f, 0.8f, 0.3f, 0.9f); // Green fill
        fillNivel2RT = fillGO.GetComponent<RectTransform>();
        fillNivel2RT.anchorMin = new Vector2(0f, 0f);
        fillNivel2RT.anchorMax = new Vector2(0f, 1f); // starts at 0%
        fillNivel2RT.offsetMin = fillNivel2RT.offsetMax = Vector2.zero;

        // ── Text Percent ───────────────────────────────────────────────
        GameObject txtPercentGO = new GameObject("TxtPercent", typeof(RectTransform));
        txtPercentGO.transform.SetParent(barContainer.transform, false);
        txtProgressPercentNivel2 = txtPercentGO.AddComponent<TextMeshProUGUI>();
        txtProgressPercentNivel2.text = "0% (0/2 Aciertos)";
        txtProgressPercentNivel2.fontSize = 20f;
        txtProgressPercentNivel2.fontStyle = FontStyles.Bold;
        txtProgressPercentNivel2.color = Color.white;
        txtProgressPercentNivel2.alignment = TextAlignmentOptions.Center;
        
        RectTransform tpRT = txtPercentGO.GetComponent<RectTransform>();
        tpRT.anchorMin = Vector2.zero;
        tpRT.anchorMax = Vector2.one;
        tpRT.offsetMin = tpRT.offsetMax = Vector2.zero;

        // ── Timing Track Container ───────────────────────────────────────
        GameObject trackContainer = new GameObject("TrackContainer", typeof(RectTransform));
        trackContainer.transform.SetParent(canvasGO.transform, false);
        trackNivel2RT = trackContainer.GetComponent<RectTransform>();
        trackNivel2RT.anchorMin = trackNivel2RT.anchorMax = new Vector2(0.5f, 0f);
        trackNivel2RT.pivot = new Vector2(0.5f, 0f);
        trackNivel2RT.anchoredPosition = new Vector2(0f, 170f);
        trackNivel2RT.sizeDelta = new Vector2(800f, 50f);

        Image bgTrackImg = trackContainer.AddComponent<Image>();
        bgTrackImg.color = new Color(0.15f, 0.12f, 0.1f, 0.95f);
        Outline outlineTrack = trackContainer.AddComponent<Outline>();
        outlineTrack.effectColor = new Color(0.4f, 0.3f, 0.2f, 0.8f);
        outlineTrack.effectDistance = new Vector2(2f, 2f);

        // Green Zone
        greenZoneInicioNivel2 = Random.Range(0.10f, 1f - greenZoneAnchoNivel2 - 0.10f);
        GameObject greenZoneGO = UIPanel(trackContainer.transform, "GreenZone", new Color(0.2f, 0.85f, 0.3f, 0.8f));
        greenZoneNivel2RT = greenZoneGO.GetComponent<RectTransform>();
        greenZoneNivel2RT.anchorMin = new Vector2(greenZoneInicioNivel2, 0.05f);
        greenZoneNivel2RT.anchorMax = new Vector2(greenZoneInicioNivel2 + greenZoneAnchoNivel2, 0.95f);
        greenZoneNivel2RT.offsetMin = greenZoneNivel2RT.offsetMax = Vector2.zero;
        Outline gzOutline = greenZoneGO.AddComponent<Outline>();
        gzOutline.effectColor = new Color(0.4f, 1f, 0.4f);
        gzOutline.effectDistance = new Vector2(2f, 2f);

        // Indicator line
        GameObject indGO = UIPanel(trackContainer.transform, "Indicator", new Color(1f, 0.9f, 0.1f));
        indicatorNivel2RT = indGO.GetComponent<RectTransform>();
        indicatorNivel2RT.anchorMin = new Vector2(0f, 0.02f);
        indicatorNivel2RT.anchorMax = new Vector2(0f, 0.98f);
        indicatorNivel2RT.sizeDelta = new Vector2(8f, 0f); // 8px thickness
        indicatorNivel2RT.anchoredPosition = new Vector2(4f, 0f);

        // ── Timer Text ───────────────────────────────────────────────
        GameObject txtTimerGO = new GameObject("TxtTimer", typeof(RectTransform));
        txtTimerGO.transform.SetParent(canvasGO.transform, false);
        txtTimerNivel2 = txtTimerGO.AddComponent<TextMeshProUGUI>();
        txtTimerNivel2.text = "TIEMPO RESTANTE: 15.0s";
        txtTimerNivel2.fontSize = 32f;
        txtTimerNivel2.fontStyle = FontStyles.Bold;
        txtTimerNivel2.color = new Color(1f, 0.88f, 0.1f);
        txtTimerNivel2.alignment = TextAlignmentOptions.Center;

        RectTransform ttRT = txtTimerGO.GetComponent<RectTransform>();
        ttRT.anchorMin = new Vector2(0.5f, 0f);
        ttRT.anchorMax = new Vector2(0.5f, 0f);
        ttRT.pivot = new Vector2(0.5f, 0f);
        ttRT.anchoredPosition = new Vector2(0f, 250f);
        ttRT.sizeDelta = new Vector2(600f, 50f);

        // ── Instruction Text ─────────────────────────────────────────────
        GameObject txtInstGO = new GameObject("TxtInstruccion", typeof(RectTransform));
        txtInstGO.transform.SetParent(canvasGO.transform, false);
        txtInstruccion = txtInstGO.AddComponent<TextMeshProUGUI>();
        txtInstruccion.text = "¡PRESIONÁ <color=#FFAA00>ESPACIO</color> CUANDO EL INDICADOR ESTÉ EN LA ZONA VERDE!";
        txtInstruccion.fontSize = 24f;
        txtInstruccion.fontStyle = FontStyles.Bold;
        txtInstruccion.color = new Color(0.88f, 0.82f, 0.65f);
        txtInstruccion.alignment = TextAlignmentOptions.Center;

        RectTransform tiRT = txtInstGO.GetComponent<RectTransform>();
        tiRT.anchorMin = new Vector2(0.5f, 0f);
        tiRT.anchorMax = new Vector2(0.5f, 0f);
        tiRT.pivot = new Vector2(0.5f, 0f);
        tiRT.anchoredPosition = new Vector2(0f, 30f);
        tiRT.sizeDelta = new Vector2(1200f, 40f);

        // ── Text Resultado ───────────────────────────────────────────────
        GameObject txtResGO = new GameObject("TxtResultado", typeof(RectTransform));
        txtResGO.transform.SetParent(canvasGO.transform, false);
        txtResultado = txtResGO.AddComponent<TextMeshProUGUI>();
        txtResultado.text = "";
        txtResultado.fontSize = 56f;
        txtResultado.fontStyle = FontStyles.Bold;
        txtResultado.color = Color.white;
        txtResultado.alignment = TextAlignmentOptions.Center;

        RectTransform trRT = txtResGO.GetComponent<RectTransform>();
        trRT.anchorMin = new Vector2(0.1f, 0.45f);
        trRT.anchorMax = new Vector2(0.9f, 0.65f);
        trRT.offsetMin = trRT.offsetMax = Vector2.zero;

        StartCoroutine(ArrancarJuegoNivel2());
    }

    private IEnumerator ArrancarJuegoNivel2()
    {
        yield return new WaitForSeconds(DELAY_INICIO);
        isNivel2Active = true;
    }

    private void UpdateNivel2()
    {
        if (!isNivel2Active) return;

        // Decrement timer
        timerNivel2 -= Time.deltaTime;
        if (timerNivel2 <= 0f)
        {
            timerNivel2 = 0f;
            isNivel2Active = false;
            StartCoroutine(MostrarResultadoNivel2(false));
        }

        // Move indicator
        if (!yaPresionadoNivel2)
        {
            posIndicadorNivel2 += dirIndicadorNivel2 * velocidadNivel2 * Time.deltaTime;
            if (posIndicadorNivel2 >= 1f) { posIndicadorNivel2 = 1f; dirIndicadorNivel2 = -1f; }
            if (posIndicadorNivel2 <= 0f) { posIndicadorNivel2 = 0f; dirIndicadorNivel2 =  1f; }

            if (indicatorNivel2RT != null)
            {
                indicatorNivel2RT.anchorMin = new Vector2(posIndicadorNivel2, indicatorNivel2RT.anchorMin.y);
                indicatorNivel2RT.anchorMax = new Vector2(posIndicadorNivel2, indicatorNivel2RT.anchorMax.y);
                indicatorNivel2RT.anchoredPosition = new Vector2(4f, 0f);
            }
        }

        // Tap check
        if (Input.GetKeyDown(KeyCode.Space) && !yaPresionadoNivel2)
        {
            yaPresionadoNivel2 = true;
            bool enZona = posIndicadorNivel2 >= greenZoneInicioNivel2 &&
                          posIndicadorNivel2 <= greenZoneInicioNivel2 + greenZoneAnchoNivel2;

            if (enZona)
            {
                aciertosNivel2++;
                if (aciertosNivel2 >= 2)
                {
                    isNivel2Active = false;
                    StartCoroutine(MostrarResultadoNivel2(true));
                }
                else
                {
                    StartCoroutine(FlashAciertoNivel2());
                }
            }
            else
            {
                StartCoroutine(FlashFalloNivel2());
            }
        }

        // Update UI elements
        if (txtTimerNivel2 != null)
        {
            txtTimerNivel2.text = $"TIEMPO RESTANTE: {timerNivel2:F1}s";
        }
    }

    private IEnumerator FlashAciertoNivel2()
    {
        // Update bar fill
        float progress = (float)aciertosNivel2 / 2f;
        if (fillNivel2RT != null) fillNivel2RT.anchorMax = new Vector2(progress, 1f);
        if (txtProgressPercentNivel2 != null) txtProgressPercentNivel2.text = $"{Mathf.RoundToInt(progress * 100f)}% ({aciertosNivel2}/2 Aciertos)";

        if (txtResultado != null)
        {
            txtResultado.color = new Color(0.2f, 1f, 0.35f);
            txtResultado.text = "¡ACIERTO!";
        }

        yield return new WaitForSeconds(0.8f);

        if (txtResultado != null) txtResultado.text = "";

        // Reset indicator and randomize green zone
        posIndicadorNivel2 = 0f;
        dirIndicadorNivel2 = 1f;
        greenZoneInicioNivel2 = Random.Range(0.10f, 1f - greenZoneAnchoNivel2 - 0.10f);
        if (greenZoneNivel2RT != null)
        {
            greenZoneNivel2RT.anchorMin = new Vector2(greenZoneInicioNivel2, 0.05f);
            greenZoneNivel2RT.anchorMax = new Vector2(greenZoneInicioNivel2 + greenZoneAnchoNivel2, 0.95f);
            greenZoneNivel2RT.offsetMin = greenZoneNivel2RT.offsetMax = Vector2.zero;
        }

        yaPresionadoNivel2 = false;
    }

    private IEnumerator FlashFalloNivel2()
    {
        // Subtract 2.5s penalty
        timerNivel2 = Mathf.Max(0f, timerNivel2 - 2.5f);

        if (txtResultado != null)
        {
            txtResultado.color = new Color(1f, 0.25f, 0.2f);
            txtResultado.text = "¡FALLASTE!\n-2.5s de Penalización";
        }

        yield return new WaitForSeconds(0.8f);

        if (txtResultado != null) txtResultado.text = "";

        // Reset indicator and randomize green zone
        posIndicadorNivel2 = 0f;
        dirIndicadorNivel2 = 1f;
        greenZoneInicioNivel2 = Random.Range(0.10f, 1f - greenZoneAnchoNivel2 - 0.10f);
        if (greenZoneNivel2RT != null)
        {
            greenZoneNivel2RT.anchorMin = new Vector2(greenZoneInicioNivel2, 0.05f);
            greenZoneNivel2RT.anchorMax = new Vector2(greenZoneInicioNivel2 + greenZoneAnchoNivel2, 0.95f);
            greenZoneNivel2RT.offsetMin = greenZoneNivel2RT.offsetMax = Vector2.zero;
        }

        yaPresionadoNivel2 = false;
    }

    private IEnumerator MostrarResultadoNivel2(bool gano)
    {
        if (txtInstruccion != null) txtInstruccion.text = "";
        if (txtTimerNivel2 != null) txtTimerNivel2.text = "";

        if (gano)
        {
            float progress = (float)aciertosNivel2 / 2f; // Should be 1.0f
            if (fillNivel2RT != null) fillNivel2RT.anchorMax = new Vector2(progress, 1f);
            if (txtProgressPercentNivel2 != null) txtProgressPercentNivel2.text = "100% (2/2 Aciertos)";

            InventoryManager.CurrentFuel = 1.0f;
            NPCInteraction.MarcarTratoAceptado(NPCInteraction.lastInteractedNPC);
            txtResultado.color = new Color(0.2f, 1f, 0.35f);
            txtResultado.text = "¡Le robaste la nafta!\n¡Tanque lleno al 100%!";
            yield return new WaitForSeconds(DELAY_RESULTADO);
            TerminarMinijuego();
        }
        else
        {
            txtResultado.color = new Color(1f, 0.25f, 0.2f);
            txtResultado.text = "¡Fallaste!\nNo conseguiste la nafta.";
            NPCInteraction.MarcarTratoAceptado(NPCInteraction.lastInteractedNPC);
            yield return new WaitForSeconds(DELAY_RESULTADO);
            TerminarMinijuego();
        }
    }

    // =====================================================================
    // LEVEL 3 SPECIFIC IMPLEMENTATION
    // =====================================================================
    private void BuildMinijuegoNivel3()
    {
        isNivel3 = true;
        aciertosNivel3 = 0;
        timerNivel3 = 15f;
        isNivel3Active = false;

        // ── Canvas principal ─────────────────────────────────────────────
        GameObject canvasGO = new GameObject("MinijuegoCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Fondo oscuro ─────────────────────────────────────────────────
        GameObject fondo = UIPanel(canvasGO.transform, "Fondo", new Color(0f, 0f, 0f, 0.95f));
        FullStretch(fondo.GetComponent<RectTransform>());

        // ── Background Robonafta 4 ───────────────────────────────────────
        Sprite bgSprite = Resources.Load<Sprite>("Sprites/robonafta4");
        if (bgSprite != null)
        {
            GameObject bgGO = new GameObject("RoboNaftaBg", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(canvasGO.transform, false);
            Image bgImg = bgGO.GetComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.preserveAspect = true;
            bgImg.raycastTarget = false;
            
            RectTransform bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0.05f, 0.15f);
            bgRT.anchorMax = new Vector2(0.95f, 0.95f);
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        }

        // ── Progress Bar Container (Bidón verde) ─────────────────────────
        GameObject barContainer = new GameObject("BarContainer", typeof(RectTransform));
        barContainer.transform.SetParent(canvasGO.transform, false);
        RectTransform bcRT = barContainer.GetComponent<RectTransform>();
        bcRT.anchorMin = bcRT.anchorMax = new Vector2(0.5f, 0f);
        bcRT.pivot = new Vector2(0.5f, 0f);
        bcRT.anchoredPosition = new Vector2(0f, 90f);
        bcRT.sizeDelta = new Vector2(800f, 40f);

        Image bgBarImg = barContainer.AddComponent<Image>();
        bgBarImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        Outline outlineBar = barContainer.AddComponent<Outline>();
        outlineBar.effectColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        outlineBar.effectDistance = new Vector2(2f, 2f);

        // Fill Image
        GameObject fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(barContainer.transform, false);
        Image fillImg = fillGO.GetComponent<Image>();
        fillImg.color = new Color(0.2f, 0.8f, 0.3f, 0.9f); // Green fill
        fillNivel3RT = fillGO.GetComponent<RectTransform>();
        fillNivel3RT.anchorMin = new Vector2(0f, 0f);
        fillNivel3RT.anchorMax = new Vector2(0f, 1f); // starts at 0%
        fillNivel3RT.offsetMin = fillNivel3RT.offsetMax = Vector2.zero;

        // ── Text Percent ───────────────────────────────────────────────
        GameObject txtPercentGO = new GameObject("TxtPercent", typeof(RectTransform));
        txtPercentGO.transform.SetParent(barContainer.transform, false);
        txtProgressPercentNivel3 = txtPercentGO.AddComponent<TextMeshProUGUI>();
        txtProgressPercentNivel3.text = "0% (0/2 Aciertos)";
        txtProgressPercentNivel3.fontSize = 20f;
        txtProgressPercentNivel3.fontStyle = FontStyles.Bold;
        txtProgressPercentNivel3.color = Color.white;
        txtProgressPercentNivel3.alignment = TextAlignmentOptions.Center;
        
        RectTransform tpRT = txtPercentGO.GetComponent<RectTransform>();
        tpRT.anchorMin = Vector2.zero;
        tpRT.anchorMax = Vector2.one;
        tpRT.offsetMin = tpRT.offsetMax = Vector2.zero;

        // ── Timing Track Container ───────────────────────────────────────
        GameObject trackContainer = new GameObject("TrackContainer", typeof(RectTransform));
        trackContainer.transform.SetParent(canvasGO.transform, false);
        trackNivel3RT = trackContainer.GetComponent<RectTransform>();
        trackNivel3RT.anchorMin = trackNivel3RT.anchorMax = new Vector2(0.5f, 0f);
        trackNivel3RT.pivot = new Vector2(0.5f, 0f);
        trackNivel3RT.anchoredPosition = new Vector2(0f, 170f);
        trackNivel3RT.sizeDelta = new Vector2(800f, 50f);

        Image bgTrackImg = trackContainer.AddComponent<Image>();
        bgTrackImg.color = new Color(0.15f, 0.12f, 0.1f, 0.95f);
        Outline outlineTrack = trackContainer.AddComponent<Outline>();
        outlineTrack.effectColor = new Color(0.4f, 0.3f, 0.2f, 0.8f);
        outlineTrack.effectDistance = new Vector2(2f, 2f);

        // Green Zone
        greenZoneInicioNivel3 = Random.Range(0.35f, 0.70f);
        GameObject greenZoneGO = UIPanel(trackContainer.transform, "GreenZone", new Color(0.2f, 0.85f, 0.3f, 0.8f));
        greenZoneNivel3RT = greenZoneGO.GetComponent<RectTransform>();
        greenZoneNivel3RT.anchorMin = new Vector2(greenZoneInicioNivel3, 0.05f);
        greenZoneNivel3RT.anchorMax = new Vector2(greenZoneInicioNivel3 + greenZoneAnchoNivel3, 0.95f);
        greenZoneNivel3RT.offsetMin = greenZoneNivel3RT.offsetMax = Vector2.zero;
        Outline gzOutline = greenZoneGO.AddComponent<Outline>();
        gzOutline.effectColor = new Color(0.4f, 1f, 0.4f);
        gzOutline.effectDistance = new Vector2(2f, 2f);

        // Indicator line
        GameObject indGO = UIPanel(trackContainer.transform, "Indicator", new Color(1f, 0.9f, 0.1f));
        indicatorNivel3RT = indGO.GetComponent<RectTransform>();
        indicatorNivel3RT.anchorMin = new Vector2(0f, 0.02f);
        indicatorNivel3RT.anchorMax = new Vector2(0f, 0.98f);
        indicatorNivel3RT.sizeDelta = new Vector2(8f, 0f); // 8px thickness
        indicatorNivel3RT.anchoredPosition = new Vector2(4f, 0f);

        // ── Timer Text ───────────────────────────────────────────────
        GameObject txtTimerGO = new GameObject("TxtTimer", typeof(RectTransform));
        txtTimerGO.transform.SetParent(canvasGO.transform, false);
        txtTimerNivel3 = txtTimerGO.AddComponent<TextMeshProUGUI>();
        txtTimerNivel3.text = "TIEMPO RESTANTE: 15.0s";
        txtTimerNivel3.fontSize = 32f;
        txtTimerNivel3.fontStyle = FontStyles.Bold;
        txtTimerNivel3.color = new Color(1f, 0.88f, 0.1f);
        txtTimerNivel3.alignment = TextAlignmentOptions.Center;

        RectTransform ttRT = txtTimerGO.GetComponent<RectTransform>();
        ttRT.anchorMin = new Vector2(0.5f, 0f);
        ttRT.anchorMax = new Vector2(0.5f, 0f);
        ttRT.pivot = new Vector2(0.5f, 0f);
        ttRT.anchoredPosition = new Vector2(0f, 250f);
        ttRT.sizeDelta = new Vector2(600f, 50f);

        // ── Instruction Text ─────────────────────────────────────────────
        GameObject txtInstGO = new GameObject("TxtInstruccion", typeof(RectTransform));
        txtInstGO.transform.SetParent(canvasGO.transform, false);
        txtInstruccion = txtInstGO.AddComponent<TextMeshProUGUI>();
        txtInstruccion.text = "¡MANTENÉ PRESIONADA <color=#FFAA00>ESPACIO</color> Y SOLTALA EN LA ZONA VERDE!";
        txtInstruccion.fontSize = 24f;
        txtInstruccion.fontStyle = FontStyles.Bold;
        txtInstruccion.color = new Color(0.88f, 0.82f, 0.65f);
        txtInstruccion.alignment = TextAlignmentOptions.Center;

        RectTransform tiRT = txtInstGO.GetComponent<RectTransform>();
        tiRT.anchorMin = new Vector2(0.5f, 0f);
        tiRT.anchorMax = new Vector2(0.5f, 0f);
        tiRT.pivot = new Vector2(0.5f, 0f);
        tiRT.anchoredPosition = new Vector2(0f, 30f);
        tiRT.sizeDelta = new Vector2(1200f, 40f);

        // ── Text Resultado ───────────────────────────────────────────────
        GameObject txtResGO = new GameObject("TxtResultado", typeof(RectTransform));
        txtResGO.transform.SetParent(canvasGO.transform, false);
        txtResultado = txtResGO.AddComponent<TextMeshProUGUI>();
        txtResultado.text = "";
        txtResultado.fontSize = 56f;
        txtResultado.fontStyle = FontStyles.Bold;
        txtResultado.color = Color.white;
        txtResultado.alignment = TextAlignmentOptions.Center;

        RectTransform trRT = txtResGO.GetComponent<RectTransform>();
        trRT.anchorMin = new Vector2(0.1f, 0.45f);
        trRT.anchorMax = new Vector2(0.9f, 0.65f);
        trRT.offsetMin = trRT.offsetMax = Vector2.zero;

        StartCoroutine(ArrancarJuegoNivel3());
    }

    private IEnumerator ArrancarJuegoNivel3()
    {
        yield return new WaitForSeconds(DELAY_INICIO);
        isNivel3Active = true;
    }

    private void UpdateNivel3()
    {
        if (!isNivel3Active) return;

        // Decrement timer
        timerNivel3 -= Time.deltaTime;
        if (timerNivel3 <= 0f)
        {
            timerNivel3 = 0f;
            isNivel3Active = false;
            StartCoroutine(MostrarResultadoNivel3(false));
            return;
        }

        if (!yaPresionadoNivel3)
        {
            // 1. First, check if spacebar was released in this frame (so we validate the charged value before resetting)
            if (Input.GetKeyUp(KeyCode.Space))
            {
                yaPresionadoNivel3 = true;
                bool enZona = posIndicadorNivel3 >= greenZoneInicioNivel3 &&
                              posIndicadorNivel3 <= greenZoneInicioNivel3 + greenZoneAnchoNivel3;

                if (enZona)
                {
                    aciertosNivel3++;
                    if (aciertosNivel3 >= 2)
                    {
                        isNivel3Active = false;
                        StartCoroutine(MostrarResultadoNivel3(true));
                    }
                    else
                    {
                        StartCoroutine(FlashAciertoNivel3());
                    }
                }
                else
                {
                    bool poco = posIndicadorNivel3 < greenZoneInicioNivel3;
                    StartCoroutine(FlashFalloNivel3(poco));
                }
            }
            // 2. If it was not released, check if it's currently held
            else if (Input.GetKey(KeyCode.Space))
            {
                // Charge up the valve
                posIndicadorNivel3 += velocidadNivel3 * Time.deltaTime;
                if (posIndicadorNivel3 > 1f)
                {
                    posIndicadorNivel3 %= 1f; // loop back to 0
                }
            }
            // 3. Otherwise (not held and not released), reset indicator to 0
            else
            {
                posIndicadorNivel3 = 0f;
            }

            // Update UI indicator line position
            if (indicatorNivel3RT != null)
            {
                indicatorNivel3RT.anchorMin = new Vector2(posIndicadorNivel3, indicatorNivel3RT.anchorMin.y);
                indicatorNivel3RT.anchorMax = new Vector2(posIndicadorNivel3, indicatorNivel3RT.anchorMax.y);
                indicatorNivel3RT.anchoredPosition = new Vector2(4f, 0f);
            }
        }

        // Update UI timer text
        if (txtTimerNivel3 != null)
        {
            txtTimerNivel3.text = $"TIEMPO RESTANTE: {timerNivel3:F1}s";
        }
    }

    private IEnumerator FlashAciertoNivel3()
    {
        // Update bar fill
        float progress = (float)aciertosNivel3 / 2f;
        if (fillNivel3RT != null) fillNivel3RT.anchorMax = new Vector2(progress, 1f);
        if (txtProgressPercentNivel3 != null) txtProgressPercentNivel3.text = $"{Mathf.RoundToInt(progress * 100f)}% ({aciertosNivel3}/2 Aciertos)";

        if (txtResultado != null)
        {
            txtResultado.color = new Color(0.2f, 1f, 0.35f);
            txtResultado.text = "¡ABRIÓ CORRECTAMENTE!";
        }

        yield return new WaitForSeconds(1.0f);

        if (txtResultado != null) txtResultado.text = "";

        // Reset indicator and randomize green zone
        posIndicadorNivel3 = 0f;
        greenZoneInicioNivel3 = Random.Range(0.35f, 0.70f);
        if (greenZoneNivel3RT != null)
        {
            greenZoneNivel3RT.anchorMin = new Vector2(greenZoneInicioNivel3, 0.05f);
            greenZoneNivel3RT.anchorMax = new Vector2(greenZoneInicioNivel3 + greenZoneAnchoNivel3, 0.95f);
            greenZoneNivel3RT.offsetMin = greenZoneNivel3RT.offsetMax = Vector2.zero;
        }

        yaPresionadoNivel3 = false;
    }

    private IEnumerator FlashFalloNivel3(bool poco)
    {
        // Subtract 2.5s penalty
        timerNivel3 = Mathf.Max(0f, timerNivel3 - 2.5f);

        if (txtResultado != null)
        {
            txtResultado.color = new Color(1f, 0.25f, 0.2f);
            txtResultado.text = poco ? "¡NO ABRIÓ SUFICIENTE!\n-2.5s de Penalización" : "¡ABRIÓ DEMASIADO!\n-2.5s de Penalización";
        }

        yield return new WaitForSeconds(1.0f);

        if (txtResultado != null) txtResultado.text = "";

        // Reset indicator and randomize green zone
        posIndicadorNivel3 = 0f;
        greenZoneInicioNivel3 = Random.Range(0.35f, 0.70f);
        if (greenZoneNivel3RT != null)
        {
            greenZoneNivel3RT.anchorMin = new Vector2(greenZoneInicioNivel3, 0.05f);
            greenZoneNivel3RT.anchorMax = new Vector2(greenZoneInicioNivel3 + greenZoneAnchoNivel3, 0.95f);
            greenZoneNivel3RT.offsetMin = greenZoneNivel3RT.offsetMax = Vector2.zero;
        }

        yaPresionadoNivel3 = false;
    }

    private IEnumerator MostrarResultadoNivel3(bool gano)
    {
        if (txtInstruccion != null) txtInstruccion.text = "";
        if (txtTimerNivel3 != null) txtTimerNivel3.text = "";

        if (gano)
        {
            float progress = (float)aciertosNivel3 / 2f; // 100%
            if (fillNivel3RT != null) fillNivel3RT.anchorMax = new Vector2(progress, 1f);
            if (txtProgressPercentNivel3 != null) txtProgressPercentNivel3.text = "100% (2/2 Aciertos)";

            InventoryManager.CurrentFuel = 1.0f;
            NPCInteraction.MarcarTratoAceptado(NPCInteraction.lastInteractedNPC);
            txtResultado.color = new Color(0.2f, 1f, 0.35f);
            txtResultado.text = "¡Le robaste la nafta!\n¡Tanque lleno al 100%!";
            yield return new WaitForSeconds(DELAY_RESULTADO);
            TerminarMinijuego();
        }
        else
        {
            txtResultado.color = new Color(1f, 0.25f, 0.2f);
            txtResultado.text = "¡Fallaste!\nNo conseguiste la nafta.";
            NPCInteraction.MarcarTratoAceptado(NPCInteraction.lastInteractedNPC);
            yield return new WaitForSeconds(DELAY_RESULTADO);
            TerminarMinijuego();
        }
    }

    // =====================================================================
    // LEVEL 4 SPECIFIC IMPLEMENTATION  – Reaction / Signal minigame
    // =====================================================================
    private void BuildMinijuegoNivel4()
    {
        isNivel4 = true;
        aciertosNivel4 = 0;
        timerNivel4 = 15f;
        isNivel4Active = false;
        senalMostradaNivel4 = false;
        feedbackActivoNivel4 = false;

        // ── Canvas principal ─────────────────────────────────────────────
        GameObject canvasGO = new GameObject("MinijuegoCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Fondo oscuro ─────────────────────────────────────────────────
        GameObject fondo = UIPanel(canvasGO.transform, "Fondo", new Color(0f, 0f, 0f, 0.95f));
        FullStretch(fondo.GetComponent<RectTransform>());

        // ── Background Robonafta 5 ───────────────────────────────────────
        Sprite bgSprite = Resources.Load<Sprite>("Sprites/robonafta5");
        if (bgSprite != null)
        {
            GameObject bgGO = new GameObject("RoboNaftaBg", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(canvasGO.transform, false);
            Image bgImg = bgGO.GetComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.preserveAspect = true;
            bgImg.raycastTarget = false;

            RectTransform bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0.03f, 0.12f);
            bgRT.anchorMax = new Vector2(0.97f, 0.97f);
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        }

        // ── Progress Bar Container (bidón verde) ─────────────────────────
        GameObject barContainer = new GameObject("BarContainer", typeof(RectTransform));
        barContainer.transform.SetParent(canvasGO.transform, false);
        RectTransform bcRT = barContainer.GetComponent<RectTransform>();
        bcRT.anchorMin = bcRT.anchorMax = new Vector2(0.5f, 0f);
        bcRT.pivot = new Vector2(0.5f, 0f);
        bcRT.anchoredPosition = new Vector2(0f, 25f);
        bcRT.sizeDelta = new Vector2(800f, 40f);

        Image bgBarImg = barContainer.AddComponent<Image>();
        bgBarImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        Outline outlineBar = barContainer.AddComponent<Outline>();
        outlineBar.effectColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        outlineBar.effectDistance = new Vector2(2f, 2f);

        GameObject fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(barContainer.transform, false);
        fillGO.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.3f, 0.9f);
        fillNivel4RT = fillGO.GetComponent<RectTransform>();
        fillNivel4RT.anchorMin = new Vector2(0f, 0f);
        fillNivel4RT.anchorMax = new Vector2(0f, 1f);
        fillNivel4RT.offsetMin = fillNivel4RT.offsetMax = Vector2.zero;

        GameObject txtPercentGO = new GameObject("TxtPercent", typeof(RectTransform));
        txtPercentGO.transform.SetParent(barContainer.transform, false);
        txtProgressPercentNivel4 = txtPercentGO.AddComponent<TextMeshProUGUI>();
        txtProgressPercentNivel4.text = "0% (0/2 Aciertos)";
        txtProgressPercentNivel4.fontSize = 20f;
        txtProgressPercentNivel4.fontStyle = FontStyles.Bold;
        txtProgressPercentNivel4.color = Color.white;
        txtProgressPercentNivel4.alignment = TextAlignmentOptions.Center;
        RectTransform tpRT = txtPercentGO.GetComponent<RectTransform>();
        tpRT.anchorMin = Vector2.zero; tpRT.anchorMax = Vector2.one;
        tpRT.offsetMin = tpRT.offsetMax = Vector2.zero;

        // ── Timer Text ────────────────────────────────────────────────────
        GameObject txtTimerGO = new GameObject("TxtTimer", typeof(RectTransform));
        txtTimerGO.transform.SetParent(canvasGO.transform, false);
        txtTimerNivel4 = txtTimerGO.AddComponent<TextMeshProUGUI>();
        txtTimerNivel4.text = "TIEMPO RESTANTE: 15.0s";
        txtTimerNivel4.fontSize = 32f;
        txtTimerNivel4.fontStyle = FontStyles.Bold;
        txtTimerNivel4.color = new Color(1f, 0.88f, 0.1f);
        txtTimerNivel4.alignment = TextAlignmentOptions.Center;
        RectTransform ttRT = txtTimerGO.GetComponent<RectTransform>();
        ttRT.anchorMin = new Vector2(0.5f, 0f); ttRT.anchorMax = new Vector2(0.5f, 0f);
        ttRT.pivot = new Vector2(0.5f, 0f);
        ttRT.anchoredPosition = new Vector2(0f, 80f);
        ttRT.sizeDelta = new Vector2(600f, 50f);

        // ── Instrucción ────────────────────────────────────────────────────
        GameObject txtInstGO = new GameObject("TxtInstruccion", typeof(RectTransform));
        txtInstGO.transform.SetParent(canvasGO.transform, false);
        txtInstruccion = txtInstGO.AddComponent<TextMeshProUGUI>();
        txtInstruccion.text = "Esperá la señal... y presioná <color=#FFAA00>ESPACIO</color> cuando aparezca <color=#00FF66>¡AHORA!</color>";
        txtInstruccion.fontSize = 26f;
        txtInstruccion.fontStyle = FontStyles.Bold;
        txtInstruccion.color = new Color(0.88f, 0.82f, 0.65f);
        txtInstruccion.alignment = TextAlignmentOptions.Center;
        RectTransform tiRT = txtInstGO.GetComponent<RectTransform>();
        tiRT.anchorMin = new Vector2(0.5f, 0f); tiRT.anchorMax = new Vector2(0.5f, 0f);
        tiRT.pivot = new Vector2(0.5f, 0f);
        tiRT.anchoredPosition = new Vector2(0f, 145f);
        tiRT.sizeDelta = new Vector2(1300f, 55f);

        // ── Señal central ¡AHORA! / Feedback ─────────────────────────────
        GameObject txtResGO = new GameObject("TxtResultado", typeof(RectTransform));
        txtResGO.transform.SetParent(canvasGO.transform, false);
        txtResultado = txtResGO.AddComponent<TextMeshProUGUI>();
        txtResultado.text = "";
        txtResultado.fontSize = 90f;
        txtResultado.fontStyle = FontStyles.Bold;
        txtResultado.color = Color.white;
        txtResultado.alignment = TextAlignmentOptions.Center;
        RectTransform trRT = txtResGO.GetComponent<RectTransform>();
        trRT.anchorMin = new Vector2(0.1f, 0.35f);
        trRT.anchorMax = new Vector2(0.9f, 0.75f);
        trRT.offsetMin = trRT.offsetMax = Vector2.zero;

        StartCoroutine(ArrancarJuegoNivel4());
    }

    private IEnumerator ArrancarJuegoNivel4()
    {
        yield return new WaitForSeconds(DELAY_INICIO);
        isNivel4Active = true;
        waitTimerNivel4 = Random.Range(2f, 5f); // first wait window
    }

    private void UpdateNivel4()
    {
        if (!isNivel4Active) return;
        if (feedbackActivoNivel4) return; // wait while coroutine handles feedback

        // ── Manual space key tracking (more reliable than GetKeyDown) ─────
        bool spaceNow = Input.GetKey(KeyCode.Space);
        bool justPressed = spaceNow && !spaceWasPressedNivel4;
        spaceWasPressedNivel4 = spaceNow;

        // Global timer
        timerNivel4 -= Time.deltaTime;
        if (timerNivel4 <= 0f)
        {
            timerNivel4 = 0f;
            isNivel4Active = false;
            StartCoroutine(MostrarResultadoNivel4(false));
            return;
        }
        if (txtTimerNivel4 != null)
            txtTimerNivel4.text = $"TIEMPO RESTANTE: {timerNivel4:F1}s";

        if (!senalMostradaNivel4)
        {
            // Waiting phase – player must NOT press space
            waitTimerNivel4 -= Time.deltaTime;

            if (justPressed)
            {
                // Pressed too early → failure
                feedbackActivoNivel4 = true;
                StartCoroutine(FalloTempranoNivel4());
                return;
            }

            if (waitTimerNivel4 <= 0f)
            {
                // Show the signal
                senalMostradaNivel4 = true;
                signalActiveTimeNivel4 = 0f;
                if (txtResultado != null)
                {
                    txtResultado.color = new Color(0.1f, 1f, 0.4f);
                    txtResultado.text = "¡AHORA!";
                }
            }
        }
        else
        {
            // Signal is showing – player must press space
            signalActiveTimeNivel4 += Time.deltaTime;

            if (justPressed)
            {
                // Hit!
                feedbackActivoNivel4 = true;
                aciertosNivel4++;
                if (aciertosNivel4 >= 2)
                {
                    isNivel4Active = false;
                    StartCoroutine(MostrarResultadoNivel4(true));
                }
                else
                {
                    StartCoroutine(AciertoNivel4());
                }
                return;
            }

            // Miss window: signal visible for up to 2.5 s without pressing → fail
            if (signalActiveTimeNivel4 >= 2.5f)
            {
                feedbackActivoNivel4 = true;
                StartCoroutine(FalloTardioNivel4());
            }
        }
    }

    private IEnumerator AciertoNivel4()
    {
        if (txtResultado != null)
        {
            txtResultado.color = new Color(0.2f, 1f, 0.35f);
            txtResultado.text = "¡BIEN!";
        }
        float progress = (float)aciertosNivel4 / 2f;
        if (fillNivel4RT != null) fillNivel4RT.anchorMax = new Vector2(progress, 1f);
        if (txtProgressPercentNivel4 != null)
            txtProgressPercentNivel4.text = $"{Mathf.RoundToInt(progress * 100f)}% ({aciertosNivel4}/2 Aciertos)";

        yield return new WaitForSeconds(0.9f);

        if (txtResultado != null) txtResultado.text = "";
        senalMostradaNivel4 = false;
        waitTimerNivel4 = Random.Range(2f, 5f);
        feedbackActivoNivel4 = false;
    }

    private IEnumerator FalloTempranoNivel4()
    {
        timerNivel4 = Mathf.Max(0f, timerNivel4 - 3f);
        if (txtResultado != null)
        {
            txtResultado.color = new Color(1f, 0.25f, 0.2f);
            txtResultado.text = "¡DEMASIADO PRONTO!\n-3s de Penalización";
        }
        yield return new WaitForSeconds(1.1f);

        if (txtResultado != null) txtResultado.text = "";
        senalMostradaNivel4 = false;
        waitTimerNivel4 = Random.Range(2f, 5f);
        feedbackActivoNivel4 = false;
    }

    private IEnumerator FalloTardioNivel4()
    {
        timerNivel4 = Mathf.Max(0f, timerNivel4 - 2.5f);
        if (txtResultado != null)
        {
            txtResultado.color = new Color(1f, 0.25f, 0.2f);
            txtResultado.text = "¡REACCIONASTE TARDE!\n-2.5s de Penalización";
        }
        yield return new WaitForSeconds(1.1f);

        if (txtResultado != null) txtResultado.text = "";
        senalMostradaNivel4 = false;
        waitTimerNivel4 = Random.Range(2f, 5f);
        feedbackActivoNivel4 = false;
    }

    private IEnumerator MostrarResultadoNivel4(bool gano)
    {
        if (txtInstruccion != null) txtInstruccion.text = "";
        if (txtTimerNivel4 != null) txtTimerNivel4.text = "";

        if (gano)
        {
            if (fillNivel4RT != null) fillNivel4RT.anchorMax = new Vector2(1f, 1f);
            if (txtProgressPercentNivel4 != null) txtProgressPercentNivel4.text = "100% (2/2 Aciertos)";

            InventoryManager.CurrentFuel = 1.0f;
            NPCInteraction.MarcarTratoAceptado(NPCInteraction.lastInteractedNPC);
            txtResultado.color = new Color(0.2f, 1f, 0.35f);
            txtResultado.text = "¡Le robaste la nafta!\n¡Tanque lleno al 100%!";
            yield return new WaitForSeconds(DELAY_RESULTADO);
            TerminarMinijuego();
        }
        else
        {
            txtResultado.color = new Color(1f, 0.25f, 0.2f);
            txtResultado.text = "¡Fallaste!\nNo conseguiste la nafta.";
            NPCInteraction.MarcarTratoAceptado(NPCInteraction.lastInteractedNPC);
            yield return new WaitForSeconds(DELAY_RESULTADO);
            TerminarMinijuego();
        }
    }

    // =====================================================================
    // LEVEL 5 SPECIFIC IMPLEMENTATION – Hold-in-zone retention minigame
    // =====================================================================
    private void BuildMinijuegoNivel5()
    {
        isNivel5 = true;
        isNivel5Active = false;
        timerNivel5 = 15f;
        holdTimeNivel5 = 0f;
        posIndicadorNivel5 = 0.5f;
        driftDirNivel5 = 1f;
        driftSpeedNivel5 = 0.65f;
        changeDriftNivel5Timer = 0f;
        spaceHeldNivel5 = false;

        // ── Canvas ───────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("MinijuegoCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Fondo oscuro ─────────────────────────────────────────────────
        GameObject fondo = UIPanel(canvasGO.transform, "Fondo", new Color(0f, 0f, 0f, 0.95f));
        FullStretch(fondo.GetComponent<RectTransform>());

        // ── Background robonafta6 ─────────────────────────────────────────
        Sprite bgSpr = Resources.Load<Sprite>("Sprites/robonafta6");
        if (bgSpr != null)
        {
            GameObject bgGO = new GameObject("RoboNaftaBg", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(canvasGO.transform, false);
            Image bgImg = bgGO.GetComponent<Image>();
            bgImg.sprite = bgSpr;
            bgImg.preserveAspect = true;
            bgImg.raycastTarget  = false;
            RectTransform bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0.03f, 0.15f);
            bgRT.anchorMax = new Vector2(0.97f, 0.97f);
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        }

        // ── Barra de hold (progreso del tiempo en zona verde) ────────────
        GameObject holdContainer = new GameObject("HoldContainer", typeof(RectTransform));
        holdContainer.transform.SetParent(canvasGO.transform, false);
        RectTransform hcRT = holdContainer.GetComponent<RectTransform>();
        hcRT.anchorMin = hcRT.anchorMax = new Vector2(0.5f, 0f);
        hcRT.pivot = new Vector2(0.5f, 0f);
        hcRT.anchoredPosition = new Vector2(0f, 28f);
        hcRT.sizeDelta = new Vector2(800f, 36f);
        Image hBg = holdContainer.AddComponent<Image>();
        hBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        Outline hOut = holdContainer.AddComponent<Outline>();
        hOut.effectColor    = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        hOut.effectDistance = new Vector2(2f, 2f);

        GameObject fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(holdContainer.transform, false);
        fillGO.GetComponent<Image>().color = new Color(0.2f, 0.85f, 0.35f, 0.9f);
        fillNivel5RT = fillGO.GetComponent<RectTransform>();
        fillNivel5RT.anchorMin = new Vector2(0f, 0f);
        fillNivel5RT.anchorMax = new Vector2(0f, 1f);
        fillNivel5RT.offsetMin = fillNivel5RT.offsetMax = Vector2.zero;

        // hold label
        GameObject lblGO = new GameObject("TxtHold", typeof(RectTransform));
        lblGO.transform.SetParent(holdContainer.transform, false);
        txtHoldNivel5 = lblGO.AddComponent<TextMeshProUGUI>();
        txtHoldNivel5.text      = "0.0 / 5.0s en zona";
        txtHoldNivel5.fontSize  = 20f;
        txtHoldNivel5.fontStyle = FontStyles.Bold;
        txtHoldNivel5.color     = Color.white;
        txtHoldNivel5.alignment = TextAlignmentOptions.Center;
        RectTransform lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;

        // ── Track de control ─────────────────────────────────────────────
        GameObject trackGO = new GameObject("TrackContainer", typeof(RectTransform));
        trackGO.transform.SetParent(canvasGO.transform, false);
        RectTransform trackRT = trackGO.GetComponent<RectTransform>();
        trackRT.anchorMin = trackRT.anchorMax = new Vector2(0.5f, 0f);
        trackRT.pivot = new Vector2(0.5f, 0f);
        trackRT.anchoredPosition = new Vector2(0f, 100f);
        trackRT.sizeDelta = new Vector2(800f, 50f);
        Image tBg = trackGO.AddComponent<Image>();
        tBg.color = new Color(0.12f, 0.1f, 0.08f, 0.95f);
        Outline tOut = trackGO.AddComponent<Outline>();
        tOut.effectColor    = new Color(0.4f, 0.3f, 0.2f, 0.8f);
        tOut.effectDistance = new Vector2(2f, 2f);

        // Zona verde fija en el centro (ZONA_CENTRO ± ZONA_HALF)
        GameObject zonaGO = UIPanel(trackGO.transform, "ZonaVerde", new Color(0.2f, 0.85f, 0.3f, 0.8f));
        zonaVerdeNivel5RT = zonaGO.GetComponent<RectTransform>();
        zonaVerdeNivel5RT.anchorMin = new Vector2(ZONA_CENTRO - ZONA_HALF, 0.05f);
        zonaVerdeNivel5RT.anchorMax = new Vector2(ZONA_CENTRO + ZONA_HALF, 0.95f);
        zonaVerdeNivel5RT.offsetMin = zonaVerdeNivel5RT.offsetMax = Vector2.zero;
        Outline zOut = zonaGO.AddComponent<Outline>();
        zOut.effectColor    = new Color(0.4f, 1f, 0.4f);
        zOut.effectDistance = new Vector2(2f, 2f);

        // Indicador amarillo (comienza en el centro)
        GameObject indGO = UIPanel(trackGO.transform, "Indicador", new Color(1f, 0.9f, 0.1f));
        indicadorNivel5RT = indGO.GetComponent<RectTransform>();
        indicadorNivel5RT.anchorMin = new Vector2(0.5f, 0.02f);
        indicadorNivel5RT.anchorMax = new Vector2(0.5f, 0.98f);
        indicadorNivel5RT.sizeDelta        = new Vector2(8f, 0f);
        indicadorNivel5RT.anchoredPosition = new Vector2(4f, 0f);

        // ── Timer ─────────────────────────────────────────────────────────
        GameObject timerGO = new GameObject("TxtTimer", typeof(RectTransform));
        timerGO.transform.SetParent(canvasGO.transform, false);
        txtTimerNivel5 = timerGO.AddComponent<TextMeshProUGUI>();
        txtTimerNivel5.text      = "TIEMPO RESTANTE: 15.0s";
        txtTimerNivel5.fontSize  = 32f;
        txtTimerNivel5.fontStyle = FontStyles.Bold;
        txtTimerNivel5.color     = new Color(1f, 0.88f, 0.1f);
        txtTimerNivel5.alignment = TextAlignmentOptions.Center;
        RectTransform tiRT = timerGO.GetComponent<RectTransform>();
        tiRT.anchorMin = new Vector2(0.5f, 0f); tiRT.anchorMax = new Vector2(0.5f, 0f);
        tiRT.pivot = new Vector2(0.5f, 0f);
        tiRT.anchoredPosition = new Vector2(0f, 185f);
        tiRT.sizeDelta = new Vector2(620f, 50f);

        // ── Instrucción ───────────────────────────────────────────────────
        GameObject instGO = new GameObject("TxtInstruccion", typeof(RectTransform));
        instGO.transform.SetParent(canvasGO.transform, false);
        txtInstruccion = instGO.AddComponent<TextMeshProUGUI>();
        txtInstruccion.text      = "Mant\u00e9 la barra en la <color=#00FF66>ZONA VERDE</color>\u00a0con la tecla <color=#FFAA00>ESPACIO</color>\u00a0\u2192 cada press vuelve el indicador al centro";
        txtInstruccion.fontSize  = 24f;
        txtInstruccion.fontStyle = FontStyles.Bold;
        txtInstruccion.color     = new Color(0.88f, 0.82f, 0.65f);
        txtInstruccion.alignment = TextAlignmentOptions.Center;
        RectTransform inRT = instGO.GetComponent<RectTransform>();
        inRT.anchorMin = new Vector2(0.5f, 0f); inRT.anchorMax = new Vector2(0.5f, 0f);
        inRT.pivot = new Vector2(0.5f, 0f);
        inRT.anchoredPosition = new Vector2(0f, 30f);
        inRT.sizeDelta = new Vector2(1300f, 55f);

        // ── Texto resultado ───────────────────────────────────────────────
        GameObject resGO = new GameObject("TxtResultado", typeof(RectTransform));
        resGO.transform.SetParent(canvasGO.transform, false);
        txtResultado = resGO.AddComponent<TextMeshProUGUI>();
        txtResultado.text      = "";
        txtResultado.fontSize  = 60f;
        txtResultado.fontStyle = FontStyles.Bold;
        txtResultado.color     = Color.white;
        txtResultado.alignment = TextAlignmentOptions.Center;
        RectTransform resRT = resGO.GetComponent<RectTransform>();
        resRT.anchorMin = new Vector2(0.1f, 0.42f);
        resRT.anchorMax = new Vector2(0.9f, 0.68f);
        resRT.offsetMin = resRT.offsetMax = Vector2.zero;

        StartCoroutine(ArrancarJuegoNivel5());
    }

    private IEnumerator ArrancarJuegoNivel5()
    {
        yield return new WaitForSeconds(DELAY_INICIO);
        isNivel5Active = true;
    }

    private void UpdateNivel5()
    {
        if (!isNivel5Active) return;

        // ── Manual spacebar tracking ──────────────────────────────────────
        bool spaceNow = Input.GetKey(KeyCode.Space);
        bool justPressed = spaceNow && !spaceHeldNivel5;
        spaceHeldNivel5 = spaceNow;

        // If just pressed → snap indicator to center
        if (justPressed)
        {
            posIndicadorNivel5 = ZONA_CENTRO;
        }

        // ── Global timer ──────────────────────────────────────────────────
        timerNivel5 -= Time.deltaTime;
        if (timerNivel5 <= 0f)
        {
            timerNivel5 = 0f;
            isNivel5Active = false;
            StartCoroutine(MostrarResultadoNivel5(false));
            return;
        }
        if (txtTimerNivel5 != null)
            txtTimerNivel5.text = $"TIEMPO RESTANTE: {timerNivel5:F1}s";

        // ── Drift: random direction change every 0.7–1.4s ────────────────
        changeDriftNivel5Timer -= Time.deltaTime;
        if (changeDriftNivel5Timer <= 0f)
        {
            driftDirNivel5   = Random.value >= 0.5f ? 1f : -1f;
            driftSpeedNivel5 = Random.Range(0.5f, 0.85f);
            changeDriftNivel5Timer = Random.Range(0.7f, 1.4f);
        }

        posIndicadorNivel5 += driftDirNivel5 * driftSpeedNivel5 * Time.deltaTime;
        posIndicadorNivel5 = Mathf.Clamp(posIndicadorNivel5, 0.01f, 0.99f);

        // ── Update indicator UI ───────────────────────────────────────────
        if (indicadorNivel5RT != null)
        {
            indicadorNivel5RT.anchorMin = new Vector2(posIndicadorNivel5, indicadorNivel5RT.anchorMin.y);
            indicadorNivel5RT.anchorMax = new Vector2(posIndicadorNivel5, indicadorNivel5RT.anchorMax.y);
            indicadorNivel5RT.anchoredPosition = new Vector2(4f, 0f);
        }

        // ── Check if inside green zone ────────────────────────────────────
        bool enZona = posIndicadorNivel5 >= (ZONA_CENTRO - ZONA_HALF) &&
                      posIndicadorNivel5 <= (ZONA_CENTRO + ZONA_HALF);

        if (enZona)
        {
            holdTimeNivel5 = Mathf.Min(HOLD_GOAL_NIVEL5, holdTimeNivel5 + Time.deltaTime);
            if (txtResultado != null)
            {
                txtResultado.color = new Color(0.1f, 1f, 0.4f);
                txtResultado.text  = "\u00a1BIEN! \u00a1Segu\u00ed!";
            }
        }
        else
        {
            holdTimeNivel5 = Mathf.Max(0f, holdTimeNivel5 - 0.5f * Time.deltaTime); // slow drain when out
            if (txtResultado != null)
            {
                txtResultado.color = new Color(1f, 0.28f, 0.2f);
                txtResultado.text  = "\u00a1FUERA DE ZONA!\n\u00a1Pres\u00e1 ESPACIO!";
            }
        }

        // ── Update hold fill bar ──────────────────────────────────────────
        if (fillNivel5RT != null)
            fillNivel5RT.anchorMax = new Vector2(holdTimeNivel5 / HOLD_GOAL_NIVEL5, 1f);
        if (txtHoldNivel5 != null)
            txtHoldNivel5.text = $"{holdTimeNivel5:F1} / {HOLD_GOAL_NIVEL5:F1}s en zona";

        // ── Win condition ─────────────────────────────────────────────────
        if (holdTimeNivel5 >= HOLD_GOAL_NIVEL5)
        {
            isNivel5Active = false;
            StartCoroutine(MostrarResultadoNivel5(true));
        }
    }

    private IEnumerator MostrarResultadoNivel5(bool gano)
    {
        if (txtInstruccion != null) txtInstruccion.text = "";
        if (txtTimerNivel5 != null) txtTimerNivel5.text = "";
        if (txtHoldNivel5  != null) txtHoldNivel5.text  = "";

        if (gano)
        {
            if (fillNivel5RT != null) fillNivel5RT.anchorMax = new Vector2(1f, 1f);
            InventoryManager.CurrentFuel = 1.0f;
            NPCInteraction.MarcarTratoAceptado(NPCInteraction.lastInteractedNPC);
            txtResultado.color = new Color(0.2f, 1f, 0.35f);
            txtResultado.text  = "\u00a1Le robaste la nafta!\n\u00a1Tanque lleno al 100%!";
            yield return new WaitForSeconds(DELAY_RESULTADO);
            TerminarMinijuego();
        }
        else
        {
            txtResultado.color = new Color(1f, 0.25f, 0.2f);
            txtResultado.text  = "\u00a1Fallaste!\nNo conseguiste la nafta.";
            NPCInteraction.MarcarTratoAceptado(NPCInteraction.lastInteractedNPC);
            yield return new WaitForSeconds(DELAY_RESULTADO);
            TerminarMinijuego();
        }
    }

    // =====================================================================
    // HELPERS UI
    // =====================================================================
    private static GameObject UIPanel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static void FullStretch(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = rt.offsetMax = Vector2.zero;
    }
}
