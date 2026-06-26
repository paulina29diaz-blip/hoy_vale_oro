using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class NPCInteraction : MonoBehaviour
{
    public GameObject interactionText;
    public string sceneToLoad = "escenatrueque";

    private bool playerNear = false;
    private GameObject customPromptCanvas;
    private GameObject promptPanel;

    // Track last interacted NPC name
    public static string lastInteractedNPC = "";
    public static string previousScene = "nivel_1_ypf";

    // Track if the deal with each NPC has been completed
    public static bool tratoTerminado = false;
    public static bool tratoVagabundoTerminado = false;
    public static bool tratoNpc3Terminado = false;
    public static bool tratoNpc4Terminado = false;
    public static bool tratoNpc5Terminado = false;
    public static bool tratoNpc6Terminado = false;
    public static bool tratoNpc7Terminado = false;
    public static bool tratoNpc8Terminado = false;
    public static bool tratoNpc9Terminado = false;
    public static bool tratoNpc10Terminado = false;
    public static bool tratoNpc11Terminado = false;
    public static bool tratoNpc12Terminado = false;
    public static bool tratoNpc13Terminado = false;
    public static bool tratoNpc14Terminado = false;
    public static bool tratoNpc15Terminado = false;
    public static bool tratoNpc16Terminado = false;

    // Reset all static flags on every game start (handles editor Domain Reload disabled + builds)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        ResetearTodosLosTratos();
    }

    void Start()
    {
        // Si el trato con este NPC fue aceptado (exitosamente), ocultar NPC + objeto
        if (TratoAceptadoParaEsteNPC())
        {
            OcultarNPCYObjeto();
            return;
        }

        // Hide original canvas text if assigned
        if (interactionText != null)
            interactionText.SetActive(false);

        // Programmatically build the gorgeous post-apocalyptic interaction prompt
        BuildInteractionPrompt();
    }

    /// <summary>
    /// Devuelve true si el trato de ESTE NPC ya fue aceptado (flag en true).
    /// </summary>
    private bool TratoAceptadoParaEsteNPC()
    {
        switch (gameObject.name)
        {
            case "combativa_0":
            case "npc1":   return tratoTerminado;
            case "npc2":
            case "vagabundo": return tratoVagabundoTerminado;
            case "npc3":   return tratoNpc3Terminado;
            case "npc4":   return tratoNpc4Terminado;
            case "npc5":   return tratoNpc5Terminado;
            case "npc6":   return tratoNpc6Terminado;
            case "npc7":   return tratoNpc7Terminado;
            case "npc8":   return tratoNpc8Terminado;
            case "npc9":   return tratoNpc9Terminado;
            case "npc10":  return tratoNpc10Terminado;
            case "npc11":  return tratoNpc11Terminado;
            case "npc12":  return tratoNpc12Terminado;
            case "npc13":  return tratoNpc13Terminado;
            case "npc14":  return tratoNpc14Terminado;
            case "npc15":  return tratoNpc15Terminado;
            case "npc16":  return tratoNpc16Terminado;
            default:       return false;
        }
    }

    /// <summary>
    /// Oculta el NPC y el objeto de escena asociado a él.
    /// Grupo bidon   → npc1/combativa_0, npc4, npc7, npc10, npc13, npc16
    /// Grupo brujula → npc2/vagabundo, npc5, npc8, npc11, npc14
    /// Grupo objeto9 → npc3, npc6, npc9, npc12, npc15
    /// </summary>
    private void OcultarNPCYObjeto()
    {
        string nombreObjeto = ObtenerNombreObjeto();
        if (!string.IsNullOrEmpty(nombreObjeto))
        {
            GameObject obj = GameObject.Find(nombreObjeto);
            if (obj != null) obj.SetActive(false);
        }
        gameObject.SetActive(false);
    }

    private string ObtenerNombreObjeto()
    {
        switch (gameObject.name)
        {
            case "combativa_0":
            case "npc1":
            case "npc4":
            case "npc7":
            case "npc10":
            case "npc13":
            case "npc16":
                return "bidon";

            case "vagabundo":
            case "npc2":
            case "npc5":
            case "npc8":
            case "npc11":
            case "npc14":
                return "brujula";

            case "npc3":
            case "npc6":
            case "npc9":
            case "npc12":
            case "npc15":
                return "objeto9";

            default:
                return "";
        }
    }

    void Update()
    {
        if (playerNear && Input.GetKeyDown(KeyCode.F))
        {
            lastInteractedNPC = gameObject.name;
            previousScene = SceneManager.GetActiveScene().name;
            // NOTA: el flag tratoXTerminado se marca solo cuando el trato
            // es ACEPTADO (exitoso), no al iniciar la interacción.
            // Ver: MarcarTratoAceptado() que llama DialogoManager al cerrar con éxito.

            // Guardar posición del auto antes de cambiar de escena
            Movimiento car = FindAnyObjectByType<Movimiento>();
            if (car != null)
            {
                Movimiento.GuardarPosicion(car.transform.position);
            }

            // npc1, npc4, npc7, npc10, npc13 → minijuego de nafta
            bool esNpcNafta = gameObject.name == "npc1"  ||
                              gameObject.name == "npc4"  ||
                              gameObject.name == "npc7"  ||
                              gameObject.name == "npc10" ||
                              gameObject.name == "npc13";

            if (gameObject.name == "npc16")
            {
                // NPC16 (final boss): selector de 5 objetos en pantalla, sin cambio de escena
                GameObject finalGO = new GameObject("FinalNPC16Controller");
                DontDestroyOnLoad(finalGO);
                finalGO.AddComponent<FinalNPC16>();
            }
            else if (esNpcNafta)
            {
                // Crear el controlador del minijuego (sobrevive al cambio de escena)
                GameObject mgGO = new GameObject("MinijuegoNafta");
                MinijuegoNafta mg = mgGO.AddComponent<MinijuegoNafta>();
                mg.escenaRetorno = SceneManager.GetActiveScene().name;
                SceneManager.LoadScene("escenatrueque");
            }
            else
            {
                SceneManager.LoadScene(sceneToLoad);
            }
        }
    }

    /// <summary>
    /// Marca el trato del NPC como terminado (aceptado exitosamente).
    /// Solo llamar cuando el jugador ACEPTA el trato — NO al rechazarlo.
    /// </summary>
    public static void MarcarTratoAceptado(string npcName)
    {
        switch (npcName)
        {
            case "combativa_0": case "npc1":  tratoTerminado         = true; break;
            case "npc3":                       tratoNpc3Terminado      = true; break;
            case "npc4":                       tratoNpc4Terminado      = true; break;
            case "npc5":                       tratoNpc5Terminado      = true; break;
            case "npc6":                       tratoNpc6Terminado      = true; break;
            case "npc7":                       tratoNpc7Terminado      = true; break;
            case "npc8":                       tratoNpc8Terminado      = true; break;
            case "npc9":                       tratoNpc9Terminado      = true; break;
            case "npc10":                      tratoNpc10Terminado     = true; break;
            case "npc11":                      tratoNpc11Terminado     = true; break;
            case "npc12":                      tratoNpc12Terminado     = true; break;
            case "npc13":                      tratoNpc13Terminado     = true; break;
            case "npc14":                      tratoNpc14Terminado     = true; break;
            case "npc15":                      tratoNpc15Terminado     = true; break;
            case "npc16":                      tratoNpc16Terminado     = true; break;
            case "vagabundo":                  tratoVagabundoTerminado = true; break;
        }

        // Ocultar el objeto de escena asociado INMEDIATAMENTE (sin esperar al reload)
        string objNombre = ObtenerNombreObjetoStatic(npcName);
        if (!string.IsNullOrEmpty(objNombre))
        {
            GameObject sceneObj = GameObject.Find(objNombre);
            if (sceneObj != null) sceneObj.SetActive(false);
        }
    }

    /// <summary>
    /// Mapeo estático NPC → nombre del objeto de escena asociado.
    /// (Versión estática del método de instancia ObtenerNombreObjeto())
    /// </summary>
    private static string ObtenerNombreObjetoStatic(string npcName)
    {
        switch (npcName)
        {
            case "combativa_0":
            case "npc1":
            case "npc4":
            case "npc7":
            case "npc10":
            case "npc13":
            case "npc16":
                return "bidon";

            case "vagabundo":
            case "npc2":
            case "npc5":
            case "npc8":
            case "npc11":
            case "npc14":
                return "brujula";

            case "npc3":
            case "npc6":
            case "npc9":
            case "npc12":
            case "npc15":
                return "objeto9";

            default:
                return "";
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = true;

            if (promptPanel != null)
            {
                promptPanel.SetActive(true);
            }

            if (interactionText != null)
            {
                interactionText.SetActive(false);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = false;

            if (promptPanel != null)
            {
                promptPanel.SetActive(false);
            }

            if (interactionText != null)
            {
                interactionText.SetActive(false);
            }
        }
    }

    private void BuildInteractionPrompt()
    {
        Vector3 parentScale = transform.lossyScale;
        float parentScaleX = parentScale.x != 0 ? parentScale.x : 1f;
        float parentScaleY = parentScale.y != 0 ? parentScale.y : 1f;

        // The baseline scale factor is based on combativa_0's scale (approx 0.25f)
        float baselineScale = 0.25098053f;

        // Calculate Y position above sprite bounds
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float localY = 3.3f;
        if (sr != null && sr.sprite != null)
        {
            float halfSpriteHeight = sr.sprite.bounds.size.y * 0.5f;
            float worldOffset = 2.3f * baselineScale;
            localY = halfSpriteHeight + (worldOffset / Mathf.Abs(parentScaleY));
        }

        // Create Canvas in World Space
        customPromptCanvas = new GameObject("CustomInteractionPromptCanvas");
        customPromptCanvas.transform.SetParent(this.transform, false);
        customPromptCanvas.transform.localPosition = new Vector3(0, localY, 0);

        // Scale it so that the prompt has the exact same world scale as it does on combativa_0.
        // We divide by parentScale (including sign) so the world scale is always positive,
        // which prevents the text from being mirrored if the NPC is flipped.
        float targetLocalScaleX = (0.04f * baselineScale) / parentScaleX;
        float targetLocalScaleY = (0.04f * baselineScale) / parentScaleY;
        customPromptCanvas.transform.localScale = new Vector3(targetLocalScaleX, targetLocalScaleY, 1f);

        Canvas canvas = customPromptCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50; // Render above other sprites

        // 1. Create Balloon Container (holds circle + triangle + !)
        GameObject balloonObj = new GameObject("Balloon");
        balloonObj.transform.SetParent(customPromptCanvas.transform, false);
        
        RectTransform balloonRect = balloonObj.AddComponent<RectTransform>();
        balloonRect.sizeDelta = new Vector2(60, 60);
        balloonRect.anchoredPosition = Vector2.zero;

        // Draw filled yellow circle with black outline
        Texture2D circleTex = new Texture2D(128, 128);
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float dx = x - 64f;
                float dy = y - 64f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= 62f && dist > 54f)
                {
                    circleTex.SetPixel(x, y, new Color(0.06f, 0.05f, 0.05f, 1f)); // Dark outline
                }
                else if (dist <= 54f)
                {
                    circleTex.SetPixel(x, y, new Color(0.92f, 0.78f, 0.18f, 1f)); // Golden warning yellow
                }
                else
                {
                    circleTex.SetPixel(x, y, Color.clear);
                }
            }
        }
        circleTex.Apply();
        Sprite circleSprite = Sprite.Create(circleTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));

        Image circleImg = balloonObj.AddComponent<Image>();
        circleImg.sprite = circleSprite;

        // Add downward triangle (stem)
        GameObject triObj = new GameObject("Stem");
        triObj.transform.SetParent(balloonObj.transform, false);
        RectTransform triRect = triObj.AddComponent<RectTransform>();
        triRect.sizeDelta = new Vector2(24, 24);
        triRect.anchoredPosition = new Vector2(0, -38);

        Texture2D triTex = new Texture2D(64, 64);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float limit = (64 - y) * 0.5f;
                if (x >= 32 - limit && x <= 32 + limit)
                {
                    if (x < 32 - limit + 4 || x > 32 + limit - 4 || y < 4)
                    {
                        triTex.SetPixel(x, y, new Color(0.06f, 0.05f, 0.05f, 1f)); // Dark outline
                    }
                    else
                    {
                        triTex.SetPixel(x, y, new Color(0.92f, 0.78f, 0.18f, 1f)); // Yellow fill
                    }
                }
                else
                {
                    triTex.SetPixel(x, y, Color.clear);
                }
            }
        }
        triTex.Apply();
        Sprite triSprite = Sprite.Create(triTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));

        Image triImg = triObj.AddComponent<Image>();
        triImg.sprite = triSprite;

        // Add Exclamation Mark Text inside circle
        GameObject exclamationObj = new GameObject("ExclamationText");
        exclamationObj.transform.SetParent(balloonObj.transform, false);
        TextMeshProUGUI exclText = exclamationObj.AddComponent<TextMeshProUGUI>();
        exclText.text = "!";
        exclText.fontStyle = FontStyles.Bold;
        exclText.fontSize = 42;
        exclText.alignment = TextAlignmentOptions.Center;
        exclText.color = new Color(0.08f, 0.08f, 0.08f, 1f); // Dark fill for !

        RectTransform exclRect = exclamationObj.GetComponent<RectTransform>();
        exclRect.anchorMin = Vector2.zero;
        exclRect.anchorMax = Vector2.one;
        exclRect.offsetMin = Vector2.zero;
        exclRect.offsetMax = Vector2.zero;

        // 2. Create Options Panel (floats to the right when player is near, except for npc3 which floats to the left)
        promptPanel = new GameObject("PromptPanel");
        promptPanel.transform.SetParent(customPromptCanvas.transform, false);
        
        RectTransform panelRect = promptPanel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(180, 70); // Increased height to fit NPC name header
        if (gameObject.name == "npc3")
        {
            panelRect.pivot = new Vector2(1f, 0.5f);
            panelRect.anchoredPosition = new Vector2(-45, 0); // Offset to the left to avoid screen cut-off
        }
        else
        {
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(45, 0); // Offset to the right
        }

        Image panelImg = promptPanel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.08f, 0.9f); // Dark translucent background

        Outline panelOutline = promptPanel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.5f, 0.25f, 0.1f, 0.8f); // Saddle brown/rust border
        panelOutline.effectDistance = new Vector2(1.5f, 1.5f);

        // Add NPC Name Header Text at the top of the panel
        GameObject nameObj = new GameObject("NPCNameHeader");
        nameObj.transform.SetParent(promptPanel.transform, false);
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        
        // Helper inline map for NPC names
        string npcDisplayName = GetNPCDisplayName(gameObject.name);

        nameText.text = npcDisplayName.ToUpper();
        nameText.fontStyle = FontStyles.Bold;
        nameText.fontSize = 15;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = new Color(1f, 0.55f, 0f, 1f); // Orange / Amber color for name header

        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.5f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 0.5f);
        nameRect.offsetMin = new Vector2(5, 0);
        nameRect.offsetMax = new Vector2(-5, -5);

        // Add text "INTERACTUAR [F]" at the bottom
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(promptPanel.transform, false);
        TextMeshProUGUI promptText = textObj.AddComponent<TextMeshProUGUI>();
        promptText.text = "INTERACTUAR <color=#FFD700>[F]</color>";
        promptText.fontStyle = FontStyles.Bold;
        promptText.fontSize = 13; // Slightly smaller to ensure fit
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = new Color(0.9f, 0.9f, 0.85f, 1f);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = new Vector2(5, 5);
        textRect.offsetMax = new Vector2(-5, 0);

        // Apply custom post-apocalyptic fonts
        ApplyFont(nameText, true);
        ApplyFont(exclText, false);
        ApplyFont(promptText, true);

        // Start prompt panel hidden until player is near
        promptPanel.SetActive(false);
    }

    private void ApplyFont(TextMeshProUGUI tmpText, bool applyEffects)
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
                fontMat.SetFloat("_FaceDilate", -0.06f);
                fontMat.SetFloat("_OutlineSoftness", 0.2f);
                fontMat.SetFloat("_OutlineWidth", 0.18f);
                fontMat.SetColor("_OutlineColor", new Color(0.05f, 0.05f, 0.05f, 0.95f));
            }
        }
    }

    public static void ResetearTodosLosTratos()
    {
        lastInteractedNPC = "";
        previousScene = "nivel_1_ypf";
        tratoTerminado = false;
        tratoVagabundoTerminado = false;
        tratoNpc3Terminado = false;
        tratoNpc4Terminado = false;
        tratoNpc5Terminado = false;
        tratoNpc6Terminado = false;
        tratoNpc7Terminado = false;
        tratoNpc8Terminado = false;
        tratoNpc9Terminado = false;
        tratoNpc10Terminado = false;
        tratoNpc11Terminado = false;
        tratoNpc12Terminado = false;
        tratoNpc13Terminado = false;
        tratoNpc14Terminado = false;
        tratoNpc15Terminado = false;
        tratoNpc16Terminado = false;
    }

    public static void ResetearTratosDeNivel(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        string nameLower = sceneName.ToLower();
        if (nameLower.Contains("nivel_1"))
        {
            tratoTerminado = false;
            tratoVagabundoTerminado = false;
            tratoNpc3Terminado = false;
        }
        else if (nameLower.Contains("nivel_2"))
        {
            tratoNpc4Terminado = false;
            tratoNpc5Terminado = false;
            tratoNpc6Terminado = false;
        }
        else if (nameLower.Contains("nivel_3"))
        {
            tratoNpc7Terminado = false;
            tratoNpc8Terminado = false;
            tratoNpc9Terminado = false;
        }
        else if (nameLower.Contains("nivel_4"))
        {
            tratoNpc10Terminado = false;
            tratoNpc11Terminado = false;
            tratoNpc12Terminado = false;
        }
        else if (nameLower.Contains("nivel_5"))
        {
            tratoNpc13Terminado = false;
            tratoNpc14Terminado = false;
            tratoNpc15Terminado = false;
        }
        else if (nameLower.Contains("nivel_6"))
        {
            tratoNpc16Terminado = false;
        }
    }

    public static string GetNPCDisplayName(string gameObjectName)
    {
        if (string.IsNullOrEmpty(gameObjectName)) return "NPC";
        string lower = gameObjectName.ToLower();
        if (lower == "combativa_0" || lower == "npc1") return "Roxana";
        if (lower == "vagabundo" || lower == "npc2") return "Beto";
        if (lower == "npc3") return "Carlos";
        if (lower == "npc4") return "Sergio";
        if (lower == "npc5") return "Beatriz";
        if (lower == "npc6") return "Jetsar";
        if (lower == "npc7") return "Enrique";
        if (lower == "npc8") return "Li";
        if (lower == "npc9") return "Santino";
        if (lower == "npc10") return "Carla";
        if (lower == "npc11") return "Martín";
        if (lower == "npc12") return "Tomás";
        if (lower == "npc13") return "Lucas y Gonza";
        if (lower == "npc14") return "Ezequiel";
        if (lower == "npc15") return "Antonella";
        if (lower == "npc16") return "Fran fafi";
        return gameObjectName;
    }

    public static void ResetearTratoActual()
    {
        if (string.IsNullOrEmpty(lastInteractedNPC)) return;
        string lower = lastInteractedNPC.ToLower();
        if (lower.Contains("combativa_0") || lower == "npc1") tratoTerminado = false;
        else if (lower.Contains("vagabundo") || lower == "npc2") tratoVagabundoTerminado = false;
        else if (lower.Contains("npc3")) tratoNpc3Terminado = false;
        else if (lower.Contains("npc4")) tratoNpc4Terminado = false;
        else if (lower.Contains("npc5")) tratoNpc5Terminado = false;
        else if (lower.Contains("npc6")) tratoNpc6Terminado = false;
        else if (lower.Contains("npc7")) tratoNpc7Terminado = false;
        else if (lower.Contains("npc8")) tratoNpc8Terminado = false;
        else if (lower.Contains("npc9")) tratoNpc9Terminado = false;
        else if (lower.Contains("npc10")) tratoNpc10Terminado = false;
        else if (lower.Contains("npc11")) tratoNpc11Terminado = false;
        else if (lower.Contains("npc12")) tratoNpc12Terminado = false;
        else if (lower.Contains("npc13")) tratoNpc13Terminado = false;
        else if (lower.Contains("npc14")) tratoNpc14Terminado = false;
        else if (lower.Contains("npc15")) tratoNpc15Terminado = false;
        else if (lower.Contains("npc16")) tratoNpc16Terminado = false;
    }
}