using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SimpleScraper : MonoBehaviour
{
    [Header("Capas")]
    public LayerMask paintableLayer;
    public LayerMask scrapableLayer;

    [Header("Detección")]
    public Transform bladeTip; 
    public float contactRadius = 0.03f; // Radio para detectar contacto

    [Header("Restricción de Ángulo")]
    [Tooltip("Ángulo máximo de error. 45º permite bastante libertad. 0º requiere precisión quirúrgica.")]
    [Range(0, 90)] public float maxAngleError = 45f;

    [Header("Feedback")]
    public AudioSource scrapeSound;
    public AudioSource popSound;
    public ParticleSystem scrapeParticles;
    
    // Sonido de roce
    public AudioSource slideAudioSource;
    public float minSpeedForSound = 0.1f;

    // Estado
    private bool isTouchingHull = false;
    private bool isAngleCorrect = false;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;
    private Vector3 lastPosition;
    private float currentSpeed;

    private void Awake()
    {
        interactable = GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
    }

    private void Update()
    {
        // 1. Calcular velocidad
        currentSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;

        // 2. Detección Física
        CheckSurfaceContact();

        // 3. Sonido de Roce (Solo si toca el coche y el ángulo es bueno)
        HandleSlideSound();
    }

    private void CheckSurfaceContact()
    {
        // A. ¿Tocamos el coche? (Usamos esfera para robustez)
        isTouchingHull = Physics.CheckSphere(bladeTip.position, contactRadius, paintableLayer);

        isAngleCorrect = false;

        if (isTouchingHull)
        {
            // B. ¿Es el ángulo correcto?
            // Lanzamos un rayo para obtener la Normal de la superficie exacta
            RaycastHit hit;
            if (Physics.Raycast(bladeTip.position, bladeTip.forward, out hit, contactRadius * 2f, paintableLayer))
            {
                // Vector Normal: Hacia afuera del coche
                // Blade Up: La cara plana de la espátula
                float angle = Vector3.Angle(bladeTip.up, hit.normal);

                // LÓGICA DOBLE CARA:
                // Cara A (Normal): El ángulo es cercano a 0
                bool sideA = angle < maxAngleError;
                
                // Cara B (Invertida): El ángulo es cercano a 180
                bool sideB = angle > (180f - maxAngleError);

                if (sideA || sideB)
                {
                    isAngleCorrect = true;
                }
            }
            else
            {
                // Si la esfera toca pero el rayo falla (estamos en un borde raro), 
                // asumimos que el ángulo es bueno para no frustrar al jugador.
                isAngleCorrect = true; 
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // 1. Filtro Lapa
        if (((1 << other.gameObject.layer) & scrapableLayer) == 0) return;

        // 2. Filtro Contacto Físico
        if (!isTouchingHull) return;

        // 3. Filtro Ángulo (Si clavas de punta, no funciona)
        if (!isAngleCorrect) return;

        SimpleBarnacle barnacle = other.GetComponent<SimpleBarnacle>();
        if (barnacle != null)
        {
            // Enviamos deltaTime. Retorna true si explota.
            bool popped = barnacle.AddScrapeTime(Time.deltaTime);

            if (!popped)
            {
                TriggerHaptic(0.4f); // Vibración constante mientras rascas
            }
            else
            {
                TriggerHaptic(1.0f); // Vibración fuerte al romper
                if (popSound != null) popSound.Play();
            }
            
            // Feedback Visual
            if (scrapeParticles != null && !scrapeParticles.isPlaying) scrapeParticles.Play();
        }
    }

    private void HandleSlideSound()
    {
        if (slideAudioSource == null) return;

        // Suena si tocamos, el ángulo es bueno y nos movemos
        bool shouldPlay = isTouchingHull && isAngleCorrect && currentSpeed > minSpeedForSound;

        if (shouldPlay)
        {
            if (!slideAudioSource.isPlaying) slideAudioSource.Play();
            slideAudioSource.volume = Mathf.Lerp(slideAudioSource.volume, Mathf.Clamp01(currentSpeed), Time.deltaTime * 10f);
            slideAudioSource.pitch = 0.8f + (slideAudioSource.volume * 0.4f);
        }
        else
        {
            if (slideAudioSource.isPlaying)
            {
                slideAudioSource.volume = Mathf.Lerp(slideAudioSource.volume, 0f, Time.deltaTime * 10f);
                if (slideAudioSource.volume < 0.01f) slideAudioSource.Stop();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & scrapableLayer) != 0)
        {
            if (scrapeParticles != null) scrapeParticles.Stop();
        }
    }

    private void TriggerHaptic(float amplitude)
    {
        if (interactable != null && interactable.isSelected)
        {
            if (interactable.firstInteractorSelecting is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor controller)
            {
                controller.xrController.SendHapticImpulse(amplitude, 0.1f);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (bladeTip != null)
        {
            // Verde = Listo para rascar (Toca y Ángulo OK)
            // Amarillo = Toca pero Ángulo MAL (Clavando)
            // Rojo = No toca
            
            Color c = Color.red;
            if (isTouchingHull) c = isAngleCorrect ? Color.green : Color.yellow;

            Gizmos.color = c;
            Gizmos.DrawWireSphere(bladeTip.position, contactRadius);
            
            // Dibujamos la dirección del filo para orientarnos
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(bladeTip.position, bladeTip.forward * 0.05f);
            
            // Dibujamos la cara "Arriba"
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(bladeTip.position, bladeTip.up * 0.03f);
        }
    }
}