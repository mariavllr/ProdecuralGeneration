using UnityEditor;

namespace Tessera
{
    [CustomEditor(typeof(SeparationConstraint))]
    [CanEditMultipleObjects]
    public class SeparationConstraintEditor : Editor
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minDistance"));
            serializedObject.ApplyModifiedProperties();
        }

        public void TileList(SerializedProperty list)
        {
        }
    }
}
