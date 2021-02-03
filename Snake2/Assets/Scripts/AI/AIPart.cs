using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIPart : MonoBehaviour
{
    private SnakeControllerAI aIScript;


    private void Awake()
    {
        aIScript = GameObject.Find("GameManager").GetComponent<SnakeControllerAI>();
    }


    private void Update()
    {
        //pokud pozice článku není v listu pozic všech článků, zničí ho
        if (!aIScript.snakePosListAI.Contains(transform.position))
        {
            Destroy(gameObject);
        }
    }

    //KOLIZE (sám se sebou)
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("AI"))
        {
            aIScript.isAliveAI = false;
            aIScript.GameOver(true);
        }
    }
}
