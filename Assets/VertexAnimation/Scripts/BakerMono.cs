#if UNITY_EDITOR
using UnityEngine;

public class BakerMono : MonoBehaviour
{
    public Bake bake;
    public string basePath = "Assets/VertexAnimation/VertexAnim";
    public Shader animMapShader;
    public ComputeShader computeShader;

    private void Awake()
    {
        bake = new Bake(basePath, animMapShader, computeShader);
        StartCoroutine(bake.BakeVertexTexture(gameObject));
    }
}
#endif
