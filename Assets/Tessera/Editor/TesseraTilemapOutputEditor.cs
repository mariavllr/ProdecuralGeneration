using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace Tessera
{
    [CustomEditor(typeof(TesseraTilemapOutput))]
    class TesseraTilemapOutputEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var tilemapOutput = (TesseraTilemapOutput)target;
            var generator = tilemapOutput.GetComponent<TesseraGenerator>();
            var tilemap = tilemapOutput.tilemap;

            if (tilemap != null)
            {

                void Warn(string axis, int size, float tileSize, float cellSize, float cellGap)
                {
                    if (size > 1 && !FuzzyEquals(tileSize, cellSize + cellGap))
                    {
                        EditorGUILayout.HelpBox($"The cellSize of the generator and tilemap do not match on axis {axis}: {tileSize} versus {cellSize}{(cellGap > 0 ? " + " + cellGap : "")}", MessageType.Warning);
                    }
                }

                Warn("X", generator.size.x, generator.tileSize.x, tilemap.cellSize.x, tilemap.cellGap.x);
                Warn("Y", generator.size.y, generator.tileSize.y, tilemap.cellSize.y, tilemap.cellGap.y);
                Warn("Z", generator.size.z, generator.tileSize.z, tilemap.cellSize.z, tilemap.cellGap.z);
            }
        }

        private static bool FuzzyEquals(float x, float y)
        {
            return Math.Abs(x-y) < 1e-6;
        }
    }
}
