using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System;
using VRStandardAssets.Utils;

public class DropdownInteractiveItem : MonoBehaviour 
{
	public VolumeVisualizer volumeVis;
	[SerializeField] private VRInteractiveItem m_InteractiveItem;

	float enterTime = 0.0f;	// used to calcute the amount of time the button is held

	bool handClicked = false;

	public bool menuOpen = false;

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
		if(!volumeVis.sliderBeingUsed && !menuOpen)
		{
			var pointer = new PointerEventData(EventSystem.current);
			ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerEnterHandler);
			gameObject.GetComponent<Animator>().SetBool("Highlighted", true);
			gameObject.GetComponent<Animator>().SetBool("Normal", false);
			//gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, gameObject.transform.localPosition.z - 50.0f);
		}
	}


	//Handle the Out event
	private void HandleOut()
	{
		//Debug.Log("Show out state");
		if(!volumeVis.sliderBeingUsed && !menuOpen)
		{
			var pointer = new PointerEventData(EventSystem.current);
			ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerExitHandler);
			gameObject.GetComponent<Animator>().SetBool("Normal", true);
			gameObject.GetComponent<Animator>().SetBool("Highlighted", false);
			//gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, gameObject.transform.localPosition.z + 50.0f);
		}
	}


	//Handle the Click event
	private void HandleClick()
	{
		// don't allow a new slider to function if another one is being used
		if(!volumeVis.sliderBeingUsed)
		{
			ToggleMenu();
		
			//Debug.Log("Show click state dropdown main");
			var pointer = new PointerEventData(EventSystem.current);
			ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerClickHandler);
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

	public void ToggleMenu()
	{
		menuOpen = !menuOpen;

		//Debug.Log(menuOpen);
		// change the box collider based on whether the menu is open or not
		if(menuOpen)
		{
			//gameObject.GetComponent<BoxCollider>().center = new Vector3(0f, -107f, 0f);
			//gameObject.GetComponent<BoxCollider>().size = new Vector3(220f, 260f, 1f);
			gameObject.GetComponent<Dropdown>().Show();
		}
		else
		{
			//gameObject.GetComponent<BoxCollider>().center = new Vector3(0f, 0f, 0f);
			//gameObject.GetComponent<BoxCollider>().size = new Vector3(220f, 60f, 1f);
			gameObject.GetComponent<Dropdown>().Hide();

			HandleOut();

			enterTime = Time.time;
			volumeVis.dropdownClicked = false;
		}
	}

	void OnTriggerEnter(Collider other)
	{	
		if(other.CompareTag("IndexBone"))
		{
			enterTime = Time.time;
			volumeVis.currentDropdown = gameObject;
		}
	}

	void OnTriggerStay(Collider other)
	{
		if(!volumeVis.dropdownClicked && !handClicked && other.CompareTag("IndexBone"))
		{
			// if the button is held for more than 0.5s then the button is clicked
			float timetoClick = menuOpen ? 0.5f : 0.05f;
	
			if(Time.time - enterTime > timetoClick)
			{
				//var pointer = new PointerEventData(EventSystem.current);
				//ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerClickHandler);
				//Debug.Log("LeapMotion click");

				// don't allow a new slider to function if another one is being used
				if(!volumeVis.sliderBeingUsed)
				{
					ToggleMenu();

					//Debug.Log("Show click state dropdown main");
					//var pointer = new PointerEventData(EventSystem.current);
					//ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerClickHandler);
				}


				enterTime = Time.time;
				volumeVis.dropdownClicked = true;
				handClicked = true;
			}
			else
			{
				if(!volumeVis.sliderBeingUsed && !menuOpen)
				{
					var pointer = new PointerEventData(EventSystem.current);
					ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerEnterHandler);
					gameObject.GetComponent<Animator>().SetBool("Highlighted", true);
					gameObject.GetComponent<Animator>().SetBool("Normal", false);
					//gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, gameObject.transform.localPosition.z - 50.0f);
				}
			}
		}
	}

	void OnTriggerExit(Collider other)
	{
		if(other.CompareTag("IndexBone"))
		{
			if(!volumeVis.sliderBeingUsed && !menuOpen)
			{
				var pointer = new PointerEventData(EventSystem.current);
				ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerExitHandler);
				gameObject.GetComponent<Animator>().SetBool("Normal", true);
				gameObject.GetComponent<Animator>().SetBool("Highlighted", false);
				//gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, gameObject.transform.localPosition.z + 50.0f);
			}

			enterTime = Time.time;
			volumeVis.dropdownClicked = false;
			handClicked = false;
		}
	}
}