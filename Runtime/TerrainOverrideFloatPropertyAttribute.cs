namespace TSM.Runtime {
    using UnityEngine;

    public class TerrainOverrideFloatPropertyAttribute : PropertyAttribute {
        public string propertyName = "";
        public bool fromData = false;
        public float minValue = 0f;
        public float maxValue = 1f;

        public TerrainOverrideFloatPropertyAttribute(string propertyName, bool fromData, float minValue, float maxValue) {
            this.propertyName = propertyName;
            this.fromData = fromData;
            this.minValue = minValue;
            this.maxValue = maxValue;
        }
    }
}
