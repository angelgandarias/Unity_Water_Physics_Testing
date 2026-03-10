using UnityEngine;

[ExecuteAlways] 
public class WaveManager : MonoBehaviour
{
    // We create a clean struct to hold our wave data, making the Inspector much tidier
    [System.Serializable]
    public struct WaveSettings
    {
        public Vector2 direction;
        [Range(0, 1)] public float steepness;
        public float wavelength;
    }

    [Header("Material Sync")]
    [Tooltip("Assign your URP Ocean Material here")]
    [SerializeField] private Material oceanMaterial;

    [Header("Waves Setup")]
    [Tooltip("Make sure there are exactly 6 elements to match the shader")]
    [SerializeField] public WaveSettings[] waves = new WaveSettings[6];

    [Header("Settings")]
    [SerializeField] private float baseWaterHeight = 0f;
    [SerializeField] public float rippleStrenght = 0.15f;

    // We store our Property IDs in an array to match our waves array
    private readonly int[] wavePropertyIDs = new int[]
    {
        Shader.PropertyToID("_Wave1"),
        Shader.PropertyToID("_Wave2"),
        Shader.PropertyToID("_Wave3"),
        Shader.PropertyToID("_Wave4"),
        Shader.PropertyToID("_Wave5"),
        Shader.PropertyToID("_Wave6")
    };
    private readonly int rippleStrenghtID = Shader.PropertyToID("_NormalStrength");


    //The wave data is saved so we only have to calculate it once each frame
    private struct CachedWaveData
    {
        public Vector2 normalizedDir;
        public float k;
        public float c;
        public float a;
    }

    private CachedWaveData[] cachedWaveData = new CachedWaveData[6];

    private void Update()
    {
        UpdateWaveCache();
        SyncMaterial();
    }

    private void SyncMaterial()
    {
        if (oceanMaterial == null || waves == null) return;

        // Loop through the array and push data to the shader
        for (int i = 0; i < Mathf.Min(waves.Length, wavePropertyIDs.Length); i++)
        {
            Vector4 waveData = new Vector4(waves[i].direction.x, waves[i].direction.y, waves[i].steepness, waves[i].wavelength);
            oceanMaterial.SetVector(wavePropertyIDs[i], waveData);
        }
        oceanMaterial.SetFloat(rippleStrenghtID, rippleStrenght);
    }
    private void UpdateWaveCache()
    {
        for (int i = 0; i < cachedWaveData.Length; i++)
        {
            cachedWaveData[i] = CalculateCache(waves[i]);
        }
    }
    private CachedWaveData CalculateCache(WaveSettings wave)
    {
        CachedWaveData cache = new CachedWaveData();
        // Prevent division by zero if you accidentally set wavelength to 0 in the inspector
        float wLength = Mathf.Max(0.001f, wave.wavelength); 
        
        cache.k = Mathf.PI * 2f / wLength;
        cache.c = Mathf.Sqrt(9.8f / cache.k);
        cache.normalizedDir = wave.direction.normalized;
        cache.a = wave.steepness / cache.k;
        
        return cache;
    }

    private Vector3 CalculateGerstnerWave(CachedWaveData cache, Vector2 gridPoint)
    {

        
        // Time.time maps exactly to _Time.y in the shader
        float f = cache.k * (Vector2.Dot(cache.normalizedDir, gridPoint) - cache.c * Time.time);
        
        return new Vector3(
            cache.normalizedDir.x * (cache.a * Mathf.Cos(f)),
            cache.a * Mathf.Sin(f),
            cache.normalizedDir.y * (cache.a * Mathf.Cos(f))
        );
    }

    public float WaveHeight(float x, float z)
    {
        Vector2 estimatedGridPoint = new Vector2(x, z);

        // 3 iterations of approximation to find the correct origin point
        for (int i = 0; i < 3; i++) 
        {
            Vector3 offset = Vector3.zero;
            
            // Loop through all waves to accumulate the offset
            for (int w = 0; w < cachedWaveData.Length; w++)
            {
                offset += CalculateGerstnerWave(cachedWaveData[w], estimatedGridPoint);
            }

            Vector2 error = new Vector2(x - (estimatedGridPoint.x + offset.x), z - (estimatedGridPoint.y + offset.z));
            estimatedGridPoint += error;
        }

        Vector3 finalDisplacement = Vector3.zero;
        
        // Calculate the final height using the corrected grid point
        for (int w = 0; w < cachedWaveData.Length; w++)
        {
            finalDisplacement += CalculateGerstnerWave(cachedWaveData[w], estimatedGridPoint);
        }

        return baseWaterHeight + finalDisplacement.y;
    }
}