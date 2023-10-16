using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using VRStandardAssets.Utils;

public class CanvasInteractiveItem : MonoBehaviour {

	public VolumeVisualizer volumeVis;
	[SerializeField] private VRInteractiveItem m_InteractiveItem;

	//float enterTime = 0.0f;	// used to calcute the amount of time the button is held

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
	}


	private void OnDisable()
	{
		m_InteractiveItem.OnOver -= HandleOver;
		m_InteractiveItem.OnOut -= HandleOut;
		m_InteractiveItem.OnClick -= HandleClick;
		m_InteractiveItem.OnDoubleClick -= HandleDoubleClick;
	}

	//Handle the Over event
	private void HandleOver()
	{
		//Debug.Log("Show over state");

	}


	//Handle the Out event
	private void HandleOut()
	{
		//Debug.Log("Show out state");
	}


	//Handle the Click event
	private void HandleClick()
	{
		//Debug.Log("Show click state: Canvas");
		// don't allow a new slider to function if another one is being used
		if(volumeVis.sliderBeingUsed)
		{
			try
			{
				if(!volumeVis.colocalizationActive)
				{
					// reset all the sliders
					for(int i = 0; i < volumeVis.guiSliders.Length; i++)
					{
						if(!volumeVis.guiSliders[i].CompareTag("Untagged"))
						{
							volumeVis.guiSliders[i].transform.localPosition = new Vector3(volumeVis.guiSliders[i].transform.localPosition.x, volumeVis.guiSliders[i].transform.localPosition.y, volumeVis.guiSliders[i].transform.localPosition.z + 50.0f);
						}

						volumeVis.guiSliders[i].tag = "Untagged";
						volumeVis.guiSliders[i].transform.localScale = Vector3.one;
						volumeVis.guiSliders[i].GetComponent<Slider>().interactable = true;
						volumeVis.guiSliders[i].GetComponent<SliderInteractiveItem>().currentlyActive = false;
						//texSlicing.texSliceSliders[i].SetActive(true);
					}
				}
				else
				{
					// reset all the sliders
					for(int i = 0; i < volumeVis.colocSliders.Length; i++)
					{
						if(!volumeVis.colocSliders[i].CompareTag("Untagged"))
						{
							volumeVis.colocSliders[i].transform.localPosition = new Vector3(volumeVis.colocSliders[i].transform.localPosition.x, volumeVis.colocSliders[i].transform.localPosition.y, volumeVis.colocSliders[i].transform.localPosition.z + 50.0f);
						}

						volumeVis.colocSliders[i].tag = "Untagged";
						volumeVis.colocSliders[i].transform.localScale = Vector3.one;
						volumeVis.colocSliders[i].GetComponent<Slider>().interactable = true;
						volumeVis.colocSliders[i].GetComponent<SliderInteractiveItem>().currentlyActive = false;
						//texSlicing.texSliceSliders[i].SetActive(true);
					}
				}
			}
			catch(Exception e)
			{
				Debug.LogAssertion(e);
			}
				
			volumeVis.sliderBeingUsed = false;
			volumeVis.currentSliderNum = -1;

		}
	}


	//Handle the DoubleClick event
	private void HandleDoubleClick()
	{
		Debug.Log("Show double click");

	}

	/*
	void OnTriggerEnter(Collider other)
	{
		enterTime = Time.time;
		volumeVis.currentButton = gameObject;
	}

	void OnTriggerStay(Collider other)
	{
		if(!volumeVis.buttonClicked && other.CompareTag("IndexBone"))
		{
			// if the button is held for more than 0.5s then the button is clicked
			if(Time.time - enterTime > 0.5f)
			{
				HandleClick();
				enterTime = Time.time;
				volumeVis.buttonClicked = true;
			}

		}
	}

	void OnTriggerExit(Collider other)
	{

		enterTime = Time.time;
		volumeVis.buttonClicked = false;
	}
	*/
}
