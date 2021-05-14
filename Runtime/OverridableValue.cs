using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSM.Runtime {
    [System.Serializable]
    public class OverridableValue<T> {
        public bool isOverriding = false;
        public T value;
    }
}
