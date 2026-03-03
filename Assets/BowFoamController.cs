using UnityEngine;

public class BowFoamController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Rigidbody driving the ship")]
    [SerializeField] private Rigidbody shipRigidbody;

    [Tooltip("The Particle System at the front of the ship")]
    [SerializeField] private ParticleSystem bowFoamParticles;

    [Header("Foam Settings")]
    [Tooltip("The maximum amount of particles to spawn per second when sailing fast")]
    [SerializeField] private float maxEmissionRate = 50f;

    [Tooltip("The speed required to reach maximum foam emission")]
    [SerializeField] private float speedForMaxFoam = 10f;

    // We have to cache the emission module to modify it at runtime
    private ParticleSystem.EmissionModule emissionModule;

    private void Awake()
    {
        // Safety check to ensure we have the components
        if (shipRigidbody == null) shipRigidbody = GetComponent<Rigidbody>();

        if (bowFoamParticles != null)
        {
            emissionModule = bowFoamParticles.emission;
        }
    }

    private void Update()
    {
        if (shipRigidbody == null || bowFoamParticles == null) return;

        // 1. Calculate how fast the ship is moving specifically in its FORWARD direction.
        // Vector3.Dot compares the ship's actual velocity against the direction it is pointing.
        float forwardSpeed = Vector3.Dot(shipRigidbody.linearVelocity, -transform.forward);

        // 2. If the ship is moving backward, we don't want bow foam!
        if (forwardSpeed < 0.1f)
        {
            emissionModule.rateOverTime = 0f;
            return;
        }

        // 3. Figure out what percentage of our "max speed" we are currently traveling
        // Mathf.Clamp01 ensures this value stays exactly between 0.0 and 1.0 (0% to 100%)
        float foamPercentage = Mathf.Clamp01(forwardSpeed / speedForMaxFoam);

        // 4. Apply that percentage to our maximum emission rate
        emissionModule.rateOverTime = foamPercentage * maxEmissionRate;
    }
}