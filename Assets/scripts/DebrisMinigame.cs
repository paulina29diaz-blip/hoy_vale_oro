using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.Collections.Generic;
using TMPro;

public class DebrisMinigame : MonoBehaviour
{
    private DialogoManager dialogoManager;

    private GameObject puzzlePanel;
    private GameObject metalCasing;
    private GameObject board;
    private GameObject boxContainer;
    
    private Texture2D boxTexture;
    private Texture2D[] debrisTextures = new Texture2D[6];
    private Sprite boxSprite;
    private Sprite[] debrisSprites = new Sprite[6];
    
    private GameObject[] debrisObjects = new GameObject[6];
    private RectTransform[] debrisRects = new RectTransform[6];
    
    // Initial pile layout (overlapping in the center)
    private Vector2[] initialPositions = new Vector2[]
    {
        new Vector2(0f, 30f),       // Center top
        new Vector2(-110f, 90f),    // Top-left offset
        new Vector2(120f, 70f),     // Top-right offset
        new Vector2(-100f, -80f),   // Bottom-left offset
        new Vector2(110f, -70f),    // Bottom-right offset
        new Vector2(10f, -100f)     // Center bottom
    };

    private float[] rotations = new float[] { 0f, 25f, -15f, -30f, 40f, -10f };
    private Vector2[] sizes = new Vector2[]
    {
        new Vector2(340f, 340f),
        new Vector2(290f, 290f),
        new Vector2(310f, 310f),
        new Vector2(300f, 300f),
        new Vector2(280f, 280f),
        new Vector2(320f, 320f)
    };

    private bool isSolved = false;
    private Canvas canvas;

    public void Iniciar(DialogoManager manager)
    {
        dialogoManager = manager;
        isSolved = false;

        // Hide DialogoManager panel to focus on the minigame
        if (dialogoManager.panelDialogo != null)
        {
            dialogoManager.panelDialogo.SetActive(false);
        }

        // Get the active canvas
        canvas = FindAnyObjectByType<Canvas>();
        Transform parentTransform = canvas != null ? canvas.transform : transform;

        // 1. Create main full-screen dark overlay panel
        puzzlePanel = new GameObject("DebrisMinigamePanel");
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
        casingImg.color = new Color(0.22f, 0.24f, 0.26f, 1f); // Steel grey casing

        Outline casingOutline = metalCasing.AddComponent<Outline>();
        casingOutline.effectColor = new Color(0.08f, 0.09f, 0.1f, 1f);
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
            rImg.color = new Color(0.35f, 0.38f, 0.4f, 1f);
            rivet.AddComponent<Outline>().effectColor = new Color(0.08f, 0.1f, 0.12f, 0.8f);
        }

        // 3. Load box and debris textures
        CargarTexturas();

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
        titleText.text = "DESPEJAR ESCOMBROS";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        titleText.fontSize = 24f;
        titleText.color = new Color(0.9f, 0.93f, 0.95f, 1f);
        CopiarFuenteTemplate(titleText);

        // 4b. Create Instruction Text (Explanation on how to clear debris)
        GameObject instrObj = new GameObject("InstructionText");
        instrObj.transform.SetParent(metalCasing.transform, false);
        RectTransform instrRect = instrObj.AddComponent<RectTransform>();
        instrRect.anchorMin = new Vector2(0f, 1f);
        instrRect.anchorMax = new Vector2(1f, 1f);
        instrRect.pivot = new Vector2(0.5f, 1f);
        instrRect.sizeDelta = new Vector2(0f, 55f);
        instrRect.anchoredPosition = new Vector2(0f, -50f);

        TextMeshProUGUI instrText = instrObj.AddComponent<TextMeshProUGUI>();
        instrText.text = "Arrastrá los escombros hacia afuera para descubrir la caja.";
        instrText.alignment = TextAlignmentOptions.Center;
        instrText.fontStyle = FontStyles.Italic;
        instrText.fontSize = 20f;
        instrText.color = new Color(0.75f, 0.78f, 0.8f, 1.0f);
        CopiarFuenteTemplate(instrText);

        // 5. Create board slot container
        board = new GameObject("DebrisBoard");
        board.transform.SetParent(metalCasing.transform, false);

        RectTransform boardRect = board.AddComponent<RectTransform>();
        boardRect.anchorMin = new Vector2(0.5f, 0.5f);
        boardRect.anchorMax = new Vector2(0.5f, 0.5f);
        boardRect.pivot = new Vector2(0.5f, 0.5f);
        boardRect.sizeDelta = new Vector2(800f, 800f);
        boardRect.anchoredPosition = new Vector2(0f, -20f);

        Image boardImg = board.AddComponent<Image>();
        boardImg.color = new Color(0.08f, 0.08f, 0.08f, 1f); // Dark inner slot background

        // 6. Create Box Image (behind debris)
        boxContainer = new GameObject("BoxImage");
        boxContainer.transform.SetParent(board.transform, false);
        RectTransform boxRect = boxContainer.AddComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(560f, 560f); // Box size
        boxRect.anchoredPosition = Vector2.zero;

        Image boxImg = boxContainer.AddComponent<Image>();
        boxImg.sprite = boxSprite;

        // 7. Create debris GameObjects (in front of box, stacked on top)
        for (int i = 0; i < 6; i++)
        {
            GameObject debrisObj = new GameObject("Debris_" + i);
            debrisObj.transform.SetParent(board.transform, false);
            debrisObjects[i] = debrisObj;

            RectTransform pr = debrisObj.AddComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.5f, 0.5f);
            pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.pivot = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = sizes[i];
            pr.anchoredPosition = initialPositions[i];
            pr.localRotation = Quaternion.Euler(0f, 0f, rotations[i]);
            debrisRects[i] = pr;

            Image img = debrisObj.AddComponent<Image>();
            img.sprite = debrisSprites[i];

            // Muted border outline
            Outline outline = debrisObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Add Draggable helper component
            DraggableDebris dragHelper = debrisObj.AddComponent<DraggableDebris>();
            dragHelper.Init(canvas, CheckWinCondition);
        }

        // 8. Create bottom control buttons (Cancel/Close)
        CrearBotonesControl();
    }

    private void CargarTexturas()
    {
        // 1. Load Box Texture from Resources/Sprites/puzzles/cajafernandezpuzzle
        boxTexture = Resources.Load<Texture2D>("Sprites/puzzles/cajafernandezpuzzle");
        if (boxTexture != null)
        {
            boxSprite = Sprite.Create(boxTexture, new Rect(0, 0, boxTexture.width, boxTexture.height), new Vector2(0.5f, 0.5f));
        }

        if (boxSprite == null)
        {
            Debug.LogError("[DebrisMinigame] No se pudo cargar cajafernandezpuzzle desde Resources. Usando fallback.");
            Texture2D fallbackBox = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            for (int p = 0; p < pixels.Length; p++) pixels[p] = new Color(0.5f, 0.35f, 0.2f); // wood brown
            fallbackBox.SetPixels(pixels);
            fallbackBox.Apply();
            boxSprite = Sprite.Create(fallbackBox, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        }

        // 2. Load 6 Debris Textures individually from Resources/Sprites/puzzles/escombroX
        for (int i = 0; i < 6; i++)
        {
            int fileNum = i + 1;
            string resourcePath = "Sprites/puzzles/escombro" + fileNum;
            debrisTextures[i] = Resources.Load<Texture2D>(resourcePath);
            bool debrisLoaded = false;
            
            if (debrisTextures[i] != null)
            {
                debrisSprites[i] = Sprite.Create(debrisTextures[i], new Rect(0, 0, debrisTextures[i].width, debrisTextures[i].height), new Vector2(0.5f, 0.5f));
                debrisLoaded = true;
            }

            if (!debrisLoaded)
            {
                Debug.LogError("[DebrisMinigame] No se pudo cargar " + resourcePath + " desde Resources. Usando fallback.");
                Color[] colors = new Color[] { Color.grey, Color.darkGray, new Color(0.3f, 0.3f, 0.3f), new Color(0.4f, 0.4f, 0.4f), Color.grey, Color.darkGray };
                Texture2D tex = new Texture2D(64, 64);
                Color[] pixels = new Color[64 * 64];
                for (int p = 0; p < pixels.Length; p++) pixels[p] = colors[i];
                tex.SetPixels(pixels);
                tex.Apply();
                debrisSprites[i] = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
            }
        }
    }

    private void CheckWinCondition()
    {
        if (isSolved) return;

        // Check if all 6 pieces of debris have been dragged far enough from the center (0, 0)
        // Threshold: 165 pixels from center of the board
        bool win = true;
        for (int i = 0; i < 6; i++)
        {
            if (debrisRects[i] != null)
            {
                float dist = Vector2.Distance(debrisRects[i].anchoredPosition, Vector2.zero);
                if (dist < 330f)
                {
                    win = false;
                    break;
                }
            }
        }

        if (win)
        {
            isSolved = true;
            Debug.Log("[DebrisMinigame] ¡Caja descubierta con éxito!");

            // Disable dragging on all pieces
            for (int i = 0; i < 6; i++)
            {
                if (debrisObjects[i] != null)
                {
                    DraggableDebris dragHelper = debrisObjects[i].GetComponent<DraggableDebris>();
                    if (dragHelper != null) Destroy(dragHelper);
                }
            }

            // Show success label overlay
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(metalCasing.transform, false);
            RectTransform sr = statusObj.AddComponent<RectTransform>();
            sr.anchorMin = new Vector2(0.5f, 0.5f);
            sr.anchorMax = new Vector2(0.5f, 0.5f);
            sr.sizeDelta = new Vector2(400f, 60f);
            sr.anchoredPosition = new Vector2(0f, -10f);

            TextMeshProUGUI sTxt = statusObj.AddComponent<TextMeshProUGUI>();
            sTxt.text = "¡CAJA RECUPERADA!";
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
            dialogoManager.FinalizarPuzzleExitoBeatriz();
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

// Drag & Drop helper component that handles mouse and touch drag events in Unity UI
public class DraggableDebris : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private System.Action onDragCallback;

    public void Init(Canvas parentCanvas, System.Action dragCallback)
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = parentCanvas;
        onDragCallback = dragCallback;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Bring to front on drag so it renders above other debris pieces
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform != null && canvas != null)
        {
            // Adjust delta relative to UI canvas scaling factor
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
            
            // Trigger distance checks
            if (onDragCallback != null)
            {
                onDragCallback();
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (onDragCallback != null)
        {
            onDragCallback();
        }
    }
}
