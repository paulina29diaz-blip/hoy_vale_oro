using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class CambiarEscena : MonoBehaviour
{
    public string nombreEscena;
    public float duracionFade = 1f;

    private Image panelFade;

    void Start()
    {
        // Busca el panel en la escena
        panelFade = GameObject.Find("PanelFade").GetComponent<Image>();
        panelFade.color = new Color(0, 0, 0, 0); // empieza transparente
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(FadeYCambiar());
        }
    }

    IEnumerator FadeYCambiar()
    {
        float tiempo = 0f;

        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            float alpha = Mathf.Clamp01(tiempo / duracionFade);
            panelFade.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        // Save progress when exiting a level
        string activeScene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt(activeScene + "_completado", 1);
        PlayerPrefs.Save();
        Debug.Log($"CambiarEscena: Marked {activeScene} as completed in PlayerPrefs.");

        SceneManager.LoadScene(nombreEscena);
    }
}