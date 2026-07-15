using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestNetwork : MonoBehaviour
{
    public Button button;
    public TMP_InputField inputField;

    private void OnEnable()
    {
        button.onClick.AddListener(OnButtonClick);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        NetworkManager.Instance.SendBytesToServer(inputField.text);
    }

    void Update()
    {
        if (NetworkManager.Instance.IsHaveServerSendBytes())
        {
            byte[] bytes = NetworkManager.Instance.GetServerSendBytes();
            Debug.Log(Encoding.UTF8.GetString(bytes));
        }
    }
}