using DeBroglie.Rot;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Records the painted colors for a single face of one cube in a <see cref="TesseraTile"/>
    /// </summary>
    [Serializable]
    public class FaceDetails
    {
        public int topLeft;
        public int top;
        public int topRight;
        public int left;
        public int center;
        public int right;
        public int bottomLeft;
        public int bottom;
        public int bottomRight;

        // For use with hexes
        public int hexRight { get { return right; } set { right = value; } }
        public int hexRightAndTopRight;
        public int hexTopRight { get { return topRight; } set { topRight = value; } }
        public int hexTopRightAndTopLeft { get { return top; } set { top = value; } }
        public int hexTopLeft { get { return topLeft; } set { topLeft = value; } }
        public int hexTopLeftAndLeft;
        public int hexLeft { get { return left; } set { left = value; } }
        public int hexLeftAndBottomLeft;
        public int hexBottomLeft { get { return bottomLeft; } set { bottomLeft = value; } }
        public int hexBottomLeftAndBottomRight { get { return bottom; } set { bottom = value; } }
        public int hexBottomRight { get { return bottomRight; } set { bottomRight = value; } }
        public int hexBottomRightAndRight;
        public int hexCenter { get { return center; } set { center = value; } }

        // TODO: These mutating methods should not be needed publically
        internal FaceDetails Clone()
        {
            return (FaceDetails)MemberwiseClone();
        }

        internal void ReflectX()
        {
            (topLeft, topRight) = (topRight, topLeft);
            (left, right) = (right, left);
            (bottomLeft, bottomRight) = (bottomRight, bottomLeft);
        }

        internal void RotateCw()
        {
            (topRight, bottomRight, bottomLeft, topLeft) = (topLeft, topRight, bottomRight, bottomLeft);
            (right, bottom, left, top) = (top, right, bottom, left);
        }

        internal void RotateCcw()
        {
            (topLeft, topRight, bottomRight, bottomLeft) = (topRight, bottomRight, bottomLeft, topLeft);
            (top, right, bottom, left) = (right, bottom, left, top);
        }

        internal void HexReflectX()
        {
            (hexRight, hexLeft) = (hexLeft, hexRight);
            (hexRightAndTopRight, hexTopLeftAndLeft) = (hexTopLeftAndLeft, hexRightAndTopRight);
            (hexTopRight, hexTopLeft) = (hexTopLeft, hexTopRight);
            (hexLeftAndBottomLeft, hexBottomRightAndRight) = (hexBottomRightAndRight, hexLeftAndBottomLeft);
            (hexBottomLeft, hexBottomRight) = (hexBottomRight, hexBottomLeft);
        }

        internal void HexRotateCw()
        {
            (hexTopRight, hexTopLeft, hexLeft, hexBottomLeft, hexBottomRight, hexRight) = (hexRight, hexTopRight, hexTopLeft, hexLeft, hexBottomLeft, hexBottomRight);
            (hexTopRightAndTopLeft, hexTopLeftAndLeft, hexLeftAndBottomLeft, hexBottomLeftAndBottomRight, hexBottomRightAndRight, hexRightAndTopRight) = (hexRightAndTopRight, hexTopRightAndTopLeft, hexTopLeftAndLeft, hexLeftAndBottomLeft, hexBottomLeftAndBottomRight, hexBottomRightAndRight);
        }

        internal void HexRotateCcw()
        {
            (hexRight, hexTopRight, hexTopLeft, hexLeft, hexBottomLeft, hexBottomRight) = (hexTopRight, hexTopLeft, hexLeft, hexBottomLeft, hexBottomRight, hexRight);
            (hexRightAndTopRight, hexTopRightAndTopLeft, hexTopLeftAndLeft, hexLeftAndBottomLeft, hexBottomLeftAndBottomRight, hexBottomRightAndRight) = (hexTopRightAndTopLeft, hexTopLeftAndLeft, hexLeftAndBottomLeft, hexBottomLeftAndBottomRight, hexBottomRightAndRight, hexRightAndTopRight);
        }

        internal void TriangleReflectX()
        {
            (topLeft, topRight) = (topRight, topLeft);
            (bottomLeft, bottomRight) = (bottomRight, bottomLeft);
        }
        internal void TriangleRotateCcw()
        {
            (bottom, topRight, topLeft) = (topLeft, bottom, topRight);
            (top, bottomLeft, bottomRight) = (bottomRight, top, bottomLeft);
        }

        internal void TriangleRotateCcw60()
        {
            (bottomLeft, topLeft, top, topRight, bottomRight, bottom) = (topLeft, top, topRight, bottomRight, bottom, bottomLeft);
        }

        public override string ToString()
        {
            return $"({topLeft},{top},{topRight};{left},{center},{right};{bottomLeft},{bottom},{bottomRight})";
        }

        /// <summary>
        /// Checks if two FaceDetails have the same values.
        /// This is an exact match, with no reflection built in.
        /// See TesseraPalette.Match for a fuzzier match.
        /// </summary>
        public bool IsEquivalent(FaceDetails other)
        {
            return 
                topLeft == other.topLeft &&
                top == other.top &&
                topRight == other.topRight &&
                left == other.left &&
                center == other.center &&
                right == other.right &&
                bottomLeft == other.bottomLeft &&
                bottom == other.bottom &&
                bottomRight == other.bottomRight &&
                hexRightAndTopRight == other.hexRightAndTopRight &&
                hexTopLeftAndLeft == other.hexTopLeftAndLeft &&
                hexLeftAndBottomLeft == other.hexLeftAndBottomLeft &&
                hexBottomRightAndRight == other.hexBottomRightAndRight;
        }
    }
}