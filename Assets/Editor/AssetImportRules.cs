using UnityEditor;
using UnityEngine;

namespace VCS.Editor
{
    /// <summary>
    /// Import rules for generated assets: everything under Resources/UI becomes a UI sprite,
    /// music streams, short effects decompress on load.
    /// </summary>
    public class AssetImportRules : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/UI/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.wrapMode = TextureWrapMode.Clamp;
        }

        void OnPreprocessAudio()
        {
            string p = assetPath.Replace('\\', '/');
            if (!p.Contains("/Resources/Audio/")) return;
            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            if (p.Contains("/Music/"))
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.6f;
            }
            else
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
            }
            importer.defaultSampleSettings = settings;
        }
    }
}
