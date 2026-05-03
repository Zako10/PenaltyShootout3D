using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class EasyLevelAutoRepair
{
    private const string SessionKey = "PenaltyShootout.AutoRepairedEasyLevel";

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
    private const string ScenePath = "Assets/Scenes/Levels/EasyPenaltyShootout.unity";
    private const string CapturedScenePath = "Assets/Scenes/Levels/EasyPenaltyShootout_Captured.unity";
    private const string TribunePrefab = "Assets/Lightning Poly/Football Essentials 3D/Prefabs/Tribune.prefab";
    private const string ScoreboardPrefab = "Assets/Lightning Poly/Football Essentials 3D/Prefabs/Scoreboard.prefab";
    private const string StadiumPrefab = "Assets/Hayq Art/GrantStadium/Prefabs/Buildings/SM_Stadium.prefab";

    [MenuItem("Tools/Penalty Shootout/Builder Window")]
    public static void OpenBuilderWindow()
    {
        PenaltyBuilderWindow.ShowWindow();
    }

    [MenuItem("Tools/Penalty Shootout/Build Easy Level Scene")]
    public static void BuildEasyLevelScene()
    {
        if (File.Exists(CapturedScenePath))
        {
            File.Copy(CapturedScenePath, ScenePath, true);
            AssetDatabase.ImportAsset(ScenePath);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EnsureSceneInBuildSettings(ScenePath);
            Debug.Log("Built Easy level from captured scene: " + ScenePath);
            return;
        }

        BuildDefaultEasyLevelScene();
    }

    [MenuItem("Tools/Penalty Shootout/Rebuild Default Easy Level Scene")]
    public static void RebuildDefaultEasyLevelScene()
    {
        BuildDefaultEasyLevelScene();
    }

    private static void BuildDefaultEasyLevelScene()
    {
        Directory.CreateDirectory("Assets/Scenes/Levels");
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
        Transform goalFrame = CreateGoalArea(gameplay.transform, white, transparentGoal);
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
            ballBody.drag = 0.18f;
            ballBody.angularDrag = 0.08f;
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

        GameObject managerObject = new GameObject("Penalty Game Manager");
        managerObject.transform.SetParent(gameplay.transform);
        AudioSource audioSource = managerObject.AddComponent<AudioSource>();
        PenaltyGameManager manager = managerObject.AddComponent<PenaltyGameManager>();

        UiRefs refs = CreateUi(uiRoot.transform, managerObject);
        Assign(manager, "ball", ballBody);
        Assign(manager, "ballStart", ballStart);
        Assign(manager, "player", player.transform);
        Assign(manager, "goalkeeper", keeper.transform);
        Assign(manager, "keeperHome", keeperHome);
        Assign(manager, "aimMarker", aimMarker);
        Assign(manager, "leftPost", goalFrame.Find("Left Post"));
        Assign(manager, "rightPost", goalFrame.Find("Right Post"));
        Assign(manager, "crossbar", goalFrame.Find("Crossbar"));
        Assign(manager, "obstacles", obstacles);
        Assign(manager, "mainMenuPanel", refs.MainMenu);
        Assign(manager, "hudPanel", refs.Hud);
        Assign(manager, "gameOverPanel", refs.GameOver);
        Assign(manager, "scoreText", refs.ScoreText);
        Assign(manager, "timerText", refs.TimerText);
        Assign(manager, "messageText", refs.MessageText);
        Assign(manager, "gameOverText", refs.GameOverText);
        Assign(manager, "powerSlider", refs.PowerSlider);
        Assign(manager, "audioSource", audioSource);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EnsureSceneInBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Built default Easy level scene: " + ScenePath);
    }

    [MenuItem("Tools/Penalty Shootout/Capture Current Scene To Builder")]
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
            GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cone.name = "Moving Cone " + (i + 1);
            cone.transform.SetParent(parent);
            cone.transform.position = positions[i];
            cone.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            cone.GetComponent<Renderer>().sharedMaterial = i % 2 == 0 ? yellow : black;
            Rigidbody rb = cone.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            MovingObstacle obstacle = cone.AddComponent<MovingObstacle>();
            obstacles.Add(obstacle);
        }

        return obstacles.ToArray();
    }

    private static UiRefs CreateUi(Transform parent, GameObject captureReceiver)
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
        CreateText("Subtitle", "Easy Level - A/D move, W/S aim, click shoots", mainMenu.transform, 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.58f), new Vector2(760f, 50f));
        Button easyButton = CreateButton("Start Easy Button", "Start Easy", mainMenu.transform, new Vector2(0.5f, 0.45f), new Vector2(260f, 66f));

        GameObject hud = CreatePanel("HUD", canvasObject.transform, new Color(0f, 0f, 0f, 0f));
        Text score = CreateText("Score Text", "Score: 0", hud.transform, 30, TextAnchor.MiddleLeft, new Vector2(0.08f, 0.93f), new Vector2(260f, 48f));
        Text timer = CreateText("Timer Text", "Time: 60", hud.transform, 30, TextAnchor.MiddleRight, new Vector2(0.9f, 0.93f), new Vector2(260f, 48f));
        Text message = CreateText("Message Text", "", hud.transform, 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.86f), new Vector2(900f, 48f));
        Slider power = CreateSlider("Power Slider", hud.transform, new Vector2(0.5f, 0.08f), new Vector2(420f, 28f));
        Button captureButton = CreateButton("Capture Scene Button", "Capture Scene", hud.transform, new Vector2(0.9f, 0.08f), new Vector2(230f, 54f));
        EditorSceneCaptureButton capture = captureReceiver.AddComponent<EditorSceneCaptureButton>();
        UnityEventTools.AddPersistentListener(captureButton.onClick, capture.CaptureSceneForBuilder);

        GameObject gameOver = CreatePanel("Game Over", canvasObject.transform, new Color(0.02f, 0.03f, 0.05f, 0.84f));
        Text gameOverText = CreateText("Game Over Text", "Game Over", gameOver.transform, 44, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.6f), new Vector2(620f, 180f));
        Button restartButton = CreateButton("Restart Button", "Restart", gameOver.transform, new Vector2(0.5f, 0.38f), new Vector2(240f, 64f));

        PenaltyGameManager manager = captureReceiver.GetComponent<PenaltyGameManager>();
        UnityEventTools.AddPersistentListener(easyButton.onClick, manager.StartEasyMatch);
        UnityEventTools.AddPersistentListener(restartButton.onClick, manager.RestartFromGameOver);

        return new UiRefs(mainMenu, hud, gameOver, score, timer, message, gameOverText, power);
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

    private static Slider CreateSlider(string name, Transform parent, Vector2 anchorCenter, Vector2 dimensions)
    {
        GameObject sliderObject = new GameObject(name);
        sliderObject.transform.SetParent(parent, false);
        RectTransform rect = sliderObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorCenter;
        rect.anchorMax = anchorCenter;
        rect.sizeDelta = dimensions;

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.65f;

        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObject.transform, false);
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bg = background.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.48f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(4f, 4f);
        fillAreaRect.offsetMax = new Vector2(-4f, -4f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.95f, 0.72f, 0.18f, 1f);
        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        return slider;
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
        post.transform.SetParent(parent);
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

        CreateBodyPart("Body", PrimitiveType.Capsule, new Vector3(0f, 0.95f, 0f), new Vector3(0.52f, 0.82f, 0.52f), root.transform, kit);
        CreateBodyPart("Head", PrimitiveType.Sphere, new Vector3(0f, 1.88f, 0f), new Vector3(0.34f, 0.34f, 0.34f), root.transform, skin);
        CreateBodyPart("Left Arm", PrimitiveType.Capsule, new Vector3(-0.42f, 1.08f, 0f), new Vector3(0.16f, 0.5f, 0.16f), root.transform, kit).transform.rotation = rotation * Quaternion.Euler(0f, 0f, -24f);
        CreateBodyPart("Right Arm", PrimitiveType.Capsule, new Vector3(0.42f, 1.08f, 0f), new Vector3(0.16f, 0.5f, 0.16f), root.transform, kit).transform.rotation = rotation * Quaternion.Euler(0f, 0f, 24f);
        CreateBodyPart("Left Leg", PrimitiveType.Capsule, new Vector3(-0.18f, 0.3f, 0f), new Vector3(0.18f, 0.45f, 0.18f), root.transform, kit);
        CreateBodyPart("Right Leg", PrimitiveType.Capsule, new Vector3(0.18f, 0.3f, 0f), new Vector3(0.18f, 0.45f, 0.18f), root.transform, kit);
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

    private static GameObject SpawnPrefabOrCapsule(string assetPath, string name, Vector3 position, Quaternion rotation, Transform parent, Material fallbackMaterial)
    {
        GameObject obj = SpawnPrefab(assetPath, name, position, rotation, parent);
        if (obj != null)
        {
            return obj;
        }

        obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.transform.localScale = new Vector3(0.65f, 1.3f, 0.65f);
        obj.GetComponent<Renderer>().sharedMaterial = fallbackMaterial;
        return obj;
    }

    private static GameObject SpawnPrefabOrPrimitive(string assetPath, PrimitiveType type, string name, Vector3 position, Quaternion rotation, Transform parent, Material fallbackMaterial)
    {
        GameObject obj = SpawnPrefab(assetPath, name, position, rotation, parent);
        if (obj != null)
        {
            return obj;
        }

        obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.GetComponent<Renderer>().sharedMaterial = fallbackMaterial;
        return obj;
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

    private static void EnsureSceneInBuildSettings(string path)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(scene => scene.path == path))
        {
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    private readonly struct UiRefs
    {
        public readonly GameObject MainMenu;
        public readonly GameObject Hud;
        public readonly GameObject GameOver;
        public readonly Text ScoreText;
        public readonly Text TimerText;
        public readonly Text MessageText;
        public readonly Text GameOverText;
        public readonly Slider PowerSlider;

        public UiRefs(GameObject mainMenu, GameObject hud, GameObject gameOver, Text scoreText, Text timerText, Text messageText, Text gameOverText, Slider powerSlider)
        {
            MainMenu = mainMenu;
            Hud = hud;
            GameOver = gameOver;
            ScoreText = scoreText;
            TimerText = timerText;
            MessageText = messageText;
            GameOverText = gameOverText;
            PowerSlider = powerSlider;
        }
    }
}

public sealed class PenaltyBuilderWindow : EditorWindow
{
    public static void ShowWindow()
    {
        GetWindow<PenaltyBuilderWindow>("Penalty Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Penalty Shootout Arena", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Build creates the Easy level scene. Capture saves the currently open scene so the next Build recreates exactly that captured version.",
            MessageType.Info);

        if (GUILayout.Button("Build Easy Level Scene", GUILayout.Height(36f)))
        {
            EasyLevelSceneBuilder.BuildEasyLevelScene();
        }

        if (GUILayout.Button("Rebuild Default Easy Level Scene", GUILayout.Height(36f)))
        {
            EasyLevelSceneBuilder.RebuildDefaultEasyLevelScene();
        }

        if (GUILayout.Button("Capture Current Scene To Builder", GUILayout.Height(36f)))
        {
            EasyLevelSceneBuilder.CaptureCurrentScene();
        }
    }
}
