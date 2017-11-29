﻿using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour {

    public void StartButton()
    {
        SceneManager.LoadScene("Main");
    }

    public void QuitButton()
    {
        Application.Quit();
    }
}
