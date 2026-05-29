//--------------------------------------------
//This code manages the buttons on the title screen
//--------------------------------------------
// - Oskar Benjamin Trillitzsch

using UnityEngine;

public class TitleButtons : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartWorld()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("WorldCreation");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void CreateWorld()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Overworld");
    }

    public void LoadScreen()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("LoadWorld");
    }
}
