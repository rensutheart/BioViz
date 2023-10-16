using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;

public class ScatterRender : MonoBehaviour
{
    private GameObject scatterGeom;
    private Mesh scatterMesh;
    private MeshFilter mf;
    private MeshRenderer mr;
    public  Material scatterMaterial;

    private Vector3[] scatterVertices;
    private int[] scatterTriangls;

    private int[,] numbers;
    private int[,] numbers127;
    private int maxVal;
    public float maxScatterHeight = 10f;
    public float scatterSizeDivFactor = 50f;

    // Use this for initialization
    void Start () {

        readDataFromFile();
        StartCoroutine(Generate(127, 127));
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    private void readDataFromFile()
    {
        
        Debug.Log("Reading from text file");
        var fileContent = File.ReadAllText("C:\\Users\\Rensu Theart\\Dropbox\\Masters\\Results\\NewData.m");
        var array = fileContent.Split((string[])null, StringSplitOptions.RemoveEmptyEntries);
        
        List<int> allNumbers = array.Select(arg => int.Parse(arg)).ToList();

        Debug.Log(allNumbers.Count + " numbers");

        numbers = new int[256, 256];
        
        for (int y = 0, i = 0 ; y < 256; y++)
        {
            for (int x = 0; x < 256; x++, i++)
            {
                numbers[x, y] = allNumbers[i];
            }
        }

        // reduce the resolution by half
        numbers127 = new int[128, 128];
        maxVal = 0;
        for (int x = 0; x < 128; x++)
        {
            for (int y = 0; y < 128; y++)
            {
                int newX = x * 2;
                int newY = y * 2;
                int average = (numbers[newX, newY] + numbers[newX+1, newY] + numbers[newX, newY+1] + numbers[newX+1, newY+1]) / 4;
                maxVal = Math.Max(maxVal, average);
                numbers127[x, y] = average;
            }
        }

        Debug.Log("Max value: " + maxVal);
    }

    IEnumerator Generate(int xDim, int yDim)
    {
        WaitForSeconds wait = new WaitForSeconds(0.05f);

        scatterGeom = new GameObject("Scatter Geometry");
        scatterMesh = new Mesh();
        scatterMesh.name = "Scatter Mesh";

        mf = scatterGeom.AddComponent(typeof(MeshFilter)) as MeshFilter;
        mf.mesh = scatterMesh;

        mr = scatterGeom.AddComponent(typeof(MeshRenderer)) as MeshRenderer;

        mr.material = scatterMaterial;
        //mr.receiveShadows = false;
        //mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        //mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        //mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        //mr.material = scatterMaterial;


        // initialize the proxyVertices and Triangels
        scatterVertices = new Vector3[(xDim+1) * (yDim+1)];
        Vector2[] uv = new Vector2[scatterVertices.Length];
        for (int i = 0, y = 0; y <= yDim; y++)
        {
            for (int x = 0; x <= xDim; x++, i++)
            {
                scatterVertices[i] = new Vector3(x / scatterSizeDivFactor, (float)numbers127[x, y] / maxVal * maxScatterHeight, y / scatterSizeDivFactor);
                uv[i] = new Vector2((float)x / xDim, (float)y / yDim);
            }
        }
        scatterMesh.vertices = scatterVertices;
        scatterMesh.uv = uv;

        scatterTriangls = new int[xDim * yDim * 6];
        for (int ti = 0, vi = 0, y = 0; y < yDim; y++, vi++)
        {
            for (int x = 0; x < xDim; x++, ti += 6, vi++)
            {
                scatterTriangls[ti] = vi;
                scatterTriangls[ti + 3] = scatterTriangls[ti + 2] = vi + 1;
                scatterTriangls[ti + 4] = scatterTriangls[ti + 1] = vi + xDim + 1;
                scatterTriangls[ti + 5] = vi + xDim + 2;
            }
        }

        scatterMesh.SetTriangles(scatterTriangls, 0);

        scatterMesh.RecalculateBounds();
        scatterMesh.RecalculateNormals();

        yield return null;
    }
    /*
    private void OnDrawGizmos()
    {
        if (scatterVertices != null)
        {
            Gizmos.color = Color.black;
            for (int i = 0; i < scatterVertices.Length; i++)
            {
                Gizmos.DrawSphere(scatterVertices[i], 0.01f);
            }
        }
    }
    */
}
