using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class test1 : MonoBehaviour
{
    //==================================================================================
    // 音频
    //==================================================================================

    [Header("音频控制")]
    public AudioController audioController;

    [Range(1f, 30f)]
    public float attackSpeed = 15f;

    [Range(1f, 10f)]
    public float decaySpeed = 2f;

    //==================================================================================
    // 粒子核心
    //==================================================================================

    [Header("粒子核心")]
    [Range(10, 500)]
    public int maxParticles = 230;

    [Range(1f, 60f)]
    public float emissionRate = 12f;

    [Range(0.5f, 100f)]
    public float particleLifetime = 1.8f;

    // ---- 新增功能：音频影响生命周期 ----
    [Header("生命周期音频控制")]
    [Tooltip("如果勾选，生命周期会随音频大小在最小值和给定值之间变化；如果不勾选，则固定为给定值。")]
    public bool affectLifetimeByAudio = false;

    [Range(0.1f, 100f)]
    public float minParticleLifetime = 0.5f;
    // ----------------------------------

    //==================================================================================
    // 粒子大小
    //==================================================================================

    [Header("粒子大小")]
    public Vector2 startSizeRange = new Vector2(80f, 90f);

    [Range(0f, 3f)]
    public float sizeAudioBoost = 1.2f;

    //==================================================================================
    // 粒子运动
    //==================================================================================

    [Header("粒子运动")]
    public Vector3 flowDirection = Vector3.up;

    [Range(0f, 5f)]
    public float baseRiseSpeed = 1.8f;

    [Range(0f, 15f)]
    public float riseAudioBoost = 9f;

    //==================================================================================
    // 粒子波动
    //==================================================================================

    [Header("粒子波动")]
    [Range(0f, 5f)]
    public float breathingSpeed = 1.5f;

    [Range(0f, 5f)]
    public float breathingAmplitude = 2f;

    //==================================================================================
    // 粒子飘逸
    //==================================================================================

    [Header("粒子飘逸")]
    [Range(0f, 5f)]
    public float swayStrength = 2f;

    [Range(0f, 3f)]
    public float swayFrequency = 1f;

    //==================================================================================
    // 粒子扭动
    //==================================================================================

    [Header("粒子扭动")]
    [Range(0f, 2f)]
    public float spiralStrength = 0.1f;

    //==================================================================================
    // 速度跟随
    //==================================================================================

    [Header("速度跟随")]
    [Range(1f, 20f)]
    public float velocityFollowSpeed = 5f;

    //==================================================================================
    // 旋转
    //==================================================================================

    [Header("旋转")]
    public bool enableRotation = true;

    [Header("初始旋转")]
    public Vector3 initialRotationEuler;

    [Range(0f, 180f)]
    public float baseRotationSpeed = 80f;

    [Range(0f, 360f)]
    public float audioRotationBoost = 220f;

    [Header("垂直角度")]
    public float uprightRotationZ = 90f;

    //==================================================================================
    // 颜色
    //==================================================================================

    [Header("颜色渐变")]
    public Gradient audioGradient;

    //==================================================================================
    // 发射源
    //==================================================================================

    [Header("发射源")]
    [Range(0.01f, 300f)]
    public float emitRadius = 0.5f;

    //==================================================================================

    private ParticleSystem ps;
    private ParticleSystem.MainModule main;
    private ParticleSystem.EmissionModule emission;
    private ParticleSystem.ShapeModule shape;
    private ParticleSystem.NoiseModule noise;

    private ParticleSystem.Particle[] particles;

    private float smoothedAmplitude;
    private int appliedMaxParticles = -1;

    //==================================================================================

    private void Awake()
    {
        ParticleSystemRenderer renderer = GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.alignment = ParticleSystemRenderSpace.World;
        }

        ps = GetComponent<ParticleSystem>();

        main = ps.main;
        emission = ps.emission;
        shape = ps.shape;
        noise = ps.noise;

        InitializeParticleSystem();

        transform.localRotation = Quaternion.Euler(initialRotationEuler);
    }

    private void Update()
    {
        UpdateAudio();
        ApplyParticleLimit();
        UpdateModules();
        UpdateParticles();
    }

    //==================================================================================

    void InitializeParticleSystem()
    {
        main.loop = true;
        main.playOnAwake = true;
        main.startSpeed = 0f;

        // 初始设置生命周期
        if (affectLifetimeByAudio)
            main.startLifetime = minParticleLifetime;
        else
            main.startLifetime = particleLifetime;

        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startRotation3D = true;

        emission.rateOverTime = emissionRate;

        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = emitRadius;
        shape.radiusThickness = 1f;

        noise.enabled = true;
        noise.strengthMultiplier = 0.15f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.3f;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        SetupColorOverLifetime();
        ApplyParticleLimit();

        ps.Clear();
        ps.Play();
    }

    void ApplyParticleLimit()
    {
        int clampedMaxParticles = Mathf.Max(1, maxParticles);

        if (appliedMaxParticles == clampedMaxParticles)
            return;

        main.maxParticles = clampedMaxParticles;
        particles = new ParticleSystem.Particle[clampedMaxParticles];
        appliedMaxParticles = clampedMaxParticles;
    }

    void SetupColorOverLifetime()
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0),
                new GradientColorKey(Color.white, 1)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0, 0),
                new GradientAlphaKey(1, 0.1f),
                new GradientAlphaKey(1, 0.8f),
                new GradientAlphaKey(0, 1)
            });

        col.color = new ParticleSystem.MinMaxGradient(g);
    }

    void UpdateAudio()
    {
        float target = audioController != null ? GetAverageBand(0, 8) : 0f;
        float speed = target > smoothedAmplitude ? attackSpeed : decaySpeed;

        smoothedAmplitude = Mathf.Lerp(smoothedAmplitude, target, Time.deltaTime * speed);
    }

    void UpdateModules()
    {
        float t = Mathf.Clamp01(smoothedAmplitude);

        // ---- 新增功能：在 Update 中实时控制粒子生命周期 ----
        if (affectLifetimeByAudio)
        {
            // 当音频音量最大(t=1)时，生命周期为给定最大值 particleLifetime 
            // 当音频没有声音(t=0)时，生命周期为设定的最小值 minParticleLifetime
            main.startLifetime = Mathf.Lerp(minParticleLifetime, particleLifetime, t);
        }
        else
        {
            // 未勾选时，固定为给定的 particleLifetime
            main.startLifetime = particleLifetime;
        }
        // -------------------------------------------------

        float minSize = Mathf.Lerp(startSizeRange.x, startSizeRange.x * (1f + sizeAudioBoost), t);
        float maxSize = Mathf.Lerp(startSizeRange.y, startSizeRange.y * (1f + sizeAudioBoost), t);

        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        emission.rateOverTime = emissionRate * Mathf.Lerp(0.2f, 1f, t);

        if (audioGradient != null)
        {
            Color c = audioGradient.Evaluate(t);
            c.a = 1f;
            main.startColor = c;
        }
    }

    void UpdateParticles()
    {
        if (particles == null) return;

        int count = ps.GetParticles(particles);
        if (count <= 0) return;

        Vector3 riseDir = flowDirection.normalized;
        float breathing = Mathf.Sin(Time.time * breathingSpeed);
        float riseSpeed = baseRiseSpeed + smoothedAmplitude * riseAudioBoost + breathing * breathingAmplitude;

        for (int i = 0; i < count; i++)
        {
            ParticleSystem.Particle p = particles[i];
            float phase = (p.randomSeed % 1000) * 0.006283f;

            float sx = Mathf.Sin(Time.time * swayFrequency + phase) * swayStrength;
            float sz = Mathf.Cos(Time.time * swayFrequency * 0.6f + phase) * swayStrength * 0.25f;
            Vector3 sway = new Vector3(sx, 0f, sz) * (0.3f + smoothedAmplitude * 0.7f);

            Vector3 spiral = Vector3.zero;
            if (spiralStrength > 0.01f)
            {
                Vector3 sp = new Vector3(-p.position.z, 0f, p.position.x);
                if (sp.sqrMagnitude > 0.001f) sp.Normalize();
                spiral = sp * spiralStrength * smoothedAmplitude;
            }

            p.velocity = Vector3.Lerp(
                p.velocity,
                riseDir * riseSpeed + sway + spiral,
                Time.deltaTime * velocityFollowSpeed);

            particles[i] = p;
        }

        ps.SetParticles(particles, count);
    }

    float GetAverageBand(int startInclusive, int endExclusive)
    {
        if (audioController == null || audioController.audioBandBuffer == null)
            return 0f;

        int start = Mathf.Clamp(startInclusive, 0, audioController.audioBandBuffer.Length);
        int end = Mathf.Clamp(endExclusive, start + 1, audioController.audioBandBuffer.Length);

        float total = 0f;
        int count = 0;

        for (int i = start; i < end; i++)
        {
            total += audioController.audioBandBuffer[i];
            count++;
        }

        return count > 0 ? total / count : 0f;
    }
}