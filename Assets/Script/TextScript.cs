using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class TextScript : MonoBehaviour
{
    public int PlayerInput;
    public Text Inputtext; 
    //private GameScript gameScript;

    // Start is called before the first frame update

    void Start()
    {

       //gameScript=GameObject.Find("GameManager").GetComponent<GameScript>();

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0)||Input.GetKeyDown(KeyCode.Keypad0))
        {
            PlayerInput = PlayerInput * 10;
        }
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            PlayerInput = PlayerInput * 10 + 1;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            PlayerInput = PlayerInput * 10 + 2;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            PlayerInput = PlayerInput * 10 + 3;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            PlayerInput = PlayerInput * 10 + 4;
        }
        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
        {
            PlayerInput = PlayerInput * 10 + 5;
        }
        if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
        {
            PlayerInput = PlayerInput * 10 + 6;
        }
        if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7))
        {
            PlayerInput = PlayerInput * 10 + 7;
        }
        if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
        {
            PlayerInput = PlayerInput * 10 + 8;
        }
        if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9))
        {
            PlayerInput = PlayerInput * 10 + 9;
        }
        
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            PlayerInput = PlayerInput /10;
        }
        
        
    }
}
