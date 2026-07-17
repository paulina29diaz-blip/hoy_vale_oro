using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reemplaza la intro de video de IA por un storyboard interactivo ilustrado.
/// Muestra diapositivas narrativas con textos y fondos reales del juego.
/// Presioná ESPACIO o haz click para avanzar.
/// </summary>
public class VideoSceneManager : MonoBehaviour
{
    public string nextSceneName = "menu";

    private struct Diapositiva
    {
        public string texto;
        public string pathSpriteFondo;
    }

    private readonly Diapositiva[] diapositivas = new Diapositiva[]
    {
        new Diapositiva 
        { 
            texto = "El mundo tal como lo conocías colapsó de un día para el otro...\nEl dinero en papel dejó de tener valor, y ahora lo único que vale oro es lo que puedas conseguir e intercambiar.", 
            pathSpriteFondo = "Sprites/intro_ruta" 
        },
        new Diapositiva 
        { 
            texto = "Dicen que en el Obelisco de Buenos Aires hay un refugio militar fortificado.\nEs tu destino final, tu única esperanza de sobrevivir al invierno que se avecina.", 
            pathSpriteFondo = "Sprites/intro_obelisco" 
        },
        new Diapositiva 
        { 
            texto = "Pero el guardia del búnker no te dejará entrar con las manos vacías.\nDebés conseguir: una caja de herramientas, una batería, un mapa de la zona, un anafe y guantes de trabajo.", 
            pathSpriteFondo = "Sprites/intro_ruta" 
        },
        new Diapositiva 
        { 
            texto = "Cargá lo que encuentres en tu vieja camioneta Rastrojero, administrá con cuidado tu combustible, negociá con la gente en la ruta y hacé que tus pertenencias valgan. ¡Buena suerte, viajero!", 
            pathSpriteFondo = "Sprites/intro_obelisco" 
        }
    };

    private int indexActual = 0;
    private bool cambiandoEscena = false;

    // UI refs
    private Image imgFondo;
    private TextMeshProUGUI txtNarrativa;
    private TextMeshProUGUI txtPaginacion;
    private Image fadeOverlay;
    
    private float fadeDuration = 0.8f;
    private bool fadingIn = true;
    private float fadeTimer = 0f;

    void Start()
    {
        // Limpiar cualquier video player de la escena para que no haga ruido
        UnityEngine.Video.VideoPlayer vp = GetComponent<UnityEngine.Video.VideoPlayer>();
        if (vp != null) vp.enabled = false;

        CreateStoryboardUI();
        MostrarDiapositiva(0);
    }

    void Update()
    {
        // Fade in inicial
        if (fadingIn)
        {
            fadeTimer += Time.deltaTime;
            float t = fadeTimer / fadeDuration;
            if (fadeOverlay != null)
                fadeOverlay.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(t));
            if (t >= 1f)
            {
                fadingIn = false;
                if (fadeOverlay != null)
                    fadeOverlay.gameObject.SetActive(false);
            }
        }

        // Avanzar con click, espacio, enter o tap
        if (!cambiandoEscena && !fadingIn &&
            (Input.GetKeyDown(KeyCode.Space) ||
             Input.GetKeyDown(KeyCode.Return) ||
             Input.GetMouseButtonDown(0)))
        {
            AvanzarDiapositiva();
        }
    }

    private void MostrarDiapositiva(int index)
    {
        if (index < 0 || index >= diapositivas.Length) return;

        Diapositiva diap = diapositivas[index];

        // Cargar y aplicar el fondo
        Sprite fondoSprite = Resources.Load<Sprite>(diap.pathSpriteFondo);
        if (fondoSprite != null && imgFondo != null)
        {
            imgFondo.sprite = fondoSprite;
        }

        // Aplicar texto
        if (txtNarrativa != null)
        {
            txtNarrativa.text = diap.texto;
        }

        // Paginación
        if (txtPaginacion != null)
        {
            txtPaginacion.text = $"{index + 1} / {diapositivas.Length}";
        }
    }

    private void AvanzarDiapositiva()
    {
        indexActual++;
        if (indexActual < diapositivas.Length)
        {
            MostrarDiapositiva(indexActual);
        }
        else
        {
            IrAlMenu();
        }
    }

    private void IrAlMenu()
    {
        if (cambiandoEscena) return;
        cambiandoEscena = true;
        SceneManager.LoadScene(nextSceneName);
    }

    private void CreateStoryboardUI()
    {
        // Canvas
        GameObject canvasGO = new GameObject("StoryboardCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Imagen de Fondo Ilustrado
        GameObject bgGO = new GameObject("FondoIlustrado", typeof(RectTransform));
        bgGO.transform.SetParent(canvasGO.transform, false);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        imgFondo = bgGO.AddComponent<Image>();
        imgFondo.color = Color.white;
        imgFondo.preserveAspect = false;

        // Overlay oscuro general para mejorar contraste general de la escena
        GameObject dimGO = new GameObject("DimOverlay", typeof(RectTransform));
        dimGO.transform.SetParent(canvasGO.transform, false);
        RectTransform dimRT = dimGO.GetComponent<RectTransform>();
        dimRT.anchorMin = Vector2.zero;
        dimRT.anchorMax = Vector2.one;
        dimRT.offsetMin = dimRT.offsetMax = Vector2.zero;
        Image dimImg = dimGO.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.25f); // oscurece un poco la ilustración de fondo

        // Panel inferior de diálogo (Novela Gráfica)
        GameObject panelGO = new GameObject("TextoPanel", typeof(RectTransform));
        panelGO.transform.SetParent(canvasGO.transform, false);
        RectTransform pRT = panelGO.GetComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0.1f, 0.08f);
        pRT.anchorMax = new Vector2(0.9f, 0.32f);
        pRT.offsetMin = pRT.offsetMax = Vector2.zero;
        
        Image panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.05f, 0.05f, 0.88f); // Fondo gris muy oscuro translúcido con excelente contraste

        // Borde decorativo para el panel
        GameObject borderGO = new GameObject("Border", typeof(RectTransform));
        borderGO.transform.SetParent(panelGO.transform, false);
        RectTransform bRT = borderGO.GetComponent<RectTransform>();
        bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;
        bRT.offsetMin = new Vector2(-4, -4); bRT.offsetMax = new Vector2(4, 4);
        Image borderImg = borderGO.AddComponent<Image>();
        borderImg.color = new Color(0.7f, 0.55f, 0.3f, 0.6f); // Borde color bronce/óxido gastado
        panelGO.transform.SetAsLastSibling(); // El panel arriba del borde interior

        // Texto de la narrativa
        GameObject textGO = new GameObject("TextoNarrativo", typeof(RectTransform));
        textGO.transform.SetParent(panelGO.transform, false);
        RectTransform tRT = textGO.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0.04f, 0.15f);
        tRT.anchorMax = new Vector2(0.96f, 0.85f);
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;

        txtNarrativa = textGO.AddComponent<TextMeshProUGUI>();
        txtNarrativa.fontSize = 28f;
        txtNarrativa.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        txtNarrativa.alignment = TextAlignmentOptions.Center;
        txtNarrativa.fontStyle = FontStyles.Normal;

        // Paginación sutil en la esquina inferior derecha del panel
        GameObject pageGO = new GameObject("PaginacionText", typeof(RectTransform));
        pageGO.transform.SetParent(panelGO.transform, false);
        RectTransform pgRT = pageGO.GetComponent<RectTransform>();
        pgRT.anchorMin = new Vector2(0.9f, 0.04f);
        pgRT.anchorMax = new Vector2(0.98f, 0.15f);
        pgRT.offsetMin = pgRT.offsetMax = Vector2.zero;

        txtPaginacion = pageGO.AddComponent<TextMeshProUGUI>();
        txtPaginacion.fontSize = 18f;
        txtPaginacion.color = new Color(1f, 1f, 1f, 0.5f);
        txtPaginacion.alignment = TextAlignmentOptions.MidlineRight;

        // Botón o indicación de "Continuar [Espacio]"
        GameObject contGO = new GameObject("ContinuarText", typeof(RectTransform));
        contGO.transform.SetParent(panelGO.transform, false);
        RectTransform cRT = contGO.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.02f, 0.04f);
        cRT.anchorMax = new Vector2(0.4f, 0.15f);
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;

        TextMeshProUGUI txtContinuar = contGO.AddComponent<TextMeshProUGUI>();
        txtContinuar.text = "Presioná  ESPACIO  para continuar...";
        txtContinuar.fontSize = 18f;
        txtContinuar.fontStyle = FontStyles.Italic;
        txtContinuar.color = new Color(0.7f, 0.55f, 0.3f, 0.8f);
        txtContinuar.alignment = TextAlignmentOptions.MidlineLeft;

        // Fade overlay (para transiciones suaves al inicio)
        GameObject fadeGO = new GameObject("FadeOverlay", typeof(RectTransform));
        fadeGO.transform.SetParent(canvasGO.transform, false);
        RectTransform fadeRT = fadeGO.GetComponent<RectTransform>();
        fadeRT.anchorMin = Vector2.zero;
        fadeRT.anchorMax = Vector2.one;
        fadeRT.offsetMin = fadeRT.offsetMax = Vector2.zero;
        fadeOverlay = fadeGO.AddComponent<Image>();
        fadeOverlay.color = Color.black;
    }
}
