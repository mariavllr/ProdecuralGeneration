using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    public enum TileListDisplayType
    {
        SingleTile,
        Checkboxes,
        List,
    }

    /// <summary>
    /// Utility for displaying a list of TesseraTile in the Unity editor
    /// </summary>
    public class TileListUtil
    {
        public TesseraGenerator generator;
        private readonly string title;
        public SerializedProperty list;
        private readonly bool allowSingleTile;
        private readonly bool allowCheckboxes;
        private readonly bool allowList;
        public TileListDisplayType displayType;

        public TileListUtil(TesseraGenerator generator, string title, SerializedProperty list, bool allowSingleTile = true, bool allowCheckboxes = true, bool allowList = true)
        {
            this.generator = generator;
            this.title = title;
            this.list = list;
            this.allowSingleTile = allowSingleTile;
            this.allowCheckboxes = allowCheckboxes;
            this.allowList = allowList;
            // Pick display type
            if (SingleTileEnabled)
            {
                displayType = TileListDisplayType.SingleTile;
            }
            else if (CheckboxesEnabled)
            {
                displayType = TileListDisplayType.Checkboxes;
            }
            else
            {
                displayType = TileListDisplayType.List;
            }
        }

        public bool SingleTileEnabled => allowSingleTile && list.arraySize <= 1;

        public bool CheckboxesEnabled
        {
            get
            {
                if (!allowCheckboxes) return false;
                var tiles = new HashSet<TesseraTileBase>(generator.tiles.Select(x => x.tile));
                for (int i = 0; i < list.arraySize; i++)
                {
                    var tile = (TesseraTileBase)list.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (tile != null && !tiles.Contains(tile))
                        return false;
                }
                return true;
            }
        }

        public bool ListEnabled => allowList || (!CheckboxesEnabled && !SingleTileEnabled);

        public bool Enabled(System.Enum e)
        {
            var t = (TileListDisplayType)e;
            switch (t)
            {
                case TileListDisplayType.SingleTile:
                    return SingleTileEnabled;
                case TileListDisplayType.Checkboxes:
                    return CheckboxesEnabled;
                case TileListDisplayType.List:
                    return true;
            }
            throw new System.Exception();
        }

        public void Draw()
        {
            var set = new HashSet<TesseraTileBase>();
            for (int i = 0; i < list.arraySize; i++)
            {
                set.Add((TesseraTileBase)list.GetArrayElementAtIndex(i).objectReferenceValue);
            }

            var t = title;
            if (displayType == TileListDisplayType.SingleTile)
            {
                t = t.TrimEnd('s');
            }
            list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, list.displayName);

            if (list.isExpanded)
            {
                int oldIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel++;

                var optionStrings = new List<string>();
                var optionValues = new List<TileListDisplayType>();
                int selectedIndex = -1;
                void Add(string s, TileListDisplayType value)
                {
                    if (displayType == value)
                        selectedIndex = optionValues.Count;
                    optionStrings.Add(s);
                    optionValues.Add(value);
                }
                if (SingleTileEnabled) Add("Single Tile", TileListDisplayType.SingleTile);
                if (CheckboxesEnabled) Add("Checkboxes", TileListDisplayType.Checkboxes);
                if (ListEnabled) Add("List", TileListDisplayType.List);

                //displayType = (TileListDisplayType)EditorGUILayout.EnumPopup(new GUIContent(), displayType, Enabled, true);
                if (optionStrings.Count <= 1)
                {
                    selectedIndex = 0;
                }
                else
                {
                    selectedIndex = EditorGUILayout.Popup(selectedIndex, optionStrings.ToArray());
                }

                displayType = optionValues[selectedIndex];

                switch (displayType)
                {
                    case TileListDisplayType.SingleTile:
                        {
                            var current = list.arraySize == 0 ? null : list.GetArrayElementAtIndex(0).objectReferenceValue;
                            var next = EditorGUILayout.ObjectField(current, typeof(TesseraTileBase), true);
                            if (next == null)
                            {
                                list.arraySize = 0;
                            }
                            else
                            {
                                list.arraySize = 1;
                                list.GetArrayElementAtIndex(0).objectReferenceValue = next;
                            }
                            break;
                        }
                    case TileListDisplayType.Checkboxes:
                        foreach (var tileEntry in generator.tiles)
                        {
                            var tile = tileEntry.tile;
                            var current = set.Contains(tile);
                            if (tile == null)
                            {
                                continue;
                            }
                            var newValue = EditorGUILayout.Toggle(tile.name, current);
                            if (newValue && !current)
                            {
                                list.arraySize += 1;
                                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = tile;
                            }
                            if (!newValue && current)
                            {
                                for (int i = 0; i < list.arraySize; i++)
                                {
                                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == tile)
                                    {
                                        // https://answers.unity.com/questions/555724/serializedpropertydeletearrayelementatindex-leaves.html
                                        list.GetArrayElementAtIndex(i).objectReferenceValue = null;
                                        list.DeleteArrayElementAtIndex(i);
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    case TileListDisplayType.List:
                        EditorGUILayout.PropertyField(list.FindPropertyRelative("Array.size"));
                        for (int i = 0; i < list.arraySize; i++)
                        {
                            EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i));
                        }
                        break;

                }

                EditorGUI.indentLevel = oldIndentLevel;
            }
        }
    }
}
