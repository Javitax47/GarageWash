using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class PowerWashController : MonoBehaviour
{
    public enum HandType { LeftHand, RightHand }

    [Header("---- Configuración VR ----")]
    public Transform muzzlePoint;
    public HandType handSide = HandType.RightHand;
    public LayerMask paintableLayer;

    [Header("---- Configuración de Herramienta ----")]
    public PaintableSurface.DirtType dirtTypeToClean = PaintableSurface.DirtType.TypeA_Red;

    [Header("---- Balística del Agua ----")]
    public float waterSpeed = 15.0f;
    public float gravityModifier = 1.0f;
    public float projectilesPerSecond = 40f; 
    
    [Tooltip("Tamaño físico del proyectil (para colisiones). Ponlo MUY pequeño (0.01) para entrar en grietas.")]
    public float collisionRadius = 0.01f;

    [Header("---- Configuración de Limpieza ----")]
    public float focusSensitivity = 1.0f;
    public float maxConeAngle = 15f; 
    public float minConeAngle = 0f;
    
    // Radios de la mancha de pintura
    public float maxJetRadius = 0.15f;
    public float minJetRadius = 0.05f;
    
    public float minJetStrength = 0.05f; 
    public float maxJetStrength = 0.2f;

    [Header("---- Referencias ----")]
    [Tooltip("Arrastra aquí TODOS los sistemas de partículas (Chorro principal, niebla, etc)")]
    public ParticleSystem[] waterParticles; 

    // --- Estado Interno ---
    private InputAction triggerAction;
    private InputAction thumbstickAction;
    private AudioSource audioSource;
    
    private bool isWashing = false;
    private bool isGrabbed = false;
    private float sprayFocus = 1f; 
    
    private float currentConeAngle;
    private float currentJetRadius;
    private float currentJetStrength;
    
    private float fireTimer = 0f;
    private float timeBetweenShots;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        string handStr = (handSide == HandType.RightHand) ? "RightHand" : "LeftHand";
        
        triggerAction = new InputAction(type: InputActionType.Value, binding: $"<XRController>{{{handStr}}}/trigger");
        thumbstickAction = new InputAction(type: InputActionType.Value, binding: $"<XRController>{{{handStr}}}/thumbstick/y");

        ApplySprayFocus();
    }

    private void OnEnable() { triggerAction.Enable(); thumbstickAction.Enable(); }
    private void OnDisable() { triggerAction.Disable(); thumbstickAction.Disable(); SetWashing(false); }

    private void Update()
    {
        if (!isGrabbed) 
        {
            if (isWashing) SetWashing(false);
            return;
        }
        
        HandleInput();
        
        if (isWashing)
        {
            HandleShooting();
        }
    }

    private void HandleInput()
    {
        float triggerValue = triggerAction.ReadValue<float>();
        bool isTriggerPulled = triggerValue > 0.1f;
        SetWashing(isTriggerPulled);

        if (thumbstickAction != null && thumbstickAction.enabled)
        {
            float stickValue = thumbstickAction.ReadValue<float>();
            if (Mathf.Abs(stickValue) > 0.1f)
            {
                sprayFocus += stickValue * focusSensitivity * Time.deltaTime;
                sprayFocus = Mathf.Clamp01(sprayFocus);
                ApplySprayFocus();
            }
        }
    }

    private void SetWashing(bool active)
    {
        if (isWashing == active) return;
        isWashing = active;

        if (isWashing)
        {
            timeBetweenShots = 1.0f / projectilesPerSecond;
            
            // Activar todas las partículas
            if (waterParticles != null)
            {
                foreach (var ps in waterParticles) if(ps != null) ps.Play();
            }

            if (audioSource != null) audioSource.Play();
            if (AudioManager.Instance != null) AudioManager.Instance.StartWashingLoop();
        }
        else
        {
            // Parar todas las partículas
            if (waterParticles != null)
            {
                foreach (var ps in waterParticles) if(ps != null) ps.Stop();
            }

            if (audioSource != null) audioSource.Stop();
            if (AudioManager.Instance != null) AudioManager.Instance.StopWashingLoop();
        }
    }

    private void ApplySprayFocus()
    {
        currentConeAngle = Mathf.Lerp(maxConeAngle, minConeAngle, sprayFocus);
        currentJetRadius = Mathf.Lerp(maxJetRadius, minJetRadius, sprayFocus);
        currentJetStrength = Mathf.Lerp(minJetStrength, maxJetStrength, sprayFocus);

        // Aplicar ángulo a todos los sistemas de partículas
        if (waterParticles != null)
        {
            foreach (var ps in waterParticles)
            {
                if (ps != null)
                {
                    var shape = ps.shape;
                    // Solo aplicamos ángulo si es tipo Cono para no romper efectos de niebla fijos
                    if (shape.shapeType == ParticleSystemShapeType.Cone)
                    {
                        shape.angle = currentConeAngle;
                    }
                }
            }
        }
    }

    private void HandleShooting()
    {
        fireTimer += Time.deltaTime;
        while (fireTimer >= timeBetweenShots)
        {
            SpawnProjectile();
            fireTimer -= timeBetweenShots;
        }
    }

    private void SpawnProjectile()
    {
        // Nota: En producción, usar Object Pooling. Aquí instanciamos por simplicidad.
        GameObject bulletObj = new GameObject("WaterBullet");
        bulletObj.transform.position = muzzlePoint.position;
        
        Vector3 direction = muzzlePoint.forward;
        if (currentConeAngle > 0)
        {
            float spread = Random.Range(0f, currentConeAngle);
            Quaternion rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), muzzlePoint.forward) * Quaternion.AngleAxis(spread, muzzlePoint.up);
            direction = rotation * muzzlePoint.forward;
        }

        WaterProjectile projectile = bulletObj.AddComponent<WaterProjectile>();
        
        // Pasamos AMBOS RADIOS
        projectile.Initialize(
            direction, 
            waterSpeed, 
            gravityModifier, 
            collisionRadius, // Pequeño (Física)
            currentJetRadius, // Grande (Pintura)
            currentJetStrength, 
            dirtTypeToClean, 
            paintableLayer
        );
    }

    public void OnGrab() => isGrabbed = true;
    public void OnRelease() { isGrabbed = false; SetWashing(false); }
}