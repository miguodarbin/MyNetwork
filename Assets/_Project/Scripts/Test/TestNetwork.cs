using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestNetwork : MonoBehaviour
{
    public TMP_InputField inputField;
    public Button stringButton;
    public Button sendCustomTypeButton;
    public Button sendFenBaoButton;
    public Button sendNianBaoButton;
    public Button sendFenBaoNianBaoButton;

    private void OnEnable()
    {
        stringButton.onClick.AddListener(OnStringButtonClick);
        sendCustomTypeButton.onClick.AddListener(OnSendCustomTypeButtonClick);
        sendFenBaoButton.onClick.AddListener(OnSendFenBaoButtonClick);
        sendNianBaoButton.onClick.AddListener(OnSendNianBaoButtonClick);
        sendNianBaoButton.onClick.AddListener(OnSendFenBaoNianBaoButtonClick);
    }

    private void OnDisable()
    {
        stringButton.onClick.RemoveListener(OnStringButtonClick);
        sendCustomTypeButton.onClick.RemoveListener(OnSendCustomTypeButtonClick);
        sendFenBaoButton.onClick.RemoveListener(OnSendFenBaoButtonClick);
        sendNianBaoButton.onClick.RemoveListener(OnSendNianBaoButtonClick);
        sendFenBaoNianBaoButton.onClick.RemoveListener(OnSendFenBaoNianBaoButtonClick);
    }

    private void OnStringButtonClick()
    {
        //向服务器发送string
        NetworkManager.Instance.SendBytesToServer(Encoding.UTF8.GetBytes(inputField.text));
    }

    private void OnSendCustomTypeButtonClick()
    {
        //向服务器发送自定义消息类型
        PlayerMsg playerMsg = new PlayerMsg(1, new PlayerData("老炮", 99.8f, 18, true));
        NetworkManager.Instance.SendBytesToServer(playerMsg.SerializeToBytes());
    }

    private void OnSendFenBaoButtonClick()
    {
        Debug.Log("Send FenBao");
    }

    private void OnSendNianBaoButtonClick()
    {
        Debug.Log("Send NianBao");
    }

    private void OnSendFenBaoNianBaoButtonClick()
    {
        Debug.Log("Send FenBaoNianBao");
    }

    void Update()
    {
        //接收消息
        if (NetworkManager.Instance.IsHaveServerSendBytes())
        {
            byte[] bytes = NetworkManager.Instance.GetServerSendBytes();
            int MsgType = BitConverter.ToInt32(bytes, 0);
            Debug.Log(MsgType);
            switch (MsgType)
            {
                case 1001:
                    PlayerMsg playerMsg = new PlayerMsg();
                    playerMsg.DeSerializeFormBytes(bytes);
                    Debug.Log(playerMsg.playerId);
                    Debug.Log(playerMsg.playerData.playerAge);
                    Debug.Log(playerMsg.playerData.playerHealth);
                    Debug.Log(playerMsg.playerData.playerName);
                    Debug.Log(playerMsg.playerData.playerSex);
                    break;
            }
        }
    }
}