using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;

public class WaveFunction3D : MonoBehaviour
{
    public int iterations = 0;

    [Header("Map generation")]
    [SerializeField] public int dimensions;                      //The map is a square
    [SerializeField] public Tile3D[] tileObjects;                  //All the map tiles that you can use
    [SerializeField] private GameObject[] extraObjects;           //Houses, rocks, people...

    [Header("Paths")]
    [SerializeField] private int numberOfRivers;
    [SerializeField] private int numberOfPaths;

    [Header("Cities")]
    [Range(0f, 100f)]
    [SerializeField] private float extrasDensity;
    [SerializeField] int maxCities;
    [SerializeField] int maxNumeroAnillos;

    [Header("Grid")]
    [SerializeField] public List<Cell3D> gridComponents;           //A list with all the cells inside the grid
    [SerializeField] private Cell3D cellObj;                        //They can be collapsed or not. Tiles are their children.

    //Events
    public delegate void OnRegenerate();
    public static event OnRegenerate onRegenerate;

    private void OnEnable()
    {
        PathGenerator.onPathEnd += FinCamino;
    }

    private void OnDisable()
    {
        PathGenerator.onPathEnd -= FinCamino;
    }

    void Awake()
    {
        ClearNeighbours(ref tileObjects);
        CreateRemainingCells(ref tileObjects);
        DefineNeighbourTiles(ref tileObjects, ref tileObjects);

        gridComponents = new List<Cell3D>();
        InitializeGrid();
    }

    public void ClearNeighbours(ref Tile3D[] tileArray)
    {
        foreach(Tile3D tile in tileArray)
        {
            tile.upNeighbours.Clear();
            tile.rightNeighbours.Clear();
            tile.downNeighbours.Clear();
            tile.leftNeighbours.Clear();
        }
    }


    Tile3D CreateNewTileVariation(Tile3D tile, string nameVariation)
    {
        string name = tile.gameObject.name + nameVariation;
        GameObject newTile = new GameObject(name);
        newTile.gameObject.tag = tile.gameObject.tag; //comprobar si tag puede ser nulo
        newTile.SetActive(false);
        newTile.hideFlags = HideFlags.HideInHierarchy;

        MeshFilter meshFilter = newTile.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = tile.gameObject.GetComponent<MeshFilter>().sharedMesh;
        MeshRenderer meshRenderer = newTile.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = tile.gameObject.GetComponent<MeshRenderer>().sharedMaterials;

        Tile3D tileRotated = newTile.AddComponent<Tile3D>();
        tileRotated.probability = tile.probability;

        return tileRotated;
    }
    //---------------Look if we have to create more tiles-------------------
    public void CreateRemainingCells(ref Tile3D[] tileArray)
    {
        //print("llamado con tile de length: " + tileArray.Length);
        List<Tile3D> newTiles = new List<Tile3D>();
        foreach (Tile3D tile in tileArray)
        {
            //tile._transform = tile.gameObject.transform;
            if (tile.isHorizontalSymetric)
            {
                //----------    ----------
                //|   | |__|    |__| |   |
                //|   | ___|    |___ |   |
                //|        |    |        |
                //----------    ----------
                string name = tile.gameObject.name + "_HorizontalSymetric";
                GameObject newTile = new GameObject(name);
                newTile.gameObject.tag = tile.gameObject.tag; //comprobar si tag puede ser nulo
                newTile.SetActive(false);
                newTile.hideFlags = HideFlags.HideInHierarchy;

                MeshFilter meshFilter = newTile.AddComponent<MeshFilter>();
                meshFilter.mesh = tile.gameObject.GetComponent<MeshFilter>().mesh;
                MeshRenderer meshRenderer = newTile.AddComponent<MeshRenderer>();
                meshRenderer.materials = tile.gameObject.GetComponent<MeshRenderer>().materials;

                Tile3D tileSymetrical = newTile.AddComponent<Tile3D>();
                tileSymetrical.probability = tile.probability;

                tileSymetrical.upBorder = tile.upBorder;
                tileSymetrical.downBorder = tile.downBorder;

                //El borde izquierdo ahora es derecho
                tileSymetrical.leftBorder = tile.rightBorder;
                //El borde derecho ahora es izquierdo
                tileSymetrical.rightBorder = tile.leftBorder;

                tileSymetrical.scale = new Vector3(-newTile.transform.localScale.x, newTile.transform.localScale.y, newTile.transform.localScale.z);
                newTiles.Add(tileSymetrical);
            }
            //TO DO
           /* if (tile.isVerticalSymetric)
            {
                //----------
                //|    ____|
                //|   |  __|
                //|   | |  |
                //----------
                //
                //----------
                //|   | |__|
                //|   | ___|
                //|        |
                //----------

                Tile3D tileSymetrical = new Tile3D();

                tileSymetrical.leftBorder = tile.leftBorder;
                tileSymetrical.rightBorder = tile.rightBorder;

                tileSymetrical.leftNeighbours = tile.leftNeighbours;
                tileSymetrical.rightNeighbours = tile.rightNeighbours;

                //El borde arriba ahora es el de abajo
                tileSymetrical.upBorder = tile.downBorder;
                tileSymetrical.upNeighbours = tile.downNeighbours;
                //El borde abajo ahora es el de arriba
                tileSymetrical.downBorder = tile.upBorder;
                tileSymetrical.downNeighbours = tile.upNeighbours;

                newTiles.Add(tileSymetrical);
            }*/

            if (tile.rotateRight) //Por defecto, sentido horario
            {
                Tile3D tileRotated = CreateNewTileVariation(tile, "_RotateRight");
                RotateBorders90(tile, tileRotated);

                tileRotated.rotation = new Vector3(0f, 90f, 0f);
                newTiles.Add(tileRotated);
            }

            if (tile.rotate180)
            {
                Tile3D tileRotated = CreateNewTileVariation(tile, "_Rotate180");
                RotateBorders180(tile, tileRotated);
                tileRotated.rotation = new Vector3(0f, 180f, 0f);
                newTiles.Add(tileRotated);
            }

            if (tile.rotateLeft)
            {
                Tile3D tileRotated = CreateNewTileVariation(tile, "_RotateLeft");
                RotateBorders270(tile, tileRotated);
                tileRotated.rotation = new Vector3(0f, 270f, 0f);
                newTiles.Add(tileRotated);
            }
        }

        if(newTiles.Count != 0)
        {
            Tile3D[] aux = tileArray.Concat(newTiles.ToArray()).ToArray();
            tileArray = aux;
        }
    }

    void RotateBorders90(Tile3D originalTile, Tile3D tileRotated)
    {
        //El borde de arriba ahora es el derecho
        tileRotated.rightBorder = originalTile.upBorder;
        //El borde de abajo ahora es el izquierdo
        tileRotated.leftBorder = originalTile.downBorder;
        //El borde izquierdo ahora es el de arriba
        tileRotated.upBorder = originalTile.leftBorder;
        //El borde derecho ahora es el de abajo
        tileRotated.downBorder = originalTile.rightBorder;
    }

    void RotateBorders180(Tile3D originalTile, Tile3D tileRotated)
    {
        //El borde de arriba ahora es el de abajo
        tileRotated.downBorder = originalTile.upBorder;
        //El borde de abajo ahora es el de arriba
        tileRotated.upBorder = originalTile.downBorder;
        //El borde izquierdo ahora es el de la derecha
        tileRotated.rightBorder = originalTile.leftBorder;
        //El borde derecho ahora es el izquierdo
        tileRotated.leftBorder = originalTile.rightBorder;
    }

    void RotateBorders270(Tile3D originalTile, Tile3D tileRotated) //O rotar a la izquierda
    {
        //El borde de arriba ahora es el izquierdo
        tileRotated.leftBorder = originalTile.upBorder;
        //El borde de abajo ahora es el de la derecha
        tileRotated.rightBorder = originalTile.downBorder;
        //El borde izquierdo ahora es el de abajo
        tileRotated.downBorder = originalTile.leftBorder;
        //El borde derecho ahora es el de arriba
        tileRotated.upBorder = originalTile.rightBorder;
    }


    //Define the neighbours
    public void DefineNeighbourTiles(ref Tile3D[] tileArray, ref Tile3D[] otherTileArray)
    {
        foreach(Tile3D tile in tileArray)
        {
            foreach (Tile3D otherTile in otherTileArray)
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
                Cell3D newCell = Instantiate(cellObj, new Vector3(x, 0, z), Quaternion.identity, gameObject.transform);
                newCell.CreateCell(false, tileObjects);
                gridComponents.Add(newCell);
            }
        }

        StartCoroutine(GeneratePaths());
    }

    IEnumerator GeneratePaths()
    {
        for(int i = 0; i < numberOfRivers; i++)
        {
            yield return StartCoroutine(GetComponent<PathGenerator>().GeneratePath("RioCurva", "RioRecto"));
            
        }

        for (int i = 0; i < numberOfPaths; i++)
        {
            yield return StartCoroutine(GetComponent<PathGenerator>().GeneratePath("Curva", "Recto"));

        }
        UpdateGeneration();
    }

    void FinCamino()
    {

    }


//--------GENERATE THE REST OF THE MAP-----------
IEnumerator CheckEntropy()
    {
        List<Cell3D> tempGrid = new List<Cell3D>(gridComponents);

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

    void CollapseCell(List<Cell3D> tempGrid)
    {
        //Elegir la celda con menos tiles posibles
        int randIndex = UnityEngine.Random.Range(0, tempGrid.Count);
        Cell3D cellToCollapse = tempGrid[randIndex];
        cellToCollapse.collapsed = true;

        //Elegir una tile para esa celda
        List<(Tile3D tile, int weight)> weightedTiles = cellToCollapse.tileOptions.Select(tile => (tile, tile.probability)).ToList();
        Tile3D selectedTile = ChooseTile(weightedTiles);

        if (selectedTile == null)
        {
            Debug.LogError("INCOMPATIBILITY!");
            Regenerate();
            return;
        }        

        cellToCollapse.tileOptions = new Tile3D[] { selectedTile };
        Tile3D foundTile = cellToCollapse.tileOptions[0];

        if(cellToCollapse.transform.childCount != 0)
        {
            foreach (Transform child in cellToCollapse.transform)
            {
                Destroy(child.gameObject);
            }
        }

        Tile3D instantiatedTile = Instantiate(foundTile, cellToCollapse.transform.position, Quaternion.identity, cellToCollapse.transform);
        if (instantiatedTile.rotation != Vector3.zero)
        {
            instantiatedTile.gameObject.transform.Rotate(foundTile.rotation, Space.Self);
        }
        
        //instantiatedTile.gameObject.transform.localScale = foundTile.scale;

        instantiatedTile.gameObject.SetActive(true);

       // CheckExtras(foundTile, cellToCollapse.transform);

        UpdateGeneration();
    }

    void CheckExtras(Tile3D foundTile, Transform transform)
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

    Tile3D ChooseTile(List<(Tile3D tile, int weight)> weightedTiles)
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
        List<Cell3D> newGenerationCell = new List<Cell3D>(gridComponents);
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
                    List<Tile3D> options = new List<Tile3D>();
                    foreach (Tile3D t in tileObjects)
                    {
                        options.Add(t);                     
                    }

                    
                    //Mira la celda de abajo
                    if (y > 0)
                    {
                        //|| (y > 1 && gridComponents[x + (y - 2) * dimensions].collapsed)
                        List<Tile3D> validOptions = new List<Tile3D>();

                            foreach (Tile3D possibleOptions in gridComponents[down].tileOptions)
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
                        List<Tile3D> validOptions = new List<Tile3D>();
                        foreach (Tile3D possibleOptions in gridComponents[right].tileOptions)
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


                        List<Tile3D> validOptions = new List<Tile3D>();
                        
                            foreach (Tile3D possibleOptions in gridComponents[up].tileOptions)
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


                        List<Tile3D> validOptions = new List<Tile3D>();

                            foreach (Tile3D possibleOptions in gridComponents[left].tileOptions)
                            {

                                var valid = possibleOptions.rightNeighbours;
                                validOptions = validOptions.Concat(valid).ToList();
                            }

                            CheckValidity(options, validOptions);

                    }

                    Tile3D[] newTileList = new Tile3D[options.Count];

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
            GenerateCities();
        }
    }

    void GenerateCities()
    {
        int citiesNum = UnityEngine.Random.Range(2, maxCities + 1); //Numero random de ciudades en un rango dado
        bool encontrado;
        int indice = citiesNum;

        for(int c = 0; c < indice; c++)
        {
            //centro ciudad random
            int x = UnityEngine.Random.Range(0, dimensions);
            int y = UnityEngine.Random.Range(0, dimensions);
            var index = x + y * dimensions;

            encontrado = false;
            while (!encontrado)
            {
                if (gridComponents[index].tileOptions[0].CompareTag("Hierba") && !gridComponents[index].tieneCiudad)
                {
                    encontrado = true;
                }
                else
                {
                    x = UnityEngine.Random.Range(0, dimensions);
                    y = UnityEngine.Random.Range(0, dimensions);
                    index = x + y * dimensions;
                }
            }

            if (citiesNum < 0) return;

                    //Anillos

                    int maxRings = UnityEngine.Random.Range(1, maxNumeroAnillos); //Numero random de anillos que puede tener una ciudad

                    int ring = 0;
                    int celdasVisitadas = -1;
                    int n = 4;
                    int i;
                    (int, int) indexCell;

                    Queue<(int, int)> celdasVisitarAnillos = new Queue<(int, int)>();

                    celdasVisitarAnillos.Enqueue((x, y));

                    while (celdasVisitarAnillos.Count != 0 && ring < maxRings)
                    {
                        indexCell = celdasVisitarAnillos.Dequeue();
                        i = (indexCell.Item1) + (indexCell.Item2) * dimensions;
                        celdasVisitadas++;

                        if (celdasVisitadas == n)
                        {
                            ring++;
                            n = n * 2;
                        }

                        if (!gridComponents[i].tieneCiudad)
                        {
                            int r = UnityEngine.Random.Range(0, extraObjects.Length);
                            if (gridComponents[i].tileOptions[0].CompareTag("Hierba"))
                            {
                                Instantiate(extraObjects[r], gridComponents[i].gameObject.transform.position, Quaternion.identity, gridComponents[i].gameObject.transform);
                                gridComponents[i].tieneCiudad = true;
                            }
                            //Si tiene a alguien a la derecha
                            if (indexCell.Item1 != dimensions - 1)
                            {
                                celdasVisitarAnillos.Enqueue((indexCell.Item1 + 1, indexCell.Item2));
                            }
                            //Si no tiene a nadie debajo
                            if (indexCell.Item2 != 0)
                            {
                                celdasVisitarAnillos.Enqueue((indexCell.Item1, indexCell.Item2 - 1));
                            }
                            //Si no tiene a nadie a la izquierda
                            if (indexCell.Item1 != 0)
                            {
                                celdasVisitarAnillos.Enqueue((indexCell.Item1 - 1, indexCell.Item2));
                            }
                            //Si no tiene a nadie arriba
                            if (indexCell.Item2 != dimensions - 1)
                            {
                                celdasVisitarAnillos.Enqueue((indexCell.Item1, indexCell.Item2 + 1));
                            }
                        }
                    }

                    citiesNum--;

        }
    }

    void CheckValidity(List<Tile3D> optionList, List<Tile3D> validOption)
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


    //TO DO: Que se active solo cuando no se esté generando un mapa
    public void Regenerate()
    {
        if(onRegenerate != null)
        {
            onRegenerate();
        }
        StopAllCoroutines();
        //Borrar todas las celdas
        for (int i = gameObject.transform.childCount-1; i>=0; i--)
        {
            Destroy(gameObject.transform.GetChild(i).gameObject);
        }

        iterations = 0;
        gridComponents.Clear();

        InitializeGrid();
    }


}
