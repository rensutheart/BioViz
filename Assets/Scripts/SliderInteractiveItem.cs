using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System;
using VRStandardAssets.Utils;

public class SliderInteractiveItem : MonoBehaviour 
{
	public VolumeVisualizer volumeVis;
	[SerializeField] private VRInteractiveItem m_InteractiveItem;

	public bool currentlyActive = false;		// a flag that indicates whether this slider is busy being used

	float enterTime = 0.0f;	// used to calcute the amount of time the button is held
	bool handClicked = false;


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
		var pointer = new PointerEventData(EventSystem.current);
		ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerEnterHandler);

		//Debug.Log("Show over state");
		if(!volumeVis.sliderBeingUsed)
		{
			//gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, gameObject.transform.localPosition.z - 50.0f);
		}
	}


	//Handle the Out event
	private void HandleOut()
	{
		var pointer = new PointerEventData(EventSystem.current);
		ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerExitHandler);
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
		if(!volumeVis.sliderBeingUsed || currentlyActive)
		{
			//Debug.Log("Show click state");
			var pointer = new PointerEventData(EventSystem.current);
			ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerClickHandler);
			if(!currentlyActive)
			{
				try
				{
					gameObject.tag = "CurrentSlider";
					if(!volumeVis.colocalizationActive)
					{
						for(int i = 0; i < volumeVis.guiSliders.Length; i++)
						{
							if(volumeVis.guiSliders[i].CompareTag("CurrentSlider"))
							{
								volumeVis.currentSliderNum = i;
							}
							else
							{
								volumeVis.guiSliders[i].transform.localScale = new Vector3(0.6f, 0.6f, 1f);
								volumeVis.guiSliders[i].GetComponent<Slider>().interactable = false;
								//texSlicing.texSliceSliders[i].SetActive(false);
							}
						}
					}
					else
					{
						for(int i = 0; i < volumeVis.colocSliders.Length; i++)
						{
							if(volumeVis.colocSliders[i].CompareTag("CurrentSlider"))
							{
								volumeVis.currentSliderNum = i;
							}
							else
							{
								volumeVis.colocSliders[i].transform.localScale = new Vector3(0.6f, 0.6f, 1f);
								volumeVis.colocSliders[i].GetComponent<Slider>().interactable = false;
								//texSlicing.texSliceSliders[i].SetActive(false);
							}
						}
					}
				}
				catch(Exception e)
				{
					Debug.LogAssertion(e);
				}

				currentlyActive = true;
				volumeVis.sliderBeingUsed = true;
			}
			else
			{
				try
				{
					if(!volumeVis.colocalizationActive)
					{
						// reset all the sliders
						for(int i = 0; i < volumeVis.guiSliders.Length; i++)
						{
							volumeVis.guiSliders[i].tag = "Untagged";
							volumeVis.guiSliders[i].transform.localScale = Vector3.one;
							volumeVis.guiSliders[i].GetComponent<Slider>().interactable = true;
							volumeVis.guiSliders[i].GetComponent<SliderInteractiveItem>().currentlyActive = false;
							//volumeVis.texSliceSliders[i].SetActive(true);
						}
					}
					else
					{
						// reset all the sliders
						for(int i = 0; i < volumeVis.colocSliders.Length; i++)
						{
							volumeVis.colocSliders[i].tag = "Untagged";
							volumeVis.colocSliders[i].transform.localScale = Vector3.one;
							volumeVis.colocSliders[i].GetComponent<Slider>().interactable = true;
							volumeVis.colocSliders[i].GetComponent<SliderInteractiveItem>().currentlyActive = false;
							//volumeVis.texSliceSliders[i].SetActive(true);
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
		else
		{
			Debug.Log("Show click state: Cancel previous");
			// don't allow a new slider to function if another one is being used

			try
			{
				if(!volumeVis.colocalizationActive)
				{
					// reset all the sliders
					for(int i = 0; i < volumeVis.guiSliders.Length; i++)
					{
						if(!volumeVis.guiSliders[i].CompareTag("Untagged"))
						{
							//volumeVis.guiSliders[i].transform.localPosition = new Vector3(volumeVis.guiSliders[i].transform.localPosition.x, volumeVis.guiSliders[i].transform.localPosition.y, volumeVis.guiSliders[i].transform.localPosition.z + 50.0f);
						}

						volumeVis.guiSliders[i].tag = "Untagged";
						volumeVis.guiSliders[i].transform.localScale = Vector3.one;
						volumeVis.guiSliders[i].GetComponent<Slider>().interactable = true;
						volumeVis.guiSliders[i].GetComponent<SliderInteractiveItem>().currentlyActive = false;
						//volumeVis.texSliceSliders[i].SetActive(true);
					}
				}
				else
				{
					// reset all the sliders
					for(int i = 0; i < volumeVis.colocSliders.Length; i++)
					{
						if(!volumeVis.colocSliders[i].CompareTag("Untagged"))
						{
							//volumeVis.colocSliders[i].transform.localPosition = new Vector3(volumeVis.colocSliders[i].transform.localPosition.x, volumeVis.colocSliders[i].transform.localPosition.y, volumeVis.colocSliders[i].transform.localPosition.z + 50.0f);
						}

						volumeVis.colocSliders[i].tag = "Untagged";
						volumeVis.colocSliders[i].transform.localScale = Vector3.one;
						volumeVis.colocSliders[i].GetComponent<Slider>().interactable = true;
						volumeVis.colocSliders[i].GetComponent<SliderInteractiveItem>().currentlyActive = false;
						//volumeVis.texSliceSliders[i].SetActive(true);
					}
				}

				// this line is to prevent incorrect translation later on
				//gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, gameObject.transform.localPosition.z - 50.0f);
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

	void OnTriggerEnter(Collider other)
	{
		if(other.CompareTag("IndexBone"))
		{
			enterTime = Time.time;
		}
	}

	void OnTriggerStay(Collider other)
	{
		// the world position of the four corners of the slider
		if(other.CompareTag("IndexTip"))
		{
			if(currentlyActive)
			{
				Vector3[] corners = new Vector3[4];
				gameObject.GetComponent<RectTransform>().GetWorldCorners(corners);
				float sliderLength = corners[2].x - corners[0].x;//Vector3.Distance(corners[0], corners[2]); // from the bottom left to the top right corner of the slider (diagonal)

				// the world position of the finger
				Vector3 fingerPos = other.transform.position;

				Slider currSlider = gameObject.GetComponent<Slider>();
				float valueVariation = currSlider.maxValue - currSlider.minValue;
				//Debug.Log("cx: " + corners[0].x + " fx: " + fingerPos.x + " val: " + (fingerPos.x - corners[0].x)/ sliderLength); // Vector3.Distance(corners[0], fingerPos)
				currSlider.value = ((fingerPos.x - corners[0].x) / sliderLength) * valueVariation + currSlider.minValue;  //Vector3.Distance(corners[0], fingerPos) //(fingerPos.x - corners[0].x)
			}
			else //( don't allow the cancel action when it's currently active and clicking on this item)
			{
				if(!handClicked)
				{
					float timetoClick = volumeVis.sliderBeingUsed ? 0.25f : 0.5f;
					// if the button is held for more than 0.5s then the button is clicked
					if(Time.time - enterTime > timetoClick)
					{
						handClicked = true;
						HandleClick();
						//Debug.Log("LeapMotion click");
					}
					else
					{
						var pointer = new PointerEventData(EventSystem.current);
						ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerEnterHandler);
					}
				}
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
			handClicked = false;
		}
	}
}