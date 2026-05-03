using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public class PenaltySceneBuilder
{
    private const string ScenePath = "Assets/Scenes/PenaltyScene.unity";

    [MenuItem("Tools/Build Penalty Scene")]
    public static void BuildScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject ground = CreateGround();
        GameObject ball = CreateBall();
        GameObject player = CreatePlayer();
        GameObject goal = CreateGoal();
        GameObject goalkeeper = CreateGoalkeeper();
        GameObject camera = CreateCamera();
        CreateLight();
        GameObject canvas = CreateCanvas();

        WireReferences(player, ball, goal, goalkeeper, canvas);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.Refresh();

        Debug.Log("✓ Penalty Scene built successfully!");
    }

    private static GameObject CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, -0.5f, 5);
        ground.transform.localScale = new Vector3(3, 1, 10);

        DestroyImmediate(ground.GetComponent<Collider>());
        BoxCollider collider = ground.AddComponent<BoxCollider>();
        collider.center = Vector3.zero;

        return ground;
    }

    private static GameObject CreateBall()
    {
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Ball";
        ball.transform.position = new Vector3(0, 0.3f, 0);
        ball.transform.localScale = Vector3.one * 0.3f;

        DestroyImmediate(ball.GetComponent<Collider>());
        SphereCollider collider = ball.AddComponent<SphereCollider>();

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb == null) rb = ball.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = true;

        BallController ballCtrl = ball.AddComponent<BallController>();
        SerializedObject so = new SerializedObject(ballCtrl);
        so.FindProperty("ballRigidbody").objectReferenceValue = rb;
        // create reset transform child and assign
        GameObject resetPoint = new GameObject("ResetPoint");
        resetPoint.transform.SetParent(ball.transform, false);
        resetPoint.transform.localPosition = Vector3.zero;
        so.FindProperty("resetPoint").objectReferenceValue = resetPoint.transform;
        so.ApplyModifiedProperties();

        return ball;
    }

    private static GameObject CreatePlayer()
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0, 0, -3);

        DestroyImmediate(player.GetComponent<Collider>());
        CapsuleCollider collider = player.AddComponent<CapsuleCollider>();

        player.AddComponent<PlayerKicker>();

        return player;
    }

    private static GameObject CreateGoal()
    {
        GameObject goal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        goal.name = "Goal";
        goal.transform.position = new Vector3(0, 1, 10);
        goal.transform.localScale = new Vector3(2, 2, 0.5f);

        BoxCollider collider = goal.GetComponent<BoxCollider>();
        collider.isTrigger = true;

        goal.AddComponent<GoalDetector>();

        return goal;
    }

    private static GameObject CreateGoalkeeper()
    {
        GameObject goalkeeper = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        goalkeeper.name = "Goalkeeper";
        goalkeeper.transform.position = new Vector3(0, 0, 9);

        DestroyImmediate(goalkeeper.GetComponent<Collider>());
        CapsuleCollider collider = goalkeeper.AddComponent<CapsuleCollider>();

        Rigidbody rb = goalkeeper.GetComponent<Rigidbody>();
        if (rb == null) rb = goalkeeper.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        GoalkeeperController gkCtrl = goalkeeper.AddComponent<GoalkeeperController>();
        SerializedObject so = new SerializedObject(gkCtrl);
        so.FindProperty("goalkeeperRigidbody").objectReferenceValue = rb;
        so.ApplyModifiedProperties();

        return goalkeeper;
    }

    private static GameObject CreateCamera()
    {
        GameObject cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        cameraObj.AddComponent<Camera>();
        cameraObj.AddComponent<AudioListener>();
        cameraObj.transform.position = new Vector3(0, 2, -8);
        cameraObj.transform.LookAt(new Vector3(0, 1, 10));

        return cameraObj;
    }

    private static void CreateLight()
    {
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.5f;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
    }

    private static GameObject CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas");

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        UIManager uiMgr = canvasObj.AddComponent<UIManager>();

        Text scoreText = CreateUIText(canvasObj, "ScoreText", "P1 Score: 0", new Vector2(-150, 150), 24);
        Text timerText = CreateUIText(canvasObj, "TimerText", "Time: 60", new Vector2(0, 150), 24);
        Text resultText = CreateUIText(canvasObj, "ResultText", "", new Vector2(0, 0), 32);

        SerializedObject uiSo = new SerializedObject(uiMgr);
        uiSo.FindProperty("scoreText").objectReferenceValue = scoreText;
        uiSo.FindProperty("timerText").objectReferenceValue = timerText;
        uiSo.FindProperty("resultText").objectReferenceValue = resultText;
        uiSo.ApplyModifiedProperties();

        return canvasObj;
    }

    private static Text CreateUIText(GameObject parent, string name, string content, Vector2 position, int fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(300, 60);

        Image bgImage = textObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);

        Text text = textObj.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return text;
    }

    private static void WireReferences(GameObject player, GameObject ball, GameObject goal, GameObject goalkeeper, GameObject canvas)
    {
        GameObject gmObj = new GameObject("GameManager");
        GameManager gm = gmObj.AddComponent<GameManager>();

        PlayerKicker playerKicker = player.GetComponent<PlayerKicker>();
        BallController ballController = ball.GetComponent<BallController>();
        GoalkeeperController goalkeeperController = goalkeeper.GetComponent<GoalkeeperController>();
        UIManager uiManager = canvas.GetComponent<UIManager>();

        SerializedObject gmSo = new SerializedObject(gm);
        gmSo.FindProperty("player").objectReferenceValue = playerKicker;
        gmSo.FindProperty("ball").objectReferenceValue = ballController;
        gmSo.FindProperty("goalkeeper").objectReferenceValue = goalkeeperController;
        gmSo.FindProperty("uiManager").objectReferenceValue = uiManager;
        gmSo.ApplyModifiedProperties();

        SerializedObject pkSo = new SerializedObject(playerKicker);
        pkSo.FindProperty("ballTarget").objectReferenceValue = ball.transform;
        pkSo.ApplyModifiedProperties();
    }
}
