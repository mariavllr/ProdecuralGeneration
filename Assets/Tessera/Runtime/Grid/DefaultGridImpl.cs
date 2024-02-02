using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tessera
{
    public static class DefaultGridImpl
    {
        public static bool TryMoveByOffset(IGrid grid, Vector3Int startCell, Vector3Int startOffset, Vector3Int destOffset, CellRotation startRotation, out Vector3Int destCell, out CellRotation destRotation)
        {
            // Shortcut
            if (startOffset == destOffset)
            {
                destCell = startCell;
                destRotation = startRotation;
                return true;
            }

            var cellType = grid.CellType;

            var cell = startCell;
            var rotation = startRotation;
            foreach (var stepDir in cellType.FindPath(startOffset, destOffset))
            {
                var dir = cellType.Rotate(stepDir, rotation);
                if (!grid.TryMove(cell, dir, out cell, out var _, out var edgeRotation))
                {
                    destCell = default;
                    destRotation = default;
                    return false;
                }
                rotation = cellType.Multiply(edgeRotation, rotation);
            }
            destCell = cell;
            destRotation = rotation;
            return true;
        }

    }
}
