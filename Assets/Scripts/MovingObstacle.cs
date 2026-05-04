using UnityEngine;

public sealed class MovingObstacle : MonoBehaviour
{
    [SerializeField] private float range = 1.2f;
    [SerializeField] private float baseSpeed = 0.75f;
    [SerializeField] private Vector3 axis = Vector3.right;

    private Vector3 startPosition;
    private float difficulty;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        float speed = baseSpeed + difficulty * 0.9f;
        transform.position = startPosition + axis.normalized * Mathf.Sin(Time.time * speed) * range;
        transform.Rotate(Vector3.up, (35f + difficulty * 45f) * Time.deltaTime, Space.World);
    }

    public void SetDifficulty(float value)
    {
        difficulty = Mathf.Clamp01(value);
    }
}
