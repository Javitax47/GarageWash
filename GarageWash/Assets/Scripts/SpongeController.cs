using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MagneticSponge : MonoBehaviour
{
    [Header("Referencias")]
    public Transform spongeVisual; 
    public Transform anchorPoint; 
    public LayerMask paintableLayer;

    [Header("Configuración")]
    public float searchRadius = 0.3f; 
    public float snapDistance = 0.2f;
    public float smoothSpeed = 25f;

    [Header("Limpieza")]
    public PaintableSurface.DirtType dirtType = PaintableSurface.DirtType.TypeB_Green;
    public float cleanRadius = 0.15f;
    public float cleanStrength = 5.0f;

    [Header("Feedback")]
    public AudioSource rubSound;
    public ParticleSystem bubbles;

    [Header("DEBUG")]
    public bool showDebugLines = true; // Actívalo en el inspector

    private bool isSnapped = false;
    private bool isGrabbed = false;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;
    private Collider[] hitColliders = new Collider[5]; 
    private Vector3 lastHitPos;

    private void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnGrab);
        interactable.selectExited.AddListener(OnRelease);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnGrab);
        interactable.selectExited.RemoveListener(OnRelease);
    }

    private void Update()
    {
        if (!isGrabbed)
        {
            ResetVisuals();
            return;
        }

        // 1. Buscando coche cerca...
        int numHits = Physics.OverlapSphereNonAlloc(anchorPoint.position, searchRadius, hitColliders, paintableLayer);
        
        Collider closestCollider = null;
        Vector3 closestPoint = Vector3.zero;
        float shortestDist = float.MaxValue;

        // Estrategia segura para MeshColliders
        for (int i = 0; i < numHits; i++)
        {
            Collider col = hitColliders[i];
            
            // Truco para MeshCollider: Usamos bounds o centro
            Vector3 pointOnBounds = col.bounds.ClosestPoint(anchorPoint.position);
            Vector3 dirToObj = (pointOnBounds - anchorPoint.position).normalized;
            if (dirToObj == Vector3.zero) dirToObj = anchorPoint.forward;

            RaycastHit hitInfo;
            if (col.Raycast(new Ray(anchorPoint.position, dirToObj), out hitInfo, searchRadius * 2f))
            {
                if (hitInfo.distance < shortestDist)
                {
                    shortestDist = hitInfo.distance;
                    closestCollider = col;
                    closestPoint = hitInfo.point;
                }
            }
        }

        // Lógica Snap
        if (closestCollider != null && shortestDist <= snapDistance) isSnapped = true;
        else if (closestCollider == null || shortestDist > searchRadius) isSnapped = false;

        if (isSnapped && closestCollider != null)
        {
            // Calculamos normal aproximada para orientar
            Vector3 approxNormal = (anchorPoint.position - closestPoint).normalized;
            SnapAndClean(closestCollider, closestPoint, approxNormal);
        }
        else
        {
            ResetVisualsSmooth();
            UpdateFeedback(false, 0);
        }
    }

    private void SnapAndClean(Collider targetCollider, Vector3 targetPoint, Vector3 targetNormal)
    {
        // --- A. VISUALES ---
        Vector3 visualPos = targetPoint + (targetNormal * 0.02f);
        Vector3 handProj = Vector3.ProjectOnPlane(anchorPoint.forward, targetNormal).normalized;
        if (handProj == Vector3.zero) handProj = anchorPoint.up;
        Quaternion visualRot = Quaternion.LookRotation(handProj, targetNormal);
        visualRot *= Quaternion.Euler(90, 0, 0); 
        
        spongeVisual.position = Vector3.Lerp(spongeVisual.position, visualPos, Time.deltaTime * smoothSpeed);
        spongeVisual.rotation = Quaternion.Slerp(spongeVisual.rotation, visualRot, Time.deltaTime * smoothSpeed);

        // --- B. LIMPIEZA (RAYO DE CONFIRMACIÓN) ---
        
        // Origen: Un poco alejado de la esponja visual hacia afuera (para no nacer dentro del coche)
        Vector3 rayOrigin = spongeVisual.position + (targetNormal * 0.1f);
        Vector3 rayDir = -targetNormal; // Hacia el coche

        RaycastHit cleanHit;
        
        // DEBUG: Dibujamos el intento de rayo
        if (showDebugLines) Debug.DrawRay(rayOrigin, rayDir * 0.2f, Color.cyan);

        if (targetCollider.Raycast(new Ray(rayOrigin, rayDir), out cleanHit, 0.3f))
        {
            // ¡IMPACTO FÍSICO!
            if (showDebugLines) Debug.DrawLine(rayOrigin, cleanHit.point, Color.green); // Verde = Toca

            PaintableSurface surface = cleanHit.collider.GetComponentInParent<PaintableSurface>();
            
            if (surface != null)
            {
                // DEBUG IMPORTANTE: Chequeo de UVs
                if (cleanHit.textureCoord == Vector2.zero)
                {
                    // Si sale este mensaje, el problema es el COLLIDER CONVEXO
                    if(Time.frameCount % 60 == 0) 
                        Debug.LogError($"[SPONGE ERROR] Toco {cleanHit.collider.name} pero las UVs son (0,0). ¿Tiene 'Convex' activado? Desactívalo.");
                }
                else
                {
                    // TODO OK: Limpiando
                    surface.Paint(cleanHit.point, cleanHit.normal, cleanRadius, cleanStrength * Time.deltaTime, dirtType);
                    
                    float speed = (cleanHit.point - lastHitPos).magnitude / Time.deltaTime;
                    UpdateFeedback(true, speed);
                    lastHitPos = cleanHit.point;
                }
            }
            else
            {
                // Toca collider pero no tiene script
                if(Time.frameCount % 60 == 0) 
                    Debug.LogWarning($"[SPONGE WARN] Toco {cleanHit.collider.name} pero no tiene script PaintableSurface.");
            }
        }
        else
        {
            // El rayo falló (Rojo)
            if (showDebugLines) Debug.DrawLine(rayOrigin, rayOrigin + rayDir * 0.3f, Color.red);
        }
    }

    // ... (Resto de métodos ResetVisuals, Feedback, etc. IGUAL QUE ANTES) ...
    private void ResetVisuals() { spongeVisual.position = anchorPoint.position; spongeVisual.rotation = anchorPoint.rotation; UpdateFeedback(false, 0); }
    private void ResetVisualsSmooth() { spongeVisual.position = Vector3.Lerp(spongeVisual.position, anchorPoint.position, Time.deltaTime * smoothSpeed); spongeVisual.rotation = Quaternion.Slerp(spongeVisual.rotation, anchorPoint.rotation, Time.deltaTime * smoothSpeed); }
    private void UpdateFeedback(bool active, float speed) { if (rubSound != null) { if (active && speed > 0.1f) { if (!rubSound.isPlaying) rubSound.Play(); rubSound.volume = Mathf.Clamp01(speed); } else rubSound.Stop(); } if (bubbles != null) { if (active) { if (!bubbles.isPlaying) bubbles.Play(); } else bubbles.Stop(); } if (active && speed > 0.1f) TriggerHaptic(speed * 0.05f); }
    private void TriggerHaptic(float amplitude) { if (interactable != null && interactable.isSelected && interactable.firstInteractorSelecting is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor controller) controller.SendHapticImpulse(amplitude, 0.1f); }
    private void OnGrab(SelectEnterEventArgs args) { isGrabbed = true; }
    private void OnRelease(SelectExitEventArgs args) { isGrabbed = false; isSnapped = false; }
}