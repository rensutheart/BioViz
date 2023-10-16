using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TextureSlicing : VolumeVisualizer 
{
    /*
	public int num_slices = 200;

	private GameObject proxyGeom;
	private MeshFilter mf;
	private MeshRenderer mr;
	private Mesh proxyMesh;
	// these attributes are used to create the proxyMesh
	private Vector3[] proxyVertices;
	private List<int[]> proxyTriangles;

	private Vector3 viewDir;	// the camera's view direction

	//(Rensu: The independant paths along the edges depending on which vertex is in the front)
	// (index 0, 1, 3 is path index 2 (6 and 10) is the "dotted line" which is another possible edge to determine the intersection for, etc)
	private int[,] edgeList;

	private const float EPSILON = 0.0001f; //for floating point inaccuracy
	private int max_slices = 1000;	// the maximum allowable number of slices

	private enum MaterialProperty {NumSlices, FilterThresh, Opacity, RedOpacity, GreenOpacity, BlueOpacity, PurpleOpacity, SliceSelect};

	//private int frameCount = 0;	// the frame count, to ensure proxy geometry is not calculated every frame (Quality setting to reduce CPU load)

	void Start()
	{
        Debug.Log("USING TEXTURE SLICING");
		InitializeSuper((int)VolumeRenderingType.TextureSlicing);

		// create an object of the mainCamera and get viewDir (TODO: I can actually remove the mainCamera line, since it happens in VolumeVisualizer)
		mainCamera = GameObject.FindGameObjectWithTag ("MainCamera");
		viewDir = -mainCamera.transform.forward;

		// verify that num_slices is in the valid bounds
		if (num_slices > max_slices)
			num_slices = max_slices;
		else if (num_slices < 2)
			num_slices = 2;

		proxyGeom = new GameObject ("Proxy Geometry");
		proxyMesh = new Mesh ();
		proxyGeom.transform.SetParent(boundingBox.transform);

		mf = proxyGeom.AddComponent (typeof(MeshFilter)) as MeshFilter;
		mf.mesh = proxyMesh;

		mr = proxyGeom.AddComponent (typeof(MeshRenderer)) as MeshRenderer;

		mr.material = internalMat;
		mr.receiveShadows = false;
		mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
		mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
		mr.material = internalMat;


		// initialize the proxyVertices and Triangels
		proxyVertices = new Vector3[max_slices * 6]; // 6 vertices per slice
		proxyTriangles = new List<int[]>();	// 4 triangles in the triangle fan with 3 vertices each

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

		CalculateProxyGeometry ();

		proxyMesh.RecalculateBounds ();
		proxyMesh.RecalculateNormals ();

		//Set the material properties to be the same as the slider's defaults
		for(int i = 0; i < guiSliders.Length; i++)
		{
			currentSliderNum = i;
            // non important initalizing code
            if(i == (int)MaterialProperty.FilterThresh)
            {
                Slider slider = guiSliders[i].GetComponent<Slider>();
                slider.value = setThreshold;
                //Debug.Log(slider.value);
                string sliderText = slider.GetComponentInChildren<UnityEngine.UI.Text>().text;
                slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0:0.000}", slider.value);
            }
			changeMatProperty(i);
		}
		currentSliderNum = -1;
	}

	void Update()
	{
		UpdateSuper();

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
			CalculateProxyGeometry ();
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
    }

    public void Export3DButtonPressed()
    {
        screenshotSample(2048, new bool[] { true, true, true, true }, (int)ColocMethod.NoColoc);

        switch (colocChannel0)
        {
            case 0: // red
                screenshotSample(2048, new bool[] { true, false, false, false }, (int)ColocMethod.NoColoc);
                break;
            case 1: // green
                screenshotSample(2048, new bool[] { false, true, false, false }, (int)ColocMethod.NoColoc);
                break;
            case 2: // blue
                screenshotSample(2048, new bool[] { false, false, true, false }, (int)ColocMethod.NoColoc);
                break;
            case 3: // purple
                screenshotSample(2048, new bool[] { false, false, false, true }, (int)ColocMethod.NoColoc);
                break;
        }
        switch (colocChannel1)
        {
            case 0: // red
                screenshotSample(2048, new bool[] { true, false, false, false }, (int)ColocMethod.NoColoc);
                break;
            case 1: // green
                screenshotSample(2048, new bool[] { false, true, false, false }, (int)ColocMethod.NoColoc);
                break;
            case 2: // blue
                screenshotSample(2048, new bool[] { false, false, true, false }, (int)ColocMethod.NoColoc);
                break;
            case 3: // purple
                screenshotSample(2048, new bool[] { false, false, false, true }, (int)ColocMethod.NoColoc);
                break;
        }



        screenshotSample(2048, new bool[] { true, true, true, true }, (int)ColocMethod.OnlyColocWhite);
        //screenshotSample(2048, new bool[] { true, true, true, true }, 10);
        screenshotSample(2048, new bool[] { true, true, true, true }, (int)ColocMethod.OnlyColocHeatmap);
        screenshotSample(2048, new bool[] { true, true, true, true }, (int)ColocMethod.OnlyColocNMDP);


        Debug.Log("Finished exporting 3D");
    }

    void screenshotSample(int texSize,  bool[] channelsToSave, int colocMethod = (int)ColocMethod.NoColoc)
    {
        string showChannelsString = "Ch_";
        if (colocMethod == (int)ColocMethod.NoColoc)
        {
            internalMat.SetFloat("_redOpacity", channelsToSave[0] ? 1.0f : 0.0f);
            internalMat.SetFloat("_greenOpacity", channelsToSave[1] ? 1.0f : 0.0f);
            internalMat.SetFloat("_blueOpacity", channelsToSave[2] ? 1.0f : 0.0f);
            internalMat.SetFloat("_purpleOpacity", channelsToSave[3] ? 1.0f : 0.0f);

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

        // update the proxy geometry based on the results camera
        Matrix4x4 rotationMat = Matrix4x4.TRS(Vector3.zero, boundingBoxSetup.transform.rotation, Vector3.one);
        viewDir = rotationMat.inverse * (-resultsCamera.transform.forward);
        CalculateProxyGeometry();

        resultsCamera.enabled = true;
        resultsCamera.GetComponent<RenderTextureForCamera>().updateRenderTexture(texSize);
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = resultsCamera.targetTexture;

        List<Color> totalImage = new List<Color>();

        int rotationSteps = 5;

        for (int i = 1; i <= rotationSteps; i++)
        {
            resultsCamera.Render();
            Texture2D image = new Texture2D(resultsCamera.targetTexture.width, resultsCamera.targetTexture.height);
            image.ReadPixels(new Rect(0, 0, resultsCamera.targetTexture.width, resultsCamera.targetTexture.height), 0, 0);            
            image.Apply();
            Color[] pixels = image.GetPixels();
            pixels = RotateMatrix(pixels, image.width);
            foreach (var p in pixels)
            {
                totalImage.Add(p);
            }

            // Encode texture into PNG
            byte[] bytes = image.EncodeToPNG();
            string filePath = Application.persistentDataPath + "/Results/Camera/" + texSize + "/cameraOutput_" + 45f * (i - 1) + "_" +  showChannelsString + "_" + Time.time + ".png";
            //string filePath = "I:/Master's results/Results/Camera/cameraOutput_" + 45f * (i - 1) + "_" + Time.time + ".png";
            System.IO.File.WriteAllBytes(filePath, bytes);
            Debug.Log("Wrote current camera output to " + filePath);

            boundingBox.transform.localRotation = Quaternion.Euler(0f, 45f * i, 0f);

            // update the proxy geometry based on the results camera
            rotationMat = Matrix4x4.TRS(Vector3.zero, boundingBoxSetup.transform.rotation, Vector3.one);
            viewDir = rotationMat.inverse * (-resultsCamera.transform.forward);
            CalculateProxyGeometry();
        }

        Texture2D totalImageTex = new Texture2D(resultsCamera.targetTexture.width, resultsCamera.targetTexture.height * rotationSteps);
        totalImageTex.wrapMode = TextureWrapMode.Clamp;
        totalImageTex.SetPixels(totalImage.ToArray());
        totalImageTex.Apply();

        // Encode texture into PNG
        byte[] totalBytes = totalImageTex.EncodeToPNG();
        string fPath = Application.persistentDataPath + "/Results/Camera/" + texSize + "/TotalImage" + "_" + showChannelsString + Time.time + ".png";
        //string filePath = "I:/Master's results/Results/Camera/cameraOutput_" + 45f * (i - 1) + "_" + Time.time + ".png";
        System.IO.File.WriteAllBytes(fPath, totalBytes);

        boundingBox.transform.localRotation = initalRotation;
        RenderTexture.active = currentRT;
        resultsCamera.enabled = false;


        internalMat.SetFloat("_redOpacity", 1.0f);
        internalMat.SetFloat("_greenOpacity", 1.0f);
        internalMat.SetFloat("_blueOpacity", 1.0f);
        internalMat.SetFloat("_purpleOpacity", 1.0f);


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
		proxyTriangles = new List<int[]>();	// 4 triangles in the triangle fan with 3 vertices each

		//get the max and min distance of each vertex of the unit cube
		//in the viewing direction
		//(Rensu: In other words, get the vertex that's furthest away)
		float max_dist = Vector3.Dot (viewDir, boxVertices [0]);
		float min_dist = max_dist; // (Rensu: set to temp max so that the for loop can determine the min)
		int max_index = 0;

		// loop counter to copy volume slice vertices to proxyVertices
		int vertexCount = 0;

		// (Rensu: determine the min and max distance (vertex) and the vertex that is the furthest away)
		for (int i = 1; i < 8; i++) {
			//get the distance between the current unit cube vertex and the view vector by dot product
			float dist = Vector3.Dot (viewDir, boxVertices [i]);

			//if distance is > max_dist, store the value and index (Rensu: store the vertex that is furthest away in max_index)
			if (dist > max_dist) {
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
		for (int i = 0; i < 12; i++) {
			//get the start position vertex by table lookup (Rensu Note: this is back-to-front order! so if v2 is front then v4 will be in vecStart)
			vecStart [i] = boxVertices [boundingBoxSetup.edges [edgeList [max_index, i], 0]];

			//get the direction by table lookup (Rensu: get the direction vector ei->j from the start vertex)
			vecDir [i] = boxVertices [boundingBoxSetup.edges [edgeList [max_index, i], 1]] - vecStart [i];

			//do a dot of vecDir with the view direction vector (Rensu: calculate the denominator term for the lambda formula)
			denom = Vector3.Dot (vecDir [i], viewDir);

			//determine the plane intersection parameter (lambda) and plane intersection parameter increment (lambda_inc)
			//if (1.0 + denom != 1.0) 
			//(Rensu: lambda_inc is actually an increment of the d variable in the lambda expression, to move the plane forward
			// and is the amount that the plane will move on that specific edge between slices)
			//(Rens: Lambda is the starting point of the plane on that edge)
			if (denom != 0.0f) {
				lambda_inc [i] = plane_dist_inc / denom;
				lambda [i] = (plane_dist - Vector3.Dot (vecStart [i], viewDir)) / denom;
			} else {  //(Rensu: The lambda is invalid therefore no intersection with this edge was found. Or co-planer? do research)
				lambda [i] = -1.0f;
				lambda_inc [i] = 0.0f;
			}
		}

		//local variables to store the intesected points (Rensu: between the plane and the edge) note that for a plane and 
		//sub intersection, we can have a minimum of 3 and a maximum of 6 vertex polygon
		Vector3[] intersection = new Vector3[6];
		float[] dL = new float[12]; // (Rensu: the lambda positions on each edge (remember if lamda was invalid dL[e] will be = -1)

		//loop through all the slices (Rensu: ...in back to front order)
		//for (int i = num_slices - 1; i >= 0; i--)
		for (int i = 0; i < num_slices; i++) {
			//determine the lambda value for all edges (Rensu: ...for all the slices)
			for (int e = 0; e < 12; e++) {
				dL [e] = lambda [e] + i * lambda_inc [e];
			}

			//if the values are between 0-1, we have an intersection at the current edge repeat the same for all 12 edges
			// (Rensu: I suppose this could also be simply a test for dl[e] != -1, but this seems a bit safer)

			// Rensu:
			// test if there is an intersection on one of the three main paths and add that intersection
			// (if there is a plane that intersects the cube there will ALWAYS be an intersection with all three main paths)
			// Path 1
			if ((dL [0] >= 0.0f) && (dL [0] < 1.0f)) {
				intersection [0] = vecStart [0] + dL [0] * vecDir [0];
			} else if ((dL [1] >= 0.0f) && (dL [1] < 1.0f)) {
				intersection [0] = vecStart [1] + dL [1] * vecDir [1];
			} else if ((dL [3] >= 0.0f) && (dL [3] < 1.0f)) {
				intersection [0] = vecStart [3] + dL [3] * vecDir [3];
			} else
				continue; // (Rensu: the slice plane does not intersect the cube so continue)

			// (Rensu: if there is an edge on the dotted line, else just default to one of the edges on the main path (thus duplicate point))
			// Dotted line 1
			if ((dL [2] >= 0.0f) && (dL [2] < 1.0f)) {
				intersection [1] = vecStart [2] + dL [2] * vecDir [2];
			} else if ((dL [0] >= 0.0f) && (dL [0] < 1.0f)) {
				intersection [1] = vecStart [0] + dL [0] * vecDir [0];
			} else if ((dL [1] >= 0.0f) && (dL [1] < 1.0f)) {
				intersection [1] = vecStart [1] + dL [1] * vecDir [1];
			} else {
				intersection [1] = vecStart [3] + dL [3] * vecDir [3];
			}

			// Path 2
			if ((dL [4] >= 0.0f) && (dL [4] < 1.0f)) {
				intersection [2] = vecStart [4] + dL [4] * vecDir [4];
			} else if ((dL [5] >= 0.0f) && (dL [5] < 1.0f)) {
				intersection [2] = vecStart [5] + dL [5] * vecDir [5];
			} else {
				intersection [2] = vecStart [7] + dL [7] * vecDir [7];
			}

			// (Rensu: if there is an edge on the dotted line, else just default to one of the edges on the main path (thus duplicate point))
			// Dotted line 2
			if ((dL [6] >= 0.0f) && (dL [6] < 1.0f)) {
				intersection [3] = vecStart [6] + dL [6] * vecDir [6];
			} else if ((dL [4] >= 0.0f) && (dL [4] < 1.0f)) {
				intersection [3] = vecStart [4] + dL [4] * vecDir [4];
			} else if ((dL [5] >= 0.0f) && (dL [5] < 1.0f)) {
				intersection [3] = vecStart [5] + dL [5] * vecDir [5];
			} else {
				intersection [3] = vecStart [7] + dL [7] * vecDir [7];
			}

			// Path 3
			if ((dL [8] >= 0.0f) && (dL [8] < 1.0f)) {
				intersection [4] = vecStart [8] + dL [8] * vecDir [8];
			} else if ((dL [9] >= 0.0f) && (dL [9] < 1.0f)) {
				intersection [4] = vecStart [9] + dL [9] * vecDir [9];
			} else {
				intersection [4] = vecStart [11] + dL [11] * vecDir [11];
			}

			// (Rensu: if there is an edge on the dotted line, else just default to one of the edges on the main path (thus duplicate point))
			// Dotted line 3
			if ((dL [10] >= 0.0f) && (dL [10] < 1.0f)) {
				intersection [5] = vecStart [10] + dL [10] * vecDir [10];
			} else if ((dL [8] >= 0.0f) && (dL [8] < 1.0f)) {
				intersection [5] = vecStart [8] + dL [8] * vecDir [8];
			} else if ((dL [9] >= 0.0f) && (dL [9] < 1.0f)) {
				intersection [5] = vecStart [9] + dL [9] * vecDir [9];
			} else {
				intersection [5] = vecStart [11] + dL [11] * vecDir [11];
			}

			//after all 6 possible intersection vertices are obtained, we calculated the proper polygon indices by using indices of a triangular fan
			int[] indices = new int[]{ 0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 5 };

			//setting up the triangles, 12 = 4 triangles times 3 vertices per triangle
			int[] triangleFan = new int[12];
			for (int k = 0; k < 12; k++) {
				triangleFan [k] = indices [k] + vertexCount;
			}
			proxyTriangles.Add (triangleFan);

			//Using the indices, pass the intersection vertices to the vTextureSlices vector
			for (int k = 0; k < 6; k++) {
				proxyVertices [vertexCount++] = intersection [k];
			}
		}

		for (int i = vertexCount; i < max_slices * 6; i++) {
			proxyVertices [i] = new Vector3 (0.0f, 0.0f, 0.0f);
		}
		
		// update the mesh triangles
		//for (int i = 0; i < proxyTriangles.Count; i++) 
		//{
		//	proxyMesh.SetTriangles (proxyTriangles [i], i);
		//}
		
		List<int> proxyTriangleList = new List<int>();
		for (int i = 0; i < proxyTriangles.Count; i++) 
		{
			for(int index = 0; index < proxyTriangles[i].Length; index++)
				proxyTriangleList.Add(proxyTriangles[i][index]);
		}
		proxyMesh.SetTriangles (proxyTriangleList, 0);
		proxyMesh.vertices = proxyVertices;

		proxyGeom.transform.position = boundingBoxSetup.transform.position;
		proxyGeom.transform.rotation = boundingBoxSetup.transform.rotation;
		proxyGeom.transform.localScale = Vector3.one; //boundingBoxSetup.transform.localScale;
		//proxyMesh.RecalculateBounds ();
		//proxyMesh.RecalculateNormals ();

	}

	public void changeMatProperty(int sliderNum)
	{
		if(currentSliderNum == -1)
		{
			Debug.LogAssertion("No slider currently set, so can't update values (texture slicing)");
			return;
		}
		if(showMIP)
		{
			MIPChangeMatProperty(sliderNum);
			//return;
		}

		Slider slider = guiSliders[sliderNum].GetComponent<Slider>();

		//Debug.Log(slider.value);
		string sliderText = slider.GetComponentInChildren<UnityEngine.UI.Text>().text;
		slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0:0.000}", slider.value);

		switch(sliderNum)
		{
		case (int)MaterialProperty.NumSlices: //0
			num_slices = (int)slider.value;
			slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0}", slider.value);
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
        case (int)MaterialProperty.SliceSelect: //7
            slider.GetComponentInChildren<UnityEngine.UI.Text>().text = sliderText.Substring(0, sliderText.IndexOf("-") + 2) + string.Format("{0}", slider.value);
            SelectSlice((int)slider.value);
            break;
        }
	}*/
}