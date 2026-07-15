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
        //向服务器发送string
        //NetworkManager.Instance.SendBytesToServer(Encoding.UTF8.GetBytes(inputField.text));
        //向服务器发送自定义消息类型
        PlayerMsg playerMsg = new PlayerMsg(1, new PlayerData("老炮", 99.8f, 18, true));
        NetworkManager.Instance.SendBytesToServer(playerMsg.SerializeToBytes());
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