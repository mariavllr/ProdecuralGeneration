using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile2D : MonoBehaviour
{
    public int probability;
    public bool uniqueTile;

    public bool tileAlreadyPlaced = false;

    public Tile2D[] upNeighbours;
    public Tile2D[] rightNeighbours;
    public Tile2D[] downNeighbours;
    public Tile2D[] leftNeighbours;
}
