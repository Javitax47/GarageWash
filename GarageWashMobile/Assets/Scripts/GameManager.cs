using TMPro; // Asegúrate de cambiar esto a 'using UnityEngine.UI;' si usas Text Legacy
using UnityEngine;
using UnityEngine.UI; // Y añade esta línea

public class GameManager : MonoBehaviour
{
    // --- Singleton Pattern para acceso fácil desde otros scripts ---
    public static GameManager Instance { get; private set; }

    [Header("Configuración del Temporizador")]
    [Tooltip("Tiempo inicial en segundos (3 minutos = 180 segundos).")]
    public float countdownTime = 180f;

    [Header("Referencias de UI")]
    [Tooltip("El GameObject padre de toda la UI de juego (crosshair, barra, temporizador).")]
    [SerializeField] private GameObject gameplayHUD;
    [Tooltip("El componente de texto para mostrar el temporizador.")]
    [SerializeField] private Text timerText; // Cambiado a Text Legacy
    [Tooltip("El Canvas que se muestra al perder.")]
    [SerializeField] private GameObject defeatCanvas;
    
    // --- Propiedad Pública de Estado ---
    // 'public' para que otros scripts puedan leerlo, 'private set' para que solo este script pueda modificarlo.
    public bool isTimerRunning { get; private set; } = false;
    
    private float currentTime;

    private void Awake()
    {
        // Configuración del Singleton
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
        }
        else 
        { 
            Instance = this; 
        }
    }

    private void Start()
    {
        // Forzamos el estado inicial de la escena
        currentTime = countdownTime;
        if (gameplayHUD != null) gameplayHUD.SetActive(false);
        if (defeatCanvas != null) defeatCanvas.SetActive(false);
        UpdateTimerDisplay();
    }

    private void Update()
    {
        // Si el temporizador no está activo, no hacemos nada.
        if (!isTimerRunning) return;

        // Restamos el tiempo transcurrido desde el último frame
        currentTime -= Time.deltaTime;

        // Comprobamos si el tiempo se ha agotado
        if (currentTime <= 0)
        {
            currentTime = 0;
            HandleDefeat(); // El HandleDefeat ya se encarga de poner isTimerRunning a false
        }

        // Actualizamos el texto en la UI
        UpdateTimerDisplay();
    }

    // Esta función es llamada por el PlacementController cuando se coloca el objeto
    public void StartGame()
    {
        Debug.Log("[GameManager] ¡Juego iniciado! El temporizador está corriendo.");
        if (gameplayHUD != null) gameplayHUD.SetActive(true);
        isTimerRunning = true;

        // Le decimos al AudioManager que empiece el sonido del cronómetro
        AudioManager.Instance.StartTimerLoop();
    }

    // Esta función es llamada por el CleaningUIManager cuando se gana
    public void StopTimerOnWin()
    {
        if (!isTimerRunning) return; // Evitar que se llame múltiples veces
        
        Debug.Log("[GameManager] ¡Victoria! El temporizador se ha detenido.");
        isTimerRunning = false;
        
        // Detiene todos los sonidos de bucle (en este caso, el tic-tac)
        AudioManager.Instance.StopAllLoops();
    }

    // Esta función se llama cuando el tiempo llega a cero
    private void HandleDefeat()
    {
        if (!isTimerRunning) return; // Evitar que se llame múltiples veces
        
        Debug.Log("[GameManager] ¡Derrota! El tiempo se ha agotado.");
        isTimerRunning = false;
        
        // Detiene todos los sonidos de bucle
        AudioManager.Instance.StopAllLoops();
        // Reproduce el sonido de derrota
        AudioManager.Instance.PlayDefeat();

        // Ocultamos la UI de juego y mostramos la de derrota
        if (gameplayHUD != null) gameplayHUD.SetActive(false);
        if (defeatCanvas != null) defeatCanvas.SetActive(true);

        // Desactivamos la manguera para que no se pueda seguir jugando
        if (Camera.main != null)
        {
            var powerWasherController = Camera.main.GetComponentInChildren<PowerWasherController>();
            if (powerWasherController != null)
            {
                powerWasherController.enabled = false;
                powerWasherController.SendMessage("SetWashing", false, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    // Actualiza el texto del temporizador en el formato MM:SS
    private void UpdateTimerDisplay()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}