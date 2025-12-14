using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void CPUdifficultBotten()
    {
        SceneManager.LoadScene("CPUdifficultScene");
    }
    public void PeopleBotten()
    {
        SceneManager.LoadScene("PlayerScene");
    }
    public void CPUeasyBotten()
    {
        SceneManager.LoadScene("CPUeasyScene");
    }
    public void HomeBotten()
    {
        SceneManager.LoadScene("Title" +
            "r");
    }
}
