using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using TMPro;

public class CablesMinigame : MonoBehaviour
{
    private DialogoManager dialogoManager;

    private GameObject puzzlePanel;
    private GameObject metalCasing;
    private GameObject board;
    private GameObject backgroundContainer;
    
    private Texture2D bgTexture;
    private Sprite bgSprite;

    // Color definitions matching the left side:
    // Left 0: Yellow (O)
    // Left 1: Red (Δ)
    // Left 2: Magenta (☆)
    // Left 3: Blue (X)
    private Color[] colors = new Color[]
    {
        new Color(0.95f, 0.85f, 0.1f, 0.7f),  // 0: Yellow (O)
        new Color(0.85f, 0.2f, 0.15f, 0.7f),  // 1: Red (Δ)
        new Color(0.85f, 0.15f, 0.85f, 0.7f), // 2: Magenta (☆)
        new Color(0.15f, 0.35f, 0.85f, 0.7f)  // 3: Blue (X)
    };

    // Right side order from top to bottom matches:
    // Right 0: Red (Δ)      -> matches Left 1 (Red)
    // Right 1: Blue (X)      -> matches Left 3 (Blue)
    // Right 2: Yellow (O)    -> matches Left 0 (Yellow)
    // Right 3: Magenta (☆)   -> matches Left 2 (Magenta)
    private int[] rightColorIndices = new int[] { 1, 3, 0, 2 };

    // Sockets data
    private RectTransform[] leftSocketRects = new RectTransform[4];
    private RectTransform[] rightSocketRects = new RectTransform[4];
    private Outline[] leftSocketOutlines = new Outline[4];
    private Outline[] rightSocketOutlines = new Outline[4];

    private bool[] connected = new bool[4];
    private List<GameObject> activeCables = new List<GameObject>();

    private int selectedLeftIndex = -1; // -1 means none
    private bool isSolved = false;

    public void Iniciar(DialogoManager manager)
    {
        dialogoManager = manager;
        isSolved = false;
        selectedLeftIndex = -1;
        connected = new bool[4];

        // Hide DialogoManager panel to focus on the minigame
        if (dialogoManager.panelDialogo != null)
        {
            dialogoManager.panelDialogo.SetActive(false);
        }

        // Get the active canvas
        Canvas canvas = FindAnyObjectByType<Canvas>();
        Transform parentTransform = canvas != null ? canvas.transform : transform;

        // 1. Create main full-screen dark overlay panel
        puzzlePanel = new GameObject("CablesMinigamePanel");
        puzzlePanel.transform.SetParent(parentTransform, false);
        
        RectTransform panelRect = puzzlePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImg = puzzlePanel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.12f, 0.96f); // Dark industrial overlay

        // 2. Create the central metal container box
        metalCasing = new GameObject("MetalCasing");
        metalCasing.transform.SetParent(puzzlePanel.transform, false);

        RectTransform casingRect = metalCasing.AddComponent<RectTransform>();
        casingRect.anchorMin = new Vector2(0.5f, 0.5f);
        casingRect.anchorMax = new Vector2(0.5f, 0.5f);
        casingRect.pivot = new Vector2(0.5f, 0.5f);
        casingRect.sizeDelta = new Vector2(1000f, 960f);
        casingRect.anchoredPosition = Vector2.zero;

        Image casingImg = metalCasing.AddComponent<Image>();
        casingImg.color = new Color(0.24f, 0.22f, 0.2f, 1f); // Steel bronze casing

        Outline casingOutline = metalCasing.AddComponent<Outline>();
        casingOutline.effectColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        casingOutline.effectDistance = new Vector2(2f, -2f);

        // Add warning stripe top border
        GameObject hazardBar = new GameObject("HazardBar");
        hazardBar.transform.SetParent(metalCasing.transform, false);
        RectTransform hazardRect = hazardBar.AddComponent<RectTransform>();
        hazardRect.anchorMin = new Vector2(0f, 1f);
        hazardRect.anchorMax = new Vector2(1f, 1f);
        hazardRect.pivot = new Vector2(0.5f, 1f);
        hazardRect.sizeDelta = new Vector2(0f, 20f);
        hazardRect.anchoredPosition = Vector2.zero;

        Image hazardImg = hazardBar.AddComponent<Image>();
        hazardImg.color = new Color(0.85f, 0.55f, 0.1f, 1f); // Warning stripe color

        // Add 4 rivets in casing corners
        Vector2[] rivetOffsets = new Vector2[] {
            new Vector2(15f, -35f), new Vector2(-15f, -35f),
            new Vector2(15f, 55f), new Vector2(-15f, 55f)
        };
        Vector2[] rivetAnchors = new Vector2[] {
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, 0f), new Vector2(1f, 0f)
        };
        for (int i = 0; i < 4; i++)
        {
            GameObject rivet = new GameObject("Rivet_" + i);
            rivet.transform.SetParent(metalCasing.transform, false);
            RectTransform rr = rivet.AddComponent<RectTransform>();
            rr.anchorMin = rivetAnchors[i];
            rr.anchorMax = rivetAnchors[i];
            rr.pivot = new Vector2(0.5f, 0.5f);
            rr.anchoredPosition = rivetOffsets[i];
            rr.sizeDelta = new Vector2(10f, 10f);

            Image rImg = rivet.AddComponent<Image>();
            rImg.color = new Color(0.38f, 0.35f, 0.32f, 1f);
            rivet.AddComponent<Outline>().effectColor = new Color(0.1f, 0.08f, 0.06f, 0.8f);
        }

        // 3. Load background texture
        CargarTextura();

        // 4. Create Title Text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(metalCasing.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 40f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "CONECTAR EL GENERADOR";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        titleText.fontSize = 24f;
        titleText.color = new Color(0.95f, 0.92f, 0.88f, 1f);
        CopiarFuenteTemplate(titleText);

        // 4b. Create Instruction Text
        GameObject instrObj = new GameObject("InstructionText");
        instrObj.transform.SetParent(metalCasing.transform, false);
        RectTransform instrRect = instrObj.AddComponent<RectTransform>();
        instrRect.anchorMin = new Vector2(0f, 1f);
        instrRect.anchorMax = new Vector2(1f, 1f);
        instrRect.pivot = new Vector2(0.5f, 1f);
        instrRect.sizeDelta = new Vector2(0f, 55f);
        instrRect.anchoredPosition = new Vector2(0f, -50f);

        TextMeshProUGUI instrText = instrObj.AddComponent<TextMeshProUGUI>();
        instrText.text = "Hacé click en un conector izquierdo y luego en su correspondiente por color a la derecha.";
        instrText.alignment = TextAlignmentOptions.Center;
        instrText.fontStyle = FontStyles.Italic;
        instrText.fontSize = 20f;
        instrText.color = new Color(0.8f, 0.75f, 0.7f, 1.0f);
        CopiarFuenteTemplate(instrText);

        // 5. Create board slot container
        board = new GameObject("CablesBoard");
        board.transform.SetParent(metalCasing.transform, false);

        RectTransform boardRect = board.AddComponent<RectTransform>();
        boardRect.anchorMin = new Vector2(0.5f, 0.5f);
        boardRect.anchorMax = new Vector2(0.5f, 0.5f);
        boardRect.pivot = new Vector2(0.5f, 0.5f);
        boardRect.sizeDelta = new Vector2(800f, 800f);
        boardRect.anchoredPosition = new Vector2(0f, -20f);

        Image boardImg = board.AddComponent<Image>();
        boardImg.color = new Color(0.08f, 0.08f, 0.08f, 1f);

        // 6. Background Image (cablespanel background)
        backgroundContainer = new GameObject("BackgroundImage");
        backgroundContainer.transform.SetParent(board.transform, false);
        RectTransform bgRect = backgroundContainer.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bgImg = backgroundContainer.AddComponent<Image>();
        bgImg.sprite = bgSprite;

        // 7. Create sockets (starting left terminals, ending right terminals)
        // Positioned vertically exactly over the printed sockets on the cablespuzzle.jpg image
        float[] yPositions = new float[] { 230f, 76f, -76f, -230f };
        
        for (int i = 0; i < 4; i++)
        {
            // --- LEFT TERMINAL ---
            GameObject leftObj = new GameObject("SocketLeft_" + i);
            leftObj.transform.SetParent(board.transform, false);

            RectTransform lRect = leftObj.AddComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0.5f, 0.5f);
            lRect.anchorMax = new Vector2(0.5f, 0.5f);
            lRect.pivot = new Vector2(0.5f, 0.5f);
            lRect.sizeDelta = new Vector2(70f, 70f);
            lRect.anchoredPosition = new Vector2(-310f, yPositions[i]);
            leftSocketRects[i] = lRect;

            // Make the socket hitbox image fully transparent so it doesn't cover the background graphics
            Image lImg = leftObj.AddComponent<Image>();
            lImg.color = new Color(1f, 1f, 1f, 0f);

            Outline lOutline = leftObj.AddComponent<Outline>();
            lOutline.effectColor = new Color(1f, 0.9f, 0f, 0.8f); // glowing yellow highlight on click
            lOutline.effectDistance = new Vector2(2f, 2f);
            lOutline.enabled = false; // invisible by default
            leftSocketOutlines[i] = lOutline;

            Button lBtn = leftObj.AddComponent<Button>();
            int leftIdx = i;
            lBtn.onClick.AddListener(() => OnLeftSocketClicked(leftIdx));

            // --- RIGHT TERMINAL ---
            GameObject rightObj = new GameObject("SocketRight_" + i);
            rightObj.transform.SetParent(board.transform, false);

            RectTransform rRect = rightObj.AddComponent<RectTransform>();
            rRect.anchorMin = new Vector2(0.5f, 0.5f);
            rRect.anchorMax = new Vector2(0.5f, 0.5f);
            rRect.pivot = new Vector2(0.5f, 0.5f);
            rRect.sizeDelta = new Vector2(70f, 70f);
            rRect.anchoredPosition = new Vector2(310f, yPositions[i]);
            rightSocketRects[i] = rRect;

            // Make the socket hitbox image fully transparent
            Image rImg = rightObj.AddComponent<Image>();
            rImg.color = new Color(1f, 1f, 1f, 0f);

            Outline rOutline = rightObj.AddComponent<Outline>();
            rOutline.effectColor = Color.red;
            rOutline.effectDistance = new Vector2(2f, 2f);
            rOutline.enabled = false; // invisible by default
            rightSocketOutlines[i] = rOutline;

            Button rBtn = rightObj.AddComponent<Button>();
            int rightIdx = i;
            rBtn.onClick.AddListener(() => OnRightSocketClicked(rightIdx));
        }

        // 8. Create bottom control buttons (Cancel/Close)
        CrearBotonesControl();
    }

    private void CargarTextura()
    {
        bgTexture = Resources.Load<Texture2D>("Sprites/puzzles/cablespuzzle");
        if (bgTexture != null)
        {
            bgSprite = Sprite.Create(bgTexture, new Rect(0, 0, bgTexture.width, bgTexture.height), new Vector2(0.5f, 0.5f));
        }

        if (bgSprite == null)
        {
            Debug.LogError("[CablesMinigame] No se pudo cargar cablespuzzle desde Resources. Usando fallback.");
            Texture2D fallbackBg = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            for (int p = 0; p < pixels.Length; p++) pixels[p] = new Color(0.15f, 0.15f, 0.18f); // dark slate
            fallbackBg.SetPixels(pixels);
            fallbackBg.Apply();
            bgSprite = Sprite.Create(fallbackBg, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        }
    }

    private void OnLeftSocketClicked(int leftIndex)
    {
        if (isSolved || connected[leftIndex]) return;

        // Clear previous selection highlight if any
        if (selectedLeftIndex != -1)
        {
            leftSocketOutlines[selectedLeftIndex].enabled = false;
        }

        selectedLeftIndex = leftIndex;

        // Highlight selected left socket with yellow glowing border
        leftSocketOutlines[leftIndex].enabled = true;
    }

    private void OnRightSocketClicked(int rightIndex)
    {
        if (isSolved || selectedLeftIndex == -1) return;

        int rightColorIdx = rightColorIndices[rightIndex];

        // Check if colors match
        if (selectedLeftIndex == rightColorIdx)
        {
            // Success! Create cable line connection
            DrawCableLine(selectedLeftIndex, rightIndex);
            connected[selectedLeftIndex] = true;

            // Disable buttons for these connected sockets
            leftSocketOutlines[selectedLeftIndex].gameObject.GetComponent<Button>().interactable = false;
            rightSocketOutlines[rightIndex].gameObject.GetComponent<Button>().interactable = false;

            // Turn off outline highlight
            leftSocketOutlines[selectedLeftIndex].enabled = false;

            selectedLeftIndex = -1;

            CheckWinCondition();
        }
        else
        {
            // Visual error feedback on mismatch (flashes outline red)
            StartCoroutine(FlashRedOutline(rightSocketOutlines[rightIndex]));
        }
    }

    private System.Collections.IEnumerator FlashRedOutline(Outline o)
    {
        o.enabled = true;
        yield return new WaitForSeconds(0.4f);
        o.enabled = false;
    }

    private void DrawCableLine(int leftIndex, int rightIndex)
    {
        Vector2 start = leftSocketRects[leftIndex].anchoredPosition;
        Vector2 end = rightSocketRects[rightIndex].anchoredPosition;

        Vector2 dir = end - start;
        float dist = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 1. Draw the thin, semi-transparent cable wire connection
        GameObject lineObj = new GameObject("CableConnection_" + leftIndex);
        lineObj.transform.SetParent(board.transform, false);
        lineObj.transform.SetSiblingIndex(1); // just in front of background
        activeCables.Add(lineObj);

        RectTransform lr = lineObj.AddComponent<RectTransform>();
        lr.anchorMin = new Vector2(0.5f, 0.5f);
        lr.anchorMax = new Vector2(0.5f, 0.5f);
        lr.pivot = new Vector2(0.5f, 0.5f);
        lr.anchoredPosition = start + dir * 0.5f;
        lr.sizeDelta = new Vector2(dist, 10f); // 10px thickness
        lr.localRotation = Quaternion.Euler(0f, 0f, angle);

        Image img = lineObj.AddComponent<Image>();
        img.color = colors[leftIndex]; // uses semi-transparent color matching the wires

        Outline lOutline = lineObj.AddComponent<Outline>();
        lOutline.effectColor = new Color(0f, 0f, 0f, 0.4f);
        lOutline.effectDistance = new Vector2(1f, -1f);

        // 2. Create small glowing circular joint plug dots at the ends
        CrearJointGlow(start, colors[leftIndex]);
        CrearJointGlow(end, colors[leftIndex]);
    }

    private void CrearJointGlow(Vector2 pos, Color c)
    {
        GameObject glowObj = new GameObject("JointGlow");
        glowObj.transform.SetParent(board.transform, false);
        glowObj.transform.SetSiblingIndex(2); // in front of the lines
        activeCables.Add(glowObj);

        RectTransform gr = glowObj.AddComponent<RectTransform>();
        gr.anchorMin = new Vector2(0.5f, 0.5f);
        gr.anchorMax = new Vector2(0.5f, 0.5f);
        gr.pivot = new Vector2(0.5f, 0.5f);
        gr.anchoredPosition = pos;
        gr.sizeDelta = new Vector2(28f, 28f); // joint dot size

        Image img = glowObj.AddComponent<Image>();
        Color jointColor = c;
        jointColor.a = 0.8f;
        img.color = jointColor;

        Outline o = glowObj.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.4f);
        o.effectDistance = new Vector2(1f, -1f);
    }

    private void CheckWinCondition()
    {
        bool win = true;
        for (int i = 0; i < 4; i++)
        {
            if (!connected[i])
            {
                win = false;
                break;
            }
        }

        if (win)
        {
            isSolved = true;
            Debug.Log("[CablesMinigame] Generador conectado correctamente!");

            // Show success label overlay
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(metalCasing.transform, false);
            RectTransform sr = statusObj.AddComponent<RectTransform>();
            sr.anchorMin = new Vector2(0.5f, 0.5f);
            sr.anchorMax = new Vector2(0.5f, 0.5f);
            sr.sizeDelta = new Vector2(400f, 60f);
            sr.anchoredPosition = new Vector2(0f, -10f);

            TextMeshProUGUI sTxt = statusObj.AddComponent<TextMeshProUGUI>();
            sTxt.text = "¡ENERGÍA RESTABLECIDA!";
            sTxt.color = Color.green;
            sTxt.fontStyle = FontStyles.Bold;
            sTxt.fontSize = 28f;
            sTxt.alignment = TextAlignmentOptions.Center;
            CopiarFuenteTemplate(sTxt);

            Outline sShadow = statusObj.AddComponent<Outline>();
            sShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            sShadow.effectDistance = new Vector2(2f, -2f);

            // Complete minigame and return after 1.2s delay
            Invoke("FinalizarConExito", 1.2f);
        }
    }

    private void FinalizarConExito()
    {
        if (dialogoManager != null)
        {
            dialogoManager.panelDialogo.SetActive(true);
            dialogoManager.FinalizarPuzzleExitoSantino();
        }
        LimpiarCables();
        Destroy(puzzlePanel);
        Destroy(gameObject);
    }

    private void CancelarYSalir()
    {
        if (dialogoManager != null)
        {
            dialogoManager.panelDialogo.SetActive(true);
        }
        LimpiarCables();
        Destroy(puzzlePanel);
        Destroy(gameObject);
    }

    private void LimpiarCables()
    {
        foreach (var cab in activeCables)
        {
            if (cab != null) Destroy(cab);
        }
        activeCables.Clear();
    }

    private void CrearBotonesControl()
    {
        // Cancel/Close Button
        GameObject btnCancel = new GameObject("BtnCancel");
        btnCancel.transform.SetParent(metalCasing.transform, false);
        RectTransform cr = btnCancel.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0.5f, 0f);
        cr.anchorMax = new Vector2(0.5f, 0f);
        cr.pivot = new Vector2(0.5f, 0f);
        cr.sizeDelta = new Vector2(240f, 40f);
        cr.anchoredPosition = new Vector2(0f, 20f);

        Image cImg = btnCancel.AddComponent<Image>();
        cImg.color = new Color(0.4f, 0.15f, 0.15f, 1f); // Muted red

        Button cBtn = btnCancel.AddComponent<Button>();
        cBtn.onClick.AddListener(CancelarYSalir);
        ConfigurarBotonHoverColors(cBtn, cImg.color);

        GameObject cTextObj = new GameObject("Text");
        cTextObj.transform.SetParent(btnCancel.transform, false);
        TextMeshProUGUI cTxt = cTextObj.AddComponent<TextMeshProUGUI>();
        cTxt.text = "VOLVER AL DIÁLOGO";
        cTxt.alignment = TextAlignmentOptions.Center;
        cTxt.fontSize = 12f;
        cTxt.fontStyle = FontStyles.Bold;
        cTxt.color = new Color(0.9f, 0.9f, 0.85f, 1f);
        CopiarFuenteTemplate(cTxt);
        RectTransformCentrar(cTextObj.GetComponent<RectTransform>());
    }

    private void ConfigurarBotonHoverColors(Button btn, Color normal)
    {
        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        ColorBlock cb = btn.colors;
        cb.normalColor = normal;
        cb.highlightedColor = normal * 1.25f;
        cb.pressedColor = normal * 0.75f;
        btn.colors = cb;
    }

    private void RectTransformCentrar(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    private void CopiarFuenteTemplate(TextMeshProUGUI target)
    {
        if (dialogoManager != null)
        {
            TextMeshProUGUI template = dialogoManager.GetComponentInChildren<TextMeshProUGUI>();
            if (template != null)
            {
                target.font = template.font;
            }
        }
    }
}
