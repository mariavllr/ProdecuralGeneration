using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile_FiToon : MonoBehaviour
{
    public enum Border
    {
        HIGH_GRASS,
        LOW_GRASS,
        MEDIUM_GRASS,
        HIGH_SLOPE,
        MEDIUM_SLOPE,
        PATH,
        WATER
    }

    [Header("Only if MULTIPLE TILE")]
    public bool multipleTile;
    public Vector2 multipleTileDimensions;
    public Tile_FiToon baseTile;


    [Header("Basic properties")]
    public int probability;
    public float tileHeight;
    public bool isHorizontalSymetric;
    public bool isVerticalSymetric;

    [Header("Create rotated tiles")]
    public bool rotateRight;
    public bool rotate180;
    public bool rotateLeft;

    public Vector3 rotation;
    public Vector3 scale;

    public bool defineNeighboursManually;

    public List<Tile_FiToon> upNeighbours = new List<Tile_FiToon>();
    public List<Tile_FiToon> rightNeighbours = new List<Tile_FiToon>();
    public List<Tile_FiToon> downNeighbours = new List<Tile_FiToon>();
    public List<Tile_FiToon> leftNeighbours = new List<Tile_FiToon>();

    [Tooltip("Para definir la direccion la derecha siempre será el eje X (rojo) y arriba será el eje Z (azul)")]
    [Header("Borders")]
    
    public Border upBorder; //Z
    public Border rightBorder; //X
    public Border leftBorder; //-X
    public Border downBorder; //-Z

}
