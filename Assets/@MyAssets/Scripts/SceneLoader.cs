using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private string sceneToLoad = "";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetDayScene()
    {
        sceneToLoad ="Test_Day";
    }

    public void SetNightScene()
    {
        sceneToLoad = "Test_Night";
    }


    public void LoadCurrentlySetScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }

}
