using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AcreTile : MonoBehaviour
{
    //TO DO: Hay cosas que SIEMPRE tienen que estar y SOLO pueden estar una vez:
    //Casa del jugador
    //Tienda
    //Restaurante
    //Museo
    //etc

    public enum Border
    {
        //ALTITUDES:
        //DOWN is sea level, MIDDLE is little elevation level, BASE is base terrain level, HIGH is first elevation level on base terrain
        //If is the entire border is named as [...]_ALL, if is half the border is marked as [...]_HALF_[...]

        //BORDERS:
        //The border of the map is marked as BORDER (right, left, down, top)
        //The border as a neighbour is marked as BORDER

        MAPBORDER_RIGHT,
        MAPBORDER_DOWN,
        MAPBORDER_LEFT,
        MAPBORDER_TOP,

        BASE_ALL,
        DOWN_ALL,
        MIDDLE_ALL,
        HIGH_ALL,
        DOWN_HALF_MIDDLE,
        DOWN_HALF_BASE,
        BASE_HALF_HIGH,

        RIVER_RIGHT,
        RIVER_MIDDLE,
        RIVER_LEFT,

        BORDER_RIGHT,
        BORDER_DOWN,
        BORDER_LEFT,
        BORDER_TOP,
    }

    public int probability;
    public Vector3 rotation;
    public AcreCell.Type acreType;

    public List<AcreTile> upNeighbours = new List<AcreTile>();
    public List<AcreTile> rightNeighbours = new List<AcreTile>();
    public List<AcreTile> downNeighbours = new List<AcreTile>();
    public List<AcreTile> leftNeighbours = new List<AcreTile>();

    [Tooltip("Para definir la direccion la derecha siempre será el eje X (rojo) y arriba será el eje Z (azul)")]
    [Header("Borders")]

    public Border upBorder; //Z
    public Border rightBorder; //X
    public Border leftBorder; //-X
    public Border downBorder; //-Z
}
