using UnityEngine;
using UnityEngine.InputSystem; // Importante: Usa el sistema nuevo

public class HandMenu : MonoBehaviour
{
    [Header("Arrastra aquí tu Canvas")]
    public GameObject wristCanvas;

    // Esta variable crea la acción internamente, sin necesitar assets externos
    private InputAction botonMenuAction;

    private void Awake()
    {
        // DEFINIMOS LA ACCIÓN POR CÓDIGO (Truco para evitar buscar referencias)
        // "<XRController>{LeftHand}/primaryButton" es la dirección interna de la 'X' en Oculus/Meta
        botonMenuAction = new InputAction(type: InputActionType.Button, binding: "<XRController>{LeftHand}/primaryButton");
    }

    private void OnEnable()
    {
        botonMenuAction.Enable(); // Encendemos la escucha
    }

    private void OnDisable()
    {
        botonMenuAction.Disable(); // La apagamos al salir
    }

    private void Update()
    {
        // Si el botón se pulsó en este frame...
        if (botonMenuAction.WasPressedThisFrame())
        {   
            if (wristCanvas != null)
            {
                wristCanvas.SetActive(!wristCanvas.activeSelf);
            }
        }
    }
}