using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    //REFERENCE
    public GameObject snakePart;
    public GameObject gapFiller;
    public Text score;
    public GameObject snakeHead;

    //VEŘEJNÉ PROMĚNNÉ
    [HideInInspector] public string direction = "left";
    [HideInInspector] public Vector2 currentPos;
    [HideInInspector] public Vector2 startPos;
    [HideInInspector] public int snakeLength = 1;
    [HideInInspector] public List<Vector2> snakePosList = new List<Vector2>();
    [HideInInspector] public List<Vector2> gapPosList = new List<Vector2>();
    [HideInInspector] public bool isAlive = true;
    [HideInInspector] public Vector2 goRight;
    [HideInInspector] public Vector2 goLeft;
    [HideInInspector] public Vector2 goUp;
    [HideInInspector] public Vector2 goDown;
    [HideInInspector] public int gridSize;
    [HideInInspector] public int halfGridSize;
    [HideInInspector] public float timeBtwMoves;
    [HideInInspector] public GameObject snakeHeadClone;

    //PRIVÁTNÍ PROMĚNNÉ
    private string lastDirection = "left";
    private float elapsedTime = -2f;
    private Color scoreTextColor;
    private SnakeControllerAI aIScript;


    private void Awake()
    {
        aIScript = GetComponent<SnakeControllerAI>();
    }


    void Start()
    {
        Setup();
        //v intervalu "timeBtwMoves" volá "Movement", začne dříve než AI aby se nehýbali ve stejný čas a nevznikaly sporné kolize
        InvokeRepeating(nameof(Movement), aIScript.startDelay - timeBtwMoves / 2, timeBtwMoves);
    }


    void Update()
    {
        if (isAlive)
        {
            PlayerInput();
            FoodCheck();
            SnakeCheck();
        }
        else
        {
            CancelInvoke();
            RemoveSnake();
            //Debug.Log("hráč mrtvý");
            ReduceGapList();
        }
        ShowScore();
    }

    
    void Setup()
    {
        startPos = new Vector2(halfGridSize, halfGridSize);
        currentPos = startPos;
        scoreTextColor = new Color(0.9176471f, 0.7686275f, 0.2078431f, 1f);
    }

    //OVLÁDÁNÍ (vstup hráče)
    void PlayerInput()
    {
        //"lastDirection" je tu aby se had nemohl na místě otočit o 180 a narazit sám do sebe
        if (Input.GetKeyDown(KeyCode.LeftArrow) && lastDirection != "right")
        {
            direction = "left";
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) && lastDirection != "left")
        {
            direction = "right";
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) && lastDirection != "down")
        {
            direction = "up";
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) && lastDirection != "up")
        {
            direction = "down";
        }
    }

    //POHYB HRÁČE
    void Movement()
    {
        Directions();
        switch (direction)
        {
            case "right":
                Move(goRight);
                break;
            case "left":
                Move(goLeft);
                break;
            case "up":
                Move(goUp);
                break;
            case "down":
                Move(goDown);
                break;
        }

        aIScript.aIMovedLast = false;
        ReduceSnakePosList();
        ReduceGapList();

        //vytváří "grid" když AI zemře, aby se i potom správně spawnovalo jídlo
        if (!aIScript.isAliveAI)
        {
            aIScript.CreateGrid();
        }
    }

    //SBÍRÁNÍ JÍDLA
    void FoodCheck()
    {
        if (currentPos == aIScript.foodPos)
        {
            Destroy(aIScript.foodClone);
            aIScript.foodIsSpawned = false;
            snakeLength += 1;
        }
    }

    //HAD - SMRT
    void SnakeCheck()
    {
        //kontrola, jestli je had uvnitř hracího pole
        if (currentPos.x > halfGridSize || currentPos.x < -halfGridSize || currentPos.y > halfGridSize || currentPos.y < -halfGridSize)
        {
            isAlive = false;
            aIScript.GameOver(false);
        }

        //náraz do AI
        if (aIScript.snakePosListAI.Contains(currentPos))
        {
            isAlive = false;
            aIScript.GameOver(false);
        }
    }

    //RELATIVNÍ SMĚRY (pozice vedlejších uzlů)
    void Directions()
    {
        goRight = new Vector2(currentPos.x + 1, currentPos.y);
        goLeft = new Vector2(currentPos.x - 1, currentPos.y);
        goUp = new Vector2(currentPos.x, currentPos.y + 1);
        goDown = new Vector2(currentPos.x, currentPos.y - 1);
    }

    //POHNE S HADEM
    void Move(Vector2 directionVector)
    {
        //na pozici "directionVector" vytvoří další část hada a přidá ji do listu pozic jednotlivých článků
        currentPos = directionVector;
        Instantiate(snakePart, currentPos, Quaternion.identity);

        if (snakeHeadClone)
        {
            Destroy(snakeHeadClone);
        }
        snakeHeadClone = Instantiate(snakeHead, currentPos, Quaternion.identity);
        snakePosList.Insert(0, currentPos);
        lastDirection = direction;

        //mezi jednotlivé články přidá výplň, aby byl had přehlednější
        if (snakeLength >= 2)
        {
            Vector2 gapPos = (snakePosList[0] + snakePosList[1]) / 2f;
            Instantiate(gapFiller, gapPos, Quaternion.identity);
            gapPosList.Add(gapPos);
        }
    }

    //ZKRÁTÍ LIST POZIC ČLÁNKŮ
    void ReduceSnakePosList()
    {
        if (snakePosList.Count > snakeLength)
        {
            snakePosList.RemoveAt(snakePosList.Count - 1);
        }
    }

    //ZKRÁTÍ LIST VÝPLNÍ
    void ReduceGapList()
    {
        if (gapPosList.Count > snakePosList.Count - 1 && gapPosList.Count > 0)
        {
            gapPosList.RemoveAt(0);
        }
        else if(gapPosList.Count == 0 && snakeLength != 1 && snakeHeadClone != null)
        {
            Destroy(snakeHeadClone);
        }
    }

    //POSTUPNĚ ODSTRANÍ HADA
    void RemoveSnake()
    {
        if (snakePosList.Count > 0)
        {
            if (elapsedTime <= 0)
            {
                snakePosList.RemoveAt(snakePosList.Count - 1);
                elapsedTime = timeBtwMoves / 2f;
            }
            else
            {
                elapsedTime -= Time.deltaTime;
            }
        }
    }

    //ZOBRAZÍ SKÓRE
    void ShowScore()
    {
        score.text = snakeLength.ToString();
        //když se blíží smrti kvůli rozdílu velikostí, zvýrazní skóre červeně
        if (aIScript.snakeLengthAI - snakeLength > aIScript.maxLengthDifference - 2)
        {
            score.color = Color.red;
        }
        else
        {
            score.color = scoreTextColor;
        }
    }
}
