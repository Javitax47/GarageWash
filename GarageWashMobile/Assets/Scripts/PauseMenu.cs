using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePause : MonoBehaviour
{
    [Header("Canvases")]
    public GameObject gameCanvas;   // Canvas con HUD de la partida
    public GameObject pauseCanvas;  // Canvas con el menú de pausa

    private bool isPaused = false;

    void Start()
    {
        // Estado inicial: jugando
        Time.timeScale = 1f;
        isPaused = false;

        if (gameCanvas != null)
            gameCanvas.SetActive(true);

        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);  // El menú de pausa empieza oculto
    }

    public void Pause()
    {
        if (isPaused) return;
        isPaused = true;

        // Detener el tiempo del juego (cronómetro, físicas, etc.)
        Time.timeScale = 0f;

        // Mostrar menú de pausa
        if (pauseCanvas != null)
            pauseCanvas.SetActive(true);

        // Opcional: ocultar HUD de partida
        if (gameCanvas != null)
            gameCanvas.SetActive(false);
    }

    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;

        // Reanudar tiempo
        Time.timeScale = 1f;

        // Ocultar menú de pausa
        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);

        // Volver a mostrar HUD de partida
        if (gameCanvas != null)
            gameCanvas.SetActive(true);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuPrincipal");
    }
}
