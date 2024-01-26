using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SimpleSpawner : MonoBehaviour
{
    public GameObject go;
    public Transform camera;
    public float speed;
    public Text log;

    public int row;
    public int column;
    public ClipDataWrapper clipDataWrapper;
    //public Texture2D mainTexture;
    //public Texture2D posTexture;
    //public Texture2D nolTexture;
    private float deltaTime;

    private void Awake()
    {
        Application.targetFrameRate = 128;
    }

    // Start is called before the first frame update
    void Start()
    {
        bool result = SystemInfo.supports2DArrayTextures;
        clipDataWrapper = ClipDataWrapper.CreateClipDataFromJson(Application.streamingAssetsPath + "/VertexAmim/Bake/Json/" + go.name + ".json");
        Debug.Log(clipDataWrapper);
        MaterialPropertyBlock mpb1 = new MaterialPropertyBlock();
        MaterialPropertyBlock mpb2 = new MaterialPropertyBlock();

        List<ClipData> tmpList = new List<ClipData>();
        for (int i = 0; i < clipDataWrapper.clipDataList.Count; i++)
        {
            if(clipDataWrapper.clipDataList[i].name != "Interval")
            {
                tmpList.Add(clipDataWrapper.clipDataList[i]);
            }
        }

        clipDataWrapper = new ClipDataWrapper(tmpList);

        for (int r = 0; r < row; r++)
        {
            for (int c = 0; c < column; c++)
            {
                GameObject newGameObject = Instantiate(go);
                newGameObject.transform.position += new Vector3((float)r * 20f, 0, (float)c * 20f);
                var mr = newGameObject.GetComponent<MeshRenderer>();
                if(mr != null)
                {
                    int rd = Random.Range(0, clipDataWrapper.clipDataList.Count);
                    mr.GetPropertyBlock(mpb1);
                    //mpb1.SetTexture("_MainTex", mainTexture);
                    //mpb1.SetTexture("_PosTex", posTexture);
                    //mpb1.SetTexture("_NmlTex", nolTexture);
                    mpb1.SetFloat("_Length", clipDataWrapper.clipDataList[rd].length);
                    mpb1.SetFloat("_Amount", clipDataWrapper.clipDataList[rd].amount);
                    mpb1.SetFloat("_Offset", clipDataWrapper.clipDataList[rd].offset);
                    //mpb1.SetColor("_Color", Color.yellow);
                    mr.SetPropertyBlock(mpb1);
                }
            }
        }

        Destroy(go);
    }

    // Update is called once per frame
    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        log.text = $"FPS: {(int)fps}, {(int)(Time.deltaTime * 1000)} ms";
        ControllerCamera();
    }

    private void ControllerCamera()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Debug.Log("horizontal " + horizontal);

        camera.position += (new Vector3(horizontal, vertical, 0) * Time.deltaTime * speed);
    }
}
