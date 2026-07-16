using System;
using System.Collections;
using System.Collections.Generic;
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

    private void OnEnable()
    {
        stringButton.onClick.AddListener(OnStringButtonClick);
        sendCustomTypeButton.onClick.AddListener(OnSendCustomTypeButtonClick);
        sendFenBaoButton.onClick.AddListener(OnSendFenBaoButtonClick);
        sendNianBaoButton.onClick.AddListener(OnSendNianBaoButtonClick);
        sendFenBaoNianBaoButton.onClick.AddListener(OnSendFenBaoNianBaoButtonClick);
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
        StringMsg stringMsg = new StringMsg(inputField.text);
        NetworkManager.Instance.SendBytesToServer(stringMsg.SerializeToBytes());
    }

    private void OnSendCustomTypeButtonClick()
    {
        //向服务器发送自定义消息类型
        PlayerMsg playerMsg = new PlayerMsg(1, new PlayerData("老炮", 99.8f, 18, true));
        NetworkManager.Instance.SendBytesToServer(playerMsg.SerializeToBytes());
    }

    private void OnSendFenBaoButtonClick()
    {
        StringMsg msg = new StringMsg("黏包的String");
        byte[] nianBaoBytes = msg.SerializeToBytes();
        byte[] first = nianBaoBytes[0..8];
        byte[] second = nianBaoBytes[8..];
        NetworkManager.Instance.SendBytesToServer(first);
        Thread.Sleep(1000);
        NetworkManager.Instance.SendBytesToServer(second);
        Console.WriteLine("已发送");
    }

    private void OnSendNianBaoButtonClick()
    {
        StringMsg msg = new StringMsg("黏包的String");
        StringMsg msg2 = new StringMsg("NianBaoのString");
        byte[] nianBaoBytes = msg.SerializeToBytes().Concat(msg2.SerializeToBytes()).ToArray();
        NetworkManager.Instance.SendBytesToServer(nianBaoBytes);
    }

    private void OnSendFenBaoNianBaoButtonClick()
    {
        Debug.Log("Send FenBaoNianBao");
    }

    void Update()
    {
        //接收消息
        if (NetworkManager.Instance.IsHaveServerSendMsg())
        {
            //将字节取出来。
            byte[] bytes = NetworkManager.Instance.GetServerSendMsg();
            
            //目前字节有这些情况:
            int MsgType = BitConverter.ToInt32(bytes, 0);
            
            switch (MsgType)
            {
                case 1000:
                    StringMsg stringMsg = new StringMsg();
                    stringMsg.DeSerializeFormBytes(bytes);
                    Debug.Log(stringMsg.msg);
                    break;

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
        
        /*
         *
         *******************************************************************************************
         * 成员变量：缓存区List<byte>：不管 37二十一，拿到了客户端的字节就往里面放
         * 成员变量：成功区Queue<byte[]>：被 ProcessTCPStream 处理过的字节才往里面放，到时候单开一个任务正常读取这个成功区的字节进行解析
         *
         *******************************************************************************************
         * 然后就这么进行判断：
         *
         *
         * 不断的接收客户端的发来的字节，然后放到缓存区里。只要一有内容放到缓存区就调用ProcessTCPStream()
         *
         *
         * ProcessTCPStream(){
         *
         *
         * 循环判断：缓存区有字节就进入循环，无字节就退出循环
         *
         *
         * 局部变量：游标：处理到缓存区的哪里了？
         *
         * 如果缓存区里有字节，说明有消息，且游标在消息头最开始，可以正常解析。
         * 判断一下缓存区里的字节长度是否大于消息头的长度，如果缓存区的字节长度大于等于消息头的长度，那就说明可以继续解析消息头的具体内容,此时游标 +8，解析得到消息类型和消息体长度
         * 拿缓存区的总个数-目前游标位置得到缓存区剩余的字节数，与刚才解析出来的消息体长度比较，如果缓存区剩余的字节数大于消息体长度，说明粘包了。
         * 然后把从0到对象字节长度那么多的字节从缓存区拿走，加入成功区。，此时游标指向粘包消息的头部，不用打破循环，继续循环走逻辑，游标置零，让游标指向消息头，一直走到打破循环的分支上去。
         *
         *
         * 如果缓存区里有字节，说明有消息，且游标在消息头最开始，可以正常解析。
         * 判断一下缓存区里的字节长度是否大于消息头的长度，如果缓存区的字节长度大于等于消息头的长度，那就说明可以继续解析消息头的具体内容,此时游标 +8，解析得到消息类型和消息体长度
         * 拿缓存区的总个数-目前游标位置得到缓存区剩余的字节数，与刚才解析出来的消息体长度比较，如果缓存区剩余的字节数等于消息体长度，说明没有发生粘包分包。
         * ，然后把从0到对象字节长度那么多的字节从缓存区拿走，加入成功区，打破循环，因为缓存区再拿完这条消息之后就没字节了。
         *
         * 游标
         * 如果缓存区里有字节，说明有消息，，可以正常解析。
         * 判断一下缓存区里的字节长度是否大于消息头的长度，如果缓存区的字节长度大于等于消息头的长度，那就说明可以继续解析消息头的具体内容,此时游标 +8，解析得到消息类型和消息体长度
         * 拿缓存区的总个数-目前游标位置得到缓存区剩余的字节数，与刚才解析出来的消息体长度比较，如果缓存区剩余的字节数小于消息体长度，说明发生分包了。
         * 此时不应该继续处理了，直接打破循环，等之后的再发消息过来，再来判断能不能处理。
         *
         *
         * 如果缓存区里有字节，说明有消息，，可以正常解析。
         * 判断一下缓存区里的字节长度是否大于消息头的长度，如果缓存区的字节长度小于消息头的长度，那也不进行处理，直接打破循环，等之后的再发消息过来，再来判断能不能处理。
         *
         *}
         */
    }
}