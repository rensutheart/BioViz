using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System;
using VRStandardAssets.Utils;

public class BoundingBoxInteractiveItem : MonoBehaviour 
{
	public VolumeVisualizer volumeVis;
	public Material edgeMaterial;
	[SerializeField] private VRInteractiveItem m_InteractiveItem;


	// Use this for initialization
	void Start () 
	{
		
	}

	// Update is called once per frame
	void Update () 
	{

	}

	private void OnEnable()
	{
		m_InteractiveItem.OnOver += HandleOver;
		m_InteractiveItem.OnOut += HandleOut;
		m_InteractiveItem.OnClick += HandleClick;
		m_InteractiveItem.OnDoubleClick += HandleDoubleClick;

		//gameObject.GetComponent<Animator>().SetBool("Normal", true);
		//Debug.Log(gameObject.name + " Enabled");
	}


	private void OnDisable()
	{
		m_InteractiveItem.OnOver -= HandleOver;
		m_InteractiveItem.OnOut -= HandleOut;
		m_InteractiveItem.OnClick -= HandleClick;
		m_InteractiveItem.OnDoubleClick -= HandleDoubleClick;

		edgeMaterial.color = new Color(0.16f, 0.16f, 0.16f);

		//gameObject.GetComponent<Animator>().SetBool("Normal", true);
		//Debug.Log(gameObject.name + " Disabled");
	}

	//Handle the Over event
	private void HandleOver()
	{
		edgeMaterial.color = new Color(0.48f, 0.525f, 0.631f);
		volumeVis.boundingBoxHighlighted = true;
		//var pointer = new PointerEventData(EventSystem.current);
		//ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerEnterHandler);

		//gameObject.GetComponent<Animator>().SetBool("Normal", false);
		//gameObject.GetComponent<Animator>().SetBool("Highlighted", true);

		//Debug.Log("Show over state");
		if(!volumeVis.sliderBeingUsed)
		{
			//gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, gameObject.transform.localPosition.z - 50.0f);
		}
	}


	//Handle the Out event
	private void HandleOut()
	{
		edgeMaterial.color = new Color(0.16f, 0.16f, 0.16f);
		volumeVis.boundingBoxHighlighted = false;
		//var pointer = new PointerEventData(EventSystem.current);
		//ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerExitHandler);

		//gameObject.GetComponent<Animator>().SetBool("Highlighted", false);
		//gameObject.GetComponent<Animator>().SetBool("Normal", true);

		//Debug.Log("Show out state");
		if(!volumeVis.sliderBeingUsed )
		{
			//gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, gameObject.transform.localPosition.z + 50.0f);
		}
	}


	//Handle the Click event
	private void HandleClick()
	{
		// don't allow a new slider to function if another one is being used
		if(!volumeVis.sliderBeingUsed)
		{
			//Debug.Log("Show click state");
			//var pointer = new PointerEventData(EventSystem.current);
			//ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerExitHandler);
			//gameObject.GetComponent<Animator>().SetBool("Highlighted", false);
			//gameObject.GetComponent<Animator>().SetBool("Normal", true);
			//ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerClickHandler);

			//gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, gameObject.transform.localPosition.z + 50.0f);

		}
		else
		{
			Debug.Log("Show click state: Button click, but inactive");
			// don't allow a new slider to function if another one is being used

		}
	}

	//Handle the DoubleClick event
	private void HandleDoubleClick()
	{
		Debug.Log("Show double click");

	}

	// Leap Motion code
	void OnTriggerEnter(Collider other)
	{
		if(other.CompareTag("IndexBone"))
		{
			edgeMaterial.color = new Color(0.48f, 0.525f, 0.631f);
			volumeVis.boundingBoxHighlighted = true;
		}
	}

	void OnTriggerStay(Collider other)
	{
		if(other.CompareTag("IndexBone"))
		{
			volumeVis.fingerDrawing = true;
			volumeVis.fingerDrawPos = other.transform.position;
			volumeVis.fingerDrawPos = new Vector3(volumeVis.fingerDrawPos.x, volumeVis.fingerDrawPos.y, 0f);
		}
	}

	void OnTriggerExit(Collider other)
	{
		if(other.CompareTag("IndexBone"))
		{
			edgeMaterial.color = new Color(0.16f, 0.16f, 0.16f);
			volumeVis.boundingBoxHighlighted = false;
		}
	}
}