using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class BallController : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Rigidbody ballRigidbody;
    [SerializeField] private Transform resetPoint;

    [Header("Shot Rules")]
    [SerializeField] private float missYLimit = -1f;
    [SerializeField] private float missDistance = 28f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isShotActive;
    private bool isResolved;

    public event Action GoalScored;
    public event Action Missed;
    public event Action Saved;

    private void Awake()
    {
        if (ballRigidbody == null)
        {
            ballRigidbody = GetComponent<Rigidbody>();
        }

        startPosition = resetPoint != null ? resetPoint.position : transform.position;
        startRotation = resetPoint != null ? resetPoint.rotation : transform.rotation;
    }

    private void Update()
    {
        if (!isShotActive || isResolved)
        {
            return;
        }

        if (transform.position.y < missYLimit || Vector3.Distance(startPosition, transform.position) > missDistance)
        {
            ResolveMiss();
        }
    }

    public void Kick(Vector3 direction, float force)
    {
        isShotActive = true;
        isResolved = false;

        ballRigidbody.isKinematic = false;
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
        ballRigidbody.AddForce(direction.normalized * force, ForceMode.Impulse);
    }

    public void ResetBall()
    {
        isShotActive = false;
        isResolved = false;

        transform.SetPositionAndRotation(startPosition, startRotation);
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
        ballRigidbody.isKinematic = true;
    }

    public void ResolveGoal()
    {
        if (!CanResolve())
        {
            return;
        }

        GoalScored?.Invoke();
    }

    public void ResolveSave()
    {
        if (!CanResolve())
        {
            return;
        }

        Saved?.Invoke();
    }

    public void ResolveMiss()
    {
        if (!CanResolve())
        {
            return;
        }

        Missed?.Invoke();
    }

    private bool CanResolve()
    {
        if (isResolved)
        {
            return false;
        }

        isResolved = true;
        isShotActive = false;
        return true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isShotActive || isResolved)
        {
            return;
        }

        if (collision.collider.TryGetComponent(out GoalkeeperController _))
        {
            ResolveSave();
        }
    }
}
