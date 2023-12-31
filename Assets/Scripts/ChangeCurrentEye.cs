using UnityEngine;
using System.Collections;

public class ChangeCurrentEye : MonoBehaviour 
{
	//public Material mat;
	public VolumeVisualizer volumeVis;
    public Camera mainCam;
	private int currentEye = 0;



	/// <summary>
	///  called twice-per frame (once per eye)
	/// </summary>
	void OnPreRender()
	{

        if (mainCam.GetComponent<Camera>().stereoTargetEye == StereoTargetEyeMask.Left)
            volumeVis.internalMat.SetInt("_currentEye", 0);
        else if (mainCam.GetComponent<Camera>().stereoTargetEye == StereoTargetEyeMask.Right)
            volumeVis.internalMat.SetInt("_currentEye", 1);
        //volumeVis.internalMat.SetInt("_currentEye", currentEye);
       // Debug.Log("OnPreRender()" + currentEye);

        currentEye = 1 - currentEye;
	}
    /*
    void OnRenderImage()
    {
        Debug.Log("OnRenderImage()" + currentEye);
    }*/
}
