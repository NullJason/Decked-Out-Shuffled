using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class FlickeringLight : MonoBehaviour
{
    // Reference
    Light2D light2D;

    [Header("Smooth (slow) variation")]
    public bool enableSmoothVariation = true;
    public float smoothSpeed = 1f; // speed of interpolation / noise
    public float intensityMin = 0.6f;
    public float intensityMax = 1.1f;
    public float innerRadiusMin = 0.0f;
    public float innerRadiusMax = 0.0f;
    public float outerRadiusMin = 3f;
    public float outerRadiusMax = 4f;
    public float innerAngleMin = 0f; // for spot-like lights
    public float innerAngleMax = 0f;
    public float outerAngleMin = 360f;
    public float outerAngleMax = 360f;
    public float falloffMin = 0.1f; // maps to shapeLightFalloffSize
    public float falloffMax = 1f;

    [Header("Broken-bulb flicker bursts")]
    public bool enableFlickerBursts = true;
    public float averageSecondsBetweenBursts = 4f; // sampling mean (we randomize)
    public int minFlickersPerBurst = 3;
    public int maxFlickersPerBurst = 12;
    public float minFlickerDuration = 0.03f; // how short each flicker can be
    public float maxFlickerDuration = 0.18f; // how long each flicker can be
    [Range(0f,1f)]
    public float flickerDimAmount = 0.0f; // how dim during a flicker (0 = off, 1 = no change)
    public float flickerIntensitySpikeChance = 0.05f; // tiny chance a flicker spawns a spike

    // internal state
    float noiseOffset;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        noiseOffset = Random.Range(0f, 100f);
    }

    void OnEnable()
    {
        if (enableFlickerBursts)
            StartCoroutine(FlickerBurstLoop());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    void Update()
    {
        // Smooth variation using Perlin noise (givs natural cheap oscillation)
        if (enableSmoothVariation)
        {
            float t = Time.time * smoothSpeed + noiseOffset;

            // Intensity
            float nI = Mathf.PerlinNoise(t, 0f); // 0..1
            light2D.intensity = Mathf.Lerp(intensityMin, intensityMax, nI);

            // Radii
            light2D.pointLightInnerRadius = Mathf.Lerp(innerRadiusMin, innerRadiusMax, Mathf.PerlinNoise(t + 10f, 0f));
            light2D.pointLightOuterRadius = Mathf.Lerp(outerRadiusMin, outerRadiusMax, Mathf.PerlinNoise(t + 20f, 0f));

            // Angles (use separate noise channels)
            light2D.pointLightInnerAngle = Mathf.Lerp(innerAngleMin, innerAngleMax, Mathf.PerlinNoise(t + 30f, 0f));
            light2D.pointLightOuterAngle = Mathf.Lerp(outerAngleMin, outerAngleMax, Mathf.PerlinNoise(t + 40f, 0f));

            // Falloff (shape falloff size)
            light2D.shapeLightFalloffSize = Mathf.Lerp(falloffMin, falloffMax, Mathf.PerlinNoise(t + 50f, 0f));
        }
    }

    IEnumerator FlickerBurstLoop()
    {
        // loop forever; wait a randomized interval between bursts
        while (true)
        {
            float wait = Mathf.Max(0.1f, Random.Range(averageSecondsBetweenBursts * 0.5f, averageSecondsBetweenBursts * 1.5f));
            yield return new WaitForSeconds(wait);

            // Start a burst:
            int flicks = Random.Range(minFlickersPerBurst, maxFlickersPerBurst + 1);
            for (int i = 0; i < flicks; i++)
            {
                // Decide flicker type: dim or spike
                bool spike = Random.value < flickerIntensitySpikeChance;
                float originalIntensity = light2D.intensity;
                float targetIntensity;

                if (spike)
                {
                    // brief flash up to 150% of max (tweak as you like)
                    targetIntensity = Mathf.Lerp(intensityMax, intensityMax * 1.5f, Random.value);
                }
                else
                {
                    // dim to flickerDimAmount fraction between 0..original
                    targetIntensity = Mathf.Lerp(0f, originalIntensity, flickerDimAmount);
                }

                float dur = Random.Range(minFlickerDuration, maxFlickerDuration);
                // quick interpolate to target and back
                float half = dur * 0.5f;
                // lerp down
                yield return DoQuickLerpIntensity(originalIntensity, targetIntensity, half);
                // lerp back up
                yield return DoQuickLerpIntensity(targetIntensity, originalIntensity, half);

                // small pause between flickers inside the burst
                float pause = Random.Range(0.01f, 0.12f);
                yield return new WaitForSeconds(pause);
            }
        }
    }

    // small coroutine helper to animate intensity over duration
    IEnumerator DoQuickLerpIntensity(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            light2D.intensity = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            // smoothstep for slightly nicer curve
            float s = Mathf.SmoothStep(from, to, p);
            light2D.intensity = s;
            yield return null;
        }

        light2D.intensity = to;
    }
}
