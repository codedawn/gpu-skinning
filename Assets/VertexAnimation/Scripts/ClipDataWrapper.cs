using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class ClipDataWrapper
{
    public List<ClipData> clipDataList = new List<ClipData>();

    public ClipDataWrapper(List<ClipData> clipDataList)
    {
        this.clipDataList = clipDataList;
    }

    public static ClipDataWrapper CreateClipDataFromJson(String path)
    {
        ClipDataWrapper clipDataWrapper = null;
        string jsonString = "";
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
                jsonString = File.ReadAllText(path);
                break;

            case RuntimePlatform.Android:
                jsonString = LoadJson(path);
                break;

            case RuntimePlatform.IPhonePlayer:
                jsonString = LoadJson(path);
                break;
        }

        if (jsonString != null)
        {
            clipDataWrapper = JsonUtility.FromJson<ClipDataWrapper>(jsonString);
        }
        else
        {
            Debug.LogError("Failed to load JSON file.");
        }
        return clipDataWrapper;

    }

    public static string LoadJson(string uri)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(uri))
        {
            request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error: " + request.error);
            }
            else
            {
                // 成功读取 JSON 字符串
                string jsonString = request.downloadHandler.text;
                return jsonString;
            }
        }
        return "";
    }
}