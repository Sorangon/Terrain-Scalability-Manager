namespace TSM.Runtime {
    using UnityEngine;
    using UnityEngine.Rendering;

    /// <summary>
    /// Contains terrain datas applied on a specific platform
    /// </summary>
    [System.Serializable]
    public class TerrainScalabilitySetting {
        #region Settings
#if UNITY_EDITOR
        [SerializeField] internal UnityEditor.BuildTarget[] buildTargets = { }; 
#endif
        [SerializeField] internal TreeScalabilitySetting[] treeScalabilities = { };

        //NOTE : Overriden values will not be taken in account if the value is under 0

        [Header("Drawing")]
        [SerializeField] internal bool drawInstanced = false;
        [SerializeField, TerrainOverrideFloatProperty("heightmapPixelError", false, 1f, 200f)] internal float overridenPixelError = -1f;
        [SerializeField, TerrainOverrideFloatProperty("basemapDistance", false, 0f, 20000f)] internal float overridenBaseMapDist = -1f;
        [SerializeField] internal ShadowCastingMode overrideShadowCastingMode = (ShadowCastingMode)(-1);

        [Header("Textures")]
        [SerializeField] internal TextureResolution overrideControlTextureRes = TextureResolution._1024;
        [SerializeField] internal TextureResolution overrideBaseTextureRes = TextureResolution._1024;
        #endregion
    }
}
