using UnityEngine;

public class MomentumTracker
{
    private Vector2 currentVelocity = Vector2.zero;
    private float damping = 5f; // configurable in Inspector
    private float minVelocityThreshold = 0.05f;
    private float decelerationRate = 3.5f; // Controls how fast momentum decays

    public void UpdateMomentum(Vector2 delta)
    {
        currentVelocity = delta;
    }

    public void ApplyMomentum(ref Vector3 cameraPosition, Camera cam, float speedMultiplier)
    {
        if (!IsMomentumActive()) return;

        Vector3 right = cam.transform.right;
        Vector3 forward = Vector3.Cross(right, Vector3.up);
        Vector3 move = (-currentVelocity.x * right + -currentVelocity.y * forward) * speedMultiplier * Time.deltaTime;
        cameraPosition += move;

        currentVelocity = Vector2.Lerp(currentVelocity, Vector2.zero, damping * Time.deltaTime);
    }

    public void StopMomentum()
    {
        currentVelocity = Vector2.zero;
    }

    public bool IsMomentumActive()
    {
        return currentVelocity.magnitude > minVelocityThreshold;
    }

    public Vector3 GetMomentumDelta(float panSpeed, float deltaTime)
    {
        if (!IsMomentumActive()) return Vector3.zero;

        Vector2 velocity = currentVelocity;
        velocity *= Mathf.Exp(-decelerationRate * deltaTime); // decay

        currentVelocity = velocity;

        Vector3 right = Camera.main.transform.right;
        Vector3 forward = Vector3.Cross(right, Vector3.up);

        return (-velocity.x * right + -velocity.y * forward) * panSpeed * deltaTime;
    }

}
