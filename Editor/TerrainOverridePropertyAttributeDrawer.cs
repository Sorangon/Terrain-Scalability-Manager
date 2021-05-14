namespace TSM.Editor {
    using UnityEngine;
    using UnityEditor;
    using TSM.Runtime;
    using System;
    using System.Reflection;
    using static TSMDrawerUtils;

    [CustomPropertyDrawer(typeof(TerrainOverrideFloatPropertyAttribute))]
    public class TerrainOverridePropertyAttributeDrawer : PropertyDrawer {
        #region Currents
        private PropertyInfo m_TargetMember = null;
        private bool m_IsInit = false;
        private UnityEngine.Object m_TargetObject = null;
        private TerrainOverrideFloatPropertyAttribute m_Attribute = null;
        private bool m_PreviousOverrideState = false;
        private static GUIStyle m_ToggleStyle = null;
        private bool m_RefreshOverrideStateFlag = false;
        #endregion

        #region Callbacks
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.LabelField(GetLabelRect(position), label);

            if (property.propertyType != SerializedPropertyType.Float) {
                EditorGUI.HelpBox(position, "Uncompatible property type", MessageType.Error);
                return;
            }

            if (!m_IsInit) {
                Terrain targetTerrain = ((Component)property.serializedObject.targetObject).GetComponent<Terrain>();

                m_Attribute = attribute as TerrainOverrideFloatPropertyAttribute;

                Type targetType;
                if (m_Attribute.fromData) {
                    m_TargetObject = targetTerrain.terrainData;
                    targetType = typeof(TerrainData);
                } else {
                    m_TargetObject = targetTerrain;
                    targetType = typeof(Terrain);
                }

                m_TargetMember = targetType.GetProperty(m_Attribute.propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                m_RefreshOverrideStateFlag = true;

                if (m_ToggleStyle == null) {
                    m_ToggleStyle = new GUIStyle("Toggle");
                }

                Undo.undoRedoPerformed -= OnUndoRedoPerformed;
                Undo.undoRedoPerformed += OnUndoRedoPerformed;

                m_IsInit = true;
            }

            if (m_RefreshOverrideStateFlag) {
                m_PreviousOverrideState = property.floatValue >= 0f;
            }

            if (m_TargetObject == null) {
                EditorGUI.HelpBox(position, "Missing Terrain", MessageType.Error);
                return;
            }

            Rect valueRect = GetValueRect(position);
            Rect toggleRect = valueRect;
            toggleRect.width = 15f;
            toggleRect.x += k_PropertyOffset + 2f;

            EditorGUI.BeginChangeCheck();
            bool overrideState = EditorGUI.Toggle(toggleRect, m_PreviousOverrideState, m_ToggleStyle);

            float value;
            if (overrideState != m_PreviousOverrideState) {
                if (overrideState) {
                    value = (float)m_TargetMember.GetValue(m_TargetObject);
                } else {
                    value = -1;
                }

                m_PreviousOverrideState = overrideState;
            } else {
                value = property.floatValue;
            }

            valueRect.width -= 18f;
            valueRect.x += 18f;

            float displayedValue = overrideState ? value : (float)m_TargetMember.GetValue(m_TargetObject);
            if (overrideState) {
                value = EditorGUI.Slider(valueRect, displayedValue, m_Attribute.minValue, m_Attribute.maxValue);
            } else {
                EditorGUI.LabelField(valueRect, "Default : " + displayedValue);
            }
            if (overrideState) {
                property.floatValue = value;
            }
            if (EditorGUI.EndChangeCheck()) {
                EditorUtility.SetDirty(m_TargetObject);
                Undo.RecordObject(m_TargetObject, "Set Overriden Value");
                property.floatValue = value;
            }
        }

        private void OnUndoRedoPerformed() {
            m_RefreshOverrideStateFlag = true;
        } 
        #endregion
    }
}
