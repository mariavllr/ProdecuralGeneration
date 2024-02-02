using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    [CustomEditor(typeof(TesseraTileBase), true)]
    [CanEditMultipleObjects]
    public class TesseraTileBaseEditor : Editor
    {
        void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var tileBase = (TesseraTileBase)target;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("palette"));
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("faceDetails"));
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("offsets"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("center"));
            if (tileBase.CellType is HexPrismCellType)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tileSize").FindPropertyRelative("x"), new GUIContent("Hex Tile Size"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tileSize").FindPropertyRelative("y"), new GUIContent("Tile Height"));
            }
            else if (tileBase.CellType is TrianglePrismCellType)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tileSize").FindPropertyRelative("x"), new GUIContent("Triangle Tile Size"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tileSize").FindPropertyRelative("y"), new GUIContent("Tile Height"));
            }
            else
            if (tileBase.CellType is SquareCellType)
            {
                Vector2Field("Tile Size", serializedObject.FindProperty("tileSize"));
            }
            else if(tileBase.CellType is CubeCellType)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tileSize"));
            }
            else
            {
                throw new System.Exception();
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rotatable"));
            if (serializedObject.targetObjects.Cast<TesseraTileBase>().Any(t => t.rotatable) && tileBase.CellType is CubeCellType)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationGroupType"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("reflectable"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("symmetric"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("instantiateChildrenOnly"));

            var meshFilter = tileBase.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                if (meshFilter.sharedMesh.hideFlags == HideFlags.HideAndDontSave)
                {
                    if (TesseraGeneratorEditor.HelpBoxWithButton("Mesh is flagged as HideAndDontSave", "Save it", MessageType.Info))
                    {
                        foreach(var t in targets)
                        {
                            foreach (var mf in ((TesseraTileBase)t).GetComponents<MeshFilter>())
                            {
                                meshFilter.sharedMesh.hideFlags = HideFlags.None;
                            }
                        }
                    }
                }
            }


            serializedObject.ApplyModifiedProperties();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Paint");
            EditorGUILayout.EditorToolbar(TesseraTileEditorToolBase.tile3dEditorTools);
            GUILayout.EndHorizontal();
        }

        private void Vector2Field(string label, SerializedProperty property)
        {
            var v3 = property.vector3Value;
            var v2 = new Vector2(v3.x, v3.y);
            v2 = EditorGUILayout.Vector2Field(label, v2);
            property.vector3Value = new Vector3(v2.x, v2.y, 0);
        }
    }
}