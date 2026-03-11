using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentGameManager : MonoBehaviour
{

    public static PersistentGameManager Instance;
    private WeatherController weatherController;

    private float stormIntensity = 0;
    private bool isStormActive = false;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        SceneManager.sceneLoaded += OnSceneWasLoaded;
    }
    
    void OnSceneWasLoaded(Scene scene, LoadSceneMode mode)
    {
        weatherController = FindAnyObjectByType<WeatherController>();
        if(weatherController != null)
        {
            if(isStormActive){
                weatherController.enabled = true;
            weatherController.stormIntensity = this.stormIntensity;
            }
            else
            {
                weatherController.enabled = false;
            }
        }
        else
        {
            Debug.Log("Weather controller not found!");
        }
    }
    private void Start()
    {
        DontDestroyOnLoad(gameObject);


    }

    public void  SetStormActive(bool value){
        isStormActive = value;
    }
    public void SetStormIntensity(float intensity)
    {
        stormIntensity = intensity;
    }
    public void ChangeStormIsActive()
    {
        isStormActive = !isStormActive;
    }




}
