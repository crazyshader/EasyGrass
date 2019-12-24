using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.Editor
{
    public class Debugger : MonoBehaviour
    {
        public Toggle inspectorPanel;
        public GameObject runtimeInspector;
        public GameObject runtimeHierarchy;

        public void ShowInspectorPanel()
        {
            runtimeInspector.SetActive(inspectorPanel.isOn);
            runtimeHierarchy.SetActive(inspectorPanel.isOn);
        }
    }
}
