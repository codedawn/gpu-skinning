using System;
using System.IO;
using UnityEngine;

[Serializable] 
public class ClipData
{
    public string name;
    //动画时长
    public float length;
    public int frameCount;
    [NonSerialized]
    public Texture2D posTex;
    [NonSerialized]
    public Texture2D norTex;
    //占大图的百分比
    public float amount;
    public float offset;

    public ClipData(string name, float length, int frameCount, Texture2D posTex, Texture2D norTex)
    {
        this.name = name;
        this.length = length;
        this.frameCount = frameCount;
        this.posTex = posTex;
        this.norTex = norTex;
    }

    public ClipData Clone()
    {
        return new ClipData(name, length, frameCount, posTex, norTex);
    }
}