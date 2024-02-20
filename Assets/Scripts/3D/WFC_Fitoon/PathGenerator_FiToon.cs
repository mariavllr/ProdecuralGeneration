using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathGenerator_FiToon : MonoBehaviour
{
    private WaveFunction3D wfc;
    private int dimensions;
    public Tile3D[] pathTiles;
    public Tile3D leftRight, leftDown, rightDown, downLeft, downRight, downPath, leftRightBridge, downBridge, cross;
    private int curX;
    private int curY;
    private Tile3D tileToUse;
    private bool forceDirectionChange = false;

    private bool continueLeft = false;
    private bool continueRight = false;
    private int currentCount = 0;               //Each 3 equal iterations it is forced to change direction


    private enum CurrentDirection
    {
        LEFT,
        RIGHT,
        DOWN,
        UP
    };
    private CurrentDirection curDirection = CurrentDirection.DOWN;

    //Events
    public delegate void OnPathEnd();
    public static event OnPathEnd onPathEnd;

    private void OnEnable()
    {
        WaveFunction3D.onRegenerate += Regenerate;
    }

    private void OnDisable()
    {
        WaveFunction3D.onRegenerate -= Regenerate;
    }

    private void Awake()
    {
        wfc = gameObject.GetComponent<WaveFunction3D>();
        dimensions = wfc.dimensions;
        wfc.ClearNeighbours(ref pathTiles);
        wfc.CreateRemainingCells(ref pathTiles);
        wfc.DefineNeighbourTiles(ref pathTiles, ref pathTiles); //vecinos entre los caminos
        wfc.DefineNeighbourTiles(ref pathTiles, ref wfc.tileObjects); //vecinos con las otras tiles
        //wfc.DefineNeighbourTiles(wfc.tileObjects, pathTiles);

    }




    public IEnumerator GeneratePath(string tagCurva, string tagRecta)
    {

        //Ajustar las tiles        
        List<Tile3D> curves = new List<Tile3D>();
        List<Tile3D> straights = new List<Tile3D>();
        List<Tile3D> crossTiles = new List<Tile3D>(); //Puentes y cruces de caminos

        foreach (Tile3D t in pathTiles)
        {
            if (t.gameObject.tag == tagCurva) curves.Add(t);
            else if (t.gameObject.tag == tagRecta) straights.Add(t);
            else if (t.gameObject.tag == "Cross") crossTiles.Add(t);
        }
        //Curvas
        foreach (Tile3D curveTile in curves)
        {
            if ((curveTile.upBorder == Tile3D.Border.PATH && curveTile.leftBorder == Tile3D.Border.PATH) || (curveTile.upBorder == Tile3D.Border.WATER && curveTile.leftBorder == Tile3D.Border.WATER))
            {
                rightDown = curveTile;
            }
            else if ((curveTile.upBorder == Tile3D.Border.PATH && curveTile.rightBorder == Tile3D.Border.PATH) || (curveTile.upBorder == Tile3D.Border.WATER && curveTile.rightBorder == Tile3D.Border.WATER))
            {
                leftDown = curveTile;
            }
            else if ((curveTile.rightBorder == Tile3D.Border.PATH && curveTile.downBorder == Tile3D.Border.PATH) || (curveTile.rightBorder == Tile3D.Border.WATER && curveTile.downBorder == Tile3D.Border.WATER))
            {
                downRight = curveTile;
            }
            else if ((curveTile.downBorder == Tile3D.Border.PATH && curveTile.leftBorder == Tile3D.Border.PATH) || (curveTile.downBorder == Tile3D.Border.WATER && curveTile.leftBorder == Tile3D.Border.WATER))
            {
                downLeft = curveTile;
            }
        }

        //Recto        
        foreach (Tile3D straightTile in straights)
        {
            if ((straightTile.upBorder == Tile3D.Border.PATH && straightTile.downBorder == Tile3D.Border.PATH) || (straightTile.upBorder == Tile3D.Border.WATER && straightTile.downBorder == Tile3D.Border.WATER))
            {
                downPath = straightTile;
            }
            else if ((straightTile.leftBorder == Tile3D.Border.PATH && straightTile.rightBorder == Tile3D.Border.PATH) || (straightTile.leftBorder == Tile3D.Border.WATER && straightTile.rightBorder == Tile3D.Border.WATER))
            {
                leftRight = straightTile;
            }
        }

        //Cruces
        foreach (Tile3D crossTile in crossTiles)
        {
            if (crossTile.upBorder == Tile3D.Border.PATH && crossTile.downBorder == Tile3D.Border.PATH && crossTile.leftBorder == Tile3D.Border.WATER && crossTile.rightBorder == Tile3D.Border.WATER)
            {
                //Es un puente asi ||
                downBridge = crossTile;
            }
            else if (crossTile.upBorder == Tile3D.Border.WATER && crossTile.downBorder == Tile3D.Border.WATER && crossTile.leftBorder == Tile3D.Border.PATH && crossTile.rightBorder == Tile3D.Border.PATH)
            {
                //Es un puente asi =
                leftRightBridge = crossTile;
            }

            else
            {
                //Es un cruce de caminos
                cross = crossTile;
            }
        }

        curX = Random.Range(0, dimensions);
        curY = 0;

        tileToUse = downPath;

        while (curY <= dimensions - 1)
        {
            CheckCurrentDirections();
            ChooseDirection();

            if (curY <= dimensions - 1)
            {
                UpdateMap(curX, curY, tileToUse);
            }

            if (curDirection == CurrentDirection.DOWN)
            {
                curY++;
            }

            yield return new WaitForSeconds(0.0f) ;
        }

        if (onPathEnd != null)
        {
            onPathEnd();
        }
    }

    private void CheckCurrentDirections()
    {
        //Cell left = gridComponents[curX - 1 + curY * dimensions];
        // Cell right = gridComponents[curX + 1 + curY * dimensions];
        // Cell down = gridComponents[curX + (curY + 1) * dimensions];
        // Cell up = gridComponents[curX + (curY - 1) * dimensions];

        // Cell upLeftCorner = gridComponents[curX - 1 + (curY - 1) * dimensions];
        // Cell upRightCorner = gridComponents[curX + 1 + (curY - 1) * dimensions];

        if (curDirection == CurrentDirection.LEFT && curX - 1 >= 0 && !wfc.gridComponents[curX - 1 + curY * dimensions].collapsed)
        {
            //Si la direccion es izquierda, la posición a la izquierda no se sale del mapa y la celda está vacía
            //Ir a la izquierda
            curX--;
        }
        else if (curDirection == CurrentDirection.RIGHT && curX + 1 <= dimensions - 1 && !wfc.gridComponents[curX + 1 + curY * dimensions].collapsed)
        {
            //Si la direccion es derecha, la posición a la derecha no se sale del mapa y la celda está vacía
            //Ir a la derecha
            curX++;
        }
        else if (curDirection == CurrentDirection.UP && curY - 1 >= 0 && !wfc.gridComponents[curX + (curY - 1) * dimensions].collapsed)
        {
            //Si la direccion es arriba, la posición arriba no se sale del mapa y la celda está vacía
            if (continueLeft && !wfc.gridComponents[curX - 1 + (curY - 1) * dimensions].collapsed ||
            continueRight && !wfc.gridComponents[curX + 1 + (curY - 1) * dimensions].collapsed)
            {
                //Si la esquina superior izquierda y derecha esta vacía
                curY--;
            }
            else
            {
                forceDirectionChange = true;

            }
        }
        else if (curDirection != CurrentDirection.DOWN)
        {
            forceDirectionChange = true;
        }
    }

    private void ChooseDirection()
    {
        if (currentCount < 3 && !forceDirectionChange)
        {
            currentCount++;
        }
        else
        {
            bool chanceToChange = Mathf.FloorToInt(Random.value * 1.99f) == 0;

            if (chanceToChange || forceDirectionChange || currentCount > 7)
            {
                currentCount = 0;
                forceDirectionChange = false;
                ChangeDirection();
            }

            currentCount++;
        }
    }

    private void ChangeDirection()
    {
        int dirValue = Mathf.FloorToInt(Random.value * 2.99f);

        if (dirValue == 0 && curDirection == CurrentDirection.LEFT && curX - 1 > 0
        || dirValue == 0 && curDirection == CurrentDirection.RIGHT && curX + 1 < dimensions - 1)
        {
            if (curY - 1 >= 0)
            {
                Cell3D up = wfc.gridComponents[curX + (curY - 1) * dimensions];
                Cell3D upLeftCorner = wfc.gridComponents[curX - 1 + (curY - 1) * dimensions];
                Cell3D upRightCorner = wfc.gridComponents[curX + 1 + (curY - 1) * dimensions];
                if (!up.collapsed &&
                !upRightCorner.collapsed &&
                !upLeftCorner.collapsed)
                {
                    GoUp();
                    return;
                }
            }
        }

        if (curDirection == CurrentDirection.LEFT)
        {
            UpdateMap(curX, curY, leftDown);
        }
        else if (curDirection == CurrentDirection.RIGHT)
        {
            UpdateMap(curX, curY, rightDown);
        }

        if (curDirection == CurrentDirection.LEFT || curDirection == CurrentDirection.RIGHT)
        {
            curY++;
            tileToUse = downPath;
            curDirection = CurrentDirection.DOWN;
            return;
        }

        if (curX - 1 > 0 && curX + 1 < dimensions - 1 || continueLeft || continueRight)
        {
            if (dirValue == 1 && !continueRight || continueLeft)
            {
                Cell3D left = wfc.gridComponents[curX - 1 + curY * dimensions];

                if (!left.collapsed)
                {
                    if (continueLeft)
                    {
                        tileToUse = rightDown;
                        continueLeft = false;
                    }
                    else
                    {
                        tileToUse = downLeft;
                    }
                    curDirection = CurrentDirection.LEFT;
                }
            }
            else
            {
                Cell3D right = wfc.gridComponents[curX + 1 + curY * dimensions];
                if (!right.collapsed)
                {
                    if (continueRight)
                    {
                        continueRight = false;
                        tileToUse = leftDown;
                    }
                    else
                    {
                        tileToUse = downRight;
                    }
                    curDirection = CurrentDirection.RIGHT;
                }
            }
        }
        else if (curX - 1 > 0)
        {
            tileToUse = downLeft;
            curDirection = CurrentDirection.LEFT;
        }
        else if (curX + 1 < dimensions - 1)
        {
            tileToUse = downRight;
            curDirection = CurrentDirection.RIGHT;
        }

        if (curDirection == CurrentDirection.LEFT)
        {
            GoLeft();
        }
        else if (curDirection == CurrentDirection.RIGHT)
        {
            GoRight();
        }
    }

    private void GoUp()
    {
        if (curDirection == CurrentDirection.LEFT)
        {
            UpdateMap(curX, curY, downRight);
            continueLeft = true;
        }
        else
        {
            UpdateMap(curX, curY, downLeft);
            continueRight = true;
        }
        curDirection = CurrentDirection.UP;
        curY--;
        tileToUse = downPath;
    }

    private void GoLeft()
    {
        UpdateMap(curX, curY, tileToUse);
        curX--;
        tileToUse = leftRight;
    }

    private void GoRight()
    {
        UpdateMap(curX, curY, tileToUse);
        curX++;
        tileToUse = leftRight;
    }

    private void UpdateMap(int x, int y, Tile3D selectedTile)
    {
        List<Cell3D> tempGrid = new List<Cell3D>(wfc.gridComponents);
        Cell3D cellToCollapse = tempGrid[x + y * dimensions];

        if (cellToCollapse.collapsed)
        {
            //Si choca con una curva
            if (cellToCollapse.tileOptions[0].CompareTag("RioCurva"))
            {
                
                try
                {
                    ChangeDirection();
                }
                catch (System.Exception)
                {

                    Regenerate();
                }
                return;
            }

            //Colocar puente
            else if(selectedTile.CompareTag("Recto") && cellToCollapse.tileOptions[0].CompareTag("RioRecto"))
            {
                //Si yo voy recto y la casilla donde estoy también está recta (deberia ser perpendicular por el if de arriba)

                if(curDirection == CurrentDirection.UP || curDirection == CurrentDirection.DOWN)
                {
                    selectedTile = downBridge;
                }
                else
                {
                    selectedTile = leftRightBridge;
                }
            }

            //Colocar cruce

            else if(cellToCollapse.tileOptions[0].CompareTag("Recto"))
            {
                print("AAAA");
               // selectedTile = cross;
            }
        }
        

        cellToCollapse.collapsed = true;


        if (selectedTile == null)
        {
            Debug.Log("que??");
            return;
        }

        cellToCollapse.tileOptions = new Tile3D[] { selectedTile };
        Tile3D foundTile = cellToCollapse.tileOptions[0];

        if (cellToCollapse.transform.childCount != 0)
        {
            foreach (Transform child in cellToCollapse.transform)
            {
                Destroy(child.gameObject);
                wfc.iterations--;
            }
        }

        Tile3D instantiatedTile = Instantiate(foundTile, cellToCollapse.transform.position, Quaternion.identity, cellToCollapse.transform);
        if (instantiatedTile.rotation != Vector3.zero)
        {
            instantiatedTile.gameObject.transform.Rotate(foundTile.rotation, Space.Self);
        }
        instantiatedTile.gameObject.SetActive(true);
        wfc.iterations++;
    }

    void Regenerate()
    {
        StopAllCoroutines();

        tileToUse = downPath;
        forceDirectionChange = false;

        continueLeft = false;
        continueRight = false;
        currentCount = 0;
}
}
