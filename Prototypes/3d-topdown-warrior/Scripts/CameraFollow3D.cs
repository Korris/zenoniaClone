using UnityEngine;

/// <summary>Ragnarok-style follow camera: fixed yaw, pitched down, smooth follow; mouse wheel zooms, Q / E rotate the view in 45° steps.</summary>
public class CameraFollow3D : MonoBehaviour
{
    public Transform Target;
    public float Pitch = 50f, Yaw = 0f, Distance = 7.5f, MinDistance = 4f, MaxDistance = 20f, Smooth = 8f;
    float yawGoal;

    void Start() { yawGoal = Yaw; Snap(); }

    void Snap() { transform.position = Goal(); transform.rotation = Quaternion.Euler(Pitch, Yaw, 0f); }

    Vector3 Goal()
    {
        Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0f);
        return Target.position + Vector3.up * 0.6f - rot * Vector3.forward * Distance;
    }

    void LateUpdate()
    {
        if (Target == null) return;
        Distance = Mathf.Clamp(Distance - Input.mouseScrollDelta.y * 1.2f, MinDistance, MaxDistance);
        if (Input.GetKeyDown(KeyCode.Q)) yawGoal -= 45f;
        if (Input.GetKeyDown(KeyCode.E)) yawGoal += 45f;
        Yaw = Mathf.LerpAngle(Yaw, yawGoal, 1f - Mathf.Exp(-10f * Time.deltaTime));
        transform.position = Vector3.Lerp(transform.position, Goal(), 1f - Mathf.Exp(-Smooth * Time.deltaTime));
        transform.rotation = Quaternion.Euler(Pitch, Yaw, 0f);
    }
}
