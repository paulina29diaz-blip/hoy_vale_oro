using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransitionTrigger : MonoBehaviour
{
    [Header("Transition Settings")]
    public string targetSceneName = "mapa";
    
    private bool isTransitioning = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Transition on any physics collision
        Transition();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Transition on any trigger overlap
        Transition();
    }

    private void Transition()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        
        // Save progress when exiting a level
        string activeScene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt(activeScene + "_completado", 1);
        PlayerPrefs.Save();
        Debug.Log($"LevelTransitionTrigger: Marked {activeScene} as completed in PlayerPrefs.");

        Debug.Log($"LevelTransitionTrigger: Loading scene '{targetSceneName}' immediately on collision with {gameObject.name}");
        SceneManager.LoadScene(targetSceneName);
    }
}
