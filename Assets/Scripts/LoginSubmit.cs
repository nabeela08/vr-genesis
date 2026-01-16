using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoginSubmit : MonoBehaviour
{
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;

    private void Awake()
    {
        if (emailInput != null)
            emailInput.onSubmit.AddListener(_ => Focus(passwordInput));

        if (passwordInput != null)
            passwordInput.onSubmit.AddListener(_ => ClickLogin());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (passwordInput != null && passwordInput.isFocused) ClickLogin();
            else if (emailInput != null && emailInput.isFocused) Focus(passwordInput);
        }
    }

    private void Focus(TMP_InputField field)
    {
        if (field == null) return;
        field.Select();
        field.ActivateInputField();
    }

    private void ClickLogin()
    {
        if (loginButton == null) return;
        loginButton.onClick.Invoke();
    }
}
