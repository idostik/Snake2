using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SnakeControllerAI : MonoBehaviour
{
    //REFERENCE
    public GameObject snakePartAI;
    public GameObject snakeHeadAI;
    public GameObject gapFillerAI;
    public GameObject food;
    public Transform background;
    public Transform wall;
    public Camera cam;
    private PlayerController playerScript;
    private Pathfinding pathScript;
    public Text scoreAI;
    public GameObject gameOver;             //overlay co se zobrazí po skonční hry
    public Text lossText;
    public Text winText;

    //VEŘEJNÉ PROMĚNNÉ
        //proměnné upravitelné z Unity Editoru
    public float timeBtwMoves = 0.15f;
    public int gridSize = 21;  
    public float startDelay = 2f;
    public int maxLengthDifference = 10;
    public float minSnakePortionAccessible = 0.8f;
    public string difficulty = "hard";
    public int attackRadius = 4;
        //proměnné neviditelné v editoru
    [HideInInspector] public bool isAliveAI = true;
    [HideInInspector] public Vector2 foodPos;
    [HideInInspector] public GameObject foodClone;
    [HideInInspector] public GameObject headCloneAI;
    [HideInInspector] public bool foodIsSpawned = false;
    [HideInInspector] public List<Vector2> snakePosListAI = new List<Vector2>();
    [HideInInspector] public List<Vector2> gapPosListAI = new List<Vector2>();
    [HideInInspector] public Vector2 currentPosAI;
    [HideInInspector] public bool aIMovedLast;
    [HideInInspector] public int snakeLengthAI = 1;
    [HideInInspector] public Node[,] grid;
    [HideInInspector] public List<Node> pathAI;


    //PRIVÁTNÍ PROMĚNNÉ
    private string directionAI = "right";
    private int halfGridSize;
    private Vector2 startPosAI;
    private Color scoreColorAI;
    private float elapsedTime = -2f;
    private bool gameEnded = false;
    private Vector2 goRightAI;
    private Vector2 goLeftAI;
    private Vector2 goUpAI;
    private Vector2 goDownAI;


    private void Awake()
    {
        playerScript = GetComponent<PlayerController>();
        pathScript = GetComponent<Pathfinding>();
    }


    void Start()
    {
        SetupAI();
        //v daném intervalu "timeBtwMoves" volá "MovementAI" a tím se AI pohybuje
        InvokeRepeating(nameof(MovementAI), startDelay, timeBtwMoves);
    }


    void Update()
    {
        SpawnFood();
        if (isAliveAI)
        {
            FoodCheckAI();
            SnakeCheckAI();
        }
        else
        {
            CancelInvoke();
            RemoveSnakeAI();
            ReduceGapListAI();
        }
        ShowScoreAI();
        //Debug.Log(Mathf.Round(1.0f / Time.deltaTime));
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Break();
        }
    }

    //PŘIŘAZENÍ PROMĚNNÝCH PŘED ZAČÁTKEM HRY
    void SetupAI()
    {
        gridSize = MainMenu.gridSize;
        difficulty = MainMenu.difficulty;
        //nastavení velikosti hracího pole
        halfGridSize = (gridSize - 1) / 2;
        background.localScale = new Vector3(gridSize, gridSize, 1);
        wall.localScale = new Vector3(gridSize + .5f, gridSize + .5f, 1);
        cam.orthographicSize = gridSize / 2f + 0.25f;

        startPosAI = new Vector2(-halfGridSize, -halfGridSize);
        currentPosAI = startPosAI;
        scoreColorAI = new Color(0.1333333f, 0.5882353f, 0.1921569f, 1f);
        //rychlost pohybu podle dané obtížnosti
        switch (difficulty)
        {
            case "hard":
                timeBtwMoves = 0.18f;
                break;
            case "medium":
                timeBtwMoves = 0.22f;
                break;
            case "easy":
                timeBtwMoves = 0.26f;
                break;
            case "testing":
                break;
        }
        //přiřazení proměnných do druhého skriptu
        playerScript.gridSize = gridSize;
        playerScript.halfGridSize = halfGridSize;
        playerScript.timeBtwMoves = timeBtwMoves;

        gameOver.SetActive(false);
    }

    //POHYB AI
    void MovementAI()
    {
        CreateGrid();
        DirectionsAI();
        GetPath();
        NextDirection();
        //podle nastaveného směru "directionAI" se AI pohne
        switch (directionAI)
        {
            case "right":
                MoveAI(goRightAI);
                break;
            case "left":
                MoveAI(goLeftAI);
                break;
            case "up":
                MoveAI(goUpAI);
                break;
            case "down":
                MoveAI(goDownAI);
                break;
        }
        aIMovedLast = true;
        ReduceSnakePosListAI();
        ReduceGapListAI();
    }

    //VYTVOŘÍ SÍŤ UZLŮ
    public void CreateGrid()
    {   
        //z "Node" vytvoří souřadnicovou síť o velikosti "gridSize"
        grid = new Node[gridSize, gridSize];
        //prochází jednotlivé prvky
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {   
                //každému prvku přiřadí jeho souřadnice, pozici v této síti(x, y) a jeslti je schůdný
                Vector2 worlPoint = startPosAI + Vector2.right * x + Vector2.up * y;
                bool walkable = true;
                
                if (snakePosListAI.Contains(worlPoint) || playerScript.snakePosList.Contains(worlPoint))
                {
                    walkable = false;
                }
                grid[x, y] = new Node(walkable, worlPoint, x, y);
            }
        }
    }

    //RELATIVNÍ SMĚRY (pozice vedlejších uzlů)
    void DirectionsAI()
    {
        goRightAI = new Vector2(currentPosAI.x + 1, currentPosAI.y);
        goLeftAI = new Vector2(currentPosAI.x - 1, currentPosAI.y);
        goUpAI = new Vector2(currentPosAI.x, currentPosAI.y + 1);
        goDownAI = new Vector2(currentPosAI.x, currentPosAI.y - 1);
    }

    //JÍDLO - SPAWNOVÁNÍ
    void SpawnFood()
    {
        //pokud na hrací ploše není jídlo, vybere náhodný ze schůdných uzlů, na kterém se jídlo objeví
        if (!foodIsSpawned)
        {
            List<Node> walkableNodes = new List<Node>();
            if (grid != null)
            {
                foreach (Node node in grid)
                {
                    if (node.walkable)
                    {
                        walkableNodes.Add(node);
                    }
                }
                int randomIndex = Mathf.RoundToInt(Random.Range(0, walkableNodes.Count - 1));
                foodPos = walkableNodes[randomIndex].worldPosition;
                foodClone = Instantiate(food, foodPos, Quaternion.identity);    //"foodClone" vytvářím abych potom neničil originální "food" prefab
                foodIsSpawned = true;
            }
        }
    }

    //JÍDLO - SBÍRÁNÍ
    void FoodCheckAI()
    {
        if (currentPosAI == foodPos)
        {
            Destroy(foodClone);
            foodIsSpawned = false;
            snakeLengthAI += 1;
        }
    }

    //HAD - SMRT
    void SnakeCheckAI()
    {   
        //kontroluje jestli AI had nevyjel z hracího pole
        if (currentPosAI.x > halfGridSize || currentPosAI.x < -halfGridSize || currentPosAI.y > halfGridSize || currentPosAI.y < -halfGridSize)
        {
            isAliveAI = false;
            GameOver(true);
        }
        //kontrola nárazu AI do hráče
        else if (playerScript.snakePosList.Contains(currentPosAI))
        {
            isAliveAI = false;
            GameOver(true);
        }

        //řeší vzájemnou kolizi hlavami, prohraje ten kdo se pohnul jako poslední (mohl tomu zabránit)
        if (currentPosAI == playerScript.currentPos && playerScript.isAlive)
        {
            if (aIMovedLast)
            {
                isAliveAI = false;
                GameOver(true);
            }
            else
            {
                playerScript.isAlive = false;
                GameOver(false);
            }
        }

        //smrt kvůli výraznému rozdílu velikostí (kdyby se neřešila velikost, hráč by mohl zůstat malý a počkat až AI samo nabourá)
        if (playerScript.snakeLength - snakeLengthAI >= maxLengthDifference)
        {
            isAliveAI = false;
            GameOver(true);
        }
        else if (snakeLengthAI - playerScript.snakeLength >= maxLengthDifference)
        {
            playerScript.isAlive = false;
            GameOver(false);
        }
    }

    //VYBRÁNÍ CESTY PRO AI
    void GetPath()
    {   
        //vytvoří cestu k jídlu
        pathAI = pathScript.FindPath(currentPosAI, foodPos);

        if (difficulty == "hard" && pathAI != null && playerScript.isAlive && snakeLengthAI > 3)
        {   
            //zkusí vytvořit cestu o jeden uzel před hráče (aby hráč narazil)
            List<Node> attackPath = pathScript.FindPath(currentPosAI, GetPlayerNextPos());
            if (attackPath != null && attackPath.Count <= attackRadius)
            {
                pathAI = attackPath;
            }
        }
        if (difficulty != "easy")
        {   
            //pokud cesta k jídlu neexistuje, není bezpečné jít pro jídlo nebo by AI mohlo být dalším krokem zatlačeno ke stěně
            if (pathAI == null || !IsEatingSafe() ||(difficulty == "hard" && IsNextMoveTrap()))
            {   
                //vytvoří nejdelší možnou cestu k uzlu, u kterého se jako první otevře nová úniková cesta
                pathAI = pathScript.FindLongestPath(currentPosAI, GetExitNode().worldPosition);
            }
        }
    }

    //NASTAVÍ SMĚR PŘÍŠTÍHO POHYBU AI
    void NextDirection()
    {   
        if (pathAI != null && pathAI.Count != 0)
        {
            if (pathAI[0].worldPosition == goRightAI)
            {
                directionAI = "right";
            }
            else if (pathAI[0].worldPosition == goLeftAI)
            {
                directionAI = "left";
            }
            else if (pathAI[0].worldPosition == goUpAI)
            {
                directionAI = "up";
            }
            else if (pathAI[0].worldPosition == goDownAI)
            {
                directionAI = "down";
            }
        }
    }

    //POHNE S AI
    void MoveAI(Vector2 directionVectorAI)
    {
        currentPosAI = directionVectorAI;   //přiřadí novou pozici hlavy hada
        //na dané pozici vytvoří další část hada a tuto pozici vloží na začátek listu pozic jednotlivých částí
        Instantiate(snakePartAI, currentPosAI, Quaternion.identity);
        snakePosListAI.Insert(0, currentPosAI);
        //zničí starý obličej hada a na nový článek ho přidá
        if (headCloneAI)
        {
            Destroy(headCloneAI);
        }
        headCloneAI = Instantiate(snakeHeadAI, currentPosAI, Quaternion.identity);

        if (snakeLengthAI >= 2)
        {   
            //mezi jednotlivé články vkládá výplň, aby byl had přehlednější
            Vector2 gapPos = (snakePosListAI[0] + snakePosListAI[1]) / 2f;
            Instantiate(gapFillerAI, gapPos, Quaternion.identity);
            gapPosListAI.Add(gapPos);
        }
    }

    //MAŽE POZICE JIŽ NEEXISTUJÍCÍCH ČLÁNKŮ
    void ReduceSnakePosListAI()
    {
        if (snakePosListAI.Count > snakeLengthAI)
        {
            snakePosListAI.RemoveAt(snakePosListAI.Count - 1);
        }
    }

    //MAŽE POZICE JIŽ NEEXISTUJÍCÍCH VÝPLNÍ (a nakonec i hlavu)
    void ReduceGapListAI()
    {
        if (gapPosListAI.Count > snakePosListAI.Count - 1 && gapPosListAI.Count > 0)
        {
            gapPosListAI.RemoveAt(0);
        }
    }

    //VŠECHNY SOUSEDNÍ UZLE
    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();
        Node checkNode;

        checkNode = GetRightNeighbour(node);
        if (checkNode != null)
        {
            neighbours.Add(checkNode);
        }

        checkNode = GetLeftNeighbour(node);
        if (checkNode != null)
        {
            neighbours.Add(checkNode);
        }

        checkNode = GetTopNeighbour(node);
        if (checkNode != null)
        {
            neighbours.Add(checkNode);
        }

        checkNode = GetBottomNeighbour(node);
        if (checkNode != null)
        {
            neighbours.Add(checkNode);
        }
        return neighbours;
    }

    //VŠECHNY SCHŮDNÉ SOUSEDNÍ UZLE
    public List<Node> GetWalkableNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();
        Node checkNode;

        checkNode = GetRightNeighbour(node);
        if (checkNode != null && checkNode.walkable)
        {
            neighbours.Add(checkNode);
        }

        checkNode = GetLeftNeighbour(node);
        if (checkNode != null && checkNode.walkable)
        {
            neighbours.Add(checkNode);
        }

        checkNode = GetTopNeighbour(node);
        if (checkNode != null && checkNode.walkable)
        {
            neighbours.Add(checkNode);
        }

        checkNode = GetBottomNeighbour(node);
        if (checkNode != null && checkNode.walkable)
        {
            neighbours.Add(checkNode);
        }
        return neighbours;
    }

    //JEDNOTLIVÉ SOUSEDNÍ UZLE
    public Node GetTopNeighbour(Node node)
    {
        if (node.gridY + 1 < gridSize)
        {
            return grid[node.gridX, node.gridY + 1];
        }
        return null;
    }
    public Node GetBottomNeighbour(Node node)
    {
        if (node.gridY - 1 >= 0)
        {
            return grid[node.gridX, node.gridY - 1];
        }
        return null;
    }
    public Node GetRightNeighbour(Node node)
    {
        if (node.gridX + 1 < gridSize)
        {
            return grid[node.gridX + 1, node.gridY];
        }
        return null;
    }
    public Node GetLeftNeighbour(Node node)
    {
        if (node.gridX - 1 >= 0)
        {
            return grid[node.gridX - 1, node.gridY];
        }
        return null;
    }

    //AKTUÁLNĚ DOSTUPNÉ SCHŮDNÉ UZLE
    public List<Node> GetAccessibleNodes(Node currentPosNode)
    {
        List<Node> accessibleNodes = new List<Node>{currentPosNode}; //obsahuje "currentPosNode", aby na začátku nebyl prázný a dal se procházet cyklem
        //prochází list dostupných uzlů (během procházení se zvětšuje)
        for (int i = 0; i < accessibleNodes.Count; i++)
        {   
            List<Node> walkableNeighbours = GetWalkableNeighbours(accessibleNodes[i]);
            //prochází schůdné sousední uzle pro daný dostupný uzel
            for (int u = 0; u < walkableNeighbours.Count; u++)
            {
                if (!accessibleNodes.Contains(walkableNeighbours[u]))
                {   
                    //pokud "accessibleNodes" ještě neobsahuje daný sousední uzel, přidá ho
                    accessibleNodes.Add(walkableNeighbours[u]);
                }
            }
        }
        accessibleNodes.Remove(currentPosNode);     //uzel na pozici hlavy nepatří mezi dostupné
        return accessibleNodes;
    }

    //VRACÍ UZEL NÁLEŽÍCÍ DANÉ POZICI
    public Node NodeFromWorldPoint(Vector2 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x) + halfGridSize;
        int y = Mathf.RoundToInt(worldPosition.y) + halfGridSize;
        //pokud je pozice mimo hrací pole, vrátí krajní uzel
        if(x >= gridSize)
        {
            x = gridSize - 1;
        }
        else if (x < 0)
        {
            x = 0;
        }
        if (y >= gridSize)
        {
            y = gridSize - 1;
        }
        else if (y < 0)
        {
            y = 0;
        }

        return grid[x, y];
    }
  
    //PŘÍŠTÍ POZICE HLAVY HRÁČE (pokud nezmění směr)
    public Vector2 GetPlayerNextPos()
    {
        switch (playerScript.direction)
        {
            case "up":
                return playerScript.goUp;
            case "down":
                return playerScript.goDown;
            case "right":
                return playerScript.goRight;
            case "left":
                return playerScript.goLeft;
            default:
                return foodPos;
        }
    }

    //VRÁTÍ UZEL, KTERÝ JAKO PRVNÍ UVOLNÍ ÚNIKOVOU CESTU
    public Node GetExitNode()
    {
        List<Node> accessibleNodes = GetAccessibleNodes(NodeFromWorldPoint(currentPosAI));
        int maxIndex = -2;
        Node exitNode = NodeFromWorldPoint(currentPosAI);   //"currentPos" uzel je výchozí, aby to nevracelo error, když jiná "exitNode" neexistuje
        foreach(Node n in accessibleNodes)
        {   
            //prochází všechny sousedy dostupných uzlů
            List<Node> neighbours = GetNeighbours(n);
            for (int i = 0; i < neighbours.Count; i++)
            {   
                //pokud je daný uzel sousedem hada a má větší index (dřív zmizí) než ostatní, stane se "exitNode"
                int neighbourIndex = snakePosListAI.IndexOf(neighbours[i].worldPosition);
                if (neighbourIndex > maxIndex)
                {
                    maxIndex = neighbourIndex;
                    exitNode = n;
                }
            }
        }
        return exitNode;
    }

    //VYTVOŘÍ POZICI AI PO SEBRÁNÍ JÍDLA
    private List<Node> GetFutureAIListAfterEating()
    {
        List<Node> newSnakePosListAI = new List<Node>();
        //pokud je had kratší než cesta, bude had po sebrání jídla pouze na pozicích přechozí cesty
        if (pathAI.Count > snakePosListAI.Count)
        {
            for (int i = 0; i < snakePosListAI.Count; i++)
            {
                newSnakePosListAI.Add(pathAI[pathAI.Count - 1 - i]);
            }
            return newSnakePosListAI;
        }
        else
        {
            //když je delší, had bude zabírat celou přechozí cestu a ještě kus svých předchozích pozic
            foreach (Node n in pathAI)
            {
                newSnakePosListAI.Add(n);
            }

            for (int i = 0; i < snakePosListAI.Count - pathAI.Count; i++)
            {
                newSnakePosListAI.Insert(0, NodeFromWorldPoint(snakePosListAI[i]));
            }
            newSnakePosListAI.Reverse();    //pozice jsou obráceně, potřeba obrátit
            return newSnakePosListAI;
        }
    }

    //OTESTUJE, JESTLI JE BEZPEČNÉ SEBRAT JÍDLO
    private bool IsEatingSafe()
    {
        //pkud je had velký 1, vrátí true, aby nedošlo k erroru
        if (snakeLengthAI == 1) 
        {
            return true;
        }

        List<Node> futureAIListAfterEating = GetFutureAIListAfterEating();
        //všechny aktuální uzle hada udělá schůdné, potom pozice budoucího hada po sebrání neschůdné
        for (int i = 0; i < snakePosListAI.Count; i++)
        {
            NodeFromWorldPoint(snakePosListAI[i]).walkable = true;
        }
        for (int i = 0; i < futureAIListAfterEating.Count; i++)
        {
            futureAIListAfterEating[i].walkable = false;
        }

        List<Node> futureAccessibleNodes = GetAccessibleNodes(futureAIListAfterEating[0]);
        List<Node> futureTailNeighboursAI = GetWalkableNeighbours(futureAIListAfterEating[futureAIListAfterEating.Count - 1]);
        for (int i = 0; i < futureTailNeighboursAI.Count; i++)
        {
            //projde všechny uzle sousedící s budoucím ocasem, pokud budou dostupné a nebudou hned vedle hlavy, je to bezpečné
            //ocas bude pořád mizet, ale nesmí být moc blízko, aby nenastala kolize po případném sebrání jídla a prodloužení
            if (futureAccessibleNodes.Contains(futureTailNeighboursAI[i]) && pathScript.GetDistance(futureTailNeighboursAI[i], futureAIListAfterEating[0]) > 3)
            {
                ReverseWalkabilityBack(futureAIListAfterEating);
                return true;
            }
        }

        if (playerScript.isAlive)
        {
            //to samé jako výše akorát s hráčem, nepřesné protože nejde odhadnout jeho budoucí pozice a počítá se pouze s aktuální
            List<Node> tailNeighboursPlayer = GetWalkableNeighbours(NodeFromWorldPoint(playerScript.snakePosList[playerScript.snakePosList.Count - 1]));
            for (int i = 0; i < tailNeighboursPlayer.Count; i++)
            {
                if (futureAccessibleNodes.Contains(tailNeighboursPlayer[i]))
                {
                    ReverseWalkabilityBack(futureAIListAfterEating);
                    return true;
                }
            }
        }
        //bezpečné také pokud počet dostupných uzlů je větší než většina hada
        if (futureAccessibleNodes.Count > snakePosListAI.Count * minSnakePortionAccessible)
        {
            ReverseWalkabilityBack(futureAIListAfterEating);
            return true;
        }
        ReverseWalkabilityBack(futureAIListAfterEating);
        return false;
    }

    //OTOČÍ ZPĚT SCHŮDNOST UZLŮ HADA (a budoucího hada)
    void ReverseWalkabilityBack(List<Node> futureSnakeList)
    {
        for (int i = 0; i < futureSnakeList.Count; i++)
        {
            futureSnakeList[i].walkable = true;
        }
        for (int i = 0; i < snakePosListAI.Count; i++)
        {
            NodeFromWorldPoint(snakePosListAI[i]).walkable = false;
        }
    }

    //TESTUJE, JESTLI MŮŽE BÝT AI ZATLAČENO HRÁČEM KE STĚNĚ (bez možnosti úniku)
    private bool IsNextMoveTrap()
    {
        if (pathAI != null && playerScript.isAlive)
        {
            //vrátí "true" pokud je AI po příším pohybu mezi zdí a hráčem
            if (IsNextToWall(pathAI[0].worldPosition))
            {
                List<Node> neighbours = GetNeighbours(pathAI[0]);
                for (int i = 0; i < neighbours.Count; i++)
                {
                    if (playerScript.snakePosList.Contains(neighbours[i].worldPosition))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }


    //POSTUPNĚ ODSTRANÍ HADA
    void RemoveSnakeAI()
    {
        //timer, v daném intervalu odstraňuje postupně pozice z listu
        if (snakePosListAI.Count > 0)
        {
            if (elapsedTime <= 0)
            {
                snakePosListAI.RemoveAt(snakePosListAI.Count - 1);
                elapsedTime = timeBtwMoves / 2f;
            }
            else
            {
                elapsedTime -= Time.deltaTime;
            }
        }
        else if (headCloneAI)
        {
            Destroy(headCloneAI);
        }
    }

    //UKAZUJE VELIKOST (skóre) AI
    void ShowScoreAI()
    {
        scoreAI.text = snakeLengthAI.ToString();
        //když se blíží kritickému rozdílu, zčervená
        if (playerScript.snakeLength - snakeLengthAI > maxLengthDifference - 2)
        {
            scoreAI.color = Color.red;
        }
        else
        {
            scoreAI.color = scoreColorAI;
        }
    }

    //JESTLI JE VEDLE STĚNY (hranice pole)
    public bool IsNextToWall(Vector2 pos)
    {
        if (Mathf.Abs(pos.x) == halfGridSize || Mathf.Abs(pos.y) == halfGridSize)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //ZOBRAZÍ GAMEOVER OVERLAY
   public void GameOver(bool playerWon)
    {
        if (!gameEnded)
        {
            gameEnded = true;
            gameOver.SetActive(true);
            winText.enabled = playerWon;
            lossText.enabled = !playerWon;
            //pokud vyhrálo AI, zrychlí a zlepší AI a nechá ho hrát na pozadí
            if (!playerWon)
            {
                CancelInvoke();
                difficulty = "hard";
                InvokeRepeating(nameof(MovementAI), 0f, 0.1f);
            }
        }
    }

    //TESTOVÁNÍ V UNITY EDITORU
    private void OnDrawGizmos()
    {
        //vykreslí síť uzlů
        Gizmos.DrawWireCube(transform.position, new Vector3(gridSize, gridSize, 1));

        if (grid != null)
        {
            //zvýrazní uzly na základě daných podmínek
            foreach (Node node in grid)
            {
                if (node.walkable)
                {
                    Gizmos.color = Color.white;
                }
                else
                {
                    Gizmos.color = Color.red;
                }

                if (pathAI != null)
                {
                    if (pathAI.Contains(node))
                    {
                        Gizmos.color = Color.black;
                    }
                }
                //    if (futureSnakePosListAI.Count > 0)
                //    {
                //        if (futureSnakePosListAI[0] == node)
                //        {
                //            Gizmos.color = Color.green;
                //        }
                //    }
                //}

                if (snakePosListAI != null && snakePosListAI.Count > 0)
                {
                    if (snakePosListAI[0] == node.worldPosition)
                    {
                        Gizmos.color = Color.blue;
                    }
                }

                Gizmos.DrawCube(node.worldPosition, Vector3.one * 0.5f);
            }
        }
    }

}

