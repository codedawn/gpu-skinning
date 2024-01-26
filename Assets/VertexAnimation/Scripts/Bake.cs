#if UNITY_EDITOR 
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.Animations;   
using UnityEngine;

public class Bake
{
    public GameObject gameObject;
    private string outPath;
    private string prefix = "Bake";
    private SaveStrategy strategy = SaveStrategy.Prefab;
    private Shader animMapShader;
    public ComputeShader computeShader;

    public Bake(string outPath, Shader animMapShader, ComputeShader computeShader)
    {
        this.outPath = Path.Combine(outPath, prefix);
        this.computeShader = computeShader;
        this.animMapShader = animMapShader;
    }

    AnimationClip GetAnimationClipFromStateName(Animator animator, string stateName)
    {
        AnimatorController ac = animator.runtimeAnimatorController as AnimatorController;
        if (ac == null)
            return null;

        foreach (var layer in ac.layers)
        {
            foreach (var state in layer.stateMachine.states)
            {
                if (state.state.name == stateName)
                {
                    Motion motion = state.state.motion;
                    if (motion is AnimationClip)
                    {
                        return motion as AnimationClip;
                    }
                    else if (motion is BlendTree)
                    {
                        // 如果是BlendTree，你需要决定如何处理这种情况
                        // 例如遍历BlendTree中的所有子动画片段
                    }
                }
            }
        }

        return null; // 如果没有找到，返回null
    }

    public IEnumerator BakeVertexTexture(GameObject gameObject)
    {
        this.gameObject = gameObject;
        var animator = gameObject.GetComponent<Animator>();
        var clips = animator.runtimeAnimatorController.animationClips;
        var skin = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
        var vertCount = skin.sharedMesh.vertexCount;

        var mesh = new Mesh();
        animator.speed = 1;
        var textWidth = vertCount;
        AnimatorController ac = animator.runtimeAnimatorController as AnimatorController;
        BakeData bakeData = new BakeData(outPath);

        foreach (var layer in ac.layers)
        {
            foreach (var state in layer.stateMachine.states)
            {
                var clip = GetAnimationClipFromStateName(animator, state.state.name);
                var frameCount = (int)(clip.length * clip.frameRate);
                Debug.Log(state.state.name + " frames:" + frameCount);
                var vertexs = new List<VertexInfo>();

                var positionRenderTexture =
                    new RenderTexture(textWidth, frameCount, 0, RenderTextureFormat.ARGBHalf);
                var normalRenderTexture =
                    new RenderTexture(textWidth, frameCount, 0, RenderTextureFormat.ARGBHalf);

                positionRenderTexture.name = string.Format("{0}-{1}.position", gameObject.name, clip.name);
                normalRenderTexture.name = string.Format("{0}-{1}.normal", gameObject.name, clip.name);

                foreach (var renderTexture in new[] { positionRenderTexture, normalRenderTexture })
                {
                    renderTexture.enableRandomWrite = true;
                    renderTexture.Create();
                    RenderTexture.active = renderTexture;
                    GL.Clear(true, true, Color.clear);
                }
                animator.Play(state.state.name);
                yield return 0;
                for (var i = 0; i < frameCount; ++i)
                {
                    animator.Play(state.state.name, 0, (float)i / frameCount);
                    yield return 0;
                    skin.BakeMesh(mesh);
                    vertexs.AddRange(Enumerable.Range(0, vertCount).Select(idx => new VertexInfo
                    {
                        position = mesh.vertices[idx],
                        normal = mesh.normals[idx]
                    }));
                }

                var buffer = new ComputeBuffer(vertexs.Count, Marshal.SizeOf(typeof(VertexInfo)));
                buffer.SetData(vertexs);

                var kernelID = computeShader.FindKernel("CSMain");

                computeShader.SetInt("vertCount", vertCount);
                computeShader.SetBuffer(kernelID, "meshInfo", buffer);
                computeShader.SetTexture(kernelID, "OutPositionTexture", positionRenderTexture);
                computeShader.SetTexture(kernelID, "OutNormalTexture", normalRenderTexture);

                computeShader.Dispatch(kernelID, vertCount, frameCount, 1);
                buffer.Release();

#if UNITY_EDITOR

                var pt = Convert(positionRenderTexture);
                var nt = Convert(normalRenderTexture);

                Graphics.CopyTexture(positionRenderTexture, pt);
                Graphics.CopyTexture(normalRenderTexture, nt);

                bakeData.AddClip(new ClipData(clip.name, clip.length, frameCount, pt, nt));
#endif
            }
        }
        Save(bakeData);
        Debug.Log("烘培结束！");
        yield return null;
    }

    public static Texture2D Convert(RenderTexture rt)
    {
        var texture2D = new Texture2D(rt.width, rt.height, TextureFormat.RGBAHalf, false);
        RenderTexture.active = rt;
        texture2D.ReadPixels(Rect.MinMaxRect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = null;
        texture2D.name = rt.name;
        return texture2D;
    }

    private void Save(BakeData bakeData)
    {
        switch (strategy)
        {
            case SaveStrategy.Asset:
                bakeData.SaveAsAsset(gameObject);
                break;
            case SaveStrategy.Mat:
                bakeData.SaveAsMat(gameObject, animMapShader);
                break;
            case SaveStrategy.Prefab:
                bakeData.SaveAsPrefab(gameObject, animMapShader);
                break;
        }
    }
}

public enum SaveStrategy
{
    Asset,
    Mat,
    Prefab
}

internal struct VertexInfo
{
    public Vector3 position;
    public Vector3 normal;
}
#endif
