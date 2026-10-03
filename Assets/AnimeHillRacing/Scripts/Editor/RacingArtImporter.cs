#if UNITY_EDITOR
using UnityEditor;

namespace AnimeHillRacing.Editor
{
    public sealed class RacingArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/AnimeHillRacing/Resources/RacingArt/"))return;
            bool city=assetPath.EndsWith("NeonDepthCity.png");
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
            importer.alphaSource=TextureImporterAlphaSource.None;importer.isReadable=false;
            importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.mipmapEnabled=city;importer.wrapMode=city?UnityEngine.TextureWrapMode.Repeat:UnityEngine.TextureWrapMode.Clamp;
            importer.filterMode=city?UnityEngine.FilterMode.Trilinear:UnityEngine.FilterMode.Bilinear;
            importer.npotScale=TextureImporterNPOTScale.ToNearest;
        }
    }
}
#endif
