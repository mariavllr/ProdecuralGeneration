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
    [CustomEditor(typeof(TesseraMultipassGenerator))]
    class MultipassGeneratorEditor : Editor
    {
        private SerializedProperty list;
        private ReorderableList rl;

        const int k_fieldPadding = 2;
        const int k_elementPadding = 5;

        private void OnEnable()
        {
            list = serializedObject.FindProperty("passes");

            rl = new ReorderableList(serializedObject, list, true, false, true, true);

            rl.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {

                SerializedProperty targetElement = list.GetArrayElementAtIndex(index);
                if (targetElement.hasVisibleChildren)
                    rect.xMin += 10;
                var passTypeProperty = targetElement.FindPropertyRelative("passType");

                var passTypeRect = rect;
                passTypeRect.height = EditorGUI.GetPropertyHeight(passTypeProperty);
                EditorGUI.PropertyField(passTypeRect, passTypeProperty);

                var passType = (TesseraMultipassPassType)passTypeProperty.enumValueIndex;
                switch(passType)
                {
                    case TesseraMultipassPassType.Generator:
                        {
                            var generatorProperty = targetElement.FindPropertyRelative("generator");
                            var generatorRect = rect;
                            generatorRect.yMin = passTypeRect.yMax + k_fieldPadding;
                            generatorRect.height = EditorGUI.GetPropertyHeight(generatorProperty);
                            EditorGUI.PropertyField(generatorRect, generatorProperty);
                        }
                        break;
                    case TesseraMultipassPassType.Event:
                        {
                            var generateEventProperty = targetElement.FindPropertyRelative("generateEvent");
                            var generateEventRect = rect;
                            generateEventRect.yMin = passTypeRect.yMax + k_fieldPadding;
                            generateEventRect.height = EditorGUI.GetPropertyHeight(generateEventProperty);
                            EditorGUI.PropertyField(generateEventRect, generateEventProperty);
                            var clearEventProperty = targetElement.FindPropertyRelative("clearEvent");
                            var clearEventRect = rect;
                            clearEventRect.yMin = generateEventRect.yMax + k_fieldPadding;
                            clearEventRect.height = EditorGUI.GetPropertyHeight(clearEventProperty);
                            EditorGUI.PropertyField(clearEventRect, clearEventProperty);

                        }
                        break;
                }
            };



            rl.elementHeightCallback = (int index) =>
            {
                SerializedProperty targetElement = list.GetArrayElementAtIndex(index);
                var rect = new Rect();
                var passTypeProperty = targetElement.FindPropertyRelative("passType");

                var passTypeRect = rect;
                passTypeRect.height = EditorGUI.GetPropertyHeight(passTypeProperty);

                var passType = (TesseraMultipassPassType)passTypeProperty.enumValueIndex;
                switch (passType)
                {
                    case TesseraMultipassPassType.Generator:
                        {
                            var generatorProperty = targetElement.FindPropertyRelative("generator");
                            var generatorRect = rect;
                            generatorRect.yMin = passTypeRect.yMax + k_fieldPadding;
                            generatorRect.height = EditorGUI.GetPropertyHeight(generatorProperty);

                            return generatorRect.yMax + k_elementPadding;
                        }
                    case TesseraMultipassPassType.Event:
                        {
                            var generateEventProperty = targetElement.FindPropertyRelative("generateEvent");
                            var generateEventRect = rect;
                            generateEventRect.yMin = passTypeRect.yMax + k_fieldPadding;
                            generateEventRect.height = EditorGUI.GetPropertyHeight(generateEventProperty);
                            var clearEventProperty = targetElement.FindPropertyRelative("clearEvent");
                            var clearEventRect = rect;
                            clearEventRect.yMin = generateEventRect.yMax + k_fieldPadding;
                            clearEventRect.height = EditorGUI.GetPropertyHeight(clearEventProperty);
                            return clearEventRect.yMax + k_elementPadding;
                        }
                    default:
                        return passTypeRect.yMax + k_elementPadding;
                }
            };
        }

        public override void OnInspectorGUI()
        {
            var t = target as TesseraMultipassGenerator;

            //EditorGUILayout.PropertyField(serializedObject.FindProperty("passes"));

            rl.DoLayoutList();

            if (GUILayout.Button("Clear"))
            {
                t.Clear();
            }

            if (GUILayout.Button("Regenerate"))
            {
                t.Clear();
                t.Generate();
            }

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
