using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VegetationGenerator : MonoBehaviour
{
    [Range(0f, 100f)]
    [SerializeField] private float extrasDensity;
    [SerializeField] private GameObject[] extraObjects;
    public void GenerateVegetation(Transform acre)
    {
        //Por cada acre, leer sus tiles individuales (el tile es hijo del acre)

            //Visitar las tiles
            for (int i = 0; i < acre.childCount; i++)
            {
                Transform tile = acre.GetChild(i);
                if (tile.CompareTag("Hierba"))
                {
                    //Colocar arbol random
                    float rand = Random.Range(0, 100);
                if (rand > extrasDensity)
                    {
                        return;
                    }
                    else
                    {
                        int randomExtra = Random.Range(0, extraObjects.Length);
                        Instantiate(extraObjects[randomExtra], tile.transform.position, Quaternion.identity, tile);
                }
                }
            }
        
    }
}
