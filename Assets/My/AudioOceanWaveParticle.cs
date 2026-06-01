using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class AudioOceanWaveParticleSimple : MonoBehaviour
{
    public AudioController audioController;

    [Header("平滑控制")]
    [Range(5f, 40f)]
    public float attackSpeed = 22f;
    [Range(1f, 10f)]
    public float decaySpeed = 4f;

    [Header("矩形范围")]
    [Range(8, 200)]
    public int gridX = 80;
    [Range(8, 200)]
    public int gridZ = 80;
    [Range(0.05f, 1f)]
    public float spacing = 0.28f;
    public float planeWidthOffset = 0f;
    public float planeDepthOffset = 0f;
    public float heightOffset = 0.08f;

    [Header("海浪运动")]
    public float baseWaveHeight = 0.25f;
    public float musicWaveBoost = 2.2f;
    [Range(0.1f, 4f)]
    public float overallAmplitudeMultiplier = 1f;
    [Range(0.1f, 4f)]
    public float overallSpeedMultiplier = 1f;
    public float waveSpeed = 2f;
    public float waveFrequencyX = 1.1f;
    public float waveFrequencyZ = 1.25f;
    public float secondaryWaveStrength = 0.45f;

    [Header("粒子独立扰动")]
    [Range(0f, 1f)]
    public float perParticleNoiseStrength = 0.55f;
    [Range(0.1f, 3f)]
    public float perParticleNoiseSpeed = 0.6f;
    [Range(0f, 1f)]
    public float perParticleSizeVariance = 0.4f;
    [Range(0f, 2f)]
    public float perParticlePhaseVariance = 1.5f;

    [Header("粒子表现")]
    public float baseSize = 0.06f;
    public float sizeBoost = 0.08f;
    [Range(0f, 1f)]
    public float waveColorInfluence = 0.35f;
    public Gradient colorGradient;

    [Header("音频敏感度")]
    [Range(0.5f, 5f)]
    public float audioSensitivity = 2.0f;
    [Range(0.5f, 3f)]
    public float audioCurve = 1.2f;

    // ── 私有状态 ──────────────────────────────────────────────────────────────
    private ParticleSystem particleSystemComponent;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.Particle[] particles;
    private Vector3[] basePositions;
    private float[] particlePhaseOffset;
    private float[] particleNoiseOffset;
    private float[] particleSizeSeed;
    private int currentCount;
    private int lastGridX = -1;
    private int lastGridZ = -1;
    private float lastSpacing = -1f;
    private float lastPlaneWidthOffset;
    private float lastPlaneDepthOffset;
    private bool needsRebuild = true;
    private float lowBand;
    private float midBand;
    private float highBand;
    private float volumeBand;

    void Awake()
    {
        particleSystemComponent = GetComponent<ParticleSystem>();
        mainModule = particleSystemComponent.main;
        emissionModule = particleSystemComponent.emission;
        needsRebuild = true;
        EnsureField();
    }

    void OnValidate()
    {
        gridX = Mathf.Clamp(gridX, 8, 200);
        gridZ = Mathf.Clamp(gridZ, 8, 200);
        spacing = Mathf.Max(0.05f, spacing);
        baseSize = Mathf.Max(0.01f, baseSize);
        sizeBoost = Mathf.Max(0f, sizeBoost);
        overallAmplitudeMultiplier = Mathf.Max(0.1f, overallAmplitudeMultiplier);
        overallSpeedMultiplier = Mathf.Max(0.1f, overallSpeedMultiplier);
        needsRebuild = true;
    }

    void Update()
    {
        EnsureField();

        if (audioController != null)
            AnalyzeAudio();
        else
        {
            lowBand = SmoothValue(lowBand, 0f);
            midBand = SmoothValue(midBand, 0f);
            highBand = SmoothValue(highBand, 0f);
            volumeBand = SmoothValue(volumeBand, 0f);
        }

        UpdateWaveParticles();
    }

    void EnsureField()
    {
        if (!needsRebuild &&
            lastGridX == gridX && lastGridZ == gridZ &&
            Mathf.Approximately(lastSpacing, spacing) &&
            Mathf.Approximately(lastPlaneWidthOffset, planeWidthOffset) &&
            Mathf.Approximately(lastPlaneDepthOffset, planeDepthOffset))
            return;

        RebuildField();
    }

    void RebuildField()
    {
        currentCount = gridX * gridZ;
        particles = new ParticleSystem.Particle[currentCount];
        basePositions = new Vector3[currentCount];
        particlePhaseOffset = new float[currentCount];
        particleNoiseOffset = new float[currentCount];
        particleSizeSeed = new float[currentCount];

        for (int i = 0; i < currentCount; i++)
        {
            float seed = i * 0.1732f;
            particlePhaseOffset[i] = Mathf.Sin(seed * 7.3f) * Mathf.PI * 2f;
            particleNoiseOffset[i] = Mathf.Abs(Mathf.Sin(seed * 13.7f)) * 100f;
            particleSizeSeed[i] = Mathf.Abs(Mathf.Sin(seed * 5.1f));
        }

        mainModule.loop = false;
        mainModule.playOnAwake = false;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;
        mainModule.maxParticles = currentCount;
        mainModule.startSpeed = 0f;
        mainModule.startLifetime = Mathf.Infinity;
        mainModule.startSize = baseSize;
        mainModule.simulationSpeed = 1f;
        mainModule.gravityModifier = 0f;

        emissionModule.enabled = false;
        particleSystemComponent.Clear();
        BuildGrid();
        particleSystemComponent.Emit(currentCount);

        int particleCount = particleSystemComponent.GetParticles(particles);
        for (int i = 0; i < particleCount; i++)
        {
            particles[i].position = basePositions[i];
            particles[i].startSize = baseSize;

            if (colorGradient != null)
            {
                float gx = gridX > 1 ? (float)(i % gridX) / (gridX - 1) : 0f;
                float gz = gridZ > 1 ? (float)(i / gridX) / (gridZ - 1) : 0f;
                particles[i].startColor = colorGradient.Evaluate(Mathf.Clamp01(gz * 0.85f + gx * 0.15f));
            }
        }

        particleSystemComponent.SetParticles(particles, particleCount);
        lastGridX = gridX;
        lastGridZ = gridZ;
        lastSpacing = spacing;
        lastPlaneWidthOffset = planeWidthOffset;
        lastPlaneDepthOffset = planeDepthOffset;
        needsRebuild = false;
    }

    void BuildGrid()
    {
        int index = 0;
        float halfX = (gridX - 1) * spacing * 0.5f;
        float halfZ = (gridZ - 1) * spacing * 0.5f;

        for (int z = 0; z < gridZ; z++)
            for (int x = 0; x < gridX; x++)
            {
                basePositions[index++] = new Vector3(
                    x * spacing - halfX + planeWidthOffset,
                    0f,
                    z * spacing - halfZ + planeDepthOffset);
            }
    }

    void AnalyzeAudio()
    {
        float rawLow = ApplySensitivity(GetAverageBand(0, 4));
        float rawMid = ApplySensitivity(GetAverageBand(4, 10));
        float rawHigh = ApplySensitivity(GetAverageBand(10, 16));
        float rawVolume = rawLow * 0.45f + rawMid * 0.35f + rawHigh * 0.2f;

        lowBand = SmoothValue(lowBand, rawLow);
        midBand = SmoothValue(midBand, rawMid);
        highBand = SmoothValue(highBand, rawHigh);
        volumeBand = SmoothValue(volumeBand, rawVolume);
    }

    float ApplySensitivity(float raw)
    {
        float boosted = Mathf.Clamp01(raw * audioSensitivity);
        return Mathf.Pow(boosted, 1f / Mathf.Max(0.1f, audioCurve));
    }

    float GetAverageBand(int startInclusive, int endExclusive)
    {
        if (audioController == null || audioController.audioBandBuffer == null) return 0f;

        int start = Mathf.Clamp(startInclusive, 0, audioController.audioBandBuffer.Length);
        int end = Mathf.Clamp(endExclusive, start + 1, audioController.audioBandBuffer.Length);
        float total = 0f;
        int count = 0;

        for (int i = start; i < end; i++) { total += audioController.audioBandBuffer[i]; count++; }
        return count > 0 ? total / count : 0f;
    }

    float SmoothValue(float current, float target)
    {
        float speed = target > current ? attackSpeed : decaySpeed;
        return Mathf.Lerp(current, target, Time.deltaTime * speed);
    }

    void UpdateWaveParticles()
    {
        if (particles == null || basePositions == null || currentCount == 0) return;

        int particleCount = particleSystemComponent.GetParticles(particles);
        float time = Time.time * overallSpeedMultiplier;
        float amplitude = (baseWaveHeight + lowBand * musicWaveBoost) * overallAmplitudeMultiplier;
        float baseParticleSize = baseSize + midBand * sizeBoost + volumeBand * 0.04f;

        for (int i = 0; i < particleCount; i++)
        {
            Vector3 pos = basePositions[i];
            float nx = pos.x * waveFrequencyX;
            float nz = pos.z * waveFrequencyZ;
            float phaseShift = particlePhaseOffset[i] * perParticlePhaseVariance;

            float wave1 = Mathf.Sin(nx + time * waveSpeed + lowBand * 4f + phaseShift);
            float wave2 = Mathf.Cos(nz * 1.3f - time * (waveSpeed * 0.8f) + midBand * 3f + phaseShift * 0.7f);
            float wave3 = Mathf.Sin((nx + nz) * 0.7f + time * (waveSpeed * 1.2f) + highBand * 6f + phaseShift * 1.3f);
            float wave4 = Mathf.Sin(nx * 2.7f - nz * 1.8f + time * (waveSpeed * 1.7f) + midBand * 2f) * 0.5f;
            float wave5 = Mathf.Cos(nx * 0.9f + nz * 3.1f - time * (waveSpeed * 0.6f) + lowBand * 5f) * 0.4f;

            float oceanWave = (wave1 + wave2 * 0.8f + wave3 * secondaryWaveStrength + wave4 + wave5) / 3.5f;

            float noiseT = time * perParticleNoiseSpeed + particleNoiseOffset[i];
            float noiseY = Mathf.Sin(noiseT * 1.0f) * 0.50f
                         + Mathf.Sin(noiseT * 2.3f) * 0.30f
                         + Mathf.Sin(noiseT * 5.1f) * 0.20f;
            noiseY *= perParticleNoiseStrength * amplitude;

            pos.y = heightOffset + oceanWave * amplitude + noiseY;
            particles[i].position = pos;

            float sizeNoise = Mathf.Sin(time * perParticleNoiseSpeed * 1.4f + particleNoiseOffset[i]) * 0.5f + 0.5f;
            float individualSize = baseParticleSize * Mathf.Lerp(1f, 0.5f + particleSizeSeed[i], perParticleSizeVariance * sizeNoise);
            particles[i].startSize = Mathf.Max(0.005f, individualSize);

            // ── 普通颜色粒子 ───────────────────────────────────────────────
            if (colorGradient != null)
            {
                float gradientX = gridX > 1 ? (float)(i % gridX) / (gridX - 1) : 0f;
                float gradientZ = gridZ > 1 ? (float)(i / gridX) / (gridZ - 1) : 0f;
                float t = Mathf.Clamp01(gradientZ * 0.85f + gradientX * 0.15f + oceanWave * waveColorInfluence);
                Color color = colorGradient.Evaluate(t);
                color.a = 1f;
                particles[i].startColor = color;
            }
        }

        particleSystemComponent.SetParticles(particles, particleCount);
    }
}