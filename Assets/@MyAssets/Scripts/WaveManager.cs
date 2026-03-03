using UnityEngine;

[ExecuteAlways]
public class WaveManager : MonoBehaviour
{
    [Header("Material Sync")]
    [Tooltip("Assign your URP Ocean Material here")]
    [SerializeField] private Material oceanMaterial;

    [Header("Wave 1")]
    [SerializeField] private Vector2 wave1Direction = new Vector2(1, 0);
    [SerializeField] private float wave1Steepness = 0.5f;
    [SerializeField] private float wave1Wavelength = 10f;

    [Header("Wave 2")]
    [SerializeField] private Vector2 wave2Direction = new Vector2(0, 1);
    [SerializeField] private float wave2Steepness = 0.25f;
    [SerializeField] private float wave2Wavelength = 20f;

    [Header("Wave 3")]
    [SerializeField] private Vector2 wave3Direction = new Vector2(1, 1);
    [SerializeField] private float wave3Steepness = 0.15f;
    [SerializeField] private float wave3Wavelength = 10f;

    [Header("Settings")]
    [SerializeField] private float baseWaterHeight = 0f;

    // Shader.PropertyToID is highly recommended for performance instead of using string names constantly
    private readonly int waveAId = Shader.PropertyToID("_WaveA");
    private readonly int waveBId = Shader.PropertyToID("_WaveB");
    private readonly int waveCId = Shader.PropertyToID("_WaveC");

    private void Update()
    {
        SyncMaterial();
    }

    private void SyncMaterial()
    {
        if (oceanMaterial == null) return;

        Vector4 waveA = new Vector4(wave1Direction.x, wave1Direction.y, wave1Steepness, wave1Wavelength);
        Vector4 waveB = new Vector4(wave2Direction.x, wave2Direction.y, wave2Steepness, wave2Wavelength);
        Vector4 waveC = new Vector4(wave3Direction.x, wave3Direction.y, wave3Steepness, wave3Wavelength);

        // Send the packed data to the shader
        oceanMaterial.SetVector(waveAId, waveA);
        oceanMaterial.SetVector(waveBId, waveB);
        oceanMaterial.SetVector(waveCId, waveC);
    }


    private Vector3 CalculateGerstnerWave(Vector2 waveDirection, float wavelength, float steepness, Vector2 gridPoint)
    {
        float k = Mathf.PI * 2f / wavelength;
        float c = Mathf.Sqrt(9.8f / k);
        waveDirection.Normalize();

        float f = k * (Vector2.Dot(waveDirection, gridPoint) - c * Time.time);
        float a = steepness / k;

        return new Vector3(
            waveDirection.x * (a * Mathf.Cos(f)),
            a * Mathf.Sin(f),
            waveDirection.y * (a * Mathf.Cos(f))
        );
    }

    public float WaveHeight(float x, float z)
    {
        Vector2 estimatedGridPoint = new Vector2(x, z);

        for (int i = 0; i < 3; i++)
        {
            Vector3 offset = Vector3.zero;
            offset += CalculateGerstnerWave(wave1Direction, wave1Wavelength, wave1Steepness, estimatedGridPoint);
            offset += CalculateGerstnerWave(wave2Direction, wave2Wavelength, wave2Steepness, estimatedGridPoint);
            offset += CalculateGerstnerWave(wave3Direction, wave3Wavelength, wave3Steepness, estimatedGridPoint);

            Vector2 error = new Vector2(x - (estimatedGridPoint.x + offset.x), z - (estimatedGridPoint.y + offset.z));
            estimatedGridPoint += error;
        }

        Vector3 finalDisplacement = Vector3.zero;
        finalDisplacement += CalculateGerstnerWave(wave1Direction, wave1Wavelength, wave1Steepness, estimatedGridPoint);
        finalDisplacement += CalculateGerstnerWave(wave2Direction, wave2Wavelength, wave2Steepness, estimatedGridPoint);
        finalDisplacement += CalculateGerstnerWave(wave3Direction, wave3Wavelength, wave3Steepness, estimatedGridPoint);

        return baseWaterHeight + finalDisplacement.y;
    }
}