using System;
using UnityEditor;

namespace TripoModels
{
    // Textures of the generated models under Assets/Tripo/: at most 1024 pixels, compressed, with mipmaps.
    sealed class TripoTextureImport : AssetPostprocessor
    {
        const string Folder = "Assets/Tripo";

        void OnPreprocessTexture()
        {
            if (assetPath.StartsWith(Folder + "/", StringComparison.Ordinal))
                Cap((TextureImporter)assetImporter);
        }

        // True when the importer's settings had to change.
        static bool Cap(TextureImporter importer)
        {
            if (importer.maxTextureSize <= 1024 && importer.mipmapEnabled
                && importer.textureCompression == TextureImporterCompression.Compressed)
                return false;
            importer.maxTextureSize = Math.Min(importer.maxTextureSize, 1024);
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.mipmapEnabled = true;
            return true;
        }

        // Once per script load: textures imported before this was compiled.
        [InitializeOnLoadMethod]
        static void CapEarlierImports()
        {
            EditorApplication.delayCall += () =>
            {
                if (BuildPipeline.isBuildingPlayer || !AssetDatabase.IsValidFolder(Folder))
                    return;
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Folder }))
                {
                    var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
                    if (importer != null && Cap(importer))
                        importer.SaveAndReimport();
                }
            };
        }
    }
}
