using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tessera
{
    // Implementations for various ICellType methods.
    // These are generally not efficient, so interfaces can specialize them if needs be.
    public static class DefaultCellTypeImpl
    {
        public static bool TryMoveByOffset(ICellType cellType, Vector3Int startCell, Vector3Int startOffset, Vector3Int destOffset, CellRotation rotation, out Vector3Int destCell)
        {
            // Shortcut
            if (startOffset == destOffset)
            {
                destCell = startCell;
                return true;
            }

            var cell = startCell;
            foreach (var stepDir in cellType.FindPath(startOffset, destOffset))
            {
                var dir = cellType.Rotate(stepDir, rotation);
                if (!cellType.TryMove(cell, dir, out cell))
                {
                    destCell = default;
                    return false;
                }
            }
            destCell = cell;
            return true;
        }

        public static IDictionary<Vector3Int, Vector3Int> Realign(ICellType cellType, ISet<Vector3Int> shape, CellRotation rotation)
        {
            var shapeList = shape.ToList();
            var start = shapeList[0];
            // Try every possible re-alignment
            foreach(var realignedStart in shapeList)
            {
                var realignedDict = new Dictionary<Vector3Int, Vector3Int>();
                // Attempt to realign all offsets with the given start, using transport
                var failed = false;
                foreach (var offset in shapeList)
                {
                    if(!cellType.TryMoveByOffset(realignedStart, start, offset, rotation, out var realignedOffset))
                        failed = true;
                    if (!shape.Contains(realignedOffset))
                        failed = true;
                    if(failed)
                    {
                        break;
                    }
                    realignedDict[offset] = realignedOffset;
                }
                if(failed)
                    continue;
                return realignedDict;
            }
            // Every possible re-alignment failed.
            return null;
        }

    }
}
