using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class FileSelection : MonoBehaviour {

    public bool BenProject = true;
    //[System.NonSerialized]
    public string samplesPath = "E:\\";
    public ScrollableList scrollList;
    public Text pathTextUI;
    public Text infoTextUI;

    private LoadSample loadSample;

    [SerializeField]
    private GameObject samplesPreview;
    //[SerializeField]
    //private Slider progressSlider;

    private string[] subFolders;
    private bool validSampleFolder = false;
    private bool validFileType = false;
    private string fileExtension = "";

    // Use this for initialization
    void Start ()
    {
        if (File.Exists(Application.persistentDataPath + "\\startFolder.txt"))
        {
            samplesPath = File.ReadAllText(Application.persistentDataPath + "\\startFolder.txt");
            Debug.Log("Loaded: " + (Application.persistentDataPath) + "\\startFolder.txt");
        }

        if (scrollList == null)
            Debug.LogError("No ScrollableList given");
        if (pathTextUI == null)
            Debug.LogError("No pathTextUI given");
        if (infoTextUI == null)
            Debug.LogError("No infoTextUI given");

        if (samplesPreview == null)
            Debug.LogError("No samplesPreview given");

        UpdateFolderButtons();

        if(BenProject)
        {
            // this approach instead of just including it from the beginning is to be able to script many runs with persistent data being refreshed each time
            GameObject PD = GameObject.Find("PersistentData");
            PD.AddComponent<LoadSample>();
            LoadSample temp = PD.GetComponent<LoadSample>();
            LoadSample reference = GameObject.Find("New LoadSample").GetComponent<LoadSample>();
            temp.sampleSize = reference.sampleSize;
            temp.internalMaterial = reference.internalMaterial;
            temp.TextureSlicingMat = reference.TextureSlicingMat;
            temp.RayCastingMat = reference.RayCastingMat;
            temp.RayCastingIsoSurfaceMat = reference.RayCastingIsoSurfaceMat;
            temp.progressSlider = reference.progressSlider;
            temp.sampleImages = reference.sampleImages;
            temp.sceneToLoad = reference.sceneToLoad;
            temp.zRatio = reference.zRatio;
            temp.isPreLoaded = reference.isPreLoaded;
            temp.samplePath = reference.samplePath;

            loadSample = temp;


            PersistentData persistentData = PD.GetComponent<PersistentData>();
            
            if (persistentData != null)
            {
                samplesPreview.SetActive(true);
                //Debug.Log("Loading sample: " + persistentData.cellPaths[persistentData.currentCell]);
                Debug.Log(loadSample);
                loadSample.LoadSampleSeperate(persistentData.cellPaths[persistentData.currentCell++], "*.png");
            }
        }
        else
        {
            loadSample = GameObject.Find("PersistentData").GetComponent<LoadSample>();
        }
    }
	
	// Update is called once per frame
	void Update () {
        // close application when Esc key pressed
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    private void UpdateFolderButtons()
    {
        pathTextUI.text = samplesPath;

        samplesPreview.SetActive(false);

        if (!Directory.Exists(samplesPath))
        {
            samplesPath = "E:\\";
        }
        subFolders = Directory.GetDirectories(samplesPath);

        for (int i = 0; i < subFolders.Length; i++)
        {
            subFolders[i] = subFolders[i].Substring(subFolders[i].LastIndexOf("\\") + 1);
            //Debug.Log("Folder found: " + subFolders[i]);
        }

        string[] fileEntries = Directory.GetFiles(samplesPath);

        int redCount = 0, greenCount = 0, blueCount = 0, purpleCount = 0, nonsampleCount = 0; ;
        string fileType = "";
        foreach (string file in fileEntries)
        {
            if ((file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".jpeg")))
            {
                if (file.Substring(file.LastIndexOf("\\")).ToLower().Contains("red"))
                    redCount++;
                else if (file.Substring(file.LastIndexOf("\\")).ToLower().Contains("green") )
                    greenCount++;
                else if (file.Substring(file.LastIndexOf("\\")).ToLower().Contains("blue"))
                    blueCount++;
                else if (file.Substring(file.LastIndexOf("\\")).ToLower().Contains("purple"))
                    purpleCount++;
                else
                {
                    nonsampleCount++;
                    continue;
                }
            }
            if (file.EndsWith(".png"))
            {
                fileType = "<color=green>png</color>";
                fileExtension = "*.png";
            }
            if (file.EndsWith(".jpg"))
            {
                fileType = "<color=green>jpg</color>";
                fileExtension = "*.jpg";
            }
            if (file.EndsWith(".jpeg"))
            {
                fileType = "<color=green>jpeg</color>";
                fileExtension = "*.jpeg";
            }
        }

        if (fileType == "")
        {
            fileType = "<color=red>no valid file type found (only png and jpg accepted)</color>";
            validFileType = false;
        }
        else
        {
            validFileType = true;
        }

        if ((redCount == greenCount || greenCount == 0) && (redCount == blueCount || blueCount == 0) && (redCount == purpleCount || purpleCount == 0) ||
            (greenCount == purpleCount || purpleCount == 0) && (greenCount == blueCount || blueCount == 0) && (greenCount == redCount || redCount == 0) ||
            (blueCount == greenCount || greenCount == 0) && (blueCount == purpleCount || purpleCount == 0) && (blueCount == redCount || redCount == 0) ||
            (purpleCount == greenCount || greenCount == 0) && (purpleCount == blueCount || blueCount == 0) && (purpleCount == redCount || redCount == 0))
        {
            infoTextUI.text = string.Format("Red Files: <color=green>{0}</color>   Green Files: <color=green>{1}</color>   Blue Files: <color=green>{2}</color>   Purple Files: <color=green>{3}</color>   Non Sample Files: {4}\nFile type: {5}", redCount, greenCount, blueCount, purpleCount, nonsampleCount, fileType);
            validSampleFolder = true;
        }
        else
        {
            infoTextUI.text = string.Format("Red Files: <color=red>{0}</color>   Green Files: <color=red>{1}</color>   Blue Files: <color=red>{2}</color>   Purple Files: <color=red>{3}</color>   Non Sample Files: {4}\nFile type: {5}", redCount, greenCount, blueCount, purpleCount, nonsampleCount, fileType);
            validSampleFolder = false;
        }

        scrollList.CreateButtons(subFolders);
    }

    public void FolderClicked(Text btnText)
    {
        samplesPath += "\\" + btnText.text;
       // Debug.Log("New sample path: " + samplesPath);

        UpdateFolderButtons();
    }

    public void OpenSampleFolder()
    {
        if(!(validFileType && validSampleFolder))
        {
            Debug.Log("Invalid folder or file type, unable to open.");
            return;
        }

        //File.CreateText(Application.persistentDataPath + "\\startFolder.txt");
        File.WriteAllText(Application.persistentDataPath + "\\startFolder.txt", samplesPath);

        samplesPreview.SetActive(true);
        loadSample.LoadSampleSeperate(samplesPath, fileExtension);
    }

    public void BackButtonClicked()
    {
        samplesPath = samplesPath.Substring(0, samplesPath.LastIndexOf("\\"));
        if (!samplesPath.Contains("\\"))
            samplesPath += "\\";

        UpdateFolderButtons();
    }
}
