using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Referencias de AudioSources")]
    [SerializeField] private AudioSource effectsAudioSource;
    // --- ¡CAMBIO CLAVE! ---
    [SerializeField] private AudioSource timerLoopSource;
    [SerializeField] private AudioSource washingLoopSource;
    
    [Header("Clips de Efectos (One-Shot)")]
    [SerializeField] private AudioClip victoryClip;
    [SerializeField] private AudioClip defeatClip;
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip placeObjectClip;

    [Header("Clips de Bucles (Loops)")]
    [SerializeField] private AudioClip timerLoopClip;
    [SerializeField] private AudioClip washingLoopClip;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }

        if (effectsAudioSource == null) effectsAudioSource = GetComponent<AudioSource>();
        // Buscamos los nuevos AudioSources por nombre si no están asignados
        if (timerLoopSource == null && transform.Find("TimerLoopSource") != null)
            timerLoopSource = transform.Find("TimerLoopSource").GetComponent<AudioSource>();
        if (washingLoopSource == null && transform.Find("WashingLoopSource") != null)
            washingLoopSource = transform.Find("WashingLoopSource").GetComponent<AudioSource>();
    }

    // --- Funciones para Efectos Cortos (sin cambios) ---
    public void PlayVictory() { PlayEffect(victoryClip); }
    public void PlayDefeat() { PlayEffect(defeatClip); }
    public void PlayButtonClick() { PlayEffect(buttonClickClip); }
    public void PlayPlaceObject() { PlayEffect(placeObjectClip); }
    private void PlayEffect(AudioClip clip) { if (effectsAudioSource != null && clip != null) { effectsAudioSource.PlayOneShot(clip); } }

    // --- ¡NUEVAS FUNCIONES PARA LOS BUCLES (MÁS SIMPLES)! ---

    public void StartTimerLoop()
    {
        if (timerLoopSource != null && timerLoopClip != null)
        {
            timerLoopSource.clip = timerLoopClip;
            timerLoopSource.loop = true;
            timerLoopSource.Play();
        }
    }

    public void StartWashingLoop()
    {
        if (washingLoopSource != null && washingLoopClip != null)
        {
            washingLoopSource.clip = washingLoopClip;
            washingLoopSource.loop = true;
            washingLoopSource.Play();
        }
    }
    
    // Ahora simplemente detenemos el sonido del agua, sin preocuparnos por el temporizador.
    public void StopWashingLoop()
    {
        if (washingLoopSource != null)
        {
            washingLoopSource.Stop();
        }
    }

    // Ahora debe detener ambos bucles.
    public void StopAllLoops()
    {
        if (timerLoopSource != null)
        {
            timerLoopSource.Stop();
        }
        if (washingLoopSource != null)
        {
            washingLoopSource.Stop();
        }
    }
}