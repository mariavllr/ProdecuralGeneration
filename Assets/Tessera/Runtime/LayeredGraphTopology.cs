using DeBroglie.Topo;

namespace Tessera
{
    // Identifies a cell (x, y, z), with face x on submesh z, at layer y.
    internal class SubMeshTopology : ITopology
    {
        private readonly int indexCount;
        private readonly int directionsCount;
        private readonly bool[] mask;

        // By index, direction
        private readonly NeighbourDetails[,] neighbours;
        private readonly int maxFaceCount;
        private readonly int subMeshCount;

        public SubMeshTopology(NeighbourDetails[,] neighbours, int maxFaceCount, int height, int subMeshCount, bool[] mask = null)
        {
            indexCount = neighbours.GetLength(0);
            directionsCount = neighbours.GetLength(1);
            this.neighbours = neighbours;
            this.maxFaceCount = maxFaceCount;
            Height = height;
            this.subMeshCount = subMeshCount;
            this.mask = mask;
        }

        public int IndexCount => indexCount;

        public int DirectionsCount => directionsCount;

        public int Width => maxFaceCount;

        public int Height { get; set; }

        public int Depth => subMeshCount;

        public bool[] Mask => mask;

        public SubMeshTopology WithMask(bool[] mask)
        {
            return new SubMeshTopology(neighbours, maxFaceCount, Height, subMeshCount, mask);
        }

        ITopology ITopology.WithMask(bool[] mask)
        {
            return WithMask(mask);
        }

        public void GetCoord(int index, out int x, out int y, out int z)
        {
            x = index % maxFaceCount;
            y = (index / maxFaceCount) % Height;
            z = index / maxFaceCount / Height;
        }

        public int GetIndex(int x, int y, int z)
        {
            return x + y * maxFaceCount + z * maxFaceCount * Height;
        }

        public bool TryMove(int index, Direction direction, out int dest, out Direction inverseDirection, out EdgeLabel edgeLabel)
        {
            var neighbour = neighbours[index, (int)direction];
            dest = neighbour.Index;
            inverseDirection = neighbour.InverseDirection;
            edgeLabel = neighbour.EdgeLabel;
            return neighbour.Index >= 0;
        }

        public bool TryMove(int index, Direction direction, out int dest)
        {
            var neighbour = neighbours[index, (int)direction];
            dest = neighbour.Index;
            return neighbour.Index >= 0;
        }

        public bool TryMove(int x, int y, int z, Direction direction, out int dest)
        {
            var index = GetIndex(x, y, z);
            var neighbour = neighbours[index, (int)direction];
            dest = neighbour.Index;
            return neighbour.Index >= 0;
        }

        public bool TryMove(int x, int y, int z, Direction direction, out int dest, out Direction inverseDirection, out EdgeLabel edgeLabel)
        {
            var index = GetIndex(x, y, z);
            var neighbour = neighbours[index, (int)direction];
            dest = neighbour.Index;
            inverseDirection = neighbour.InverseDirection;
            edgeLabel = neighbour.EdgeLabel;
            return neighbour.Index >= 0;
        }

        public bool TryMove(int x, int y, int z, Direction direction, out int destx, out int desty, out int destz)
        {
            var index = GetIndex(x, y, z);
            var neighbour = neighbours[index, (int)direction];
            destx = neighbour.Index;
            desty = 0;
            destz = 0;
            return neighbour.Index >= 0;
        }

        /// <summary>
        /// Describes a single neighbour of a node.
        /// (also called a half-edge in some literature).
        /// </summary>
        public struct NeighbourDetails
        {
            /// <summary>
            /// Where this edge leads to.
            /// Set to -1 to indicate no neighbour.
            /// </summary>
            public int Index { get; set; }

            /// <summary>
            /// The edge label of this edge
            /// </summary>
            public EdgeLabel EdgeLabel { get; set; }

            /// <summary>
            /// The direction to move from Index which will return back along this edge.
            /// </summary>
            public Direction InverseDirection { get; set; }
        }
    }
}
