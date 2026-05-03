using System.Collections;
using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Start,
        Playing,
        Shooting,
        End
    }

    [Header("Game Systems")]
    [SerializeField] private PlayerKicker player;
    [SerializeField] private BallController ball;
    [SerializeField] private GoalkeeperController goalkeeper;
    [SerializeField] private UIManager uiManager;

    [Header("Rules")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private float roundResetDelay = 2f;
    [SerializeField] private float shotResolveDelay = 3f;

    private GameState currentState = GameState.Start;
    private Coroutine roundRoutine;
    private float timer;
    private int score;
    private bool roundResolved;

    public GameState CurrentState => currentState;

    private void Awake()
    {
        timer = gameDuration;
    }

    private void OnEnable()
    {
        if (ball != null)
        {
            ball.GoalScored += HandleGoalScored;
            ball.Missed += HandleMissed;
            ball.Saved += HandleSaved;
        }
    }

    private void OnDisable()
    {
        if (ball != null)
        {
            ball.GoalScored -= HandleGoalScored;
            ball.Missed -= HandleMissed;
            ball.Saved -= HandleSaved;
        }
    }

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        if (currentState == GameState.Playing)
        {
            TickTimer();
            ReadShootInput();
        }
    }

    public void StartGame()
    {
        score = 0;
        timer = gameDuration;
        currentState = GameState.Playing;
        roundResolved = false;

        ResetRound();
        RefreshUI(string.Empty);
    }

    private void TickTimer()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            timer = 0f;
            EndGame();
        }

        uiManager?.UpdateTimer(timer);
    }

    private void ReadShootInput()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            Shoot();
        }
    }

    public void Shoot()
    {
        if (currentState != GameState.Playing || player == null || ball == null)
        {
            return;
        }

        currentState = GameState.Shooting;
        roundResolved = false;
        uiManager?.UpdateResult(string.Empty);

        goalkeeper?.DiveRandom();
        player.StartKick(ball, StartShotResolveTimer);
    }

    private void StartShotResolveTimer()
    {
        if (roundRoutine != null)
        {
            StopCoroutine(roundRoutine);
        }

        roundRoutine = StartCoroutine(ResolveShotAfterDelay());
    }

    private IEnumerator ResolveShotAfterDelay()
    {
        yield return new WaitForSeconds(shotResolveDelay);

        if (!roundResolved)
        {
            HandleMissed();
        }
    }

    private void HandleGoalScored()
    {
        if (roundResolved || currentState == GameState.End)
        {
            return;
        }

        roundResolved = true;
        score++;
        uiManager?.UpdateScore(score);
        uiManager?.UpdateResult("Goal");
        StartNextRound();
    }

    private void HandleMissed()
    {
        if (roundResolved || currentState == GameState.End)
        {
            return;
        }

        roundResolved = true;
        uiManager?.UpdateResult("Miss");
        StartNextRound();
    }

    private void HandleSaved()
    {
        if (roundResolved || currentState == GameState.End)
        {
            return;
        }

        roundResolved = true;
        uiManager?.UpdateResult("Saved");
        StartNextRound();
    }

    private void StartNextRound()
    {
        if (roundRoutine != null)
        {
            StopCoroutine(roundRoutine);
        }

        roundRoutine = StartCoroutine(ResetRoundAfterDelay());
    }

    private IEnumerator ResetRoundAfterDelay()
    {
        yield return new WaitForSeconds(roundResetDelay);

        if (currentState != GameState.End)
        {
            ResetRound();
            currentState = GameState.Playing;
        }
    }

    private void ResetRound()
    {
        player?.ResetPlayer();
        ball?.ResetBall();
        goalkeeper?.ResetGoalkeeper();
    }

    private void EndGame()
    {
        currentState = GameState.End;

        if (roundRoutine != null)
        {
            StopCoroutine(roundRoutine);
            roundRoutine = null;
        }

        uiManager?.UpdateResult("Time Finished");
    }

    private void RefreshUI(string result)
    {
        uiManager?.UpdateScore(score);
        uiManager?.UpdateTimer(timer);
        uiManager?.UpdateResult(result);
    }
}
