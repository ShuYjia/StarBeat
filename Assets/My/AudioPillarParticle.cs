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

    [Header("基础发射")]
    public float minEmission = 20f;
    public float maxEmission = 160f;

    [Header("柱式粒子总控")]
    [Range(0.1f, 5f)]
    public float overallSpeedMultiplier = 1f;
    [Range(0f, 5f)]
    public float emissionRateMultiplier = 1f;
    [Range(1, 3000)]
    public int maxParticles = 600;

    [Header("柱体形态")]
    public float baseRadius = 0.35f;
    public float maxRadius = 0.9f;
    public float baseParticleSize = 0.12f;
    public float maxParticleSize = 0.35f;
    public float baseLifetime = 2.6f;
    public float lifetimeBoost = 1.4f;

    [Header("运动控制")]
    public Vector3 flowDirection = Vector3.up;
    public float baseRiseSpeed = 1.8f;
    public float riseBoost = 8f;
    public float detailSway = 1.4f;

    [Header("噪声扰动")]
    public float baseNoiseStrength = 0.2f;
    public float maxNoiseStrength = 1.4f;
    public float baseNoiseFrequency = 0.25f;
    public float noiseFrequencyBoost = 1.3f;
    public float baseSimulationSpeed = 1f;
    public float simulationSpeedBoost = 1.2f;

    [Header("颜色")]
    public Gradient audioColorGradient;

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
        if (audioController == null)
        {
            return;
        }

        float mainAmplitude = GetAverageBand(0, 8);
        float detailAmplitude = GetAverageBand(4, 16);

        float mainSpeed = mainAmplitude > smoothedMainAmplitude ? attackSpeed : decaySpeed;
        smoothedMainAmplitude = Mathf.Lerp(smoothedMainAmplitude, mainAmplitude, Time.deltaTime * mainSpeed);

        float detailSpeed = detailAmplitude > smoothedDetailAmplitude ? attackSpeed : decaySpeed;
        smoothedDetailAmplitude = Mathf.Lerp(smoothedDetailAmplitude, detailAmplitude, Time.deltaTime * detailSpeed);

        ApplyInspectorSettings();
        ApplyAudioToModules();
        UpdateParticles();
    }

    void ConfigureParticleSystem()
    {
        mainModule.loop = true;
        mainModule.playOnAwake = true;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;
        ApplyInspectorSettings();
        mainModule.startLifetime = baseLifetime;
        mainModule.startSpeed = 0f;
        mainModule.startSize = baseParticleSize;
        mainModule.simulationSpeed = baseSimulationSpeed;
        mainModule.gravityModifier = 0f;

        emissionModule.enabled = true;
        emissionModule.rateOverTime = minEmission;

        shapeModule.enabled = true;
        shapeModule.shapeType = ParticleSystemShapeType.Box;
        shapeModule.scale = new Vector3(baseRadius * 2f, 0.15f, baseRadius * 2f);

        noiseModule.enabled = true;
        noiseModule.strengthMultiplier = baseNoiseStrength;
        noiseModule.frequency = baseNoiseFrequency;

        lightsModule.enabled = false;

        particleSystemComponent.Clear();
        particleSystemComponent.Play();
    }

    void ApplyInspectorSettings()
    {
        int clampedMaxParticles = Mathf.Max(1, maxParticles);
        if (appliedMaxParticles == clampedMaxParticles)
        {
            return;
        }

        appliedMaxParticles = clampedMaxParticles;
        mainModule.maxParticles = appliedMaxParticles;
        particles = new ParticleSystem.Particle[appliedMaxParticles];
    }

    float GetAverageBand(int startInclusive, int endExclusive)
    {
        if (audioController == null || audioController.audioBandBuffer == null)
        {
            return 0f;
        }

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

    void ApplyAudioToModules()
    {
        float pillarRadius = Mathf.Lerp(baseRadius, maxRadius, smoothedMainAmplitude);
        shapeModule.scale = new Vector3(pillarRadius * 2f, 0.15f + smoothedMainAmplitude * 0.25f, pillarRadius * 2f);

        emissionModule.rateOverTime = Mathf.Lerp(minEmission, maxEmission, smoothedMainAmplitude) * emissionRateMultiplier;

        mainModule.startLifetime = baseLifetime + smoothedMainAmplitude * lifetimeBoost;
        mainModule.startSize = Mathf.Lerp(baseParticleSize, maxParticleSize, smoothedMainAmplitude);
        mainModule.simulationSpeed = (baseSimulationSpeed + smoothedMainAmplitude * simulationSpeedBoost + smoothedDetailAmplitude * 0.35f) * overallSpeedMultiplier;

        noiseModule.strengthMultiplier = Mathf.Lerp(baseNoiseStrength, maxNoiseStrength, smoothedDetailAmplitude);
        noiseModule.frequency = baseNoiseFrequency + smoothedDetailAmplitude * noiseFrequencyBoost;

        Color finalColor = audioColorGradient.Evaluate(Mathf.Clamp01(smoothedMainAmplitude * 0.75f + smoothedDetailAmplitude * 0.25f));
        finalColor.a = Mathf.Lerp(0.65f, 1f, smoothedMainAmplitude);
        mainModule.startColor = finalColor;
    }

    void UpdateParticles()
    {
        int particleCount = particleSystemComponent.GetParticles(particles);
        Vector3 riseDirection = flowDirection.sqrMagnitude > 0.0001f ? flowDirection.normalized : Vector3.up;
        Vector3 upwardVelocity = riseDirection * (baseRiseSpeed + smoothedMainAmplitude * riseBoost) * overallSpeedMultiplier;
        float swayStrength = smoothedDetailAmplitude * detailSway;

        Color animatedColor = audioColorGradient.Evaluate(Mathf.Repeat(Time.time * 0.08f + smoothedDetailAmplitude * 0.35f, 1f));
        animatedColor.a = Mathf.Lerp(0.65f, 1f, smoothedMainAmplitude);

        for (int i = 0; i < particleCount; i++)
        {
            Vector3 position = particles[i].position;
            Vector3 sideways = new Vector3(
                Mathf.Sin(Time.time * 1.6f + position.y * 0.9f),
                0f,
                Mathf.Cos(Time.time * 1.2f + position.x * 0.8f)) * swayStrength;

            particles[i].velocity = upwardVelocity + sideways;
            particles[i].startColor = animatedColor;
        }

        particleSystemComponent.SetParticles(particles, particleCount);
    }
}
