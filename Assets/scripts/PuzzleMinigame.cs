using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using TMPro;

public class PuzzleMinigame : MonoBehaviour
{
    private DialogoManager dialogoManager;
    private string targetItemName;

    private GameObject puzzlePanel;
    private GameObject metalCasing;
    private GameObject board;
    
    private Texture2D puzzleTexture;
    private Sprite[] pieceSprites = new Sprite[4];
    private Image[] pieceImages = new Image[4];
    private Outline[] pieceOutlines = new Outline[4];
    private Button[] pieceButtons = new Button[4];
    
    // Tracks which piece ID is currently placed in which grid slot (0 to 3)
    // Slot 0: Top-Left, Slot 1: Top-Right, Slot 2: Bottom-Left, Slot 3: Bottom-Right
    private int[] slotToPiece = new int[4];
    
    // Positions of the slots relative to the board center
    private Vector2[] slotPositions = new Vector2[]
    {
        new Vector2(-200f, 200f),  // Slot 0 (TL)
        new Vector2(200f, 200f),   // Slot 1 (TR)
        new Vector2(-200f, -200f), // Slot 2 (BL)
        new Vector2(200f, -200f)   // Slot 3 (BR)
    };

    private int selectedSlot = -1; // -1 means no piece selected
    private bool isSolved = false;

    public void Iniciar(DialogoManager manager, string itemName)
    {
        dialogoManager = manager;
        targetItemName = itemName;
        isSolved = false;
        selectedSlot = -1;

        // Hide DialogoManager panel to focus on the minigame
        if (dialogoManager.panelDialogo != null)
        {
            dialogoManager.panelDialogo.SetActive(false);
        }

        // Parent to the same Canvas
        Canvas canvas = FindAnyObjectByType<Canvas>();
        Transform parentTransform = canvas != null ? canvas.transform : transform;

        // 1. Create main full-screen dark overlay panel
        puzzlePanel = new GameObject("PuzzleMinigamePanel");
        puzzlePanel.transform.SetParent(parentTransform, false);
        
        RectTransform panelRect = puzzlePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImg = puzzlePanel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.11f, 0.1f, 0.96f); // Dark post-apocalyptic grey overlay

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
        casingImg.color = new Color(0.26f, 0.22f, 0.18f, 1f); // Rusty industrial metal brown

        Outline casingOutline = metalCasing.AddComponent<Outline>();
        casingOutline.effectColor = new Color(0.1f, 0.08f, 0.06f, 1f);
        casingOutline.effectDistance = new Vector2(2f, -2f);

        // Add yellow/black hazard stripe top border
        GameObject hazardBar = new GameObject("HazardBar");
        hazardBar.transform.SetParent(metalCasing.transform, false);
        RectTransform hazardRect = hazardBar.AddComponent<RectTransform>();
        hazardRect.anchorMin = new Vector2(0f, 1f);
        hazardRect.anchorMax = new Vector2(1f, 1f);
        hazardRect.pivot = new Vector2(0.5f, 1f);
        hazardRect.sizeDelta = new Vector2(0f, 20f);
        hazardRect.anchoredPosition = Vector2.zero;

        Image hazardImg = hazardBar.AddComponent<Image>();
        hazardImg.color = new Color(0.85f, 0.55f, 0.1f, 1f); // Dirty Warning Orange/Yellow

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
            rivet.AddComponent<Outline>().effectColor = new Color(0.12f, 0.1f, 0.08f, 0.8f);
        }

        // 3. Load and slice puzzle texture
        CargarYFraccionarTextura();

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
        titleText.text = "REPARAR DIBUJO DE CARLOS";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        titleText.fontSize = 24f;
        titleText.color = new Color(0.95f, 0.92f, 0.88f, 1f);
        CopiarFuenteTemplate(titleText);

        // 4b. Create Instruction/Explanation Text (super simple explanation on how to swap pieces)
        GameObject instrObj = new GameObject("InstructionText");
        instrObj.transform.SetParent(metalCasing.transform, false);
        RectTransform instrRect = instrObj.AddComponent<RectTransform>();
        instrRect.anchorMin = new Vector2(0f, 1f);
        instrRect.anchorMax = new Vector2(1f, 1f);
        instrRect.pivot = new Vector2(0.5f, 1f);
        instrRect.sizeDelta = new Vector2(0f, 55f);
        instrRect.anchoredPosition = new Vector2(0f, -50f);

        TextMeshProUGUI instrText = instrObj.AddComponent<TextMeshProUGUI>();
        instrText.text = "Hacé click en una pieza y luego en otra para intercambiarlas de lugar.";
        instrText.alignment = TextAlignmentOptions.Center;
        instrText.fontStyle = FontStyles.Italic;
        instrText.fontSize = 20f;
        instrText.color = new Color(0.8f, 0.75f, 0.7f, 1.0f);
        CopiarFuenteTemplate(instrText);

        // 5. Create board slot container
        board = new GameObject("PuzzleBoard");
        board.transform.SetParent(metalCasing.transform, false);

        RectTransform boardRect = board.AddComponent<RectTransform>();
        boardRect.anchorMin = new Vector2(0.5f, 0.5f);
        boardRect.anchorMax = new Vector2(0.5f, 0.5f);
        boardRect.pivot = new Vector2(0.5f, 0.5f);
        boardRect.sizeDelta = new Vector2(800f, 800f);
        boardRect.anchoredPosition = new Vector2(0f, -20f);

        Image boardImg = board.AddComponent<Image>();
        boardImg.color = new Color(0.14f, 0.12f, 0.1f, 1f); // Dark inner slot color

        // 6. Create piece GameObjects
        for (int i = 0; i < 4; i++)
        {
            GameObject pieceObj = new GameObject("Piece_" + i);
            pieceObj.transform.SetParent(board.transform, false);

            RectTransform pr = pieceObj.AddComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.5f, 0.5f);
            pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.pivot = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(390f, 390f); // 10px margin in between slots

            Image img = pieceObj.AddComponent<Image>();
            img.sprite = pieceSprites[i];
            pieceImages[i] = img;

            Outline outline = pieceObj.AddComponent<Outline>();
            outline.effectColor = Color.yellow;
            outline.effectDistance = new Vector2(3f, 3f);
            outline.enabled = false;
            pieceOutlines[i] = outline;

            Button btn = pieceObj.AddComponent<Button>();
            int index = i;
            btn.onClick.AddListener(() => OnPieceClicked(index));
            pieceButtons[i] = btn;
        }

        // 7. Scramble the puzzle slots
        MezclarPuzzle();

        // 8. Create bottom control buttons (Reset & Cancel)
        CrearBotonesControl();
    }

    private void CargarYFraccionarTextura()
    {
        puzzleTexture = Resources.Load<Texture2D>("Sprites/puzzles/fotonpc3puzzle");
        if (puzzleTexture != null)
        {
            int w = puzzleTexture.width;
            int h = puzzleTexture.height;
            int hw = w / 2;
            int hh = h / 2;

            // Slice into 4 pieces
            // Sprite.Create uses world space coordinate rectangles
            pieceSprites[0] = Sprite.Create(puzzleTexture, new Rect(0, hh, hw, hh), new Vector2(0.5f, 0.5f));   // TL
            pieceSprites[1] = Sprite.Create(puzzleTexture, new Rect(hw, hh, hw, hh), new Vector2(0.5f, 0.5f));  // TR
            pieceSprites[2] = Sprite.Create(puzzleTexture, new Rect(0, 0, hw, hh), new Vector2(0.5f, 0.5f));     // BL
            pieceSprites[3] = Sprite.Create(puzzleTexture, new Rect(hw, 0, hw, hh), new Vector2(0.5f, 0.5f));    // BR
            return;
        }

        // Fallback: Create solid color textures if file loading fails
        Debug.LogError("[PuzzleMinigame] No se pudo cargar el recurso fotonpc3puzzle desde Resources. Usando colores fallback.");
        Color[] colors = new Color[] { Color.red, Color.green, Color.blue, Color.magenta };
        for (int i = 0; i < 4; i++)
        {
            Texture2D tex = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            for (int p = 0; p < pixels.Length; p++) pixels[p] = colors[i];
            tex.SetPixels(pixels);
            tex.Apply();
            pieceSprites[i] = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        }
    }

    private void MezclarPuzzle()
    {
        selectedSlot = -1;
        for (int i = 0; i < 4; i++)
        {
            pieceOutlines[i].enabled = false;
        }

        // Keep shuffling until it's not solved
        bool solvedState = true;
        List<int> pieces = new List<int> { 0, 1, 2, 3 };

        while (solvedState)
        {
            // Simple Fisher-Yates shuffle
            for (int i = pieces.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);
                int temp = pieces[i];
                pieces[i] = pieces[r];
                pieces[r] = temp;
            }

            // Check if not solved
            solvedState = true;
            for (int i = 0; i < 4; i++)
            {
                if (pieces[i] != i)
                {
                    solvedState = false;
                    break;
                }
            }
        }

        // Set layout
        for (int slot = 0; slot < 4; slot++)
        {
            int pieceId = pieces[slot];
            slotToPiece[slot] = pieceId;
            
            // Move piece GameObject to slot position
            RectTransform pr = pieceImages[pieceId].GetComponent<RectTransform>();
            pr.anchoredPosition = slotPositions[slot];
        }
    }

    private void OnPieceClicked(int pieceId)
    {
        if (isSolved) return;

        // Find which slot this piece currently occupies
        int clickedSlot = -1;
        for (int i = 0; i < 4; i++)
        {
            if (slotToPiece[i] == pieceId)
            {
                clickedSlot = i;
                break;
            }
        }

        if (clickedSlot == -1) return;

        if (selectedSlot == -1)
        {
            // Select first piece
            selectedSlot = clickedSlot;
            pieceOutlines[pieceId].enabled = true;
        }
        else
        {
            if (selectedSlot == clickedSlot)
            {
                // Clicked same piece: deselect
                pieceOutlines[pieceId].enabled = false;
                selectedSlot = -1;
            }
            else
            {
                // Swap pieces in slot configuration
                int selectedPieceId = slotToPiece[selectedSlot];
                
                slotToPiece[selectedSlot] = pieceId;
                slotToPiece[clickedSlot] = selectedPieceId;

                // Animate/move UI pieces to new slot positions
                pieceImages[pieceId].GetComponent<RectTransform>().anchoredPosition = slotPositions[selectedSlot];
                pieceImages[selectedPieceId].GetComponent<RectTransform>().anchoredPosition = slotPositions[clickedSlot];

                // Clear highlights
                pieceOutlines[pieceId].enabled = false;
                pieceOutlines[selectedPieceId].enabled = false;
                selectedSlot = -1;

                // Check win condition
                CheckWinCondition();
            }
        }
    }

    private void CheckWinCondition()
    {
        bool win = true;
        for (int slot = 0; slot < 4; slot++)
        {
            if (slotToPiece[slot] != slot)
            {
                win = false;
                break;
            }
        }

        if (win)
        {
            isSolved = true;
            Debug.Log("[PuzzleMinigame] Puzzle resuelto con éxito!");
            
            // Highlight all borders green as success feedback
            for (int i = 0; i < 4; i++)
            {
                pieceOutlines[i].effectColor = Color.green;
                pieceOutlines[i].enabled = true;
                pieceButtons[i].interactable = false;
            }

            // Show success label
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(metalCasing.transform, false);
            RectTransform sr = statusObj.AddComponent<RectTransform>();
            sr.anchorMin = new Vector2(0.5f, 0.5f);
            sr.anchorMax = new Vector2(0.5f, 0.5f);
            sr.sizeDelta = new Vector2(400f, 60f);
            sr.anchoredPosition = new Vector2(0f, -10f); // Centers overlay

            TextMeshProUGUI sTxt = statusObj.AddComponent<TextMeshProUGUI>();
            sTxt.text = "¡MECANISMO CONECTADO!";
            sTxt.color = Color.green;
            sTxt.fontStyle = FontStyles.Bold;
            sTxt.fontSize = 28f;
            sTxt.alignment = TextAlignmentOptions.Center;
            CopiarFuenteTemplate(sTxt);

            // Add simple drop shadow
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
            dialogoManager.FinalizarPuzzleExitoCarlos();
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
        // Cancel/Close Button
        GameObject btnCancel = new GameObject("BtnCancel");
        btnCancel.transform.SetParent(metalCasing.transform, false);
        RectTransform cr = btnCancel.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(1f, 0f);
        cr.anchorMax = new Vector2(1f, 0f);
        cr.pivot = new Vector2(1f, 0f);
        cr.sizeDelta = new Vector2(240f, 40f);
        cr.anchoredPosition = new Vector2(-30f, 20f);

        Image cImg = btnCancel.AddComponent<Image>();
        cImg.color = new Color(0.45f, 0.15f, 0.15f, 1f); // Dark brick red

        Button cBtn = btnCancel.AddComponent<Button>();
        cBtn.onClick.AddListener(CancelarYSalir);
        ConfigurarBotonHoverColors(cBtn, cImg.color);

        GameObject cTextObj = new GameObject("Text");
        cTextObj.transform.SetParent(btnCancel.transform, false);
        TextMeshProUGUI cTxt = cTextObj.AddComponent<TextMeshProUGUI>();
        cTxt.text = "VOLVER / ESCAPAR";
        cTxt.alignment = TextAlignmentOptions.Center;
        cTxt.fontSize = 12f;
        cTxt.fontStyle = FontStyles.Bold;
        cTxt.color = new Color(0.9f, 0.9f, 0.85f, 1f);
        CopiarFuenteTemplate(cTxt);
        RectTransformCentrar(cTextObj.GetComponent<RectTransform>());

        // Reset/Reshuffle Button
        GameObject btnReset = new GameObject("BtnReset");
        btnReset.transform.SetParent(metalCasing.transform, false);
        RectTransform rr = btnReset.AddComponent<RectTransform>();
        rr.anchorMin = new Vector2(0f, 0f);
        rr.anchorMax = new Vector2(0f, 0f);
        rr.pivot = new Vector2(0f, 0f);
        rr.sizeDelta = new Vector2(240f, 40f);
        rr.anchoredPosition = new Vector2(30f, 20f);

        Image rImg = btnReset.AddComponent<Image>();
        rImg.color = new Color(0.2f, 0.28f, 0.18f, 1f); // Olive green

        Button rBtn = btnReset.AddComponent<Button>();
        rBtn.onClick.AddListener(MezclarPuzzle);
        ConfigurarBotonHoverColors(rBtn, rImg.color);

        GameObject rTextObj = new GameObject("Text");
        rTextObj.transform.SetParent(btnReset.transform, false);
        TextMeshProUGUI rTxt = rTextObj.AddComponent<TextMeshProUGUI>();
        rTxt.text = "MEZCLAR PIEZAS";
        rTxt.alignment = TextAlignmentOptions.Center;
        rTxt.fontSize = 12f;
        rTxt.fontStyle = FontStyles.Bold;
        rTxt.color = new Color(0.9f, 0.9f, 0.85f, 1f);
        CopiarFuenteTemplate(rTxt);
        RectTransformCentrar(rTextObj.GetComponent<RectTransform>());
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
