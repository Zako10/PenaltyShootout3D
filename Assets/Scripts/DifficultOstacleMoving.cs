using UnityEngine;

public sealed class MovingObstacleGroup : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float range = 1.2f;
    [SerializeField] private float baseSpeed = 0.75f;
    [SerializeField] private float maxAdditionalSpeed = 1.5f;

    [Header("Difficulty Scaling")]
    [Tooltip("How much difficulty increases per second. 0.01 means 100 seconds to max difficulty.")]
    [SerializeField] private float difficultyIncreaseRate = 0.01f;

    private Vector3 startPosition;
    private float difficulty = 0f;
    private float elapsedTime = 0f;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        // 1. Progressively increase difficulty based on real time
        // We clamp it at 1.0 so it doesn't become infinitely fast
        if (difficulty < 1f)
        {
            float newDifficulty = difficulty + (Time.deltaTime * difficultyIncreaseRate);
            SetDifficulty(newDifficulty);
        }

        // 2. Calculate speed based on current difficulty
        float speed = baseSpeed + (difficulty * maxAdditionalSpeed);

        // 3. Accumulate internal time for smooth sine movement
        elapsedTime += Time.deltaTime * speed;

        // 4. Apply the side-to-side (Ping-Pong) movement
        transform.position = startPosition
            + Vector3.right
            * Mathf.Sin(elapsedTime)
            * range;
    }

    public void SetDifficulty(float value)
    {
        difficulty = Mathf.Clamp01(value);

        // Dynamic Range: The obstacles swing wider as it gets harder
        range = 1.2f + (difficulty * 0.8f);
    }
}