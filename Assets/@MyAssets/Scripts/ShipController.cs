using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    // 1. Define an enum for the possible forward axes
    public enum AxisDirection
    {
        ForwardZ,
        BackwardZ,
        RightX,
        LeftX,
        UpY,
        DownY
    }

    [Header("Axis Settings")]
    [Tooltip("Select which local axis represents the forward direction of the ship model.")]
    [SerializeField] private AxisDirection forwardAxis = AxisDirection.ForwardZ;

    [Header("Engine Settings")]
    [SerializeField] private float enginePower = 20f;
    [SerializeField] private float reversePower = 10f;
    [SerializeField] private float rudderPower = 15f;

    [SerializeField] private bool invertTurn = true;

    private Rigidbody rbody;
    private float moveInput;
    private float turnInput;

    private void Awake()
    {
        rbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // Gather input in Update (better for responsiveness)
        // Default axes: W/S or Up/Down arrows for Vertical, A/D or Left/Right arrows for Horizontal
        moveInput = Input.GetAxis("Vertical");
        if(invertTurn){
            turnInput = -Input.GetAxis("Horizontal");
        }
        else
        {
            turnInput = Input.GetAxis("Horizontal");
        }
        if(Input.GetKey(KeyCode.Escape)) Application.Quit();
    }

    private void FixedUpdate()
    {
        // Apply physics in FixedUpdate
        ApplyThrust();
        ApplySteering();
    }

    // 2. Helper method to return the correct local directional vector
    private Vector3 GetForwardVector()
    {
        switch (forwardAxis)
        {
            case AxisDirection.ForwardZ: return transform.forward;
            case AxisDirection.BackwardZ: return -transform.forward;
            case AxisDirection.RightX: return transform.right;
            case AxisDirection.LeftX: return -transform.right;
            case AxisDirection.UpY: return transform.up;
            case AxisDirection.DownY: return -transform.up;
            default: return transform.forward;
        }
    }

    private void ApplyThrust()
    {
        if (Mathf.Abs(moveInput) > 0.05f)
        {
            // Use different power based on going forward or backward
            float power = moveInput > 0 ? enginePower : reversePower;

            // 3. Push the ship in the custom forward direction
            rbody.AddForce(GetForwardVector() * (moveInput * power), ForceMode.Acceleration);
        }
    }

    private void ApplySteering()
    {
        if (Mathf.Abs(turnInput) > 0.05f)
        {
            // Apply torque (rotational force) around the ship's local Up axis
            // We multiply by moveInput so the ship turns faster when moving, and reverses steering when going backward!
            float turnMultiplier = Mathf.Clamp(moveInput, -1f, 1f);

            // Fallback: If we aren't moving forward/back, let it turn slowly in place
            if (Mathf.Abs(turnMultiplier) < 0.1f) turnMultiplier = 0.5f;

            rbody.AddTorque(transform.up * (turnInput * rudderPower * turnMultiplier), ForceMode.Acceleration);
        }
    }
}