using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class DialogoManager : MonoBehaviour
{
    public GameObject panelDialogo;
    public GameObject panelBotones;
    public GameObject panelResultado;
    public GameObject botonContinuar;

    public TextMeshProUGUI textoNPC;
    public TextMeshProUGUI textoResultado;

    public static DialogoManager Instance;

    private bool dialogoAbierto = false;

    private enum State
    {
        Inicio,                  // NPC starts: "Tengo combustible..."
        JugadorHablo,            // Player box shown with response
        NpcContesto,             // NPC box updates to react to strategy
        JugadorEligeInventario,  // NPC box shows "Elegí qué objeto..." and inventory buttons are shown
        JugadorOfrecioObjeto,    // Player box shows "Te ofrezco mi..."
        NpcReaccionoObjeto,      // NPC box shows final reaction (success/failure)
        FinResultado             // Showing deal results in centered popup
    }

    private enum TipoRespuesta
    {
        Honesto,
        Chamuyero,
        Mentiroso
    }

    private State currentState = State.Inicio;
    private TipoRespuesta chosenType;
    private bool exito = false;
    private string npcReactionText; // Stores computed reaction of NPC

    // Original coordinates of the text boxes
    private Vector2 originalNpcPos;
    private Vector2 originalPlayerPos;

    // Custom UI Containers (post-apocalyptic design)
    private GameObject npcBoxObjeto;
    private TextMeshProUGUI npcBoxTexto;
    private TextMeshProUGUI npcNameHeader;

    private GameObject playerBoxObjeto;
    private TextMeshProUGUI playerBoxTexto;
    private TextMeshProUGUI playerNameHeader;

    private GameObject btnContinuarDialogoObjeto;
    private GameObject btnAbrirInventarioCustom;
    private GameObject btnRechazarTratoCustom;
    private GameObject btnHonestoDesafioCustom;
    private GameObject btnChamuyeroDesafioCustom;
    private GameObject btnMentirosoDesafioCustom;

    // Center Popup Elements
    private GameObject popupObjeto;
    private TextMeshProUGUI popupTexto;

    private Sprite boxSprite;

    private List<EscenarioDialogo> escenarios = new List<EscenarioDialogo>();
    private EscenarioDialogo escenarioActual;
    private OpcionDialogo opcionSeleccionada;
    private int randomFuelAmount = 0;
    private int totalFallosNPC = 0;

    void Start()
    {
        Instance = this;
        // Swap background based on interacted NPC
        GameObject bgObj = GameObject.Find("trueque1_0");
        if (bgObj != null)
        {
            SpriteRenderer sr = bgObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                string npc = NPCInteraction.lastInteractedNPC.ToLower();
                if (npc.Contains("combativa_0") || npc == "npc1")
                {
                    EstablecerImagenTrueque(sr, "Sprites/trueque1");
                }
                else if (npc.Contains("vagabundo") || npc == "npc2")
                {
                    EstablecerImagenTrueque(sr, "Sprites/trueque2");
                }
                else if (npc.Contains("npc3"))
                {
                    EstablecerImagenTrueque(sr, "Sprites/trueque3");
                }
                else if (npc.Contains("npc4"))
                {
                    EstablecerImagenTrueque(sr, "Sprites/truequenpc5");
                }
                else if (npc.Contains("npc5"))
                {
                    EstablecerImagenTrueque(sr, "Sprites/truequenpc4");
                }
                else if (npc.Contains("npc6"))
                {
                    EstablecerImagenTrueque(sr, "Sprites/truequenpc6");
                }
                else if (npc.Contains("npc7"))
                {
                    EstablecerImagenTrueque(sr, "Sprites/truequenpc7");
                }
                else if (npc.Contains("npc8"))
                {
                    EstablecerImagenTrueque(sr, "Sprites/truequenpc8");
                }
                else if (npc.Contains("npc9"))
                {
                    EstablecerImagenTrueque(sr, "Sprites/truequenpc9");
                }
                else if (npc.Contains("npc10"))
                {
                    EstablecerImagenTrueque(sr, "Sprites/truequenpc10");
                }
                else if (npc.Contains("npc11"))
                {
                    EstablecerImagenTrueque(sr, "Sprites/truequenpc11");
                }
                else if (npc.Contains("npc12"))
                {
                    EstablecerImagenTrueque(sr, "Sprites/truequenpc12");
                }
                else if (npc.Contains("npc13"))
                {
                    EstablecerImagenTrueque(sr, "Sprites/truequenpc13");
                }
                else if (npc.Contains("npc14"))
                {
                    EstablecerImagenTrueque(sr, "Sprites/truequenpc14");
                }
                else if (npc.Contains("npc15"))
                {
                    EstablecerImagenTrueque(sr, "Sprites/truequenpc15");
                }
                else if (npc.Contains("npc16"))
                {
                    EstablecerImagenTrueque(sr, "Sprites/truequenpc16");
                }
                else
                {
                    Sprite trueque1Sprite = CargarSpriteDesdeResources("Sprites/trueque1");
                    if (trueque1Sprite != null)
                    {
                        sr.sprite = trueque1Sprite;
                    }
                }
            }
        }

        // Auto-assign references if they were lost during reload or edit
        if (panelDialogo == null || panelBotones == null || panelResultado == null || botonContinuar == null || textoNPC == null || textoResultado == null)
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
                {
                    if (panelDialogo == null && t.name == "paneldialogo") panelDialogo = t.gameObject;
                    if (panelBotones == null && t.name == "panelbotones") panelBotones = t.gameObject;
                    if (panelResultado == null && t.name == "panelresultado") panelResultado = t.gameObject;
                    if (botonContinuar == null && t.name == "botoncontinuar") botonContinuar = t.gameObject;
                    if (textoNPC == null && t.name == "textonpc") textoNPC = t.GetComponent<TextMeshProUGUI>();
                    if (textoResultado == null && t.name == "textoresultado") textoResultado = t.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        // Safety fallback to prevent crashes if something remains unassigned
        if (panelDialogo == null || panelResultado == null || textoNPC == null || textoResultado == null)
        {
            Debug.LogError("Error: Algunos componentes del DialogoManager no pudieron ser asignados.");
            return;
        }

        // Configure CanvasScaler to scale with screen size (reference resolution 1920x1080)
        Canvas canvasObj = panelDialogo.GetComponentInParent<Canvas>();
        if (canvasObj != null)
        {
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasObj.gameObject.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        PopularEscenarios();
        if (escenarios.Count > 0)
        {
            string targetNPCName = GetInteractedNPCDisplayName();

            escenarioActual = null;
            if (!string.IsNullOrEmpty(targetNPCName))
            {
                foreach (var e in escenarios)
                {
                    if (e.npcNombre.Equals(targetNPCName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        escenarioActual = e;
                        break;
                    }
                }
            }

            if (escenarioActual == null)
            {
                escenarioActual = escenarios[Random.Range(0, escenarios.Count)];
            }
        }
        else
        {
            escenarioActual = new EscenarioDialogo();
        }

        if (escenarioActual != null && 
            (escenarioActual.npcNombre == "Roxana" || 
             escenarioActual.npcNombre == "Sergio" || 
             escenarioActual.npcNombre == "Enrique" || 
             escenarioActual.npcNombre == "Carla" || 
             escenarioActual.npcNombre == "Lucas y Gonza"))
        {
            randomFuelAmount = Random.Range(20, 51); // 20 to 50 inclusive
        }

        // Temporarily activate panels to force layout calculations and update canvases
        bool dialogWasActive = panelDialogo.activeSelf;
        bool resultWasActive = panelResultado.activeSelf;
        
        panelDialogo.SetActive(true);
        panelResultado.SetActive(true);
        Canvas.ForceUpdateCanvases();

        // 1. Capture design-time anchored positions of the original text components
        if (textoNPC != null)
        {
            originalNpcPos = textoNPC.GetComponent<RectTransform>().anchoredPosition;
        }
        else
        {
            originalNpcPos = new Vector2(431, 416);
        }

        if (textoResultado != null)
        {
            originalPlayerPos = textoResultado.GetComponent<RectTransform>().anchoredPosition;
        }
        else
        {
            originalPlayerPos = new Vector2(-665, 24);
        }

        // 2. Find and store the original background box sprite, then deactivate the old background boxes
        boxSprite = null;
        Transform oldNpcBoxTrans = panelDialogo != null ? panelDialogo.transform.Find("cuadrodetexto_0") : null;
        if (oldNpcBoxTrans != null)
        {
            SpriteRenderer sr = oldNpcBoxTrans.GetComponent<SpriteRenderer>();
            if (sr != null) boxSprite = sr.sprite;
            oldNpcBoxTrans.gameObject.SetActive(false);
        }
        else
        {
            GameObject go = GameObject.Find("cuadrodetexto_0");
            if (go != null)
            {
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) boxSprite = sr.sprite;
                go.SetActive(false);
            }
        }

        Transform oldPlayerBoxTrans = panelResultado != null ? panelResultado.transform.Find("cuadrodetexto_0 (1)") : null;
        if (oldPlayerBoxTrans != null)
        {
            if (boxSprite == null)
            {
                SpriteRenderer sr = oldPlayerBoxTrans.GetComponent<SpriteRenderer>();
                if (sr != null) boxSprite = sr.sprite;
            }
            oldPlayerBoxTrans.gameObject.SetActive(false);
        }
        else
        {
            GameObject go = GameObject.Find("cuadrodetexto_0 (1)");
            if (go != null)
            {
                if (boxSprite == null)
                {
                    SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                    if (sr != null) boxSprite = sr.sprite;
                }
                go.SetActive(false);
            }
        }

        // Fallback to load from Resources if not found in the scene
        if (boxSprite == null)
        {
            boxSprite = Resources.Load<Sprite>("Sprites/cuadrodetexto");
        }

        // Restore original active states of the panels
        panelDialogo.SetActive(dialogWasActive);
        panelResultado.SetActive(resultWasActive);

        // Hide original text fields (they will be reactivated and reparented inside the new boxes)
        textoNPC.gameObject.SetActive(false);
        textoResultado.gameObject.SetActive(false);
        panelResultado.SetActive(false);

        // Create new custom containers that match the post-apocalyptic style
        CrearContenedoresDialogo();
        CrearPopupTrato();

        // Adjust response buttons: keep text and prevent stretching
        if (panelBotones != null)
        {
            foreach (Transform button in panelBotones.transform)
            {
                // Ensure Text (TMP) child is active so the label is visible
                Transform textTmp = button.Find("Text (TMP)");
                if (textTmp != null)
                {
                    textTmp.gameObject.SetActive(true);
                    TextMeshProUGUI tmpText = textTmp.GetComponent<TextMeshProUGUI>();
                    if (tmpText != null)
                    {
                        // White color with 75% opacity, bold, uppercase
                        tmpText.color = new Color(1f, 1f, 1f, 0.75f);
                        tmpText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
                        tmpText.text = tmpText.text.ToUpper();

                        // Make it a bit larger
                        tmpText.fontSize = 21f;

                        // Load Capture It SDF if available, fallback to Oswald Bold SDF or Anton SDF
                        TMP_FontAsset stencilFont = Resources.Load<TMP_FontAsset>("Capture It SDF");
                        if (stencilFont == null) stencilFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Capture It SDF");
                        if (stencilFont == null) stencilFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");
                        if (stencilFont == null) stencilFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Anton SDF");
                        
                        if (stencilFont != null)
                        {
                            tmpText.font = stencilFont;
                        }

                        // Apply weathered/eroded effects directly to the material
                        Material fontMat = tmpText.fontMaterial;
                        if (fontMat != null)
                        {
                            fontMat.SetFloat("_FaceDilate", -0.06f); // Slight erosion/wear
                            fontMat.SetFloat("_OutlineSoftness", 0.2f); // Faded outline edges
                            fontMat.SetFloat("_OutlineWidth", 0.18f); // Outline boundary
                            fontMat.SetColor("_OutlineColor", new Color(0.05f, 0.05f, 0.05f, 0.95f)); // Soot dark outline
                        }

                        // Add dark drop shadow outline for stencil paint look
                        Shadow shadow = textTmp.GetComponent<Shadow>();
                        if (shadow == null)
                        {
                            shadow = textTmp.gameObject.AddComponent<Shadow>();
                        }
                        shadow.effectColor = new Color(0.08f, 0.08f, 0.08f, 0.95f);
                        shadow.effectDistance = new Vector2(2f, -2f);
                    }

                    // Shift the text down a bit to match post-apocalyptic layouts
                    RectTransform textRect = textTmp.GetComponent<RectTransform>();
                    if (textRect != null)
                    {
                        textRect.anchorMin = Vector2.zero;
                        textRect.anchorMax = Vector2.one;
                        textRect.pivot = new Vector2(0.5f, 0.5f);
                        textRect.anchoredPosition = new Vector2(0f, -15f); // Lowered by 15 units
                    }
                }

                // Adjust size to match the sprite's ~1.55:1 aspect ratio
                RectTransform btnRect = button.GetComponent<RectTransform>();
                if (btnRect != null)
                {
                    float currentWidth = btnRect.sizeDelta.x;
                    float newHeight = currentWidth / 1.55f;
                    btnRect.sizeDelta = new Vector2(currentWidth, newHeight);
                }

                // Preserve aspect ratio to prevent stretching
                Image img = button.GetComponent<Image>();
                if (img != null)
                {
                    img.preserveAspect = true;
                }

                // Check if player has the items required for this button's trade type
                TipoRespuesta tipo = TipoRespuesta.Honesto;
                if (button.name.ToLower() == "botonhonesto") tipo = TipoRespuesta.Honesto;
                else if (button.name.ToLower() == "botonchamuyero") tipo = TipoRespuesta.Chamuyero;
                else if (button.name.ToLower() == "botonmentiroso") tipo = TipoRespuesta.Mentiroso;

                Button btnComp = button.GetComponent<Button>();
                if (btnComp != null)
                {
                    bool hasItems = TieneObjetosRequeridos(tipo);
                    btnComp.interactable = hasItems;

                    if (!hasItems)
                    {
                        Image btnImg = button.GetComponent<Image>();
                        if (btnImg != null)
                        {
                            btnImg.color = new Color(btnImg.color.r, btnImg.color.g, btnImg.color.b, 0.4f);
                        }

                        Transform missingTextTransform = button.Find("Text (TMP)");
                        if (missingTextTransform != null)
                        {
                            TextMeshProUGUI missingTextComp = missingTextTransform.GetComponent<TextMeshProUGUI>();
                            if (missingTextComp != null)
                            {
                                missingTextComp.text += "\n<color=red><size=11>(FALTAN OBJETOS)</size></color>";
                            }
                        }
                    }
                }
            }
        }

        InicializarBotones();
        AbrirDialogo();
    }

    private void EstablecerImagenTrueque(SpriteRenderer sr, string resourcePath)
    {
        Sprite truequeSprite = CargarSpriteDesdeResources(resourcePath);
        if (truequeSprite != null)
        {
            Sprite originalSprite = sr.sprite;
            if (originalSprite != null)
            {
                float origWidth = originalSprite.bounds.size.x;
                float origHeight = originalSprite.bounds.size.y;
                float newWidth = truequeSprite.bounds.size.x;
                float newHeight = truequeSprite.bounds.size.y;

                if (newWidth > 0f && newHeight > 0f)
                {
                    Vector3 currentScale = sr.transform.localScale;
                    sr.transform.localScale = new Vector3(
                        currentScale.x * (origWidth / newWidth),
                        currentScale.y * (origHeight / newHeight),
                        currentScale.z
                    );
                }
            }
            sr.sprite = truequeSprite;
        }
        else
        {
            Debug.LogError("No se pudo cargar el sprite de " + resourcePath + " desde Resources");
        }
    }

    private Vector2 GetCanvasPosFromWorldObject(Transform worldObj)
    {
        if (panelDialogo != null)
        {
            RectTransform parentRect = panelDialogo.GetComponent<RectTransform>();
            if (parentRect != null)
            {
                // Convert world position of the object directly into the local space of panelDialogo
                Vector3 worldPos = worldObj.position;
                Vector3 localPos = parentRect.InverseTransformPoint(worldPos);
                return (Vector2)localPos;
            }
        }
        return Vector2.zero;
    }

    public void AbrirDialogo()
    {
        if (dialogoAbierto)
            return;

        dialogoAbierto = true;
        totalFallosNPC = 0; // Reset fail counter

        panelDialogo.SetActive(true);
        panelResultado.SetActive(false);
        botonContinuar.SetActive(false);

        // Setup visibility for custom containers
        npcBoxObjeto.SetActive(true);
        playerBoxObjeto.SetActive(false);
        btnContinuarDialogoObjeto.SetActive(false);
        popupObjeto.SetActive(false);

        if (npcNameHeader != null)
        {
            string displayName = GetInteractedNPCDisplayName();
            npcNameHeader.text = displayName.ToUpper();
        }

        UpdateNpcPreguntaClue();

        // Configure the initial dialogue screen with ABRIR INVENTARIO and RECHAZAR TRATO buttons
        RegresarAInicioDialogo();
    }

    public void RespuestaHonesto()
    {
        string lastNPC = NPCInteraction.lastInteractedNPC.ToLower();
        bool isDesafio = lastNPC == "npc1" || lastNPC.Contains("combativa_0") ||
                         lastNPC == "npc4" ||
                         lastNPC == "npc7" ||
                         lastNPC == "npc10" ||
                         lastNPC == "npc13";
        if (currentState == State.Inicio && isDesafio)
        {
            SeleccionarRespuesta(TipoRespuesta.Honesto);
        }
    }

    public void RespuestaChamuyero()
    {
        string lastNPC = NPCInteraction.lastInteractedNPC.ToLower();
        bool isDesafio = lastNPC == "npc1" || lastNPC.Contains("combativa_0") ||
                         lastNPC == "npc4" ||
                         lastNPC == "npc7" ||
                         lastNPC == "npc10" ||
                         lastNPC == "npc13";
        if (currentState == State.Inicio && isDesafio)
        {
            SeleccionarRespuesta(TipoRespuesta.Chamuyero);
        }
    }

    public void RespuestaMentiroso()
    {
        string lastNPC = NPCInteraction.lastInteractedNPC.ToLower();
        bool isDesafio = lastNPC == "npc1" || lastNPC.Contains("combativa_0") ||
                         lastNPC == "npc4" ||
                         lastNPC == "npc7" ||
                         lastNPC == "npc10" ||
                         lastNPC == "npc13";
        if (currentState == State.Inicio && isDesafio)
        {
            SeleccionarRespuesta(TipoRespuesta.Mentiroso);
        }
    }

    private void SeleccionarRespuesta(TipoRespuesta tipo)
    {
        chosenType = tipo;
        panelBotones.SetActive(false);
        if (btnAbrirInventarioCustom != null) btnAbrirInventarioCustom.SetActive(false);
        if (btnRechazarTratoCustom != null) btnRechazarTratoCustom.SetActive(false);
        if (btnHonestoDesafioCustom != null) btnHonestoDesafioCustom.SetActive(false);
        if (btnChamuyeroDesafioCustom != null) btnChamuyeroDesafioCustom.SetActive(false);
        if (btnMentirosoDesafioCustom != null) btnMentirosoDesafioCustom.SetActive(false);
        
        // Hide NPC box, show player box and continue button
        npcBoxObjeto.SetActive(false);
        playerBoxObjeto.SetActive(true);
        btnContinuarDialogoObjeto.SetActive(true);
 
        // Position continue button below Player box
        RectTransform btnRect = btnContinuarDialogoObjeto.GetComponent<RectTransform>();
        if (btnRect != null)
        {
            btnRect.anchoredPosition = new Vector2(originalPlayerPos.x + 195f, originalPlayerPos.y - 320f);
        }
        
        currentState = State.JugadorHablo;
 
        string lastNPC = NPCInteraction.lastInteractedNPC.ToLower();
        bool correctStrategy = false;
        if (lastNPC == "npc1" || lastNPC.Contains("combativa_0"))
        {
            correctStrategy = (tipo == TipoRespuesta.Honesto);
        }
        else if (lastNPC == "npc4")
        {
            correctStrategy = (tipo == TipoRespuesta.Chamuyero);
        }
        else if (lastNPC == "npc7")
        {
            correctStrategy = (tipo == TipoRespuesta.Mentiroso);
        }
        else if (lastNPC == "npc10")
        {
            correctStrategy = (tipo == TipoRespuesta.Honesto);
        }
        else if (lastNPC == "npc13")
        {
            correctStrategy = (tipo == TipoRespuesta.Chamuyero);
        }

        exito = correctStrategy;
        if (!exito)
        {
            totalFallosNPC++;
        }

        opcionSeleccionada = null;

        switch (tipo)
        {
            case TipoRespuesta.Honesto:
                if (escenarioActual.honestoOptions.Count > 0)
                {
                    opcionSeleccionada = escenarioActual.honestoOptions[Random.Range(0, escenarioActual.honestoOptions.Count)];
                }
                break;

            case TipoRespuesta.Chamuyero:
                if (escenarioActual.chamuyeroOptions.Count > 0)
                {
                    opcionSeleccionada = escenarioActual.chamuyeroOptions[Random.Range(0, escenarioActual.chamuyeroOptions.Count)];
                }
                break;

            case TipoRespuesta.Mentiroso:
                if (escenarioActual.mentirosoOptions.Count > 0)
                {
                    opcionSeleccionada = escenarioActual.mentirosoOptions[Random.Range(0, escenarioActual.mentirosoOptions.Count)];
                }
                break;
        }

        if (opcionSeleccionada != null)
        {
            if (playerNameHeader != null)
            {
                playerNameHeader.text = "JUGADOR PRINCIPAL";
            }
            playerBoxTexto.text = "\"" + ReemplazarNombresEnTexto(opcionSeleccionada.jugadorTexto) + "\"";
            
            // Use success or failure reaction text depending on the strategy check
            npcReactionText = exito 
                ? "\"" + ReemplazarNombresEnTexto(opcionSeleccionada.npcExito) + "\""
                : "\"" + ReemplazarNombresEnTexto(opcionSeleccionada.npcFallo) + "\"";

            customDetailResult = exito 
                ? opcionSeleccionada.detalleExito 
                : opcionSeleccionada.detalleFallo;
        }
        else
        {
            playerBoxTexto.text = "...";
            npcReactionText = "\"Mostrame qué tenés para ofrecer.\"";
        }
    }

    public void OnContinuarClicked()
    {
        if (currentState == State.JugadorHablo)
        {
            playerBoxObjeto.SetActive(false);
            npcBoxObjeto.SetActive(true);
            npcBoxTexto.text = ReemplazarNombresEnTexto(npcReactionText);

            RectTransform btnRect = btnContinuarDialogoObjeto.GetComponent<RectTransform>();
            if (btnRect != null)
            {
                btnRect.anchoredPosition = new Vector2(originalNpcPos.x + 195f, originalNpcPos.y - 320f);
            }

            currentState = State.NpcContesto;
        }
        else if (currentState == State.NpcContesto)
        {
            if (exito)
            {
                CerrarDialogo();
            }
            else
            {
                if (totalFallosNPC >= 3)
                {
                    customDetailResult = "El NPC se cansó de tus mentiras y chamuyos.\nTrato cancelado.";
                    CerrarDialogo();
                }
                else
                {
                    RegresarAInicioDialogo();
                }
            }
        }
        else if (currentState == State.JugadorOfrecioObjeto)
        {
            // NPC responds to the specific item offered (final reaction)
            playerBoxObjeto.SetActive(false);
            npcBoxObjeto.SetActive(true);
            npcBoxTexto.text = ReemplazarNombresEnTexto(npcReactionText);

            // Position continue button below NPC box
            RectTransform btnRect = btnContinuarDialogoObjeto.GetComponent<RectTransform>();
            if (btnRect != null)
            {
                btnRect.anchoredPosition = new Vector2(originalNpcPos.x + 195f, originalNpcPos.y - 320f);
            }

            currentState = State.NpcReaccionoObjeto;
        }
        else if (currentState == State.NpcReaccionoObjeto)
        {
            if (exito)
            {
                // Close dialogue and display centered result popup
                CerrarDialogo();
            }
            else
            {
                if (totalFallosNPC >= 3)
                {
                    customDetailResult = "El NPC se cansó de tus ofertas inútiles.\nTrato cancelado.";
                    CerrarDialogo();
                }
                else
                {
                    // Reopen the inventory so they can choose a different item to try again
                    if (InventoryManager.Instance != null)
                    {
                        InventoryManager.Instance.OpenInventario();
                    }

                    if (totalFallosNPC > 1)
                    {
                        npcBoxTexto.text = "\"¡Te dije que eso no me sirve! Mostrame algo útil de una vez o cancelamos el trato.\"";
                    }
                    else
                    {
                        npcBoxTexto.text = "\"Eso no me sirve. Mostrame qué otra cosa tenés.\"";
                    }
                    btnContinuarDialogoObjeto.SetActive(false);

                    currentState = State.JugadorEligeInventario;
                }
            }
        }
    }

    public void CerrarDialogo()
    {
        if (currentState == State.NpcReaccionoObjeto || currentState == State.NpcContesto)
        {
            currentState = State.FinResultado;
            
            // Hide the active dialogue boxes
            npcBoxObjeto.SetActive(false);
            playerBoxObjeto.SetActive(false);
            btnContinuarDialogoObjeto.SetActive(false);
            if (btnAbrirInventarioCustom != null) btnAbrirInventarioCustom.SetActive(false);
            if (btnRechazarTratoCustom != null) btnRechazarTratoCustom.SetActive(false);
            if (btnHonestoDesafioCustom != null) btnHonestoDesafioCustom.SetActive(false);
            if (btnChamuyeroDesafioCustom != null) btnChamuyeroDesafioCustom.SetActive(false);
            if (btnMentirosoDesafioCustom != null) btnMentirosoDesafioCustom.SetActive(false);
            if (panelBotones != null) panelBotones.SetActive(false);

            // Configure and display the centered popup
            string header = exito ? "<color=#4CAF50>¡TRATO HECHO!</color>" : "<color=#F44336>¡TRATO NO HECHO!</color>";
            string details = "";

            if (exito)
            {
                ProcesarTruequeInventario();
                details = customDetailResult;
                if (randomFuelAmount > 0 && !string.IsNullOrEmpty(details))
                {
                    int finalFuel = randomFuelAmount;
                    if (totalFallosNPC > 0)
                    {
                        finalFuel = Mathf.Max(10, randomFuelAmount - (8 * totalFallosNPC));
                        details = $"Conseguiste {finalFuel}L de nafta (con penalización por dar vueltas)\nIntercambio completado";
                    }
                    else
                    {
                        details = System.Text.RegularExpressions.Regex.Replace(details, @"Conseguiste \d+L de nafta", "Conseguiste " + randomFuelAmount + "L de nafta");
                    }
                }
            }
            else
            {
                details = customDetailResult;
            }

            popupTexto.text = $"{header}\n\n{ReemplazarNombresEnTexto(details)}";
            popupObjeto.SetActive(true);
        }
    }

    void CrearContenedoresDialogo()
    {
        // 1. Create NPC dialogue box
        npcBoxObjeto = new GameObject("CustomNpcBox");
        npcBoxObjeto.transform.SetParent(panelDialogo.transform, false);

        RectTransform npcRect = npcBoxObjeto.AddComponent<RectTransform>();
        npcRect.anchorMin = new Vector2(0.5f, 0.5f);
        npcRect.anchorMax = new Vector2(0.5f, 0.5f);
        npcRect.pivot = new Vector2(0f, 1f); // TOP-LEFT PIVOT!
        npcRect.anchoredPosition = originalNpcPos + new Vector2(-30f, 35f); // Shift to align text precisely
        npcRect.sizeDelta = new Vector2(450, 330); // 450x330 matches the aspect ratio of cuadrodetexto

        Image npcImg = npcBoxObjeto.AddComponent<Image>();
        if (boxSprite != null)
        {
            npcImg.sprite = boxSprite;
            npcImg.type = Image.Type.Simple;
            npcImg.color = Color.white;
        }
        else
        {
            npcImg.color = new Color(0.09f, 0.08f, 0.08f, 0.98f);
        }

        // Create NPC name header (consistent title/header area)
        GameObject npcHeaderObj = new GameObject("NpcNameHeader");
        npcHeaderObj.transform.SetParent(npcBoxObjeto.transform, false);
        npcNameHeader = npcHeaderObj.AddComponent<TextMeshProUGUI>();
        npcNameHeader.fontStyle = FontStyles.Bold;
        npcNameHeader.fontSize = 20;
        npcNameHeader.alignment = TextAlignmentOptions.Center;
        npcNameHeader.color = new Color(1f, 0.55f, 0f, 1f); // Orange / Amber
        ApplyFont(npcNameHeader, true);

        RectTransform npcHeaderRect = npcHeaderObj.GetComponent<RectTransform>();
        npcHeaderRect.anchorMin = new Vector2(0f, 1f);
        npcHeaderRect.anchorMax = new Vector2(1f, 1f);
        npcHeaderRect.pivot = new Vector2(0.5f, 1f);
        npcHeaderRect.anchoredPosition = new Vector2(0, -32);
        npcHeaderRect.sizeDelta = new Vector2(-60, 30);

        // Reparent original NPC text component
        textoNPC.transform.SetParent(npcBoxObjeto.transform, false);
        textoNPC.gameObject.SetActive(true);
        npcBoxTexto = textoNPC;
        npcBoxTexto.alignment = TextAlignmentOptions.TopLeft;
        npcBoxTexto.enableWordWrapping = true;
        npcBoxTexto.enableAutoSizing = true;
        npcBoxTexto.fontSizeMin = 13f;
        npcBoxTexto.fontSizeMax = 20f;
        npcBoxTexto.margin = new Vector4(0, 0, 0, 0);
        npcBoxTexto.color = new Color(0.95f, 0.95f, 0.9f, 1f); // Weathered paper white/light bone
        ApplyFont(npcBoxTexto, true);

        RectTransform npcTxtRect = textoNPC.GetComponent<RectTransform>();
        npcTxtRect.anchorMin = Vector2.zero;
        npcTxtRect.anchorMax = Vector2.one;
        npcTxtRect.pivot = new Vector2(0.5f, 0.5f);
        npcTxtRect.offsetMin = new Vector2(30, 30); // Clear margins for the yellow border
        npcTxtRect.offsetMax = new Vector2(-30, -65); // Margins inside the box (clearing the header)

        // 2. Create Player dialogue box
        playerBoxObjeto = new GameObject("CustomPlayerBox");
        playerBoxObjeto.transform.SetParent(panelDialogo.transform, false);

        RectTransform playerRect = playerBoxObjeto.AddComponent<RectTransform>();
        playerRect.anchorMin = new Vector2(0.5f, 0.5f);
        playerRect.anchorMax = new Vector2(0.5f, 0.5f);
        playerRect.pivot = new Vector2(0f, 1f); // TOP-LEFT PIVOT!
        playerRect.anchoredPosition = originalPlayerPos + new Vector2(-30f, 35f); // Shift to align text precisely
        playerRect.sizeDelta = new Vector2(450, 330);

        Image playerImg = playerBoxObjeto.AddComponent<Image>();
        if (boxSprite != null)
        {
            playerImg.sprite = boxSprite;
            playerImg.type = Image.Type.Simple;
            playerImg.color = Color.white;
        }
        else
        {
            playerImg.color = new Color(0.09f, 0.08f, 0.08f, 0.98f);
        }

        // Create Player name header (consistent title/header area)
        GameObject playerHeaderObj = new GameObject("PlayerNameHeader");
        playerHeaderObj.transform.SetParent(playerBoxObjeto.transform, false);
        playerNameHeader = playerHeaderObj.AddComponent<TextMeshProUGUI>();
        playerNameHeader.fontStyle = FontStyles.Bold;
        playerNameHeader.fontSize = 20;
        playerNameHeader.alignment = TextAlignmentOptions.Center;
        playerNameHeader.color = new Color(1f, 0.55f, 0f, 1f); // Orange / Amber
        ApplyFont(playerNameHeader, true);

        RectTransform playerHeaderRect = playerHeaderObj.GetComponent<RectTransform>();
        playerHeaderRect.anchorMin = new Vector2(0f, 1f);
        playerHeaderRect.anchorMax = new Vector2(1f, 1f);
        playerHeaderRect.pivot = new Vector2(0.5f, 1f);
        playerHeaderRect.anchoredPosition = new Vector2(0, -32);
        playerHeaderRect.sizeDelta = new Vector2(-60, 30);

        // Reparent original Player/Result text component
        textoResultado.transform.SetParent(playerBoxObjeto.transform, false);
        textoResultado.gameObject.SetActive(true);
        playerBoxTexto = textoResultado;
        playerBoxTexto.alignment = TextAlignmentOptions.TopLeft;
        playerBoxTexto.enableWordWrapping = true;
        playerBoxTexto.enableAutoSizing = true;
        playerBoxTexto.fontSizeMin = 13f;
        playerBoxTexto.fontSizeMax = 20f;
        playerBoxTexto.margin = new Vector4(0, 0, 0, 0);
        playerBoxTexto.color = new Color(0.95f, 0.95f, 0.9f, 1f); // Weathered paper white/light bone
        ApplyFont(playerBoxTexto, true);

        RectTransform playerTxtRect = textoResultado.GetComponent<RectTransform>();
        playerTxtRect.anchorMin = Vector2.zero;
        playerTxtRect.anchorMax = Vector2.one;
        playerTxtRect.pivot = new Vector2(0.5f, 0.5f);
        playerTxtRect.offsetMin = new Vector2(30, 30); // Clear margins for the yellow border
        playerTxtRect.offsetMax = new Vector2(-30, -65); // Margins inside the box (clearing the header)

        // 3. Create Dialogue Continue Button
        btnContinuarDialogoObjeto = new GameObject("CustomBtnContinuarDialogo");
        btnContinuarDialogoObjeto.transform.SetParent(panelDialogo.transform, false);

        RectTransform btnRect = btnContinuarDialogoObjeto.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(originalPlayerPos.x + 195f, originalPlayerPos.y - 320f);
        btnRect.sizeDelta = new Vector2(180, 45);

        Image btnImg = btnContinuarDialogoObjeto.AddComponent<Image>();
        btnImg.color = new Color(0.25f, 0.32f, 0.2f, 1f); // Military Olive Green

        Outline btnOutline = btnContinuarDialogoObjeto.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.12f, 0.16f, 0.1f, 0.8f);
        btnOutline.effectDistance = new Vector2(1.5f, 1.5f);

        Button btn = btnContinuarDialogoObjeto.AddComponent<Button>();
        btn.onClick.AddListener(OnContinuarClicked);

        // Hover/press transitions (Military green transitions)
        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.25f, 0.32f, 0.2f, 1f);
        cb.highlightedColor = new Color(0.32f, 0.4f, 0.26f, 1f);
        cb.pressedColor = new Color(0.18f, 0.24f, 0.14f, 1f);
        btn.colors = cb;

        // Button Text
        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnContinuarDialogoObjeto.transform, false);
        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "CONTINUAR >>";
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.fontSize = 16;
        btnText.color = new Color(0.9f, 0.9f, 0.85f, 1f);
        ApplyFont(btnText, true);

        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;

        // Create custom buttons for trading flow
        btnAbrirInventarioCustom = CrearBotonCustom(
            "CustomBtnAbrirInventario",
            "ABRIR INVENTARIO",
            new Vector2(originalNpcPos.x + 195f, originalNpcPos.y - 365f),
            new Vector2(220, 45),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            AbrirInventarioDesdeBoton
        );
        btnAbrirInventarioCustom.SetActive(false);

        btnRechazarTratoCustom = CrearBotonCustom(
            "CustomBtnRechazarTrato",
            "RECHAZAR TRATO",
            new Vector2(0f, 50f),
            new Vector2(220, 45),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            RechazarTrato
        );
        btnRechazarTratoCustom.SetActive(false);

        btnHonestoDesafioCustom = CrearBotonCustom(
            "CustomBtnHonestoDesafio",
            "",
            new Vector2(originalNpcPos.x + 195f, originalNpcPos.y - 365f),
            new Vector2(450, 45),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            RespuestaHonesto
        );
        btnHonestoDesafioCustom.SetActive(false);

        btnChamuyeroDesafioCustom = CrearBotonCustom(
            "CustomBtnChamuyeroDesafio",
            "",
            new Vector2(originalNpcPos.x + 195f, originalNpcPos.y - 420f),
            new Vector2(450, 45),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            RespuestaChamuyero
        );
        btnChamuyeroDesafioCustom.SetActive(false);

        btnMentirosoDesafioCustom = CrearBotonCustom(
            "CustomBtnMentirosoDesafio",
            "",
            new Vector2(originalNpcPos.x + 195f, originalNpcPos.y - 475f),
            new Vector2(450, 45),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            RespuestaMentiroso
        );
        btnMentirosoDesafioCustom.SetActive(false);
    }

    private GameObject CrearBotonCustom(string name, string text, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, UnityEngine.Events.UnityAction onClickAction)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(panelDialogo.transform, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.25f, 0.32f, 0.2f, 1f); // Military olive green

        Outline outline = buttonObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.12f, 0.16f, 0.1f, 0.8f);
        outline.effectDistance = new Vector2(1.5f, 1.5f);

        Button btn = buttonObj.AddComponent<Button>();
        btn.onClick.AddListener(onClickAction);

        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.25f, 0.32f, 0.2f, 1f);
        cb.highlightedColor = new Color(0.32f, 0.4f, 0.26f, 1f);
        cb.pressedColor = new Color(0.18f, 0.24f, 0.14f, 1f);
        btn.colors = cb;

        // Button Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text.ToUpper();
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 16;
        tmp.color = new Color(0.9f, 0.9f, 0.85f, 1f);
        ApplyFont(tmp, true);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return buttonObj;
    }

    private void ApplyFont(TextMeshProUGUI tmpText, bool applyEffects = false)
    {
        if (tmpText == null) return;
        TMP_FontAsset stencilFont = Resources.Load<TMP_FontAsset>("Capture It SDF");
        if (stencilFont == null) stencilFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Capture It SDF");
        if (stencilFont == null) stencilFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");
        if (stencilFont == null) stencilFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Anton SDF");
        
        if (stencilFont != null)
        {
            tmpText.font = stencilFont;
        }

        if (applyEffects)
        {
            Material fontMat = tmpText.fontMaterial;
            if (fontMat != null)
            {
                fontMat.SetFloat("_FaceDilate", -0.06f); // Slight erosion/wear
                fontMat.SetFloat("_OutlineSoftness", 0.2f); // Faded outline edges
                fontMat.SetFloat("_OutlineWidth", 0.18f); // Outline boundary
                fontMat.SetColor("_OutlineColor", new Color(0.05f, 0.05f, 0.05f, 0.95f)); // Soot dark outline
            }
        }
    }

    void CrearPopupTrato()
    {
        // 1. Create Popup Background Panel
        popupObjeto = new GameObject("PopupTratoCentrado");
        popupObjeto.transform.SetParent(panelDialogo.transform, false);

        RectTransform rect = popupObjeto.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(500, 320);

        AplicarEstiloPostApocaliptico(popupObjeto);

        // 2. Create Title & Detail Text inside the popup
        GameObject textoObj = new GameObject("PopupTexto");
        textoObj.transform.SetParent(popupObjeto.transform, false);

        popupTexto = textoObj.AddComponent<TextMeshProUGUI>();
        popupTexto.alignment = TextAlignmentOptions.Center;
        popupTexto.fontSize = 22;
        popupTexto.color = new Color(0.9f, 0.88f, 0.85f, 1f);

        RectTransform textRect = textoObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(30, 95);  // Space for button
        textRect.offsetMax = new Vector2(-30, -35); // Space for top bar

        // 3. Create Continue Button inside the popup
        GameObject buttonObj = new GameObject("PopupBoton");
        buttonObj.transform.SetParent(popupObjeto.transform, false);

        RectTransform btnRect = buttonObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = new Vector2(0, 30);
        btnRect.sizeDelta = new Vector2(180, 45);

        Image btnImg = buttonObj.AddComponent<Image>();
        btnImg.color = new Color(0.25f, 0.32f, 0.2f, 1f); // Military green

        Outline btnOutline = buttonObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.12f, 0.16f, 0.1f, 0.8f);
        btnOutline.effectDistance = new Vector2(1.5f, 1.5f);

        Button btn = buttonObj.AddComponent<Button>();
        btn.onClick.AddListener(CerrarPopupYContinuar);

        // Button hover/press states
        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.25f, 0.32f, 0.2f, 1f);
        cb.highlightedColor = new Color(0.32f, 0.4f, 0.26f, 1f);
        cb.pressedColor = new Color(0.18f, 0.24f, 0.14f, 1f);
        btn.colors = cb;

        // Button Text
        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(buttonObj.transform, false);

        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "CONFIRMAR >>";
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.fontSize = 16;
        btnText.color = new Color(0.9f, 0.9f, 0.85f, 1f);

        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;

        popupObjeto.SetActive(false);
    }

    private void AplicarEstiloPostApocaliptico(GameObject panel)
    {
        // 1. Dark rusted steel background
        Image img = panel.GetComponent<Image>();
        if (img == null)
        {
            img = panel.AddComponent<Image>();
        }
        img.color = new Color(0.09f, 0.08f, 0.08f, 0.98f); // Dark scrap iron

        // 2. Dual layer border (Rust orange + soot shadow)
        Outline outline = panel.GetComponent<Outline>();
        if (outline == null)
        {
            outline = panel.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0.5f, 0.25f, 0.1f, 0.8f); // Rust saddle brown
        outline.effectDistance = new Vector2(2, 2);

        Shadow shadow = panel.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = panel.AddComponent<Shadow>();
        }
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f); // dark soot shadow
        shadow.effectDistance = new Vector2(4, -4);

        // 3. Add yellow/black warning hazard stripes at the top
        AgregarCintaPeligro(panel);

        // 4. Add corner rivets (remaches)
        AgregarRemaches(panel);
    }

    private void AgregarCintaPeligro(GameObject panel)
    {
        // Create the yellow background strip
        GameObject yellowStrip = new GameObject("HazardStrip");
        yellowStrip.transform.SetParent(panel.transform, false);

        RectTransform rect = yellowStrip.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -6);
        rect.sizeDelta = new Vector2(-20, 10); // Slightly smaller than container width

        Image img = yellowStrip.AddComponent<Image>();
        img.color = new Color(0.85f, 0.65f, 0.1f, 0.9f); // Caution Yellow

        // Add the black slash stripes over the yellow strip
        GameObject stripesObj = new GameObject("StripesText");
        stripesObj.transform.SetParent(yellowStrip.transform, false);

        TextMeshProUGUI stripesText = stripesObj.AddComponent<TextMeshProUGUI>();
        stripesText.text = "<b>/ / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / /</b>";
        stripesText.alignment = TextAlignmentOptions.Center;
        stripesText.fontSize = 8;
        stripesText.color = new Color(0.05f, 0.05f, 0.05f, 0.85f); // Matte black slashes

        RectTransform stripesRect = stripesObj.GetComponent<RectTransform>();
        stripesRect.anchorMin = Vector2.zero;
        stripesRect.anchorMax = Vector2.one;
        stripesRect.offsetMin = Vector2.zero;
        stripesRect.offsetMax = Vector2.zero;
    }

    private void AgregarRemaches(GameObject panel)
    {
        // Add 4 corner rivets (remaches de metal oxidado)
        Vector2[] offsets = new Vector2[] {
            new Vector2(12, -18),    // Top-Left (shifted down to clear warning tape)
            new Vector2(-12, -18),   // Top-Right
            new Vector2(12, 12),     // Bottom-Left
            new Vector2(-12, 12)     // Bottom-Right
        };

        Vector2[] anchors = new Vector2[] {
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject rivet = new GameObject("Rivet_" + i);
            rivet.transform.SetParent(panel.transform, false);

            RectTransform r = rivet.AddComponent<RectTransform>();
            r.anchorMin = anchors[i];
            r.anchorMax = anchors[i];
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = offsets[i];
            r.sizeDelta = new Vector2(8, 8); // small circular rivet

            Image img = rivet.AddComponent<Image>();
            img.color = new Color(0.35f, 0.32f, 0.3f, 1f); // Dark metal rivet color

            Outline o = rivet.AddComponent<Outline>();
            o.effectColor = new Color(0.1f, 0.08f, 0.05f, 0.8f);
            o.effectDistance = new Vector2(1, -1);
        }
    }

    private void CerrarPopupYContinuar()
    {
        // Marcar el trato como aceptado SOLO si fue exitoso.
        // Esto hace que el NPC y su objeto desaparezcan al volver al nivel.
        // Si el trato falló (o fue rechazado), el flag NO se marca y el NPC permanece.
        if (exito)
        {
            NPCInteraction.MarcarTratoAceptado(NPCInteraction.lastInteractedNPC);
        }

        string sceneToLoad = "nivel_1_ypf";
        if (!string.IsNullOrEmpty(NPCInteraction.previousScene))
        {
            sceneToLoad = NPCInteraction.previousScene;
        }
        SceneManager.LoadScene(sceneToLoad);
    }    public class OpcionDialogo
    {
        public string jugadorTexto = "";
        public string npcExito = "";
        public string npcFallo = "";
        public string detalleExito = "";
        public string detalleFallo = "";
        // Items the player must have in inventory for this text to make sense.
        // Leave null/empty for options that don't mention specific items.
        public string[] itemsNecesarios = null;
    }

    private class EscenarioDialogo
    {
        public string npcNombre = "NPC";
        public string npcPregunta = "";
        
        public List<OpcionDialogo> honestoOptions = new List<OpcionDialogo>();
        public List<OpcionDialogo> chamuyeroOptions = new List<OpcionDialogo>();
        public List<OpcionDialogo> mentirosoOptions = new List<OpcionDialogo>();
    }

    private void PopularEscenarios()
    {
        escenarios.Clear();

        // 1. NPC1: Roxana (Fuel, Desafío, Honesto correct)
        EscenarioDialogo e0 = new EscenarioDialogo();
        e0.npcNombre = "Roxana";
        e0.npcPregunta = "Para conseguir mi combustible tenés que ser digno y hablarme con el corazón, sin vueltas ni chamuyos baratos. Decime, ¿por qué andás vagando por estas rutas destruidas?";
        addOptHonesto(e0, "La verdad es que no me queda nada, Roxana. Se me apaga la camioneta y sigo mi viaje para ver si queda algo de esperanza.",
                      "Tu franqueza te honra. No abunda la gente sincera hoy en día. Tomá el bidón, pibe.", "",
                      "Conseguiste nafta\nRoxana valoró tu sinceridad", "");
        addOptChamuyero(e0, "Estoy en una misión secreta del alto mando para reconectar las comunicaciones nacionales.",
                        "", "No me vengas con cuentos gubernamentales, que a esos les perdí la fe hace años. Andate.",
                        "", "Roxana rechaza tus mentiras");
        addOptMentiroso(e0, "Te cambio la nafta por la ubicación de un camión cisterna militar lleno de combustible.",
                        "", "Ese cuento de la cisterna es más viejo que el asfalto. No me mientas en la cara.",
                        "", "Intento de engaño fallido");
        escenarios.Add(e0);

        // 2. NPC2: Beto (Offers brujula, needs termo)
        EscenarioDialogo e1 = new EscenarioDialogo();
        e1.npcNombre = "Beto";
        e1.npcPregunta = "El frío de la noche cala los huesos, amigo. Para no perder el rumbo tengo un instrumento de aguja imantada que apunta al norte. A cambio, busco un envase cilíndrico de doble pared, de esos que atrapan el calor del líquido y no lo dejan ir, para poder cebarme unos mates calientes en la intemperie. ¿Tenés uno?";
        addOptHonesto(e1, "Te ofrezco mi termo de acero inoxidable, mantiene el agua hirviendo por horas.",
                      "¡Un termo de acero! Excelente, justo lo que mis mates necesitaban. Trato hecho, acá tenés la brújula.",
                      "Si no tenés el termo a mano, no hay trato, amigo. Conseguilo y volvé.",
                      "Conseguiste 1 Brújula\nLe diste tu termo", "No tenés un termo", new[] { "termo" });
        escenarios.Add(e1);

        // 3. NPC3: Carlos (Offers caja de herramientas, needs garrafa)
        EscenarioDialogo e2 = new EscenarioDialogo();
        e2.npcNombre = "Carlos";
        e2.npcPregunta = "Tengo este pesado cofre metálico lleno de herramientas para reparar tu vehículo. Pero para mi taller necesito una buena presión de fuego, un envase metálico pesado que guarde gas licuado a alta presión para alimentar mi soplete. Si me conseguís uno de esos cilindros, el cofre es tuyo.";
        addOptHonesto(e2, "Te doy esta garrafa de gas cargada, ideal para el taller y alta presión.",
                      "¡Espectacular! Una garrafa cargada es justo lo que requiero para mis trabajos. Tomá la caja de herramientas.",
                      "Sin la garrafa no puedo trabajar. Volvé cuando la consigas.",
                      "Conseguiste 1 Caja de Herramientas\nLe diste tu garrafa", "No tenés una garrafa", new[] { "garrafa" });
        escenarios.Add(e2);

        // 4. NPC4: Sergio (Fuel, Desafío, Chamuyero correct)
        EscenarioDialogo e3 = new EscenarioDialogo();
        e3.npcNombre = "Sergio";
        e3.npcPregunta = "El combustible cotiza alto en este sector. Si querés una parte, tenés que pasar mi prueba mental. Decime: ¿qué es lo primero que se quema en un motor cuando se queda sin lubricante por completo?";
        addOptHonesto(e3, "La verdad, no tengo idea de mecánica de motores, solo sé manejar el Rastrojero.",
                      "", "Si no sabés cuidar tu máquina, el combustible es un desperdicio en tus manos. Pensá otra respuesta.",
                      "", "Sergio rechazó tu respuesta");
        addOptChamuyero(e3, "Claramente son los cojinetes de biela y bancada debido a la fricción térmica que funde el metal antifricción, seguido por los aros de pistón.",
                        "¡Mirá vos, sabés de mecánica! Me convenciste, tomá el bidón de nafta.", "",
                        "Conseguiste nafta\nSergio admira tus conocimientos", "");
        addOptMentiroso(e3, "El volante de inercia y los cables de la batería explotan en mil pedazos inmediatamente.",
                        "", "Eso no tiene ningún sentido mecánico. No me inventes cosas de ciencia ficción. Probá de nuevo.",
                        "", "Sergio rechazó tu respuesta");
        escenarios.Add(e3);

        // 5. NPC5: Beatriz (Offers bateria, needs pitusas)
        EscenarioDialogo e4 = new EscenarioDialogo();
        e4.npcNombre = "Beatriz";
        e4.npcPregunta = "Tengo este pesado acumulador de doce voltios que recuperé de un coche abandonado. Pero estoy exhausta y mi cuerpo me pide algo dulce, unas tapitas concéntricas rellenas de crema con aroma a vainilla que solían endulzar las tardes de merienda de los chicos. Si me conseguís ese paquete de delicias clásicas, te daré la batería.";
        addOptHonesto(e4, "Te ofrezco este paquete de galletitas pitusas de vainilla para recuperar energías.",
                      "¡Las pitusas! Qué recuerdo tan dulce. Hacemos el trato de inmediato, tomá la batería.",
                      "Si no tenés las pitusas dulces, no te puedo entregar este acumulador. Buscalas.",
                      "Conseguiste 1 Batería\nLe diste tus pitusas", "No tenés pitusas en el inventario", new[] { "pitusas" });
        escenarios.Add(e4);

        // 6. NPC6: Jetsar (Offers cacerola, needs fosforitos)
        EscenarioDialogo e5 = new EscenarioDialogo();
        e5.npcNombre = "Jetsar";
        e5.npcPregunta = "Tengo esta olla de aluminio profunda con mango para hervir guisos y sopas. Pero de nada me sirve si no puedo encender el fuego. Busco esas pequeñas astillas de madera con cabeza de azufre que se encienden al rasparlas. ¿Tenés una caja de esas?";
        addOptHonesto(e5, "Te doy esta caja de fósforos secos para encender cualquier fuego.",
                      "¡Fósforos! Qué alivio, por fin podré encender la leña. Tomá la cacerola, trato hecho.",
                      "Sin fuego no puedo comer. Necesito los fósforos para el trato.",
                      "Conseguiste 1 Cacerola\nLe entregaste tus fósforos", "No tenés fósforos en el inventario", new[] { "fosforitos" });
        escenarios.Add(e5);

        // 7. NPC7: Enrique (Fuel, Desafío, Mentiroso correct)
        EscenarioDialogo e6 = new EscenarioDialogo();
        e6.npcNombre = "Enrique";
        e6.npcPregunta = "Las patrullas de la autopista andan buscando fugitivos y exigen credenciales oficiales para circular por acá. Contame una buena razón para dejarte pasar con combustible.";
        addOptHonesto(e6, "No tengo ningún pase oficial, solo soy un conductor tratando de avanzar en mi viaje con lo justo.",
                      "", "La honestidad no me sirve de escudo contra las multas. No te puedo dar combustible.",
                      "", "Enrique rechazó tu respuesta");
        addOptChamuyero(e6, "Soy un viejo amigo del inspector de tránsito municipal, seguro me conocés de nombre.",
                        "", "El inspector fue despedido el año pasado, payaso. No me chamuyes.",
                        "", "Enrique rechazó tu respuesta");
        addOptMentiroso(e6, "Soy el enviado directo de la jefatura central de transporte y vengo a requisar combustible bajo la ley de emergencia.",
                        "¡Ah, disculpe Oficial! No quería causar problemas. Aquí tiene el combustible requisado, circule tranquilo.", "",
                        "Conseguiste nafta asustando a Enrique con credenciales falsas", "");
        escenarios.Add(e6);

        // 8. NPC8: Li (Offers manta solar, needs gancia)
        EscenarioDialogo e7 = new EscenarioDialogo();
        e7.npcNombre = "Li";
        e7.npcPregunta = "Las noches en esta zona costera son heladas y esta lona con celdas fotovoltaicas te puede dar abrigo térmico y electricidad. A cambio, busco calmar mi garganta con esa bebida herbal cristalina de botella verde que se toma con limón. ¿Tenés una?";
        addOptHonesto(e7, "Tengo una botella de gancia, ideal para amenizar la velada.",
                      "¡Gancia! Un elixir clásico para relajar las tensiones del fin del mundo. Trato cerrado, tomá la manta solar.",
                      "Sin mi bebida herbal de botella verde no hay trato. Conseguila.",
                      "Conseguiste 1 Manta Solar\nLe diste tu gancia", "No tenés gancia en el inventario", new[] { "gancia" });
        escenarios.Add(e7);

        // 9. NPC9: Santino (Offers mapa, needs cacerola)
        EscenarioDialogo e8 = new EscenarioDialogo();
        e8.npcNombre = "Santino";
        e8.npcPregunta = "Tengo dibujada la cartografía detallada con los atajos y zonas seguras para tu viaje, pero no tengo dónde cocinar el arroz que me queda. Necesito un recipiente metálico hondo con mango para hervir comida al fuego. ¿Hacemos trato?";
        addOptHonesto(e8, "Te doy esta cacerola de aluminio con mango, perfecta para tu guiso.",
                      "¡La cacerola! Excelente, ahora podré cocinar caliente. Acá tenés el mapa de la ruta, que te sea de ayuda.",
                      "Sin olla para cocinar, el mapa se queda conmigo. Conseguí una.",
                      "Conseguiste 1 Mapa de la ruta\nLe entregaste tu cacerola", "No tenés una cacerola en el inventario", new[] { "cacerola" });
        escenarios.Add(e8);

        // 10. NPC10: Carla (Fuel, Desafío, Honesto correct)
        EscenarioDialogo e9 = new EscenarioDialogo();
        e9.npcNombre = "Carla";
        e9.npcPregunta = "Todos los que pasan por acá me mienten y me versean para conseguir nafta. Decime la pura y cruda verdad de por qué hacés este viaje tan arriesgado.";
        addOptHonesto(e9, "La verdad es que mi familia me espera y el Rastrojero es lo único que me queda para volver a verlos.",
                      "Tus ojos no mienten. Me conmueve tu historia y valoro tu franqueza. Tomá el bidón de combustible, que llegues bien.", "",
                      "Conseguiste nafta\nCarla se conmovió con tu historia", "");
        addOptChamuyero(e9, "Soy un explorador de la ONU mapeando recursos para el regreso de la civilización.",
                        "", "La ONU no existe hace una década. Andate a mentir a otro lado.",
                        "", "Carla rechazó tu respuesta");
        addOptMentiroso(e9, "Tengo un pase VIP firmado por el intendente de la ciudad que me otorga libre combustible.",
                        "", "Esa firma es más falsa que billete de tres pesos. No te creo nada.",
                        "", "Carla rechazó tu respuesta");
        escenarios.Add(e9);

        // 11. NPC11: Martín (Offers anafe, needs brujula)
        EscenarioDialogo e10 = new EscenarioDialogo();
        e10.npcNombre = "Martín";
        e10.npcPregunta = "Tengo este anafe de camping portátil a gas para cocinar en cualquier lado, pero estoy completamente perdido en este laberinto de ruinas. Necesito un instrumento magnético con aguja que me marque el norte para no seguir dando vueltas en círculo. ¿Tenés uno?";
        addOptHonesto(e10, "Te doy esta brújula militar para que encuentres tu rumbo en el mapa.",
                      "¡La brújula! Genial, ahora sé hacia dónde caminar. Tomá el anafe a gas, me salvaste.",
                      "Sin la brújula no me muevo de acá. Conseguila si querés el anafe.",
                      "Conseguiste 1 Anafe\nLe diste tu brújula", "No tenés una brújula en el inventario", new[] { "brujula" });
        escenarios.Add(e10);

        // 12. NPC12: Tomás (Offers mate, needs manta solar)
        EscenarioDialogo e11 = new EscenarioDialogo();
        e11.npcNombre = "Tomás";
        e11.npcPregunta = "Tengo el mate de calabaza con bombilla listo, pero el frío de la intemperie me está liquidando y no tengo abrigo térmico. Busco esa manta tecnológica que absorbe y retiene el calor del sol para pasar la noche. Si me la das, te entrego mi mate.";
        addOptHonesto(e11, "Tengo esta manta solar térmica de alta tecnología para abrigarte.",
                      "¡Qué abrigada es esta manta! El calor se siente al instante. Trato hecho, llevate el mate.",
                      "El frío es insoportable. Necesito la manta solar si querés el mate.",
                      "Conseguiste 1 Mate\nLe diste tu manta solar", "No tenés la manta solar en el inventario", new[] { "manta solar" });
        escenarios.Add(e11);

        // 13. NPC13: Lucas y Gonza (Fuel, Desafío, Chamuyero correct)
        EscenarioDialogo e12 = new EscenarioDialogo();
        e12.npcNombre = "Lucas y Gonza";
        e12.npcPregunta = "¡Eh, loco! Queremos escuchar una buena historia o una propuesta de negocios copada antes de soltar este bidón de nafta súper. Contanos algo que valga la pena.";
        addOptHonesto(e12, "Solo soy un tipo común siguiendo mi viaje, no tengo historias interesantes que contar.",
                      "", "Qué aburrido... Para tipos comunes no tenemos nafta gratis. Seguí participando.",
                      "", "Lucas y Gonza rechazaron tu respuesta");
        addOptChamuyero(e12, "Si me dan la nafta, les enseño a destilar bioetanol casero usando restos de frutas fermentadas y caña de azúcar.",
                        "¡Pará, eso del bioetanol suena tremendo! Nos sirve para las motos. Tomá el bidón, trato hecho, contanos más.", "",
                        "Conseguiste nafta con tu propuesta de destilación de bioetanol", "");
        addOptMentiroso(e12, "Soy el campeón nacional de automovilismo de TC y les firmo un autógrafo si me dan el bidón.",
                        "", "¡Ja! El campeón de TC corre en autos de verdad, no en esa cafetera desvencijada. Andá.",
                        "", "Lucas y Gonza rechazaron tu respuesta");
        escenarios.Add(e12);

        // 14. NPC14: Ezequiel (Offers pinza, needs mate)
        EscenarioDialogo e13 = new EscenarioDialogo();
        e13.npcNombre = "Ezequiel";
        e13.npcPregunta = "Tengo esta pinza reforzada para cortar y apretar alambres, pero ando con ganas de tomar unos buenos amargos para pasar la tarde y me falta el recipiente tradicional de calabaza o madera. ¿Tenés un mate?";
        addOptHonesto(e13, "Te doy este mate curado listo para cebar unos amargos.",
                      "¡Qué buen mate! Bien curado y listo para cebar. Hacemos trato, llevate la pinza.",
                      "Sin mate no puedo tomar amargos. Traeme uno si querés la pinza.",
                      "Conseguiste 1 Pinza\nLe diste tu mate", "No tenés un mate en el inventario", new[] { "mate" });
        escenarios.Add(e13);

        // 15. NPC15: Antonella (Offers guantes, needs pinza)
        EscenarioDialogo e14 = new EscenarioDialogo();
        e14.npcNombre = "Antonella";
        e14.npcPregunta = "Tengo estos guantes de cuero reforzado ideales para proteger las manos en tus tareas, pero necesito urgente una herramienta de sujeción metálica con dientes para apretar unas tuercas rebeldes de mi generador. ¿Tenés una pinza?";
        addOptHonesto(e14, "Tengo una pinza de mecánico de cromo vanadio ideal para tus tuercas.",
                      "¡La pinza! Con esto podré ajustar el generador. Hacemos trueque, acá tenés los guantes.",
                      "Si no tenés la pinza a mano, no puedo soltar los guantes. Conseguila.",
                      "Conseguiste 1 par de Guantes\nLe diste tu pinza", "No tenés una pinza en el inventario", new[] { "pinza" });
        escenarios.Add(e14);

        // 16. NPC16: Fran fafi (Win condition, needs 5 items: caja de herramientas, bateria, mapa, anafe, guantes)
        EscenarioDialogo e15 = new EscenarioDialogo();
        e15.npcNombre = "Fran fafi";
        e15.npcPregunta = "¡Felicitaciones por llegar al puesto final! Para completar la reactivación del puesto de mando y asegurar la zona, necesito las 5 herramientas clave: la caja de herramientas, la batería, el mapa detallado, el anafe y los guantes de cuero. ¿Los tenés todos para completar tu aventura?";
        addOptHonesto(e15, "Sí, acá tengo los 5 elementos clave: la caja de herramientas, la batería, el mapa, el anafe y los guantes.",
                      "¡Increíble! Lograste reunir todo en este viaje tan peligroso. Reactivando sistemas... ¡Completaste el juego con éxito!",
                      "Te faltan elementos para asegurar la zona. Revisá tu inventario y volvé con los 5 objetos.",
                      "¡Juego completado con éxito!", "Te faltan algunos de los 5 objetos requeridos",
                      new[] { "caja de herramientas", "bateria", "mapa", "anafe", "guantes" });
        escenarios.Add(e15);
    }

    private void addOptHonesto(EscenarioDialogo e, string jugadorTexto, string npcExito, string npcFallo, string detalleExito, string detalleFallo, string[] itemsNecesarios = null)
    {
        e.honestoOptions.Add(new OpcionDialogo {
            jugadorTexto = jugadorTexto,
            npcExito = npcExito,
            npcFallo = npcFallo,
            detalleExito = detalleExito,
            detalleFallo = detalleFallo,
            itemsNecesarios = itemsNecesarios
        });
    }

    private void addOptChamuyero(EscenarioDialogo e, string jugadorTexto, string npcExito, string npcFallo, string detalleExito, string detalleFallo, string[] itemsNecesarios = null)
    {
        e.chamuyeroOptions.Add(new OpcionDialogo {
            jugadorTexto = jugadorTexto,
            npcExito = npcExito,
            npcFallo = npcFallo,
            detalleExito = detalleExito,
            detalleFallo = detalleFallo,
            itemsNecesarios = itemsNecesarios
        });
    }

    private void addOptMentiroso(EscenarioDialogo e, string jugadorTexto, string npcExito, string npcFallo, string detalleExito, string detalleFallo, string[] itemsNecesarios = null)
    {
        e.mentirosoOptions.Add(new OpcionDialogo {
            jugadorTexto = jugadorTexto,
            npcExito = npcExito,
            npcFallo = npcFallo,
            detalleExito = detalleExito,
            detalleFallo = detalleFallo,
            itemsNecesarios = itemsNecesarios
        });
    }

    private Sprite CargarSpriteDesdeDisco(string relativePath)
    {
#if UNITY_EDITOR
        object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/" + relativePath);
        foreach (object asset in assets)
        {
            if (asset is Sprite)
            {
                return (Sprite)asset;
            }
        }
        return null;
#else
        string fullPath = System.IO.Path.Combine(Application.dataPath, relativePath);
        if (System.IO.File.Exists(fullPath))
        {
            byte[] fileData = System.IO.File.ReadAllBytes(fullPath);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(fileData))
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
        return null;
#endif
    }

    private Sprite CargarSpriteDesdeResources(string resourcePath)
    {
        Sprite s = Resources.Load<Sprite>(resourcePath);
        if (s != null)
        {
            return s;
        }
        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
        if (sprites != null && sprites.Length > 0)
        {
            return sprites[0];
        }
        return null;
    }

    private bool TieneObjetosRequeridos(TipoRespuesta tipo)
    {
        return true;
    }

    private void ProcesarTruequeInventario()
    {
        if (InventoryManager.Instance == null || escenarioActual == null) return;

        string lastNPC = NPCInteraction.lastInteractedNPC.ToLower();

        if (exito)
        {
            if (lastNPC.Contains("npc16"))
            {
                // Remove all 5 items required by npc16
                InventoryManager.Instance.RemoveItem("caja de herramientas", 1);
                InventoryManager.Instance.RemoveItem("bateria", 1);
                InventoryManager.Instance.RemoveItem("mapa", 1);
                InventoryManager.Instance.RemoveItem("anafe", 1);
                InventoryManager.Instance.RemoveItem("guantes", 1);
            }
            else
            {
                // Remove offered item if there was one
                if (!string.IsNullOrEmpty(chosenItemOfferName))
                {
                    if (chosenItemOfferName.ToLower() == "fosforitos")
                    {
                        InventoryManager.Instance.RemoveItem("fosforitos", 10);
                    }
                    else
                    {
                        InventoryManager.Instance.RemoveItem(chosenItemOfferName, 1);
                    }
                }

                // Add reward for specific NPCs
                if (lastNPC.Contains("npc2") || lastNPC.Contains("vagabundo"))
                {
                    InventoryManager.Instance.AddItem("brujula", 1);
                }
                else if (lastNPC.Contains("npc3"))
                {
                    InventoryManager.Instance.AddItem("caja de herramientas", 1);
                }
                else if (lastNPC.Contains("npc5"))
                {
                    InventoryManager.Instance.AddItem("bateria", 1);
                }
                else if (lastNPC.Contains("npc6"))
                {
                    InventoryManager.Instance.AddItem("cacerola", 1);
                }
                else if (lastNPC.Contains("npc8"))
                {
                    InventoryManager.Instance.AddItem("manta solar", 1);
                }
                else if (lastNPC.Contains("npc9"))
                {
                    InventoryManager.Instance.AddItem("mapa", 1);
                }
                else if (lastNPC.Contains("npc11"))
                {
                    InventoryManager.Instance.AddItem("anafe", 1);
                }
                else if (lastNPC.Contains("npc12"))
                {
                    InventoryManager.Instance.AddItem("mate", 1);
                }
                else if (lastNPC.Contains("npc14"))
                {
                    InventoryManager.Instance.AddItem("pinza", 1);
                }
                else if (lastNPC.Contains("npc15"))
                {
                    InventoryManager.Instance.AddItem("guantes", 1);
                }
                else
                {
                    // Fuel NPCs (npc1, npc4, npc7, npc10, npc13)
                    int finalFuel = randomFuelAmount;
                    if (totalFallosNPC > 0)
                    {
                        finalFuel = Mathf.Max(10, randomFuelAmount - (8 * totalFallosNPC));
                    }
                    InventoryManager.Instance.AddItem("nafta", finalFuel);
                }
            }
        }
    }

    // --- New Trade Mechanic Helpers ---

    private int currentInventoryPage = 0;
    private string chosenItemOfferName = "";
    private string customDetailResult = "";

    private Button btnHonesto;
    private Button btnChamuyero;
    private Button btnMentiroso;

    private void InicializarBotones()
    {
        if (panelBotones == null) return;
        foreach (Transform t in panelBotones.transform)
        {
            if (t.name.ToLower() == "botonhonesto") btnHonesto = t.GetComponent<Button>();
            else if (t.name.ToLower() == "botonchamuyero") btnChamuyero = t.GetComponent<Button>();
            else if (t.name.ToLower() == "botonmentiroso") btnMentiroso = t.GetComponent<Button>();
        }
    }

    private void UpdateNpcPreguntaClue()
    {
        // NPC clues are defined directly in PopularEscenarios()
    }

    private void ActualizarBotonesTrueque()
    {
        if (panelBotones == null) return;
        
        // 1. Gather all tradeable items from player inventory (excluding nafta/combustible)
        List<string> tradeableItems = new List<string>();
        if (InventoryManager.Instance != null && InventoryManager.Instance.items != null)
        {
            foreach (var item in InventoryManager.Instance.items)
            {
                if (item.name.ToLower() != "nafta" && item.quantity > 0)
                {
                    if (!tradeableItems.Contains(item.name))
                    {
                        tradeableItems.Add(item.name);
                    }
                }
            }
        }

        if (btnHonesto == null || btnChamuyero == null || btnMentiroso == null) return;

        // Clear previous listeners on buttons
        btnHonesto.onClick.RemoveAllListeners();
        btnChamuyero.onClick.RemoveAllListeners();
        btnMentiroso.onClick.RemoveAllListeners();

        // Helper to set button text
        System.Action<Button, string> setBtnText = (btn, text) => {
            btn.gameObject.SetActive(true);
            btn.interactable = true;
            
            Image img = btn.GetComponent<Image>();
            if (img != null) img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);

            Transform txtTrans = btn.transform.Find("Text (TMP)");
            if (txtTrans != null)
            {
                TextMeshProUGUI tmp = txtTrans.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = text.ToUpper();
                    tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
                    tmp.color = new Color(1f, 1f, 1f, 0.75f);
                    tmp.fontSize = 18f;
                    tmp.enableAutoSizing = false;
                    tmp.alignment = TextAlignmentOptions.Center;
                }
            }
        };

        // Paging Logic
        if (tradeableItems.Count <= 3)
        {
            if (tradeableItems.Count > 0)
            {
                string item0 = tradeableItems[0];
                setBtnText(btnHonesto, "OFRECER " + item0);
                btnHonesto.onClick.AddListener(() => OfrecerObjeto(item0));
            }
            else
            {
                setBtnText(btnHonesto, "NO TENGO NADA");
                btnHonesto.interactable = false;
            }

            if (tradeableItems.Count > 1)
            {
                string item1 = tradeableItems[1];
                setBtnText(btnChamuyero, "OFRECER " + item1);
                btnChamuyero.onClick.AddListener(() => OfrecerObjeto(item1));
            }
            else
            {
                btnChamuyero.gameObject.SetActive(false);
            }

            if (tradeableItems.Count > 2)
            {
                string item2 = tradeableItems[2];
                setBtnText(btnMentiroso, "OFRECER " + item2);
                btnMentiroso.onClick.AddListener(() => OfrecerObjeto(item2));
            }
            else
            {
                btnMentiroso.gameObject.SetActive(false);
            }
        }
        else
        {
            // Paging required (more than 3 items)
            int startIndex = currentInventoryPage * 2;
            
            if (startIndex < tradeableItems.Count)
            {
                string item0 = tradeableItems[startIndex];
                setBtnText(btnHonesto, "OFRECER " + item0);
                btnHonesto.onClick.AddListener(() => OfrecerObjeto(item0));
            }
            else
            {
                btnHonesto.gameObject.SetActive(false);
            }

            if (startIndex + 1 < tradeableItems.Count)
            {
                string item1 = tradeableItems[startIndex + 1];
                setBtnText(btnChamuyero, "OFRECER " + item1);
                btnChamuyero.onClick.AddListener(() => OfrecerObjeto(item1));
            }
            else
            {
                btnChamuyero.gameObject.SetActive(false);
            }

            bool hasMore = startIndex + 2 < tradeableItems.Count;
            if (hasMore)
            {
                setBtnText(btnMentiroso, "VER MÁS OBJETOS");
                btnMentiroso.onClick.AddListener(() => {
                    currentInventoryPage++;
                    ActualizarBotonesTrueque();
                });
            }
            else
            {
                setBtnText(btnMentiroso, "VOLVER AL INICIO");
                btnMentiroso.onClick.AddListener(() => {
                    currentInventoryPage = 0;
                    ActualizarBotonesTrueque();
                });
            }
        }
    }

    private bool IsCorrectItemForNPC(string npcName, string itemName)
    {
        string lastNPC = NPCInteraction.lastInteractedNPC.ToLower();
        string item = itemName.ToLower();

        if (lastNPC.Contains("npc2") || lastNPC.Contains("vagabundo"))
        {
            return item == "termo";
        }
        else if (lastNPC.Contains("npc3"))
        {
            return item == "garrafa";
        }
        else if (lastNPC.Contains("npc5"))
        {
            return item == "pitusas";
        }
        else if (lastNPC.Contains("npc6"))
        {
            return item == "fosforitos";
        }
        else if (lastNPC.Contains("npc8"))
        {
            return item == "gancia";
        }
        else if (lastNPC.Contains("npc9"))
        {
            return item == "cacerola";
        }
        else if (lastNPC.Contains("npc11"))
        {
            return item == "brujula";
        }
        else if (lastNPC.Contains("npc12"))
        {
            return item == "manta solar";
        }
        else if (lastNPC.Contains("npc14"))
        {
            return item == "mate";
        }
        else if (lastNPC.Contains("npc15"))
        {
            return item == "pinza";
        }
        else if (lastNPC.Contains("npc16"))
        {
            bool hasAll = InventoryManager.Instance.HasItem("caja de herramientas", 1) &&
                          InventoryManager.Instance.HasItem("bateria", 1) &&
                          InventoryManager.Instance.HasItem("mapa", 1) &&
                          InventoryManager.Instance.HasItem("anafe", 1) &&
                          InventoryManager.Instance.HasItem("guantes", 1);
            return hasAll && (item == "caja de herramientas" || item == "bateria" || item == "mapa" || item == "anafe" || item == "guantes");
        }

        return false;
    }

    private void RestaurarBotonesEstrategia()
    {
        if (btnHonestoDesafioCustom == null || btnChamuyeroDesafioCustom == null || btnMentirosoDesafioCustom == null) return;
        if (escenarioActual == null) return;

        // Retrieve the player option texts
        string textHonesto = "";
        string textChamuyero = "";
        string textMentiroso = "";

        if (escenarioActual.honestoOptions.Count > 0) textHonesto = escenarioActual.honestoOptions[0].jugadorTexto;
        if (escenarioActual.chamuyeroOptions.Count > 0) textChamuyero = escenarioActual.chamuyeroOptions[0].jugadorTexto;
        if (escenarioActual.mentirosoOptions.Count > 0) textMentiroso = escenarioActual.mentirosoOptions[0].jugadorTexto;

        System.Action<GameObject, string> setBtnStyle = (btnObj, text) => {
            if (string.IsNullOrEmpty(text))
            {
                btnObj.SetActive(false);
                return;
            }
            btnObj.SetActive(true);
            
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null) btn.interactable = true;

            Transform txtTrans = btnObj.transform.Find("Text");
            if (txtTrans != null)
            {
                TextMeshProUGUI tmp = txtTrans.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = text; // Raw text (no uppercase)
                    tmp.fontStyle = FontStyles.Normal; // Normal font style
                    tmp.color = new Color(0.9f, 0.9f, 0.85f, 1f);
                    tmp.fontSize = 12f;
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = 8f;
                    tmp.fontSizeMax = 14f;
                    tmp.alignment = TextAlignmentOptions.Center;
                }
            }
        };

        setBtnStyle(btnHonestoDesafioCustom, textHonesto);
        setBtnStyle(btnChamuyeroDesafioCustom, textChamuyero);
        setBtnStyle(btnMentirosoDesafioCustom, textMentiroso);
    }

    private OpcionDialogo BuscarOpcionPorObjeto(string itemName)
    {
        if (escenarioActual == null) return null;
        string itemLower = itemName.ToLower();

        foreach (var opt in escenarioActual.honestoOptions)
        {
            if (opt.itemsNecesarios != null)
            {
                foreach (string itemReq in opt.itemsNecesarios)
                {
                    if (itemReq.ToLower() == itemLower) return opt;
                }
            }
        }

        foreach (var opt in escenarioActual.chamuyeroOptions)
        {
            if (opt.itemsNecesarios != null)
            {
                foreach (string itemReq in opt.itemsNecesarios)
                {
                    if (itemReq.ToLower() == itemLower) return opt;
                }
            }
        }

        foreach (var opt in escenarioActual.mentirosoOptions)
        {
            if (opt.itemsNecesarios != null)
            {
                foreach (string itemReq in opt.itemsNecesarios)
                {
                    if (itemReq.ToLower() == itemLower) return opt;
                }
            }
        }

        return null;
    }

    private OpcionDialogo ObtenerOpcionAlAzarParaObjeto(string itemName, out bool foundMatch)
    {
        foundMatch = false;
        if (escenarioActual == null) return null;

        List<OpcionDialogo> matchingOpts = new List<OpcionDialogo>();
        string itemLower = itemName.ToLower();

        System.Action<List<OpcionDialogo>> collectMatches = (optionsList) => {
            if (optionsList == null) return;
            foreach (var opt in optionsList)
            {
                if (opt.itemsNecesarios != null)
                {
                    foreach (string itemReq in opt.itemsNecesarios)
                    {
                        if (itemReq.ToLower() == itemLower)
                        {
                            matchingOpts.Add(opt);
                            break;
                        }
                    }
                }
            }
        };

        collectMatches(escenarioActual.honestoOptions);
        collectMatches(escenarioActual.chamuyeroOptions);
        collectMatches(escenarioActual.mentirosoOptions);

        if (matchingOpts.Count > 0)
        {
            foundMatch = true;
            return matchingOpts[Random.Range(0, matchingOpts.Count)];
        }

        return null;
    }

    private void OfrecerObjeto(string itemName)
    {
        bool isCorrect = IsCorrectItemForNPC(escenarioActual.npcNombre, itemName);
        exito = isCorrect;
        
        if (!isCorrect)
        {
            totalFallosNPC++;
        }
        
        if (btnAbrirInventarioCustom != null) btnAbrirInventarioCustom.SetActive(false);
        if (btnRechazarTratoCustom != null) btnRechazarTratoCustom.SetActive(false);
        panelBotones.SetActive(false);

        if (!isCorrect && totalFallosNPC >= 3)
        {
            // NPC is angry and cancels trade immediately (abrupt termination, no player text, no continue button)
            string angryText = "";
            string npc = escenarioActual.npcNombre.ToLower();
            if (npc == "gervasio")
                angryText = "¡Basta de pavadas! Me cansaste con tus ofertas. No hacemos ningún trato, andate.";
            else if (npc == "beto")
                angryText = "¡Me estás tomando el pelo! Me estoy congelando y me das esto. Se terminó, no te doy nada.";
            else if (npc == "roxana" || npc == "flavia")
                angryText = "¡Qué denso que sos! Te pedí algo dulce de comer y seguís dando vueltas. Olvidate del combustible.";
            else if (npc == "jacinto")
                angryText = "¡Qué pesado! Así no se puede matear ni charlar. Quedate con tus cosas, yo me quedo con la brújula.";
            else if (npc == "hector" || npc == "héctor" || npc == "npc3")
                angryText = "¡Olvidalo! Necesito el termo ya, no tengo tiempo para perder contigo. Chau.";
            else
                angryText = "¡Se terminó! No tenés nada de lo que busco. No hay trato.";

            customDetailResult = angryText;
            currentState = State.NpcReaccionoObjeto;
            CerrarDialogo();
            return;
        }
        
        npcBoxObjeto.SetActive(false);
        playerBoxObjeto.SetActive(true);
        btnContinuarDialogoObjeto.SetActive(true);
        
        RectTransform btnRect = btnContinuarDialogoObjeto.GetComponent<RectTransform>();
        if (btnRect != null)
        {
            btnRect.anchoredPosition = new Vector2(originalPlayerPos.x + 195f, originalPlayerPos.y - 320f);
        }
        
        currentState = State.JugadorOfrecioObjeto;
        
        string playerText = "";
        string npcReaction = "";

        bool foundMatch = false;
        OpcionDialogo matchedOpt = ObtenerOpcionAlAzarParaObjeto(itemName, out foundMatch);

        if (foundMatch && matchedOpt != null)
        {
            playerText = matchedOpt.jugadorTexto;
            if (isCorrect)
            {
                npcReaction = matchedOpt.npcExito;
                customDetailResult = matchedOpt.detalleExito;
            }
            else
            {
                npcReaction = matchedOpt.npcFallo;
                customDetailResult = matchedOpt.detalleFallo;
            }
        }
        else
        {
            // Generic randomized dialogue about the offered item
            string[] genericPlayerLines = new[] {
                $"Te ofrezco mi {itemName}. Es un buen trato, ¿qué decís?",
                $"Mirá, tengo un {itemName}. ¿Te interesa?",
                $"Te puedo ofrecer este {itemName}."
            };
            playerText = genericPlayerLines[Random.Range(0, genericPlayerLines.Length)];

            if (isCorrect)
            {
                string npcSuccessText = "";
                string successDetailText = "";

                if (escenarioActual.npcNombre.ToLower() == "gervasio")
                {
                    npcSuccessText = "¡Excelente! Justo lo que necesitaba para hacer andar el generador y encender la lumbre. Tomá la nafta.";
                    successDetailText = "Conseguiste nafta\nIntercambio completado";
                }
                else if (escenarioActual.npcNombre.ToLower() == "beto")
                {
                    npcSuccessText = "¡Qué frío hace! Con esto voy a poder abrigarme bien esta noche. Tomá, te ganaste este combustible.";
                    successDetailText = "Conseguiste nafta\nIntercambio completado";
                }
                else if (escenarioActual.npcNombre.ToLower() == "roxana")
                {
                    npcSuccessText = "Muchas gracias, me muero de hambre. Hace días que no como nada dulce. Aquí tienes el combustible.";
                    successDetailText = "Conseguiste nafta\nIntercambio completado";
                }
                else if (escenarioActual.npcNombre.ToLower() == "flavia")
                {
                    npcSuccessText = "¡Genial! Me encanta comer algo dulce mientras cuido la zona. Tomá el combustible.";
                    successDetailText = "Conseguiste nafta\nIntercambio completado";
                }
                else if (escenarioActual.npcNombre.ToLower() == "jacinto")
                {
                    npcSuccessText = "¡Unos buenos mates con algo dulce! Tomá la brújula, te va a guiar en tu camino.";
                    successDetailText = "Conseguiste la brújula\nIntercambio completado";
                }
                else if (escenarioActual.npcNombre.ToLower() == "hector" || escenarioActual.npcNombre.ToLower() == "héctor" || escenarioActual.npcNombre.ToLower() == "npc3")
                {
                    npcSuccessText = "¡Buenísimo! Ahora sí puedo mantener el agua bien caliente para el viaje. Tomá la caja de herramientas.";
                    successDetailText = "Conseguiste la caja de herramientas\nIntercambio completado";
                }
                else
                {
                    npcSuccessText = "Trato hecho, esto me sirve. Aquí tienes.";
                    successDetailText = "Intercambio completado con éxito";
                }

                npcReaction = npcSuccessText;
                customDetailResult = successDetailText;
            }
            else
            {
                string npcFailureText = "";
                string failureDetailText = "";

                if (escenarioActual.npcNombre.ToLower() == "gervasio")
                {
                    npcFailureText = totalFallosNPC >= 3
                        ? "¡Basta de pavadas! Me cansaste con tus ofertas. No hacemos ningún trato, andate."
                        : (totalFallosNPC > 1
                            ? "¡Dejate de joder! Te dije que necesito herramientas o fuego, no pavadas. Traeme algo útil o se termina el trato."
                            : "Eso no me sirve. Necesito herramientas para arreglar el motor o algo con qué prender fuego.");
                }
                else if (escenarioActual.npcNombre.ToLower() == "beto")
                {
                    npcFailureText = totalFallosNPC >= 3
                        ? "¡Me estás tomando el pelo! Me estoy congelando y me das esto. Se terminó, no te doy nada."
                        : (totalFallosNPC > 1
                            ? "¡Me estoy congelando acá afuera y me ofreces eso! Ponete las pilas, necesito abrigo ya."
                            : "No gracias, lo que necesito con urgencia es abrigo para pasar este frío tremendo.");
                }
                else if (escenarioActual.npcNombre.ToLower() == "roxana" || escenarioActual.npcNombre.ToLower() == "flavia")
                {
                    npcFailureText = totalFallosNPC >= 3
                        ? "¡Qué denso que sos! Te pedí algo dulce de comer y seguís dando vueltas. Olvidate del combustible."
                        : (totalFallosNPC > 1
                            ? "¡Te dije que tengo un hambre bárbara de algo dulce! Dejá de dar vueltas y mostrame algo dulce de comer."
                            : "No, ando buscando algo dulce para comer. Eso no me interesa.");
                }
                else if (escenarioActual.npcNombre.ToLower() == "jacinto")
                {
                    npcFailureText = totalFallosNPC >= 3
                        ? "¡Qué pesado! Así no se puede matear ni charlar. Quedate con tus cosas, yo me quedo con la brújula."
                        : (totalFallosNPC > 1
                            ? "¡Che! Quiero mate y algo dulce. No me vengas con otra cosa o no te doy la brújula."
                            : "No me sirve. Necesito mate y algo dulce para pasar la tarde.");
                }
                else if (escenarioActual.npcNombre.ToLower() == "hector" || escenarioActual.npcNombre.ToLower() == "héctor" || escenarioActual.npcNombre.ToLower() == "npc3")
                {
                    npcFailureText = totalFallosNPC >= 3
                        ? "¡Olvidalo! Necesito el termo ya, no tengo tiempo para perder contigo. Chau."
                        : (totalFallosNPC > 1
                            ? "¡No des más vueltas! Estoy varado sin termo. Dame un termo para calentar el agua o se pudre todo."
                            : "Lo que necesito con urgencia es un termo para mantener caliente el agua.");
                }
                else
                {
                    npcFailureText = totalFallosNPC >= 3
                        ? "¡Se terminó! No tenés nada de lo que busco. No hay trato."
                        : (totalFallosNPC > 1
                            ? "¡Te dije que eso no me sirve! No me hagas perder el tiempo. ¿Tenés lo que te pedí o no?"
                            : "No, eso no me sirve. Ofreceme otra cosa.");
                }
                failureDetailText = "El NPC rechazó tu oferta";

                npcReaction = npcFailureText;
                customDetailResult = failureDetailText;
            }
        }

        chosenItemOfferName = itemName;
        playerBoxTexto.text = "\"" + ReemplazarNombresEnTexto(playerText) + "\"";
        npcReactionText = "\"" + ReemplazarNombresEnTexto(npcReaction) + "\"";
    }

    public bool IsOfferingState()
    {
        return dialogoAbierto && currentState == State.JugadorEligeInventario;
    }

    public void SelectAndOfferItem(string itemName)
    {
        if (IsOfferingState())
        {
            // Close the inventory panel
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.CloseInventario();
            }
            // Call OfrecerObjeto to handle the transaction and continue dialogue
            OfrecerObjeto(itemName);
        }
    }

    public void RegresarAInicioDialogo()
    {
        currentState = State.Inicio;
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.CloseInventario();
        }

        npcBoxObjeto.SetActive(true);
        playerBoxObjeto.SetActive(false);
        btnContinuarDialogoObjeto.SetActive(false);
        popupObjeto.SetActive(false);

        // Show the initial question/clue in the NPC box
        npcBoxTexto.text = "\"" + ReemplazarNombresEnTexto(escenarioActual.npcPregunta) + "\"";

        string lastNPC = NPCInteraction.lastInteractedNPC.ToLower();
        bool isDesafioNPC = lastNPC == "npc1" || lastNPC.Contains("combativa_0") ||
                            lastNPC == "npc4" ||
                            lastNPC == "npc7" ||
                            lastNPC == "npc10" ||
                            lastNPC == "npc13";

        if (panelBotones != null)
        {
            panelBotones.SetActive(false);
        }

        if (isDesafioNPC)
        {
            RestaurarBotonesEstrategia();
            if (btnAbrirInventarioCustom != null)
            {
                btnAbrirInventarioCustom.SetActive(false);
            }
            if (btnRechazarTratoCustom != null)
            {
                btnRechazarTratoCustom.SetActive(true);
            }
        }
        else
        {
            if (btnHonestoDesafioCustom != null) btnHonestoDesafioCustom.SetActive(false);
            if (btnChamuyeroDesafioCustom != null) btnChamuyeroDesafioCustom.SetActive(false);
            if (btnMentirosoDesafioCustom != null) btnMentirosoDesafioCustom.SetActive(false);

            if (btnAbrirInventarioCustom != null)
            {
                btnAbrirInventarioCustom.SetActive(true);
            }
            if (btnRechazarTratoCustom != null)
            {
                btnRechazarTratoCustom.SetActive(true);
            }
        }
    }

    public void AbrirInventarioDesdeBoton()
    {
        currentState = State.JugadorEligeInventario;
        if (btnAbrirInventarioCustom != null)
        {
            btnAbrirInventarioCustom.SetActive(false);
        }
        if (btnRechazarTratoCustom != null)
        {
            btnRechazarTratoCustom.SetActive(false);
        }
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OpenInventario();
        }
    }

    public void RechazarTrato()
    {
        NPCInteraction.ResetearTratoActual();
        currentState = State.FinResultado;
        exito = false;

        // Hide dialogue boxes and custom buttons
        npcBoxObjeto.SetActive(false);
        playerBoxObjeto.SetActive(false);
        btnContinuarDialogoObjeto.SetActive(false);
        if (btnAbrirInventarioCustom != null)
        {
            btnAbrirInventarioCustom.SetActive(false);
        }
        if (btnRechazarTratoCustom != null)
        {
            btnRechazarTratoCustom.SetActive(false);
        }
        if (btnHonestoDesafioCustom != null) btnHonestoDesafioCustom.SetActive(false);
        if (btnChamuyeroDesafioCustom != null) btnChamuyeroDesafioCustom.SetActive(false);
        if (btnMentirosoDesafioCustom != null) btnMentirosoDesafioCustom.SetActive(false);
        if (panelBotones != null)
        {
            panelBotones.SetActive(false);
        }

        // Show the popup
        popupTexto.text = "<color=#F44336>¡TRATO NO HECHO!</color>\n\nIntercambio cancelado";
        popupObjeto.SetActive(true);
    }

    public string GetInteractedNPCDisplayName()
    {
        string lastNPC = NPCInteraction.lastInteractedNPC;
        if (string.IsNullOrEmpty(lastNPC))
        {
            return escenarioActual != null ? escenarioActual.npcNombre : "NPC";
        }
        string lower = lastNPC.ToLower();
        if (lower.Contains("combativa_0") || lower == "npc1") return "Roxana";
        if (lower.Contains("vagabundo") || lower == "npc2") return "Beto";
        if (lower.Contains("npc3")) return "Carlos";
        if (lower.Contains("npc4")) return "Sergio";
        if (lower.Contains("npc5")) return "Beatriz";
        if (lower.Contains("npc6")) return "Jetsar";
        if (lower.Contains("npc7")) return "Enrique";
        if (lower.Contains("npc8")) return "Li";
        if (lower.Contains("npc9")) return "Santino";
        if (lower.Contains("npc10")) return "Carla";
        if (lower.Contains("npc11")) return "Martín";
        if (lower.Contains("npc12")) return "Tomás";
        if (lower.Contains("npc13")) return "Lucas y Gonza";
        if (lower.Contains("npc14")) return "Ezequiel";
        if (lower.Contains("npc15")) return "Antonella";
        if (lower.Contains("npc16")) return "Fran fafi";

        return escenarioActual != null ? escenarioActual.npcNombre : "NPC";
    }

    public string ReemplazarNombresEnTexto(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if (escenarioActual == null) return text;

        string targetNPCName = escenarioActual.npcNombre;
        string displayNPCName = GetInteractedNPCDisplayName();

        if (string.IsNullOrEmpty(targetNPCName) || string.IsNullOrEmpty(displayNPCName)) return text;

        string result = ReplaceNameCaseInsensitive(text, targetNPCName, displayNPCName);
        
        // Also handle Héctor / Hector specifically
        if (targetNPCName.Equals("Héctor", System.StringComparison.OrdinalIgnoreCase))
        {
            result = ReplaceNameCaseInsensitive(result, "Hector", displayNPCName);
        }

        return result;
    }

    private string ReplaceNameCaseInsensitive(string input, string target, string replacement)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(target)) return input;
        return System.Text.RegularExpressions.Regex.Replace(
            input,
            System.Text.RegularExpressions.Regex.Escape(target),
            replacement,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
    }

    // -----------------------------------------------------------------------
    // Modo Minijuego de Nafta
    // Llamado por MinijuegoNafta después de que Start() inicializa la UI.
    // Reemplaza el texto y los botones para el flujo del minijuego.
    // -----------------------------------------------------------------------
    public void ActivarModoMinijuego(System.Action onContinuar, System.Action onRechazar)
    {
        // 1. Establecer texto del NPC
        if (npcBoxTexto != null)
            npcBoxTexto.text = "Tengo nafta para intercambiar.\n¿Qué me das a cambio?";

        // 2. Ocultar botones de estrategia (honesto / chamuyero / mentiroso)
        if (btnHonestoDesafioCustom   != null) btnHonestoDesafioCustom.SetActive(false);
        if (btnChamuyeroDesafioCustom != null) btnChamuyeroDesafioCustom.SetActive(false);
        if (btnMentirosoDesafioCustom != null) btnMentirosoDesafioCustom.SetActive(false);
        if (btnContinuarDialogoObjeto != null) btnContinuarDialogoObjeto.SetActive(false);
        if (btnAbrirInventarioCustom  != null) btnAbrirInventarioCustom.SetActive(false);

        // 3. Botón RECHAZAR TRATO → callback personalizado
        if (btnRechazarTratoCustom != null)
        {
            btnRechazarTratoCustom.SetActive(true);
            Button btnRech = btnRechazarTratoCustom.GetComponent<Button>();
            if (btnRech != null)
            {
                btnRech.onClick.RemoveAllListeners();
                btnRech.onClick.AddListener(() => onRechazar?.Invoke());
            }
        }

        // 4. Crear botón CONTINUAR >> con el mismo estilo que los demás botones custom
        if (btnAbrirInventarioCustom != null)
        {
            // Clonar la posición del botón ABRIR INVENTARIO pero renombrarlo CONTINUAR
            GameObject btnCont = Instantiate(btnAbrirInventarioCustom,
                                             btnAbrirInventarioCustom.transform.parent);
            btnCont.name = "CustomBtnContinuarMinijuego";
            btnCont.SetActive(true);

            // Cambiar texto
            TextMeshProUGUI[] txts = btnCont.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var t in txts) t.text = "CONTINUAR >>";

            // Limpiar listeners y agregar el propio
            Button btnComp = btnCont.GetComponent<Button>();
            if (btnComp != null)
            {
                btnComp.onClick.RemoveAllListeners();
                btnComp.onClick.AddListener(() => onContinuar?.Invoke());
            }
        }
        else
        {
            // Fallback: crear botón desde cero con el mismo estilo que CrearBotonCustom
            GameObject btnCont = CrearBotonCustom(
                "CustomBtnContinuarMinijuego",
                "CONTINUAR >>",
                new Vector2(originalNpcPos.x + 195f, originalNpcPos.y - 365f),
                new Vector2(220, 45),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                () => onContinuar?.Invoke()
            );
            btnCont.SetActive(true);
        }
    }
}
