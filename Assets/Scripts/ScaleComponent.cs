using UnityEngine;
using System.Collections;

public class ScaleComponent : MonoBehaviour {
	public float scaleFactor;
	// Use this for initialization
	void Start () {
		//gameObject.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
	}
	
	// Update is called once per frame
	void Update () {
		gameObject.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
	}
}
