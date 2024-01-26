using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.EditorCoroutines.Editor;
using UnityEditor.SceneManagement;

public class AnimBakeWindows : EditorWindow
{
    private const string animMapShaderName = "Custom/VertexAnimationShader";
    private static string path = "Assets/VertexAnimation/Demo";
    private static Shader animMapShader;
    public static ComputeShader computeShader;
    private static string savePath = "Assets/VertexAnimation/VertexAmim";
    private Vector2 scrollPosition;
    private bool foldOut;

    private readonly List<GameObject> prefabList = new List<GameObject>();


    [MenuItem("Window/AnimMapBaker")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(AnimBakeWindows));
        animMapShader = Shader.Find(animMapShaderName);
        computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(PlayerPrefs.GetString("MeshInfoTextureBaker"));
    }

    private void OnEnable()
    {
        path = PlayerPrefs.GetString("AnimBakePrefabPath");
        if(PlayerPrefs.GetString("savePath") != "")
        {
            savePath = PlayerPrefs.GetString("savePath");
        }
        InitPrefabs();
    }

    private void InitPrefabs()
    {
        prefabList.Clear();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { path });
        foreach (var id in guids)
        {
            prefabList.Add(GetGameObjectFromGUID(id));
        }
    }

    public GameObject GetGameObjectFromGUID(string guid)
    {
        // 将GUID转换为资源路径
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogError("未找到对应GUID的资源。");
            return null;
        }

        // 加载资源
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogError("加载路径的资源不是GameObject。");
            return null;
        }

        // 实例化Prefab
        //GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        //if (instance == null)
        //{
        //    Debug.LogError("无法实例化Prefab。");
        //    return null;
        //}

        return prefab;
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("预制体路径:", path);
        //记录上次选择目录

        if (GUILayout.Button("选择"))
        {
            string folderPath = PlayerPrefs.GetString("AnimBakePrefabPath");
            string searchPath = EditorUtility.OpenFolderPanel("select path", folderPath, "");
            if (searchPath != "")
            {
                searchPath = searchPath.Replace(Application.dataPath, "Assets");//全路径，要截断回AssetDatabase所需的相对路径
                PlayerPrefs.SetString("AnimBakePrefabPath", searchPath);
                PlayerPrefs.Save();
                path = searchPath;
                InitPrefabs();
            }
        }
        EditorGUILayout.EndHorizontal();

        foldOut = EditorGUILayout.BeginFoldoutHeaderGroup(foldOut, "预制体");
        if (foldOut) //只有foldout为true时，才会显示下方内容，相当于“折叠”了。
        {
            GUI.enabled = false;
            EditorGUILayout.LabelField("下面是要烘培的预制体");
            foreach (var gb in prefabList)
            {
                EditorGUILayout.ObjectField(gb, typeof(GameObject), true);
            }
            GUI.enabled = true;
        }
        EditorGUILayout.EndFoldoutHeaderGroup(); //只不过这种折叠需要成对使用，不然会有BUG

        //scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        //GUILayout.EndScrollView();

        EditorGUILayout.LabelField("加速烘培的shader(默认是MeshInfoTextureBaker)：");
        computeShader = (ComputeShader)EditorGUILayout.ObjectField(computeShader, typeof(ComputeShader), false);
        if (computeShader != null)
        {
            PlayerPrefs.SetString("MeshInfoTextureBaker", AssetDatabase.GetAssetPath(computeShader));
        }

        EditorGUILayout.LabelField("顶点动画shader：");
        animMapShader = (Shader)EditorGUILayout.ObjectField(animMapShader, typeof(Shader), false);

        EditorGUILayout.LabelField("保存根路径：");
        savePath = EditorGUILayout.TextField(savePath);
        PlayerPrefs.SetString("savePath", savePath);
        //EditorGUILayout.LabelField("跳转烘培scene后，点击play开始烘培");
        
        if (GUILayout.Button("开始烘培"))
        {
            foreach (var gb in prefabList)
            {
                GameObject instance = GameObject.Instantiate(gb);
                Bake bake = new Bake(savePath, animMapShader, computeShader);
                this.StartCoroutine(bake.BakeVertexTexture(instance));
                //EditorApplication.update += () => {
                //    animator.Update(Time.deltaTime); // 手动更新Animator
                //    i++;
                //    skin.BakeMesh(mesh);
                //    Debug.Log(mesh.ToString());
                //    animator.Play("Ani3D_Whitetipshark_Normal_SwimTurn_A_01", 0, (float)((i % 36) + 1) / 36);
                //};
                break;
            }
        }
    }
}