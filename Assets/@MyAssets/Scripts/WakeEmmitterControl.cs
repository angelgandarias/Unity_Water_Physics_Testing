using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class WakeEmitterControl : MonoBehaviour
{
    [Tooltip("Reference to your Wave Manager so we can calculate the water height")]
    [SerializeField] private WaveManager waveManager;
    
    [Tooltip("How far above the water can the emitter go before it stops drawing?")]
    [SerializeField] private float airTolerance = 0.5f;

    private TrailRenderer trail;

    private void Awake()
    {
        trail = GetComponent<TrailRenderer>();
    }

    private void Update()
    {
        if (waveManager == null) return;

        // Find the exact height of the mathematical waves right beneath the emitter
        float currentWaterHeight = waveManager.WaveHeight(transform.position.x, transform.position.z);

        // If the emitter is higher than the water (plus our tolerance), cut the trail!
        if (transform.position.y > currentWaterHeight + airTolerance)
        {
            trail.emitting = false;
        }
        else
        {
            trail.emitting = true;
        }
    }
}