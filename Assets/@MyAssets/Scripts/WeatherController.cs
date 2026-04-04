using UnityEngine;

public class WeatherController : MonoBehaviour
{
    [Header("Global Weather")]
    [Range(0f, 1f)]
    [Tooltip("0 = Perfectly Calm, 1 = Raging Storm")]
    public float stormIntensity = 0.8f; // Defaulting to your test level!

    [Header("Ocean Control")]
    [SerializeField] private WaveManager waveManager;
    [Tooltip("Multiplier for your Inspector wave steepness when perfectly calm (Intensity 0)")]
    [SerializeField] private float calmSteepnessMultiplier = 0.2f;
    [Tooltip("Multiplier for your Inspector wave steepness at max storm (Intensity 1)")]
    [SerializeField] private float stormSteepnessMultiplier = 1.25f;
    private float[] originalSteepness;

    [Header("Cloud Control")]
    [SerializeField] private GameObject cloudPlane;
    private Material cloudMaterial;
    [Tooltip("The exact name of the density property in your cloud shader")]
    [SerializeField] private string cloudDensityPropertyName = "Density";
    [SerializeField] private float calmCloudDensity = 0.45f;
    [SerializeField] private float stormCloudDensity = 2.0f;
    private int cloudDensityID;

    [Header("Rain Control")]
    [SerializeField] private ParticleSystem foregroundRain;
    [SerializeField] private ParticleSystem backgroundRain;
    [SerializeField] private float maxForegroundDrops = 500f;
    [SerializeField] private float maxBackgroundDrops = 2000f;

    [Header("Atmosphere Control")]
    [SerializeField] private Light sunLight;
    [SerializeField] private Color calmFogColor = new Color(0.6f, 0.7f, 0.8f);
    [SerializeField] private Color stormFogColor = new Color(0.2f, 0.25f, 0.3f);
    [SerializeField] private float calmFogDensity = 0.005f;
    [SerializeField] private float stormFogDensity = 0.03f;
    [SerializeField] private float calmSunIntensity = 1.2f;
    [SerializeField] private float stormSunIntensity = 0.2f;
    [SerializeField] private float calmSkyboxExposure = 1.3f;
    [SerializeField] private float stormSkyboxExposure = 0.4f;
    private float skyboxExposure = 1.3f;
    private Material skyboxMaterial;

    [Header("Wind Control")]
    [SerializeField] public Vector3 windDirection = new Vector3(0, 0, 0);
    private void Start()
    {
        skyboxMaterial = new Material(RenderSettings.skybox);
        RenderSettings.skybox = skyboxMaterial;
        cloudMaterial = new Material(cloudPlane.GetComponent<MeshRenderer>().material);
        cloudPlane.GetComponent<MeshRenderer>().material = cloudMaterial;
        // 1. Memorize the exact wave settings you dialed into the Inspector
        if (waveManager != null && waveManager.waves != null)
        {
            originalSteepness = new float[waveManager.waves.Length];
            for (int i = 0; i < waveManager.waves.Length; i++)
            {
                originalSteepness[i] = waveManager.waves[i].steepness;
            }
        }

        // Cache the shader property ID for performance
        cloudDensityID = Shader.PropertyToID(cloudDensityPropertyName);
    }

    private void Update()
    {
        ApplyWeather();
    }

    private void ApplyWeather()
    {
        // 1. Scale the Ocean Waves based on your custom multipliers
        if (waveManager != null && originalSteepness != null)
        {
            float currentMultiplier = Mathf.Lerp(calmSteepnessMultiplier, stormSteepnessMultiplier, stormIntensity);

            for (int i = 0; i < waveManager.waves.Length; i++)
            {
                waveManager.waves[i].steepness = originalSteepness[i] * currentMultiplier;
            }
        }

        // 2. Control the Cloud Density
        if (cloudMaterial != null)
        {
            float currentDensity = Mathf.Lerp(calmCloudDensity, stormCloudDensity, stormIntensity);
            cloudMaterial.SetFloat(cloudDensityID, currentDensity);
            //Darken the skybox
            skyboxExposure = Mathf.Lerp(calmSkyboxExposure, stormSkyboxExposure, stormIntensity);
            skyboxMaterial.SetFloat("_Exposure", skyboxExposure);

        }

        // 3. Control the Rain Emission
        if (foregroundRain != null && backgroundRain != null)
        {
            var fgEmission = foregroundRain.emission;
            fgEmission.rateOverTime = stormIntensity * maxForegroundDrops;

            var bgEmission = backgroundRain.emission;
            bgEmission.rateOverTime = stormIntensity * maxBackgroundDrops;
        }

        // 4. Update Fog and Sun Lighting
        RenderSettings.fogColor = Color.Lerp(calmFogColor, stormFogColor, stormIntensity);
        RenderSettings.fogDensity = Mathf.Lerp(calmFogDensity, stormFogDensity, stormIntensity);

        if (sunLight != null)
        {
            sunLight.intensity = Mathf.Lerp(calmSunIntensity, stormSunIntensity, stormIntensity);
        }

        //Update Ripple strength

        if (waveManager != null)
        {
            waveManager.rippleStrenght = Mathf.Lerp(0.03f, 0.15f, stormIntensity);
        }
    }
}
