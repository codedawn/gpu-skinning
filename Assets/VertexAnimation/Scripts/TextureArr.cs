using UnityEngine;

public class TextureArr 
{
    private Texture2D texture2D;

    private int curHeight;

    public TextureArr(int width, int height, TextureFormat textureFormat, bool mipChain, FilterMode filterMode, TextureWrapMode wrapMode)
    {
        texture2D = new Texture2D(width, height, textureFormat, mipChain);
        //texture2D.filterMode = filterMode;
        //texture2D.wrapMode = wrapMode;
        texture2D.filterMode = FilterMode.Bilinear;
        texture2D.wrapMode = TextureWrapMode.Clamp;
        curHeight = 0;
    }

    public void AddTexture(Texture2D source)
    {
        Graphics.CopyTexture(source, 0, 0, 0, 0, source.width, source.height, texture2D, 0, 0, 0, curHeight);
        curHeight = curHeight + source.height;
    }

    public void Apply()
    {
        texture2D.Apply();
    }

    public Texture2D GetTexture2D()
    {
        return texture2D;
    }
}