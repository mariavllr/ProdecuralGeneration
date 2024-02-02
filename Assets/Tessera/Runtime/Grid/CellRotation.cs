using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tessera
{
    /// Represents a particular rotation of a generic cell.
    /// Despite the name, this usually includes reflections too.
    /// The enum is empty - to work with directions, you need to either:
    /// * Use the methods on <see cref="ICellType"/>.
    /// * Cast to the enum specific to a given cell type, e.g. <see cref="CubeRotation"/>.
    public enum CellRotation
    {

    }
}
