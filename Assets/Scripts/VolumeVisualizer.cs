using UnityEngine;
using UnityEngine.VR;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

using VRStandardAssets.Utils;

public class VolumeVisualizer : MonoBehaviour
{
    public bool BenProject = true;
    public bool autostarted = false;
    private string thisImageNameBen = "";
    private string thisImageFolderStructure = "";
    private string saveLocation3D = "C:\\BEN_FILES\\Export\\3D";
    private string saveLocation2D = "C:\\BEN_FILES\\Export\\MIP";
    #region variables
    public float setThreshold = 0.03f;
    public float setOpacity = 0.05f;
    public float setMax;
    public float setDepth = 0.2f;
    public float setColocOpacity = 0.25f;
    public float setAngle = 45f;
    public float setnMDPMin = 0.25f;
    public float setnMDPMax = 0.25f;

    public Material internalMat;

    public Material TextureSlicingMat;
    public Material RayCastingMat;
    public Material RayCastingIsoSurfaceMat;

    public int renderType;

    public Text mainHeadingText;

    [Space(10)]

    [Header("TextureSlicing settings")]
    public int num_slices = 200;

    private GameObject proxyGeom;
    private MeshFilter mf;
    private MeshRenderer mr;
    private Mesh proxyMesh;
    // these attributes are used to create the proxyMesh
    private Vector3[] proxyVertices;
    private List<int[]> proxyTriangles;

    private Vector3 viewDir;    // the camera's view direction

    //(Rensu: The independant paths along the edges depending on which vertex is in the front)
    // (index 0, 1, 3 is path index 2 (6 and 10) is the "dotted line" which is another possible edge to determine the intersection for, etc)
    private int[,] edgeList;

    private const float EPSILON = 0.0001f; //for floating point inaccuracy
    private int max_slices = 1000;  // the maximum allowable number of slices

    private enum TSMaterialProperty { NumSlices, FilterThresh, Opacity, RedOpacity, GreenOpacity, BlueOpacity, PurpleOpacity, SliceSelect, zScale };
    [Space(10)]

    [Header("RayCasting settings")]
    public GameObject isosurfaceMenu;   // the menu that holds the isosurface menu

    private int[] showChannelState;

    private Vector4[] camPos;   // the camera's position

    private enum RCMaterialProperty { NumSamples, FilterThresh, Opacity, RedOpacity, GreenOpacity, BlueOpacity, PurpleOpacity, SliceSelect, zScale, NumSamplesIso, Isovalue, DeltaValue };


    [Space(10)]

    [Header("ROI tools settings")]
    public GameObject boundingBox;
    public GameObject ROIBox;
    public GameObject ROICylinder;
    public GameObject ROIFreehand;
    public GameObject ROISphere;

    protected BoundingBoxSetup boundingBoxSetup;
    protected ROIBoxSetup ROIBoxSetup;
    protected ROIBoxSetup ROICylinderSetup;
    protected ROIBoxSetup ROISphereSetup;
    protected Vector3 initialBoxRatio;

    protected ColorMaps CM;
    //public Texture3D tex;
    //[System.NonSerialized]
    public int size = 1024;
    public bool separateMIPChannels = true;    // whether the channels are supplied as separte images or all in one image
                                               //public Vector3 sampleDim;	// the dimensions of the sample in micrometers (or any equivelant unit) (x length, y length, z increment)
                                               //public float zRatio = 10;

    public GameObject SliceSelectPanel;
    public bool showSelectSlice = false;

    public float translationSpeed = 2.0f;
    public float rotationSpeed = 100.0f;
    public float scalingSpeed = 2.0f;

    public GameObject HeatmapLegend;
    public Text HeatmapLowLabel;
    public Text HeatmapMidLabel;
    public Text HeatmapHighLabel;
    protected int maxHeatmapValue = 1; // legacy code, can remove (or replace with internalMat.GetFloat("_maxValue");)

    public GameObject ScatterHeatmapLegend;
    public Text ScatterHeatmapLowLabel;
    public Text ScatterHeatmapMidLabel;
    public Text ScatterHeatmapHighLabel;
    private int ScattermaxHeatmapValue = 1;

    public GameObject[] guiSliders;
    public GameObject[] colocSliders;
    public bool sliderBeingUsed = false;    // if a slider is being used 
    public int currentSliderNum = -1;       // the index of the current slider being used
    public float sliderChangeSensitivity = 0.5f;    // the sensitivity with which the slider is changed with the Dpad

    public GameObject volumeVizTypeDropdown;

    public GameObject mainMenu;     // the canvas that holds the main menu
    public GameObject colocMenu;    // the canvas that holds the colocalization menu
    public GameObject ROIMenu;      // the canvas that holds the Region of Interest menu
    public GameObject colocResultsCanvase;      // the canvas that holds the scatter plot and colocalization results

    public GameObject SliceQuad2D;

    public Material mipMat;
    public GameObject MIPQuad;

    public bool showMIP = false;
    public bool MIPMatInitialized = false;
    private enum MIPMaterialProperty { NumSamples, FilterThresh, Opacity, RedOpacity, GreenOpacity, BlueOpacity, PurpleOpacity };

    private Color[] MIPSliceCh0;
    private Color[] MIPSliceCh1;

    public enum VolumeRenderingType { TextureSlicing, RayCasting, RayCastingIso };

    protected List<Vector3> boxVertices;

    protected GameObject mainCamera;
    public Camera resultsCamera;

    protected Matrix4x4 extraModelMatrix;   // the addition that needs to be made to the model matrix due to transformation of the bounding box

    protected enum ColocMatProperties { ColocOpacity, chan0LowThres, chan0HighThres, chan1LowThres, chan1HighThres, MaxValue, Percentage, Angle};
    protected enum ScatterPlotTypes { FrequencyScatter, DisplayColors };

    // these are the colocalization variables
    public bool colocalizationActive = false;

    public class ColocalizationData
    {
        public int colocVoxelsRegion4 = 0;
        public int colocVoxelsRegion3 = 0;
        public int colocVoxelsRegion2 = 0;
        public int colocVoxelsRegion1 = 0;
        //public int colocalizedPixelsLowToHigh = 0;
        public int[] totalPixelCountAboveHigh;
        public int[] totalPixelCountLowToHigh; // this doesn't seem to have any significance
        public float PCC = 0.0f;
        public float MOC = 0.0f;
        public float[] MCC;

        public ColocalizationData()
        {
            totalPixelCountAboveHigh = new int[2];
            totalPixelCountLowToHigh = new int[2];
            MCC = new float[2];
        }
    }

    // calculate the percentage colocalization between the given two channels
    //ColocalizationData calculatePercentageColocalization(int channel0, int channel1, float thersholdHighCh0, float thersholdLowCh0, float thersholdHighCh1, float thersholdLowCh1);
    //void calculatePercentageColocalization();
    protected float percColocalizedHigh;
    protected float percColocalizedLow;

    protected float percColocalizedHigh_chan0; // divided by only chan0 total
    protected float percColocalizedLow_chan0; // divided by only chan0 total

    protected float percColocalizedHigh_chan1; // divided by only chan1 total
    protected float percColocalizedLow_chan1; // divided by only chan1 total

    protected float colCalcPercProgress;        // the percentage progress made in calculating colocalization


    // for the given channel
    protected float[] thresholdHighValue;
    protected float[] thresholdLowValue;
    protected float[] thresholdHighMin;
    protected float[] thresholdLowMin;
    protected float[] thresholdHighMax;
    protected float[] thresholdLowMax;
    protected int thresholdMaxInt = 255;

    public enum ColocMethod { OverlayWhite, OnlyColoc, OnlyColocWhite, OnlyColocHeatmap, OnlyColocNMDP, OnlyColocNMDPSobel, NoColoc };
    protected int colocalizationMethod = (int)ColocMethod.NoColoc;
    protected int colocThresIntervalDisplay = 0; // the interval that should be displayed 0 = above high, 1 = between low and high, 2 = below low
    protected int colocChannel0 = 0;    // the current channel0 that is used to calculate colocalization
    protected int colocChannel1 = 1;    // the current channel1 that is used to calculate colocalization
    protected float colocalizationOpacity = 0.5f;

    protected ColocalizationData lastCalcedColoc;   // the last calculated colocalization results
    protected bool runColcalizationCalc = false; // whether the colocalization should be calculated
    protected bool calcColThreadDone = true; // whether the calculateColocalization thread is done
    protected int colocCounter = 0;             // the image counter that increments every frame

    // intermediate variables to calculate colocalization metrics
    protected double[] channelAverage;

    public GameObject SliceCh1Quad;
    public GameObject SliceCh2Quad;


    // this variable is changed by the GUI to indicate what the selected colour channel was
    public GameObject colocChan0Dropdown;
    public GameObject colocChan1Dropdown;
    public GameObject colocMethodDropdown;
    public GameObject colocIntervalDropdown;

    public GameObject colocLeftLabel;
    public GameObject colocRightLabel;
    public GameObject colocScatterLabel;

    protected List<string> colocDropdownOptions;
    protected bool colocBeingCalculated = false;

    Color[] channel0Data;
    Color[] channel1Data;
    Color[] colocalization2DLayer;
    Color[] colocalization2DLayerSlice;
    Color[] colocalization2DLayerMIP;

    // the region of interest variables
    public bool useROI = false;

    public GameObject ROIToolDropdown;
    protected enum ROITools { Box, Cylinder, Freehand, Sphere };
    public GameObject currentROITool;
    public GameObject activeROIDropdown;
    public List<GameObject> activeROITools;
    private List<GameObject> activeROIToolsGlobal;
    private List<GameObject> activeROIToolsInside;
    private List<Dropdown.OptionData> otherOptions;
    private int globalToolSelected = 0;
    private int insideToolSelected = 0;
    private Vector3 prevGlobalBoxScale;
    private int currentActiveROI = 0;
    private int currentActiveROIGlobal = 0;
    private int currentActiveROIInside = 0;
    [Space(10)]

    [Header("MIP Export settings")]
    public bool onlyInsideROI = true;
    public bool showHeatMap = true;
    public bool showMIPColors = false;
    public bool showColoc2D = true;
    public bool seperateChannelsExport2D = true;
    public int numChannelsToExport = 2;

    public bool showScatterPlot = true;

    //protected bool[,] ROIMask_XY; // the 2D mask that is projected into the volume
    //protected bool[,] ROIMask_XZ;
    //protected bool[,] ROIMask_YZ;
    //protected int MaskZDepth = 100;

    protected bool[,,] ROIMask3D; // the 3D ROI mask captureing in the volume
    protected int ROIMaskDepth = 32;
    protected bool[,] ROIMask2D; // the 2D ROI mask captureing in the volume (the 3D one is flattened into this one)

    /*
    public GameObject ROIMaskParent;
    public GameObject ROIMaskQuadXY;
    public GameObject ROIMaskQuadXZ;
    public GameObject ROIMaskQuadYZ;
    */
    // the start and end z position (in terms of a percentage. It will run from -.5 to .5)
    [Space(10)]
    [Header("ROI Settings")]
    public float startZ = 0.5f;
    public float endZ = -0.5f;
    private bool ROIMaskTextureBeingCalculated = false;

    protected Vector3 ROIFreehandHitPos;
    protected Vector3 ROIFreehandHitPosUnscaled;
    public GameObject ROIFreehandEdge;
    protected List<GameObject> ROIFreehandPath;
    protected List<Vector3> ROIFreehandVertices;
    protected List<int> ROIFreehandTriangles;
    public bool boundingBoxHighlighted = false;     // this is to indicate the the freehand tool is over the bounding box
                                                    //protected List<FreehandPolygon> freehandPolygon;
                                                    //protected int currentFreehandPolygon = -1; // after first add it will be 0
    protected bool updateMaskRunning = false;
    protected bool maskShouldUpdate = false;
    protected Vector3 previousBoxScale;
    public GameObject ROIClearButton;
    public GameObject ROIUndoButton;
    public GameObject ROIScaleAxisDropdown;

    public GameObject ROIAddButton;
    public GameObject ROIRemoveButton;
    public GameObject ROICurrentDropdown;
    public GameObject ROIViewSelectedROIButton;
    private bool ROIViewSelectedROI = false;	// a flag to determine whether a selected ROI is currently being viewed or not (in other words inside another ROI)

    private bool ROILockAxisTranslation = false;
    private bool ROITranslateTogether = false;

    protected bool ROISubtractiveSelect = false;

    private Texture3D ROI3DMaskTexture;
    // private Texture3D ShowAllMask;

    public Vector3 ROIscaleAxis;
    protected bool showROIMesh = true;
    // protected bool showROIMask = false;
    protected bool showROIVoxelsOnly = false;
    public bool scaleROIAxis = false;
    protected float previousFreehandPointTime;

    public GameObject ScatterPlot;
    public GameObject ScatterPlotDropbox;
    public GameObject ScatterPlotUpdateButton;
    protected Texture2D colorScatterTex;
    protected Texture2D freqScatterTex;

    protected bool ColocDispalyScatterToTexture3DBusy = false;
    protected bool ColocFrequencyScatterToTexture3DBusy = false;

    private int[,] scatterCountsFreq3D;
    private int[,] scatterCountsFreq2D;


    protected bool ROIisInside = true;

    public Material ROIMeshMaterial;
    public Material ROINotSelectedMat;

    public string[] channelNames;

    private int currentSlice = 0;

    //Leap Motions variables
    public bool buttonClicked = false;
    public bool dropdownClicked = false;
    public GameObject currentButton;
    public GameObject currentDropdown;

    public bool fingerDrawing = false;
    public Vector3 fingerDrawPos;

    public GameObject scaleROIaxisToggle;

    public GameObject everything;    

    public float highNum = 0.5f;

    private Color[,] dispScatterColors3D;
    private Color[,] freqScatterColors3D;
    private Color[,] dispScatterColors2D;
    private Color[,] freqScatterColors2D;


    private Color[] redPixels2D;
    private Color[] greenPixels2D;
    private Color[] bluePixels2D;
    private Color[] purplePixels2D;

    public float percentageUsed = 0.99f;


    long totalVoxelsInROI = 0;
    long totalVoxelsInROIMIP = 0;

    float m_LinRegThroughThres = 1;
    float c_LinRegThroughThres = 0;

    float m_LinRegThroughThresMIP = 1;
    float c_LinRegThroughThresMIP = 0;

    float m_LinReg = 1;
    float c_LinReg = 0;

    bool showHeatMapGuidelines = false;

    Vector2 p1, p2, p_end;
    private bool mayUpdateDistance = false;

    Vector2 p1MIP, p2MIP, p_endMIP;
    private bool mayUpdateDistanceMIP = false;
    private float visAngleMIP = 0;
    private int visDistanceThresMIP = 0;
    private float visMaxMIP = 0;

    public bool automaticUpdateAngle = true;

    public float angleDistanceReferenceVal = 0.7f;

    public GameObject heatmapElements;

    public LoadSample loadSample;

    double chan0AverageNMDP_MIP = 0f;
    double chan1AverageNMDP_MIP = 0f;
    double chan0MaxNMDP_MIP = 0f;
    double chan1MaxNMDP_MIP = 0f;

    double chan0Average_MIP = 0f;
    double chan1Average_MIP = 0f;
    double chan0Max_MIP = 0f;
    double chan1Max_MIP = 0f;
    double xMean_MIP = 0.0;
    double yMean_MIP = 0.0;
    #endregion

    public void OnEnable()
    {
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        try
        {
            mainCamera.GetComponent<VREyeRaycaster>().OnRaycasthit += OnRaycasthit;
        }
        catch (System.NullReferenceException e)
        {
            Debug.Log("No VREyeRaycaster attached to main camera. If using hand tracking, this is fine. " + e.Message);
        }
    }

    public void OnDisable()
    {
        //TODO: this shouldn't be commented but it causes an error?
        //mainCamera = GameObject.FindGameObjectWithTag ("MainCamera");
        //mainCamera.GetComponent<VREyeRaycaster>().OnRaycasthit -= OnRaycasthit;
        ROIMeshMaterial.SetColor("_Color", new Color(0.01176f, 0.5686f, 0.898f, 0.07843f));
    }

    public void Start()
    {
        volumeVizTypeDropdown.GetComponent<Dropdown>().value = renderType;
        switch (renderType)
        {
            case (int)VolumeRenderingType.TextureSlicing:
                mainHeadingText.text = "Texture Slicing";
                Debug.Log("USING TEXTURE SLICING");
                InitializeSuper((int)VolumeRenderingType.TextureSlicing);

                // create an object of the mainCamera and get viewDir (TODO: I can actually remove the mainCamera line, since it happens in VolumeVisualizer)
                mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
                viewDir = -mainCamera.transform.forward;

                // verify that num_slices is in the valid bounds
                if (num_slices > max_slices)
                    num_slices = max_slices;
                else if (num_slices < 2)
                    num_slices = 2;

                proxyGeom = new GameObject("Proxy Geometry");
                proxyGeom.SetActive(true);
                proxyMesh = new Mesh();
                proxyGeom.transform.SetParent(boundingBox.transform);

                mf = proxyGeom.AddComponent(typeof(MeshFilter)) as MeshFilter;
                mf.mesh = proxyMesh;

                mr = proxyGeom.AddComponent(typeof(MeshRenderer)) as MeshRenderer;

                mr.material = internalMat;
                mr.receiveShadows = false;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                mr.material = internalMat;


                // initialize the proxyVertices and Triangels
                proxyVertices = new Vector3[max_slices * 6]; // 6 vertices per slice
                proxyTriangles = new List<int[]>(); // 4 triangles in the triangle fan with 3 vertices each

                proxyMesh.vertices = proxyVertices;
                proxyMesh.subMeshCount = max_slices;

                // The paths that are followed along the edges to calculate the proxy geometry
                edgeList = new int[8, 12]{
            { 0, 1, 5, 6, 4, 8, 11, 9, 3, 7, 2, 10 },	// v0 is front
			{ 0, 4, 3, 11, 1, 2, 6, 7, 5, 9, 8, 10 },	// v1 is front
			{ 1, 5, 0, 8, 2, 3, 7, 4, 6, 10, 9, 11 }, // v2 is front
			{ 7, 11, 10, 8, 2, 6, 1, 9, 3, 0, 4, 5 },	// v3 is front
			{ 8, 5, 9, 1, 11, 10, 7, 6, 4, 3, 0, 2 },	// v4 is front
			{ 9, 6, 10, 2, 8, 11, 4, 7, 5, 0, 1, 3 },	// v5 is front
			{ 9, 8, 5, 4, 6, 1, 2, 0, 10, 7, 11, 3 }, // v6 is front
			{ 10, 9, 6, 5, 7, 2, 3, 1, 11, 4, 8, 0 }	// v7 is front
		};

                CalculateProxyGeometry();

                proxyMesh.RecalculateBounds();
                proxyMesh.RecalculateNormals();

                //Set the material properties to be the same as the slider's defaults
                for (int i = 0; i < guiSliders.Length; i++)
                {
                    currentSliderNum = i;
                    // non important initalizing code
                    if (i == (int)TSMaterialProperty.FilterThresh)
                    {
                        Slider slider = guiSliders[i].GetComponent<Slider>();
                        slider.value = setThreshold;
                        //Debug.Log(slider.value);
                        string sliderText = slider.GetComponentInChildren<UnityEngine.UI.Text>().text;
                        slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0:0.000}", slider.value);
                    }
                    else if (i == (int)TSMaterialProperty.NumSlices)
                    {
                        Slider slider = guiSliders[i].GetComponent<Slider>();
                        slider.value = num_slices;
                        //Debug.Log(slider.value);
                        string sliderText = slider.GetComponentInChildren<UnityEngine.UI.Text>().text;
                        slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0:0}", slider.value);
                    }
                    else if (i == (int)TSMaterialProperty.Opacity)
                    {
                        Slider slider = guiSliders[i].GetComponent<Slider>();
                        slider.value = setOpacity;
                        //Debug.Log(slider.value);
                        string sliderText = slider.GetComponentInChildren<UnityEngine.UI.Text>().text;
                        slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0:0.000}", slider.value);
                    }

                    else if (i == (int)TSMaterialProperty.zScale)
                    {
                        Slider slider = guiSliders[i].GetComponent<Slider>();
                        slider.value = setDepth;
                        //Debug.Log(slider.value);
                        string sliderText = slider.GetComponentInChildren<UnityEngine.UI.Text>().text;
                        slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0:0.000}", slider.value);
                    }
                    changeMatProperty(i);
                }
                currentSliderNum = -1;
                break;
            case (int)VolumeRenderingType.RayCasting:
                mainHeadingText.text = "Ray Casting";
                Debug.Log("USING RAY CASTING");
                if(proxyGeom != null)
                    proxyGeom.SetActive(false);

                InitializeSuper((int)VolumeRenderingType.RayCasting);

                boundingBoxSetup.GetComponent<Renderer>().material = internalMat;

                camPos = new Vector4[2];
                camPos[0] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
                camPos[1] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

                showChannelState = new int[] { 1, 1, 1, 1 };

                //Set the material properties to be the same as the slider's defaults
                for (int i = 0; i < guiSliders.Length; i++)
                {
                    currentSliderNum = i;
                    changeMatProperty(i);
                }
                currentSliderNum = -1;
                break;
            case (int)VolumeRenderingType.RayCastingIso:
                mainHeadingText.text = "Ray Casting";
                Debug.Log("USING RAY CASTING ISO");
                if (proxyGeom != null)
                    proxyGeom.SetActive(false);

                InitializeSuper((int)VolumeRenderingType.RayCastingIso);

                boundingBoxSetup.GetComponent<Renderer>().material = internalMat;

                camPos = new Vector4[2];
                camPos[0] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
                camPos[1] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

                showChannelState = new int[] { 1, 1, 1, 1 };

                //Set the material properties to be the same as the slider's defaults
                for (int i = 0; i < guiSliders.Length; i++)
                {
                    currentSliderNum = i;
                    changeMatProperty(i);
                }
                currentSliderNum = -1;

                internalMat.SetInt("_showRed", 1);
                internalMat.SetInt("_showGreen", 1);
                internalMat.SetInt("_showBlue", 1);
                internalMat.SetInt("_showPurple", 1);

                break;
        }
    }

    public void Update()
    {
        UpdateSuper();

        switch (renderType)
        {
            case (int)VolumeRenderingType.TextureSlicing:
                if (loadSample != null)
                {
                    if (!loadSample.doneLoadingSample && !loadSample.isPreLoaded)
                    {
                        //Debug.Log("Still busy loading sample");
                        return;
                    }
                }

                // verify that num_slices is in the valid bounds
                if (num_slices > max_slices)
                    num_slices = max_slices;
                else if (num_slices < 2)
                    num_slices = 2;

                Matrix4x4 rotationMat = Matrix4x4.TRS(Vector3.zero, boundingBoxSetup.transform.rotation, Vector3.one);
                viewDir = rotationMat.inverse * (-mainCamera.transform.forward);

                // this only takes some load off the CPU but creates jumping artifacts
                //if(frameCount++ > 10)
                {
                    CalculateProxyGeometry();
                    //frameCount = 0;
                }
                //proxyMesh.vertices = boxVertices.ToArray(); // proxyVertices

                //TODO: Only use this if I'm not using a GameObject, thus no meshrenderer
                //	for(int i = 0; i < proxyTriangles.Count; i++)
                //        Graphics.DrawMesh (proxyMesh, Vector3.zero, Quaternion.identity, mat, proxyGeom.layer, null, i);


                //TODO: should be a different function for screenshot
                if (Input.GetButtonUp("Submit"))
                {
                    Export3DButtonPressed();
                    
                }
                break;
            case (int)VolumeRenderingType.RayCasting:
            case (int)VolumeRenderingType.RayCastingIso:
                /*
                if(mainCamera.GetComponent<Camera>().stereoTargetEye == StereoTargetEyeMask.Left)
                    internalMat.SetInt("_currentEye", 0);
                else if (mainCamera.GetComponent<Camera>().stereoTargetEye == StereoTargetEyeMask.Right)
                    internalMat.SetInt("_currentEye", 1);
                            
                currentEye = 1 - currentEye;
                */

                //Debug.Log("Update");

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
                break;
        }

        if(BenProject && !autostarted)
        {
            autostarted = true;
            StartCoroutine(autoRunAnalysis());
        }

        // this if should be deleted
        if (!autostarted)
        {
            autostarted = true;
            StartCoroutine(autoRunAnalysis());
        }
    }

    // Use this for initialization
    //public VolumeVisualizer() 
    protected void InitializeSuper(int type)
    {
        GameObject persistentData = GameObject.Find("PersistentData");

        if (persistentData == null && loadSample == null)
        {
            Debug.Log("No persistent data transferred");
            loadSample = new LoadSample(size, internalMat, true);
            size = loadSample.sampleSize;
        }
        else if (persistentData != null)
        {            
            Debug.Log("Got persistent data");
            loadSample = persistentData.GetComponent<LoadSample>();
            thisImageFolderStructure = loadSample.samplePath.Substring(loadSample.samplePath.IndexOf("Output") + 6);
            Debug.Log("thisImageFolderStructure " + thisImageFolderStructure);

            thisImageNameBen = thisImageFolderStructure.Substring(0, thisImageFolderStructure.Length - 1);
            thisImageNameBen = thisImageNameBen.Substring(thisImageNameBen.LastIndexOf("\\"));

            // create 2D and 3D paths
            string tempPath = saveLocation3D + thisImageFolderStructure;
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);
            tempPath = saveLocation2D + thisImageFolderStructure;
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);
        }
        else if (loadSample != null)
        {
            loadSample.InitExistingSample();
        }

        renderType = type;
        CM = new ColorMaps();
        channelNames = new string[] { "Red", "Green", "Blue", "Purple" };
        /*
        ShowAllMask = new Texture3D(1, 1, 1, TextureFormat.RGB24, false);
        ShowAllMask.SetPixels(new Color[] { Color.white });
        ShowAllMask.Apply();*/

        // initialize game components
        boundingBoxSetup = boundingBox.GetComponent<BoundingBoxSetup>();
        boundingBoxSetup.boxDim.z = setDepth;


        ROIBoxSetup = ROIBox.GetComponent<ROIBoxSetup>();
        ROICylinderSetup = ROICylinder.GetComponent<ROIBoxSetup>();
        ROISphereSetup = ROISphere.GetComponent<ROIBoxSetup>();
        ROIFreehandPath = new List<GameObject>();
        ROIFreehandVertices = new List<Vector3>();
        ROIFreehandTriangles = new List<int>();
        //freehandPolygon = new List<FreehandPolygon> ();
        ROIscaleAxis = new Vector3(1f, 1f, 1f);

        activeROIToolsGlobal = new List<GameObject>();
        activeROIToolsInside = new List<GameObject>();
        otherOptions = new List<Dropdown.OptionData>();

        activeROITools = activeROIToolsGlobal;
        activeROITools.Add((GameObject)Instantiate(ROIBox));
        //activeROITools[activeROITools.Count - 1].SetActive(true);
        activeROITools[activeROITools.Count - 1].transform.SetParent(ROIBox.transform.parent);
        currentROITool = activeROITools[activeROITools.Count - 1];

        previousFreehandPointTime = Time.realtimeSinceStartup;

        // this code is necessary otherwise a bug occurs where the translation and scale is not transfered to the other component (since the scripts are only run after first enable)
        ROIBox.SetActive(true);
        ROIBox.SetActive(false);
        ROICylinder.SetActive(true);
        ROICylinder.SetActive(false);
        ROIFreehand.SetActive(true);
        ROIFreehand.SetActive(false);
        ROISphere.SetActive(true);
        ROISphere.SetActive(false);

        /*
        ROIMask_XY = new bool[size, size];
        // start by initializing the mask to all true (the ROI selector will make some sections false
        for (int row = 0; row < ROIMask_XY.GetLength(0); row++)
            for (int col = 0; col < ROIMask_XY.GetLength(1); col++)
                ROIMask_XY[row, col] = true;

        ROIMask_XZ = new bool[size, MaskZDepth];
        //Debug.Log("ROIMaskXZ dim: " + ROIMask_XZ.GetLength(0) + "   " + ROIMask_XZ.GetLength(1));
        // start by initializing the mask to all true (the ROI selector will make some sections false
        for (int row = 0; row < ROIMask_XZ.GetLength(0); row++)
            for (int col = 0; col < ROIMask_XZ.GetLength(1); col++)
                ROIMask_XZ[row, col] = true;

        ROIMask_YZ = new bool[size, MaskZDepth];
        // start by initializing the mask to all true (the ROI selector will make some sections false
        for (int row = 0; row < ROIMask_YZ.GetLength(0); row++)
            for (int col = 0; col < ROIMask_YZ.GetLength(1); col++)
                ROIMask_YZ[row, col] = true;
        
        
        StartCoroutine(ROIMaskToTexture());
        */


        // TODO: a better way to do this might be to use a matrix?... 
        // put all the box vertices in a List for easy modification later on
        boxVertices = new List<Vector3>();
        foreach (Vector3 vert in boundingBoxSetup.actualBoxVertices)
            boxVertices.Add(vert);//new Vector3(vert.x * everything.transform.localScale.x, vert.y * everything.transform.localScale.y, vert.z * everything.transform.localScale.z));


        switch (renderType)
        {
            case (int)VolumeRenderingType.TextureSlicing:
                Material blank = new Material(Shader.Find("Unlit/TransparentParam"));
                blank.SetColor("_tint", new Color(0.0f, 0.0f, 0.0f, 0.0f));
                boundingBoxSetup.GetComponent<MeshRenderer>().sharedMaterial.shader = blank.shader;

                internalMat = Material.Instantiate(TextureSlicingMat);
                break;
            case (int)VolumeRenderingType.RayCasting:
                //internalMat = new Material(Shader.Find("Standard"));
                internalMat = Material.Instantiate(RayCastingMat);
                boundingBoxSetup.GetComponent<MeshRenderer>().sharedMaterial.shader = internalMat.shader;

                //internalMat = new Material(Shader.Find("Standard"));
                //internalMat = boundingBoxSetup.GetComponent<MeshRenderer>().sharedMaterial;
                break;
            case (int)VolumeRenderingType.RayCastingIso:
                //internalMat = new Material(Shader.Find("Volume Rendering/Ray Casting Iso Surface"));
                internalMat = Material.Instantiate(RayCastingIsoSurfaceMat);
                boundingBoxSetup.GetComponent<MeshRenderer>().sharedMaterial.shader = internalMat.shader;
                break;
        }

        internalMat.SetVector("_BoxDim", new Vector4(boundingBoxSetup.boxDim.x, boundingBoxSetup.boxDim.y, boundingBoxSetup.boxDim.z, 0.0f));
        internalMat.SetVector("_ROI_P1", new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
        internalMat.SetVector("_ROI_P2", new Vector4(1.0f, 1.0f, 1.0f, 0.0f));

        ROI3DMaskTexture = new Texture3D(1, 1, 1, TextureFormat.RGB24, false);
        ROI3DMaskTexture.SetPixels(new Color[] { Color.white });
        ROI3DMaskTexture.Apply();
        internalMat.SetTexture("_ROIMask3D", ROI3DMaskTexture);

        // initialize colocalization methods
        colocDropdownOptions = new List<string> { "0 - Red", "1 - Green", "2 - Blue", "3 - Purple" };


        thresholdHighValue = new float[4] { 0.20f, 0.20f, 0.20f, 0.20f };
        thresholdLowValue = new float[4] { 0.040f, 0.040f, 0.040f, 0.040f };
        thresholdHighMin = new float[4] { 0.040f, 0.040f, 0.040f, 0.040f };
        thresholdLowMin = new float[4] { 0.0f, 0.0f, 0.0f, 0.0f };
        thresholdHighMax = new float[4] { 1f, 1f, 1f, 1f };
        thresholdLowMax = new float[4] { 0.20f, 0.20f, 0.20f, 0.20f };

        //string[] thesholdLines = File.ReadAllLines(@"Assets\Resources\Data\thresholds.dat");
        string[] thresholdLines = File.ReadAllLines(Application.persistentDataPath + "\\thresholds.txt");
        int threshHighVal0 = 0, threshHighVal1 = 0, threshLowVal0 = 0, threshLowVal1 = 0;
        if (thresholdLines.Length != 0)
        {
            try
            {
                if (!int.TryParse(thresholdLines[0].Substring(thresholdLines[0].IndexOf("=") + 1), out thresholdMaxInt))
                {
                    Debug.Log("thresholdMaxInt could not be initialized from file");
                    throw new Exception();
                }


                for (int ch = 0; ch < 4; ch++)
                {
                    string line = thresholdLines[1 + ch * 2];

                    int low, high;
                    if (!int.TryParse(line.Substring(line.IndexOf("=") + 1), out low))
                    {
                        throw new Exception();
                    }

                    line = thresholdLines[1 + ch * 2 + 1];
                    if (!int.TryParse(line.Substring(line.IndexOf("=") + 1), out high))
                    {
                        throw new Exception();
                    }

                    thresholdLowValue[ch] = (float)low / thresholdMaxInt;
                    thresholdHighValue[ch] = (float)high / thresholdMaxInt;
                    thresholdLowMax[ch] = thresholdHighValue[ch];
                    thresholdHighMin[ch] = thresholdLowValue[ch];

                    //Debug.Log("Channel " + ch + "  Low: " + low + "  High: " + high);
                    //Debug.Log(thresholdLowValue[ch]);

                    //Debug.Log(thresholdHighValue[ch] * thresholdMaxInt);

                    if (ch == 0)
                    {
                        threshHighVal0 = high;
                        threshLowVal0 = low;
                    }
                    if (ch == 1)
                    {
                        threshHighVal1 = high;
                        threshLowVal1 = low;
                    }


                }
                //Debug.Log("before" + thresholdHighValue[0] * thresholdMaxInt);
                // update the values to those that were saved
                colocSliders[(int)ColocMatProperties.chan0HighThres].GetComponent<Slider>().maxValue = thresholdHighMax[0] * thresholdMaxInt;
                colocSliders[(int)ColocMatProperties.chan0HighThres].GetComponent<Slider>().minValue = thresholdHighMin[0] * thresholdMaxInt;
                colocSliders[(int)ColocMatProperties.chan0LowThres].GetComponent<Slider>().maxValue = thresholdLowMax[0] * thresholdMaxInt;
                colocSliders[(int)ColocMatProperties.chan0HighThres].GetComponent<Slider>().value = threshHighVal0;//thresholdHighValue[0] * thresholdMaxInt;
                colocSliders[(int)ColocMatProperties.chan0LowThres].GetComponent<Slider>().value = threshLowVal0;// thresholdLowValue[0] * thresholdMaxInt;


                //Debug.Log("after" + thresholdHighValue[0] * thresholdMaxInt);

                //Debug.Log(colocSliders[(int)ColocMatProperties.chan0HighThres].GetComponent<Slider>().maxValue);
                //Debug.Log(colocSliders[(int)ColocMatProperties.chan0HighThres].GetComponent<Slider>().value);
                // update the values to those that were saved
                colocSliders[(int)ColocMatProperties.chan1HighThres].GetComponent<Slider>().maxValue = thresholdHighMax[1] * thresholdMaxInt;
                colocSliders[(int)ColocMatProperties.chan1HighThres].GetComponent<Slider>().minValue = thresholdHighMin[1] * thresholdMaxInt;
                colocSliders[(int)ColocMatProperties.chan1LowThres].GetComponent<Slider>().maxValue = thresholdLowMax[1] * thresholdMaxInt;
                colocSliders[(int)ColocMatProperties.chan1HighThres].GetComponent<Slider>().value = threshHighVal1;//thresholdHighValue[1] * thresholdMaxInt;
                colocSliders[(int)ColocMatProperties.chan1LowThres].GetComponent<Slider>().value = threshLowVal1;// thresholdLowValue[1] * thresholdMaxInt;


            }
            catch (Exception e)
            {
                Debug.Log("Problem processing threshold file. Used default values. " + e.Message);
            }
        }


        channelAverage = new double[2];

        //reset the shader
        internalMat.SetInt("_colChannel0", colocChannel0);
        internalMat.SetInt("_colChannel1", colocChannel1);
        internalMat.SetInt("_colocalizationMethod", colocalizationMethod);
        internalMat.SetInt("_colThresIntervalDisplay", colocThresIntervalDisplay);

        mipMat.SetInt("_colChannel0", colocChannel0);
        mipMat.SetInt("_colChannel1", colocChannel1);
        mipMat.SetInt("_colocalizationMethod", colocalizationMethod);
        mipMat.SetInt("_colThresIntervalDisplay", colocThresIntervalDisplay);

        internalMat.SetFloat("_nmdp_minMultFactor", setnMDPMin*255);
        internalMat.SetFloat("_nmdp_maxMultFactor", setnMDPMax*255);
        internalMat.SetFloat("_angle", setAngle);

        //Set the material properties to be the same as the slider's defaults
        for (int i = 0; i < colocSliders.Length; i++)
        {
            currentSliderNum = i;

            // non important initializing code
            if (i == (int)ColocMatProperties.MaxValue)
            {
                Slider slider = colocSliders[i].GetComponent<Slider>();
                slider.value = setMax/thresholdMaxInt;
                maxHeatmapValue = (int)setMax;
                //Debug.Log(slider.value);
                string sliderText = slider.GetComponentInChildren<UnityEngine.UI.Text>().text;
                slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0:0.000}", slider.value);
            }
            if (i == (int)ColocMatProperties.ColocOpacity)
            {
                Slider slider = colocSliders[i].GetComponent<Slider>();
                slider.value = setColocOpacity;

                //Debug.Log(slider.value);
                string sliderText = slider.GetComponentInChildren<UnityEngine.UI.Text>().text;
                slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0:0.000}", slider.value);
            }


            changeColocMatProperty(i);
        }
        currentSliderNum = -1;

        //colocalization2DLayer = new Color[size * size];

        // Load the new volume data
        if (separateMIPChannels)
        {
            //Load3DTextureSeparate (@"Assets\microDat\Set1\");
            //Load3DTextureSeparate (@"Assets\microDat\Yigael1\Test\");
            //Load3DTextureSeparate (@"Assets\microDat\Yigael1\6 hour\");

            //Load3DTextureSeparate(@"Assets\microDat\Yigael2\6 hour\", "*.png");
            //loadSample.LoadSampleSeperate(@"Assets\microDat\Yigael2\Control\", "*.png");

            /*
            loadSample.texSeparate[0] = (Texture3D)Resources.Load("Textures/Yig_red.tex");
            loadSample.texSeparate[1] = (Texture3D)Resources.Load("Textures/Yig_green.tex");
            loadSample.texSeparate[2] = (Texture3D)Resources.Load("Textures/Yig_blue.tex");
            loadSample.texSeparate[3] = (Texture3D)Resources.Load("Textures/Yig_purple.tex");

            
            loadSample.texSeparate[0].Apply();
            loadSample.texSeparate[1].Apply();
            loadSample.texSeparate[2].Apply();
            loadSample.texSeparate[3].Apply();

            loadSample.internalMaterial.SetTexture ("_VolumeRed", loadSample.texSeparate[0]);
            loadSample.internalMaterial.SetTexture ("_VolumeGreen", loadSample.texSeparate[1]);
            loadSample.internalMaterial.SetTexture ("_VolumeBlue", loadSample.texSeparate[2]);
            loadSample.internalMaterial.SetTexture ("_VolumePurple", loadSample.texSeparate[3]);
            */

            
            loadSample.texSeparate[0] = (Texture3D)internalMat.GetTexture("_VolumeRed");
            loadSample.texSeparate[1] = (Texture3D)internalMat.GetTexture("_VolumeGreen");
            loadSample.texSeparate[2] = (Texture3D)internalMat.GetTexture("_VolumeBlue");
            loadSample.texSeparate[3] = (Texture3D)internalMat.GetTexture("_VolumePurple");

            try
            {
                redPixels2D = loadSample.texSeparate[0].GetPixels();
                greenPixels2D = loadSample.texSeparate[1].GetPixels();
                bluePixels2D = loadSample.texSeparate[2].GetPixels();
                purplePixels2D = loadSample.texSeparate[3].GetPixels();

                loadSample.texDepth = loadSample.texSeparate[0].depth;
            }
            catch (Exception e)            
            {
                Debug.LogError(e.Message);
            }

/*
            Debug.Log("TEST: " + loadSample.texSeparate[0]);
            Debug.Log("TEST2 " + loadSample.internalMaterial.GetTexture("_VolumeRed"));
            Debug.Log("TEST3 " + internalMat.GetTexture("_VolumeRed"));
            Debug.Log("TEST4 " + givenMat.GetTexture("_VolumeRed"));
            */

            //Debug.Log("Tex Depth: " + loadSample.texDepth);
            //Debug.Log("Sample Dim: " + loadSample.sampleDim);
            if (loadSample.zRatio <= 0)
            {
                Debug.LogWarning("zRation was 0");
                loadSample.zRatio = 0.5f;
            }
            //Debug.Log("z ratio: " + loadSample.zRatio);
            //Debug.Log("(loadSample.sampleDim.z/ loadSample.sampleDim.x)* loadSample.texDepth * loadSample.zRatio = " + (loadSample.sampleDim.z / loadSample.sampleDim.x) * loadSample.texDepth * loadSample.zRatio);

            //boundingBox.transform.localScale = new Vector3(loadSample.sampleDim.x / loadSample.sampleDim.x, loadSample.sampleDim.y / loadSample.sampleDim.x, (loadSample.sampleDim.z / loadSample.sampleDim.x) * loadSample.texDepth * loadSample.zRatio);
            boundingBox.transform.localScale = new Vector3(loadSample.sampleDim.x / loadSample.sampleDim.x, loadSample.sampleDim.y / loadSample.sampleDim.x, setDepth);
            //Debug.Log(boundingBox.transform.localScale);
            initialBoxRatio = boundingBox.transform.localScale.normalized;

            prevGlobalBoxScale = boundingBox.transform.localScale;
            previousBoxScale = boundingBox.transform.localScale;

            // initialize the slice select slider
            SliceSelectPanel.GetComponentInChildren<Slider>().maxValue = loadSample.texDepth - 1;
            SliceSelectPanel.GetComponentInChildren<Slider>().minValue = 0;
            SliceSelectPanel.GetComponentInChildren<Slider>().value = loadSample.texDepth / 2;
            currentSlice = loadSample.texDepth / 2;

            ROIMask3D = new bool[size, size, ROIMaskDepth];
            // start by initializing the mask to all true (the ROI selector will make some sections false
            for (int x = 0; x < ROIMask3D.GetLength(0); x++)
                for (int y = 0; y < ROIMask3D.GetLength(1); y++)
                    for (int z = 0; z < ROIMask3D.GetLength(2); z++)
                        ROIMask3D[x, y, z] = true;

            ROIMask2D = new bool[size, size];
            // start by initializing the mask to all true (the ROI selector will make some sections false
            for (int x = 0; x < ROIMask2D.GetLength(0); x++)
                for (int y = 0; y < ROIMask2D.GetLength(1); y++)
                        ROIMask2D[x, y] = true;

            internalMat.SetFloat("_useROIMask", 0.0f);
            // StartCoroutine(ROIMaskToTexture());

        }
        else // this is an obsolete method of importing the files
        {            
            //Load3DTexture (@"Assets\microDat\Bacteria\");
            //tex = (Texture3D)Resources.Load("Assets/Textures/3dTex.tex"); //3dTex //torus
        }

        if (showMIP)
        {
            MIPQuad.SetActive(true);
            setupMIP();
        }

        Debug.Log("Done initializing super");
    }

    // Update is called once per frame
    protected void UpdateSuper()
    {
        // close application when Esc key pressed
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();

        //If V is pressed, toggle VRSettings.enabled
        if (Input.GetKeyDown(KeyCode.V))
        {
            VRSettings.enabled = !VRSettings.enabled;
            Debug.Log("Changed VRSettings.enabled to:" + VRSettings.enabled);
        }
        
        //get input from the gamepad to interact with the rendering
        if (!useROI)
            gamePadInput();
        else
            ROIGamePadInput();

        //TODO: should be a different function for screenshot
        if (Input.GetButtonUp("Shift"))
        {
            //CaptureView("MIP");
            //GenerateCompleteMIP();
            //StartCoroutine(GenerateCompleteMIPWithColoc(onlyInsideROI));

            //Debug.Log("EXPORT MIP AND 2D");
            //StartCoroutine(GenerateCompleteMIP(onlyInsideROI, true, showColoc2D, seperateChannelsExport2D, numChannelsToExport, showHeatMap, showMIPColors));
            
            
            //StartCoroutine(GenerateCompleteMIP_v2(onlyInsideROI));
            StartCoroutine(exportHeatmapAndNMDPSlices(onlyInsideROI));

            //StartCoroutine(Generate2DSlice(onlyInsideROI, true, showColoc2D, seperateChannelsExport2D, numChannelsToExport));
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            //CaptureView("MIP");
            //GenerateCompleteMIP();
            //StartCoroutine(GenerateCompleteMIPWithColoc(onlyInsideROI));

            //Debug.Log("EXPORT MIP AND 2D");
            //StartCoroutine(GenerateCompleteMIP(onlyInsideROI, true, showColoc2D, seperateChannelsExport2D, numChannelsToExport, showHeatMap, showMIPColors));


            StartCoroutine(GenerateCompleteMIP_v2(onlyInsideROI));
            //StartCoroutine(exportHeatmapAndNMDPSlices(onlyInsideROI));

            //StartCoroutine(Generate2DSlice(onlyInsideROI, true, showColoc2D, seperateChannelsExport2D, numChannelsToExport));
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (mainMenu.activeInHierarchy) // change to coloc menu
            {
                colocalizationActive = true;

                mainMenu.SetActive(false);
                colocMenu.SetActive(true);

                // show colocalization
                colocalizationMethod = colocMethodDropdown.GetComponent<Dropdown>().value;
                internalMat.SetInt("_colocalizationMethod", colocalizationMethod);
                mipMat.SetInt("_colocalizationMethod", colocalizationMethod);

            }

            calcColocButtonPressed();
        }

        if (Input.GetButtonUp("Swap_coloc_method"))
        {
            int currentMethod = internalMat.GetInt("_colocalizationMethod");
            int newMethod = currentMethod;
            if(currentMethod == (int)ColocMethod.OnlyColocHeatmap)
            {
                newMethod = (int)ColocMethod.OnlyColocNMDP;
            }
            else if(currentMethod == (int)ColocMethod.OnlyColocNMDP)
            {
                newMethod = (int)ColocMethod.OnlyColocNMDPSobel;
            }
            else if (currentMethod == (int)ColocMethod.OnlyColocNMDPSobel)
            {
                newMethod = (int)ColocMethod.OnlyColocHeatmap;
            }

            colocMethodDropdown.GetComponent<Dropdown>().value = newMethod;
            internalMat.SetInt("_colocalizationMethod", newMethod);
            mipMat.SetInt("_colocalizationMethod", newMethod);

        }


        if (!updateMaskRunning && maskShouldUpdate)// && showROIMask)// && ROIMaskQuadYZ.activeInHierarchy)
        {
            StartCoroutine(updateROIMask());
        }

        // register a LeapMotion button click
        if (buttonClicked)
        {
            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(currentButton, pointer, ExecuteEvents.pointerClickHandler);

            buttonClicked = false;
        }

        // register a LeapMotion dropdown click
        if (dropdownClicked)
        {
            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(currentDropdown, pointer, ExecuteEvents.pointerClickHandler);

            dropdownClicked = false;
        }

        // MIP initialization
        showMIP = MIPQuad.activeInHierarchy;
        if (!MIPMatInitialized && showMIP)
            setupMIP();
    }

    #region input



    void gamePadInput()
    {
        BoundingBoxSetup boxUsed = boundingBox.GetComponent<BoundingBoxSetup>();
        /*
		if(!useROI)
			boxUsed = boundingBox.GetComponent<BoundingBoxSetup>();
		else
			boxUsed = ROIBox.GetComponent<BoundingBoxSetup>();
			*/

        float leftX = Input.GetAxis("Oculus_GearVR_LThumbstickX");
        float leftY = Input.GetAxis("Oculus_GearVR_LThumbstickY");
        float rightX = Input.GetAxis("Oculus_GearVR_RThumbstickX");
        float rightY = Input.GetAxis("Oculus_GearVR_RThumbstickY");

        bool leftTriggerPressed = Input.GetButton("Jump");
        bool rightTriggerPressed = Input.GetButton("Fire2");
        bool scaleTriggerPressed = Input.GetButton("Fire3");
        bool toggleROISelect = Input.GetButtonUp("ROI_Toggle");


        if (leftX != 0f || leftY != 0f || rightX != 0f || rightY != 0f)
        {
            //Debug.Log("INSIDE: "  + leftY * 100);
            //use the left joystick for both translation and scaling
            if (!scaleTriggerPressed) // Translations
            {
                // if modifier button pressed then move in z instead of y
                if (!leftTriggerPressed)
                {
                    //Debug.Log(leftY * 100);                    
                    boxUsed.transform.Translate(new Vector3(leftX, leftY, 0.0f) * Time.deltaTime * translationSpeed, Space.World);
                }
                else
                {
                    boxUsed.transform.Translate(new Vector3(leftX, 0.0f, leftY) * Time.deltaTime * translationSpeed, Space.World);
                }
            }
            else // scaling
            {
                boxUsed.transform.localScale += new Vector3(leftY * boxUsed.boxDimRatio.x, leftY * boxUsed.boxDimRatio.y, leftY * boxUsed.boxDimRatio.z) * Time.deltaTime * scalingSpeed;

                // ensure that it doesn't invert
                //TODO: this should probably not be hardcoded
                if (boxUsed.transform.localScale.x < 0.1f * boxUsed.boxDimRatio.x || boxUsed.transform.localScale.y < 0.1f * boxUsed.boxDimRatio.y || boxUsed.transform.localScale.z < 0.1f * boxUsed.boxDimRatio.z)
                    boxUsed.transform.localScale = new Vector3(0.1f * boxUsed.boxDimRatio.x, 0.1f * boxUsed.boxDimRatio.y, 0.1f * boxUsed.boxDimRatio.z);
            }

            // rotation
            if (!rightTriggerPressed)
            {
                boxUsed.transform.Rotate(Vector3.up, -rightX * Time.deltaTime * rotationSpeed, Space.World);
                boxUsed.transform.Rotate(Vector3.right, rightY * Time.deltaTime * rotationSpeed, Space.World);
            }
            else
            {
                boxUsed.transform.Rotate(Vector3.forward, -rightX * Time.deltaTime * rotationSpeed, Space.World);
                boxUsed.transform.Rotate(Vector3.right, rightY * Time.deltaTime * rotationSpeed, Space.World);
            }
        }

        if (toggleROISelect && colocalizationActive)
            ROIButtonPressed();

        // changing the sliders
        if (currentSliderNum != -1)
        {
            Slider slider = null;
            if (!colocalizationActive)
            {
                slider = guiSliders[currentSliderNum].GetComponent<Slider>();

            }
            else
            {
                slider = colocSliders[currentSliderNum].GetComponent<Slider>();
            }

            float sliderChange = Input.GetAxis("Oculus_GearVR_DpadX") * (slider.maxValue - slider.minValue) * sliderChangeSensitivity * Time.deltaTime;

            // this ensures it will work even for whole numbers
            if (slider.wholeNumbers && Mathf.Abs(sliderChange) > 0.0f && (int)Mathf.Abs(sliderChange) == 0)
                slider.value += Mathf.Sign(sliderChange);

            slider.value += sliderChange;
        }
    }

    void ROIGamePadInput()
    {
        ROIBoxSetup toolUsed = currentROITool.GetComponent<ROIBoxSetup>();

        float leftX = Input.GetAxis("Oculus_GearVR_LThumbstickX");
        float leftY = Input.GetAxis("Oculus_GearVR_LThumbstickY");
        float rightX = Input.GetAxis("Oculus_GearVR_RThumbstickX");
        float rightY = Input.GetAxis("Oculus_GearVR_RThumbstickY");

        bool leftTriggerPressed = Input.GetButton("Jump");
        bool selectButtonPressed = Input.GetButton("Fire1");
        bool rightTriggerPressed = Input.GetButton("Fire2");
        bool scaleTriggerPressed = Input.GetButton("Fire3");
        bool toggleROISelect = Input.GetButtonUp("ROI_Toggle");

        if ((leftX != 0f || leftY != 0f || rightX != 0f || rightY != 0f) && (currentROITool != ROIFreehand))
        {
            //use the left joystick for both translation and scaling
            if (!scaleTriggerPressed) // Translations
            {
                if (ROITranslateTogether)
                {
                    for (int i = 0; i < activeROITools.Count; i++)
                    {
                        ROIBoxSetup toolUsedLoop = activeROITools[i].GetComponent<ROIBoxSetup>();
                        // if modifier button pressed then move in z instead of y
                        if (ROILockAxisTranslation)
                        {
                            toolUsedLoop.transform.Translate(Vector3.Scale(new Vector3(leftX, leftY, -leftY), ROIscaleAxis) * Time.deltaTime * translationSpeed, Space.Self);
                        }
                        else
                        {
                            if (!leftTriggerPressed)
                            {
                                toolUsedLoop.transform.Translate(new Vector3(leftX, leftY, 0.0f) * Time.deltaTime * translationSpeed, Space.World);
                            }
                            else
                            {
                                toolUsedLoop.transform.Translate(new Vector3(leftX, 0.0f, leftY) * Time.deltaTime * translationSpeed, Space.World);
                            }
                        }
                        // if the ROI as a whole is scaled lock the z axis
                        if (!scaleROIAxis)
                            toolUsedLoop.transform.localPosition = new Vector3(toolUsed.transform.localPosition.x, toolUsed.transform.localPosition.y, 0.0f);
                    }
                }
                else
                {
                    // if modifier button pressed then move in z instead of y
                    if (ROILockAxisTranslation)
                    {
                        toolUsed.transform.Translate(Vector3.Scale(new Vector3(leftX, leftY, -leftY), ROIscaleAxis) * Time.deltaTime * translationSpeed, Space.Self);
                    }
                    else
                    {
                        if (!leftTriggerPressed)
                        {
                            toolUsed.transform.Translate(new Vector3(leftX, leftY, 0.0f) * Time.deltaTime * translationSpeed, Space.World);
                        }
                        else
                        {
                            toolUsed.transform.Translate(new Vector3(leftX, 0.0f, leftY) * Time.deltaTime * translationSpeed, Space.World);
                        }
                    }
                    // if the ROI as a whole is scaled lock the z axis
                    if (!scaleROIAxis)
                        toolUsed.transform.localPosition = new Vector3(toolUsed.transform.localPosition.x, toolUsed.transform.localPosition.y, 0.0f);
                }
            }
            else // scaling
            {
                if (ROITranslateTogether)
                {
                    for (int i = 0; i < activeROITools.Count; i++)
                    {
                        ROIBoxSetup toolUsedLoop = activeROITools[i].GetComponent<ROIBoxSetup>();
                        toolUsedLoop.transform.localScale += new Vector3(leftY * toolUsedLoop.boxDimRatio.x * ROIscaleAxis.x, leftY * toolUsedLoop.boxDimRatio.y * ROIscaleAxis.y, leftY * toolUsedLoop.boxDimRatio.z * ROIscaleAxis.z) * Time.deltaTime * scalingSpeed;
                    }
                }
                else
                {
                    toolUsed.transform.localScale += new Vector3(leftY * toolUsed.boxDimRatio.x * ROIscaleAxis.x, leftY * toolUsed.boxDimRatio.y * ROIscaleAxis.y, leftY * toolUsed.boxDimRatio.z * ROIscaleAxis.z) * Time.deltaTime * scalingSpeed;
                }
            }

            //Debug.Log("start: " + startZ + " end: " + endZ);

            // rotation shouldn't work for ROI selection, since the box is a child of the bounding box. Therefore rotate the bounding box unless pressing the trigger button
            if (!rightTriggerPressed)
            {
                boundingBox.transform.Rotate(Vector3.up, -rightX * Time.deltaTime * rotationSpeed, Space.World);
                boundingBox.transform.Rotate(Vector3.right, rightY * Time.deltaTime * rotationSpeed, Space.World);
            }
            else
            {
                if (currentROITool.CompareTag("ROICylinder") || currentROITool.CompareTag("ROISphere"))
                {
                    //toolUsed.transform.Rotate(Vector3.forward, -rightX * Time.deltaTime * rotationSpeed, Space.World);
                    toolUsed.transform.Rotate(Vector3.forward, -rightX * Time.deltaTime * rotationSpeed, Space.Self);
                    //toolUsed.transform.Rotate(Vector3.right, rightY * Time.deltaTime * rotationSpeed, Space.World);
                }
            }

            checkROIBounds();
            //previousBoxScale = boundingBoxSetup.boxDim;
        }

        // only z scaling should happen with the freehand tool
        if ((leftX != 0f || leftY != 0f || rightX != 0f || rightY != 0f) && (currentROITool.CompareTag("ROIFreehand")))
        {
            //use the left joystick for both translation and scaling
            if (!scaleTriggerPressed) // Translations
            {
                // if modifier button pressed then move in z instead of y
                if (!leftTriggerPressed)
                {
                    toolUsed.transform.Translate(new Vector3(leftX, leftY, 0.0f) * Time.deltaTime * translationSpeed, Space.World);
                }
                else
                {
                    toolUsed.transform.Translate(new Vector3(leftX, 0.0f, leftY) * Time.deltaTime * translationSpeed, Space.World);
                }
            }
            else // scaling
            {
                toolUsed.transform.localScale += new Vector3(0f, 0f, leftY * toolUsed.boxDimRatio.z * ROIscaleAxis.z) * Time.deltaTime * scalingSpeed;
            }

            //Debug.Log("start: " + startZ + " end: " + endZ);

            // rotation shouldn't work for ROI selection, since the box is a child of the bounding box. Therefore rotate the bounding box
            if (!rightTriggerPressed)
            {
                boundingBox.transform.Rotate(Vector3.up, -rightX * Time.deltaTime * rotationSpeed, Space.World);
                boundingBox.transform.Rotate(Vector3.right, rightY * Time.deltaTime * rotationSpeed, Space.World);
            }
            else
            {
                boundingBox.transform.Rotate(Vector3.forward, -rightX * Time.deltaTime * rotationSpeed, Space.World);
                boundingBox.transform.Rotate(Vector3.right, rightY * Time.deltaTime * rotationSpeed, Space.World);
            }

            checkROIBounds();

        }


        if (toggleROISelect && colocalizationActive)
            ROIButtonPressed();

        //TODO: this should probably be in a different function
        // for the freehand tool instantiate an edge (looking at bounding box and selection button pressed)
        //TODO: there is still a bug that if I draw after the z-axis has been scaled or translated that it draws with the wrong depth...  So I should actually disable drawing after z has changed...
        if (currentROITool.CompareTag("ROIFreehand") && (selectButtonPressed || fingerDrawing) && boundingBoxHighlighted && (Time.realtimeSinceStartup - previousFreehandPointTime > 0.09f) && boundingBox.transform.position == new Vector3(0f, 0f, 0f) && boundingBox.transform.rotation == Quaternion.identity && !scaleROIAxis) //TODO: this time factor determines the point draw rate
        {
            MeshFilter mf = currentROITool.GetComponent<MeshFilter>();
            Mesh freehandMesh = mf.mesh;

            ROIFreehandTriangles = new List<int>(freehandMesh.GetTriangles(0));
            ROIFreehandVertices = new List<Vector3>(freehandMesh.vertices);

            previousFreehandPointTime = Time.realtimeSinceStartup;
            float usageScale = 1.0f;// this scale factor is necessary since the leap motion scales everuthing down
            Vector3 hitPos;
            if (fingerDrawing)
            {
                hitPos = fingerDrawPos;
                usageScale = 0.5f;
            }
            else
            {
                hitPos = ROIFreehandHitPos;
                usageScale = 1.0f;
            }

            GameObject pathPoint = (GameObject)Instantiate(ROIFreehandEdge, ROIFreehandHitPosUnscaled, Quaternion.identity);
            //pathPoint.transform.localScale = new Vector3(0.005f, 0.005f,1.005f);	// TODO: this shouldn't be necessary?!
            pathPoint.transform.SetParent(currentROITool.transform);
            pathPoint.transform.localScale = new Vector3(pathPoint.transform.localScale.x, pathPoint.transform.localScale.y, toolUsed.transform.localScale.z);

            ROIFreehandPath.Add(pathPoint);

            // TODO: these lines of code doesn't actually quite work yet...
            float halfLength = toolUsed.transform.localScale.z / 2.0f;
            float startPos = toolUsed.transform.localPosition.z;

            // this is used to calculate the mesh
            ROIFreehandVertices.Add(hitPos / usageScale + new Vector3(0f, 0f, startPos + halfLength));
            ROIFreehandVertices.Add(hitPos / usageScale + new Vector3(0f, 0f, startPos - halfLength));

            // this is used to calculate the mask
            currentROITool.GetComponent<FreehandPolygon>().Add(hitPos / usageScale);

            // it is necessary that there are at least three vertices to start off
            while (ROIFreehandPath.Count < 3)
            {
                GameObject pathPoint2 = (GameObject)Instantiate(ROIFreehandEdge, hitPos, Quaternion.identity);
                pathPoint2.transform.SetParent(currentROITool.transform);
                pathPoint2.transform.localScale = new Vector3(pathPoint2.transform.localScale.x, pathPoint2.transform.localScale.y, toolUsed.transform.localScale.z);
                ROIFreehandPath.Add(pathPoint2);

                // it is necessary that there are at least three vertices to start off
                ROIFreehandVertices.Add(hitPos / usageScale + new Vector3(0f, 0f, startPos + halfLength));
                ROIFreehandVertices.Add(hitPos / usageScale + new Vector3(0f, 0f, startPos - halfLength));

                //sides
                ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 2);
                ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 4);
                ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 3);

                ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 2);
                ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 3);
                ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 1);

            }

            // it is necessary that there are at least three vertices to start off
            if (ROIFreehandVertices.Count < 3)
            {
                // it is necessary that there are at least three vertices to start off
                ROIFreehandVertices.Add(hitPos / usageScale + new Vector3(0f, 0f, startPos + halfLength));
                ROIFreehandVertices.Add(hitPos / usageScale + new Vector3(0f, 0f, startPos - halfLength));

                //sides
                ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 2);
                ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 4);
                ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 3);

                ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 2);
                ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 3);
                ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 1);
            }


            //TODO: check which way around culling happens
            // top face triangles (note two vertices are added per point)
            ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 2);
            ROIFreehandTriangles.Add(0);
            ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 4);


            //bottom face
            ROIFreehandTriangles.Add(1);
            ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 1);
            ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 3);


            //sides
            ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 2);
            ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 4);
            ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 3);

            ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 2);
            ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 3);
            ROIFreehandTriangles.Add(ROIFreehandVertices.Count - 1);

            //TODO: the loop isn't closed for the freehand geometry


            //MeshFilter mf = currentROITool.GetComponent<MeshFilter>();
            //Mesh freehandMesh = mf.mesh;

            freehandMesh.Clear();
            freehandMesh.SetVertices(ROIFreehandVertices);
            freehandMesh.SetTriangles(ROIFreehandTriangles, 0);

            /*
			if(!updateMaskRunning)// && showROIMask)// && ROIMaskParent.activeInHierarchy)
				StartCoroutine(updateROIMask());
			else
				maskShouldUpdate = true;
                */
        }

        // changing the sliders
        if (currentSliderNum != -1)
        {
            if (!colocalizationActive)
            {
                Slider slider = guiSliders[currentSliderNum].GetComponent<Slider>();
                float sliderChange = Input.GetAxis("Oculus_GearVR_DpadX") * (slider.maxValue - slider.minValue) * sliderChangeSensitivity * Time.deltaTime;

                // this ensures it will work even for whole numbers
                if (slider.wholeNumbers && Mathf.Abs(sliderChange) > 0.0f && (int)Mathf.Abs(sliderChange) == 0)
                    slider.value += Mathf.Sign(sliderChange);

                slider.value += sliderChange;
            }
            else
            {
                Slider slider = colocSliders[currentSliderNum].GetComponent<Slider>();
                float sliderChange = Input.GetAxis("Oculus_GearVR_DpadX") * (slider.maxValue - slider.minValue) * sliderChangeSensitivity * Time.deltaTime;

                // this ensures it will work even for whole numbers
                if (slider.wholeNumbers && Mathf.Abs(sliderChange) > 0.0f && (int)Mathf.Abs(sliderChange) == 0)
                    slider.value += Mathf.Sign(sliderChange);

                slider.value += sliderChange;
            }
        }


    }

    // this function ensures the position and scale is within the bounds of the bounding box
    public void checkROIBounds()
    {
        ROIBoxSetup toolUsed = currentROITool.GetComponent<ROIBoxSetup>();

        if (ROITranslateTogether)
        {
            if (currentROITool != ROIFreehand)
            {
                for (int i = 0; i < activeROITools.Count; i++)
                {
                    ROIBoxSetup toolUsedLoop = activeROITools[i].GetComponent<ROIBoxSetup>();

                    // if the ROI as a whole is scaled lock the z axis
                    if (!scaleROIAxis)
                        toolUsedLoop.transform.localScale = new Vector3(toolUsedLoop.transform.localScale.x, toolUsedLoop.transform.localScale.y, 1.0f); // NOTE: I can use 1.0 because it's a child of the bounding box

                    // ensure that it doesn't invert
                    float minScaleFactor = 0.01f;
                    //TODO: this should probably not be hardcoded
                    // x-axis
                    if (toolUsedLoop.transform.localScale.x < minScaleFactor * toolUsedLoop.boxDimRatio.x)
                        toolUsedLoop.transform.localScale = new Vector3(minScaleFactor * toolUsedLoop.boxDimRatio.x, toolUsedLoop.transform.localScale.y, toolUsedLoop.transform.localScale.z);
                    //y-axis
                    if (toolUsedLoop.transform.localScale.y < minScaleFactor * toolUsedLoop.boxDimRatio.y)
                        toolUsedLoop.transform.localScale = new Vector3(toolUsedLoop.transform.localScale.x, minScaleFactor * toolUsedLoop.boxDimRatio.y, toolUsedLoop.transform.localScale.z);
                    //z-axis
                    if (toolUsedLoop.transform.localScale.z < minScaleFactor * toolUsedLoop.boxDimRatio.z)
                        toolUsedLoop.transform.localScale = new Vector3(toolUsedLoop.transform.localScale.x, toolUsedLoop.transform.localScale.y, minScaleFactor * toolUsedLoop.boxDimRatio.z);

                    // ensure that the box is fixed inside the bounding box
                    //+X
                    if (toolUsedLoop.transform.localPosition.x + (toolUsedLoop.transform.localScale.x) / 2.0f > 0.5f)
                    {
                        float newX = 0.5f - (toolUsedLoop.transform.localScale.x) / 2.0f;
                        toolUsedLoop.transform.localPosition = new Vector3(newX, toolUsedLoop.transform.localPosition.y, toolUsedLoop.transform.localPosition.z);
                    }
                    //-X
                    if (toolUsedLoop.transform.localPosition.x - (toolUsedLoop.transform.localScale.x) / 2.0f < -0.5f)
                    {
                        float newX = -0.5f + (toolUsedLoop.transform.localScale.x) / 2.0f;
                        toolUsedLoop.transform.localPosition = new Vector3(newX, toolUsedLoop.transform.localPosition.y, toolUsedLoop.transform.localPosition.z);
                    }
                    //+Y
                    if (toolUsedLoop.transform.localPosition.y + (toolUsedLoop.transform.localScale.y) / 2.0f > 0.5f)
                    {
                        float newY = 0.5f - (toolUsedLoop.transform.localScale.y) / 2.0f;
                        toolUsedLoop.transform.localPosition = new Vector3(toolUsedLoop.transform.localPosition.x, newY, toolUsedLoop.transform.localPosition.z);
                    }
                    //-Y
                    if (toolUsedLoop.transform.localPosition.y - (toolUsedLoop.transform.localScale.y) / 2.0f < -0.5f)
                    {
                        float newY = -0.5f + (toolUsedLoop.transform.localScale.y) / 2.0f;
                        toolUsedLoop.transform.localPosition = new Vector3(toolUsedLoop.transform.localPosition.x, newY, toolUsedLoop.transform.localPosition.z);
                    }
                    //+Z
                    if (toolUsedLoop.transform.localPosition.z + (toolUsedLoop.transform.localScale.z) / 2.0f > 0.5f)
                    {
                        float newZ = 0.5f - (toolUsedLoop.transform.localScale.z) / 2.0f;
                        toolUsedLoop.transform.localPosition = new Vector3(toolUsedLoop.transform.localPosition.x, toolUsedLoop.transform.localPosition.y, newZ);
                    }
                    //-Z
                    if (toolUsedLoop.transform.localPosition.z - (toolUsedLoop.transform.localScale.z) / 2.0f < -0.5f)
                    {
                        float newZ = -0.5f + (toolUsedLoop.transform.localScale.z) / 2.0f;
                        toolUsedLoop.transform.localPosition = new Vector3(toolUsedLoop.transform.localPosition.x, toolUsedLoop.transform.localPosition.y, newZ);
                    }


                    // ensure the ROI box doesn't scale bigger than the bounding box
                    if (toolUsedLoop.transform.localScale.x > 1.0f)
                        toolUsedLoop.transform.localScale = new Vector3(1.0f, toolUsedLoop.transform.localScale.y, toolUsedLoop.transform.localScale.z); // NOTE: I can use 1.0 because it's a child of the bounding box
                    if (toolUsedLoop.transform.localScale.y > 1.0f)
                        toolUsedLoop.transform.localScale = new Vector3(toolUsedLoop.transform.localScale.x, 1.0f, toolUsedLoop.transform.localScale.z); // NOTE: I can use 1.0 because it's a child of the bounding box
                    if (toolUsedLoop.transform.localScale.z > 1.0f)
                        toolUsedLoop.transform.localScale = new Vector3(toolUsedLoop.transform.localScale.x, toolUsedLoop.transform.localScale.y, 1.0f); // NOTE: I can use 1.0 because it's a child of the bounding box
                }
            }
        }
        else
        {
            if (currentROITool != ROIFreehand)
            {
                // if the ROI as a whole is scaled lock the z axis
                if (!scaleROIAxis)
                    toolUsed.transform.localScale = new Vector3(toolUsed.transform.localScale.x, toolUsed.transform.localScale.y, 1.0f); // NOTE: I can use 1.0 because it's a child of the bounding box

                // ensure that it doesn't invert
                float minScaleFactor = 0.01f;
                //TODO: this should probably not be hardcoded
                // x-axis
                if (toolUsed.transform.localScale.x < minScaleFactor * toolUsed.boxDimRatio.x)
                    toolUsed.transform.localScale = new Vector3(minScaleFactor * toolUsed.boxDimRatio.x, toolUsed.transform.localScale.y, toolUsed.transform.localScale.z);
                //y-axis
                if (toolUsed.transform.localScale.y < minScaleFactor * toolUsed.boxDimRatio.y)
                    toolUsed.transform.localScale = new Vector3(toolUsed.transform.localScale.x, minScaleFactor * toolUsed.boxDimRatio.y, toolUsed.transform.localScale.z);
                //z-axis
                if (toolUsed.transform.localScale.z < minScaleFactor * toolUsed.boxDimRatio.z)
                    toolUsed.transform.localScale = new Vector3(toolUsed.transform.localScale.x, toolUsed.transform.localScale.y, minScaleFactor * toolUsed.boxDimRatio.z);

                // ensure that the box is fixed inside the bounding box
                //+X
                if (toolUsed.transform.localPosition.x + (toolUsed.transform.localScale.x) / 2.0f > 0.5f)
                {
                    float newX = 0.5f - (toolUsed.transform.localScale.x) / 2.0f;
                    toolUsed.transform.localPosition = new Vector3(newX, toolUsed.transform.localPosition.y, toolUsed.transform.localPosition.z);
                }
                //-X
                if (toolUsed.transform.localPosition.x - (toolUsed.transform.localScale.x) / 2.0f < -0.5f)
                {
                    float newX = -0.5f + (toolUsed.transform.localScale.x) / 2.0f;
                    toolUsed.transform.localPosition = new Vector3(newX, toolUsed.transform.localPosition.y, toolUsed.transform.localPosition.z);
                }
                //+Y
                if (toolUsed.transform.localPosition.y + (toolUsed.transform.localScale.y) / 2.0f > 0.5f)
                {
                    float newY = 0.5f - (toolUsed.transform.localScale.y) / 2.0f;
                    toolUsed.transform.localPosition = new Vector3(toolUsed.transform.localPosition.x, newY, toolUsed.transform.localPosition.z);
                }
                //-Y
                if (toolUsed.transform.localPosition.y - (toolUsed.transform.localScale.y) / 2.0f < -0.5f)
                {
                    float newY = -0.5f + (toolUsed.transform.localScale.y) / 2.0f;
                    toolUsed.transform.localPosition = new Vector3(toolUsed.transform.localPosition.x, newY, toolUsed.transform.localPosition.z);
                }
                //+Z
                if (toolUsed.transform.localPosition.z + (toolUsed.transform.localScale.z) / 2.0f > 0.5f)
                {
                    float newZ = 0.5f - (toolUsed.transform.localScale.z) / 2.0f;
                    toolUsed.transform.localPosition = new Vector3(toolUsed.transform.localPosition.x, toolUsed.transform.localPosition.y, newZ);
                }
                //-Z
                if (toolUsed.transform.localPosition.z - (toolUsed.transform.localScale.z) / 2.0f < -0.5f)
                {
                    float newZ = -0.5f + (toolUsed.transform.localScale.z) / 2.0f;
                    toolUsed.transform.localPosition = new Vector3(toolUsed.transform.localPosition.x, toolUsed.transform.localPosition.y, newZ);
                }


                // ensure the ROI box doesn't scale bigger than the bounding box
                if (toolUsed.transform.localScale.x > 1.0f)
                    toolUsed.transform.localScale = new Vector3(1.0f, toolUsed.transform.localScale.y, toolUsed.transform.localScale.z); // NOTE: I can use 1.0 because it's a child of the bounding box
                if (toolUsed.transform.localScale.y > 1.0f)
                    toolUsed.transform.localScale = new Vector3(toolUsed.transform.localScale.x, 1.0f, toolUsed.transform.localScale.z); // NOTE: I can use 1.0 because it's a child of the bounding box
                if (toolUsed.transform.localScale.z > 1.0f)
                    toolUsed.transform.localScale = new Vector3(toolUsed.transform.localScale.x, toolUsed.transform.localScale.y, 1.0f); // NOTE: I can use 1.0 because it's a child of the bounding box

            }
            else if (currentROITool.CompareTag("ROIFreehand"))
            {
                // ensure that it doesn't invert
                //TODO: this should probably not be hardcoded
                //z-axis

                if (toolUsed.transform.localScale.z < 0.1f * toolUsed.boxDimRatio.z)
                    toolUsed.transform.localScale = new Vector3(1f, 1f, 0.1f * toolUsed.boxDimRatio.z);
                //				toolUsed.transform.localScale = new Vector3(toolUsed.transform.localScale.x, toolUsed.transform.localScale.y, 0.1f*toolUsed.boxDimRatio.z);


                // ensure that the box is fixed inside the bounding box
                //+X
                if (toolUsed.transform.localPosition.x + (toolUsed.transform.localScale.x) / 2.0f > 0.5f)
                {
                    float newX = 0.5f - (toolUsed.transform.localScale.x) / 2.0f;
                    toolUsed.transform.localPosition = new Vector3(newX, toolUsed.transform.localPosition.y, toolUsed.transform.localPosition.z);
                }
                //-X
                if (toolUsed.transform.localPosition.x - (toolUsed.transform.localScale.x) / 2.0f < -0.5f)
                {
                    float newX = -0.5f + (toolUsed.transform.localScale.x) / 2.0f;
                    toolUsed.transform.localPosition = new Vector3(newX, toolUsed.transform.localPosition.y, toolUsed.transform.localPosition.z);
                }
                //+Y
                if (toolUsed.transform.localPosition.y + (toolUsed.transform.localScale.y) / 2.0f > 0.5f)
                {
                    float newY = 0.5f - (toolUsed.transform.localScale.y) / 2.0f;
                    toolUsed.transform.localPosition = new Vector3(toolUsed.transform.localPosition.x, newY, toolUsed.transform.localPosition.z);
                }
                //-Y
                if (toolUsed.transform.localPosition.y - (toolUsed.transform.localScale.y) / 2.0f < -0.5f)
                {
                    float newY = -0.5f + (toolUsed.transform.localScale.y) / 2.0f;
                    toolUsed.transform.localPosition = new Vector3(toolUsed.transform.localPosition.x, newY, toolUsed.transform.localPosition.z);
                }
                //+Z
                if (toolUsed.transform.localPosition.z + (toolUsed.transform.localScale.z) / 2.0f > 0.5f)
                {
                    float newZ = 0.5f - (toolUsed.transform.localScale.z) / 2.0f;
                    toolUsed.transform.localPosition = new Vector3(toolUsed.transform.localPosition.x, toolUsed.transform.localPosition.y, newZ);
                }
                //+Z
                if (toolUsed.transform.localPosition.z + (toolUsed.transform.localScale.z) / 2.0f > 0.5f)
                {
                    float newZ = 0.5f - (toolUsed.transform.localScale.z) / 2.0f;
                    toolUsed.transform.localPosition = new Vector3(toolUsed.transform.localPosition.x, toolUsed.transform.localPosition.y, newZ);
                }
                //-Z
                if (toolUsed.transform.localPosition.z - (toolUsed.transform.localScale.z) / 2.0f < -0.5f)
                {
                    float newZ = -0.5f + (toolUsed.transform.localScale.z) / 2.0f;
                    toolUsed.transform.localPosition = new Vector3(toolUsed.transform.localPosition.x, toolUsed.transform.localPosition.y, newZ);
                }


                // ensure the ROI box doesn't scale bigger than the bounding box
                if (toolUsed.transform.localScale.z > 1.0f)
                    toolUsed.transform.localScale = new Vector3(1f, 1f, 1f); // NOTE: I can use 1.0 because it's a child of the bounding box
                                                                             //toolUsed.transform.localScale = new Vector3(toolUsed.transform.localScale.x, toolUsed.transform.localScale.y, 1.0f); // NOTE: I can use 1.0 because it's a child of the bounding box

            }
        }
        /*
		if(!updateMaskRunning)// && showROIMask)// && ROIMaskParent.activeInHierarchy)
			StartCoroutine(updateROIMask());
		else
			maskShouldUpdate = true;
            */
    }

    public void CalculateDepthROIMasks()
    {

    }

    #endregion
    // calculate the closest power of two to the input numberR
    int closestPowerOfTwo(int v)
    {
        v--;
        v |= v >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        v++;
        return v;
    }

    public void changeMenuColoc()
    {
        // change the current cancvas that is displayed
        if (mainMenu.activeInHierarchy) // change to coloc menu
        {
            colocalizationActive = true;

            mainMenu.SetActive(false);
            colocMenu.SetActive(true);

            // show colocalization
            colocalizationMethod = colocMethodDropdown.GetComponent<Dropdown>().value;
            internalMat.SetInt("_colocalizationMethod", colocalizationMethod);
            mipMat.SetInt("_colocalizationMethod", colocalizationMethod);
            //mainMenu.transform.localPosition += new Vector3(0f, 0f, 0.10f);
            //isosurfaceMenu.transform.localPosition += new Vector3(0f, 0f, -0.10f);
        }
        else     // change to main menu
        {
            colocalizationActive = false;

            //mainMenu.transform.localPosition += new Vector3(0f, 0f, -0.10f);
            //isosurfaceMenu.transform.localPosition += new Vector3(0f, 0f, 0.10f);
            colocMenu.SetActive(false);
            mainMenu.SetActive(true);

            // don't show colocalization
            colocalizationMethod = (int)ColocMethod.NoColoc;
            internalMat.SetInt("_colocalizationMethod", colocalizationMethod);
            mipMat.SetInt("_colocalizationMethod", colocalizationMethod);
        }

    }

    public void volumeVizTypeDropdownChanged()
    {
        int newRenderType = volumeVizTypeDropdown.GetComponent<Dropdown>().value;

        // save material settings
        switch(renderType)
        {
            case (int)VolumeRenderingType.TextureSlicing:
                TextureSlicingMat = internalMat;
                break;
            case (int)VolumeRenderingType.RayCasting:
                RayCastingMat = internalMat;
                break;
            case (int)VolumeRenderingType.RayCastingIso:
                RayCastingIsoSurfaceMat = internalMat;
                break;
        }

        // start with new render type
        renderType = newRenderType;
        Start();

        if (newRenderType == (int)VolumeRenderingType.RayCastingIso)
        {
            mainMenu.SetActive(false);
            isosurfaceMenu.SetActive(true);
        }
    }

    #region colocMenu
    public void dropDownChanged(int colorChannel)
    {
        List<string> newOptions = new List<string>();
        string prevText;
        int newIndex = 0;

        switch (colorChannel)
        {
            case 0:
                colocChannel0 = Int32.Parse(colocChan0Dropdown.GetComponent<Dropdown>().options[colocChan0Dropdown.GetComponent<Dropdown>().value].text.Substring(0, 1));
                //Debug.Log("colocChannel0 changed to " + colocChannel0);
                // remove the item from the other dropbox that was selected
                prevText = colocChan1Dropdown.GetComponent<Dropdown>().options[colocChan1Dropdown.GetComponent<Dropdown>().value].text;
                colocChan1Dropdown.GetComponent<Dropdown>().ClearOptions();
                for (int i = 0; i < colocDropdownOptions.Count; i++)
                {
                    if (i != colocChannel0)
                    {
                        newOptions.Add(colocDropdownOptions[i]);

                        // this code allows me to keep the other dropbox on the same item as before
                        if (prevText.Equals(colocDropdownOptions[i]))
                        {
                            newIndex = newOptions.Count - 1;
                            //Debug.Log("Channel 0 - found " + newIndex + " with text " + dropDownOptions[i] + " with prevText in chan1 = " + prevText);
                        }
                    }

                }
                colocChan1Dropdown.GetComponent<Dropdown>().AddOptions(newOptions);
                if (colocChan1Dropdown.GetComponent<Dropdown>().value != newIndex)
                {
                    //Debug.Log("Updated 1");
                    colocChan1Dropdown.GetComponent<Dropdown>().value = newIndex;
                }

                // update the values to those that were saved
                colocSliders[(int)ColocMatProperties.chan0HighThres].GetComponent<Slider>().minValue = thresholdHighMin[colocChannel0] * thresholdMaxInt;
                //colocSliders[(int)ColocMatProperties.chan0LowThres].GetComponent<Slider>().minValue = thresholdLowMin[colocChannel0];
                colocSliders[(int)ColocMatProperties.chan0HighThres].GetComponent<Slider>().maxValue = thresholdHighMax[colocChannel0] * thresholdMaxInt;
                colocSliders[(int)ColocMatProperties.chan0LowThres].GetComponent<Slider>().maxValue = thresholdLowMax[colocChannel0] * thresholdMaxInt;
                colocSliders[(int)ColocMatProperties.chan0HighThres].GetComponent<Slider>().value = thresholdHighValue[colocChannel0] * thresholdMaxInt;
                colocSliders[(int)ColocMatProperties.chan0LowThres].GetComponent<Slider>().value = thresholdLowValue[colocChannel0] * thresholdMaxInt;

                break;
            case 1:
                colocChannel1 = Int32.Parse(colocChan1Dropdown.GetComponent<Dropdown>().options[colocChan1Dropdown.GetComponent<Dropdown>().value].text.Substring(0, 1));
                //Debug.Log("colocChannel1 changed to " + colocChannel1);
                // remove the item from the other dropbox that was selected
                prevText = colocChan0Dropdown.GetComponent<Dropdown>().options[colocChan0Dropdown.GetComponent<Dropdown>().value].text;
                colocChan0Dropdown.GetComponent<Dropdown>().ClearOptions();
                for (int i = 0; i < colocDropdownOptions.Count; i++)
                {
                    if (i != colocChannel1)
                        newOptions.Add(colocDropdownOptions[i]);

                    // this code allows me to keep the other dropbox on the same item as before
                    if (prevText.Equals(colocDropdownOptions[i]))
                    {
                        newIndex = newOptions.Count - 1;
                        //Debug.Log("Channel 1 - found " + newIndex + " with text " + dropDownOptions[i] + " with prevText in chan0 = " + prevText);
                    }
                }
                colocChan0Dropdown.GetComponent<Dropdown>().AddOptions(newOptions);
                if (colocChan0Dropdown.GetComponent<Dropdown>().value != newIndex)
                {
                    //Debug.Log("Updated 0");
                    colocChan0Dropdown.GetComponent<Dropdown>().value = newIndex;
                }

                // update the values to those that were saved
                colocSliders[(int)ColocMatProperties.chan1HighThres].GetComponent<Slider>().minValue = thresholdHighMin[colocChannel1] * thresholdMaxInt;
                //colocSliders[(int)ColocMatProperties.chan1LowThres].GetComponent<Slider>().minValue = thresholdLowMin[colocChannel1];
                colocSliders[(int)ColocMatProperties.chan1HighThres].GetComponent<Slider>().maxValue = thresholdHighMax[colocChannel1] * thresholdMaxInt;
                colocSliders[(int)ColocMatProperties.chan1LowThres].GetComponent<Slider>().maxValue = thresholdLowMax[colocChannel1] * thresholdMaxInt;
                colocSliders[(int)ColocMatProperties.chan1HighThres].GetComponent<Slider>().value = thresholdHighValue[colocChannel1] * thresholdMaxInt;
                colocSliders[(int)ColocMatProperties.chan1LowThres].GetComponent<Slider>().value = thresholdLowValue[colocChannel1] * thresholdMaxInt;
                break;
        }


        if (colocChannel0 == colocChannel1)
        {
            Debug.LogAssertion("Colocalization channels should not be able to be set to the same thing");
            //colocChan0Dropdown.GetComponent<Image>().color = new Color(0.752f, 0.314f, 0.302f);
            //colocChan1Dropdown.GetComponent<Image>().color = new Color(0.752f, 0.314f, 0.302f);
        }
        else
        {
            //colocChan0Dropdown.GetComponent<Image>().color = new Color(0.267f, 0.329f, 0.486f);
            //colocChan1Dropdown.GetComponent<Image>().color = new Color(0.267f, 0.329f, 0.486f);
        }



        internalMat.SetInt("_colChannel0", colocChannel0);
        internalMat.SetInt("_colChannel1", colocChannel1);

        mipMat.SetInt("_colChannel0", colocChannel0);
        mipMat.SetInt("_colChannel1", colocChannel1);
    }

    public void dropDownChangedRenderType()
    {
        colocalizationMethod = colocMethodDropdown.GetComponent<Dropdown>().value;

        // change the sliders to match the method used
        if (colocalizationMethod == (int)ColocMethod.OnlyColocHeatmap)
        {
            heatmapElements.SetActive(true);
            HeatmapLegend.SetActive(true);
            /*
            int lowVal, midVal, highVal = maxHeatmapValue;
            lowVal = (int)(((thresholdHighValue[colocChannel0] + thresholdHighValue[colocChannel1]) / 2.0f) * thresholdMaxInt);
            midVal = (lowVal + highVal) / 2;

            HeatmapLowLabel.text = lowVal.ToString();
            HeatmapMidLabel.text = midVal.ToString();
            HeatmapHighLabel.text = highVal.ToString();*/


            Slider[] sliders = heatmapElements.GetComponentsInChildren<Slider>();

            Slider percentageSlider;
            Slider angleSlider;
            if (sliders[0].name.Equals("PercentageIncluded Slider"))
            {             
                percentageSlider = sliders[0];
                angleSlider = sliders[1];
            }
            else
            {
                percentageSlider = sliders[1];
                angleSlider = sliders[0];
            }

            float percentage = percentageUsed ;
            float angle = internalMat.GetFloat("_angle");

            percentageSlider.wholeNumbers = true;
            percentageSlider.maxValue = 200;
            percentageSlider.value = (int)(percentage*200.0f);

            angleSlider.wholeNumbers = true;
            angleSlider.maxValue = 89;
            angleSlider.value = (int)angle;

            percentageSlider.GetComponentInChildren<UnityEngine.UI.Text>().text = "Percentage to include - " + string.Format("{0:0.0}%", percentage * 100);
            angleSlider.GetComponentInChildren<UnityEngine.UI.Text>().text = "Tightness (angle) - " + string.Format("{0:0}", angle);
        }
        else if (colocalizationMethod == (int)ColocMethod.OnlyColocNMDP || colocalizationMethod == (int)ColocMethod.OnlyColocNMDPSobel)
        {
            heatmapElements.SetActive(true);
            Slider[] sliders = heatmapElements.GetComponentsInChildren<Slider>();

            Slider nmdpMinSlider;
            Slider nmdpMaxSlider;
            if (sliders[0].name.Equals("PercentageIncluded Slider"))
            {
                nmdpMinSlider = sliders[0];
                nmdpMaxSlider = sliders[1];
            }
            else
            {             
                nmdpMinSlider = sliders[1];
                nmdpMaxSlider = sliders[0];
            }

            float minValue = internalMat.GetFloat("_nmdp_minMultFactor");
            float maxValue = internalMat.GetFloat("_nmdp_maxMultFactor");

            nmdpMinSlider.wholeNumbers = false;
            nmdpMinSlider.maxValue = 510.0f;
            nmdpMinSlider.value = minValue;

            nmdpMaxSlider.wholeNumbers = false;
            nmdpMaxSlider.maxValue = 510.0f;
            nmdpMaxSlider.value = maxValue;

            nmdpMinSlider.GetComponentInChildren<UnityEngine.UI.Text>().text = "nMDP Min value - " + string.Format("{0:0.00}", minValue / 255.0);
            nmdpMaxSlider.GetComponentInChildren<UnityEngine.UI.Text>().text = "nMDP Max value - " + string.Format("{0:0.00}", maxValue / 255.0);
        }
        else
        {
            HeatmapLegend.SetActive(false);
            heatmapElements.SetActive(false);
        }

        internalMat.SetInt("_colocalizationMethod", colocalizationMethod);

        mipMat.SetInt("_colocalizationMethod", colocalizationMethod);
    }

    public void dropDownChangedRenderInterval()
    {
        colocThresIntervalDisplay = colocIntervalDropdown.GetComponent<Dropdown>().value;
        //		Debug.Log(colocThresIntervalDisplay);

        internalMat.SetInt("_colThresIntervalDisplay", colocThresIntervalDisplay);
        mipMat.SetInt("_colThresIntervalDisplay", colocThresIntervalDisplay);

    }

    public void changeColocMatProperty(int sliderNum)
    {
        /*
		switch(renderType)
		{
		case (int) VolumeRenderingType.TextureSlicing:
			internalMat.SetVector ("_BoxDim", new Vector4 (boundingBoxSetup.boxDim.x / everything.transform.localScale.x, boundingBoxSetup.boxDim.y / everything.transform.localScale.y, boundingBoxSetup.boxDim.z / everything.transform.localScale.z, 0.0f));
			break;
		case (int) VolumeRenderingType.RayCasting:
			internalMat.SetVector ("_BoxDim", new Vector4 (boundingBoxSetup.boxDim.x, boundingBoxSetup.boxDim.y, boundingBoxSetup.boxDim.z, 0.0f));
			break;
		}*/
        internalMat.SetVector("_BoxDim", new Vector4(boundingBoxSetup.boxDim.x, boundingBoxSetup.boxDim.y, boundingBoxSetup.boxDim.z, 0.0f));
        mipMat.SetVector("_BoxDim", new Vector4(boundingBoxSetup.boxDim.x, boundingBoxSetup.boxDim.y, boundingBoxSetup.boxDim.z, 0.0f));

        Slider slider = colocSliders[sliderNum].GetComponent<Slider>();

        //Debug.Log(slider.value);
        string sliderText = slider.GetComponentInChildren<UnityEngine.UI.Text>().text;
        slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0:0}", slider.value);

        switch (sliderNum)
        {
            case (int)ColocMatProperties.ColocOpacity: //0
                setColocOpacity = slider.value;
                internalMat.SetFloat("_colocalizationOpacity", slider.value);
                mipMat.SetFloat("_colocalizationOpacity", slider.value);
                sliderText = slider.GetComponentInChildren<UnityEngine.UI.Text>().text;
                slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0:0.000}", slider.value);
                break;
            case (int)ColocMatProperties.chan0LowThres: //1
                internalMat.SetFloat("_chan0ThresholdLow", slider.value / (float)thresholdMaxInt);
                mipMat.SetFloat("_chan0ThresholdLow", slider.value / (float)thresholdMaxInt);
                colocSliders[(int)ColocMatProperties.chan0HighThres].GetComponent<Slider>().minValue = slider.value;
                thresholdHighMin[colocChannel0] = slider.value / (float)thresholdMaxInt;
                thresholdLowValue[colocChannel0] = slider.value / (float)thresholdMaxInt;
                break;
            case (int)ColocMatProperties.chan0HighThres: //2
                internalMat.SetFloat("_chan0ThresholdHigh", slider.value / (float)thresholdMaxInt);
                mipMat.SetFloat("_chan0ThresholdHigh", slider.value / (float)thresholdMaxInt);
                colocSliders[(int)ColocMatProperties.chan0LowThres].GetComponent<Slider>().maxValue = slider.value;
                thresholdLowMin[colocChannel0] = slider.value / (float)thresholdMaxInt;
                thresholdHighValue[colocChannel0] = slider.value / (float)thresholdMaxInt;
                break;
            case (int)ColocMatProperties.chan1LowThres: //3
                internalMat.SetFloat("_chan1ThresholdLow", slider.value / (float)thresholdMaxInt);
                mipMat.SetFloat("_chan1ThresholdLow", slider.value / (float)thresholdMaxInt);
                colocSliders[(int)ColocMatProperties.chan1HighThres].GetComponent<Slider>().minValue = slider.value;
                thresholdHighMin[colocChannel1] = slider.value / (float)thresholdMaxInt;
                thresholdLowValue[colocChannel1] = slider.value / (float)thresholdMaxInt;
                break;
            case (int)ColocMatProperties.chan1HighThres: //4
                internalMat.SetFloat("_chan1ThresholdHigh", slider.value / (float)thresholdMaxInt);
                mipMat.SetFloat("_chan1ThresholdHigh", slider.value / (float)thresholdMaxInt);
                colocSliders[(int)ColocMatProperties.chan1LowThres].GetComponent<Slider>().maxValue = slider.value;
                thresholdLowMin[colocChannel1] = slider.value / (float)thresholdMaxInt;
                thresholdHighValue[colocChannel1] = slider.value / (float)thresholdMaxInt;
                break;
            case (int)ColocMatProperties.MaxValue: //5
                internalMat.SetFloat("_maxValue", slider.value);
                mipMat.SetFloat("_maxValue", slider.value);
                maxHeatmapValue = (int)(slider.value * thresholdMaxInt);

                /*
                int lowVal, midVal, highVal = maxHeatmapValue;
                lowVal = (int)(((thresholdHighValue[colocChannel0] + thresholdHighValue[colocChannel1]) / 2.0f) * thresholdMaxInt);
                midVal = (lowVal + highVal) / 2;

                HeatmapLowLabel.text = lowVal.ToString();
                HeatmapMidLabel.text = midVal.ToString();
                HeatmapHighLabel.text = highVal.ToString();
                */


                sliderText = slider.GetComponentInChildren<UnityEngine.UI.Text>().text;
                slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0:0}", maxHeatmapValue);
                break;
            case (int)ColocMatProperties.Percentage: //6
                if (colocalizationMethod == (int)ColocMethod.OnlyColocHeatmap)
                {
                    percentageUsed = slider.value / 200.0f; // slider between 0 and 200
                    if (mayUpdateDistance)
                        UpdateDistance();

                    slider.GetComponentInChildren<UnityEngine.UI.Text>().text = "Percentage to include - " + string.Format("{0:0.0}%", percentageUsed*100);                    
                }
                else if (colocalizationMethod == (int)ColocMethod.OnlyColocNMDP || colocalizationMethod == (int)ColocMethod.OnlyColocNMDPSobel)
                {
                    slider.GetComponentInChildren<UnityEngine.UI.Text>().text = "nMDP Min value - " + string.Format("{0:0.00}", slider.value / 255.0);
                    internalMat.SetFloat("_nmdp_minMultFactor", slider.value);                    
                }
                break;
            case (int)ColocMatProperties.Angle: //7

                if (colocalizationMethod == (int)ColocMethod.OnlyColocHeatmap)
                {
                    internalMat.SetFloat("_angle", slider.value);
                    visAngleMIP = slider.value;
                    /*
                    if (mayUpdateDistance)
                        UpdateDistance();
                        */
                    sliderText = "Tightness (angle) - ";
                    slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText + string.Format("{0:0}", slider.value);
                }
                else if (colocalizationMethod == (int)ColocMethod.OnlyColocNMDP || colocalizationMethod == (int)ColocMethod.OnlyColocNMDPSobel)
                {
                    slider.GetComponentInChildren<UnityEngine.UI.Text>().text = "nMDP Max value - " + string.Format("{0:0.00}", slider.value / 255.0);
                    internalMat.SetFloat("_nmdp_maxMultFactor", slider.value);
                }
                
                break;
        }

        if (showScatterPlot)
        {
            if (colocResultsCanvase.activeInHierarchy && sliderNum != (int)ColocMatProperties.ColocOpacity && sliderNum != (int)ColocMatProperties.MaxValue && sliderNum != (int)ColocMatProperties.Percentage && sliderNum != (int)ColocMatProperties.Angle)
            {
                switch (ScatterPlotDropbox.GetComponent<Dropdown>().value)
                {
                    case (int)ScatterPlotTypes.FrequencyScatter: // frequency
                        if (!ColocFrequencyScatterToTexture3DBusy)
                            ScatterToTexture(256, freqScatterColors3D, (int)ScatterPlotTypes.FrequencyScatter, "3D");
                        break;
                    case (int)ScatterPlotTypes.DisplayColors:
                        if (!ColocDispalyScatterToTexture3DBusy)
                            ScatterToTexture(256, dispScatterColors3D, (int)ScatterPlotTypes.DisplayColors, "3D");
                        break;
                }
            }
        }
    }

    public void calcColocButtonPressed()
    {
        colocRightLabel.GetComponent<Text>().text = "Calculating colocalization...";
        //colocRightLabel.GetComponent<Text>().text = "";

        automaticUpdateAngle = true;

        // reset the result
        lastCalcedColoc = new ColocalizationData();

        // reset the intermediate variables
        channelAverage[0] = 0.0;
        channelAverage[1] = 0.0;

        if (!colocBeingCalculated)
            StartCoroutine("calculateColocalization3D");
    }
    #endregion
    /*
	IEnumerator calculateColocalization()
	{

        double PCC_numer = 0.0;
        double PCC_denom1 = 0.0;
        double PCC_denom2 = 0.0;

        double MOC_numer = 0.0;
        double MOC_denom1 = 0.0;
        double MOC_denom2 = 0.0;

        // ...for MCC
        double M1_numer = 0.0;
        double M1_denom = 0.0;
        double M2_numer = 0.0;
        double M2_denom = 0.0;

        colocBeingCalculated = true;

		colocRightLabel.GetComponent<Text> ().text = "Step 0 (Update ROI Mask)";
		yield return StartCoroutine(updateROIMask());

		channel0Data = loadSample.texSeparate[colocChannel0].GetPixels();
		//yield return null;
		channel1Data = loadSample.texSeparate[colocChannel1].GetPixels();
		yield return null;

		if(channel0Data.Length != channel1Data.Length)
			Debug.LogAssertion("The 3D textures are not the same size, and colocalization cannot be calculated...");

		int maskRow = 0;
		int maskCol = 0;
		int maskDepth = 0;
		float depthFraction = 0.5f; // start from 0.5 since the localscale is 1 for depth, and since depthFraction run from -0.5 to 0.5...
		// calculate the total in the given channel to calculate the average for that channel
		for(int i = 0; i < channel0Data.Length; i++)
		{
			// logic that ensures using the correct indes for the mask
			maskCol++;
			if (maskCol >= size) 
			{
				maskCol = 0;
				maskRow++;

				if (maskRow >= size) // use the same mask for all depths
				{ 
					maskRow = 0;
					maskDepth++;
					depthFraction = -0.5f + (float)maskDepth/ loadSample.texDepth;
					//Debug.Log("depthFraction: " + depthFraction + "maskDepth: " + maskDepth + "   texDepth: " + texDepth);  
				}
			}

			// TODO: I have to thoroughly test this
			if (ROImaskTest(maskCol, maskRow, depthFraction))
			{
				switch (colocChannel0) {
				case 0: // red
					channelAverage [0] += channel0Data [i].r;	
					break;
				case 1: // green
					channelAverage [0] += channel0Data [i].g;	
					break;
				case 2: // blue
					channelAverage [0] += channel0Data [i].b;	
					break;
				case 3: // purple
					channelAverage [0] += channel0Data [i].r;	
					break;
				}
				switch (colocChannel1) {
				case 0: // red
					channelAverage [1] += channel1Data [i].r;	
					break;
				case 1: // green
					channelAverage [1] += channel1Data [i].g;	
					break;
				case 2: // blue
					channelAverage [1] += channel1Data [i].b;	
					break;
				case 3: // purple
					channelAverage [1] += channel1Data [i].r;	
					break;
				}
	
				if (i % 500000 == 0) {
					//Debug.Log(i);
					colCalcPercProgress = i / (float)channel0Data.Length;
					colocRightLabel.GetComponent<Text> ().text = "Step 1 (Channel Averages): " + (int)(colCalcPercProgress * 100) + "%";
					yield return null;
				}
			}
			
		}
		channelAverage[0] = channelAverage[0] / channel0Data.Length;
		channelAverage[1] = channelAverage[1] / channel1Data.Length;

		maskRow = 0;
		maskCol = 0;
		maskDepth = 0;
		depthFraction = 0.5f; // start from 0.5 since the localscale is 1 for depth, and since depthFraction run from -0.5 to 0.5...
		//colocLeftLabel.GetComponent<Text>().text = "Averages: 0 = " + channelAverage[0] + " 1 = " + channelAverage[1];

		for(int i = 0; i < channel0Data.Length; i++)
		{
			// logic that ensures using the correct indes for the mask
			maskCol++;
			if (maskCol >= size) 
			{
				maskCol = 0;
				maskRow++;

				if (maskRow >= size) // use the same mask for all depths
				{ 
					maskRow = 0;
					maskDepth++;
					depthFraction = -0.5f + (float)maskDepth/ loadSample.texDepth;
				}
			}

			// TODO: I have to thoroughly test this
			if (ROImaskTest(maskCol, maskRow, depthFraction))
			{
				
				float valChan0 = -1f;
				float valChan1 = -1f;
				switch (colocChannel0) {
				case 0: // red
					valChan0 = channel0Data [i].r;	
					break;
				case 1: // green
					valChan0 = channel0Data [i].g;	
					break;
				case 2: // blue
					valChan0 = channel0Data [i].b;	
					break;
				case 3: // purple
					valChan0 = channel0Data [i].r;	
					break;
				}
				switch (colocChannel1) {
				case 0: // red
					valChan1 = channel1Data [i].r;	
					break;
				case 1: // green
					valChan1 = channel1Data [i].g;	
					break;
				case 2: // blue
					valChan1 = channel1Data [i].b;	
					break;
				case 3: // purple
					valChan1 = channel1Data [i].r;	
					break;
				}

				// a colocalized pixel is detected above the high tolerence
				if (valChan0 >= thresholdHighValue [colocChannel0] && valChan1 >= thresholdHighValue [colocChannel1]) {
					lastCalcedColoc.colocalizedPixelsRegion3++;
				}
				// a colocalized pixel is detected between high and low
				else if (valChan0 < thresholdHighValue [colocChannel0] && valChan1 < thresholdHighValue [colocChannel1] &&
				        valChan0 >= thresholdLowValue [colocChannel0] && valChan1 >= thresholdLowValue [colocChannel1]) {
					lastCalcedColoc.colocalizedPixelsLowToHigh++;
				}

				// total pixels for channel 0
				if (valChan0 >= thresholdHighValue [colocChannel0]) {
					lastCalcedColoc.totalPixelCountAboveHigh [0]++;
				} else if (valChan0 < thresholdHighValue [colocChannel0] &&
				        valChan0 >= thresholdLowValue [colocChannel0]) {
					lastCalcedColoc.totalPixelCountLowToHigh [0]++;
				}

				// total pixels for channel 1
				if (valChan1 >= thresholdHighValue [colocChannel1]) {
					lastCalcedColoc.totalPixelCountAboveHigh [1]++;
				} else if (valChan1 < thresholdHighValue [colocChannel1] &&
				        valChan1 >= thresholdLowValue [colocChannel1]) {
					lastCalcedColoc.totalPixelCountLowToHigh [1]++;
				}


				// TODO: this ensures that calculations don't take the "background" into accound, but did I do it correctly (don't think so)
				//	if (valChan0 >= thresholdLowToHigh[colChannel0]  && valChan1 >= thresholdLowToHigh[colChannel1] )
				{
					// Calculate PCC
					PCC_numer += (valChan0 - channelAverage [0]) * (valChan1 - channelAverage [1]);
					PCC_denom1 += (valChan0 - channelAverage [0]) * (valChan0 - channelAverage [0]);
					PCC_denom2 += (valChan1 - channelAverage [1]) * (valChan1 - channelAverage [1]);

					// Calculate MOC
					MOC_numer += valChan0 * valChan1;
					MOC_denom1 += valChan0 * valChan0;
					MOC_denom2 += valChan1 * valChan1;
				}

				//Calculate MCC (TODO: check the condition)
				if (valChan1 >= thresholdLowValue [colocChannel1]) //(valChan1 > 0)
				{
					M1_numer += valChan0;
				}
				M1_denom += valChan0;

				if (valChan0 >= thresholdLowValue [colocChannel0]) // (valChan0 > 0)
				{
					M2_numer += valChan1;
				}
				M2_denom += valChan1;	
			
				if (i % 100000 == 0) {
					//Debug.Log(i);
					colCalcPercProgress = i / (float)channel0Data.Length;
					colocRightLabel.GetComponent<Text> ().text = "Step 2 (Calculating metrics): " + (int)(colCalcPercProgress * 100) + "%";
					yield return null;
				}
			}
		}

		// prevent div by 0
		if ((lastCalcedColoc.totalPixelCountAboveHigh[0] + lastCalcedColoc.totalPixelCountAboveHigh[1]) < 1)
			percColocalizedHigh = -1;
		else
			percColocalizedHigh = ((float)lastCalcedColoc.colocalizedPixelsRegion3) / (lastCalcedColoc.totalPixelCountAboveHigh[0] + lastCalcedColoc.totalPixelCountAboveHigh[1] );

		if ((lastCalcedColoc.totalPixelCountLowToHigh[0] + lastCalcedColoc.totalPixelCountLowToHigh[1]) < 1)
			percColocalizedLow = -1;
		else
			percColocalizedLow = ((float)lastCalcedColoc.colocalizedPixelsLowToHigh) / (lastCalcedColoc.totalPixelCountLowToHigh[0] + lastCalcedColoc.totalPixelCountLowToHigh[1] );

		if ((lastCalcedColoc.totalPixelCountAboveHigh[0]) < 1)
			percColocalizedHigh_chan0 = -1;
		else
			percColocalizedHigh_chan0 = ((float)lastCalcedColoc.colocalizedPixelsRegion3) / (lastCalcedColoc.totalPixelCountAboveHigh[0] );

		if ((lastCalcedColoc.totalPixelCountLowToHigh[0]) < 1)
			percColocalizedLow_chan0 = -1;
		else
			percColocalizedLow_chan0 = ((float)lastCalcedColoc.colocalizedPixelsLowToHigh) / (lastCalcedColoc.totalPixelCountLowToHigh[0] );

		if ((lastCalcedColoc.totalPixelCountAboveHigh[1]) < 1)
			percColocalizedHigh_chan1 = -1;
		else
			percColocalizedHigh_chan1 = ((float)lastCalcedColoc.colocalizedPixelsRegion3) / (lastCalcedColoc.totalPixelCountAboveHigh[1]);

		if ((lastCalcedColoc.totalPixelCountLowToHigh[1]) < 1)
			percColocalizedLow_chan1 = -1;
		else
			percColocalizedLow_chan1 = ((float)lastCalcedColoc.colocalizedPixelsLowToHigh) / (lastCalcedColoc.totalPixelCountLowToHigh[1]);

		lastCalcedColoc.PCC = (float)PCC_numer / (Mathf.Sqrt((float)(PCC_denom1*PCC_denom2)));
		lastCalcedColoc.MOC = (float)MOC_numer / (Mathf.Sqrt((float)(MOC_denom1*MOC_denom2)));
		lastCalcedColoc.MCC[0] = (float)(M1_numer / M1_denom);
		lastCalcedColoc.MCC[1] = (float)(M2_numer / M2_denom);

		colocLeftLabel.GetComponent<Text>().text = string.Format("Btwn Low & High (total) = <b>{0:0.0}%</b>\nBtwn Low & High (div. 1st) = <b>{1:0.0}%</b>\nBtwn Low & High (div. 2nd) = <b>{2:0.0}%</b>\nPCC = <b>{3:0.000}</b>\tMOC = <b>{4:0.000}</b>", (percColocalizedLow *100), (percColocalizedLow_chan0* 100.0), (percColocalizedLow_chan1* 100.0), lastCalcedColoc.PCC, lastCalcedColoc.MOC);
		colocRightLabel.GetComponent<Text>().text = string.Format("Above High (total) = <b>{0:0.0}%</b>\nAbove High (div. 1st) = <b>{1:0.0}%</b>\nAbove High (div. 2nd) = <b>{2:0.0}%</b>\nMCC1 = <b>{3:0.000}</b>\tMCC2 = <b>{4:0.000}</b>", (percColocalizedHigh * 100), (percColocalizedHigh_chan0 * 100.0), (percColocalizedHigh_chan1 * 100.0), lastCalcedColoc.MCC[0], lastCalcedColoc.MCC[1]);


		colocScatterLabel.GetComponent<Text>().text = string.Format("Ch1: <b>{0}</b>\t\tCh2: <b>{1}</b>\n\n<b><i>Colocalization percentages</i></b>\nBtwn Low & High (total) = <b>{2:0.0}%</b>\nAbove High (total) = <b>{3:0.0}%</b>\n\nBtwn Low & High (div. 1st) = <b>{4:0.0}%</b>\nAbove High (div. 1st) = <b>{5:0.0}%</b>\n\nBtwn Low & High (div. 2nd) = <b>{6:0.0}%</b>\nAbove High (div. 2nd) = <b>{7:0.0}%</b>\n\n<b><i>Colocalization correlations</i></b>\nPCC = <b>{8:0.000}</b>\nMOC = <b>{9:0.000}</b>\nMCC1 = <b>{10:0.000}</b>\nMCC2 = <b>{11:0.000}</b>",
			channelNames[colocChannel0], channelNames[colocChannel1], (percColocalizedLow *100), (percColocalizedHigh * 100), (percColocalizedLow_chan0* 100.0), (percColocalizedHigh_chan0 * 100.0), (percColocalizedLow_chan1* 100.0), (percColocalizedHigh_chan1 * 100.0), lastCalcedColoc.PCC, lastCalcedColoc.MOC, lastCalcedColoc.MCC[0], lastCalcedColoc.MCC[1]);

        string text = string.Format("Ch1: {0}\t\tCh2: {1}\n\nColocalization percentages\nBtwn Low & High (total) = {2:0.0}%\nAbove High (total) = {3:0.0}%\n\nBtwn Low & High (div. 1st) = {4:0.0}%\nAbove High (div. 1st) = {5:0.0}%\n\nBtwn Low & High (div. 2nd) = {6:0.0}%\nAbove High (div. 2nd) = {7:0.0}%\n\nColocalization correlations\nPCC = {8:0.000}\nMOC = {9:0.000}\nMCC1 = {10:0.000}\nMCC2 = {11:0.000}",
            channelNames[colocChannel0], channelNames[colocChannel1], (percColocalizedLow * 100), (percColocalizedHigh * 100), (percColocalizedLow_chan0 * 100.0), (percColocalizedHigh_chan0 * 100.0), (percColocalizedLow_chan1 * 100.0), (percColocalizedHigh_chan1 * 100.0), lastCalcedColoc.PCC, lastCalcedColoc.MOC, lastCalcedColoc.MCC[0], lastCalcedColoc.MCC[1]);
        System.IO.File.WriteAllText(Application.persistentDataPath + "/Results/ColocMetrics/ColocMetrics_" + Time.time + ".txt", text);
        //System.IO.File.WriteAllText("I:/Master's results/Results/ColocMetrics/ColocMetrics_" + Time.time + ".txt", text);
        
        colocBeingCalculated = false;
		yield return null;

		StartCoroutine(ColocColorScatterToTexture3D());
		StartCoroutine(ColocFrequencyScatterToTexture3D());
		colocResultsCanvase.SetActive(true);
		ScatterPlotDropbox.GetComponent<Dropdown>().value = 0;
		switch(ScatterPlotDropbox.GetComponent<Dropdown>().value)
		{
		case (int) ScatterPlotTypes.FrequencyScatter: // frequency
			ScatterPlot.GetComponent<Renderer>().material.mainTexture = freqScatterTex;
			break;
		case (int) ScatterPlotTypes.DisplayColors:
			ScatterPlot.GetComponent<Renderer>().material.mainTexture = colorScatterTex;
			break;
		}
	}
    */

    IEnumerator calculateColocalization3D()
    {
        colocBeingCalculated = true;

        totalVoxelsInROI = 0;

        if (!BenProject)
        {
            colocRightLabel.GetComponent<Text>().text = "Step 0 (Update ROI Mask)";
            yield return StartCoroutine(updateROIMask());
        }

        colocRightLabel.GetComponent<Text>().text = "Step 1 (Load Sample Data)";
        channel0Data = loadSample.texSeparate[colocChannel0].GetPixels();
        channel1Data = loadSample.texSeparate[colocChannel1].GetPixels();

        double chan0Average = 0f;
        double chan1Average = 0f;

        double chan0AverageNMDP = 0f;
        double chan1AverageNMDP = 0f;
        double chan0MaxNMDP = 0f;
        double chan1MaxNMDP = 0f;
        double chan0CountNMDP = 0f;
        double chan1CountNMDP = 0f;

        double chan0Max = 0f;
        double chan1Max = 0f;        

        double xySum = 0.0;
        double x2Sum = 0.0;

        double xMax = 0.0;
        double yMax = 0.0;

        double xMean = 0.0;
        double yMean = 0.0;
        double xMeanMinThres = 0.0;
        double yMeanMinThres = 0.0;
        double xy_diffSum = 0.0;
        double x2_diffSum = 0.0;
        double y_div_x_minThres_Sum = 0.0;

        double varXXsum = 0.0;
        double varYYsum = 0.0;
        double varXYsum = 0.0;

        double PCC_numer = 0.0;
        double PCC_denom1 = 0.0;
        double PCC_denom2 = 0.0;

        double MOC_numer = 0.0;
        double MOC_denom1 = 0.0;
        double MOC_denom2 = 0.0;

        // ...for MCC
        double M1_numer = 0.0;
        double M1_denom = 0.0;
        double M2_numer = 0.0;
        double M2_denom = 0.0;


        double region1TotalInensityCh1 = 0.0;
        double region2TotalInensityCh1 = 0.0;
        double region3TotalInensityCh1 = 0.0;
        double region4TotalInensityCh1 = 0.0;

        double region1TotalInensityCh2 = 0.0;
        double region2TotalInensityCh2 = 0.0;
        double region3TotalInensityCh2 = 0.0;
        double region4TotalInensityCh2 = 0.0;

        yield return null;

        if (channel0Data.Length != channel1Data.Length)
            Debug.LogAssertion("The 3D textures are not the same size, and colocalization cannot be calculated...");

        int maskRow = 0;
        int maskCol = 0;
        int maskDepth = 0;
        float depthFraction = 0.5f; // start from 0.5 since the localscale is 1 for depth, and since depthFraction run from -0.5 to 0.5...
                                    // calculate the total in the given channel to calculate the average for that channel
        int voxelsInROI = 0;
        int voxelsInROIandAboveThresh = 0;

        Debug.Log("Ch1: " + colocChannel0 + " Ch2: " + colocChannel1);

        string path2 = Application.persistentDataPath + "/Results/ColocMetrics/";
        //Open File
        TextWriter tw = new StreamWriter("file.csv");

        try
        {            
            if (!Directory.Exists(path2))
                Directory.CreateDirectory(path2);

            tw = new StreamWriter(path2+"file.csv");
        }
        catch(Exception e)
        {

        }

        for (int i = 0; i < channel0Data.Length; i++)
        {
            // TODO: I have to thoroughly test this
            // if the voxel is in the ROI
            if (ROImaskTest3D(maskCol, maskRow, depthFraction))
            {
                //////////////////LIN REG CODE STARTS HERE
                float valChan0 = -1f;
                float valChan1 = -1f;
                switch (colocChannel0)
                {
                    case 0: // red
                        valChan0 = channel0Data[i].r;
                        break;
                    case 1: // green
                        valChan0 = channel0Data[i].g;
                        break;
                    case 2: // blue
                        valChan0 = channel0Data[i].b ;
                        break;
                    case 3: // purple
                        valChan0 = channel0Data[i].r;
                        break;
                }
                switch (colocChannel1)
                {
                    case 0: // red
                        valChan1 = channel1Data[i].r;
                        break;
                    case 1: // green
                        valChan1 = channel1Data[i].g;
                        break;
                    case 2: // blue
                        valChan1 = channel1Data[i].b;
                        break;
                    case 3: // purple
                        valChan1 = channel1Data[i].r;
                        break;
                }

                //the way nMDP handles the thresholds
                if(valChan0 >= thresholdHighValue[colocChannel0])
                {
                    chan0AverageNMDP += valChan0;
                    chan0MaxNMDP = Math.Max(valChan0, chan0MaxNMDP);
                    chan0CountNMDP++;
                }

                if (valChan1 >= thresholdHighValue[colocChannel1])
                {
                    chan1AverageNMDP += valChan1;
                    chan1MaxNMDP = Math.Max(valChan1, chan1MaxNMDP);
                    chan1CountNMDP++;
                }

                // valChan0/1 is a value between 0 and 1
                if (valChan0 >= thresholdHighValue[colocChannel0] && valChan1 >= thresholdHighValue[colocChannel1])
                {
                    xMean += valChan0;
                    yMean += valChan1;

                    try
                    {
                        //Write to file
                        //tw.WriteLine((valChan0 + ";" + valChan1));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                    }


                    float xMinThresh = valChan0 - thresholdHighValue[colocChannel0];
                    float yMinThresh = valChan1 - thresholdHighValue[colocChannel1];

                    y_div_x_minThres_Sum += yMinThresh / xMinThresh;

                    xMeanMinThres += xMinThresh;
                    yMeanMinThres += yMinThresh;

                    voxelsInROIandAboveThresh++;

                    xMax = valChan0 > xMax ? valChan0 : xMax;
                    yMax = valChan1 > yMax ? valChan1 : yMax;


                }
                //////////////////LIN REG CODE ENDS HERE


                voxelsInROI++;

                switch (colocChannel0)
                {
                    case 0: // red
                        chan0Average += channel0Data[i].r;
                        chan0Max = channel0Data[i].r > chan0Max ? channel0Data[i].r : chan0Max;
                        break;
                    case 1: // green
                        chan0Average += channel0Data[i].g;
                        chan0Max = channel0Data[i].g > chan0Max ? channel0Data[i].g : chan0Max;
                        break;
                    case 2: // blue
                        chan0Average += channel0Data[i].b;
                        chan0Max = channel0Data[i].b > chan0Max ? channel0Data[i].b  : chan0Max;
                        break;
                    case 3: // purple
                        chan0Average += channel0Data[i].r;
                        chan0Max = channel0Data[i].r > chan0Max ? channel0Data[i].r : chan0Max;
                        break;
                }
                switch (colocChannel1)
                {
                    case 0: // red
                        chan1Average += channel1Data[i].r;
                        chan1Max = channel1Data[i].r > chan1Max ? channel1Data[i].r : chan1Max;
                        break;
                    case 1: // green
                        chan1Average += channel1Data[i].g;
                        chan1Max = channel1Data[i].g  > chan1Max ? channel1Data[i].g : chan1Max;
                        break;
                    case 2: // blue
                        chan1Average += channel1Data[i].b;
                        chan1Max = channel1Data[i].b > chan1Max ? channel1Data[i].b  : chan1Max;
                        break;
                    case 3: // purple
                        chan1Average += channel1Data[i].r;
                        chan1Max = channel1Data[i].r> chan1Max ? channel1Data[i].r : chan1Max;
                        break;
                }

                if (i % 200000 == 0)
                {
                    //Debug.Log(i);
                    colCalcPercProgress = i / (float)channel0Data.Length;
                    colocRightLabel.GetComponent<Text>().text = "Step 1 (Channel Averages 3D): " + (int)(colCalcPercProgress * 100) + "%";
                    yield return null;
                }
            }

            // logic that ensures using the correct index for the mask, since the voxel array is one dimensional
            maskCol++;
            if (maskCol >= size)
            {
                maskCol = 0;
                maskRow++;

                if (maskRow >= size) // use the same mask for all depths
                {
                    maskRow = 0;
                    maskDepth++;
                    depthFraction = -0.5f + (float)maskDepth / loadSample.texDepth;
                    //Debug.Log("depthFraction: " + depthFraction + "maskDepth: " + maskDepth + "   texDepth: " + texDepth);  
                }
            }

        }

        //Close File
        tw.Close();

        //TODO: figure out which one it should be (I do have strong confidence that I did do it correct however)
        // PCC is the average for the ENTIRE image?
        //chan0Average = chan0Average / channel0Voxels.Length;
        //chan1Average = chan1Average / channel1Voxels.Length;

        //PCC is average of all the pixels in the sample (according to wikipedia)
        chan0Average = chan0Average / (float)voxelsInROI;
        chan1Average = chan1Average / (float)voxelsInROI;

        chan0AverageNMDP = chan0AverageNMDP / chan0CountNMDP;
        chan1AverageNMDP = chan1AverageNMDP / chan1CountNMDP;

        internalMat.SetFloat("_ch0AverageNMDP", (float)chan0AverageNMDP);
        internalMat.SetFloat("_ch1AverageNMDP", (float)chan1AverageNMDP);
        internalMat.SetFloat("_ch0MaxNMDP", (float)chan0MaxNMDP);
        internalMat.SetFloat("_ch1MaxNMDP", (float)chan1MaxNMDP);


        xMean = xMean / (double)voxelsInROIandAboveThresh;
        yMean = yMean / (double)voxelsInROIandAboveThresh;

        y_div_x_minThres_Sum = y_div_x_minThres_Sum / (double)voxelsInROIandAboveThresh;

        xMeanMinThres = xMeanMinThres / (double)voxelsInROIandAboveThresh;
        yMeanMinThres = yMeanMinThres / (double)voxelsInROIandAboveThresh;

        internalMat.SetFloat("_ch0Average", (float)chan0Average);// / thresholdMaxInt);
        internalMat.SetFloat("_ch1Average", (float)chan1Average);// / thresholdMaxInt);
        internalMat.SetFloat("_ch0AverageAboveThres", (float)xMean);// / thresholdMaxInt);
        internalMat.SetFloat("_ch1AverageAboveThres", (float)yMean);// / thresholdMaxInt);

        internalMat.SetFloat("_ch0Max", (float)xMax);// / thresholdMaxInt);
        internalMat.SetFloat("_ch1Max", (float)yMax);// / thresholdMaxInt);
        Debug.Log(String.Format("MAX Channel 1: {0} Channel 2: {1}  Average {2}", xMax * thresholdMaxInt, yMax * thresholdMaxInt, ((xMax + yMax) / 2.0f) * thresholdMaxInt));
        Debug.Log(String.Format("Channel 1 average: {0} Channel 2 average: {1}", xMean * thresholdMaxInt, yMean * thresholdMaxInt));

        maskRow = 0;
        maskCol = 0;
        maskDepth = 0;
        depthFraction = 0.5f; // start from 0.5 since the localscale is 1 for depth, and since depthFraction run from -0.5 to 0.5...
                              //colocLeftLabel.GetComponent<Text>().text = "Averages: 0 = " + chan0Average + " 1 = " + chan1Average;

        for (int i = 0; i < channel0Data.Length; i++)
        {
            // TODO: I have to thoroughly test this method
            if (ROImaskTest3D(maskCol, maskRow, depthFraction))
            {

                float valChan0 = -1f;
                float valChan1 = -1f;
                switch (colocChannel0)
                {
                    case 0: // red
                        valChan0 = channel0Data[i].r;
                        break;
                    case 1: // green
                        valChan0 = channel0Data[i].g;
                        break;
                    case 2: // blue
                        valChan0 = channel0Data[i].b;
                        break;
                    case 3: // purple
                        valChan0 = channel0Data[i].r;
                        break;
                }
                switch (colocChannel1)
                {
                    case 0: // red
                        valChan1 = channel1Data[i].r;
                        break;
                    case 1: // green
                        valChan1 = channel1Data[i].g;
                        break;
                    case 2: // blue
                        valChan1 = channel1Data[i].b;
                        break;
                    case 3: // purple
                        valChan1 = channel1Data[i].r;
                        break;
                }

                /*
                //THE AUTOMATIC MAX SECTION
                Vector2 p3 = new Vector2(valChan0, valChan1);
                float k = ((p2.y - p1.y) * (p3.x - p1.x) - (p2.x - p1.x) * (p3.y - p1.y)) / ((p2.y - p1.y) * (p2.y - p1.y) + (p2.x - p1.x) * (p2.x - p1.x));
                float result = p3.x - k * (p2.y - p1.y);

                // in case the point is above the line make it the max
                float m = (p2.y - p1.y) / (p2.x - p1.x);
                float c = p2.y - m * p2.x;
                float inverseC = p2.y + 1 / m * p2.x;

                float value = 0;
                if (p3.y < (-1 / m) * p3.x + inverseC)
                    value = ((result - p1.x) / (p2.x - p1.x));
                else
                    value = 1;

                if (value > 1)
                    value = 1;


                //END OF THE AUTOMATIC MAX SECTION
                */



                //TODO: ensure that I'm not throwing away voxels just because I have a low threshold (play around with values)
                // a colocalized pixel is detected above the high tolerence (Scatter region 3)
                if (valChan0 >= thresholdHighValue[colocChannel0] && valChan1 >= thresholdHighValue[colocChannel1])
                {
                    totalVoxelsInROI++;
                    

                    lastCalcedColoc.colocVoxelsRegion3++;
                    region3TotalInensityCh1 += valChan0;
                    region3TotalInensityCh2 += valChan1;

                    // LIN REG
                    float xMinThresh = valChan0 - thresholdHighValue[colocChannel0];
                    float yMinThresh = valChan1 - thresholdHighValue[colocChannel1];
                    xySum += (xMinThresh) * (yMinThresh);
                    x2Sum += (xMinThresh) * (xMinThresh);

                    xy_diffSum += (valChan0 - xMean) * (valChan1 - yMean);
                    x2_diffSum += (valChan0 - xMean) * (valChan0 - xMean);

                    varXXsum += (valChan0 - xMean) * (valChan0 - xMean);
                    varYYsum += (valChan1 - yMean) * (valChan1 - yMean);
                    varXYsum += (valChan0 - xMean) * (valChan1 - yMean);
                }
                // a colocalized pixel is detected between high and low (Region 2)
                else if (valChan0 < thresholdHighValue[colocChannel0] && valChan0 >= thresholdLowValue[colocChannel0] && valChan1 >= thresholdHighValue[colocChannel1])
                {
                    lastCalcedColoc.colocVoxelsRegion2++;
                    region2TotalInensityCh1 += valChan0;
                    region2TotalInensityCh2 += valChan1;
                }
                // a colocalized pixel is detected between high and low (region 1)
                else if (valChan1 < thresholdHighValue[colocChannel1] && valChan1 >= thresholdLowValue[colocChannel1] && valChan0 >= thresholdHighValue[colocChannel0])
                {
                    lastCalcedColoc.colocVoxelsRegion1++;
                    region1TotalInensityCh1 += valChan0;
                    region1TotalInensityCh2 += valChan1;
                }
                // region 4 (sub-threshold pixels, background intensities)
                else if (valChan1 < thresholdHighValue[colocChannel1] && valChan1 >= thresholdLowValue[colocChannel1] && valChan0 < thresholdHighValue[colocChannel0] && valChan0 >= thresholdLowValue[colocChannel0])
                {
                    lastCalcedColoc.colocVoxelsRegion4++;
                    region4TotalInensityCh1 += valChan0;
                    region4TotalInensityCh2 += valChan1;
                }

                // TODO: this ensures that calculations don't take the "background" into account, but did I do it correctly (don't think so) The average should be above the thresholds...?
                if (valChan0 >= thresholdHighValue[colocChannel0] && valChan1 >= thresholdHighValue[colocChannel0])
                {
                    // Calculate PCC
                    //PCC_numer += (valChan0 - chan0Average) * (valChan1 - chan1Average);
                    //PCC_denom1 += (valChan0 - chan0Average) * (valChan0 - chan0Average);
                    //PCC_denom2 += (valChan1 - chan1Average) * (valChan1 - chan1Average);

                    PCC_numer += (valChan0 - xMean) * (valChan1 - yMean);
                    PCC_denom1 += (valChan0 - xMean) * (valChan0 - xMean);
                    PCC_denom2 += (valChan1 - yMean) * (valChan1 - yMean);

                    // Calculate MOC (it's necessary to separete the denominator since miss calculates)
                    MOC_numer += valChan0 * valChan1;
                    MOC_denom1 += (valChan0 * valChan0);
                    MOC_denom2 += (valChan1 * valChan1);
                }

                //Calculate MCC (TODO: check the condition)
                if (valChan1 >= thresholdHighValue[colocChannel1]) //(valChan1 > 0)
                {
                    M1_numer += valChan0;
                }
                M1_denom += valChan0;

                if (valChan0 >= thresholdHighValue[colocChannel0]) // (valChan0 > 0)
                {
                    M2_numer += valChan1;
                }
                M2_denom += valChan1;

                if (i % 50000 == 0)
                {
                    //Debug.Log(i);
                    colCalcPercProgress = i / (float)channel0Data.Length;
                    colocRightLabel.GetComponent<Text>().text = "Step 2 (Calculating metrics 3D): " + (int)(colCalcPercProgress * 100) + "%";
                    yield return null;
                }
            }

            // logic that ensures using the correct index for the mask, since the voxel array is one dimensional
            maskCol++;
            if (maskCol >= size)
            {
                maskCol = 0;
                maskRow++;

                if (maskRow >= size) // use the same mask for all depths
                {
                    maskRow = 0;
                    maskDepth++;
                    depthFraction = -0.5f + (float)maskDepth / loadSample.texDepth;
                }
            }
        }

        // deming regression
        double varX = varXXsum / (totalVoxelsInROI-1);
        double varY = varYYsum / (totalVoxelsInROI-1);

        varXXsum = varXXsum / (totalVoxelsInROI - 1);
        varYYsum = varYYsum / (totalVoxelsInROI - 1);
        varXYsum = varXYsum / (totalVoxelsInROI - 1);

        Debug.Log("VarXX = " + varXXsum + "  VarYY = " + varYYsum + "  VarXY = " + varXYsum);

        double lambda = 1;// varX / varY;

        double B0 = 0.0;
        double B1 = 0.0;

        //double val = lambda * varYYsum - varXXsum;
        //B1 = (val + Math.Sqrt(val * val + 4 * lambda * varXYsum * varXYsum)) / (2 * lambda * varXYsum);



        if (varXYsum < 0)
        {
            Debug.Log("The covariance is negative!");
            double val = lambda * varYYsum - varXXsum;
            B1 = (val - Math.Sqrt(val * val + 4 * lambda * varXYsum * varXYsum)) / (2 * lambda * varXYsum);

            //B1 = -1 / B1;
        }
        else
        {
            double val = lambda * varYYsum - varXXsum;
            B1 = (val + Math.Sqrt(val * val + 4 * lambda * varXYsum * varXYsum)) / (2 * lambda * varXYsum);
        }
        B0 = yMean - B1 * xMean;


        Debug.Log("xySum = " + xySum + "  x2Sum = " + x2Sum + "  xySum/x2Sum = " + xySum / x2Sum);

        // total pixels for channel 0
        lastCalcedColoc.totalPixelCountAboveHigh[0] = lastCalcedColoc.colocVoxelsRegion3 + lastCalcedColoc.colocVoxelsRegion1;
        lastCalcedColoc.totalPixelCountLowToHigh[0] = lastCalcedColoc.colocVoxelsRegion2 + lastCalcedColoc.colocVoxelsRegion4;


        // total pixels for channel 1
        lastCalcedColoc.totalPixelCountAboveHigh[1] = lastCalcedColoc.colocVoxelsRegion3 + lastCalcedColoc.colocVoxelsRegion2;
        lastCalcedColoc.totalPixelCountLowToHigh[1] = lastCalcedColoc.colocVoxelsRegion1 + lastCalcedColoc.colocVoxelsRegion4;


        float percColocalizedAboveHigh = 0f;
        float percColocalizedLowToHigh = 0f;
        float colocCoefCh1 = 0f;
        float colocCoefCh2 = 0f;

        int totalVoxelsAboveThreshold = (lastCalcedColoc.colocVoxelsRegion1 + lastCalcedColoc.colocVoxelsRegion2 + lastCalcedColoc.colocVoxelsRegion3);
        int totalVoxelsLowToHigh = (lastCalcedColoc.colocVoxelsRegion1 + lastCalcedColoc.colocVoxelsRegion2 + lastCalcedColoc.colocVoxelsRegion4);

        // High coloc perctage
        // prevent div by 0
        if (totalVoxelsAboveThreshold < 1) percColocalizedAboveHigh = -1f; else percColocalizedAboveHigh = ((float)lastCalcedColoc.colocVoxelsRegion3 / totalVoxelsAboveThreshold) * 100;

        // Low coloc percentage 
        // prevent div by 0
        if (totalVoxelsLowToHigh < 1) percColocalizedLowToHigh = -1f; else percColocalizedLowToHigh = ((float)lastCalcedColoc.colocVoxelsRegion4 / totalVoxelsLowToHigh) * 100;

        // channel 1 coef
        if (lastCalcedColoc.totalPixelCountAboveHigh[0] < 1) colocCoefCh1 = -1f; else colocCoefCh1 = (float)lastCalcedColoc.colocVoxelsRegion3 / lastCalcedColoc.totalPixelCountAboveHigh[0];

        // channel 2 coef
        if (lastCalcedColoc.totalPixelCountAboveHigh[1] < 1) colocCoefCh2 = -1f; else colocCoefCh2 = (float)lastCalcedColoc.colocVoxelsRegion3 / lastCalcedColoc.totalPixelCountAboveHigh[1];

        lastCalcedColoc.PCC = (float)PCC_numer / (Mathf.Sqrt((float)(PCC_denom1 * PCC_denom2)));
        lastCalcedColoc.MOC = (float)MOC_numer / (Mathf.Sqrt((float)(MOC_denom1 * MOC_denom2)));
        lastCalcedColoc.MCC[0] = (float)(M1_numer / M1_denom);
        lastCalcedColoc.MCC[1] = (float)(M2_numer / M2_denom);

        // FOR SHADER
        internalMat.SetFloat("_PCC_denom", (float)(Mathf.Sqrt((float)(PCC_denom1 * PCC_denom2))));
        internalMat.SetFloat("_MOC_denom", (float)(Mathf.Sqrt((float)(MOC_denom1 * MOC_denom2))));

        //START OF HEATMAP SECTION

        // LIN REG SECTION THROUGH THRESHOLD (NO LONGER THROGH THRESHOLD, now using Deming regression
        //m_LinRegThroughThres = (float)(xySum / x2Sum);
        //m_LinRegThroughThres = (float)((yMean - thresholdHighValue[colocChannel1]) / (xMean - thresholdHighValue[colocChannel0])); // through mean and threshold
        //m_LinRegThroughThres = (float)y_div_x_minThres_Sum;
        //c_LinRegThroughThres = thresholdHighValue[colocChannel1] - m_LinRegThroughThres * thresholdHighValue[colocChannel0];
        m_LinRegThroughThres = (float)B1;
        c_LinRegThroughThres = (float)B0;



        Debug.Log("m thres: " + m_LinRegThroughThres + "   c thres: " + c_LinRegThroughThres);
        Debug.Log("m thres (alternative): " + (yMeanMinThres/xMeanMinThres)); // this is "faking" linear regression... but is statistically incorrect...

        if (thresholdHighValue[colocChannel0] * m_LinRegThroughThres + c_LinRegThroughThres >= thresholdHighValue[colocChannel1])
            p1 = new Vector2(thresholdHighValue[colocChannel0], thresholdHighValue[colocChannel0] * m_LinRegThroughThres + c_LinRegThroughThres);
        else
            p1 = new Vector2((thresholdHighValue[colocChannel1] - c_LinRegThroughThres) / m_LinRegThroughThres, thresholdHighValue[colocChannel1]);

        if (m_LinRegThroughThres + c_LinRegThroughThres > 1f)
            p2 = new Vector2((1f-c_LinRegThroughThres)/m_LinRegThroughThres,1f);
        else
            p2 = new Vector2(1f, m_LinRegThroughThres + c_LinRegThroughThres);
            
        
        /// LIN REG SECTION WITH NO RESTRICTIONS
        m_LinReg = (float)(xy_diffSum / x2_diffSum);
        c_LinReg = (float)(yMean - m_LinReg * xMean);
        Debug.Log("m: " + m_LinReg + "   c: " + c_LinReg);
        /*
        if(thresholdHighValue[colocChannel0]*m_LinReg + c_LinReg >= thresholdHighValue[colocChannel1])
            p1 = new Vector2(thresholdHighValue[colocChannel0], thresholdHighValue[colocChannel0] * m_LinReg + c_LinReg);
        else
            p1 = new Vector2((thresholdHighValue[colocChannel1]-c_LinReg)/m_LinReg, thresholdHighValue[colocChannel1]);

        if (m_LinReg + c_LinReg > 1f)
            p2 = new Vector2((1f - c_LinReg) / m_LinReg, 1f);
        else
            p2 = new Vector2(1f, m_LinReg + c_LinReg);

        m_LinRegThroughThres = m_LinReg;
        c_LinRegThroughThres = c_LinReg;
        */

        // for shader
        internalMat.SetVector("_p1", p1);
        internalMat.SetVector("_p2", p2);

        // DONE WITH HEATMAP SECTION

        colocLeftLabel.GetComponent<Text>().text = "";//string.Format("Btwn Low & High (total) = <b>{0:0.0}%</b>\nBtwn Low & High (div. 1st) = <b>{1:0.0}%</b>\nBtwn Low & High (div. 2nd) = <b>{2:0.0}%</b>\nPCC = <b>{3:0.000}</b>\tMOC = <b>{4:0.000}</b>", (percColocalizedLow * 100), (percColocalizedLow_chan0 * 100.0), (percColocalizedLow_chan1 * 100.0), lastCalcedColoc.PCC, lastCalcedColoc.MOC);
        colocRightLabel.GetComponent<Text>().text = "Done 3D! Starting 2D calculation!";//string.Format("Above High (total) = <b>{0:0.0}%</b>\nAbove High (div. 1st) = <b>{1:0.0}%</b>\nAbove High (div. 2nd) = <b>{2:0.0}%</b>\nMCC1 = <b>{3:0.000}</b>\tMCC2 = <b>{4:0.000}</b>", (percColocalizedHigh * 100), (percColocalizedHigh_chan0 * 100.0), (percColocalizedHigh_chan1 * 100.0), lastCalcedColoc.MCC[0], lastCalcedColoc.MCC[1]);


        float region1Volume = lastCalcedColoc.colocVoxelsRegion1 * (loadSample.sampleDim.x / size) * (loadSample.sampleDim.z / loadSample.texDepth);
        float region2Volume = lastCalcedColoc.colocVoxelsRegion2 * (loadSample.sampleDim.x / size) * (loadSample.sampleDim.z / loadSample.texDepth);
        float region3Volume = lastCalcedColoc.colocVoxelsRegion3 * (loadSample.sampleDim.x / size) * (loadSample.sampleDim.z / loadSample.texDepth);
        float region4Volume = lastCalcedColoc.colocVoxelsRegion4 * (loadSample.sampleDim.x / size) * (loadSample.sampleDim.z / loadSample.texDepth);

        colocScatterLabel.GetComponent<Text>().text = string.Format("3D calculation results:\n" +
            "Channel 1:{0}\nChannel 2:{1}\n" +
            "\nAbove High % Coloc: {6:0.0}%\nLow to high % Coloc: {14:0.0}%\n" +
            "\nCoef 1: {7:0.000}  Coef 2:{8:0.000}\n" +
            "\nM1: {9:0.000}  M2: {10:0.000}\n" +
            "\nPCC: {11:0.000}  RxR: {12:0.000}  \n\nMOC: {13:0.000}\n",
            channelNames[colocChannel0], channelNames[colocChannel1], lastCalcedColoc.colocVoxelsRegion1, lastCalcedColoc.colocVoxelsRegion2, lastCalcedColoc.colocVoxelsRegion3, lastCalcedColoc.colocVoxelsRegion4,
            percColocalizedAboveHigh, colocCoefCh1, colocCoefCh2, lastCalcedColoc.MCC[0], lastCalcedColoc.MCC[1], lastCalcedColoc.PCC, lastCalcedColoc.PCC * lastCalcedColoc.PCC, lastCalcedColoc.MOC,
            percColocalizedLowToHigh);

        string csvText = string.Format("sep=,\n" +
            "3D Colocalization calculations\n" +
            "Channel 1:,{0},Channel 2:,{1}\n" +
            "Threshold Ch1 Low:,{15},Ch1 High:,{16}\n" +
            "Threshold Ch2 Low:,{17},Ch2 High:,{18}\n" +
            "Scatter Region,Number Voxels,Volume(um x um x um),Mean Intensity Ch1,Mean Intensity Ch2,% colocalization,Ch1 Coloc Coef,Ch2 Coloc Coef,M1,M2,PCC(R),RxR,MOC\n" +
            "1,{2},{19:0.0},{23:0},{24:0},,,,,,,,\n" +
            "2,{3},{20:0.0},{25:0},{26:0},,,,,,,,\n" +
            "3,{4},{21:0.0},{27:0},{28:0},{6:0.0},{7:0.000},{8:0.000},{9:0.000},{10:0.000},{11:0.000},{12:0.000},{13:0.000}\n" +
            "4,{5},{22:0.0},{29:0},{30:0},{14:0.0},,,,,,,\n",
            channelNames[colocChannel0], channelNames[colocChannel1], lastCalcedColoc.colocVoxelsRegion1, lastCalcedColoc.colocVoxelsRegion2, lastCalcedColoc.colocVoxelsRegion3, lastCalcedColoc.colocVoxelsRegion4,
            percColocalizedAboveHigh, colocCoefCh1, colocCoefCh2, lastCalcedColoc.MCC[0], lastCalcedColoc.MCC[1], lastCalcedColoc.PCC, lastCalcedColoc.PCC * lastCalcedColoc.PCC, lastCalcedColoc.MOC,
            percColocalizedLowToHigh, (int)(thresholdLowValue[colocChannel0] * thresholdMaxInt), (int)(thresholdHighValue[colocChannel0] * thresholdMaxInt), (int)(thresholdLowValue[colocChannel1] * thresholdMaxInt), (int)(thresholdHighValue[colocChannel1] * thresholdMaxInt),
            region1Volume, region2Volume, region3Volume, region4Volume,
            (region1TotalInensityCh1 / lastCalcedColoc.colocVoxelsRegion1) * thresholdMaxInt, (region1TotalInensityCh2 / lastCalcedColoc.colocVoxelsRegion1) * thresholdMaxInt,
            (region2TotalInensityCh1 / lastCalcedColoc.colocVoxelsRegion2) * thresholdMaxInt, (region2TotalInensityCh2 / lastCalcedColoc.colocVoxelsRegion2) * thresholdMaxInt,
            (region3TotalInensityCh1 / lastCalcedColoc.colocVoxelsRegion3) * thresholdMaxInt, (region3TotalInensityCh2 / lastCalcedColoc.colocVoxelsRegion3) * thresholdMaxInt,
            (region4TotalInensityCh1 / lastCalcedColoc.colocVoxelsRegion4) * thresholdMaxInt, (region4TotalInensityCh2 / lastCalcedColoc.colocVoxelsRegion4) * thresholdMaxInt);

        try
        {
            string path = Application.persistentDataPath + "/Results/ColocMetrics/";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            System.IO.File.WriteAllText(path + "ColocMetricsTable3D_" + Time.time + ".csv", csvText);


            if (!BenProject)
            {
                string pathScat = Application.persistentDataPath + "/Results/Scatter/";
                if (!Directory.Exists(pathScat))
                    Directory.CreateDirectory(pathScat);
                System.IO.File.WriteAllText(pathScat + "regLine3D_" + Time.time + ".txt", "m_3D = " + m_LinRegThroughThres + "  c_3D = " + c_LinRegThroughThres +
                    " Thresh 0 = " + (int)(thresholdHighValue[colocChannel0] * thresholdMaxInt) + "  Thresh 1 = " + (int)(thresholdHighValue[colocChannel1] * thresholdMaxInt) +
                    " xMeanNMDP = " + chan0AverageNMDP + " yMeanNMDP = " + chan1AverageNMDP + " _ch0AverageAboveThres = " + xMean + " _ch1AverageAboveThres = " + yMean);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
        
        if(BenProject)
        {
            char[] splitchar = { '\\' };
            string[] parts = thisImageFolderStructure.Split(splitchar);
            string csvTextBEN = String.Format("{0},{1},{2},{3},{4},{5},{6},{7}\n", parts[1], parts[2], parts[3], parts[4], lastCalcedColoc.PCC, lastCalcedColoc.MOC, lastCalcedColoc.MCC[0], lastCalcedColoc.MCC[1]);

            string pathBEN = saveLocation3D +  "\\Results.csv";            

            System.IO.File.AppendAllText(pathBEN, csvTextBEN);
        }


        // calculate 2D colocalization for a single slice
        if (false)
        {
            //TODO: this should not be hardcoded, choose interactively with slider
            //TODO: allow to only have the data "onlyInsideROI" 
            // get only a single slice
            Color[] sliceImageCh0 = new Color[size * size];
            Array.Copy(channel0Data, size * size * currentSlice, sliceImageCh0, 0, size * size);

            Color[] sliceImageCh1 = new Color[size * size];
            Array.Copy(channel1Data, size * size * currentSlice, sliceImageCh1, 0, size * size);


            yield return StartCoroutine(calculateColocalizationMIP2D(sliceImageCh0, sliceImageCh1, "Slice"));
            colocalization2DLayerSlice = colocalization2DLayer;
        }

        // calculate MIP colocalization
        if (true)
        {
            yield return GenerateMIPForCalc();

            yield return StartCoroutine(calculateColocalizationMIP2D(MIPSliceCh0, MIPSliceCh1, "MIP"));
            // colocalization2DLayerMIP = colocalization2DLayer;
        }

        colocBeingCalculated = false;
        yield return null;


        if (showScatterPlot)
        {
            colocResultsCanvase.SetActive(true);
            yield return StartCoroutine(ColocColorScatterToTexture3D());
            yield return StartCoroutine(ColocFrequencyScatterToTexture3D());
            ScatterPlotDropbox.GetComponent<Dropdown>().value = 0;
            switch (ScatterPlotDropbox.GetComponent<Dropdown>().value)
            {
                case (int)ScatterPlotTypes.FrequencyScatter: // frequency
                    ScatterPlot.GetComponent<Renderer>().material.mainTexture = freqScatterTex;
                    ScatterHeatmapLegend.SetActive(true);
                    break;
                case (int)ScatterPlotTypes.DisplayColors:
                    ScatterPlot.GetComponent<Renderer>().material.mainTexture = colorScatterTex;
                    ScatterHeatmapLegend.SetActive(false);
                    break;
            }
        }

        /* //Automatically open path
        if (!BenProject)
        {
            String pathOut = Application.persistentDataPath + "/Results";
            System.Diagnostics.Process.Start(pathOut);
        }
        */
    }

    IEnumerator calculateColocalizationMIP2D(Color[] sliceImageCh0, Color[] sliceImageCh1, string label = "Slice")
    {
        colocBeingCalculated = true;

        colocalization2DLayer = new Color[sliceImageCh0.Length];
        Debug.Log("Initialized colocalization2D Layer " + colocalization2DLayer.Length);

        double chan0CountNMDP = 0f;
        double chan1CountNMDP = 0f;

        double xySum = 0.0;
        double x2Sum = 0.0;

        double xMax = 0.0;
        double yMax = 0.0;

        double varXXsum = 0.0;
        double varYYsum = 0.0;
        double varXYsum = 0.0;


        double xMeanMinThres_MIP = 0.0;
        double yMeanMinThres_MIP = 0.0;
        double xy_diffSum = 0.0;
        double x2_diffSum = 0.0;
        double y_div_x_minThres_Sum = 0.0;

        totalVoxelsInROIMIP = 0;
        chan0AverageNMDP_MIP = 0f;
        chan1AverageNMDP_MIP = 0f;
        chan0MaxNMDP_MIP = 0f;
        chan1MaxNMDP_MIP = 0f;

        chan0Average_MIP = 0f;
        chan1Average_MIP = 0f;
        chan0Max_MIP = 0f;
        chan1Max_MIP = 0f;
        xMean_MIP = 0.0;
        yMean_MIP = 0.0;

        double PCC_numer = 0.0;
        double PCC_denom1 = 0.0;
        double PCC_denom2 = 0.0;

        double MOC_numer = 0.0;
        double MOC_denom1 = 0.0;
        double MOC_denom2 = 0.0;

        // ...for MCC
        double M1_numer = 0.0;
        double M1_denom = 0.0;
        double M2_numer = 0.0;
        double M2_denom = 0.0;


        double region1TotalIntensityCh1 = 0.0;
        double region2TotalIntensityCh1 = 0.0;
        double region3TotalIntensityCh1 = 0.0;
        double region4TotalIntensityCh1 = 0.0;

        double region1TotalIntensityCh2 = 0.0;
        double region2TotalIntensityCh2 = 0.0;
        double region3TotalIntensityCh2 = 0.0;
        double region4TotalIntensityCh2 = 0.0;

        int totalPixelsRegion4 = 0;
        int totalPixelsRegion3 = 0;
        int totalPixelsRegion2 = 0;
        int totalPixelsRegion1 = 0;

        int maskRow = 0;
        int maskCol = 0;

        int pixelsInROI = 0;
        int pixelsInROIandAboveThresh = 0;

        for (int i = 0; i < sliceImageCh0.Length; i++)
        {
            // TODO: I have to thoroughly test this
            // if the pixel is in the ROI
            //if (ROImaskTest2D(maskCol, maskRow))
            if (ROIMask2D[maskCol, maskRow])
            {
                //////////////////LIN REG CODE STARTS HERE
                float valChan0 = -1f;
                float valChan1 = -1f;
                switch (colocChannel0)
                {
                    case 0: // red
                        valChan0 = sliceImageCh0[i].r;
                        break;
                    case 1: // green
                        valChan0 = sliceImageCh0[i].g;
                        break;
                    case 2: // blue
                        valChan0 = sliceImageCh0[i].b;
                        break;
                    case 3: // purple
                        valChan0 = sliceImageCh0[i].r;
                        break;
                }
                switch (colocChannel1)
                {
                    case 0: // red
                        valChan1 = sliceImageCh1[i].r;
                        break;
                    case 1: // green
                        valChan1 = sliceImageCh1[i].g;
                        break;
                    case 2: // blue
                        valChan1 = sliceImageCh1[i].b;
                        break;
                    case 3: // purple
                        valChan1 = sliceImageCh1[i].r;
                        break;
                }

                //the way nMDP handles the thresholds
                if (valChan0 >= thresholdHighValue[colocChannel0])
                {
                    chan0AverageNMDP_MIP += valChan0;
                    chan0MaxNMDP_MIP = Math.Max(valChan0, chan0MaxNMDP_MIP);
                    chan0CountNMDP++;
                }

                if (valChan1 >= thresholdHighValue[colocChannel1])
                {
                    chan1AverageNMDP_MIP += valChan1;
                    chan1MaxNMDP_MIP = Math.Max(valChan1, chan1MaxNMDP_MIP);
                    chan1CountNMDP++;
                }

                // valChan0/1 is a value between 0 and 1
                if (valChan0 >= thresholdHighValue[colocChannel0] && valChan1 >= thresholdHighValue[colocChannel1])
                {
                    xMean_MIP += valChan0;
                    yMean_MIP += valChan1;
                    pixelsInROIandAboveThresh++;

                    // for Deming reg
                    float xMinThresh = valChan0 - thresholdHighValue[colocChannel0];
                    float yMinThresh = valChan1 - thresholdHighValue[colocChannel1];

                    y_div_x_minThres_Sum += yMinThresh / xMinThresh;

                    xMeanMinThres_MIP += xMinThresh;
                    yMeanMinThres_MIP += yMinThresh;


                    xMax = valChan0 > xMax ? valChan0 : xMax;
                    yMax = valChan1 > yMax ? valChan1 : yMax;


                }
                //////////////////LIN REG CODE ENDS HERE
                pixelsInROI++;

                switch (colocChannel0)
                {
                    case 0: // red
                        chan0Average_MIP += sliceImageCh0[i].r;
                        chan0Max_MIP = sliceImageCh0[i].r > chan0Max_MIP ? sliceImageCh0[i].r : chan0Max_MIP;
                        break;
                    case 1: // green
                        chan0Average_MIP += sliceImageCh0[i].g;
                        chan0Max_MIP = sliceImageCh0[i].g > chan0Max_MIP ? sliceImageCh0[i].g : chan0Max_MIP;
                        break;
                    case 2: // blue
                        chan0Average_MIP += sliceImageCh0[i].b;
                        chan0Max_MIP = sliceImageCh0[i].b > chan0Max_MIP ? sliceImageCh0[i].b : chan0Max_MIP;
                        break;
                    case 3: // purple
                        chan0Average_MIP += sliceImageCh0[i].r;
                        chan0Max_MIP = sliceImageCh0[i].r > chan0Max_MIP ? sliceImageCh0[i].r : chan0Max_MIP;
                        break;
                }
                switch (colocChannel1)
                {
                    case 0: // red
                        chan1Average_MIP += sliceImageCh1[i].r;
                        chan1Max_MIP = sliceImageCh1[i].r > chan1Max_MIP ? sliceImageCh1[i].r : chan1Max_MIP;
                        break;
                    case 1: // green
                        chan1Average_MIP += sliceImageCh1[i].g;
                        chan1Max_MIP = sliceImageCh1[i].g > chan1Max_MIP ? sliceImageCh1[i].g : chan1Max_MIP;
                        break;
                    case 2: // blue
                        chan1Average_MIP += sliceImageCh1[i].b;
                        chan1Max_MIP = sliceImageCh1[i].b > chan1Max_MIP ? sliceImageCh1[i].b : chan1Max_MIP;
                        break;
                    case 3: // purple
                        chan1Average_MIP += sliceImageCh1[i].r;
                        chan1Max_MIP = sliceImageCh1[i].r > chan1Max_MIP ? sliceImageCh1[i].r : chan1Max_MIP;
                        break;
                }

                if (i % 500000 == 0)
                {
                    //Debug.Log(i);
                    colCalcPercProgress = i / (float)sliceImageCh0.Length;
                    colocRightLabel.GetComponent<Text>().text = "Step 1 (Channel Averages" + label + "): " + (int)(colCalcPercProgress * 100) + "%";
                    yield return null;
                }
            }

            // logic that ensures using the correct index for the mask, since the voxel array is one dimensional
            maskCol++;
            if (maskCol >= size)
            {
                maskCol = 0;
                maskRow++;

                if (maskRow >= size) // use the same mask for all depths
                {
                    maskRow = 0;
                    Debug.Log("End of slice image reached... this should not occur more than once");
                }
            }
        }

        //PCC is average of all the pixels in the sample (according to wikipedia)
        chan0Average_MIP = chan0Average_MIP / (float)pixelsInROI;
        chan1Average_MIP = chan1Average_MIP / (float)pixelsInROI;

        chan0AverageNMDP_MIP = chan0AverageNMDP_MIP / chan0CountNMDP;
        chan1AverageNMDP_MIP = chan1AverageNMDP_MIP / chan1CountNMDP;

        xMean_MIP = xMean_MIP / pixelsInROIandAboveThresh;
        yMean_MIP = yMean_MIP / pixelsInROIandAboveThresh;

        Debug.Log(String.Format("MIP: MAX Channel 1: {0} Channel 2: {1}  Average {2}", xMax * thresholdMaxInt, yMax * thresholdMaxInt, ((xMax + yMax) / 2.0f) * thresholdMaxInt));
        Debug.Log(String.Format("MIP: Channel 1 average: {0} Channel 2 average: {1}", xMean_MIP * thresholdMaxInt, yMean_MIP * thresholdMaxInt));

        maskRow = 0;
        maskCol = 0;

        for (int i = 0; i < sliceImageCh0.Length; i++)
        {

            colocalization2DLayer[i] = Color.black;
            // TODO: I have to thoroughly test this method
            //if (ROImaskTest2D(maskCol, maskRow))
            if (ROIMask2D[maskCol, maskRow])
            {
                float valChan0 = -1f;
                float valChan1 = -1f;
                switch (colocChannel0)
                {
                    case 0: // red
                        valChan0 = sliceImageCh0[i].r;
                        break;
                    case 1: // green
                        valChan0 = sliceImageCh0[i].g;
                        break;
                    case 2: // blue
                        valChan0 = sliceImageCh0[i].b;
                        break;
                    case 3: // purple
                        valChan0 = sliceImageCh0[i].r;
                        break;
                }
                switch (colocChannel1)
                {
                    case 0: // red
                        valChan1 = sliceImageCh1[i].r;
                        break;
                    case 1: // green
                        valChan1 = sliceImageCh1[i].g;
                        break;
                    case 2: // blue
                        valChan1 = sliceImageCh1[i].b;
                        break;
                    case 3: // purple
                        valChan1 = sliceImageCh1[i].r;
                        break;
                }

                //TODO: ensure that I'm not throwing away voxels just because I have a low threshold (play around with values)
                // a colocalized pixel is detected above the high tolerence (Scatter region 3)
                if (valChan0 >= thresholdHighValue[colocChannel0] && valChan1 >= thresholdHighValue[colocChannel1])
                {
                    totalVoxelsInROIMIP++;

                    colocalization2DLayer[i] = Color.white;
                    totalPixelsRegion3++;
                    region3TotalIntensityCh1 += valChan0;
                    region3TotalIntensityCh2 += valChan1;

                    // LIN REG
                    float xMinThresh = valChan0 - thresholdHighValue[colocChannel0];
                    float yMinThresh = valChan1 - thresholdHighValue[colocChannel1];
                    xySum += (xMinThresh * yMinThresh);
                    x2Sum += (xMinThresh * xMinThresh);

                    xy_diffSum += (valChan0 - xMean_MIP) * (valChan1 - yMean_MIP);
                    x2_diffSum += (valChan0 - xMean_MIP) * (valChan0 - xMean_MIP);

                    varXXsum += (valChan0 - xMean_MIP) * (valChan0 - xMean_MIP);
                    varYYsum += (valChan1 - yMean_MIP) * (valChan1 - yMean_MIP);
                    varXYsum += (valChan0 - xMean_MIP) * (valChan1 - yMean_MIP);
                }
                // a colocalized pixel is detected between high and low (Region 2)
                else if (valChan0 < thresholdHighValue[colocChannel0] && valChan0 >= thresholdLowValue[colocChannel0] && valChan1 >= thresholdHighValue[colocChannel1])
                {
                    totalPixelsRegion2++;
                    region2TotalIntensityCh1 += valChan0;
                    region2TotalIntensityCh2 += valChan1;
                }
                // a colocalized pixel is detected between high and low (region 1)
                else if (valChan1 < thresholdHighValue[colocChannel1] && valChan1 >= thresholdLowValue[colocChannel1] && valChan0 >= thresholdHighValue[colocChannel0])
                {
                    totalPixelsRegion1++;
                    region1TotalIntensityCh1 += valChan0;
                    region1TotalIntensityCh2 += valChan1;
                }
                // region 4 (sub-threshold pixels, background intensities)
                else if (valChan1 < thresholdHighValue[colocChannel1] && valChan1 >= thresholdLowValue[colocChannel1] && valChan0 < thresholdHighValue[colocChannel0] && valChan0 >= thresholdLowValue[colocChannel0])
                {
                    totalPixelsRegion4++;
                    region4TotalIntensityCh1 += valChan0;
                    region4TotalIntensityCh2 += valChan1;
                }

                // TODO: this ensures that calculations don't take the "background" into account, but did I do it correctly (don't think so)
                if (valChan0 >= thresholdHighValue[colocChannel0] && valChan1 >= thresholdHighValue[colocChannel1])
                {
                    // Calculate PCC
                    //PCC_numer += (valChan0 - chan0Average_MIP) * (valChan1 - chan1Average_MIP);
                    //PCC_denom1 += (valChan0 - chan0Average_MIP) * (valChan0 - chan0Average_MIP);
                    //PCC_denom2 += (valChan1 - chan1Average_MIP) * (valChan1 - chan1Average_MIP);

                    PCC_numer += (valChan0 - xMean_MIP) * (valChan1 - yMean_MIP);
                    PCC_denom1 += (valChan0 - xMean_MIP) * (valChan0 - xMean_MIP);
                    PCC_denom2 += (valChan1 - yMean_MIP) * (valChan1 - yMean_MIP);

                    // Calculate MOC (it's necessary to separete the denominator since miss calculates)
                    MOC_numer += valChan0 * valChan1;
                    MOC_denom1 += (valChan0 * valChan0);
                    MOC_denom2 += (valChan1 * valChan1);
                }

                //Calculate MCC (TODO: check the condition)
                if (valChan1 >= thresholdHighValue[colocChannel1]) //(valChan1 > 0)
                {
                    M1_numer += valChan0;
                }
                M1_denom += valChan0;

                if (valChan0 >= thresholdHighValue[colocChannel0]) // (valChan0 > 0)
                {
                    M2_numer += valChan1;
                }
                M2_denom += valChan1;

                if (i % 50000 == 0)
                {
                    //Debug.Log(i);
                    colCalcPercProgress = i / (float)sliceImageCh0.Length;
                    colocRightLabel.GetComponent<Text>().text = "Step 2 (Calculating metrics 2D " + label + "): " + (int)(colCalcPercProgress * 100) + "%";
                    yield return null;
                }
            }

            // logic that ensures using the correct index for the mask, since the voxel array is one dimensional
            maskCol++;
            if (maskCol >= size)
            {
                maskCol = 0;
                maskRow++;

                if (maskRow >= size) // use the same mask for all depths
                {
                    maskRow = 0;
                    Debug.Log("End of " + label + " image reached... this should not occur");
                }
            }
        }

        // total pixels for channel 0
        int totalPixelsAboveHighCh0 = totalPixelsRegion3 + totalPixelsRegion1;
        int totalPixelsLowToHighCh0 = totalPixelsRegion2 + totalPixelsRegion4;

        // total pixels for channel 1
        int totalPixelsAboveHighCh1 = totalPixelsRegion3 + totalPixelsRegion2;
        int totalPixelsLowToHighCh1 = totalPixelsRegion1 + totalPixelsRegion4;


        float percColocalizedAboveHigh = 0f;
        float percColocalizedLowToHigh = 0f;
        float colocCoefCh1 = 0f;
        float colocCoefCh2 = 0f;
        float PCC = 0f, MOC = 0f, M1 = 0f, M2 = 0f;

        int totalVoxelsAboveThreshold = (totalPixelsRegion1 + totalPixelsRegion2 + totalPixelsRegion3);
        int totalVoxelsLowToHigh = (totalPixelsRegion1 + totalPixelsRegion2 + totalPixelsRegion4);

        // High coloc perctage
        // prevent div by 0
        if (totalVoxelsAboveThreshold < 1) percColocalizedAboveHigh = -1f; else percColocalizedAboveHigh = ((float)totalPixelsRegion3 / totalVoxelsAboveThreshold) * 100;

        // Low coloc percentage 
        // prevent div by 0
        if (totalVoxelsLowToHigh < 1) percColocalizedLowToHigh = -1f; else percColocalizedLowToHigh = ((float)totalPixelsRegion4 / totalVoxelsLowToHigh) * 100;

        // channel 1 coef
        if (totalPixelsAboveHighCh0 < 1) colocCoefCh1 = -1f; else colocCoefCh1 = (float)totalPixelsRegion3 / totalPixelsAboveHighCh0;

        // channel 2 coef
        if (totalPixelsAboveHighCh1 < 1) colocCoefCh2 = -1f; else colocCoefCh2 = (float)totalPixelsRegion3 / totalPixelsAboveHighCh1;

        PCC = (float)PCC_numer / (Mathf.Sqrt((float)(PCC_denom1 * PCC_denom2)));
        MOC = (float)MOC_numer / (Mathf.Sqrt((float)(MOC_denom1 * MOC_denom2)));
        M1 = (float)(M1_numer / M1_denom);
        M2 = (float)(M2_numer / M2_denom);


        // deming regression
        double varX = varXXsum / (totalVoxelsInROI - 1);
        double varY = varYYsum / (totalVoxelsInROI - 1);

        varXXsum = varXXsum / (totalVoxelsInROI - 1);
        varYYsum = varYYsum / (totalVoxelsInROI - 1);
        varXYsum = varXYsum / (totalVoxelsInROI - 1);

        double lambda = 1;// varX / varY;

        double B0 = 0.0;
        double B1 = 0.0;

        //double val = lambda * varYYsum - varXXsum;
        //B1 = (val + Math.Sqrt(val * val + 4 * lambda * varXYsum * varXYsum)) / (2 * lambda * varXYsum);
        
        if (varXYsum < 0)
        {
            Debug.Log("The covariance is negative!");
            double val = lambda * varYYsum - varXXsum;
            B1 = (val - Math.Sqrt(val * val + 4 * lambda * varXYsum * varXYsum)) / (2 * lambda * varXYsum);

            //B1 = -1 / B1;
        }
        else
        {
            double val = lambda * varYYsum - varXXsum;
            B1 = (val + Math.Sqrt(val * val + 4 * lambda * varXYsum * varXYsum)) / (2 * lambda * varXYsum);
        }
        B0 = yMean_MIP - B1 * xMean_MIP;



        // LIN REG SECTION THROUGH THRESHOLD
        //m_LinRegThroughThresMIP = (float)(xySum / x2Sum);
        //m_LinRegThroughThresMIP = (float)(xy_diffSum / x2_diffSum); // don't use this
        //c_LinRegThroughThresMIP = thresholdHighValue[colocChannel1] - m_LinRegThroughThresMIP * thresholdHighValue[colocChannel0];

        m_LinRegThroughThresMIP = (float)B1;
        c_LinRegThroughThresMIP = (float)B0;

        Debug.Log("MIP m thres: " + m_LinRegThroughThresMIP + "   c thres: " + c_LinRegThroughThresMIP);

        if (thresholdHighValue[colocChannel0] * m_LinRegThroughThresMIP + c_LinRegThroughThresMIP >= thresholdHighValue[colocChannel1])
            p1MIP = new Vector2(thresholdHighValue[colocChannel0], thresholdHighValue[colocChannel0] * m_LinRegThroughThresMIP + c_LinRegThroughThresMIP);
        else
            p1MIP = new Vector2((thresholdHighValue[colocChannel1] - c_LinRegThroughThresMIP) / m_LinRegThroughThresMIP, thresholdHighValue[colocChannel1]);

        if (m_LinRegThroughThresMIP + c_LinRegThroughThresMIP > 1f)
            p2MIP = new Vector2((1f - c_LinRegThroughThresMIP) / m_LinRegThroughThresMIP, 1f);
        else
            p2MIP = new Vector2(1f, m_LinRegThroughThresMIP + c_LinRegThroughThresMIP);



        colocLeftLabel.GetComponent<Text>().text = "";
        colocRightLabel.GetComponent<Text>().text = "Done 2D " + label + "!";


        float region1Area = totalPixelsRegion1 * loadSample.sampleDim.x / size;
        float region2Area = totalPixelsRegion2 * loadSample.sampleDim.x / size;
        float region3Area = totalPixelsRegion3 * loadSample.sampleDim.x / size;
        float region4Area = totalPixelsRegion4 * loadSample.sampleDim.x / size;

        /*
        colocScatterLabel.GetComponent<Text>().text = string.Format("Channel 1:{0}  Channel 2:{1} " + label + "\n" +
            "High %:{6:0.0}  Low %:{14:0.0}\n" +
            "Coef 1: {7:0.000}  Coef 2:{8:0.000}\n" +
            "M1: {9:0.000}  M2: {10:0.000}\n" +
            "PCC: {11:0.000}  RxR: {12:0.000}  \nMOC: {13:0.000}\n",
            channelNames[colocChannel0], channelNames[colocChannel1], totalPixelsRegion1, totalPixelsRegion2, totalPixelsRegion3, totalPixelsRegion4,
            percColocalizedAboveHigh, colocCoefCh1, colocCoefCh2, M1, M2, PCC, PCC * PCC, MOC,
            percColocalizedLowToHigh);
            */

        string csvText = string.Format("sep=,\n" +
            "2D Colocalization Calculation (" + label + ")\n" +
            "Channel 1:,{0},Channel 2:,{1}\n" +
            "Threshold Ch1 Low:,{15},Ch1 High:,{16}\n" +
            "Threshold Ch2 Low:,{17},Ch2 High:,{18}\n" +
            "Scatter Region,Number Pixels,Area(um x um),Mean Intensity Ch1,Mean Intensity Ch2,% colocalization,Ch1 Coloc Coef,Ch2 Coloc Coef,M1,M2,PCC(R),RxR,MOC\n" +
            "1,{2},{19:0.0},{23:0},{24:0},,,,,,,,\n" +
            "2,{3},{20:0.0},{25:0},{26:0},,,,,,,,\n" +
            "3,{4},{21:0.0},{27:0},{28:0},{6:0.0},{7:0.000},{8:0.000},{9:0.000},{10:0.000},{11:0.000},{12:0.000},{13:0.000}\n" +
            "4,{5},{22:0.0},{29:0},{30:0},{14:0.0},,,,,,,\n",
            channelNames[colocChannel0], channelNames[colocChannel1], totalPixelsRegion1, totalPixelsRegion2, totalPixelsRegion3, totalPixelsRegion4,
            percColocalizedAboveHigh, colocCoefCh1, colocCoefCh2, M1, M2, PCC, PCC * PCC, MOC,
            percColocalizedLowToHigh, (int)(thresholdLowValue[colocChannel0] * thresholdMaxInt), (int)(thresholdHighValue[colocChannel0] * thresholdMaxInt), (int)(thresholdLowValue[colocChannel1] * thresholdMaxInt), (int)(thresholdHighValue[colocChannel1] * thresholdMaxInt),
            region1Area, region2Area, region3Area, region4Area,
            (region1TotalIntensityCh1 / totalPixelsRegion1) * thresholdMaxInt, (region1TotalIntensityCh2 / totalPixelsRegion1) * thresholdMaxInt,
            (region2TotalIntensityCh1 / totalPixelsRegion2) * thresholdMaxInt, (region2TotalIntensityCh2 / totalPixelsRegion2) * thresholdMaxInt,
            (region3TotalIntensityCh1 / totalPixelsRegion3) * thresholdMaxInt, (region3TotalIntensityCh2 / totalPixelsRegion3) * thresholdMaxInt,
            (region4TotalIntensityCh1 / totalPixelsRegion4) * thresholdMaxInt, (region4TotalIntensityCh2 / totalPixelsRegion4) * thresholdMaxInt);

        try
        {
            string path = Application.persistentDataPath + "/Results/ColocMetrics/";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            System.IO.File.WriteAllText(path+"ColocMetricsTable2D" + label + "_" + Time.time + ".csv", csvText);

            if (!BenProject)
            {
                string pathScat = Application.persistentDataPath + "/Results/Scatter/";
                if (!Directory.Exists(pathScat))
                    Directory.CreateDirectory(pathScat);
                System.IO.File.WriteAllText(pathScat + "regLineMIP_" + Time.time + ".txt", "m_MIP = " + m_LinRegThroughThresMIP + "  c_MIP = " + c_LinRegThroughThresMIP +
                    " Thresh 0 = " + (int)(thresholdHighValue[colocChannel0] * thresholdMaxInt) + "  Thresh 1 = " + (int)(thresholdHighValue[colocChannel1] * thresholdMaxInt) +
                    " xMeanNMDP = " + chan0AverageNMDP_MIP + " yMeanNMDP = " + chan1AverageNMDP_MIP + " _ch0AverageAboveThres = " + xMean_MIP + " _ch1AverageAboveThres = " + yMean_MIP);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }


        if (BenProject)
        {
            char[] splitchar = { '\\' };
            string[] parts = thisImageFolderStructure.Split(splitchar);            
            string csvTextBEN = String.Format("{0},{1},{2},{3},{4},{5},{6},{7}\n", parts[1], parts[2], parts[3], parts[4], PCC, MOC, M1, M2);

            string pathBEN = saveLocation2D + "\\Results.csv";

            System.IO.File.AppendAllText(pathBEN, csvTextBEN);
        }

        colocBeingCalculated = false;
        yield return null;

        // TODO: I still need to do this in 2D... I think I might have done this now...
        if (showScatterPlot)
        {
            yield return StartCoroutine(ColocColorScatterToTextureMIP2D(sliceImageCh0, sliceImageCh1, label + "2D"));
            yield return StartCoroutine(ColocFrequencyScatterToTextureMIP2D(sliceImageCh0, sliceImageCh1, label + "2D"));
        }

        if (label == "MIP")
        {
            colocalization2DLayerMIP = new Color[size * size];
            Array.Copy(colocalization2DLayer, colocalization2DLayerMIP, size * size);
            Debug.Log("Initialized colocalization2DLayerMIP " + colocalization2DLayerMIP.ToString());
        }

        // export MIP images
        StartCoroutine(GenerateCompleteMIP_v2(onlyInsideROI));
    }


    IEnumerator updateROIMask()
    {
        updateMaskRunning = true;
        maskShouldUpdate = false;

        // clear mask
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < ROIMaskDepth; z++)
                {
                    ROIMask3D[x, y, z] = false;
                }
            }
        }

        yield return null;

        GameObject globalTool = activeROIToolsGlobal[currentActiveROIGlobal];
        Vector3 globalPos = globalTool.transform.localPosition + new Vector3(0.5f, 0.5f, 0.5f);
        Vector3 globalScale = globalTool.transform.localScale;

        // the bottom left front (P1) point and the top right back point (P2) of the global (external) ROI
        Vector3 ROI_P1 = globalPos - globalScale * 0.5f;
        Vector3 ROI_P2 = globalPos + globalScale * 0.5f;

        List<bool[,]> TempROIMask = new List<bool[,]>();


        for (int t = 0; t < activeROITools.Count; t++)
        {
            GameObject tool = activeROITools[t];

            if (ROIViewSelectedROI)
            {
                startZ = (ROI_P2.z - ROI_P1.z) * (tool.transform.localPosition.z + tool.transform.localScale.z / 2.0f) + globalTool.transform.localPosition.z;
                endZ = (ROI_P2.z - ROI_P1.z) * (tool.transform.localPosition.z - tool.transform.localScale.z / 2.0f) + globalTool.transform.localPosition.z;
            }
            else
            {
                startZ = tool.transform.localPosition.z + tool.transform.localScale.z / 2.0f;
                endZ = tool.transform.localPosition.z - tool.transform.localScale.z / 2.0f;
            }
            //DEPTH mapping = -0.5 -> 0.5 => 0 -> 99. Thus 50 = the middle (or 0)

            if (tool.CompareTag("ROIBox"))
            {
                // the internal points (or if globally viewed, they are the same as  P1 and P2)
                Vector3 ROI_P3;
                Vector3 ROI_P4;
                if (ROIViewSelectedROI)
                {
                    // Temporary intermediate variables
                    Vector3 P3inP12 = tool.transform.localPosition + new Vector3(0.5f, 0.5f, 0.5f) - tool.transform.localScale * 0.5f;
                    Vector3 P4inP12 = tool.transform.localPosition + new Vector3(0.5f, 0.5f, 0.5f) + tool.transform.localScale * 0.5f;

                    ROI_P3 = Vector3.Scale(P3inP12, (ROI_P2 - ROI_P1)) + ROI_P1;
                    ROI_P4 = Vector3.Scale(P4inP12, (ROI_P2 - ROI_P1)) + ROI_P1;
                }
                else
                {
                    Vector3 pos = tool.transform.localPosition + new Vector3(0.5f, 0.5f, 0.5f);
                    Vector3 scale = tool.transform.localScale;

                    // the bottom left front (P1) point and the top right back point (P2) of the global (external) ROI
                    ROI_P3 = pos - scale * 0.5f;
                    ROI_P4 = pos + scale * 0.5f;
                }

                for (int row = 0; row < size; row++)
                {
                    for (int col = 0; col < size; col++)
                    {
                        for (int z = 0; z < ROIMaskDepth; z++)
                        {
                            float depthFraction = z / (float)ROIMaskDepth - 0.5f;
                            //if((row > (pos.x - scale.x/2f + 0.5f)*size && row < (pos.x + scale.x/2f + 0.5f)*size) && (col > (pos.y - scale.y/2f + 0.5f)*size && col < (pos.y + scale.y/2f + 0.5f)*size))
                            if ((endZ <= depthFraction && depthFraction <= startZ) && (row > (ROI_P3.x) * size && row < (ROI_P4.x) * size) && (col > (ROI_P3.y) * size && col < (ROI_P4.y) * size))
                            {
                                if (ROISubtractiveSelect)
                                {
                                    ROIMask3D[row, col, z] = !ROIMask3D[row, col, z];
                                }
                                else
                                {
                                    ROIMask3D[row, col, z] = true;
                                }
                            }
                        }
                    }

                    if (row % 50 == 0)
                    {
                        yield return null;
                    }
                }
            }
            else if (tool.CompareTag("ROICylinder"))
            {
                Vector3 center;
                float a, b;

                if (ROIViewSelectedROI)
                {
                    // Temporary intermediate variables
                    Vector3 centerInP12 = tool.transform.localPosition + new Vector3(0.5f, 0.5f, 0.5f);
                    center = (Vector3.Scale(centerInP12, (ROI_P2 - ROI_P1)) + ROI_P1) * size;

                    a = ((ROI_P2.x - ROI_P1.x) * tool.transform.localScale.x / 2.0f) * size;
                    b = ((ROI_P2.y - ROI_P1.y) * tool.transform.localScale.y / 2.0f) * size;

                }
                else
                {
                    center = (tool.transform.localPosition + new Vector3(0.5f, 0.5f, 0.5f)) * size;
                    //float radius = (currentROITool.transform.localScale.x/2.0f) * size;

                    a = (tool.transform.localScale.x / 2.0f) * size;
                    b = (tool.transform.localScale.y / 2.0f) * size;
                }

                float h = center.x;
                float k = center.y;
                float a2 = a * a;
                float b2 = b * b;
                float A = -Mathf.Deg2Rad * tool.transform.localRotation.eulerAngles.z;

                for (int row = 0; row < size; row++)
                {
                    for (int col = 0; col < size; col++)
                    {
                        for (int z = 0; z < ROIMaskDepth; z++)
                        {
                            float depthFraction = z / (float)ROIMaskDepth - 0.5f;
                            //ROIMask[row,col] = ((row - center.x)*(row - center.x) + (col - center.y)*(col - center.y)<(radius*radius));
                            // unrotated form
                            //if ((endZ <= depthFraction && depthFraction <= startZ) && ((row - center.x) * (row - center.x) / (a * a) + (col - center.y) * (col - center.y) / (b * b) < 1f))
                            float x = row;
                            float y = col;
                            bool condition = false;
                            
                            if(Math.Abs(A * Mathf.Rad2Deg) <= 1f)
                            {
                                condition = (endZ <= depthFraction && depthFraction <= startZ) && ((x - h) * (x - h) / (a2) + (y - k) * (y - k) / (b2) < 1f);
                            }
                            else
                            {
                                condition = (endZ <= depthFraction && depthFraction <= startZ) &&
                                ((((x - h) * Mathf.Cos(A) - (y - k) * Mathf.Sin(A)) * ((x - h) * Mathf.Cos(A) - (y - k) * Mathf.Sin(A))) / (a * a) +
                                (((x - h) * Mathf.Sin(A) + (y - k) * Mathf.Cos(A)) * ((x - h) * Mathf.Sin(A) + (y - k) * Mathf.Cos(A))) / (b * b) < 1f);
                            }

                            //https://math.stackexchange.com/questions/426150/what-is-the-general-equation-of-the-ellipse-that-is-not-in-the-origin-and-rotate
                            if (condition)
                            {
                                if (ROISubtractiveSelect)
                                {
                                    ROIMask3D[row, col, z] = !ROIMask3D[row, col, z];
                                }
                                else
                                {
                                    ROIMask3D[row, col, z] = true;
                                }
                            }
                        }
                    }

                    if (row % 50 == 0)
                    {
                        yield return null;
                    }
                }
            }
            else if (tool.CompareTag("ROISphere"))
            {
                Vector3 center;
                float a, b, c; //Corrospondence: a=x-axis, b=y-axis, c=z-axis

                if (ROIViewSelectedROI)
                {
                    // Temporary intermediate variables
                    Vector3 centerInP12 = tool.transform.localPosition + new Vector3(0.5f, 0.5f, 0.5f);
                    center = (Vector3.Scale(centerInP12, (ROI_P2 - ROI_P1)) + ROI_P1) * size;

                    a = ((ROI_P2.x - ROI_P1.x) * tool.transform.localScale.x / 2.0f) * size;
                    b = ((ROI_P2.y - ROI_P1.y) * tool.transform.localScale.y / 2.0f) * size;
                    c = ((ROI_P2.z - ROI_P1.z) * tool.transform.localScale.z / 2.0f) * ROIMaskDepth;
                }
                else
                {
                    center = (tool.transform.localPosition + new Vector3(0.5f, 0.5f, 0.5f)) * size;
                    //float radius = (currentROITool.transform.localScale.x/2.0f) * size;

                    a = (tool.transform.localScale.x / 2.0f) * size;
                    b = (tool.transform.localScale.y / 2.0f) * size;
                    c = (tool.transform.localScale.z / 2.0f) * ROIMaskDepth;
                }

                float h = center.x;
                float k = center.y;
                float i = center.z / size * ROIMaskDepth;
                float a2 = a * a;
                float b2 = b * b;
                float c2 = c * c;

                float A = -Mathf.Deg2Rad * tool.transform.localRotation.eulerAngles.z;

                for (int row = 0; row < size; row++)
                {
                    for (int col = 0; col < size; col++)
                    {
                        for (int z = 0; z < ROIMaskDepth; z++)
                        {
                            //float depthFraction = z / (float)ROIMaskDepth - 0.5f;
                            //ROIMask[row,col] = ((row - center.x)*(row - center.x) + (col - center.y)*(col - center.y)<(radius*radius));
                            //if (((row - center.x) * (row - center.x) / (a * a) + (col - center.y) * (col - center.y) / (b * b) + (z - center.z / size * ROIMaskDepth) * (z - center.z / size * ROIMaskDepth) / (c * c) < 1f))
                            float x = row;
                            float y = col;
                            bool condition = false;
                            
                            if (Math.Abs(A * Mathf.Rad2Deg) <= 1f)
                            {
                                condition = ((x - h) * (x - h) / (a2) + (y - k) * (y - k) / (b2) + (z - i) * (z - i) / (c2) < 1f);
                            }
                            else
                            {
                                condition = ((((x - h) * Mathf.Cos(A) - (y - k) * Mathf.Sin(A)) * ((x - h) * Mathf.Cos(A) - (y - k) * Mathf.Sin(A))) / a2 +
                                (((x - h) * Mathf.Sin(A) + (y - k) * Mathf.Cos(A)) * ((x - h) * Mathf.Sin(A) + (y - k) * Mathf.Cos(A))) / b2 +
                                (z - i) * (z - i) / c2 < 1f);
                            }


                            if (condition)
                            {
                                if (ROISubtractiveSelect)
                                {
                                    ROIMask3D[row, col, z] = !ROIMask3D[row, col, z];
                                }
                                else
                                {
                                    ROIMask3D[row, col, z] = true;
                                }
                            }
                        }
                    }

                    if (row % 50 == 0)
                    {
                        yield return null;
                    }
                }
            }
            else if (tool.CompareTag("ROIFreehand"))
            {
                List<Vector3> freehandPoly = tool.GetComponent<FreehandPolygon>().polygon;
                TempROIMask.Add(new bool[size, size]);
                int tempMaskCount = TempROIMask.Count - 1;

                if (ROIViewSelectedROI)
                {
                    Vector3 P3inP12;
                    Vector3 P4inP12;
                    Vector3 ROI_P3;
                    Vector3 ROI_P4;

                    // determine which pixels lie within the freehand curve
                    for (int row = 0; row < size; row++)
                    {
                        for (int col = 0; col < size; col++)
                        {
                            TempROIMask[tempMaskCount][col, row] = false;
                            // http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html
                            for (int i = 0, j = freehandPoly.Count - 1; i < freehandPoly.Count; j = i++)
                            {
                                // Temporary intermediate variables
                                P3inP12 = freehandPoly[i] + new Vector3(0.5f, 0.5f, 0.5f);
                                P4inP12 = freehandPoly[j] + new Vector3(0.5f, 0.5f, 0.5f);

                                ROI_P3 = Vector3.Scale(P3inP12, (ROI_P2 - ROI_P1)) + ROI_P1; // i
                                ROI_P4 = Vector3.Scale(P4inP12, (ROI_P2 - ROI_P1)) + ROI_P1; // j

                                if ((ROI_P3.y > (float)row / size) != (ROI_P4.y > (float)row / size) &&
                                    (float)col / size < (ROI_P4.x - ROI_P3.x) * ((float)row / size - ROI_P3.y) / (ROI_P4.y - ROI_P3.y) + ROI_P3.x)
                                {
                                    TempROIMask[tempMaskCount][col, row] = !TempROIMask[tempMaskCount][col, row];
                                }
                            }
                        }

                        if (row % 10 == 0)
                        {
                            yield return null;
                        }
                    }
                }
                else
                {
                    // determine which pixels lie within the freehand curve
                    for (int row = 0; row < size; row++)
                    {
                        for (int col = 0; col < size; col++)
                        {
                            TempROIMask[tempMaskCount][col, row] = false;
                            // http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html
                            for (int i = 0, j = freehandPoly.Count - 1; i < freehandPoly.Count; j = i++)
                            {
                                if ((freehandPoly[i].y > (float)row / size - 0.5f) != (freehandPoly[j].y > (float)row / size - 0.5f) &&
                                    (float)col / size - 0.5f < (freehandPoly[j].x - freehandPoly[i].x) * ((float)row / size - 0.5f - freehandPoly[i].y) / (freehandPoly[j].y - freehandPoly[i].y) + freehandPoly[i].x)
                                {
                                    TempROIMask[tempMaskCount][col, row] = !TempROIMask[tempMaskCount][col, row];
                                }
                            }
                        }

                        if (row % 10 == 0)
                        {
                            yield return null;
                        }
                    }
                }

                for (int row = 0; row < size; row++)
                {
                    for (int col = 0; col < size; col++)
                    {
                        // calculate the depth masks
                        for (int z = 0; z < ROIMaskDepth; z++)
                        {
                            float depthFraction = z / (float)ROIMaskDepth - 0.5f;

                            if (endZ <= depthFraction && depthFraction <= startZ && TempROIMask[tempMaskCount][col, row])
                            {
                                if (ROISubtractiveSelect)
                                {
                                    ROIMask3D[col, row, z] = !ROIMask3D[col, row, z];
                                }
                                else
                                {
                                    ROIMask3D[col, row, z] = true;
                                }
                            }
                        }
                    }

                    if (row % 50 == 0)
                    {
                        yield return null;
                    }
                }
            }
        }
        /*
        // combine all the freehand tools' masks
        for (int mask = 0; mask < TempROIMask.Count; mask++)
        {
            //Debug.Log("Processing Freehand mask: " + mask);

            for (int row = 0; row < size; row++)
            {
                for (int col = 0; col < size; col++)
                {

                    if (TempROIMask[mask][col, row])
                    {
                        if (ROISubtractiveSelect)
                        {
                            if (ROIMask_XY[col, row])
                                ROIMask_XY[col, row] = false;
                            else
                                ROIMask_XY[col, row] = true;
                        }
                        else
                        {
                            ROIMask_XY[col, row] = true;
                        }

                    }
                }
            }
        }
        */
        if (!ROIMaskTextureBeingCalculated)
            yield return StartCoroutine(ROIMaskToTexture());

        Debug.Log("Finished calculating boolean masks");

        updateMaskRunning = false;
    }
    /*
IEnumerator updateROIMaskOLD2D_SHOULD_NOT_USE()
{
    updateMaskRunning = true;
    maskShouldUpdate = false;

    // clear mask
    for(int row = 0; row < size; row++)
    {
        for(int col = 0; col < size; col++)	
        {
            ROIMask_XY[row,col] = false;
        }

        for(int col = 0; col < MaskZDepth; col++)
        {
            ROIMask_XZ[row, col] = false;
            ROIMask_YZ[row, col] = false;
        }
    }

    yield return null;

    GameObject globalTool = activeROIToolsGlobal[currentActiveROIGlobal];
    Vector3 globalPos = globalTool.transform.localPosition + new Vector3(0.5f, 0.5f, 0.5f);
    Vector3 globalScale = globalTool.transform.localScale;

    // the bottom left front (P1) point and the top right back point (P2) of the global (external) ROI
    Vector3 ROI_P1 = globalPos - globalScale*0.5f;
    Vector3 ROI_P2 = globalPos + globalScale*0.5f;

    List<bool[,]> TempROIMask = new List<bool[,]>();


    for(int t = 0; t < activeROITools.Count; t++)
    {
        GameObject tool = activeROITools[t];

        if (ROIViewSelectedROI)
        {
            startZ = (ROI_P2.z - ROI_P1.z) * (tool.transform.localPosition.z + tool.transform.localScale.z / 2.0f) + globalTool.transform.localPosition.z;
            endZ = (ROI_P2.z - ROI_P1.z) * (tool.transform.localPosition.z - tool.transform.localScale.z / 2.0f) + globalTool.transform.localPosition.z;
        }
        else
        {
            startZ = tool.transform.localPosition.z + tool.transform.localScale.z / 2.0f;
            endZ = tool.transform.localPosition.z - tool.transform.localScale.z / 2.0f;
        }
        //DEPTH mapping = -0.5 -> 0.5 => 0 -> 99. Thus 50 = the middle (or 0)

        if (tool.CompareTag("ROIBox"))
        {
            // the internal points (or if globally viewed, they are the same as  P1 and P2)
            Vector3 ROI_P3;
            Vector3 ROI_P4;
            if(ROIViewSelectedROI)
            {
                // Temporary intermediate variables
                Vector3 P3inP12 = tool.transform.localPosition + new Vector3(0.5f, 0.5f, 0.5f) - tool.transform.localScale*0.5f;
                Vector3 P4inP12 = tool.transform.localPosition + new Vector3(0.5f, 0.5f, 0.5f) + tool.transform.localScale*0.5f;

                ROI_P3 = Vector3.Scale(P3inP12, (ROI_P2 - ROI_P1)) + ROI_P1;
                ROI_P4 = Vector3.Scale(P4inP12, (ROI_P2 - ROI_P1)) + ROI_P1;
            }
            else
            {
                Vector3 pos = tool.transform.localPosition + new Vector3(0.5f, 0.5f, 0.5f);
                Vector3 scale = tool.transform.localScale;

                // the bottom left front (P1) point and the top right back point (P2) of the global (external) ROI
                ROI_P3 = pos - scale*0.5f;
                ROI_P4 = pos + scale*0.5f;
            }

            for(int row = 0; row < size; row++)
            {
                for(int col = 0; col < size; col++)	
                {
                    //if((row > (pos.x - scale.x/2f + 0.5f)*size && row < (pos.x + scale.x/2f + 0.5f)*size) && (col > (pos.y - scale.y/2f + 0.5f)*size && col < (pos.y + scale.y/2f + 0.5f)*size))
                    if ((row > (ROI_P3.x) * size && row < (ROI_P4.x) * size) && (col > (ROI_P3.y) * size && col < (ROI_P4.y) * size))
                    {
                        if (ROISubtractiveSelect)
                        {
                            if (ROIMask_XY[row, col])
                                ROIMask_XY[row, col] = false;
                            else
                                ROIMask_XY[row, col] = true;
                        }
                        else
                        {
                            ROIMask_XY[row, col] = true;
                        }

                        // calculate the depth masks
                        for(int z = 0; z < 100; z++)
                        {
                            bool value_XZ = false;
                            bool value_YZ = false;
                            float depthFraction = z / 100.0f - 0.5f;
                            if(((endZ <= depthFraction && depthFraction <= startZ))) // && ROIisInside) || ((endZ >= depthFraction || depthFraction <= startZ) && ROIisInside))
                            {
                                value_XZ = true;
                                value_YZ = true;
                            }
                            else
                            {
                                value_XZ = ROIMask_XZ[row, z];
                                value_YZ = ROIMask_YZ[col, z];
                            }

                            ROIMask_XZ[row, z] = value_XZ;
                            ROIMask_YZ[col, z] = value_YZ;
                        }
                    }
                }

                if(row % 50 == 0)
                {
                    yield return null;
                }
            }
        }
        else if(tool.CompareTag("ROICylinder"))
        {
            Vector3 center;
            float a,b;

            if(ROIViewSelectedROI)
            {
                // Temporary intermediate variables
                Vector3 centerInP12 = tool.transform.localPosition + new Vector3(0.5f, 0.5f, 0.5f);
                center = (Vector3.Scale(centerInP12, (ROI_P2 - ROI_P1)) + ROI_P1) * size;

                a = ((ROI_P2.x - ROI_P1.x) * tool.transform.localScale.x/2.0f) * size;
                b = ((ROI_P2.y - ROI_P1.y) * tool.transform.localScale.y/2.0f) * size;

            }
            else
            {
                center = (tool.transform.localPosition + new Vector3(0.5f, 0.5f, 0.5f)) * size;
                //float radius = (currentROITool.transform.localScale.x/2.0f) * size;

                a = (tool.transform.localScale.x/2.0f) * size;
                b = (tool.transform.localScale.y/2.0f) * size;
            }

            for(int row = 0; row < size; row++)
            {
                for(int col = 0; col < size; col++)	
                {
                    //ROIMask[row,col] = ((row - center.x)*(row - center.x) + (col - center.y)*(col - center.y)<(radius*radius));
                    if (((row - center.x) * (row - center.x) / (a * a) + (col - center.y) * (col - center.y) / (b * b) < 1f))
                    {
                        if (ROISubtractiveSelect)
                        {
                            if (ROIMask_XY[row, col])
                                ROIMask_XY[row, col] = false;
                            else
                                ROIMask_XY[row, col] = true;
                        }
                        else
                        {
                            ROIMask_XY[row, col] = true;
                        }


                        // calculate the depth masks
                        for (int z = 0; z < 100; z++)
                        {
                            float depthFraction = z / 100.0f - 0.5f;

                            if (((endZ <= depthFraction && depthFraction <= startZ)))
                            {
                                ROIMask_XZ[row, z] = true;
                                ROIMask_YZ[col, z] = true;
                            }
                        }
                    }
                }

                if(row % 50 == 0)
                {
                    yield return null;
                }
            }
        }
        else if (tool.CompareTag("ROISphere"))
        {
           Vector3 center;
            float a,b,c; //Corrospondence: a=x-axis, b=y-axis, c=z-axis

            if(ROIViewSelectedROI)
            {
                // Temporary intermediate variables
                Vector3 centerInP12 = tool.transform.localPosition + new Vector3(0.5f, 0.5f, 0.5f);
                center = (Vector3.Scale(centerInP12, (ROI_P2 - ROI_P1)) + ROI_P1) * size;

                a = ((ROI_P2.x - ROI_P1.x) * tool.transform.localScale.x/2.0f) * size;
                b = ((ROI_P2.y - ROI_P1.y) * tool.transform.localScale.y/2.0f) * size;
                c = ((ROI_P2.z - ROI_P1.z) * tool.transform.localScale.z/2.0f) * MaskZDepth;
            }
            else
            {
                center = (tool.transform.localPosition + new Vector3(0.5f, 0.5f, 0.5f)) * size;
                //float radius = (currentROITool.transform.localScale.x/2.0f) * size;

                a = (tool.transform.localScale.x/2.0f) * size;
                b = (tool.transform.localScale.y/2.0f) * size;
                c = (tool.transform.localScale.z / 2.0f) * MaskZDepth;
            }

            for(int row = 0; row < size; row++)
            {
                for(int col = 0; col < size; col++)	
                {
                    //ROIMask[row,col] = ((row - center.x)*(row - center.x) + (col - center.y)*(col - center.y)<(radius*radius));
                    if (((row - center.x) * (row - center.x) / (a * a) + (col - center.y) * (col - center.y) / (b * b) < 1f))
                    {
                        if (ROISubtractiveSelect)
                        {
                            if (ROIMask_XY[row, col])
                                ROIMask_XY[row, col] = false;
                            else
                                ROIMask_XY[row, col] = true;
                        }
                        else
                        {
                            ROIMask_XY[row, col] = true;
                        }


                        // calculate the depth masks
                        for (int z = 0; z < 100; z++)
                        {
                            float depthFraction = z / 100.0f - 0.5f;

                            if (((z - center.z/size*MaskZDepth) * (z - center.z / size * MaskZDepth) / (c * c) + (col - center.y) * (col - center.y) / (b * b) < 1f))
                            {                                    
                                ROIMask_YZ[col, z] = true;
                            }

                            if (((row - center.x) * (row - center.x) / (a * a) + (z - center.z / size * MaskZDepth) * (z - center.z / size * MaskZDepth) / (c * c) < 1f))
                            {
                                ROIMask_XZ[row, z] = true;
                            }
                        }
                    }
                }

                if(row % 50 == 0)
                {
                    yield return null;
                }
            }
        }
        else if(tool.CompareTag("ROIFreehand"))
        {
            List<Vector3> freehandPoly = tool.GetComponent<FreehandPolygon>().polygon;
            TempROIMask.Add(new bool[size,size]);
            int tempMaskCount = TempROIMask.Count - 1;

            if(ROIViewSelectedROI)
            {
                Vector3 P3inP12;
                Vector3 P4inP12;
                Vector3 ROI_P3;
                Vector3 ROI_P4;

                // determine which pixels lie within the freehand curve
                for(int row = 0; row < size; row++)
                {
                    for(int col = 0; col < size; col++)	
                    {
                        TempROIMask[tempMaskCount] [col, row] = false;
                        // http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html
                        for ( int i = 0, j = freehandPoly.Count - 1 ; i < freehandPoly.Count ; j = i++ )
                        {
                            // Temporary intermediate variables
                            P3inP12 = freehandPoly[ i ] + new Vector3(0.5f, 0.5f, 0.5f);
                            P4inP12 = freehandPoly[ j ] + new Vector3(0.5f, 0.5f, 0.5f);

                            ROI_P3 = Vector3.Scale(P3inP12, (ROI_P2 - ROI_P1)) + ROI_P1; // i
                            ROI_P4 = Vector3.Scale(P4inP12, (ROI_P2 - ROI_P1)) + ROI_P1; // j

                            if ( ( ROI_P3.y > (float)row/size ) != ( ROI_P4.y > (float)row/size ) &&
                                (float)col/size  < ( ROI_P4.x - ROI_P3.x ) * ( (float)row/size - ROI_P3.y ) / ( ROI_P4.y - ROI_P3.y ) + ROI_P3.x )
                            {
                                TempROIMask[tempMaskCount] [col, row] = !TempROIMask[tempMaskCount][col, row];
                            }
                        }
                    }

                    if(row % 10 == 0)
                    {
                        yield return null;
                    }
                }	
            }
            else
            {
                // determine which pixels lie within the freehand curve
                for(int row = 0; row < size; row++)
                {
                    for(int col = 0; col < size; col++)	
                    {
                        TempROIMask[tempMaskCount] [col, row] = false;
                        // http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html
                        for ( int i = 0, j = freehandPoly.Count - 1 ; i < freehandPoly.Count ; j = i++ )
                        {
                            if ( ( freehandPoly[ i ].y > (float)row/size - 0.5f ) != ( freehandPoly[ j ].y > (float)row/size - 0.5f ) &&
                                (float)col/size - 0.5f < ( freehandPoly[ j ].x - freehandPoly[ i ].x ) * ( (float)row/size - 0.5f - freehandPoly[ i ].y ) / ( freehandPoly[ j ].y - freehandPoly[ i ].y ) + freehandPoly[ i ].x )
                            {
                                TempROIMask[tempMaskCount] [col, row] = !TempROIMask[tempMaskCount][col, row];
                            }
                        }
                    }

                    if(row % 10 == 0)
                    {
                        yield return null;
                    }
                }	
            }

            for (int row = 0; row < size; row++)
            {
                for (int col = 0; col < size; col++)
                {
                    // calculate the depth masks
                    for (int z = 0; z < 100; z++)
                    {
                        float depthFraction = z / 100.0f - 0.5f;

                        if (endZ <= depthFraction && depthFraction <= startZ && TempROIMask[tempMaskCount][col, row])
                        {
                            ROIMask_XZ[col, z] = true;
                            ROIMask_YZ[row, z] = true;
                        }                            
                    }
                }

                if (row % 50 == 0)
                {
                    yield return null;
                }
            }
        }
    }

    // combine all the freehand tools' masks
    for(int mask = 0; mask < TempROIMask.Count; mask++)
    {
        //Debug.Log("Processing Freehand mask: " + mask);

        for(int row = 0; row < size; row++)
        {
            for(int col = 0; col < size; col++)	
            {
                if (TempROIMask[mask][col, row])
                {
                    if (ROISubtractiveSelect)
                    {
                        if (ROIMask_XY[col, row])
                            ROIMask_XY[col, row] = false;
                        else
                            ROIMask_XY[col, row] = true;
                    }
                    else
                    {
                        ROIMask_XY[col, row] = true;
                    }

                }
            }
        }
    }
    if (!ROIMaskTextureBeingCalculated)
        yield return StartCoroutine(ROIMaskToTexture());


    updateMaskRunning = false;
}
*/
    #region ROIMenu
    public bool ROImaskTest3D(int maskCol, int maskRow, float depthFraction)
    {
        // two cases (inside ROI or outside ROI selection). Then also take the start and end z positions into account

        int depthToZ = (int)((depthFraction + 0.5f) * ROIMaskDepth);

        // just a verification stepup
        if (depthToZ >= ROIMaskDepth)
            depthToZ--;


        return ((ROIMask3D[maskCol, maskRow, depthToZ] && ROIisInside) || (!ROIMask3D[maskCol, maskRow, depthToZ] && !ROIisInside));
    }

    public bool ROImaskTest2D(int maskCol, int maskRow)
    {
        // two cases (inside ROI or outside ROI selection). Then also take the start and end z positions into account

        int depthToZ = (int)((currentSlice / (float)loadSample.texDepth) * ROIMaskDepth);

        // just a verification step
        if (depthToZ >= ROIMaskDepth)
            depthToZ--;


        return ((ROIMask3D[maskCol, maskRow, depthToZ] && ROIisInside) || (!ROIMask3D[maskCol, maskRow, depthToZ] && !ROIisInside));
    }

    public void ROI2DMaskFrom3D()
    {
        ROIMask2D = new bool[size, size];

        // start by initializing the mask to all false
        for (int x = 0; x < ROIMask2D.GetLength(0); x++)
            for (int y = 0; y < ROIMask2D.GetLength(1); y++)
                ROIMask2D[x, y] = false;


        for (int x = 0; x < ROIMask3D.GetLength(0); x++)
        {
            for (int y = 0; y < ROIMask3D.GetLength(1); y++)
            {
                for (int z = 0; z < ROIMask3D.GetLength(2); z++)
                {
                    if (ROIMask3D[x, y, z])
                        ROIMask2D[x, y] = true;
                }
            }
        }
                  
    }

    private void FlattenBoxFreehand()
    {
        previousBoxScale = boundingBoxSetup.boxDim;
        Vector2 boundingBoxAspectRatio = new Vector2(previousBoxScale.x, previousBoxScale.y);
        boundingBoxAspectRatio = Vector2.Scale(boundingBoxAspectRatio.normalized, new Vector2(Mathf.Sqrt(2), Mathf.Sqrt(2)));

        try
        {
            // if hand tracking is used
            if (GameObject.FindGameObjectWithTag("HandTrackingCamera").activeInHierarchy)
            {
                Vector3 anchorScale = GameObject.FindGameObjectWithTag("RTS_Anchor").transform.localScale;
                //boundingBoxSetup.transform.localScale = new Vector3(1.0f / anchorScale.x, 1.0f / anchorScale.y, 0.001f / anchorScale.z);// flatten the box // TODO: this is an alternative, where the scale is set to 1
                boundingBox.transform.localScale = new Vector3(boundingBoxAspectRatio.x / anchorScale.x, boundingBoxAspectRatio.y / anchorScale.y, 0.001f / anchorScale.z);// flatten the box // TODO: this is an alternative, where the scale is set to 1
            }
            else
            {
                //boundingBoxSetup.transform.localScale = new Vector3(previousBoxScale.x, previousBoxScale.y, 0.001f);// flatten the box 
                //boundingBoxSetup.transform.localScale = new Vector3(1.0f, 1.0f, 0.001f);// flatten the box // TODO: this is an alternative, where the scale is set to 1

                boundingBox.transform.localScale = new Vector3(boundingBoxAspectRatio.x, boundingBoxAspectRatio.y, 0.001f);// flatten the box // TODO: this is an alternative, where the scale is set to 1	
            }
        }
        catch (NullReferenceException e)
        {
            Debug.Log(e.ToString());
            //boundingBoxSetup.transform.localScale = new Vector3(1.0f, 1.0f, 0.001f);// flatten the box // TODO: this is an alternative, where the scale is set to 1	
            boundingBox.transform.localScale = new Vector3(boundingBoxAspectRatio.x, boundingBoxAspectRatio.y, 0.001f);// flatten the box // TODO: this is an alternative, where the scale is set to 1	
        }
    }

    public void ROIButtonPressed()
    {
        useROI = !useROI;

        if (useROI)
        {
            boundingBox.GetComponent<BoxCollider>().enabled = false;
            currentROITool.GetComponent<BoxCollider>().enabled = true;
            colocMenu.SetActive(false);
            ROIMenu.SetActive(true);
            if (showROIMesh)
                currentROITool.SetActive(true);
            //if(showROIMask)
            //   ROIMaskParent.SetActive (true);

            if (currentROITool.CompareTag("ROIFreehand") && !scaleROIAxis)
            {
                boundingBox.transform.position = new Vector3(0f, 0f, 0f);
                boundingBox.transform.rotation = Quaternion.identity;

                FlattenBoxFreehand();
            }
        }
        else
        {
            boundingBox.GetComponent<BoxCollider>().enabled = true;
            currentROITool.GetComponent<BoxCollider>().enabled = false;

            if (currentROITool.CompareTag("ROIFreehand"))
                boundingBox.transform.localScale = previousBoxScale;

            ROIMenu.SetActive(false);
            colocMenu.SetActive(true);
            //ROIMaskQuad.SetActive (false);
            //currentROITool.SetActive(false);
        }
    }

    public void ROIToolDropdownChanged()
    {
        int toolValue = ROIToolDropdown.GetComponent<Dropdown>().value;

        if (activeROITools.Count == 0)
        {
            Debug.Log("Forcebly returned");
            return;
        }

        currentROITool.SetActive(false);
        ROIClearButton.SetActive(false);
        ROIUndoButton.SetActive(false);
        ROIAddButton.SetActive(true);
        ROIRemoveButton.SetActive(true);
        ROICurrentDropdown.SetActive(true);

        /*
		 * //reactivate all (after freehand deactivated them)
		for(int elements = 0; elements < activeROITools.Count; elements++)
			activeROITools[elements].SetActive(true);
			*/

        ROIFreehand.SetActive(false);

        // save the state of the previous tool
        GameObject prevGO = activeROITools[currentActiveROI];
        Vector3 prevToolPos = prevGO.transform.position;
        Quaternion prevToolRot = prevGO.transform.rotation;
        Vector3 prevToolScale = prevGO.transform.localScale;

        switch (toolValue)
        {
            case (int)ROITools.Box:
                if (activeROITools[currentActiveROI].CompareTag("ROIBox"))
                {
                    OnlyHighlightSelectedROI();
                    return;
                }

                if (activeROITools[currentActiveROI].CompareTag("ROIFreehand"))
                {
                    boundingBox.transform.localScale = previousBoxScale;
                }

                //RemoveFreehandPolygon();
                //			Debug.Log("Box selected" + prevToolScale);

                activeROITools[currentActiveROI] = (GameObject)Instantiate(ROIBox, prevToolPos, prevToolRot);
                activeROITools[currentActiveROI].SetActive(true);
                activeROITools[currentActiveROI].transform.SetParent(prevGO.transform.parent);
                activeROITools[currentActiveROI].SetActive(true);
                activeROITools[currentActiveROI].transform.localScale = prevToolScale;


                break;
            case (int)ROITools.Cylinder:
                if (activeROITools[currentActiveROI].CompareTag("ROICylinder"))
                {
                    OnlyHighlightSelectedROI();
                    return;
                }
                if (activeROITools[currentActiveROI].CompareTag("ROIFreehand"))
                {
                    boundingBox.transform.localScale = previousBoxScale;
                }

                /*
                if(!currentROITool.CompareTag("ROIFreehand"))
                {
                    ROICylinder.transform.localPosition = currentROITool.transform.localPosition;
                    ROICylinder.transform.localScale = currentROITool.transform.localScale;			
                }
                */
                //RemoveFreehandPolygon();

                //Debug.Log("Cylinder selected" + prevToolScale);

                activeROITools[currentActiveROI] = (GameObject)Instantiate(ROICylinder, prevToolPos, prevToolRot);
                activeROITools[currentActiveROI].SetActive(true);
                activeROITools[currentActiveROI].transform.SetParent(prevGO.transform.parent);
                activeROITools[currentActiveROI].SetActive(true);
                activeROITools[currentActiveROI].transform.localScale = prevToolScale;


                break;
            case (int)ROITools.Sphere:
                if (activeROITools[currentActiveROI].CompareTag("ROISphere"))
                {
                    OnlyHighlightSelectedROI();
                    return;
                }

                if (activeROITools[currentActiveROI].CompareTag("ROIFreehand"))
                {
                    boundingBox.transform.localScale = previousBoxScale;
                }

                //RemoveFreehandPolygon();
                //			Debug.Log("Box selected" + prevToolScale);

                activeROITools[currentActiveROI] = (GameObject)Instantiate(ROISphere, prevToolPos, prevToolRot);
                activeROITools[currentActiveROI].SetActive(true);
                activeROITools[currentActiveROI].transform.SetParent(prevGO.transform.parent);
                activeROITools[currentActiveROI].SetActive(true);
                activeROITools[currentActiveROI].transform.localScale = prevToolScale;


                break;
            case (int)ROITools.Freehand:
                if (activeROITools[currentActiveROI].CompareTag("ROIFreehand"))
                {
                    OnlyHighlightSelectedROI();
                    return;
                }

                ROIClearButton.SetActive(true);
                ROIUndoButton.SetActive(true);


                /*
                ROIAddButton.SetActive(false);
                ROIRemoveButton.SetActive(false);
                ROICurrentDropdown.SetActive(false);
                */
                //Debug.Log("Freehand selected");

                activeROITools[currentActiveROI] = (GameObject)Instantiate(ROIFreehand); //ROIFreehand;// Instantiate(ROIFreehand);//, prevToolPos, prevToolRot);
                activeROITools[currentActiveROI].SetActive(true);
                activeROITools[currentActiveROI].transform.SetParent(prevGO.transform.parent);
                activeROITools[currentActiveROI].SetActive(true);
                activeROITools[currentActiveROI].transform.localScale = Vector3.one;

                //AddFreehandPolygon();
                /*
                 * Deactivate all others when using freehand
                ROIFreehand.SetActive(true);
                for(int elements = 0; elements < activeROITools.Count; elements++)
                    activeROITools[elements].SetActive(false);
                    */

                Toggle scaleROIToggle = scaleROIaxisToggle.GetComponent<Toggle>();
                scaleROIToggle.isOn = false;
                ToggleScaleROIAxis(scaleROIToggle);

                FlattenBoxFreehand();


                //currentROITool = ROIFreehand;
                boundingBox.transform.position = new Vector3(0f, 0f, 0f);
                boundingBox.transform.rotation = Quaternion.identity;
                break;
        }

        //if(!prevGO.CompareTag("ROIFreehand"))
        Destroy(prevGO);

        currentROITool = activeROITools[currentActiveROI];
        currentROITool.SetActive(true);


        if (currentROITool.CompareTag("ROIFreehand"))
            currentROITool.transform.localScale = Vector3.one;

        /*
		if(!updateMaskRunning)// && ROIMaskParent.activeInHierarchy)
			StartCoroutine(updateROIMask());
		else
			maskShouldUpdate = true;
            */

        OnlyHighlightSelectedROI();
    }

    /*
	private void AddFreehandPolygon()
	{
		// add new freehandPolygon
		if(!activeROITools[currentActiveROI].CompareTag("ROIFreehand"))
		{
			freehandPolygon.Add(activeROITools[currentActiveROI].GetComponent<FreehandPolygon>());
			currentFreehandPolygon++;

			Debug.Log("Add: Count: " + freehandPolygon.Count + " current: " + currentFreehandPolygon);
		}
	}

	private void RemoveFreehandPolygon()
	{
		if(activeROITools[currentActiveROI].CompareTag("ROIFreehand"))
		{
			freehandPolygon.RemoveAt(currentFreehandPolygon);
			currentFreehandPolygon--;

			Debug.Log("Remove: Count: " + freehandPolygon.Count + " current: " + currentFreehandPolygon);
		}
	}
	*/


    IEnumerator ROIMaskToTexture()
    {
        ROIMaskTextureBeingCalculated = true;
        // create and initialize the 3D texture
        ROI3DMaskTexture = new Texture3D(size, size, ROIMaskDepth, TextureFormat.RGB24, false); //ARGB32
        ROI3DMaskTexture.filterMode = FilterMode.Trilinear;
        ROI3DMaskTexture.wrapMode = TextureWrapMode.Clamp;
        ROI3DMaskTexture.anisoLevel = 0;
        Debug.Log("3D texture Done");

        var colors = new Color[size * size * ROIMaskDepth];
        int idx = 0;
        for (int z = 0; z < ROI3DMaskTexture.depth; z++)
        {
            for (int y = 0; y < ROI3DMaskTexture.height; y++)
            {
                for (int x = 0; x < ROI3DMaskTexture.width; x++, idx++)
                {
                    if (ROIisInside)
                        colors[idx] = (ROIMask3D[x, y, z] ? Color.white : Color.black); //i + (j*dim) + (k*dim*dim)
                    else
                        colors[idx] = (ROIMask3D[x, y, z] ? Color.black : Color.white);

                }
            }
            if (z % 1 == 0)
                yield return null;

            Debug.Log("ROIMaskToTexture z: " + z);
        }
        //yield return null;
        ROI3DMaskTexture.SetPixels(colors);
        ROI3DMaskTexture.Apply();

        internalMat.SetTexture("_ROIMask3D", ROI3DMaskTexture);

        //ROIMaskQuad.SetActive(true);
        yield return null;
        ROI2DMaskFrom3D();

        ROIMaskTextureBeingCalculated = false;
        Debug.Log("Finished converting to texture");
    }


    /*
    IEnumerator ROIMaskToTextureOLD_DONT_USE()
	{
        //XY
		int divFactor = closestPowerOfTwo(2);
		int texSize = size / divFactor;
		ROIMaskTextureBeingCalculated = true;
		Texture2D texture = new Texture2D(texSize, texSize);
		texture.wrapMode = TextureWrapMode.Clamp;

		ROIMaskQuadXY.GetComponent<Renderer>().material.mainTexture = texture;
		//ROIMaskQuad.SetActive(false);

		var colors = new Color[texSize * texSize];
		int idx = 0;
		for (int col = 0; col < texture.width; col++) {			
			for (int row = 0; row < texture.height; row++, idx++) {
				if(ROIisInside)
					colors[idx] = (ROIMask_XY[row*divFactor, col*divFactor] ? Color.white : Color.black);
				else
					colors[idx] = (ROIMask_XY[row*divFactor, col*divFactor] ? Color.black : Color.white);
			}
			//yield return null;
		}
		//yield return null;
		texture.SetPixels(colors);
		texture.Apply();

        internalMat.SetTexture("_ROI_XY", texture);

        // Encode texture into PNG
        byte[] bytes = texture.EncodeToPNG();
        string filePath = Application.persistentDataPath + "/Results/ROIMasks/ROIMask_XY_" + Time.time + ".png";
        //string filePath = "I:/Master's results/Results/ROIMasks/ROIMask_" + Time.time + ".png";
        File.WriteAllBytes(filePath, bytes);
        Debug.Log("Wrote current ROI mask to " + filePath);

        //ROIMaskQuad.SetActive(true);
        yield return null;



        //XZ
        texture = new Texture2D(texSize, MaskZDepth);
        texture.wrapMode = TextureWrapMode.Clamp;

        ROIMaskQuadXZ.GetComponent<Renderer>().material.mainTexture = texture;
        //ROIMaskQuad.SetActive(false);

        colors = new Color[texSize * MaskZDepth];
        idx = 0;
        for (int col = 0; col < texture.height; col++)
        {
            for (int row = 0; row < texture.width; row++, idx++)
            {
                if (ROIisInside)
                    colors[idx] = (ROIMask_XZ[row * divFactor, col] ? Color.white : Color.black);
                else
                    colors[idx] = (ROIMask_XZ[row * divFactor, col] ? Color.black : Color.white);
            }
            //yield return null;
        }
        //yield return null;
        texture.SetPixels(colors);
        texture.Apply();

        internalMat.SetTexture("_ROI_XZ", texture);

        // Encode texture into PNG
        bytes = texture.EncodeToPNG();
        filePath = Application.persistentDataPath + "/Results/ROIMasks/ROIMask_XZ_" + Time.time + ".png";
        //string filePath = "I:/Master's results/Results/ROIMasks/ROIMask_" + Time.time + ".png";
        File.WriteAllBytes(filePath, bytes);
        Debug.Log("Wrote current ROI mask to " + filePath);

        yield return null;


        //YZ
        texture = new Texture2D(texSize, MaskZDepth);
        texture.wrapMode = TextureWrapMode.Clamp;

        ROIMaskQuadYZ.GetComponent<Renderer>().material.mainTexture = texture;
        //ROIMaskQuad.SetActive(false);

        colors = new Color[texSize * MaskZDepth];
        idx = 0;
        for (int col = 0; col < texture.height; col++)
        {
            for (int row = 0; row < texture.width; row++, idx++)
            {
                if (ROIisInside)
                    colors[idx] = (ROIMask_YZ[row * divFactor, col] ? Color.white : Color.black);
                else
                    colors[idx] = (ROIMask_YZ[row * divFactor, col] ? Color.black : Color.white);
            }
            //yield return null;
        }
        //yield return null;
        texture.SetPixels(colors);
        texture.Apply();

        internalMat.SetTexture("_ROI_YZ", texture);

        // Encode texture into PNG
        bytes = texture.EncodeToPNG();
        filePath = Application.persistentDataPath + "/Results/ROIMasks/ROIMask_YZ_" + Time.time + ".png";
        //string filePath = "I:/Master's results/Results/ROIMasks/ROIMask_" + Time.time + ".png";
        File.WriteAllBytes(filePath, bytes);
        Debug.Log("Wrote current ROI mask to " + filePath);

        ROIMaskTextureBeingCalculated = false;
    }*/

    public void OnRaycasthit(RaycastHit hit)
    {
        if (currentROITool.CompareTag("ROIFreehand"))
        {
            Vector3 boundingboxAspRatio = new Vector3(1f / boundingBox.transform.localScale.x, 1f / boundingBox.transform.localScale.y, 0.0f);

            ROIFreehandHitPos = Vector3.Scale(hit.point, boundingboxAspRatio);
            ROIFreehandHitPos.z = 0f;

            ROIFreehandHitPosUnscaled = hit.point;
            ROIFreehandHitPosUnscaled.z = 0f;
            //string s = String.Format("x: {0:0.000}  y: {1:0.000}   z = {2:0.000}", hit.point.x, hit.point.y, hit.point.z);
            //Debug.Log("FROM VOLUME VIS: " + s);
        }
    }

    public void AddROI()
    {
        int toolValue = ROIToolDropdown.GetComponent<Dropdown>().value;
        //List<string> oldOptions = activeROIDropdown.GetComponent<Dropdown>().options;
        //string addText = "";

        switch (toolValue)
        {
            case (int)ROITools.Box:
                activeROITools.Add(Instantiate(ROIBox));
                //addText = "Box";
                break;
            case (int)ROITools.Cylinder:
                activeROITools.Add(Instantiate(ROICylinder));
                //addText = "Cylinder";
                break;
            case (int)ROITools.Sphere:
                activeROITools.Add(Instantiate(ROISphere));
                //addText = "Box";
                break;
            case (int)ROITools.Freehand:
                if (!activeROITools[currentActiveROI].CompareTag("ROIFreehand"))
                    FlattenBoxFreehand();

                activeROITools.Add(Instantiate(ROIFreehand));
                //addText = "Freehand";
                break;
        }


        activeROITools[activeROITools.Count - 1].transform.SetParent(ROIBox.transform.parent);

        activeROITools[activeROITools.Count - 1].SetActive(true);
        activeROITools[activeROITools.Count - 1].transform.localRotation = Quaternion.identity;
        activeROITools[activeROITools.Count - 1].transform.localScale = currentROITool.transform.localScale;//Vector3.one;

        activeROIDropdown.GetComponent<Dropdown>().options.Add(new Dropdown.OptionData() { text = activeROITools.Count.ToString() });
        activeROIDropdown.GetComponent<Dropdown>().value = activeROITools.Count - 1;

        currentROITool = activeROITools[activeROITools.Count - 1];
        currentActiveROI = activeROITools.Count - 1;
        OnlyHighlightSelectedROI();
    }

    public void RemoveROI()
    {
        bool checkForNewTool = false;
        if (activeROITools[currentActiveROI].CompareTag("ROIFreehand"))
        {
            checkForNewTool = true;
        }

        int ROIValue = activeROIDropdown.GetComponent<Dropdown>().value;
        List<Dropdown.OptionData> oldOptions = activeROIDropdown.GetComponent<Dropdown>().options;
        List<string> newOptions = new List<string>();

        //Debug.Log("ROI to delete = " + ROIValue);
        //Debug.Log("Number of old options = " + oldOptions.Count);

        // make sure tha no ROI is deleted if there is only one
        if (oldOptions.Count <= 1)
        {
            Debug.Log("No ROI deleted, since there is only one");
            return;
        }


        //RemoveFreehandPolygon();

        // update the options of the dropdown menu
        for (int i = 0; i < oldOptions.Count; i++)
        {
            if (i == ROIValue)
            {
                GameObject temp = activeROITools[i];
                activeROITools.RemoveAt(i);
                Destroy(temp);
            }
            else
            {
                //Debug.Log("Added: " + oldOptions[i].text);
                newOptions.Add(oldOptions[i].text);
            }
        }
        activeROIDropdown.GetComponent<Dropdown>().ClearOptions();
        activeROIDropdown.GetComponent<Dropdown>().AddOptions(newOptions);


        // change the current tool value to a valid one
        if (ROIValue != 0)
        {
            currentActiveROI--;
            activeROIDropdown.GetComponent<Dropdown>().value--;
        }

        currentROITool = activeROITools[currentActiveROI];
        currentROITool.SetActive(true);

        /*
		if(!updateMaskRunning)// && ROIMaskParent.activeInHierarchy)
			StartCoroutine(updateROIMask());
		else
			maskShouldUpdate = true;
            */

        if (!activeROITools[currentActiveROI].CompareTag("ROIFreehand") && checkForNewTool)
        {
            boundingBox.transform.localScale = previousBoxScale;
        }

        OnlyHighlightSelectedROI();
    }

    public void ActiveROIDropdownChanged()
    {
        currentActiveROI = activeROIDropdown.GetComponent<Dropdown>().value;
        if (currentActiveROI > activeROITools.Count - 1)
        {
            Debug.Log("Strange things are happening..." + currentActiveROI);
            currentActiveROI = activeROITools.Count - 1;
        }
        currentROITool = activeROITools[currentActiveROI];

        OnlyHighlightSelectedROI();
    }

    public void OnlyHighlightSelectedROI()
    {
        for (int i = 0; i < activeROITools.Count; i++)
        {
            if (i == currentActiveROI)
            {
                activeROITools[i].GetComponentInChildren<MeshRenderer>().material = ROIMeshMaterial;
            }
            else
            {
                activeROITools[i].GetComponentInChildren<MeshRenderer>().material = ROINotSelectedMat;
            }
        }

    }

    public void ViewOnlySelectedROI()
    {
        // can't go into freehand (but can go out of)
        if (activeROITools[currentActiveROI].CompareTag("ROIFreehand") && !ROIViewSelectedROI)
        {
            Debug.Log("Viewing of Freehand not allow");
            return;
        }

        ROIViewSelectedROI = !ROIViewSelectedROI;

        if (ROIViewSelectedROI)
        {
            GameObject tool = activeROITools[currentActiveROI];
            Vector3 pos = tool.transform.localPosition + new Vector3(0.5f, 0.5f, 0.5f);
            Vector3 scale = tool.transform.localScale;

            internalMat.SetVector("_ROI_P1", new Vector4(pos.x - scale.x / 2f, pos.y - scale.y / 2f, pos.z - scale.z / 2f, 0.0f));
            internalMat.SetVector("_ROI_P2", new Vector4(pos.x + scale.x / 2f, pos.y + scale.y / 2f, pos.z + scale.z / 2f, 0.0f));

            prevGlobalBoxScale = boundingBoxSetup.boxDim;
            boundingBox.transform.localScale = new Vector3(scale.normalized.x * initialBoxRatio.x * 3, scale.normalized.y * initialBoxRatio.y * 3, scale.normalized.z * initialBoxRatio.z * 3);

            // swap everything
            activeROIToolsGlobal = activeROITools;
            activeROITools = activeROIToolsInside;
            currentActiveROIGlobal = currentActiveROI;
            currentActiveROI = currentActiveROIInside;


            for (int i = 0; i < activeROIToolsGlobal.Count; i++)
            {
                activeROIToolsGlobal[i].SetActive(false);
            }
            for (int i = 0; i < activeROIToolsInside.Count; i++)
            {
                activeROIToolsInside[i].SetActive(true);
            }

            globalToolSelected = ROIToolDropdown.GetComponent<Dropdown>().value;
            ROIToolDropdown.GetComponent<Dropdown>().value = insideToolSelected;

            List<Dropdown.OptionData> tempOptions = activeROIDropdown.GetComponent<Dropdown>().options;
            activeROIDropdown.GetComponent<Dropdown>().options = otherOptions;
            otherOptions = tempOptions;

            ROIViewSelectedROIButton.GetComponentInChildren<Text>().text = "View Global Sample";

            if (activeROITools.Count == 0)
            {
                Debug.Log("Forcebly added a ROI");
                AddROI();
            }
        }
        else
        {
            internalMat.SetVector("_ROI_P1", new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
            internalMat.SetVector("_ROI_P2", new Vector4(1.0f, 1.0f, 1.0f, 0.0f));

            boundingBox.transform.localScale = new Vector3(initialBoxRatio.x * Mathf.Sqrt(3), initialBoxRatio.y * Mathf.Sqrt(3), initialBoxRatio.z * Mathf.Sqrt(3));
            previousBoxScale = prevGlobalBoxScale;
            //boundingBox.transform.localScale = prevGlobalBoxScale;

            // swap everything
            activeROIToolsInside = activeROITools;
            activeROITools = activeROIToolsGlobal;
            currentActiveROIInside = currentActiveROI;
            currentActiveROI = currentActiveROIGlobal;

            for (int i = 0; i < activeROIToolsGlobal.Count; i++)
            {
                activeROIToolsGlobal[i].SetActive(true);
            }
            for (int i = 0; i < activeROIToolsInside.Count; i++)
            {
                activeROIToolsInside[i].SetActive(false);
            }

            if (activeROITools.Count == 0)
            {
                AddROI();
            }

            insideToolSelected = ROIToolDropdown.GetComponent<Dropdown>().value;
            ROIToolDropdown.GetComponent<Dropdown>().value = globalToolSelected;

            List<Dropdown.OptionData> tempOptions = activeROIDropdown.GetComponent<Dropdown>().options;
            activeROIDropdown.GetComponent<Dropdown>().options = otherOptions;
            otherOptions = tempOptions;

            ROIViewSelectedROIButton.GetComponentInChildren<Text>().text = "View Only Selected ROI";
        }
        try
        {
            currentROITool = activeROITools[currentActiveROI];
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

        OnlyHighlightSelectedROI();
    }

    public void ToggleScaleROIAxis(Toggle toggle)
    {
        scaleROIAxis = toggle.isOn;

        if (toggle.isOn)
        {
            ROIScaleAxisDropdown.SetActive(true);
            ROIscaleAxisDropboxChanged();

            if (currentROITool.CompareTag("ROIFreehand"))
                boundingBox.transform.localScale = previousBoxScale;
        }
        else
        {
            ROIScaleAxisDropdown.SetActive(false);
            ROIscaleAxis = new Vector3(1f, 1f, 1f);

            if (currentROITool.CompareTag("ROIFreehand"))
            {
                //boundingBoxSetup.transform.localScale = new Vector3(previousBoxScale.x, previousBoxScale.y, 0.001f);// flatten the box
                //boundingBoxSetup.transform.localScale = new Vector3(1.0f, 1.0f, 0.001f);// flatten the box
                FlattenBoxFreehand();
                boundingBox.transform.position = new Vector3(0f, 0f, 0f);
                boundingBox.transform.rotation = Quaternion.identity;
            }
        }
    }

    public void ROIscaleAxisDropboxChanged()
    {
        /*
		if(currentROITool.CompareTag("ROIFreehand"))
		{
			boundingBoxSetup.transform.localScale = new Vector3(previousBoxScale.x, previousBoxScale.y, 0.001f);// flatten the box
			boundingBox.transform.position = new Vector3(0f,0f,0f);
			boundingBox.transform.rotation = Quaternion.identity;
		}
		*/

        // cannot scale along x or y for freehand tool
        if (currentROITool.CompareTag("ROIFreehand"))
        {
            ROIScaleAxisDropdown.GetComponent<Dropdown>().value = 2;
        }

        switch (ROIScaleAxisDropdown.GetComponent<Dropdown>().value)
        {
            case 0: // x-axis
                ROIscaleAxis = new Vector3(1f, 0f, 0f);
                break;
            case 1: // y-axis
                ROIscaleAxis = new Vector3(0f, 1f, 0f);
                break;
            case 2: // z-axis
                ROIscaleAxis = new Vector3(0f, 0f, 1f);
                //			boundingBoxSetup.transform.localScale = previousBoxScale;
                break;
        }

    }

    public void ToggleROIInsideOrOutside(Toggle toggle)
    {
        ROIisInside = toggle.isOn;
        if (ROIisInside)
        {
            ROIMeshMaterial.SetColor("_Color", new Color(0.01176f, 0.5686f, 0.898f, 0.07843f));
        }
        else
        {
            ROIMeshMaterial.SetColor("_Color", new Color(0.898f, 0.5686f, 0.01176f, 0.07843f));
        }

        StartCoroutine(ROIMaskToTexture());
    }

    public void ToggleSubtractiveSelect(Toggle toggle)
    {
        ROISubtractiveSelect = toggle.isOn;
        Debug.Log("Subtractive select = " + ROISubtractiveSelect);

        /*
        if (!updateMaskRunning)// && showROIMask)// && ROIMaskParent.activeInHierarchy)
            StartCoroutine(updateROIMask());
        else
            maskShouldUpdate = true;
            */
    }

    //public void ToggleShowROIMask(Toggle toggle)
    //{
    //showROIMask = toggle.isOn;
    /*
    if (toggle.isOn)
        ROIMaskParent.SetActive (true);
    else
        ROIMaskParent.SetActive (false);
        */
    //}

    public void ToggleShowVoxelsOnly(Toggle toggle)
    {
        showROIVoxelsOnly = toggle.isOn;

        if (showROIVoxelsOnly)
        {
            internalMat.SetFloat("_useROIMask", 1.0f);
            //internalMat.SetTexture("_ROIMask3D", ROI3DMaskTexture);
        }
        else
        {
            internalMat.SetFloat("_useROIMask", 0.0f);
        }
        /*
        if (!updateMaskRunning)
            StartCoroutine(updateROIMask());
        else
            maskShouldUpdate = true;
            */
    }

    public void ToggleShowHeatMapGuides(Toggle toggle)
    {
        showHeatMapGuidelines = toggle.isOn;

        ScatterDropdownChanged();
    }

    public void CalculateROIMask()
    {
        if (!updateMaskRunning)
            StartCoroutine(updateROIMask());
        else
            maskShouldUpdate = true;
    }

    public void ToggleShowROIMesh(Toggle toggle)
    {
        showROIMesh = toggle.isOn;
        /*
		if (toggle.isOn)
			currentROITool.SetActive (true);
		else
			currentROITool.SetActive (false);
            */
        for (int i = 0; i < activeROITools.Count; i++)
        {
            activeROITools[i].SetActive(showROIMesh);
        }
    }

    public void ClearFreehandDrawing()
    {
        for (int i = 0; i < ROIFreehandPath.Count; i++)
        {
            Destroy(ROIFreehandPath[i]);
        }
        ROIFreehandPath.Clear();

        ROIFreehandVertices.Clear();
        ROIFreehandTriangles.Clear();

        activeROITools[currentActiveROI].GetComponent<FreehandPolygon>().Clear();
        //freehandPolygon[currentFreehandPolygon].Clear();

        MeshFilter mf = currentROITool.GetComponent<MeshFilter>();
        Mesh freehandMesh = mf.mesh;
        freehandMesh.Clear();
    }

    public void UndoFreehandPoint()
    {
        Destroy(ROIFreehandPath[ROIFreehandPath.Count - 1]);
        ROIFreehandPath.RemoveAt(ROIFreehandPath.Count - 1);

        ROIFreehandVertices.RemoveAt(ROIFreehandVertices.Count - 1);
        ROIFreehandVertices.RemoveAt(ROIFreehandVertices.Count - 1);

        // this is used to calculate the mask
        activeROITools[currentActiveROI].GetComponent<FreehandPolygon>().RemoveAt(activeROITools[currentActiveROI].GetComponent<FreehandPolygon>().Count - 1);
        //freehandPolygon[currentFreehandPolygon].RemoveAt(freehandPolygon.Count - 1);

        // top face triangles (note two vertices are added per point)
        ROIFreehandTriangles.RemoveAt(ROIFreehandTriangles.Count - 1);
        ROIFreehandTriangles.RemoveAt(ROIFreehandTriangles.Count - 1);
        ROIFreehandTriangles.RemoveAt(ROIFreehandTriangles.Count - 1);
        ROIFreehandTriangles.RemoveAt(ROIFreehandTriangles.Count - 1);
        ROIFreehandTriangles.RemoveAt(ROIFreehandTriangles.Count - 1);
        ROIFreehandTriangles.RemoveAt(ROIFreehandTriangles.Count - 1);
        ROIFreehandTriangles.RemoveAt(ROIFreehandTriangles.Count - 1);
        ROIFreehandTriangles.RemoveAt(ROIFreehandTriangles.Count - 1);
        ROIFreehandTriangles.RemoveAt(ROIFreehandTriangles.Count - 1);
        ROIFreehandTriangles.RemoveAt(ROIFreehandTriangles.Count - 1);
        ROIFreehandTriangles.RemoveAt(ROIFreehandTriangles.Count - 1);
        ROIFreehandTriangles.RemoveAt(ROIFreehandTriangles.Count - 1);


        MeshFilter mf = currentROITool.GetComponent<MeshFilter>();
        Mesh freehandMesh = mf.mesh;

        freehandMesh.SetTriangles(ROIFreehandTriangles, 0);
        freehandMesh.SetVertices(ROIFreehandVertices);

        /*
		if(!updateMaskRunning)// && showROIMask)// && ROIMaskParent.activeInHierarchy)
			StartCoroutine(updateROIMask());
		else
			maskShouldUpdate = true;
            */
    }
    #endregion

    #region scatterCalc

    IEnumerator ColocColorScatterToTexture3D()
    {
        int maskCol = 0;
        int maskRow = 0;
        int maskDepth = 0;
        float depthFraction = 0.5f;  // start from 0.5 since the localscale is 1 for depth, and since depthFraction run from -0.5 to 0.5...

        ColocDispalyScatterToTexture3DBusy = true;
        //Debug.Log("Start");
        int texSize = 256;
        dispScatterColors3D = new Color[texSize, texSize];

      //  int[,] scatterCounts = new int[texSize, texSize];
       // int maxCount = 0;

        for (int i = 0; i < texSize; i++)
        {
            for (int j = 0; j < texSize; j++)
            {
                dispScatterColors3D[i, j] = Color.black;
            }
        }
        yield return null;
        //Debug.Log("After init");

        // extract the color to use
        Color chan0Color = Color.black;
        Color chan1Color = Color.black;
        switch (colocChannel0)
        {
            case 0: // red
                chan0Color = Color.red;
                break;
            case 1: // green
                chan0Color = Color.green;
                break;
            case 2: // blue
                chan0Color = Color.blue;
                break;
            case 3: // purple
                chan0Color = Color.magenta;
                break;
        }
        switch (colocChannel1)
        {
            case 0: // red
                chan1Color = Color.red;
                break;
            case 1: // green
                chan1Color = Color.green;
                break;
            case 2: // blue
                chan1Color = Color.blue;
                break;
            case 3: // purple
                chan1Color = Color.magenta;
                break;
        }


        for (int i = 0; i < channel0Data.Length; i++)
        {
            // TODO: I have to thoroughly test this
            if (ROImaskTest3D(maskCol, maskRow, depthFraction))
            {
                // get the pixel values
                float valChan0 = -1f;
                float valChan1 = -1f;
                switch (colocChannel0)
                {
                    case 0: // red
                        valChan0 = channel0Data[i].r;
                        break;
                    case 1: // green
                        valChan0 = channel0Data[i].g;
                        break;
                    case 2: // blue
                        valChan0 = channel0Data[i].b;
                        break;
                    case 3: // purple
                        valChan0 = channel0Data[i].r;
                        break;
                }
                switch (colocChannel1)
                {
                    case 0: // red
                        valChan1 = channel1Data[i].r;
                        break;
                    case 1: // green
                        valChan1 = channel1Data[i].g;
                        break;
                    case 2: // blue
                        valChan1 = channel1Data[i].b;
                        break;
                    case 3: // purple
                        valChan1 = channel1Data[i].r;
                        break;
                }
                /*
                scatterCounts[(int)(valChan0 * 255), (int)(valChan1 * 255)]++;

                if (scatterCounts[(int)(valChan0 * 255), (int)(valChan1 * 255)] > maxCount)
                    maxCount = scatterCounts[(int)(valChan0 * 255), (int)(valChan1 * 255)];
                    */
                //TODO: test if I don't multiply with the intensity, and lower the 0.1 to for example 0.05
                dispScatterColors3D[(int)(valChan0 * 255), (int)(valChan1 * 255)] += chan0Color * valChan0 * 0.03f + chan1Color * valChan1 * 0.03f;

                if (i % 100000 == 0)
                {
                    float colCalcPercProgress = i / (float)channel0Data.Length;
                    ScatterPlotUpdateButton.GetComponentInChildren<Text>().text = "Updating1 " + (int)(colCalcPercProgress * 100) + "%";
                    yield return null;
                }
            }

            // logic that ensures using the correct indes for the mask
            maskCol++;
            if (maskCol >= size)
            {
                maskCol = 0;
                maskRow++;

                if (maskRow >= size) // use the same mask for all depths
                {
                    maskRow = 0;
                    maskDepth++;
                    depthFraction = -0.5f + (float)maskDepth / loadSample.texDepth;
                }
            }
        }
        yield return null;      /*
        Debug.Log("Max: " + maxCount);
  
        for (int i = 0; i < texSize; i++)
        {
            for (int j = 0; j < texSize; j++)
            {
                dispScatterColors3D[i, j] = (chan0Color * scatterCounts[i, j] / (float)maxCount + chan1Color * scatterCounts[i, j] / (float)maxCount);
            }
        }
        yield return null;*/
        ColocDispalyScatterToTexture3DBusy = false;
        ScatterToTexture(texSize, dispScatterColors3D, (int)ScatterPlotTypes.DisplayColors, "3D");
    }

    IEnumerator ColocFrequencyScatterToTexture3D()
    {
        int maskCol = 0;
        int maskRow = 0;
        int maskDepth = 0;
        float depthFraction = 0.5f;  // start from 0.5 since the localscale is 1 for depth, and since depthFraction run from -0.5 to 0.5...
        ColocFrequencyScatterToTexture3DBusy = true;
        int texSize = 256;
        freqScatterColors3D = new Color[texSize, texSize];
        scatterCountsFreq3D = new int[texSize, texSize];
        int maxCount = 0;


        for (int i = 0; i < texSize; i++)
        {
            for (int j = 0; j < texSize; j++)
            {
                freqScatterColors3D[i, j] = Color.black;
                scatterCountsFreq3D[i, j] = 0;
            }
        }
        yield return null;
        //Debug.Log("After init");


        for (int i = 0; i < channel0Data.Length; i++)
        {
            // TODO: I have to thoroughly test this
            if (ROImaskTest3D(maskCol, maskRow, depthFraction))
            {
                // get the pixel values
                float valChan0 = -1f;
                float valChan1 = -1f;
                switch (colocChannel0)
                {
                    case 0: // red
                        valChan0 = channel0Data[i].r;
                        break;
                    case 1: // green
                        valChan0 = channel0Data[i].g;
                        break;
                    case 2: // blue
                        valChan0 = channel0Data[i].b;
                        break;
                    case 3: // purple
                        valChan0 = channel0Data[i].r;
                        break;
                }
                switch (colocChannel1)
                {
                    case 0: // red
                        valChan1 = channel1Data[i].r;
                        break;
                    case 1: // green
                        valChan1 = channel1Data[i].g;
                        break;
                    case 2: // blue
                        valChan1 = channel1Data[i].b;
                        break;
                    case 3: // purple
                        valChan1 = channel1Data[i].r;
                        break;
                }
                //TODO: this is a noise filtering section... NOt sure if this should be here
                if (valChan0 >= thresholdHighValue[colocChannel0] && valChan1 >= thresholdHighValue[colocChannel1])
                {
                    scatterCountsFreq3D[(int)(valChan0 * 255), (int)(valChan1 * 255)]++;
                }

                if (scatterCountsFreq3D[(int)(valChan0 * 255), (int)(valChan1 * 255)] > maxCount)
                    maxCount = scatterCountsFreq3D[(int)(valChan0 * 255), (int)(valChan1 * 255)];

                if (i % 100000 == 0)
                {
                    float colCalcPercProgress = i / (float)channel0Data.Length;
                    ScatterPlotUpdateButton.GetComponentInChildren<Text>().text = "Updating... " + (int)(colCalcPercProgress * 100) + "%";
                    yield return null;
                }
            }

            // logic that ensures using the correct indes for the mask
            maskCol++;
            if (maskCol >= size)
            {
                maskCol = 0;
                maskRow++;

                if (maskRow >= size) // use the same mask for all depths
                {
                    maskRow = 0;
                    maskDepth++;
                    depthFraction = -0.5f + (float)maskDepth / loadSample.texDepth;
                }
            }
        }

        ExportScatterDataValues(scatterCountsFreq3D, texSize, "3D");



        //Debug.Log("After aloc");
        //Debug.Log(maxCount);
        // TODO: this is not ensure the results aren't skewed... probably not a good idea?
        // if (maxCount > 500)
        //	maxCount = 500;

        for (int i = 0; i < texSize; i++)
        {
            for (int j = 0; j < texSize; j++)
            {
                /*
                int index = (int)((Mathf.Log10(scatterCountsFreq3D[i, j]) / (Mathf.Log10(maxCount)/2)) * 255);//(int)(((Mathf.Log10(scatterCountsFreq3D[i, j]) / Mathf.Log10(maxCount)) / Mathf.Log10(255)) * 255);
                if (index < 0)
                    index = 0;
                else if (index > 255)
                    index = 255;
                freqScatterColors3D[i, j] = CM.viridis[index]; //HeatMapColorLog(scatterCounts[i,j], 0, maxCount); //new Color((float)scatterCounts[i,j]/(float)maxCount, (float)scatterCounts[i,j]/(float)maxCount, (float)scatterCounts[i,j]/(float)maxCount);
                */
                freqScatterColors3D[i, j] = HeatMapColorLog(scatterCountsFreq3D[i, j], 0, maxCount);
            }
        }

        ScatterHeatmapLowLabel.text = "0";
        ScatterHeatmapMidLabel.text = Mathf.Pow(Mathf.Log10(maxCount) / 2, 10).ToString();
        ScatterHeatmapHighLabel.text = maxCount.ToString();
        try
        {
            string path = Application.persistentDataPath + "/Results/Scatter/";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            System.IO.File.WriteAllText(path+ "Frequency3D" + Time.time + ".txt", "3D\nLow = 0\nMid = " + ScatterHeatmapMidLabel.text + "\nHigh = " + maxCount);
        }
        catch(Exception e)
        {
            Debug.LogError(e.Message);
        }

        yield return null;

        ColocFrequencyScatterToTexture3DBusy = false;
        ScatterToTexture(texSize, freqScatterColors3D, (int)ScatterPlotTypes.FrequencyScatter, "3D");

        UpdateDistance();
        mayUpdateDistance = true;

        // this should be the last step in the process
        if(BenProject)
        {
            PersistentData persistentData = GameObject.Find("PersistentData").GetComponent<PersistentData>();

            if (persistentData != null)
            {
                Debug.Log("Done processing cell!");
                UnityEngine.SceneManagement.SceneManager.LoadScene("FileSelection");
            }
        }
    }

    IEnumerator ColocColorScatterToTextureMIP2D(Color[] sliceCh0, Color[] sliceCh1, string label = "Slice")
    {
        int maskCol = 0;
        int maskRow = 0;

        //Debug.Log("Start");
        int texSize = 256;
        dispScatterColors2D = new Color[texSize, texSize];
        for (int i = 0; i < texSize; i++)
        {
            for (int j = 0; j < texSize; j++)
            {
                dispScatterColors2D[i, j] = Color.black;
            }
        }
        yield return null;
        //Debug.Log("After init");

        // extract the color to use
        Color chan0Color = Color.black;
        Color chan1Color = Color.black;
        switch (colocChannel0)
        {
            case 0: // red
                chan0Color = Color.red;
                break;
            case 1: // green
                chan0Color = Color.green;
                break;
            case 2: // blue
                chan0Color = Color.blue;
                break;
            case 3: // purple
                chan0Color = Color.magenta;
                break;
        }
        switch (colocChannel1)
        {
            case 0: // red
                chan1Color = Color.red;
                break;
            case 1: // green
                chan1Color = Color.green;
                break;
            case 2: // blue
                chan1Color = Color.blue;
                break;
            case 3: // purple
                chan1Color = Color.magenta;
                break;
        }


        for (int i = 0; i < sliceCh0.Length; i++)
        {
            // TODO: I have to thoroughly test this
            //if (ROImaskTest2D(maskCol, maskRow))
            if (ROIMask2D[maskCol, maskRow])
            {
                // get the pixel values
                float valChan0 = -1f;
                float valChan1 = -1f;
                switch (colocChannel0)
                {
                    case 0: // red
                        valChan0 = sliceCh0[i].r;
                        break;
                    case 1: // green
                        valChan0 = sliceCh0[i].g;
                        break;
                    case 2: // blue
                        valChan0 = sliceCh0[i].b;
                        break;
                    case 3: // purple
                        valChan0 = sliceCh0[i].r;
                        break;
                }
                switch (colocChannel1)
                {
                    case 0: // red
                        valChan1 = sliceCh1[i].r;
                        break;
                    case 1: // green
                        valChan1 = sliceCh1[i].g;
                        break;
                    case 2: // blue
                        valChan1 = sliceCh1[i].b;
                        break;
                    case 3: // purple
                        valChan1 = sliceCh1[i].r;
                        break;
                }

                //TODO: test if I don't multiply with the intensity, and lower the 0.1 to for example 0.05
                dispScatterColors2D[(int)(valChan0 * 255), (int)(valChan1 * 255)] += chan0Color * valChan0 * 0.1f + chan1Color * valChan1 * 0.1f;

                if (i % 100000 == 0)
                {
                    float colCalcPercProgress = i / (float)channel0Data.Length;
                    ScatterPlotUpdateButton.GetComponentInChildren<Text>().text = "Updating1 " + (int)(colCalcPercProgress * 100) + "%";
                    yield return null;
                }
            }

            maskCol++;
            if (maskCol >= size)
            {
                maskCol = 0;
                maskRow++;

                if (maskRow >= size) // use the same mask for all depths
                {
                    maskRow = 0;
                    Debug.Log("End of " + label + " image reached... this should not occur? (Scatter)");
                }
            }
        }
        yield return null;
        ScatterToTexture(texSize, dispScatterColors2D, (int)ScatterPlotTypes.DisplayColors, label);
    }

    IEnumerator ColocFrequencyScatterToTextureMIP2D(Color[] sliceCh0, Color[] sliceCh1, string label = "Slice")
    {
        int maskCol = 0;
        int maskRow = 0;

        int texSize = 256;
        freqScatterColors2D = new Color[texSize, texSize];
        scatterCountsFreq2D = new int[texSize, texSize];
        int maxCount = 0;

        for (int i = 0; i < texSize; i++)
        {
            for (int j = 0; j < texSize; j++)
            {
                freqScatterColors2D[i, j] = Color.black;
                scatterCountsFreq2D[i, j] = 0;
            }
        }
        yield return null;
        //Debug.Log("After init");

        for (int i = 0; i < sliceCh0.Length; i++)
        {
            // TODO: I have to thoroughly test this
            //if (ROImaskTest2D(maskCol, maskRow))
            if (ROIMask2D[maskCol, maskRow])
            {
                // get the pixel values
                float valChan0 = -1f;
                float valChan1 = -1f;
                switch (colocChannel0)
                {
                    case 0: // red
                        valChan0 = sliceCh0[i].r;
                        break;
                    case 1: // green
                        valChan0 = sliceCh0[i].g;
                        break;
                    case 2: // blue
                        valChan0 = sliceCh0[i].b;
                        break;
                    case 3: // purple
                        valChan0 = sliceCh0[i].r;
                        break;
                }
                switch (colocChannel1)
                {
                    case 0: // red
                        valChan1 = sliceCh1[i].r;
                        break;
                    case 1: // green
                        valChan1 = sliceCh1[i].g;
                        break;
                    case 2: // blue
                        valChan1 = sliceCh1[i].b;
                        break;
                    case 3: // purple
                        valChan1 = sliceCh1[i].r;
                        break;
                }
                //TODO: this is a noise filtering section... NOt sure if this should be here
                if (valChan0 >= thresholdHighValue[colocChannel0] && valChan1 >= thresholdHighValue[colocChannel1])
                    scatterCountsFreq2D[(int)(valChan0 * 255), (int)(valChan1 * 255)]++;

                if (scatterCountsFreq2D[(int)(valChan0 * 255), (int)(valChan1 * 255)] > maxCount)
                    maxCount = scatterCountsFreq2D[(int)(valChan0 * 255), (int)(valChan1 * 255)];

                if (i % 100000 == 0)
                {
                    float colCalcPercProgress = i / (float)channel0Data.Length;
                    ScatterPlotUpdateButton.GetComponentInChildren<Text>().text = "Updating... " + (int)(colCalcPercProgress * 100) + "%";
                    yield return null;
                }
            }

            maskCol++;
            if (maskCol >= size)
            {
                maskCol = 0;
                maskRow++;

                if (maskRow >= size) // use the same mask for all depths
                {
                    maskRow = 0;
                    Debug.Log("End of " + label + " image reached... this should not occur? (Scatter)");
                }
            }
        }
        //Debug.Log("After aloc");
        //Debug.Log(maxCount);
        // this is not ensure the results aren't skewed... probably not a good idea?
        //if (maxCount > 255)
        //   maxCount = 255;

        ExportScatterDataValues(scatterCountsFreq2D, texSize, "MIP");


        for (int i = 0; i < texSize; i++)
        {
            for (int j = 0; j < texSize; j++)
            {
                /*
                int index = (int)(((Mathf.Log10(scatterCounts[i, j]) / Mathf.Log10(maxCount)) / Mathf.Log10(255)) * 255);
                if (index > 255 || index < 0)
                    index = 0;
                freqScatterColors2D[i, j] = CM.viridis[index]; //HeatMapColorLog(scatterCounts[i, j], 0, maxCount); //new Color((float)scatterCounts[i,j]/(float)maxCount, (float)scatterCounts[i,j]/(float)maxCount, (float)scatterCounts[i,j]/(float)maxCount);
                */
                freqScatterColors2D[i, j] = HeatMapColorLog(scatterCountsFreq2D[i, j], 0, maxCount);
            }
        }
        ScatterHeatmapLowLabel.text = "0";
        ScatterHeatmapMidLabel.text = Mathf.Pow(Mathf.Log10(maxCount) / 2, 10).ToString();
        ScatterHeatmapHighLabel.text = maxCount.ToString();
        try
        {
            string path = Application.persistentDataPath + "/Results/Scatter/";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            System.IO.File.WriteAllText(path +"Frequency2D" + label + Time.time + ".txt", "2D " + label + "\nLow = 0\nMid = " + ScatterHeatmapMidLabel.text + "\nHigh = " + maxCount);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }        
        yield return null;
        ScatterToTexture(texSize, freqScatterColors2D, (int)ScatterPlotTypes.FrequencyScatter, label);


        UpdateDistanceMIP(label);
        mayUpdateDistanceMIP = true;
    }

    public void ExportScatterDataValues(int[,] dataset, int texSize, string label = "3D")
    {
        //Write the data to a text file        
        string scatterData = "";
        for (int i = 0; i < texSize; i++)
        {
            for (int j = 0; j < texSize; j++)
            {
                scatterData += dataset[j, i] + ",";
            }
            scatterData += "\n";
        }

        //scatterData += "";

        System.IO.File.WriteAllText(Application.persistentDataPath + "/Results/Scatter/FreqScatterData" + label + Time.time + ".csv", scatterData);
    }

    public void ScatterToTexture(int texSize, Color[,] scatterColors, int scatterType, string label, bool save=true)
    {
        Texture2D scatterTex = new Texture2D(texSize, texSize);
        scatterTex.wrapMode = TextureWrapMode.Clamp;

        //	ScatterPlot.GetComponent<Renderer>().material.mainTexture = freqScatterTex;
        // This code does the dotted lines on the scatter
        var colors = new Color[texSize * texSize];
        int idx = 0;
        float distanceThreshold = internalMat.GetFloat("_distThresh");
        float angle = internalMat.GetFloat("_angle");
        float m_LinRegThroughThresTEMP = m_LinRegThroughThres;
        float c_LinRegThroughThresTEMP = c_LinRegThroughThres;

        Debug.Log("Using on Scatter ("+ label+"): m " + m_LinRegThroughThresTEMP + " c " + c_LinRegThroughThresTEMP);

        Vector2 p_1TEMP = p1;
        Vector2 p_2TEMP = p2;
        Vector2 p_endTEMP = p_end;
        if (label.Contains("2D") || label.Contains("MIP"))
        {
            distanceThreshold = visDistanceThresMIP;
            angle = visAngleMIP;
            m_LinRegThroughThresTEMP = m_LinRegThroughThresMIP;
            c_LinRegThroughThresTEMP = c_LinRegThroughThresMIP;
            p_endTEMP = p_endMIP;
            p_1TEMP = p1MIP;
            p_2TEMP = p2MIP;
        }

        float lineThickness = 1.5f;

        for (int col = 0; col < texSize; col++)
        {
            for (int row = 0; row < texSize; row++, idx++)
            {
                // show distance                
                float dis = (Mathf.Abs((p_2TEMP.y - p_1TEMP.y) * row / (float)texSize - (p_2TEMP.x - p_1TEMP.x) * col / (float)texSize + p_2TEMP.x * p_1TEMP.y - p_2TEMP.y * p_1TEMP.x)) / Mathf.Sqrt((p_2TEMP.y - p_1TEMP.y) * (p_2TEMP.y - p_1TEMP.y) + (p_2TEMP.x - p_1TEMP.x) * (p_2TEMP.x - p_1TEMP.x)) * texSize;

                // max cut off line
                double c_max = p_endTEMP.y - (-1.0 / m_LinRegThroughThresTEMP) * p_endTEMP.x;


                // low and high thresholds threshold
                if ((((row / 5) % 2 == 0 && Mathf.Abs(col / (float)texSize - thresholdLowValue[colocChannel1]) < 0.003f) || ((col / 5) % 2 == 0 && Mathf.Abs(row / (float)texSize - thresholdLowValue[colocChannel0]) < 0.003f)) || (((row / 5) % 2 == 0 && Mathf.Abs(col / (float)texSize - thresholdHighValue[colocChannel1]) < 0.003f) || ((col / 5) % 2 == 0 && Mathf.Abs(row / (float)texSize - thresholdHighValue[colocChannel0]) < 0.003f)))
                    colors[idx] = Color.white - scatterColors[row, col];

                else if (showHeatMapGuidelines)
                {
                    // regression line                
                    /*
                    if (col >= row * m_LinReg + c_LinReg * texSize - 3 && col <= row * m_LinReg + c_LinReg * texSize + 3)
                        colors[idx] = Color.green;
                        */

                    // regression line
                    if (col >= row * m_LinRegThroughThresTEMP + c_LinRegThroughThresTEMP * texSize - lineThickness && col <= row * m_LinRegThroughThresTEMP + c_LinRegThroughThresTEMP * texSize + lineThickness)
                        colors[idx] = Color.red;
                    // max line
                    else if (col >= row * (-1.0 / m_LinRegThroughThresTEMP) + c_max * texSize - lineThickness && col <= row * (-1.0 / m_LinRegThroughThresTEMP) + c_max * texSize + lineThickness)
                        colors[idx] = Color.green;
                    // distance with angle                    
                    else if (dis < distanceThreshold)
                    {                    
                        float angleSubtract = dis * Mathf.Tan(Mathf.Deg2Rad*angle)/255;
                        if (angleSubtract >= angleDistanceReferenceVal)
                            angleSubtract = angleDistanceReferenceVal;
                        colors[idx] = scatterColors[row, col] + new Color(angleDistanceReferenceVal, angleDistanceReferenceVal, angleDistanceReferenceVal) - new Color(angleSubtract, angleSubtract, angleSubtract);
                    }
                    else
                        colors[idx] = scatterColors[row, col];
                }
                else
                    colors[idx] = scatterColors[row, col];                              

            }
            //yield return null;
        }
        //Debug.Log("After new colors");
        scatterTex.SetPixels(colors);
        scatterTex.Apply();


        ScatterPlotUpdateButton.GetComponentInChildren<Text>().text = "Update Scatter Plot";
        ScatterPlot.GetComponent<Renderer>().material.mainTexture = scatterTex;

        string scatterTypeText = "Scatter";
        switch (scatterType)
        {
            case (int)ScatterPlotTypes.FrequencyScatter: // frequency
                freqScatterTex = scatterTex;
                scatterTypeText = "FreqScatter";
                break;
            case (int)ScatterPlotTypes.DisplayColors:
                colorScatterTex = scatterTex;
                scatterTypeText = "ColorScatter";
                break;
        }

        if (save)
        {
            // Encode texture into PNG
            byte[] bytes = scatterTex.EncodeToPNG();
            try
            {
                string path = Application.persistentDataPath + "/Results/Scatter/";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                string filePath = path + scatterTypeText + label + Time.time + ".png";
                if (BenProject)
                {
                    if (label.Contains("2D") || label.Contains("MIP"))
                        filePath = saveLocation2D + thisImageFolderStructure + scatterTypeText + label + Time.time + ".png";
                    else
                        filePath = saveLocation3D + thisImageFolderStructure + scatterTypeText + label + Time.time + ".png";
                }
                //string filePath = "I:/Master's results/Results/Scatter/" + scatterTypeText + Time.time + ".png";
                System.IO.File.WriteAllBytes(filePath, bytes);
                Debug.Log("Wrote scatter " + filePath);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }           

        }
    }

    // 7 segmnet heatmap
    public Color HeatMapColor(int val, int min, int max)
    {
        //float v = (float)(val - min) / (float)(max-min);
        int diff = max - min;
        int diffFraction = diff / 6;
        int[] positions = new int[] { min, diffFraction + min, diffFraction * 2 + min, diffFraction * 3 + min, diffFraction * 4 + min, diffFraction * 5 + min, diffFraction * 6 + min }; //  the positions at which colors change

        // first interval (blue increases)
        if (val > positions[0] && val < positions[1])
        {
            float v = (float)(val - positions[0]) / (float)(positions[1] - positions[0]);
            return new Color(0f, 0f, v);
        }

        // second interval (blue constant, green grows)
        if (val > positions[1] && val < positions[2])
        {
            float v = (float)(val - positions[1]) / (float)(positions[2] - positions[1]);
            return new Color(0f, v, 1f);
        }

        // third interval (blue decrease, green constant)
        if (val > positions[2] && val < positions[3])
        {
            float v = (float)(val - positions[2]) / (float)(positions[3] - positions[2]);
            return new Color(0f, 1f, (1f - v));
        }

        // fourth interval (red increases, green constant)
        if (val > positions[3] && val < positions[4])
        {
            float v = (float)(val - positions[3]) / (float)(positions[4] - positions[3]);
            return new Color(v, 1f, 0f);
        }

        // fifth interval (red constnat, green decrease)
        if (val > positions[4] && val < positions[5])
        {
            float v = (float)(val - positions[4]) / (float)(positions[5] - positions[4]);
            return new Color(1f, (1f - v), 0f);
        }

        // sixth interval (red constnat, blue and green increase)
        if (val > positions[5] && val < positions[6])
        {
            float v = (float)(val - positions[5]) / (float)(positions[6] - positions[5]);
            return new Color(1f, v, v);
        }

        if (val > max)
            return Color.white;

        return Color.black;
    }

    public Color HeatMapColorRainbow(int val, int min, int max)
    {
        //float v = (float)(val - min) / (float)(max-min);
        float diff = max - min;
        float diffFraction = diff / 7.0f;
        float[] positions = new float[] { min, diffFraction + min, diffFraction * 2 + min, diffFraction * 3 + min, diffFraction * 4 + min, diffFraction * 5 + min, diffFraction * 6 + min, diffFraction * 7 + min }; //  the positions at which colors change

        // first interval (blue increases)
        if (val > positions[0] && val < positions[1])
        {
            float v = (float)(val - positions[0]) / (float)(positions[1] - positions[0]);
            return new Color(0f, 0f, v);
        }

        // second interval (blue constant, green grows)
        if (val > positions[1] && val < positions[2])
        {
            float v = (float)(val - positions[1]) / (float)(positions[2] - positions[1]);
            return new Color(0f, v, 1f);
        }

        // third interval (blue decrease, green constant)
        if (val > positions[2] && val < positions[3])
        {
            float v = (float)(val - positions[2]) / (float)(positions[3] - positions[2]);
            return new Color(0f, 1f, (1f - v));
        }

        // fourth interval (red increases, green constant)
        if (val > positions[3] && val < positions[4])
        {
            float v = (float)(val - positions[3]) / (float)(positions[4] - positions[3]);
            return new Color(v, 1f, 0f);
        }

        // fifth interval (red constnat, green decrease)
        if (val > positions[4] && val < positions[5])
        {
            float v = (float)(val - positions[4]) / (float)(positions[5] - positions[4]);
            return new Color(1f, (1f - v), 0f);
        }

        // sixth interval (red constant, blue  increase)
        if (val > positions[5] && val < positions[6])
        {
            float v = (float)(val - positions[5]) / (float)(positions[6] - positions[5]);
            return new Color(1f, 0f, v);
        }

        // sevent interval (red and blue constant, green increase)
        if (val > positions[6] && val < positions[7])
        {
            float v = (float)(val - positions[6]) / (float)(positions[7] - positions[6]);
            return new Color(1f, v, 1f);
        }

        if (val > max)
            return Color.white;

        return Color.black;
    }

    // 7 segmnet heatmap
    public Color HeatMapColorLog(int value, int min, int max)
    {
        //float v = (float)(val - min) / (float)(max-min);
        if (value == 0)
        {
            value = 1;
        }

        float logMax = Mathf.Log10((float)max);
        float logMin = Mathf.Log10((float)(min <= 0 ? 1 : min));
        float logDiff = logMax - logMin;
        float logDiffFraction = logDiff / 7.0f; // divide the total into 6 color fractions
        //Debug.Log("LogMin: " + logMin + "  LogMax: " + logMax + "  LogDiffFraction: " + logDiffFraction);
        //int diff = max - min;
        //int diffFraction = diff / 6; // divide the total into 6 color fractions
        float[] positions = new float[] { logMin, logDiffFraction + logMin, logDiffFraction * 2 + logMin, logDiffFraction * 3 + logMin, logDiffFraction * 4 + logMin, logDiffFraction * 5 + logMin, logDiffFraction * 6 + logMin, logDiffFraction * 7 + logMin }; //  the positions at which colors change

        float val = Mathf.Log10((float)value);

        // first interval (blue increases)
        if (val > positions[0] && val < positions[1])
        {
            float v = (val - positions[0]) / (positions[1] - positions[0]);
            return new Color(0f, 0f, v);
        }

        // second interval (blue constant, green grows)
        if (val > positions[1] && val < positions[2])
        {
            float v = (val - positions[1]) / (positions[2] - positions[1]);
            return new Color(0f, v, 1f);
        }

        // third interval (blue decrease, green constant)
        if (val > positions[2] && val < positions[3])
        {
            float v = (val - positions[2]) / (positions[3] - positions[2]);
            return new Color(0f, 1f, (1f - v));
        }

        // fourth interval (red increases, green constant)
        if (val > positions[3] && val < positions[4])
        {
            float v = (val - positions[3]) / (positions[4] - positions[3]);
            return new Color(v, 1f, 0f);
        }

        // fifth interval (red constnat, green decrease)
        if (val > positions[4] && val < positions[5])
        {
            float v = (val - positions[4]) / (positions[5] - positions[4]);
            return new Color(1f, (1f - v), 0f);
        }

        // sixth interval (red constant, blue  increase)
        if (val > positions[5] && val < positions[6])
        {
            float v = (val - positions[5]) / (positions[6] - positions[5]);
            return new Color(1f, 0f, v);
        }

        // sevent interval (red and blue constant, green increase)
        if (val > positions[6] && val < positions[7])
        {
            float v = (val - positions[6]) / (positions[7] - positions[6]);
            return new Color(1f, v, 1f);
        }

        if (val > logMax)
            return Color.white;

        return Color.black;
    }

    public void UpdateScatterButtonPressed()
    {
        if (!ColocFrequencyScatterToTexture3DBusy)
        {
            StartCoroutine(ColocFrequencyScatterToTexture3D());
        }
        if (!ColocDispalyScatterToTexture3DBusy)
        {
            StartCoroutine(ColocColorScatterToTexture3D());
        }

        switch (ScatterPlotDropbox.GetComponent<Dropdown>().value)
        {
            case (int)ScatterPlotTypes.FrequencyScatter: // frequency
                ScatterPlot.GetComponent<Renderer>().material.mainTexture = freqScatterTex;
                ScatterHeatmapLegend.SetActive(true);
                break;
            case (int)ScatterPlotTypes.DisplayColors:
                ScatterPlot.GetComponent<Renderer>().material.mainTexture = colorScatterTex;
                ScatterHeatmapLegend.SetActive(false);
                break;
        }

        /*
		switch(ScatterPlotDropbox.GetComponent<Dropdown>().value)
		{
		case (int) ScatterPlotTypes.FrequencyScatter: // frequency
			if(!ColocFrequencyScatterToTexture3DBusy)
				ScatterToTexture(256, freqScatterColors, (int)ScatterPlotTypes.FrequencyScatter);
			break;
		case (int) ScatterPlotTypes.DisplayColors:
			if(!ColocDispalyScatterToTexture3DBusy)
				ScatterToTexture(256, dispScatterColors, (int)ScatterPlotTypes.DisplayColors);
			break;
		}

*/


        /*
		if(!ColocDispalyScatterToTexture3DBusy && !ColocFrequencyScatterToTexture3DBusy)
		{
			StartCoroutine (ColocColorScatterToTexture3D());
			StartCoroutine(ColocFrequencyScatterToTexture3D());
		}
		*/
        /*
		int toolValue = ROIToolDropdown.GetComponent<Dropdown>().value;

		switch(toolValue)
		{
		case (int) ScatterPlotTypes.DisplayColors:
			if(!ColocDispalyScatterToTexture3DBusy)
				StartCoroutine (ColocColorScatterToTexture3D());
			break;
		case (int) ScatterPlotTypes.FrequencyScatter:
			if(!ColocFrequencyScatterToTexture3DBusy)
				StartCoroutine(ColocFrequencyScatterToTexture3D());
			break;
		}
		*/
    }

    public void ScatterDropdownChanged()
    {
        int toolValue = ScatterPlotDropbox.GetComponent<Dropdown>().value;

        switch (toolValue)
        {
            case (int)ScatterPlotTypes.FrequencyScatter: // frequency
                ScatterToTexture(256, freqScatterColors3D, (int)ScatterPlotTypes.FrequencyScatter, "3D");
                ScatterPlot.GetComponent<Renderer>().material.mainTexture = freqScatterTex;
                ScatterHeatmapLegend.SetActive(true);
                break;
            case (int)ScatterPlotTypes.DisplayColors:
                ScatterToTexture(256, dispScatterColors3D, (int)ScatterPlotTypes.DisplayColors, "3D");
                ScatterPlot.GetComponent<Renderer>().material.mainTexture = colorScatterTex;
                ScatterHeatmapLegend.SetActive(false);
                break;
        }
        /*
		switch(toolValue)
		{
		case (int) ScatterPlotTypes.DisplayColors:
			ScatterPlot.GetComponent<Renderer>().material.mainTexture = colorScatterTex;
			//ScatterPlotUpdateButton.SetActive(false);
			break;
		case (int) ScatterPlotTypes.FrequencyScatter:
			ScatterPlot.GetComponent<Renderer>().material.mainTexture = freqScatterTex;
			//ScatterPlotUpdateButton.SetActive(true);
			break;
		}
		*/
    }

    public void changeMatProperty(int sliderNum)
    {
        if (currentSliderNum == -1)
        {
            Debug.LogAssertion("No slider currently set, so can't update values");
            return;
        }
        if (showMIP)
        {
            MIPChangeMatProperty(sliderNum);
            //return;
        }

        Slider slider = guiSliders[sliderNum].GetComponent<Slider>();

        //Debug.Log(slider.value);
        string sliderText = slider.GetComponentInChildren<UnityEngine.UI.Text>().text;
        slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0:0.000}", slider.value);


        switch (renderType)
        {
            case (int)VolumeRenderingType.TextureSlicing:
                switch (sliderNum)
                {
                    case (int)TSMaterialProperty.NumSlices: //0
                        num_slices = (int)slider.value;
                        slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0}", slider.value);
                        break;
                    case (int)TSMaterialProperty.FilterThresh: //1
                        setThreshold = slider.value;
                        internalMat.SetFloat("_Threshold", slider.value);
                        break;
                    case (int)TSMaterialProperty.Opacity: //2
                        setOpacity = slider.value;
                        internalMat.SetFloat("_Opacity", slider.value);
                        break;
                    case (int)TSMaterialProperty.RedOpacity: //3
                        internalMat.SetFloat("_redOpacity", slider.value);
                        break;
                    case (int)TSMaterialProperty.GreenOpacity: //4
                        internalMat.SetFloat("_greenOpacity", slider.value);
                        break;
                    case (int)TSMaterialProperty.BlueOpacity: //5
                        internalMat.SetFloat("_blueOpacity", slider.value);
                        break;
                    case (int)TSMaterialProperty.PurpleOpacity: //6
                        internalMat.SetFloat("_purpleOpacity", slider.value);
                        break;
                    case (int)TSMaterialProperty.SliceSelect: //7
                        slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0}", slider.value);
                        SelectSlice((int)slider.value);
                        break;
                    case (int)TSMaterialProperty.zScale: // 8
                        boundingBox.transform.localScale = new Vector3(boundingBox.transform.localScale.x, boundingBox.transform.localScale.y, boundingBox.transform.localScale.x * slider.value);
                        break;
                }
                break;
            case (int)VolumeRenderingType.RayCasting:
            case (int)VolumeRenderingType.RayCastingIso:
                switch (sliderNum)
                {
                    case (int)RCMaterialProperty.NumSamples: //0
                    case (int)RCMaterialProperty.NumSamplesIso: //9
                        internalMat.SetInt("_numSamples", (int)slider.value);

                        Slider mainSamplesSlider = guiSliders[(int)RCMaterialProperty.NumSamples].GetComponent<Slider>();
                        Slider isoSamplesSlider = guiSliders[(int)RCMaterialProperty.NumSamplesIso].GetComponent<Slider>();

                        mainSamplesSlider.value = slider.value;
                        mainSamplesSlider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0}", slider.value);
                        isoSamplesSlider.value = slider.value;
                        isoSamplesSlider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0}", slider.value);

                        break;
                    case (int)RCMaterialProperty.FilterThresh: //1
                        internalMat.SetFloat("_Threshold", slider.value);
                        break;
                    case (int)RCMaterialProperty.Opacity: //2
                        internalMat.SetFloat("_Opacity", slider.value);
                        break;
                    case (int)RCMaterialProperty.RedOpacity: //3
                        internalMat.SetFloat("_redOpacity", slider.value);
                        break;
                    case (int)RCMaterialProperty.GreenOpacity: //4
                        internalMat.SetFloat("_greenOpacity", slider.value);
                        break;
                    case (int)RCMaterialProperty.BlueOpacity: //5
                        internalMat.SetFloat("_blueOpacity", slider.value);
                        break;
                    case (int)RCMaterialProperty.PurpleOpacity: //6
                        internalMat.SetFloat("_purpleOpacity", slider.value);
                        break;
                    case (int)RCMaterialProperty.SliceSelect: //7
                        slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0}", slider.value);
                        SelectSlice((int)slider.value);
                        break;
                    case (int)RCMaterialProperty.zScale: //8
                        boundingBox.transform.localScale = new Vector3(boundingBox.transform.localScale.x, boundingBox.transform.localScale.y, boundingBox.transform.localScale.x * slider.value);
                        break;
                    case (int)RCMaterialProperty.Isovalue:  //10
                        internalMat.SetInt("_isoValueIn", (int)slider.value);
                        slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0}", slider.value);
                        break;
                    case (int)RCMaterialProperty.DeltaValue: //11
                        internalMat.SetFloat("_deltaValue", slider.value);
                        slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0:0.0000}", slider.value);
                        break;
                    
                }
                break;
        }
    }
    #endregion

    #region MIP
    public void setupMIP()
    {
        MIPMatInitialized = true;
        mipMat.SetTexture("_VolumeRed", loadSample.texSeparate[0]);
        mipMat.SetTexture("_VolumeGreen", loadSample.texSeparate[1]);
        mipMat.SetTexture("_VolumeBlue", loadSample.texSeparate[2]);
        mipMat.SetTexture("_VolumePurple", loadSample.texSeparate[3]);

    }

    // the reason why I take slidernum in and not just use currentSliderNum all the way through is because I have two NumSamples sliders, which both give 0
    public void MIPChangeMatProperty(int sliderNum)
    {
        if (currentSliderNum == -1)
        {
            Debug.LogAssertion("No slider currently set, so can't update values (MIP)");
            return;
        }
        Slider slider = guiSliders[currentSliderNum].GetComponent<Slider>();

        //Debug.Log(slider.value);
        string sliderText = slider.GetComponentInChildren<UnityEngine.UI.Text>().text;
        slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0:0.000}", slider.value);

        switch (sliderNum)
        {
            case (int)MIPMaterialProperty.NumSamples: //0
                mipMat.SetInt("_numSamples", (int)slider.value);
                /*
                Slider mainSamplesSlider = guiSliders[(int) MIPMaterialProperty.NumSamples].GetComponent<Slider>();

                mainSamplesSlider.value = slider.value;
                mainSamplesSlider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0}", slider.value);
    */
                break;
            case (int)MIPMaterialProperty.FilterThresh: //1
                mipMat.SetFloat("_Threshold", slider.value);
                break;
            case (int)MIPMaterialProperty.Opacity: //2
                mipMat.SetFloat("_Opacity", slider.value);
                break;
            case (int)MIPMaterialProperty.RedOpacity: //3
                mipMat.SetFloat("_redOpacity", slider.value);
                break;
            case (int)MIPMaterialProperty.GreenOpacity: //4
                mipMat.SetFloat("_greenOpacity", slider.value);
                break;
            case (int)MIPMaterialProperty.BlueOpacity: //5
                mipMat.SetFloat("_blueOpacity", slider.value);
                break;
            case (int)MIPMaterialProperty.PurpleOpacity: //6
                mipMat.SetFloat("_purpleOpacity", slider.value);
                break;
        }
    }

    // takes the full z-stack for a specific colour channel (that is only Red, Green, etc.) and returns the MIP (as a 2D color array) of that stack
    public Color[] CalculateMIPArray(Color[] fullZStack, bool onlyInsideROI = false)
    {
        Color[] MIP = new Color[size * size];

        int row = 0;
        int col = 0;
        int depth = 0;

        int MIPIndex = 0;

        for (int i = 0; i < fullZStack.Length; i++)
        {
            Color c = fullZStack[i];
            if (onlyInsideROI && !ROImaskTest3D(col, row, ((float)depth / loadSample.texDepth) - 0.5f))
            {
                c = Color.black;
            }

            MIP[MIPIndex] = new Color(Mathf.Max(MIP[MIPIndex].r, c.r), Mathf.Max(MIP[MIPIndex].g, c.g), Mathf.Max(MIP[MIPIndex].b, c.b));

            col++;
            if (col >= size)
            {
                col = 0;
                row++;

                if (row >= size) // use the same mask for all depths
                {
                    row = 0;
                    depth++;
                    if (depth >= loadSample.texDepth)
                    {
                        Debug.Log("Reached the end depth in MIP calculation");
                    }
                }
            }


            MIPIndex = col + row * size;

        }

        return MIP;
    }

    /*

    IEnumerator Generate2DSliceWithColoc(bool onlyInsideROI = false, bool export = true)
    {
        // get only a single slice
        Color[] redSlice = new Color[size * size];
        Array.Copy(redPixels2D, size * size * currentSlice, redSlice, 0, size * size);

        Color[] greenSlice = new Color[size * size];
        Array.Copy(greenPixels2D, size * size * currentSlice, greenSlice, 0, size * size);

        Color[] blueSlice = new Color[size * size];
        Array.Copy(bluePixels2D, size * size * currentSlice, blueSlice, 0, size * size);

        Color[] purpleSlice = new Color[size * size];
        Array.Copy(bluePixels2D, size * size * currentSlice, purpleSlice, 0, size * size);

        Debug.Log("Done copying all the slice images from the 3D textures");
        yield return null;

        Color[] Slice2D;

        Debug.Log("coloc array: " + colocalization2DLayer);
        if (colocalization2DLayer != null && colocalization2DLayer.Length == size * size)
        {
            Debug.Log("Using colocalization 2D layer as base");
            Slice2D = colocalization2DLayer;
        }
        else
        {
            Slice2D = new Color[size * size];
        }

        int col = 0, row = 0;
        for(int i = 0; i < redSlice.Length; i++)
        {
            if(onlyInsideROI && !ROImaskTest2D(col, row))
            {
                Slice2D[i] = Color.black;
            }
            // in other words not a colocalizaed pixel
            else if (Slice2D[i] != Color.white)
            {
                Slice2D[i].r = redSlice[i].r + 0.5f * purpleSlice[i].r;
                Slice2D[i].g = greenSlice[i].g;
                Slice2D[i].b = blueSlice[i].b + 0.5f * purpleSlice[i].b;
            }

            col++;
            if (col >= size)
            {
                col = 0;
                row++;

                if (row >= size) // use the same mask for all depths
                {
                    row = 0;
                }
            }
        }

        if(export)
            yield return ExportPixelsAsImage2D(Slice2D, "Slice2DColoc");
        else
        {
            Texture2D image = new Texture2D(size, size);
            image.SetPixels(Slice2D);
            image.Apply();

            yield return null;

            //SliceQuad2D.SetActive(true);
            SliceQuad2D.GetComponent<Renderer>().material.mainTexture = image;
        }
    }*/

    IEnumerator Generate2DSlice(bool onlyInsideROI = false, bool export = true, bool showColoc = true, bool separateChannels = true, int numChannels = 4)
    {
        List<bool> showChannel = new List<bool>();
        showChannel.Add(true); showChannel.Add(true); showChannel.Add(true); showChannel.Add(true);

        Debug.Log("Generating 2D slice at slice: " + currentSlice + " of " + loadSample.texDepth);

        // get only a single slice
        Color[] redSlice = new Color[size * size];
        Array.Copy(redPixels2D, size * size * currentSlice, redSlice, 0, size * size);

        Color[] greenSlice = new Color[size * size];
        Array.Copy(greenPixels2D, size * size * currentSlice, greenSlice, 0, size * size);

        Color[] blueSlice = new Color[size * size];
        Array.Copy(bluePixels2D, size * size * currentSlice, blueSlice, 0, size * size);

        Color[] purpleSlice = new Color[size * size];
        Array.Copy(bluePixels2D, size * size * currentSlice, purpleSlice, 0, size * size);

        Debug.Log("Done copying all the slice images from the 3D textures");
        yield return null;

        for (int ch = 0; ch < numChannels; ch++)
        {
            if (separateChannels)
            {
                showChannel[0] = showChannel[1] = showChannel[2] = showChannel[3] = false;
                showChannel[ch] = true;
            }
            else
            {
                showChannel[0] = showChannel[1] = showChannel[2] = showChannel[3] = true;
                ch = 10; // ensure the loop only executes once
            }

            if (showChannel.Count != 4)
            {
                Debug.LogError("The showChannel list must contain 4 bools, one for each color channel");
            }
            string showChannelsString = "Ch_";
            for (int i = 0; i < showChannel.Count; i++)
            {
                if (showChannel[i])
                {
                    switch (i)
                    {
                        case 0: // red
                            showChannelsString += "red";
                            break;
                        case 1: // green
                            showChannelsString += "green";
                            break;
                        case 2: // blue
                            showChannelsString += "blue";
                            break;
                        case 3: // purple
                            showChannelsString += "purple";
                            break;
                    }
                }
            }

            Color[] Slice2D;
            Debug.Log("coloc array: " + colocalization2DLayerSlice);
            if (showColoc && colocalization2DLayerSlice != null && colocalization2DLayerSlice.Length == size * size)
            {
                Debug.Log("Using colocalization 2D layer as base");
                Slice2D = colocalization2DLayerSlice;
            }
            else if (showColoc)
            {
                Debug.Log("SOMETHING WENT WRONG with the colocalization2DLayerSlice (possibly not initialized... run colocalization calculation)");
                Slice2D = new Color[size * size];
                for (int i = 0; i < Slice2D.Length; i++)
                {
                    Slice2D[i] = Color.black;
                }
            }
            else
            {
                Slice2D = new Color[size * size];
                for (int i = 0; i < Slice2D.Length; i++)
                {
                    Slice2D[i] = Color.black;
                }
            }


            int col = 0, row = 0;
            for (int i = 0; i < Slice2D.Length; i++)
            {
                if (onlyInsideROI && !ROImaskTest2D(col, row))
                {
                    Slice2D[i] = Color.black;
                }
                // in other words not a colocalizaed pixel
                else if (Slice2D[i] != Color.white || !showColoc)
                {
                    Slice2D[i].r = (showChannel[0] ? redSlice[i].r : 0f) + 0.5f * (showChannel[3] ? purpleSlice[i].r : 0f);
                    Slice2D[i].g = (showChannel[1] ? greenSlice[i].g : 0f);
                    Slice2D[i].b = (showChannel[2] ? blueSlice[i].b : 0f) + 0.5f * (showChannel[3] ? purpleSlice[i].b : 0f);
                }

                col++;
                if (col >= size)
                {
                    col = 0;
                    row++;

                    if (row >= size) // use the same mask for all depths
                    {
                        row = 0;
                    }
                }
            }

            if (export)
                yield return ExportPixelsAsImage2D(Slice2D, "Slice2DColoc(" + showChannelsString + ")");
            else
            {
                Texture2D image = new Texture2D(size, size);
                image.SetPixels(Slice2D);
                image.Apply();

                yield return null;

                //SliceQuad2D.SetActive(true);
                SliceQuad2D.GetComponent<Renderer>().material.mainTexture = image;
            }
        }
    }


    IEnumerator Generate2DSliceForQuad(int sliceNum, bool export = false)
    {
        if (sliceNum != currentSlice)
        {
            Debug.Log("Note that the slice to generate is not the same as the current slice");
        }
        // get only a single slice
        Color[] redSlice = new Color[size * size];
        Array.Copy(redPixels2D, size * size * sliceNum, redSlice, 0, size * size);

        Color[] greenSlice = new Color[size * size];
        Array.Copy(greenPixels2D, size * size * sliceNum, greenSlice, 0, size * size);

        Color[] blueSlice = new Color[size * size];
        Array.Copy(bluePixels2D, size * size * sliceNum, blueSlice, 0, size * size);

        Color[] purpleSlice = new Color[size * size];
        Array.Copy(bluePixels2D, size * size * sliceNum, purpleSlice, 0, size * size);

        //Debug.Log("Done copying all the slice images from the 3D textures (Generate2DSlice)");
        yield return null;
        Color[] Slice2D = new Color[size * size];

        for (int i = 0; i < redSlice.Length; i++)
        {
            Slice2D[i].r = redSlice[i].r + 0.5f * purpleSlice[i].r;
            Slice2D[i].g = greenSlice[i].g;
            Slice2D[i].b = blueSlice[i].b + 0.5f * purpleSlice[i].b;
        }

        if (export)
            yield return ExportPixelsAsImage2D(Slice2D, "Slice2D");
        else
        {
            Texture2D image = new Texture2D(size, size);
            image.SetPixels(Slice2D);
            image.Apply();

            yield return null;

            //SliceQuad2D.SetActive(true);
            SliceQuad2D.GetComponent<Renderer>().material.mainTexture = image;
        }
    }
    /*
    // takes the full z-stack for a specific colour channel (that is only Red, Green, etc.) and returns the MIP (as a 2D color array) of that stack
    IEnumerator GenerateCompleteMIPWithColoc(bool onlyInsideROI = false, bool export = true)
    {
        Color[] MIP;
        Debug.Log("coloc array: " + colocalization2DLayer);
        if (colocalization2DLayer != null && colocalization2DLayer.Length == size*size)
        {
            Debug.Log("Using colocalization 2D layer as base");
            MIP = colocalization2DLayer;
        }
        else
        {
            MIP = new Color[size * size];
        }

        int row = 0;
        int col = 0;
        int depth = 0;

        int MIPIndex = 0;

        for (int i = 0; i < redPixels2D.Length; i++)
        {
            Color c = new Color(redPixels2D[i].r + 0.5f * purplePixels2D[i].r, greenPixels2D[i].g, bluePixels2D[i].b + 0.5f * purplePixels2D[i].r);
            if (onlyInsideROI && !ROImaskTest3D(col, row, ((float)depth / loadSample.texDepth) - 0.5f))
            {
                c = Color.black;
            }
            MIP[MIPIndex] = new Color(Mathf.Max(MIP[MIPIndex].r, c.r), Mathf.Max(MIP[MIPIndex].g, c.g), Mathf.Max(MIP[MIPIndex].b, c.b));


            // I need this method, since I'm "ignoring" depth, and cycle over each image with the same coordinates in the MIP
            col++;
            if (col >= size)
            {
                col = 0;
                row++;

                if (row >= size) // use the same mask for all depths
                {
                    row = 0;
                    depth++;
                    if (depth >= loadSample.texDepth)
                    {
                        Debug.Log("Reached the end depth in MIP calculation");
                    }
                }
            }

            MIPIndex = col + row * size;

            if(i % 1000000 == 0)
            {
                Debug.Log(string.Format("MIP generation {0:0.0}% complete!", (float)i / redPixels2D.Length * 100));
                yield return null;
            }
        }

        if (export)
        {
            yield return ExportPixelsAsImage2D(MIP, "RawMIP");
        }
        else
        {
            Texture2D image = new Texture2D(size, size);
            image.SetPixels(MIP);
            image.Apply();

            yield return null;

            //SliceQuad2D.SetActive(true);
            SliceQuad2D.GetComponent<Renderer>().material.mainTexture = image;
        }

    }
    */
    /*
    // takes the full z-stack for a specific colour channel (that is only Red, Green, etc.) and returns the MIP (as a 2D color array) of that stack
    IEnumerator GenerateCompleteMIP(bool onlyInsideROI = false, bool export = true, bool showColoc = true, bool separateChannels = true, int numChannels = 4, bool showHeatMap = true, bool showMIPColors = false)
    {
        List<bool> showChannel = new List<bool>();
        showChannel.Add(true); showChannel.Add(true); showChannel.Add(true); showChannel.Add(true);

        Debug.Log("coloc array: " + colocalization2DLayerMIP);
        float maxFloat = (float)maxHeatmapValue / thresholdMaxInt;

        if (loadSample == null)
        {
            Debug.Log("For some reason loadSample is null...");
            yield break;
        }

        for (int ch = 0; ch < numChannels; ch++)
        {
            Color[] MIP;
            Color[] onlyColocMIP = new Color[size * size];

            if (separateChannels)
            {
                showChannel[0] = showChannel[1] = showChannel[2] = showChannel[3] = false;
                showChannel[ch] = true;
            }
            else
            {
                showChannel[0] = showChannel[1] = showChannel[2] = showChannel[3] = true;
                ch = 10; // ensure the loop only executes once
            }

            if (showChannel.Count != 4)
            {
                Debug.LogError("The showChannel list must contain 4 bools, one for each color channel");
            }
            string showChannelsString = "Ch_";
            for (int i = 0; i < showChannel.Count; i++)
            {
                if (showChannel[i])
                {
                    switch (i)
                    {
                        case 0: // red
                            showChannelsString += "red";
                            break;
                        case 1: // green
                            showChannelsString += "green";
                            break;
                        case 2: // blue
                            showChannelsString += "blue";
                            break;
                        case 3: // purple
                            showChannelsString += "purple";
                            break;
                    }
                }
            }

            if ((showColoc && !showHeatMap) && colocalization2DLayerMIP != null && colocalization2DLayerMIP.Length == size * size)
            {
                Debug.Log("Using colocalization 2D layer as base");
                MIP = new Color[size * size];
                Array.Copy(colocalization2DLayerMIP, MIP, size * size);
            }
            else if ((showColoc && !showHeatMap))
            {
                Debug.Log("SOMETHING WENT WRONG with the colocalization2DLayerMIP (possibly not initialized... run colocalization calculation)");
                MIP = new Color[size * size];
            }
            else
            {
                Debug.Log("Created new MIP array");
                MIP = new Color[size * size];
            }

            int row = 0;
            int col = 0;
            int depth = 0;

            int MIPIndex = 0;

            for (int i = 0; i < redPixels2D.Length; i++)
            {
                Color c = new Color((showChannel[0] ? redPixels2D[i].r : 0f) + 0.5f * (showChannel[3] ? purplePixels2D[i].r : 0f), (showChannel[1] ? greenPixels2D[i].g : 0f), (showChannel[2] ? bluePixels2D[i].b : 0f) + 0.5f * (showChannel[3] ? purplePixels2D[i].r : 0f));
                if (onlyInsideROI && !ROImaskTest3D(col, row, ((float)depth / loadSample.texDepth) - 0.5f))
                {
                    c = Color.black;
                    MIP[MIPIndex] = c;
                }
                else if (MIP[MIPIndex] != Color.white || !showColoc)
                {
                    MIP[MIPIndex] = new Color((showChannel[0] || showChannel[3] ? Mathf.Max(MIP[MIPIndex].r, c.r) : 0f), (showChannel[1] ? Mathf.Max(MIP[MIPIndex].g, c.g) : 0f), (showChannel[2] || showChannel[3] ? Mathf.Max(MIP[MIPIndex].b, c.b) : 0f));
                }
                if (!showMIPColors && showHeatMap)
                    onlyColocMIP[MIPIndex] = Color.black;
                else if (showColoc && !showMIPColors && !showHeatMap)
                    onlyColocMIP[MIPIndex] = colocalization2DLayerMIP[MIPIndex];




                // I need this method, since I'm "ignoring" depth, and cycle over each image with the same coordinates in the MIP
                col++;
                if (col >= size)
                {
                    col = 0;
                    row++;

                    if (row >= size) // use the same mask for all depths
                    {
                        row = 0;
                        depth++;
                        if (depth >= loadSample.texDepth)
                        {
                            Debug.Log("Reached the end depth in MIP calculation (" + showChannelsString + ")");
                        }
                    }
                }

                MIPIndex = col + row * size;

                if (i % 1000000 == 0)
                {
                    Debug.Log(string.Format("MIP generation {0:0.0}% complete! (" + showChannelsString + ")", (float)i / redPixels2D.Length * 100));
                    yield return null;
                }
            }

            if (showHeatMap && showColoc)
            {
                for (int i = 0; i < MIP.Length; i++)
                {
                    if (colocalization2DLayerMIP[i].r > 0.5f)
                    {

                        if (showMIPColors)
                            MIP[i] = getGradientColocAverageHM(MIP[i].r, MIP[i].g, maxFloat);
                        else
                            onlyColocMIP[i] = getGradientColocAverageHM(MIP[i].r, MIP[i].g, maxFloat);
                    }
                }
            }

            if (export)
            {
                if (showMIPColors)
                    yield return ExportPixelsAsImage2D(MIP, "RawMIP(" + showChannelsString + ")");
                else
                    yield return ExportPixelsAsImage2D(onlyColocMIP, "OnlyColocMIP(" + showChannelsString + ")");
            }
            else
            {
                Texture2D image = new Texture2D(size, size);
                if (showMIPColors)
                    image.SetPixels(MIP);
                else
                    image.SetPixels(onlyColocMIP);
                image.Apply();

                yield return null;

                //SliceQuad2D.SetActive(true);
                SliceQuad2D.GetComponent<Renderer>().material.mainTexture = image;
            }
        }
    }*/


    // takes the full z-stack for a specific colour channel (that is only Red, Green, etc.) and returns the MIP (as a 2D color array) of that stack
    IEnumerator GenerateCompleteMIP_v2(bool onlyInsideROI = false)
    {
        float maxFloat = internalMat.GetFloat("_maxValue"); //(float)maxHeatmapValue / thresholdMaxInt;
        float c_max = p_endMIP[1] + (1 / m_LinRegThroughThresMIP) * p_endMIP[0];
        float x_max = (c_LinRegThroughThresMIP - c_max) / (-1 / m_LinRegThroughThresMIP - m_LinRegThroughThresMIP);
        Debug.Log("x_max MIP = " + x_max + "  c_max MIP = " + c_max);

        if (loadSample == null)
        {
            Debug.Log("For some reason loadSample is null...");
            yield break;
        }


        Color[] MIP = new Color[size * size];
        Color[] MIPpurple = new Color[size * size];
        for (int i = 0; i < MIP.Length; i++)
        {
            MIP[i] = Color.black;
            MIPpurple[i] = Color.black;
        }

        Color[] onlyColocMIP = new Color[size * size];

        int row = 0;
        int col = 0;
        int depth = 0;

        int successCount = 0;

        int MIPIndex = 0;
        // extract full MIP (without coloc)
        for (int i = 0; i < redPixels2D.Length; i++)
        {
            // NOTE: Currently the MIP does not include the purple channel, so exporting MIP_ALL does not include the purple
            //Color c = new Color(redPixels2D[i].r + purplePixels2D[i].r, greenPixels2D[i].g, bluePixels2D[i].b + purplePixels2D[i].r);
            Color c = new Color(redPixels2D[i].r, greenPixels2D[i].g, bluePixels2D[i].b);
            Color cPurp = new Color(purplePixels2D[i].r, 0, purplePixels2D[i].r);
            if (onlyInsideROI && !ROImaskTest3D(col, row, ((float)depth / loadSample.texDepth) - 0.5f ))
            {
                //c = Color.black;
                //MIP[MIPIndex] = c;
            }
            else
            {
                
                MIP[MIPIndex] = new Color(Mathf.Max(MIP[MIPIndex].r, c.r), Mathf.Max(MIP[MIPIndex].g, c.g), Mathf.Max(MIP[MIPIndex].b, c.b));
                MIPpurple[MIPIndex] = new Color(Mathf.Max(MIPpurple[MIPIndex].r, cPurp.r), 0f, Mathf.Max(MIPpurple[MIPIndex].b, cPurp.b));
                successCount++;
            }


            // I need this method, since I'm "ignoring" depth, and cycle over each image with the same coordinates in the MIP
            col++;
            if (col >= size)
            {
                col = 0;
                row++;

                if (row >= size) // use the same mask for all depths
                {
                    row = 0;
                    depth++;
                    if (depth >= loadSample.texDepth)
                    {
                        Debug.Log("Reached the end depth in MIP(v2) calculation");
                    }
                }
            }

            MIPIndex = col + row * size;

            if (i % 1000000 == 0)
            {
                Debug.Log(string.Format("MIP generation {0:0.0}% complete", (float)i / redPixels2D.Length * 100));
                yield return null;
            }
        }

        Debug.Log("Sucess Count: " + successCount);

        Color[] redCh = new Color[size * size];
        Color[] greenCh = new Color[size * size];
        Color[] blueCh = new Color[size * size];
        Color[] purpleCh = new Color[size * size];
        for (int i = 0; i < MIP.Length; i++)
        {
            redCh[i] = Color.black;
            redCh[i].r = MIP[i].r;
            greenCh[i] = Color.black;
            greenCh[i].g = MIP[i].g;
            blueCh[i] = Color.black;
            blueCh[i].b = MIP[i].b;
            purpleCh[i] = Color.black;
            purpleCh[i].r = MIPpurple[i].r;
            purpleCh[i].b = MIPpurple[i].b;
        }

        switch (colocChannel0)
        {
            case 0: // red
                yield return ExportPixelsAsImage2D(redCh, "MIP_RED");
                break;
            case 1: // green
                yield return ExportPixelsAsImage2D(greenCh, "MIP_GREEN");
                break;
            case 2: // blue
                yield return ExportPixelsAsImage2D(blueCh, "MIP_BLUE");
                break;
            case 3: // purple
                yield return ExportPixelsAsImage2D(purpleCh, "MIP_PURPLE");
                break;
        }
        switch (colocChannel1)
        {
            case 0: // red
                yield return ExportPixelsAsImage2D(redCh, "MIP_RED");
                break;
            case 1: // green
                yield return ExportPixelsAsImage2D(greenCh, "MIP_GREEN");
                break;
            case 2: // blue
                yield return ExportPixelsAsImage2D(blueCh, "MIP_BLUE");
                break;
            case 3: // purple
                yield return ExportPixelsAsImage2D(purpleCh, "MIP_PURPLE");
                break;
        }        
        
        yield return ExportPixelsAsImage2D(MIP, "MIP_ALL");

        if (colocalization2DLayerMIP != null && colocalization2DLayerMIP.Length == size * size)
        {
            yield return ExportPixelsAsImage2D(colocalization2DLayerMIP, "MIP_ONLY_COLOC");

            //MIP[MIPIndex] = colocalization2DLayerMIP[MIPIndex];
            /*
            Color[] heatMap = new Color[size * size];
            Color[] gradientWhite = new Color[size * size];
            for (int i = 0; i < MIP.Length; i++)
            {
                heatMap[i] = Color.black;
                gradientWhite[i] = Color.black;
                if (MIP[i] == Color.black) // this is for ROI purposes
                {
                    heatMap[i] = Color.black;
                    gradientWhite[i] = Color.black;
                }
                else if (colocalization2DLayerMIP[i].r > 0.5f)
                {
                    heatMap[i] = getGradientColocAverageHM(MIP[i].r, MIP[i].g, maxFloat);
                    gradientWhite[i] = getGradientColocAverageWhite(MIP[i].r, MIP[i].g, maxFloat);
                }
            }
            //yield return ExportPixelsAsImage2D(gradientWhite, "MIP_WHITE_GRAD");
            yield return ExportPixelsAsImage2D(heatMap, "MIP_HEATMAP");
            */


            Color[] heatMapNew = new Color[size * size];
            Color[] heatMapNewBin = new Color[size * size];
            Color[] heatMapNMDP = new Color[size * size];
            Color[] heatMapNMDPSobel = new Color[size * size];
            Color[] heatMapNMDPSobelBin = new Color[size * size];
            Color[] colocOverlay = new Color[size * size];

            float _angle = internalMat.GetFloat("_angle");
            float _distThresh = visDistanceThresMIP;

            /*
            float _ch0Average = internalMat.GetFloat("_ch0AverageAboveThres");
            float _ch1Average = internalMat.GetFloat("_ch1AverageAboveThres");
            float _ch0Max = internalMat.GetFloat("_ch0Max");
            float _ch1Max = internalMat.GetFloat("_ch1Max");

            if (_ch0Average + _ch1Average + _ch0Max + _ch1Max > 4)
                Debug.LogError("I need to divide by 255");
                */

            float _nmdp_minMultFactor = internalMat.GetFloat("_nmdp_minMultFactor") / 255.0f;
            float _nmdp_maxMultFactor = internalMat.GetFloat("_nmdp_maxMultFactor") / 255.0f;

            for (int i = 0; i < MIP.Length; i++)
            {
                float valChan0 = -1f;
                float valChan1 = -1f;
                switch (colocChannel0)
                {
                    case 0: // red
                        valChan0 = MIP[i].r;
                        break;
                    case 1: // green
                        valChan0 = MIP[i].g;
                        break;
                    case 2: // blue
                        valChan0 = MIP[i].b;
                        break;
                    case 3: // purple
                        valChan0 = MIPpurple[i].r;
                        break;
                }
                switch (colocChannel1)
                {
                    case 0: // red
                        valChan1 = MIP[i].r;
                        break;
                    case 1: // green
                        valChan1 = MIP[i].g;
                        break;
                    case 2: // blue
                        valChan1 = MIP[i].b;
                        break;
                    case 3: // purple
                        valChan1 = MIPpurple[i].r;
                        break;
                }


                heatMapNew[i] = Color.black;
                heatMapNewBin[i] = Color.black;
                heatMapNMDP[i] = Color.black;
                heatMapNMDPSobel[i] = Color.black;
                heatMapNMDPSobelBin[i] = Color.black;
                colocOverlay[i] = MIP[i];

                if (MIP[i] == Color.black) // this is for ROI purposes
                {
                    heatMapNew[i] = Color.black;
                    heatMapNewBin[i] = Color.black;
                    heatMapNMDP[i] = Color.black;
                    heatMapNMDPSobel[i] = Color.black;
                    heatMapNMDPSobelBin[i] = Color.black;
                    colocOverlay[i] = Color.black;
                }
                else if (colocalization2DLayerMIP[i].r > 0.5f)
                {
                    heatMapNew[i] = getGradientColocAverageHeatmapNew(valChan0, valChan1, maxFloat, _angle, _distThresh, p1MIP, p2MIP, x_max);
                    if (heatMapNew[i] != Color.black)
                        heatMapNewBin[i] = Color.white;

                    heatMapNMDP[i] = getNMDP(valChan0, valChan1, 1.0f, (float)xMean_MIP, (float)yMean_MIP, (float)chan0Max_MIP, (float)chan1Max_MIP, _nmdp_minMultFactor, _nmdp_maxMultFactor, true);

                    colocOverlay[i] = Color.white;
                }

                if (valChan0 >= thresholdHighValue[colocChannel0] || valChan1 >= thresholdHighValue[colocChannel1])
                {
                    heatMapNMDPSobel[i] = getNMDP(valChan0, valChan1, 1.0f, (float)chan0AverageNMDP_MIP, (float)chan1AverageNMDP_MIP, (float)chan0MaxNMDP_MIP, (float)chan1MaxNMDP_MIP, _nmdp_minMultFactor, _nmdp_maxMultFactor, true);

                    float value = (float)(((valChan0 - chan0AverageNMDP_MIP) * (valChan1 - chan1AverageNMDP_MIP)) / ((chan0MaxNMDP_MIP - chan0AverageNMDP_MIP) * (chan1MaxNMDP_MIP - chan1AverageNMDP_MIP)));
                    if (heatMapNewBin[i] == Color.white)
                    {
                        if (value < 0)
                        {
                            heatMapNMDPSobelBin[i] = Color.white;
                        }
                    }
                    else
                    {
                        if (value >= 0)
                        {
                            heatMapNMDPSobelBin[i] = Color.grey;
                        }
                    }

                }

            }

            if (!BenProject)
            {
                yield return ExportPixelsAsImage2D(heatMapNewBin, "MIP_HEATMAP_NEW_BIN");
                yield return ExportPixelsAsImage2D(heatMapNMDP, "MIP_HEATMAP_NMDP");
                yield return ExportPixelsAsImage2D(heatMapNMDPSobel, "MIP_HEATMAP_NMDP_Sobel");
                yield return ExportPixelsAsImage2D(heatMapNMDPSobelBin, "MIP_HEATMAP_NMDP_Sobel_BIN");

            }
            yield return ExportPixelsAsImage2D(heatMapNew, "MIP_HEATMAP_NEW");
            yield return ExportPixelsAsImage2D(colocOverlay, "MIP_COLOC_OVERLAY");

        }

        // Automatically open path
        /*
        if (!BenProject)
        {
            String path = Application.persistentDataPath + "/Results/Camera";
            System.Diagnostics.Process.Start(path);
        }
        */
    }

    IEnumerator exportHeatmapAndNMDPSlices(bool onlyInsideROI = false)
    {
        if (loadSample == null)
        {
            Debug.Log("For some reason loadSample is null...");
            yield break;
        }

        int row = 0;
        int col = 0;
        int depth = 0;
        float depthFraction = 0.5f;

        Color[] heatMapNew = new Color[size * size];
        //Color[] heatMapNMDPColoc = new Color[size * size];
        Color[] heatMapNMDPBoth = new Color[size * size];
        Color[] heatMapNMDPBothSobel = new Color[size * size];

        Color[] heatMapNewBinary = new Color[size * size];
        Color[] heatMapNMDPBinary = new Color[size * size];
        Color[] heatMapNMDPBinarySobel = new Color[size * size];

        float _angle = internalMat.GetFloat("_angle");
        float _distThresh = internalMat.GetFloat("_distThresh");
        
        float _ch0Average = internalMat.GetFloat("_ch0AverageAboveThres");
        float _ch1Average = internalMat.GetFloat("_ch1AverageAboveThres");
        float _ch0Max = internalMat.GetFloat("_ch0Max");
        float _ch1Max = internalMat.GetFloat("_ch1Max");

        float _ch0AverageNMDP = internalMat.GetFloat("_ch0AverageNMDP");
        float _ch1AverageNMDP = internalMat.GetFloat("_ch1AverageNMDP");
        float _ch0MaxNMDP = internalMat.GetFloat("_ch0MaxNMDP");
        float _ch1MaxNMDP = internalMat.GetFloat("_ch1MaxNMDP");

        float x_max = internalMat.GetFloat("_x_max");
        Debug.Log("x_max = " + x_max);
        
        float _nmdp_minMultFactor = internalMat.GetFloat("_nmdp_minMultFactor") / 255.0f;
        float _nmdp_maxMultFactor = internalMat.GetFloat("_nmdp_maxMultFactor") / 255.0f;

        Debug.Log("nMDP min: " + _nmdp_minMultFactor + " nMDP max: " + _nmdp_maxMultFactor);

        float maxFloat = internalMat.GetFloat("_maxValue"); //(float)maxHeatmapValue / thresholdMaxInt;
        int pixelIndex = 0;
        for (int i = 0; i < redPixels2D.Length; i++)
        {
            heatMapNewBinary[pixelIndex] = Color.black;
            heatMapNMDPBinary[pixelIndex] = Color.black;
            heatMapNMDPBinarySobel[pixelIndex] = Color.black;

            heatMapNew[pixelIndex] = Color.black;
            //heatMapNMDPColoc[pixelIndex] = Color.black;
            heatMapNMDPBoth[pixelIndex] = Color.black;
            heatMapNMDPBothSobel[pixelIndex] = Color.black;

            if (onlyInsideROI && ROImaskTest3D(col, row, depthFraction))
            {
                float valChan0 = -1f;
                float valChan1 = -1f;
                switch (colocChannel0)
                {
                    case 0: // red
                        valChan0 = channel0Data[i].r;
                        break;
                    case 1: // green
                        valChan0 = channel0Data[i].g;
                        break;
                    case 2: // blue
                        valChan0 = channel0Data[i].b;
                        break;
                    case 3: // purple
                        valChan0 = channel0Data[i].r;
                        break;
                }
                switch (colocChannel1)
                {
                    case 0: // red
                        valChan1 = channel1Data[i].r;
                        break;
                    case 1: // green
                        valChan1 = channel1Data[i].g;
                        break;
                    case 2: // blue
                        valChan1 = channel1Data[i].b;
                        break;
                    case 3: // purple
                        valChan1 = channel1Data[i].r;
                        break;
                }

                if (valChan0 >= thresholdHighValue[colocChannel0] && valChan1 >= thresholdHighValue[colocChannel1])
                {
                    //row + (size-col) * size
                    float value = (float)(((valChan0 - _ch0Average) * (valChan1 - _ch1Average)) / ((_ch0Max - _ch0Average) * (_ch1Max - _ch1Average)));
                    if(value > 0)
                    {
                        heatMapNMDPBinary[pixelIndex] = Color.white;
                    }

                    heatMapNew[pixelIndex] = getGradientColocAverageHeatmapNew(valChan0, valChan1, maxFloat, _angle, _distThresh, p1, p2, x_max);
                    if (heatMapNew[pixelIndex] != Color.black)
                        heatMapNewBinary[pixelIndex] = Color.white;

                    //heatMapNMDPColoc[pixelIndex] = getNMDP(valChan0, valChan1, 1.0f, (float)_ch0Average, (float)_ch1Average, (float)_ch0Max, (float)_ch1Max, _nmdp_minMultFactor, _nmdp_maxMultFactor);
                    heatMapNMDPBoth[pixelIndex] = getNMDP(valChan0, valChan1, 1.0f, (float)_ch0Average, (float)_ch1Average, (float)_ch0Max, (float)_ch1Max, _nmdp_minMultFactor, _nmdp_maxMultFactor, true);
                }

                //nmdp sobel
                if (valChan0 >= thresholdHighValue[colocChannel0] || valChan1 >= thresholdHighValue[colocChannel1])
                {
                    float value = (float)(((valChan0 - _ch0AverageNMDP) * (valChan1 - _ch1AverageNMDP)) / ((_ch0MaxNMDP - _ch0AverageNMDP) * (_ch1MaxNMDP - _ch1AverageNMDP)));
                    if (heatMapNewBinary[pixelIndex] == Color.white)
                    {
                        if (value < 0)
                        {
                            heatMapNMDPBinarySobel[pixelIndex] = Color.white;
                        }
                    }
                    else
                    {
                        if (value >= 0)
                        {
                            heatMapNMDPBinarySobel[pixelIndex] = Color.grey;
                        }
                    }                    

                    heatMapNMDPBothSobel[pixelIndex] = getNMDP(valChan0, valChan1, 1.0f, (float)_ch0AverageNMDP, (float)_ch1AverageNMDP, (float)_ch0MaxNMDP, (float)_ch1MaxNMDP, _nmdp_minMultFactor, _nmdp_maxMultFactor, true);
                }

            }
            pixelIndex++;

            col++;
            if (col >= size)
            {
                col = 0;
                row++;

                if (row >= size) // use the same mask for all depths
                {
                    String depthText = depth.ToString();
                    if (depth < 10)
                        depthText = "0" + depthText;

                    yield return ExportPixelsAsImage2D(heatMapNewBinary, thisImageNameBen + "HeatmapBin_" + depthText + "_", "Slices/HeatmapBin/");
                    yield return ExportPixelsAsImage2D(heatMapNMDPBinary, thisImageNameBen + "NMDPBin_" + depthText + "_", "Slices/NMDP coloc bin/");
                    yield return ExportPixelsAsImage2D(heatMapNMDPBinarySobel, thisImageNameBen + "NMDPBinSobel_" + depthText + "_", "Slices/NMDP sobel bin/");

                    yield return ExportPixelsAsImage2D(heatMapNew, thisImageNameBen + "Heatmap_" + depthText + "_", "Slices/Heatmap/");
                    //yield return ExportPixelsAsImage2D(heatMapNMDPColoc, "NMDP_Coloc_" + depthText + "_", "Slices/NDMP coloc/");
                    yield return ExportPixelsAsImage2D(heatMapNMDPBoth, thisImageNameBen + "NMDP_Both_" + depthText + "_", "Slices/NMDP Full/");
                    yield return ExportPixelsAsImage2D(heatMapNMDPBothSobel, thisImageNameBen + "NMDP_Both_Sobel_" + depthText + "_", "Slices/NMDP full sobel/");
                    row = 0;
                    pixelIndex = 0;
                    depth++;
                    depthFraction = -0.5f + (float)depth / loadSample.texDepth;
                    if (depth >= loadSample.texDepth)
                    {
                        Debug.Log("Reached the end depth in exportHeatmapAndNMDPSlices calculation");
                    }
                }
            }

            if (i % 1000000 == 0)
            {
                Debug.Log(string.Format("nMDP generation {0:0.0}% complete", (float)i / redPixels2D.Length * 100));
                yield return null;
            }
        }

        yield return null;
    }

    // takes the full z-stack for a specific colour channel (that is only Red, Green, etc.) and returns the MIP (as a 2D color array) of that stack
    IEnumerator GenerateMIPForCalc ()
    {
        float maxFloat = internalMat.GetFloat("_maxValue"); //(float)maxHeatmapValue / thresholdMaxInt;

        if (loadSample == null)
        {
            Debug.Log("For some reason loadSample is null...");
            yield break;
        }

        MIPSliceCh0 = new Color[size * size];
        MIPSliceCh1 = new Color[size * size];
        for (int i = 0; i < MIPSliceCh0.Length; i++)
        {
            MIPSliceCh0[i] = Color.black;
            MIPSliceCh1[i] = Color.black;
        }

        int row = 0;
        int col = 0;
        int depth = 0;

        int MIPIndex = 0;
        // extract full MIP (without coloc)
        for (int i = 0; i < redPixels2D.Length; i++)
        {
            if (onlyInsideROI && !ROImaskTest3D(col, row, ((float)depth / loadSample.texDepth) - 0.5f))
            {
                // nothing... keep black
            }
            else
            {
                switch (colocChannel0)
                {
                    case 0: // red
                        MIPSliceCh0[MIPIndex].r = Mathf.Max(MIPSliceCh0[MIPIndex].r, redPixels2D[i].r);
                        break;
                    case 1: // green
                        MIPSliceCh0[MIPIndex].g = Mathf.Max(MIPSliceCh0[MIPIndex].g, greenPixels2D[i].g);
                        break;
                    case 2: // blue
                        MIPSliceCh0[MIPIndex].b = Mathf.Max(MIPSliceCh0[MIPIndex].b, bluePixels2D[i].b);
                        break;
                    case 3: // purple
                        Color c = new Color(purplePixels2D[i].r, 0f,purplePixels2D[i].r);
                        MIPSliceCh0[MIPIndex] = new Color(Mathf.Max(MIPSliceCh0[MIPIndex].r, c.r), 0f, Mathf.Max(MIPSliceCh0[MIPIndex].b, c.b));
                        break;
                }

                switch (colocChannel1)
                {
                    case 0: // red
                        MIPSliceCh1[MIPIndex].r = Mathf.Max(MIPSliceCh1[MIPIndex].r, redPixels2D[i].r);
                        break;
                    case 1: // green
                        MIPSliceCh1[MIPIndex].g = Mathf.Max(MIPSliceCh1[MIPIndex].g, greenPixels2D[i].g);
                        break;
                    case 2: // blue
                        MIPSliceCh1[MIPIndex].b = Mathf.Max(MIPSliceCh1[MIPIndex].b, bluePixels2D[i].b);
                        break;
                    case 3: // purple
                        Color c = new Color( purplePixels2D[i].r, 0f, purplePixels2D[i].r);
                        MIPSliceCh1[MIPIndex] = new Color(Mathf.Max(MIPSliceCh1[MIPIndex].r, c.r), 0f, Mathf.Max(MIPSliceCh1[MIPIndex].b, c.b));
                        break;
                }
            }

            // I need this method, since I'm "ignoring" depth, and cycle over each image with the same coordinates in the MIP
            col++;
            if (col >= size)
            {
                col = 0;
                row++;

                if (row >= size) // use the same mask for all depths
                {
                    row = 0;
                    depth++;
                    if (depth >= loadSample.texDepth)
                    {
                        Debug.Log("Reached the end depth in MIP for calc calculation");
                    }
                }
            }

            MIPIndex = col + row * size;

            if (i % 1000000 == 0)
            {
                yield return null;
            }
            if (i % 5000000 == 0)
            {
                Debug.Log(string.Format("MIP generation {0:0.0}% complete", (float)i / redPixels2D.Length * 100));
            }
        }
    }

    Color getGradientColocAverageHM(float ch0Val, float ch1Val, float max)
    {

        float average = (ch0Val + ch1Val) / 2.0f;
        float min = ((thresholdHighValue[colocChannel0] + thresholdHighValue[colocChannel1]) / 2.0f); // this is the minimum value that average can be
        int value = (int)(((average - min) / (max - min)) * thresholdMaxInt);


        //return _viridisCM[value/max*256];
        return HeatMapColorRainbow(value, (int)(min * thresholdMaxInt), (int)(max * thresholdMaxInt));
        //return HeatMapColorRainbow(50, 0, 100);
        //return HeatMapColor(value, min, max);
        //return HeatMapColorLog(value, min, max);

    }

    Color getGradientColocAverageWhite(float ch0Val, float ch1Val, float max)
    {

        float average = (ch0Val + ch1Val) / 2.0f;
        float min = ((thresholdHighValue[colocChannel0] + thresholdHighValue[colocChannel1]) / 2.0f); // this is the minimum value that average can be
        float value = (((average - min) / (max - min)));

        return new Color(value, value, value);
    }

    Color getGradientColocAverageHeatmapNew(float ch0Val, float ch1Val, float max, float _angle, float _distThresh, Vector2 p1_local, Vector2 p2_local, float x_max)
    {
        //https://en.wikipedia.org/wiki/Vector_projection
        Vector2 p3 = new Vector2(ch0Val, ch1Val);

        float k = ((p2_local.y - p1_local.y) * (p3.x - p1_local.x) - (p2_local.x - p1_local.x) * (p3.y - p1_local.y)) / ((p2_local.y - p1_local.y) * (p2_local.y - p1_local.y) + (p2_local.x - p1_local.x) * (p2_local.x - p1_local.x));
        float result = p3.x - k * (p2_local.y - p1_local.y);

        float value = 0f;
        /*
        // in case the point is above the line make it the max
        float m = (p2_local.y - p1_local.y) / (p2_local.x - p1_local.x);
        float c = p2_local.y - m * p2_local.x;
        float inverseC = p2_local.y + 1 / m * p2_local.x;

        
        if (p3.y < (-1 / m) * p3.x + inverseC)
            value = ((result - p1_local.x) / (p2_local.x - p1_local.x));
        else
            value = 1;

        if (value > 1)
            value = 1;


        //http://mathworld.wolfram.com/Point-LineDistance2-Dimensional.html
        float dis = (Mathf.Abs((p2_local.y - p1_local.y) * p3.x - (p2_local.x - p1_local.x) * p3.y + p2_local.x * p1_local.y - p2_local.y * p1_local.x)) / Mathf.Sqrt((p2_local.y - p1_local.y) * (p2_local.y - p1_local.y) + (p2_local.x - p1_local.x) * (p2_local.x - p1_local.x)) * 255;
        if (dis < _distThresh)
            value = value * 255 - dis * Mathf.Tan(_angle * Mathf.Deg2Rad);
        else
            value = 0;

        if (value < 0)
            value = 0;
         */

        //http://mathworld.wolfram.com/Point-LineDistance2-Dimensional.html
        float x_min = p1_local[0];
        //x_max = x_max;
        float dis = (Mathf.Abs((p2_local.y - p1_local.y) * p3.x - (p2_local.x - p1_local.x) * p3.y + p2_local.x * p1_local.y - p2_local.y * p1_local.x)) / Mathf.Sqrt((p2_local.y - p1_local.y) * (p2_local.y - p1_local.y) + (p2_local.x - p1_local.x) * (p2_local.x - p1_local.x));
        if (result <= (dis * (x_max - x_min) * Mathf.Tan(_angle * Mathf.Deg2Rad) + x_min) || dis > _distThresh/255.0f)
            value = 0f;
        else if ((dis * (x_max - x_min) * Mathf.Tan(_angle * Mathf.Deg2Rad) + x_min) < result && result < x_max)
            value = ((result - x_min) / (x_max - x_min)) - dis * Mathf.Tan(_angle * Mathf.Deg2Rad);
        else if (result >= x_max)
            value = 1 - dis * Mathf.Tan(_angle * Mathf.Deg2Rad);

        int index = (int)(value * 255);
        if(index > 255)
        {
            Debug.Log("HM value was too big: " + index + " p3 = " + p3*255);
            index = 255;
        }
        else if (index < 0)
        {
            Debug.Log("HM value was negative: " + index + " p3 = " + p3 * 255);
            index = 0;
        }
        //return CM.heatmapColormap[index];
        return CM.magma[index];
        //return HeatMapColorRainbow((int)value, 0, (int)(max * thresholdMaxInt));

    }

    Color getNMDP(float ch0Val, float ch1Val, float max, float _ch0Average, float _ch1Average, float _ch0Max, float _ch1Max, float _nmdp_minMultFactor, float _nmdp_maxMultFactor, bool bothColocAndNotColoc = false)
    {
        if(_ch1Max > 1)
        {
            Debug.LogError("_ch1Max I need to divide by 255");
        }
        if(_ch0Average > 1)
        {
            Debug.LogError("_ch0Average I need to divide by 255");
        }
        if (ch0Val > 1)
        {
            Debug.LogError("ch0Val I need to divide by 255: " + ch0Val);
        }

        // this is using the https://sites.google.com/site/colocalizationcolormap/home formula
        float value = (((ch0Val - _ch0Average) * (ch1Val - _ch1Average)) / ((_ch0Max - _ch0Average) * (_ch1Max - _ch1Average)));

        return HeatMapColorNMDP(value, max * _nmdp_minMultFactor, max * _nmdp_maxMultFactor, bothColocAndNotColoc);
    }

    Color HeatMapColorNMDP(float val, float min, float max, bool bothColocAndNotColoc)
    {
        


           
        /*
    if (val > 0)
    {
        if (val < max / 3.0f)
        {
            float v = (val - max / 3.0f) / (max / 3.0f - 0.0f);
            return new Color(v, 0.0f, 0.0f);
        }
        else if (val < max * 2.0f / 3.0f)
        {
            float v = (val - max * 2.0f / 3.0f) / ( max * 2.0f / 3.0f - max / 3.0f);
            return new Color(1.0f, v, 0.0f);
        }
        else
        {
            float v = (val - max) / (max - max * 2.0f / 3.0f);
            return new Color(1.0f, 1.0f, v);
        }

    }
    else
    {
        //return Color.black;
        if (-val < min / 2.0)
        {
            float v = (-val - min / 2.0f) / (min / 2.0f - 0.0f);
            return new Color(0.0f, 1.0f - v, 1.0f);
        }
        else
        {
            float v = (-val - min) / (min - min / 2.0f);
            return new Color(0.0f, 0f, 1.0f - v);
        }
    }
    */
        if (val >= 0)
        {
            if (val > max)
                return CM.colocNMDP[255];

            int mappedVal = (int)(val / max * 255);
            return CM.colocNMDP[mappedVal];
        }
        // for negative values        
        else if(bothColocAndNotColoc)
        {
            if(-val > min)
                return CM.notColocNMDP[255];

            int mappedVal = (int)(-val / min * 255);
            return CM.notColocNMDP[mappedVal];
        }

        return Color.black;

    }

    IEnumerator ExportPixelsAsImage2D(Color[] pixels, string nameLabel, string folder="")
    {
        Texture2D image = new Texture2D(size, size);
        image.SetPixels(pixels);
        image.Apply();

        yield return null;

        //SliceQuad2D.SetActive(true);
        //SliceQuad2D.GetComponent<Renderer>().material.mainTexture = image;

        // Encode texture into PNG
        byte[] bytes = image.EncodeToPNG();

        try
        {
            string path = Application.persistentDataPath + "/Results/Camera/" + folder;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string filePath = path  + nameLabel + Time.time + ".png";
            if(BenProject)
                filePath = saveLocation2D + thisImageFolderStructure + nameLabel + Time.time + ".png";
            //string filePath = "I:/Master's results/Results/Camera/cameraOutput_" + 45f * (i - 1) + "_" + Time.time + ".png";
            System.IO.File.WriteAllBytes(filePath, bytes);
            //Debug.Log("Wrote current " + nameLabel + " output to " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

        
    }

    public void ToggleShowMIP(Toggle toggle)
    {
        showMIP = toggle.isOn;
        MIPQuad.SetActive(showMIP);

        if (!MIPMatInitialized)
            setupMIP();

    }
    
    public void ToggleColorOnOff(Toggle toggle)
    {
        float newVal = toggle.isOn ? 1.0f : 0.0f;

        if (toggle.gameObject.name.ToLower().Contains("red"))
            internalMat.SetFloat("_redOnOff", newVal);
        else if (toggle.gameObject.name.ToLower().Contains("green"))
            internalMat.SetFloat("_greenOnOff", newVal);
        else if (toggle.gameObject.name.ToLower().Contains("blue"))
            internalMat.SetFloat("_blueOnOff", newVal);
        else if (toggle.gameObject.name.ToLower().Contains("purple"))
            internalMat.SetFloat("_purpleOnOff", newVal);
       
    }

    public void ConvertSphereCylinderButton()
    {
        bool toSphere = activeROITools[0].CompareTag("ROICylinder");

        for (int i = 0; i < activeROITools.Count; i++)
        {
            GameObject prevGO = activeROITools[i];
            Vector3 prevToolPos = prevGO.transform.position;
            Quaternion prevToolRot = prevGO.transform.rotation;
            Vector3 prevToolScale = prevGO.transform.localScale;
            //Debug.Log("Scale: " + prevToolScale);

            if (prevGO.CompareTag("ROIFreehand"))
            {
                boundingBox.transform.localScale = previousBoxScale;
            }

            activeROITools[i] = (GameObject)Instantiate(toSphere ? ROISphere : ROICylinder, prevToolPos, prevToolRot);
            activeROITools[i].SetActive(true);
            activeROITools[i].transform.SetParent(prevGO.transform.parent);
            activeROITools[i].transform.localScale = prevToolScale;

            Destroy(prevGO);

            currentROITool = activeROITools[i];

            currentROITool.SetActive(true);
        }
    }

    /*
    public void ToggleShowROIMask(Toggle toggle)
    {
        showROIMask = toggle.isOn;

        if(showROIMask)
        {
            internalMat.SetTexture("_ROIMask3D", ROI3DMaskTexture);
        }
        else
        {
            internalMat.SetTexture("_ROIMask3D", ShowAllMask);
        }
    }*/

    public void ToggleSelectSlice(Toggle toggle)
    {
        showSelectSlice = toggle.isOn;
        SliceSelectPanel.SetActive(showSelectSlice);
    }

    public void SelectSlice(int slice)
    {
        currentSlice = slice;
        StartCoroutine(Generate2DSliceForQuad(slice));
    }

    public void ToggleMIPOnlyInsideROI(Toggle toggle)
    {
        onlyInsideROI = toggle.isOn;
    }

    public void ToggleShowMIPHeatMap(Toggle toggle)
    {
        showHeatMap = toggle.isOn;
    }

    public void ToggleShowMIPColors(Toggle toggle)
    {
        showMIPColors = toggle.isOn;
    }

    public void ToggleShowSeparteColors(Toggle toggle)
    {
        seperateChannelsExport2D = toggle.isOn;
    }

    public void ToggleShowMIPColoc(Toggle toggle)
    {
        showColoc2D = toggle.isOn;
    }

    public void ToggleShowScatterPlot(Toggle toggle)
    {
        showScatterPlot = toggle.isOn;
    }

    public void ToggleLockAxisTranslation(Toggle toggle)
    {
        ROILockAxisTranslation = toggle.isOn;
    }

    public void ToggleTranslateTogether(Toggle toggle)
    {
        ROITranslateTogether = toggle.isOn;
    }

    public void UpdateDistance()
    {
        int texSize = 256;

        long countInRange = 0;

        // in case the point is above the line make it the max
        if (m_LinRegThroughThres == 1)
        {
            Debug.LogError("I am probably not using m_LinRegThroughThres. Check in FreqScatterPlot function.");
        }

        //THE AUTOMATIC MAX DETECTION
        int newTotalVoxels = 0;
        for (int x = 0; x < texSize; x++)
        {
            for (int y = 0; y < texSize; y++)
            {
                if (x/(float)texSize >= thresholdHighValue[colocChannel0] && y / (float)texSize >= thresholdHighValue[colocChannel1])                
                    newTotalVoxels += scatterCountsFreq3D[x, y];
            }
        }

        float m = -1.0f / m_LinRegThroughThres;// (p2.y - p1.y) / (p2.x - p1.x);
        //float c = p2.y - m * p2.x;
        //float inverseC = p2.y + 1 / m * p2.x;
        p_end = Vector2.zero;
        Debug.Log("m (updatedistance) = " + m);
        float percentageActuallyUsed = 0f;
        if (m < -1) // use x
        {
            for (int testX = 0; testX < texSize * 3; testX++)// times 2 should be sufficient, times 3 is just for safety...
            {
                float inverseC = 0 - m * testX / (float)texSize;
                //NB! x and y might be swopped?
                for (int x = 0; x < testX; x++)
                {
                    if (x >= texSize)
                        break;

                    for (int y = 0; y < texSize; y++)
                    {
                        Vector2 p3 = new Vector2(x / (float)texSize, y / (float)texSize);

                        if (p3.x >= thresholdHighValue[colocChannel0] && p3.y >= thresholdHighValue[colocChannel1])
                        {

                            countInRange += scatterCountsFreq3D[x, y];


                            if (p3.y >= m * p3.x + inverseC)
                            {
                                break;
                            }
                        }
                    }
                    if (countInRange / (float)newTotalVoxels >= percentageUsed)
                    {
                        break;
                    }
                }
                //Debug.Log("testX = " + testX + " countInRange = " + countInRange + " percentage = " + countInRange / (float)newTotalVoxels);
                /*
                if (percentageActuallyUsed == 0 && testX >= texSize)
                {
                    percentageActuallyUsed = percentageUsed;
                }*/
                if (countInRange / (float)newTotalVoxels >= percentageUsed)
                {
                    p_end.x = testX / (float)texSize;

                    double max_c = p_end.y - m * p_end.x;
                    if (m_LinRegThroughThres + c_LinRegThroughThres < m + max_c)
                    {
                        Debug.Log("The maximum had to be limited...");
                        p_end.x = 1.0f;
                        p_end.y = m_LinRegThroughThres + c_LinRegThroughThres;
                    }
                    /*
                    else
                    {
                        percentageActuallyUsed = percentageUsed;
                    }*/
                    break;
                }
                else
                {
                    countInRange = 0;
                }
            }
        }
        else // use y
        {
            for (int testY = 0; testY < texSize * 3; testY++) // times 2 should be sufficient, times 3 is just for safety...
            {
                float inverseC = testY / (float)texSize;
                //NB! x and y might be swopped?
                for (int x = 0; x < texSize; x++)
                {
                    for (int y = 0; y < testY; y++)
                    {
                        if (y >= texSize)
                            break;

                        Vector2 p3 = new Vector2(x / (float)texSize, y / (float)texSize);

                        if (p3.x >= thresholdHighValue[colocChannel0] && p3.y >= thresholdHighValue[colocChannel1])
                        {
                            //Debug.Log("ADDED: " + scatterCounts[x, y]);
                            countInRange += scatterCountsFreq3D[x, y];
                            

                            if (p3.y >= m * p3.x + inverseC)
                            {
                                //Debug.Log("BREAK");
                                break;
                            }
                        }
                    }
                    if (countInRange / (float)newTotalVoxels >= percentageUsed)
                    {
                        break;
                    }
                }
                Debug.Log("testY = " + testY + " countInRange = " + countInRange + " percentage = " + countInRange / (float)newTotalVoxels);
                /*if (percentageActuallyUsed == 0 && testY >= texSize)
                {
                    percentageActuallyUsed = percentageUsed;
                }*/
                if (countInRange / (float)newTotalVoxels >= percentageUsed)
                {
                    p_end.y = testY / (float)texSize;

                    double max_c = p_end.y - m * p_end.x;
                    if (m_LinRegThroughThres + c_LinRegThroughThres < m + max_c)
                    {
                        Debug.Log("The maximum had to be limited...");
                        p_end.x = 1.0f;
                        p_end.y = m_LinRegThroughThres + c_LinRegThroughThres;
                    }/*
                    else
                    {
                        percentageActuallyUsed = percentageUsed;
                    }*/
                    break;
                }
                else
                {

                    countInRange = 0;
                }
            }
        }

        if(p_end.x == 0.0 && p_end.y == 0.0)
        {
            Debug.Log("Could not reach 99%...");
            p_end.x = 1.0f;
            p_end.y = m_LinRegThroughThres + c_LinRegThroughThres;
        }

        float c_max = p_end[1] + (1 / m_LinRegThroughThres) * p_end[0];
        float x_max = (c_LinRegThroughThres - c_max) / (-1 / m_LinRegThroughThres - m_LinRegThroughThres);
        Debug.Log("x_max = " + x_max + "  c_max = " + c_max);
        internalMat.SetFloat("_x_max", x_max);


        Debug.Log("P_END: " + p_end);// + " Percentage actually used: " + percentageActuallyUsed);

        float k = ((p2.y - p1.y) * (p_end.x - p1.x) - (p2.x - p1.x) * (p_end.y - p1.y)) / ((p2.y - p1.y) * (p2.y - p1.y) + (p2.x - p1.x) * (p2.x - p1.x));
        float projectionX = p_end.x - k * (p2.y - p1.y);

        float Cvalue = ((projectionX - p1.x) / (p2.x - p1.x));
        Debug.Log("Cvalue: " + Cvalue);
        if (Cvalue > 1)
        {
            Debug.LogError("Cvalue was greater than 1, something went wrong");
            Cvalue = 1;
            internalMat.SetFloat("_maxValue", Cvalue);
        }
        else if(Cvalue < 0)
        {
            Debug.LogError("Cvalue was less than 0, something went wrong");
        }
        else
        {
            internalMat.SetFloat("_maxValue", Cvalue);
        }

        //END OF AUTOMATIC MAX

        // START of AUTOMATIC DISTANCE
        countInRange = 0;
        int usedDistanceThreshold = 0;
        for(int distanceThresh = 0; distanceThresh < texSize; distanceThresh++)
        {
            for (int x = 0; x < texSize; x++)
            {
                for (int y = 0; y < texSize; y++)
                {
                    Vector2 p3 = new Vector2(x / (float)texSize, y / (float)texSize);
                    if (p3.x >= thresholdHighValue[colocChannel0] && p3.y >= thresholdHighValue[colocChannel1])
                    {
                        float dis = (Mathf.Abs((p2.y - p1.y) * p3.x - (p2.x - p1.x) * p3.y + p2.x * p1.y - p2.y * p1.x)) / Mathf.Sqrt((p2.y - p1.y) * (p2.y - p1.y) + (p2.x - p1.x) * (p2.x - p1.x)) * texSize;
                        if (dis < distanceThresh)
                        {
                            countInRange += scatterCountsFreq3D[x, y];
                            if (countInRange / (float)newTotalVoxels >= percentageUsed)
                            {
                                break;
                            }
                        }
                    }
                }
                if (countInRange / (float)newTotalVoxels >= percentageUsed)
                {
                    break;
                }
            }
            //Debug.Log("Current distance Thresh: " + distanceThresh + " percentage current: " + countInRange / (float)newTotalVoxels);
            if (countInRange / (float)newTotalVoxels >= percentageUsed)
            {
                Debug.Log("Using distance threshold: " + distanceThresh);
                if (distanceThresh < 1)
                    distanceThresh = 1;

                internalMat.SetFloat("_distThresh", distanceThresh);
                usedDistanceThreshold = distanceThresh;
                break;
            }
            else
            {
                countInRange = 0;
            }

        }

        // END of AUTOMATIC DISTANCE
        /*
        //START OF AUTOMATIC ANGLE
        if (automaticUpdateAngle)
        {
            automaticUpdateAngle = false;
            //using distance formula
            float angle = Mathf.Rad2Deg * Mathf.Atan2(Mathf.Sqrt((p_end.x - p1.x) * (p_end.x - p1.x) + (p_end.y - p1.y) * (p_end.y - p1.y)), usedDistanceThreshold / (float)texSize);

            //using default of 45 degrees
            angle = 45;

            internalMat.SetFloat("_angle", angle);
            Slider slider = colocSliders[(int)ColocMatProperties.Angle].GetComponent<Slider>();

            Debug.Log("Automatic angle in degrees: " + angle);

            String sliderText = slider.GetComponentInChildren<UnityEngine.UI.Text>().text;
            slider.value = angle;
            slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0:0}", slider.value);
        }
        //END OF AUTOMATIC ANGLE
        */

        try
        {
            string pathScat = Application.persistentDataPath + "/Results/Scatter/";
            if (!Directory.Exists(pathScat))
                Directory.CreateDirectory(pathScat);
            System.IO.File.WriteAllText(pathScat + "regLine3D_thresholds_" + Time.time + ".txt", "max_3D = " + Cvalue + " p_end = [" + p_end.x + ", " + p_end.y + "]  distThresh3D = " + usedDistanceThreshold);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

        ColocFrequencyScatterToTexture3DBusy = false;
        ScatterToTexture(texSize, freqScatterColors3D, (int)ScatterPlotTypes.FrequencyScatter, "3D", false);
    }
    
    public void UpdateDistanceMIP(string label)
    {
        int texSize = 256;

        long countInRange = 0;

        // in case the point is above the line make it the max
        if (m_LinRegThroughThresMIP == 1)
        {
            Debug.LogError("I am probably not using m_LinRegThroughThresMIP. Check in FreqScatterPlot function.");
        }

        //THE AUTOMATIC MAX DETECTION
        int newTotalVoxels = 0;
        for (int x = 0; x < texSize; x++)
        {
            for (int y = 0; y < texSize; y++)
            {
                
                if (x / (float)texSize >= thresholdHighValue[colocChannel0] && y / (float)texSize >= thresholdHighValue[colocChannel1])
                {
                    newTotalVoxels += scatterCountsFreq2D[x, y];
                }
            }
        }


        float m = -1.0f / m_LinRegThroughThresMIP;// (p2MIP.y - p1MIP.y) / (p2MIP.x - p1MIP.x);
        //float c = p2MIP.y - m * p2MIP.x;
        //float inverseC = p2MIP.y + 1 / m * p2MIP.x;
        p_endMIP = Vector2.zero;
        Debug.Log("m ("+label+") = " + m);
        float percentageActuallyUsed = 0f;
        if (m < -1) // use x
        {
            for (int testX = 0; testX < texSize * 3; testX++)// times 2 should be sufficient, times 3 is just for safety...
            {
                float inverseC = 0 - m * testX / (float)texSize;
                //NB! x and y might be swopped?
                for (int x = 0; x < testX; x++)
                {
                    if (x >= texSize)
                        break;

                    for (int y = 0; y < texSize; y++)
                    {
                        Vector2 p3MIP = new Vector2(x / (float)texSize, y / (float)texSize);

                        if (p3MIP.x >= thresholdHighValue[colocChannel0] && p3MIP.y >= thresholdHighValue[colocChannel1])
                        {

                            countInRange += scatterCountsFreq2D[x, y];


                            if (p3MIP.y >= m * p3MIP.x + inverseC)
                                break;
                        }
                    }
                    if (countInRange / (float)newTotalVoxels >= percentageUsed)
                    {
                        break;
                    }
                }
                // Debug.Log("testX = " + testX + " countInRange = " + countInRange + " percentage = " + countInRange / (float)newTotalVoxels);
                /*
                if (percentageActuallyUsed == 0 && testX >= texSize)
                {
                    percentageActuallyUsed = percentageUsed;
                }*/
                if (countInRange / (float)newTotalVoxels >= percentageUsed)
                {
                    p_endMIP.x = testX / (float)texSize;

                    double max_c = p_endMIP.y - m * p_endMIP.x;
                    if (m_LinRegThroughThresMIP + c_LinRegThroughThresMIP < m + max_c)
                    {
                        Debug.Log("The maximum had to be limited...");
                        p_endMIP.x = 1.0f;
                        p_endMIP.y = m_LinRegThroughThresMIP + c_LinRegThroughThresMIP;
                    }
                    /*
                    else
                    {
                        percentageActuallyUsed = percentageUsed;
                    }*/
                    break;
                }
                else
                {
                    countInRange = 0;
                }
            }
        }
        else // use y
        {
            for (int testY = 0; testY < texSize * 3; testY++) // times 2 should be sufficient, times 3 is just for safety...
            {
                float inverseC = testY / (float)texSize;
                //NB! x and y might be swopped?
                for (int x = 0; x < texSize; x++)
                {
                    for (int y = 0; y < testY; y++)
                    {
                        if (y >= texSize)
                            break;

                        Vector2 p3MIP = new Vector2(x / (float)texSize, y / (float)texSize);

                        if (p3MIP.x >= thresholdHighValue[colocChannel0] && p3MIP.y >= thresholdHighValue[colocChannel1])
                        {
                            //Debug.Log("ADDED: " + scatterCounts[x, y]);
                            countInRange += scatterCountsFreq2D[x, y];


                            if (p3MIP.y >= m * p3MIP.x + inverseC)
                            {
                                //Debug.Log("BREAK");
                                break;
                            }
                        }
                    }
                    if (countInRange / (float)newTotalVoxels >= percentageUsed)
                    {
                        break;
                    }
                }
                //Debug.Log("testY = " + testY + " countInRange = " + countInRange + " percentage = " + countInRange / (float)newTotalVoxels);
                /*if (percentageActuallyUsed == 0 && testY >= texSize)
                {
                    percentageActuallyUsed = percentageUsed;
                }*/
                if (countInRange / (float)newTotalVoxels >= percentageUsed)
                {
                    p_endMIP.y = testY / (float)texSize;

                    double max_c = p_endMIP.y - m * p_endMIP.x;
                    if (m_LinRegThroughThresMIP + c_LinRegThroughThresMIP < m + max_c)
                    {
                        Debug.Log("The maximum had to be limited...");
                        p_endMIP.x = 1.0f;
                        p_endMIP.y = m_LinRegThroughThresMIP + c_LinRegThroughThresMIP;
                    }/*
                    else
                    {
                        percentageActuallyUsed = percentageUsed;
                    }*/
                    break;
                }
                else
                {

                    countInRange = 0;
                }
            }
        }

        if (p_endMIP.x == 0.0 && p_endMIP.y == 0.0)
        {
            Debug.Log("Could not reach 99%... MIP");
            p_endMIP.x = 1.0f;
            p_endMIP.y = m_LinRegThroughThresMIP + c_LinRegThroughThresMIP;
        }


        Debug.Log("p_endMIP: " + p_endMIP);// + " Percentage actually used: " + percentageActuallyUsed);

        float k = ((p2MIP.y - p1MIP.y) * (p_endMIP.x - p1MIP.x) - (p2MIP.x - p1MIP.x) * (p_endMIP.y - p1MIP.y)) / ((p2MIP.y - p1MIP.y) * (p2MIP.y - p1MIP.y) + (p2MIP.x - p1MIP.x) * (p2MIP.x - p1MIP.x));
        float projectionX = p_endMIP.x - k * (p2MIP.y - p1MIP.y);

        float Cvalue = ((projectionX - p1MIP.x) / (p2MIP.x - p1MIP.x));
        Debug.Log("Cvalue  (MIP): " + Cvalue);
        if (Cvalue > 1)
        {
            Debug.LogError("Cvalue was greater than 1, something went wrong (MIP)");
            Cvalue = 1;
            //internalMat.SetFloat("_maxValue", Cvalue);
            visMaxMIP = Cvalue;
        }
        else if (Cvalue < 0)
        {
            Debug.LogError("Cvalue was less than 0, something went wrong  (MIP)");
        }
        else
        {
            visMaxMIP = Cvalue;
            //internalMat.SetFloat("_maxValue", Cvalue);
        }

        //END OF AUTOMATIC MAX

        // START of AUTOMATIC DISTANCE
        countInRange = 0;
        int usedDistanceThresholdMIP = 0;
        for (int distanceThresh = 0; distanceThresh < texSize; distanceThresh++)
        {
            for (int x = 0; x < texSize; x++)
            {
                for (int y = 0; y < texSize; y++)
                {
                    Vector2 p3MIP = new Vector2(x / (float)texSize, y / (float)texSize);
                    if (p3MIP.x >= thresholdHighValue[colocChannel0] && p3MIP.y >= thresholdHighValue[colocChannel1])
                    {
                        float dis = (Mathf.Abs((p2MIP.y - p1MIP.y) * p3MIP.x - (p2MIP.x - p1MIP.x) * p3MIP.y + p2MIP.x * p1MIP.y - p2MIP.y * p1MIP.x)) / Mathf.Sqrt((p2MIP.y - p1MIP.y) * (p2MIP.y - p1MIP.y) + (p2MIP.x - p1MIP.x) * (p2MIP.x - p1MIP.x)) * texSize;
                        if (dis < distanceThresh)
                        {
                            countInRange += scatterCountsFreq2D[x, y];
                            if (countInRange / (float)newTotalVoxels >= percentageUsed)
                            {
                                break;
                            }
                        }
                    }
                }
                if (countInRange / (float)newTotalVoxels >= percentageUsed)
                {
                    break;
                }
            }
            //Debug.Log("Current distance Thresh: " + distanceThresh + " percentage current: " + countInRange / (float)newTotalVoxels);
            if (countInRange / (float)newTotalVoxels >= percentageUsed)
            {
                Debug.Log("Using distance threshold: " + distanceThresh);
                if (distanceThresh < 1)
                    distanceThresh = 1;

                //internalMat.SetFloat("_distThresh", distanceThresh);
                usedDistanceThresholdMIP = distanceThresh;
                visDistanceThresMIP = distanceThresh;
                break;
            }
            else
            {
                countInRange = 0;
            }

        }

        // END of AUTOMATIC DISTANCE
        /*
        //START OF AUTOMATIC ANGLE
        if (automaticUpdateAngle)
        {
            automaticUpdateAngle = false;
            //using distance formula
            visAngleMIP = Mathf.Rad2Deg * Mathf.Atan2(Mathf.Sqrt((p_endMIP.x - p1MIP.x) * (p_endMIP.x - p1MIP.x) + (p_endMIP.y - p1MIP.y) * (p_endMIP.y - p1MIP.y)), usedDistanceThresholdMIP / (float)texSize);

            Debug.Log("MIP: Automatic angle in degrees: " + visAngleMIP);

            //Slider slider = colocSliders[(int)ColocMatProperties.Angle].GetComponent<Slider>();
            //String sliderText = slider.GetComponentInChildren<UnityEngine.UI.Text>().text;
            //slider.value = visAngleMIP;
            //slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0:0}", slider.value);
        }
        //END OF AUTOMATIC ANGLE
        */

        try
        {
            string pathScat = Application.persistentDataPath + "/Results/Scatter/";
            if (!Directory.Exists(pathScat))
                Directory.CreateDirectory(pathScat);
            System.IO.File.WriteAllText(pathScat + "regLineMIP_thresholds_" + Time.time + ".txt", "max_MIP = " + Cvalue + " p_endMIP = [" + p_endMIP.x + ", " + p_endMIP.y + "]  distThreshMIP = " + usedDistanceThresholdMIP);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

        //ColocFrequencyScatterToTexture2DBusy = false;
        ScatterToTexture(texSize, freqScatterColors2D, (int)ScatterPlotTypes.FrequencyScatter, label);
    }

    public void CaptureView(string type)
    {
        resultsCamera.enabled = true;
        resultsCamera.GetComponent<RenderTextureForCamera>().updateRenderTexture(1024);
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = resultsCamera.targetTexture;

        resultsCamera.Render();
        Texture2D image = new Texture2D(resultsCamera.targetTexture.width, resultsCamera.targetTexture.height);
        image.ReadPixels(new Rect(0, 0, resultsCamera.targetTexture.width, resultsCamera.targetTexture.height), 0, 0);
        image.Apply();

        // Encode texture into PNG
        byte[] bytes = image.EncodeToPNG();
        try
        {
            string path = Application.persistentDataPath + "/Results/Camera/";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string filePath = path + type + Time.time + ".png";
            //string filePath = "I:/Master's results/Results/Camera/cameraOutput_" + 45f * (i - 1) + "_" + Time.time + ".png";
            System.IO.File.WriteAllBytes(filePath, bytes);
            Debug.Log("Wrote current camera output to " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
       

        RenderTexture.active = currentRT;
        resultsCamera.enabled = false;
    }

    public void ExportMIPButtonPressed()
    {
        StartCoroutine(GenerateCompleteMIP_v2(onlyInsideROI));
    }

    #endregion


    public void Export3DButtonPressed()
    {
        int size = 1024;
        
        screenshotSample(size, new bool[] { true, true, true, true }, (int)ColocMethod.NoColoc);
        screenshotSample(size, new bool[] { true, true, true, true }, (int)ColocMethod.OverlayWhite);

        // separate channels by themselves
        //screenshotSample(size, new bool[] { colocChannel0 == 0, colocChannel0 == 1, colocChannel0 == 2, colocChannel0 == 3 }, (int)ColocMethod.NoColoc);
        //screenshotSample(size, new bool[] { colocChannel1 == 0, colocChannel1 == 1, colocChannel1 == 2, colocChannel1 == 3 }, (int)ColocMethod.NoColoc);

        //screenshotSample(size, new bool[] { true, true, true, true }, (int)ColocMethod.OnlyColocWhite);
        //screenshotSample(size, new bool[] { colocChannel0 == 0 || colocChannel1 == 0, colocChannel0 == 1 || colocChannel1 == 1, colocChannel0 == 2 || colocChannel1 == 2, colocChannel0 == 3 || colocChannel1 == 3 }, (int)ColocMethod.OverlayWhite);
        //screenshotSample(size, new bool[] { true, true, true, true }, (int)ColocMethod.OverlayWhite);
        //screenshotSample(size, new bool[] { true, true, true, true }, (int)ColocMethod.OnlyColocHeatmap);
        //screenshotSample(size, new bool[] { true, true, true, true }, (int)ColocMethod.OnlyColocNMDP);

        //screenshotSample(size, new bool[] { true, true, true, true }, (int)ColocMethod.OnlyColocNMDPSobel);

        String path = Application.persistentDataPath + "/Results/Camera/" + size;
        Debug.Log("Finished exporting 3D: " + path);
        // Automatically open path
        /*
        if (!BenProject)
        {
            System.Diagnostics.Process.Start(path);

        }
        */
    }

    void screenshotSample(int texSize, bool[] channelsToSave, int colocMethod = (int)ColocMethod.NoColoc, bool exportTotal = false)
    {
        float initRedOpacity = internalMat.GetFloat("_redOpacity");
        float initGreenOpacity = internalMat.GetFloat("_greenOpacity");
        float initBlueOpacity = internalMat.GetFloat("_blueOpacity");
        float initPurpleOpacity = internalMat.GetFloat("_purpleOpacity");

        string showChannelsString = "Ch_";
        if (colocMethod == (int)ColocMethod.NoColoc)
        {
            internalMat.SetFloat("_redOpacity", channelsToSave[0] ? initRedOpacity : 0.0f);
            internalMat.SetFloat("_greenOpacity", channelsToSave[1] ? initGreenOpacity : 0.0f);
            internalMat.SetFloat("_blueOpacity", channelsToSave[2] ? initBlueOpacity : 0.0f);
            internalMat.SetFloat("_purpleOpacity", channelsToSave[3] ? initPurpleOpacity : 0.0f);

            internalMat.SetInt("_colocalizationMethod", colocMethod);
            mipMat.SetInt("_colocalizationMethod", colocMethod);

            for (int i = 0; i < channelsToSave.Length; i++)
            {
                if (channelsToSave[i])
                {
                    switch (i)
                    {
                        case 0: // red
                            showChannelsString += "red";
                            break;
                        case 1: // green
                            showChannelsString += "green";
                            break;
                        case 2: // blue
                            showChannelsString += "blue";
                            break;
                        case 3: // purple
                            showChannelsString += "purple";
                            break;
                    }
                }
            }
        }
        else
        {
            internalMat.SetFloat("_redOpacity", 1.0f);
            internalMat.SetFloat("_greenOpacity", 1.0f);
            internalMat.SetFloat("_blueOpacity", 1.0f);
            internalMat.SetFloat("_purpleOpacity", 1.0f);

            internalMat.SetInt("_colocalizationMethod", colocMethod);
            mipMat.SetInt("_colocalizationMethod", colocMethod);

            showChannelsString += "coloc_" + colocMethod + "max" + (int)(maxHeatmapValue);
        }

        // save and reset rotation
        Quaternion initalRotation = boundingBox.transform.localRotation;
        boundingBox.transform.localRotation = Quaternion.identity;

        Vector3 initialScale = boundingBox.transform.localScale;
        if(initialScale.x > initialScale.y)
            boundingBox.transform.localScale = new Vector3(1f, initialScale.y/ initialScale.x, initialScale.z / initialScale.x);
        else
            boundingBox.transform.localScale = new Vector3(initialScale.x / initialScale.y, 1f, initialScale.z / initialScale.y);

        // update the proxy geometry based on the results camera
        Matrix4x4 rotationMat = Matrix4x4.TRS(Vector3.zero, boundingBoxSetup.transform.rotation, Vector3.one);
        viewDir = rotationMat.inverse * (-resultsCamera.transform.forward);
        CalculateProxyGeometry();

        resultsCamera.enabled = true;
        resultsCamera.GetComponent<RenderTextureForCamera>().updateRenderTexture(texSize);
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = resultsCamera.targetTexture;

        List<Color> totalImage = new List<Color>();

        //int rotationSteps = 5;

        float[] imageAngles = {-30f, 0f, 30f, 60f, 90f };//{ 0f, 30f, 45f, 60f, 90f };

        for (int i = 0; i < imageAngles.Length; i++)
        {
            boundingBox.transform.localRotation = Quaternion.Euler(0f, imageAngles[i], 0f); // normal
            //boundingBox.transform.localRotation = Quaternion.Euler(Math.Abs(imageAngles[i]), imageAngles[i], -90f); //  for the MEL paper for the OLD sample 2, to rotate on its side
            //boundingBox.transform.localRotation = Quaternion.Euler(Math.Abs(imageAngles[i]), imageAngles[i], 0f); // for the MEL paper
            //boundingBox.transform.localRotation = Quaternion.Euler(0f, 0f, 0f); // for cylinder-cylinder rotate bounding box anchor with x: 0 y: -90 z: 60 using the editor (inspector)

            resultsCamera.Render();
            Texture2D image = new Texture2D(resultsCamera.targetTexture.width, resultsCamera.targetTexture.height);
            image.ReadPixels(new Rect(0, 0, resultsCamera.targetTexture.width, resultsCamera.targetTexture.height), 0, 0);
            image.Apply();
            Color[] pixels = image.GetPixels();
            pixels = RotateMatrix(pixels, image.width);
            if (exportTotal)
            {
                foreach (var p in pixels)
                {
                    totalImage.Add(p);
                }
            }

            // Encode texture into PNG
            byte[] bytes = image.EncodeToPNG();
            try
            {
                string path = Application.persistentDataPath + "/Results/Camera/" + texSize + "/";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                string filePath = path + "cameraOutput_" + imageAngles[i] + "_" + showChannelsString + "_" + Time.time + ".png";
                if (BenProject)
                {
                    filePath = saveLocation3D + thisImageFolderStructure + "cameraOutput_" + imageAngles[i] + "_" + showChannelsString + "_" + Time.time + ".png";
                }
                //string filePath = "I:/Master's results/Results/Camera/cameraOutput_" + 45f * (i - 1) + "_" + Time.time + ".png";
                System.IO.File.WriteAllBytes(filePath, bytes);
                //Debug.Log("Wrote current camera output to " + filePath);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
           

            // update the proxy geometry based on the results camera
            rotationMat = Matrix4x4.TRS(Vector3.zero, boundingBoxSetup.transform.rotation, Vector3.one);
            viewDir = rotationMat.inverse * (-resultsCamera.transform.forward);
            CalculateProxyGeometry();
        }

        if (exportTotal)
        {
            Texture2D totalImageTex = new Texture2D(resultsCamera.targetTexture.width, resultsCamera.targetTexture.height * imageAngles.Length);
            totalImageTex.wrapMode = TextureWrapMode.Clamp;
            totalImageTex.SetPixels(totalImage.ToArray());
            totalImageTex.Apply();

            // Encode texture into PNG
            byte[] totalBytes = totalImageTex.EncodeToPNG();
            try
            {
                string path = Application.persistentDataPath + "/Results/Camera/" + texSize + "/";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                string fPath = path + "TotalImage" + "_" + showChannelsString + Time.time + ".png";
                //string filePath = "I:/Master's results/Results/Camera/cameraOutput_" + 45f * (i - 1) + "_" + Time.time + ".png";
                System.IO.File.WriteAllBytes(fPath, totalBytes);

            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
       
        boundingBox.transform.localRotation = initalRotation;
        RenderTexture.active = currentRT;
        boundingBox.transform.localScale = initialScale;
        resultsCamera.enabled = false;


        internalMat.SetFloat("_redOpacity", initRedOpacity);
        internalMat.SetFloat("_greenOpacity", initGreenOpacity);
        internalMat.SetFloat("_blueOpacity", initPurpleOpacity);
        internalMat.SetFloat("_purpleOpacity", initPurpleOpacity);


        internalMat.SetInt("_colocalizationMethod", colocalizationMethod);
        mipMat.SetInt("_colocalizationMethod", colocalizationMethod);
    }

    static Color[] RotateMatrix(Color[] matrix, int n)
    {
        Color[] ret = new Color[n * n];

        for (int i = 0; i < n; ++i)
        {
            for (int j = 0; j < n; ++j)
            {
                ret[i * n + j] = matrix[(n - j - 1) * n + i];
            }
        }

        return ret;
    }

    //main slicing function
    void CalculateProxyGeometry()
    {
        // verify that num_slices is in the valid bounds
        if (num_slices > max_slices)
            num_slices = max_slices;
        else if (num_slices < 2)
            num_slices = 2;

        //reset the list
        proxyTriangles = new List<int[]>(); // 4 triangles in the triangle fan with 3 vertices each

        //get the max and min distance of each vertex of the unit cube
        //in the viewing direction
        //(Rensu: In other words, get the vertex that's furthest away)
        float max_dist = Vector3.Dot(viewDir, boxVertices[0]);
        float min_dist = max_dist; // (Rensu: set to temp max so that the for loop can determine the min)
        int max_index = 0;

        // loop counter to copy volume slice vertices to proxyVertices
        int vertexCount = 0;

        // (Rensu: determine the min and max distance (vertex) and the vertex that is the furthest away)
        for (int i = 1; i < 8; i++)
        {
            //get the distance between the current unit cube vertex and the view vector by dot product
            float dist = Vector3.Dot(viewDir, boxVertices[i]);

            //if distance is > max_dist, store the value and index (Rensu: store the vertex that is furthest away in max_index)
            if (dist > max_dist)
            {
                max_dist = dist;
                max_index = i;
            }

            //if distance is < min_dist, store the value  (Rensu: store the the distance to the vertex that is the closest)
            if (dist < min_dist)
                min_dist = dist;
        }


        //expand it a little bit (Rensu: make provision for floating point rounding errors)
        min_dist -= EPSILON;
        max_dist += EPSILON;

        //local variables to store the start, direction vectors, lambda intersection values
        Vector3[] vecStart = new Vector3[12];
        Vector3[] vecDir = new Vector3[12];
        float[] lambda = new float[12];
        float[] lambda_inc = new float[12];
        float denom = 0;

        //set the minimum distance as the plane_dist subtract the max and min distances and divide by the 
        //total number of slices to get the plane increment
        //(Rensu: In other words fit num_slices in between the start and end vertex
        float plane_dist = min_dist;
        float plane_dist_inc = (max_dist - min_dist) / (float)num_slices;

        //for all edges (Rensu: loop through all the edges in the cube and test for valid intersections i.e. lambda = [0, 1])
        for (int i = 0; i < 12; i++)
        {
            //get the start position vertex by table lookup (Rensu Note: this is back-to-front order! so if v2 is front then v4 will be in vecStart)
            vecStart[i] = boxVertices[boundingBoxSetup.edges[edgeList[max_index, i], 0]];

            //get the direction by table lookup (Rensu: get the direction vector ei->j from the start vertex)
            vecDir[i] = boxVertices[boundingBoxSetup.edges[edgeList[max_index, i], 1]] - vecStart[i];

            //do a dot of vecDir with the view direction vector (Rensu: calculate the denominator term for the lambda formula)
            denom = Vector3.Dot(vecDir[i], viewDir);

            //determine the plane intersection parameter (lambda) and plane intersection parameter increment (lambda_inc)
            //if (1.0 + denom != 1.0) 
            //(Rensu: lambda_inc is actually an increment of the d variable in the lambda expression, to move the plane forward
            // and is the amount that the plane will move on that specific edge between slices)
            //(Rens: Lambda is the starting point of the plane on that edge)
            if (denom != 0.0f)
            {
                lambda_inc[i] = plane_dist_inc / denom;
                lambda[i] = (plane_dist - Vector3.Dot(vecStart[i], viewDir)) / denom;
            }
            else
            {  //(Rensu: The lambda is invalid therefore no intersection with this edge was found. Or co-planer? do research)
                lambda[i] = -1.0f;
                lambda_inc[i] = 0.0f;
            }
        }

        //local variables to store the intesected points (Rensu: between the plane and the edge) note that for a plane and 
        //sub intersection, we can have a minimum of 3 and a maximum of 6 vertex polygon
        Vector3[] intersection = new Vector3[6];
        float[] dL = new float[12]; // (Rensu: the lambda positions on each edge (remember if lamda was invalid dL[e] will be = -1)

        //loop through all the slices (Rensu: ...in back to front order)
        //for (int i = num_slices - 1; i >= 0; i--)
        for (int i = 0; i < num_slices; i++)
        {
            //determine the lambda value for all edges (Rensu: ...for all the slices)
            for (int e = 0; e < 12; e++)
            {
                dL[e] = lambda[e] + i * lambda_inc[e];
            }

            //if the values are between 0-1, we have an intersection at the current edge repeat the same for all 12 edges
            // (Rensu: I suppose this could also be simply a test for dl[e] != -1, but this seems a bit safer)

            // Rensu:
            // test if there is an intersection on one of the three main paths and add that intersection
            // (if there is a plane that intersects the cube there will ALWAYS be an intersection with all three main paths)
            // Path 1
            if ((dL[0] >= 0.0f) && (dL[0] < 1.0f))
            {
                intersection[0] = vecStart[0] + dL[0] * vecDir[0];
            }
            else if ((dL[1] >= 0.0f) && (dL[1] < 1.0f))
            {
                intersection[0] = vecStart[1] + dL[1] * vecDir[1];
            }
            else if ((dL[3] >= 0.0f) && (dL[3] < 1.0f))
            {
                intersection[0] = vecStart[3] + dL[3] * vecDir[3];
            }
            else
                continue; // (Rensu: the slice plane does not intersect the cube so continue)

            // (Rensu: if there is an edge on the dotted line, else just default to one of the edges on the main path (thus duplicate point))
            // Dotted line 1
            if ((dL[2] >= 0.0f) && (dL[2] < 1.0f))
            {
                intersection[1] = vecStart[2] + dL[2] * vecDir[2];
            }
            else if ((dL[0] >= 0.0f) && (dL[0] < 1.0f))
            {
                intersection[1] = vecStart[0] + dL[0] * vecDir[0];
            }
            else if ((dL[1] >= 0.0f) && (dL[1] < 1.0f))
            {
                intersection[1] = vecStart[1] + dL[1] * vecDir[1];
            }
            else
            {
                intersection[1] = vecStart[3] + dL[3] * vecDir[3];
            }

            // Path 2
            if ((dL[4] >= 0.0f) && (dL[4] < 1.0f))
            {
                intersection[2] = vecStart[4] + dL[4] * vecDir[4];
            }
            else if ((dL[5] >= 0.0f) && (dL[5] < 1.0f))
            {
                intersection[2] = vecStart[5] + dL[5] * vecDir[5];
            }
            else
            {
                intersection[2] = vecStart[7] + dL[7] * vecDir[7];
            }

            // (Rensu: if there is an edge on the dotted line, else just default to one of the edges on the main path (thus duplicate point))
            // Dotted line 2
            if ((dL[6] >= 0.0f) && (dL[6] < 1.0f))
            {
                intersection[3] = vecStart[6] + dL[6] * vecDir[6];
            }
            else if ((dL[4] >= 0.0f) && (dL[4] < 1.0f))
            {
                intersection[3] = vecStart[4] + dL[4] * vecDir[4];
            }
            else if ((dL[5] >= 0.0f) && (dL[5] < 1.0f))
            {
                intersection[3] = vecStart[5] + dL[5] * vecDir[5];
            }
            else
            {
                intersection[3] = vecStart[7] + dL[7] * vecDir[7];
            }

            // Path 3
            if ((dL[8] >= 0.0f) && (dL[8] < 1.0f))
            {
                intersection[4] = vecStart[8] + dL[8] * vecDir[8];
            }
            else if ((dL[9] >= 0.0f) && (dL[9] < 1.0f))
            {
                intersection[4] = vecStart[9] + dL[9] * vecDir[9];
            }
            else
            {
                intersection[4] = vecStart[11] + dL[11] * vecDir[11];
            }

            // (Rensu: if there is an edge on the dotted line, else just default to one of the edges on the main path (thus duplicate point))
            // Dotted line 3
            if ((dL[10] >= 0.0f) && (dL[10] < 1.0f))
            {
                intersection[5] = vecStart[10] + dL[10] * vecDir[10];
            }
            else if ((dL[8] >= 0.0f) && (dL[8] < 1.0f))
            {
                intersection[5] = vecStart[8] + dL[8] * vecDir[8];
            }
            else if ((dL[9] >= 0.0f) && (dL[9] < 1.0f))
            {
                intersection[5] = vecStart[9] + dL[9] * vecDir[9];
            }
            else
            {
                intersection[5] = vecStart[11] + dL[11] * vecDir[11];
            }

            //after all 6 possible intersection vertices are obtained, we calculated the proper polygon indices by using indices of a triangular fan
            int[] indices = new int[] { 0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 5 };

            //setting up the triangles, 12 = 4 triangles times 3 vertices per triangle
            int[] triangleFan = new int[12];
            for (int k = 0; k < 12; k++)
            {
                triangleFan[k] = indices[k] + vertexCount;
            }
            proxyTriangles.Add(triangleFan);

            //Using the indices, pass the intersection vertices to the vTextureSlices vector
            for (int k = 0; k < 6; k++)
            {
                proxyVertices[vertexCount++] = intersection[k];
            }
        }

        for (int i = vertexCount; i < max_slices * 6; i++)
        {
            proxyVertices[i] = new Vector3(0.0f, 0.0f, 0.0f);
        }
        /*
		// update the mesh triangles
		for (int i = 0; i < proxyTriangles.Count; i++) 
		{
			proxyMesh.SetTriangles (proxyTriangles [i], i);
		}
		*/
        List<int> proxyTriangleList = new List<int>();
        for (int i = 0; i < proxyTriangles.Count; i++)
        {
            for (int index = 0; index < proxyTriangles[i].Length; index++)
                proxyTriangleList.Add(proxyTriangles[i][index]);
        }
        proxyMesh.SetTriangles(proxyTriangleList, 0);
        proxyMesh.vertices = proxyVertices;

        proxyGeom.transform.position = boundingBoxSetup.transform.position;
        proxyGeom.transform.rotation = boundingBoxSetup.transform.rotation;
        proxyGeom.transform.localScale = Vector3.one; //boundingBoxSetup.transform.localScale;
                                                      //proxyMesh.RecalculateBounds ();
                                                      //proxyMesh.RecalculateNormals ();

    }



    //RayCasting functions

    public void changeMenu(Material newMat)
    {
        //internalMat = newMat;

        // change the current cancvas that is displayed
        if (mainMenu.activeInHierarchy) // change to iso menu
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
            
            volumeVizTypeDropdown.GetComponent<Dropdown>().value = (int)VolumeRenderingType.RayCasting;
            renderType = (int)VolumeRenderingType.RayCasting;

            Start();
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

    IEnumerator autoRunAnalysis()
    {
        yield return new WaitForSeconds(1.0f);

        if (mainMenu.activeInHierarchy) // change to coloc menu
        {
            colocalizationActive = true;

            mainMenu.SetActive(false);
            colocMenu.SetActive(true);

            // show colocalization
            colocalizationMethod = colocMethodDropdown.GetComponent<Dropdown>().value;
            internalMat.SetInt("_colocalizationMethod", colocalizationMethod);
            mipMat.SetInt("_colocalizationMethod", colocalizationMethod);

        }
        yield return null;
        calcColocButtonPressed();
        yield return null;
       // Export3DButtonPressed();                
    }
}