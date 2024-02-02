using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public bool collapsed;
    public Tile2D[] tileOptions;
    public bool haSidoVisitado;

    public void CreateCell(bool collapseState, Tile2D[] tiles)
    {
        collapsed = collapseState;
        tileOptions = tiles;
        haSidoVisitado = false;
    }

    public void RecreateCell(Tile2D[] tiles)
    {
        tileOptions = tiles;
    }
}
