using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class PersistentData : MonoBehaviour {

    public static PersistentData Instance;

    public bool BenProject = false;
    public List<string> cellPaths;
    public int currentCell = 0;

    void Awake()
    {
        // this approach instead of just including it from the beginning is to be able to script many runs with persistent data being refreshed each time
        gameObject.AddComponent<LoadSample>();
        LoadSample temp = gameObject.GetComponent<LoadSample>();
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

        if (Instance == null)
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        cellPaths = new List<string>();
        if (BenProject)
        {
            string path = "C:\\BEN_FILES\\Output\\";
            string[] subFolders = Directory.GetDirectories(path);

            for (int i = 0; i < subFolders.Length; i++)
            {
                Debug.Log("Mutation found: " + subFolders[i]);
                if (subFolders[i].Contains("WT+TOMs"))
                {
                    string[] TOMs = Directory.GetDirectories(subFolders[i]);
                    for (int j = 0; j < TOMs.Length; j++)
                    {
                        Debug.Log("TOM found: " + TOMs[j]);

                        string[] type = Directory.GetDirectories(TOMs[j]);
                        for (int k = 0; k < type.Length; k++)
                        {
                            //Debug.Log("Type found: " + type[k]);
                            string[] cell = Directory.GetDirectories(type[k]);
                            for (int l = 0; l < cell.Length; l++)
                            {
                                cellPaths.Add(cell[l]);
                                //Debug.Log("Cell found: " + cell[l]);
                            }
                        }
                    }
                }

            }

            Debug.Log("Num cells: " + cellPaths.Count);
        }
    }

    // Use this for initialization
    void Start () {
        
    }
    	
	// Update is called once per frame
	void Update () {
	
	}
}
