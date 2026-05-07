using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class PenaltyGameManager : MonoBehaviour
{
    private enum MatchState
    {
        Menu,
        Playing,
        ResolvingShot,
        GameOver
    }

    [Header("Scene")]
    [SerializeField] private Rigidbody ball;
    [SerializeField] private Transform ballStart;
    [SerializeField] private Transform player;
    [SerializeField] private Transform goalkeeper;
    [SerializeField] private Transform keeperHome;
    [SerializeField] private Transform aimMarker;
    [SerializeField] private MovingObstacle[] obstacles;

    [Header("UI")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text timerText;
    [SerializeField] private Text difficultyText;
    [SerializeField] private Text messageText;
    [SerializeField] private Text gameOverText;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip kickClip;
    [SerializeField] private AudioClip goalClip;
    [SerializeField] private AudioClip saveClip;
    [SerializeField] private AudioClip whistleClip;
    [SerializeField] private AudioClip musicClip;

    [Header("Easy Level")]
    [SerializeField] private float matchLength = 60f;
    [SerializeField] private float baseShotPower = 18f;
    [SerializeField] private float playerRunSpeed = 5.2f;
    [SerializeField] private float playerKickDistance = 0.75f;
    [SerializeField] private float keeperBaseSpeed = 2.2f;
    [SerializeField] private float keeperEasyReactionDelay = 0.28f;
    [SerializeField] private float difficultyRampPerSecond = 0.025f;
    [SerializeField] private bool startImmediately;

    [Header("Hard Level Overrides")]
    [SerializeField] private float hardShotPower = 24f;
    [SerializeField] private float hardKeeperBaseSpeed = 7.4f;
    [SerializeField] private float hardKeeperReactionDelay = 0.06f;
    [SerializeField] private float hardDifficultyRampPerSecond = 0.07f;

    private bool isHardMode = false;
    [SerializeField] private string startMenu = "StartMenu";
    private MatchState state = MatchState.Menu;
    private Vector2 aim = new Vector2(0f, 1.8f);
    private float timer;
    private float currentDifficulty;
    private int score;
    private int shots;
    private bool shotResolved;
    private Vector3 keeperStartScale;
    private Vector3 playerStartScale;
    private Vector3 playerHomePosition;
    private Vector3 keeperDiveTarget;
    private Vector3 lastBallPosition;
    private Vector3 resolvedGoalPoint;
    private Coroutine playerShotRoutine;
    private float kickMoment;
    private bool shotHasBeenKicked;
    private bool obstacleTouchedShot;

    private const float GoalLineZ = 10.15f;
    private const float GoalHalfWidth = 2.25f;
    private const float GoalHeight = 2.35f;
    private const float PlayerHalfRange = 2.05f;
    private const int WinningScore = 10;
    private const float BonusTargetX = 1.45f;
    private const float BonusTargetY = 1.7f;
    private const float BonusTargetHalfSize = 0.325f;

    private void Awake()
    {
        ApplyDifficultySettings();

        kickClip = kickClip != null ? kickClip : CreateTone("Kick", 120f, 0.09f, 0.7f);
        goalClip = goalClip != null ? goalClip : CreateTone("Goal", 660f, 0.22f, 0.45f);
        saveClip = saveClip != null ? saveClip : CreateTone("Save", 220f, 0.16f, 0.45f);
        whistleClip = whistleClip != null ? whistleClip : CreateTone("Whistle", 920f, 0.18f, 0.35f);
        musicClip = musicClip != null ? musicClip : CreateMusicLoop();
        keeperStartScale = goalkeeper != null ? goalkeeper.localScale : Vector3.one;
        playerStartScale = player != null ? player.localScale : Vector3.one;
        playerHomePosition = player != null ? player.position : Vector3.zero;
        EnsureBallCollisionReporter();

        if (startImmediately)
        {
            StartMatch();
        }
        else
        {
            ShowMenu();
        }
    }

    private void ApplyDifficultySettings()
    {
        int level = PlayerPrefs.GetInt("DifficultyLevel", 1);
        isHardMode = (level == 3);

        if (isHardMode)
        {
            baseShotPower = hardShotPower;
            keeperBaseSpeed = hardKeeperBaseSpeed;
            keeperEasyReactionDelay = hardKeeperReactionDelay;
            difficultyRampPerSecond = hardDifficultyRampPerSecond;
        }
    }

    private void Update()
    {
        if (state == MatchState.Menu)
        {
            if (WasStartPressed())
            {
                StartMatch();
            }

            return;
        }

        if (state != MatchState.Playing && state != MatchState.ResolvingShot)
        {
            if (state == MatchState.GameOver && WasRestartPressed())
            {
                StartMatch();
            }

            return;
        }

        timer -= Time.deltaTime;
        currentDifficulty += Time.deltaTime * difficultyRampPerSecond;
        UpdateHud();

        if (timer <= 0f)
        {
            EndMatch(score >= WinningScore);
            return;
        }

        if (state == MatchState.Playing)
        {
            UpdateAim();
            UpdateKeeperPatrol();
            AnimatePlayerReady(); 

            if (WasShootPressed())
            {
                Shoot();
            }
        }
        else 
        {
            UpdateKeeperDive();
            CheckShotResult();
        }
    }

    public void StartMatch()
    {
        state = MatchState.Playing;
        timer = matchLength;
        score = 0;
        shots = 0;
        currentDifficulty = 0f;
        aim = new Vector2(0f, 1.8f);
        mainMenuPanel.SetActive(false);
        hudPanel.SetActive(true);
        gameOverPanel.SetActive(false);

        messageText.text = "Reach 10 points. Normal goal +1, highlighted target +2, miss -1.";

        PlayMusic();
        PlayClip(whistleClip);
        ResetShot();
        UpdateHud();
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(startMenu);
    }

    public void ShowMenu()
    {
        state = MatchState.Menu;
        mainMenuPanel.SetActive(true);
        hudPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        messageText.text = string.Empty;
        StopMusic();
        ResetShot();
    }

    public void RestartFromGameOver()
    {
        StartMatch();
    }

    private void UpdateAim()
    {
        Vector2 move = ReadMoveInput();
        float horizontal = move.x;
        float vertical = move.y;

        if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
        {
            MovePlayer(horizontal);
            aim.y += vertical * Time.deltaTime * 1.4f;
        }
        else
        {
            Vector3 mouse = ReadMousePosition();
            aim.y = Mathf.Lerp(0.55f, GoalHeight, Mathf.Clamp01(mouse.y / Screen.height));
        }

        aim.x = player != null ? player.position.x : aim.x;
        aim.x = Mathf.Clamp(aim.x, -GoalHalfWidth, GoalHalfWidth);
        aim.y = Mathf.Clamp(aim.y, 0.45f, GoalHeight);

        if (aimMarker != null)
        {
            aimMarker.position = new Vector3(aim.x, aim.y, GoalLineZ - 0.18f);
        }
    }

    private void MovePlayer(float horizontal)
    {
        if (player == null)
        {
            return;
        }

        Vector3 position = player.position;
        position.x = Mathf.Clamp(
            position.x + horizontal * Time.deltaTime * 3.4f,
            -PlayerHalfRange,
            PlayerHalfRange);
        player.position = position;
    }

    private void Shoot()
    {
        state = MatchState.ResolvingShot;
        shotResolved = false;
        shotHasBeenKicked = false;
        obstacleTouchedShot = false;
        shots++;
        messageText.text = string.Empty;

        if (playerShotRoutine != null)
        {
            StopCoroutine(playerShotRoutine);
        }

        playerShotRoutine = StartCoroutine(RunPlayerToBallAndKick());
    }

    private IEnumerator RunPlayerToBallAndKick()
    {
        if (player == null || ball == null)
        {
            KickBallNow();
            yield break;
        }

        Vector3 kickSpot = ball.position + Vector3.back * playerKickDistance;
        kickSpot.y = playerHomePosition.y;

        while (Vector3.Distance(Flatten(player.position), Flatten(kickSpot)) > 0.06f)
        {
            Vector3 direction = kickSpot - player.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                player.rotation = Quaternion.Slerp(
                    player.rotation,
                    Quaternion.LookRotation(direction.normalized),
                    Time.deltaTime * 14f);
                player.position = Vector3.MoveTowards(
                    player.position,
                    kickSpot,
                    playerRunSpeed * Time.deltaTime);
            }

            AnimatePlayerKicking(); 

            yield return null;
        }

        KickBallNow();
        playerShotRoutine = null;
    }






    private void KickBallNow()
    {
        if (ball == null)
        {
            return;
        }

        Vector3 target = new Vector3(aim.x, aim.y, GoalLineZ);
        Vector3 direction = (target - ball.position).normalized;
        ball.isKinematic = false;
        ball.linearVelocity = Vector3.zero;
        ball.angularVelocity = Vector3.zero;
        ball.AddForce(direction * baseShotPower, ForceMode.Impulse);
        ball.AddTorque(
            new Vector3(Random.Range(2f, 5f), Random.Range(-3f, 3f), -6f),
            ForceMode.Impulse);

        shotHasBeenKicked = true;
        kickMoment = Time.time;
        lastBallPosition = ball.position;
        PrepareKeeperDiveTarget();
        StartCoroutine(KickAnimation());
        PlayClip(kickClip);
    }

    private void UpdateKeeperPatrol()
    {
        if (goalkeeper == null || keeperHome == null)
        {
            return;
        }

        float patrolRange = isHardMode
            ? Mathf.Lerp(0.9f, 1.85f, Mathf.Clamp01(currentDifficulty))
            : Mathf.Lerp(0.55f, 1.4f, Mathf.Clamp01(currentDifficulty));
        float patrolSpeed = isHardMode
            ? 1.45f + currentDifficulty * 1.35f
            : 1.1f + currentDifficulty * 0.9f;
        Vector3 target = keeperHome.position
            + Vector3.right * Mathf.Sin(Time.time * patrolSpeed) * patrolRange;
        goalkeeper.position = Vector3.MoveTowards(
            goalkeeper.position,
            target,
            (keeperBaseSpeed * 0.65f) * Time.deltaTime);
        goalkeeper.rotation = Quaternion.Lerp(
            goalkeeper.rotation,
            Quaternion.Euler(0f, 180f, 0f),
            Time.deltaTime * 8f);
    }

    private void UpdateKeeperDive()
    {
        if (goalkeeper == null || keeperHome == null || !shotHasBeenKicked)
        {
            return;
        }

        float reactionDelay = isHardMode
            ? Mathf.Lerp(keeperEasyReactionDelay, 0.005f, Mathf.Clamp01(currentDifficulty))
            : keeperEasyReactionDelay;

        if (Time.time < kickMoment + reactionDelay)
        {
            return;
        }

        float distanceToGoal = Mathf.Max(0.1f, GoalLineZ - (ball != null ? ball.position.z : 0f));
        float ballSpeed = ball != null ? ball.linearVelocity.magnitude : 1f;
        float timeToGoal = ballSpeed > 0.1f ? distanceToGoal / ballSpeed : 0.5f;
        float distanceToTarget = Vector3.Distance(goalkeeper.position, keeperDiveTarget);
        float requiredSpeed = timeToGoal > 0.05f ? distanceToTarget / timeToGoal : 999f;

        float baseSpeed = isHardMode
            ? keeperBaseSpeed + currentDifficulty * 2.4f
            : keeperBaseSpeed + currentDifficulty * 0.8f;

        float speed = isHardMode
            ? Mathf.Max(baseSpeed, requiredSpeed * 0.95f)
            : Mathf.Min(baseSpeed, requiredSpeed * 0.55f);

        goalkeeper.position = Vector3.MoveTowards(
            goalkeeper.position,
            keeperDiveTarget,
            speed * Time.deltaTime);

        float diveDirection = keeperDiveTarget.x - keeperHome.position.x;

        float zRotation = diveDirection > 0 ? -75f : 75f;

        float xRotation = 25f;

        Quaternion diveRotation = Quaternion.Euler(xRotation, 180f, zRotation);

        goalkeeper.rotation = Quaternion.Lerp(
            goalkeeper.rotation,
            diveRotation,
            Time.deltaTime * 10f); 
    }

    private void CheckShotResult()
    {
        if (shotResolved || ball == null || !shotHasBeenKicked)
        {
            return;
        }

        Vector3 currentBallPosition = ball.position;
        if (TryGetGoalLineCrossing(lastBallPosition, currentBallPosition, out Vector3 goalPoint))
        {
            resolvedGoalPoint = goalPoint;
            bool insideGoal = IsInsideGoal(goalPoint);
            ResolveShot(
                insideGoal,
                insideGoal && IsBonusTargetHit(goalPoint) ? "Highlighted target! +2" :
                insideGoal ? "Goal! +1" : "Missed! -1");
            return;
        }

        lastBallPosition = currentBallPosition;

        bool expired = currentBallPosition.z > GoalLineZ + 2.5f
            || currentBallPosition.y < -0.5f
            || (Time.time >= kickMoment + 0.5f && ball.linearVelocity.magnitude < 0.35f);

        if (expired)
        {
            ResolveShot(false, obstacleTouchedShot ? "Blocked! -1" : "No goal. -1");
        }
    }

    private void ResolveShot(bool goal, string message)
    {
        shotResolved = true;
        if (goal)
        {
            score += IsBonusTargetHit(resolvedGoalPoint) ? 2 : 1;
            PlayClip(goalClip);
            StartCoroutine(CelebrationAnimation());
        }
        else
        {

            score = Mathf.Max(0, score - 1);
            PlayClip(saveClip);
            StartCoroutine(LoseAnimation());
        }

        messageText.text = message;
        UpdateHud();

        if (score >= WinningScore)
        {
            EndMatch(true);
            return;
        }

        StartCoroutine(ResetAfterDelay(1.15f));
    }

    private IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (state == MatchState.ResolvingShot)
        {
            ResetShot();
            state = MatchState.Playing;
        }
    }

    private void ResetShot()
    {
        if (playerShotRoutine != null)
        {
            StopCoroutine(playerShotRoutine);
            playerShotRoutine = null;
        }

        shotHasBeenKicked = false;

        if (ball == null || ballStart == null)
        {
            return;
        }

        ball.isKinematic = false;
        ball.linearVelocity = Vector3.zero;
        ball.angularVelocity = Vector3.zero;
        ball.isKinematic = true;
        ball.transform.SetPositionAndRotation(ballStart.position, ballStart.rotation);

        if (goalkeeper != null && keeperHome != null)
        {
            goalkeeper.SetPositionAndRotation(
                keeperHome.position,
                Quaternion.Euler(0f, 180f, 0f)); 
            goalkeeper.localScale = keeperStartScale;
        }

        if (player != null)
        {
            player.position = playerHomePosition;
            player.localScale = playerStartScale;
            player.rotation = Quaternion.identity;
        }
    }
    private void PrepareKeeperDiveTarget()
    {
        if (keeperHome == null)
        {
            keeperDiveTarget = goalkeeper != null ? goalkeeper.position : Vector3.zero;
            return;
        }

        float targetX;

        if (isHardMode)
        {
            if (ball != null)
            {
                Vector3 ballPos = ball.position;
                Vector3 ballVel = ball.linearVelocity;

                float distanceZ = GoalLineZ - ballPos.z;
                float timeToGoal = ballVel.z > 0.01f
                    ? distanceZ / ballVel.z
                    : 0.3f;


                float dragFactor = Mathf.Exp(-0.1f * timeToGoal); 
                float predictedX = ballPos.x + ballVel.x * timeToGoal * dragFactor;

                float anticipation = Mathf.Lerp(0.35f, 0.75f, Mathf.Clamp01(currentDifficulty));
                predictedX = Mathf.Lerp(predictedX, aim.x, anticipation);

                float reactionError = Mathf.Lerp(0.04f, 0.005f, Mathf.Clamp01(currentDifficulty));
                targetX = predictedX + Random.Range(-reactionError, reactionError);
            }
            else
            {
                targetX = aim.x;
            }
        }
        else
        {
            float reactionError = Mathf.Lerp(1.45f, 0.75f, Mathf.Clamp01(currentDifficulty));
            targetX = aim.x + Random.Range(-reactionError, reactionError);

            if (Random.value < 0.18f)
            {
                targetX *= -0.65f;
            }
        }

        keeperDiveTarget = new Vector3(
            Mathf.Clamp(targetX, -GoalHalfWidth + 0.25f, GoalHalfWidth - 0.25f),
            keeperHome.position.y,
            keeperHome.position.z);
    }

    private static Vector3 Flatten(Vector3 value)
    {
        return new Vector3(value.x, 0f, value.z);
    }

    private void EndMatch(bool won = false)
    {
        state = MatchState.GameOver;
        hudPanel.SetActive(false);
        gameOverPanel.SetActive(true);
        gameOverText.text = won
            ? "You Win!\nPoints: " + score + "\nShots: " + shots
            : "You Lose\nPoints: " + score + "\nShots: " + shots;
        StopMusic();
        PlayClip(whistleClip);
    }

    private void UpdateHud()
    {
        scoreText.text = "Points: " + score + " / " + WinningScore;
        timerText.text = "Time: " + Mathf.CeilToInt(Mathf.Max(0f, timer));
        if (difficultyText != null)
        {
            difficultyText.text = "Difficulty: "
                + Mathf.RoundToInt(Mathf.Clamp01(currentDifficulty) * 100f) + "%";
        }

        foreach (MovingObstacle obstacle in obstacles)
        {
            if (obstacle != null)
            {
                obstacle.SetDifficulty(currentDifficulty);
            }
        }
    }

    private static bool IsBonusTargetHit(Vector3 point)
    {
        return Mathf.Abs(point.x - BonusTargetX) <= BonusTargetHalfSize
            && Mathf.Abs(point.y - BonusTargetY) <= BonusTargetHalfSize;
    }

    private static bool IsInsideGoal(Vector3 point)
    {
        return Mathf.Abs(point.x) <= GoalHalfWidth
            && point.y >= 0.15f
            && point.y <= GoalHeight;
    }

    private static bool TryGetGoalLineCrossing(Vector3 from, Vector3 to, out Vector3 crossing)
    {
        crossing = default;
        if (from.z >= GoalLineZ || to.z < GoalLineZ)
        {
            return false;
        }

        float t = Mathf.InverseLerp(from.z, to.z, GoalLineZ);
        crossing = Vector3.Lerp(from, to, t);
        return true;
    }

    private void EnsureBallCollisionReporter()
    {
        if (ball == null)
        {
            return;
        }

        PenaltyShotCollisionReporter reporter = ball.GetComponent<PenaltyShotCollisionReporter>();
        if (reporter == null)
        {
            reporter = ball.gameObject.AddComponent<PenaltyShotCollisionReporter>();
        }

        reporter.Configure(this);
    }

    internal void NotifyShotCollision(Collider other)
    {
        if (shotResolved || !shotHasBeenKicked || other == null
            || ball == null || ball.position.z >= GoalLineZ)
        {
            return;
        }

        if (IsGoalkeeperCollider(other))
        {
            ResolveShot(false, "Saved! -1");
            return;
        }

        if (other.GetComponentInParent<MovingObstacle>() != null)
        {
            obstacleTouchedShot = true;
        }
    }

    private bool IsGoalkeeperCollider(Collider other)
    {
        return goalkeeper != null
            && (other.transform == goalkeeper
            || other.transform.IsChildOf(goalkeeper));
    }



    private void AnimatePlayerReady()
    {
        if (player == null) return;

        float bob = Mathf.Sin(Time.time * 4f) * 0.025f;
        player.localScale = playerStartScale + new Vector3(0f, bob, 0f);
        player.rotation = Quaternion.identity;
    }

    private void AnimatePlayerRunning()
    {
        if (player == null) return;

        float speed = Time.time * 12f;

        float bounce = Mathf.Abs(Mathf.Sin(speed)) * 0.04f;

        float squashX = 1f + Mathf.Sin(speed * 2f) * 0.03f;
        float squashY = 1f + bounce * 0.8f;
        float squashZ = 1f - Mathf.Sin(speed * 2f) * 0.02f;

        player.localScale = new Vector3(
            playerStartScale.x * squashX,
            playerStartScale.y * squashY,
            playerStartScale.z * squashZ);

        float sway = Mathf.Sin(speed) * 2.5f;

        float forwardLean = 15f;

        player.rotation = Quaternion.Euler(forwardLean, 0f, sway);

        Vector3 pos = player.position;
        pos.y = playerHomePosition.y + bounce;
        player.position = pos;
    }

    private void AnimatePlayerKicking()
    {
        if (player == null) return;

        float speed = Time.time * 16f;
        float bounce = Mathf.Abs(Mathf.Sin(speed)) * 0.06f;

        float squashX = 1f + Mathf.Sin(speed * 2f) * 0.04f;
        float squashY = 1f + bounce * 1.2f;
        float squashZ = 1f - Mathf.Sin(speed * 2f) * 0.03f;

        player.localScale = new Vector3(
            playerStartScale.x * squashX,
            playerStartScale.y * squashY,
            playerStartScale.z * squashZ);
        
        float sway = Mathf.Sin(speed) * 4f;
        float forwardLean = 25f;

        player.rotation = Quaternion.Euler(forwardLean, 0f, sway);

        Vector3 pos = player.position;
        pos.y = playerHomePosition.y + bounce;
        player.position = pos;
    }



    private static Vector2 ReadMoveInput()
    {
        float x = 0f;
        float y = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
        }
#endif
        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }

    private static bool WasStartPressed()
    {
        bool pressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        pressed |= keyboard != null
            && (keyboard.enterKey.wasPressedThisFrame
            || keyboard.spaceKey.wasPressedThisFrame);
#endif
        return pressed;
    }

    private static bool WasRestartPressed()
    {
        bool pressed = Input.GetKeyDown(KeyCode.R);
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        pressed |= keyboard != null && keyboard.rKey.wasPressedThisFrame;
#endif
        return pressed;
    }

    private static bool WasShootPressed()
    {
        bool pressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        pressed |= keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
        pressed |= mouse != null && mouse.leftButton.wasPressedThisFrame;
#endif
        return pressed;
    }

    private static Vector3 ReadMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 position = mouse.position.ReadValue();
            return new Vector3(position.x, position.y, 0f);
        }
#endif
        return Input.mousePosition;
    }

    private IEnumerator KickAnimation()
    {
        if (player == null) yield break;

        float t = 0f;
        Quaternion start = player.rotation;
        Quaternion windup = Quaternion.Euler(-7f, 0f, 0f);
        Quaternion kick = Quaternion.Euler(15f, 0f, 0f);

        while (t < 1f)
        {
            t += Time.deltaTime * 8f;
            player.rotation = Quaternion.Slerp(start, windup, t);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 14f;
            player.rotation = Quaternion.Slerp(windup, kick, t);
            yield return null;
        }

        player.rotation = start;
    }

    private IEnumerator CelebrationAnimation()
    {
        if (player == null) yield break;

        for (int i = 0; i < 3; i++)
        {
            player.localScale = playerStartScale * 1.08f;
            yield return new WaitForSeconds(0.12f);
            player.localScale = playerStartScale;
            yield return new WaitForSeconds(0.12f);
        }
    }

    private IEnumerator LoseAnimation()
    {
        if (player == null) yield break;

        Quaternion start = player.rotation;
        Quaternion down = Quaternion.Euler(0f, 0f, -10f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            player.rotation = Quaternion.Slerp(start, down, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.25f);
        player.rotation = start;
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void PlayMusic()
    {
        if (musicSource == null || musicClip == null) return;

        musicSource.clip = musicClip;
        musicSource.loop = true;
        musicSource.volume = 0.16f;
        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    private void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    private static AudioClip CreateTone(string clipName, float frequency, float length, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * length);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = 1f - (i / (float)sampleCount);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateMusicLoop()
    {
        const int sampleRate = 44100;
        const float length = 1.6f;
        int sampleCount = Mathf.CeilToInt(sampleRate * length);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float beat = Mathf.Sin(2f * Mathf.PI * 2f * t) > 0.15f ? 1f : 0.35f;
            float bass = Mathf.Sin(2f * Mathf.PI * 82f * t) * 0.08f;
            float pulse = Mathf.Sin(2f * Mathf.PI * 164f * t) * 0.035f * beat;
            samples[i] = bass + pulse;
        }

        AudioClip clip = AudioClip.Create("Stadium Pulse", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}

public sealed class PenaltyShotCollisionReporter : MonoBehaviour
{
    private PenaltyGameManager manager;

    public void Configure(PenaltyGameManager owner)
    {
        manager = owner;
    }

    private void OnCollisionEnter(Collision collision)
    {
        manager?.NotifyShotCollision(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        manager?.NotifyShotCollision(other);
    }
}
