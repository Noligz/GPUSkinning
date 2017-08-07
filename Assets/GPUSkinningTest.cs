using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class GPUSkinningTest : MonoBehaviour
{
    public AnimationClip clip;
    public SkinnedMeshRenderer smr;
    public float targetFPS = 30f;

    [ContextMenu("CreateMatrixTexture")]
    public void CreateMatrixTexture()
    {
        var deltaTime = 1f / targetFPS;
        int curPixelIdx = 0;
        var boneCnt = smr.bones.Length;
        var matrixs = new Matrix4x4[boneCnt];
        int frameCnt = (int)(clip.length * targetFPS);
        int textureWidth = 1, textureHeight = 1;
        var pixelsPerMatrix = 3;
        CalMatrixTextureSize(boneCnt * frameCnt * pixelsPerMatrix, out textureWidth, out textureHeight);
        var texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAHalf, false, true);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = texture.GetPixels();
        for (float curTime = 0; curTime < clip.length; curTime += deltaTime)
        {
            for (int i = 0; i < boneCnt; i++)
            {
                matrixs[i] = GetBoneMatrixFromClip(clip, curTime, smr, i);
            }

            RecursiveMultiplyMatrix(smr.rootBone, smr, Matrix4x4.identity, matrixs);

            for (int i = 0; i < boneCnt; i++)
            {
                var m = matrixs[i];
                pixels[curPixelIdx++] = new Color(m.m00, m.m01, m.m02, m.m03);
                pixels[curPixelIdx++] = new Color(m.m10, m.m11, m.m12, m.m13);
                pixels[curPixelIdx++] = new Color(m.m20, m.m21, m.m22, m.m23);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();

        AssetDatabase.CreateAsset(texture, "Assets/Generated/AnimTexture.asset");

        AssetDatabase.Refresh();

        Debug.LogErrorFormat("PixelPerFrame = {0}, FrameCount = {1}", boneCnt * pixelsPerMatrix, frameCnt);
    }

    string GetBonePath(Transform bone, Transform root)
    {
        string ret = bone.name;
        var tran = bone;
        while(tran != root && tran.parent != null)
        {
            tran = tran.parent;
            ret = string.Format("{0}/{1}", tran.name, ret);
        }
        return ret;
    }

    int GetBoneIdx(Transform bone, Transform[] bones)
    {
        for (int i = 0, length = bones.Length; i < length; i++)
        {
            if (bone == bones[i])
                return i;
        }
        return -1;
    }

    Vector2 CalMatrixTextureSize(int pixelCnt, out int width, out int height)
    {
        width = 1;
        height = 1;
        var flag = true;
        while(width * height < pixelCnt)
        {
            if (flag)
                width *= 2;
            else
                height *= 2;
            flag = !flag;
        }
        return new Vector2(width, height);
    }

    Matrix4x4 GetBoneMatrixFromClip(AnimationClip clip, float time, SkinnedMeshRenderer smr, int boneIdx)
    {
        var b = smr.bones[boneIdx];
        var path = GetBonePath(b, smr.rootBone);
        var curveBinding = new EditorCurveBinding() { path = path, type = typeof(Transform) };

        curveBinding.propertyName = "m_LocalPosition.x";
        var posX = AnimationUtility.GetEditorCurve(clip, curveBinding).Evaluate(time);
        curveBinding.propertyName = "m_LocalPosition.y";
        var posY = AnimationUtility.GetEditorCurve(clip, curveBinding).Evaluate(time);
        curveBinding.propertyName = "m_LocalPosition.z";
        var posZ = AnimationUtility.GetEditorCurve(clip, curveBinding).Evaluate(time);
        var pos = new Vector3(posX, posY, posZ);

        curveBinding.propertyName = "m_LocalRotation.x";
        var rotX = AnimationUtility.GetEditorCurve(clip, curveBinding).Evaluate(time);
        curveBinding.propertyName = "m_LocalRotation.y";
        var rotY = AnimationUtility.GetEditorCurve(clip, curveBinding).Evaluate(time);
        curveBinding.propertyName = "m_LocalRotation.z";
        var rotZ = AnimationUtility.GetEditorCurve(clip, curveBinding).Evaluate(time);
        curveBinding.propertyName = "m_LocalRotation.w";
        var rotW = AnimationUtility.GetEditorCurve(clip, curveBinding).Evaluate(time);
        var tmpRot = new Vector4(rotX, rotY, rotZ, rotW);
        tmpRot = tmpRot.normalized;
        var rot = new Quaternion(tmpRot.x, tmpRot.y, tmpRot.z, tmpRot.w);

        curveBinding.propertyName = "m_LocalScale.x";
        var scaleX = AnimationUtility.GetEditorCurve(clip, curveBinding).Evaluate(time);
        curveBinding.propertyName = "m_LocalScale.y";
        var scaleY = AnimationUtility.GetEditorCurve(clip, curveBinding).Evaluate(time);
        curveBinding.propertyName = "m_LocalScale.z";
        var scaleZ = AnimationUtility.GetEditorCurve(clip, curveBinding).Evaluate(time);
        var scale = new Vector3(scaleX, scaleY, scaleZ);

        return Matrix4x4.TRS(pos, rot, scale);
    }

    void RecursiveMultiplyMatrix(Transform bone, SkinnedMeshRenderer smr, Matrix4x4 parentMatrix, Matrix4x4[] matrixs)
    {
        var bones = smr.bones;
        var boneIdx = GetBoneIdx(bone, bones);
        var m = parentMatrix * matrixs[boneIdx];
        for (int i = 0, length = bone.childCount; i < length; i++)
        {
            var child = bone.GetChild(i);
            RecursiveMultiplyMatrix(child, smr, m, matrixs);
        }
        matrixs[boneIdx] = m * smr.sharedMesh.bindposes[boneIdx];
    }

    private class BoneWeightPair : System.IComparable<BoneWeightPair>
    {
        public int index = 0;
        public float weight = 0;

        public int CompareTo(BoneWeightPair b)
        {
            return weight > b.weight ? -1 : 1;
        }
    }

    [ContextMenu("CreateMesh")]
    void CreateMesh()
    {
        var originMesh = smr.sharedMesh;
        var generateMesh = new Mesh();
        generateMesh.vertices = originMesh.vertices;
        generateMesh.uv = originMesh.uv;
        generateMesh.tangents = originMesh.tangents;
        generateMesh.triangles = originMesh.triangles;
        generateMesh.bounds = originMesh.bounds;
        generateMesh.colors = originMesh.colors;
        generateMesh.normals = originMesh.normals;

        List<Vector4> boneIdxWight01 = new List<Vector4>(originMesh.vertexCount);
        List<Vector4> boneIdxWight23 = new List<Vector4>(originMesh.vertexCount);
        for (int i = 0, length = originMesh.vertexCount; i < length; i++)
        {
            var boneWeight = originMesh.boneWeights[i];
            BoneWeightPair[] boneWeightPairs = new BoneWeightPair[4];
            boneWeightPairs[0] = new BoneWeightPair() { index = boneWeight.boneIndex0, weight = boneWeight.weight0 };
            boneWeightPairs[1] = new BoneWeightPair() { index = boneWeight.boneIndex1, weight = boneWeight.weight1 };
            boneWeightPairs[2] = new BoneWeightPair() { index = boneWeight.boneIndex2, weight = boneWeight.weight2 };
            boneWeightPairs[3] = new BoneWeightPair() { index = boneWeight.boneIndex3, weight = boneWeight.weight3 };
            System.Array.Sort(boneWeightPairs);

            boneIdxWight01.Add(new Vector4(boneWeightPairs[0].index, boneWeightPairs[0].weight, boneWeightPairs[1].index, boneWeightPairs[1].weight));
            boneIdxWight23.Add(new Vector4(boneWeightPairs[2].index, boneWeightPairs[2].weight, boneWeightPairs[3].index, boneWeightPairs[3].weight));
        }
        generateMesh.SetUVs(1, boneIdxWight01);
        generateMesh.SetUVs(2, boneIdxWight23);

        AssetDatabase.CreateAsset(generateMesh, "Assets/Generated/Mesh.asset");
    }

}
