using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Returned by TesseraGenerator after generation finishes
    /// </summary>
    public class TesseraCompletion
    {
        private IList<TesseraTileInstance> m_tileInstances;

        /// <summary>
        /// True if all tiles were successfully found.
        /// </summary>
        public bool success { get; set; }

        /// <summary>
        /// The list of tiles to create.
        /// </summary>
        public IList<TesseraTileInstance> tileInstances => m_tileInstances ?? (m_tileInstances = TesseraTilemapConversions.ToTileInstances(tileData, grid, gridTransform).ToList());

        /// <summary>
        /// The raw tile data
        /// </summary>
        public IDictionary<Vector3Int, ModelTile> tileData { get; set; }

        /// <summary>
        /// The number of times the generation process was restarted.
        /// </summary>
        public int retries { get; set; }

        /// <summary>
        /// The number of times the generation process backtracked.
        /// </summary>
        public int backtrackCount { get; set; }

        /// <summary>
        /// If success is false, indicates where the generation failed.
        /// </summary>
        public Vector3Int? contradictionLocation { get; set; }

        /// <summary>
        /// Indicates these instances should be added to the previous set of instances.
        /// </summary>
        public bool isIncremental { get; set; }

        /// <summary>
        /// Gives details about the cells.
        /// </summary>
        public IGrid grid { get; set; }

        /// <summary>
        /// The relationship of the grid 
        /// </summary>
        public TRS gridTransform { get; set; }

        /// <summary>
        /// Writes error information to Unity's log.
        /// </summary>
        public void LogErrror()
        {
            if (!success)
            {
                if (contradictionLocation != null)
                {
                    var loc = contradictionLocation;
                    Debug.LogError($"Failed to complete generation, issue at tile {loc}");
                }
                else
                {
                    Debug.LogError("Failed to complete generation");
                }
            }
        }
    }
}