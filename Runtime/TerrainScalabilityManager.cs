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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void OnInit() {
            m_backedTerrainDatas = new Dictionary<TerrainData, TerrainData>(); ;
        }

        [PostProcessScene]
        public static void OnPostProcessScene() {
            if (BuildPipeline.isBuildingPlayer) {
                foreach (var tsm in FindObjectsOfType<TerrainScalabilityManager>()) {
                    if (tsm.m_TargetTerrain == null) continue;
                   
                    TerrainScalabilitySetting targetSetting = tsm.GetPlatformSetting();
                    if (targetSetting == null) continue;

                    TerrainData backedData = tsm.GetScaledTerrainData(out bool alreadyInDatabase);

                    if (!alreadyInDatabase) {
                        tsm.ApplyTerrainDataScalability(backedData, targetSetting);
                    }

                    tsm.SetTerrainData(backedData);
                    tsm.ApplyTerrainScalability(targetSetting);
                }
            }
        }

        private void Reset() {
            m_TargetTerrain = GetComponent<Terrain>();
        }

        private void Awake() {
            TerrainScalabilitySetting targetSetting = GetPlatformSetting();

            if (targetSetting != null) {
                SetTerrainData(GetScaledTerrainData(out bool alreadyInDatabase));
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

            if(setting.overrideDetailDistance >= 0f) {
                m_TargetTerrain.detailObjectDistance = setting.overrideDetailDistance;
            }

            if (setting.overrideDetailDensity >= 0f) {
                m_TargetTerrain.detailObjectDensity = setting.overrideDetailDensity;
            }

            if (setting.overrideTreeDistance >= 0f) {
                m_TargetTerrain.treeDistance = setting.overrideTreeDistance;
            }

            if (setting.overrideBillboardStart >= 0f) {
                m_TargetTerrain.treeBillboardDistance = setting.overrideBillboardStart;
            }

            if (setting.overrideFadeLength >= 0f) {
                m_TargetTerrain.treeCrossFadeLength = setting.overrideFadeLength;
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
        private void SetTerrainData(TerrainData data) {
            m_TargetTerrain.terrainData = data;

            //We get all terrain colliders to workaround the case where object has multiple colliders because of prefabs added components 
            foreach (var collider in GetComponents<TerrainCollider>()) {
                collider.terrainData = data;
            }
        }

        private TerrainData GetScaledTerrainData(out bool alreadyInDatabase) {
            TerrainData backedData = null;
            alreadyInDatabase = false;

            foreach (var btd in m_backedTerrainDatas) {
                if (m_TargetTerrain.terrainData == btd.Key) {
                    backedData = btd.Value;
                    alreadyInDatabase = true;
                    break;
                }
            }

            if (backedData == null) {
                TerrainData sourceData = m_TargetTerrain.terrainData;
                string suffix = "_Generated_" + EditorUserBuildSettings.activeBuildTarget;

                if (AssetDatabase.Contains(sourceData)) {
                    string sourceAssetPath = AssetDatabase.GetAssetPath(sourceData);
                    string newAssetPath = sourceAssetPath.Substring(0, sourceAssetPath.Length - 6) + suffix + ".asset";
                    if (AssetDatabase.FindAssets(newAssetPath).Length <= 0) {
                        if (!AssetDatabase.CopyAsset(sourceAssetPath, newAssetPath)) {
                            Debug.LogError("Failed to copy " + sourceAssetPath);
                        } else {
                            backedData = AssetDatabase.LoadMainAssetAtPath(newAssetPath) as TerrainData;
                        }
                    } else {
                        backedData = AssetDatabase.LoadMainAssetAtPath(newAssetPath) as TerrainData;
                    }

                    AssetDatabase.ImportAsset(newAssetPath);
                    m_backedTerrainDatas.Add(sourceData, backedData);
                } else {
                    backedData = Instantiate(sourceData);
                    backedData.name += suffix;
                }
            }

            return backedData;
        }

        //Random number generation, reference : https://thebookofshaders.com/11/?lan=fr
        private float RandomFloat(Vector2 coordinates) {
            return (((Mathf.Sin(Vector2.Dot(coordinates, new Vector2(12.9898f, 79.233f))) * 43758.5453123f) % 1) + 1.0f) * 0.5f;
        }
        #endregion
#endif
    }
}