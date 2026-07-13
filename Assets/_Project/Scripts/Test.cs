using System;
using System.Text;
using UnityEngine;

public class Test : MonoBehaviour
{
    private void Start()
    {
        Person p = new Person(88, new Dog("旺财", 2), "老炮", 3.14f, true);
        byte[] pBytes = p.GetAllFieldBytes();
    }
}


public class Person : CanBinarySerialize
{
    public int age;
    public Dog dog;
    public string name;
    public float height;
    public bool sex;

    public Person(int age, Dog dog, string name, float height, bool sex)
    {
        this.age = age;
        this.dog = dog;
        this.name = name;
        this.height = height;
        this.sex = sex;
    }

    public override int GetAllFieldBytesLength()
    {
        int dogBytesCount = dog.GetAllFieldBytesLength();
        int stringBytesCount = GetStringFieldAllCount(name);
        int allCount = sizeof(int) + dogBytesCount + stringBytesCount + sizeof(float) + sizeof(bool);
        return allCount;
    }

    protected override void WriteInAllFieldBytes()
    {
        WriteIntFieldToAllFieldBytes(age);
        WriteCustomFieldToAllFieldBytes(dog);
        WriteStringFieldToAllFieldBytes(name);
        WriteFloatFieldToAllFieldBytes(height);
        WriteBoolFieldToAllFieldBytes(sex);
    }
}

public class Dog : CanBinarySerialize
{
    public string name;
    public int age;

    public Dog(string name, int age)
    {
        this.name = name;
        this.age = age;
    }

    public override int GetAllFieldBytesLength()
    {
        int nameBytesAllCount = GetStringFieldAllCount(name);
        int allCount = nameBytesAllCount + sizeof(int);
        return allCount;
    }

    protected override void WriteInAllFieldBytes()
    {
        WriteStringFieldToAllFieldBytes(name);
        WriteIntFieldToAllFieldBytes(age);
    }
}