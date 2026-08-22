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
        private const string CanopusPath = "Assets/_game/Generated/Canopus";
        private const string MasksPath = GeneratedPath + "/Masks";
        private const string WaterlineMasksPath = GeneratedPath + "/WaterlineMasks";
        private const string BowWaveMasksPath = CanopusPath + "/BowWaveMasks";
        private const string ScenePath = GeneratedPath + "/CanopusPrototype.unity";
        private const string FoamBreakupPath = "Assets/_game/Content/WaterEffects/FoamBreakup.png";
        private const string WakeDecalPath = "Assets/_game/Content/WaterEffects/WakeDecal.png";
        private const string WakeCenterAPath = "Assets/_game/Content/WaterEffects/WakeCenterTurbulenceA.png";
        private const string WakeCenterBPath = "Assets/_game/Content/WaterEffects/WakeCenterTurbulenceB.png";
        private const string WakeSidePath = "Assets/_game/Content/WaterEffects/WakeSideBreaker.png";

        [MenuItem("Game/Build Canopus Prototype")]
        public static void Build()
        {
            EnsureFolders();
            ConfigureTextureImports();
            GenerateSubmersionMasks();
            var hullMaterial = CreateMaterial("CanopusHull", "Game/SubmergedSprite");
            var waterMaterial = CreateMaterial("AnimatedWater", "Game/AnimatedWater");
            var wakeCenterMaterial = CreateMaterial("WaterEffects", "Game/WakeDecal");
            var wakeSideMaterial = CreateMaterial("WakeSideEffects", "Game/WakeRibbon");
            var wakeResidualMaterial = CreateMaterial("WakeResidualEffects", "Game/WakeDecal");
            var bowEffectsMaterial = CreateMaterial("BowEffects", "Game/WaterInteraction");
            var waterlineMaterial = CreateMaterial("WaterlineFoam", "Game/WaterlineFoam");
            ConfigureWakeMaterial(wakeCenterMaterial, WakeCenterAPath, WakeCenterBPath, 0.9f, 0.01f, 1.25f);
            ConfigureWakeRibbonMaterial(wakeSideMaterial, WakeSidePath, 0.78f, 1.2f);
            ConfigureWakeMaterial(wakeResidualMaterial, WakeDecalPath, WakeDecalPath, 0.38f, 0f, 1f);
            hullMaterial.SetFloat("_UnderwaterAlphaMultiplier", 0.8f);
            ConfigureEffectsMaterial(bowEffectsMaterial, FoamBreakupPath, 1f, new Vector2(1.25f, 1.1f), 0.08f,
                0.58f, 0.16f, 0.12f);
            ConfigureWaterlineMaterial(waterlineMaterial);
            var profile = CreateProfile();
            AssetDatabase.SaveAssets();
            CreateScene(profile, hullMaterial, waterMaterial, wakeCenterMaterial, wakeSideMaterial,
                wakeResidualMaterial, bowEffectsMaterial, waterlineMaterial);
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
            ConfigureEffectTexture(WakeCenterAPath, false);
            ConfigureEffectTexture(WakeCenterBPath, false);
            ConfigureEffectTexture(WakeSidePath, true);
            foreach (var path in Directory.GetFiles(CanopusPath, "*.png"))
            {
                ConfigureTexture(path.Replace('\\', '/'), false);
            }

            foreach (var path in Directory.GetFiles(BowWaveMasksPath, "*.png"))
            {
                ConfigureMask(path.Replace('\\', '/'));
            }
        }

        private static void GenerateSubmersionMasks()
        {
            foreach (var sourcePath in Directory.GetFiles(CanopusPath, "*.png"))
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

        private static void ConfigureWakeMaterial(Material material, string firstTexturePath, string secondTexturePath,
            float opacity, float flowSpeed, float alphaPower)
        {
            material.SetTexture("_WakeTexA", AssetDatabase.LoadAssetAtPath<Texture2D>(firstTexturePath));
            material.SetTexture("_WakeTexB", AssetDatabase.LoadAssetAtPath<Texture2D>(secondTexturePath));
            material.SetColor("_Tint", new Color(0.92f, 0.985f, 1f, opacity));
            material.SetFloat("_FlowSpeed", flowSpeed);
            material.SetFloat("_AlphaPower", alphaPower);
            EditorUtility.SetDirty(material);
        }

        private static void ConfigureWakeRibbonMaterial(Material material, string texturePath, float opacity,
            float alphaPower)
        {
            material.SetTexture("_WakeTex", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetColor("_Tint", new Color(0.92f, 0.985f, 1f, opacity));
            material.SetFloat("_AlphaPower", alphaPower);
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
            var directions = serializedProfile.FindProperty("<Directions>k__BackingField");
            directions.arraySize = 8;
            SetDirection(directions.GetArrayElementAtIndex(0), ShipDirection.Right, "Canopus_base.png", "Right", new Vector2(-0.06f, -0.46f), new Vector2(0f, 0.55f), new Vector2(0f, -0.55f), new Vector2(0f, -0.6f), 0f);
            SetDirection(directions.GetArrayElementAtIndex(1), ShipDirection.RightForward, "Canopus_45_RightTop.png", "RightForward", new Vector2(-0.12f, 0.04f), new Vector2(-0.45f, 0.55f), new Vector2(0.45f, -0.55f), Vector2.zero, -15f);
            SetDirection(directions.GetArrayElementAtIndex(2), ShipDirection.Forward, "Canopus_stern.png", "Forward", new Vector2(-0.02f, 0f), new Vector2(-0.7f, 0f), new Vector2(0.7f, 0f), Vector2.zero, 0f);
            SetDirection(directions.GetArrayElementAtIndex(3), ShipDirection.LeftForward, "Canopus_45_LeftTop.png", "LeftForward", new Vector2(0.12f, 0.04f), new Vector2(-0.45f, -0.55f), new Vector2(0.45f, 0.55f), Vector2.zero, 15f);
            SetDirection(directions.GetArrayElementAtIndex(4), ShipDirection.Left, "Canopus_base_b.png", "Left", new Vector2(0.06f, -0.46f), new Vector2(0f, -0.55f), new Vector2(0f, 0.55f), new Vector2(0f, 0.6f), 0f);
            SetDirection(directions.GetArrayElementAtIndex(5), ShipDirection.LeftBackward, "Canopus_45_LeftDown.png", "LeftBackward", new Vector2(-0.22f, -0.08f), new Vector2(0.45f, -0.55f), new Vector2(-0.45f, 0.55f), Vector2.zero, -15f);
            SetDirection(directions.GetArrayElementAtIndex(6), ShipDirection.Backward, "Canopus_bow.png", "Backward", new Vector2(-0.02f, -0.02f), new Vector2(0.7f, 0f), new Vector2(-0.7f, 0f), Vector2.zero, 0f);
            SetDirection(directions.GetArrayElementAtIndex(7), ShipDirection.RightBackward, "Canopus_45_RightDown.png", "RightBackward", new Vector2(0.22f, -0.08f), new Vector2(0.45f, 0.55f), new Vector2(-0.45f, -0.55f), Vector2.zero, 15f);
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            ValidateProfile(profile);

            return profile;
        }

        private static void ValidateProfile(ShipVisualProfile profile)
        {
            for (var i = 0; i < profile.Directions.Length; i++)
            {
                var current = profile.Directions[i];
                var next = profile.Directions[(i + 1) % profile.Directions.Length];
                var hullLength = Vector2.Distance(current.BowAnchor, current.SternAnchor);
                var sternJump = Vector2.Distance(current.SternAnchor, next.SternAnchor);
                if (hullLength < 0.3f || sternJump > 2.4f)
                {
                    Debug.LogError($"Invalid Canopus water anchors between directions {i} and {(i + 1) % 8}");
                }
            }
        }

        private static void SetDirection(SerializedProperty property, ShipDirection direction, string spriteName,
            string bowWaveName, Vector2 visualOffset, Vector2 port, Vector2 starboard, Vector2 bowWaveOffset,
            float bowWaveAngleOffset)
        {
            property.FindPropertyRelative("<Direction>k__BackingField").enumValueIndex = (int)direction;
            property.FindPropertyRelative("<Sprite>k__BackingField").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>($"{CanopusPath}/{spriteName}");
            property.FindPropertyRelative("<SubmersionMask>k__BackingField").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>($"{MasksPath}/{Path.GetFileNameWithoutExtension(spriteName)}_Mask.png");
            property.FindPropertyRelative("<WaterlineMask>k__BackingField").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>($"{WaterlineMasksPath}/{Path.GetFileNameWithoutExtension(spriteName)}_WaterlineV3.png");
            property.FindPropertyRelative("<BowWaveMask>k__BackingField").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{BowWaveMasksPath}/Canopus_BowWave_{bowWaveName}.png");
            property.FindPropertyRelative("<VisualOffset>k__BackingField").vector2Value = visualOffset;
            property.FindPropertyRelative("<BowAnchor>k__BackingField").vector2Value = CalculateWaterlineAnchor(
                spriteName, visualOffset, true);
            property.FindPropertyRelative("<SternAnchor>k__BackingField").vector2Value = CalculateWaterlineAnchor(
                spriteName, visualOffset, false);
            property.FindPropertyRelative("<PortAnchor>k__BackingField").vector2Value =
                port + visualOffset * 0.36f;
            property.FindPropertyRelative("<StarboardAnchor>k__BackingField").vector2Value =
                starboard + visualOffset * 0.36f;
            property.FindPropertyRelative("<FoamWidth>k__BackingField").floatValue = 0.1f;
            property.FindPropertyRelative("<BowWaveOffset>k__BackingField").vector2Value = bowWaveOffset;
            property.FindPropertyRelative("<BowWaveSize>k__BackingField").vector2Value = new Vector2(5.2f, 5.2f);
            property.FindPropertyRelative("<BowWaveAngleOffset>k__BackingField").floatValue = bowWaveAngleOffset;
        }

        private static Vector2 CalculateWaterlineAnchor(string spriteName, Vector2 visualOffset, bool bow)
        {
            const float visualScale = 0.36f;
            const float pixelsPerUnit = 100f;
            const float edgeDepth = 10f;
            var maskPath = $"{WaterlineMasksPath}/{Path.GetFileNameWithoutExtension(spriteName)}_WaterlineV3.png";
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(maskPath));
            var pixels = texture.GetPixels32();
            var direction = GetMaskDirection(spriteName) * (bow ? 1f : -1f);
            var center = new Vector2(texture.width * 0.5f, texture.height * 0.5f);
            var maximumProjection = float.MinValue;
            for (var i = 0; i < pixels.Length; i++)
            {
                var channelValue = bow ? pixels[i].g : pixels[i].b;
                if (channelValue <= 24)
                {
                    continue;
                }

                var position = new Vector2(i % texture.width, i / texture.width) - center;
                maximumProjection = Mathf.Max(maximumProjection, Vector2.Dot(position, direction));
            }

            var weightedPosition = Vector2.zero;
            var totalWeight = 0f;
            for (var i = 0; i < pixels.Length; i++)
            {
                var weight = (bow ? pixels[i].g : pixels[i].b) / 255f;
                var position = new Vector2(i % texture.width, i / texture.width) - center;
                if (weight <= 0f || Vector2.Dot(position, direction) < maximumProjection - edgeDepth)
                {
                    continue;
                }

                weightedPosition += position * weight;
                totalWeight += weight;
            }

            Object.DestroyImmediate(texture);
            var localPosition = weightedPosition / totalWeight / pixelsPerUnit + visualOffset;

            return localPosition * visualScale;
        }

        private static void CreateScene(ShipVisualProfile profile, Material hullMaterial, Material waterMaterial,
            Material wakeCenterMaterial, Material wakeSideMaterial, Material wakeResidualMaterial,
            Material bowEffectsMaterial, Material waterlineMaterial)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = CreateCamera();
            var waterView = CreateWater(waterMaterial);
            CreateShip(profile, hullMaterial, wakeCenterMaterial, wakeSideMaterial, wakeResidualMaterial,
                bowEffectsMaterial, waterlineMaterial, out var shipView, out var foamView, out var wakeView);
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

        private static void CreateShip(ShipVisualProfile profile, Material hullMaterial, Material wakeCenterMaterial,
            Material wakeSideMaterial, Material wakeResidualMaterial, Material bowEffectsMaterial,
            Material waterlineMaterial,
            out ShipView shipView, out ShipFoamView foamView, out ShipWakeView wakeView)
        {
            shipView = CreateObject<ShipView>("Canopus");
            var primaryRenderer = CreateChildRenderer(shipView.transform, "Hull Primary", hullMaterial, 10);
            var serializedShip = new SerializedObject(shipView);
            SetReference(serializedShip, "_primaryRenderer", primaryRenderer);
            serializedShip.ApplyModifiedPropertiesWithoutUndo();

            var primaryFoamRenderer = CreateChildRenderer(shipView.transform, "Foam Primary", waterlineMaterial, 20);
            foamView = ObjectFactory.AddComponent<ShipFoamView>(shipView.gameObject);
            var serializedFoam = new SerializedObject(foamView);
            SetReference(serializedFoam, "_primaryRenderer", primaryFoamRenderer);
            serializedFoam.ApplyModifiedPropertiesWithoutUndo();

            var wakeRenderer = CreateEffectRenderer("Wake Center", wakeCenterMaterial, 5, out var centerMeshFilter);
            CreateEffectRenderer("Wake Sides", wakeSideMaterial, 4, out var sideMeshFilter);
            CreateEffectRenderer("Wake Residuals", wakeResidualMaterial, 3, out var residualMeshFilter);
            var bowRenderer = CreateEffectRenderer("Bow Waves", bowEffectsMaterial, 6, out var bowMeshFilter);
            wakeView = ObjectFactory.AddComponent<ShipWakeView>(wakeRenderer.gameObject);
            var serializedWake = new SerializedObject(wakeView);
            SetReference(serializedWake, "_centerMeshFilter", centerMeshFilter);
            SetReference(serializedWake, "_sideMeshFilter", sideMeshFilter);
            SetReference(serializedWake, "_residualMeshFilter", residualMeshFilter);
            SetReference(serializedWake, "_bowMeshFilter", bowMeshFilter);
            SetReference(serializedWake, "_bowRenderer", bowRenderer);
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
