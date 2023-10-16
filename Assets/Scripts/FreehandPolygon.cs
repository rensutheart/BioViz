using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FreehandPolygon : MonoBehaviour {

	public List<Vector3> polygon;
	public int Count = 0;

	// Use this for initialization
	void Start()
	{
		polygon = new List<Vector3>();
	}

	public void Add(Vector3 v)
	{
		polygon.Add(v);
		Count = polygon.Count;
	}

	public void Remove(Vector3 v)
	{
		polygon.Remove(v);
		Count = polygon.Count;
	}

	public void RemoveAt(int i)
	{
		polygon.RemoveAt(i);
		Count = polygon.Count;
	}

	public void Clear()
	{
		polygon.Clear();
		Count = polygon.Count;
	}

	public int NumElements()
	{
		return polygon.Count;
	}
}
