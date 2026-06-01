using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class AudioPillarParticle : MonoBehaviour
{
    public AudioController audioController;

    [Header("平滑控制")]
    [Range(5f, 50f)]
    public float attackSpeed = 26f;

    [Range(1f, 10f)]
    public float decaySpeed = 3.5f;

    [Header("基础发射数量")]
    public float minEmission = 20f;
    public float maxEmission = 160f;

    [Header("柱式粒子总控")]
    [Range(0.1f, 5f)]
    public float overallSpeedMultiplier = 1f;

    [Range(0f, 5f)]
    public float emissionRateMultiplier = 1f;

    [Range(1, 3000)]
    public int maxParticles = 600;

    [Header("固定柱体发射范围")]
    public float spawnRadius = 0.35f;
    public float spawnHeight = 0.15f;

    [Header("初始粒子大小范围")]
    public Vector2 startSizeRange = new Vector2(0.08f, 0.18f);

    [Range(1f, 3f)]
    public float sizeAudioBoost = 1.2f;

    [Header("生命与运动")]
    public float baseLifetime = 2.6f;
    public float lifetimeBoost = 1.4f;

    public Vector3 flowDirection = Vector3.up;

    public float baseRiseSpeed = 1.8f;
    public float riseBoost = 8f;

    [Range(1f, 20f)]
    public float velocityFollowSpeed = 8f;

    public float detailSway = 1.4f;

    [Header("噪声扰动")]
    public float baseNoiseStrength = 0.2f;
    public float maxNoiseStrength = 1.4f;
    public float baseNoiseFrequency = 0.25f;
    public float noiseFrequencyBoost = 1.3f;

    [Header("模拟速度")]
    public float baseSimulationSpeed = 1f;
    public float simulationSpeedBoost = 1.2f;

    [Header("粒子颜色")]
    public Gradient audioColorGradient;

    [Header("音乐自旋转")]
    public bool enableMusicRotation = true;

    [Tooltip("无音乐时基础转速")]
    public float baseRotationSpeed = 30f;

    [Tooltip("音乐增强时额外转速")]
    public float rotationAudioBoost = 360f;

    [Tooltip("每个粒子随机转速差异")]
    [Range(0f, 1f)]
    public float randomRotationFactor = 0.4f;

    [Tooltip("旋转呼吸感频率")]
    public float rotationPulseSpeed = 4f;

    [Tooltip("呼吸感强度")]
    [Range(0f, 1f)]
    public float rotationPulseAmount = 0.3f;

    private ParticleSystem particleSystemComponent;

    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.ShapeModule shapeModule;
    private ParticleSystem.NoiseModule noiseModule;
    private ParticleSystem.LightsModule lightsModule;

    private ParticleSystem.Particle[] particles;

    private int appliedMaxParticles;

    private float smoothedMainAmplitude;
    private float smoothedDetailAmplitude;

    void Awake()
    {
        particleSystemComponent = GetComponent<ParticleSystem>();

        mainModule = particleSystemComponent.main;
        emissionModule = particleSystemComponent.emission;
        shapeModule = particleSystemComponent.shape;
        noiseModule = particleSystemComponent.noise;
        lightsModule = particleSystemComponent.lights;

        ConfigureParticleSystem();
    }

    void Update()
    {
        if (audioController != null)
        {
            float mainAmplitude = GetAverageBand(0, 8);
            float detailAmplitude = GetAverageBand(4, 16);

            float mainSpeed =
                mainAmplitude > smoothedMainAmplitude ?
                attackSpeed :
                decaySpeed;

            smoothedMainAmplitude =
                Mathf.Lerp(
                    smoothedMainAmplitude,
                    mainAmplitude,
                    Time.deltaTime * mainSpeed);

            float detailSpeed =
                detailAmplitude > smoothedDetailAmplitude ?
                attackSpeed :
                decaySpeed;

            smoothedDetailAmplitude =
                Mathf.Lerp(
                    smoothedDetailAmplitude,
                    detailAmplitude,
                    Time.deltaTime * detailSpeed);
        }
        else
        {
            smoothedMainAmplitude =
                Mathf.Lerp(
                    smoothedMainAmplitude,
                    0f,
                    Time.deltaTime * decaySpeed);

            smoothedDetailAmplitude =
                Mathf.Lerp(
                    smoothedDetailAmplitude,
                    0f,
                    Time.deltaTime * decaySpeed);
        }

        ApplyInspectorSettings();
        ApplyAudioToModules();
        UpdateParticles();
    }

    void ConfigureParticleSystem()
    {
        mainModule.loop = true;
        mainModule.playOnAwake = true;
        mainModule.simulationSpace =
            ParticleSystemSimulationSpace.Local;

        mainModule.startRotation3D = true;

        mainModule.startLifetime = baseLifetime;
        mainModule.startSpeed = 0f;

        mainModule.startSize =
            new ParticleSystem.MinMaxCurve(
                startSizeRange.x,
                startSizeRange.y);

        mainModule.simulationSpeed =
            baseSimulationSpeed;

        mainModule.gravityModifier = 0f;

        emissionModule.enabled = true;
        emissionModule.rateOverTime = minEmission;

        shapeModule.enabled = true;
        shapeModule.shapeType =
            ParticleSystemShapeType.Box;

        shapeModule.scale =
            new Vector3(
                spawnRadius * 2f,
                spawnHeight,
                spawnRadius * 2f);

        noiseModule.enabled = true;
        noiseModule.strengthMultiplier =
            baseNoiseStrength;

        noiseModule.frequency =
            baseNoiseFrequency;

        lightsModule.enabled = false;

        particleSystemComponent.Clear();
        particleSystemComponent.Play();
    }

    void ApplyInspectorSettings()
    {
        int clampedMaxParticles =
            Mathf.Max(1, maxParticles);

        if (appliedMaxParticles ==
            clampedMaxParticles)
        {
            return;
        }

        appliedMaxParticles =
            clampedMaxParticles;

        mainModule.maxParticles =
            appliedMaxParticles;

        particles =
            new ParticleSystem.Particle[
                appliedMaxParticles];
    }

    float GetAverageBand(
        int startInclusive,
        int endExclusive)
    {
        if (audioController == null ||
            audioController.audioBandBuffer == null)
        {
            return 0f;
        }

        int start =
            Mathf.Clamp(
                startInclusive,
                0,
                audioController.audioBandBuffer.Length);

        int end =
            Mathf.Clamp(
                endExclusive,
                start + 1,
                audioController.audioBandBuffer.Length);

        float total = 0f;
        int count = 0;

        for (int i = start; i < end; i++)
        {
            total += audioController.audioBandBuffer[i];
            count++;
        }

        return count > 0 ? total / count : 0f;
    }

    void ApplyAudioToModules()
    {
        float mainT =
            Mathf.Clamp01(smoothedMainAmplitude);

        float detailT =
            Mathf.Clamp01(smoothedDetailAmplitude);

        emissionModule.rateOverTime =
            Mathf.Lerp(
                minEmission,
                maxEmission,
                mainT)
            * emissionRateMultiplier;

        mainModule.startLifetime =
            baseLifetime +
            mainT * lifetimeBoost;

        float sizeBoost =
            Mathf.Lerp(
                1f,
                sizeAudioBoost,
                mainT);

        float minSize =
            Mathf.Max(
                0.001f,
                startSizeRange.x * sizeBoost);

        float maxSize =
            Mathf.Max(
                minSize,
                startSizeRange.y * sizeBoost);

        mainModule.startSize =
            new ParticleSystem.MinMaxCurve(
                minSize,
                maxSize);

        Color finalColor =
            audioColorGradient.Evaluate(
                Mathf.Clamp01(
                    mainT * 0.75f +
                    detailT * 0.25f));

        finalColor.a =
            Mathf.Lerp(
                0.65f,
                1f,
                mainT);

        mainModule.startColor =
            new ParticleSystem.MinMaxGradient(
                finalColor);

        mainModule.simulationSpeed =
            (baseSimulationSpeed +
            mainT * simulationSpeedBoost +
            detailT * 0.35f)
            * overallSpeedMultiplier;

        noiseModule.strengthMultiplier =
            Mathf.Lerp(
                baseNoiseStrength,
                maxNoiseStrength,
                detailT);

        noiseModule.frequency =
            baseNoiseFrequency +
            detailT * noiseFrequencyBoost;
    }

    void UpdateParticles()
    {
        if (particles == null)
            return;

        int particleCount =
            particleSystemComponent.GetParticles(
                particles);

        if (particleCount <= 0)
            return;

        Vector3 riseDirection =
            flowDirection.sqrMagnitude > 0.0001f ?
            flowDirection.normalized :
            Vector3.up;

        float riseSpeed =
            (baseRiseSpeed +
            smoothedMainAmplitude * riseBoost)
            * overallSpeedMultiplier;

        Vector3 targetUpVelocity =
            riseDirection * riseSpeed;

        float swayStrength =
            smoothedDetailAmplitude *
            detailSway;

        Color animatedColor =
            audioColorGradient.Evaluate(
                Mathf.Repeat(
                    Time.time * 0.08f +
                    smoothedDetailAmplitude * 0.35f,
                    1f));

        animatedColor.a =
            Mathf.Lerp(
                0.85f,
                1f,
                smoothedMainAmplitude);

        for (int i = 0; i < particleCount; i++)
        {
            Vector3 position =
                particles[i].position;

            Vector3 sideways =
                new Vector3(
                    Mathf.Sin(
                        Time.time * 1.6f +
                        position.y * 0.9f),
                    0f,
                    Mathf.Cos(
                        Time.time * 1.2f +
                        position.x * 0.8f))
                * swayStrength;

            Vector3 targetVelocity =
                targetUpVelocity +
                sideways;

            particles[i].velocity =
                Vector3.Lerp(
                    particles[i].velocity,
                    targetVelocity,
                    Time.deltaTime *
                    velocityFollowSpeed);

            particles[i].startColor =
                animatedColor;

            particles[i].color =
                animatedColor;

            if (enableMusicRotation)
            {
                float randomSeed =
                    Mathf.Abs(
                        Mathf.Sin(
                            particles[i].randomSeed *
                            0.001f));

                float rotationMultiplier =
                    Mathf.Lerp(
                        1f - randomRotationFactor,
                        1f + randomRotationFactor,
                        randomSeed);

                float pulse =
                    1f +
                    Mathf.Sin(
                        Time.time *
                        rotationPulseSpeed +
                        randomSeed * 10f)
                    * rotationPulseAmount;

                float rotationSpeed =
                    (baseRotationSpeed +
                    smoothedMainAmplitude *
                    rotationAudioBoost)
                    * rotationMultiplier
                    * pulse;

                Vector3 rot =
                    particles[i].rotation3D;

                rot.y +=
                    rotationSpeed *
                    Time.deltaTime;

                particles[i].rotation3D = rot;
            }
        }

        particleSystemComponent.SetParticles(
            particles,
            particleCount);
    }
}