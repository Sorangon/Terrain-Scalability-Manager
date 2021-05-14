namespace TSM.Runtime {
    using UnityEngine;

    /// <summary>
    /// Allow user to determine the amount of tree of type a terrain must contains on a target platoform
    /// </summary>
    [System.Serializable]
    public class TreeScalabilitySetting {
        #region Settings
        [SerializeField, TreePrototypeSelector] internal int prototypeId = 0;
        [SerializeField, Range(0f, 1f)] internal float amount = 0f;
        [SerializeField] internal float seed = 0;
        #endregion
    }
}