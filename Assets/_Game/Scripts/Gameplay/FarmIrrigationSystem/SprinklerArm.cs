using UnityEngine;

public class SprinklerArm : MonoBehaviour
{
    [SerializeField] private SprinklerArmConfig config;
    [SerializeField] private Transform armPivot;
    [SerializeField] private GameObject[] sprinklers;

    public SprinklerArmConfig Config => config;
    public Transform Pivot => armPivot != null ? armPivot : transform;
    public int CurrentArmLength { get; private set; } = 1;
    private float RotationOffsetZ => config != null ? config.rotationOffsetZ : 0f;

    private void Awake()
    {
        if (armPivot == null)
        {
            armPivot = transform;
        }

        ApplyInitialRotation();
    }

    private void ApplyInitialRotation(float additionalAngle = 0f)
    {
        float offsetZ = RotationOffsetZ + additionalAngle;
        if (Pivot != null)
        {
            Pivot.localRotation = Quaternion.Euler(0f, 0f, offsetZ);
        }
    }

    public void RotateArm(float speed, float deltaTime)
    {
        if (Pivot == null || Mathf.Approximately(speed, 0f))
            return;

        float rotation = -speed * deltaTime;
        Pivot.Rotate(0f, 0f, rotation, Space.Self);
    }

    public void SetArmLength(int length)
    {
        CurrentArmLength = Mathf.Max(1, length);
        if (sprinklers == null || sprinklers.Length == 0)
            return;

        int targetIndex = Mathf.Clamp(length - 1, 0, sprinklers.Length - 1);
        for (int i = 0; i < sprinklers.Length; i++)
        {
            if (sprinklers[i] != null)
            {
                sprinklers[i].SetActive(i <= targetIndex);
            }
        }
    }

    public Vector3 GetForwardDirection(float additionalAngleOffset = 0f)
    {
        Vector3 forward = Pivot.right;
        if (Mathf.Approximately(additionalAngleOffset, 0f))
            return forward;

        Quaternion offset = Quaternion.Euler(0f, 0f, additionalAngleOffset);
        return (offset * forward).normalized;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform p = armPivot != null ? armPivot : transform;
        Gizmos.color = new Color(0.1f, 0.6f, 1f, 0.9f);
        Gizmos.DrawRay(p.position, GetForwardDirection() * 2f);
    }
#endif
}
