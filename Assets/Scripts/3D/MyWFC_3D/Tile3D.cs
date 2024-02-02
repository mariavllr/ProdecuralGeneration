using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile3D : MonoBehaviour
{
    public enum Border
    {
        GRASS,
        PATH,
        WATER
    }

    public int probability;
    public bool isHorizontalSymetric;
    public bool isVerticalSymetric;
    [Header("Create rotated tiles")]
    public bool rotateRight;
    public bool rotate180;
    public bool rotateLeft;

    public Vector3 rotation;
    public Vector3 scale;

    public List<Tile3D> upNeighbours = new List<Tile3D>();
    public List<Tile3D> rightNeighbours = new List<Tile3D>();
    public List<Tile3D> downNeighbours = new List<Tile3D>();
    public List<Tile3D> leftNeighbours = new List<Tile3D>();

    public Border upBorder; //Z
    public Border rightBorder; //X
    public Border leftBorder; //-X
    public Border downBorder; //-Z

}
