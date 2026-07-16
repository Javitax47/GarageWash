using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class PaintableSurface : MonoBehaviour
{
    // --- ENUMS ---
    public enum DirtType { TypeA_Red, TypeB_Green, All }
    public enum TextureSizes { XS = 64, S = 128, M = 256, L = 512, XL = 1024 }

    // --- DEBUG ---
    [Header("Debug")]
    public bool enableLogs = true; 

    // --- CONFIGURACIÓN DE LIMPIEZA (Faltaban estas variables) ---
    [Header("Configuración Inicial")]
    public Texture2D initialMaskTexture;

    [Header("Configuración General")]
    public TextureSizes TextureSize = TextureSizes.L;
    [SerializeField, Range(0f, 1.0f)] private float completionThreshold = 0.9f;

    [Header("Parámetros de Limpieza")]
    [Tooltip("Radio de limpieza para partículas individuales")]
    public float particleCleanRadius = 0.03f;
    [Tooltip("Fuerza de limpieza para partículas individuales")]
    [Range(0.01f, 1.0f)] public float cleaningStrength = 0.1f;
    [Tooltip("Tipo de suciedad que limpia este objeto por defecto")]
    public DirtType dirtTypeToClean = DirtType.TypeA_Red;
    
    // --- REFERENCIAS ---
    [Header("Referencias")]
    public Shader PaintShader;
    public ComputeShader CoverageShader;
    public RawImage textureVisualizer;
    [SerializeField] private MeshRenderer _mainRenderer;

    // --- EVENTOS Y PROPIEDADES PÚBLICAS ---
    public event Action<float> OnCleanlinessChanged; 
    public float Cleanliness { get; private set; } = 0; 
    public float Dirtiness { get; private set; } = 1;
    public List<Vector3> LastParticleCollisionPoints = new List<Vector3>(); 

    // --- VARIABLES DE INTERPOLACIÓN ---
    private Vector3 lastPaintPosition;
    private float lastPaintTime;
    private bool hasLastPosition = false;
    private const float MAX_INTERPOLATION_DISTANCE = 0.5f; 

    // --- ESTADO INTERNO ---
    private RenderTexture _rt;
    private CommandBuffer _cmd;
    private Material _paintMaterial;
    private bool _hasSetup = false;
    private bool isFullyClean = false;
    
    // --- VARIABLES DE PARTÍCULAS (Faltaban estas variables) ---
    private static readonly List<ParticleCollisionEvent> COLLISION_EVENTS = new List<ParticleCollisionEvent>();
    private ParticleSystem _particleSystem;

    // --- VARIABLES DE MEDICIÓN ---
    private float nextMeasureTime = 0f;
    private float measureInterval = 0.5f; 
    private float initialSumDirtiness = -1f;

#if UNITY_EDITOR || UNITY_STANDALONE
    private static readonly Vector2[] COVERAGE_RESULT_BUFFER = new Vector2[1];
    private int _coverageKernel;
    private int _coverageGroups;
    private uint _coverageThreadGroupSize;
    private ComputeBuffer[] _coverageBuffers = new ComputeBuffer[2];
    private LocalKeyword _coverageShaderTextureModeKeyword;
#else
    private Texture2D _readableTexture;
    private bool _isMeasuring = false;
#endif

    // --- UNITY CALLBACKS ---

    private void Awake()
    {
        if (_mainRenderer == null) _mainRenderer = GetComponent<MeshRenderer>();
        if (_mainRenderer == null) _mainRenderer = GetComponentInChildren<MeshRenderer>();
    }

    private void Start()
    {
        Setup();
    }

    private void Update()
    {
        // Medir progreso periódicamente
        if (_hasSetup && !isFullyClean && Time.time > nextMeasureTime)
        {
            MeasureCoverage();
            nextMeasureTime = Time.time + measureInterval;
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        if (isFullyClean) return;

        // Cacheamos el sistema de partículas
        if (_particleSystem == null) _particleSystem = other.GetComponent<ParticleSystem>();
        if (_particleSystem == null) return;

        int numEvents = _particleSystem.GetCollisionEvents(this.gameObject, COLLISION_EVENTS);
        if (numEvents == 0) return;

        // Guardamos puntos para lógica externa (ej. PowerWasherVR)
        LastParticleCollisionPoints.Clear();
        for (int i = 0; i < numEvents; i++) LastParticleCollisionPoints.Add(COLLISION_EVENTS[i].intersection);

        // Usamos las variables que ahora sí están declaradas
        Paint(COLLISION_EVENTS, particleCleanRadius, cleaningStrength, dirtTypeToClean);
    }

    // --- MÉTODOS DE PINTADO ---

    // 1. PINTAR UN SOLO PUNTO (Con Interpolación) - Usado por Manguera(Raycast) y Esponja
    public void Paint(Vector3 position, Vector3 normal, float radius, float strength, DirtType dirtType)
    {
        if (!_hasSetup) Setup();

        Color brushColor = GetBrushColor(strength, dirtType);
        
        _paintMaterial.SetFloat(Shader.PropertyToID("_SpreadRadius"), radius);
        _paintMaterial.SetColor("_PaintColor", brushColor);
        
        _cmd.Clear();
        _cmd.SetRenderTarget(_rt);
        _cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        
        // A. Pintar punto actual
        _paintMaterial.SetVector(Shader.PropertyToID("_PaintNormalWS"), normal);
        _paintMaterial.SetVector(Shader.PropertyToID("_PaintPositionWS"), position);
        _cmd.DrawRenderer(_mainRenderer, _paintMaterial, 0, 0);

        // B. Interpolación (Rellenar huecos si movemos rápido)
        if (hasLastPosition)
        {
            float dist = Vector3.Distance(position, lastPaintPosition);
            if (Time.time - lastPaintTime < 0.1f && dist > radius * 0.5f && dist < MAX_INTERPOLATION_DISTANCE)
            {
                int steps = Mathf.CeilToInt(dist / (radius * 0.3f));
                for (int i = 1; i < steps; i++)
                {
                    float t = (float)i / steps;
                    Vector3 interpolatedPos = Vector3.Lerp(lastPaintPosition, position, t);
                    _paintMaterial.SetVector(Shader.PropertyToID("_PaintPositionWS"), interpolatedPos);
                    _cmd.DrawRenderer(_mainRenderer, _paintMaterial, 0, 0);
                }
            }
        }
        
        Graphics.ExecuteCommandBuffer(_cmd);
        
        lastPaintPosition = position;
        lastPaintTime = Time.time;
        hasLastPosition = true;
    }

    // 2. PINTAR LISTA DE EVENTOS (Sin Interpolación) - Usado por Colisión de Partículas
    public void Paint(List<ParticleCollisionEvent> collisionEvents, float radius, float strength, DirtType dirtType)
    {
        if (!_hasSetup) Setup();
        if (collisionEvents.Count == 0) return;

        Color brushColor = GetBrushColor(strength, dirtType);
        
        _paintMaterial.SetFloat(Shader.PropertyToID("_SpreadRadius"), radius);
        _paintMaterial.SetColor("_PaintColor", brushColor);

        _cmd.Clear();
        _cmd.SetRenderTarget(_rt);
        _cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

        for (int i = 0; i < collisionEvents.Count; i++)
        {
            _paintMaterial.SetVector(Shader.PropertyToID("_PaintPositionWS"), collisionEvents[i].intersection);
            _paintMaterial.SetVector(Shader.PropertyToID("_PaintNormalWS"), collisionEvents[i].normal);
            _cmd.DrawRenderer(_mainRenderer, _paintMaterial, 0, 0);
        }

        Graphics.ExecuteCommandBuffer(_cmd);
    }

    public void FloodColor(Color color)
    {
        if (!_hasSetup) Setup();
        
        _paintMaterial.EnableKeyword("FLOOD_COLOR");
        _paintMaterial.SetColor("_PaintColor", color);
        
        CommandBuffer floodCmd = new CommandBuffer { name = "FloodCmd" };
        floodCmd.SetRenderTarget(_rt);
        floodCmd.DrawRenderer(_mainRenderer, _paintMaterial, 0, 0);
        Graphics.ExecuteCommandBuffer(floodCmd);
        
        _paintMaterial.DisableKeyword("FLOOD_COLOR");
    }

    // --- SETUP ---

    [ContextMenu("Setup")]
    private void Setup()
    {
        if (_hasSetup) return;
        if (_mainRenderer == null) return;

        _hasSetup = true;
        _paintMaterial = new Material(PaintShader);
        
        _rt = new RenderTexture((int)TextureSize, (int)TextureSize, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            useMipMap = false,
            autoGenerateMips = false
        };
        _rt.Create();

        // Cargar textura inicial si existe, si no, Blanco
        if (initialMaskTexture != null)
        {
            Graphics.Blit(initialMaskTexture, _rt);
        }
        else
        {
            RenderTexture temp = RenderTexture.active;
            RenderTexture.active = _rt;
            GL.Clear(true, true, Color.white); 
            RenderTexture.active = temp;
        }

        var materials = _mainRenderer.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i] = new Material(materials[i]);
            materials[i].SetTexture("_DirtMask", _rt);
        }
        _mainRenderer.sharedMaterials = materials;

        _cmd = new CommandBuffer { name = "ParticlePaint" };
        if (textureVisualizer != null) textureVisualizer.texture = _rt;

        SetupComputeShader();
    }

    private Color GetBrushColor(float strength, DirtType dirtType)
    {
        switch (dirtType)
        {
            case DirtType.TypeA_Red:   return new Color(0, 1, 1, strength);
            case DirtType.TypeB_Green: return new Color(1, 0, 1, strength); 
            default:                   return new Color(0, 0, 0, strength);
        }
    }
    
    // --- MEDICIÓN DE PROGRESO ---

    private void SetupComputeShader()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (CoverageShader != null)
        {
            _coverageKernel = CoverageShader.FindKernel("Reduce");
            CoverageShader.GetKernelThreadGroupSizes(_coverageKernel, out _coverageThreadGroupSize, out uint _, out uint _);   
            _coverageShaderTextureModeKeyword = new LocalKeyword(CoverageShader, "TEXTURE_MODE");
            _coverageGroups = Mathf.CeilToInt(_rt.width * _rt.height / (float) _coverageThreadGroupSize);
            _coverageGroups = Mathf.Min(_coverageGroups, 65535);
            _coverageBuffers[0] = new ComputeBuffer(_coverageGroups, sizeof(float) * 2);
            _coverageBuffers[1] = new ComputeBuffer(_coverageGroups, sizeof(float) * 2);
        }
#else
        _readableTexture = new Texture2D(32, 32, TextureFormat.ARGB32, false);
#endif
    }

    private void MeasureCoverage()
    {
        float totalDirt = 0;
        float totalPixels = 0;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (CoverageShader == null) return;

        int groups = _coverageGroups;
        CoverageShader.EnableKeyword(_coverageShaderTextureModeKeyword);
        CoverageShader.SetTexture(_coverageKernel, Shader.PropertyToID("InputTexture"), _rt);
        CoverageShader.SetBuffer(_coverageKernel, Shader.PropertyToID("OutputBuffer"), _coverageBuffers[0]);
        CoverageShader.Dispatch(_coverageKernel, groups, 1, 1);
        CoverageShader.DisableKeyword(_coverageShaderTextureModeKeyword);
        
        bool bufferToggle = false;
        while (groups > 1)
        {
            groups = Mathf.CeilToInt(groups / (float)_coverageThreadGroupSize);
            CoverageShader.SetBuffer(_coverageKernel, Shader.PropertyToID("InputBuffer"), _coverageBuffers[bufferToggle ? 1 : 0]);
            CoverageShader.SetBuffer(_coverageKernel, Shader.PropertyToID("OutputBuffer"), _coverageBuffers[bufferToggle ? 0 : 1]);
            CoverageShader.Dispatch(_coverageKernel, groups, 1, 1);
            bufferToggle = !bufferToggle;
        }
        
        ComputeBuffer final = _coverageBuffers[bufferToggle ? 1 : 0];
        final.GetData(COVERAGE_RESULT_BUFFER);
        Vector2 output = COVERAGE_RESULT_BUFFER[0];
        
        totalDirt = output.x;
        totalPixels = output.y;
#else
        if (_isMeasuring) return;
        _isMeasuring = true;
        RenderTexture lowResRT = RenderTexture.GetTemporary(32, 32);
        Graphics.Blit(_rt, lowResRT);
        RenderTexture.active = lowResRT;
        _readableTexture.ReadPixels(new Rect(0, 0, 32, 32), 0, 0);
        _readableTexture.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(lowResRT);
        Color[] pixels = _readableTexture.GetPixels();
        
        for (int i = 0; i < pixels.Length; i++) 
        {
            totalDirt += (pixels[i].r + pixels[i].g); // Sumamos ambos canales
        }
        totalPixels = pixels.Length;
        _isMeasuring = false;
#endif

        ProcessCoverageResult(totalDirt, totalPixels);
    }

    private void ProcessCoverageResult(float sumDirtiness, float pixelCount)
    {
        if (pixelCount == 0) return;

        // Establecer la referencia inicial (el 100% de suciedad al arrancar)
        if (initialSumDirtiness < 0) 
        {
            initialSumDirtiness = sumDirtiness;
            if (enableLogs) Debug.Log($"[CLEANING DEBUG] Referencia inicial: {initialSumDirtiness}");
        }
        
        if (initialSumDirtiness == 0) return;

        // 1. Calculamos la limpieza REAL (de 0.0 a 1.0 de todo el barco)
        float realDirtiness = Mathf.Clamp01(sumDirtiness / initialSumDirtiness);
        float realCleanliness = 1f - realDirtiness;

        // 2. MAQUILAJE MATEMÁTICO (Normalización)
        // Mapeamos el rango [0 a completionThreshold] al rango [0 a 1]
        // Ejemplo: Si threshold es 0.2 (20%):
        // Si realCleanliness es 0.1 -> displayCleanliness es 0.5 (50%)
        float displayCleanliness = Mathf.Clamp01(realCleanliness / completionThreshold);

        // Actualizamos las propiedades públicas con el valor "maquillado"
        this.Cleanliness = displayCleanliness;
        this.Dirtiness = 1f - displayCleanliness;

        // 3. Verificamos si ha ganado (según el valor REAL)
        if (!isFullyClean && realCleanliness >= completionThreshold)
        {
            isFullyClean = true;
            this.Cleanliness = 1.0f; // Forzamos 100% visual
            this.Dirtiness = 0.0f;
            
            // Limpiamos visualmente el barco entero (opcional, para que no queden manchas)
            FloodColor(Color.black); 
            
            OnCleanlinessChanged?.Invoke(1.0f);
            
            if (enableLogs) Debug.Log("[CLEANING] ¡Umbral alcanzado! Victoria.");
            return;
        }
        
        // Enviamos el progreso normalizado a la UI para que suba suavemente
        OnCleanlinessChanged?.Invoke(this.Cleanliness);

        if (enableLogs && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[PROGRESS] Real: {(realCleanliness*100):F1}% | Visual: {(displayCleanliness*100):F0}%");
        }
    }
    
    private void OnDestroy()
    {
        if (_hasSetup && _rt != null) _rt.Release();
#if UNITY_EDITOR || UNITY_STANDALONE
        if(_coverageBuffers[0] != null) _coverageBuffers[0].Release();
        if(_coverageBuffers[1] != null) _coverageBuffers[1].Release();
#else
        if (_readableTexture != null) Destroy(_readableTexture);
#endif
    }
}