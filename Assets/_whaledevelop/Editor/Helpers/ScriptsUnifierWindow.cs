using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public sealed class ScriptsUnifierWindow : OdinEditorWindow
{
    [MenuItem("Tools/Whaledevelop/Scripts Unifier")]
    private static void Open()
    {
        var window = GetWindow<ScriptsUnifierWindow>();
        window.titleContent = new GUIContent("Scripts Unifier");
        window.Show();
    }

    [BoxGroup("Sources")]
    [FolderPath(AbsolutePath = false)]
    [SerializeField]
    private List<string> _folders = new();

    [BoxGroup("Sources")]
    [AssetsOnly]
    [SerializeField]
    private List<MonoScript> _files = new();

    [BoxGroup("Output")]
    [Sirenix.OdinInspector.FilePath(
        Extensions = "txt",
        AbsolutePath = false)]
    [SerializeField]
    private string _outputTxtPath = "Assets/Local/scripts.txt";

    [BoxGroup("Options")]
    [SerializeField]
    private bool _includeHeaders = true;

    [BoxGroup("Options")]
    [SerializeField]
    private List<string> _extensions = new()
    {
        "cs",
        "uxml",
        "uss"
    };

    [BoxGroup("Limits")]
    [MinValue(1)]
    [SerializeField]
    private int _symbolsLimit = 500_000;

    [BoxGroup("Actions")]
    [Button(ButtonSizes.Large)]
    private void Generate()
    {
        try
        {
            var candidates = CollectCandidatePaths();

            if (candidates.Count == 0)
            {
                Debug.LogError(
                    $"No files found in selected folders/files. " +
                    $"Extensions: {GetExtensionsLabel()}");

                return;
            }

            var distinctPaths = GetDistinctValidPaths(candidates);

            if (distinctPaths.Count == 0)
            {
                Debug.LogError(
                    $"No valid files to process. " +
                    $"Extensions: {GetExtensionsLabel()}");

                return;
            }

            distinctPaths.Sort(StringComparer.OrdinalIgnoreCase);

            var result = BuildResult(distinctPaths);

            if (result.Length > _symbolsLimit)
            {
                Debug.LogError(
                    $"Result exceeds symbols limit: " +
                    $"{result.Length} > {_symbolsLimit}. Aborting.");

                return;
            }

            if (string.IsNullOrWhiteSpace(_outputTxtPath))
            {
                Debug.LogError("Target path is not set.");

                return;
            }

            var targetAssetPath = EnsureTxtExtension(
                NormalizeAssetPath(_outputTxtPath));

            if (!IsAssetPath(targetAssetPath))
            {
                Debug.LogError(
                    $"Output path must be inside the Assets folder: " +
                    $"{targetAssetPath}");

                return;
            }

            var absoluteTargetPath = AssetPathToAbsolute(targetAssetPath);
            var targetDirectory = Path.GetDirectoryName(absoluteTargetPath);

            if (string.IsNullOrWhiteSpace(targetDirectory))
            {
                Debug.LogError(
                    $"Could not determine output directory: " +
                    $"{absoluteTargetPath}");

                return;
            }

            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            File.WriteAllText(
                absoluteTargetPath,
                result,
                new UTF8Encoding(false));

            AssetDatabase.Refresh();

            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                targetAssetPath);

            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }

            Debug.Log(
                $"Files unified into: {targetAssetPath}\n" +
                $"Files count: {distinctPaths.Count}\n" +
                $"Characters: {result.Length}\n" +
                $"Extensions: {GetExtensionsLabel()}");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private List<string> CollectCandidatePaths()
    {
        var filePaths = new List<string>();

        CollectFilesFromFolders(filePaths);
        CollectManuallySelectedFiles(filePaths);

        return filePaths;
    }

    private void CollectFilesFromFolders(List<string> filePaths)
    {
        if (_folders == null || _folders.Count == 0)
        {
            return;
        }

        var extensions = GetNormalizedExtensions();

        foreach (var folder in _folders)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                continue;
            }

            var assetFolderPath = NormalizeAssetPath(folder);

            if (!IsAssetPath(assetFolderPath))
            {
                Debug.LogWarning(
                    $"Folder must be inside Assets: {assetFolderPath}");

                continue;
            }

            var absoluteFolderPath = AssetPathToAbsolute(
                assetFolderPath);

            if (!Directory.Exists(absoluteFolderPath))
            {
                Debug.LogWarning(
                    $"Folder does not exist.\n" +
                    $"Asset path: {assetFolderPath}\n" +
                    $"Absolute path: {absoluteFolderPath}");

                continue;
            }

            foreach (var extension in extensions)
            {
                var searchPattern = $"*.{extension}";

                string[] foundFiles;

                try
                {
                    foundFiles = Directory.GetFiles(
                        absoluteFolderPath,
                        searchPattern,
                        SearchOption.AllDirectories);
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"Failed to scan folder: {absoluteFolderPath}\n" +
                        exception.Message);

                    continue;
                }

                foreach (var absoluteFilePath in foundFiles)
                {
                    var assetPath = AbsoluteToAssetPath(
                        absoluteFilePath);

                    filePaths.Add(assetPath);
                }
            }
        }
    }

    private void CollectManuallySelectedFiles(
        List<string> filePaths)
    {
        if (_files == null || _files.Count == 0)
        {
            return;
        }

        foreach (var monoScript in _files)
        {
            if (monoScript == null)
            {
                continue;
            }

            var assetPath = AssetDatabase.GetAssetPath(
                monoScript);

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                continue;
            }

            filePaths.Add(NormalizeAssetPath(assetPath));
        }
    }

    private List<string> GetDistinctValidPaths(
        List<string> candidates)
    {
        var result = new List<string>();

        var uniquePaths = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var assetPath = NormalizeAssetPath(candidate);

            if (!IsAssetPath(assetPath))
            {
                Debug.LogWarning(
                    $"Skipped path outside Assets: {assetPath}");

                continue;
            }

            if (!HasAllowedExtension(assetPath))
            {
                continue;
            }

            if (!uniquePaths.Add(assetPath))
            {
                continue;
            }

            result.Add(assetPath);
        }

        return result;
    }

    private string BuildResult(List<string> assetPaths)
    {
        var builder = new StringBuilder();

        foreach (var assetPath in assetPaths)
        {
            var absolutePath = AssetPathToAbsolute(assetPath);

            if (!File.Exists(absolutePath))
            {
                Debug.LogError(
                    $"Source file was not found.\n" +
                    $"Asset path: {assetPath}\n" +
                    $"Absolute path: {absolutePath}");

                continue;
            }

            if (_includeHeaders)
            {
                builder
                    .AppendLine()
                    .AppendLine()
                    .Append("// ===== ")
                    .Append(assetPath)
                    .AppendLine(" =====");
            }

            try
            {
                var text = File.ReadAllText(absolutePath);

                builder.Append(text);

                if (!text.EndsWith("\n", StringComparison.Ordinal))
                {
                    builder.AppendLine();
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Failed to read file.\n" +
                    $"Asset path: {assetPath}\n" +
                    $"Absolute path: {absolutePath}\n" +
                    exception.Message);
            }
        }

        return builder.ToString();
    }

    private bool HasAllowedExtension(
        string assetPath)
    {
        var extension = Path.GetExtension(assetPath);

        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        var normalizedExtension = extension
            .TrimStart('.')
            .ToLowerInvariant();

        foreach (var allowedExtension in GetNormalizedExtensions())
        {
            if (string.Equals(
                    normalizedExtension,
                    allowedExtension,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private List<string> GetNormalizedExtensions()
    {
        var result = new List<string>();
        var unique = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        if (_extensions != null)
        {
            foreach (var extension in _extensions)
            {
                if (string.IsNullOrWhiteSpace(extension))
                {
                    continue;
                }

                var normalizedExtension = extension
                    .Trim()
                    .TrimStart('.')
                    .ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(normalizedExtension))
                {
                    continue;
                }

                if (unique.Add(normalizedExtension))
                {
                    result.Add(normalizedExtension);
                }
            }
        }

        if (result.Count == 0)
        {
            result.Add("cs");
        }

        return result;
    }

    private string GetExtensionsLabel()
    {
        return string.Join(", ", GetNormalizedExtensions());
    }

    private static string EnsureTxtExtension(
        string path)
    {
        if (path.EndsWith(
                ".txt",
                StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        return path + ".txt";
    }

    private static bool IsAssetPath(
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalizedPath = NormalizeAssetPath(path);

        return string.Equals(
                   normalizedPath,
                   "Assets",
                   StringComparison.OrdinalIgnoreCase)
               || normalizedPath.StartsWith(
                   "Assets/",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeAssetPath(
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return path
            .Replace('\\', '/')
            .Trim();
    }

    private static string AssetPathToAbsolute(
        string assetPath)
    {
        assetPath = NormalizeAssetPath(assetPath);

        if (!IsAssetPath(assetPath))
        {
            throw new ArgumentException(
                $"Expected Unity asset path starting with " +
                $"'Assets', but received: {assetPath}");
        }

        var projectRoot = Directory
            .GetParent(Application.dataPath)?
            .FullName;

        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            throw new DirectoryNotFoundException(
                $"Could not determine project root from " +
                $"Application.dataPath: {Application.dataPath}");
        }

        return Path.GetFullPath(
            Path.Combine(projectRoot, assetPath));
    }

    private static string AbsoluteToAssetPath(
        string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            return string.Empty;
        }

        var normalizedAbsolutePath = Path
            .GetFullPath(absolutePath)
            .Replace('\\', '/');

        var normalizedAssetsPath = Path
            .GetFullPath(Application.dataPath)
            .Replace('\\', '/')
            .TrimEnd('/');

        if (!normalizedAbsolutePath.StartsWith(
                normalizedAssetsPath + "/",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"File is outside the project's Assets folder: " +
                $"{absolutePath}");
        }

        return "Assets" + normalizedAbsolutePath
            .Substring(normalizedAssetsPath.Length);
    }
}