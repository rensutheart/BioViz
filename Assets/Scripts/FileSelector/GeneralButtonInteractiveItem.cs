using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System;
using VRStandardAssets.Utils;

public class GeneralButtonInteractiveItem : MonoBehaviour 
{
	[SerializeField] private VRInteractiveItem m_InteractiveItem;

	float enterTime = 0.0f;	// used to calcute the amount of time the button is held
	bool handClicked = false;

    bool buttonClicked = false;

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
		var pointer = new PointerEventData(EventSystem.current);
		ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerEnterHandler);

		//gameObject.GetComponent<Animator>().SetBool("Normal", false);
		//gameObject.GetComponent<Animator>().SetBool("Highlighted", true);

		//Debug.Log("Show over state");
	}


	//Handle the Out event
	private void HandleOut()
	{
		var pointer = new PointerEventData(EventSystem.current);
		ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerExitHandler);

		//gameObject.GetComponent<Animator>().SetBool("Highlighted", false);
		//gameObject.GetComponent<Animator>().SetBool("Normal", true);

		//Debug.Log("Show out state");
	}


	//Handle the Click event
	private void HandleClick()
	{		
		//Debug.Log("Show click state");
		var pointer = new PointerEventData(EventSystem.current);
		//ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerExitHandler);
		//gameObject.GetComponent<Animator>().SetBool("Highlighted", false);
		//gameObject.GetComponent<Animator>().SetBool("Normal", true);
		ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerClickHandler);

		//gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, gameObject.transform.localPosition.z + 50.0f);

	}

	//Handle the DoubleClick event
	private void HandleDoubleClick()
	{
		//Debug.Log("Show double click");

        var pointer = new PointerEventData(EventSystem.current);
        //ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerExitHandler);
        //gameObject.GetComponent<Animator>().SetBool("Highlighted", false);
        //gameObject.GetComponent<Animator>().SetBool("Normal", true);
        ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

	void OnTriggerEnter(Collider other)
	{
		if(other.CompareTag("IndexBone"))
		{
			enterTime = Time.time;

		}
	}

	void OnTriggerStay(Collider other)
	{
		if(!buttonClicked && !handClicked && other.CompareTag("IndexBone"))
		{
			// if the button is held for more than 0.5s then the button is clicked
			if(Time.time - enterTime > 0.5f)
			{
				//var pointer = new PointerEventData(EventSystem.current);
				//ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerClickHandler);
				//Debug.Log("LeapMotion click");
				enterTime = Time.time;
				buttonClicked = true;
				handClicked = true;
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
			buttonClicked = false;
			handClicked = false;

			//Debug.Log(other.name + " Exited " + gameObject.name);
		}
	}
}