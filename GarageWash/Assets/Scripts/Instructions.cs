using UnityEngine;

public class ObjetoInfoFijo : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject canvasInfo;   // Arrastra aquí tu Canvas fijo

    // Variables para recordar dónde "vivía" el objeto
    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;
    private Rigidbody rb;

    void Start()
    {
        // Guardamos la posición original al arrancar el juego
        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;
        rb = GetComponent<Rigidbody>();

        // Aseguramos que el panel informativo empiece oculto
        if (canvasInfo != null) canvasInfo.SetActive(false);
    }

    // --- AL COGERLO ---
    public void MostrarInfo()
    {
        // Simplemente encendemos el Canvas. 
        // Aparecerá donde tú lo hayas colocado en la escena.
        if (canvasInfo != null)
        {
            canvasInfo.SetActive(true);
        }
    }

    // --- AL SOLTARLO ---
    public void ResetearPosicion()
    {
        // 1. Apagamos el Canvas
        if (canvasInfo != null)
        {
            canvasInfo.SetActive(false);
        }

        // 2. Reseteamos físicas para que se quede quieto al volver
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 3. Teletransportamos el objeto a su origen
        transform.position = posicionInicial;
        transform.rotation = rotacionInicial;
    }
}