using UnityEngine;
using UnityEngine.UI;
using Leap.Unity;

namespace Leap.PinchUtility
{

	/// <summary>
	/// Use this component on a Game Object to allow it to be manipulated by a pinch gesture.  The component
	/// allows rotation, translation, and scale of the object (RTS).
	/// </summary>
	[ExecuteAfter (typeof(LeapPinchDetector))]
	public class LeapRTS : MonoBehaviour
	{

		public enum RotationMethod
		{
			None,
			Single,
			Full
		}

		private bool leftHandInside = false;
		private bool rightHandInside = false;

		// these two variables show that the hands pinched first before entering
		private bool leftHandInsideWrong = false;
		private bool rightHandInsideWrong = false;


		[SerializeField]
		private LeapPinchDetector _pinchDetectorL;

		[SerializeField]
		private LeapPinchDetector _pinchDetectorR;

		[SerializeField]
		private RotationMethod _oneHandedRotationMethod;

		[SerializeField]
		private RotationMethod _twoHandedRotationMethod;

		[SerializeField]
		private bool _allowScale = true;

		[Header ("GUI Options")]
		[SerializeField]
		private KeyCode _toggleGuiState = KeyCode.None;

		[SerializeField]
		private bool _showGUI = true;

		private Transform _anchor;

		private float _defaultNearClip;
	
		public VolumeVisualizer volumeVis;
		public Material boxEdgesMat;

		public GameObject infoText;
		public bool showDebugTest = false;

		//System.IO.StreamWriter file;
		//string lineText;
		//private Quaternion prevRot;

		void Awake ()
		{
			if (_pinchDetectorL == null || _pinchDetectorR == null) {
				Debug.LogWarning ("Both Pinch Detectors of the LeapRTS component must be assigned. This component has been disabled.");
				enabled = false;
			}

			GameObject pinchControl = new GameObject (gameObject.name + "_RTS Anchor");
			pinchControl.tag = "RTS_Anchor";
			_anchor = pinchControl.transform;
			_anchor.transform.parent = transform.parent;
			transform.parent = _anchor;

			boxEdgesMat.color = new Color32(45, 45, 45, 255);

			//prevRot = Quaternion.identity;

			//file = new System.IO.StreamWriter(@"OutputSize.csv");
			//lineText = "";
		}

		void Update ()
		{
			if (Input.GetKeyDown (_toggleGuiState)) {
				_showGUI = !_showGUI;
			}
            /*
			bool didUpdate = false;
			didUpdate |= _pinchDetectorL.DidChangeFromLastFrame;
			didUpdate |= _pinchDetectorR.DidChangeFromLastFrame;

			if (didUpdate) {
				transform.SetParent (null, true);
                
            }

			if (_pinchDetectorL.IsPinching && _pinchDetectorR.IsPinching && (leftHandInside && rightHandInside)) {
                boxEdgesMat.color = Color.red;
				//boxEdgesMat.SetColor("_mainColor", Color.red);
				transformDoubleAnchor ();
			} else if (_pinchDetectorL.IsPinching && leftHandInside) {
                boxEdgesMat.color = Color.yellow;
				//boxEdgesMat.SetColor("_mainColor", Color.yellow);
				transformSingleAnchor (_pinchDetectorL);
			} else if (_pinchDetectorR.IsPinching && rightHandInside) {
                boxEdgesMat.color = Color.magenta;
				//boxEdgesMat.SetColor("_mainColor", Color.magenta);
				transformSingleAnchor (_pinchDetectorR);
			} else {
				//boxEdgesMat.color = new Color32(45, 45, 45, 255);
			}

			if (didUpdate) {
				transform.SetParent (_anchor, true);
			}
            */
			
		}

		void OnGUI ()
		{
			if (_showGUI) {
				GUILayout.Label ("One Handed Settings");
				doRotationMethodGUI (ref _oneHandedRotationMethod);
				GUILayout.Label ("Two Handed Settings");
				doRotationMethodGUI (ref _twoHandedRotationMethod);
				_allowScale = GUILayout.Toggle (_allowScale, "Allow Two Handed Scale");
			}
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
			_anchor.position = (_pinchDetectorL.Position + _pinchDetectorR.Position) / 2.0f;

			switch (_twoHandedRotationMethod) {
			case RotationMethod.None:
				break;
			case RotationMethod.Single:
				Vector3 p = _pinchDetectorL.Position;
				p.y = _anchor.position.y;
				_anchor.LookAt (p);
				break;
			case RotationMethod.Full:
				Quaternion pp = Quaternion.Lerp (_pinchDetectorL.Rotation, _pinchDetectorR.Rotation, 0.5f);
				Vector3 u = pp * Vector3.up;
				_anchor.LookAt (_pinchDetectorL.Position, u);
				break;
			}

			if (_allowScale) {
				//Vector3 boxDim = volumeVis.boundingBox.GetComponent<BoundingBoxSetup>().boxDim;
				//Vector3 boxRatio = volumeVis.boundingBox.GetComponent<BoundingBoxSetup>().boxDimRatio;

				_anchor.localScale = Vector3.one * Vector3.Distance (_pinchDetectorL.Position, _pinchDetectorR.Position);

				//TODO: this should probably not be hardcoded
				// prevent small scale
				float smallScale = 0.075f;
				if(_anchor.localScale.x < smallScale || _anchor.localScale.y < smallScale || _anchor.localScale.z < smallScale)
					_anchor.localScale = new Vector3(smallScale, smallScale, smallScale);

				// prevent big scale, but doesn't work (and isn't such a big problem)
				float bigScale = 1.0f;
				if(_anchor.localScale.x > bigScale || _anchor.localScale.y > bigScale || _anchor.localScale.z > bigScale)
					_anchor.localScale = new Vector3(bigScale, bigScale, bigScale);

			}
		}

		private void transformSingleAnchor (LeapPinchDetector singlePinch)
		{
			_anchor.position = singlePinch.Position;

			switch (_oneHandedRotationMethod) {
			case RotationMethod.None:
				break;
			case RotationMethod.Single:
				Vector3 p = singlePinch.Rotation * Vector3.right;
				p.y = _anchor.position.y;
				_anchor.LookAt (p);
				break;
			case RotationMethod.Full:
				_anchor.rotation = singlePinch.Rotation;//*Quaternion.Inverse(prevRot);
				break;
			}

			//_anchor.localScale = Vector3.one;
		}

		void OnTriggerEnter(Collider other)
		{
			if(other.CompareTag("LeftIndex"))
			{
				if(!_pinchDetectorL.IsPinching)
					leftHandInside = true;
				else
				{
					leftHandInsideWrong = true;
					infoText.GetComponentInChildren<Text>().text = "First insert hand then pinch";
				}
			}
			if(other.CompareTag("RightIndex"))
			{
				if(!_pinchDetectorR.IsPinching)
					rightHandInside = true;
				else
				{
					rightHandInsideWrong = true;
					infoText.GetComponentInChildren<Text>().text = "First insert hand then pinch";
				}
			}
		}

		void OnTriggerStay(Collider other)
		{
			if(showDebugTest)
				infoText.SetActive(true);
			if(leftHandInside)
			{
				boxEdgesMat.color = Color.green;
				infoText.GetComponentInChildren<Text>().text = "Left hands in box";
			}
			if(rightHandInside)
			{
				boxEdgesMat.color = Color.blue;
				infoText.GetComponentInChildren<Text>().text = "Right hand in box";
			}

			if(leftHandInside && rightHandInside)
			{
				boxEdgesMat.color = Color.cyan;
				infoText.GetComponentInChildren<Text>().text = "Both hands in box";
			}

			// after incorrect insertion of hands now it's corrected
			if(leftHandInsideWrong && !_pinchDetectorL.IsPinching)
			{
				leftHandInsideWrong = false;
				leftHandInside = true;
			}
			if(rightHandInsideWrong && !_pinchDetectorR.IsPinching)
			{
				rightHandInsideWrong = false;
				rightHandInside = true;
			}


			bool didUpdate = false;
			didUpdate |= _pinchDetectorL.DidChangeFromLastFrame;
			didUpdate |= _pinchDetectorR.DidChangeFromLastFrame;

			if (didUpdate) {
				transform.SetParent (null, true);
			}

			if (_pinchDetectorL.IsPinching && _pinchDetectorR.IsPinching && (leftHandInside && rightHandInside)) {
				boxEdgesMat.color = Color.red;
				infoText.GetComponentInChildren<Text>().text = "Both hands pinching";
				//boxEdgesMat.SetColor("_mainColor", Color.red);
				transformDoubleAnchor ();
				//lineText = "0.1,";
			} else if (_pinchDetectorL.IsPinching && leftHandInside) {
				boxEdgesMat.color = Color.yellow;
				infoText.GetComponentInChildren<Text>().text = "Left hand pinching";
				//boxEdgesMat.SetColor("_mainColor", Color.yellow);
				transformSingleAnchor (_pinchDetectorL);
				//lineText = "1.1,";
			} else if (_pinchDetectorR.IsPinching && rightHandInside) {
				boxEdgesMat.color = Color.magenta;
				infoText.GetComponentInChildren<Text>().text = "Right hand pinching";
				//boxEdgesMat.SetColor("_mainColor", Color.magenta);
				transformSingleAnchor (_pinchDetectorR);
				//lineText = "2.1,";
			} else {
				//lineText = "3.1,";
				//boxEdgesMat.color = new Color32(45, 45, 45, 255);
			}

			if (didUpdate) {
				Vector3 boxRatio = volumeVis.boundingBox.GetComponent<BoundingBoxSetup>().boxDimRatio;

				volumeVis.boundingBox.GetComponent<BoundingBoxSetup>().boxDim = new Vector3(boxRatio.x *_anchor.localScale.x, boxRatio.y *_anchor.localScale.y, boxRatio.z *_anchor.localScale.z );

				//file.WriteLine(lineText + transform.localScale.x + "," + _anchor.localScale.x);

				transform.SetParent (_anchor, true);

				//prevRot = _anchor.rotation;
			}


			//lineText = "";
		}

		void OnTriggerExit(Collider other)
		{
			if(other.CompareTag("LeftIndex"))
			{
				leftHandInside = false;
				leftHandInsideWrong = false;
			}
			if(other.CompareTag("RightIndex"))
			{
				rightHandInside = false;
				rightHandInsideWrong = false;
			}

			if(!leftHandInside && !rightHandInside)
			{
				boxEdgesMat.color = new Color32(45, 45, 45, 255);
			}
			infoText.GetComponentInChildren<Text>().text = "No hands detected";
			infoText.SetActive(false);
		}
	}
}
