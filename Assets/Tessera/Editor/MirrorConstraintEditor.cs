using System;
using System.Text;
using System.Threading.Tasks;
using Tessera;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    [CustomEditor(typeof(MirrorConstraint))]
    public class MirrorConstraintEditor : Editor
    {
        public void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
            var generator = ((TesseraConstraint)target).GetComponent<TesseraGenerator>();
            var cellType = generator.CellType;
            serializedObject.Update();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Mirror", "The axis to mirror along"));
            var axisProperty = serializedObject.FindProperty("axis");
            if (cellType is CubeCellType)
            {
                EditorGUI.BeginChangeCheck();
                axisProperty.enumValueIndex = EditorGUILayout.IntPopup(axisProperty.enumValueIndex, new[] { "X", "Y", "Z" }, new[] { (int)MirrorConstraint.Axis.X, (int)MirrorConstraint.Axis.Y, (int)MirrorConstraint.Axis.Z });
            }
            else if (cellType is SquareCellType)
            {
                EditorGUI.BeginChangeCheck();
                axisProperty.enumValueIndex = EditorGUILayout.IntPopup(axisProperty.enumValueIndex, new[] { "X", "Y" }, new[] { (int)MirrorConstraint.Axis.X, (int)MirrorConstraint.Axis.Y });
            }
            else if (cellType is HexPrismCellType)
            {
                axisProperty.enumValueIndex = EditorGUILayout.IntPopup(axisProperty.enumValueIndex, new[] { "X", "Z - x", "Z + x" }, new[] { (int)MirrorConstraint.Axis.X, (int)MirrorConstraint.Axis.Z, (int)MirrorConstraint.Axis.W });
            }
            else if (cellType is TrianglePrismCellType)
            {
                axisProperty.enumValueIndex = EditorGUILayout.IntPopup(axisProperty.enumValueIndex, new[] { "X" }, new[] { (int)MirrorConstraint.Axis.X });
            }
            else
            {
                throw new Exception();
            }
            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
