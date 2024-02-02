using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tessera
{
    internal static class MirrorUtils
    {
        // TODO: Don't use CellFaceDir as the axis enum
        public interface IMirrorOps
        {
            // Reflect a grid cell
            // This uses the grid bounds, it doesn't reflect over a fixed point.
            bool ReflectIndex(CellRotation rotation, IGrid grid, ITopology topology, int i, out int i2);
        }

        private class CubeMirrorOps : IMirrorOps
        {
            public bool ReflectIndex(CellRotation rotation, IGrid grid, ITopology topology, int i, out int i2)
            {
                topology.GetCoord(i, out var x, out var y, out var z);
                if (rotation == CubeRotation.ReflectX)
                {
                    x = topology.Width - 1 - x;
                }
                else if (rotation == CubeRotation.ReflectY)
                {
                    y = topology.Height - 1 - y;
                }
                else if (rotation == CubeRotation.ReflectZ)
                {
                    z = topology.Depth - 1 - z;
                }
                else
                {
                    throw new Exception();
                }
                i2 = topology.GetIndex(x, y, z);
                return topology.ContainsIndex(i2);
            }
        }

        private class SquareMirrorOps : IMirrorOps
        {
            public bool ReflectIndex(CellRotation rotation, IGrid grid, ITopology topology, int i, out int i2)
            {
                topology.GetCoord(i, out var x, out var y, out var z);
                if (rotation == SquareRotation.ReflectX)
                {
                    x = topology.Width - 1 - x;
                }
                else if (rotation == SquareRotation.ReflectY)
                {
                    y = topology.Height - 1 - y;
                }
                else
                {
                    throw new Exception();
                }
                i2 = topology.GetIndex(x, y, z);
                return topology.ContainsIndex(i2);
            }
        }

        // Always reflects in x axis for now
        private class TrianglePrismMirrorOps : IMirrorOps
        {
            public bool ReflectIndex(CellRotation rotation, IGrid grid, ITopology topology, int i, out int i2)
            {
                if (rotation != TriangleRotation.ReflectX)
                    throw new Exception();

                topology.GetCoord(i, out var x, out var y, out var z);

                var q = x + z;
                var min = 0;
                var max = (topology.Width - 1) + (topology.Depth - 1);
                var q2 = min + max - q;
                x += Mathf.FloorToInt((q2 - q));

                if (x < 0 || x >= topology.Width || z < 0 || z >= topology.Depth)
                {
                    i2 = default;
                    return false;
                }
                i2 = topology.GetIndex(x, y, z);
                return topology.ContainsIndex(i2);
            }
        }

        private class HexPrismMirrorOps : IMirrorOps
        {
            public bool ReflectIndex(CellRotation rotation, IGrid grid, ITopology topology, int i, out int i2)
            {
                topology.GetCoord(i, out var x, out var y, out var z);
                if (rotation == HexRotation.ReflectX)
                {
                    var q = x * 2 - z;
                    var min = 0 - topology.Depth - 1;
                    var max = (topology.Width - 1) * 2;
                    var q2 = min + max - q;
                    x += Mathf.FloorToInt((q2 - q) / 2f);
                }
                else if (rotation == HexRotation.ReflectForwardLeft)
                {
                    var q = 2 * z - x;
                    var min = 0 - (topology.Width - 1);
                    var max = 2 * (topology.Depth - 1) - 0;
                    var q2 = min + max - q;
                    z += Mathf.FloorToInt((q2 - q) / 2f);
                }
                else if (rotation == HexRotation.ReflectForwardRight)
                {
                    var q = z + x;
                    var min = 0;
                    var max = (topology.Depth - 1) + (topology.Width - 1);
                    var q2 = min + max - q;
                    x += Mathf.FloorToInt((q2 - q) / 2f);
                    z += Mathf.FloorToInt((q2 - q) / 2f);
                }
                else
                {
                    throw new Exception();
                }
                if (x < 0 || x >= topology.Width || z < 0 || z >= topology.Depth)
                {
                    i2 = default;
                    return false;
                }
                i2 = topology.GetIndex(x, y, z);
                return topology.ContainsIndex(i2);
            }
        }


        public static IMirrorOps GetMirrorOps(ICellType cellType)
        {
            if(cellType is CubeCellType)
            {
                return new CubeMirrorOps();
            }
            else if (cellType is SquareCellType)
            {
                return new SquareMirrorOps();
            }
            else if (cellType is HexPrismCellType)
            {
                return new HexPrismMirrorOps();
            }
            else if (cellType is TrianglePrismCellType)
            {
                return new TrianglePrismMirrorOps();
            }
            else
            {
                throw new Exception($"No MirrorOps for {cellType.GetType()}");
            }
        }
    }
}
