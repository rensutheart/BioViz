using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System;
using VRStandardAssets.Utils;

public class DropdownItemInteractiveItem : MonoBehaviour 
{
	public VolumeVisualizer volumeVis;
	public DropdownInteractiveItem parentDropdown;
	[SerializeField] private VRInteractiveItem m_InteractiveItem;

	float enterTime = 0.0f;	// used to calcute the amount of time the button is held

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

		//gameObject.GetComponent<Animator>().SetBool("Normal", true);
		//Debug.Log(gameObject.name + " Disabled");
	}

	//Handle the Over event
	private void HandleOver()
	{
		//Debug.Log("Show over state");
		if(!volumeVis.sliderBeingUsed)
		{
			var pointer = new PointerEventData(EventSystem.current);
			ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerEnterHandler);
			//gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, gameObject.transform.localPosition.z - 50.0f);
		}
	}


	//Handle the Out event
	private void HandleOut()
	{
		//Debug.Log("Show out state");
		if(!volumeVis.sliderBeingUsed)
		{
			var pointer = new PointerEventData(EventSystem.current);
			ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerExitHandler);
			//gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, gameObject.transform.localPosition.z + 50.0f);
		}
	}


	//Handle the Click event
	private void HandleClick()
	{
		// don't allow a new slider to function if another one is being used
		if(!volumeVis.sliderBeingUsed)
		{
			//Debug.Log("Show click state dropdown item");
			var pointer = new PointerEventData(EventSystem.current);
			ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerClickHandler);

			parentDropdown.ToggleMenu();
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

	void OnTriggerEnter(Collider other)
	{
		if(other.CompareTag("IndexBone"))
		{
			enterTime = Time.time;
			volumeVis.currentButton = gameObject;
		}
	}

	void OnTriggerStay(Collider other)
	{
		if(!volumeVis.buttonClicked && other.CompareTag("IndexBone"))
		{
			// if the button is held for more than 0.5s then the button is clicked
			if(Time.time - enterTime > 0.5f)
			{
				parentDropdown.ToggleMenu();
				//var pointer = new PointerEventData(EventSystem.current);
				//ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerClickHandler);
				//Debug.Log("LeapMotion click");
				enterTime = Time.time;
				volumeVis.buttonClicked = true;
			}
			else
			{
				var pointer = new PointerEventData(EventSystem.current);
				ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerEnterHandler);
			}
		}
	}

	void OnTriggerExit(Collider other)
	{
		if(other.CompareTag("IndexBone"))
		{
			var pointer = new PointerEventData(EventSystem.current);
			ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerExitHandler);

			enterTime = Time.time;
			volumeVis.buttonClicked = false;
		}
	}
}