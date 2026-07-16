using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLost
 : MonoBehaviour
{
    [Header("Canvases")]
    public GameObject gameCanvas;   // Canvas con HUD de la partida
    public GameObject lostCanvas;  // Canvas con el menú


    void Start()
    {
        // Estado inicial: jugando
        Time.timeScale = 1f;

        if (gameCanvas != null)
            gameCanvas.SetActive(true);

        if (lostCanvas != null)
            lostCanvas.SetActive(false);  // El menú empieza oculto
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
