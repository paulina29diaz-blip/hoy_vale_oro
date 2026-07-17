using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using TMPro;

public class WheelsMinigame : MonoBehaviour
{
    private DialogoManager dialogoManager;

    private GameObject puzzlePanel;
    private GameObject metalCasing;
    private GameObject board;
    private GameObject backgroundContainer;
    
    private Texture2D bgTexture;
    private Texture2D wheelTexture;
    private Sprite bgSprite;
    private Sprite wheelSprite;

    // Sockets data
    private GameObject[] wheelObjects = new GameObject[3];
    private RectTransform[] wheelRects = new RectTransform[3];
    
    // Values (0 to 9) currently pointing at the bottom arrow for each wheel
    private int[] currentValues = new int[3];
    private float[] targetAngles = new float[3];
    private float[] currentAngles = new float[3];

    private bool isSolved = false;

    public void Iniciar(DialogoManager manager)
    {
        dialogoManager = manager;
        isSolved = false;

        // Initialize target values (0 to 9) randomly, making sure none start at 7 (correct code)
        for (int i = 0; i < 3; i++)
        {
            int val = Random.Range(0, 10);
            while (val == 7)
            {
                val = Random.Range(0, 10);
            }
            currentValues[i] = val;
            
            // Formula to position number N at the bottom arrow:
            // Default rotation (0) has 5 at the bottom, and each step is 36 degrees.
            // Rotating counter-clockwise (positive angle) shifts numbers clockwise.
            // 7 is clockwise from 5, so it needs a positive rotation: (7 - 5) * 36 = 72 degrees.
            float targetAngle = (val - 5) * 36f;
            targetAngles[i] = targetAngle;
            currentAngles[i] = targetAngle;
        }

        // Hide DialogoManager panel to focus on the minigame
        if (dialogoManager.panelDialogo != null)
        {
            dialogoManager.panelDialogo.SetActive(false);
        }

        // Get the active canvas
        Canvas canvas = FindAnyObjectByType<Canvas>();
        Transform parentTransform = canvas != null ? canvas.transform : transform;

        // 1. Create main full-screen dark overlay panel
        puzzlePanel = new GameObject("WheelsMinigamePanel");
        puzzlePanel.transform.SetParent(parentTransform, false);
        
        RectTransform panelRect = puzzlePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImg = puzzlePanel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.11f, 0.1f, 0.96f); // Dark industrial overlay

        // 2. Create the central metal container box
        metalCasing = new GameObject("MetalCasing");
        metalCasing.transform.SetParent(puzzlePanel.transform, false);

        RectTransform casingRect = metalCasing.AddComponent<RectTransform>();
        casingRect.anchorMin = new Vector2(0.5f, 0.5f);
        casingRect.anchorMax = new Vector2(0.5f, 0.5f);
        casingRect.pivot = new Vector2(0.5f, 0.5f);
        casingRect.sizeDelta = new Vector2(1088f, 832f); // 16:9 board box
        casingRect.anchoredPosition = Vector2.zero;

        Image casingImg = metalCasing.AddComponent<Image>();
        casingImg.color = new Color(0.24f, 0.22f, 0.18f, 1f); // Rusty industrial metal brown

        Outline casingOutline = metalCasing.AddComponent<Outline>();
        casingOutline.effectColor = new Color(0.1f, 0.08f, 0.06f, 1f);
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
            rivet.AddComponent<Outline>().effectColor = new Color(0.12f, 0.1f, 0.08f, 0.8f);
        }

        // 3. Load background and wheel textures
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
        titleText.text = "DESTRABAR DEPÓSITO";
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
        instrText.text = "Hacé click en cada rueda para girarla hasta alinear el número 7 con su flecha inferior.";
        instrText.alignment = TextAlignmentOptions.Center;
        instrText.fontStyle = FontStyles.Italic;
        instrText.fontSize = 20f;
        instrText.color = new Color(0.8f, 0.75f, 0.7f, 1.0f);
        CopiarFuenteTemplate(instrText);

        // 5. Create board slot container (fits the background image exactly in 16:9 ratio)
        board = new GameObject("WheelsBoard");
        board.transform.SetParent(metalCasing.transform, false);

        RectTransform boardRect = board.AddComponent<RectTransform>();
        boardRect.anchorMin = new Vector2(0.5f, 0.5f);
        boardRect.anchorMax = new Vector2(0.5f, 0.5f);
        boardRect.pivot = new Vector2(0.5f, 0.5f);
        boardRect.sizeDelta = new Vector2(960f, 539f); // 16:9 ratio scaled
        boardRect.anchoredPosition = new Vector2(0f, -20f);

        Image boardImg = board.AddComponent<Image>();
        boardImg.color = new Color(0.08f, 0.08f, 0.08f, 1f);

        // 6. Background Image (ruedaspuzzle background)
        backgroundContainer = new GameObject("BackgroundImage");
        backgroundContainer.transform.SetParent(board.transform, false);
        RectTransform bgRect = backgroundContainer.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bgImg = backgroundContainer.AddComponent<Image>();
        bgImg.sprite = bgSprite;

        // 7. Overlay wheels (left dial, center dial, right dial)
        // Position coordinates aligned exactly with the printed brass dials on ruedaspuzzle.jpg
        float[] xPositions = new float[] { -261f, 0f, 261f };
        float wheelSize = 253f; // perfectly circular 1:1 scale for the cropped wheel
        
        for (int i = 0; i < 3; i++)
        {
            GameObject wheelObj = new GameObject("Wheel_" + i);
            wheelObj.transform.SetParent(board.transform, false);
            wheelObjects[i] = wheelObj;

            RectTransform wr = wheelObj.AddComponent<RectTransform>();
            wr.anchorMin = new Vector2(0.5f, 0.5f);
            wr.anchorMax = new Vector2(0.5f, 0.5f);
            wr.pivot = new Vector2(0.5f, 0.5f);
            wr.sizeDelta = new Vector2(wheelSize, wheelSize); // perfect square size matching dial diameter
            wr.anchoredPosition = new Vector2(xPositions[i], -12f); // lowered to align centers exactly
            wheelRects[i] = wr;

            Image img = wheelObj.AddComponent<Image>();
            img.sprite = wheelSprite;
            img.preserveAspect = true; // extra safety flag to prevent stretching

            // Subtle highlight outline
            Outline outline = wheelObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = wheelObj.AddComponent<Button>();
            int index = i;
            btn.onClick.AddListener(() => OnWheelClicked(index));

            // Set initial rotation
            wr.localRotation = Quaternion.Euler(0f, 0f, currentAngles[i]);
        }

        // 8. Create bottom control buttons (Cancel/Close)
        CrearBotonesControl();
    }

    private void CargarTexturas()
    {
        // 1. Load Background Texture from Resources/Sprites/puzzles/ruedaspuzzle
        bgTexture = Resources.Load<Texture2D>("Sprites/puzzles/ruedaspuzzle");
        if (bgTexture != null)
        {
            bgSprite = Sprite.Create(bgTexture, new Rect(0, 0, bgTexture.width, bgTexture.height), new Vector2(0.5f, 0.5f));
        }

        if (bgSprite == null)
        {
            Debug.LogError("[WheelsMinigame] No se pudo cargar ruedaspuzzle desde Resources. Usando fallback.");
            Texture2D fallbackBg = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            for (int p = 0; p < pixels.Length; p++) pixels[p] = new Color(0.15f, 0.15f, 0.18f); // dark slate
            fallbackBg.SetPixels(pixels);
            fallbackBg.Apply();
            bgSprite = Sprite.Create(fallbackBg, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        }

        // 2. Load Cropped Wheel Texture from Resources/Sprites/puzzles/ruedapuzzle_cropped
        wheelTexture = Resources.Load<Texture2D>("Sprites/puzzles/ruedapuzzle_cropped");
        if (wheelTexture != null)
        {
            wheelSprite = Sprite.Create(wheelTexture, new Rect(0, 0, wheelTexture.width, wheelTexture.height), new Vector2(0.5f, 0.5f));
        }

        if (wheelSprite == null)
        {
            Debug.LogError("[WheelsMinigame] No se pudo cargar ruedapuzzle_cropped desde Resources. Usando fallback.");
            Texture2D fallbackWheel = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            for (int p = 0; p < pixels.Length; p++) pixels[p] = Color.yellow;
            fallbackWheel.SetPixels(pixels);
            fallbackWheel.Apply();
            wheelSprite = Sprite.Create(fallbackWheel, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        }
    }

    private void OnWheelClicked(int index)
    {
        if (isSolved) return;

        // Shift value clockwise by 1 ( N -> N + 1 )
        // Let's increment current value (wrap 0 to 9)
        currentValues[index] = (currentValues[index] + 1) % 10;
        
        // Calculate new target angle using correct rotation formula (val - 5) * 36
        targetAngles[index] = (currentValues[index] - 5) * 36f;

        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        // Solved when all 3 wheels point to number 7 at the bottom arrow (Code 777)
        bool win = true;
        for (int i = 0; i < 3; i++)
        {
            if (currentValues[i] != 7)
            {
                win = false;
                break;
            }
        }

        if (win)
        {
            isSolved = true;
            Debug.Log("[WheelsMinigame] ¡Mecanismo destrabado con código 777!");

            // Highlight all borders green as success feedback
            for (int i = 0; i < 3; i++)
            {
                if (wheelObjects[i] != null)
                {
                    Outline outline = wheelObjects[i].GetComponent<Outline>();
                    if (outline != null)
                    {
                        outline.effectColor = Color.green;
                        outline.effectDistance = new Vector2(3f, 3f);
                    }
                    Button btn = wheelObjects[i].GetComponent<Button>();
                    if (btn != null) btn.interactable = false;
                }
            }

            // Show success label overlay
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(metalCasing.transform, false);
            RectTransform sr = statusObj.AddComponent<RectTransform>();
            sr.anchorMin = new Vector2(0.5f, 0.5f);
            sr.anchorMax = new Vector2(0.5f, 0.5f);
            sr.sizeDelta = new Vector2(450f, 60f);
            sr.anchoredPosition = new Vector2(0f, -10f);

            TextMeshProUGUI sTxt = statusObj.AddComponent<TextMeshProUGUI>();
            sTxt.text = "¡MECANISMO DESTRABADO!";
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

    private void Update()
    {
        // Smoothly interpolate rotations to their target angles
        for (int i = 0; i < 3; i++)
        {
            if (wheelRects[i] != null)
            {
                currentAngles[i] = Mathf.MoveTowardsAngle(currentAngles[i], targetAngles[i], Time.deltaTime * 360f);
                wheelRects[i].localRotation = Quaternion.Euler(0f, 0f, currentAngles[i]);
            }
        }
    }

    private void FinalizarConExito()
    {
        if (dialogoManager != null)
        {
            dialogoManager.panelDialogo.SetActive(true);
            dialogoManager.FinalizarPuzzleExitoMartin();
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

    private void LimpiarCables()
    {
        // Dummy method just in case it is called
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
