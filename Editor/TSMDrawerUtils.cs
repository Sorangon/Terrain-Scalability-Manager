namespace TSM.Editor {
    using UnityEngine;
    using UnityEditor;

    public static class TSMDrawerUtils {
        internal const float k_PropertyOffset = 13f;

        public static Rect GetLabelRect(Rect propertyRect) {
            propertyRect.width -= EditorGUIUtility.labelWidth - k_PropertyOffset;
            return propertyRect;
        }

        public static Rect GetValueRect(Rect propertyRect) {
            float labelWidth = EditorGUIUtility.labelWidth - k_PropertyOffset;
            propertyRect.x += labelWidth;
            propertyRect.width -= labelWidth;
            return propertyRect;
        }
    }
}
