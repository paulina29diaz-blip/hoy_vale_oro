using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using TMPro;

public class MedicineMinigame : MonoBehaviour
{
    private DialogoManager dialogoManager;

    private GameObject puzzlePanel;
    private GameObject metalCasing;
    private GameObject board;
    private GameObject backgroundContainer;
    private GameObject trayPanel;
    
    private Texture2D bgTexture;
    private Texture2D[] medTextures = new Texture2D[8];
    private Sprite bgSprite;
    private Sprite[] medSprites = new Sprite[8];

    // Correct Compartment Coordinates (Centers of the 8 slots in the kit box, aligned with background image)
    private Vector2[] compartmentPositions = new Vector2[]
    {
        new Vector2(-201.6f, 96f),  // Compartment 0: ANALGÉSICOS
        new Vector2(-52.8f, 96f),   // Compartment 1: ANTIBIÓTICOS
        new Vector2(102.4f, 96f),    // Compartment 2: ANTISÉPTICOS
        new Vector2(272f, 96f),   // Compartment 3: VENDAS
        new Vector2(-201.6f, -57.6f), // Compartment 4: JERINGAS
        new Vector2(-52.8f, -57.6f),  // Compartment 5: VITAMINAS
        new Vector2(102.4f, -57.6f),   // Compartment 6: SUERO
        new Vector2(272f, -57.6f)   // Compartment 7: PASTILLAS
    };

    private GameObject[] medObjects = new GameObject[8];
    private DraggableMedicine[] medDraggables = new DraggableMedicine[8];
    private bool[] correctPlacement = new bool[8];

    private bool isSolved = false;

    public void Iniciar(DialogoManager manager)
    {
        dialogoManager = manager;
        isSolved = false;
        correctPlacement = new bool[8];

        // Hide DialogoManager panel to focus on the minigame
        if (dialogoManager.panelDialogo != null)
        {
            dialogoManager.panelDialogo.SetActive(false);
        }

        // Get the active canvas
        Canvas canvas = FindAnyObjectByType<Canvas>();
        Transform parentTransform = canvas != null ? canvas.transform : transform;

        // 1. Create main full-screen dark overlay panel
        puzzlePanel = new GameObject("MedicineMinigamePanel");
        puzzlePanel.transform.SetParent(parentTransform, false);
        
        RectTransform panelRect = puzzlePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImg = puzzlePanel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.14f, 0.96f); // Dark industrial overlay

        // 2. Create the central metal container box
        metalCasing = new GameObject("MetalCasing");
        metalCasing.transform.SetParent(puzzlePanel.transform, false);

        RectTransform casingRect = metalCasing.AddComponent<RectTransform>();
        casingRect.anchorMin = new Vector2(0.5f, 0.5f);
        casingRect.anchorMax = new Vector2(0.5f, 0.5f);
        casingRect.pivot = new Vector2(0.5f, 0.5f);
        casingRect.sizeDelta = new Vector2(992f, 992f); // Taller window to avoid overlaps at the bottom
        casingRect.anchoredPosition = Vector2.zero;

        Image casingImg = metalCasing.AddComponent<Image>();
        casingImg.color = new Color(0.22f, 0.22f, 0.24f, 1f); // Steel/iron first aid kit casing

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
            rImg.color = new Color(0.35f, 0.35f, 0.38f, 1f);
            rivet.AddComponent<Outline>().effectColor = new Color(0.1f, 0.1f, 0.12f, 0.8f);
        }

        // 3. Load textures
        CargarTexturas();

        // 4. Create Title Text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(metalCasing.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 40f);
        titleRect.anchoredPosition = new Vector2(0f, -15f);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "ORDENAR EL BOTIQUÍN";
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
        instrRect.anchoredPosition = new Vector2(0f, -40f);

        TextMeshProUGUI instrText = instrObj.AddComponent<TextMeshProUGUI>();
        instrText.text = "Arrastrá cada medicamento desde la bandeja inferior hasta su compartimento correspondiente.";
        instrText.alignment = TextAlignmentOptions.Center;
        instrText.fontStyle = FontStyles.Italic;
        instrText.fontSize = 20f;
        instrText.color = new Color(0.8f, 0.75f, 0.7f, 1.0f);
        CopiarFuenteTemplate(instrText);

        // 5. Create board container (fits the background image exactly in 1.4 aspect ratio)
        board = new GameObject("MedicineBoard");
        board.transform.SetParent(metalCasing.transform, false);

        RectTransform boardRect = board.AddComponent<RectTransform>();
        boardRect.anchorMin = new Vector2(0.5f, 0.5f);
        boardRect.anchorMax = new Vector2(0.5f, 0.5f);
        boardRect.pivot = new Vector2(0.5f, 0.5f);
        boardRect.sizeDelta = new Vector2(896f, 640f); // 1.4 ratio scaled
        boardRect.anchoredPosition = new Vector2(0f, 90f); // Shifted higher inside the 992px casing

        Image boardImg = board.AddComponent<Image>();
        boardImg.color = new Color(0.08f, 0.08f, 0.08f, 1f);

        // 6. Background Image (cajamedicamento background)
        backgroundContainer = new GameObject("BackgroundImage");
        backgroundContainer.transform.SetParent(board.transform, false);
        RectTransform bgRect = backgroundContainer.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bgImg = backgroundContainer.AddComponent<Image>();
        bgImg.sprite = bgSprite;

        // 7. Bottom Tray Panel (holds unplaced medicine items, taller to fit larger items)
        trayPanel = new GameObject("TrayPanel");
        trayPanel.transform.SetParent(board.transform, false);
        
        RectTransform trayRect = trayPanel.AddComponent<RectTransform>();
        trayRect.anchorMin = new Vector2(0.5f, 0.5f);
        trayRect.anchorMax = new Vector2(0.5f, 0.5f);
        trayRect.pivot = new Vector2(0.5f, 0.5f);
        trayRect.sizeDelta = new Vector2(848f, 168f); // Taller tray box
        trayRect.anchoredPosition = new Vector2(0f, -440f); // Shifted lower to avoid overlapping compartments

        Image trayImg = trayPanel.AddComponent<Image>();
        trayImg.color = new Color(0.15f, 0.15f, 0.18f, 0.85f); // metal tray dark grey overlay

        Outline trayOutline = trayPanel.AddComponent<Outline>();
        trayOutline.effectColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        trayOutline.effectDistance = new Vector2(1f, -1f);

        // 8. Spawn medicine items, shuffled on starting tray positions
        List<int> shuffleList = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
        for (int i = 0; i < shuffleList.Count; i++)
        {
            int temp = shuffleList[i];
            int randIdx = Random.Range(i, shuffleList.Count);
            shuffleList[i] = shuffleList[randIdx];
            shuffleList[randIdx] = temp;
        }

        // Spawn medicine items
        float trayStartY = -440f;
        for (int k = 0; k < 8; k++)
        {
            int medIdx = shuffleList[k]; // correct index of the medicine (0..7)
            float trayX = -358.4f + k * 102.4f;
            Vector2 trayPos = new Vector2(trayX, trayStartY);

            GameObject medObj = new GameObject("MedicineItem_" + medIdx);
            medObj.transform.SetParent(board.transform, false);
            medObjects[medIdx] = medObj;

            RectTransform wr = medObj.AddComponent<RectTransform>();
            wr.anchorMin = new Vector2(0.5f, 0.5f);
            wr.anchorMax = new Vector2(0.5f, 0.5f);
            wr.pivot = new Vector2(0.5f, 0.5f);
            
            // Native aspect ratio is 0.7589. Height in tray is 100px (compact on tray)
            wr.sizeDelta = new Vector2(100f * 0.7589f, 100f);
            wr.anchoredPosition = trayPos;

            Image img = medObj.AddComponent<Image>();
            img.sprite = medSprites[medIdx];
            img.preserveAspect = true;

            // Add drag handler
            DraggableMedicine dragHelper = medObj.AddComponent<DraggableMedicine>();
            dragHelper.Init(canvas, trayPos, medIdx, ValidarDropMedicamento);
            medDraggables[medIdx] = dragHelper;

            // Small 3D drop shadow
            Outline o = medObj.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.5f);
            o.effectDistance = new Vector2(1f, -1f);
        }

        // 9. Create bottom control buttons (Cancel/Close)
        CrearBotonesControl();
    }

    private void CargarTexturas()
    {
        // 1. Load Background Box Texture from Resources/Sprites/puzzles/cajamedicamento
        bgTexture = Resources.Load<Texture2D>("Sprites/puzzles/cajamedicamento");
        if (bgTexture != null)
        {
            bgSprite = Sprite.Create(bgTexture, new Rect(0, 0, bgTexture.width, bgTexture.height), new Vector2(0.5f, 0.5f));
        }

        if (bgSprite == null)
        {
            Debug.LogError("[MedicineMinigame] No se pudo cargar cajamedicamento desde Resources. Usando fallback.");
            Texture2D fallbackBg = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            for (int p = 0; p < pixels.Length; p++) pixels[p] = new Color(0.15f, 0.15f, 0.18f); // dark slate
            fallbackBg.SetPixels(pixels);
            fallbackBg.Apply();
            bgSprite = Sprite.Create(fallbackBg, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        }

        // 2. Load 8 Medicine Sprites from Resources/Sprites/puzzles/medicamentoX
        for (int i = 0; i < 8; i++)
        {
            string resourcePath = $"Sprites/puzzles/medicamento{i + 1}";
            medTextures[i] = Resources.Load<Texture2D>(resourcePath);
            if (medTextures[i] != null)
            {
                medSprites[i] = Sprite.Create(medTextures[i], new Rect(0, 0, medTextures[i].width, medTextures[i].height), new Vector2(0.5f, 0.5f));
            }

            if (medSprites[i] == null)
            {
                Debug.LogError($"[MedicineMinigame] No se pudo cargar {resourcePath} desde Resources. Usando fallback.");
                Texture2D fallbackMed = new Texture2D(64, 64);
                Color[] pixels = new Color[64 * 64];
                for (int p = 0; p < pixels.Length; p++) pixels[p] = Color.red;
                fallbackMed.SetPixels(pixels);
                fallbackMed.Apply();
                medSprites[i] = Sprite.Create(fallbackMed, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
            }
        }
    }

    private void ValidarDropMedicamento(DraggableMedicine draggedItem, UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (isSolved) return;

        int medIdx = draggedItem.GetIndex();
        RectTransform rt = draggedItem.GetComponent<RectTransform>();
        Vector2 droppedPos = rt.anchoredPosition;

        // Check distance to its CORRECT target compartment
        Vector2 correctPos = compartmentPositions[medIdx];
        float dist = Vector2.Distance(droppedPos, correctPos);

        // Increased tolerance radius to 120f to cover the entire compartment area
        if (dist < 120f)
        {
            // Correct drop! Snap exactly to compartment center and lock
            draggedItem.SnapTo(correctPos);
            draggedItem.SetLocked(true);
            correctPlacement[medIdx] = true;

            // Highlight border green briefly
            StartCoroutine(FlashBorderFeedback(draggedItem.GetComponent<Outline>(), Color.green));

            CheckWinCondition();
        }
        else
        {
            // Incorrect drop! Snap back to starting tray slot
            draggedItem.ReturnToOriginalPosition();

            // Highlight border red briefly to denote error
            StartCoroutine(FlashBorderFeedback(draggedItem.GetComponent<Outline>(), Color.red));
        }
    }

    private System.Collections.IEnumerator FlashBorderFeedback(Outline o, Color c)
    {
        if (o != null)
        {
            o.effectColor = c;
            o.effectDistance = new Vector2(3f, 3f);
            yield return new WaitForSeconds(0.4f);
            o.effectColor = new Color(0f, 0f, 0f, 0.5f);
            o.effectDistance = new Vector2(1f, -1f);
        }
    }

    private void CheckWinCondition()
    {
        bool win = true;
        for (int i = 0; i < 8; i++)
        {
            if (!correctPlacement[i])
            {
                win = false;
                break;
            }
        }

        if (win)
        {
            isSolved = true;
            Debug.Log("[MedicineMinigame] Botiquín ordenado correctamente!");

            // Show success label overlay
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(metalCasing.transform, false);
            RectTransform sr = statusObj.AddComponent<RectTransform>();
            sr.anchorMin = new Vector2(0.5f, 0.5f);
            sr.anchorMax = new Vector2(0.5f, 0.5f);
            sr.sizeDelta = new Vector2(450f, 60f);
            sr.anchoredPosition = new Vector2(0f, -10f);

            TextMeshProUGUI sTxt = statusObj.AddComponent<TextMeshProUGUI>();
            sTxt.text = "¡BOTIQUÍN ORDENADO!";
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
            dialogoManager.FinalizarPuzzleExitoAntonella();
        }
        Destroy(puzzlePanel);
        Destroy(gameObject);
    }

    private void CancelarYSalir()
    {
        if (dialogoManager != null)
        {
            dialogoManager.panelDialogo.SetActive(true);
        }
        Destroy(puzzlePanel);
        Destroy(gameObject);
    }

    private void CrearBotonesControl()
    {
        // Cancel/Close Button - Positioned at the bottom center of the taller casing frame
        GameObject btnCancel = new GameObject("BtnCancel");
        btnCancel.transform.SetParent(metalCasing.transform, false);
        RectTransform cr = btnCancel.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0.5f, 0f);
        cr.anchorMax = new Vector2(0.5f, 0f);
        cr.pivot = new Vector2(0.5f, 0.5f);
        cr.sizeDelta = new Vector2(260f, 44f); // Taller and wider button
        cr.anchoredPosition = new Vector2(0f, 25f); // Perfect placement inside the bottom clearance gap

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
