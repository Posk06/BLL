using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScene : MonoBehaviour
{
    public GameObject loadingScreen;

    public void LoadScene()
    {
        loadingScreen.SetActive(true);
    }
    public void LoadDone()
    {
        loadingScreen.SetActive(false);
    }
}