using DeBroglie;
using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Rot;
using DeBroglie.Topo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Tessera
{
    public enum PinType
    {
        /// <summary>
        /// Forces generation the pinned tile at the location of the pin.
        /// </summary>
        Pin,
        /// <summary>
        /// The faces of the pinned tile are used to constrain the cells adjacent to the location of the pinned tile. 
        /// </summary>
        FacesOnly,
        /// <summary>
        /// The faces of the pinned tile are used to constrain the cells adjacent to the location of the pinned tile and
        /// the cells covered by the pin tile are masked out so no tiles will be generated in that location.
        /// </summary>
        FacesAndInterior,
    }

    [AddComponentMenu("Tessera/Tessera Pinned", 21)]
    public class TesseraPinned : MonoBehaviour
    {
        /// <summary>
        /// The tile to pin. 
        /// Defaults to a tile component found on the same GameObject
        /// </summary>
        public TesseraTile tile;

        /// <summary>
        /// Sets the type of pin to apply.
        /// </summary>
        public PinType pinType;

    }
}
