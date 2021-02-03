using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    SnakeControllerAI aIScript;

    void Awake()
    {
        aIScript = GetComponent<SnakeControllerAI>();
    }

    //NAJDE NEJKRATŠÍ MOŽNOU CESTU
    public List<Node> FindPath(Vector2 startPos, Vector2 targetPos)
    {
        //podle pozice vybere odpovídající uzle
        Node startNode = aIScript.NodeFromWorldPoint(startPos);
        Node targetNode = aIScript.NodeFromWorldPoint(targetPos);

        //skupina nevyhodnocených uzlů
        List<Node> openSet = new List<Node>();
        //skupina vyhodnocených uzlů
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        //cyklus dokud existují nějáké nevyhodnocené uzly
        while (openSet.Count > 0)
        {
            //najde uzel s nejnižší "FCost" ("currentNode")
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost || openSet[i].FCost == currentNode.FCost && openSet[i].hCost < currentNode.hCost)
                {
                    currentNode = openSet[i];
                }
            }

            //"currentNode" (uzel s nejnižší "FCost") se odebere ze skupiny nevyhodnocených uzlů a přidá se do skupiny vyhodnocených
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            //pokud je "currentNode" cílový uzel, cesta je hotová
            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            //prochází sousední uzly "currentNode"
            foreach (Node neighbour in aIScript.GetNeighbours(currentNode))
            {
                //pokud sousední uzel není schůdný nebo už je vyhodnocený, tak se přeskočí
                if (!neighbour.walkable || closedSet.Contains(neighbour))
                {
                    continue;
                }

                //vzdálenost k sousednímu uzlu je vzdálenost k aktuálnímu uzlu + 1 (protože sousední uzly jsou od sebe vzdáleny o 1)
                int newMovementCostToNeighbour = currentNode.gCost + 1;
                //pokud je nová vzdálenost od počátečního uzlu ("gCost") kratší než předchozí 
                //nebo pokud sousední uzel není ve skupině nevyhodnocených uzlů
                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    //tak se uzlu přiřadí nové proměnné
                    neighbour.gCost = newMovementCostToNeighbour;           //=vzálenost od počátečního uzlu
                    neighbour.hCost = GetDistance(neighbour, targetNode);   //vzdálenost k cílovému uzlu (společně s "gCost" dají dohromady "FCost")
                    neighbour.parent = currentNode;                         //aktuální uzel se nastaví jako "parent", aby cesta šla nakonec sestavit

                    //pokud sousední uzel ještě není ve skupině nevyhodnocených uzlů, tak se tam přidá
                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                }
            }
        }
        //pokud cesta neexistuje, vrátí "null"
        return null;
    }

    //SESTAVÍ FINÁLNÍ CESTU
    List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        //od konce pomocí rodičů přiřazených v "FindPath()" sestaví výslednou cestu
        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        //protože se cesta posládala od konce, je potřeva obrátit
        path.Reverse();

        return path;
    }

    //SPOČTE VZDÁLENOST MEZI DVOUMA UZLY (tzv. Manhattanskou metrikou)
    public int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        return dstX + dstY;
    }

    //NAJDE NEJDELŠÍ MOŽNOU CESTU
    public List<Node> FindLongestPath(Vector2 startPos, Vector2 targetPos)
    {
        //začne nejkratší cestou mezi danými body, a tu pak prodlužuje
        List<Node> path = FindPath(startPos, targetPos);
        //pokud neexistuje nejkratší cesta, nebude existovat ani nejdelší
        if (path == null)
        {
            return null;
        }
        //na první pozici přidá uzel hlavy hada (aby cesta šla rozšířit hned na začátku, viz. "IncreasePath()")
        path.Insert(0, aIScript.NodeFromWorldPoint(aIScript.currentPosAI));
        //zkusí cestu rozšířit (dvakrát, protože při první rozšířením se občas možné jedno zvětšení přeskočí)
        path = IncreasePath(path);
        path = IncreasePath(path);
        //z první pocize se odstraní uzel hlavy hada (není součástí cesty, pouze pomoc při rozšiřování)
        path.RemoveAt(0);

        return path;
    }

    //ROZŠÍŘÍ CESTU
    public List<Node> IncreasePath(List<Node> path)
    {
        //prochází postupně sousední uzly v cestě a kontroluje, jestli u nich jde cesta o další dva vedlejší uzly prodloužit
        for (int i = 0; i <= path.Count - 2; i++)
        {
            Node firstNode = path[i];
            Node secondNode = path[i + 1];
            bool pathIncreased = false;     //aby zbytečně nekontroloval obě strany, když už na jedné cestu prodloužil

            //UZLY JSOU NAD SEBOU (=> pokusí se cestu rozšířit do stran)
            if (firstNode.gridX == secondNode.gridX)
            {   
                //PRAVÁ STRANA
                Node checkFirst = aIScript.GetRightNeighbour(firstNode);
                Node checkSecond = aIScript.GetRightNeighbour(secondNode);
                //pokud sousední uzly existují, jsou schůdné a ještě nejsou součástí cesty
                if (checkFirst != null && checkSecond != null)
                {
                    if (checkFirst.walkable && checkSecond.walkable && !path.Contains(checkFirst) && !path.Contains(checkSecond))
                    {
                        //jsou vloženy mezi aktuálně kontrolované sousedy
                        path.Insert(i + 1, checkFirst);
                        path.Insert(i + 2, checkSecond);
                        pathIncreased = true;       //=> levá strana už se nebude kontrolovat
                    }
                }

                //VLEVO
                checkFirst = aIScript.GetLeftNeighbour(firstNode);
                checkSecond = aIScript.GetLeftNeighbour(secondNode);
                if (!pathIncreased && checkFirst != null && checkSecond != null && !path.Contains(checkFirst) && !path.Contains(checkSecond))
                {
                    if (checkFirst.walkable && checkSecond.walkable)
                    {
                        path.Insert(i + 1, checkFirst);
                        path.Insert(i + 2, checkSecond);
                    }
                }
            }

            //UZLY JSOU VEDLE SEBE
            else if (firstNode.gridY == secondNode.gridY)
            {
                //HORNÍ STRANA
                Node checkFirst = aIScript.GetTopNeighbour(firstNode);
                Node checkSecond = aIScript.GetTopNeighbour(secondNode);

                if (checkFirst != null && checkSecond != null)
                {
                    if (checkFirst.walkable && checkSecond.walkable && !path.Contains(checkFirst) && !path.Contains(checkSecond))
                    {
                        path.Insert(i + 1, checkFirst);
                        path.Insert(i + 2, checkSecond);
                        pathIncreased = true;
                    }
                }
                //DOLNÍ STRANA
                checkFirst = aIScript.GetBottomNeighbour(firstNode);
                checkSecond = aIScript.GetBottomNeighbour(secondNode);
                if (!pathIncreased && checkFirst != null && checkSecond != null && !path.Contains(checkFirst) && !path.Contains(checkSecond))
                {
                    if (checkFirst.walkable && checkSecond.walkable)
                    {
                        path.Insert(i + 1, checkFirst);
                        path.Insert(i + 2, checkSecond);
                    }
                }
            }
        }
        return path;
    }
}
