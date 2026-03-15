using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class OceanAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private Transform playerCamera;
    
    [Header("Terrain Occlusion")]
    [Tooltip("The physics layer the terrain is in")]
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField] private float inlandMuffledVolume = 0.1f;

    private AudioSource oceanSource;
    private float targetVolume = 1f;
    private float baseMaxVolume;

    private void Awake()
    {
        oceanSource = GetComponent<AudioSource>();
        baseMaxVolume = oceanSource.volume;
    }

    private void Update()
    {
        if (playerCamera == null || waveManager == null) return;

        Vector3 camPos = playerCamera.position;

        // 1. Lock the audio source to the water surface directly under the camera
        float currentWaterHeight = waveManager.WaveHeight(camPos.x, camPos.z);
        transform.position = new Vector3(camPos.x, currentWaterHeight, camPos.z);

        // 2. Check for terrain occlusion (Are we inland?)
        CheckTerrainOcclusion(camPos, currentWaterHeight);

        // 3. Smoothly adjust the volume
        oceanSource.volume = Mathf.Lerp(oceanSource.volume, targetVolume * baseMaxVolume, Time.deltaTime * fadeSpeed);
    }

    private void CheckTerrainOcclusion(Vector3 camPos, float waterHeight)
    {
        // Raycast down from the camera to see if there is land between the player and the water level
        float rayDistance = camPos.y - waterHeight;
        
        // If the camera is below the water level (underwater), just keep it loud for now
        if (rayDistance <= 0) 
        {
            targetVolume = 1f;
            return;
        }

        // Check if land is blocking the water directly below us
        bool isOverLand = Physics.Raycast(camPos, Vector3.down, rayDistance, terrainLayer);

        if (isOverLand)
        {
            // The player is standing on land that is higher than the water. 
            // We fade the volume down heavily. 
            targetVolume = inlandMuffledVolume;
        }
        else
        {
            // The player has a clear line of sight straight down to the water (e.g. on a boat or swimming).
            targetVolume = 1f;
        }
    }
}