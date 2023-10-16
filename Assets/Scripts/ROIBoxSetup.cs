using UnityEngine;
using System.Collections;

public class ROIBoxSetup : MonoBehaviour
{
	public Vector3 boxDim;	// the bounding box dimension: width, height, depth
	public Vector3 boxDimRatio;

	// Use this for initialization
	void Awake ()
	{
		// update the bounding box to the box dimensions, and get the offset position
		gameObject.transform.localScale = boxDim;
		boxDimRatio = boxDim.normalized;
	}

	void Update()
	{
		boxDim = gameObject.transform.localScale;
		//boxOffset = gameObject.transform.position;
	}
}