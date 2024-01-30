using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile3D : MonoBehaviour
{
    public int probability;

    public Tile3D[] upNeighbours;
    public Tile3D[] rightNeighbours;
    public Tile3D[] downNeighbours;
    public Tile3D[] leftNeighbours;
}
