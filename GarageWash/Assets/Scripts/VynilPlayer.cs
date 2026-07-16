using UnityEngine;
using System.Collections; // Necesario para usar Corrutinas

public class InteractableToggle : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animator;
    public AudioSource audioSource;
    
    [Header("Configuración")]
    public string nombreParametroAnimacion = "IsActive";
    public float retrasoAudio = 2.0f; // Tiempo de espera en segundos
    
    private bool estaActivado = false;
    private Coroutine rutinaAudio; // Para guardar la referencia del contador

    public void ToggleAccion()
    {
        estaActivado = !estaActivado;

        if (estaActivado)
        {
            // --- ACTIVAR ---
            
            // 1. Iniciamos animación inmediatamente
            if(animator) animator.SetBool(nombreParametroAnimacion, true);

            // 2. Iniciamos el contador para el audio
            // Si había uno corriendo, lo paramos por seguridad
            if (rutinaAudio != null) StopCoroutine(rutinaAudio);
            rutinaAudio = StartCoroutine(EsperarYReproducir());
        }
        else
        {
            // --- DESACTIVAR ---

            // 1. Paramos el contador del audio inmediatamente
            // (Si el usuario apaga el objeto antes de los 2 seg, el audio nunca sonará)
            if (rutinaAudio != null) StopCoroutine(rutinaAudio);
            
            // 2. Cortamos el audio si ya estaba sonando
            if(audioSource) audioSource.Stop();

            // 3. Mandamos la señal de apagar animación
            if(animator) animator.SetBool(nombreParametroAnimacion, false);
        }
    }

    // Esta es la rutina que cuenta el tiempo en segundo plano
    IEnumerator EsperarYReproducir()
    {
        yield return new WaitForSeconds(retrasoAudio);
        if(audioSource) audioSource.Play();
    }
}