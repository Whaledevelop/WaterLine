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
        private const string MasksPath = GeneratedPath + "/Masks";
        private const string WaterlineMasksPath = GeneratedPath + "/WaterlineMasks";
        private const string ScenePath = GeneratedPath + "/CanopusPrototype.unity";
        private const string FoamBreakupPath = "Assets/_game/Content/WaterEffects/FoamBreakup.png";
        private const string WakeDecalPath = "Assets/_game/Content/WaterEffects/WakeDecal.png";

        [MenuItem("Game/Build Canopus Prototype")]
        public static void Build()
        {
            EnsureFolders();
            ConfigureTextureImports();
            GenerateSubmersionMasks();
            var hullMaterial = CreateMaterial("CanopusHull", "Game/SubmergedSprite");
            var waterMaterial = CreateMaterial("AnimatedWater", "Game/AnimatedWater");
            var effectsMaterial = CreateMaterial("WaterEffects", "Game/WaterInteraction");
            var sideEffectsMaterial = CreateMaterial("WakeSideEffects", "Game/WaterInteraction");
            var residualEffectsMaterial = CreateMaterial("WakeResidualEffects", "Game/WaterInteraction");
            var waterlineMaterial = CreateMaterial("WaterlineFoam", "Game/WaterlineFoam");
            ConfigureEffectsMaterial(effectsMaterial, FoamBreakupPath, 1f, new Vector2(1.25f, 1.1f), 0.08f,
                0.58f, 0.16f, 0.12f);
            ConfigureEffectsMaterial(sideEffectsMaterial, FoamBreakupPath, 0.42f, new Vector2(0.75f, 1.4f), 0.035f,
                0.64f, 0.15f, 0.2f);
            ConfigureEffectsMaterial(residualEffectsMaterial, WakeDecalPath, 0.38f, Vector2.one, 0f, 0.56f, 0.16f,
                0.08f);
            ConfigureWaterlineMaterial(waterlineMaterial);
            var profile = CreateProfile();
            AssetDatabase.SaveAssets();
            CreateScene(profile, hullMaterial, waterMaterial, effectsMaterial, sideEffectsMaterial,
                residualEffectsMaterial, waterlineMaterial);
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
            Directory.CreateDirectory(MasksPath);
            Directory.CreateDirectory(WaterlineMasksPath);
        }

        private static void ConfigureTextureImports()
        {
            ConfigureTexture("Assets/_game/Content/water 1.png", true);
            ConfigureEffectTexture(FoamBreakupPath, true);
            ConfigureEffectTexture(WakeDecalPath, false);
            foreach (var path in Directory.GetFiles("Assets/_game/Content/Canopus", "*.png"))
            {
                ConfigureTexture(path.Replace('\\', '/'), false);
            }
        }

        private static void GenerateSubmersionMasks()
        {
            foreach (var sourcePath in Directory.GetFiles("Assets/_game/Content/Canopus", "*.png"))
            {
                if (sourcePath.EndsWith("Canopus-scene.png"))
                {
                    continue;
                }

                var sourceBytes = File.ReadAllBytes(sourcePath);
                var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                source.LoadImage(sourceBytes);
                var sourcePixels = source.GetPixels32();
                var maskPixels = new Color32[sourcePixels.Length];
                for (var i = 0; i < sourcePixels.Length; i++)
                {
                    var sourceColor = sourcePixels[i];
                    var redDominance = sourceColor.r - Mathf.Max(sourceColor.g, sourceColor.b);
                    var strength = (byte)Mathf.RoundToInt(Mathf.InverseLerp(10f, 65f, redDominance) * sourceColor.a);
                    maskPixels[i] = new Color32(strength, strength, strength, 255);
                }

                var mask = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                mask.SetPixels32(maskPixels);
                mask.Apply();
                var maskName = $"{Path.GetFileNameWithoutExtension(sourcePath)}_Mask.png";
                var maskPath = $"{MasksPath}/{maskName}";
                if (!File.Exists(maskPath))
                {
                    File.WriteAllBytes(maskPath, mask.EncodeToPNG());
                }

                var direction = GetMaskDirection(Path.GetFileName(sourcePath));
                var waterlineMask = CreateWaterlineMask(source.width, source.height, maskPixels, direction);
                var waterlineMaskName = $"{Path.GetFileNameWithoutExtension(sourcePath)}_WaterlineV3.png";
                var waterlineMaskPath = $"{WaterlineMasksPath}/{waterlineMaskName}";
                if (!File.Exists(waterlineMaskPath))
                {
                    File.WriteAllBytes(waterlineMaskPath, waterlineMask.EncodeToPNG());
                }
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(mask);
                Object.DestroyImmediate(waterlineMask);
            }

            AssetDatabase.Refresh();
            foreach (var maskPath in Directory.GetFiles(MasksPath, "*.png"))
            {
                ConfigureMask(maskPath.Replace('\\', '/'));
            }

            foreach (var maskPath in Directory.GetFiles(WaterlineMasksPath, "*.png"))
            {
                ConfigureMask(maskPath.Replace('\\', '/'));
            }
        }

        private static Texture2D CreateWaterlineMask(int width, int height, Color32[] submersionPixels,
            Vector2 direction)
        {
            const int radius = 18;
            var waterlineStrength = new float[submersionPixels.Length];
            for (var x = 0; x < width; x++)
            {
                var waterlineY = -1;
                for (var y = height - 1; y >= 0; y--)
                {
                    if (submersionPixels[y * width + x].r > 24)
                    {
                        waterlineY = y;
                        break;
                    }
                }

                if (waterlineY < 0)
                {
                    continue;
                }

                for (var offset = -radius; offset <= radius; offset++)
                {
                    var targetY = waterlineY + offset;
                    if (targetY < 0 || targetY >= height)
                    {
                        continue;
                    }

                    var strength = 1f - Mathf.Abs(offset) / (radius + 1f);
                    var pixelIndex = targetY * width + x;
                    waterlineStrength[pixelIndex] = Mathf.Max(waterlineStrength[pixelIndex], strength);
                }
            }

            var minimumProjection = float.MaxValue;
            var maximumProjection = float.MinValue;
            var center = new Vector2(width * 0.5f, height * 0.5f);
            for (var i = 0; i < waterlineStrength.Length; i++)
            {
                if (waterlineStrength[i] <= 0f)
                {
                    continue;
                }

                var position = new Vector2(i % width, i / width) - center;
                var projection = Vector2.Dot(position, direction);
                minimumProjection = Mathf.Min(minimumProjection, projection);
                maximumProjection = Mathf.Max(maximumProjection, projection);
            }

            var waterlinePixels = new Color32[submersionPixels.Length];
            for (var i = 0; i < waterlinePixels.Length; i++)
            {
                var baseStrength = waterlineStrength[i];
                var position = new Vector2(i % width, i / width) - center;
                var projection = Vector2.Dot(position, direction);
                var bowFactor = Mathf.InverseLerp(maximumProjection * 0.45f, maximumProjection, projection);
                var sternFactor = 1f - Mathf.InverseLerp(minimumProjection, minimumProjection * 0.45f, projection);
                var waterline = (byte)Mathf.RoundToInt(baseStrength * 255f);
                var bow = (byte)Mathf.RoundToInt(baseStrength * bowFactor * 255f);
                var stern = (byte)Mathf.RoundToInt(baseStrength * sternFactor * 255f);
                waterlinePixels[i] = new Color32(waterline, bow, stern, 255);
            }

            var waterlineMask = new Texture2D(width, height, TextureFormat.RGBA32, false);
            waterlineMask.SetPixels32(waterlinePixels);
            waterlineMask.Apply();

            return waterlineMask;
        }

        private static Vector2 GetMaskDirection(string spriteName)
        {
            return spriteName switch
            {
                "Canopus_base.png" => Vector2.right,
                "Canopus_45_RightTop.png" => new Vector2(1f, 1f).normalized,
                "Canopus_stern.png" => Vector2.up,
                "Canopus_45_LeftTop.png" => new Vector2(-1f, 1f).normalized,
                "Canopus_base_b.png" => Vector2.left,
                "Canopus_45_LeftDown.png" => new Vector2(-1f, -1f).normalized,
                "Canopus_bow.png" => Vector2.down,
                "Canopus_45_RightDown.png" => new Vector2(1f, -1f).normalized,
                _ => Vector2.right
            };
        }

        private static void ConfigureMask(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
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

        private static void ConfigureEffectTexture(string path, bool repeat)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
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

        private static void ConfigureEffectsMaterial(Material material, string texturePath, float opacity,
            Vector2 textureScale, float flowSpeed, float threshold, float softness, float edgeSoftness)
        {
            material.SetTexture("_FoamTex", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetColor("_Tint", new Color(0.92f, 0.985f, 1f, opacity));
            material.SetVector("_TextureScale", textureScale);
            material.SetFloat("_FlowSpeed", flowSpeed);
            material.SetFloat("_TextureThreshold", threshold);
            material.SetFloat("_TextureSoftness", softness);
            material.SetFloat("_EdgeSoftness", edgeSoftness);
            EditorUtility.SetDirty(material);
        }

        private static void ConfigureWaterlineMaterial(Material material)
        {
            material.SetColor("_Tint", new Color(0.9f, 0.985f, 1f, 1f));
            material.SetFloat("_NoiseScale", 4.5f);
            material.SetVector("_NoiseSpeed", new Vector4(0.11f, 0.035f, 0f, 0f));
            EditorUtility.SetDirty(material);
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
            SetDirection(directions.GetArrayElementAtIndex(0), ShipDirection.Right, "Canopus_base.png", new Vector2(-0.06f, -0.46f), new Vector2(2.25f, 0f), new Vector2(-2.25f, 0f), new Vector2(0f, 0.55f), new Vector2(0f, -0.55f));
            SetDirection(directions.GetArrayElementAtIndex(1), ShipDirection.RightForward, "Canopus_45_RightTop.png", new Vector2(-0.12f, 0.04f), new Vector2(1.75f, 1.1f), new Vector2(-1.75f, -1.1f), new Vector2(-0.45f, 0.55f), new Vector2(0.45f, -0.55f));
            SetDirection(directions.GetArrayElementAtIndex(2), ShipDirection.Forward, "Canopus_stern.png", new Vector2(-0.02f, 0f), new Vector2(0f, 1.35f), new Vector2(0f, -1.35f), new Vector2(-0.7f, 0f), new Vector2(0.7f, 0f));
            SetDirection(directions.GetArrayElementAtIndex(3), ShipDirection.LeftForward, "Canopus_45_LeftTop.png", new Vector2(0.12f, 0.04f), new Vector2(-1.75f, 1.1f), new Vector2(1.75f, -1.1f), new Vector2(-0.45f, -0.55f), new Vector2(0.45f, 0.55f));
            SetDirection(directions.GetArrayElementAtIndex(4), ShipDirection.Left, "Canopus_base_b.png", new Vector2(0.06f, -0.46f), new Vector2(-2.25f, 0f), new Vector2(2.25f, 0f), new Vector2(0f, -0.55f), new Vector2(0f, 0.55f));
            SetDirection(directions.GetArrayElementAtIndex(5), ShipDirection.LeftBackward, "Canopus_45_LeftDown.png", new Vector2(-0.22f, -0.08f), new Vector2(-1.75f, -1.1f), new Vector2(1.75f, 1.1f), new Vector2(0.45f, -0.55f), new Vector2(-0.45f, 0.55f));
            SetDirection(directions.GetArrayElementAtIndex(6), ShipDirection.Backward, "Canopus_bow.png", new Vector2(-0.02f, -0.02f), new Vector2(0f, -1.35f), new Vector2(0f, 1.35f), new Vector2(0.7f, 0f), new Vector2(-0.7f, 0f));
            SetDirection(directions.GetArrayElementAtIndex(7), ShipDirection.RightBackward, "Canopus_45_RightDown.png", new Vector2(0.22f, -0.08f), new Vector2(1.75f, -1.1f), new Vector2(-1.75f, 1.1f), new Vector2(0.45f, 0.55f), new Vector2(-0.45f, -0.55f));
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();

            return profile;
        }

        private static void SetDirection(SerializedProperty property, ShipDirection direction, string spriteName,
            Vector2 visualOffset, Vector2 bow, Vector2 stern, Vector2 port, Vector2 starboard)
        {
            property.FindPropertyRelative("<Direction>k__BackingField").enumValueIndex = (int)direction;
            property.FindPropertyRelative("<Sprite>k__BackingField").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_game/Content/Canopus/{spriteName}");
            property.FindPropertyRelative("<SubmersionMask>k__BackingField").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>($"{MasksPath}/{Path.GetFileNameWithoutExtension(spriteName)}_Mask.png");
            property.FindPropertyRelative("<WaterlineMask>k__BackingField").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>($"{WaterlineMasksPath}/{Path.GetFileNameWithoutExtension(spriteName)}_WaterlineV3.png");
            property.FindPropertyRelative("<VisualOffset>k__BackingField").vector2Value = visualOffset;
            property.FindPropertyRelative("<BowAnchor>k__BackingField").vector2Value = bow;
            property.FindPropertyRelative("<SternAnchor>k__BackingField").vector2Value = stern;
            property.FindPropertyRelative("<PortAnchor>k__BackingField").vector2Value = port;
            property.FindPropertyRelative("<StarboardAnchor>k__BackingField").vector2Value = starboard;
            property.FindPropertyRelative("<FoamWidth>k__BackingField").floatValue = 0.1f;
        }

        private static void CreateScene(ShipVisualProfile profile, Material hullMaterial, Material waterMaterial,
            Material effectsMaterial, Material sideEffectsMaterial, Material residualEffectsMaterial,
            Material waterlineMaterial)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = CreateCamera();
            var waterView = CreateWater(waterMaterial);
            CreateShip(profile, hullMaterial, effectsMaterial, sideEffectsMaterial, residualEffectsMaterial,
                waterlineMaterial, out var shipView, out var foamView, out var wakeView);
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
            Material sideEffectsMaterial, Material residualEffectsMaterial, Material waterlineMaterial,
            out ShipView shipView, out ShipFoamView foamView, out ShipWakeView wakeView)
        {
            shipView = CreateObject<ShipView>("Canopus");
            var primaryRenderer = CreateChildRenderer(shipView.transform, "Hull Primary", hullMaterial, 10);
            var secondaryRenderer = CreateChildRenderer(shipView.transform, "Hull Secondary", hullMaterial, 11);
            var serializedShip = new SerializedObject(shipView);
            SetReference(serializedShip, "_primaryRenderer", primaryRenderer);
            SetReference(serializedShip, "_secondaryRenderer", secondaryRenderer);
            serializedShip.ApplyModifiedPropertiesWithoutUndo();

            var primaryFoamRenderer = CreateChildRenderer(shipView.transform, "Foam Primary", waterlineMaterial, 20);
            var secondaryFoamRenderer = CreateChildRenderer(shipView.transform, "Foam Secondary", waterlineMaterial, 21);
            foamView = ObjectFactory.AddComponent<ShipFoamView>(shipView.gameObject);
            var serializedFoam = new SerializedObject(foamView);
            SetReference(serializedFoam, "_primaryRenderer", primaryFoamRenderer);
            SetReference(serializedFoam, "_secondaryRenderer", secondaryFoamRenderer);
            serializedFoam.ApplyModifiedPropertiesWithoutUndo();

            var wakeRenderer = CreateEffectRenderer("Wake Center", effectsMaterial, 5, out var centerMeshFilter);
            CreateEffectRenderer("Wake Sides", sideEffectsMaterial, 4, out var sideMeshFilter);
            CreateEffectRenderer("Wake Residuals", residualEffectsMaterial, 3, out var residualMeshFilter);
            wakeView = ObjectFactory.AddComponent<ShipWakeView>(wakeRenderer.gameObject);
            var serializedWake = new SerializedObject(wakeView);
            SetReference(serializedWake, "_centerMeshFilter", centerMeshFilter);
            SetReference(serializedWake, "_sideMeshFilter", sideMeshFilter);
            SetReference(serializedWake, "_residualMeshFilter", residualMeshFilter);
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

        private static MeshRenderer CreateEffectRenderer(string name, Material material, int sortingOrder,
            out MeshFilter meshFilter)
        {
            var meshRenderer = CreateObject<MeshRenderer>(name);
            meshRenderer.sharedMaterial = material;
            meshRenderer.sortingOrder = sortingOrder;
            meshFilter = ObjectFactory.AddComponent<MeshFilter>(meshRenderer.gameObject);

            return meshRenderer;
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
