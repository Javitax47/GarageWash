using System.Collections.Generic;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public class PowerWasherController : MonoBehaviour
{
    private PaintableSurface currentTargetSurface;
    [Header("Referencias de la Curva")]
    [SerializeField] private Transform curveControlPointsParent;
    [Tooltip("Puntos de control que definen la trayectoria. Arrástralos aquí en orden.")]
    [SerializeField] private Transform[] controlPoints;
    [SerializeField] private LayerMask paintableLayer;
    
    [Header("Detalle de la Curva")]
    [Tooltip("Cuántos puntos intermedios generar entre cada par de puntos de control.")]
    [SerializeField, Range(2, 50)] private int curveDetail = 10;

    [Header("Configuración de Puntería")]
    [SerializeField] private float aimSpeed = 5f;

    [Header("Configuración del Chorro (Spray Focus)")]
    [Tooltip("Sensibilidad del deslizamiento vertical para cambiar el enfoque del chorro.")]
    [SerializeField] private float focusControlSensitivity = 0.1f;
    [Space]
    [Tooltip("Ángulo del cono de partículas en el modo más ancho (foco = 0).")]
    [SerializeField] private float maxConeAngle = 15f;
    [Tooltip("Ángulo del cono de partículas en el modo más concentrado (foco = 1).")]
    [SerializeField] private float minConeAngle = 1f;
    [Space]
    [Tooltip("Radio de limpieza principal en el modo más ancho.")]
    [SerializeField] private float maxJetRadius = 0.25f;
    [Tooltip("Radio de limpieza principal en el modo más concentrado.")]
    [SerializeField] private float minJetRadius = 0.05f;
    [Space]
    
    // --- ¡NUEVOS TOOLTIPS Y RANGO NORMALIZADO! ---
    [Tooltip("Fuerza de limpieza (0.1 = ~1 segundo para limpiar, 1.0 = instantáneo) en el modo más ancho.")]
    [SerializeField] private float minJetStrength = 0.4f;
    [Tooltip("Fuerza de limpieza (0.1 = ~1 segundo para limpiar, 1.0 = instantáneo) en el modo más concentrado.")]
    [SerializeField] private float maxJetStrength = 1.0f;
    [Tooltip("Tiempo en segundos durante el cual el chorro principal limpia incondicionalmente al empezar a lavar.")]
    public float cleaningGracePeriod = 0.3f;
    
    // --- Variables Privadas de Estado ---
    private float calculatedOptimalDistance;
    private float calculatedMaxDistance;
    private readonly List<Vector3> highResolutionCurvePoints = new List<Vector3>();
    private Camera mainCamera;
    private ParticleSystem[] powerWashers;
    private bool isWashing = false;
    private LineRenderer lineRenderer;
    private float sprayFocus = 0.5f;
    private float currentJetRadius;
    private float currentJetStrength;
    private float washStartTime = -1f;
    private Vector3 lastMainJetHitPoint = Vector3.positiveInfinity; // Para detectar el movimiento de la mira
    private float newTargetGracePeriodTimer = 0f; // El temporizador del nuevo periodo de gracia
    private bool hasFiredWashingStarted = false;

    void Awake()
    {
        mainCamera = Camera.main;
        powerWashers = GetComponentsInChildren<ParticleSystem>();
        lineRenderer = GetComponentInChildren<LineRenderer>();

        if (controlPoints == null || controlPoints.Length < 2)
        {
            this.enabled = false;
            return;
        }
        
        GenerateHighResolutionCurve();
        CalculateCurveDistances();
        ApplySprayFocus();
    }

    void OnEnable() { SetWashing(false); }
    
    void Update()
    {
        HandleWashingInput();
        AimWasher();
        if (isWashing) 
        { 
            ApplyMainJetCleaning(); 
        }
    }

    private void HandleWashingInput()
    {
        bool isHolding = (Mouse.current != null && Mouse.current.leftButton.isPressed) ||
                         (Touchscreen.current != null && Touchscreen.current.press.isPressed);

        if (isHolding)
        {
            // La lógica para ajustar el foco del spray se mantiene,
            // pero ya no dispara ningún evento de tutorial.
            float deltaY = 0;
            var activeTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
            if (activeTouches.Count > 0)
                deltaY = activeTouches[0].delta.y;
            else if (Mouse.current != null)
                deltaY = Mouse.current.delta.ReadValue().y;
                
            if (Mathf.Abs(deltaY) > 0.1f)
            {
                sprayFocus += deltaY * focusControlSensitivity * Time.deltaTime;
                sprayFocus = Mathf.Clamp01(sprayFocus);
                ApplySprayFocus();
            }
        }

        if (isHolding != isWashing)
        {
            SetWashing(isHolding);
        }
    }

    private void SetWashing(bool shouldWash)
    {
        isWashing = shouldWash;

        if (isWashing)
        {
            washStartTime = Time.time;
            if (!hasFiredWashingStarted)
            {
                hasFiredWashingStarted = true;
                TutorialEvents.FireOnWashingStarted();
            }

            AudioManager.Instance.StartWashingLoop();
        }
        else
        {
            // --- ¡LLAMADA SIMPLIFICADA! ---
            // Ya no necesita saber si el temporizador está activo.
            AudioManager.Instance.StopWashingLoop();
        }

        if (powerWashers != null)
        {
            foreach (var ps in powerWashers)
            {
                var emission = ps.emission;
                emission.enabled = isWashing;
            }
        }
    }
    
    private void ApplySprayFocus()
    {
        float newConeAngle = Mathf.Lerp(maxConeAngle, minConeAngle, sprayFocus);
        currentJetRadius = Mathf.Lerp(maxJetRadius, minJetRadius, sprayFocus);
        currentJetStrength = Mathf.Lerp(minJetStrength, maxJetStrength, sprayFocus);

        if (powerWashers != null)
        {
            foreach (var ps in powerWashers)
            {
                var shape = ps.shape;
                shape.angle = newConeAngle;
            }
        }
    }

    private void AimWasher()
    {
        UpdateLineRenderer();
        Ray targetRay = mainCamera.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(targetRay, out RaycastHit targetHit, 100f, paintableLayer))
        {
            targetHit.collider.TryGetComponent(out currentTargetSurface);
            Vector3 targetPoint = targetHit.point;
            bool curveIntersects = FindCurveIntersection(out Vector3 currentIntersection, out RaycastHit _);
            
            Quaternion targetRotation;
            if (curveIntersects)
                targetRotation = Quaternion.LookRotation(transform.forward + (targetPoint - currentIntersection));
            else
                targetRotation = Quaternion.LookRotation(transform.forward + (targetPoint - transform.TransformPoint(highResolutionCurvePoints[highResolutionCurvePoints.Count - 1])));
            
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * aimSpeed);
        }
    }

    private void ApplyMainJetCleaning()
    {
        if (FindCurveIntersection(out Vector3 finalIntersection, out RaycastHit finalIntersectionHit))
        {
            if (finalIntersectionHit.collider.TryGetComponent(out PaintableSurface surface))
            {
                // --- ¡NUEVA LÓGICA DE DETECCIÓN DE MOVIMIENTO! ---
                // Si nos hemos movido a una nueva zona, reiniciamos el temporizador de gracia.
                if (Vector3.Distance(finalIntersection, lastMainJetHitPoint) > currentJetRadius)
                {
                    newTargetGracePeriodTimer = cleaningGracePeriod;
                }
                lastMainJetHitPoint = finalIntersection; // Actualizamos el último punto

                // Actualizamos el temporizador
                if (newTargetGracePeriodTimer > 0)
                {
                    newTargetGracePeriodTimer -= Time.deltaTime;
                }
                
                // --- CONDICIÓN DE LIMPIEZA FINAL Y ROBUSTA ---
                bool inInitialGracePeriod = Time.time - washStartTime < cleaningGracePeriod;
                bool inNewTargetGracePeriod = newTargetGracePeriodTimer > 0;
                bool particlesAreHittingNearby = AreParticlesHittingNearby(surface, finalIntersection);

                // Limpiamos si se cumple CUALQUIERA de estas condiciones
                if (!inInitialGracePeriod && !inNewTargetGracePeriod && !particlesAreHittingNearby)
                {
                    return;
                }
                
                // Si la condición se cumple, procedemos con la limpieza
                float distance = Vector3.Distance(this.transform.position, finalIntersection);
                float distanceFactor = Mathf.InverseLerp(calculatedMaxDistance, calculatedOptimalDistance, distance);
                if(distanceFactor <= 0) return;

                float dynamicStrength = (currentJetStrength >= 1.0f) ? 1.0f : (currentJetStrength / 0.1f) * Time.deltaTime;
                dynamicStrength *= distanceFactor;
                
                Color mainCleaningColor = new Color(0, 0, 0, dynamicStrength);
                surface.Paint(finalIntersection, finalIntersectionHit.normal, currentJetRadius, mainCleaningColor);
            }
        }
        else
        {
            // Si la curva no choca, reseteamos el punto para que el próximo impacto sea "nuevo"
            lastMainJetHitPoint = Vector3.positiveInfinity;
        }
    }
    
    // --- ¡NUEVA FUNCIÓN DE COMPROBACIÓN DE PROXIMIDAD! ---
    private bool AreParticlesHittingNearby(PaintableSurface surface, Vector3 checkPosition)
    {
        // Si no tenemos una superficie válida o no hay colisiones de partículas, devolvemos false
        if (surface == null || surface.LastParticleCollisionPoints.Count == 0)
        {
            return false;
        }

        // Comprobamos si alguna de las últimas colisiones de partículas está cerca de nuestro chorro principal
        foreach (var particleHitPoint in surface.LastParticleCollisionPoints)
        {
            // Usamos el radio del chorro como umbral de proximidad
            if (Vector3.Distance(particleHitPoint, checkPosition) < currentJetRadius * 2f)
            {
                return true; // Encontramos una partícula cercana, ¡podemos limpiar!
            }
        }

        // No se encontró ninguna partícula cercana
        return false;
    }

    private bool FindCurveIntersection(out Vector3 intersectionPoint, out RaycastHit hitInfo)
    {
        for (int i = 0; i < highResolutionCurvePoints.Count - 1; i++)
        {
            Vector3 startPoint = transform.TransformPoint(highResolutionCurvePoints[i]);
            Vector3 endPoint = transform.TransformPoint(highResolutionCurvePoints[i + 1]);
            
            if (Physics.Linecast(startPoint, endPoint, out hitInfo, paintableLayer))
            {
                intersectionPoint = hitInfo.point;
                return true;
            }
        }
        intersectionPoint = Vector3.zero;
        hitInfo = new RaycastHit();
        return false;
    }

    private void GenerateHighResolutionCurve()
    {
        highResolutionCurvePoints.Clear();
        for (int i = 0; i < controlPoints.Length - 1; i++)
        {
            Vector3 p0 = i == 0 ? controlPoints[i].localPosition : controlPoints[i - 1].localPosition;
            Vector3 p1 = controlPoints[i].localPosition;
            Vector3 p2 = controlPoints[i + 1].localPosition;
            Vector3 p3 = i == controlPoints.Length - 2 ? controlPoints[i + 1].localPosition : controlPoints[i + 2].localPosition;
            
            for (int j = 0; j < curveDetail; j++)
            {
                float t = (float)j / curveDetail;
                highResolutionCurvePoints.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }
        highResolutionCurvePoints.Add(controlPoints[controlPoints.Length - 1].localPosition);
    }
    
    public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * ((2.0f * p1) + (-p0 + p2) * t + (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * t2 + (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * t3);
    }
    
    private void CalculateCurveDistances()
    {
        if (highResolutionCurvePoints.Count < 2) return;
        calculatedMaxDistance = 0f;
        for (int i = 0; i < highResolutionCurvePoints.Count - 1; i++)
        {
            calculatedMaxDistance += Vector3.Distance(highResolutionCurvePoints[i], highResolutionCurvePoints[i + 1]);
        }
        calculatedOptimalDistance = calculatedMaxDistance * 0.25f;
    }

    private void UpdateLineRenderer()
    {
        if (lineRenderer == null) return;
        lineRenderer.positionCount = highResolutionCurvePoints.Count;
        for (int i = 0; i < highResolutionCurvePoints.Count; i++)
        {
            lineRenderer.SetPosition(i, transform.TransformPoint(highResolutionCurvePoints[i]));
        }
    }

#if UNITY_EDITOR
    private void OnValidate() { this.ValidateRefs(); }
#endif
}