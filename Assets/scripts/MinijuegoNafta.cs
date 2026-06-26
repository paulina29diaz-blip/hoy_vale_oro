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

        dm.ActivarModoMinijuego(
            onContinuar: IniciarMinijuego,
            onRechazar:  () =>
            {
                Time.timeScale = 1f;
                string destino = string.IsNullOrEmpty(escenaRetorno) ? "mapa" : escenaRetorno;
                Destroy(gameObject);
                SceneManager.LoadScene(destino);
            }
        );
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
        int nivel = ExtraerNumeroNivel(escenaRetorno);
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
