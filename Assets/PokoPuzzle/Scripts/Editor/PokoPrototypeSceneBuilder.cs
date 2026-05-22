#if UNITY_EDITOR
using PokoPuzzle.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PokoPuzzle.Editor
{
    public readonly struct PokoPrototypeSceneSettings
    {
        public PokoPrototypeSceneSettings(string scenePath, int width, int height, int tileTypes, float spacing, bool useHexGrid)
        {
            ScenePath = scenePath;
            Width = width;
            Height = height;
            TileTypes = tileTypes;
            Spacing = spacing;
            UseHexGrid = useHexGrid;
        }

        public string ScenePath { get; }
        public int Width { get; }
        public int Height { get; }
        public int TileTypes { get; }
        public float Spacing { get; }
        public bool UseHexGrid { get; }

        public static PokoPrototypeSceneSettings Default => new("Assets/Scenes/PokoPrototype.unity", 4, 13, 5, 0.84f, true);
    }

    public static class PokoPrototypeSceneBuilder
    {
        [MenuItem("Tools/Poko Puzzle/Create Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            CreatePrototypeScene(PokoPrototypeSceneSettings.Default);
        }

        public static void CreatePrototypeScene(PokoPrototypeSceneSettings settings)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.transform.position = new Vector3(0f, 0.72f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.10f, 0.14f);
            cameraObject.tag = "MainCamera";

            var boardObject = new GameObject("Line Linker Board");
            var board = boardObject.AddComponent<LineLinkerBoard>();

            var lineObject = new GameObject("Link Line");
            var line = lineObject.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = Color.white;
            line.endColor = Color.white;
            line.startWidth = 0.15f;
            line.endWidth = 0.15f;
            line.numCapVertices = 8;

            var scoreObject = CreateText("Score HUD", new Vector3(-3.25f, 3.35f, 0f), 30, TextAnchor.UpperLeft);
            var agentObject = CreateText("AI Designer HUD", new Vector3(-3.25f, -3.05f, 0f), 15, TextAnchor.LowerLeft);
            agentObject.GetComponent<TextMesh>().color = new Color(0.82f, 0.92f, 1f);
            var feedbackObject = CreateText("Feedback HUD", new Vector3(0f, 3.5f, 0f), 42, TextAnchor.MiddleCenter);
            feedbackObject.GetComponent<TextMesh>().color = new Color(1f, 0.88f, 0.24f);

            var serializedBoard = new SerializedObject(board);
            serializedBoard.FindProperty("width").intValue = settings.Width;
            serializedBoard.FindProperty("height").intValue = settings.Height;
            serializedBoard.FindProperty("tileTypes").intValue = settings.TileTypes;
            serializedBoard.FindProperty("spacing").floatValue = 0.84f;
            serializedBoard.FindProperty("useHexGrid").boolValue = settings.UseHexGrid;
            serializedBoard.FindProperty("moveLimit").intValue = 20;
            serializedBoard.FindProperty("targetScore").intValue = 2200;
            serializedBoard.FindProperty("enablePlayLog").boolValue = true;
            serializedBoard.FindProperty("playLogPath").stringValue = "md/playtest-logs/latest-playtest.jsonl";
            serializedBoard.FindProperty("boardCamera").objectReferenceValue = camera;
            serializedBoard.FindProperty("linkLine").objectReferenceValue = line;
            serializedBoard.FindProperty("scoreText").objectReferenceValue = scoreObject.GetComponent<TextMesh>();
            serializedBoard.FindProperty("agentText").objectReferenceValue = agentObject.GetComponent<TextMesh>();
            serializedBoard.FindProperty("feedbackText").objectReferenceValue = feedbackObject.GetComponent<TextMesh>();
            serializedBoard.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, settings.ScenePath);
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(settings.ScenePath));
        }

        private static GameObject CreateText(string name, Vector3 position, int fontSize, TextAnchor anchor)
        {
            var textObject = new GameObject(name);
            textObject.transform.position = position;
            var text = textObject.AddComponent<TextMesh>();
            text.fontSize = fontSize;
            text.anchor = anchor;
            text.color = Color.white;
            text.characterSize = 0.08f;
            return textObject;
        }
    }
}
#endif
