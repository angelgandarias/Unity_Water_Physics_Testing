using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    [Header("Engine Settings")]
    [SerializeField] private float enginePower = 20f;
    [SerializeField] private float reversePower = 10f;
    [SerializeField] private float rudderPower = 15f;

    private Rigidbody rbody;
    private float moveInput;
    private float turnInput;

    private void Awake()
    {
        rbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // 1. Gather input in Update (better for responsiveness)
        // Default axes: W/S or Up/Down arrows for Vertical, A/D or Left/Right arrows for Horizontal
        moveInput = Input.GetAxis("Vertical");
        turnInput = -Input.GetAxis("Horizontal");
    }

    private void FixedUpdate()
    {
        // 2. Apply physics in FixedUpdate
        ApplyThrust();
        ApplySteering();
    }

    private void ApplyThrust()
    {
        if (Mathf.Abs(moveInput) > 0.05f)
        {
            // Use different power based on going forward or backward
            float power = moveInput > 0 ? enginePower : reversePower;

            // Push the ship in the direction it is currently facing
            rbody.AddForce(transform.forward * (moveInput * power), ForceMode.Acceleration);
        }
    }

    private void ApplySteering()
    {
        if (Mathf.Abs(turnInput) > 0.05f)
        {
            // Apply torque (rotational force) around the ship's local Up axis
            // We multiply by moveInput so the ship turns faster when moving, and reverses steering when going backward!
            // (If you want to be able to turn in place, just remove the moveInput multiplier)
            float turnMultiplier = Mathf.Clamp(moveInput, -1f, 1f);

            // Fallback: If we aren't moving forward/back, let it turn slowly in place
            if (Mathf.Abs(turnMultiplier) < 0.1f) turnMultiplier = 0.5f;

            rbody.AddTorque(transform.up * (turnInput * rudderPower * turnMultiplier), ForceMode.Acceleration);
        }
    }
}