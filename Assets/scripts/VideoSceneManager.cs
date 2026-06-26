using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reproduce introjuego.mp4 al arrancar el juego.
/// Presioná ESPACIO (o cualquier tecla/click) para saltear.
/// Al terminar o saltear va a la escena "menu".
/// </summary>
public class VideoSceneManager : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string nextSceneName = "menu";

    private bool    isPrepared  = false;
    private bool    saltando    = false;

    // Fade
    private Image   fadeOverlay;
    private float   fadeDuration = 1.0f;
    private bool    fadingIn     = true;
    private float   fadeTimer    = 0f;

    // -----------------------------------------------------------------------
    void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        CreateUI();

        if (videoPlayer != null)
        {
            // Usar VideoClip asignado en el Inspector si existe
            if (videoPlayer.clip != null)
            {
                videoPlayer.source = VideoSource.VideoClip;
            }
            else
            {
                // StreamingAssets es la única carpeta accesible en builds
                // El video debe estar en Assets/StreamingAssets/introjuego.mp4
                string fileName = "introjuego.mp4";
                string videoUrl;

#if UNITY_ANDROID
                // Android usa el path directo sin file://
                videoUrl = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
#elif UNITY_WEBGL
                videoUrl = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
#else
                // Windows, Mac, Linux, Editor
                videoUrl = "file://" + System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
#endif
                videoPlayer.source = VideoSource.Url;
                videoPlayer.url    = videoUrl;
                Debug.Log("[VideoSceneManager] URL del video: " + videoUrl);
            }

            videoPlayer.audioOutputMode   = VideoAudioOutputMode.Direct;
            videoPlayer.prepareCompleted += OnPrepared;
            videoPlayer.loopPointReached += OnFinished;
            videoPlayer.errorReceived    += OnError;
            videoPlayer.Prepare();
        }
        else
        {
            Debug.LogWarning("[VideoSceneManager] VideoPlayer no encontrado. Yendo directo al menú.");
            IrAlMenu();
        }
    }

    // -----------------------------------------------------------------------
    void Update()
    {
        // Fade in al arrancar
        if (fadingIn)
        {
            fadeTimer += Time.deltaTime;
            float t = fadeTimer / fadeDuration;
            if (fadeOverlay != null)
                fadeOverlay.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(t));
            if (t >= 1f)
            {
                fadingIn = false;
                if (fadeOverlay != null)
                    fadeOverlay.gameObject.SetActive(false);
            }
        }

        // Saltear con ESPACIO, Enter, Escape o click
        if (!saltando && isPrepared &&
            (Input.GetKeyDown(KeyCode.Space)  ||
             Input.GetKeyDown(KeyCode.Return) ||
             Input.GetKeyDown(KeyCode.Escape) ||
             Input.GetMouseButtonDown(0)))
        {
            IrAlMenu();
        }
    }

    // -----------------------------------------------------------------------
    private void OnPrepared(VideoPlayer vp)
    {
        isPrepared = true;
        vp.Play();
    }

    private void OnFinished(VideoPlayer vp) => IrAlMenu();

    private void OnError(VideoPlayer vp, string msg)
    {
        Debug.LogError("[VideoSceneManager] Error: " + msg);
        IrAlMenu();
    }

    private void IrAlMenu()
    {
        if (saltando) return;
        saltando = true;
        if (videoPlayer != null) videoPlayer.Stop();
        SceneManager.LoadScene(nextSceneName);
    }

    // -----------------------------------------------------------------------
    // UI: fondo negro de fade + hint "Presioná ESPACIO para saltear"
    // -----------------------------------------------------------------------
    private void CreateUI()
    {
        // Canvas
        GameObject cgo = new GameObject("VideoUICanvas");
        Canvas canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        CanvasScaler sc = cgo.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight  = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();

        // Fade overlay (empieza opaco, se va disolviendo)
        GameObject fgo = new GameObject("FadeOverlay", typeof(RectTransform));
        fgo.transform.SetParent(cgo.transform, false);
        RectTransform frt = fgo.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = frt.offsetMax = Vector2.zero;
        fadeOverlay       = fgo.AddComponent<Image>();
        fadeOverlay.color = Color.black;

        // Hint "Presioná ESPACIO para saltear" — esquina inferior derecha
        GameObject hgo = new GameObject("SkipHint", typeof(RectTransform));
        hgo.transform.SetParent(cgo.transform, false);
        RectTransform hrt = hgo.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0.55f, 0.02f);
        hrt.anchorMax = new Vector2(0.98f, 0.09f);
        hrt.offsetMin = hrt.offsetMax = Vector2.zero;

        TextMeshProUGUI hint = hgo.AddComponent<TextMeshProUGUI>();
        hint.text      = "Presioná  ESPACIO  para saltear";
        hint.fontSize  = 22f;
        hint.fontStyle = FontStyles.Italic;
        hint.alignment = TextAlignmentOptions.MidlineRight;
        hint.color     = new Color(1f, 1f, 1f, 0.65f);
    }
}
