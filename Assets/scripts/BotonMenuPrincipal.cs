using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BotonMenuPrincipal : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    public float targetRotation = 5f; // Rotation in degrees on hover
    public float transitionSpeed = 8f;

    [Header("Navigation")]
    public bool isIniciarJuego = false;

    private Quaternion originalRotation;
    private bool isHovered = false;

    void Start()
    {
        originalRotation = transform.localRotation;

        // Register button click listener
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnButtonClick);
        }
    }

    void Update()
    {
        Quaternion targetRot = isHovered ? Quaternion.Euler(0, 0, targetRotation) : originalRotation;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * transitionSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    private void OnButtonClick()
    {
        if (isIniciarJuego)
        {
            // Reset level progress on starting a new game
            PlayerPrefs.DeleteKey("nivel_1_ypf_completado");
            PlayerPrefs.DeleteKey("nivel_2_quilmes_completado");
            PlayerPrefs.DeleteKey("nivel_3_avellaneda_completado");
            PlayerPrefs.DeleteKey("nivel_4_caminito_completado");
            PlayerPrefs.DeleteKey("nivel_5_uade_completado");
            PlayerPrefs.DeleteKey("nivel_6_obelisco_completado");
            // Mark that a game session has started (so MapSceneManager doesn't auto-reset)
            PlayerPrefs.SetInt("sesion_activa", 1);
            PlayerPrefs.Save();

            SceneManager.LoadScene("videointro");
        }
    }
}
