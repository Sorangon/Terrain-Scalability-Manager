namespace TSM.Runtime {
    using UnityEngine;
    using System.Collections.Generic;

#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.Callbacks;
#endif

    /// <summary>
    /// Scales trees density depending platofrm 
    /// </summary>
    [RequireComponent(typeof(Terrain))]
    public class TerrainScalabilityManager : MonoBehaviour {
#if UNITY_EDITOR
        #region Settings
        [SerializeField, HideInInspector] private Terrain m_TargetTerrain = null;
        [SerializeField] private TerrainScalabilitySetting[] m_ScalabilitySetting = { };
        #endregion

        #region Currents
        private static Dictionary<TerrainData, TerrainData> m_backedTerrainDatas = new Dictionary<TerrainData, TerrainData>();
        #endregion

        #region Callbacks
        [PostProcessScene]
        public static void OnPostProcessScene() {
            if (BuildPipeline.isBuildingPlayer) {
                foreach (var tsm in FindObjectsOfType<TerrainScalabilityManager>()) {
                    if (tsm.m_TargetTerrain == null) continue;

                    TerrainScalabilitySetting targetSetting = tsm.GetPlatformSetting();
                    if (targetSetting == null) continue;

                    TerrainData backedData = null;
                    foreach (var btd in m_backedTerrainDatas) {
                        if(tsm.m_TargetTerrain.terrainData == btd.Key) {
                            backedData = btd.Value;
                        }
                    }

                    if (backedData == null) {
                        TerrainData sourceData = tsm.m_TargetTerrain.terrainData;
                        backedData = Instantiate(sourceData);
                        string suffix = "_Backed_" + EditorUserBuildSettings.activeBuildTarget;
                        backedData.name += suffix;

                        //Create terrain data asset
                        if (AssetDatabase.Contains(sourceData)) {
                            string sourceAssetPath = AssetDatabase.GetAssetPath(sourceData);
                            string newAssetPath = sourceAssetPath.Substring(0, sourceAssetPath.Length - 6) + suffix + ".asset";
                            AssetDatabase.CreateAsset(backedData, newAssetPath);
                            AssetDatabase.ImportAsset(newAssetPath);
                            m_backedTerrainDatas.Add(sourceData, backedData);
                            tsm.ApplyTerrainDataScalability(backedData, targetSetting);
                        }

                        tsm.m_TargetTerrain.terrainData = backedData;
                        tsm.ApplyTerrainScalability(targetSetting);
                    }
                }
            }
        }

        private void Reset() {
            m_TargetTerrain = GetComponent<Terrain>();
        }

        private void Start() {
            TerrainScalabilitySetting targetSetting = GetPlatformSetting();

            if (targetSetting != null) {
                m_TargetTerrain.terrainData = Instantiate(m_TargetTerrain.terrainData);

                ApplyTerrainDataScalability(m_TargetTerrain.terrainData, targetSetting);
                ApplyTerrainScalability(targetSetting); 
            }
        }
        #endregion

        #region Apply
        private void ApplyTerrainDataScalability(TerrainData targetData, TerrainScalabilitySetting setting) {
            targetData.baseMapResolution = (int)setting.overrideBaseTextureRes;
            targetData.SetBaseMapDirty();

            if ((int)setting.overrideControlTextureRes < targetData.alphamapResolution) {
                float[,,] sourceAlphamap = targetData.GetAlphamaps(0, 0, targetData.alphamapResolution, targetData.alphamapResolution);
                int targetRes = (int)setting.overrideControlTextureRes;
                int divideRatio = targetData.alphamapResolution / targetRes;

                float[,,] newAlphaMap = new float[targetRes, targetRes, sourceAlphamap.GetLength(2)];
                for (int i = 0; i < targetRes; i++) {
                    for (int j = 0; j < targetRes; j++) {
                        int coll = i * divideRatio;
                        int row = j * divideRatio;
                        for (int k = 0; k < sourceAlphamap.GetLength(2); k++) {
                            newAlphaMap[i, j, k] = sourceAlphamap[coll, row, k];
                        }
                    }
                }

                targetData.alphamapResolution = targetRes;
                targetData.SetAlphamaps(0, 0, newAlphaMap);
            } else {
                Debug.LogWarning("[Terrrain Scalability Manager] The control texture override value is higher or equal than the base one, update it is not required");
            }

            ManageTreesDensity(setting, targetData);
        }

        private void ApplyTerrainScalability(TerrainScalabilitySetting setting) {
            if (setting.overridenPixelError >= 0f) {
                m_TargetTerrain.heightmapPixelError = setting.overridenPixelError;
            }

            if (setting.overridenBaseMapDist >= 0f) {
                m_TargetTerrain.basemapDistance = setting.overridenBaseMapDist;
            }
        } 

        private TerrainScalabilitySetting GetPlatformSetting() {
            BuildTarget currentTarget = EditorUserBuildSettings.activeBuildTarget;
            foreach (var setting in m_ScalabilitySetting) {
                foreach (var target in setting.buildTargets) {
                    if (target == currentTarget) {
                        return setting;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Removes trees depending trees scalability settings
        /// </summary>
        /// <param name="scalabilitySetting"></param>
        private void ManageTreesDensity(TerrainScalabilitySetting scalabilitySetting, TerrainData terrainData) {
            List<TreeInstance> remainingInstances = new List<TreeInstance>();
            TreeInstance[] treeInstances = terrainData.treeInstances;

            for (int i = 0; i < treeInstances.Length; i++) {
                TreeInstance ti = treeInstances[i];

                TreeScalabilitySetting setting = null;

                foreach (var treeScalability in scalabilitySetting.treeScalabilities) {
                    if(treeScalability.prototypeId == ti.prototypeIndex) {
                        setting = treeScalability;
                        break;
                    }
                }

                if(setting != null) {
                    float randomValue = RandomFloat(new Vector2((float)i, setting.seed));
                    if (randomValue <= setting.amount) {
                        remainingInstances.Add(ti);
                    }
                } else {
                    remainingInstances.Add(ti);
                }
            }

            terrainData.treeInstances = remainingInstances.ToArray();
        }
        #endregion

        #region Utility 
        //Random number generation, reference : https://thebookofshaders.com/11/?lan=fr
        private float RandomFloat(Vector2 coordinates) {
            return (((Mathf.Sin(Vector2.Dot(coordinates, new Vector2(12.9898f, 79.233f))) * 43758.5453123f) % 1) + 1.0f) * 0.5f;
        }
        #endregion
#endif
    }
}