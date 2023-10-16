using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LoadSample : MonoBehaviour {
    public int sampleSize;
    public Material internalMaterial;

    public Material TextureSlicingMat;
    public Material RayCastingMat;
    public Material RayCastingIsoSurfaceMat;

    public Slider progressSlider;
    public RawImage[] sampleImages; //0=red 1=green 2=blue 3=purple

    public string sceneToLoad;

    [System.NonSerialized]
    public Vector3 sampleDim;   // the dimensions of the sample in micrometers (or any equivelant unit) (x length, y length, z increment)
    //[System.NonSerialized]
    public float zRatio = 0.3f;


    [System.NonSerialized]
    public int numChannels;  // the number of channels present
    [System.NonSerialized]
    public bool[] channelPresent;    // a flag that detrmines whether the channel in question is present
    [System.NonSerialized]
    public int[] channelDepth;       // the number of images in the given channel

    [System.NonSerialized]
    public Texture3D[] texSeparate;
    [System.NonSerialized]
    public int texDepth = 0;

    [System.NonSerialized]
    public bool doneLoadingSample = false;

    public bool isPreLoaded = false;

    public string samplePath = "";
    
    public LoadSample(Material intMat)
    {
        Debug.Log("LoadSample manually initialized");
        sampleSize = 1024;
        internalMaterial = intMat;
        texSeparate = new Texture3D[4];
        sampleDim = new Vector3(1f, 1f, 0.02f);
    }

    public LoadSample(int size, Material intMat, bool preLoaded = false)
    {
        Debug.Log("LoadSample manually initialized with parameters");
        sampleSize = closestPowerOfTwo(size);	// this is necessary for the textures that are used in Unity
        internalMaterial = intMat;
        texSeparate = new Texture3D[4];
        sampleDim = new Vector3(1f, 1f, 0.02f);
        isPreLoaded = preLoaded;
    }
    
    public void InitExistingSample()
    {
        Debug.Log("Initializing existing sample data");
        sampleSize = closestPowerOfTwo(sampleSize);	// this is necessary for the textures that are used in Unity
        texSeparate = new Texture3D[4];
        sampleDim = new Vector3(1f, 1f, 0.02f);
    }
    
    // Use this for initialization
    void Awake () {

        texSeparate = new Texture3D[4];
        Debug.Log(sceneToLoad + "'s LoadSample started");
    }
	
	// Update is called once per frame
	void Update () {
	
	}


    #region texture_setup
    /*
	public void Load3DTexture(string path)
	{
		string[] fileArray = Directory.GetFiles(path, "*.jpg");
		int texDepth = closestPowerOfTwo(fileArray.Length);
		Debug.Log("TexDepth: " + texDepth);

		Texture2D[] texArray = new Texture2D[texDepth];

		// fill the texture up so that it's length is a multiple of two
		if (fileArray.Length < texDepth) 
		{
			// create a filler texture
			Texture2D filler = new Texture2D (sampleSize, sampleSize, TextureFormat.ARGB32, true);
			for (int row = 0; row < sampleSize; row++) 
			{
				for (int column = 0; column < sampleSize; column++) 
				{
					filler.SetPixel(row, column, Color.black);
				}
			}
			filler.Apply ();

			int numFillSlices = texDepth - fileArray.Length;
			int slice = 0;
			for (int i = 0; i < numFillSlices / 2; i++) 
			{
				texArray [slice++] = filler;
			}
			for (int i = 0; i < fileArray.Length; i++) 
			{
				//Debug.Log (i + ": " + fileArray [i]);
				texArray [slice++] = LoadImage (fileArray[i]);
			}
			for (int i = slice; i < texDepth; i++) 
			{
				texArray [slice++] = filler;
			}
		}

		// create and initialize the 3D texture
		tex = new Texture3D (sampleSize, sampleSize,  texArray.Length, TextureFormat.ARGB32, false);
		tex.filterMode = FilterMode.Trilinear;
		tex.wrapMode = TextureWrapMode.Clamp;
		tex.anisoLevel = 0;

		// convert the 2D texture array to a 3D texture
		var cols = new Color[sampleSize * sampleSize * texArray.Length];
		int idx = 0;
		for (int z = 0; z < texArray.Length; ++z)
		{
			for (int y = 0; y < sampleSize; ++y)
			{
				for (int x = 0; x < sampleSize; ++x, ++idx)
				{
					cols [idx] = texArray[z].GetPixel(x, y);
				}
			}
		}

		tex.SetPixels (cols);
		//AssetDatabase.CreateAsset(tex, "Assets/Textures/3dTex.tex");

		tex.Apply ();
		internalMat.SetTexture ("_Volume", tex);
	}
	*/

    public void LoadSampleSeperate(string path, string fileType = "*.jpg")
    {
        // this is just to fix a rare case that happens due to the back button
        if (!path.EndsWith("\\"))
            path += "\\";

        Debug.Log("Path: " + path + "  Type: " + fileType);
        samplePath = path;
        string[] fileArray = Directory.GetFiles(path, fileType);
        //int texDepth = 0;
        channelDepth = new int[4] { 0, 0, 0, 0 };
        channelPresent = new bool[4] { false, false, false, false };
        numChannels = 0;


        //read sample dimensions
        string[] lines = new string[] { "line" };// = File.ReadAllLines(path + "sample_info.txt");


        if (lines.Length != 4)
        {
            Debug.Log("Incorrect number of lines in sample_info.txt");
            sampleDim = new Vector3(1f, 1f, 0.05f);
        }
        else
        {
            float x, y, z;//, n;
            if (float.TryParse(lines[0].Substring(lines[0].IndexOf("=") + 1), out x))
            {
                //   Debug.Log("x=" + x);
            }
            else
                Debug.Log("String (" + lines[0].Substring(lines[0].IndexOf("=") + 1) + ") could not be parsed.");

            if (float.TryParse(lines[1].Substring(lines[1].IndexOf("=") + 1), out y))
            {
                //   Debug.Log("y=" + y);
            }
            else
                Debug.Log("String (" + lines[1].Substring(lines[1].IndexOf("=") + 1) + ") could not be parsed.");

            if (float.TryParse(lines[2].Substring(lines[2].IndexOf("=") + 1), out z))
            {
             //   Debug.Log("z=" + z);
            }
            else
                Debug.Log("String (" + lines[2].Substring(lines[2].IndexOf("=") + 1) + ") could not be parsed.");

            /*
            if (float.TryParse(lines[3].Substring(lines[3].IndexOf("=") + 1), out n))
            {
                if (fileArray.Length != n)
                    Debug.LogError("The n length is not the same as the number of files " + n + " vs " + fileArray.Length);
            }
            else
                Debug.Log("String (" + lines[3].Substring(lines[3].IndexOf("=") + 1) + ") could not be parsed.");
                */
            if (x==0||y==0||z==0)
                sampleDim = new Vector3(1f, 1f, 0.05f);
            else
                sampleDim = new Vector3(x, y, z);
        }

        Debug.Log("The read sample dim is: " + sampleDim);

        //Debug.Log(fileArray.Length + " Images found");
        //texSeparte = new Texture3D[4];
        StartCoroutine(Load3DTextureSeparate(fileArray));
    }

    IEnumerator Load3DTextureSeparate(string[] fileArray)
    {
       // Debug.Log("In coroutine");
        doneLoadingSample = false;
        List<string>[] channelPaths = new List<string>[4];
        for (int c = 0; c < 4; c++)
            channelPaths[c] = new List<string>();

        // count the number of images in each channel
        foreach (string f in fileArray)
        {
            if (f.Substring(f.LastIndexOf("\\")).ToLower().Contains("red"))
            {
                channelPresent[0] = true;
                channelDepth[0]++;
                channelPaths[0].Add(f);
                 //Debug.Log("RED: " + f);
            }
            else if (f.Substring(f.LastIndexOf("\\")).ToLower().Contains("green"))
            {
                channelPresent[1] = true;
                channelDepth[1]++;
                channelPaths[1].Add(f);
                //Debug.Log("GREEN: " + f);
            }
            else if (f.Substring(f.LastIndexOf("\\")).ToLower().Contains("blue"))
            {
                channelPresent[2] = true;
                channelDepth[2]++;
                channelPaths[2].Add(f);
               // Debug.Log("BLUE: " + f);
            }
            else if (f.Substring(f.LastIndexOf("\\")).ToLower().Contains("purple"))
            {
                channelPresent[3] = true;
                channelDepth[3]++;
                channelPaths[3].Add(f);
               // Debug.Log("PURPLE: " + f);
            }
            else
            {
                Debug.Log("Not a valid image name: " + f);
            }
        }
        channelPaths[0].Sort();
        channelPaths[1].Sort();
        channelPaths[2].Sort();
        // count the total number of channels
        int maxDepth = 0;
        int count = 0;
        foreach (bool chan in channelPresent)
        {
            if (chan)
            {
                numChannels++;
            }

            maxDepth = Math.Max(maxDepth, channelDepth[count++]);
        }
        texDepth = closestPowerOfTwo(maxDepth);
        Debug.Log("TexDepth: " + texDepth);

        // create a filler texture (blank)
        Texture2D filler = new Texture2D(sampleSize, sampleSize, TextureFormat.RGB24, true); // ARGB32
        for (int row = 0; row < sampleSize; row++)
        {
            for (int column = 0; column < sampleSize; column++)
            {
                filler.SetPixel(row, column, Color.black);
            }
        }
        //filler.Apply ();

        if (progressSlider != null)
            progressSlider.value = 0.05f;
        yield return null;
        
        for (int chan = 0; chan < 4; chan++)
        {
            Texture2D[] texArray = new Texture2D[texDepth];

            //TODO: this assumes that red, green, blue, pruple are used and always in that order...
            //if (chan < numChannels)
            if(channelPresent[chan])
            {
                int currChanDepth = channelDepth[chan];

                //Debug.Log("Chan: " + chan + " chan depth: " + currChanDepth);
                // fill the texture up so that it's length is a multiple of two
                if (currChanDepth < texDepth)
                {
                    int numFillSlices = texDepth - currChanDepth;
                    int slice = 0;
                    for (int i = 0; i < numFillSlices / 2; i++)
                    {
                        texArray[slice++] = filler;
                    }
                    for (int i = 0; i < currChanDepth; i++)
                    {
                        //Debug.Log (i + ": " + fileArray [i]);
                        texArray[slice++] = LoadImage(channelPaths[chan][i]);


                        try
                        {
                            if (i == currChanDepth / 2)
                            {
                                sampleImages[chan].transform.gameObject.SetActive(true);
                                sampleImages[chan].texture = texArray[slice - 1];
                                //Debug.Log("Channel " + chan + " displaying sample image");
                            }
                        }
                        catch(System.IndexOutOfRangeException e)
                        {
                            Debug.LogWarning(e.Message);
                        }

                        if (progressSlider != null)
                            progressSlider.value = 0.05f + 0.8f * ((float)chan / 4) + 0.14f * ((float)(chan) / 4) + 0.2f * ((float)(slice+1) / texDepth);
                        yield return null;
                    }
                    for (int i = slice; i < texDepth; i++)
                    {
                        texArray[slice++] = filler;
                    }
                }
                else if (currChanDepth == texDepth)
                {
                    for (int i = 0; i < currChanDepth; i++)
                    {
                        //Debug.Log (i + ": " + fileArray [i]);
                        texArray[i] = LoadImage(channelPaths[chan][i]);

                        try
                        {
                            if (i == currChanDepth / 2)
                            {
                                sampleImages[chan].transform.gameObject.SetActive(true);
                                sampleImages[chan].texture = texArray[i - 1];
                                //Debug.Log("Channel " + chan + " displaying sample image");
                            }
                        }
                        catch (System.IndexOutOfRangeException e)
                        {
                            Debug.LogWarning(e.Message);
                        }

                        if (progressSlider != null)
                            progressSlider.value = 0.05f + 0.8f * ((float)chan / 4) + 0.14f * ((float)(chan) / 4) + 0.2f * ((float)(i+1) / texDepth);
                        yield return null;
                    }
                }
            }
            else // if channel not present make it blank
            {
                for (int i = 0; i < texDepth; i++)
                {
                    texArray[i] = filler;

                    if (progressSlider != null)
                        progressSlider.value = 0.05f + 0.8f * ((float)chan / 4) + 0.14f * ((float)(chan) / 4) + 0.2f * ((float)(i+1) / texDepth);
                }
            }

            // create and initialize the 3D texture
            texSeparate[chan] = new Texture3D(sampleSize, sampleSize, texDepth, TextureFormat.RGB24, false); //ARGB32
            texSeparate[chan].filterMode = FilterMode.Trilinear;
            texSeparate[chan].wrapMode = TextureWrapMode.Clamp;
            texSeparate[chan].anisoLevel = 0;

            Debug.Log("After files loaded");

            // convert the 2D texture array to a 3D texture
            var cols = new Color[sampleSize * sampleSize * texDepth];
            int idx = 0;
            for (int z = 0; z < texDepth; ++z)
            {
                for (int y = 0; y < sampleSize; ++y)
                {
                    for (int x = 0; x < sampleSize; ++x, ++idx)
                    {
                        cols[idx] = texArray[z].GetPixel(x, y);
                    }
                }

                if (progressSlider != null)
                    progressSlider.value = 0.05f + 0.8f * ((float)(chan + 1) / 4) + (0.14f * ((float)z / texDepth) * ((float)(chan) / 4) + 0.14f * ((float)(chan) / 4));
                yield return null;
            }

            texSeparate[chan].SetPixels(cols);

            texSeparate[chan].Apply();
            switch (chan)
            {
                case 0: // Red
                    internalMaterial.SetTexture("_VolumeRed", texSeparate[chan]);

                    TextureSlicingMat.SetTexture("_VolumeRed", texSeparate[chan]);
                    RayCastingMat.SetTexture("_VolumeRed", texSeparate[chan]);
                    RayCastingIsoSurfaceMat.SetTexture("_VolumeRed", texSeparate[chan]);
                    // Debug.Log("RED");
#if UNITY_EDITOR
                    AssetDatabase.CreateAsset(texSeparate[chan], "Assets/Resources/Textures/Yig_red.tex");
#endif
                    break;
                case 1: // Green
                    internalMaterial.SetTexture("_VolumeGreen", texSeparate[chan]);

                    TextureSlicingMat.SetTexture("_VolumeGreen", texSeparate[chan]);
                    RayCastingMat.SetTexture("_VolumeGreen", texSeparate[chan]);
                    RayCastingIsoSurfaceMat.SetTexture("_VolumeGreen", texSeparate[chan]);
                    //  Debug.Log("GREEN");
#if UNITY_EDITOR
                    AssetDatabase.CreateAsset(texSeparate[chan], "Assets/Resources/Textures/Yig_green.tex");
#endif
                    break;
                case 2: // Blue
                    internalMaterial.SetTexture("_VolumeBlue", texSeparate[chan]);

                    TextureSlicingMat.SetTexture("_VolumeBlue", texSeparate[chan]);
                    RayCastingMat.SetTexture("_VolumeBlue", texSeparate[chan]);
                    RayCastingIsoSurfaceMat.SetTexture("_VolumeBlue", texSeparate[chan]);
                    // Debug.Log("BLUE");
#if UNITY_EDITOR
                    AssetDatabase.CreateAsset(texSeparate[chan], "Assets/Resources/Textures/Yig_blue.tex");
#endif
                    break;
                case 3: // Purple

                    internalMaterial.SetTexture("_VolumePurple", texSeparate[chan]);

                    TextureSlicingMat.SetTexture("_VolumePurple", texSeparate[chan]);
                    RayCastingMat.SetTexture("_VolumePurple", texSeparate[chan]);
                    RayCastingIsoSurfaceMat.SetTexture("_VolumePurple", texSeparate[chan]);
                    // Debug.Log("PURPLE");
#if UNITY_EDITOR
                    AssetDatabase.CreateAsset(texSeparate[chan], "Assets/Resources/Textures/Yig_purple.tex");
#endif
                    break;
            }

            yield return null;
            Debug.Log("End of loop");
        }

        if (progressSlider != null)
            progressSlider.value = 1f;
        yield return null;
        doneLoadingSample = true;

        Debug.Log("Done loading sample. Loading scene " + sceneToLoad);
        SceneManager.LoadScene(sceneToLoad);
    }

    public Texture2D LoadImage(string filePath)
    {

        Texture2D imTex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            //Debug.Log(filePath);
            fileData = File.ReadAllBytes(filePath);
            //TODO: this value is hardcoded, and should actually be dynamically checked for by the image itself
            imTex = new Texture2D(sampleSize, sampleSize);
            imTex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            TextureScale.Bilinear(imTex, sampleSize, sampleSize);
        }
        else
        {
            Debug.Log(filePath + " does not exist");
        }
        return imTex;
    }
    #endregion

    // calculate the closest power of two to the input number
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

    public void SliderChanged()
    {
        //Debug.Log(progressSlider.value);
    }

}
