using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;

public class WaveFunction_FiToon : MonoBehaviour
{
    public int iterations = 0;

    [Header("Map generation")]
    [SerializeField] public int cellSize;
    [SerializeField] public int dimensionsX;                      //The map is a square
    [SerializeField] public int dimensionsY;                      //The map is a square
    [SerializeField] public Tile_FiToon[] tileObjects;                  //All the map tiles that you can use
    [SerializeField] Tile_FiToon comodinTile;


    [Header("Paths")]
    [SerializeField] private int numberOfRivers;
    [SerializeField] private int numberOfPaths;

    [Header("Cities and Obstacles")]
    [SerializeField] private GameObject[] cityObjects;
    [SerializeField] private GameObject[] obstacleObjects;
    [Range(0f, 100f)]
    [SerializeField] private float obstaclesDensity;
    [SerializeField] int maxCities;
    [SerializeField] int maxNumeroAnillos;

    [Header("Grid")]
    [SerializeField] public List<Cell_FiToon> gridComponents;           //A list with all the cells inside the grid
    [SerializeField] private Cell_FiToon cellObj;                        //They can be collapsed or not. Tiles are their children.

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

        gridComponents = new List<Cell_FiToon>();
        InitializeGrid();
    }

    public void ClearNeighbours(ref Tile_FiToon[] tileArray)
    {
        foreach(Tile_FiToon tile in tileArray)
        {
            if (tile.defineNeighboursManually) continue;
            tile.upNeighbours.Clear();
            tile.rightNeighbours.Clear();
            tile.downNeighbours.Clear();
            tile.leftNeighbours.Clear();
        }
    }


    Tile_FiToon CreateNewTileVariation(Tile_FiToon tile, string nameVariation)
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

        Tile_FiToon tileRotated = newTile.AddComponent<Tile_FiToon>();
        tileRotated.probability = tile.probability;

        return tileRotated;
    }
    //---------------Look if we have to create more tiles-------------------
    public void CreateRemainingCells(ref Tile_FiToon[] tileArray)
    {
        //print("llamado con tile de length: " + tileArray.Length);
        List<Tile_FiToon> newTiles = new List<Tile_FiToon>();
        foreach (Tile_FiToon tile in tileArray)
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

                Tile_FiToon tileSymetrical = newTile.AddComponent<Tile_FiToon>();
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
                Tile_FiToon tileRotated = CreateNewTileVariation(tile, "_RotateRight");
                RotateBorders90(tile, tileRotated);

                tileRotated.rotation = new Vector3(0f, 90f, 0f);
                newTiles.Add(tileRotated);
            }

            if (tile.rotate180)
            {
                Tile_FiToon tileRotated = CreateNewTileVariation(tile, "_Rotate180");
                RotateBorders180(tile, tileRotated);
                tileRotated.rotation = new Vector3(0f, 180f, 0f);
                newTiles.Add(tileRotated);
            }

            if (tile.rotateLeft)
            {
                Tile_FiToon tileRotated = CreateNewTileVariation(tile, "_RotateLeft");
                RotateBorders270(tile, tileRotated);
                tileRotated.rotation = new Vector3(0f, 270f, 0f);
                newTiles.Add(tileRotated);
            }
        }

        if(newTiles.Count != 0)
        {
            Tile_FiToon[] aux = tileArray.Concat(newTiles.ToArray()).ToArray();
            tileArray = aux;
        }
    }

    void RotateBorders90(Tile_FiToon originalTile, Tile_FiToon tileRotated)
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

    void RotateBorders180(Tile_FiToon originalTile, Tile_FiToon tileRotated)
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

    void RotateBorders270(Tile_FiToon originalTile, Tile_FiToon tileRotated) //O rotar a la izquierda
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
    public void DefineNeighbourTiles(ref Tile_FiToon[] tileArray, ref Tile_FiToon[] otherTileArray)
    {
        foreach(Tile_FiToon tile in tileArray)
        {
            if (tile.defineNeighboursManually || tile.multipleTile) continue;
            foreach (Tile_FiToon otherTile in otherTileArray)
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
        for (float z = 0; z < dimensionsY; z++)
        {
            for (float x = 0; x < dimensionsX; x++)
            {
                Cell_FiToon newCell = Instantiate(cellObj, new Vector3(x*cellSize, 0, z*cellSize), Quaternion.identity, gameObject.transform);
                newCell.CreateCell(false, tileObjects);
                gridComponents.Add(newCell);
            }
        }

        // StartCoroutine(GeneratePaths());
        UpdateGeneration();
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
        List<Cell_FiToon> tempGrid = new List<Cell_FiToon>(gridComponents);

        tempGrid.RemoveAll(c => c.collapsed);

        //The result of this calculation determines the order of the elements in the sorted list.
        //If the result is negative, it means a should come before b; if positive, it means a should come after b;
        //and if zero, their order remains unchanged.

        //Para generacion aleatoria, no en linea:
        //tempGrid.Sort((a, b) => { return a.tileOptions.Length - b.tileOptions.Length; });
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

        yield return new WaitForSeconds(0);

        CollapseCell(tempGrid);
    }

    void CollapseCell(List<Cell_FiToon> tempGrid)
    {
        bool opcionValida = false;
        //Elegir la celda con menos tiles posibles
        int randIndex = 0; // Para generar aleatorio: UnityEngine.Random.Range(0, tempGrid.Count);
        Cell_FiToon cellToCollapse = tempGrid[randIndex];
        

        //Elegir una tile para esa celda
        List<(Tile_FiToon tile, int weight)> weightedTiles = cellToCollapse.tileOptions.Select(tile => (tile, tile.probability)).ToList();
        Tile_FiToon selectedTile = ChooseTile(weightedTiles);

        if (selectedTile == null)
        {
            Debug.LogWarning("INCOMPATIBILITY!");
            Regenerate();
            //selectedTile = comodinTile;
            //opcionValida = true;
            return;
        }

        
        while (!opcionValida)
        {
            if (selectedTile.multipleTile)
            {
                if (!MultipleTile(selectedTile, cellToCollapse)) //comprobar validez
                {
                    //No se vale aqui, eliminar esta posibilidad y buscar otra
                    List<Tile_FiToon> aux = cellObj.tileOptions.ToList();
                    aux.Remove(selectedTile);
                    cellObj.tileOptions = aux.ToArray();

                    //Elegir una tile para esa celda
                    weightedTiles = cellToCollapse.tileOptions.Select(tile => (tile, tile.probability)).ToList();
                    selectedTile = ChooseTile(weightedTiles);
                    opcionValida = false;
                }

                else
                {
                    //multiple tile se puede colocar y se ha colocado
                    opcionValida = true;
                }
            }
            else opcionValida = true; //no es multiple tile
        }

        if (!selectedTile.multipleTile)
        {
            cellToCollapse.tileOptions = new Tile_FiToon[] { selectedTile };
            Tile_FiToon foundTile = cellToCollapse.tileOptions[0];

            if (cellToCollapse.transform.childCount != 0)
            {
                foreach (Transform child in cellToCollapse.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            Tile_FiToon instantiatedTile = Instantiate(foundTile, cellToCollapse.transform.position, Quaternion.identity, cellToCollapse.transform);
            if (instantiatedTile.rotation != Vector3.zero)
            {
                instantiatedTile.gameObject.transform.Rotate(foundTile.rotation, Space.Self);
            }

            instantiatedTile.gameObject.SetActive(true);
            cellToCollapse.collapsed = true;

            if (selectedTile.CompareTag("Obstaculo"))
            {
                cellToCollapse.tieneObstaculo = true;
            }

            GenerateObstacles(foundTile, cellToCollapse);
        }

        UpdateGeneration();
    }

    bool MultipleTile(Tile_FiToon tile, Cell_FiToon cell)
    {
        int index = gridComponents.IndexOf(cell);

        //Obtener las coordenadas de la celda a partir del índice
        int actualX = index % dimensionsX;
        int actualY = index / dimensionsX;

        //Comprobar que hay espacio en el mapa alrededor
        if((tile.multipleTileDimensions.x + actualX > dimensionsX) || (tile.multipleTileDimensions.y + actualY > dimensionsY))
        {
            return false; //cambiar de tile en collapsed tiile
        }

        //Comprobar si están vacías alrededor
        for (int y = actualY; y < tile.multipleTileDimensions.y + actualY; y++)
        { 
            for (int x = actualX; x < tile.multipleTileDimensions.x + actualX; x++)
            {
                if (gridComponents[x + y * dimensionsX].collapsed)
                {
                    return false; //cambiar de tile en collapsed tile
                }
            }
        }

        //Si todo funciona, colocar tile multiple
        Instantiate(tile, cell.transform.position, Quaternion.identity, cell.transform);
        print("Instanciado multiple");
        print(iterations);
        //Actualizar vecinos
        for (int y = actualY; y < tile.multipleTileDimensions.y + actualY; y++)
        {
            for (int x = actualX; x < tile.multipleTileDimensions.x + actualX; x++)
            {
                gridComponents[x + y * dimensionsX].collapsed = true;
                gridComponents[x + y * dimensionsX].tieneObstaculo = true;
                gridComponents[x + y * dimensionsX].tileOptions = new Tile_FiToon[] {tile.baseTile};
                iterations++;
                print(iterations);
            }
        }
        iterations--; //porque sumará una en el updateGeneration
        print(iterations);

        return true;
    }

    void GenerateObstacles(Tile_FiToon foundTile, Cell_FiToon cellToCollapse)
    {
        if(foundTile.gameObject.CompareTag("Hierba"))
        {
            float rand = UnityEngine.Random.Range(0, 100);

            if ((rand > obstaclesDensity) || foundTile.CompareTag("Obstaculo") || cellToCollapse.tieneObstaculo)
            {
                return;
            }
            else
            {
                int randomObstacle = UnityEngine.Random.Range(0, obstacleObjects.Length);
                Vector3 position = new Vector3(cellToCollapse.transform.position.x, foundTile.tileHeight, cellToCollapse.transform.position.z);
                Instantiate(obstacleObjects[randomObstacle], position, Quaternion.identity, cellToCollapse.transform);
            }

        }
    }

    Tile_FiToon ChooseTile(List<(Tile_FiToon tile, int weight)> weightedTiles)
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
        List<Cell_FiToon> newGenerationCell = new List<Cell_FiToon>(gridComponents);
        int up, down, left, right;

        for (int y = 0; y < dimensionsY; y++)
        {
            for (int x = 0; x < dimensionsX; x++)
            {
                var index = x + y * dimensionsX;
                down = x + (y - 1) * dimensionsX;
                right = x + 1 + y * dimensionsX;
                up = x + (y + 1) * dimensionsX;
                left = x - 1 + y * dimensionsX;
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
                    List<Tile_FiToon> options = new List<Tile_FiToon>();
                    foreach (Tile_FiToon t in tileObjects)
                    {
                        options.Add(t);                     
                    }

                    
                    //Mira la celda de abajo
                    if (y > 0)
                    {
                        //|| (y > 1 && gridComponents[x + (y - 2) * dimensions].collapsed)
                        List<Tile_FiToon> validOptions = new List<Tile_FiToon>();

                            foreach (Tile_FiToon possibleOptions in gridComponents[down].tileOptions)
                            {
                                var valid = possibleOptions.upNeighbours;
                                validOptions = validOptions.Concat(valid).ToList();
                            }
                            CheckValidity(options, validOptions);
 

                    }

                    //Mirar la celda derecha
                    if (x < dimensionsX - 1)
                    {
                        //|| ( x < dimensions - 2 && gridComponents[x + 2 + y * dimensions].collapsed)
                        List<Tile_FiToon> validOptions = new List<Tile_FiToon>();
                        foreach (Tile_FiToon possibleOptions in gridComponents[right].tileOptions)
                        {
                            var valid = possibleOptions.leftNeighbours;
                            validOptions = validOptions.Concat(valid).ToList();
                        }

                       CheckValidity(options, validOptions);
                    }



                    //Mira la celda de arriba
                    if (y < dimensionsY - 1)
                    {
                        //|| (y < dimensions - 2 && gridComponents[x + (y + 2) * dimensions].collapsed)


                        List<Tile_FiToon> validOptions = new List<Tile_FiToon>();
                        
                            foreach (Tile_FiToon possibleOptions in gridComponents[up].tileOptions)
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


                        List<Tile_FiToon> validOptions = new List<Tile_FiToon>();

                            foreach (Tile_FiToon possibleOptions in gridComponents[left].tileOptions)
                            {

                                var valid = possibleOptions.rightNeighbours;
                                validOptions = validOptions.Concat(valid).ToList();
                            }

                            CheckValidity(options, validOptions);

                    }

                    Tile_FiToon[] newTileList = new Tile_FiToon[options.Count];

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
        
        if (iterations <= dimensionsX * dimensionsY)
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
            int x = UnityEngine.Random.Range(0, dimensionsX);
            int y = UnityEngine.Random.Range(0, dimensionsY);
            var index = x + y * dimensionsX;

            encontrado = false;
            while (!encontrado)
            {
                if (gridComponents[index].tileOptions[0].CompareTag("Hierba") && !gridComponents[index].tieneObstaculo)
                {
                    encontrado = true;
                }
                else
                {
                    x = UnityEngine.Random.Range(0, dimensionsX);
                    y = UnityEngine.Random.Range(0, dimensionsY);
                    index = x + y * dimensionsX;
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
                        i = (indexCell.Item1) + (indexCell.Item2) * dimensionsX;
                        celdasVisitadas++;

                        if (celdasVisitadas == n)
                        {
                            ring++;
                            n = n * 2;
                        }

                        if (!gridComponents[i].tieneObstaculo)
                        {
                            int r = UnityEngine.Random.Range(0, cityObjects.Length);
                            if (gridComponents[i].tileOptions[0].CompareTag("Hierba"))
                            {
                                Vector3 position = new Vector3(gridComponents[i].gameObject.transform.position.x, gridComponents[i].tileOptions[0].tileHeight, gridComponents[i].gameObject.transform.position.z);
                                Instantiate(cityObjects[r], position, Quaternion.identity, gridComponents[i].gameObject.transform);
                                gridComponents[i].tieneObstaculo = true;
                            }
                            //Si tiene a alguien a la derecha
                            if (indexCell.Item1 != dimensionsX - 1)
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
                            if (indexCell.Item2 != dimensionsY - 1)
                            {
                                celdasVisitarAnillos.Enqueue((indexCell.Item1, indexCell.Item2 + 1));
                            }
                        }
                    }

                    citiesNum--;

        }
    }
    
    void CheckValidity(List<Tile_FiToon> optionList, List<Tile_FiToon> validOption)
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
        down = x + (y - 1) * dimensionsX;
        right = x + 1 + y * dimensionsX;
        up = x + (y + 1) * dimensionsX;
        left = x - 1 + y * dimensionsX;
        rightUp = x + 1 + (y + 1) * dimensionsX;
        rightDown = x + 1 + (y - 1) * dimensionsX;
        leftUp = x - 1 + (y + 1) * dimensionsX;
        leftDown = x - 1 + (y - 1) * dimensionsX;

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
        else if(y == 0 && x == dimensionsX - 1)
        {
            if (gridComponents[up].collapsed || gridComponents[left].collapsed || gridComponents[leftUp].collapsed)
            {
                return true;
            }
            else return false;
        }

        //Si está en la esquina arriba-izquierda
        else if(y == dimensionsY -1 && x == 0)
        {
            if (gridComponents[down].collapsed || gridComponents[right].collapsed || gridComponents[rightDown].collapsed)
            {
                return true;
            }
            else return false;
        }

        //Si está en la esquina arriba-derecha
        else if (y == dimensionsY - 1 && x == dimensionsX -1)
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
        else if (x == dimensionsX - 1)
        {
            if (gridComponents[up].collapsed || gridComponents[down].collapsed || gridComponents[left].collapsed || gridComponents[leftUp].collapsed || gridComponents[leftDown].collapsed)
            {
                return true;
            }
            else return false;
        }

        //Si está en la fila de arriba
        else if (y == dimensionsY - 1)
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
