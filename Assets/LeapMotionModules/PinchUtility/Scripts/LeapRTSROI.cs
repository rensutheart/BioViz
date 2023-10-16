using UnityEngine;
using Leap.Unity;

namespace Leap.PinchUtility
{

	/// <summary>
	/// Use this component on a Game Object to allow it to be manipulated by a pinch gesture.  The component
	/// allows rotation, translation, and scale of the object (RTS).
	/// </summary>
	[ExecuteAfter (typeof(LeapPinchDetector))]
	public class LeapRTSROI : MonoBehaviour
	{

		public enum RotationMethod
		{
			None,
			Single,
			Full
		}

		private bool leftHandInside = false;
		private bool rightHandInside = false;

		private bool pinched = false;

		[SerializeField]
		private LeapPinchDetector _pinchDetectorL;

		[SerializeField]
		private LeapPinchDetector _pinchDetectorR;

		[SerializeField]
		private bool _allowScale = true;

		private float _defaultNearClip;
	
		public VolumeVisualizer volumeVis;
		public Material boxEdgesMat;

		private Vector3 posOffset;
		private Vector3 posOffset2;

		void Awake ()
		{
			if (_pinchDetectorL == null || _pinchDetectorR == null) {
				Debug.LogWarning ("Both Pinch Detectors of the LeapRTS component must be assigned. This component has been disabled.");
				enabled = false;
			}
			/*
			GameObject pinchControl = new GameObject (gameObject.name + " RTS Anchor");
			_anchor = pinchControl.transform;
			_anchor.transform.parent = transform.parent;
			transform.parent = _anchor;

			if(!gameObject.CompareTag("Boundingbox"))
			{
				_anchor.position = Vector3.zero;
				_anchor.rotation = Quaternion.identity;
				_anchor.localScale = Vector3.one;
			}
*/
			boxEdgesMat.color = new Color32(195, 213, 255, 255);
		}

		void Update ()
		{

			if(volumeVis.useROI)
				volumeVis.checkROIBounds();

		}
			

		private void doRotationMethodGUI (ref RotationMethod rotationMethod)
		{
			GUILayout.BeginHorizontal ();

			GUI.color = rotationMethod == RotationMethod.None ? Color.green : Color.white;
			if (GUILayout.Button ("No Rotation")) {
				rotationMethod = RotationMethod.None;
			}

			GUI.color = rotationMethod == RotationMethod.Single ? Color.green : Color.white;
			if (GUILayout.Button ("Single Axis")) {
				rotationMethod = RotationMethod.Single;
			}

			GUI.color = rotationMethod == RotationMethod.Full ? Color.green : Color.white;
			if (GUILayout.Button ("Full Rotation")) {
				rotationMethod = RotationMethod.Full;
			}

			GUI.color = Color.white;

			GUILayout.EndHorizontal ();
		}

		private void transformDoubleAnchor ()
		{
			/*
			// this is only true the first time for a new pinch
			if(!pinched)
			{
				posOffset2 = gameObject.transform.position -  (_pinchDetectorL.Position + _pinchDetectorR.Position) / 2.0f;
				pinched = true;
			}*/
			gameObject.transform.position = (_pinchDetectorL.Position + _pinchDetectorR.Position) / 2.0f;

			if (_allowScale) {
				BoundingBoxSetup toolUsed = volumeVis.boundingBox.GetComponent<BoundingBoxSetup>();//currentROITool.GetComponent<ROIBoxSetup>();
				//ROIBoxSetup toolUsed = volumeVis.currentROITool.GetComponent<ROIBoxSetup>();
				float dist = Vector3.Distance (_pinchDetectorL.Position, _pinchDetectorR.Position)*1.1f;
				//Debug.Log(dist/0.4f);
				if(volumeVis.currentROITool != volumeVis.ROIFreehand)
				{
					if(!volumeVis.scaleROIAxis)
					{
						//TODO: this code doesn't make any sense to me... but it seems to work. (the else statement is the one that doens't make sense)
					//	if(toolUsed.boxDim.x <= 1.0f)
							gameObject.transform.localScale = new Vector3((dist/0.4f) / (toolUsed.boxDim.x), (dist/0.4f) / (toolUsed.boxDim.y), 1.0f); // z is fixed when not scaling to axis
						//else
						//	gameObject.transform.localScale = new Vector3((dist/0.4f) * (toolUsed.boxDim.x*0.4f), (dist/0.4f) * (toolUsed.boxDim.y*0.4f), 1.0f); // z is fixed when not scaling to axis
					}
					else
					{
						// the greater than 0.5f is just to ensure floating point inaccuracies doesn't break it. It should be either 1 or 0
						if(volumeVis.ROIscaleAxis.x > 0.5f)
							gameObject.transform.localScale = new Vector3((dist/0.4f) / toolUsed.boxDim.x, gameObject.transform.localScale.y, gameObject.transform.localScale.z);
						if(volumeVis.ROIscaleAxis.y > 0.5f)
							gameObject.transform.localScale = new Vector3(gameObject.transform.localScale.x, (dist/0.4f) / toolUsed.boxDim.y, gameObject.transform.localScale.z);
						if(volumeVis.ROIscaleAxis.z > 0.5f)
							gameObject.transform.localScale = new Vector3(gameObject.transform.localScale.x, gameObject.transform.localScale.y, (dist/0.4f) / toolUsed.boxDim.z);
						
					}
				}
				else
				{
					gameObject.transform.localScale = new Vector3(gameObject.transform.localScale.x, gameObject.transform.localScale.y, (dist/0.4f) / toolUsed.boxDim.z);
				}
			}
			pinched = false;
		}

		private void transformSingleAnchor (LeapPinchDetector singlePinch)
		{
			// this is only true the first time for a new pinch
			if(!pinched)
			{
				posOffset = gameObject.transform.position - singlePinch.Position;
				pinched = true;
			}

			if(volumeVis.currentROITool != volumeVis.ROIFreehand)
			{
				gameObject.transform.position = posOffset + singlePinch.Position;
			}
			else
			{
				Vector3 newPos = singlePinch.Position;
				newPos.x = 0f;
				newPos.y = 0f;
				gameObject.transform.position = newPos;
			}
		}

		void OnTriggerEnter(Collider other)
		{
			if(other.CompareTag("LeftIndex"))
			{
				leftHandInside = true;
			}
			if(other.CompareTag("RightIndex"))
			{
				rightHandInside = true;
			}
		}

		void OnTriggerStay(Collider other)
		{
			if(leftHandInside)
				boxEdgesMat.color = Color.green;
			if(rightHandInside)
				boxEdgesMat.color = Color.blue;

			if(leftHandInside && rightHandInside)
			{
				boxEdgesMat.color = Color.cyan;
			}


			bool didUpdate = false;
			didUpdate |= _pinchDetectorL.DidChangeFromLastFrame;
			didUpdate |= _pinchDetectorR.DidChangeFromLastFrame;

			if (didUpdate) {
				//transform.SetParent (null, true);
			}

			if (_pinchDetectorL.IsPinching && _pinchDetectorR.IsPinching && (leftHandInside && rightHandInside)) { // 
				boxEdgesMat.color = Color.red;
				//boxEdgesMat.SetColor("_mainColor", Color.red);
				transformDoubleAnchor ();
			} else if (_pinchDetectorL.IsPinching && leftHandInside) { //
				boxEdgesMat.color = Color.yellow;
				//boxEdgesMat.SetColor("_mainColor", Color.yellow);
				transformSingleAnchor (_pinchDetectorL);
			} else if (_pinchDetectorR.IsPinching && rightHandInside) { //
				boxEdgesMat.color = Color.magenta;
				//boxEdgesMat.SetColor("_mainColor", Color.magenta);
				transformSingleAnchor (_pinchDetectorR);
			} else {
				pinched = false;
				//boxEdgesMat.color = new Color32(45, 45, 45, 255);
			}

			if (didUpdate) {
				//transform.SetParent (_anchor, true);
				//transform.position = _anchor.position;
				//transform.localScale = _anchor.localScale;
				//transform.rotation = _anchor.rotation;
			}
		}

		void OnTriggerExit(Collider other)
		{
			if(other.CompareTag("LeftIndex"))
			{
				leftHandInside = false;
			}
			if(other.CompareTag("RightIndex"))
			{
				rightHandInside = false;
			}

			if(!leftHandInside && !rightHandInside)
			{
				boxEdgesMat.color = new Color32(195, 213, 255, 255);
			}
		}
			
	}
}
