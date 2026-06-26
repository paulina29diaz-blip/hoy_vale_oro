using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Diálogo introductorio: aparece la primera vez que se entra al MAPA.
/// Panel inferior con el sprite-marco como fondo (estilo cuadrodetexto).
/// Click / Espacio para avanzar.
/// </summary>
public class IntroDialogo : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Flag de sesión
    // -----------------------------------------------------------------------
    private static bool yaSeMostro = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetearFlagAlIniciar() => yaSeMostro = false;

    public static void ResetearParaNuevaPartida() => yaSeMostro = false;

    public static void IntentarMostrar()
    {
        if (yaSeMostro) return;
        yaSeMostro = true;
        GameObject go = new GameObject("IntroDialogo");
        DontDestroyOnLoad(go);
        go.AddComponent<IntroDialogo>();
    }

    // -----------------------------------------------------------------------
    // Diálogo
    // -----------------------------------------------------------------------
    private struct Linea { public bool esFran; public string nombre, texto; }

    private readonly Linea[] lineas = new Linea[]
    {
        new Linea { esFran = true,  nombre = "FRAN",
            texto = "¡Escuchame bien! Hay un virus que se está propagando por todo el país. El mundo tal como lo conocemos... se está terminando." },
        new Linea { esFran = true,  nombre = "FRAN",
            texto = "En el Obelisco hay un refugio anti-apocalipsis. Necesitás llegar lo antes posible. Es nuestra única esperanza de sobrevivir." },
        new Linea { esFran = true,  nombre = "FRAN",
            texto = "Para entrar al bunker necesitás: caja de herramientas, batería, un mapa, anafe y guantes. ¡No te olvides de ninguno!" },
        new Linea { esFran = false, nombre = "PROTAGONISTA",
            texto = "¿Qué? ¿Un virus? ¿El mundo se está terminando? No puedo creer lo que me estás diciendo, Fran..." },
        new Linea { esFran = false, nombre = "PROTAGONISTA",
            texto = "Voy en camino. Preparate para verme llegar." },
    };

    private int lineaActual = 0;

    // -----------------------------------------------------------------------
    // UI refs
    // -----------------------------------------------------------------------
    private Canvas          canvas;
    private Image           marcoImg;
    private RectTransform   marcoRT;        // para reposicionar según el hablante
    private TextMeshProUGUI txtNombre;
    private TextMeshProUGUI txtDialogo;
    private TextMeshProUGUI txtContinuar;

    private Sprite spriteFran;
    private Sprite spriteProta;

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------
    void Start()
    {
        spriteFran  = Resources.Load<Sprite>("Sprites/dialogos/dialogocelu");
        spriteProta = Resources.Load<Sprite>("Sprites/dialogos/dialogopjprincipal");

        Time.timeScale = 0f;
        BuildUI();
        MostrarLinea(0);
    }

    void OnDestroy() => Time.timeScale = 1f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            OnContinuar();
    }

    // -----------------------------------------------------------------------
    // UI  – marco al 60% del tamaño original (40% más chico)
    // -----------------------------------------------------------------------
    private void BuildUI()
    {
        // Canvas
        GameObject cgo = new GameObject("IntroCanvas");
        cgo.transform.SetParent(transform, false);
        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler cs = cgo.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();

        // Fondo oscuro suave
        GameObject fondo = GO(cgo.transform, "Fondo");
        SetAnchors(fondo, 0f, 0f, 1f, 1f);
        fondo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

        // ── Marco: 30% más chico que la versión anterior (total ~58% del original)
        //   Ancho: 0.20 → 0.80  (60% de pantalla, centrado)
        //   Alto:  0.04 → 0.21  (17% de pantalla)
        GameObject marco = GO(cgo.transform, "Marco");
        SetAnchors(marco, 0.20f, 0.04f, 0.80f, 0.21f);

        marcoImg                = marco.AddComponent<Image>();
        marcoImg.sprite         = spriteProta;
        marcoImg.type           = Image.Type.Simple;
        marcoImg.preserveAspect = false;
        marcoImg.color          = Color.white;
        marcoRT                 = marco.GetComponent<RectTransform>();

        // Nombre y texto: posición inicial (se actualiza dinámicamente en MostrarLinea)
        txtNombre = TMPU(marco.transform, "Nombre", 0.30f, 0.60f, 0.97f, 0.95f);
        txtNombre.fontSize            = 22f;
        txtNombre.fontStyle           = FontStyles.Bold;
        txtNombre.color               = new Color(1f, 0.82f, 0.15f);
        txtNombre.alignment           = TextAlignmentOptions.MidlineLeft;
        txtNombre.enableWordWrapping  = false;
        txtNombre.overflowMode        = TextOverflowModes.Ellipsis;

        txtDialogo = TMPU(marco.transform, "Texto", 0.30f, 0.06f, 0.97f, 0.62f);
        txtDialogo.fontSize           = 18f;
        txtDialogo.color              = new Color(0.95f, 0.92f, 0.82f);
        txtDialogo.alignment          = TextAlignmentOptions.TopLeft;
        txtDialogo.enableWordWrapping = true;
        txtDialogo.overflowMode       = TextOverflowModes.Truncate;

        // Hint inferior (justo debajo del marco)
        txtContinuar = TMPU(cgo.transform, "TxtContinuar", 0.20f, 0.01f, 0.80f, 0.045f);
        txtContinuar.text      = "Presioná ESPACIO o HAZ CLICK para continuar";
        txtContinuar.fontSize  = 14f;
        txtContinuar.color     = new Color(1f, 1f, 1f, 0.65f);
        txtContinuar.alignment = TextAlignmentOptions.Center;
        txtContinuar.fontStyle = FontStyles.Italic;
    }

    // -----------------------------------------------------------------------
    // Secuencia
    // -----------------------------------------------------------------------
    private void MostrarLinea(int idx)
    {
        if (idx >= lineas.Length) { Destroy(gameObject); return; }

        Linea l = lineas[idx];

        // ── Reposicionar marco y texto según el hablante ────────────────────
        if (marcoRT != null)
        {
            RectTransform nombreRT  = txtNombre?.GetComponent<RectTransform>();
            RectTransform dialogoRT = txtDialogo?.GetComponent<RectTransform>();

            if (l.esFran)
            {
                // Fran: texto a la IZQUIERDA, retrato a la derecha del sprite
                // Marco centrado en pantalla
                marcoRT.anchorMin = new Vector2(0.14f, 0.04f);
                marcoRT.anchorMax = new Vector2(0.86f, 0.22f);

                // Nombre: columna izquierda
                if (nombreRT != null)  { nombreRT.anchorMin  = new Vector2(0.04f, 0.60f); nombreRT.anchorMax  = new Vector2(0.70f, 0.95f); }
                // Texto:  columna izquierda
                if (dialogoRT != null) { dialogoRT.anchorMin = new Vector2(0.04f, 0.06f); dialogoRT.anchorMax = new Vector2(0.70f, 0.62f); }
            }
            else
            {
                // Protagonista: texto a la DERECHA, retrato a la izquierda del sprite
                marcoRT.anchorMin = new Vector2(0.20f, 0.04f);
                marcoRT.anchorMax = new Vector2(0.80f, 0.21f);

                // Nombre: columna derecha
                if (nombreRT != null)  { nombreRT.anchorMin  = new Vector2(0.30f, 0.60f); nombreRT.anchorMax  = new Vector2(0.97f, 0.95f); }
                // Texto:  columna derecha
                if (dialogoRT != null) { dialogoRT.anchorMin = new Vector2(0.30f, 0.06f); dialogoRT.anchorMax = new Vector2(0.97f, 0.62f); }
            }

            marcoRT.offsetMin = marcoRT.offsetMax = Vector2.zero;
            if (nombreRT  != null) { nombreRT.offsetMin  = nombreRT.offsetMax  = Vector2.zero; }
            if (dialogoRT != null) { dialogoRT.offsetMin = dialogoRT.offsetMax = Vector2.zero; }
        }

        marcoImg.sprite = l.esFran ? spriteFran : spriteProta;
        txtNombre.text  = "[" + l.nombre + "]:";
        txtDialogo.text = l.texto;

        bool esUltima = (idx == lineas.Length - 1);
        txtContinuar.text = esUltima
            ? "HAZ CLICK o ESPACIO para ¡ARRANCAR!"
            : "Presioná ESPACIO o HAZ CLICK para continuar";
    }

    private void OnContinuar()
    {
        lineaActual++;
        MostrarLinea(lineaActual);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------
    private static GameObject GO(Transform p, string n)
    {
        GameObject go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(p, false);
        return go;
    }

    private static void SetAnchors(GameObject go, float ax, float ay, float bx, float by)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(ax, ay);
        rt.anchorMax = new Vector2(bx, by);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI TMPU(Transform p, string n,
        float ax, float ay, float bx, float by)
    {
        GameObject go = GO(p, n);
        SetAnchors(go, ax, ay, bx, by);
        return go.AddComponent<TextMeshProUGUI>();
    }
}
