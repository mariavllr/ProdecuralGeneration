using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell_FiToon : MonoBehaviour
{
    public bool collapsed;
    public Tile_FiToon[] tileOptions;
    public bool haSidoVisitado; //debug
    public bool tieneObstaculo;

    public void CreateCell(bool collapseState, Tile_FiToon[] tiles)
    {
        collapsed = collapseState;
        tileOptions = tiles;
        haSidoVisitado = false;
        tieneObstaculo = false;
    }

    public void RecreateCell(Tile_FiToon[] tiles)
    {
        tileOptions = tiles;
    }
}
