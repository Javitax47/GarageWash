using System;
using System.Collections.Generic;
using KBCore.Refs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PaintableSurface : MonoBehaviour
{
    [HideInInspector]
    public float lastParticleCollisionTime = -1f;

    // --- Evento para notificar a la UI ---
    public event Action<float> OnCleanlinessChanged;

    // --- Enum para la calidad de la textura ---
    public enum TextureSizes
    {
        XS = 64, S = 128, M = 256, L = 512, XL = 1024, XXL = 2048,
    }
    public readonly List<Vector3> LastParticleCollisionPoints = new List<Vector3>();

    [Header("Configuración de Progreso")]
    [Tooltip("El porcentaje de limpieza real (0.0 a 1.0) en el que la barra de progreso marcará 100% y el objeto se autolimpiará.")]
    [SerializeField, Range(0f, 1.0f)] private float completionThreshold = 0.9f;

    [Header("Configuración de Limpieza")]
    [Tooltip("El radio de limpieza de cada partícula individual.")]
    public float particleCleanRadius = 0.03f;
    [Tooltip("Qué tan potente es la limpieza de cada partícula (0.01 a 1.0).")]
    [Range(0.01f, 1.0f)] public float cleaningStrength = 0.1f;
    
    [Header("Configuración General")]
    public TextureSizes TextureSize = TextureSizes.L;
    public Shader PaintShader;
    public ComputeShader CoverageShader;
    
    [Header("Depuración")]
    [Tooltip("Arrastra aquí una RawImage de tu UI para visualizar la máscara de suciedad en tiempo real.")]
    public RawImage textureVisualizer;
    
    // --- Propiedades Públicas de Estado ---
    public float Dirtiness { get; private set; } = 1;
    public float Cleanliness { get; private set; } = 0;
    
    // --- Referencias Internas ---
    [SerializeField, Self] private Transform _transform;
    [SerializeField, Child(Flag.Editable)] private MeshRenderer _mainRenderer;
    
    // --- Variables Privadas de Estado ---
    private bool _hasSetup;
    private bool isFullyClean = false;
    private RenderTexture _rt;
    private CommandBuffer _cmd;
    private readonly Dictionary<Material, Material> _materials = new();
    private Material _paintMaterial;    
    private LocalKeyword _floorColorKeyword;
    
    // --- Variables para Partículas ---
    private static readonly List<ParticleCollisionEvent> COLLISION_EVENTS = new List<ParticleCollisionEvent>();
    private ParticleSystem _particleSystem;
    
    // --- Variables para Optimización ---
    private float nextMeasureTime = 0f;
    private float measureInterval = 0.5f;

    // --- Variable para Normalizar el Progreso ---
    private float initialSumDirtiness = -1f;

    // --- Variables específicas para cada método de medición ---
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

    // --- Métodos de Ciclo de Vida de Unity ---

    private void Update()
    {
        if (_hasSetup && !isFullyClean && Time.time > nextMeasureTime)
        {
            MeasureCoverage();
            nextMeasureTime = Time.time + measureInterval;
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        lastParticleCollisionTime = Time.time; // Mantenemos la comunicación simple
        
        if (isFullyClean) return;
        
        if (_particleSystem == null) _particleSystem = other.GetComponent<ParticleSystem>();
        if (_particleSystem == null) return;

        int numCollisionEvents = _particleSystem.GetCollisionEvents(this.gameObject, COLLISION_EVENTS);
        if (numCollisionEvents == 0) return;

        // --- ¡NUEVA LÓGICA! ---
        // Limpiamos la lista y la rellenamos con los nuevos puntos de impacto.
        LastParticleCollisionPoints.Clear();
        for (int i = 0; i < numCollisionEvents; i++)
        {
            LastParticleCollisionPoints.Add(COLLISION_EVENTS[i].intersection);
        }
        
        Color cleaningColor = new Color(0, 0, 0, cleaningStrength);
        Paint(COLLISION_EVENTS, particleCleanRadius, cleaningColor);
    }
    
    private void OnDestroy()
    {
        if (_hasSetup) { _rt.Release(); Destroy(_rt); }
        foreach (var kvp in _materials) Destroy(kvp.Value);
        if (_paintMaterial) Destroy(_paintMaterial);
        
        #if UNITY_EDITOR || UNITY_STANDALONE
        if(_coverageBuffers[0] != null) _coverageBuffers[0].Release();
        if(_coverageBuffers[1] != null) _coverageBuffers[1].Release();
        #else
        if(_readableTexture != null) Destroy(_readableTexture);
        #endif
    }

    // --- Métodos de Pintado ---

    public void Paint(List<ParticleCollisionEvent> collisionEvents, float radius, Color color)
    {
        if (!_hasSetup) this.Setup();
        if (!_hasSetup) return;

        _paintMaterial.SetKeyword(_floorColorKeyword, false);
        _paintMaterial.SetFloat(Shader.PropertyToID("_SpreadRadius"), radius);
        _paintMaterial.SetColor("_PaintColor", color);
        
        _cmd.Clear();
        _cmd.GetTemporaryRT(Shader.PropertyToID("_PreviousFrameTex"), _rt.descriptor);
        _cmd.Blit(_rt, Shader.PropertyToID("_PreviousFrameTex"));
        _cmd.SetRenderTarget(_rt);
        _cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

        for (int i = 0; i < collisionEvents.Count; i++)
        {
            _paintMaterial.SetVector(Shader.PropertyToID("_PaintPositionWS"), collisionEvents[i].intersection);
            _paintMaterial.SetVector(Shader.PropertyToID("_PaintNormalWS"), collisionEvents[i].normal);
            _cmd.DrawRenderer(_mainRenderer, _paintMaterial, 0, 0);
        }
        
        _cmd.ReleaseTemporaryRT(Shader.PropertyToID("_PreviousFrameTex"));
        Graphics.ExecuteCommandBuffer(_cmd);
    }
    
    public void Paint(Vector3 position, Vector3 normal, float radius, Color color)
    {
        if (!_hasSetup) this.Setup();
        if (!_hasSetup) return;

        _paintMaterial.SetKeyword(_floorColorKeyword, false);
        _paintMaterial.SetVector(Shader.PropertyToID("_PaintPositionWS"), position);
        _paintMaterial.SetVector(Shader.PropertyToID("_PaintNormalWS"), normal);
        _paintMaterial.SetFloat(Shader.PropertyToID("_SpreadRadius"), radius);
        _paintMaterial.SetColor("_PaintColor", color);

        CommandBuffer singlePaintCmd = new CommandBuffer { name = "MainJetPaint" };
        singlePaintCmd.GetTemporaryRT(Shader.PropertyToID("_PreviousFrameTex"), _rt.descriptor);
        singlePaintCmd.Blit(_rt, Shader.PropertyToID("_PreviousFrameTex"));
        singlePaintCmd.SetRenderTarget(_rt);
        singlePaintCmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        singlePaintCmd.DrawRenderer(_mainRenderer, _paintMaterial, 0, 0);
        singlePaintCmd.ReleaseTemporaryRT(Shader.PropertyToID("_PreviousFrameTex"));
        Graphics.ExecuteCommandBuffer(singlePaintCmd);
    }
    
    public void FloodColor(Color color)
    {
        if (!_hasSetup) this.Setup();
        if (!_hasSetup) return;
        _paintMaterial.SetKeyword(_floorColorKeyword, true);
        _paintMaterial.SetColor("_PaintColor", color);
        CommandBuffer floodCmd = new CommandBuffer { name = "FloodCmd" };
        floodCmd.SetRenderTarget(_rt);
        floodCmd.DrawRenderer(_mainRenderer, _paintMaterial, 0, 0);
        Graphics.ExecuteCommandBuffer(floodCmd);
        _paintMaterial.SetKeyword(_floorColorKeyword, false);
    }

    // --- Métodos de Configuración y Medición ---

    [ContextMenu("Setup")]
    private void Setup()
    {
        if (_hasSetup) return;
        _hasSetup = true;

        _paintMaterial = new Material(PaintShader);
        _floorColorKeyword = new LocalKeyword(_paintMaterial.shader, "FLOOD_COLOR");
        _rt = new RenderTexture((int)TextureSize, (int)TextureSize, 0, RenderTextureFormat.ARGB32)
            { enableRandomWrite = true, wrapMode = TextureWrapMode.Clamp };
        _rt.Create();
        
        RenderTexture temp = RenderTexture.active;
        RenderTexture.active = _rt;
        GL.Clear(true, true, Color.white);
        RenderTexture.active = temp;
        
        SetupRenderer(_mainRenderer);
        _cmd = new CommandBuffer { name = "ParticlePaint" };
        if (textureVisualizer != null) textureVisualizer.texture = _rt;
        
        #if UNITY_EDITOR || UNITY_STANDALONE
        _coverageKernel = CoverageShader.FindKernel("Reduce");
        CoverageShader.GetKernelThreadGroupSizes(_coverageKernel, out _coverageThreadGroupSize, out uint _, out uint _);   
        _coverageShaderTextureModeKeyword = new LocalKeyword(CoverageShader, "TEXTURE_MODE");
        _coverageGroups = Mathf.CeilToInt(_rt.width * _rt.height / (float) _coverageThreadGroupSize);
        _coverageGroups = Mathf.Min(_coverageGroups, 65535);
        _coverageBuffers[0] = new ComputeBuffer(_coverageGroups, sizeof(float) * 2);
        _coverageBuffers[1] = new ComputeBuffer(_coverageGroups, sizeof(float) * 2); 
        #else
        _readableTexture = new Texture2D(32, 32, TextureFormat.ARGB32, false);
        #endif
    }
    
    private int SetupRenderer(Renderer rend)
    {
        var sharedMaterials = new List<Material>();
        rend.GetSharedMaterials(sharedMaterials);
        for (int i = 0; i < sharedMaterials.Count; i++)
        {
            Material material = sharedMaterials[i];
            if (!_materials.TryGetValue(material, out Material remappedMaterial))
            {
                remappedMaterial = new Material(material);
                remappedMaterial.SetTexture(Shader.PropertyToID("_DirtMask"), _rt);
                _materials[material] = remappedMaterial;
            }
            sharedMaterials[i] = remappedMaterial;
        }
        rend.SetSharedMaterials(sharedMaterials);
        return sharedMaterials.Count;
    }
    
    private void MeasureCoverage()
    {
        #if UNITY_EDITOR || UNITY_STANDALONE
        // --- MÉTODO RÁPIDO (GPU) PARA EDITOR/PC ---
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
        if (output.y == 0) return;
        ProcessCoverageResult(output.x, output.y);
        #else
        // --- MÉTODO LENTO PERO SEGURO (CPU) PARA MÓVIL ---
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
        float sumDirtiness = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            sumDirtiness += pixels[i].r;
        }
        _isMeasuring = false;
        ProcessCoverageResult(sumDirtiness, pixels.Length);
        #endif
    }
    
    private void ProcessCoverageResult(float sumDirtiness, float pixelCount)
    {
        if (pixelCount == 0) return;

        // Si es la primera vez que medimos, guardamos el valor inicial como el 100% de suciedad.
        if (initialSumDirtiness < 0)
        {
            initialSumDirtiness = sumDirtiness;
        }
        if (initialSumDirtiness == 0) return;
        
        // Calculamos la suciedad real como una proporción del valor inicial
        this.Dirtiness = Mathf.Clamp01(sumDirtiness / initialSumDirtiness);
        this.Cleanliness = 1 - this.Dirtiness;

        if (!isFullyClean && this.Cleanliness >= completionThreshold)
        {
            isFullyClean = true;
            this.Cleanliness = 1.0f;
            this.Dirtiness = 0.0f;
            FloodColor(Color.black);
            OnCleanlinessChanged?.Invoke(1.0f);
            return;
        }

        if (!isFullyClean)
        {
            float displayedCleanliness = Mathf.Clamp01(this.Cleanliness / completionThreshold);
            OnCleanlinessChanged?.Invoke(displayedCleanliness);
        }
    }
    
#if UNITY_EDITOR
    // --- Métodos solo para el Editor ---
    private void OnValidate() { this.ValidateRefs(); if(_mainRenderer != null) ValidateRenderer(_mainRenderer); }
    private static void ValidateRenderer(Renderer rend)
    {
        var staticFlags = GameObjectUtility.GetStaticEditorFlags(rend.gameObject);
        if ((staticFlags & StaticEditorFlags.BatchingStatic) > 0)
        {
            Debug.LogWarning($"{rend.name} tiene Static Batching activado. Debe ser desactivado para que la pintura funcione.", rend.gameObject);
            staticFlags &= ~StaticEditorFlags.BatchingStatic;
            GameObjectUtility.SetStaticEditorFlags(rend.gameObject, staticFlags);
            EditorUtility.SetDirty(rend.gameObject);
        }
    }
#endif
}