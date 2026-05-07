using UnityEngine;

public sealed class MovingObstacle : MonoBehaviour
{
    [SerializeField] private float range = 1.2f;
    [SerializeField] private float baseSpeed = 1.05f;
    [SerializeField] private Vector3 axis = Vector3.right;

    private Vector3 startPosition;
    private float difficulty;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        float speed = baseSpeed + difficulty * 1.25f;
        transform.position = startPosition + axis.normalized * Mathf.Sin(Time.time * speed) * range;
        transform.Rotate(Vector3.up, (50f + difficulty * 65f) * Time.deltaTime, Space.World);
    }

    public void SetDifficulty(float value)
    {
        difficulty = Mathf.Clamp01(value);
    }
}
