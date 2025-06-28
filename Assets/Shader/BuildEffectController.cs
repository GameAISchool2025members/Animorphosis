using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BuildEffectController : MonoBehaviour
{
    public Material effectMaterial;
    public float buildDuration = 3f;
    [Range(0f, 0.5f)]
    public float buildSmoothness = 0.1f;
    public float startDelay = 0f;
    public float waveAmplitude = 0.1f;
    public float waveFrequency = 2.0f;
    public float waveSpeed = 1.0f;
    public bool buildDown = false;
    public bool pulse = false;
    [Range(0f, 0.5f)]
    public float pulseAmplitude = 0.01f;
    public float pulseSpeed = 1.0f;

    private float timer;
    private float baseProgress;
    private float baseSmoothness;

    void Start()
    {
        timer = -startDelay;
        baseProgress = buildDown ? 1f : 0f;
        baseSmoothness = buildSmoothness;
        if (effectMaterial != null)
            effectMaterial.SetFloat("_BuildProgress", baseProgress);
    }

    void Update()
    {
        if (effectMaterial == null) return;

        effectMaterial.SetFloat("_WaveAmplitude", waveAmplitude);
        effectMaterial.SetFloat("_WaveFrequency", waveFrequency);
        effectMaterial.SetFloat("_WaveSpeed", waveSpeed);

        if (pulse)
        {
            float sin = Mathf.Sin(Time.time * pulseSpeed * 2f * Mathf.PI);
            float smooth = Mathf.Clamp(baseSmoothness + sin * pulseAmplitude, 0.1f, 0.8f);
            effectMaterial.SetFloat("_BuildSmoothness", smooth);
            return;
        }

        timer += Time.deltaTime;
        if (timer < 0f) return;

        float t = Mathf.Clamp01(timer / buildDuration);
        float progress = buildDown ? 1f - t : t;
        effectMaterial.SetFloat("_BuildProgress", progress);
        effectMaterial.SetFloat("_BuildSmoothness", buildSmoothness);

        if (t >= 1f) enabled = false;
    }

    //public void BuildUp()
    //{
    //    pulse = false;
    //    buildDown = false;
    //    baseProgress = 0f;
    //    timer = 0f;
    //    enabled = true;
    //}

    public void BuildDown()
    {
        pulse = false;
        buildDown = true;
        baseProgress = 1f;
        timer = 0f;
        enabled = true;
    }

    public void StartPulse()
    {
        pulse = true;
        baseSmoothness = effectMaterial != null
            ? effectMaterial.GetFloat("_BuildSmoothness")
            : buildSmoothness;
        enabled = true;
    }

    public void StopPulse()
    {
        pulse = false;
        effectMaterial.SetFloat("_BuildSmoothness", buildSmoothness);
        enabled = false;
    }

    //public void OnValidate()
    //{
    //    if (effectMaterial == null) return;
    //    effectMaterial.SetFloat("_WaveAmplitude", waveAmplitude);
    //    effectMaterial.SetFloat("_WaveFrequency", waveFrequency);
    //    effectMaterial.SetFloat("_WaveSpeed", waveSpeed);
    //    effectMaterial.SetFloat("_BuildSmoothness", buildSmoothness);
    //    effectMaterial.SetFloat("_BuildProgress", buildDown ? 0.90f : 0f);
    //}
}
