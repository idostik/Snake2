using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyFiling : MonoBehaviour
{
    private SnakeControllerAI aIScript;


    private void Awake()
    {
        aIScript = GameObject.Find("GameManager").GetComponent<SnakeControllerAI>();
    }


    void Update()
    {
        //pokud pozice výplně není v listu pozic všech výplní, zničí tuto výplň
        if (!aIScript.gapPosListAI.Contains(transform.position))
        {
            Destroy(gameObject);
        }
    }
}
