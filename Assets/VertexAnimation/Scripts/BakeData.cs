#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BakeData
{
    public List<ClipData> clipDataList = new List<ClipData>();
    public Strategy strategy = Strategy.Array;
    public TextureArr posT2Arr;
    public TextureArr norT2Arr;

    private string outPath;
    private string posPath;
    private string norPath;
    private string matPath;
    private string prbPath;
    private string posPngPath;
    private string norPngPath;
    private string jsonPath;


    public BakeData(string outPath)
    {
        this.outPath = outPath;

        this.posPath = Path.Combine(outPath, "Position/");
        this.norPath = Path.Combine(outPath, "Normal/");
        this.matPath = Path.Combine(outPath, "Mat/");
        this.prbPath = Path.Combine(outPath, "Prefab/");
        this.posPngPath = Path.Combine(outPath, "PositionPng/");
        this.norPngPath = Path.Combine(outPath, "NormalPng/");
        this.jsonPath = Path.Combine(Application.streamingAssetsPath + "/VertexAmim/Bake", "Json/");
    }

    public void AddClip(ClipData clipData)
    {
        clipDataList.Add(clipData);
    }

    public int GetLength()
    {
        return clipDataList.Count;
    }

    private ClipData GetTextureBeforeEdge(ClipData clipData)
    {
        Texture2D sourcePos = clipData.posTex;
        Texture2D sourceNor = clipData.norTex;
        Texture2D posT = new Texture2D(clipData.posTex.width, 5, clipDataList[0].posTex.format, false);
        Texture2D norT = new Texture2D(clipData.norTex.width, 5, clipDataList[0].posTex.format, false);

        Graphics.CopyTexture(sourcePos, 0, 0, 0, sourcePos.height - 5, sourcePos.width, 5, posT, 0, 0, 0, 0);
        Graphics.CopyTexture(sourceNor, 0, 0, 0, sourceNor.height - 5, sourceNor.width, 5, norT, 0, 0, 0, 0);
        ClipData clipDataAfter = new ClipData("Interval", 1, 5, posT, norT);
        return clipDataAfter;
    }

    private ClipData GetTextureAfterEdge(ClipData clipData)
    {
        Texture2D sourcePos = clipData.posTex;
        Texture2D sourceNor = clipData.norTex;
        Texture2D posT = new Texture2D(clipData.posTex.width, 5, clipDataList[0].posTex.format, false);
        Texture2D norT = new Texture2D(clipData.norTex.width, 5, clipDataList[0].posTex.format, false);

        Graphics.CopyTexture(sourcePos, 0, 0, 0, 0, sourcePos.width, 5, posT, 0, 0, 0, 0);
        Graphics.CopyTexture(sourceNor, 0, 0, 0, 0, sourceNor.width, 5, norT, 0, 0, 0, 0);
        ClipData clipDataAfter = new ClipData("Interval", 1, 5, posT, norT);
        return clipDataAfter;
    }

    public void SaveAsAsset(GameObject prefab)
    {
        if (strategy == Strategy.Sigle)
        {
            CreateFolder(posPath);
            AssetDatabase.CreateAsset(clipDataList[0].posTex, Path.Combine(posPath, clipDataList[0].posTex.name + ".asset"));

            CreateFolder(norPath);
            AssetDatabase.CreateAsset(clipDataList[0].norTex, Path.Combine(norPath, clipDataList[0].norTex.name + ".asset"));
        }
        else
        {
            List<ClipData> tmpList = new List<ClipData>();

            for (int i = 0; i < clipDataList.Count; i++)
            {
                if (i == 0)
                {
                    tmpList.Add(clipDataList[i]);
                    tmpList.Add(GetTextureAfterEdge(clipDataList[i]));
                }
                else if (i == clipDataList.Count - 1)
                {

                    tmpList.Add(GetTextureBeforeEdge(clipDataList[i]));
                    tmpList.Add(clipDataList[i]);
                }
                else
                {
                    tmpList.Add(GetTextureBeforeEdge(clipDataList[i]));
                    tmpList.Add(clipDataList[i]);
                    tmpList.Add(GetTextureAfterEdge(clipDataList[i]));
                }

                //tmpList.Add(clipDataList[i].Clone());

            }
            clipDataList.Clear();
            clipDataList.AddRange(tmpList);

            Texture2D pt = clipDataList[0].posTex;
            Texture2D nt = clipDataList[0].norTex;

            int height = 0;
            for (int i = 0; i < clipDataList.Count; i++)
            {
                height += clipDataList[i].frameCount;
            }

            posT2Arr = new TextureArr(pt.width, height, pt.format, false, pt.filterMode, pt.wrapMode);

            for (int i = 0; i < clipDataList.Count; i++)
            {
                posT2Arr.AddTexture(clipDataList[i].posTex);
            }
            posT2Arr.Apply();

            norT2Arr = new TextureArr(nt.width, height, nt.format, false, nt.filterMode, nt.wrapMode);
            for (int i = 0; i < clipDataList.Count; i++)
            {
                norT2Arr.AddTexture(clipDataList[i].norTex);
            }
            norT2Arr.Apply();

            CreateFolder(posPath);
            AssetDatabase.CreateAsset(posT2Arr.GetTexture2D(), Path.Combine(posPath, prefab.name + "PosT2DArr" + ".asset"));

            CreateFolder(norPath);
            AssetDatabase.CreateAsset(norT2Arr.GetTexture2D(), Path.Combine(norPath, prefab.name + "NorT2DArr" + ".asset"));

            float amountSum = 0;
            for (int i = 0; i < clipDataList.Count; i++)
            {
                clipDataList[i].amount = (float)(clipDataList[i].frameCount / (1.0 * height));
                clipDataList[i].offset = amountSum;
                amountSum += clipDataList[i].amount;
            }
        }

        SavaData(prefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public void SaveAsPrefab(GameObject prefab, Shader animMapShader)
    {
        var mat = SaveAsMat(prefab, animMapShader);

        if (mat == null)
        {
            EditorUtility.DisplayDialog("err", "mat is null!!", "OK");
            return;
        }

        var go = new GameObject();
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.AddComponent<MeshFilter>().sharedMesh = prefab.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;

        CreateFolder(prbPath);
        PrefabUtility.SaveAsPrefabAsset(go, Path.Combine(prbPath, $"{prefab.name}.prefab").Replace("\\", "/"));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        GameObject.DestroyImmediate(go);
    }

    public Material SaveAsMat(GameObject prefab, Shader animMapShader)
    {
        SaveAsAsset(prefab);

        if (animMapShader == null)
        {
            EditorUtility.DisplayDialog("err", "shader is null!!", "OK");
            return null;
        }

        if (prefab == null || !prefab.GetComponentInChildren<SkinnedMeshRenderer>())
        {
            EditorUtility.DisplayDialog("err", "SkinnedMeshRender is null!!", "OK");
            return null;
        }
        var smr = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
        var mat = new Material(animMapShader);

        if (strategy == Strategy.Sigle)
        {
            mat.SetTexture("_MainTex", smr.sharedMaterial.mainTexture);
            mat.SetTexture("_PosTex", clipDataList[0].posTex);
            mat.SetTexture("_NmlTex", clipDataList[0].norTex);
            mat.SetFloat("_Length", clipDataList[0].length);
            mat.SetFloat("_Amount", clipDataList[0].amount);
            mat.SetFloat("_Offset", 0);
        }
        else
        {
            mat.SetTexture("_MainTex", smr.sharedMaterial.mainTexture);
            mat.SetTexture("_PosTex", posT2Arr.GetTexture2D());
            mat.SetTexture("_NmlTex", norT2Arr.GetTexture2D());
            mat.SetFloat("_Length", clipDataList[0].length);
            mat.SetFloat("_Amount", clipDataList[0].amount);
            mat.SetFloat("_Offset", 0);
        }
        CreateFolder(matPath);
        AssetDatabase.CreateAsset(mat, Path.Combine(matPath, $"{prefab.name}.mat"));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return mat;
    }

    public void SaveAsPng()
    {
        SaveAsPNG(clipDataList[0].posTex, posPngPath);
        SaveAsPNG(clipDataList[0].norTex, norPngPath);
    }

    private void SaveAsPNG(Texture2D tx, string dir)
    {
        var bytes = tx.EncodeToJPG();
        CreateFolder(dir);

        var file = File.Open(dir + "/" + tx.name + ".png", FileMode.Create);
        var writer = new BinaryWriter(file);
        writer.Write(bytes);
        file.Close();
        writer.Close();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public void SavaData(GameObject prefab)
    {
        ClipDataWrapper clipDataWrapper = new ClipDataWrapper(this.clipDataList);
        CreateFolder(jsonPath);
        var file = File.Open(jsonPath + "/" + prefab.name + ".json", FileMode.Create);
        var writer = new StreamWriter(file);
        string json = JsonUtility.ToJson(clipDataWrapper);
        writer.Write(json);

        writer.Close();
        file.Close();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void CreateFolder(string path)
    {
        // 获取父目录的路径
        string directoryPath = Path.GetDirectoryName(path.Replace("\\", "/"));

        // 检查目录是否存在
        if (!Directory.Exists(directoryPath))
        {
            // 如果不存在，创建目录
            Directory.CreateDirectory(directoryPath);
            AssetDatabase.Refresh(); // 刷新AssetDatabase以更新Unity编辑器中的文件和文件夹列表
        }
    }
}
#endif

public enum Strategy
{
    Sigle,
    Array,
}