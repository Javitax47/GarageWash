using UnityEngine;

public class WaterProjectile : MonoBehaviour
{
    // Datos
    private float speed;
    private float gravityModifier;
    
    private float physicalRadius; // Pequeño (para colisiones)
    private float cleaningRadius; // Grande (para pintar)
    
    private float strength;
    private PaintableSurface.DirtType dirtType;
    private LayerMask layerMask;

    // Estado
    private Vector3 velocity;
    private float maxLifetime = 2.0f;
    private float lifetime = 0f;

    public void Initialize(Vector3 startDir, float speed, float gravity, float colRadius, float paintRadius, float strength, PaintableSurface.DirtType type, LayerMask layers)
    {
        this.speed = speed;
        this.gravityModifier = gravity;
        this.physicalRadius = colRadius;
        this.cleaningRadius = paintRadius;
        this.strength = strength;
        this.dirtType = type;
        this.layerMask = layers;

        this.velocity = startDir * speed;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        lifetime += dt;

        if (lifetime > maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Gravedad
        velocity += Physics.gravity * gravityModifier * dt;
        
        Vector3 currentPos = transform.position;
        Vector3 nextPos = currentPos + (velocity * dt);

        RaycastHit hit;
        Vector3 direction = (nextPos - currentPos).normalized;
        float distance = Vector3.Distance(currentPos, nextPos);

        // SphereCast con radio FÍSICO (Pequeño) para entrar en huecos
        if (Physics.SphereCast(currentPos, physicalRadius, direction, out hit, distance, layerMask))
        {
            PaintableSurface surface = hit.collider.GetComponent<PaintableSurface>();
            
            if (surface != null)
            {
                // Al pintar, usamos el radio DE LIMPIEZA (Grande) para cubrir bien
                surface.Paint(hit.point, hit.normal, cleaningRadius, strength, dirtType);
            }

            Destroy(gameObject);
        }
        else
        {
            transform.position = nextPos;
        }
    }
}