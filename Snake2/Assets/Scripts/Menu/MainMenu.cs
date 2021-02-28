using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public Text difficultyText;
    public GameObject menu;
    public GameObject help;
    public Text sliderText;
    public Slider slider;
    public static string difficulty;
    [HideInInspector] public static int gridSize = 9;

    private void Start()
    {
        slider.value = gridSize;
    }

    private void Update()
    {
            sliderText.text = gridSize.ToString();
            difficulty = difficultyText.text;
    }

    //kliknutím na tlačítko postupně prochází obtížnosti
    public void Difficulty()
    {
        if (difficultyText.text == "easy")
        {
            difficultyText.text = "medium";
        }
        else if(difficultyText.text == "medium")
        {
            difficultyText.text = "hard";
        }
        else
        {
            difficultyText.text = "easy";
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(1);
    }

    //přechod mezi hlavní nabídkou a nápovědou
    public void Help()
    {
        menu.SetActive(!menu.activeSelf);
        help.SetActive(!help.activeSelf);
    }

    public void Slider()
    {
        //velikost hracího pole musí být lichá, aby to fungovalo správně
        if (slider.value % 2 == 0)
        {
            gridSize = Mathf.RoundToInt(slider.value - 1);
        }
        else
        {
            gridSize = Mathf.RoundToInt(slider.value);
        }
    }
}
