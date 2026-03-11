using UnityEngine;
using UnityEngine.UI;

public class WeatherSettingsMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private PersistentGameManager persistentGameManager;
    [SerializeField] private Slider slider;
    void Start()
    {
        persistentGameManager = PersistentGameManager.Instance;
        if(persistentGameManager == null) Debug.Log("Error");
    }

    public void ValueOfSliderChanged()
    {
        persistentGameManager.SetStormIntensity(slider.value);
    }
}
