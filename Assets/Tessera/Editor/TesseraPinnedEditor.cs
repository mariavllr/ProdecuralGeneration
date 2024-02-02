using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    [CustomEditor(typeof(TesseraPinned))]
    [CanEditMultipleObjects]
    class TesseraPinnedEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var pinned = (target as TesseraPinned);
            var hasTile = pinned.GetComponent<TesseraTileBase>();
            var isPin = pinned.pinType == PinType.Pin;
            var expectsTilePropertySet = (isPin || !hasTile);
            var isTilePropertySet = pinned.tile != null;
            serializedObject.Update();
            if (expectsTilePropertySet || isTilePropertySet)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tile"));
            }
            if (expectsTilePropertySet && !isTilePropertySet)
            {
                EditorGUILayout.HelpBox("Tile must be set.", MessageType.Warning);
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pinType"));
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
