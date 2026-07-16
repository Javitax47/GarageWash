using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [Header("Referencias")]
    public Animator objetoAnimado;   // El objeto que aparecerá y bailará
    public GameObject canvasMenu;    // El canvas (botón) que desaparecerá
    public string triggerAnimacion = "Start"; 

    public void ActivarYSalir()
    {
        // 1. Activar el objeto y lanzar la animación
        if (objetoAnimado != null)
        {
            objetoAnimado.gameObject.SetActive(true); 
            objetoAnimado.SetTrigger(triggerAnimacion);
        }

        // 2. Desactivar el menú (Canvas)
        if (canvasMenu != null)
        {
            canvasMenu.SetActive(false); 
        }

        // --- NUEVO: INICIAR EL JUEGO (CRONÓMETRO) ---
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
            Debug.Log("Juego iniciado desde el menú.");
        }
        else
        {
            Debug.LogError("¡No se encuentra el GameManager en la escena!");
        }
    }
}