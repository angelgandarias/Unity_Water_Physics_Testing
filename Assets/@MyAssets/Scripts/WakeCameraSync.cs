using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class WakeCameraSync : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign your URP Ocean Material here")]
    [SerializeField] private Material oceanMaterial;
    
    [Tooltip("Assign the Boat/Ship transform here so the camera can follow it")]
    [SerializeField] private Transform targetShip;

    [Header("Settings")]
    [Tooltip("Height above the ocean. Doesn't affect orthographic scale, just keeps it out of the water.")]
    [SerializeField] private float cameraHeight = 50f;

    private Camera wakeCam;
    private readonly int wakeParamsId = Shader.PropertyToID("_WakeParams");

    private void Awake()
    {
        wakeCam = GetComponent<Camera>();
        
        // Force the camera aspect ratio to 1:1. 
        // This prevents the view from stretching inside the square Render Texture!
        wakeCam.aspect = 1f; 
    }

    private void LateUpdate()
    {
        if (oceanMaterial == null || wakeCam == null || targetShip == null) return;

        // 1. Follow the ship's X and Z position, but lock the Y height
        transform.position = new Vector3(targetShip.position.x, cameraHeight, targetShip.position.z);
        
        // 2. Lock the rotation to look perfectly straight down (no pitching or rolling!)
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // 3. Send the parameters to the shader
        Vector4 wakeParams = new Vector4(
            transform.position.x,
            transform.position.z,
            wakeCam.orthographicSize,
            0f 
        );

        oceanMaterial.SetVector(wakeParamsId, wakeParams);
    }
}