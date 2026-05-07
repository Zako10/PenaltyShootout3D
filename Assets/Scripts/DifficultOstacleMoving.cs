using UnityEngine;

public sealed class MovingObstacleGroup : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float range = 1.2f;
    [SerializeField] private float baseSpeed = 1.3f;
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

        if (difficulty < 1f)
        {
            float newDifficulty = difficulty + (Time.deltaTime * difficultyIncreaseRate);
            SetDifficulty(newDifficulty);
        }

        float speed = baseSpeed + (difficulty * maxAdditionalSpeed);

        
        elapsedTime += Time.deltaTime * speed;

  
        transform.position = startPosition
            + Vector3.right
            * Mathf.Sin(elapsedTime)
            * range;
    }

    public void SetDifficulty(float value)
    {
        difficulty = Mathf.Clamp01(value);

        range = 1.2f + (difficulty * 0.8f);
    }
}