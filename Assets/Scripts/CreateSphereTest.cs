using UnityEngine;
using System.Collections;

public class CreateSphereTest : MonoBehaviour 
{
	public Material mat;
	public GameObject moleculeParent;

	GameObject[] spheres;

	// Use this for initialization
	void Start () 
	{
		spheres = new GameObject[2000];
		//GameObject moleculeParent = GameObject.FindGameObjectWithTag("MoleculeParent");

		for(int i = 0; i < spheres.Length; i++)
		{
			spheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			spheres[i].transform.position = new Vector3(Random.Range(-10.0F, 10.0F), Random.Range(-10.0F, 10.0F), Random.Range(-10.0F, 10.0F));
			spheres[i].GetComponent<MeshRenderer>().material.color = new Color(Random.Range(0.0F, 1.0F), Random.Range(0.0F, 1.0F), Random.Range(0.0F, 1.0F));
			spheres[i].GetComponent<MeshRenderer>().receiveShadows = false;
			spheres[i].GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

			spheres[i].transform.SetParent(moleculeParent.transform);
			//spheres[i].AddComponent<Rigidbody>();
			//Destroy(spheres[i].GetComponent<SphereCollider>());
		}
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}
}
