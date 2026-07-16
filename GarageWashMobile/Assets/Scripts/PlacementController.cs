using UnityEngine;
using UnityEngine.UI;
using Vuforia;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public class PlacementController : MonoBehaviour
{
    [Header("Referencias de Prefabs")]
    public GameObject previewPrefab;
    public GameObject finalPrefab;

    [Header("UI Crosshair")]
    public UnityEngine.UI.Image crosshairImage;
    public Color validSurfaceColor = Color.green;
    public Color invalidSurfaceColor = Color.white;
    
    [Header("Configuración de Interacción")]
    public float doubleTapThreshold = 0.3f;
    public float minScale = 0.2f;
    public float maxScale = 3.0f;
    
    [Header("Sensibilidad de Gestos")]
    public float rotationSpeed = 0.5f;
    public float editorRotationSpeed = 0.5f;
    public float editorScaleSpeed = 0.01f;

    // --- Variables de Estado ---
    private GameObject previewInstance;
    private bool hasPlacedObject = false;
    private Plane detectedPlane;
    private bool isPlaneFound = false;
    private float lastTapTime = 0f;
    private float currentPreviewScale = 1.0f;
    private float currentPreviewRotationY = 0.0f;
    private PowerWasherController powerWasherController;
    private bool hasFiredSurfaceFound = false;
    private bool hasFiredScaleOrRotate = false;

#if !UNITY_EDITOR
    private PlaneFinderBehaviour planeFinder;
    private Vector2 touchZeroPrevPos;
    private Vector2 touchOnePrevPos;
#endif

    void OnEnable() { EnhancedTouchSupport.Enable(); }
    void OnDisable() { EnhancedTouchSupport.Disable(); }

    

    void Start()
    {
        // Buscamos el controlador y lo desactivamos al inicio
        if (Camera.main != null)
        {
            powerWasherController = Camera.main.GetComponentInChildren<PowerWasherController>(true); // 'true' lo encuentra aunque esté inactivo
            if (powerWasherController != null)
            {
                powerWasherController.enabled = false;
            }
            else
            {
                Debug.LogError("¡No se encontró el PowerWasherController en la ARCamera!");
            }
        }
        
        #if !UNITY_EDITOR
        planeFinder = FindAnyObjectByType<PlaneFinderBehaviour>();
        if(planeFinder != null) planeFinder.OnInteractiveHitTest.AddListener(OnHitTestResult);
        #endif

        if (previewPrefab != null)
        {
            previewInstance = Instantiate(previewPrefab);
            previewInstance.SetActive(false);
        }
    }

    void Update()
    {
        // El Update vuelve a ser simple: si el objeto está colocado, este script ya no hace nada.
        if (hasPlacedObject)
        {
            return;
        }
        
        // Lógica de fase de colocación
        #if UNITY_EDITOR
        SimulatePlaneDetectionInEditor();
        #else
        planeFinder.PerformHitTest(new Vector2(0.5f, 0.5f));
        #endif

        UpdatePreviewTransform();
        HandlePlacementInputs();
    }

    // --- SE HAN ELIMINADO LAS FUNCIONES HandleWashingInput y ToggleWashing DE AQUÍ ---

    private void PlaceObject(Vector3 position, Quaternion rotation, float scale)
    {
        GameObject finalObject = null;
#if UNITY_EDITOR
    finalObject = Instantiate(finalPrefab, position, rotation);
    finalObject.transform.localScale = Vector3.one * scale;
    finalObject.SetActive(true);
#else
        // Esta lógica ahora es más robusta para Android también
        finalObject = Instantiate(finalPrefab, position, rotation);
        finalObject.transform.localScale = Vector3.one * scale;
        finalObject.SetActive(true);
        finalObject.AddComponent<AnchorBehaviour>();
#endif
        AudioManager.Instance.PlayPlaceObject();
        TutorialEvents.FireOnObjectPlaced();

        // --- ¡NUEVA LÓGICA DE CONEXIÓN CON LOGS DE DEPURACIÓN! ---
        if (finalObject != null)
        {
            Debug.Log("[Placement] Objeto final instanciado. Buscando UIManager...");
            // Usamos FindAnyObjectByType para evitar la advertencia
            var uiManager = FindAnyObjectByType<CleanProgressUI>();

            if (uiManager == null)
            {
                Debug.LogError("[Placement] ¡ERROR! No se encontró el CleaningUIManager en la escena. Asegúrate de que un objeto lo tiene como componente.");
            }
            else
            {
                Debug.Log("[Placement] UIManager encontrado. Buscando PaintableSurface en el objeto final...");
                var paintableSurface = finalObject.GetComponentInChildren<PaintableSurface>();

                if (paintableSurface == null)
                {
                    Debug.LogError("[Placement] ¡ERROR! El objeto final instanciado NO tiene el componente PaintableSurface.");
                }
                else
                {
                    Debug.Log("[Placement] ¡ÉXITO! PaintableSurface encontrado. Iniciando seguimiento de la limpieza.");
                    // Le decimos a la UI que empiece a seguir el progreso de esta superficie
                    uiManager.StartTracking(paintableSurface);
                }
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
        }

        hasPlacedObject = true;
        if (previewInstance != null) Destroy(previewInstance);
        if (crosshairImage != null)
        {
            crosshairImage.color = invalidSurfaceColor;
        }

#if !UNITY_EDITOR
        if (planeFinder != null) planeFinder.enabled = false;
#endif

        Debug.Log("Objeto colocado. Mantén pulsada la pantalla para limpiar.");
    }
    
    #region Funciones Auxiliares (Sin Cambios)

    #if UNITY_EDITOR
    void SimulatePlaneDetectionInEditor()
    {
        Ray ray = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("SimulatedPlane")))
        {
            isPlaneFound = true;
            detectedPlane = new Plane(hit.normal, hit.point);
        }
        else { isPlaneFound = false; }
    }
    #endif

    public void OnHitTestResult(HitTestResult result)
    {
        if (result != null)
        {
            isPlaneFound = true;
            detectedPlane = new Plane(result.Rotation * Vector3.up, result.Position);
        }
    }

    private void UpdatePreviewTransform()
    {
        if (!isPlaneFound) { HidePreview(); return; }
        Ray cameraRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (detectedPlane.Raycast(cameraRay, out float enter))
        {
            Vector3 targetPosition = cameraRay.GetPoint(enter);
            if (previewInstance != null)
            {
                previewInstance.SetActive(true);
                if (!hasFiredSurfaceFound)
                {
                    hasFiredSurfaceFound = true;
                    TutorialEvents.FireOnSurfaceFound();
                }
                previewInstance.transform.position = targetPosition;
                previewInstance.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(cameraRay.direction, detectedPlane.normal), detectedPlane.normal) * Quaternion.Euler(0, currentPreviewRotationY, 0);
                previewInstance.transform.localScale = Vector3.one * currentPreviewScale;
            }
            if (crosshairImage != null) crosshairImage.color = validSurfaceColor;
        }
        else { HidePreview(); }
    }

    private void HidePreview()
    {
        if (previewInstance != null) previewInstance.SetActive(false);
        if (crosshairImage != null) crosshairImage.color = invalidSurfaceColor;
    }

    private void HandlePlacementInputs()
    {
#if UNITY_EDITOR
    if (previewInstance.activeSelf)
    {
        if (Keyboard.current.altKey.isPressed && Mouse.current.leftButton.isPressed)
        {
            SimulateGesturesWithMouse();
            // --- ¡LÍNEA RE-AÑADIDA! ---
            if (!hasFiredScaleOrRotate) { hasFiredScaleOrRotate = true; TutorialEvents.FireOnObjectScaledOrRotated(); }
        }
        else if (Mouse.current.leftButton.wasPressedThisFrame)
            HandleDoubleTap(null);
    }
#else
        var activeTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
        if (previewInstance.activeSelf && activeTouches.Count == 2)
        {
            HandleMobileGestures(activeTouches[0], activeTouches[1]);
            // --- ¡LÍNEA RE-AÑADIDA! ---
            if (!hasFiredScaleOrRotate) { hasFiredScaleOrRotate = true; TutorialEvents.FireOnObjectScaledOrRotated(); }
        }
        else if (activeTouches.Count == 1)
            HandleDoubleTap(activeTouches[0]);
#endif
    }
    
    private void HandleDoubleTap(UnityEngine.InputSystem.EnhancedTouch.Touch? touch)
    {
        if (touch.HasValue && touch.Value.phase != UnityEngine.InputSystem.TouchPhase.Began) return;
        if (Time.unscaledTime - lastTapTime < doubleTapThreshold)
            PlaceObject(previewInstance.transform.position, previewInstance.transform.rotation, currentPreviewScale);
        lastTapTime = Time.unscaledTime;
    }

    #if UNITY_EDITOR
    private void SimulateGesturesWithMouse()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        currentPreviewRotationY -= mouseDelta.x * editorRotationSpeed;
        currentPreviewScale += mouseDelta.y * editorScaleSpeed;
        currentPreviewScale = Mathf.Clamp(currentPreviewScale, minScale, maxScale);
    }
    #endif

    #if !UNITY_EDITOR
    private void HandleMobileGestures(UnityEngine.InputSystem.EnhancedTouch.Touch touchZero, UnityEngine.InputSystem.EnhancedTouch.Touch touchOne)
    {
        if (touchZero.phase == UnityEngine.InputSystem.TouchPhase.Began || touchOne.phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            touchZeroPrevPos = touchZero.screenPosition;
            touchOnePrevPos = touchOne.screenPosition;
            return;
        }
        if (touchZero.phase == UnityEngine.InputSystem.TouchPhase.Moved || touchOne.phase == UnityEngine.InputSystem.TouchPhase.Moved)
        {
            Vector2 tzc = touchZero.screenPosition;
            Vector2 toc = touchOne.screenPosition;
            float prevMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float curMag = (tzc - toc).magnitude;
            if (prevMag > 0)
            {
                currentPreviewScale *= (curMag / prevMag);
                currentPreviewScale = Mathf.Clamp(currentPreviewScale, minScale, maxScale);
            }
            Vector2 prevDir = (touchOnePrevPos - touchZeroPrevPos).normalized;
            Vector2 curDir = (toc - tzc).normalized;
            float angle = Vector2.SignedAngle(prevDir, curDir);
            if (!float.IsNaN(angle))
            {
                currentPreviewRotationY -= angle * rotationSpeed;
            }
            touchZeroPrevPos = tzc;
            touchOnePrevPos = toc;
        }
    }
    #endif
    
    void OnDestroy()
    {
        #if !UNITY_EDITOR
        if (planeFinder != null) 
            planeFinder.OnInteractiveHitTest.RemoveListener(OnHitTestResult);
        #endif
    }
    
    #endregion
}