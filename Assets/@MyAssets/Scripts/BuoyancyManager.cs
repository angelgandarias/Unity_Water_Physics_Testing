using System;
using UnityEngine;

public class BuoyancyManager : MonoBehaviour
{
    Rigidbody rbody;
    [SerializeField] WaveManager waveManager; //TODO Make WaveManager a singleton instead of using it like this
    [SerializeField] Transform[] floaters;
    [SerializeField] float floaterForceMultiplier;
    private float floaterWeight;

    private void Awake()
    {
        if(rbody == null)
        this.rbody = this.GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (floaters.Length < 1)
        {
            CalculateDefaultBuoyancy();
        }
        else
        {
            floaterWeight = floaterForceMultiplier / floaters.Length;
            foreach (Transform floater in floaters)
            {
                CalculateFloaterBuoyancy(floater);
            }
        }
    }

    private void CalculateFloaterBuoyancy(Transform floater)
    {
        float waterHeight = waveManager.WaveHeight(floater.position.x, floater.position.z);
        if (floater.position.y < waterHeight)
        {
            rbody.AddForceAtPosition(Vector3.up * floaterWeight * (waterHeight - floater.position.y),floater.position, ForceMode.Acceleration);
        }
    }

    private void CalculateDefaultBuoyancy()
    {
        if (transform.position.y < waveManager.WaveHeight(transform.position.x, transform.position.z))
        {
            rbody.AddForce(transform.up * 20, ForceMode.Acceleration);
        }
    }
}
