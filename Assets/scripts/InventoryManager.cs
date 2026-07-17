using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;

public class InventoryManager : MonoBehaviour
{
    private static InventoryManager instance;

    public static InventoryManager Instance
    {
        get { return instance; }
    }

    [System.Serializable]
    public class Item
    {
        public string code;        // Military code or quantity label (e.g. 200, BOX)
        public string name;        // Clean display name
        public float weight;       // Weight in KG
        public string type;        // $, i, or 🛠
        public string description; // Detail description
        public Sprite customIcon;  // Loaded icon if available
        public int quantity;       // Quantity of the item
        public bool isLocked;      // Locked state for trades
    }

    public List<Item> items = new List<Item>();
    private static List<Item> itemTemplates = new List<Item>();
    private const float MaxWeight = 200.0f; // Adjusted to match 200 KG in screenshot
    private int selectedIndex = -1;

    // UI GameObjects
    private GameObject canvasObjeto;
    private GameObject hudPanelObjeto;
    private GameObject panelInventarioObjeto;
    private GameObject gridContainerObjeto;
    private GameObject tooltipObjeto;
    private GameObject barraNaftaObjeto;
    private GameObject panelGameOverObjeto;
    private GameObject btnConfiguracionObjeto;
    private GameObject panelConfiguracionObjeto;

    private RectTransform liquidBarRect;
    private float maxFuelWidth;
    private static float currentFuel = 1.0f;

    public static float CurrentFuel
    {
        get { return currentFuel; }
        set
        {
            currentFuel = Mathf.Clamp01(value);
            if (instance != null)
            {
                instance.UpdateFuelBarUI();
            }
        }
    }

    private Movimiento jugadorMovimiento;
    private Vector3 ultimaPosicionJugador;
    private bool trackeandoPosicion = false;
    private float fuelConsumptionRate = 0.02f; // Consumo por unidad de distancia

    public void UpdateFuelBarUI()
    {
        float currentWidth = maxFuelWidth * currentFuel;
        if (liquidBarRect != null)
        {
            liquidBarRect.sizeDelta = new Vector2(currentWidth, liquidBarRect.sizeDelta.y);
        }
    }
    
    // UI Text Fields
    private TextMeshProUGUI txtHudCapacidad;
    private TextMeshProUGUI txtCapacidad;
    private TextMeshProUGUI txtTotalWeight;
    private TextMeshProUGUI txtTooltipName;
    private TextMeshProUGUI txtTooltipDesc;
    private TextMeshProUGUI txtHudHint;
    private TextMeshProUGUI txtBtnLockLabel;

    private List<GameObject> hudSlots = new List<GameObject>();
    private List<GameObject> slotObjects = new List<GameObject>();

    private GameObject btnLockObj;
    private GameObject btnOfrecerObj;
    private Material silhouetteMaterial;

    // Patience bar elements
    private GameObject patienceBarContainer;
    private Image patienceLiquidImg;
    private TextMeshProUGUI txtPatienceBarLabel;
    private float currentPatience = 1.0f;
    private float patienceTimerDuration = 35f; // 35 seconds to choose (slower)
    private bool isPatienceBarActive = false;

    // Rastrojero checking button and labels
    private GameObject btnRevisarRastrojero;
    private GameObject txtExplicacionRastrojero;
    private bool isRevisandoRastrojero = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Initialize()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("InventorySystem");
            instance = go.AddComponent<InventoryManager>();
            DontDestroyOnLoad(go);
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Pre-populate starting items and load custom sprites
        PopulateStartingItems();

        // Move all populated items to templates, then only add starting items to active inventory
        itemTemplates.Clear();
        itemTemplates.AddRange(items);
        items.Clear();

        string[] startingNames = new string[] {
            "termo", "pitusas", "fosforitos", "pava", "garrafa", "gancia"
        };
        foreach (string sName in startingNames)
        {
            Item temp = GetTemplateByName(sName);
            if (temp != null)
            {
                items.Add(new Item {
                    code = temp.code,
                    name = temp.name,
                    weight = temp.weight,
                    type = temp.type,
                    description = temp.description,
                    customIcon = temp.customIcon,
                    quantity = temp.quantity
                });
            }
        }

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Time.timeScale = 1f; // Reset timescale on any scene load to prevent freeze bugs
        VerificarEscenaMenu(scene.name);

        // Diálogo introductorio en el MAPA (solo la primera vez por partida)
        if (scene.name == "mapa")
        {
            IntroDialogo.IntentarMostrar();
        }


        // Ensure EventSystem is present and active in the newly loaded scene so UI buttons work
        UnityEngine.EventSystems.EventSystem existingES = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (existingES == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("InventoryManager: EventSystem dynamically spawned in scene " + scene.name);
        }
        else
        {
            existingES.gameObject.SetActive(true);
            existingES.enabled = true;
            if (existingES.GetComponent<UnityEngine.EventSystems.BaseInputModule>() == null)
            {
                existingES.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("InventoryManager: StandaloneInputModule dynamically added to existing EventSystem in " + scene.name);
            }
        }
    }

    private void VerificarEscenaMenu(string sceneName)
    {
        isPatienceBarActive = false;
        if (patienceBarContainer != null) patienceBarContainer.SetActive(false);
        if (btnRevisarRastrojero != null) btnRevisarRastrojero.SetActive(false);
        if (txtExplicacionRastrojero != null) txtExplicacionRastrojero.SetActive(false);
        bool isMenuOrMap = (sceneName == "menu" || sceneName == "videointro" || sceneName == "mapa" || sceneName == "dialogo" || sceneName == "dialogos" || sceneName == "Intropikirrin");
        if (sceneName == "menu" || sceneName == "videointro" || sceneName == "Intropikirrin")
        {
            CurrentFuel = 1.0f;
        }

        if (canvasObjeto != null)
        {
            canvasObjeto.SetActive(!isMenuOrMap); // Disable the entire canvas on menu/map scenes to prevent blocking raycasts
        }
        if (hudPanelObjeto != null)
        {
            hudPanelObjeto.SetActive(!isMenuOrMap);
        }
        if (panelInventarioObjeto != null)
        {
            panelInventarioObjeto.SetActive(false);
        }
        if (barraNaftaObjeto != null)
        {
            barraNaftaObjeto.SetActive(sceneName.StartsWith("nivel_"));
            UpdateFuelBarUI();
        }
        if (panelGameOverObjeto != null)
        {
            panelGameOverObjeto.SetActive(false);
        }
        if (btnConfiguracionObjeto != null)
        {
            btnConfiguracionObjeto.SetActive(!isMenuOrMap);
        }
        if (panelConfiguracionObjeto != null)
        {
            panelConfiguracionObjeto.SetActive(false);
        }
    }

    void Start()
    {
        // Programmatically build the entire inventory UI
        ConstruirUI();
        UpdateInventoryUI();

        // Initial check for the scene
        VerificarEscenaMenu(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void Update()
    {
        // Disable keyboard interactions on the menu scene, video scene, map scene, and dialogue scenes
        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (activeScene == "menu" || activeScene == "videointro" || activeScene == "mapa" || activeScene == "dialogo" || activeScene == "dialogos" || activeScene == "Intropikirrin")
        {
            return;
        }

        // Listen for the "Y" key to toggle the inventory overlay without mouse input
        if (Input.GetKeyDown(KeyCode.Y))
        {
            ToggleInventario();
        }

        // Listen for Escape key to toggle pause menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (panelConfiguracionObjeto != null)
            {
                if (panelConfiguracionObjeto.activeSelf)
                {
                    CerrarMenuPausa();
                }
                else
                {
                    AbrirMenuPausa();
                }
            }
        }

        // Fuel depletion logic in gameplay scene
        if (activeScene.StartsWith("nivel_"))
        {
            if (jugadorMovimiento == null)
            {
                jugadorMovimiento = FindFirstObjectByType<Movimiento>();
                if (jugadorMovimiento != null)
                {
                    ultimaPosicionJugador = jugadorMovimiento.transform.position;
                    trackeandoPosicion = true;
                }
            }
            else
            {
                Vector3 posicionActual = jugadorMovimiento.transform.position;
                float distancia = Vector3.Distance(posicionActual, ultimaPosicionJugador);
                if (distancia > 0f)
                {
                    CurrentFuel -= distancia * fuelConsumptionRate;
                    ultimaPosicionJugador = posicionActual;
                }
            }

            // Check for Game Over condition
            if (CurrentFuel <= 0f && panelGameOverObjeto != null && !panelGameOverObjeto.activeSelf)
            {
                panelGameOverObjeto.SetActive(true);
                if (btnConfiguracionObjeto != null)
                {
                    btnConfiguracionObjeto.SetActive(false); // Hide gear config button on defeat
                }
                Time.timeScale = 0f; // Freeze game on game over
            }
        }
        else
        {
            jugadorMovimiento = null;
            trackeandoPosicion = false;
        }

        // Patience bar depletion logic during Beto's trade
        if (isPatienceBarActive && panelInventarioObjeto != null && panelInventarioObjeto.activeSelf)
        {
            float rateMultiplier = isRevisandoRastrojero ? 4.0f : 1.0f; // 4x faster depletion when looking at inventory!
            currentPatience -= (Time.deltaTime * rateMultiplier) / patienceTimerDuration;
            if (patienceLiquidImg != null)
            {
                // Physically shrink the bar width since Image.Type.Filled ignores null sprite crop
                patienceLiquidImg.rectTransform.sizeDelta = new Vector2(434f * currentPatience, -6f);
                patienceLiquidImg.color = Color.Lerp(new Color(0.9f, 0.15f, 0.15f, 0.9f), new Color(0.2f, 0.75f, 0.3f, 0.9f), currentPatience);
            }

            if (currentPatience <= 0f)
            {
                currentPatience = 0f;
                isPatienceBarActive = false;
                isRevisandoRastrojero = false;
                CloseInventario();
                if (DialogoManager.Instance != null)
                {
                    DialogoManager.Instance.RechazarTrato();
                    if (DialogoManager.Instance.popupTexto != null)
                    {
                        DialogoManager.Instance.popupTexto.text = "<color=#F44336>¡TRATO NO HECHO!</color>\n\nBeto se quedó sin paciencia.";
                    }
                }
            }
        }
    }

    private void PopulateStartingItems()
    {
        // Try loading custom sprites from Resources
        List<Sprite> objetoSprites = CargarSpritesObjetos();

        // Fallbacks if objects directory is empty
        Sprite fallbackBidon = CargarSpriteDesdeResources("Sprites/bidon");
        Sprite fallbackCamioneta = CargarSpriteDesdeResources("Sprites/camioneta");

        // Helper to get sprite by index with fallbacks
        System.Func<int, Sprite> GetSprite = (index) => {
            if (objetoSprites != null && index < objetoSprites.Count && objetoSprites[index] != null)
            {
                return objetoSprites[index];
            }
            return (index % 2 == 0) ? fallbackBidon : fallbackCamioneta;
        };

        // 12 Items corresponding to objeto1.png to objeto12.png
        items.Add(new Item 
        { 
            code = "NAF", 
            name = "nafta", 
            weight = 4.5f, 
            type = "i", 
            description = "Bidón con nafta, esencial para el Rastrojero.",
            customIcon = GetSprite(0),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "TER", 
            name = "termo", 
            weight = 1.8f, 
            type = "$", 
            description = "Termo para mantener el agua caliente.",
            customIcon = GetSprite(1),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "PIT", 
            name = "pitusas", 
            weight = 0.3f, 
            type = "$", 
            description = "Paquete de galletitas pitusas. Un clásico infaltable para el viaje.",
            customIcon = GetSprite(2),
            quantity = 3
        });

        items.Add(new Item 
        { 
            code = "FOS", 
            name = "fosforitos", 
            weight = 0.1f, 
            type = "$", 
            description = "Caja de fósforos pequeños para encender fuego.",
            customIcon = GetSprite(3),
            quantity = 40
        });

        items.Add(new Item 
        { 
            code = "PAV", 
            name = "pava", 
            weight = 1.2f, 
            type = "$", 
            description = "Pava metálica para calentar agua para el mate.",
            customIcon = GetSprite(4),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "GAR", 
            name = "garrafa", 
            weight = 10.0f, 
            type = "🛠", 
            description = "Garrafa de gas envasado para cocinar o calefaccionar.",
            customIcon = GetSprite(5),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "TEL", 
            name = "tela impermeable", 
            weight = 2.5f, 
            type = "🛠", 
            description = "Lona o tela impermeable para proteger la carga del viento y lluvia.",
            customIcon = GetSprite(6),
            quantity = 2
        });

        items.Add(new Item 
        { 
            code = "CIN", 
            name = "cinta aislante", 
            weight = 0.2f, 
            type = "🛠", 
            description = "Rollo de cinta aisladora para reparaciones eléctricas rápidas.",
            customIcon = GetSprite(7),
            quantity = 3
        });

        items.Add(new Item 
        { 
            code = "HER", 
            name = "caja de herramientas", 
            weight = 8.5f, 
            type = "🛠", 
            description = "Caja metálica con llaves, pinzas y destornilladores.",
            customIcon = GetSprite(8),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "MAN", 
            name = "manguera", 
            weight = 1.5f, 
            type = "🛠", 
            description = "Manguera de goma útil para traspasar combustible o agua.",
            customIcon = GetSprite(9),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "FRA", 
            name = "frasada", 
            weight = 2.0f, 
            type = "$", 
            description = "Frazada abrigada para las noches frías en la ruta.",
            customIcon = GetSprite(10),
            quantity = 2
        });

        items.Add(new Item 
        { 
            code = "BAT", 
            name = "bateria", 
            weight = 15.0f, 
            type = "🛠", 
            description = "Batería de auto de 12V, pesada pero indispensable.",
            customIcon = GetSprite(11),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "CAC", 
            name = "cacerola", 
            weight = 1.5f, 
            type = "🛠", 
            description = "Cacerola de metal para cocinar comida en el campamento.",
            customIcon = GetSprite(12),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "GAN", 
            name = "gancia", 
            weight = 1.0f, 
            type = "$", 
            description = "Aperitivo Gancia, ideal para relajarse después de un largo día.",
            customIcon = GetSprite(13),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "PIN", 
            name = "pinza", 
            weight = 0.8f, 
            type = "🛠", 
            description = "Pinza metálica fuerte para reparaciones o trabajos mecánicos.",
            customIcon = GetSprite(14),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "ALF", 
            name = "alfombra", 
            weight = 3.5f, 
            type = "$", 
            description = "Alfombra tejida pequeña para aislar del frío del suelo.",
            customIcon = GetSprite(15),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "ANA", 
            name = "anafe", 
            weight = 2.2f, 
            type = "🛠", 
            description = "Anafe portátil a gas para cocinar de forma rápida.",
            customIcon = GetSprite(16),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "SOG", 
            name = "soga", 
            weight = 1.2f, 
            type = "🛠", 
            description = "Soga de cáñamo resistente, útil para atar carga.",
            customIcon = GetSprite(17),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "MAT", 
            name = "mate", 
            weight = 0.5f, 
            type = "$", 
            description = "Mate de madera listo para tomar con yerba.",
            customIcon = GetSprite(18),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "GUA", 
            name = "guantes", 
            weight = 0.3f, 
            type = "🛠", 
            description = "Guantes de cuero reforzados para trabajo pesado.",
            customIcon = GetSprite(19),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "BRU", 
            name = "brujula", 
            weight = 0.2f, 
            type = "$", 
            description = "Brújula militar para no perder la orientación.",
            customIcon = GetSprite(20),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "FAR", 
            name = "farol", 
            weight = 1.6f, 
            type = "🛠", 
            description = "Farol a kerosene para iluminar la noche.",
            customIcon = GetSprite(21),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "BOT", 
            name = "botella", 
            weight = 1.0f, 
            type = "$", 
            description = "Botella de vidrio para almacenar agua potable.",
            customIcon = GetSprite(22),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "OLL", 
            name = "olla", 
            weight = 2.0f, 
            type = "🛠", 
            description = "Olla de chapa grande para guisos en grupo.",
            customIcon = GetSprite(23),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "MAP", 
            name = "mapa", 
            weight = 0.1f, 
            type = "$", 
            description = "Mapa de carreteras desgastado con rutas marcadas.",
            customIcon = GetSprite(24),
            quantity = 1
        });

        items.Add(new Item 
        { 
            code = "MNT", 
            name = "manta solar", 
            weight = 0.5f, 
            type = "$", 
            description = "Manta térmica de tecnología solar para captar calor.",
            customIcon = GetSprite(25),
            quantity = 1
        });
    }

    private List<Sprite> CargarSpritesObjetos()
    {
        List<Sprite> list = new List<Sprite>();
        for (int i = 1; i <= 26; i++)
        {
            Sprite s = CargarSpriteDesdeResources("Sprites/objetos/objeto" + i);
            if (s != null)
            {
                list.Add(s);
            }
            else
            {
                Debug.LogWarning("No se pudo cargar el sprite desde Resources: Sprites/objetos/objeto" + i);
            }
        }
        return list;
    }

    private int ExtraerNumero(string text)
    {
        string numStr = "";
        foreach (char c in text)
        {
            if (char.IsDigit(c)) numStr += c;
        }
        int val;
        return int.TryParse(numStr, out val) ? val : 0;
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

    private void ConstruirUI()
    {
        // 1. Create Canvas
        canvasObjeto = new GameObject("InventoryCanvas");
        canvasObjeto.transform.SetParent(this.transform, false);
        Canvas canvas = canvasObjeto.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Always render on top

        CanvasScaler scaler = canvasObjeto.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObjeto.AddComponent<GraphicRaycaster>();

        // Ensure EventSystem is present
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 2. Create Top-Left HUD Inventory Bar (Replacing old simple button)
        hudPanelObjeto = new GameObject("HUDInventoryBar");
        hudPanelObjeto.transform.SetParent(canvasObjeto.transform, false);

        RectTransform hudPanelRect = hudPanelObjeto.AddComponent<RectTransform>();
        hudPanelRect.anchorMin = new Vector2(0f, 1f);
        hudPanelRect.anchorMax = new Vector2(0f, 1f);
        hudPanelRect.pivot = new Vector2(0f, 1f);
        hudPanelRect.anchoredPosition = new Vector2(50, -45); // Raised closed inventory
        hudPanelRect.sizeDelta = new Vector2(500, 180); // Enlarged closed inventory

        // Header: INVENTARIO [X/200 KG] (Translated to Spanish)
        GameObject hudCapObj = new GameObject("TxtHudCapacidad");
        hudCapObj.transform.SetParent(hudPanelObjeto.transform, false);
        txtHudCapacidad = hudCapObj.AddComponent<TextMeshProUGUI>();
        txtHudCapacidad.text = "INVENTARIO [0]";
        txtHudCapacidad.fontStyle = FontStyles.Bold;
        txtHudCapacidad.fontSize = 22; // Enlarged title
        txtHudCapacidad.color = new Color(0.9f, 0.85f, 0.8f, 1f); // Weathered paper white

        // Distressed outline for title
        Outline titleOutline = hudCapObj.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0.05f, 0.05f, 0.05f, 0.8f);
        titleOutline.effectDistance = new Vector2(1.5f, -1.5f);

        RectTransform hudCapRect = hudCapObj.GetComponent<RectTransform>();
        hudCapRect.anchorMin = new Vector2(0f, 1f);
        hudCapRect.anchorMax = new Vector2(1f, 1f);
        hudCapRect.pivot = new Vector2(0.5f, 1f);
        hudCapRect.anchoredPosition = new Vector2(10, -5);
        hudCapRect.sizeDelta = new Vector2(-20, 30);

        // Horizontal Row of 5 Slots (4 Items + 1 Arrow) - Enlarged and spaced out
        for (int i = 0; i < 5; i++)
        {
            GameObject hudSlot = new GameObject("HUDSlot_" + i);
            hudSlot.transform.SetParent(hudPanelObjeto.transform, false);

            RectTransform slotRect = hudSlot.AddComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0f, 1f);
            slotRect.anchorMax = new Vector2(0f, 1f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(50 + (i * 95), -85); // Enlarged cell spacing & centered
            slotRect.sizeDelta = new Vector2(85, 85); // Enlarged slots

            Image slotImg = hudSlot.AddComponent<Image>();
            slotImg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f); // Dark scrap iron

            Outline slotOutline = hudSlot.AddComponent<Outline>();
            slotOutline.effectColor = new Color(0.35f, 0.35f, 0.35f, 0.6f); // Grey border
            slotOutline.effectDistance = new Vector2(1.5f, 1.5f);

            Button slotBtn = hudSlot.AddComponent<Button>();
            slotBtn.onClick.AddListener(ToggleInventario);

            if (i < 4)
            {
                // Item Icon inside HUD Slot
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(hudSlot.transform, false);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
                iconImg.gameObject.SetActive(false);

                RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(8, 8);
                iconRect.offsetMax = new Vector2(-8, -8);

                // Code/Quantity label text (bottom right)
                GameObject codeObj = new GameObject("TxtCode");
                codeObj.transform.SetParent(hudSlot.transform, false);
                TextMeshProUGUI txtCode = codeObj.AddComponent<TextMeshProUGUI>();
                txtCode.text = "";
                txtCode.alignment = TextAlignmentOptions.BottomRight;
                txtCode.fontSize = 15; // Enlarged label text
                txtCode.fontStyle = FontStyles.Bold;
                txtCode.color = new Color(0.9f, 0.88f, 0.85f, 1f);

                RectTransform codeRect = codeObj.GetComponent<RectTransform>();
                codeRect.anchorMin = Vector2.zero;
                codeRect.anchorMax = Vector2.one;
                codeRect.offsetMin = new Vector2(2, 2);
                codeRect.offsetMax = new Vector2(-5, -2);
            }
            else
            {
                // Slot 5: Arrow indicating "more objects"
                GameObject arrowObj = new GameObject("ArrowText");
                arrowObj.transform.SetParent(hudSlot.transform, false);
                TextMeshProUGUI arrowTxt = arrowObj.AddComponent<TextMeshProUGUI>();
                arrowTxt.text = ">>"; // Standard arrow symbol
                arrowTxt.fontStyle = FontStyles.Bold;
                arrowTxt.alignment = TextAlignmentOptions.Center;
                arrowTxt.fontSize = 28; // Enlarged arrow
                arrowTxt.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                ApplyFont(arrowTxt, true);

                RectTransform arrowRect = arrowObj.GetComponent<RectTransform>();
                arrowRect.anchorMin = Vector2.zero;
                arrowRect.anchorMax = Vector2.one;
                arrowRect.offsetMin = Vector2.zero;
                arrowRect.offsetMax = Vector2.zero;
            }

            hudSlots.Add(hudSlot);
        }

        // Hint text removed — no longer shown below the HUD bar
        txtHudHint = null;


        // Apply custom fonts to HUD
        ApplyFont(txtHudCapacidad, true);
        ApplyFont(txtHudHint, true);

        // 3. Create Main Inventory Grid Panel
        panelInventarioObjeto = new GameObject("PanelInventario");
        panelInventarioObjeto.transform.SetParent(canvasObjeto.transform, false);

        RectTransform panelRect = panelInventarioObjeto.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.anchoredPosition = new Vector2(-50, 0);
        panelRect.sizeDelta = new Vector2(500, 780);

        AplicarEstiloPostApocaliptico(panelInventarioObjeto);

        // 4. Create Panel Header Elements
        GameObject capObj = new GameObject("TxtCapacidad");
        capObj.transform.SetParent(panelInventarioObjeto.transform, false);
        txtCapacidad = capObj.AddComponent<TextMeshProUGUI>();
        txtCapacidad.text = "<color=#FF8C00>CAPACIDAD:</color> RANURAS DE INVENTARIO";
        txtCapacidad.fontStyle = FontStyles.Bold;
        txtCapacidad.fontSize = 18;
        txtCapacidad.color = Color.white;

        RectTransform capRect = capObj.GetComponent<RectTransform>();
        capRect.anchorMin = new Vector2(0f, 1f);
        capRect.anchorMax = new Vector2(1f, 1f);
        capRect.pivot = new Vector2(0.5f, 1f);
        capRect.anchoredPosition = new Vector2(25, -25);
        capRect.sizeDelta = new Vector2(-50, 30);

        // Weight status label
        GameObject weightObj = new GameObject("TxtTotalWeight");
        weightObj.transform.SetParent(panelInventarioObjeto.transform, false);
        txtTotalWeight = weightObj.AddComponent<TextMeshProUGUI>();
        txtTotalWeight.text = "CANTIDAD DE OBJETOS: 0";
        txtTotalWeight.fontStyle = FontStyles.Bold;
        txtTotalWeight.fontSize = 15;
        txtTotalWeight.color = new Color(0.75f, 0.75f, 0.75f, 1f);

        // Create Revisar Rastrojero button
        btnRevisarRastrojero = new GameObject("BtnRevisarRastrojero");
        btnRevisarRastrojero.transform.SetParent(panelInventarioObjeto.transform, false);

        RectTransform rBtnRect = btnRevisarRastrojero.AddComponent<RectTransform>();
        rBtnRect.anchorMin = new Vector2(0.5f, 1f);
        rBtnRect.anchorMax = new Vector2(0.5f, 1f);
        rBtnRect.pivot = new Vector2(0.5f, 1f);
        rBtnRect.anchoredPosition = new Vector2(-110f, -22f);
        rBtnRect.sizeDelta = new Vector2(200f, 50f);

        Image rBtnImg = btnRevisarRastrojero.AddComponent<Image>();
        rBtnImg.color = new Color(0.45f, 0.25f, 0.15f, 1f); // Metallic scrap brown

        Outline rBtnOutline = btnRevisarRastrojero.AddComponent<Outline>();
        rBtnOutline.effectColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        rBtnOutline.effectDistance = new Vector2(1.5f, 1.5f);

        GameObject rBtnTextObj = new GameObject("Text");
        rBtnTextObj.transform.SetParent(btnRevisarRastrojero.transform, false);
        TextMeshProUGUI rBtnText = rBtnTextObj.AddComponent<TextMeshProUGUI>();
        rBtnText.text = "IR AL RASTROJERO\n(MANTENER)";
        rBtnText.fontStyle = FontStyles.Bold;
        rBtnText.alignment = TextAlignmentOptions.Center;
        rBtnText.fontSize = 11;
        rBtnText.color = new Color(0.95f, 0.95f, 0.9f, 1f);
        ApplyFont(rBtnText, true);

        RectTransform rBtnTextRect = rBtnTextObj.GetComponent<RectTransform>();
        rBtnTextRect.anchorMin = Vector2.zero;
        rBtnTextRect.anchorMax = Vector2.one;
        rBtnTextRect.offsetMin = Vector2.zero;
        rBtnTextRect.offsetMax = Vector2.zero;

        RastrojeroPressDetector detector = btnRevisarRastrojero.AddComponent<RastrojeroPressDetector>();
        detector.onDown = () => {
            isRevisandoRastrojero = true;
            // Deduct 15% patience instantly
            currentPatience = Mathf.Max(0f, currentPatience - 0.15f);
            UpdateInventoryUI();
        };
        detector.onUp = () => {
            isRevisandoRastrojero = false;
            UpdateInventoryUI();
        };

        btnRevisarRastrojero.SetActive(false);

        // Create Explicacion Rastrojero text
        txtExplicacionRastrojero = new GameObject("TxtExplicacionRastrojero");
        txtExplicacionRastrojero.transform.SetParent(panelInventarioObjeto.transform, false);

        RectTransform rExpRect = txtExplicacionRastrojero.AddComponent<RectTransform>();
        TextMeshProUGUI rExpText = txtExplicacionRastrojero.AddComponent<TextMeshProUGUI>();
        rExpText.text = "<color=#FF8C00>REGLA:</color>\nIr al rastrojero cuesta paciencia (-15%) y la consume 4 veces más rápido.";
        rExpText.alignment = TextAlignmentOptions.Left;
        rExpText.fontSize = 10f;
        rExpText.fontStyle = FontStyles.Normal;
        rExpText.color = new Color(0.85f, 0.85f, 0.8f, 1f);
        ApplyFont(rExpText, false);

        rExpRect.anchorMin = new Vector2(0.5f, 1f);
        rExpRect.anchorMax = new Vector2(0.5f, 1f);
        rExpRect.pivot = new Vector2(0.5f, 1f);
        rExpRect.anchoredPosition = new Vector2(110f, -22f);
        rExpRect.sizeDelta = new Vector2(200f, 50f);

        txtExplicacionRastrojero.SetActive(false);

        // Rastrojero Cargo Tab/Bar
        GameObject tabObj = new GameObject("CargoTab");
        tabObj.transform.SetParent(panelInventarioObjeto.transform, false);
        
        RectTransform tabRect = tabObj.AddComponent<RectTransform>();
        tabRect.anchorMin = new Vector2(0f, 1f);
        tabRect.anchorMax = new Vector2(1f, 1f);
        tabRect.pivot = new Vector2(0.5f, 1f);
        tabRect.anchoredPosition = new Vector2(0, -90);
        tabRect.sizeDelta = new Vector2(-40, 35);

        Image tabImg = tabObj.AddComponent<Image>();
        tabImg.color = new Color(0.48f, 0.24f, 0.05f, 0.9f);
        
        Outline tabOutline = tabObj.AddComponent<Outline>();
        tabOutline.effectColor = new Color(0.3f, 0.15f, 0.03f, 0.8f);
        tabOutline.effectDistance = new Vector2(1, 1);

        GameObject tabTextObj = new GameObject("Text");
        tabTextObj.transform.SetParent(tabObj.transform, false);
        TextMeshProUGUI tabText = tabTextObj.AddComponent<TextMeshProUGUI>();
        tabText.text = "Carga del Rastrojero";
        tabText.fontStyle = FontStyles.Bold;
        tabText.alignment = TextAlignmentOptions.Center;
        tabText.fontSize = 16;
        tabText.color = new Color(0.95f, 0.9f, 0.85f, 1f);

        RectTransform tabTextRect = tabTextObj.GetComponent<RectTransform>();
        tabTextRect.anchorMin = Vector2.zero;
        tabTextRect.anchorMax = Vector2.one;
        tabTextRect.offsetMin = Vector2.zero;
        tabTextRect.offsetMax = Vector2.zero;

        // 5. Create Scroll View for Grid Layout Container
        GameObject scrollViewObj = new GameObject("InventoryScrollView");
        scrollViewObj.transform.SetParent(panelInventarioObjeto.transform, false);

        RectTransform scrollRectTrans = scrollViewObj.AddComponent<RectTransform>();
        scrollRectTrans.anchorMin = new Vector2(0.5f, 1f);
        scrollRectTrans.anchorMax = new Vector2(0.5f, 1f);
        scrollRectTrans.pivot = new Vector2(0.5f, 1f);
        scrollRectTrans.anchoredPosition = new Vector2(0, -135);
        scrollRectTrans.sizeDelta = new Vector2(440, 440);

        ScrollRect scrollRect = scrollViewObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 15f;

        // Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollViewObj.transform, false);

        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.pivot = new Vector2(0f, 1f);
        viewportRect.sizeDelta = Vector2.zero;

        Image viewportImage = viewportObj.AddComponent<Image>();
        viewportImage.color = new Color(0, 0, 0, 0); // Transparent viewport
        viewportObj.AddComponent<RectMask2D>(); // Mask items that scroll out

        // Content (Grid Layout Container)
        gridContainerObjeto = new GameObject("GridContainer");
        gridContainerObjeto.transform.SetParent(viewportObj.transform, false);

        RectTransform gridRect = gridContainerObjeto.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0f, 1f);
        gridRect.anchorMax = new Vector2(1f, 1f);
        gridRect.pivot = new Vector2(0.5f, 1f);
        gridRect.anchoredPosition = Vector2.zero;
        gridRect.sizeDelta = new Vector2(0, 440);

        GridLayoutGroup gridLayout = gridContainerObjeto.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(100, 75);
        gridLayout.spacing = new Vector2(10, 10);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 4;
        gridLayout.padding = new RectOffset(10, 10, 10, 10);

        // Content Size Fitter so the content container height matches preferred size
        ContentSizeFitter sizeFitter = gridContainerObjeto.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Link content to scroll rect
        scrollRect.content = gridRect;
        scrollRect.viewport = viewportRect;

        // Build 20 Slots (4x5) as initial minimum size
        for (int i = 0; i < 20; i++)
        {
            GameObject slot = CrearSlotUI(i);
            slotObjects.Add(slot);
        }

        // 6. Create Tooltip Info Panel
        tooltipObjeto = new GameObject("TooltipPanel");
        tooltipObjeto.transform.SetParent(panelInventarioObjeto.transform, false);

        RectTransform toolRect = tooltipObjeto.AddComponent<RectTransform>();
        toolRect.anchorMin = new Vector2(0.5f, 0f);
        toolRect.anchorMax = new Vector2(0.5f, 0f);
        toolRect.pivot = new Vector2(0.5f, 0f);
        toolRect.anchoredPosition = new Vector2(0, 105);
        toolRect.sizeDelta = new Vector2(440, 95);

        Image toolImg = tooltipObjeto.AddComponent<Image>();
        toolImg.color = new Color(0.06f, 0.06f, 0.06f, 0.98f);

        Outline toolOutline = tooltipObjeto.AddComponent<Outline>();
        toolOutline.effectColor = new Color(0.18f, 0.24f, 0.28f, 0.6f);
        toolOutline.effectDistance = new Vector2(1, 1);

        // Tooltip Name
        GameObject toolNameObj = new GameObject("TxtName");
        toolNameObj.transform.SetParent(tooltipObjeto.transform, false);
        txtTooltipName = toolNameObj.AddComponent<TextMeshProUGUI>();
        txtTooltipName.text = "SELECCIONA UN OBJETO";
        txtTooltipName.fontStyle = FontStyles.Bold;
        txtTooltipName.fontSize = 15;
        txtTooltipName.color = new Color(0.0f, 0.9f, 1.0f, 1f);

        RectTransform toolNameRect = toolNameObj.GetComponent<RectTransform>();
        toolNameRect.anchorMin = new Vector2(0f, 1f);
        toolNameRect.anchorMax = new Vector2(1f, 1f);
        toolNameRect.pivot = new Vector2(0.5f, 1f);
        toolNameRect.anchoredPosition = new Vector2(15, -12);
        toolNameRect.sizeDelta = new Vector2(-30, 22);

        // Tooltip Description
        GameObject toolDescObj = new GameObject("TxtDesc");
        toolDescObj.transform.SetParent(tooltipObjeto.transform, false);
        txtTooltipDesc = toolDescObj.AddComponent<TextMeshProUGUI>();
        txtTooltipDesc.text = "Haz clic en una ranura del inventario para ver los detalles.";
        txtTooltipDesc.fontSize = 12;
        txtTooltipDesc.color = new Color(0.8f, 0.8f, 0.78f, 1f);

        RectTransform toolDescRect = toolDescObj.GetComponent<RectTransform>();
        toolDescRect.anchorMin = Vector2.zero;
        toolDescRect.anchorMax = Vector2.one;
        toolDescRect.offsetMin = new Vector2(15, 10);
        toolDescRect.offsetMax = new Vector2(-15, -35);

        // 7. Create Footer Elements (GRILLA DE INVENTARIO removed as per user request)

        // Apply custom fonts to Main Panel
        ApplyFont(txtCapacidad, true);
        ApplyFont(txtTotalWeight, true);
        ApplyFont(tabText, true);
        ApplyFont(txtTooltipName, true);
        ApplyFont(txtTooltipDesc, true);

        // Keep Button (A) - Removed as per simplified inventory UI requirements
        // GameObject btnKeepObj = CrearBotonAccion("BtnKeep", "MANTENER", new Color(0.2f, 0.45f, 0.2f, 1f), -165);
        // btnKeepObj.GetComponent<Button>().onClick.AddListener(CloseInventario);

        // Discard Button (X) - Removed as per simplified inventory UI requirements
        // GameObject btnDiscardObj = CrearBotonAccion("BtnDiscard", "DESCARTAR", new Color(0.18f, 0.24f, 0.35f, 1f), -55);
        // btnDiscardObj.GetComponent<Button>().onClick.AddListener(DiscardSelectedItem);

        // Reorganize Button (Y) - Removed as per simplified inventory UI requirements
        // GameObject btnReorgObj = CrearBotonAccion("BtnReorganize", "ORDENAR", new Color(0.45f, 0.4f, 0.15f, 1f), 55);
        // btnReorgObj.GetComponent<Button>().onClick.AddListener(ReorganizeInventory);

        // Lock Button (Bloquear) - Only available action button in normal gameplay
        btnLockObj = CrearBotonAccion("BtnLock", "Bloquear objeto", new Color(0.55f, 0.15f, 0.15f, 1f), 0f);
        btnLockObj.GetComponent<Button>().onClick.AddListener(ToggleLockSelectedItem);

        // Adjust lock button width to fit long text
        RectTransform btnLockRect = btnLockObj.GetComponent<RectTransform>();
        if (btnLockRect != null)
        {
            btnLockRect.sizeDelta = new Vector2(180f, 36f);
        }

        // Find the label text inside the Lock Button
        Transform lockLabelTrans = btnLockObj.transform.Find("Text");
        txtBtnLockLabel = lockLabelTrans != null ? lockLabelTrans.GetComponent<TextMeshProUGUI>() : null;

        // Offer Button (Ofrecer) - visible/active only during trade dialogues
        btnOfrecerObj = CrearBotonAccion("BtnOfrecer", "Ofrecer", new Color(0.2f, 0.45f, 0.2f, 1f), 0f);
        btnOfrecerObj.GetComponent<Button>().onClick.AddListener(OfrecerSelectedItem);
        btnOfrecerObj.SetActive(false); // Hidden by default

        RectTransform btnOfrecerRect = btnOfrecerObj.GetComponent<RectTransform>();
        if (btnOfrecerRect != null)
        {
            btnOfrecerRect.sizeDelta = new Vector2(180f, 36f);
        }

        // 8. Create Fuel Bar (barradenafta) at bottom-right of the Screen
        barraNaftaObjeto = new GameObject("FuelBarContainer");
        RectTransform containerRect = barraNaftaObjeto.AddComponent<RectTransform>();
        barraNaftaObjeto.transform.SetParent(canvasObjeto.transform, false);
        containerRect.localScale = Vector3.one;
        containerRect.localPosition = Vector3.zero;

        containerRect.anchorMin = new Vector2(1f, 0f); // Bottom-right anchor
        containerRect.anchorMax = new Vector2(1f, 0f);
        containerRect.pivot = new Vector2(1f, 0f); // Bottom-right pivot
        containerRect.anchoredPosition = new Vector2(-50f, 50f); // 50px offset from margins

        Sprite fuelSprite = CargarSpriteDesdeResources("Sprites/barradenafta");
        float targetWidth = 350f;
        float targetHeight = 122.6f;
        if (fuelSprite != null)
        {
            float originalWidth = fuelSprite.rect.width;
            float originalHeight = fuelSprite.rect.height;
            float aspect = originalWidth / (originalHeight > 0f ? originalHeight : 1f);
            targetHeight = targetWidth / (aspect > 0f ? aspect : 1f);
        }
        containerRect.sizeDelta = new Vector2(targetWidth, targetHeight);

        // A. Create Cyan Liquid Bar (Child index 0, renders behind)
        GameObject liquidObj = new GameObject("FuelLiquid");
        liquidBarRect = liquidObj.AddComponent<RectTransform>();
        liquidObj.transform.SetParent(barraNaftaObjeto.transform, false);
        liquidBarRect.localScale = Vector3.one;
        liquidBarRect.localPosition = Vector3.zero;

        liquidBarRect.anchorMin = new Vector2(0f, 0f);
        liquidBarRect.anchorMax = new Vector2(0f, 0f);
        liquidBarRect.pivot = new Vector2(0f, 0.5f); // Left-center pivot for easy horizontal scaling

        maxFuelWidth = 242f;
        float liquidPosX = 82f;
        float liquidPosY = 33f;
        float liquidHeight = 48f;

        liquidBarRect.anchoredPosition = new Vector2(liquidPosX, liquidPosY);
        liquidBarRect.sizeDelta = new Vector2(maxFuelWidth * currentFuel, liquidHeight);

        Image liquidImg = liquidObj.AddComponent<Image>();
        liquidImg.color = new Color(0f, 0.72f, 1f, 1f); // Celeste / Cyan color
        liquidImg.type = Image.Type.Simple;

        // B. Create Frame Image (Child index 1, renders in front)
        GameObject frameObj = new GameObject("FuelFrame");
        RectTransform frameRect = frameObj.AddComponent<RectTransform>();
        frameObj.transform.SetParent(barraNaftaObjeto.transform, false);
        frameRect.localScale = Vector3.one;
        frameRect.localPosition = Vector3.zero;

        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;

        Image frameImg = frameObj.AddComponent<Image>();
        if (fuelSprite != null)
        {
            frameImg.sprite = fuelSprite;
            frameImg.type = Image.Type.Simple;
            frameImg.preserveAspect = true;
        }
        else
        {
            frameImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            Debug.LogWarning("barradenafta sprite could not be loaded from Resources/Sprites/barradenafta");
        }



        // 9. Create Game Over Panel
        panelGameOverObjeto = new GameObject("GameOverPanel");
        panelGameOverObjeto.transform.SetParent(canvasObjeto.transform, false);

        RectTransform goRect = panelGameOverObjeto.AddComponent<RectTransform>();
        goRect.anchorMin = Vector2.zero;
        goRect.anchorMax = Vector2.one;
        goRect.offsetMin = Vector2.zero;
        goRect.offsetMax = Vector2.zero;

        Image goBg = panelGameOverObjeto.AddComponent<Image>();
        goBg.color = new Color(0.08f, 0.08f, 0.08f, 0.96f); // Semi-transparent dark overlay

        // Danger Red strip at top of Game Over panel
        GameObject goStrip = new GameObject("Strip");
        goStrip.transform.SetParent(panelGameOverObjeto.transform, false);
        Image stripImg = goStrip.AddComponent<Image>();
        stripImg.color = new Color(0.85f, 0.15f, 0.15f, 0.8f); // Red strip
        RectTransform stripRect = goStrip.GetComponent<RectTransform>();
        stripRect.anchorMin = new Vector2(0f, 0.7f);
        stripRect.anchorMax = new Vector2(1f, 0.72f);
        stripRect.offsetMin = Vector2.zero;
        stripRect.offsetMax = Vector2.zero;

        // Title Text: GAME OVER
        GameObject titleObj = new GameObject("TxtTitle");
        titleObj.transform.SetParent(panelGameOverObjeto.transform, false);
        TextMeshProUGUI txtTitle = titleObj.AddComponent<TextMeshProUGUI>();
        txtTitle.text = "FIN DEL CAMINO"; // GAME OVER
        txtTitle.alignment = TextAlignmentOptions.Center;
        txtTitle.fontSize = 72;
        txtTitle.fontStyle = FontStyles.Bold;
        txtTitle.color = new Color(0.9f, 0.2f, 0.2f, 1f); // Danger Red
        ApplyFont(txtTitle, true);

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.55f);
        titleRect.anchorMax = new Vector2(1f, 0.65f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        // Subtitle Text: Te quedaste sin nafta
        GameObject subObj = new GameObject("TxtSubtitle");
        subObj.transform.SetParent(panelGameOverObjeto.transform, false);
        TextMeshProUGUI txtSub = subObj.AddComponent<TextMeshProUGUI>();
        txtSub.text = "Te quedaste sin nafta en medio del páramo.";
        txtSub.alignment = TextAlignmentOptions.Center;
        txtSub.fontSize = 28;
        txtSub.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        ApplyFont(txtSub, true);

        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0.45f);
        subRect.anchorMax = new Vector2(1f, 0.52f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;

        // Button container
        GameObject goButtons = new GameObject("ButtonsContainer");
        goButtons.transform.SetParent(panelGameOverObjeto.transform, false);
        RectTransform goButtonsRect = goButtons.AddComponent<RectTransform>();
        goButtonsRect.anchorMin = new Vector2(0.5f, 0.3f);
        goButtonsRect.anchorMax = new Vector2(0.5f, 0.3f);
        goButtonsRect.sizeDelta = new Vector2(500, 150);
        goButtonsRect.anchoredPosition = Vector2.zero;

        // Retry Button
        GameObject btnRetryObj = CrearBotonAccionGameOver("BtnRetry", "REINTENTAR", new Color(0.2f, 0.45f, 0.2f, 1f), goButtons.transform, -120);
        btnRetryObj.GetComponent<Button>().onClick.AddListener(RestartGameLevel);

        // Menu Button
        GameObject btnMenuObj = CrearBotonAccionGameOver("BtnMenu", "MENU PRINCIPAL", new Color(0.45f, 0.4f, 0.15f, 1f), goButtons.transform, 120);
        btnMenuObj.GetComponent<Button>().onClick.AddListener(GoToMainMenu);

        // 10. Create Gear Configuration Button
        btnConfiguracionObjeto = new GameObject("BtnConfiguracion");
        btnConfiguracionObjeto.transform.SetParent(canvasObjeto.transform, false);

        RectTransform configBtnRect = btnConfiguracionObjeto.AddComponent<RectTransform>();
        configBtnRect.anchorMin = new Vector2(1f, 1f);
        configBtnRect.anchorMax = new Vector2(1f, 1f);
        configBtnRect.pivot = new Vector2(1f, 1f);
        configBtnRect.anchoredPosition = new Vector2(-50, -45);
        configBtnRect.sizeDelta = new Vector2(70, 70);

        Image configBtnImg = btnConfiguracionObjeto.AddComponent<Image>();
        Sprite configSprite = CargarSpriteDesdeResources("Sprites/tuercaconfiguracion");
        if (configSprite != null)
        {
            configBtnImg.sprite = configSprite;
            configBtnImg.type = Image.Type.Simple;
            configBtnImg.preserveAspect = true;
        }
        else
        {
            configBtnImg.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);
            Debug.LogWarning("tuercaconfiguracion sprite could not be loaded from Resources/Sprites/tuercaconfiguracion");
        }

        Button configBtn = btnConfiguracionObjeto.AddComponent<Button>();
        configBtn.onClick.AddListener(AbrirMenuPausa);

        // 11. Create Pause/Configuration Panel
        panelConfiguracionObjeto = new GameObject("PausePanel");
        panelConfiguracionObjeto.transform.SetParent(canvasObjeto.transform, false);

        RectTransform pauseRect = panelConfiguracionObjeto.AddComponent<RectTransform>();
        pauseRect.anchorMin = Vector2.zero;
        pauseRect.anchorMax = Vector2.one;
        pauseRect.offsetMin = Vector2.zero;
        pauseRect.offsetMax = Vector2.zero;

        Image pauseBg = panelConfiguracionObjeto.AddComponent<Image>();
        pauseBg.color = new Color(0f, 0f, 0f, 0.6f); // low-opacity dark overlay

        // Pause Menu Container (for background styling)
        GameObject pauseMenuContainer = new GameObject("PauseMenuContainer");
        pauseMenuContainer.transform.SetParent(panelConfiguracionObjeto.transform, false);

        RectTransform pauseContainerRect = pauseMenuContainer.AddComponent<RectTransform>();
        pauseContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
        pauseContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
        pauseContainerRect.pivot = new Vector2(0.5f, 0.5f);
        pauseContainerRect.sizeDelta = new Vector2(400, 300);

        Image pauseContainerBg = pauseMenuContainer.AddComponent<Image>();
        pauseContainerBg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f); // scrap iron dark background

        Outline pauseContainerOutline = pauseMenuContainer.AddComponent<Outline>();
        pauseContainerOutline.effectColor = new Color(0.35f, 0.35f, 0.35f, 0.6f);
        pauseContainerOutline.effectDistance = new Vector2(2f, 2f);

        // Title Text: PAUSA
        GameObject pauseTitleObj = new GameObject("TxtPauseTitle");
        pauseTitleObj.transform.SetParent(pauseMenuContainer.transform, false);
        TextMeshProUGUI txtPauseTitle = pauseTitleObj.AddComponent<TextMeshProUGUI>();
        txtPauseTitle.text = "JUEGO EN PAUSA";
        txtPauseTitle.fontStyle = FontStyles.Bold;
        txtPauseTitle.fontSize = 24;
        txtPauseTitle.alignment = TextAlignmentOptions.Center;
        txtPauseTitle.color = new Color(0.9f, 0.85f, 0.8f, 1f);
        ApplyFont(txtPauseTitle, true);

        RectTransform pauseTitleRect = pauseTitleObj.GetComponent<RectTransform>();
        pauseTitleRect.anchorMin = new Vector2(0f, 1f);
        pauseTitleRect.anchorMax = new Vector2(1f, 1f);
        pauseTitleRect.pivot = new Vector2(0.5f, 1f);
        pauseTitleRect.anchoredPosition = new Vector2(0, -30);
        pauseTitleRect.sizeDelta = new Vector2(-40, 40);

        // Action Buttons container
        GameObject pauseButtons = new GameObject("PauseButtons");
        pauseButtons.transform.SetParent(pauseMenuContainer.transform, false);

        RectTransform pauseButtonsRect = pauseButtons.AddComponent<RectTransform>();
        pauseButtonsRect.anchorMin = Vector2.zero;
        pauseButtonsRect.anchorMax = Vector2.one;
        pauseButtonsRect.offsetMin = new Vector2(20, 20);
        pauseButtonsRect.offsetMax = new Vector2(-20, -100);

        // Resume/Close button (small 'X' button in top-right of pause menu container)
        GameObject btnResumeObj = new GameObject("BtnResume");
        btnResumeObj.transform.SetParent(pauseMenuContainer.transform, false);

        RectTransform resumeBtnRect = btnResumeObj.AddComponent<RectTransform>();
        resumeBtnRect.anchorMin = new Vector2(1f, 1f);
        resumeBtnRect.anchorMax = new Vector2(1f, 1f);
        resumeBtnRect.pivot = new Vector2(1f, 1f);
        resumeBtnRect.anchoredPosition = new Vector2(-10, -10);
        resumeBtnRect.sizeDelta = new Vector2(30, 30);

        Image resumeBtnImg = btnResumeObj.AddComponent<Image>();
        resumeBtnImg.color = new Color(0.35f, 0.35f, 0.35f, 0.8f);

        Button resumeBtn = btnResumeObj.AddComponent<Button>();
        resumeBtn.targetGraphic = resumeBtnImg; // Explicitly set target graphic
        resumeBtn.onClick.AddListener(CerrarMenuPausa);

        GameObject resumeTxtObj = new GameObject("Text");
        resumeTxtObj.transform.SetParent(btnResumeObj.transform, false);
        TextMeshProUGUI txtResume = resumeTxtObj.AddComponent<TextMeshProUGUI>();
        txtResume.text = "X";
        txtResume.alignment = TextAlignmentOptions.Center;
        txtResume.fontSize = 16;
        txtResume.fontStyle = FontStyles.Bold;
        txtResume.color = Color.white;
        txtResume.raycastTarget = false; // Disable raycast target on text to prevent click interception bugs
        ApplyFont(txtResume, true);

        RectTransform resumeTxtRect = resumeTxtObj.GetComponent<RectTransform>();
        resumeTxtRect.anchorMin = Vector2.zero;
        resumeTxtRect.anchorMax = Vector2.one;
        resumeTxtRect.offsetMin = Vector2.zero;
        resumeTxtRect.offsetMax = Vector2.zero;

        // Button: REINICIAR
        GameObject btnRestartObj = CrearBotonAccionPause("BtnRestartLevel", "REINICIAR", new Color(0.2f, 0.45f, 0.2f, 1f), pauseButtons.transform, 40);
        btnRestartObj.GetComponent<Button>().onClick.AddListener(RestartGameLevel);

        // Button: VOLVER AL MENU
        GameObject btnMenuPrincipalObj = CrearBotonAccionPause("BtnGoToMenu", "VOLVER AL MENU", new Color(0.45f, 0.4f, 0.15f, 1f), pauseButtons.transform, -40);
        btnMenuPrincipalObj.GetComponent<Button>().onClick.AddListener(GoToMainMenu);

        // Start closed
        panelInventarioObjeto.SetActive(false);
        panelGameOverObjeto.SetActive(false);
        panelConfiguracionObjeto.SetActive(false);
    }

    private GameObject CrearSlotUI(int index)
    {
        GameObject slot = new GameObject("Slot_" + index);
        slot.transform.SetParent(gridContainerObjeto.transform, false);

        Image slotImg = slot.AddComponent<Image>();
        slotImg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        Outline slotOutline = slot.AddComponent<Outline>();
        slotOutline.effectColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
        slotOutline.effectDistance = new Vector2(1.5f, 1.5f);

        Button btn = slot.AddComponent<Button>();
        btn.onClick.AddListener(() => SelectSlot(index));

        // Slot hover colors
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        cb.highlightedColor = new Color(0.22f, 0.22f, 0.22f, 0.95f);
        cb.pressedColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        btn.colors = cb;

        // Custom code text (inside slot)
        GameObject codeObj = new GameObject("TxtCode");
        codeObj.transform.SetParent(slot.transform, false);
        TextMeshProUGUI txtCode = codeObj.AddComponent<TextMeshProUGUI>();
        txtCode.text = "";
        txtCode.fontStyle = FontStyles.Bold;
        txtCode.alignment = TextAlignmentOptions.Center;
        txtCode.fontSize = 13;
        txtCode.color = new Color(0.85f, 0.82f, 0.8f, 1f);

        RectTransform codeRect = codeObj.GetComponent<RectTransform>();
        codeRect.anchorMin = Vector2.zero;
        codeRect.anchorMax = Vector2.one;
        codeRect.offsetMin = new Vector2(5, 18);
        codeRect.offsetMax = new Vector2(-5, -18);

        // Custom icon image
        GameObject iconObj = new GameObject("ImgIcon");
        iconObj.transform.SetParent(slot.transform, false);
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.color = Color.white;
        iconImg.preserveAspect = true;
        iconImg.gameObject.SetActive(false);

        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(12, 18);
        iconRect.offsetMax = new Vector2(-12, -18);

        // Weight text label (bottom right)
        GameObject weightObj = new GameObject("TxtWeight");
        weightObj.transform.SetParent(slot.transform, false);
        TextMeshProUGUI txtWeight = weightObj.AddComponent<TextMeshProUGUI>();
        txtWeight.text = "";
        txtWeight.alignment = TextAlignmentOptions.BottomRight;
        txtWeight.fontSize = 10;
        txtWeight.color = new Color(0.65f, 0.65f, 0.63f, 1f);

        RectTransform weightRect = weightObj.GetComponent<RectTransform>();
        weightRect.anchorMin = Vector2.zero;
        weightRect.anchorMax = Vector2.one;
        weightRect.offsetMin = new Vector2(2, 2);
        weightRect.offsetMax = new Vector2(-5, -2);

        // Quantity indicator label (top right)
        GameObject quantityObj = new GameObject("TxtQuantity");
        quantityObj.transform.SetParent(slot.transform, false);
        TextMeshProUGUI txtQuantity = quantityObj.AddComponent<TextMeshProUGUI>();
        txtQuantity.text = "";
        txtQuantity.fontStyle = FontStyles.Bold;
        txtQuantity.alignment = TextAlignmentOptions.TopRight;
        txtQuantity.fontSize = 13;
        txtQuantity.color = new Color(0.9f, 0.7f, 0.2f, 0.9f);

        RectTransform quantityRect = quantityObj.GetComponent<RectTransform>();
        quantityRect.anchorMin = Vector2.zero;
        quantityRect.anchorMax = Vector2.one;
        quantityRect.offsetMin = new Vector2(2, 2);
        quantityRect.offsetMax = new Vector2(-6, -3);

        ApplyFont(txtQuantity, true);

        // Lock Indicator label (top left / bottom left)
        GameObject lockObj = new GameObject("TxtLockIndicator");
        lockObj.transform.SetParent(slot.transform, false);
        TextMeshProUGUI txtLock = lockObj.AddComponent<TextMeshProUGUI>();
        txtLock.text = "";
        txtLock.fontStyle = FontStyles.Bold;
        txtLock.alignment = TextAlignmentOptions.TopLeft;
        txtLock.fontSize = 11;
        txtLock.color = new Color(0.9f, 0.2f, 0.2f, 1f); // Red indicator
        ApplyFont(txtLock, true);

        RectTransform lockRect = lockObj.GetComponent<RectTransform>();
        lockRect.anchorMin = Vector2.zero;
        lockRect.anchorMax = Vector2.one;
        lockRect.offsetMin = new Vector2(5, 2);
        lockRect.offsetMax = new Vector2(-5, -2);

        return slot;
    }

    private GameObject CrearBotonAccion(string name, string text, Color cbNormal, float xOffset)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(panelInventarioObjeto.transform, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(xOffset, 20);
        rect.sizeDelta = new Vector2(105, 36);

        Image img = buttonObj.AddComponent<Image>();
        img.color = cbNormal;

        Outline outline = buttonObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.08f, 0.08f, 0.08f, 0.8f);
        outline.effectDistance = new Vector2(1, 1);

        Button btn = buttonObj.AddComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.normalColor = cbNormal;
        cb.highlightedColor = cbNormal + new Color(0.1f, 0.1f, 0.1f, 0f);
        cb.pressedColor = cbNormal - new Color(0.08f, 0.08f, 0.08f, 0f);
        btn.colors = cb;

        GameObject labelObj = new GameObject("Text");
        labelObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 13;
        label.color = new Color(0.95f, 0.95f, 0.9f, 1f);
        ApplyFont(label, true);

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return buttonObj;
    }

    private int GetTradeNPCIndex(string npc)
    {
        if (string.IsNullOrEmpty(npc)) return -1;
        string lower = npc.ToLower();
        if (lower == "npc2" || lower.Contains("vagabundo")) return 2;
        if (lower == "npc6" || lower.Contains("jetsar")) return 6;
        if (lower == "npc8" || lower.Contains("li")) return 8;
        if (lower == "npc12" || lower.Contains("tomás") || lower.Contains("tomas")) return 12;
        if (lower == "npc14" || lower.Contains("ezequiel")) return 14;
        return -1;
    }

    private float GetPatienceDurationForNPC(int npcIdx)
    {
        switch (npcIdx)
        {
            case 2: return 35f;
            case 6: return 31f;
            case 8: return 27f;
            case 12: return 23f;
            case 14: return 19f;
            default: return 35f;
        }
    }

    private string GetNPCDisplayName(int npcIdx)
    {
        switch (npcIdx)
        {
            case 2: return "BETO";
            case 6: return "JETSAR";
            case 8: return "LI";
            case 12: return "TOMÁS";
            case 14: return "EZEQUIEL";
            default: return "NPC";
        }
    }

    public void OpenInventario()
    {
        if (panelInventarioObjeto != null)
        {
            panelInventarioObjeto.SetActive(true);
            selectedIndex = -1; // Reset selection on open
            
            int npcIndex = NPCInteraction.lastInteractedNPC != null ? GetTradeNPCIndex(NPCInteraction.lastInteractedNPC) : -1;
            bool esMysteryNPC = npcIndex != -1 && DialogoManager.Instance != null && DialogoManager.Instance.dialogoAbierto;

            if (esMysteryNPC)
            {
                CrearBarraPaciencia();
                if (patienceBarContainer != null) patienceBarContainer.SetActive(true);
                
                if (txtPatienceBarLabel != null)
                {
                    txtPatienceBarLabel.text = $"PACIENCIA DE {GetNPCDisplayName(npcIndex)}";
                }
                
                patienceTimerDuration = GetPatienceDurationForNPC(npcIndex);
                currentPatience = 1.0f;
                isPatienceBarActive = true;
                isRevisandoRastrojero = false;

                // Show top warning controls
                if (btnRevisarRastrojero != null) btnRevisarRastrojero.SetActive(true);
                if (txtExplicacionRastrojero != null) txtExplicacionRastrojero.SetActive(true);
            }
            else
            {
                if (patienceBarContainer != null) patienceBarContainer.SetActive(false);
                isPatienceBarActive = false;
                isRevisandoRastrojero = false;

                if (btnRevisarRastrojero != null) btnRevisarRastrojero.SetActive(false);
                if (txtExplicacionRastrojero != null) txtExplicacionRastrojero.SetActive(false);
            }
            
            UpdateInventoryUI();
        }
    }

    public void CloseInventario()
    {
        isPatienceBarActive = false;
        isRevisandoRastrojero = false;
        if (panelInventarioObjeto != null)
        {
            panelInventarioObjeto.SetActive(false);
            UpdateInventoryUI();
        }

        if (DialogoManager.Instance != null && DialogoManager.Instance.IsOfferingState())
        {
            DialogoManager.Instance.RegresarAInicioDialogo();
        }
    }

    private void CrearBarraPaciencia()
    {
        if (patienceBarContainer != null) return; // already created

        patienceBarContainer = new GameObject("PatienceBarContainer");
        patienceBarContainer.transform.SetParent(panelInventarioObjeto.transform, false);

        RectTransform cRect = patienceBarContainer.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0.5f, 0f);
        cRect.anchorMax = new Vector2(0.5f, 0f);
        cRect.pivot = new Vector2(0.5f, 0f);
        cRect.anchoredPosition = new Vector2(0f, 212f);
        cRect.sizeDelta = new Vector2(440f, 32f);

        Image cImg = patienceBarContainer.AddComponent<Image>();
        cImg.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);

        Outline cOutline = patienceBarContainer.AddComponent<Outline>();
        cOutline.effectColor = new Color(0.35f, 0.35f, 0.35f, 0.6f);
        cOutline.effectDistance = new Vector2(1.5f, 1.5f);

        // Fill/Liquid Bar
        GameObject liquidObj = new GameObject("LiquidBar");
        liquidObj.transform.SetParent(patienceBarContainer.transform, false);

        RectTransform lRectTrans = liquidObj.AddComponent<RectTransform>();
        lRectTrans.anchorMin = new Vector2(0f, 0f);
        lRectTrans.anchorMax = new Vector2(0f, 1f); // Anchored to left edge, stretching vertically
        lRectTrans.pivot = new Vector2(0f, 0.5f);
        lRectTrans.anchoredPosition = new Vector2(3f, 0f); // X offset 3f
        lRectTrans.sizeDelta = new Vector2(434f, -6f); // Stretching with -6 offset

        patienceLiquidImg = liquidObj.AddComponent<Image>();
        patienceLiquidImg.color = new Color(0.2f, 0.75f, 0.3f, 0.9f); // Green patience bar

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(patienceBarContainer.transform, false);

        txtPatienceBarLabel = labelObj.AddComponent<TextMeshProUGUI>();
        txtPatienceBarLabel.text = "PACIENCIA DE BETO";
        txtPatienceBarLabel.alignment = TextAlignmentOptions.Center;
        txtPatienceBarLabel.fontSize = 15; // Increased to 15!
        txtPatienceBarLabel.fontStyle = FontStyles.Bold;
        txtPatienceBarLabel.color = Color.white;
        ApplyFont(txtPatienceBarLabel, true);

        RectTransform lRect = labelObj.GetComponent<RectTransform>();
        lRect.anchorMin = Vector2.zero;
        lRect.anchorMax = Vector2.one;
        lRect.offsetMin = Vector2.zero;
        lRect.offsetMax = Vector2.zero;
    }

    private void ToggleInventario()
    {
        if (panelInventarioObjeto != null)
        {
            if (panelInventarioObjeto.activeSelf)
                CloseInventario();
            else
                OpenInventario();
        }
    }

    private void OfrecerSelectedItem()
    {
        if (selectedIndex >= 0 && selectedIndex < items.Count)
        {
            Item selectedItem = items[selectedIndex];
            if (DialogoManager.Instance != null && DialogoManager.Instance.IsOfferingState())
            {
                DialogoManager.Instance.SelectAndOfferItem(selectedItem.name);
            }
        }
    }

    private void DiscardSelectedItem()
    {
        if (selectedIndex >= 0 && selectedIndex < items.Count)
        {
            if (items[selectedIndex].isLocked)
            {
                Debug.LogWarning("No se puede descartar un objeto bloqueado.");
                return;
            }
            items.RemoveAt(selectedIndex);
            selectedIndex = -1;
            UpdateInventoryUI();
        }
    }

    private void ReorganizeInventory()
    {
        items.Sort((x, y) => y.weight.CompareTo(x.weight));
        selectedIndex = -1;
        UpdateInventoryUI();
    }

    private void SelectSlot(int index)
    {
        if (index < items.Count)
        {
            selectedIndex = index;
        }
        else
        {
            selectedIndex = -1;
        }
        UpdateInventoryUI();
    }

    private void UpdateInventoryUI()
    {
        // Check if currently trading with one of the 5 mystery NPCs (Beto, Jetsar, Li, Tomás, Ezequiel)
        int npcIndex = NPCInteraction.lastInteractedNPC != null ? GetTradeNPCIndex(NPCInteraction.lastInteractedNPC) : -1;
        bool esNPC2 = npcIndex != -1 && DialogoManager.Instance != null && DialogoManager.Instance.dialogoAbierto;

        // Initialize silhouette material if needed
        if (esNPC2 && silhouetteMaterial == null)
        {
            Shader silShader = Resources.Load<Shader>("Shaders/UISilhouette");
            if (silShader == null)
            {
                silShader = Shader.Find("UI/Silhouette");
            }

            if (silShader != null)
            {
                silhouetteMaterial = new Material(silShader);
            }
        }

        // 1. Calculate total weight
        float totalWeight = 0;
        foreach (var item in items)
        {
            totalWeight += item.weight;
        }
        
        // Update both HUD and Main Panel capacity texts to reflect slots instead of weight
        txtHudCapacidad.text = $"INVENTARIO [{items.Count}]";
        txtCapacidad.text = esNPC2 ? "" : $"<color=#FF8C00>CAPACIDAD:</color> RANURAS DE INVENTARIO";
        txtTotalWeight.text = esNPC2 ? "" : $"CANTIDAD DE OBJETOS: {items.Count}";

        // Update HUD hint text based on whether the main inventory panel is open
        if (txtHudHint != null && panelInventarioObjeto != null)
        {
            txtHudHint.text = panelInventarioObjeto.activeSelf ? "[Y] CERRAR INVENTARIO" : "[Y] ABRIR INVENTARIO";
        }

        // 2. Update HUD Horizontal Slots (Slots 0 to 3 for items, Slot 4 is the arrow)
        for (int i = 0; i < 4; i++)
        {
            GameObject hudSlot = hudSlots[i];
            
            Transform iconTrans = hudSlot.transform.Find("Icon");
            Image iconImg = iconTrans != null ? iconTrans.GetComponent<Image>() : null;

            Transform codeTrans = hudSlot.transform.Find("TxtCode");
            TextMeshProUGUI txtCode = codeTrans != null ? codeTrans.GetComponent<TextMeshProUGUI>() : null;

            if (i < items.Count)
            {
                Item item = items[i];
                if (item.customIcon != null)
                {
                    if (txtCode != null) txtCode.text = esNPC2 ? "" : item.code;
                    if (iconImg != null)
                    {
                        iconImg.sprite = item.customIcon;
                        // Draw silhouette when esNPC2 is true AND we are not peeking; normal full-color when peeking
                        bool mostrarSilueta = esNPC2 && !isRevisandoRastrojero;
                        iconImg.material = mostrarSilueta ? silhouetteMaterial : null;
                        iconImg.color = mostrarSilueta ? (silhouetteMaterial != null ? Color.white : Color.black) : Color.white;
                        iconImg.gameObject.SetActive(true); // Always visible!
                    }
                }
                else
                {
                    if (iconImg != null) iconImg.gameObject.SetActive(false);
                    if (txtCode != null) txtCode.text = esNPC2 ? "" : item.code;
                }
            }
            else
            {
                // Slot is empty
                if (iconImg != null) iconImg.gameObject.SetActive(false);
                if (txtCode != null) txtCode.text = "";
            }
        }

        // Dynamic slot expansion
        while (slotObjects.Count < items.Count)
        {
            GameObject slot = CrearSlotUI(slotObjects.Count);
            slotObjects.Add(slot);
        }

        // 3. Update main panel Grid Slots
        for (int i = 0; i < slotObjects.Count; i++)
        {
            GameObject slot = slotObjects[i];
            Outline outline = slot.GetComponent<Outline>();
            
            Transform txtCodeTrans = slot.transform.Find("TxtCode");
            TextMeshProUGUI txtCode = txtCodeTrans != null ? txtCodeTrans.GetComponent<TextMeshProUGUI>() : null;
            
            Transform iconTrans = slot.transform.Find("ImgIcon");
            Image iconImg = iconTrans != null ? iconTrans.GetComponent<Image>() : null;
            
            Transform txtWeightTrans = slot.transform.Find("TxtWeight");
            TextMeshProUGUI txtWeight = txtWeightTrans != null ? txtWeightTrans.GetComponent<TextMeshProUGUI>() : null;
            
            Transform txtQuantityTrans = slot.transform.Find("TxtQuantity");
            TextMeshProUGUI txtQuantity = txtQuantityTrans != null ? txtQuantityTrans.GetComponent<TextMeshProUGUI>() : null;

            Transform lockIndicatorTrans = slot.transform.Find("TxtLockIndicator");
            TextMeshProUGUI txtLockIndicator = lockIndicatorTrans != null ? lockIndicatorTrans.GetComponent<TextMeshProUGUI>() : null;

            if (i < items.Count)
            {
                Item item = items[i];

                if (item.customIcon != null)
                {
                    if (txtCode != null) txtCode.gameObject.SetActive(false);
                    if (iconImg != null)
                    {
                        iconImg.sprite = item.customIcon;
                        // Draw silhouette when esNPC2 is true AND we are not peeking; normal full-color when peeking
                        bool mostrarSilueta = esNPC2 && !isRevisandoRastrojero;
                        iconImg.material = mostrarSilueta ? silhouetteMaterial : null;
                        iconImg.color = mostrarSilueta ? (silhouetteMaterial != null ? Color.white : Color.black) : Color.white;
                        iconImg.gameObject.SetActive(true); // Always visible!
                    }
                }
                else
                {
                    if (iconImg != null) iconImg.gameObject.SetActive(false);
                    if (txtCode != null)
                    {
                        txtCode.text = esNPC2 ? "" : item.code;
                        txtCode.gameObject.SetActive(!esNPC2);
                    }
                }

                // Hide weight info if trading with Beto (NPC2) and not peeking
                if (txtWeight != null) txtWeight.text = (esNPC2 && !isRevisandoRastrojero) ? "" : $"{item.weight:F1}kg";
                
                // Hide quantity if trading with Beto and not peeking
                if (txtQuantity != null) txtQuantity.text = (esNPC2 && !isRevisandoRastrojero) ? "" : item.quantity.ToString();
                
                if (txtLockIndicator != null) txtLockIndicator.text = item.isLocked ? "BLOQ" : "";
            }
            else
            {
                if (txtCode != null) txtCode.text = "";
                if (iconImg != null) iconImg.gameObject.SetActive(false);
                if (txtWeight != null) txtWeight.text = "";
                if (txtQuantity != null) txtQuantity.text = "";
                if (txtLockIndicator != null) txtLockIndicator.text = "";
            }

            if (i == selectedIndex && i < items.Count)
            {
                if (outline != null)
                {
                    outline.effectColor = new Color(0.0f, 0.9f, 1.0f, 0.95f);
                    outline.effectDistance = new Vector2(2f, 2f);
                }
            }
            else
            {
                if (outline != null)
                {
                    if (i < items.Count && items[i].isLocked)
                    {
                        outline.effectColor = new Color(0.9f, 0.2f, 0.2f, 0.8f); // Red outline for locked
                        outline.effectDistance = new Vector2(2f, 2f);
                    }
                    else
                    {
                        outline.effectColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
                        outline.effectDistance = new Vector2(1.5f, 1.5f);
                    }
                }
            }
        }

        // 4. Update Tooltip Info Panel
        if (selectedIndex >= 0 && selectedIndex < items.Count)
        {
            Item item = items[selectedIndex];
            
            if (esNPC2 && !isRevisandoRastrojero)
            {
                txtTooltipName.text = ""; // Hide name completely
                txtTooltipDesc.fontSize = 17f; // Larger size for Beto's warning text
                txtTooltipDesc.text = "No es buena idea mostrar todo mi inventario. Tendré que elegir el objeto de memoria.";
            }
            else
            {
                txtTooltipName.text = item.name.ToUpper();
                txtTooltipDesc.fontSize = 12f; // Restore original size
                txtTooltipDesc.text = $"{item.description}\nPeso: {item.weight:F1} kg | Cantidad: {item.quantity}";
            }
            
            if (txtBtnLockLabel != null)
            {
                txtBtnLockLabel.text = item.isLocked ? "Desbloquear objeto" : "Bloquear objeto";
            }
        }
        else
        {
            if (esNPC2 && !isRevisandoRastrojero)
            {
                txtTooltipName.text = "ATENCIÓN";
                txtTooltipDesc.fontSize = 17f; // Larger size for Beto's warning text
                txtTooltipDesc.text = "No es buena idea mostrar todo mi inventario. Tendré que elegir el objeto de memoria.";
            }
            else
            {
                txtTooltipName.text = "SELECCIONA UN OBJETO";
                txtTooltipDesc.fontSize = 12f; // Restore original size
                txtTooltipDesc.text = "Haz clic en una ranura del inventario para ver los detalles.";
            }

            if (txtBtnLockLabel != null)
            {
                txtBtnLockLabel.text = "Bloquear objeto";
            }
        }

        // 5. Update layout of action buttons dynamically during trades
        bool isOffering = DialogoManager.Instance != null && DialogoManager.Instance.IsOfferingState();
        bool hasSelection = selectedIndex >= 0 && selectedIndex < items.Count;

        if (btnOfrecerObj != null)
        {
            btnOfrecerObj.SetActive(isOffering);
            btnOfrecerObj.GetComponent<Button>().interactable = hasSelection;
            
            RectTransform ofRect = btnOfrecerObj.GetComponent<RectTransform>();
            if (ofRect != null)
            {
                ofRect.anchoredPosition = new Vector2(0f, 20f);
            }
        }

        if (btnLockObj != null)
        {
            btnLockObj.SetActive(!isOffering);
            btnLockObj.GetComponent<Button>().interactable = hasSelection;
            
            RectTransform lRect = btnLockObj.GetComponent<RectTransform>();
            if (lRect != null)
            {
                lRect.anchoredPosition = new Vector2(0f, 20f);
            }
        }
    }

    private void AplicarEstiloPostApocaliptico(GameObject panel)
    {
        Image img = panel.GetComponent<Image>();
        if (img == null)
        {
            img = panel.AddComponent<Image>();
        }
        img.color = new Color(0.09f, 0.08f, 0.08f, 0.98f);

        Outline outline = panel.GetComponent<Outline>();
        if (outline == null)
        {
            outline = panel.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0.5f, 0.25f, 0.1f, 0.8f);
        outline.effectDistance = new Vector2(2, 2);

        Shadow shadow = panel.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = panel.AddComponent<Shadow>();
        }
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(4, -4);

        AgregarCintaPeligro(panel);
        AgregarRemaches(panel);
    }

    private void AgregarCintaPeligro(GameObject panel)
    {
        GameObject yellowStrip = new GameObject("HazardStrip");
        yellowStrip.transform.SetParent(panel.transform, false);

        RectTransform rect = yellowStrip.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -6);
        rect.sizeDelta = new Vector2(-20, 10);

        Image img = yellowStrip.AddComponent<Image>();
        img.color = new Color(0.85f, 0.65f, 0.1f, 0.9f);

        GameObject stripesObj = new GameObject("StripesText");
        stripesObj.transform.SetParent(yellowStrip.transform, false);

        TextMeshProUGUI stripesText = stripesObj.AddComponent<TextMeshProUGUI>();
        stripesText.text = "<b>/ / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / / /</b>";
        stripesText.alignment = TextAlignmentOptions.Center;
        stripesText.fontSize = 8;
        stripesText.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);

        RectTransform stripesRect = stripesObj.GetComponent<RectTransform>();
        stripesRect.anchorMin = Vector2.zero;
        stripesRect.anchorMax = Vector2.one;
        stripesRect.offsetMin = Vector2.zero;
        stripesRect.offsetMax = Vector2.zero;
    }

    private void AgregarRemaches(GameObject panel)
    {
        Vector2[] offsets = new Vector2[] {
            new Vector2(12, -18),
            new Vector2(-12, -18),
            new Vector2(12, 12),
            new Vector2(-12, 12)
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
            r.sizeDelta = new Vector2(8, 8);

            Image img = rivet.AddComponent<Image>();
            img.color = new Color(0.35f, 0.32f, 0.3f, 1f);

            Outline o = rivet.AddComponent<Outline>();
            o.effectColor = new Color(0.1f, 0.08f, 0.05f, 0.8f);
            o.effectDistance = new Vector2(1, -1);
        }
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

    private Item GetTemplateByName(string itemName)
    {
        foreach (var t in itemTemplates)
        {
            if (t.name.ToLower() == itemName.ToLower())
            {
                return t;
            }
        }
        return null;
    }

    public bool HasItem(string itemName, int minQuantity = 1)
    {
        foreach (var item in items)
        {
            if (item.name.ToLower() == itemName.ToLower())
            {
                if (item.isLocked) return false; // Locked items cannot be used for trading!
                return item.quantity >= minQuantity;
            }
        }
        return false;
    }

    public void RemoveItem(string itemName, int quantityToRemove = 1)
    {
        Item target = null;
        foreach (var item in items)
        {
            if (item.name.ToLower() == itemName.ToLower())
            {
                if (item.isLocked) continue; // Skip locked items!
                target = item;
                break;
            }
        }

        if (target != null)
        {
            target.quantity -= quantityToRemove;
            if (target.quantity <= 0)
            {
                items.Remove(target);
            }
            UpdateInventoryUI();
        }
    }

    public void AddItem(string itemName, int quantityToAdd = 1)
    {
        if (itemName.ToLower() == "nafta")
        {
            float currentLiters = CurrentFuel * 75f;
            float newLiters = Mathf.Clamp(currentLiters + quantityToAdd, 0f, 75f);
            CurrentFuel = newLiters / 75f;
            return; // Fuel is not an item, only refills the bar!
        }

        // First, check if we already have this item. If so, increase quantity.
        foreach (var item in items)
        {
            if (item.name.ToLower() == itemName.ToLower())
            {
                item.quantity += quantityToAdd;
                UpdateInventoryUI();
                return;
            }
        }

        // If not, load from template
        Item temp = GetTemplateByName(itemName);
        if (temp != null)
        {

            items.Add(new Item
            {
                code = temp.code,
                name = temp.name,
                weight = temp.weight,
                type = temp.type,
                description = temp.description,
                customIcon = temp.customIcon,
                quantity = quantityToAdd
            });
            UpdateInventoryUI();
        }
        else
        {
            Debug.LogError("No template found for item: " + itemName);
        }
    }

    private GameObject CrearBotonAccionGameOver(string btnName, string label, Color bgColor, Transform parent, float xOffset)
    {
        GameObject btnObj = new GameObject(btnName);
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 50);
        rect.anchoredPosition = new Vector2(xOffset, 0);

        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;

        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img; // Explicitly set target graphic

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontSize = 16;
        txt.fontStyle = FontStyles.Bold;
        txt.color = Color.white;
        txt.raycastTarget = false; // Disable raycast target on text to prevent click interception bugs
        ApplyFont(txt, true);

        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        return btnObj;
    }

    private void ToggleLockSelectedItem()
    {
        if (selectedIndex >= 0 && selectedIndex < items.Count)
        {
            items[selectedIndex].isLocked = !items[selectedIndex].isLocked;
            UpdateInventoryUI();
        }
    }

    public void RestartGameLevel()
    {
        Time.timeScale = 1f;

        // ── Resetear progreso de niveles (PlayerPrefs) ──────────────────
        PlayerPrefs.DeleteKey("nivel_1_ypf_completado");
        PlayerPrefs.DeleteKey("nivel_2_quilmes_completado");
        PlayerPrefs.DeleteKey("nivel_3_avellaneda_completado");
        PlayerPrefs.DeleteKey("nivel_4_caminito_completado");
        PlayerPrefs.DeleteKey("nivel_5_uade_completado");
        PlayerPrefs.DeleteKey("nivel_6_obelisco_completado");
        PlayerPrefs.Save();

        // ── Resetear todos los tratos de NPCs ───────────────────────────
        NPCInteraction.ResetearTodosLosTratos();

        // ── Resetear intro (se mostrará de nuevo al arrancar nivel_1) ────
        IntroDialogo.ResetearParaNuevaPartida();

        // ── Resetear inventario, nafta y posición ───────────────────────
        CurrentFuel = 1.0f;
        Movimiento.ResetearPosicion();
        RestablecerInventarioInicial();

        // ── Cerrar paneles abiertos ─────────────────────────────────────
        if (panelGameOverObjeto != null)   panelGameOverObjeto.SetActive(false);
        if (panelConfiguracionObjeto != null) panelConfiguracionObjeto.SetActive(false);

        // ── Ir al nivel 1 ───────────────────────────────────────────────
        UnityEngine.SceneManagement.SceneManager.LoadScene("nivel_1_ypf");
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        CurrentFuel = 1.0f;
        
        // Reset position and all trade flags when returning to menu
        Movimiento.ResetearPosicion();
        NPCInteraction.ResetearTodosLosTratos();
        
        RestablecerInventarioInicial();
        if (panelGameOverObjeto != null)
        {
            panelGameOverObjeto.SetActive(false);
        }
        if (panelConfiguracionObjeto != null)
        {
            panelConfiguracionObjeto.SetActive(false);
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene("menu");
    }

    private void RestablecerInventarioInicial()
    {
        items.Clear();
        string[] startingNames = new string[] {
            "termo", "pitusas", "fosforitos", "pava", "garrafa", "gancia"
        };
        foreach (string sName in startingNames)
        {
            Item temp = GetTemplateByName(sName);
            if (temp != null)
            {
                items.Add(new Item {
                    code = temp.code,
                    name = temp.name,
                    weight = temp.weight,
                    type = temp.type,
                    description = temp.description,
                    customIcon = temp.customIcon,
                    quantity = temp.quantity
                });
            }
        }
        UpdateInventoryUI();
    }

    private GameObject CrearBotonAccionPause(string name, string label, Color cbNormal, Transform parent, float yOffset)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, yOffset);
        rect.sizeDelta = new Vector2(250, 50);

        Image img = btnObj.AddComponent<Image>();
        img.color = cbNormal;

        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.08f, 0.08f, 0.08f, 0.8f);
        outline.effectDistance = new Vector2(2, -2);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img; // Explicitly set target graphic
        ColorBlock cb = btn.colors;
        cb.normalColor = cbNormal;
        cb.highlightedColor = new Color(cbNormal.r + 0.1f, cbNormal.g + 0.1f, cbNormal.b + 0.1f, 1f);
        cb.pressedColor = new Color(cbNormal.r - 0.1f, cbNormal.g - 0.1f, cbNormal.b - 0.1f, 1f);
        btn.colors = cb;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontSize = 18;
        txt.fontStyle = FontStyles.Bold;
        txt.color = Color.white;
        txt.raycastTarget = false; // Disable raycast target on text to prevent click interception bugs
        ApplyFont(txt, true);

        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        return btnObj;
    }

    public void AbrirMenuPausa()
    {
        if (panelConfiguracionObjeto != null)
        {
            panelConfiguracionObjeto.SetActive(true);
        }
        Time.timeScale = 0f;
    }

    public void CerrarMenuPausa()
    {
        if (panelConfiguracionObjeto != null)
        {
            panelConfiguracionObjeto.SetActive(false);
        }
        Time.timeScale = 1f;
    }
}

public class RastrojeroPressDetector : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
{
    public System.Action onDown;
    public System.Action onUp;

    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (onDown != null) onDown();
    }

    public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (onUp != null) onUp();
    }

    void OnDisable()
    {
        if (onUp != null) onUp();
    }
}
