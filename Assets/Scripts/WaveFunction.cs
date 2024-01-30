using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class WaveFunction : MonoBehaviour
{
    int iterations = 0;

    [Header("Map generation")]
    [SerializeField] private int dimensions;                      //The map is a square
    [SerializeField] private Tile[] tileObjects;                  //All the map tiles that you can use
    [SerializeField] private GameObject[] extraObjects;           //Houses, rocks, people...

    [Range(0f, 100f)]
    [SerializeField] private float extrasDensity;

    [SerializeField] private List<Cell> gridComponents;           //A list with all the cells inside the grid
    [SerializeField] private Cell cellObj;                        //They can be collapsed or not. Tiles are their children.

    [Header("Path generation")]

    private bool generandoCamino = true;
        
    [SerializeField] private Tile downPath,        
        leftRight, leftDown, rightDown, downLeft, downRight;

    private int curX;
    private int curY;
    private Tile tileToUse;
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


    void Awake()
    {
        gridComponents = new List<Cell>();
        InitializeGrid();
    }

    //---------CREATE THE GRID WITH CELLS-------------

    void InitializeGrid()
    {
        for (float y = 0; y < dimensions; y++)
        {
            for (float x = 0; x < dimensions; x++)
            {
                Cell newCell = Instantiate(cellObj, new Vector2(x, y), Quaternion.identity);
                newCell.CreateCell(false, tileObjects);
                gridComponents.Add(newCell);
            }
        }
         StartCoroutine(GeneratePath());
    }


    //---------MAKE THE PATH---------

    IEnumerator GeneratePath()
    {
        curX = UnityEngine.Random.Range(0, dimensions);
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

            yield return new WaitForSeconds(0.1f);
        }

        print("FIN CAMINO");
        generandoCamino = false;
        UpdateGeneration();
    }

    private void CheckCurrentDirections()
    {
        //Cell left = gridComponents[curX - 1 + curY * dimensions];
       // Cell right = gridComponents[curX + 1 + curY * dimensions];
       // Cell down = gridComponents[curX + (curY + 1) * dimensions];
       // Cell up = gridComponents[curX + (curY - 1) * dimensions];

       // Cell upLeftCorner = gridComponents[curX - 1 + (curY - 1) * dimensions];
       // Cell upRightCorner = gridComponents[curX + 1 + (curY - 1) * dimensions];

        if (curDirection == CurrentDirection.LEFT && curX - 1 >= 0 && !gridComponents[curX - 1 + curY * dimensions].collapsed)
        {
            //Si la direccion es izquierda, la posición a la izquierda no se sale del mapa y la celda está vacía
            //Ir a la izquierda
            curX--;
        }
        else if (curDirection == CurrentDirection.RIGHT && curX + 1 <= dimensions - 1 && !gridComponents[curX + 1 + curY * dimensions].collapsed)
        {
            //Si la direccion es derecha, la posición a la derecha no se sale del mapa y la celda está vacía
            //Ir a la derecha
            curX++;
        }
        else if (curDirection == CurrentDirection.UP && curY - 1 >= 0 && !gridComponents[curX + (curY - 1) * dimensions].collapsed)
        {
            //Si la direccion es arriba, la posición arriba no se sale del mapa y la celda está vacía
            if (continueLeft && !gridComponents[curX - 1 + (curY - 1) * dimensions].collapsed ||
            continueRight && !gridComponents[curX + 1 + (curY - 1) * dimensions].collapsed)
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
            bool chanceToChange = Mathf.FloorToInt(UnityEngine.Random.value * 1.99f) == 0;

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
        
        //Cell down = gridComponents[curX + (curY + 1) * dimensions];

        int dirValue = Mathf.FloorToInt(UnityEngine.Random.value * 2.99f);

        if (dirValue == 0 && curDirection == CurrentDirection.LEFT && curX - 1 > 0
        || dirValue == 0 && curDirection == CurrentDirection.RIGHT && curX + 1 < dimensions - 1)
        {
            if (curY - 1 >= 0)
            {
                Cell up = gridComponents[curX + (curY - 1) * dimensions];
                Cell upLeftCorner = gridComponents[curX - 1 + (curY - 1) * dimensions];
                Cell upRightCorner = gridComponents[curX + 1 + (curY - 1) * dimensions];
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
                Cell left = gridComponents[curX - 1 + curY * dimensions];

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
                Cell right = gridComponents[curX + 1 + curY * dimensions];
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

    private void UpdateMap(int x, int y, Tile selectedTile)
    {
        List<Cell> tempGrid = new List<Cell>(gridComponents);       
        Cell cellToCollapse = tempGrid[x + y * dimensions];
        cellToCollapse.collapsed = true;


        if (selectedTile == null)
        {
            Debug.Log("que??");
            return;
        }

        cellToCollapse.tileOptions = new Tile[] { selectedTile };
        Tile foundTile = cellToCollapse.tileOptions[0];

        if (cellToCollapse.transform.childCount != 0)
        {
            foreach (Transform child in cellToCollapse.transform)
            {
                Destroy(child.gameObject);
                iterations--;
            }
        }

        Instantiate(foundTile, cellToCollapse.transform.position, Quaternion.identity, cellToCollapse.transform);
        iterations++;
        print(iterations);
    }


//--------GENERATE THE REST OF THE MAP-----------
IEnumerator CheckEntropy()
    {
        List<Cell> tempGrid = new List<Cell>(gridComponents);

        tempGrid.RemoveAll(c => c.collapsed);

        //The result of this calculation determines the order of the elements in the sorted list.
        //If the result is negative, it means a should come before b; if positive, it means a should come after b;
        //and if zero, their order remains unchanged.
        tempGrid.Sort((a, b) => { return a.tileOptions.Length - b.tileOptions.Length; });

        int arrLength = tempGrid[0].tileOptions.Length;
        int stopIndex = default;

        for (int i = 1; i < tempGrid.Count; i++)
        {
            if (tempGrid[i].tileOptions.Length > arrLength)
            {
                stopIndex = i;
                break;
            }
        }

        if (stopIndex > 0)
        {
            tempGrid.RemoveRange(stopIndex, tempGrid.Count - stopIndex);
        }

        yield return new WaitForSeconds(0f);

        CollapseCell(tempGrid);
    }

    void CollapseCell(List<Cell> tempGrid)
    {
        //Elegir la celda con menos tiles posibles
        int randIndex = UnityEngine.Random.Range(0, tempGrid.Count);
        Cell cellToCollapse = tempGrid[randIndex];
        cellToCollapse.collapsed = true;

        //Elegir una tile para esa celda
        List<(Tile tile, int weight)> weightedTiles = cellToCollapse.tileOptions.Select(tile => (tile, tile.probability)).ToList();
        Tile selectedTile = ChooseTile(weightedTiles);

        if (selectedTile == null)
        {
            Debug.LogError("INCOMPATIBILITY!");
            return;
        }        

        cellToCollapse.tileOptions = new Tile[] { selectedTile };
        Tile foundTile = cellToCollapse.tileOptions[0];

        if(cellToCollapse.transform.childCount != 0)
        {
            foreach (Transform child in cellToCollapse.transform)
            {
                Destroy(child.gameObject);
            }
        }

        Instantiate(foundTile, cellToCollapse.transform.position, Quaternion.identity, cellToCollapse.transform);

        CheckExtras(foundTile, cellToCollapse.transform);

        UpdateGeneration();
    }

    void CheckExtras(Tile foundTile, Transform transform)
    {
        if(foundTile.gameObject.CompareTag("Hierba"))
        {
            float rand = UnityEngine.Random.Range(0, 100);

            if (rand > extrasDensity)
            {
                return;
            }
            else
            {
                int randomExtra = UnityEngine.Random.Range(0, extraObjects.Length);
                Instantiate(extraObjects[randomExtra], transform.position, Quaternion.identity, transform);
            }

        }
    }

    Tile ChooseTile(List<(Tile tile, int weight)> weightedTiles)
    {
        // Calculate the total weight
        int totalWeight = weightedTiles.Sum(item => item.weight);

        // Generate a random number between 0 and totalWeight - 1
        System.Random random = new System.Random();
        int randomNumber = random.Next(0, totalWeight);

        // Iterate through the tiles and find the one corresponding to the random number
        foreach (var (tile, weight) in weightedTiles)
        {
            if (randomNumber < weight)
                return tile;
            randomNumber -= weight;
        }
        return null; // This should not happen if the list is not empty
    }

    void UpdateGeneration()
    {
        List<Cell> newGenerationCell = new List<Cell>(gridComponents);
        int up, down, left, right;

        for (int y = 0; y < dimensions; y++)
        {
            for (int x = 0; x < dimensions; x++)
            {
                var index = x + y * dimensions;
                down = x + (y - 1) * dimensions;
                right = x + 1 + y * dimensions;
                up = x + (y + 1) * dimensions;
                left = x - 1 + y * dimensions;
               // rightUp = x + 1 + (y + 1) * dimensions;
               // rightDown = x + 1 + (y - 1) * dimensions;
               // leftUp = x - 1 + (y + 1) * dimensions;
               // leftDown = x - 1 + (y - 1) * dimensions;

                if (gridComponents[index].collapsed)
                {
                    newGenerationCell[index] = gridComponents[index];
                }
                else if(ReviseTileOptions(x, y))
                {
                    gridComponents[index].haSidoVisitado = true;
                    List<Tile> options = new List<Tile>();
                    foreach (Tile t in tileObjects)
                    {
                        options.Add(t);                     
                    }

                    
                    //Mira la celda de abajo
                    if (y > 0)
                    {
                        //|| (y > 1 && gridComponents[x + (y - 2) * dimensions].collapsed)
                        List<Tile> validOptions = new List<Tile>();

                            foreach (Tile possibleOptions in gridComponents[down].tileOptions)
                            {
                                var valid = possibleOptions.upNeighbours;
                                validOptions = validOptions.Concat(valid).ToList();
                            }
                            CheckValidity(options, validOptions);
 

                    }

                    //Mirar la celda derecha
                    if (x < dimensions - 1)
                    {
                        //|| ( x < dimensions - 2 && gridComponents[x + 2 + y * dimensions].collapsed)
                        List<Tile> validOptions = new List<Tile>();
                        foreach (Tile possibleOptions in gridComponents[right].tileOptions)
                        {
                            var valid = possibleOptions.leftNeighbours;
                            validOptions = validOptions.Concat(valid).ToList();
                        }

                       CheckValidity(options, validOptions);
                    }



                    //Mira la celda de arriba
                    if (y < dimensions - 1)
                    {
                        //|| (y < dimensions - 2 && gridComponents[x + (y + 2) * dimensions].collapsed)


                        List<Tile> validOptions = new List<Tile>();

                            foreach (Tile possibleOptions in gridComponents[up].tileOptions)
                            {


                                var valid = possibleOptions.downNeighbours;
                                validOptions = validOptions.Concat(valid).ToList();
                            }

                            CheckValidity(options, validOptions);

                    }


                    //Mirar la celda izquierda
                    if (x > 0)
                    {
                        //|| (x > 1 && gridComponents[x - 2 + y * dimensions].collapsed)


                        List<Tile> validOptions = new List<Tile>();

                            foreach (Tile possibleOptions in gridComponents[left].tileOptions)
                            {

                                var valid = possibleOptions.rightNeighbours;
                                validOptions = validOptions.Concat(valid).ToList();
                            }

                            CheckValidity(options, validOptions);

                    }

                    Tile[] newTileList = new Tile[options.Count];

                    for (int i = 0; i < options.Count; i++)
                    {
                        newTileList[i] = options[i];
                    }

                    newGenerationCell[index].RecreateCell(newTileList);
                }
            }
        }

        
        gridComponents = newGenerationCell;

        iterations++;
        print(iterations);
        if (iterations <= dimensions * dimensions)
        {
            if(!generandoCamino) StartCoroutine(CheckEntropy());
        }
    }

    void CheckValidity(List<Tile> optionList, List<Tile> validOption)
    {
        for (int x = optionList.Count - 1; x >= 0; x--)
        {
            var element = optionList[x];
            if (!validOption.Contains(element))
            {
                optionList.RemoveAt(x);
            }
        }
    }

    bool ReviseTileOptions(int x, int y)
    {
        //Comprueba los OCHO vecinos. Si tiene alguno colapsado, revisar sus tiles.
        int up, down, left, right, rightUp, rightDown, leftUp, leftDown;
        down = x + (y - 1) * dimensions;
        right = x + 1 + y * dimensions;
        up = x + (y + 1) * dimensions;
        left = x - 1 + y * dimensions;
        rightUp = x + 1 + (y + 1) * dimensions;
        rightDown = x + 1 + (y - 1) * dimensions;
        leftUp = x - 1 + (y + 1) * dimensions;
        leftDown = x - 1 + (y - 1) * dimensions;

        //Si está en la esquina abajo-izquierda
        if (y == 0 && x == 0)
        {
            if (gridComponents[up].collapsed || gridComponents[right].collapsed || gridComponents[rightUp].collapsed)
            {
                return true;
            }
            else return false;
        }

        //Si está en la esquina abajo-derecha
        else if(y == 0 && x == dimensions - 1)
        {
            if (gridComponents[up].collapsed || gridComponents[left].collapsed || gridComponents[leftUp].collapsed)
            {
                return true;
            }
            else return false;
        }

        //Si está en la esquina arriba-izquierda
        else if(y == dimensions -1 && x == 0)
        {
            if (gridComponents[down].collapsed || gridComponents[right].collapsed || gridComponents[rightDown].collapsed)
            {
                return true;
            }
            else return false;
        }

        //Si está en la esquina arriba-derecha
        else if (y == dimensions - 1 && x == dimensions -1)
        {
            if (gridComponents[down].collapsed || gridComponents[left].collapsed || gridComponents[leftDown].collapsed)
            {
                return true;
            }
            else return false;
        }

        //Si está en la columna izquierda
        else if (x == 0)
        {
            if (gridComponents[up].collapsed || gridComponents[down].collapsed || gridComponents[right].collapsed || gridComponents[rightUp].collapsed || gridComponents[rightDown].collapsed)
            {
                return true;
            }
            else return false;
        }

        //Si está en la columna derecha
        else if (x == dimensions - 1)
        {
            if (gridComponents[up].collapsed || gridComponents[down].collapsed || gridComponents[left].collapsed || gridComponents[leftUp].collapsed || gridComponents[leftDown].collapsed)
            {
                return true;
            }
            else return false;
        }

        //Si está en la fila de arriba
        else if (y == dimensions - 1)
        {
            if (gridComponents[down].collapsed || gridComponents[left].collapsed || gridComponents[right].collapsed || gridComponents[rightDown].collapsed || gridComponents[leftDown].collapsed)
            {
                return true;
            }
            else return false;
        }

        //Si está en la fila de abajo
        else if (y == 0)
        {
            if (gridComponents[up].collapsed || gridComponents[left].collapsed || gridComponents[right].collapsed || gridComponents[rightUp].collapsed || gridComponents[leftUp].collapsed)
            {
                return true;
            }
            else return false;
        }

        //Si no, está en medio
        else if(gridComponents[up].collapsed || gridComponents[down].collapsed || gridComponents[left].collapsed || gridComponents[right].collapsed ||
            gridComponents[rightUp].collapsed || gridComponents[rightDown].collapsed || gridComponents[leftUp].collapsed || gridComponents[leftDown].collapsed)
        {
            return true;
        }

        return false;
    }
}
