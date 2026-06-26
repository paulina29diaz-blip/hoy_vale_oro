using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class DialogoSecuenciaController : MonoBehaviour
{
    [Header("UI Elements")]
    public Image spriteDisplay;
    public TextMeshProUGUI textDisplay;
    public Button continuarButton;

    [Header("Sprites")]
    public Sprite spritePJPrincipal;
    public Sprite spriteCelu;

    private int step = 0;

    void Start()
    {
        if (continuarButton != null)
        {
            continuarButton.onClick.AddListener(OnContinuarClick);
        }
        
        // Fallback: load sprites from Resources if not assigned in Inspector
        if (spritePJPrincipal == null)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites/dialogos/dialogopjprincipal");
            if (sprites != null && sprites.Length > 0) spritePJPrincipal = sprites[0];
            else Debug.LogWarning("Failed to load dialogopjprincipal from Resources");
        }
        if (spriteCelu == null)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites/dialogos/dialogocelu");
            if (sprites != null && sprites.Length > 0) spriteCelu = sprites[0];
            else Debug.LogWarning("Failed to load dialogocelu from Resources");
        }
        
        // Show first step
        ShowStep(0);
    }


    void OnContinuarClick()
    {
        step++;
        if (step >= 4)
        {
            // Reset position of truck/level states when loading the map scene
            Movimiento.ResetearPosicion();
            NPCInteraction.ResetearTratosDeNivel("mapa");
            SceneManager.LoadScene("mapa");
        }
        else
        {
            ShowStep(step);
        }
    }

    void ShowStep(int currentStep)
    {
        switch (currentStep)
        {
            case 0:
                if (spriteDisplay != null) spriteDisplay.sprite = spritePJPrincipal;
                if (textDisplay != null) textDisplay.text = "hola, hola, hay algún lugar seguro?";
                break;
            case 1:
                if (spriteDisplay != null) spriteDisplay.sprite = spriteCelu;
                if (textDisplay != null) textDisplay.text = "hola, en el obelisco tenes una zona segura pero nos faltan recursos";
                break;
            case 2:
                if (spriteDisplay != null) spriteDisplay.sprite = spritePJPrincipal;
                if (textDisplay != null) textDisplay.text = "que necesitan?";
                break;
            case 3:
                if (spriteDisplay != null) spriteDisplay.sprite = spriteCelu;
                if (textDisplay != null) textDisplay.text = "necesitamos una caja de herramientas, bateria, mapa, anafe y guantes";
                break;
        }
    }
}
