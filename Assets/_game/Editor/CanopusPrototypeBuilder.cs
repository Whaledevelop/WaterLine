using System.IO;
using Game.Prototype;
using Game.Ships;
using Game.Water;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class CanopusPrototypeBuilder
    {
        private const string GeneratedPath = "Assets/_game/Generated/CanopusPrototype";
        private const string ScenePath = GeneratedPath + "/CanopusPrototype.unity";

        [MenuItem("Game/Build Canopus Prototype")]
        public static void Build()
        {
            EnsureFolders();
            ConfigureTextureImports();
            var hullMaterial = CreateMaterial("CanopusHull", "Game/SubmergedSprite");
            var waterMaterial = CreateMaterial("AnimatedWater", "Game/AnimatedWater");
            var effectsMaterial = CreateMaterial("WaterEffects", "Sprites/Default");
            var profile = CreateProfile();
            AssetDatabase.SaveAssets();
            CreateScene(profile, hullMaterial, waterMaterial, effectsMaterial);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void BuildBatch()
        {
            Build();
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(GeneratedPath);
        }

        private static void ConfigureTextureImports()
        {
            ConfigureTexture("Assets/_game/Content/water 1.png", true);
            foreach (var path in Directory.GetFiles("Assets/_game/Content/Canopus", "*.png"))
            {
                ConfigureTexture(path.Replace('\\', '/'), false);
            }
        }

        private static void ConfigureTexture(string path, bool repeat)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            var serializedImporter = new SerializedObject(importer);
            serializedImporter.FindProperty("m_SpriteMeshType").intValue = 0;
            serializedImporter.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();
        }

        private static Material CreateMaterial(string name, string shaderName)
        {
            var path = $"{GeneratedPath}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find(shaderName));
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = Shader.Find(shaderName);
            EditorUtility.SetDirty(material);

            return material;
        }

        private static ShipVisualProfile CreateProfile()
        {
            var path = $"{GeneratedPath}/CanopusVisualProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<ShipVisualProfile>(path);
            if (!profile)
            {
                profile = ScriptableObject.CreateInstance<ShipVisualProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            var serializedProfile = new SerializedObject(profile);
            serializedProfile.FindProperty("<VisualScale>k__BackingField").floatValue = 0.36f;
            serializedProfile.FindProperty("<CrossfadeDuration>k__BackingField").floatValue = 0.1f;
            var directions = serializedProfile.FindProperty("<Directions>k__BackingField");
            directions.arraySize = 8;
            SetDirection(directions.GetArrayElementAtIndex(0), ShipDirection.Right, "Canopus_base.png", new Vector2(2.25f, 0f), new Vector2(-2.25f, 0f), new Vector2(0f, 0.55f), new Vector2(0f, -0.55f));
            SetDirection(directions.GetArrayElementAtIndex(1), ShipDirection.RightForward, "Canopus_45_RB.png", new Vector2(1.75f, 1.1f), new Vector2(-1.75f, -1.1f), new Vector2(-0.45f, 0.55f), new Vector2(0.45f, -0.55f));
            SetDirection(directions.GetArrayElementAtIndex(2), ShipDirection.Forward, "Canopus_stern.png", new Vector2(0f, 1.35f), new Vector2(0f, -1.35f), new Vector2(-0.7f, 0f), new Vector2(0.7f, 0f));
            SetDirection(directions.GetArrayElementAtIndex(3), ShipDirection.LeftForward, "Canopus_45_LB.png", new Vector2(-1.75f, 1.1f), new Vector2(1.75f, -1.1f), new Vector2(-0.45f, -0.55f), new Vector2(0.45f, 0.55f));
            SetDirection(directions.GetArrayElementAtIndex(4), ShipDirection.Left, "Canopus_base_b.png", new Vector2(-2.25f, 0f), new Vector2(2.25f, 0f), new Vector2(0f, -0.55f), new Vector2(0f, 0.55f));
            SetDirection(directions.GetArrayElementAtIndex(5), ShipDirection.LeftBackward, "Canopus_45_LF.png", new Vector2(-1.75f, -1.1f), new Vector2(1.75f, 1.1f), new Vector2(0.45f, -0.55f), new Vector2(-0.45f, 0.55f));
            SetDirection(directions.GetArrayElementAtIndex(6), ShipDirection.Backward, "Canopus_bow.png", new Vector2(0f, -1.35f), new Vector2(0f, 1.35f), new Vector2(0.7f, 0f), new Vector2(-0.7f, 0f));
            SetDirection(directions.GetArrayElementAtIndex(7), ShipDirection.RightBackward, "Canopus_45_RF.png", new Vector2(1.75f, -1.1f), new Vector2(-1.75f, 1.1f), new Vector2(0.45f, 0.55f), new Vector2(-0.45f, -0.55f));
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();

            return profile;
        }

        private static void SetDirection(SerializedProperty property, ShipDirection direction, string spriteName,
            Vector2 bow, Vector2 stern, Vector2 port, Vector2 starboard)
        {
            property.FindPropertyRelative("<Direction>k__BackingField").enumValueIndex = (int)direction;
            property.FindPropertyRelative("<Sprite>k__BackingField").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_game/Content/Canopus/{spriteName}");
            property.FindPropertyRelative("<SubmersionMask>k__BackingField").objectReferenceValue = null;
            property.FindPropertyRelative("<VisualOffset>k__BackingField").vector2Value = Vector2.zero;
            property.FindPropertyRelative("<BowAnchor>k__BackingField").vector2Value = bow;
            property.FindPropertyRelative("<SternAnchor>k__BackingField").vector2Value = stern;
            property.FindPropertyRelative("<PortAnchor>k__BackingField").vector2Value = port;
            property.FindPropertyRelative("<StarboardAnchor>k__BackingField").vector2Value = starboard;
            property.FindPropertyRelative("<FoamWidth>k__BackingField").floatValue = 0.1f;
        }

        private static void CreateScene(ShipVisualProfile profile, Material hullMaterial, Material waterMaterial, Material effectsMaterial)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = CreateCamera();
            var waterView = CreateWater(waterMaterial);
            CreateShip(profile, hullMaterial, effectsMaterial, out var shipView, out var foamView, out var wakeView);
            var scope = CreateObject<CanopusPrototypeScope>("Prototype Scope");
            scope.Setup(camera, profile, shipView, foamView, wakeView, waterView);
            EditorUtility.SetDirty(scope);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static Camera CreateCamera()
        {
            var camera = CreateObject<Camera>("Main Camera");
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.02f, 0.2f, 0.35f);
            ObjectFactory.AddComponent<AudioListener>(camera.gameObject);

            return camera;
        }

        private static WaterView CreateWater(Material material)
        {
            var spriteRenderer = CreateObject<SpriteRenderer>("Animated Water");
            spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_game/Content/water 1.png");
            spriteRenderer.material = material;
            spriteRenderer.drawMode = SpriteDrawMode.Tiled;
            spriteRenderer.size = new Vector2(30f, 20f);
            spriteRenderer.sortingOrder = -20;
            var waterView = ObjectFactory.AddComponent<WaterView>(spriteRenderer.gameObject);
            var serializedWater = new SerializedObject(waterView);
            SetReference(serializedWater, "_renderer", spriteRenderer);
            serializedWater.ApplyModifiedPropertiesWithoutUndo();

            return waterView;
        }

        private static void CreateShip(ShipVisualProfile profile, Material hullMaterial, Material effectsMaterial,
            out ShipView shipView, out ShipFoamView foamView, out ShipWakeView wakeView)
        {
            shipView = CreateObject<ShipView>("Canopus");
            var primaryRenderer = CreateChildRenderer(shipView.transform, "Hull Primary", hullMaterial, 10);
            var secondaryRenderer = CreateChildRenderer(shipView.transform, "Hull Secondary", hullMaterial, 11);
            var serializedShip = new SerializedObject(shipView);
            SetReference(serializedShip, "_primaryRenderer", primaryRenderer);
            SetReference(serializedShip, "_secondaryRenderer", secondaryRenderer);
            serializedShip.ApplyModifiedPropertiesWithoutUndo();

            var foamRenderer = CreateObject<LineRenderer>("Hull Foam");
            ConfigureLine(foamRenderer, effectsMaterial, 20, true);
            foamView = ObjectFactory.AddComponent<ShipFoamView>(foamRenderer.gameObject);
            var serializedFoam = new SerializedObject(foamView);
            SetReference(serializedFoam, "_lineRenderer", foamRenderer);
            serializedFoam.ApplyModifiedPropertiesWithoutUndo();

            var wakeRenderer = CreateObject<LineRenderer>("Wake");
            ConfigureLine(wakeRenderer, effectsMaterial, 5, false);
            wakeView = ObjectFactory.AddComponent<ShipWakeView>(wakeRenderer.gameObject);
            var serializedWake = new SerializedObject(wakeView);
            SetReference(serializedWake, "_lineRenderer", wakeRenderer);
            serializedWake.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SpriteRenderer CreateChildRenderer(Transform parent, string name, Material material, int sortingOrder)
        {
            var spriteRenderer = CreateObject<SpriteRenderer>(name);
            spriteRenderer.transform.SetParent(parent, false);
            spriteRenderer.material = material;
            spriteRenderer.sortingOrder = sortingOrder;

            return spriteRenderer;
        }

        private static void ConfigureLine(LineRenderer lineRenderer, Material material, int sortingOrder, bool loop)
        {
            lineRenderer.material = material;
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = loop;
            lineRenderer.numCornerVertices = 3;
            lineRenderer.numCapVertices = 3;
            lineRenderer.sortingOrder = sortingOrder;
        }

        private static T CreateObject<T>(string name) where T : Component
        {
            var gameObject = new GameObject(name);

            return ObjectFactory.AddComponent<T>(gameObject);
        }

        private static void SetReference(SerializedObject serializedObject, string propertyName, Object value)
        {
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
        }
    }
}
