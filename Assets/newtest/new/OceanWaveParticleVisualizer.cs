using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class OceanWaveParticleVisualizer : MonoBehaviour
{
    [Header("音频")]
    public AudioSource audioSource;

    [Header("粒子平面")]
    public int gridX = 80;
    public int gridZ = 80;
    public float spacing = 0.18f;
    public float planeWidthOffset = 0f;
    public float planeDepthOffset = 0f;

    [Header("波浪形态")]
    public float baseWaveHeight = 0.6f;
    public float musicWaveBoost = 4f;
    public float waveSpeed = 2.5f;
    public float waveFrequencyX = 1.2f;
    public float waveFrequencyZ = 1.4f;
    public float secondaryWaveStrength = 0.45f;

    [Header("音频控制")]
    public float audioGain = 18f;
    public float smoothing = 8f;

    [Header("外观")]
    public Gradient colorGradient;
    public float baseSize = 0.08f;
    public float sizeBoost = 0.15f;
    public float brightnessBoost = 2.0f;

    [Header("材质发光(可选)")]
    public Renderer targetRenderer;
    public string baseColorProperty = "_BaseColor";
    public string emissionProperty = "_EmissionColor";

    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    private Vector3[] basePositions;
    private float[] spectrum = new float[512];
    private MaterialPropertyBlock mpb;

    private float lowBand;
    private float midBand;
    private float highBand;
    private float volumeBand;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        mpb = new MaterialPropertyBlock();

        int count = gridX * gridZ;
        particles = new ParticleSystem.Particle[count];
        basePositions = new Vector3[count];

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = count;
        main.startSpeed = 0f;
        main.startLifetime = Mathf.Infinity;
        main.startSize = baseSize;

        BuildGrid();

        ps.Clear();
        ps.Emit(count);
        ps.GetParticles(particles);

        for (int i = 0; i < count; i++)
        {
            particles[i].position = basePositions[i];
            particles[i].startSize = baseSize;
            if (colorGradient != null)
            {
                float t = (float)i / Mathf.Max(1, count - 1);
                particles[i].startColor = colorGradient.Evaluate(t);
            }
        }

        ps.SetParticles(particles, count);
    }

    void Start()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    void BuildGrid()
    {
        int index = 0;
        float halfX = (gridX - 1) * spacing * 0.5f;
        float halfZ = (gridZ - 1) * spacing * 0.5f;

        for (int z = 0; z < gridZ; z++)
        {
            for (int x = 0; x < gridX; x++)
            {
                float px = x * spacing - halfX + planeWidthOffset;
                float pz = z * spacing - halfZ + planeDepthOffset;

                basePositions[index] = new Vector3(px, 0f, pz);
                index++;
            }
        }
    }

    void Update()
    {
        if (audioSource == null)
            return;

        AnalyzeAudio();
        UpdateWaveParticles();
        UpdateMaterialGlow();
    }

    void AnalyzeAudio()
    {
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        float low = BandAverage(0, 16);
        float mid = BandAverage(16, 64);
        float high = BandAverage(64, 256);
        float vol = (low + mid + high) / 3f;

        lowBand = Mathf.Lerp(lowBand, Mathf.Clamp01(low * audioGain), Time.deltaTime * smoothing);
        midBand = Mathf.Lerp(midBand, Mathf.Clamp01(mid * audioGain), Time.deltaTime * smoothing);
        highBand = Mathf.Lerp(highBand, Mathf.Clamp01(high * audioGain), Time.deltaTime * smoothing);
        volumeBand = Mathf.Lerp(volumeBand, Mathf.Clamp01(vol * audioGain), Time.deltaTime * smoothing);
    }

    float BandAverage(int start, int end)
    {
        start = Mathf.Clamp(start, 0, spectrum.Length - 1);
        end = Mathf.Clamp(end, start + 1, spectrum.Length);

        float sum = 0f;
        int count = 0;

        for (int i = start; i < end; i++)
        {
            sum += spectrum[i];
            count++;
        }

        return count > 0 ? sum / count : 0f;
    }

    void UpdateWaveParticles()
    {
        ps.GetParticles(particles);

        float time = Time.time;
        int count = particles.Length;

        float amp = baseWaveHeight + lowBand * musicWaveBoost;
        float size = baseSize + midBand * sizeBoost + volumeBand * 0.05f;

        for (int i = 0; i < count; i++)
        {
            Vector3 p = basePositions[i];

            float nx = p.x * waveFrequencyX;
            float nz = p.z * waveFrequencyZ;

            float wave1 = Mathf.Sin(nx + time * waveSpeed + lowBand * 4f);
            float wave2 = Mathf.Cos(nz * 1.3f - time * (waveSpeed * 0.8f) + midBand * 3f);
            float wave3 = Mathf.Sin((nx + nz) * 0.7f + time * (waveSpeed * 1.2f) + highBand * 6f);

            float oceanWave = (wave1 + wave2 * 0.8f + wave3 * secondaryWaveStrength) / 2.5f;

            p.y = oceanWave * amp;

            particles[i].position = p;
            particles[i].startSize = size;

            if (colorGradient != null)
            {
                float t = Mathf.Clamp01((oceanWave * 0.5f + 0.5f) + highBand * 0.15f);
                Color c = colorGradient.Evaluate(t);
                c *= 1f + volumeBand * brightnessBoost;
                particles[i].startColor = c;
            }
        }

        ps.SetParticles(particles, count);
    }

    void UpdateMaterialGlow()
    {
        if (targetRenderer == null || colorGradient == null)
            return;

        targetRenderer.GetPropertyBlock(mpb);

        Color c = colorGradient.Evaluate(Mathf.Clamp01(0.5f + lowBand * 0.5f));
        float intensity = 1f + volumeBand * brightnessBoost + highBand * 1.5f;

        mpb.SetColor(baseColorProperty, c * intensity);
        mpb.SetColor(emissionProperty, c * intensity * 2f);

        targetRenderer.SetPropertyBlock(mpb);
    }
}