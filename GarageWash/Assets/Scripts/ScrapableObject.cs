using UnityEngine;

public class SimpleBarnacle : MonoBehaviour
{
    [Header("Configuración")]
    public float timeToDetach = 1.0f; // Tiempo necesario rascando
    public float popForce = 5.0f;
    
    [Header("Visual")]
    public float shakeIntensity = 5.0f; // Cuánto vibra al rascar

    // Estado
    private float currentProgress = 0f; // Segundos acumulados
    private Quaternion originalRotation;
    private bool isDetached = false;

    void Start()
    {
        originalRotation = transform.localRotation;
    }

    // Retorna TRUE si se ha soltado en este frame
    public bool AddScrapeTime(float deltaTime)
    {
        if (isDetached) return false;

        // Sumar tiempo
        currentProgress += deltaTime;

        // Calcular % (0 a 1)
        float percent = Mathf.Clamp01(currentProgress / timeToDetach);

        // Agitar visualmente (Shake)
        // Cuanto más cerca del final, más rápido vibra
        float shakeAmount = Mathf.Sin(Time.time * 50f) * shakeIntensity * percent;
        transform.localRotation = originalRotation * Quaternion.Euler(shakeAmount, shakeAmount, 0);

        // Comprobar si termina
        if (percent >= 1.0f)
        {
            Detach();
            return true; // ¡POP!
        }

        return false;
    }

    // Si quieres que el progreso baje si dejas de rascar, añade un Update restando tiempo.
    // Si prefieres que se guarde el progreso, deja esto vacío o bórralo.
    private void Update()
    {
        if (!isDetached && currentProgress > 0)
        {
            // Pierde progreso lentamente si no se rasca (opcional)
            currentProgress -= Time.deltaTime * 0.5f; 
            if (currentProgress < 0) 
            {
                currentProgress = 0;
                transform.localRotation = originalRotation; // Volver a quieto
            }
        }
    }

    private void Detach()
    {
        isDetached = true;
        
        // Añadir física
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        
        rb.isKinematic = false;
        rb.useGravity = true;
        
        // Salir disparada hacia afuera (eje Y local o Z local según tu modelo)
        rb.AddForce(transform.up * popForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);

        // Cambiar capa para no interactuar más
        gameObject.layer = LayerMask.NameToLayer("Default");
        
        // Autodestrucción
        Destroy(gameObject, 3.0f);
        
        // Desactivar este script
        this.enabled = false;
    }
}