using KBCore.Refs;
using UnityEngine;
using UnityEngine.UI;

public class CleanProgressUI : MonoBehaviour
{
    [Header("Referencias de UI de Juego")]
    [Tooltip("El GameObject padre de la barra de progreso y su texto.")]
    [SerializeField] private GameObject progressBarGroup; // Arrastra aquí "ProgressBar_Group"
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Text percentageText;

    [Header("Referencias de UI de Victoria")]
    [Tooltip("El Canvas o GameObject padre de la pantalla de victoria.")]
    [SerializeField] private GameObject victoryCanvas;

    private PaintableSurface trackedSurface;
    private bool isVictoryScreenShown = false;

    // --- FUNCIÓN START MODIFICADA ---
    private void Start()
    {
        // Forzamos el estado inicial correcto al iniciar la escena.
        // La UI de progreso DEBE empezar oculta.
        if (progressBarGroup != null)
        {
            progressBarGroup.SetActive(false);
        }
        else
        {
            Debug.LogError("¡Referencia a 'ProgressBar Group' no asignada en CleaningUIManager!");
        }

        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (trackedSurface != null)
        {
            trackedSurface.OnCleanlinessChanged -= UpdateProgress;
        }
    }

    // --- FUNCIÓN STARTTRACKING MODIFICADA ---
    public void StartTracking(PaintableSurface surface)
    {
        trackedSurface = surface;
        if (trackedSurface != null)
        {
            trackedSurface.OnCleanlinessChanged += UpdateProgress;
            
            // AHORA es cuando activamos la barra de progreso.
            if (progressBarGroup != null)
            {
                progressBarGroup.SetActive(true);
            }
            
            // Actualizamos la UI con el valor inicial (que debería ser 0%).
            UpdateProgress(trackedSurface.Cleanliness);
        }
    }

    private void UpdateProgress(float cleanliness)
    {
        if (isVictoryScreenShown) return;

        if (progressSlider != null)
        {
            progressSlider.value = cleanliness;
        }
        if (percentageText != null)
        {
            percentageText.text = (cleanliness * 100).ToString("F0") + "%";
        }

        if (cleanliness >= 1.0f)
        {
            ShowVictoryScreen();
        }
    }

    private void ShowVictoryScreen()
    {
        if (isVictoryScreenShown) return;
        isVictoryScreenShown = true;
        AudioManager.Instance.PlayVictory();
        
        // Al ganar, ocultamos la barra de progreso (el crosshair se oculta en otro script si es necesario)
        if (progressBarGroup != null)
        {
            progressBarGroup.SetActive(false);
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StopTimerOnWin();
        }
        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(true);
        }
        
        if (Camera.main != null)
        {
            var powerWasherController = Camera.main.GetComponentInChildren<PowerWasherController>();
            if (powerWasherController != null)
            {
                powerWasherController.enabled = false;
                powerWasherController.SendMessage("SetWashing", false, SendMessageOptions.DontRequireReceiver);
            }
        }

        if (trackedSurface != null)
        {
            trackedSurface.OnCleanlinessChanged -= UpdateProgress;
        }
    }

#if UNITY_EDITOR
    private void OnValidate() { this.ValidateRefs(); }
#endif
}