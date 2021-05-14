namespace TSM.Editor {
    using TSM.Runtime;
    using UnityEditor;
    using UnityEngine;
    using static TSMDrawerUtils;

    [CustomPropertyDrawer(typeof(TreePrototypeSelectorAttribute))]
    public class TreePrototypeSelectorAttributeDrawer : PropertyDrawer {
        private bool m_IsInit = false;
        private string[] m_PrototypesId = { };
        private int m_PreviousPrototypesCount = -1;
        private Terrain m_TargetTerrain = null;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (!m_IsInit) {
                m_IsInit = true;
                m_TargetTerrain = ((Component)property.serializedObject.targetObject).GetComponent<Terrain>();
            }

            if (m_TargetTerrain == null) {
                EditorGUI.HelpBox(position, "Missing Terrain Component", MessageType.Error);
                return;
            }

            if(m_PreviousPrototypesCount != m_TargetTerrain.terrainData.treePrototypes.Length) {
                TreePrototype[] prototypes = m_TargetTerrain.terrainData.treePrototypes;
                m_PrototypesId = new string[prototypes.Length];
                for (int i = 0; i < m_PrototypesId.Length; i++) {
                    m_PrototypesId[i] = $"[{i}] : {prototypes[i].prefab.name}";
                }
            }

            EditorGUI.LabelField(GetLabelRect(position), "Tree");

            Rect propertyRect = GetValueRect(position);

            if (m_PrototypesId.Length > 0) {
                if(property.intValue >= m_PrototypesId.Length) {
                    property.intValue = -1;
                }

                if(property.intValue < 0) {
                    Rect helpBoxRect = new Rect(propertyRect);
                    helpBoxRect.width -= 60f;

                    EditorGUI.HelpBox(helpBoxRect, "Any tree reference", MessageType.Error);

                    helpBoxRect.x += helpBoxRect.width;
                    helpBoxRect.width = 60f;
                    if (GUI.Button(helpBoxRect, "Fix")) {
                        property.intValue = 0;
                    }
                } else {
                    property.intValue = EditorGUI.Popup(propertyRect, property.intValue, m_PrototypesId);
                }
            } else {
                EditorGUI.HelpBox(propertyRect, "Any tree prototypes has been created", MessageType.Error);
            }
        }
    }
}
