using UnityEngine;
using System.Collections;

public class BoundingBoxSetup : MonoBehaviour
{
	public Vector3 boxDim;	// the bounding box dimension: width, height, depth

	public Vector3[] boxVertices;	// the vertices of the bounding box
	public Vector3[] actualBoxVertices;	// actual box internal vertices
	public int[,] edges;	// the edges of the bounding box

	private Vector3 boxOffset;

	public Vector3 boxDimRatio;

	// Use this for initialization
	void Awake ()
	{
		// update the bounding box to the box dimensions, and get the offset position
		gameObject.transform.localScale = boxDim;
		boxDimRatio = boxDim.normalized;
		//Update the vertices according to the bounding box dimensions

		Vector3[] tmpBoxVert = gameObject.GetComponent<MeshFilter>().mesh.vertices;
		boxOffset = gameObject.transform.position;
		boxVertices = new Vector3[8]
		{Vector3.Scale(tmpBoxVert[7], boxDim) + boxOffset, Vector3.Scale(tmpBoxVert[6], boxDim) + boxOffset, 
			Vector3.Scale(tmpBoxVert[4], boxDim) + boxOffset, Vector3.Scale(tmpBoxVert[5], boxDim) + boxOffset, 
			Vector3.Scale(tmpBoxVert[1], boxDim) + boxOffset, Vector3.Scale(tmpBoxVert[0], boxDim) + boxOffset, 
			Vector3.Scale(tmpBoxVert[2], boxDim) + boxOffset, Vector3.Scale(tmpBoxVert[3], boxDim) + boxOffset};

		actualBoxVertices = new Vector3[8]
		{tmpBoxVert[7], tmpBoxVert[6], tmpBoxVert[4], tmpBoxVert[5], tmpBoxVert[1], tmpBoxVert[0], tmpBoxVert[2], tmpBoxVert[3]};
		/*
		actualBoxVertices = new Vector3[] 
		{
			new Vector3(-1.0f / 2.0f, -1.0f / 2.0f, -1.0f / 2.0f),	//0
			new Vector3(1.0f / 2.0f, -1.0f / 2.0f, -1.0f / 2.0f),	//1
			new Vector3(1.0f / 2.0f, 1.0f / 2.0f, -1.0f / 2.0f),	//2
			new Vector3(-1.0f / 2.0f, 1.0f / 2.0f, -1.0f / 2.0f),	//3
			new Vector3(-1.0f / 2.0f, -1.0f / 2.0f, 1.0f / 2.0f),	//4
			new Vector3(1.0f / 2.0f, -1.0f / 2.0f, 1.0f / 2.0f),	//5
			new Vector3(1.0f / 2.0f, 1.0f / 2.0f, 1.0f / 2.0f),		//6
			new Vector3(-1.0f / 2.0f, 1.0f / 2.0f, 1.0f / 2.0f) 	//7
		};

		boxVertices = new Vector3[] 
		{
			new Vector3(-1.0f / 2.0f, -1.0f / 2.0f, -1.0f / 2.0f),	//0
			new Vector3(1.0f / 2.0f, -1.0f / 2.0f, -1.0f / 2.0f),	//1
			new Vector3(1.0f / 2.0f, 1.0f / 2.0f, -1.0f / 2.0f),	//2
			new Vector3(-1.0f / 2.0f, 1.0f / 2.0f, -1.0f / 2.0f),	//3
			new Vector3(-1.0f / 2.0f, -1.0f / 2.0f, 1.0f / 2.0f),	//4
			new Vector3(1.0f / 2.0f, -1.0f / 2.0f, 1.0f / 2.0f),	//5
			new Vector3(1.0f / 2.0f, 1.0f / 2.0f, 1.0f / 2.0f),		//6
			new Vector3(-1.0f / 2.0f, 1.0f / 2.0f, 1.0f / 2.0f) 	//7
		};*/

		/*
		boxVertices = new Vector3[] 
		{
			new Vector3(-boxDim.x / 2.0f, -boxDim.y / 2.0f, -boxDim.z / 2.0f),	//0
			new Vector3(boxDim.x / 2.0f, -boxDim.y / 2.0f, -boxDim.z / 2.0f),	//1
			new Vector3(boxDim.x / 2.0f, boxDim.y / 2.0f, -boxDim.z / 2.0f),	//2
			new Vector3(-boxDim.x / 2.0f, boxDim.y / 2.0f, -boxDim.z / 2.0f),	//3
			new Vector3(-boxDim.x / 2.0f, -boxDim.y / 2.0f, boxDim.z / 2.0f),	//4
			new Vector3(boxDim.x / 2.0f, -boxDim.y / 2.0f, boxDim.z / 2.0f),	//5
			new Vector3(boxDim.x / 2.0f, boxDim.y / 2.0f, boxDim.z / 2.0f),		//6
			new Vector3(-boxDim.x / 2.0f, boxDim.y / 2.0f, boxDim.z / 2.0f) 	//7
		};
*/


		// edge number are written in the fromVertex -> toVertex format
		edges = new int[,] 
		{
			{ 0, 1 }, //0
			{ 1, 2 }, //1
			{ 2, 3 }, //2
			{ 3, 0 }, //3
			{ 0, 4 }, //4
			{ 1, 5 }, //5 
			{ 2, 6 }, //6
			{ 3, 7 }, //7
			{ 4, 5 }, //8
			{ 5, 6 }, //9
			{ 6, 7 }, //10
			{ 7, 4 }  //11
		};
	}

	void Update()
	{
		boxDim = gameObject.transform.localScale;
		boxDimRatio = boxDim.normalized;
		//boxOffset = gameObject.transform.position;
	}
}