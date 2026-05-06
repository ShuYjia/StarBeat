using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(ParticleSystemRenderer))]
public class AudioFlowParticle : MonoBehaviour
{
    public enum DepthLayerPreset
    {
        Custom,
        Foreground,
        Background
    }

    public AudioController audioController;
    public Gradient audioColorGradient;

    [Range(1f, 20f)]
    public float smoothSpeed = 8f;

    [Range(0f, 2f)]
    public float reactionIntensity = 1f;

    // 兼容场景里已经存在的旧调参字段，避免脚本升级后丢失原有效果。
    public float noiseStrengthIntensity = 1.76f;
    public float simulationSpeedIntensity = 0.57f;
    public float limitMaxSize = 0f;

    [Header("Depth Layer")]
    public DepthLayerPreset depthLayerPreset = DepthLayerPreset.Custom;

    [Header("Band Response")]
    [Range(0, 15)] public int bassStartBand = 0;
    [Range(0, 15)] public int bassEndBand = 1;
    [Range(0, 15)] public int midStartBand = 2;
    [Range(0, 15)] public int midEndBand = 7;
    [Range(0, 15)] public int trebleStartBand = 8;
    [Range(0, 15)] public int trebleEndBand = 15;
    public float bassSizeBoost = 2.2f;
    public float bassRadialForce = 16f;
    public float midNoiseBoost = 1.05f;
    public float midSwirlForce = 12f;
    public float trebleFrequencyBoost = 1.15f;
    public float trebleShimmer = 0.35f;
    public float verticalLift = 2f;

    [Header("Beat Pulse")]
    [Range(0f, 1f)] public float beatThreshold = 0.5f;
    public float beatPulseStrength = 0.95f;
    public float beatPulseDecay = 2.8f;

    [Header("Beat Flash / Ripple")]
    public float beatFlashStrength = 1f;
    public float rippleSpeed = 15f;
    public float rippleWidth = 3f;
    public float rippleForce = 20f;
    public float rippleColorBoost = 0.5f;

    [Header("Color Accents")]
    public Color bassAccentColor = new Color(1f, 0.45f, 0.18f, 1f);
    public Color midAccentColor = new Color(0.15f, 1f, 0.82f, 1f);
    public Color trebleAccentColor = new Color(0.75f, 0.45f, 1f, 1f);

    [Header("MR / VR Adaptation")]
    public bool adaptForSeeThrough = true;
    [Range(0.2f, 1f)] public float seeThroughAlpha = 0.72f;
    [Range(0.8f, 1.5f)] public float seeThroughSizeMultiplier = 0.92f;
    [Range(0.8f, 1.5f)] public float seeThroughBrightness = 1.08f;
    public int vrMaxLights = 48;
    public int seeThroughMaxLights = 24;

    [Header("Particle Motion")]
    [Range(0.8f, 0.999f)] public float particleDamping = 0.988f;
    public float depthDistance = 22f;

    private ParticleSystem particleSystemComponent;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.NoiseModule noiseModule;
    private ParticleSystem.LightsModule lightsModule;
    private ParticleSystemRenderer particleRenderer;
    private ParticleSystem.Particle[] particles;

    private float baseStartSize;
    private float baseSimulationSpeed;
    private float baseEmissionRate;
    private float baseNoiseStrength;
    private float baseNoiseFrequency;
    private float baseMaxParticleSize;
    private int baseMaxLights;
    private bool hasLightsModule;

    private float smoothedBass;
    private float smoothedMid;
    private float smoothedTreble;
    private float smoothedAmplitude;
    private float beatPulse;
    private float previousBass;
    private bool rippleActive;
    private float rippleRadius;

    private float layerSizeScale = 1f;
    private float layerEmissionScale = 1f;
    private float layerVelocityScale = 1f;
    private float layerBrightnessScale = 1f;
    private float layerRippleScale = 1f;
    private float layerAlphaScale = 1f;
    private float layerNoiseScale = 1f;

    private void Start()
    {
        particleSystemComponent = GetComponent<ParticleSystem>();
        mainModule = particleSystemComponent.main;
        emissionModule = particleSystemComponent.emission;
        noiseModule = particleSystemComponent.noise;
        lightsModule = particleSystemComponent.lights;
        particleRenderer = GetComponent<ParticleSystemRenderer>();

        particles = new ParticleSystem.Particle[Mathf.Max(1, mainModule.maxParticles)];

        // 记录粒子系统的基础值，后续所有音乐响应都在这个基础上叠加。
        baseStartSize = mainModule.startSizeMultiplier;
        baseSimulationSpeed = mainModule.simulationSpeed;
        baseEmissionRate = emissionModule.rateOverTimeMultiplier;
        baseNoiseStrength = noiseModule.strengthMultiplier;
        baseNoiseFrequency = noiseModule.frequency;
        baseMaxParticleSize = particleRenderer.maxParticleSize;
        hasLightsModule = lightsModule.enabled;
        baseMaxLights = hasLightsModule ? lightsModule.maxLights : 0;

        ApplyDepthLayerPreset();
    }

    private void ApplyDepthLayerPreset()
    {
        // 通过近景/远景预设，让现有两个实例形成明显景深，而粒子群内部继续承担中景过渡。
        switch (depthLayerPreset)
        {
            case DepthLayerPreset.Foreground:
                layerSizeScale = 1.25f;
                layerEmissionScale = 1.15f;
                layerVelocityScale = 1.18f;
                layerBrightnessScale = 1.12f;
                layerRippleScale = 1.18f;
                layerAlphaScale = 1f;
                layerNoiseScale = 1.05f;
                break;
            case DepthLayerPreset.Background:
                layerSizeScale = 0.72f;
                layerEmissionScale = 0.8f;
                layerVelocityScale = 0.74f;
                layerBrightnessScale = 0.82f;
                layerRippleScale = 0.65f;
                layerAlphaScale = 0.78f;
                layerNoiseScale = 0.78f;
                break;
            default:
                layerSizeScale = 1f;
                layerEmissionScale = 1f;
                layerVelocityScale = 1f;
                layerBrightnessScale = 1f;
                layerRippleScale = 1f;
                layerAlphaScale = 1f;
                layerNoiseScale = 1f;
                break;
        }
    }

    private void Update()
    {
        if (audioController == null)
        {
            return;
        }

        // 低频负责“冲击感”，中频负责“流动感”，高频负责“闪烁感”。
        float rawBass = GetBandAverage(bassStartBand, bassEndBand);
        float rawMid = GetBandAverage(midStartBand, midEndBand);
        float rawTreble = GetBandAverage(trebleStartBand, trebleEndBand);

        smoothedBass = SmoothBand(smoothedBass, rawBass, smoothSpeed * 1.2f);
        smoothedMid = SmoothBand(smoothedMid, rawMid, smoothSpeed * 0.9f);
        smoothedTreble = SmoothBand(smoothedTreble, rawTreble, smoothSpeed * 1.5f);

        float weightedAmplitude = (smoothedBass * 0.45f) + (smoothedMid * 0.35f) + (smoothedTreble * 0.2f);
        smoothedAmplitude = SmoothBand(smoothedAmplitude, weightedAmplitude * reactionIntensity, smoothSpeed);

        UpdateBeatPulse();
        UpdateRippleWave();

        bool seeThroughEnabled = adaptForSeeThrough && SeeThroughManager.IsSeeThroughEnabled;
        Color globalColor = BuildGlobalColor(seeThroughEnabled);

        ApplyModuleSettings(globalColor, seeThroughEnabled);
        ApplyParticleField(globalColor);
    }

    private float GetBandAverage(int startBand, int endBand)
    {
        float[] bands = audioController.audioBandBuffer;
        if (bands == null || bands.Length == 0)
        {
            return 0f;
        }

        int start = Mathf.Clamp(Mathf.Min(startBand, endBand), 0, bands.Length - 1);
        int end = Mathf.Clamp(Mathf.Max(startBand, endBand), 0, bands.Length - 1);

        float total = 0f;
        int count = 0;
        for (int i = start; i <= end; i++)
        {
            total += bands[i];
            count++;
        }

        return count > 0 ? total / count : 0f;
    }

    private float SmoothBand(float current, float target, float speed)
    {
        return Mathf.Lerp(current, target, Time.deltaTime * Mathf.Max(0.01f, speed));
    }

    private void UpdateBeatPulse()
    {
        // 使用低频上升沿来近似检测鼓点，让节拍时出现明确的闪爆反馈。
        bool beatTriggered = smoothedBass > beatThreshold && smoothedBass > previousBass + 0.02f;
        if (beatTriggered)
        {
            beatPulse = 1f;
            rippleActive = true;
            rippleRadius = 0f;
        }

        beatPulse = Mathf.Max(0f, beatPulse - (Time.deltaTime * beatPulseDecay));
        previousBass = smoothedBass;
    }

    private void UpdateRippleWave()
    {
        if (!rippleActive)
        {
            return;
        }

        rippleRadius += Time.deltaTime * rippleSpeed * layerVelocityScale;
        if (rippleRadius > depthDistance + rippleWidth)
        {
            rippleActive = false;
        }
    }

    private Color BuildGlobalColor(bool seeThroughEnabled)
    {
        // 颜色并不只跟音量绑定，还会随着时间和高频轻微流动，避免画面“死色”。
        float colorTime = Mathf.Repeat(Time.time * (0.05f + (smoothedTreble * 0.2f)), 1f);
        float gradientPosition = Mathf.Clamp01((smoothedAmplitude * 0.55f) + (smoothedMid * 0.2f) + (colorTime * 0.25f));

        Color color = audioColorGradient.Evaluate(gradientPosition);
        color = Color.Lerp(color, bassAccentColor, (smoothedBass * 0.35f) + (beatPulse * 0.2f));
        color = Color.Lerp(color, midAccentColor, smoothedMid * 0.3f);
        color = Color.Lerp(color, trebleAccentColor, smoothedTreble * 0.35f);

        float brightness = (0.85f + (smoothedTreble * trebleShimmer) + (beatPulse * beatFlashStrength)) * layerBrightnessScale;
        if (seeThroughEnabled)
        {
            brightness *= seeThroughBrightness;
        }

        color = MultiplyColor(color, brightness);
        color.a = Mathf.Clamp01((0.55f + (smoothedAmplitude * 0.22f) + (beatPulse * 0.22f)) * layerAlphaScale);

        if (seeThroughEnabled)
        {
            color.a *= seeThroughAlpha;
        }

        return color;
    }

    private void ApplyModuleSettings(Color globalColor, bool seeThroughEnabled)
    {
        // 模块级调参决定整体气势：尺寸、速度、密度、噪声一起响应音乐。
        float sizeMultiplier = (1f + (smoothedAmplitude * bassSizeBoost) + (beatPulse * beatPulseStrength)) * layerSizeScale;
        if (seeThroughEnabled)
        {
            sizeMultiplier *= seeThroughSizeMultiplier;
        }

        if (limitMaxSize > 0f)
        {
            sizeMultiplier = Mathf.Min(sizeMultiplier, limitMaxSize);
        }

        mainModule.startSizeMultiplier = baseStartSize * sizeMultiplier;
        mainModule.simulationSpeed = baseSimulationSpeed * (1f + (smoothedAmplitude * simulationSpeedIntensity) + (smoothedBass * 0.5f) + (smoothedTreble * 0.35f) + (beatPulse * 0.35f)) * layerVelocityScale;
        mainModule.startColor = new ParticleSystem.MinMaxGradient(globalColor);

        emissionModule.rateOverTimeMultiplier = baseEmissionRate * (1f + (smoothedAmplitude * 1.2f) + (smoothedBass * 1f) + (beatPulse * 1.6f)) * layerEmissionScale;

        noiseModule.strengthMultiplier = baseNoiseStrength * (1f + (smoothedAmplitude * noiseStrengthIntensity) + (smoothedMid * midNoiseBoost) + (beatPulse * 0.25f)) * layerNoiseScale;
        noiseModule.frequency = baseNoiseFrequency + (smoothedTreble * trebleFrequencyBoost) + (smoothedMid * 0.25f * layerNoiseScale);

        float targetMaxParticleSize = baseMaxParticleSize + (smoothedBass * 0.08f * layerSizeScale) + (beatPulse * 0.06f * layerSizeScale);
        if (limitMaxSize > 0f)
        {
            targetMaxParticleSize = Mathf.Min(targetMaxParticleSize, limitMaxSize);
        }
        particleRenderer.maxParticleSize = Mathf.Max(baseMaxParticleSize, targetMaxParticleSize);

        if (hasLightsModule)
        {
            int lightCap = seeThroughEnabled ? seeThroughMaxLights : vrMaxLights;
            int scaledLightCap = Mathf.Max(1, Mathf.RoundToInt(lightCap * layerEmissionScale));
            int sourceCap = baseMaxLights > 0 ? baseMaxLights : scaledLightCap;
            lightsModule.maxLights = Mathf.Clamp(scaledLightCap, 1, sourceCap);
        }
    }

    private void ApplyParticleField(Color globalColor)
    {
        if (particles == null || particles.Length < mainModule.maxParticles)
        {
            particles = new ParticleSystem.Particle[Mathf.Max(1, mainModule.maxParticles)];
        }

        int aliveCount = particleSystemComponent.GetParticles(particles);
        if (aliveCount <= 0)
        {
            return;
        }

        float now = Time.time;
        float safeDepthDistance = Mathf.Max(0.01f, depthDistance);
        float safeRippleWidth = Mathf.Max(0.01f, rippleWidth);

        for (int i = 0; i < aliveCount; i++)
        {
            Vector3 position = particles[i].position;
            float distance = position.magnitude;
            float distance01 = Mathf.Clamp01(distance / safeDepthDistance);

            Vector3 radialDirection = distance > 0.0001f ? position / distance : Vector3.up;
            Vector3 tangentDirection = Vector3.Cross(Vector3.up, radialDirection);
            if (tangentDirection.sqrMagnitude < 0.0001f)
            {
                tangentDirection = Vector3.Cross(Vector3.right, radialDirection);
            }
            tangentDirection.Normalize();

            float centerFalloff = 1f - (distance01 * 0.65f);
            float rippleBand = 0f;
            if (rippleActive)
            {
                // 波纹圈会从中心向外扫过，经过粒子时给一次额外推力和亮度抬升。
                float distanceToWave = Mathf.Abs(distance - rippleRadius);
                rippleBand = Mathf.Clamp01(1f - (distanceToWave / safeRippleWidth));
            }

            Vector3 velocity = particles[i].velocity * particleDamping;
            velocity += radialDirection * ((smoothedBass * bassRadialForce) + (beatPulse * bassRadialForce * 0.7f) + (rippleBand * rippleForce * layerRippleScale)) * Time.deltaTime * layerVelocityScale;
            velocity += tangentDirection * (smoothedMid * midSwirlForce * (0.6f + centerFalloff)) * Time.deltaTime * layerVelocityScale;
            velocity += Vector3.up * ((smoothedTreble * verticalLift) + (smoothedMid * 0.35f) + (rippleBand * verticalLift * 0.25f)) * Time.deltaTime * layerVelocityScale;
            particles[i].velocity = velocity;

            float flicker = 1f + (Mathf.PingPong((now * (6f + (smoothedTreble * 18f))) + (i * 0.031f), 0.25f) * smoothedTreble);
            float flashBoost = (beatPulse * beatFlashStrength * 0.35f) + (rippleBand * rippleColorBoost * layerRippleScale);

            Color particleColor = Color.Lerp(globalColor, trebleAccentColor, (distance01 * 0.25f) + (smoothedTreble * 0.2f));
            particleColor = Color.Lerp(particleColor, bassAccentColor, (beatPulse * 0.12f) + (rippleBand * 0.18f));
            particleColor = MultiplyColor(particleColor, flicker + flashBoost);
            particleColor.a *= Mathf.Lerp(1f, 0.72f, distance01 * 0.55f);
            particleColor.a = Mathf.Clamp01((particleColor.a + (rippleBand * 0.14f)) * layerAlphaScale);
            particles[i].startColor = particleColor;
        }

        particleSystemComponent.SetParticles(particles, aliveCount);
    }

    private Color MultiplyColor(Color color, float multiplier)
    {
        // 保留一点超过 1 的颜色强度，给 Additive 粒子更多舞台灯光感。
        color.r = Mathf.Clamp(color.r * multiplier, 0f, 1.6f);
        color.g = Mathf.Clamp(color.g * multiplier, 0f, 1.6f);
        color.b = Mathf.Clamp(color.b * multiplier, 0f, 1.6f);
        return color;
    }
}
