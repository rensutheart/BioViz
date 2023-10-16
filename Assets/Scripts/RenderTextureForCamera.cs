using UnityEngine;
using System.Collections;

public class RenderTextureForCamera : MonoBehaviour {
    public RenderTexture rt;
    public int size = 512;

    void Start()
    {
        updateRenderTexture(size);
    }

    public void updateRenderTexture(int size)
    {
        rt = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32);
        rt.Create();
        gameObject.GetComponent<Camera>().targetTexture = rt;
    }
}
