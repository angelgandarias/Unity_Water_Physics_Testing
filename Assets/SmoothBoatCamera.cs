using UnityEngine;

public class SmoothBoatCamera : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("The ship you want the camera to follow")]
    [SerializeField] private Transform target;

    [Tooltip("How far behind and above the ship the camera should sit")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -12f);

    [Header("Smoothing")]
    [Tooltip("Lower values mean the camera tracks tighter. Higher values mean a looser, floatier follow.")]
    [SerializeField] private float positionSmoothTime = 0.3f;
    [SerializeField] private float rotationSmoothSpeed = 3f;

    // Required for Vector3.SmoothDamp
    private Vector3 currentVelocity = Vector3.zero;

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. Calculate where the camera *wants* to be based on the ship's current rotation and position
        Vector3 desiredPosition = target.position + target.TransformDirection(offset);

        // 2. Smoothly glide towards that position
        // We use SmoothDamp instead of Lerp because it acts like a physical spring, perfect for tracking physics objects!
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);

        // 3. Calculate the rotation needed to look directly at the ship
        // We add a slight upward offset to the look target so the ship isn't at the very bottom of the screen
        Vector3 lookTarget = target.position + (Vector3.up * 2f);
        Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - transform.position);

        // 4. Smoothly rotate the camera towards the look target
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothSpeed * Time.deltaTime);
    }
}