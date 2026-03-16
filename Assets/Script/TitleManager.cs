using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;  // ← これを追加

public class TitleManager: MonoBehaviour
{
    void Start()
    {
        
    }
    void Update()
    {
        
    }

    public void OnSpaceClick(InputAction.CallbackContext context)  // ← CallBackContext → CallbackContext (大文字C)
    {
        if(context.performed)  // ← !contex.performed → context.performed に修正
        {
            SceneManager.LoadScene("SampleScene");
        }
    }
}