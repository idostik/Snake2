using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPart : MonoBehaviour
{
    private PlayerController playerScript;
    private SnakeControllerAI aIScript;

    private void Awake()
    {
        playerScript = GameObject.Find("GameManager").GetComponent<PlayerController>();
        aIScript = GameObject.Find("GameManager").GetComponent<SnakeControllerAI>();
    }

    private void Update()
    {
        //pokud pozice článku není v listu pozic všech článků, zničí ho
        if (!playerScript.snakePosList.Contains(transform.position))
        {
            Destroy(gameObject);
        }
    }

    //KOLIZE (sám se sebou)
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Snake"))
        {
            playerScript.isAlive = false;
            aIScript.GameOver(false);

        }
    }
}
