using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
    public Button sendInvalidButtonClick;

    private void OnEnable()
    {
        stringButton.onClick.AddListener(OnStringButtonClick);
        sendCustomTypeButton.onClick.AddListener(OnSendCustomTypeButtonClick);
        sendFenBaoButton.onClick.AddListener(OnSendFenBaoButtonClick);
        sendNianBaoButton.onClick.AddListener(OnSendNianBaoButtonClick);
        sendFenBaoNianBaoButton.onClick.AddListener(OnSendFenBaoNianBaoButtonClick);
        sendInvalidButtonClick.onClick.AddListener(OnSendInvalidButtonClick);
    }

    private void OnDisable()
    {
        stringButton.onClick.RemoveListener(OnStringButtonClick);
        sendCustomTypeButton.onClick.RemoveListener(OnSendCustomTypeButtonClick);
        sendFenBaoButton.onClick.RemoveListener(OnSendFenBaoButtonClick);
        sendNianBaoButton.onClick.RemoveListener(OnSendNianBaoButtonClick);
        sendFenBaoNianBaoButton.onClick.RemoveListener(OnSendFenBaoNianBaoButtonClick);
        sendInvalidButtonClick.onClick.RemoveListener(OnSendInvalidButtonClick);
    }

    private void OnStringButtonClick()
    {
        //向服务器发送string
        StringMsg stringMsg = new StringMsg(inputField.text);
        NetworkManager.Instance.SendFrameToServer(stringMsg.SerializeToFrameBytes());
    }

    private void OnSendCustomTypeButtonClick()
    {
        //向服务器发送自定义消息类型
        PlayerMsg playerMsg = new PlayerMsg(1, new PlayerData("老炮", 99.8f, 18, true));
        NetworkManager.Instance.SendFrameToServer(playerMsg.SerializeToFrameBytes());
    }

    private void OnSendFenBaoButtonClick()
    {
        StringMsg msg = new StringMsg("黏包的String");
        byte[] nianBaoBytes = msg.SerializeToFrameBytes();
        byte[] first = nianBaoBytes[0..8];
        byte[] second = nianBaoBytes[8..];
        NetworkManager.Instance.SendFrameToServer(first);
        Thread.Sleep(1000);
        NetworkManager.Instance.SendFrameToServer(second);
        Console.WriteLine("已发送");
    }

    private void OnSendNianBaoButtonClick()
    {
        StringMsg msg = new StringMsg("黏包的String");
        StringMsg msg2 = new StringMsg("NianBaoのString");
        byte[] nianBaoBytes = msg.SerializeToFrameBytes().Concat(msg2.SerializeToFrameBytes()).ToArray();
        NetworkManager.Instance.SendFrameToServer(nianBaoBytes);
    }

    private void OnSendFenBaoNianBaoButtonClick()
    {
        StringMsg msg = new StringMsg("黏包的String");
        StringMsg msg2 = new StringMsg("NianBaoのString");
        byte[] nianBaoBytes = msg.SerializeToFrameBytes().Concat(msg2.SerializeToFrameBytes()).ToArray();

        byte[] first = nianBaoBytes[0..25];
        byte[] second = nianBaoBytes[25..];

        NetworkManager.Instance.SendFrameToServer(first);
        Thread.Sleep(1000);
        NetworkManager.Instance.SendFrameToServer(second);
        Console.WriteLine("已发送");
    }

    private void OnSendInvalidButtonClick()
    {
        NetworkManager.Instance.SendFrameToServer(new byte[]
        {
            0xE8, 0x03, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00,
            0xFF, 0xFF, 0xFF, 0xFF
        });
    }

    void Update()
    {
        //接收消息
        if (NetworkManager.Instance.TryDequeueReceivedFrame(out byte[] frameBytes))
        {
            try
            {
                //目前字节有这些情况:
                int MsgType = BitConverter.ToInt32(frameBytes, 0);

                switch (MsgType)
                {
                    case 1000:
                        StringMsg stringMsg = new StringMsg();
                        stringMsg.DeSerializeFormFrameBytes(frameBytes);
                        Debug.Log(stringMsg.msg);
                        break;

                    case 1001:
                        PlayerMsg playerMsg = new PlayerMsg();
                        playerMsg.DeSerializeFormFrameBytes(frameBytes);
                        Debug.Log(playerMsg.playerId);
                        Debug.Log(playerMsg.playerData.playerAge);
                        Debug.Log(playerMsg.playerData.playerHealth);
                        Debug.Log(playerMsg.playerData.playerName);
                        Debug.Log(playerMsg.playerData.playerSex);
                        break;
                }
            }
            catch (InvalidDataException e)
            {
                // 完整帧已经通过外层分帧，
                // 但消息体内部的字段长度不符合协议。
                Debug.LogError(
                    $"收到非法协议消息，关闭服务器连接：{e.Message}"
                );

                // 当前连接已经不可信，不再继续处理业务。
                NetworkManager.Instance.CloseConnection();
            }
        }
    }
}