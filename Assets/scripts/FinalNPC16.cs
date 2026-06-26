using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Encuentro final con NPC16 (Fran - Obelisco).
/// Muestra el inventario del jugador con multi-seleccion.
/// El jugador elige los objetos a entregar y presiona ENTREGAR.
/// Los 5 correctos = GANASTE. Cualquier error = PERDISTE.
/// </summary>
public class FinalNPC16 : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Objetos requeridos (sin ValueTuples para maxima compatibilidad)
    // -----------------------------------------------------------------------
    private static readonly string[] KEYS_REQUERIDOS =
        { "caja de herramientas", "bateria", "mapa", "anafe", "guantes" };

    private const int MAX_SEL = 5;

    // -----------------------------------------------------------------------
    // Estado
    // -----------------------------------------------------------------------
    private struct SlotVisual { public Image bg; public Outline borde; }

    private HashSet<string>             seleccionados  = new HashSet<string>();
    private Dictionary<string, SlotVisual> slots       = new Dictionary<string, SlotVisual>();

    private Canvas              canvas;
    private TextMeshProUGUI     txtContador;
    private Button              btnEntregar;
    private GameObject          panelPrincipal;

    // -----------------------------------------------------------------------
    void Start()
    {
        Time.timeScale = 0f;
        BuildUI();
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    // -----------------------------------------------------------------------
    // UI principal
    // -----------------------------------------------------------------------
    private void BuildUI()
    {
        GameObject cgo = new GameObject("FinalNPC16Canvas");
        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        CanvasScaler sc = cgo.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight  = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(cgo);

        // Fondo
        SetImage(MakeRect(cgo.transform, "Fondo", 0f, 0f, 1f, 1f),
            new Color(0f, 0f, 0f, 0.80f));

        // Panel principal
        panelPrincipal = MakeRect(cgo.transform, "Panel", 0.12f, 0.06f, 0.88f, 0.94f);
        SetImage(panelPrincipal, new Color(0.10f, 0.08f, 0.05f, 0.97f));
        SetBorde(panelPrincipal, new Color(0.70f, 0.52f, 0.12f, 1f), 3f);

        // Cabecera
        SetImage(MakeRect(panelPrincipal.transform, "Header", 0f, 0.88f, 1f, 1f),
            new Color(0.18f, 0.14f, 0.06f, 1f));
        MakeTxt(panelPrincipal.transform, "Titulo", 0.03f, 0.89f, 0.97f, 0.99f,
            "<color=#F5C842>FRAN FAFI</color>  -  Selecciona los objetos y presiona ENTREGAR",
            24f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

        // Que pide Fran
        MakeTxt(panelPrincipal.transform, "Sub", 0.03f, 0.82f, 0.97f, 0.89f,
            "Necesito: <color=#F0D060>caja de herramientas - bateria - mapa - anafe - guantes</color>",
            18f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft,
            new Color(0.85f, 0.80f, 0.65f));

        // Divisor
        SetImage(MakeRect(panelPrincipal.transform, "Sep", 0.02f, 0.805f, 0.98f, 0.818f),
            new Color(0.60f, 0.45f, 0.12f, 0.7f));

        // Contador
        txtContador = MakeTxt(panelPrincipal.transform, "Contador",
            0.03f, 0.76f, 0.97f, 0.81f,
            "Seleccionados: 0 / 5",
            20f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft,
            new Color(1f, 0.85f, 0.3f));

        // Grid de inventario
        BuildGrid(panelPrincipal.transform);

        // Boton ENTREGAR
        GameObject btnGO = MakeRect(panelPrincipal.transform, "BtnEntregar",
            0.30f, 0.02f, 0.70f, 0.11f);
        SetImage(btnGO, new Color(0.18f, 0.42f, 0.10f, 1f));
        SetBorde(btnGO, new Color(0.35f, 0.85f, 0.20f, 0.8f), 2f);
        btnEntregar = btnGO.AddComponent<Button>();
        btnEntregar.interactable = false;
        btnEntregar.onClick.AddListener(OnEntregar);
        MakeTxt(btnGO.transform, "Lbl", 0f, 0f, 1f, 1f,
            "ENTREGAR", 26f, FontStyles.Bold, TextAlignmentOptions.Center,
            new Color(0.95f, 0.92f, 0.75f));

        ColorBlock cb    = btnEntregar.colors;
        cb.normalColor   = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 0.8f);
        cb.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);
        btnEntregar.colors = cb;
    }

    // -----------------------------------------------------------------------
    // Grid de items
    // -----------------------------------------------------------------------
    private void BuildGrid(Transform parent)
    {
        if (InventoryManager.Instance == null) return;

        List<InventoryManager.Item> disponibles = new List<InventoryManager.Item>();
        foreach (InventoryManager.Item it in InventoryManager.Instance.items)
            if (it.quantity > 0 && it.name != "nafta")
                disponibles.Add(it);

        if (disponibles.Count == 0)
        {
            MakeTxt(parent, "Empty", 0.05f, 0.12f, 0.95f, 0.76f,
                "No tenes objetos en el inventario.",
                22f, FontStyles.Normal, TextAlignmentOptions.Center,
                new Color(0.8f, 0.7f, 0.5f));
            return;
        }

        int   cols  = 6;
        float slotW = 0.94f / cols;
        float areaH = 0.62f;
        int   rows  = Mathf.CeilToInt(disponibles.Count / (float)cols);
        float slotH = areaH / Mathf.Max(rows, 1);

        for (int i = 0; i < disponibles.Count; i++)
        {
            int c = i % cols;
            int r = i / cols;
            float x0 = 0.03f + c * slotW;
            float y0 = 0.74f - (r + 1) * slotH;
            float x1 = x0 + slotW - 0.008f;
            float y1 = y0 + slotH - 0.008f;
            BuildSlot(parent, disponibles[i], x0, y0, x1, y1);
        }
    }

    private void BuildSlot(Transform parent, InventoryManager.Item item,
        float x0, float y0, float x1, float y1)
    {
        GameObject go = MakeRect(parent, "Slot_" + item.name, x0, y0, x1, y1);
        Image   bg  = SetImage(go, new Color(0.16f, 0.13f, 0.09f, 1f));
        Outline brd = SetBorde(go, new Color(0.45f, 0.38f, 0.20f, 0.9f), 1.5f);

        if (item.customIcon != null)
        {
            GameObject ico = MakeRect(go.transform, "Icon", 0.08f, 0.40f, 0.92f, 0.95f);
            Image icoImg = ico.AddComponent<Image>();
            icoImg.sprite         = item.customIcon;
            icoImg.preserveAspect = true;
        }

        MakeTxt(go.transform, "Name", 0f, 0f, 1f, 0.38f,
            item.name, 12f, FontStyles.Normal, TextAlignmentOptions.Center,
            new Color(0.88f, 0.82f, 0.65f));

        Button btn = go.AddComponent<Button>();
        ColorBlock cb   = btn.colors;
        cb.highlightedColor = new Color(0.9f, 0.85f, 0.65f, 0.15f);
        btn.colors = cb;

        SlotVisual sv; sv.bg = bg; sv.borde = brd;
        slots[item.name] = sv;

        string nombre = item.name;
        btn.onClick.AddListener(delegate { ToggleSlot(nombre); });
    }

    private void ToggleSlot(string nombre)
    {
        SlotVisual sv;
        if (!slots.TryGetValue(nombre, out sv)) return;

        if (seleccionados.Contains(nombre))
        {
            seleccionados.Remove(nombre);
            sv.bg.color        = new Color(0.16f, 0.13f, 0.09f, 1f);
            sv.borde.effectColor = new Color(0.45f, 0.38f, 0.20f, 0.9f);
        }
        else
        {
            if (seleccionados.Count >= MAX_SEL) return;
            seleccionados.Add(nombre);
            sv.bg.color          = new Color(0.15f, 0.40f, 0.10f, 1f);
            sv.borde.effectColor = new Color(0.30f, 0.90f, 0.20f, 1f);
        }

        int n = seleccionados.Count;
        if (txtContador != null)
            txtContador.text = "Seleccionados: " + n + " / " + MAX_SEL;

        if (btnEntregar != null)
            btnEntregar.interactable = (n == MAX_SEL);
    }

    // -----------------------------------------------------------------------
    // Validacion
    // -----------------------------------------------------------------------
    private void OnEntregar()
    {
        if (seleccionados.Count != MAX_SEL) return;

        // Verificar que los seleccionados coincidan exactamente con los requeridos
        bool gano = true;
        foreach (string k in KEYS_REQUERIDOS)
            if (!seleccionados.Contains(k)) { gano = false; break; }
        if (gano)
            foreach (string s in seleccionados)
                if (!IsRequerido(s)) { gano = false; break; }

        if (gano)
        {
            foreach (string k in KEYS_REQUERIDOS)
                if (InventoryManager.Instance != null)
                    InventoryManager.Instance.RemoveItem(k, 1);
            NPCInteraction.MarcarTratoAceptado("npc16");
        }

        MostrarResultado(gano);
    }

    private bool IsRequerido(string nombre)
    {
        foreach (string k in KEYS_REQUERIDOS)
            if (k == nombre) return true;
        return false;
    }

    // -----------------------------------------------------------------------
    // Pantalla resultado
    // -----------------------------------------------------------------------
    private void MostrarResultado(bool gano)
    {
        if (panelPrincipal != null)
            panelPrincipal.SetActive(false);

        GameObject res = MakeRect(canvas.transform, "Resultado",
            0.15f, 0.20f, 0.85f, 0.80f);
        SetImage(res, new Color(0.06f, 0.05f, 0.03f, 0.98f));
        SetBorde(res, gano
            ? new Color(0.4f, 0.9f, 0.2f, 0.9f)
            : new Color(0.9f, 0.2f, 0.2f, 0.9f), 4f);

        if (gano)
        {
            MakeTxt(res.transform, "TxtGano", 0.05f, 0.55f, 0.95f, 0.95f,
                "<color=#5FD060>GANASTE!</color>",
                64f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            MakeTxt(res.transform, "TxtMsg", 0.08f, 0.28f, 0.92f, 0.58f,
                "Pudiste entrar al bunker y\npasar el apocalipsis seguro.",
                30f, FontStyles.Normal, TextAlignmentOptions.Center,
                new Color(0.90f, 0.88f, 0.75f));
        }
        else
        {
            MakeTxt(res.transform, "TxtPerdio", 0.05f, 0.55f, 0.95f, 0.95f,
                "<color=#D04040>PERDISTE!</color>",
                64f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            MakeTxt(res.transform, "TxtMsg", 0.08f, 0.28f, 0.92f, 0.58f,
                "Esos no son los objetos que te pedi.\nEl apocalipsis te atrapo...",
                28f, FontStyles.Normal, TextAlignmentOptions.Center,
                new Color(0.90f, 0.80f, 0.75f));
        }

        GameObject btnGO = MakeRect(res.transform, "BtnOk",
            0.28f, 0.05f, 0.72f, 0.22f);
        SetImage(btnGO, gano
            ? new Color(0.14f, 0.40f, 0.10f)
            : new Color(0.40f, 0.10f, 0.10f));
        SetBorde(btnGO, gano
            ? new Color(0.3f, 0.9f, 0.2f, 0.7f)
            : new Color(0.9f, 0.3f, 0.2f, 0.7f), 2f);
        Button btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(IrAlMenu);
        MakeTxt(btnGO.transform, "Lbl", 0f, 0f, 1f, 1f,
            "VOLVER AL MENU", 22f, FontStyles.Bold,
            TextAlignmentOptions.Center, new Color(0.95f, 0.92f, 0.75f));
    }

    private void IrAlMenu()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.RestartGameLevel();
        Time.timeScale = 1f;
        Destroy(canvas.gameObject);
        Destroy(gameObject);
        SceneManager.LoadScene("menu");
    }

    // -----------------------------------------------------------------------
    // Helpers UI — sin lambdas complejas ni ValueTuples
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

    private static Image SetImage(GameObject go, Color c)
    {
        Image img = go.AddComponent<Image>();
        img.color = c;
        return img;
    }

    private static Outline SetBorde(GameObject go, Color c, float s)
    {
        Outline o = go.AddComponent<Outline>();
        o.effectColor    = c;
        o.effectDistance = new Vector2(s, -s);
        return o;
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
