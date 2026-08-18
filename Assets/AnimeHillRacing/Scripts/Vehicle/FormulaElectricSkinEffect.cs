using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    /// <summary>Creates a lightweight, alpha-masked overlay exclusively for Formula's Electricity skin.</summary>
    public static class FormulaElectricSkinEffect
    {
        public const string ElectricSkinId = "formula-skin-electricity";
        public const int StandardWheelSortingOrder = 11;
        public const int ElectricWheelSortingOrder = 13;
        private const string BodyMaskSkinId = "formula-skin-neon";
        private const string InternalName = "Formula Electricity - Internal";
        private const string ExternalName = "Formula Electricity - External Glow";
        private static readonly int MaskTextureId = Shader.PropertyToID("_MaskTex");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int ExternalAmountId = Shader.PropertyToID("_ExternalAmount");
        private static Material sharedMaterial;

        public static bool Apply(SpriteRenderer body, VehicleDefinition definition,
            VehicleDefinition.CustomizationOption skin)
        {
            if (body == null) return false;
            SpriteRenderer internalLayer = FindLayer(body.transform, InternalName);
            SpriteRenderer externalLayer = FindLayer(body.transform, ExternalName);
            Sprite electricity = definition != null ? definition.electricSkinOverlay : null;
            bool valid = IsElectricSkin(definition, skin) && body.sprite != null && electricity != null;
            if (!valid)
            {
                SetLayerActive(internalLayer, false);
                SetLayerActive(externalLayer, false);
                return false;
            }

            Material material = GetMaterial();
            if (material == null)
            {
                SetLayerActive(internalLayer, false);
                SetLayerActive(externalLayer, false);
                return false;
            }
            VehicleDefinition.CustomizationOption maskSkin = definition.FindSkin(BodyMaskSkinId);
            Sprite bodyMask = maskSkin != null && maskSkin.sprite != null ? maskSkin.sprite : body.sprite;
            if (bodyMask == null || bodyMask.texture == null)
            {
                SetLayerActive(internalLayer, false);
                SetLayerActive(externalLayer, false);
                return false;
            }
            internalLayer = internalLayer != null ? internalLayer : CreateLayer(body, InternalName);
            externalLayer = externalLayer != null ? externalLayer : CreateLayer(body, ExternalName);
            ConfigureLayer(internalLayer, body, electricity, bodyMask, material, 0.82f, 0f, 1f);
            // The source art has long exterior arcs. Keep them at under 1% intensity so only a trace remains.
            ConfigureLayer(externalLayer, body, electricity, bodyMask, material, 0.008f, 1f, 1.002f);
            return true;
        }

        public static bool IsElectricSkin(VehicleDefinition definition,
            VehicleDefinition.CustomizationOption skin) =>
            definition != null && definition.archetype == VehicleArchetype.Formula &&
            skin != null && skin.optionId == ElectricSkinId;

        public static int ResolveWheelSortingOrder(VehicleDefinition definition,
            VehicleDefinition.CustomizationOption skin) => IsElectricSkin(definition, skin)
            ? ElectricWheelSortingOrder : StandardWheelSortingOrder;

        private static Material GetMaterial()
        {
            if (sharedMaterial != null) return sharedMaterial;
            Shader shader = Shader.Find("AnimeHillRacing/FormulaElectricOverlay");
            if (shader == null) shader = Resources.Load<Shader>("FormulaElectricOverlay");
            if (shader == null) return null;
            sharedMaterial = new Material(shader)
            {
                name = "Formula Electric Overlay (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
            return sharedMaterial;
        }

        private static SpriteRenderer FindLayer(Transform parent, string layerName)
        {
            Transform child = parent.Find(layerName);
            return child != null ? child.GetComponent<SpriteRenderer>() : null;
        }

        private static SpriteRenderer CreateLayer(SpriteRenderer body, string layerName)
        {
            var layerObject = new GameObject(layerName);
            layerObject.transform.SetParent(body.transform, false);
            return layerObject.AddComponent<SpriteRenderer>();
        }

        private static void ConfigureLayer(SpriteRenderer layer, SpriteRenderer body, Sprite electricity,
            Sprite bodyMask, Material material, float intensity, float externalAmount, float scale)
        {
            Transform layerTransform = layer.transform;
            layerTransform.localPosition = new Vector3(0f, 0f, -.002f);
            layerTransform.localRotation = Quaternion.identity;
            layerTransform.localScale = Vector3.one * scale;
            layer.sprite = electricity;
            layer.flipX = body.flipX;
            layer.flipY = body.flipY;
            layer.color = Color.white;
            layer.sortingLayerID = body.sortingLayerID;
            layer.sortingOrder = body.sortingOrder + (externalAmount > .5f ? 1 : 2);
            layer.maskInteraction = body.maskInteraction;
            layer.sharedMaterial = material;
            var properties = new MaterialPropertyBlock();
            layer.GetPropertyBlock(properties);
            properties.SetTexture(MaskTextureId, bodyMask.texture);
            properties.SetFloat(IntensityId, intensity);
            properties.SetFloat(ExternalAmountId, externalAmount);
            layer.SetPropertyBlock(properties);
            layer.gameObject.SetActive(true);
        }

        private static void SetLayerActive(SpriteRenderer layer, bool value)
        {
            if (layer != null) layer.gameObject.SetActive(value);
        }
    }
}
