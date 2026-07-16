using System.Collections; // ¡Necesario para usar Corrutinas!
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Referencias de Audio")]
    [Tooltip("El AudioSource que reproducirá la música de fondo en bucle.")]
    [SerializeField] private AudioSource musicSource;
    [Tooltip("El AudioSource para los efectos de sonido cortos (clics).")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips de Audio")]
    [Tooltip("La música que sonará en el menú principal.")]
    [SerializeField] private AudioClip backgroundMusic;
    [Tooltip("El sonido que se reproducirá al hacer clic en un botón.")]
    [SerializeField] private AudioClip buttonClickSound;
    
    [Header("Configuración de Transición")]
    [Tooltip("El tiempo en segundos que se esperará después del clic antes de la acción.")]
    [SerializeField] private float transitionDelay = 0.5f;

    void Start()
    {
        PlayBackgroundMusic();
    }

    private void PlayBackgroundMusic()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void PlayButtonClickSound()
    {
        if (sfxSource != null && buttonClickSound != null)
        {
            sfxSource.PlayOneShot(buttonClickSound);
        }
    }

    // --- FUNCIÓN PÚBLICA MODIFICADA ---
    // Esta función, llamada por el botón, ahora solo se encarga de iniciar la corrutina.
    public void PlayGame()
    {
        StartCoroutine(LoadGameWithDelay());
    }

    // --- ¡NUEVA CORRUTINA! ---
    // Esta es una función especial que puede pausar su ejecución.
    private IEnumerator LoadGameWithDelay()
    {
        // 1. Reproducimos el sonido del clic.
        PlayButtonClickSound();
        
        // 2. Pausamos la ejecución de esta función durante el tiempo especificado.
        yield return new WaitForSeconds(transitionDelay);
        
        // 3. Después de la pausa, continuamos y cargamos la escena.
        Debug.Log("Cargando la escena del juego...");
        SceneManager.LoadScene("SampleScene");
    }

    // --- FUNCIÓN PÚBLICA MODIFICADA ---
    public void QuitGame()
    {
        StartCoroutine(QuitGameWithDelay());
    }

    // --- ¡NUEVA CORRUTINA! ---
    private IEnumerator QuitGameWithDelay()
    {
        // 1. Reproducimos el sonido.
        PlayButtonClickSound();

        // 2. Pausamos.
        yield return new WaitForSeconds(transitionDelay);
        
        // 3. Cerramos la aplicación.
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}