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
    // Flag de sesión (usando PlayerPrefs para persistencia fiable)
    // -----------------------------------------------------------------------
    public static void ResetearParaNuevaPartida()
    {
        PlayerPrefs.DeleteKey("IntroMostrada");
        PlayerPrefs.Save();
    }

    public static void IntentarMostrar()
    {
        if (PlayerPrefs.GetInt("IntroMostrada", 0) == 1) return;
        PlayerPrefs.SetInt("IntroMostrada", 1);
        PlayerPrefs.Save();
        
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
        new Linea { esFran = false, nombre = "HERNAN PIQUIRE",
            texto = "¿Qué? ¿Un virus? ¿El mundo se está terminando? No puedo creer lo que me estás diciendo, Fran..." },
        new Linea { esFran = false, nombre = "HERNAN PIQUIRE",
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

        // ── Marco: centrado en pantalla (35%–65% vertical)
        //   Ancho: 0.15 → 0.85  (70% de pantalla, centrado)
        //   Alto:  0.35 → 0.65  (30% de pantalla)
        GameObject marco = GO(cgo.transform, "Marco");
        SetAnchors(marco, 0.15f, 0.35f, 0.85f, 0.65f);

        marcoImg                = marco.AddComponent<Image>();
        marcoImg.sprite         = spriteProta;
        marcoImg.type           = Image.Type.Simple;
        marcoImg.preserveAspect = false;
        marcoImg.color          = Color.white;
        marcoRT                 = marco.GetComponent<RectTransform>();

        // Nombre y texto: posición inicial (sobrescrita dinámicamente en MostrarLinea)
        txtNombre = TMPU(marco.transform, "Nombre", 0.05f, 0.70f, 0.70f, 0.94f);
        txtNombre.fontSize            = 28f;
        txtNombre.fontStyle           = FontStyles.Bold;
        txtNombre.color               = new Color(1f, 0.82f, 0.15f);
        txtNombre.alignment           = TextAlignmentOptions.MidlineLeft;
        txtNombre.enableWordWrapping  = false;
        txtNombre.overflowMode        = TextOverflowModes.Ellipsis;

        txtDialogo = TMPU(marco.transform, "Texto", 0.05f, 0.08f, 0.70f, 0.70f);
        txtDialogo.fontSize           = 22f;
        txtDialogo.color              = new Color(0.95f, 0.92f, 0.82f);
        txtDialogo.alignment          = TextAlignmentOptions.TopLeft;
        txtDialogo.enableWordWrapping = true;
        txtDialogo.overflowMode       = TextOverflowModes.Truncate;

        // Hint centrado debajo del marco
        txtContinuar = TMPU(cgo.transform, "TxtContinuar", 0.15f, 0.31f, 0.85f, 0.355f);
        txtContinuar.text      = "Presioná ESPACIO o HAZ CLICK para continuar";
        txtContinuar.fontSize  = 16f;
        txtContinuar.color     = new Color(1f, 1f, 1f, 0.75f);
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
                // Fran: teléfono a la DERECHA → texto ocupa la zona izquierda
                marcoRT.anchorMin = new Vector2(0.12f, 0.35f);
                marcoRT.anchorMax = new Vector2(0.88f, 0.65f);

                // Nombre arriba a la izquierda (dentro del área oscura, lejos del borde)
                if (nombreRT != null)  { nombreRT.anchorMin  = new Vector2(0.05f, 0.72f); nombreRT.anchorMax  = new Vector2(0.67f, 0.93f); }
                // Texto debajo del nombre, zona izquierda
                if (dialogoRT != null) { dialogoRT.anchorMin = new Vector2(0.05f, 0.10f); dialogoRT.anchorMax = new Vector2(0.67f, 0.72f); }
            }
            else
            {
                // Protagonista: retrato a la IZQUIERDA → texto ocupa la zona derecha
                marcoRT.anchorMin = new Vector2(0.15f, 0.35f);
                marcoRT.anchorMax = new Vector2(0.85f, 0.65f);

                // Nombre arriba a la derecha (dentro del área oscura)
                if (nombreRT != null)  { nombreRT.anchorMin  = new Vector2(0.30f, 0.72f); nombreRT.anchorMax  = new Vector2(0.95f, 0.93f); }
                // Texto debajo del nombre, zona derecha
                if (dialogoRT != null) { dialogoRT.anchorMin = new Vector2(0.30f, 0.10f); dialogoRT.anchorMax = new Vector2(0.95f, 0.72f); }
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
