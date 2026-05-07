using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class EasyLevelAutoRepair
{
    private const string SessionKey = "PenaltyShootout.AutoRepairedEasyLevel.v3";

    static EasyLevelAutoRepair()
    {
        EditorApplication.delayCall += TryRepairOpenEasyLevel;
    }

    private static void TryRepairOpenEasyLevel()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Levels/EasyPenaltyShootout.unity")
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        EasyLevelSceneBuilder.RebuildDefaultEasyLevelScene();
        Debug.Log("Auto-repaired EasyPenaltyShootout with the updated default builder.");
    }
}

public static class EasyLevelSceneBuilder
{
    private const string StartMenuScenePath = "Assets/Scenes/StartMenu.unity";
    private const string ScenePath = "Assets/Scenes/Levels/EasyPenaltyShootout.unity";
    private const string CapturedScenePath = "Assets/Scenes/Levels/EasyPenaltyShootout_Captured.unity";
    private const string TribunePrefab = "Assets/Lightning Poly/Football Essentials 3D/Prefabs/Tribune.prefab";
    private const string ScoreboardPrefab = "Assets/Lightning Poly/Football Essentials 3D/Prefabs/Scoreboard.prefab";
    private const string StadiumPrefab = "Assets/Hayq Art/GrantStadium/Prefabs/Buildings/SM_Stadium.prefab";
    private const string PlayerPrefabPath = "Assets/Prefabs/PlayerStriker.prefab";
    private const string GoalkeeperPrefabPath = "Assets/Prefabs/Goalkeeper.prefab";
    private const string BallPrefabPath = "Assets/Prefabs/Ball.prefab";
    private const string GoalAreaPrefabPath = "Assets/Prefabs/GoalArea.prefab";
    private const string MovingConePrefabPath = "Assets/Prefabs/MovingCone.prefab";
    private const string PlayerIdleClipPath = "Assets/Animations/PlayerReady.anim";
    private const string KeeperIdleClipPath = "Assets/Animations/KeeperReady.anim";
    private const string PlayerControllerPath = "Assets/Animations/PlayerAnimator.controller";
    private const string KeeperControllerPath = "Assets/Animations/KeeperAnimator.controller";

    [MenuItem("Tools/Penalty Shootout/Build Scene", false, 2001)]
    public static void BuildEasyLevelScene()
    {
        BuildDefaultEasyLevelScene();
        BuildStartMenuScene();
    }

    public static void RebuildDefaultEasyLevelScene()
    {
        BuildDefaultEasyLevelScene();
    }

    private static void EnsureProjectFolders()
    {
        string[] folders =
        {
            "Assets/3rd-Party",
            "Assets/Animations",
            "Assets/Audio & Music",
            "Assets/Audio & Music/SFX",
            "Assets/Materials",
            "Assets/Models",
            "Assets/Plugins",
            "Assets/Prefabs",
            "Assets/Resources",
            "Assets/Sandbox",
            "Assets/Scenes",
            "Assets/Scenes/Levels",
            "Assets/Scenes/Other",
            "Assets/Scripts",
            "Assets/Shaders",
            "Assets/Textures"
        };

        foreach (string folder in folders)
        {
            Directory.CreateDirectory(folder);
        }
    }

    private static void EnsureAnimationAssets()
    {
        AnimationClip playerClip = CreateLoopingClip(PlayerIdleClipPath, 0.035f, 0f);
        AnimationClip keeperClip = CreateLoopingClip(KeeperIdleClipPath, 0.018f, 4f);
        CreateAnimatorController(PlayerControllerPath, playerClip);
        CreateAnimatorController(KeeperControllerPath, keeperClip);
        AssetDatabase.SaveAssets();
    }

    private static AnimationClip CreateLoopingClip(string path, float bobAmount, float tiltAmount)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.frameRate = 30f;
        clip.wrapMode = WrapMode.Loop;
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalPosition.y"),
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.35f, bobAmount),
                new Keyframe(0.7f, 0f)));

        if (Mathf.Abs(tiltAmount) > 0f)
        {
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z"),
                new AnimationCurve(
                    new Keyframe(0f, -tiltAmount),
                    new Keyframe(0.35f, tiltAmount),
                    new Keyframe(0.7f, -tiltAmount)));
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void CreateAnimatorController(string path, AnimationClip clip)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
        {
            return;
        }

        AnimatorController.CreateAnimatorControllerAtPathWithClip(path, clip);
    }

    private static void BuildDefaultEasyLevelScene()
    {
        EnsureProjectFolders();
        EnsureAnimationAssets();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Material grass = CreateMaterial("Assets/Materials/PS_Grass.mat", new Color(0.12f, 0.48f, 0.18f));
        Material white = CreateMaterial("Assets/Materials/PS_White.mat", Color.white);
        Material red = CreateMaterial("Assets/Materials/PS_Red.mat", new Color(0.9f, 0.16f, 0.12f));
        Material blue = CreateMaterial("Assets/Materials/PS_Blue.mat", new Color(0.1f, 0.28f, 0.9f));
        Material yellow = CreateMaterial("Assets/Materials/PS_Yellow.mat", new Color(1f, 0.78f, 0.15f));
        Material orange = CreateMaterial("Assets/Materials/PS_BallOrange.mat", new Color(1f, 0.36f, 0.04f));
        Material black = CreateMaterial("Assets/Materials/PS_Black.mat", new Color(0.05f, 0.05f, 0.06f));
        Material grey = CreateMaterial("Assets/Materials/PS_StadiumGrey.mat", new Color(0.35f, 0.38f, 0.42f));
        Material transparentGoal = CreateTransparentMaterial("Assets/Materials/PS_TargetTransparent.mat", new Color(1f, 0.95f, 0.1f, 0.28f));

        GameObject root = new GameObject("Penalty Shootout Arena - Easy");
        GameObject environment = new GameObject("Environment");
        GameObject gameplay = new GameObject("Gameplay");
        GameObject uiRoot = new GameObject("UI");
        environment.transform.SetParent(root.transform);
        gameplay.transform.SetParent(root.transform);
        uiRoot.transform.SetParent(root.transform);

        CreateCameraAndLights(root.transform);
        CreateEnvironment(environment.transform, grass, white, grey);
        CreateGoalArea(gameplay.transform, white, transparentGoal);
        GameObject player = CreateHumanoid("Player Striker", new Vector3(0f, 0f, -8.35f), Quaternion.identity, gameplay.transform, blue, white);
        GameObject keeper = CreateHumanoid("Goalkeeper", new Vector3(0f, 0f, 8.65f), Quaternion.Euler(0f, 180f, 0f), gameplay.transform, red, white);

        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Ball";
        ball.transform.SetParent(gameplay.transform);
        ball.transform.SetPositionAndRotation(new Vector3(0f, 0.35f, -5.6f), Quaternion.identity);
        ball.transform.localScale = Vector3.one * 0.62f;
        ball.GetComponent<Renderer>().sharedMaterial = orange;
        Rigidbody ballBody = ball.GetComponent<Rigidbody>();
        if (ballBody == null) ballBody = ball.AddComponent<Rigidbody>();
        if (ballBody != null)
        {
            ballBody.mass = 0.45f;
            ballBody.linearDamping = 0.18f;
            ballBody.angularDamping = 0.08f;
            ballBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            ballBody.isKinematic = true;
        }

        if (ball.GetComponent<Collider>() == null)
        {
            ball.AddComponent<SphereCollider>();
        }

        Transform ballStart = CreateMarker("Ball Start", ball.transform.position, gameplay.transform);
        Transform keeperHome = CreateMarker("Keeper Home", keeper.transform.position, gameplay.transform);
        Transform aimMarker = CreateAimMarker(gameplay.transform, yellow);
        MovingObstacle[] obstacles = CreateObstacles(gameplay.transform, yellow, black);
        SavePrefabAndKeepInstance(player, PlayerPrefabPath);
        SavePrefabAndKeepInstance(keeper, GoalkeeperPrefabPath);
        SavePrefabAndKeepInstance(ball, BallPrefabPath);
        SavePrefabAndKeepInstance(GameObject.Find("Goal Area"), GoalAreaPrefabPath);

        GameObject managerObject = new GameObject("Penalty Game Manager");
        managerObject.transform.SetParent(gameplay.transform);
        AudioSource audioSource = managerObject.AddComponent<AudioSource>();
        AudioSource musicSource = managerObject.AddComponent<AudioSource>();
        PenaltyGameManager manager = managerObject.AddComponent<PenaltyGameManager>();

        UiRefs refs = CreateUi(uiRoot.transform, managerObject);
        Assign(manager, "ball", ballBody);
        Assign(manager, "ballStart", ballStart);
        Assign(manager, "player", player.transform);
        Assign(manager, "goalkeeper", keeper.transform);
        Assign(manager, "keeperHome", keeperHome);
        Assign(manager, "aimMarker", aimMarker);
        Assign(manager, "obstacles", obstacles);
        Assign(manager, "mainMenuPanel", refs.MainMenu);
        Assign(manager, "hudPanel", refs.Hud);
        Assign(manager, "gameOverPanel", refs.GameOver);
        Assign(manager, "scoreText", refs.ScoreText);
        Assign(manager, "timerText", refs.TimerText);
        Assign(manager, "difficultyText", refs.DifficultyText);
        Assign(manager, "messageText", refs.MessageText);
        Assign(manager, "gameOverText", refs.GameOverText);
        Assign(manager, "audioSource", audioSource);
        Assign(manager, "musicSource", musicSource);
        Assign(manager, "startImmediately", true);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EnsureSceneInBuildSettings(StartMenuScenePath, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Built default Easy level scene: " + ScenePath);
    }

    private static void BuildStartMenuScene()
    {
        EnsureProjectFolders();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Material grass = CreateMaterial("Assets/Materials/PS_Grass.mat", new Color(0.12f, 0.48f, 0.18f));
        Material white = CreateMaterial("Assets/Materials/PS_White.mat", Color.white);
        Material yellow = CreateMaterial("Assets/Materials/PS_Yellow.mat", new Color(1f, 0.78f, 0.15f));
        Material red = CreateMaterial("Assets/Materials/PS_Red.mat", new Color(0.9f, 0.16f, 0.12f));

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 4.3f, -11.8f), Quaternion.Euler(18f, 0f, 0f));
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 50f;
        camera.clearFlags = CameraClearFlags.Skybox;
        cameraObject.AddComponent<AudioListener>();

        GameObject lightObject = new GameObject("Menu Sun Light");
        lightObject.transform.rotation = Quaternion.Euler(45f, -25f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Menu Pitch";
        ground.transform.localScale = new Vector3(2.6f, 1f, 2.4f);
        ground.GetComponent<Renderer>().sharedMaterial = grass;

        CreatePost("Menu Left Post", new Vector3(-2.4f, 1.15f, 8.9f), new Vector3(0.12f, 2.3f, 0.12f), null, white);
        CreatePost("Menu Right Post", new Vector3(2.4f, 1.15f, 8.9f), new Vector3(0.12f, 2.3f, 0.12f), null, white);
        CreatePost("Menu Crossbar", new Vector3(0f, 2.3f, 8.9f), new Vector3(4.9f, 0.12f, 0.12f), null, white);

        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Menu Ball";
        ball.transform.position = new Vector3(0f, 0.38f, -2.6f);
        ball.transform.localScale = Vector3.one * 0.7f;
        ball.GetComponent<Renderer>().sharedMaterial = yellow;

        GameObject keeper = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        keeper.name = "Menu Goalkeeper";
        keeper.transform.position = new Vector3(0f, 0.9f, 7.8f);
        keeper.GetComponent<Renderer>().sharedMaterial = red;
        Object.DestroyImmediate(keeper.GetComponent<Collider>());

        GameObject controllerObject = new GameObject("Main Menu Controller");
        MainMenuController controller = controllerObject.AddComponent<MainMenuController>();
        Assign(controller, "easyLevelSceneName", "EasyPenaltyShootout");

        CreateStartMenuUi(controller);

        EditorSceneManager.SaveScene(scene, StartMenuScenePath);
        EnsureSceneInBuildSettings(StartMenuScenePath, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Built start menu scene: " + StartMenuScenePath);
    }

    [MenuItem("Tools/Penalty Shootout/Capture", false, 2002)]
    public static void CaptureCurrentScene()
    {
        Directory.CreateDirectory("Assets/Scenes/Levels");
        Scene current = SceneManager.GetActiveScene();
        if (!current.IsValid())
        {
            Debug.LogError("No valid active scene to capture.");
            return;
        }

        EditorSceneManager.SaveScene(current);
        EditorSceneManager.SaveScene(current, CapturedScenePath, true);
        AssetDatabase.ImportAsset(CapturedScenePath);
        Debug.Log("Captured current scene for the Easy level builder: " + CapturedScenePath);
    }

    private static void CreateCameraAndLights(Transform parent)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent);
        cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 4.85f, -13.8f), Quaternion.Euler(19f, 0f, 0f));
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 54f;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 250f;
        cameraObject.AddComponent<AudioListener>();

        GameObject sun = new GameObject("Sun Light");
        sun.transform.SetParent(parent);
        sun.transform.rotation = Quaternion.Euler(48f, -28f, 8f);
        Light sunLight = sun.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.intensity = 1.2f;

        GameObject fill = new GameObject("Goal Fill Light");
        fill.transform.SetParent(parent);
        fill.transform.position = new Vector3(0f, 6f, 2f);
        Light fillLight = fill.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.range = 18f;
        fillLight.intensity = 1.4f;
    }

    private static void CreateEnvironment(Transform parent, Material grass, Material white, Material grey)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Playable Grass";
        ground.transform.SetParent(parent);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(2.4f, 1f, 2.6f);
        ground.GetComponent<Renderer>().sharedMaterial = grass;
        CreateFieldLine("Penalty Line", new Vector3(0f, 0.015f, -5.8f), new Vector3(5.2f, 0.03f, 0.05f), parent, white);
        CreateFieldLine("Goal Box Line", new Vector3(0f, 0.015f, 6.4f), new Vector3(6.2f, 0.03f, 0.05f), parent, white);
        CreateFieldLine("Center Runway", new Vector3(0f, 0.016f, 2.1f), new Vector3(0.05f, 0.03f, 14f), parent, white);

        GameObject stadium = SpawnPrefab(StadiumPrefab, "Grand Stadium", new Vector3(0f, -0.1f, 10f), Quaternion.Euler(0f, 180f, 0f), parent);
        if (stadium != null)
        {
            stadium.transform.localScale = Vector3.one * 0.14f;
            OverrideRenderers(stadium, grey);
        }

        for (int i = -1; i <= 1; i += 2)
        {
            GameObject tribune = SpawnPrefab(TribunePrefab, "Side Tribune " + i, new Vector3(i * 6.8f, 0f, 4f), Quaternion.Euler(0f, i > 0 ? -90f : 90f, 0f), parent);
            if (tribune != null)
            {
                tribune.transform.localScale = Vector3.one * 0.9f;
                OverrideRenderers(tribune, grey);
            }
        }

        GameObject scoreboard = SpawnPrefab(ScoreboardPrefab, "Scoreboard", new Vector3(0f, 2.5f, 13.8f), Quaternion.Euler(0f, 180f, 0f), parent);
        if (scoreboard != null)
        {
            OverrideRenderers(scoreboard, grey);
        }
    }

    private static Transform CreateGoalArea(Transform parent, Material white, Material targetMaterial)
    {
        GameObject goalRoot = new GameObject("Goal Area");
        goalRoot.transform.SetParent(parent);

        CreatePost("Left Post", new Vector3(-2.45f, 1.18f, 10.15f), new Vector3(0.12f, 2.35f, 0.12f), goalRoot.transform, white);
        CreatePost("Right Post", new Vector3(2.45f, 1.18f, 10.15f), new Vector3(0.12f, 2.35f, 0.12f), goalRoot.transform, white);
        CreatePost("Crossbar", new Vector3(0f, 2.35f, 10.15f), new Vector3(5f, 0.12f, 0.12f), goalRoot.transform, white);
        CreatePost("Net Back", new Vector3(0f, 1.15f, 10.55f), new Vector3(5.1f, 2.3f, 0.035f), goalRoot.transform, targetMaterial);

        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name = "Bonus Target";
        target.transform.SetParent(goalRoot.transform);
        target.transform.position = new Vector3(1.45f, 1.7f, 9.95f);
        target.transform.localScale = new Vector3(0.65f, 0.65f, 0.04f);
        target.GetComponent<Renderer>().sharedMaterial = targetMaterial;
        Object.DestroyImmediate(target.GetComponent<Collider>());

        return goalRoot.transform;
    }

    private static MovingObstacle[] CreateObstacles(Transform parent, Material yellow, Material black)
    {
        List<MovingObstacle> obstacles = new List<MovingObstacle>();
        Vector3[] positions =
        {
            new Vector3(-1.55f, 0.35f, 1.8f),
            new Vector3(1.55f, 0.35f, 3.2f),
            new Vector3(0f, 0.35f, 4.6f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject cone = CreateMovingCone();
            cone.name = "Moving Cone " + (i + 1);
            cone.transform.SetParent(parent);
            cone.transform.position = positions[i];
            cone.GetComponent<Renderer>().sharedMaterial = i % 2 == 0 ? yellow : black;
            MovingObstacle obstacle = cone.GetComponent<MovingObstacle>();
            obstacles.Add(obstacle);

            if (i == 0)
            {
                SavePrefabAndKeepInstance(cone, MovingConePrefabPath);
            }
        }

        return obstacles.ToArray();
    }

    private static GameObject CreateMovingCone()
    {
        GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cone.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        Rigidbody rb = cone.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        cone.AddComponent<MovingObstacle>();
        return cone;
    }

    private static UiRefs CreateUi(Transform parent, GameObject managerObject)
    {
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.transform.SetParent(parent);
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        GameObject canvasObject = new GameObject("Canvas");
        canvasObject.transform.SetParent(parent);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject mainMenu = CreatePanel("Main Menu", canvasObject.transform, new Color(0.02f, 0.05f, 0.09f, 0.82f));
        CreateText("Title", "Penalty Shootout Arena", mainMenu.transform, 54, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.68f), new Vector2(760f, 90f));
        CreateText("Subtitle", "Reach 5 points. Goal +1, highlighted target +2, miss -1.", mainMenu.transform, 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.58f), new Vector2(900f, 50f));
        Button easyButton = CreateButton("Start Easy Button", "Start Easy", mainMenu.transform, new Vector2(0.5f, 0.45f), new Vector2(260f, 66f));

        GameObject hud = CreatePanel("HUD", canvasObject.transform, new Color(0f, 0f, 0f, 0f));
        Text score = CreateHudText("Score Text", "Points: 0 / 5", hud.transform, TextAnchor.MiddleLeft, new Vector2(0.12f, 0.93f), new Vector2(390f, 62f));
        Text timer = CreateHudText("Timer Text", "Time: 60", hud.transform, TextAnchor.MiddleRight, new Vector2(0.88f, 0.93f), new Vector2(300f, 62f));
        Text difficulty = CreateHudText("Difficulty Text", "Difficulty: 0%", hud.transform, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.93f), new Vector2(310f, 54f));
        Text message = CreateText("Message Text", "", hud.transform, 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.86f), new Vector2(900f, 48f));

        GameObject gameOver = CreatePanel("Game Over", canvasObject.transform, new Color(0.02f, 0.03f, 0.05f, 0.84f));
        Text gameOverText = CreateText("Game Over Text", "Game Over", gameOver.transform, 44, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.6f), new Vector2(620f, 180f));
        Button restartButton = CreateButton("Restart Button", "Restart", gameOver.transform, new Vector2(0.5f, 0.38f), new Vector2(240f, 64f));

        PenaltyGameManager manager = managerObject.GetComponent<PenaltyGameManager>();
        UnityEventTools.AddPersistentListener(easyButton.onClick, manager.StartMatch);
        UnityEventTools.AddPersistentListener(restartButton.onClick, manager.RestartFromGameOver);

        return new UiRefs(mainMenu, hud, gameOver, score, timer, difficulty, message, gameOverText);
    }

    private static void CreateStartMenuUi(MainMenuController controller)
    {
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject overlay = CreatePanel("Menu Overlay", canvasObject.transform, new Color(0.015f, 0.02f, 0.03f, 0.58f));
        CreateText("Game Title", "Penalty Shootout 3D", overlay.transform, 68, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.74f), new Vector2(980f, 110f));
        Text subtitle = CreateText("Menu Subtitle", "Choose your level", overlay.transform, 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.64f), new Vector2(520f, 54f));
        subtitle.color = new Color(1f, 0.86f, 0.24f, 1f);

        Button easyButton = CreateButton("Easy Level Button", "Easy Level", overlay.transform, new Vector2(0.5f, 0.48f), new Vector2(360f, 76f));
        Button quitButton = CreateButton("Quit Button", "Quit", overlay.transform, new Vector2(0.5f, 0.38f), new Vector2(260f, 62f));

        UnityEventTools.AddPersistentListener(easyButton.onClick, controller.LoadEasyLevel);
        UnityEventTools.AddPersistentListener(quitButton.onClick, controller.QuitGame);
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private static Text CreateText(string name, string value, Transform parent, int size, TextAnchor anchor, Vector2 anchorCenter, Vector2 dimensions)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorCenter;
        rect.anchorMax = anchorCenter;
        rect.sizeDelta = dimensions;
        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = GetFont();
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 14;
        text.resizeTextMaxSize = size;
        return text;
    }

    private static Text CreateHudText(string name, string value, Transform parent, TextAnchor anchor, Vector2 anchorCenter, Vector2 dimensions)
    {
        GameObject container = new GameObject(name + " Panel");
        container.transform.SetParent(parent, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = anchorCenter;
        containerRect.anchorMax = anchorCenter;
        containerRect.sizeDelta = dimensions;
        Image background = container.AddComponent<Image>();
        background.color = new Color(1f, 0.82f, 0.12f, 0.94f);

        Text text = CreateText(name, value, container.transform, 34, anchor, new Vector2(0.5f, 0.5f), dimensions - new Vector2(34f, 0f));
        text.color = new Color(0.02f, 0.025f, 0.03f, 1f);
        return text;
    }

    private static Button CreateButton(string name, string label, Transform parent, Vector2 anchorCenter, Vector2 dimensions)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorCenter;
        rect.anchorMax = anchorCenter;
        rect.sizeDelta = dimensions;
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.95f, 0.72f, 0.18f, 0.96f);
        Button button = buttonObject.AddComponent<Button>();
        Text text = CreateText("Label", label, buttonObject.transform, 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), dimensions);
        text.color = new Color(0.05f, 0.05f, 0.06f);
        return button;
    }

    private static Transform CreateAimMarker(Transform parent, Material yellow)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "Aim Marker";
        marker.transform.SetParent(parent);
        marker.transform.position = new Vector3(0f, 1.8f, 10f);
        marker.transform.localScale = new Vector3(0.22f, 0.22f, 0.04f);
        marker.GetComponent<Renderer>().sharedMaterial = yellow;
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        return marker.transform;
    }

    private static Transform CreateMarker(string name, Vector3 position, Transform parent)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent);
        marker.transform.position = position;
        return marker.transform;
    }

    private static void CreatePost(string name, Vector3 position, Vector3 scale, Transform parent, Material material)
    {
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = name;
        if (parent != null)
        {
            post.transform.SetParent(parent);
        }
        post.transform.position = position;
        post.transform.localScale = scale;
        post.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CreateFieldLine(string name, Vector3 position, Vector3 scale, Transform parent, Material material)
    {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = name;
        line.transform.SetParent(parent);
        line.transform.position = position;
        line.transform.localScale = scale;
        line.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(line.GetComponent<Collider>());
    }

    private static GameObject CreateHumanoid(string name, Vector3 position, Quaternion rotation, Transform parent, Material kit, Material skin)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.SetPositionAndRotation(position, rotation);

        GameObject visual = new GameObject("Animated Visual");
        visual.transform.SetParent(root.transform, false);
        Animator animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            name.Contains("Goalkeeper") ? KeeperControllerPath : PlayerControllerPath);

        CreateBodyPart("Body", PrimitiveType.Capsule, new Vector3(0f, 0.95f, 0f), new Vector3(0.52f, 0.82f, 0.52f), visual.transform, kit);
        CreateBodyPart("Head", PrimitiveType.Sphere, new Vector3(0f, 1.88f, 0f), new Vector3(0.34f, 0.34f, 0.34f), visual.transform, skin);
        CreateBodyPart("Left Arm", PrimitiveType.Capsule, new Vector3(-0.42f, 1.08f, 0f), new Vector3(0.16f, 0.5f, 0.16f), visual.transform, kit).transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
        CreateBodyPart("Right Arm", PrimitiveType.Capsule, new Vector3(0.42f, 1.08f, 0f), new Vector3(0.16f, 0.5f, 0.16f), visual.transform, kit).transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
        CreateBodyPart("Left Leg", PrimitiveType.Capsule, new Vector3(-0.18f, 0.3f, 0f), new Vector3(0.18f, 0.45f, 0.18f), visual.transform, kit);
        CreateBodyPart("Right Leg", PrimitiveType.Capsule, new Vector3(0.18f, 0.3f, 0f), new Vector3(0.18f, 0.45f, 0.18f), visual.transform, kit);
        return root;
    }

    private static GameObject CreateBodyPart(string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Transform parent, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part;
    }

    private static GameObject SpawnPrefab(string assetPath, string name, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent);
        instance.transform.SetPositionAndRotation(position, rotation);
        return instance;
    }

    private static void SavePrefabAndKeepInstance(GameObject instance, string path)
    {
        if (instance == null)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        PrefabUtility.SaveAsPrefabAssetAndConnect(instance, path, InteractionMode.AutomatedAction);
    }

    private static Material CreateMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void OverrideRenderers(GameObject root, Material material)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            renderer.sharedMaterial = material;
        }
    }

    private static Material CreateTransparentMaterial(string path, Color color)
    {
        Material material = CreateMaterial(path, color);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.renderQueue = 3000;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return material;
    }

    private static Font GetFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }

    private static void Assign(Object target, string fieldName, object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogWarning("Missing serialized field: " + fieldName);
            return;
        }

        if (value is Object objectValue)
        {
            property.objectReferenceValue = objectValue;
        }
        else if (value is bool boolValue)
        {
            property.boolValue = boolValue;
        }
        else if (value is string stringValue)
        {
            property.stringValue = stringValue;
        }
        else if (value is MovingObstacle[] obstacles)
        {
            property.arraySize = obstacles.Length;
            for (int i = 0; i < obstacles.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = obstacles[i];
            }
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureSceneInBuildSettings(params string[] requiredPaths)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = requiredPaths.Length - 1; i >= 0; i--)
        {
            scenes.RemoveAll(scene => scene.path == requiredPaths[i]);
        }

        for (int i = requiredPaths.Length - 1; i >= 0; i--)
        {
            scenes.Insert(0, new EditorBuildSettingsScene(requiredPaths[i], true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private readonly struct UiRefs
    {
        public readonly GameObject MainMenu;
        public readonly GameObject Hud;
        public readonly GameObject GameOver;
        public readonly Text ScoreText;
        public readonly Text TimerText;
        public readonly Text DifficultyText;
        public readonly Text MessageText;
        public readonly Text GameOverText;

        public UiRefs(GameObject mainMenu, GameObject hud, GameObject gameOver, Text scoreText, Text timerText, Text difficultyText, Text messageText, Text gameOverText)
        {
            MainMenu = mainMenu;
            Hud = hud;
            GameOver = gameOver;
            ScoreText = scoreText;
            TimerText = timerText;
            DifficultyText = difficultyText;
            MessageText = messageText;
            GameOverText = gameOverText;
        }
    }
}
