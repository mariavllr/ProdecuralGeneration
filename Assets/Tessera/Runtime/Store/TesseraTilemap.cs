using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tessera
{
    internal class TesseraTilemap
    {
        public IGrid Grid { get; set; }
        public IDictionary<Vector3Int, ModelTile> Data { get; set; }
    }
}
