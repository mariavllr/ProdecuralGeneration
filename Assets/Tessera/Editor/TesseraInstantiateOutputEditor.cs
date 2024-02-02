using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Tessera
{
    [CustomEditor(typeof(TesseraInstantiateOutput))]
    class TesseraInstantiateOutputEditor : Editor
    {
        private SerializedProperty list;
        private ReorderableList rl;

        const int k_fieldPadding = 2;
        const int k_elementPadding = 5;
        private void OnEnable()
        {
            list = serializedObject.FindProperty("tileMappings");

            rl = new ReorderableList(serializedObject, list, true, false, true, true);

            rl.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {

                SerializedProperty targetElement = list.GetArrayElementAtIndex(index);
                if (targetElement.hasVisibleChildren)
                    rect.xMin += 10;

                var fromProperty = targetElement.FindPropertyRelative("from");
                var fromRect = rect;
                fromRect.height = EditorGUI.GetPropertyHeight(fromProperty);
                EditorGUI.PropertyField(fromRect, fromProperty);

                var toProperty = targetElement.FindPropertyRelative("to");
                var toRect = rect;
                toRect.yMin = fromRect.yMax + k_fieldPadding;
                toRect.height = EditorGUI.GetPropertyHeight(toProperty);
                EditorGUI.PropertyField(toRect, toProperty);

                var instantiateChildrenOnlyProperty = targetElement.FindPropertyRelative("instantiateChildrenOnly");
                var instantiateChildrenOnlyRect = rect;
                instantiateChildrenOnlyRect.yMin = toRect.yMax + k_fieldPadding;
                instantiateChildrenOnlyRect.height = EditorGUI.GetPropertyHeight(instantiateChildrenOnlyProperty);
                EditorGUI.PropertyField(instantiateChildrenOnlyRect, instantiateChildrenOnlyProperty);
            };

            {
                var rect = new Rect();

                var fromRect = rect;
                fromRect.height = EditorGUI.GetPropertyHeight(SerializedPropertyType.ObjectReference, GUIContent.none);

                var toRect = rect;
                toRect.yMin = fromRect.yMax + k_fieldPadding;
                toRect.height = EditorGUI.GetPropertyHeight(SerializedPropertyType.ObjectReference, GUIContent.none);

                var instantiateChildrenOnlyRect = rect;
                instantiateChildrenOnlyRect.yMin = toRect.yMax + k_fieldPadding;
                instantiateChildrenOnlyRect.height = EditorGUI.GetPropertyHeight(SerializedPropertyType.Boolean, GUIContent.none);

                rl.elementHeight = instantiateChildrenOnlyRect.yMax + k_elementPadding;
            }

        }


        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("parent"));

            list.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(list.isExpanded, new GUIContent("Tile Mappings"));

            if (list.isExpanded)
            {
                rl.DoLayoutList();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete && rl.index >= 0)
            {
                list.DeleteArrayElementAtIndex(rl.index);
                if (rl.index >= list.arraySize - 1)
                {
                    rl.index = list.arraySize - 1;
                }
            }
        }
    }
}
