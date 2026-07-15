using System;
using System.Text;
using UnityEngine;

public class Test : MonoBehaviour
{
    private void Start()
    {
        Person p = new Person(88, new Dog("旺财", 2), "老炮", 3.14f, true);
        byte[] pBytes = p.SerializeToBytes();
        Person p2 = new Person();
        p2.DeSerializeFormBytes(pBytes);

        Debug.Assert(p2.age == p.age);
        Debug.Assert(p2.name == p.name);
        Debug.Assert(p2.height == p.height);
        Debug.Assert(p2.sex == p.sex);
        Debug.Assert(p2.dog.name == p.dog.name);
        Debug.Assert(p2.dog.age == p.dog.age);
    }
}


public class Person : CanBinarySerialize<Person>
{
    public int age;
    public Dog dog;
    public string name;
    public float height;
    public bool sex;

    public Person()
    {
    }

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
        int dogBytesCount = GetCustomFieldAllCount<Dog>(dog);
        int stringBytesCount = GetStringFieldAllCount(name);
        int allCount = sizeof(int) + dogBytesCount + stringBytesCount + sizeof(float) + sizeof(bool);
        return allCount;
    }

    protected override void WriteInAllFieldBytes()
    {
        WriteIntFieldToAllFieldBytes(age);
        WriteCustomFieldToAllFieldBytes<Dog>(dog);
        WriteStringFieldToAllFieldBytes(name);
        WriteFloatFieldToAllFieldBytes(height);
        WriteBoolFieldToAllFieldBytes(sex);
    }

    protected override void ReadFromAllFieldBytes()
    {
        age = ReadAllFieldBytesToIntField();
        dog = ReadAllFieldBytesToCustomField<Dog>();
        name = ReadAllFieldBytesToStringField();
        height = ReadAllFieldBytesToFloatField();
        sex = ReadAllFieldBytesToBoolField();
    }
}

public class Dog : CanBinarySerialize<Dog>
{
    public string name;
    public int age;

    public Dog()
    {
    }

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

    protected override void ReadFromAllFieldBytes()
    {
        name = ReadAllFieldBytesToStringField();
        age = ReadAllFieldBytesToIntField();
    }
}