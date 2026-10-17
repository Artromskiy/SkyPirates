using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SlyPirates.Tooling.TextureToVertexColor.Editor
{
    public static class TextureToVertexColorProcessor
    {
        private const string NatureMaterialPath = "Assets/Visual/Pandazole_Ultimate_Pack/Pandazole Nature Environment Pack/Materials/PandaMat.mat";
        private const string VertexColorMaterialPath = "Assets/Visual/MainMaterials/VertexColor/Default/VC.mat";
        private const string NatureModelFolder = "Assets/Visual/Pandazole_Ultimate_Pack/Pandazole Nature Environment Pack/Models";
        private const string NatureMeshOutputFolder = "Assets/Visual/3D/Props/VertexColorMeshes";
        private static readonly string[] NaturePrefabFolders =
        {
            "Assets/Resources/Prefabs/Visual/Tree",
            "Assets/Resources/Prefabs/Visual/Rock",
            "Assets/Resources/Prefabs/Visual/Cactus",
        };

        private const string BaseMapProperty = "_BaseMap";
        private const string MainTexProperty = "_MainTex";
        private const string BaseColorProperty = "_BaseColor";
        private const string ColorProperty = "_Color";
        private const float SharedVertexColorTolerance = 0.002f;

        private struct TextureSource
        {
            public Texture2D Texture;
            public Vector2 Scale;
            public Vector2 Offset;
            public Color Tint;
        }

        public static bool TryCreatePrefab(
            GameObject sourceAsset,
            Renderer selectedRenderer,
            Material vertexColorMaterial,
            out string outputPath,
            out string message)
        {
            outputPath = null;
            message = null;
            List<Texture2D> temporaryTextures = new List<Texture2D>();
            if (sourceAsset == null || selectedRenderer == null || vertexColorMaterial == null)
            {
                message = "Choose a source asset, renderer, and vertex color material.";
                return false;
            }

            string sourcePath = AssetDatabase.GetAssetPath(sourceAsset);
            if (string.IsNullOrEmpty(sourcePath) || !sourcePath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                message = "The source must be a prefab or imported model inside the Assets folder.";
                return false;
            }

            string rendererPath = GetHierarchyPath(selectedRenderer.transform, sourceAsset.transform);
            if (rendererPath == null)
            {
                message = "The selected renderer does not belong to the selected source asset.";
                return false;
            }

            GameObject contents = null;
            Mesh generatedMesh = null;
            string meshPath = null;
            outputPath = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(sourcePath);
                Transform rendererTransform = FindByPath(contents.transform, rendererPath);
                Renderer targetRenderer = rendererTransform != null ? rendererTransform.GetComponent<Renderer>() : null;
                Mesh sourceMesh = GetMesh(targetRenderer, rendererTransform);
                if (sourceMesh == null)
                {
                    message = "The selected renderer has no mesh.";
                    return false;
                }
                if (!sourceMesh.isReadable)
                {
                    message = "Mesh '" + sourceMesh.name + "' is not readable. Enable Read/Write in its model importer and retry; the tool does not change importer settings.";
                    return false;
                }

                Vector3[] vertices = sourceMesh.vertices;
                Vector2[] uv = sourceMesh.uv;
                if (vertices.Length == 0 || uv.Length != vertices.Length)
                {
                    message = "The mesh must have vertices and a complete UV0 channel.";
                    return false;
                }

                Material[] sourceMaterials = targetRenderer.sharedMaterials;
                if (!TryGetTextureSources(sourceMaterials, temporaryTextures, out TextureSource[] textureSources, out message))
                {
                    return false;
                }

                if (!TryBakeColors(sourceMesh, vertices.Length, uv, textureSources, out Color[] colors, out message))
                {
                    return false;
                }

                generatedMesh = UnityEngine.Object.Instantiate(sourceMesh);
                generatedMesh.name = sourceMesh.name + "_VertexColor";
                generatedMesh.colors = colors;
                generatedMesh.UploadMeshData(false);

                string outputFolder = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(outputFolder))
                {
                    message = "Could not determine the source asset folder.";
                    return false;
                }

                string baseName = MakeSafeFileName(sourceAsset.name) + "_VertexColor";
                outputPath = AssetDatabase.GenerateUniqueAssetPath(outputFolder + "/" + baseName + ".prefab");
                string folder = Path.GetDirectoryName(outputPath)?.Replace('\\', '/') ?? outputFolder;
                meshPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + baseName + "_Mesh.asset");
                generatedMesh.name = Path.GetFileNameWithoutExtension(meshPath);
                AssetDatabase.CreateAsset(generatedMesh, meshPath);
                generatedMesh = null;

                Mesh savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (savedMesh == null)
                {
                    throw new InvalidOperationException("Unity did not create the output mesh asset.");
                }

                SetMesh(targetRenderer, rendererTransform, savedMesh);
                Material[] outputMaterials = new Material[targetRenderer.sharedMaterials.Length];
                for (int i = 0; i < outputMaterials.Length; i++)
                {
                    outputMaterials[i] = vertexColorMaterial;
                }

                targetRenderer.sharedMaterials = outputMaterials;

                if (PrefabUtility.SaveAsPrefabAsset(contents, outputPath) == null)
                {
                    throw new InvalidOperationException("Unity did not save the converted prefab.");
                }

                AssetDatabase.SaveAssets();
                message = "Created a new prefab and mesh with texture color baked into vertex colors.";
                return true;
            }
            catch (Exception exception)
            {
                if (!string.IsNullOrEmpty(outputPath))
                {
                    AssetDatabase.DeleteAsset(outputPath);
                }

                if (!string.IsNullOrEmpty(meshPath))
                {
                    AssetDatabase.DeleteAsset(meshPath);
                }

                outputPath = null;
                message = "Texture conversion failed: " + exception.Message;
                return false;
            }
            finally
            {
                if (generatedMesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(generatedMesh);
                }

                foreach (Texture2D texture in temporaryTextures)
                {
                    if (texture != null)
                    {
                        UnityEngine.Object.DestroyImmediate(texture);
                    }
                }

                if (contents != null)
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
        }

        [MenuItem("Sky Pirates/Tools/Convert Nature Obstacle Prefabs To Vertex Color")]
        public static void ConvertNatureObstaclePrefabs()
        {
            if (!TryConvertNatureObstaclePrefabs(out string message))
            {
                Debug.LogError(message);
            }
            else
            {
                Debug.Log(message);
            }
        }

        public static void ConvertNatureObstaclePrefabsAndExit()
        {
            bool succeeded = TryConvertNatureObstaclePrefabs(out string message);
            if (succeeded)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }

            EditorApplication.Exit(succeeded ? 0 : 1);
        }

        public static void RenameNatureVertexColorMeshesAndExit()
        {
            bool succeeded = TryRenameNatureVertexColorMeshes(out string message);
            if (succeeded)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }

            EditorApplication.Exit(succeeded ? 0 : 1);
        }

        public static bool TryRenameNatureVertexColorMeshes(out string message)
        {
            message = null;
            string[] sourceModelNames = AssetDatabase.FindAssets("t:Model", new[] { NatureModelFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .OrderByDescending(name => name.Length)
                .ToArray();
            string[] meshPaths = AssetDatabase.FindAssets("t:Mesh", new[] { NatureMeshOutputFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (meshPaths.Length == 0)
            {
                message = "No converted vertex-color meshes were found.";
                return false;
            }

            var renamePlans = new Dictionary<string, string>();
            var targetCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (string meshPath in meshPaths)
            {
                if (!TryGetSourceAndMeshName(meshPath, sourceModelNames, out string sourceName, out string meshName))
                {
                    message = "Could not decode the source model name from " + meshPath;
                    return false;
                }

                string readableName = GetReadableMeshName(sourceName, meshName);
                string targetPath = NatureMeshOutputFolder + "/" + readableName + ".asset";
                targetCounts.TryGetValue(targetPath, out int duplicateCount);
                targetCounts[targetPath] = duplicateCount + 1;
                if (duplicateCount > 0)
                {
                    targetPath = NatureMeshOutputFolder + "/" + readableName + "_" + (duplicateCount + 1) + ".asset";
                }

                renamePlans.Add(meshPath, targetPath);
            }

            int renamedCount = 0;
            foreach (KeyValuePair<string, string> plan in renamePlans)
            {
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(plan.Key);
                if (mesh == null)
                {
                    continue;
                }

                string newName = Path.GetFileNameWithoutExtension(plan.Value);
                mesh.name = newName;
                EditorUtility.SetDirty(mesh);
                if (plan.Key != plan.Value)
                {
                    string error = AssetDatabase.MoveAsset(plan.Key, plan.Value);
                    if (!string.IsNullOrEmpty(error))
                    {
                        message = "Could not rename '" + plan.Key + "': " + error;
                        return false;
                    }
                }

                renamedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            message = "Renamed " + renamedCount + " vertex-color mesh assets. Unity preserved their GUIDs, so prefab references remain valid.";
            return true;
        }

        public static bool TryConvertNatureObstaclePrefabs(out string message)
        {
            message = null;
            List<string> prefabPaths = new List<string>();
            foreach (string folder in NaturePrefabFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                prefabPaths.AddRange(AssetDatabase.FindAssets("t:Prefab", new[] { folder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)));
            }

            prefabPaths = prefabPaths.Distinct().OrderBy(path => path, StringComparer.Ordinal).ToList();
            if (prefabPaths.Count == 0)
            {
                message = "No tree, rock, or cactus prefabs were found.";
                return false;
            }

            var importerReadStates = new Dictionary<string, bool>();
            var generatedMeshes = new Dictionary<string, Mesh>();
            var temporaryTextures = new List<Texture2D>();
            var errors = new List<string>();
            int convertedRendererCount = 0;
            int convertedMeshCount = 0;
            try
            {
                if (!TryEnableMeshReadback(prefabPaths, importerReadStates, out message))
                {
                    return false;
                }

                Texture2D texture = LoadNatureTexture(out Material sourceMaterial, out message);
                if (texture == null || sourceMaterial == null)
                {
                    return false;
                }

                Material vertexColorMaterial = AssetDatabase.LoadAssetAtPath<Material>(VertexColorMaterialPath);
                if (vertexColorMaterial == null)
                {
                    message = "Vertex color material is missing: " + VertexColorMaterialPath;
                    return false;
                }
                if (!TryGetTextureSources(new[] { sourceMaterial }, temporaryTextures, out TextureSource[] sourceTexture, out message))
                {
                    return false;
                }

                for (int prefabIndex = 0; prefabIndex < prefabPaths.Count; prefabIndex++)
                {
                    string prefabPath = prefabPaths[prefabIndex];
                    if (!ConvertPrefabMeshes(
                            prefabPath,
                            sourceTexture[0],
                            vertexColorMaterial,
                            generatedMeshes,
                            temporaryTextures,
                            ref convertedRendererCount,
                            ref convertedMeshCount,
                            out string prefabError))
                    {
                        errors.Add(prefabPath + ": " + prefabError);
                    }

                    if (prefabIndex % 20 == 0)
                    {
                        EditorUtility.DisplayProgressBar("Texture To Vertex Color", prefabPath, (float)prefabIndex / prefabPaths.Count);
                    }
                }
            }
            catch (Exception exception)
            {
                errors.Add(exception.ToString());
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                RestoreMeshReadback(importerReadStates);
                foreach (Texture2D texture in temporaryTextures)
                {
                    if (texture != null)
                    {
                        UnityEngine.Object.DestroyImmediate(texture);
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (errors.Count > 0)
            {
                message = "Converted " + convertedRendererCount + " renderers into " + convertedMeshCount +
                          " mesh assets, but " + errors.Count + " prefabs failed. First issue: " + errors[0];
                return false;
            }

            message = "Converted " + convertedRendererCount + " obstacle renderers across " + prefabPaths.Count +
                      " prefabs into " + convertedMeshCount + " vertex-color mesh assets. Original FBX files and importer settings were restored unchanged.";
            return true;
        }

        private static bool TryEnableMeshReadback(List<string> prefabPaths, Dictionary<string, bool> originalStates, out string message)
        {
            message = null;
            var modelPaths = new HashSet<string>();
            foreach (string prefabPath in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    continue;
                }

                foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    AddModelPath(filter.sharedMesh, modelPaths);
                }

                foreach (SkinnedMeshRenderer renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    AddModelPath(renderer.sharedMesh, modelPaths);
                }
            }

            if (modelPaths.Count == 0)
            {
                message = "The obstacle prefabs do not reference any imported meshes.";
                return false;
            }

            foreach (string modelPath in modelPaths)
            {
                ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                if (importer == null)
                {
                    message = "Could not read the model importer for " + modelPath;
                    return false;
                }

                originalStates[modelPath] = importer.isReadable;
                if (importer.isReadable)
                {
                    continue;
                }

                importer.isReadable = true;
                AssetDatabase.WriteImportSettingsIfDirty(modelPath);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            return true;
        }

        private static void RestoreMeshReadback(Dictionary<string, bool> originalStates)
        {
            bool changed = false;
            foreach (KeyValuePair<string, bool> pair in originalStates)
            {
                ModelImporter importer = AssetImporter.GetAtPath(pair.Key) as ModelImporter;
                if (importer == null || importer.isReadable == pair.Value)
                {
                    continue;
                }

                importer.isReadable = pair.Value;
                AssetDatabase.WriteImportSettingsIfDirty(pair.Key);
                changed = true;
            }

            if (changed)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
        }

        private static void AddModelPath(Mesh mesh, HashSet<string> modelPaths)
        {
            if (mesh == null)
            {
                return;
            }

            string path = AssetDatabase.GetAssetPath(mesh);
            if (!string.IsNullOrEmpty(path) && path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            {
                modelPaths.Add(path);
            }
        }

        private static Texture2D LoadNatureTexture(out Material sourceMaterial, out string message)
        {
            sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(NatureMaterialPath);
            if (sourceMaterial == null)
            {
                message = "Source material is missing: " + NatureMaterialPath;
                return null;
            }

            string property = sourceMaterial.HasProperty(BaseMapProperty) && sourceMaterial.GetTexture(BaseMapProperty) != null
                ? BaseMapProperty
                : sourceMaterial.HasProperty(MainTexProperty) && sourceMaterial.GetTexture(MainTexProperty) != null
                    ? MainTexProperty
                    : null;
            Texture2D texture = property != null ? sourceMaterial.GetTexture(property) as Texture2D : null;
            if (texture == null)
            {
                message = "PandaMat has no 2D albedo texture.";
                return null;
            }

            message = null;
            return texture;
        }

        public static void ApplyNatureVertexColorMaterialsAndExit()
        {
            bool succeeded = TryApplyNatureVertexColorMaterials(out string message);
            if (succeeded)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }

            EditorApplication.Exit(succeeded ? 0 : 1);
        }

        public static bool TryApplyNatureVertexColorMaterials(out string message)
        {
            message = null;
            Material vertexColorMaterial = AssetDatabase.LoadAssetAtPath<Material>(VertexColorMaterialPath);
            if (vertexColorMaterial == null)
            {
                message = "Vertex color material is missing: " + VertexColorMaterialPath;
                return false;
            }

            var prefabPaths = NaturePrefabFolders
                .Where(AssetDatabase.IsValidFolder)
                .SelectMany(folder => AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .ToArray();

            int rendererCount = 0;
            int meshCount = 0;
            foreach (string prefabPath in prefabPaths)
            {
                GameObject contents = null;
                try
                {
                    contents = PrefabUtility.LoadPrefabContents(prefabPath);
                    bool prefabChanged = false;
                    foreach (Renderer renderer in contents.GetComponentsInChildren<Renderer>(true))
                    {
                        Mesh mesh = GetMesh(renderer, renderer.transform);
                        if (mesh == null || !AssetDatabase.GetAssetPath(mesh).StartsWith(NatureMeshOutputFolder, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        StripUvChannels(mesh);
                        SetVertexColorMaterials(renderer, mesh, vertexColorMaterial);
                        EditorUtility.SetDirty(mesh);
                        rendererCount++;
                        meshCount++;
                        prefabChanged = true;
                    }

                    if (prefabChanged && PrefabUtility.SaveAsPrefabAsset(contents, prefabPath) == null)
                    {
                        message = "Unity failed to save " + prefabPath;
                        return false;
                    }
                }
                finally
                {
                    if (contents != null)
                    {
                        PrefabUtility.UnloadPrefabContents(contents);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            message = "Assigned VC.mat and removed UV channels from " + meshCount +
                      " converted meshes across " + prefabPaths.Length + " obstacle prefabs.";
            return rendererCount > 0;
        }

        private static bool ConvertPrefabMeshes(
            string prefabPath,
            TextureSource sourceTexture,
            Material vertexColorMaterial,
            Dictionary<string, Mesh> generatedMeshes,
            List<Texture2D> temporaryTextures,
            ref int convertedRendererCount,
            ref int convertedMeshCount,
            out string message)
        {
            message = null;
            GameObject prefabContents = null;
            try
            {
                prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
                bool changed = false;

                foreach (MeshFilter filter in prefabContents.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null)
                    {
                        continue;
                    }

                    if (!TryGetOrCreateVertexColorMesh(filter.sharedMesh, sourceTexture, generatedMeshes,
                            out Mesh convertedMesh, out bool wasCreated, out message))
                    {
                        return false;
                    }

                    filter.sharedMesh = convertedMesh;
                    SetVertexColorMaterials(filter.GetComponent<MeshRenderer>(), convertedMesh, vertexColorMaterial);
                    changed = true;
                    convertedRendererCount++;
                    if (wasCreated)
                    {
                        convertedMeshCount++;
                    }
                }

                foreach (SkinnedMeshRenderer renderer in prefabContents.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (renderer.sharedMesh == null)
                    {
                        continue;
                    }

                    if (!TryGetOrCreateVertexColorMesh(renderer.sharedMesh, sourceTexture, generatedMeshes,
                            out Mesh convertedMesh, out bool wasCreated, out message))
                    {
                        return false;
                    }

                    renderer.sharedMesh = convertedMesh;
                    SetVertexColorMaterials(renderer, convertedMesh, vertexColorMaterial);
                    changed = true;
                    convertedRendererCount++;
                    if (wasCreated)
                    {
                        convertedMeshCount++;
                    }
                }

                if (changed && PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath) == null)
                {
                    message = "Unity failed to save the converted prefab.";
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                message = exception.Message;
                return false;
            }
            finally
            {
                if (prefabContents != null)
                {
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                }
            }
        }

        private static bool TryGetOrCreateVertexColorMesh(
            Mesh sourceMesh,
            TextureSource sourceTexture,
            Dictionary<string, Mesh> generatedMeshes,
            out Mesh convertedMesh,
            out bool wasCreated,
            out string message)
        {
            convertedMesh = null;
            wasCreated = false;
            message = null;
            string meshKey = GetMeshKey(sourceMesh);
            if (generatedMeshes.TryGetValue(meshKey, out convertedMesh))
            {
                return true;
            }

            if (!sourceMesh.isReadable)
            {
                message = "Mesh '" + sourceMesh.name + "' remained unreadable after temporary importer update.";
                return false;
            }

            if (AssetDatabase.GetAssetPath(sourceMesh).StartsWith(NatureMeshOutputFolder, StringComparison.Ordinal))
            {
                if (sourceMesh.colors.Length != sourceMesh.vertexCount)
                {
                    message = "Existing converted mesh '" + sourceMesh.name + "' has no complete vertex color channel.";
                    return false;
                }

                convertedMesh = sourceMesh;
                generatedMeshes.Add(meshKey, convertedMesh);
                return true;
            }

            Vector2[] uv = sourceMesh.uv;
            if (sourceMesh.vertexCount == 0 || uv.Length != sourceMesh.vertexCount)
            {
                message = "Mesh '" + sourceMesh.name + "' has no vertices or a complete UV0 channel.";
                return false;
            }

            if (!TryBakeColors(sourceMesh, sourceMesh.vertexCount, uv, new[] { sourceTexture }, out Color[] colors, out message))
            {
                return false;
            }

            convertedMesh = UnityEngine.Object.Instantiate(sourceMesh);
            convertedMesh.name = sourceMesh.name + "_VertexColor";
            convertedMesh.colors = colors;
            StripUvChannels(convertedMesh);
            convertedMesh.UploadMeshData(false);

            EnsureFolder(NatureMeshOutputFolder);
            string outputPath = GetOutputMeshPath(sourceMesh);
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(outputPath);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(convertedMesh);
                convertedMesh = existing;
                generatedMeshes.Add(meshKey, convertedMesh);
                return true;
            }

            convertedMesh.name = Path.GetFileNameWithoutExtension(outputPath);
            AssetDatabase.CreateAsset(convertedMesh, outputPath);
            EditorUtility.SetDirty(convertedMesh);
            convertedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(outputPath);
            if (convertedMesh == null || convertedMesh.colors.Length != convertedMesh.vertexCount)
            {
                message = "Unity failed to create output mesh asset '" + outputPath + "'.";
                return false;
            }

            generatedMeshes.Add(meshKey, convertedMesh);
            wasCreated = true;
            return true;
        }

        private static string GetMeshKey(Mesh mesh)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long localId);
            return guid + "_" + localId;
        }

        private static string GetOutputMeshPath(Mesh sourceMesh)
        {
            string sourcePath = AssetDatabase.GetAssetPath(sourceMesh);
            string sourceName = Path.GetFileNameWithoutExtension(sourcePath);
            string meshName = MakeSafeFileName(sourceMesh.name);
            string filename = GetReadableMeshName(sourceName, meshName) + ".asset";
            return NatureMeshOutputFolder + "/" + filename;
        }

        private static string GetReadableMeshName(string sourceName, string meshName)
        {
            if (string.Equals(sourceName, meshName, StringComparison.Ordinal))
            {
                return sourceName;
            }

            int commonLength = 0;
            int commonLimit = Mathf.Min(sourceName.Length, meshName.Length);
            while (commonLength < commonLimit && sourceName[commonLength] == meshName[commonLength])
            {
                commonLength++;
            }

            if (commonLength >= sourceName.Length)
            {
                return sourceName + meshName.Substring(commonLength);
            }

            while (commonLength > 0 && sourceName[commonLength - 1] != '_')
            {
                commonLength--;
            }

            if (commonLength > 0)
            {
                string uniqueMeshPart = meshName.Substring(commonLength).TrimStart('_');
                if (uniqueMeshPart.Length > 0)
                {
                    return sourceName + "_" + uniqueMeshPart;
                }
            }

            return sourceName + "_" + meshName;
        }

        private static bool TryGetSourceAndMeshName(string meshPath, string[] sourceModelNames, out string sourceName, out string meshName)
        {
            sourceName = null;
            meshName = null;
            string filename = Path.GetFileNameWithoutExtension(meshPath);
            const string generatedSuffix = "_VertexColor";
            if (!filename.EndsWith(generatedSuffix, StringComparison.Ordinal))
            {
                return false;
            }

            string encodedName = filename.Substring(0, filename.Length - generatedSuffix.Length);
            foreach (string candidate in sourceModelNames)
            {
                string prefix = candidate + "_";
                if (!encodedName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                string meshAndId = encodedName.Substring(prefix.Length);
                int idSeparator = meshAndId.LastIndexOf('_');
                if (idSeparator <= 0 || !long.TryParse(meshAndId.Substring(idSeparator + 1), out _))
                {
                    continue;
                }

                sourceName = candidate;
                meshName = meshAndId.Substring(0, idSeparator);
                return true;
            }

            return false;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] segments = folder.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }

        private static void SetVertexColorMaterials(Renderer renderer, Mesh mesh, Material vertexColorMaterial)
        {
            if (renderer == null || mesh == null || vertexColorMaterial == null)
            {
                return;
            }

            Material[] currentMaterials = renderer.sharedMaterials;
            int materialCount = Mathf.Max(1, Mathf.Max(mesh.subMeshCount, currentMaterials.Length));
            bool alreadyAssigned = currentMaterials.Length == materialCount;
            for (int i = 0; alreadyAssigned && i < currentMaterials.Length; i++)
            {
                alreadyAssigned = currentMaterials[i] == vertexColorMaterial;
            }

            if (alreadyAssigned)
            {
                return;
            }

            var vertexColorMaterials = new Material[materialCount];
            for (int i = 0; i < vertexColorMaterials.Length; i++)
            {
                vertexColorMaterials[i] = vertexColorMaterial;
            }

            renderer.sharedMaterials = vertexColorMaterials;
        }

        private static void StripUvChannels(Mesh mesh)
        {
            var emptyUvs = new List<Vector4>(0);
            for (int channel = 0; channel < 8; channel++)
            {
                mesh.SetUVs(channel, emptyUvs);
            }
        }

        private static bool TryGetTextureSources(
            Material[] materials,
            List<Texture2D> temporaryTextures,
            out TextureSource[] sources,
            out string message)
        {
            sources = new TextureSource[materials.Length];
            message = null;
            Dictionary<Texture, Texture2D> readableCopies = new Dictionary<Texture, Texture2D>();
            if (materials.Length == 0)
            {
                message = "The renderer has no materials assigned.";
                return false;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                {
                    message = "Material slot " + i + " is empty.";
                    return false;
                }

                string property = material.HasProperty(BaseMapProperty) && material.GetTexture(BaseMapProperty) != null
                    ? BaseMapProperty
                    : material.HasProperty(MainTexProperty) && material.GetTexture(MainTexProperty) != null
                        ? MainTexProperty
                        : null;
                Texture2D texture = property != null ? material.GetTexture(property) as Texture2D : null;
                if (texture == null)
                {
                    message = "Material '" + material.name + "' has no 2D albedo texture in _BaseMap or _MainTex.";
                    return false;
                }
                if (!texture.isReadable)
                {
                    if (!readableCopies.TryGetValue(texture, out Texture2D readableCopy))
                    {
                        readableCopy = CreateReadableCopy(texture);
                        if (readableCopy == null)
                        {
                            message = "Could not read texture '" + texture.name + "' back from the GPU.";
                            return false;
                        }

                        readableCopies.Add(texture, readableCopy);
                        temporaryTextures.Add(readableCopy);
                    }

                    texture = readableCopy;
                }

                sources[i] = new TextureSource
                {
                    Texture = texture,
                    Scale = material.GetTextureScale(property),
                    Offset = material.GetTextureOffset(property),
                    Tint = GetMaterialTint(material),
                };
            }

            return true;
        }

        private static Texture2D CreateReadableCopy(Texture2D source)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture temporary = null;
            Texture2D copy = null;
            try
            {
                temporary = RenderTexture.GetTemporary(
                    source.width,
                    source.height,
                    0,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Linear);
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true)
                {
                    wrapMode = source.wrapMode,
                    filterMode = source.filterMode,
                };
                copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
                copy.Apply(false, false);
                return copy;
            }
            catch
            {
                if (copy != null)
                {
                    UnityEngine.Object.DestroyImmediate(copy);
                }

                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                if (temporary != null)
                {
                    RenderTexture.ReleaseTemporary(temporary);
                }
            }
        }

        private static bool TryBakeColors(
            Mesh mesh,
            int vertexCount,
            Vector2[] uv,
            TextureSource[] textureSources,
            out Color[] colors,
            out string message)
        {
            colors = new Color[vertexCount];
            message = null;
            Color[] originalColors = mesh.colors;
            bool hasOriginalColors = originalColors.Length == vertexCount;
            bool[] assigned = new bool[vertexCount];

            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                int materialIndex = Mathf.Min(submesh, textureSources.Length - 1);
                TextureSource source = textureSources[materialIndex];
                int[] indices = mesh.GetIndices(submesh);
                foreach (int vertexIndex in indices)
                {
                    Vector2 sampleUv = Vector2.Scale(uv[vertexIndex], source.Scale) + source.Offset;
                    Color sample = source.Texture.GetPixelBilinear(sampleUv.x, sampleUv.y) * source.Tint;
                    if (hasOriginalColors)
                    {
                        sample *= originalColors[vertexIndex];
                    }

                    if (assigned[vertexIndex])
                    {
                        if (ColorDistance(colors[vertexIndex], sample) > SharedVertexColorTolerance)
                        {
                            message = "A mesh vertex is shared by submeshes that sample different materials. Split those vertices in the source mesh before conversion.";
                            return false;
                        }
                        continue;
                    }

                    colors[vertexIndex] = sample;
                    assigned[vertexIndex] = true;
                }
            }

            TextureSource fallback = textureSources[0];
            for (int i = 0; i < vertexCount; i++)
            {
                if (assigned[i])
                {
                    continue;
                }

                Vector2 sampleUv = Vector2.Scale(uv[i], fallback.Scale) + fallback.Offset;
                Color sample = fallback.Texture.GetPixelBilinear(sampleUv.x, sampleUv.y) * fallback.Tint;
                if (hasOriginalColors)
                {
                    sample *= originalColors[i];
                }

                colors[i] = sample;
            }

            return true;
        }

        private static Color GetMaterialTint(Material material)
        {
            if (material.HasProperty(BaseColorProperty))
            {
                return material.GetColor(BaseColorProperty);
            }

            if (material.HasProperty(ColorProperty))
            {
                return material.GetColor(ColorProperty);
            }

            return Color.white;
        }

        private static float ColorDistance(Color a, Color b)
        {
            float red = a.r - b.r;
            float green = a.g - b.g;
            float blue = a.b - b.b;
            float alpha = a.a - b.a;
            return Mathf.Sqrt(red * red + green * green + blue * blue + alpha * alpha);
        }

        private static Mesh GetMesh(Renderer renderer, Transform rendererTransform)
        {
            if (renderer is SkinnedMeshRenderer skinned)
            {
                return skinned.sharedMesh;
            }

            if (renderer is MeshRenderer && rendererTransform != null)
            {
                MeshFilter filter = rendererTransform.GetComponent<MeshFilter>();
                return filter != null ? filter.sharedMesh : null;
            }
            return null;
        }

        private static void SetMesh(Renderer renderer, Transform rendererTransform, Mesh mesh)
        {
            if (renderer is SkinnedMeshRenderer skinned)
            {
                skinned.sharedMesh = mesh;
            }
            else
            {
                rendererTransform.GetComponent<MeshFilter>().sharedMesh = mesh;
            }
        }

        private static string GetHierarchyPath(Transform target, Transform root)
        {
            List<string> names = new List<string>();
            for (Transform current = target; current != null && current != root; current = current.parent)
            {
                names.Add(current.name);
            }

            if (target != root && names.Count == 0)
            {
                return null;
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private static Transform FindByPath(Transform root, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return root;
            }

            string[] names = path.Split('/');
            Transform current = root;
            foreach (string name in names)
            {
                current = current.Find(name);
                if (current == null)
                {
                    return null;
                }
            }
            return current;
        }

        private static string MakeSafeFileName(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char character in invalid)
            {
                value = value.Replace(character, '_');
            }

            return value.Replace('/', '_').Replace('\\', '_');
        }
    }
}
