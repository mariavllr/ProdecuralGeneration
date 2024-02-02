using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    public class HexCellDrawingType : ICellDrawingType
    {
        public static HexCellDrawingType Instance = new HexCellDrawingType();

        public bool Is2D => false;

        // Distance from center to each vertex, assuming an InnerRadius of 1 (which corresponds to tileSize.x == 2)
        private const float OuterRadius = 1.15470053837925f; // 2 / sqrt(3)

        private bool IsUpDown(CellFaceDir dir) => ((HexPrismFaceDir)dir).IsUpDown();

        private Vector3 GetForward(CellFaceDir dir) => ((HexPrismFaceDir)dir).Forward();


        public void GetFaceCenterAndNormal(Vector3Int offset, CellFaceDir dir, Vector3 center, Vector3 tileSize, out Vector3 faceCenter, out Vector3 faceNormal)
        {
            faceNormal = GetForward(dir);
            faceCenter = HexGeometryUtils.GetCellCenter(offset, center, tileSize) + 0.5f * faceNormal * (IsUpDown(dir) ? tileSize.y : tileSize.x);
        }

        public Vector3[] GetFaceVertices(Vector3Int offset, CellFaceDir dir, Vector3 center, Vector3 tileSize)
        {
            var up = ((HexPrismFaceDir)dir).Up();
            GetFaceCenterAndNormal(offset, dir, center, tileSize, out var faceCenter, out var forward);
            var right = Vector3.Cross(forward, up);

            var innerRadius = 0.5f * tileSize.x;
            var outerRadius = OuterRadius * innerRadius;

            if(IsUpDown(dir))
            {
                var v1 = faceCenter - 0.5f * outerRadius * up + innerRadius * right;
                var v2 = faceCenter - outerRadius * up;
                var v3 = faceCenter - 0.5f * outerRadius * up - innerRadius * right;
                var v4 = faceCenter + 0.5f * outerRadius * up - innerRadius * right;
                var v5 = faceCenter + outerRadius * up;
                var v6 = faceCenter + 0.5f * outerRadius * up + innerRadius * right;
                return new[] { v1, v2, v3, v4, v5, v6 };
            }
            else
            {
                return SquareFaceDrawingUtils.GetFaceVertices(faceCenter, 0.5f * up * tileSize.y, 0.5f * outerRadius * right);
            }
        }

        public Vector3[] GetSubFaceVertices(Vector3Int offset, CellFaceDir dir, SubFace subface, Vector3 center, Vector3 tileSize)
        {
            var up = ((HexPrismFaceDir)dir).Up();
            GetFaceCenterAndNormal(offset, dir, center, tileSize, out var faceCenter, out var forward);
            var right = Vector3.Cross(forward, up);

            var innerRadius = 0.5f * tileSize.x;
            var outerRadius = OuterRadius * innerRadius;

            var t = 1 / 3.0f;


            if (IsUpDown(dir))
            {
                var upness = dir == (CellFaceDir)HexPrismFaceDir.Up ? 1 : -1;
                switch(subface)
                {
                    case SubFace.HexCenter:
                        {
                            var v1 = faceCenter - t * 0.5f * outerRadius * up + t * innerRadius * right;
                            var v2 = faceCenter - t * outerRadius * up;
                            var v3 = faceCenter - t * 0.5f * outerRadius * up - t * innerRadius * right;
                            var v4 = faceCenter + t * 0.5f * outerRadius * up - t * innerRadius * right;
                            var v5 = faceCenter + t * outerRadius * up;
                            var v6 = faceCenter + t * 0.5f * outerRadius * up + t * innerRadius * right;
                            return new[] { v1, v2, v3, v4, v5, v6 };
                        }
                    case SubFace.HexRight:
                    case SubFace.HexTopRight:
                    case SubFace.HexTopLeft:
                    case SubFace.HexLeft:
                    case SubFace.HexBottomLeft:
                    case SubFace.HexBottomRight:
                        {
                            var v1 = -t * 0.5f * outerRadius * up + t * innerRadius * right;
                            var v2 = +t * 0.5f * outerRadius * up + t * innerRadius * right;
                            var v3 = +t * 0.5f * outerRadius * up + innerRadius * right;
                            var v4 = -t * 0.5f * outerRadius * up + innerRadius * right;
                            var r = Quaternion.Euler(0, -upness * 30 * ( (int)subface - (int)SubFace.HexRight), 0);
                            return new[] {
                                faceCenter + r * v1,
                                faceCenter + r * v2,
                                faceCenter + r * v3,
                                faceCenter + r * v4,
                            };
                        }
                    case SubFace.HexRightAndTopRight:
                    case SubFace.HexTopRightAndTopLeft:
                    case SubFace.HexTopLeftAndLeft:
                    case SubFace.HexLeftAndBottomLeft:
                    case SubFace.HexBottomLeftAndBottomRight:
                    case SubFace.HexBottomRightAndRight:
                        {
                            var v1 = t * 0.5f * outerRadius * up + t * innerRadius * right;
                            var v2 = t * 0.5f * outerRadius * up + innerRadius * right;
                            var v3 = 0.5f * outerRadius * up + innerRadius * right;
                            var v4 = -t * 0.5f * outerRadius * up + innerRadius * right;

                            var r = Quaternion.Euler(0, -upness * 30 * ((int)subface - (int)SubFace.HexRightAndTopRight), 0);
                            var r2 = Quaternion.Euler(0, -upness * 30 * ( 2 + (int)subface - (int)SubFace.HexRightAndTopRight), 0);
                            return new[] {
                                faceCenter + r * v1,
                                faceCenter + r * v2,
                                faceCenter + r * v3,
                                faceCenter + r2 * v4,
                            };
                        }
                }
                throw new Exception();
            }
            else
            {
                return SquareFaceDrawingUtils.GetSubFaceVertices(subface, faceCenter, 0.5f * up * tileSize.y, 0.5f * outerRadius * right);
            }
        }

        private Vector2 Canonical2(SubFace subFace)
        {
            switch(subFace)
            {
                case SubFace.BottomLeft: return new Vector2(-1, -1);
                case SubFace.Bottom: return new Vector2(0, -1);
                case SubFace.BottomRight: return new Vector2(1, -1);
                case SubFace.Left: return new Vector2(-1, 0);
                case SubFace.Center: return new Vector2(0, 0);
                case SubFace.Right: return new Vector2(1, 0);
                case SubFace.TopLeft: return new Vector2(-1, 1);
                case SubFace.Top: return new Vector2(0, 1);
                case SubFace.TopRight: return new Vector2(1, 1);
                case SubFace.HexRight: return new Vector2(1, 0);
                case SubFace.HexRightAndTopRight: return new Vector2(1, OuterRadius * 0.5f);
                case SubFace.HexTopRight: return new Vector2(0.5f, OuterRadius * 0.75f);
                case SubFace.HexTopRightAndTopLeft: return new Vector2(0, OuterRadius);
                case SubFace.HexTopLeft: return new Vector2(-0.5f, OuterRadius * 0.75f);
                case SubFace.HexTopLeftAndLeft: return new Vector2(-1, OuterRadius * 0.5f);
                case SubFace.HexLeft: return new Vector2(-1, 0);
                case SubFace.HexLeftAndBottomLeft: return new Vector2(-1, -OuterRadius * 0.5f);
                case SubFace.HexBottomLeft: return new Vector2(-0.5f, -OuterRadius * 0.75f);
                case SubFace.HexBottomLeftAndBottomRight: return new Vector2(0, -OuterRadius);
                case SubFace.HexBottomRight: return new Vector2(0.5f, -OuterRadius * 0.75f);
                case SubFace.HexBottomRightAndRight: return new Vector2(1, -OuterRadius * 0.5f);
                case SubFace.HexCenter: return new Vector2(0, 0);
            }
            throw new Exception();
        }


        private Vector3 Canonical(Vector3Int offset, CellFaceDir faceDir, SubFace subFace, PaintMode paintMode)
        {
            return Canonical(offset, faceDir, subFace, paintMode, out var _, out var _2);
        }

        private Vector3 Canonical(Vector3Int offset, CellFaceDir faceDir, SubFace subFace, PaintMode paintMode, out Vector3 faceCenter, out Vector3 forward)
        {
            var c2 = Canonical2(subFace);
            var up = ((HexPrismFaceDir)faceDir).Up();
            GetFaceCenterAndNormal(offset, faceDir, Vector3.zero, new Vector3(2, 2, 0), out faceCenter, out forward);
            var right = Vector3.Cross(forward, up);
            var isUpDown = IsUpDown(faceDir);
            return faceCenter + c2.x * right * (isUpDown ? 1 : OuterRadius / 2) + c2.y * up;
        }

        public bool IsAffected(Vector3Int parentOffset, CellFaceDir parentFaceDir, SubFace parentSubface, Vector3Int childOffset, CellFaceDir childFaceDir, SubFace childSubface, PaintMode paintMode)
        {
            switch(paintMode)
            {
                case PaintMode.Pencil:
                    return parentOffset == childOffset && parentFaceDir == childFaceDir && parentSubface == childSubface;
                case PaintMode.Face:
                case PaintMode.Add:
                    return childOffset == parentOffset && childFaceDir == parentFaceDir;
                case PaintMode.Remove:
                    return childOffset == parentOffset;
                case PaintMode.Vertex:
                    return Canonical(parentOffset, parentFaceDir, parentSubface, paintMode) == Canonical(childOffset, childFaceDir, childSubface, paintMode);
                case PaintMode.Edge:
                    var v1 = Canonical(parentOffset, parentFaceDir, parentSubface, paintMode, out var faceCenter, out var forward);
                    var v2 = Canonical(childOffset, childFaceDir, childSubface, paintMode, out var childFaceCenter, out var childForward);

                    var d = v1 - v2;
                    var towardsEdge = v1 - faceCenter;
                    var alongEdge = Vector3.Cross(forward, towardsEdge);

                    return
                        // v2 must line on an infinite line defined by the edge
                        Math.Abs(Vector3.Dot(d, towardsEdge)) < 1e-6 &&
                        Math.Abs(Vector3.Dot(d, forward)) < 1e-6 &&
                        // And it must be on a face that borders the edge
                        Math.Abs(Vector3.Dot(childForward, alongEdge)) < 1e-6 &&
                        Math.Abs(Vector3.Dot(faceCenter - childFaceCenter, alongEdge)) < 1e-6;
            }
            throw new Exception();
        }

        public RaycastCellHit Raycast(Ray ray, Vector3Int offset, Vector3 center, Vector3 tileSize, float? minDistance, float? maxDistance)
        {
            var bestT1 = 0f;
            var bestT2 = float.PositiveInfinity;
            CellFaceDir? bestFace = null;

            void TestFace(HexPrismFaceDir faceDir)
            {
                GetFaceCenterAndNormal(offset, (CellFaceDir)(faceDir), center, tileSize, out var faceCenter, out var forward);
                var d1 = Vector3.Dot(faceCenter - ray.origin, forward);
                var d2 = Vector3.Dot(ray.direction, forward);
                var t = d1 / d2;
                if (d2 < 0)
                {
                    if (t > bestT1)
                    {
                        bestT1 = t;
                        bestFace = (CellFaceDir)(faceDir);
                    }
                }
                else
                {
                    if(t < bestT2)
                    {
                        bestT2 = t;
                    }
                }
            }

            TestFace(HexPrismFaceDir.Up);
            TestFace(HexPrismFaceDir.Down);
            TestFace(HexPrismFaceDir.Right);
            TestFace(HexPrismFaceDir.ForwardRight);
            TestFace(HexPrismFaceDir.ForwardLeft);
            TestFace(HexPrismFaceDir.Left);
            TestFace(HexPrismFaceDir.BackLeft);
            TestFace(HexPrismFaceDir.BackRight);

            if (bestFace == null)
            {
                return null;
            }
            if(bestT2 < bestT1)
            {
                return null;
            }
            if(bestT1 > maxDistance)
            {
                return null;
            }
            if(bestT1 < minDistance)
            {
                return null;
            }

            {
                var up = ((HexPrismFaceDir)bestFace.Value).Up();
                GetFaceCenterAndNormal(offset, bestFace.Value, center, tileSize, out var faceCenter, out var forward);
                var right = Vector3.Cross(forward, up);

                var point = ray.origin + bestT1 * ray.direction;

                var p = point - faceCenter;

                var isUpDown = IsUpDown(bestFace.Value);

                return new RaycastCellHit
                {
                    dir = bestFace.Value,
                    point = point,
                    
                    subface = new Vector2(
                        Vector3.Dot(p, right) / (isUpDown ? tileSize.x * 0.5f : tileSize.x * OuterRadius * 0.25f),
                        Vector3.Dot(p, up) / (isUpDown ? tileSize.x * 0.5f : tileSize.y * 0.5f)
                        ),
                };
            }
        }

        public SubFace RoundSubFace(Vector3Int offset, CellFaceDir dir, Vector2 subface, PaintMode paintMode)
        {
            if(IsUpDown(dir))
            {
                if (paintMode == PaintMode.Vertex)
                {
                    var angle = Mathf.Atan2(subface.y, subface.x);
                    var angleInt = Mathf.RoundToInt((angle - Mathf.PI / 6 ) / (Mathf.PI / 3) );
                    if(angleInt < 0)
                    {
                        angleInt += 6;
                    }
                    return (SubFace)((int)(SubFace.HexRightAndTopRight) + 2 * angleInt);
                }
                else
                {
                    // Probably more efficient ways to do this
                    var angle = Mathf.Atan2(subface.y, subface.x);
                    var angleInt = Mathf.RoundToInt(angle / (Mathf.PI / 3));
                    subface = Quaternion.Euler(0, 0, -60 * angleInt) * subface;
                    if (angleInt < 0)
                    {
                        angleInt += 6;
                    }
                    if (paintMode == PaintMode.Edge)
                    {
                        return (SubFace)((int)(SubFace.HexRight) + 2 * angleInt);
                    }
                    if (subface.x < 1 / 3f)
                    {
                        return SubFace.HexCenter;
                    }
                    if (subface.y > 1 / 3f * OuterRadius * 0.5f)
                    {
                        return (SubFace)((int)(SubFace.HexRightAndTopRight) + 2 * angleInt);
                    }
                    if (subface.y < -1 / 3f * OuterRadius * 0.5f)
                    {
                        return (SubFace)((int)(SubFace.HexRightAndTopRight) + 2 * ((angleInt + 5) % 6));
                    }
                    return (SubFace)((int)(SubFace.HexRight) + 2 * angleInt);
                }

            }
            else
            {
                return SquareFaceDrawingUtils.RoundSubFace(subface, paintMode);
            }
        }

        public IEnumerable<SubFace> GetSubFaces(Vector3Int offset, CellFaceDir dir)
        {
            if(IsUpDown(dir))
            {
                yield return SubFace.HexRight;
                yield return SubFace.HexRightAndTopRight;
                yield return SubFace.HexTopRight;
                yield return SubFace.HexTopRightAndTopLeft;
                yield return SubFace.HexTopLeft;
                yield return SubFace.HexTopLeftAndLeft;
                yield return SubFace.HexLeft;
                yield return SubFace.HexLeftAndBottomLeft;
                yield return SubFace.HexBottomLeft;
                yield return SubFace.HexBottomLeftAndBottomRight;
                yield return SubFace.HexBottomRight;
                yield return SubFace.HexBottomRightAndRight;
                yield return SubFace.HexCenter;
            }
            else
            {
                yield return SubFace.BottomLeft;
                yield return SubFace.Bottom;
                yield return SubFace.BottomRight;
                yield return SubFace.Left;
                yield return SubFace.Center;
                yield return SubFace.Right;
                yield return SubFace.TopLeft;
                yield return SubFace.Top;
                yield return SubFace.TopRight;
            }
        }

        public void SetSubFaceValue(FaceDetails faceDetails, SubFace subface, int color)
        {
            switch (subface)
            {
                case SubFace.BottomLeft: faceDetails.bottomLeft = color; return;
                case SubFace.Bottom: faceDetails.bottom = color; return;
                case SubFace.BottomRight: faceDetails.bottomRight= color; return;
                case SubFace.Left: faceDetails.left = color; return;
                case SubFace.Center: faceDetails.center = color; return;
                case SubFace.Right: faceDetails.right = color; return;
                case SubFace.TopLeft: faceDetails.topLeft = color; return;
                case SubFace.Top: faceDetails.top = color; return;
                case SubFace.TopRight: faceDetails.topRight = color; return;
                case SubFace.HexRight: faceDetails.hexRight = color; return;
                case SubFace.HexRightAndTopRight: faceDetails.hexRightAndTopRight = color; return;
                case SubFace.HexTopRight: faceDetails.hexTopRight = color; return;
                case SubFace.HexTopRightAndTopLeft: faceDetails.hexTopRightAndTopLeft = color; return;
                case SubFace.HexTopLeft: faceDetails.hexTopLeft = color; return;
                case SubFace.HexTopLeftAndLeft: faceDetails.hexTopLeftAndLeft = color; return;
                case SubFace.HexLeft: faceDetails.hexLeft = color; return;
                case SubFace.HexLeftAndBottomLeft: faceDetails.hexLeftAndBottomLeft = color; return;
                case SubFace.HexBottomLeft: faceDetails.hexBottomLeft = color; return;
                case SubFace.HexBottomLeftAndBottomRight: faceDetails.hexBottomLeftAndBottomRight = color; return;
                case SubFace.HexBottomRight: faceDetails.hexBottomRight = color; return;
                case SubFace.HexBottomRightAndRight: faceDetails.hexBottomRightAndRight = color; return;
                case SubFace.HexCenter: faceDetails.hexCenter = color; return;
            }
            throw new Exception();
        }

        public int GetSubFaceValue(FaceDetails faceDetails, SubFace subface)
        {

            switch (subface)
            {
                case SubFace.BottomLeft: return faceDetails.bottomLeft;
                case SubFace.Bottom: return faceDetails.bottom;
                case SubFace.BottomRight: return faceDetails.bottomRight;
                case SubFace.Left: return faceDetails.left;
                case SubFace.Center: return faceDetails.center;
                case SubFace.Right: return faceDetails.right;
                case SubFace.TopLeft: return faceDetails.topLeft;
                case SubFace.Top: return faceDetails.top;
                case SubFace.TopRight: return faceDetails.topRight;
                case SubFace.HexRight: return faceDetails.hexRight;
                case SubFace.HexRightAndTopRight: return faceDetails.hexRightAndTopRight;
                case SubFace.HexTopRight: return faceDetails.hexTopRight;
                case SubFace.HexTopRightAndTopLeft: return faceDetails.hexTopRightAndTopLeft;
                case SubFace.HexTopLeft: return faceDetails.hexTopLeft;
                case SubFace.HexTopLeftAndLeft: return faceDetails.hexTopLeftAndLeft;
                case SubFace.HexLeft: return faceDetails.hexLeft;
                case SubFace.HexLeftAndBottomLeft: return faceDetails.hexLeftAndBottomLeft;
                case SubFace.HexBottomLeft: return faceDetails.hexBottomLeft;
                case SubFace.HexBottomLeftAndBottomRight: return faceDetails.hexBottomLeftAndBottomRight;
                case SubFace.HexBottomRight: return faceDetails.hexBottomRight;
                case SubFace.HexBottomRightAndRight: return faceDetails.hexBottomRightAndRight;
                case SubFace.HexCenter: return faceDetails.hexCenter;
            }
            throw new Exception($"{subface}");
        }

        public Vector3Int Move(Vector3Int offset, CellFaceDir dir)
        {
            switch((HexPrismFaceDir)dir)
            {
                case HexPrismFaceDir.Right: return offset + new Vector3Int(1, 0, 0);
                case HexPrismFaceDir.Left: return offset + new Vector3Int(-1, 0, 0);
                case HexPrismFaceDir.Up: return offset + new Vector3Int(0, 1, 0);
                case HexPrismFaceDir.Down: return offset + new Vector3Int(0, -1, 0);
                case HexPrismFaceDir.ForwardLeft: return offset + new Vector3Int(0, 0, 1);
                case HexPrismFaceDir.BackRight: return offset + new Vector3Int(0, 0, -1);
                case HexPrismFaceDir.ForwardRight: return offset + new Vector3Int(1, 0, 1);
                case HexPrismFaceDir.BackLeft: return offset + new Vector3Int(-1, 0, -1);
            }
            throw new Exception();
        }

    }
}