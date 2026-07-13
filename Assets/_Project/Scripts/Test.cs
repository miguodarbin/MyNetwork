using System;
using System.Text;
using UnityEngine;

public class Test : MonoBehaviour
{
    private void Start()
    {
        Person p = new Person();
        p.age = 10;
        p.name = "Test";
        p.height = 3.14f;
        p.sex = true;
        byte[] bytes = p.SerializeMe();
    }
}


public class Person
{
    public int age;
    public string name;
    public float height;
    public bool sex;

    public byte[] SerializeMe()
    {
        //如果不用linq去拼接数组，那么的话就需要一开始就要定好数组的长度，否则到后面不好copy
        int finallyBytesCount = 0;
        finallyBytesCount += sizeof(int); //age的字节数组长度
        finallyBytesCount += sizeof(int); //记录name的字节数组长度的字节数组长度
        finallyBytesCount += Encoding.UTF8.GetBytes(name).Length; // name字节数组的长度
        finallyBytesCount += sizeof(float); //height的字节数组长度
        finallyBytesCount += sizeof(bool); //sex的字节数组长度
        //到这一步为止就得到了最终二进制序列化信息的那个字节数组的长度了，然后再得到这些成员变量的字节数组
        byte[] ageBytes = BitConverter.GetBytes(age);
        byte[] nameBytes = Encoding.UTF8.GetBytes(name);
        byte[] nameBytesCountBytes = BitConverter.GetBytes(nameBytes.Length);
        byte[] heightBytes = BitConverter.GetBytes(height);
        byte[] sexBytes = BitConverter.GetBytes(sex);
        //然后把这些数组都填入到最终的数组中，首先先创建一个最终的数组，长度就是刚才得到的长度
        byte[] finallyBytes = new byte[finallyBytesCount];
        int bytesIndex = 0;
        ageBytes.CopyTo(finallyBytes, bytesIndex);
        bytesIndex += sizeof(int);

        nameBytesCountBytes.CopyTo(finallyBytes, bytesIndex);
        bytesIndex += sizeof(int);

        nameBytes.CopyTo(finallyBytes, bytesIndex);
        bytesIndex += Encoding.UTF8.GetBytes(name).Length;

        heightBytes.CopyTo(finallyBytes, bytesIndex);
        bytesIndex += sizeof(float);
        sexBytes.CopyTo(finallyBytes, bytesIndex);
        
        return finallyBytes;
    }
}