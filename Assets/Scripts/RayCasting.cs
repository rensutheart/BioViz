using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR;
using System.Collections;
using System.Collections.Generic;

public class RayCasting : VolumeVisualizer 
{
    /*
public GameObject isosurfaceMenu;	// the menu that holds the isosurface menu

private int[] showChannelState;

private Vector4[] camPos;	// the camera's position

private enum MaterialProperty {NumSamples, FilterThresh, Opacity, RedOpacity, GreenOpacity, BlueOpacity, PurpleOpacity, NumSamplesIso, Isovalue, DeltaValue};

//private int currentEye = 0;

void Start()
{
    Debug.Log("USING RAY CASTING");
    InitializeSuper((int)VolumeRenderingType.RayCasting);

    boundingBoxSetup.GetComponent<Renderer>().material = internalMat;

    camPos = new Vector4[2];
    camPos[0] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
    camPos[1] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

    showChannelState = new int[]{1, 1, 1, 1};

    //Set the material properties to be the same as the slider's defaults
    for(int i = 0; i < guiSliders.Length; i++)
    {
        currentSliderNum = i;
        changeMatProperty(i);
    }
    currentSliderNum = -1;
}

void Update()
{
    UpdateSuper ();

    //if(mainCamera.GetComponent<Camera>().stereoTargetEye == StereoTargetEyeMask.Left)
    //    internalMat.SetInt("_currentEye", 0);
    //else if (mainCamera.GetComponent<Camera>().stereoTargetEye == StereoTargetEyeMask.Right)
    //    internalMat.SetInt("_currentEye", 1);

    //currentEye = 1 - currentEye;

    if (loadSample != null)
    {
        if (!loadSample.doneLoadingSample && !loadSample.isPreLoaded)
        {
            //Debug.Log("Still busy loading sample");
            return;
        }
    }

    Vector3 eyeCenter = InputTracking.GetLocalPosition((VRNode.CenterEye));
    camPos[0] = boundingBoxSetup.transform.localToWorldMatrix.inverse * ((InputTracking.GetLocalPosition(VRNode.LeftEye) - eyeCenter) + mainCamera.transform.position - boundingBoxSetup.transform.position); // transform.Find("LeftEyeAnchor").position;
    camPos[1] = boundingBoxSetup.transform.localToWorldMatrix.inverse * ((InputTracking.GetLocalPosition(VRNode.RightEye) - eyeCenter) + mainCamera.transform.position - boundingBoxSetup.transform.position); // transform.Find("LeftEyeAnchor").position;

    internalMat.SetVector("_camPosLeft", camPos[0]);
    internalMat.SetVector("_camPosRight", camPos[1]);

    //Debug.Log("Update Left: " + (InputTracking.GetLocalPosition((VRNode.LeftEye)) + mainCamera.transform.position) + "  Right: " + (InputTracking.GetLocalPosition((VRNode.RightEye)) + mainCamera.transform.position) );
}

// the reason why I take slidernum in and not just use currentSliderNum all the way through is because I have two NumSamples sliders, which both give 0
public void changeMatProperty(int sliderNum)
{
    if(currentSliderNum == -1)
    {
        Debug.LogAssertion("No slider currently set, so can't update values (ray casting)");
        return;
    }
    if(showMIP)
    {
        MIPChangeMatProperty(sliderNum);
        //return;
    }
    Slider slider = guiSliders[currentSliderNum].GetComponent<Slider>();

    //Debug.Log(slider.value);
    string sliderText = slider.GetComponentInChildren<UnityEngine.UI.Text>().text;
    slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0:0.000}", slider.value);

    switch(sliderNum)
    {
    case (int)MaterialProperty.NumSamples: //0
        internalMat.SetInt("_numSamples", (int)slider.value);

        Slider mainSamplesSlider = guiSliders[(int) MaterialProperty.NumSamples].GetComponent<Slider>();
        Slider isoSamplesSlider = guiSliders[(int) MaterialProperty.NumSamplesIso].GetComponent<Slider>();

        mainSamplesSlider.value = slider.value;
        mainSamplesSlider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0}", slider.value);
        isoSamplesSlider.value = slider.value;
        isoSamplesSlider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0}", slider.value);
        break;
    case (int)MaterialProperty.FilterThresh: //1
        internalMat.SetFloat("_Threshold", slider.value);
        break;
    case (int)MaterialProperty.Opacity: //2
        internalMat.SetFloat("_Opacity", slider.value);
        break;
    case (int)MaterialProperty.RedOpacity: //3
        internalMat.SetFloat("_redOpacity", slider.value);
        break;
    case (int)MaterialProperty.GreenOpacity: //4
        internalMat.SetFloat("_greenOpacity", slider.value);
        break;
    case (int)MaterialProperty.BlueOpacity: //5
        internalMat.SetFloat("_blueOpacity", slider.value);
        break;
    case (int)MaterialProperty.PurpleOpacity: //6
        internalMat.SetFloat("_purpleOpacity", slider.value);
        break;
    case (int)MaterialProperty.Isovalue:  //8
        internalMat.SetInt("_isoValueIn", (int)slider.value);
        slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0}", slider.value);
        break;
    case (int)MaterialProperty.DeltaValue: //9
        internalMat.SetFloat("_deltaValue", slider.value);
        slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0:0.0000}", slider.value);
        break;
    }
}

    public void changeMenu(Material newMat)
	{
		internalMat.shader = newMat.shader;

		// change the current cancvas that is displayed
		if(mainMenu.activeInHierarchy) // change to iso menu
		{
			mainMenu.SetActive(false);
			isosurfaceMenu.SetActive(true);

			//mainMenu.transform.localPosition += new Vector3(0f, 0f, 0.10f);
			//isosurfaceMenu.transform.localPosition += new Vector3(0f, 0f, -0.10f);
		}
		else     // change to main menu
		{
			//mainMenu.transform.localPosition += new Vector3(0f, 0f, -0.10f);
			//isosurfaceMenu.transform.localPosition += new Vector3(0f, 0f, 0.10f);
			isosurfaceMenu.SetActive(false);
			mainMenu.SetActive(true);
		}
	}
    
	public void changeDisplayedChannel(int channel)
	{
		showChannelState[channel] = 1 - showChannelState[channel];
		//Debug.Log("Change " + channel + " with shader: " + internalMat.shader);

		switch(channel)
		{
		case 0:	// red
			internalMat.SetInt("_showRed", showChannelState[channel]);
			break;
		case 1:	// green
			internalMat.SetInt("_showGreen", showChannelState[channel]);
			break;
		case 2:	// blue
			internalMat.SetInt("_showBlue", showChannelState[channel]);
			break;
		case 3:	// purple
			internalMat.SetInt("_showPurple", showChannelState[channel]);
			break;
		}
	}
    */
}