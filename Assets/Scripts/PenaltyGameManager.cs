using System.Collections;
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
    [SerializeField] private Transform leftPost;
    [SerializeField] private Transform rightPost;
    [SerializeField] private Transform crossbar;
    [SerializeField] private MovingObstacle[] obstacles;

    [Header("UI")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text timerText;
    [SerializeField] private Text messageText;
    [SerializeField] private Text gameOverText;
    [SerializeField] private Slider powerSlider;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip kickClip;
    [SerializeField] private AudioClip goalClip;
    [SerializeField] private AudioClip saveClip;
    [SerializeField] private AudioClip whistleClip;

    [Header("Easy Level")]
    [SerializeField] private float matchLength = 60f;
    [SerializeField] private float baseShotPower = 18f;
    [SerializeField] private float keeperBaseSpeed = 2.2f;
    [SerializeField] private float difficultyRampPerSecond = 0.025f;

    private MatchState state = MatchState.Menu;
    private Vector2 aim = new Vector2(0f, 1.8f);
    private float timer;
    private float shotPower01 = 0.65f;
    private float currentDifficulty;
    private int score;
    private int shots;
    private bool shotResolved;
    private Vector3 keeperStartScale;
    private Vector3 playerStartScale;
    private Vector3 playerHomePosition;
    private Vector3 ballHomePosition;

    private const float GoalLineZ = 10.4f;
    private const float GoalHalfWidth = 2.25f;
    private const float GoalHeight = 2.35f;
    private const float PlayerHalfRange = 2.05f;

    private void Awake()
    {
        kickClip = kickClip != null ? kickClip : CreateTone("Kick", 120f, 0.09f, 0.7f);
        goalClip = goalClip != null ? goalClip : CreateTone("Goal", 660f, 0.22f, 0.45f);
        saveClip = saveClip != null ? saveClip : CreateTone("Save", 220f, 0.16f, 0.45f);
        whistleClip = whistleClip != null ? whistleClip : CreateTone("Whistle", 920f, 0.18f, 0.35f);
        keeperStartScale = goalkeeper != null ? goalkeeper.localScale : Vector3.one;
        playerStartScale = player != null ? player.localScale : Vector3.one;
        playerHomePosition = player != null ? player.position : Vector3.zero;
        ballHomePosition = ballStart != null ? ballStart.position : Vector3.zero;
        if (player != null && ballStart != null)
        {
            ballHomePosition = new Vector3(playerHomePosition.x, 0.35f, playerHomePosition.z + 2.75f);
            ballStart.position = ballHomePosition;
        }
        ShowMenu();
    }

    private void Update()
    {
        if (state == MatchState.Menu)
        {
            if (WasStartPressed())
            {
                StartEasyMatch();
            }

            return;
        }

        if (state != MatchState.Playing && state != MatchState.ResolvingShot)
        {
            if (state == MatchState.GameOver && WasRestartPressed())
            {
                StartEasyMatch();
            }

            return;
        }

        timer -= Time.deltaTime;
        currentDifficulty += Time.deltaTime * difficultyRampPerSecond;
        UpdateHud();

        if (timer <= 0f)
        {
            EndMatch();
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

    public void StartEasyMatch()
    {
        state = MatchState.Playing;
        timer = matchLength;
        score = 0;
        shots = 0;
        currentDifficulty = 0f;
        shotPower01 = 0.65f;
        aim = new Vector2(0f, 1.8f);
        mainMenuPanel.SetActive(false);
        hudPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        messageText.text = "A/D move player. W/S aim height. Mouse aims. Click or Space shoots.";
        PlayClip(whistleClip);
        ResetShot();
        UpdateHud();
    }

    public void ShowMenu()
    {
        state = MatchState.Menu;
        mainMenuPanel.SetActive(true);
        hudPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        messageText.text = string.Empty;
        ResetShot();
    }

    public void RestartFromGameOver()
    {
        StartEasyMatch();
    }

    private void UpdateAim()
    {
        Vector2 move = ReadMoveInput();
        float horizontal = move.x;
        float vertical = move.y;

        if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
        {
            MovePlayer(horizontal);
            aim.x = player != null ? player.position.x : aim.x;
            aim.y += vertical * Time.deltaTime * 1.4f;
        }
        else
        {
            Vector3 mouse = ReadMousePosition();
            aim.x = Mathf.Lerp(-GoalHalfWidth, GoalHalfWidth, Mathf.Clamp01(mouse.x / Screen.width));
            aim.y = Mathf.Lerp(0.55f, GoalHeight, Mathf.Clamp01(mouse.y / Screen.height));
        }

        if (IsPowerUpHeld())
        {
            shotPower01 += Time.deltaTime * 0.55f;
        }
        else if (IsPowerDownHeld())
        {
            shotPower01 -= Time.deltaTime * 0.55f;
        }

        aim.x = Mathf.Clamp(aim.x, -GoalHalfWidth, GoalHalfWidth);
        aim.y = Mathf.Clamp(aim.y, 0.45f, GoalHeight);
        shotPower01 = Mathf.Clamp01(shotPower01);

        if (aimMarker != null)
        {
            aimMarker.position = new Vector3(aim.x, aim.y, GoalLineZ - 0.18f);
        }

        if (powerSlider != null)
        {
            powerSlider.value = shotPower01;
        }
    }

    private void MovePlayer(float horizontal)
    {
        if (player == null)
        {
            return;
        }

        Vector3 position = player.position;
        position.x = Mathf.Clamp(position.x + horizontal * Time.deltaTime * 3.4f, -PlayerHalfRange, PlayerHalfRange);
        player.position = position;

        if (ball != null && ball.isKinematic)
        {
            ball.transform.position = new Vector3(position.x, ballHomePosition.y, ballHomePosition.z);
        }
    }

    private void Shoot()
    {
        state = MatchState.ResolvingShot;
        shotResolved = false;
        shots++;
        messageText.text = string.Empty;

        Vector3 target = new Vector3(aim.x, aim.y, GoalLineZ);
        Vector3 direction = (target - ball.position).normalized;
        ball.isKinematic = false;
        ball.velocity = Vector3.zero;
        ball.angularVelocity = Vector3.zero;
        ball.AddForce(direction * (baseShotPower + shotPower01 * 8f), ForceMode.Impulse);
        ball.AddTorque(new Vector3(Random.Range(2f, 5f), Random.Range(-3f, 3f), -6f), ForceMode.Impulse);

        StartCoroutine(KickAnimation());
        PlayClip(kickClip);
    }

    private void UpdateKeeperPatrol()
    {
        if (goalkeeper == null || keeperHome == null)
        {
            return;
        }

        float patrolRange = Mathf.Lerp(0.55f, 1.4f, Mathf.Clamp01(currentDifficulty));
        float patrolSpeed = 1.1f + currentDifficulty * 0.9f;
        Vector3 target = keeperHome.position + Vector3.right * Mathf.Sin(Time.time * patrolSpeed) * patrolRange;
        goalkeeper.position = Vector3.MoveTowards(goalkeeper.position, target, (keeperBaseSpeed * 0.65f) * Time.deltaTime);
        goalkeeper.rotation = Quaternion.Lerp(goalkeeper.rotation, Quaternion.Euler(0f, 180f, 0f), Time.deltaTime * 8f);
    }

    private void UpdateKeeperDive()
    {
        if (goalkeeper == null)
        {
            return;
        }

        float reactionError = Mathf.Lerp(1.15f, 0.35f, Mathf.Clamp01(currentDifficulty));
        float targetX = aim.x + Random.Range(-reactionError, reactionError);
        Vector3 target = new Vector3(
            Mathf.Clamp(targetX, -GoalHalfWidth + 0.25f, GoalHalfWidth - 0.25f),
            keeperHome.position.y,
            keeperHome.position.z);

        float speed = keeperBaseSpeed + currentDifficulty * 1.8f;
        goalkeeper.position = Vector3.MoveTowards(goalkeeper.position, target, speed * Time.deltaTime);
        float lean = Mathf.Clamp((target.x - goalkeeper.position.x) * -20f, -55f, 55f);
        goalkeeper.rotation = Quaternion.Lerp(goalkeeper.rotation, Quaternion.Euler(0f, 180f, lean), Time.deltaTime * 8f);
    }

    private void CheckShotResult()
    {
        if (shotResolved || ball == null)
        {
            return;
        }

        bool crossedGoalLine = ball.position.z >= GoalLineZ;
        bool insideGoal = Mathf.Abs(ball.position.x) <= GoalHalfWidth && ball.position.y >= 0.35f && ball.position.y <= GoalHeight;
        bool keeperReachedBall = Vector3.Distance(ball.position, goalkeeper.position + Vector3.up * 1.1f) < 0.85f && ball.position.z > 7f;
        bool expired = ball.position.z > GoalLineZ + 2.5f || ball.position.y < -0.5f || ball.velocity.magnitude < 0.4f;

        if (keeperReachedBall)
        {
            ResolveShot(false, "Saved!");
            return;
        }

        if (crossedGoalLine)
        {
            ResolveShot(insideGoal, insideGoal ? "Goal!" : "Missed!");
            return;
        }

        if (expired)
        {
            ResolveShot(false, "No goal.");
        }
    }

    private void ResolveShot(bool goal, string message)
    {
        shotResolved = true;
        if (goal)
        {
            score++;
            PlayClip(goalClip);
            StartCoroutine(CelebrationAnimation());
        }
        else
        {
            PlayClip(saveClip);
            StartCoroutine(LoseAnimation());
        }

        messageText.text = message;
        UpdateHud();
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
        if (ball == null || ballStart == null)
        {
            return;
        }

        ball.isKinematic = true;
        ball.velocity = Vector3.zero;
        ball.angularVelocity = Vector3.zero;
        ball.transform.SetPositionAndRotation(ballStart.position, ballStart.rotation);

        if (goalkeeper != null && keeperHome != null)
        {
            goalkeeper.SetPositionAndRotation(keeperHome.position, Quaternion.Euler(0f, 180f, 0f));
            goalkeeper.localScale = keeperStartScale;
        }

        if (player != null)
        {
            player.position = playerHomePosition;
            player.localScale = playerStartScale;
            player.rotation = Quaternion.identity;
        }
    }

    private void EndMatch()
    {
        state = MatchState.GameOver;
        hudPanel.SetActive(false);
        gameOverPanel.SetActive(true);
        gameOverText.text = "Game Over\nScore: " + score + "\nShots: " + shots;
        PlayClip(whistleClip);
    }

    private void UpdateHud()
    {
        scoreText.text = "Score: " + score;
        timerText.text = "Time: " + Mathf.CeilToInt(Mathf.Max(0f, timer));

        foreach (MovingObstacle obstacle in obstacles)
        {
            if (obstacle != null)
            {
                obstacle.SetDifficulty(currentDifficulty);
            }
        }
    }

    private void AnimatePlayerReady()
    {
        if (player == null)
        {
            return;
        }

        float bob = Mathf.Sin(Time.time * 4f) * 0.025f;
        player.localScale = playerStartScale + new Vector3(0f, bob, 0f);
    }

    private static Vector2 ReadMoveInput()
    {
        float x = 0f;
        float y = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            x -= 1f;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            x += 1f;
        }
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            y += 1f;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            y -= 1f;
        }

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                x -= 1f;
            }
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                x += 1f;
            }
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                y += 1f;
            }
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                y -= 1f;
            }
        }
#endif

        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }

    private static bool WasStartPressed()
    {
        bool pressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        pressed |= keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
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

    private static bool IsPowerUpHeld()
    {
        bool held = Input.GetKey(KeyCode.LeftShift);
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        held |= keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
#endif
        return held;
    }

    private static bool IsPowerDownHeld()
    {
        bool held = Input.GetKey(KeyCode.LeftControl);
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        held |= keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed);
#endif
        return held;
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
        if (player == null)
        {
            yield break;
        }

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
        if (player == null)
        {
            yield break;
        }

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
        if (player == null)
        {
            yield break;
        }

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
}
