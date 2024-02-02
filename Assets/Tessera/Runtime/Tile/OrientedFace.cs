using System;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Records the painted colors and location of single face of one cube in a <see cref="TesseraTile"/>
    /// </summary>
    [Serializable]
    public struct OrientedFace
    {
        public Vector3Int offset;
        public CellFaceDir faceDir;
        public FaceDetails faceDetails;


        public OrientedFace(Vector3Int offset, CellFaceDir faceDir, FaceDetails faceDetails)
        {
            this.offset = offset;
            this.faceDir = faceDir;
            this.faceDetails = faceDetails;
        }

        public void Deconstruct(out Vector3Int offset, out CellFaceDir faceDir, out FaceDetails faceDetails)
        {
            offset = this.offset;
            faceDir = this.faceDir;
            faceDetails = this.faceDetails;
        }
    }
}