using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // EVENTOS
    public event Action OnGameLost; // Evento de derrota

    [Header("Configuración")]
    public float countdownTime = 180f;
    public bool autoStart = false;

    public bool isTimerRunning { get; private set; } = false;
    private float currentTime;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        currentTime = countdownTime;
        if (autoStart) StartGame();
    }

    private void Update()
    {
        if (!isTimerRunning) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0)
        {
            currentTime = 0;
            HandleDefeat();
        }
    }

    public void StartGame()
    {
        isTimerRunning = true;
        if (AudioManager.Instance != null) AudioManager.Instance.StartTimerLoop();
    }

    public void StopTimerOnWin()
    {
        isTimerRunning = false;
        if (AudioManager.Instance != null) AudioManager.Instance.StopAllLoops();
    }

    private void HandleDefeat()
    {
        if (!isTimerRunning) return;
        isTimerRunning = false;

        // 1. Avisar a otros scripts (como la UI)
        OnGameLost?.Invoke();

        // 2. Audio
        if (AudioManager.Instance != null) 
        {
            AudioManager.Instance.StopAllLoops();
            AudioManager.Instance.PlayDefeat();
        }
        
        Debug.Log("Derrota: Tiempo agotado.");
    }

    public void SetTimerPaused(bool paused)
    {
        // Si queremos pausar, isTimerRunning debe ser false
        isTimerRunning = !paused;

        if (paused)
        {
            // Detenemos el audio del temporizador si existe
            if (AudioManager.Instance != null) AudioManager.Instance.StopAllLoops();
            Debug.Log("Temporizador pausado.");
        }
        else
        {
            // Reanudamos el audio
            if (AudioManager.Instance != null) AudioManager.Instance.StartTimerLoop();
            Debug.Log("Temporizador reanudado.");
        }
    }

    public void RestartGame()
    {
        // MUY IMPORTANTE: Resetear el tiempo antes de cargar
        Time.timeScale = 1f;
        
        // Obtenemos la escena activa y la recargamos
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
        
        Debug.Log("Reiniciando nivel...");
    }

    // --- NUEVA FUNCIÓN: CAMBIAR A UNA ESCENA ESPECÍFICA ---
    public void GoToScene(string sceneName)
    {
        // Resetear el tiempo por si venimos de un menú de pausa
        Time.timeScale = 1f;
        
        SceneManager.LoadScene(sceneName);
        
        Debug.Log("Cargando escena: " + sceneName);
    }

    public float GetTimeRemaining() => currentTime;
}