using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyPlayerFilling : MonoBehaviour
{
    private PlayerController playerScript;


    private void Awake()
    {
        playerScript = GameObject.Find("GameManager").GetComponent<PlayerController>();
    }


    void Update()
    {
        //pokud pozice výplně není v listu pozic všech výplní, zničí tuto výplň
        if (!playerScript.gapPosList.Contains(transform.position))
        {
            Destroy(gameObject);
        }
    }
}
