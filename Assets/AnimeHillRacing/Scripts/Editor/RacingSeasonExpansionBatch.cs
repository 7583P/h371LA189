#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using AnimeHillRacing.Racing3D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;
namespace AnimeHillRacing.EditorTools
{
    public static class RacingSeasonExpansionBatch
    {
        public static void BuildAtlas5AndroidForensics()=>BuildCompressedAtlasForensics(TextureImporterFormat.ASTC_5x5,"5x5");
        public static void BuildAtlas6AndroidForensics(){ValidateAllContracts();BuildCompressedAtlasForensics(TextureImporterFormat.ASTC_6x6,"6x6");}
        static void BuildCompressedAtlasForensics(TextureImporterFormat format,string block)
        {
            string output=Path.GetFullPath(RacingFrameForensics.Argument("--closure-output"));Directory.CreateDirectory(output);
            string atlasPath="Assets/AnimeHillRacing/AndroidUiDelivery.spriteatlas";
            var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if(atlas==null)throw new InvalidOperationException("The validated full-resolution atlas is required.");
            File.Copy(atlasPath,Path.Combine(output,"AndroidUiDelivery-before.spriteatlas"));
            atlas.SetPlatformSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=4096,format=format,compressionQuality=100,textureCompression=TextureImporterCompression.CompressedHQ});
            AssetDatabase.SaveAssets();SpriteAtlasUtility.PackAtlases(new[]{atlas},BuildTarget.Android);
            int count=0;
            foreach(var packable in atlas.GetPackables())foreach(var original in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(packable)).OfType<Sprite>()){
                var packed=atlas.GetSprite(original.name);
                if(packed==null||(packed.bounds.size-original.bounds.size).magnitude>.001f||(packed.pivot/packed.rect.size-original.pivot/original.rect.size).magnitude>.001f)
                    throw new InvalidOperationException("Atlas changed sprite geometry: "+original.name);
                count++;UnityEngine.Object.DestroyImmediate(packed);
            }
            File.WriteAllText(Path.Combine(output,"atlas-validation.txt"),"Experimental ASTC "+block+", quality 100; no resizing, exact bounds/pivots for "+count+" decorative sprites. Font glyphs excluded. Visual device gate pending.\n");
            RacingRcEvidenceBuilder.BuildAndroidSizeBatch();
        }
        public static void FinalizeWindowsAndContracts()
        {
            var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/AnimeHillRacing/AndroidUiDelivery.spriteatlas");
            if(atlas!=null){
                atlas.SetPlatformSettings(new TextureImporterPlatformSettings{name="Standalone",overridden=true,maxTextureSize=4096,format=TextureImporterFormat.BC7,compressionQuality=100,textureCompression=TextureImporterCompression.CompressedHQ});
                AssetDatabase.SaveAssets();
            }
            ValidateAllContracts();
            RacingRcEvidenceBuilder.BuildWindowsBatch();
        }
        public static void ValidateAllContracts(){if(RacingRcPassContracts.RunAll()!=0)throw new InvalidOperationException("RC regression contracts failed.");RacingMmrExperimentContracts.Run();}
        public static void BuildAndroidForensics()
        {
            RacingAndroidTextureOptimization.ApplyRecipeBatch();
            RacingRcEvidenceBuilder.BuildAndroidSizeBatch();
        }
        public static void BuildAtlasedAndroidForensics()
        {
            string output=Path.GetFullPath(RacingFrameForensics.Argument("--closure-output"));Directory.CreateDirectory(output);
            string atlasPath="Assets/AnimeHillRacing/AndroidUiDelivery.spriteatlas";
            if(AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath)!=null)throw new InvalidOperationException("Preserve existing atlas; do not rebuild it implicitly.");
            File.Copy("ProjectSettings/EditorSettings.asset",Path.Combine(output,"EditorSettings-before.asset"));
            string[] paths={"Assets/Art/Chests/Coins_Chest.png","Assets/Art/Buttons/home.png","Assets/Art/Interface/HillAdventurer/You_Got.png",
                "Assets/Art/Interface/Garage/Tunes/DownforceTune.png","Assets/Art/Interface/Garage/Tunes/InclinationBuggyTune.png","Assets/Art/Interface/Garage/Tunes/ThrustersTune.png",
                "Assets/Art/Game/HUD/LowFuel.png","Assets/Art/Interface/Adventure/ChestSlots.png","Assets/Art/Interface/Adventure/CoinRewardClapperboardBadge.png",
                "Assets/Art/Interface/Adventure/OpenClapperboardBadge.png","Assets/Art/Chests/Shelf.png","Assets/Art/Interface/HillAdventurer/Background_Items.png"};
            var originals=paths.SelectMany(path=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()).ToArray();
            if(originals.Length<paths.Length)throw new InvalidOperationException("Every atlas member must already be a Sprite.");
            var atlas=new SpriteAtlas();
            atlas.SetPackingSettings(new SpriteAtlasPackingSettings{enableRotation=false,enableTightPacking=false,padding=4});
            atlas.SetTextureSettings(new SpriteAtlasTextureSettings{readable=false,generateMipMaps=false,sRGB=true,filterMode=FilterMode.Bilinear});
            atlas.SetPlatformSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=4096,format=TextureImporterFormat.ASTC_4x4,compressionQuality=80,textureCompression=TextureImporterCompression.CompressedHQ});
            atlas.SetIncludeInBuild(true);atlas.Add(paths.Select(path=>AssetDatabase.LoadMainAssetAtPath(path)).ToArray());
            AssetDatabase.CreateAsset(atlas,atlasPath);EditorSettings.spritePackerMode=SpritePackerMode.SpriteAtlasV2;
            AssetDatabase.SaveAssets();SpriteAtlasUtility.PackAtlases(new[]{atlas},BuildTarget.Android);
            int checkedSprites=0;
            foreach(var original in originals){
                var packed=atlas.GetSprite(original.name);if(packed==null)throw new InvalidOperationException("Atlas lost "+original.name);
                if((packed.bounds.size-original.bounds.size).magnitude>.001f || (packed.pivot/packed.rect.size-original.pivot/original.rect.size).magnitude>.001f)
                    throw new InvalidOperationException("Atlas changed sprite geometry: "+original.name);
                checkedSprites++;UnityEngine.Object.DestroyImmediate(packed);
            }
            File.WriteAllText(Path.Combine(output,"atlas-validation.txt"),"Source pixels and dimensions unchanged; rotation disabled; exact bounds/pivots retained for "+checkedSprites+" sprites.\n");
            RacingRcEvidenceBuilder.BuildAndroidSizeBatch();
        }
    }
}
#endif
