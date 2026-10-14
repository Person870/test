using UnityEngine;
using UnityEngine.SceneManagement;

public class menu : MonoBehaviour
{
    public string sceneToLoad = "Main";
    public string sceneToLoad2 = "Main2";


    public void StartGame() => SceneManager.LoadScene(sceneToLoad);
    public void QuitGame() => Application.Quit();
    public void ReturnGame() => SceneManager.LoadScene(sceneToLoad2);

}