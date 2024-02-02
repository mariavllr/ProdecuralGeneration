using System;
using System.Text;
using System.Threading.Tasks;
using Tessera;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    [CustomEditor(typeof(PathConstraint))]
    public class PathConstraintEditor : Editor
    {
        TileListUtil tileList;
        ColorList colorList;

        public void OnEnable()
        {
            var generator = ((TesseraConstraint)target).GetComponent<TesseraGenerator>();
            tileList = new TileListUtil(generator, "Path Tiles", serializedObject.FindProperty("pathTiles"), allowSingleTile: false);
            colorList = new ColorList(serializedObject.FindProperty("pathColors"), generator.palette);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hasPathTiles"));
            if (serializedObject.FindProperty("hasPathTiles").boolValue)
            {
                tileList.Draw();
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hasPathColors"));
            if (!serializedObject.FindProperty("hasPathTiles").boolValue && !serializedObject.FindProperty("hasPathColors").boolValue)
            {
                EditorGUILayout.HelpBox("Path Constraint needs at least one of path tiles or path colors.", MessageType.Warning);
            }

            if (serializedObject.FindProperty("hasPathColors").boolValue)
            {
                colorList.Draw();
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("connected"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("loops"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("acyclic"));
            if (serializedObject.FindProperty("hasPathColors").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("prioritize"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("parity"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
