using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    //PROMĚNNÉ UZLU
    public bool walkable;
    public Vector2 worldPosition;
    public int gridX;
    public int gridY;

    public int gCost;   //vzdálenost od počátečního uzlu
    public int hCost;   //vzdálenost od cílového uzlu
    public Node parent; //rodič neboli přechozí uzel, pomocí rodičů se cesta nakonec sestaví

    //CONSTRUCTOR - přiřadí uzlu dané proměnné
    public Node(bool _walkable, Vector2 _worldPos, int _gridX, int _gridY)
    {
        walkable = _walkable;
        worldPosition = _worldPos;
        gridX = _gridX;
        gridY = _gridY;
    }
 
    //spočítá "FCost" jako součet "gCost" a "hCost" (viz. Pathfinding skript)
    public int FCost
    {
        get
        {
            return gCost + hCost;
        }
    }
}
