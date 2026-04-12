using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class MusicWaveParticleVisualizer : MonoBehaviour
{
    [Header("音频源")]
    public AudioSource audioSource;

    [Header("粒子布局")]
    public int particleCount = 256;
    public float lineWidth = 10f;
    public float baseHeight = 0f;

    [Header("波浪控制")]
    public float waveSpeed = 6f;
    public float waveCount = 4f;
    public float baseAmplitude = 1.5f;
    public float bassAmplitudeBoost = 4f;

    [Header("外观控制")]
    public Gradient particleGradient;
    public float baseSize = 0.08f;
    public float midSizeBoost = 0.22f;
    public float highJitter = 0.25f;

    [Header("整体灵敏度")]
    public float gain = 12f;
    public float smoothing = 8f;

    [Header("可选：驱动材质发光")]
    public Renderer targetRenderer;
    public string baseColorProperty = "_BaseColor";
    public string emissionProperty = "_EmissionColor";

    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    private float[] spectrum;

    private float lowBand;
    private float midBand;
    private float highBand;
    private float volumeBand;

    private MaterialPropertyBlock mpb;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[particleCount];
        spectrum = new float[512];
        mpb = new MaterialPropertyBlock();

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = particleCount;
        main.startSpeed = 0f;
        main.startLifetime = Mathf.Infinity;
        main.startSize = baseSize;

        ps.Clear();
        ps.Emit(particleCount);
        ps.GetParticles(particles);
    }

    void Start()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    void Update()
    {
        if (audioSource == null)
            return;

        AnalyzeAudio();
        UpdateParticles();
        UpdateAppearance();
    }

    void AnalyzeAudio()
    {
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        float low = BandAverage(0, 16);
        float mid = BandAverage(16, 64);
        float high = BandAverage(64, 256);

        float vol = (low + mid + high) / 3f;

        lowBand = Mathf.Lerp(lowBand, Mathf.Clamp01(low * gain), Time.deltaTime * smoothing);
        midBand = Mathf.Lerp(midBand, Mathf.Clamp01(mid * gain), Time.deltaTime * smoothing);
        highBand = Mathf.Lerp(highBand, Mathf.Clamp01(high * gain), Time.deltaTime * smoothing);
        volumeBand = Mathf.Lerp(volumeBand, Mathf.Clamp01(vol * gain), Time.deltaTime * smoothing);
    }

    float BandAverage(int start, int end)
    {
        float sum = 0f;
        int count = 0;

        start = Mathf.Clamp(start, 0, spectrum.Length - 1);
        end = Mathf.Clamp(end, 1, spectrum.Length);

        for (int i = start; i < end; i++)
        {
            sum += spectrum[i];
            count++;
        }

        return count > 0 ? sum / count : 0f;
    }

    void UpdateParticles()
    {
        ps.GetParticles(particles);

        float amp = baseAmplitude + lowBand * bassAmplitudeBoost;
        float tTime = Time.time;

        for (int i = 0; i < particles.Length; i++)
        {
            float t = particles.Length <= 1 ? 0f : i / (particles.Length - 1f);
            float x = Mathf.Lerp(-lineWidth * 0.5f, lineWidth * 0.5f, t);

            float wave = Mathf.Sin((t * waveCount * Mathf.PI * 2f) + (tTime * waveSpeed * (1f + midBand)));
            float y = baseHeight + wave * amp;

            float z = Mathf.Sin(tTime * 2f + t * 10f) * highJitter * highBand;

            particles[i].position = new Vector3(x, y, z);

            float size = baseSize + midBand * midSizeBoost + volumeBand * 0.08f;
            particles[i].startSize = size;

            if (particleGradient != null)
            {
                float colorT = Mathf.Clamp01(t + highBand * 0.2f);
                Color c = particleGradient.Evaluate(colorT);
                c *= (1f + volumeBand * 1.5f);
                particles[i].startColor = c;
            }
        }

        ps.SetParticles(particles, particles.Length);
    }

    void UpdateAppearance()
    {
        if (targetRenderer == null || particleGradient == null)
            return;

        targetRenderer.GetPropertyBlock(mpb);

        Color c = particleGradient.Evaluate(Mathf.Clamp01(volumeBand + highBand * 0.5f));
        float intensity = 1f + volumeBand * 4f + highBand * 2f;

        mpb.SetColor(baseColorProperty, c * intensity);
        mpb.SetColor(emissionProperty, c * intensity * 2f);

        targetRenderer.SetPropertyBlock(mpb);
    }
}