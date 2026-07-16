using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    private enum TutorialState
    {
        FindSurface,
        ScaleAndRotate,
        DoubleClick,
        HoldToWash,
        Completed // Simplificamos el final
    }

    [Header("Referencias de UI")]
    [SerializeField] private CanvasGroup[] tutorialSteps;
    [Header("Configuración de Transición")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float delayBetweenSteps = 0.2f;

    private TutorialState currentState;
    private PowerWasherController powerWasherController;

    private void OnEnable()
    {
        TutorialEvents.OnSurfaceFound += HandleSurfaceFound;
        TutorialEvents.OnObjectScaledOrRotated += HandleObjectScaledOrRotated;
        TutorialEvents.OnObjectPlaced += HandleObjectPlaced;
        TutorialEvents.OnWashingStarted += HandleWashingStarted;
    }

    private void OnDisable()
    {
        TutorialEvents.OnSurfaceFound -= HandleSurfaceFound;
        TutorialEvents.OnObjectScaledOrRotated -= HandleObjectScaledOrRotated;
        TutorialEvents.OnObjectPlaced -= HandleObjectPlaced;
        TutorialEvents.OnWashingStarted -= HandleWashingStarted;
    }

    void Start()
    {
        foreach (var step in tutorialSteps) { step.alpha = 0; step.gameObject.SetActive(false); }
        
        powerWasherController = FindAnyObjectByType<PowerWasherController>(FindObjectsInactive.Include);
        if (powerWasherController != null)
        {
            powerWasherController.enabled = false;
        }

        currentState = TutorialState.FindSurface;
        StartCoroutine(FadeStep(tutorialSteps[0], true));
    }

    // --- MANEJADORES DE EVENTOS CON LA LÓGICA CORREGIDA ---

    private void HandleSurfaceFound()
    {
        if (currentState != TutorialState.FindSurface) return;
        StartCoroutine(TransitionToState(TutorialState.ScaleAndRotate));
    }

    private void HandleObjectScaledOrRotated()
    {
        if (currentState != TutorialState.ScaleAndRotate) return;
        StartCoroutine(TransitionToState(TutorialState.DoubleClick));
    }

    // --- ¡FUNCIÓN CLAVE MODIFICADA! ---
    private void HandleObjectPlaced()
    {
        // Aceptamos este evento si estamos en el paso de escalar/rotar (el jugador se lo saltó)
        // O si estamos en el paso de doble clic (el jugador siguió las instrucciones).
        if (currentState != TutorialState.ScaleAndRotate && currentState != TutorialState.DoubleClick)
        {
            return;
        }
        
        // En ambos casos, el siguiente paso lógico es lavar el coche.
        StartCoroutine(TransitionToState(TutorialState.HoldToWash));
    }

    private void HandleWashingStarted()
    {
        if (currentState != TutorialState.HoldToWash) return;
        // Al empezar a lavar, el tutorial termina.
        StartCoroutine(TransitionToState(TutorialState.Completed));
    }

    private IEnumerator TransitionToState(TutorialState nextState)
    {
        int currentStateIndex = (int)currentState;
        if (currentStateIndex < tutorialSteps.Length)
        {
            yield return StartCoroutine(FadeStep(tutorialSteps[currentStateIndex], false));
        }
        if (delayBetweenSteps > 0)
        {
            yield return new WaitForSeconds(delayBetweenSteps);
        }
        
        currentState = nextState;
        int nextStateIndex = (int)currentState;
        
        if (nextStateIndex < tutorialSteps.Length)
        {
            yield return StartCoroutine(FadeStep(tutorialSteps[nextStateIndex], true));

            if (nextState == TutorialState.HoldToWash && powerWasherController != null)
            {
                powerWasherController.enabled = true;
            }
        }
        else
        {
            Debug.Log("Tutorial completado.");
        }
    }

    private IEnumerator FadeStep(CanvasGroup step, bool fadeIn)
    {
        if (fadeIn) step.gameObject.SetActive(true);
        float startAlpha = step.alpha;
        float endAlpha = fadeIn ? 1f : 0f;
        float time = 0;
        while (time < fadeDuration)
        {
            step.alpha = Mathf.Lerp(startAlpha, endAlpha, time / fadeDuration);
            time += Time.deltaTime;
            yield return null;
        }
        step.alpha = endAlpha;
        if (!fadeIn) step.gameObject.SetActive(false);
    }
}