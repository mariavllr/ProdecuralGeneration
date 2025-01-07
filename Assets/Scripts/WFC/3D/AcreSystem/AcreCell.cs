using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AcreCell : MonoBehaviour
{
    public bool collapsed;
    public AcreTile[] tileOptions;
    public bool haSidoVisitado; //debug
    public Type cellType;
    public enum Type
    {
        TopRightCorner,
        TopLeftCorner,
        DownRightCorner,
        DownLeftCorner,
        TopBorder,
        RightBorder,
        DownBorder,
        LeftBorder,
        NormalCell
    }
    public void CreateCell(bool collapseState, AcreTile[] tiles)
    {
        collapsed = collapseState;
        tileOptions = tiles;
        haSidoVisitado = false;
        cellType = Type.NormalCell;
    }

    public void RecreateCell(AcreTile[] tiles)
    {
        tileOptions = tiles;
    }
}
