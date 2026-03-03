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
    [SerializeField] private WaveSettings[] waves = new WaveSettings[6];

    [Header("Settings")]
    [SerializeField] private float baseWaterHeight = 0f;

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

    private void Update()
    {
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
    }

    private Vector3 CalculateGerstnerWave(WaveSettings wave, Vector2 gridPoint)
    {
        // Safety check matching the shader
        if (wave.wavelength <= 0.001f) return Vector3.zero;

        float k = Mathf.PI * 2f / wave.wavelength;
        float c = Mathf.Sqrt(9.8f / k);
        Vector2 d = wave.direction.normalized;
        
        // Time.time maps exactly to _Time.y in the shader
        float f = k * (Vector2.Dot(d, gridPoint) - c * Time.time);
        float a = wave.steepness / k;
        
        return new Vector3(
            d.x * (a * Mathf.Cos(f)),
            a * Mathf.Sin(f),
            d.y * (a * Mathf.Cos(f))
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
            for (int w = 0; w < waves.Length; w++)
            {
                offset += CalculateGerstnerWave(waves[w], estimatedGridPoint);
            }

            Vector2 error = new Vector2(x - (estimatedGridPoint.x + offset.x), z - (estimatedGridPoint.y + offset.z));
            estimatedGridPoint += error;
        }

        Vector3 finalDisplacement = Vector3.zero;
        
        // Calculate the final height using the corrected grid point
        for (int w = 0; w < waves.Length; w++)
        {
            finalDisplacement += CalculateGerstnerWave(waves[w], estimatedGridPoint);
        }

        return baseWaterHeight + finalDisplacement.y;
    }
}