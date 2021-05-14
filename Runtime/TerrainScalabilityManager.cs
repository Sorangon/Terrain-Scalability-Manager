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
        [PostProcessScene]
        public static void OnPostProcessScene() {
            if (BuildPipeline.isBuildingPlayer) {
                foreach (var tsm in FindObjectsOfType<TerrainScalabilityManager>()) {
                    tsm.ApplyScalability();
                }
            }
        }

        #region Settings
        [SerializeField, HideInInspector] private Terrain m_TargetTerrain = null;
        [SerializeField] private TerrainScalabilitySetting[] m_ScalabilitySetting = { };
        #endregion

        #region Callbacks
        private void Reset() {
            m_TargetTerrain = GetComponent<Terrain>();
        }

        private void Start() {
            m_TargetTerrain.terrainData = Instantiate(m_TargetTerrain.terrainData);
            ApplyScalability(); 
        }
        #endregion

        #region Apply
        private void ApplyScalability() {
            BuildTarget currentTarget = EditorUserBuildSettings.activeBuildTarget;
            foreach (var setting in m_ScalabilitySetting) {
                bool isTarget = false;

                foreach (var target in setting.buildTargets) {
                    if(target == currentTarget) {
                        isTarget = true;
                        break;
                    }
                }

                if(setting.overridenPixelError >= 0f) {
                    m_TargetTerrain.heightmapPixelError = setting.overridenPixelError;
                }

                if(setting.overridenBaseMapDist >= 0f) {
                    m_TargetTerrain.basemapDistance = setting.overridenBaseMapDist;
                }

                TerrainData targetData = m_TargetTerrain.terrainData;

                targetData.baseMapResolution = (int)setting.overrideBaseTextureRes;
                targetData.SetBaseMapDirty();

                if((int)setting.overrideControlTextureRes < targetData.alphamapResolution) {
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

                ManageTreesDensity(setting);

                if (isTarget) {
                    break;
                }
            }
        } 
        /// <summary>
        /// Removes trees depending trees scalability settings
        /// </summary>
        /// <param name="scalabilitySetting"></param>
        private void ManageTreesDensity(TerrainScalabilitySetting scalabilitySetting) {
            List<TreeInstance> remainingInstances = new List<TreeInstance>();
            TreeInstance[] treeInstances = m_TargetTerrain.terrainData.treeInstances;

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

            m_TargetTerrain.terrainData.treeInstances = remainingInstances.ToArray();
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