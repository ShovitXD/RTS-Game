using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Called by the Start button
    public void StartGame()
    {
        SceneManager.LoadScene(1); // scene index 1
    }

    // Called by the Quit button
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // stop in editor
#else
        Application.Quit(); // quit in build
#endif
    }
}
