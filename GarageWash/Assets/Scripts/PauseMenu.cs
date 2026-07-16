using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MenuPausaInteligente : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject canvasPausa;
    public Transform camaraJugador;

    [Header("Configuración Pausa")]
    public float distanciaMaxima = 2.0f;
    public LayerMask capasBloqueo;

    [Header("Control de Video")]
    [Tooltip("Arrastra aquí el VideoPlayer de la escena (si hay uno)")]
    public VideoPlayer videoPlayer;
    public float saltoSegundos = 10f;

    private InputAction botonMenuAction;

    private void Awake()
    {
        botonMenuAction = new InputAction(type: InputActionType.Button, binding: "<XRController>{LeftHand}/menu");
    }

    private void OnEnable() => botonMenuAction.Enable();
    private void OnDisable() => botonMenuAction.Disable();

    private void Update()
    {
        if (botonMenuAction.WasPressedThisFrame())
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        if (canvasPausa.activeSelf)
            ResumeGame();
        else
            PauseGame();
    }

    public void ResumeGame()
    {
        canvasPausa.SetActive(false);
        Time.timeScale = 1f;

        if (videoPlayer != null)
        {
            videoPlayer.Play();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetTimerPaused(false);
        }

        Debug.Log("Juego y Video Reanudados.");
    }

    void PauseGame()
    {
        Time.timeScale = 0f;

        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetTimerPaused(true);
        }

        Vector3 origen = camaraJugador.position;
        Vector3 direccion = camaraJugador.forward;
        float distanciaFinal = distanciaMaxima;
        RaycastHit golpe;

        if (Physics.Raycast(origen, direccion, out golpe, distanciaMaxima, capasBloqueo))
        {
            distanciaFinal = golpe.distance - 0.15f;
        }

        canvasPausa.transform.position = origen + (direccion * distanciaFinal);
        canvasPausa.transform.LookAt(camaraJugador);
        canvasPausa.transform.Rotate(0, 180, 0);

        canvasPausa.SetActive(true);
        Debug.Log("Juego y Video Pausados.");
    }

    // =========================================================
    // BOTONES EXTRA PARA LA UI
    // =========================================================

    public void PlayPauseVideo()
    {
        if (videoPlayer == null) return;

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
        else
        {
            videoPlayer.Play();
            ResumeGame(); // Cerramos el menú al darle Play al video
        }
    }

    public void AvanzarVideo()
    {
        if (videoPlayer == null) return;
        videoPlayer.time += saltoSegundos;
    }

    public void RetrocederVideo()
    {
        if (videoPlayer == null) return;
        videoPlayer.time -= saltoSegundos;
    }

    public void ReiniciarEscena()
    {
        Debug.Log("Reiniciando escena...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void CambiarEscena(string nombreEscena)
    {
        Debug.Log("Intentando cargar escena: " + nombreEscena);
        
        if (string.IsNullOrEmpty(nombreEscena))
        {
            Debug.LogError("¡No has escrito el nombre de la escena en el botón!");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(nombreEscena);
    }
}