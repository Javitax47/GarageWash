using UnityEngine;
using UnityEngine.SceneManagement;

public class GameWin : MonoBehaviour
{
    [Header("Canvases")]
    public GameObject gameCanvas;   // Canvas con HUD de la partida
    public GameObject winCanvas;  // Canvas con el menúç


    void Start()
    {
        // Estado inicial: juga

        if (gameCanvas != null)
            gameCanvas.SetActive(true);

        if (winCanvas != null)
            winCanvas.SetActive(false);  // El menúç empieza oculto
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
