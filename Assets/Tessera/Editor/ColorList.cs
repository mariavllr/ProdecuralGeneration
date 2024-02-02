using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Utility for displaying a list of paint colors in the Unity editor
    /// </summary>
    public class ColorList
    {
        private readonly SerializedProperty list;
        private readonly TesseraPalette palette;

        public ColorList(SerializedProperty list, TesseraPalette palette)
        {
            this.list = list;
            this.palette = palette;
        }


        public void Draw()
        {
            var set = new HashSet<int>();
            for (int i = 0; i < list.arraySize; i++)
            {
                set.Add((int)list.GetArrayElementAtIndex(i).intValue);
            }

            list.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(list.isExpanded, list.displayName);

            if (list.isExpanded)
            {
                int oldIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel++;

                for (var index = 0; index < palette.entryCount; index++)
                {
                    var entry = palette.entries[index];
                    var current = set.Contains(index);
                    var texture = TextureUtil.MakeTexture(1, 1, entry.color);

                    GUILayout.BeginHorizontal();
                    var labelStyle = new GUIStyle(EditorStyles.label);
                    labelStyle.normal.background = texture;
                    labelStyle.normal.textColor = TextureUtil.GetContrastColor(entry.color);
                    labelStyle.hover.background = texture;
                    labelStyle.hover.textColor = TextureUtil.GetContrastColor(entry.color);
                    labelStyle.focused.background = texture;
                    labelStyle.focused.textColor = TextureUtil.GetContrastColor(entry.color);

                    EditorGUILayout.PrefixLabel(new GUIContent(entry.name, entry.name), labelStyle, labelStyle);

                    var newValue = EditorGUILayout.Toggle(current);

                    GUILayout.EndHorizontal();
                    if (newValue && !current)
                    {
                        list.arraySize += 1;
                        list.GetArrayElementAtIndex(list.arraySize - 1).intValue = index;
                    }
                    if (!newValue && current)
                    {
                        for (int i = 0; i < list.arraySize; i++)
                        {
                            if (list.GetArrayElementAtIndex(i).intValue == index)
                            {
                                list.DeleteArrayElementAtIndex(i);
                                break;
                            }
                        }
                    }
                }

                EditorGUI.indentLevel = oldIndentLevel;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
