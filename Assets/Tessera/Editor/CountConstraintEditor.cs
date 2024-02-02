using System;
using System.Text;
using System.Threading.Tasks;
using Tessera;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    [CustomEditor(typeof(CountConstraint))]
    [CanEditMultipleObjects]
    public class CountConstraintEditor : Editor
    {
        TileListUtil tileList;

        public void OnEnable()
        {
            tileList = new TileListUtil(((TesseraConstraint)target).GetComponent<TesseraGenerator>(), "Tiles", serializedObject.FindProperty("tiles"));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            tileList.Draw();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("comparison"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("count"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("eager"));
            serializedObject.ApplyModifiedProperties();
        }

        public void TileList(SerializedProperty list)
        {
        }
    }
}
