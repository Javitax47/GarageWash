using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CleanProgressUI_VR : MonoBehaviour
{
    [Header("Referencias del Juego")]
    public PaintableSurface targetSurface;
    public GameManager gameManager; 

    [Header("UI Circular")]
    public Image ringFillImage; 
    public TextMeshProUGUI percentageText; 
    public TextMeshProUGUI timerText; 

    [Header("Estilo Victoria/Derrota")]
    public Color victoryColor = Color.green;
    public Color defeatColor = Color.red;
    
    [Header("Sistemas de Partículas")]
    public ParticleSystem carVictoryParticles;
    public ParticleSystem carDefeatParticles; // Sistema para cuando pierdes

    private bool isGameOver = false;

    private void Start()
    {
        if (gameManager == null) gameManager = GameManager.Instance;

        // Suscribirse a eventos del barco
        if (targetSurface != null)
        {
            targetSurface.OnCleanlinessChanged += UpdateCleanliness;
            UpdateCleanliness(targetSurface.Cleanliness);
        }

        // --- NUEVO: Suscribirse a la derrota del GameManager ---
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameLost += HandleDefeatUI;
        }
    }

    private void Update()
    {
        // Solo actualizamos el cronómetro si el juego no ha terminado
        if (gameManager != null && timerText != null && !isGameOver) 
            UpdateTimerDisplay();
    }

    private void UpdateCleanliness(float cleanliness)
    {
        if (isGameOver) return;

        if (ringFillImage != null) ringFillImage.fillAmount = cleanliness;
        if (percentageText != null) percentageText.text = Mathf.FloorToInt(cleanliness * 100).ToString() + "%";

        if (cleanliness >= 1.0f)
        {
            HandleVictoryUI();
        }
    }

    private void UpdateTimerDisplay()
    {
        float timeToDisplay = gameManager.GetTimeRemaining();
        if (timeToDisplay < 0) timeToDisplay = 0;
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void HandleVictoryUI()
    {
        if (isGameOver) return;
        isGameOver = true;

        ringFillImage.color = victoryColor;
        percentageText.color = victoryColor;

        if (carVictoryParticles != null) carVictoryParticles.Play();
        
        if (gameManager != null) gameManager.StopTimerOnWin();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayVictory();
    }

    // --- NUEVA FUNCIÓN DE DERROTA ---
    private void HandleDefeatUI()
    {
        if (isGameOver) return; // Evitar que choque con victoria
        isGameOver = true;

        // 1. Cambiar a ROJO
        if (ringFillImage != null) ringFillImage.color = defeatColor;
        if (percentageText != null) percentageText.color = defeatColor;

        // 2. Partículas de derrota (ej. humo negro o chispas rojas)
        if (carDefeatParticles != null) carDefeatParticles.Play();

        Debug.Log("UI actualizada a estado de derrota.");
    }

    private void OnDestroy()
    {
        if (targetSurface != null) targetSurface.OnCleanlinessChanged -= UpdateCleanliness;
        // Desvincular evento para evitar errores de memoria
        if (GameManager.Instance != null) GameManager.Instance.OnGameLost -= HandleDefeatUI;
    }
}