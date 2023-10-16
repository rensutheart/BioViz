using UnityEngine;
using System.Collections;

public class ScaleEverything : MonoBehaviour {
	public GameObject Everything;
    public float scaleFactor = 0.5f;

	void Awake()
	{
		Everything.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
	}


	private void OnDisable()
	{
		Everything.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
	}
}
