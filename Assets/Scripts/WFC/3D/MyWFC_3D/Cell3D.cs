using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell3D : MonoBehaviour
{
    public bool collapsed;
    public Tile3D[] tileOptions;
    public bool haSidoVisitado; //debug
    public bool tieneCiudad;
    

    public void CreateCell(bool collapseState, Tile3D[] tiles)
    {
        collapsed = collapseState;
        tileOptions = tiles;
        haSidoVisitado = false;
        tieneCiudad = false;
    }

    public void RecreateCell(Tile3D[] tiles)
    {
        tileOptions = tiles;
    }
}
