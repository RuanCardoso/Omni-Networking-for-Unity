using System;
using Omni.Inspector;
using UnityEngine;

#pragma warning disable

public class Styling_TitleSample : ScriptableObject
{
    [Title("My Title")]
    public string val;

    [Title("$" + nameof(_myTitleField))]
    public Rect rect;

    [Title("$" + nameof(MyTitleProperty))]
    public Vector3 vec;

    [Title("Button Title")]
    [Button]
    public void MyButton()
    {
    }

    private string _myTitleField = "Serialized Title";

    private string MyTitleProperty => DateTime.Now.ToLongTimeString();
}