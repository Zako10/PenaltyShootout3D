using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class GoalkeeperController : MonoBehaviour
{
    private enum DiveDirection
    {
        Left,
        Center,
        Right
    }

    [Header("Movement")]
    [SerializeField] private Rigidbody goalkeeperRigidbody;
    [SerializeField] private float diveForce = 5f;
    [SerializeField] private float upwardForce = 1.4f;
    [SerializeField] private float easyReactionDelay = 0.45f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isDiving;

    private void Awake()
    {
        if (goalkeeperRigidbody == null)
        {
            goalkeeperRigidbody = GetComponent<Rigidbody>();
        }

        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    public void DiveRandom()
    {
        if (isDiving)
        {
            return;
        }

        DiveDirection direction = (DiveDirection)Random.Range(0, 3);
        Invoke(nameof(PerformDelayedDive), easyReactionDelay);
        pendingDirection = direction;
    }

    private DiveDirection pendingDirection;

    private void PerformDelayedDive()
    {
        Vector3 direction = GetDiveVector(pendingDirection);

        goalkeeperRigidbody.isKinematic = false;
        goalkeeperRigidbody.linearVelocity = Vector3.zero;
        goalkeeperRigidbody.angularVelocity = Vector3.zero;
        goalkeeperRigidbody.AddForce(direction * diveForce + Vector3.up * upwardForce, ForceMode.Impulse);

        isDiving = true;
    }

    public void ResetGoalkeeper()
    {
        CancelInvoke(nameof(PerformDelayedDive));
        isDiving = false;

        transform.SetPositionAndRotation(startPosition, startRotation);
        goalkeeperRigidbody.linearVelocity = Vector3.zero;
        goalkeeperRigidbody.angularVelocity = Vector3.zero;
        goalkeeperRigidbody.isKinematic = true;
    }

    private static Vector3 GetDiveVector(DiveDirection direction)
    {
        switch (direction)
        {
            case DiveDirection.Left:
                return Vector3.left;
            case DiveDirection.Right:
                return Vector3.right;
            default:
                return Vector3.forward * 0.2f;
        }
    }
}
