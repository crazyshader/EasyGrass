using UnityEngine;
using UnityEditor;
using EasyFramework.Grass.Runtime;

namespace EasyFramework.Grass.Editor
{
    [CustomPropertyDrawer(typeof(LayerAttribute))]
    class LayerAttributeEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.intValue = EditorGUI.LayerField(position, label, property.intValue);
        }
    }
}

