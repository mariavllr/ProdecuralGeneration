using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AcreGenerator_WFC : MonoBehaviour
{
    public int iterations = 4; //las 4 esquinas

    [Header("Map generation")]
    [SerializeField] public int dimensions;                      //The map is a square
    [SerializeField] public int cellSize;
    [SerializeField] public AcreTile[] acreObjects;                  //All the map tiles that you can use
    VegetationGenerator vegetationGenerator;


    [Header("Grid")]
    [SerializeField] public List<AcreCell> gridComponents;           //A list with all the cells inside the grid
    [SerializeField] private AcreCell cellObj;                        //They can be collapsed or not. Tiles are their children.

    //Events
    public delegate void OnRegenerate();
    public static event OnRegenerate onRegenerate;

    void Awake()
    {
        vegetationGenerator = GetComponent<VegetationGenerator>();

        ClearNeighbours(ref acreObjects);
        DefineNeighbourTiles(ref acreObjects, ref acreObjects);

        gridComponents = new List<AcreCell>();
        InitializeGrid();
    }

    public void ClearNeighbours(ref AcreTile[] tileArray)
    {
        foreach (AcreTile tile in tileArray)
        {
            tile.upNeighbours.Clear();
            tile.rightNeighbours.Clear();
            tile.downNeighbours.Clear();
            tile.leftNeighbours.Clear();
        }
    }

    //Define the neighbours
    public void DefineNeighbourTiles(ref AcreTile[] tileArray, ref AcreTile[] otherTileArray)
    {
        foreach (AcreTile tile in tileArray)
        {
            foreach (AcreTile otherTile in otherTileArray)
            {
                //Vecinos de arriba, los que coincidan en el borde de abajo
                if (otherTile.downBorder == tile.upBorder)
                {
                    tile.upNeighbours.Add(otherTile);
                }
                //Vecinos de abajo
                if (otherTile.upBorder == tile.downBorder)
                {
                    tile.downNeighbours.Add(otherTile);
                }
                //Vecinos a la derecha
                if (otherTile.leftBorder == tile.rightBorder)
                {
                    tile.rightNeighbours.Add(otherTile);
                }
                //Vecinos a la izquierda
                if (otherTile.rightBorder == tile.leftBorder)
                {
                    tile.leftNeighbours.Add(otherTile);
                }
            }
        }
    }

    //---------CREATE THE GRID WITH CELLS-------------

    void InitializeGrid()
    {
        for (float z = 0; z < dimensions; z++)
        {
            for (float x = 0; x < dimensions; x++)
            {
                AcreCell newCell = Instantiate(cellObj, new Vector3(x * cellSize, 0, z * cellSize), cellObj.transform.rotation, gameObject.transform);
                newCell.CreateCell(false, acreObjects);
                gridComponents.Add(newCell);

                if(x == 0 && z == 0)
                {
                    CollapseCorner(AcreCell.Type.DownLeftCorner, newCell);
                }
                else if (x == 0 && z == dimensions-1)
                {
                    CollapseCorner(AcreCell.Type.TopLeftCorner, newCell);
                }
                else if (x == dimensions-1 && z == dimensions-1)
                {
                    CollapseCorner(AcreCell.Type.TopRightCorner, newCell);
                }
                else if (x == dimensions-1 && z == 0)
                {
                    CollapseCorner(AcreCell.Type.DownRightCorner, newCell);
                }
            }
        }

        UpdateGeneration();
    }


    void CollapseCorner(AcreCell.Type type, AcreCell cellToCollapse)
    {
        cellToCollapse.collapsed = true;

        //Elegir una tile para esa celda
        List<(AcreTile tile, int weightedTiles)> tileOptions = cellToCollapse.tileOptions
        .Where(tile => tile.acreType == type)  // Filtrar por tipo de celda
        .Select(tile => (tile, tile.probability))  // Crear lista de tiles con sus probabilidades
        .ToList();
        AcreTile selectedTile = ChooseTile(tileOptions);

        if (selectedTile == null)
        {
            Debug.LogError("INCOMPATIBILITY!");
            Regenerate();
            return;
        }

        cellToCollapse.tileOptions = new AcreTile[] { selectedTile };
        AcreTile foundTile = cellToCollapse.tileOptions[0];

        if (cellToCollapse.transform.childCount != 0)
        {
            foreach (Transform child in cellToCollapse.transform)
            {
                Destroy(child.gameObject);
            }
        }

        AcreTile instantiatedTile = Instantiate(foundTile, cellToCollapse.transform.position, Quaternion.identity, cellToCollapse.transform);
        if (instantiatedTile.rotation != Vector3.zero)
        {
            instantiatedTile.gameObject.transform.Rotate(foundTile.rotation, Space.Self);
        }
        instantiatedTile.gameObject.SetActive(true);
    }

    //--------GENERATE THE REST OF THE MAP-----------
    IEnumerator CheckEntropy()
    {
        List<AcreCell> tempGrid = new List<AcreCell>(gridComponents);

        tempGrid.RemoveAll(c => c.collapsed);

        //The result of this calculation determines the order of the elements in the sorted list.
        //If the result is negative, it means a should come before b; if positive, it means a should come after b;
        //and if zero, their order remains unchanged.
        tempGrid.Sort((a, b) => { return a.tileOptions.Length - b.tileOptions.Length; });
        
        int arrLength;

        try
        {
            arrLength = tempGrid[0].tileOptions.Length;
        }
        catch (System.Exception)
        {
            print(iterations);
            throw;
        }
        
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

        StartCoroutine(CollapseCell(tempGrid));
    }

    IEnumerator CollapseCell(List<AcreCell> tempGrid)
    {
        int randIndex = 0;
        AcreCell cellToCollapse = tempGrid[randIndex];
        cellToCollapse.collapsed = true;

        //Elegir una tile para esa celda
        List<(AcreTile tile, int weight)> weightedTiles = cellToCollapse.tileOptions.Select(tile => (tile, tile.probability)).ToList();
        AcreTile selectedTile = ChooseTile(weightedTiles);

        if (selectedTile == null)
        {
            Debug.LogError("INCOMPATIBILITY!");
            Regenerate();
            yield break;
        }

        cellToCollapse.tileOptions = new AcreTile[] { selectedTile };
        AcreTile foundTile = cellToCollapse.tileOptions[0];

        if (cellToCollapse.transform.childCount != 0)
        {
            foreach (Transform child in cellToCollapse.transform)
            {
                Destroy(child.gameObject);
            }
        }

        AcreTile instantiatedTile = Instantiate(foundTile, cellToCollapse.transform.position, Quaternion.identity, cellToCollapse.transform);
        if (instantiatedTile.rotation != Vector3.zero)
        {
            instantiatedTile.gameObject.transform.Rotate(foundTile.rotation, Space.Self);
        }
        instantiatedTile.gameObject.SetActive(true);

        vegetationGenerator.GenerateVegetation(instantiatedTile.transform);
        yield return new WaitForSeconds(0.1f);
        UpdateGeneration();
    }

    /* void CheckExtras(Tile3D foundTile, Transform transform)
     {
         if (foundTile.gameObject.CompareTag("Hierba"))
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
     }*/

    AcreTile ChooseTile(List<(AcreTile tile, int weight)> weightedTiles)
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
        List<AcreCell> newGenerationCell = new List<AcreCell>(gridComponents);
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
                else if (ReviseTileOptions(x, y))
                {
                    gridComponents[index].haSidoVisitado = true;
                    List<AcreTile> options = new List<AcreTile>();
                    foreach (AcreTile t in acreObjects)
                    {
                        options.Add(t);
                    }


                    //Mira la celda de abajo
                    if (y > 0)
                    {
                        //|| (y > 1 && gridComponents[x + (y - 2) * dimensions].collapsed)
                        List<AcreTile> validOptions = new List<AcreTile>();

                        foreach (AcreTile possibleOptions in gridComponents[down].tileOptions)
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
                        List<AcreTile> validOptions = new List<AcreTile>();
                        foreach (AcreTile possibleOptions in gridComponents[right].tileOptions)
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


                        List<AcreTile> validOptions = new List<AcreTile>();

                        foreach (AcreTile possibleOptions in gridComponents[up].tileOptions)
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


                        List<AcreTile> validOptions = new List<AcreTile>();

                        foreach (AcreTile possibleOptions in gridComponents[left].tileOptions)
                        {

                            var valid = possibleOptions.rightNeighbours;
                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);

                    }

                    AcreTile[] newTileList = new AcreTile[options.Count];

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

        if (iterations <= dimensions * dimensions)
        {
            StartCoroutine(CheckEntropy());
        }

        else
        {
            //Ha terminado de generar el terreno
        }
    }


    void CheckValidity(List<AcreTile> optionList, List<AcreTile> validOption)
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

    bool ReviseTileOptions(int x, int y) //Optimizacion, que solo compruebe lo necesario, no todo el mapa todo el rato
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
        else if (y == 0 && x == dimensions - 1)
        {
            if (gridComponents[up].collapsed || gridComponents[left].collapsed || gridComponents[leftUp].collapsed)
            {
                return true;
            }
            else return false;
        }

        //Si está en la esquina arriba-izquierda
        else if (y == dimensions - 1 && x == 0)
        {
            if (gridComponents[down].collapsed || gridComponents[right].collapsed || gridComponents[rightDown].collapsed)
            {
                return true;
            }
            else return false;
        }

        //Si está en la esquina arriba-derecha
        else if (y == dimensions - 1 && x == dimensions - 1)
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
        else if (gridComponents[up].collapsed || gridComponents[down].collapsed || gridComponents[left].collapsed || gridComponents[right].collapsed ||
            gridComponents[rightUp].collapsed || gridComponents[rightDown].collapsed || gridComponents[leftUp].collapsed || gridComponents[leftDown].collapsed)
        {
            return true;
        }

        return false;
    }


    //TO DO: Que se active solo cuando no se esté generando un mapa
    public void Regenerate()
    {
        if (onRegenerate != null)
        {
            onRegenerate();
        }
        StopAllCoroutines();
        //Borrar todas las celdas
        for (int i = gameObject.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(gameObject.transform.GetChild(i).gameObject);
        }

        iterations = 4;
        gridComponents.Clear();

        InitializeGrid();
    }
}
