using UnityEngine;
using UnityEngine.VR;
#if UNITY_ENGINE
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class ChemicalLoader: MonoBehaviour 
{
	public int maxNumMolecules = 10;
	public BoundingBoxSetup boundingBox;
	public string fileName;
	public GameObject moleculesParent;
	public Material chemicalMat;
	public float atomRadiusScale = 1.0f;

	public float translationSpeed = 2.0f;
	public float rotationSpeed = 100.0f;
	public float startScaleFactor = 0.1f;


	private List<Vector3> boxVertices;

	// these are all the atoms that are rendered (for the bonds to show up correctly)
	private List<Vector3>[] atomPositions;	// the indices for the element buffer (in Cartesian coordinates)
	//private List<Vector3>[] atomPositionsFrac;	// the indices for the element buffer (in fractional coordinates)
	private List<Color>[] atomColours;		// the Jmol colour associated with the element;
	private List<string>[] atomNames;		// the element's name
	private List<int>[] indices;				// the indices for the element buffer

	// these are the true atom postions with all the duplicates removed (to make calculations correct, and to improve performance)
	private List<Vector3>[] reducedAtomPositions;	// the indices for the element buffer (in Cartesian coordinates) (with all the duplicate atom positions removed)
	//private List<Vector3>[] reducedAtomPositionsFrac;	// the indices for the element buffer (in fractional coordinates) (with all the duplicate atom positions removed)
	private List<Color>[] reducedAtomColours;		// the Jmol colour associated with the element (with all the duplicate atom positions removed)
	private List<float>[] reducedAtomRadii;		// the vdW Radius of every atom (with all the duplicate atom positions removed)
	private List<string>[] reducedAtomNames;	// the element's chemical name
	//private List<int>[] reducedIndices;				// the indices for the element buffer
	private List<GameObject>[] atoms;			// the physical game object spheres for the atoms
	private List<GameObject>[] bonds;			// the physical game object bonds between atoms

	private Dictionary<string, int> elementNum = new Dictionary<string, int>();
	private Dictionary<int, int> atomIDMap = new Dictionary<int, int>();		// this temporarily holds the ID map from the new ID to the old ID (used to correct the indices)


	private Vector4 minimum = new Vector4(10000.0f, 10000.0f, 10000.0f, 1.0f); // the minimum vertex x, y, z
	private Vector4 maximum = new Vector4(-10000.0f, -10000.0f, -10000.0f, 1.0f); // the maximum vertex x, y, z
	private Vector3 midpointOffset; // the offset from the origin (centers box around origin)
	private Vector3 scaleFactor;	// how much to scale the box to make it a reasonable size

	private Vector4 minimumFrac = new Vector4(10000.0f, 10000.0f, 10000.0f, 1.0f); // the minimum vertex a, b, c
	private Vector4 maximumFrac = new Vector4(-10000.0f, -10000.0f, -10000.0f, 1.0f); // the maximum vertex a, b, c
	private Vector4 moleculeCentroidFrac; // the centroid of the molecule based on Fractional coordinates

	// an atom in that is read from the file
	public class Atom
	{
		public int id = -1;						// the file allocated atom ID - 1
		public string name = "";				// element name eg. "Cu", "H", "C"
		public Vector3 position = new Vector3();			// position of the atom in 3D space (in cartesian coordinates)
		public Vector3 fracPosition = new Vector3();		// position of the atom in 3D space (in fractional coordinates)
		public List<int> bonds = new List<int>();		// the atom id's to which this one is bound 
		//public List<int> numBonds;	// the number of bonds for each atom		
		public int moleculeNum = -1;		// the molecule index to which this atom belongs
	}

	private List<Atom> readAtoms;		// the total molecule as read from the file. The atoms that were read

	//List< List<Atom> > molecule;	// a vector of all the separate molecules found
	private List<Atom>[] molecule;			// a vector of all the separate molecules found (max of 50 molecules)
	private int numOfMolecules = 0;					// the total number of molecules (this is a counter variable)
	private int mainMolecule = 0;					// the main molecule the the one chosen with the greates size and is used to determine packing.
	private GameObject[] moleculeParent;

	// the molecule cell length along the axes
	private float cellLength_a = 0.0f;
	private float cellLength_b = 0.0f;
	private float cellLength_c = 0.0f;

	private float v = 0.0f;	// the volume of the unit cell

	// the molecule cell major axes angles in degrees
	private float cellAngle_alpha = 0.0f;
	private float cellAngle_beta = 0.0f;
	private float cellAngle_gamma = 0.0f;

	// the conversion matrix to convert from cartesian coordinates to fractional coordinates
	private Vector3 a_axis = new Vector3();
	private Vector3 b_axis = new Vector3();
	private Vector3 c_axis = new Vector3();

	// the conversion matrix to convert from fractional to cartesian coordinates
	private Vector3 a_axis_to_Cart = new Vector3();
	private Vector3 b_axis_to_Cart = new Vector3();
	private Vector3 c_axis_to_Cart = new Vector3();

	// the normalized axes in cartesian coordinates
	private Vector3 a_axis_norm = new Vector3();
	private Vector3 b_axis_norm = new Vector3();
	private Vector3 c_axis_norm = new Vector3();

	// Use this for initialization
	void Start () 
	{
		initElementMapping();

		if(moleculesParent == null)
		{
			moleculesParent = GameObject.FindGameObjectWithTag("MoleculeParent");
		}


		readAtoms = new List<Atom>();
		// initialise arrays
		molecule = new List<Atom>[maxNumMolecules];

		atomPositions = new List<Vector3>[maxNumMolecules];
		//atomPositionsFrac = new List<Vector3>[maxNumMolecules];
		atomColours = new List<Color>[maxNumMolecules];
		atomNames = new List<string>[maxNumMolecules];
		indices = new List<int>[maxNumMolecules];

		reducedAtomPositions = new List<Vector3>[maxNumMolecules];
		//reducedAtomPositionsFrac = new List<Vector3>[maxNumMolecules];
		reducedAtomColours = new List<Color>[maxNumMolecules];
		reducedAtomRadii = new List<float>[maxNumMolecules];
		reducedAtomNames = new List<string>[maxNumMolecules];
		//reducedIndices = new List<int>[maxNumMolecules];

		atoms = new List<GameObject>[maxNumMolecules];
		bonds = new List<GameObject>[maxNumMolecules];
		moleculeParent = new GameObject[maxNumMolecules];

		// TODO: a better way to do this might be to use a matrix?... 
		// put all the box vertices in a List for easy modification later on
		boxVertices = new List<Vector3>();
		foreach (Vector3 vert in boundingBox.boxVertices)
			boxVertices.Add (vert);
		

		// check if the file format is supported
		//if (fileName.Contains(".mol"))
		//	readFile_mol(fileName);
		//else if (fileName.Contains(".cssr"))
		//	readFile_cssr(fileName);
		//else if (fileName.Contains(".pdb"))
			readFile_pdb(fileName);
		//else
		//	Debug.Log( "ERROR: invalid chemical file type supplied");


		// scale the parten game object to make the entire rendering a reasonable size
		moleculesParent.transform.localScale = new Vector3(startScaleFactor, startScaleFactor, startScaleFactor);

	}
	
	// Update is called once per frame
	void Update () 
	{
		//If V is pressed, toggle VRSettings.enabled
		if (Input.GetKeyDown(KeyCode.V))
		{
			VRSettings.enabled = !VRSettings.enabled;
			Debug.Log("Changed VRSettings.enabled to:"+VRSettings.enabled);
		}

		gamePadInput();
	}

	void gamePadInput()
	{
		float leftX = Input.GetAxis("Oculus_GearVR_LThumbstickX");
		float leftY = Input.GetAxis("Oculus_GearVR_LThumbstickY");
		float rightX = Input.GetAxis("Oculus_GearVR_RThumbstickX");
		float rightY = Input.GetAxis("Oculus_GearVR_RThumbstickY");

		bool leftTriggerPressed = Input.GetButton("Fire1");
		bool rightTriggerPressed = Input.GetButton("Fire2");
		bool scaleTriggerPressed = Input.GetButton("Fire3");

		//use the left joystick for both translation and scaling
		if(!scaleTriggerPressed) // Translations
		{
			// if modifier button pressed then move in z instead of y
			if(!leftTriggerPressed )
			{
				moleculesParent.transform.Translate(new Vector3(leftX, leftY, 0.0f) * Time.deltaTime * translationSpeed, Space.World);
			}		
			else
			{
				moleculesParent.transform.Translate(new Vector3(leftX, 0.0f, leftY) * Time.deltaTime * translationSpeed, Space.World);
			}
		}
		else // scaling
		{
			//TODO: should possibly have a separate scaleSpeed
			moleculesParent.transform.localScale += new Vector3(leftY, leftY, leftY) * Time.deltaTime * translationSpeed * 0.075f;

			// ensure that it doesn't invert
			//TODO: this should probably not be hardcoded
			if(moleculesParent.transform.localScale.x < 0.01f || moleculesParent.transform.localScale.y < 0.01f || boundingBox.transform.localScale.z < 0.01f)
				moleculesParent.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
		}

		if(!rightTriggerPressed)
		{
			moleculesParent.transform.Rotate(Vector3.up, -rightX * Time.deltaTime * rotationSpeed, Space.World);
			moleculesParent.transform.Rotate(Vector3.right, rightY * Time.deltaTime * rotationSpeed, Space.World);
		}
		else
		{
			moleculesParent.transform.Rotate(Vector3.forward, -rightX * Time.deltaTime * rotationSpeed, Space.World);
			moleculesParent.transform.Rotate(Vector3.right, rightY * Time.deltaTime * rotationSpeed, Space.World);
		}
	}

	int readFile_pdb(string filePath)
	{
		string line;
		string lineHead;
	
		StreamReader chemFile = new StreamReader(filePath);
		char[] delimeter = {' '};

		if (chemFile != null)
		{
			while (!chemFile.EndOfStream) // if there are more data in the file
			{
				// get next line from the file
				line = chemFile.ReadLine();
				// seperate the text in the string line
				string[] lineSeperated = line.Split(delimeter);
				List<string> lineSpaceRemoved = new List<string>();
				foreach(string s in lineSeperated)
				{
					s.Trim();
					if(s.Length != 0)
					{	
						lineSpaceRemoved.Add(s);
						//Debug.Log(s);
					}
				}
				
				lineHead = lineSpaceRemoved[0];
				if (lineHead == "CRYST1")
				{					
					cellLength_a = (float)Convert.ToDouble(lineSpaceRemoved[1]);
					cellLength_b = (float)Convert.ToDouble(lineSpaceRemoved[2]);
					cellLength_c = (float)Convert.ToDouble(lineSpaceRemoved[3]);

					cellAngle_alpha = (float)Convert.ToDouble(lineSpaceRemoved[4]);
					cellAngle_beta = (float)Convert.ToDouble(lineSpaceRemoved[5]);
					cellAngle_gamma = (float)Convert.ToDouble(lineSpaceRemoved[6]);
					//Debug.Log("a: " + cellLength_a + " b: " + cellLength_b + " c: " + cellLength_c); 
					//Debug.Log("a: " + cellAngle_alpha + " b: " + cellAngle_beta + " g: " + cellAngle_gamma);
				}
				else if (lineHead == "HETATM")
				{
					Atom tempAtom = new Atom();

					tempAtom.id = Convert.ToInt16(lineSpaceRemoved[1]);
					tempAtom.id--;	// since atom ids start from 1 and internally I use them from 0

					tempAtom.position.x = (float)Convert.ToDouble(lineSpaceRemoved[5]);
					tempAtom.position.y = (float)Convert.ToDouble(lineSpaceRemoved[6]);
					tempAtom.position.z = (float)Convert.ToDouble(lineSpaceRemoved[7]);

					tempAtom.name = lineSpaceRemoved[10];

					//Debug.Log(tempAtom.name + " x: " + tempAtom.position.x + " y: " + tempAtom.position.y + " z: " + tempAtom.position.z);

					readAtoms.Add(tempAtom);
				}
				else if (lineHead == "CONECT") // the bonds between the atoms
				{
					int startAtom, endAtom;
					startAtom = Convert.ToInt16(lineSpaceRemoved[1]);

					for(int a = 2; a < lineSpaceRemoved.Count; a++)	
					{
						endAtom = Convert.ToInt16(lineSpaceRemoved[a]);
						// due to how this file format's bonding works, there will be duplicates unless this test is added (I actually need this for the recursive method)
						//if (endAtom > startAtom)
						readAtoms[startAtom - 1].bonds.Add(endAtom - 1); // Since the ids start at 1 in the .pdb file subtract 1 to make it all from 0
					}
				}

			}
			chemFile.Close();
		}
		else
		{
			Debug.Log("Error: Could not open chemical file!");
			return -1;
		}

		extractMolecules();
		return 0;
	}

	float DegreeToRad(float angle)
	{
		return (float)((Math.PI / 180.0) * angle);
	}

	int extractMolecules()
	{
		// calculate the unit cell's vertices
		//https://en.wikipedia.org/wiki/Fractional_coordinates
		v = (float)Math.Sqrt(1.0 - Math.Cos(DegreeToRad(cellAngle_alpha))*Math.Cos(DegreeToRad(cellAngle_alpha)) - Math.Cos(DegreeToRad(cellAngle_beta))*Math.Cos(DegreeToRad(cellAngle_beta)) - Math.Cos(DegreeToRad(cellAngle_gamma))*Math.Cos(DegreeToRad(cellAngle_gamma)) + 2.0 * Math.Cos(DegreeToRad(cellAngle_alpha))*Math.Cos(DegreeToRad(cellAngle_beta))*Math.Cos(DegreeToRad(cellAngle_gamma)));

		//TODO: note that the z is negated (why I'm not really sure)...

		a_axis = new Vector3(1.0f / cellLength_a, (float)-(Math.Cos(DegreeToRad(cellAngle_gamma)) / (cellLength_a*Math.Sin(DegreeToRad(cellAngle_gamma)))), (float)((Math.Cos(DegreeToRad(cellAngle_alpha))*Math.Cos(DegreeToRad(cellAngle_gamma)) - Math.Cos(DegreeToRad(cellAngle_beta))) / (cellLength_a*v*Math.Sin(DegreeToRad(cellAngle_gamma)))));
		b_axis = new Vector3(0.0f, (float)(1.0f / (cellLength_b*Math.Sin(DegreeToRad(cellAngle_gamma)))), (float)((Math.Cos(DegreeToRad(cellAngle_beta))*Math.Cos(DegreeToRad(cellAngle_gamma)) - Math.Cos(DegreeToRad(cellAngle_alpha))) / (cellLength_b*v*Math.Sin(DegreeToRad(cellAngle_gamma)))));
		c_axis = new Vector3(0.0f, 0.0f, (float)(Math.Sin(DegreeToRad(cellAngle_gamma)) / (cellLength_c*v)));

		// convert to cartesian
		a_axis_to_Cart = new Vector3(cellLength_a, (float)(cellLength_b*Math.Cos(DegreeToRad(cellAngle_gamma))), (float)(cellLength_c*Math.Cos(DegreeToRad(cellAngle_beta))));
		b_axis_to_Cart = new Vector3(0.0f, (float)(cellLength_b*Math.Sin(DegreeToRad(cellAngle_gamma))), (float)(cellLength_c* (Math.Cos(DegreeToRad(cellAngle_alpha)) - Math.Cos(DegreeToRad(cellAngle_beta))*Math.Cos(DegreeToRad(cellAngle_gamma))) / (Math.Sin(DegreeToRad(cellAngle_gamma)))));	
		c_axis_to_Cart = new Vector3(0.0f, 0.0f, (float)((cellLength_c*v) / (Math.Sin(DegreeToRad(cellAngle_gamma)))));

		a_axis_norm = a_axis.normalized;
		b_axis_norm = b_axis.normalized;
		c_axis_norm = c_axis.normalized;


		for (int i = 0; i < readAtoms.Count; i++)
		{
			// atom is unallocated to a molecule
			if (readAtoms[i].moleculeNum == -1)
			{
				molecule[numOfMolecules] = new List<Atom>();

				Debug.Log("Molecule: " + numOfMolecules);
				discoverMolecule(i, numOfMolecules);

				//TODO: find out if this is correct from chemistry department
				// if a molecule consisting of only one atom is found, it is probably not to be used
				if (molecule[numOfMolecules].Count == 1)
				{
					Debug.Log("Disconnected atom found and discarded");
					molecule[numOfMolecules].Clear();
				}
				else
				{
					// update main molecule based on the greater size
					if (molecule[numOfMolecules].Count > molecule[mainMolecule].Count)
						mainMolecule = numOfMolecules;

					// ensure that the molecule is sorted (Since the molecule finder changes the order of the atoms)
					molecule[numOfMolecules].Sort(delegate(Atom x, Atom y) {
						return x.id.CompareTo(y.id);
					});

					// change all the id's to start from 0 (for the indices when rendering)
					for (int new_id = 0; new_id < molecule[numOfMolecules].Count; new_id++)
					{
						atomIDMap[molecule[numOfMolecules][new_id].id] = new_id;
						molecule[numOfMolecules][new_id].id = new_id;
					}
					// change all the bond id's to the new ID
					for (int j = 0; j < molecule[numOfMolecules].Count; j++)
					{
						for (int k = 0; k < molecule[numOfMolecules][j].bonds.Count; k++)
						{
							molecule[numOfMolecules][j].bonds[k] = atomIDMap[molecule[numOfMolecules][j].bonds[k]];
						}
					}

					List<Vector3> reflecionAxes = new List<Vector3>();
					List<Vector3> offsets = new List<Vector3>();

					//All the symmetry renderings as from the .cif file
					reflecionAxes.Add(new Vector3(1.0f, 1.0f, 1.0f));
					reflecionAxes.Add(new Vector3(-1.0f, 1.0f, -1.0f));
					reflecionAxes.Add(new Vector3(1.0f, 1.0f, 1.0f));
					reflecionAxes.Add(new Vector3(-1.0f, 1.0f, -1.0f));
					reflecionAxes.Add(new Vector3(-1.0f, -1.0f, -1.0f));
					reflecionAxes.Add(new Vector3(1.0f, -1.0f, 1.0f));
					reflecionAxes.Add(new Vector3(-1.0f, -1.0f, -1.0f));
					reflecionAxes.Add(new Vector3(1.0f, -1.0f, 1.0f));

					offsets.Add(new Vector3(0.0f, 0.0f, 0.0f));
					offsets.Add(new Vector3(0.0f, 0.0f, 0.5f));
					offsets.Add(new Vector3(0.5f, 0.5f, 0.0f));
					offsets.Add(new Vector3(0.5f, 0.5f, 0.5f));
					offsets.Add(new Vector3(0.0f, 0.0f, 0.0f));
					offsets.Add(new Vector3(0.0f, 0.0f, -0.5f));
					offsets.Add(new Vector3(0.5f, 0.5f, 0.0f));
					offsets.Add(new Vector3(0.5f, 0.5f, -0.5f));


					int symmetrySize = reflecionAxes.Count;
					Vector3 newOffset = new Vector3(0.0f, 0.0f, 0.0f);

					// this uses a binary counter to alternate the ofsets. Quite clever if I have to say so myself :-) Thanks for reading this comment
					for (int elem = 0; elem < symmetrySize; elem++)
					{					
						for (int b = 0; b <= 7; b++) 
						{
							// possitive
							newOffset.x = (b & 0x1) / 1;
							newOffset.y = (b & 0x2) / 2;
							newOffset.z = (b & 0x4) / 4;

							reflecionAxes.Add(reflecionAxes[elem]);
							offsets.Add(offsets[elem] + newOffset);

							// negative
							reflecionAxes.Add(reflecionAxes[elem]);
							offsets.Add(offsets[elem] - newOffset);
						}
					}							



					// generate the current molecule's vertices
					generate3DMesh(numOfMolecules, reflecionAxes, offsets);

					//move to next molecule in the list
					numOfMolecules++;

					if (numOfMolecules == maxNumMolecules)
					{
						Debug.Log("ERROR: Molecule capacity of "+maxNumMolecules+" is exhausted. Overriding last molecule");
						numOfMolecules--;
					}
				}
			}
		}


		return 0;
	}

	// This implements depth first search
	void discoverMolecule(int atomIndex, int moleculeIndex)
	{
		//static int numAtoms = 1;

		//cout << "\t" << numAtoms++ << ": " << readAtoms[atomIndex].name << endl;
		// label atom as found
		readAtoms[atomIndex].moleculeNum = moleculeIndex;
		molecule[moleculeIndex].Add(readAtoms[atomIndex]);

		// loop throught the bonds
		for (int i = 0; i < readAtoms[atomIndex].bonds.Count; i++)
		{
			// if the bond has not been assigned yet, assign it...
			if (readAtoms[readAtoms[atomIndex].bonds[i]].moleculeNum == -1)
				discoverMolecule(readAtoms[atomIndex].bonds[i], moleculeIndex);
		}	
	}

	void generate3DMesh(int moleculeNum, List<Vector3> reflectionAxes, List<Vector3> offsets)
	{
		// ensure the centroid is calculated with the newest information
		calculateCentroid(moleculeNum);

		//initialize all the arrays
		atomPositions[moleculeNum] = new List<Vector3>();
		atomColours[moleculeNum] = new List<Color>();
		atomNames[moleculeNum] = new List<string>();
		reducedAtomPositions[moleculeNum] = new List<Vector3>();
		reducedAtomColours[moleculeNum] = new List<Color>();
		reducedAtomRadii[moleculeNum] = new List<float>();
		reducedAtomNames[moleculeNum] = new List<string>();
		//reducedIndices[moleculeNum] = new List<int>();
		indices[moleculeNum] = new List<int>();
		atoms[moleculeNum] = new List<GameObject>();
		bonds[moleculeNum] = new List<GameObject>();

		moleculeParent[moleculeNum] = new GameObject();
		moleculeParent[moleculeNum].transform.SetParent(moleculesParent.transform);
		moleculeParent[moleculeNum].name = "Molecule " + moleculeNum;

		if (reflectionAxes.Count != offsets.Count)
		{
			Debug.Log( "ERROR: The reflection Axes and offsets must be equal. Can't determine 3DMesh.");
			return;
		}
		else if (reflectionAxes.Count == 0) // render the molecule in the middle of the cell
		{
			// extract the atom positions and colours (based on main molecule)
			for (int i = 0; i < molecule[moleculeNum].Count; i++)
			{
				atomPositions[moleculeNum].Add(molecule[moleculeNum][i].position);
				atomColours[moleculeNum].Add(getElementColour(elementNum[molecule[moleculeNum][i].name]));
				atomNames[moleculeNum].Add(molecule[moleculeNum][i].name);

				// add the atom to the stack (for sphere rendering)
				reducedAtomPositions[moleculeNum].Add(molecule[moleculeNum][i].position);
				reducedAtomColours[moleculeNum].Add(getElementColour(elementNum[molecule[moleculeNum][i].name]));
				reducedAtomRadii[moleculeNum].Add(getAtomRadius(elementNum[molecule[moleculeNum][i].name]));
				reducedAtomNames[moleculeNum].Add(molecule[moleculeNum][i].name);
				//atomPositionsFrac[moleculeNum].Add(molecule[moleculeNum][i].fracPosition);
			}


			// loop through all the atoms in the readAtoms to determine the bond indices
			for (int i = 0; i < molecule[moleculeNum].Count; i++)
			{
				// loop through all the bonds of that atom
				for (int j = 0; j < molecule[moleculeNum][i].bonds.Count; j++)
				{
					indices[moleculeNum].Add(molecule[moleculeNum][i].id);		// the 'from' atom
					indices[moleculeNum].Add(molecule[moleculeNum][i].bonds[j]);	// the 'to' atom
				}
			}
		}
		else // render the number of repeats as indicated by reflectionAxes.Count
		{
			//atomPositions[moleculeNum].clear();
			int duplicateCount = 0;
			int totalAtomsRemoved = 0;
			int totalIndicesRemoved = 0;

			int skippedCount = 0;	// number of atoms skipped
			int notSkippedCount = 0;// the number of atoms not skipped (should be equal to molecule.count - skippedCount
			int bondsToSkip = 0;	// the number of bonds (indices) I should skip
			int usedCounter = 0;	// this is to increment the bond ids only when I actually used that molecule (no duplicate detected)

			for (int i = 0; i < reflectionAxes.Count; i++)
			{
				//Ensure I don't accidently send zero and cancel out that channel
				if (reflectionAxes[i].x == 0.0f) reflectionAxes[i].Set(1.0f, reflectionAxes[i].y, reflectionAxes[i].z);
				if (reflectionAxes[i].y == 0.0f) reflectionAxes[i].Set(reflectionAxes[i].x, 1.0f, reflectionAxes[i].z);
				if (reflectionAxes[i].z == 0.0f) reflectionAxes[i].Set(reflectionAxes[i].x, reflectionAxes[i].y, 1.0f);

				// extract the atom positions and colours (based on main molecule)
				for (int atom = 0; atom < molecule[moleculeNum].Count; atom++)
				{
					Vector3 newPosition = new Vector3();
					Vector3 newFracPosition = new Vector3();

					newFracPosition.x = (reflectionAxes[i].x) * (molecule[moleculeNum][atom].fracPosition.x - moleculeCentroidFrac.x) + offsets[i].x;
					newFracPosition.y = (reflectionAxes[i].y) * (molecule[moleculeNum][atom].fracPosition.y - moleculeCentroidFrac.y) + offsets[i].y;
					newFracPosition.z = (reflectionAxes[i].z) * (molecule[moleculeNum][atom].fracPosition.z - moleculeCentroidFrac.z) + offsets[i].z;

					// convert from fractional coordinates to cartesian coordinates
					newPosition.x = Vector3.Dot(a_axis_to_Cart, newFracPosition);
					newPosition.y = Vector3.Dot(b_axis_to_Cart, newFracPosition);
					newPosition.z = Vector3.Dot(c_axis_to_Cart, newFracPosition);

					// check if there isn't maybe already an atom at that position (and skip it if it already exists
					bool skip = false;
					for (int a = 0; a < reducedAtomPositions[moleculeNum].Count; a++)
					{
						//TODO: this offset value should be more dynamically set... (test it)
						//if(moleculeNum == 1)
						//	Debug.Log("Marker");
						if (Math.Abs(reducedAtomPositions[moleculeNum][a].x - newPosition.x) < 0.01 && Math.Abs(reducedAtomPositions[moleculeNum][a].y - newPosition.y) < 0.01 && Math.Abs(reducedAtomPositions[moleculeNum][a].z - newPosition.z) < 0.01)
						{
							/*
						char str[1024];
						sprintf(str, " removed with diff x: %.5f  y: %.5f  z: %.5f was removed", abs(atomPositions[moleculeNum][a].x - newPosition.x),
							abs(atomPositions[moleculeNum][a].y - newPosition.y), abs(atomPositions[moleculeNum][a].z - newPosition.z));
						
						cout << molecule[moleculeNum][atom].name << str << endl;
						*/

							duplicateCount++;
							skip = true;
							break;
						}
					}
					if (!skip) // if it's not a duplicate atom add it to the stack
					{
						// add the atom to the stack
						reducedAtomPositions[moleculeNum].Add(newPosition);
						reducedAtomColours[moleculeNum].Add(getElementColour(elementNum[molecule[moleculeNum][atom].name]));
						reducedAtomRadii[moleculeNum].Add(getAtomRadius(elementNum[molecule[moleculeNum][atom].name]));
						reducedAtomNames[moleculeNum].Add(molecule[moleculeNum][atom].name);
						/* // TODO: I don't think this has any practical usefulness...
						// loop through all the bonds of that atom
						for (int j = 0; j < molecule[moleculeNum][atom].bonds.Count; j++)
						{
							reducedIndices[moleculeNum].Add(molecule[moleculeNum][atom].id);		// the 'from' atom
							reducedIndices[moleculeNum].Add(molecule[moleculeNum][atom].bonds[j]);	// the 'to' atom
						}
						*/

						notSkippedCount++;
					}
					else
						skippedCount++;


					// add the atom to the stack
					atomPositions[moleculeNum].Add(newPosition);
					atomColours[moleculeNum].Add(getElementColour(elementNum[molecule[moleculeNum][atom].name]));
					atomNames[moleculeNum].Add(molecule[moleculeNum][atom].name);

					// loop through all the bonds of that atom
					for (int j = 0; j < molecule[moleculeNum][atom].bonds.Count; j++)
					{
						indices[moleculeNum].Add(molecule[moleculeNum][atom].id);		// the 'from' atom
						indices[moleculeNum].Add(molecule[moleculeNum][atom].bonds[j]);	// the 'to' atom
						bondsToSkip += 2;
					}
				}
				//Debug.Log("Skipped: " + skippedCount + " Not SKipped: " + notSkippedCount + " Atom Pos count: " + atomPositions[moleculeNum].Count);

				if(notSkippedCount == 0)
				{
					atomPositions[moleculeNum].RemoveRange(atomPositions[moleculeNum].Count - skippedCount, skippedCount);
					atomColours[moleculeNum].RemoveRange(atomColours[moleculeNum].Count - skippedCount, skippedCount);
					atomNames[moleculeNum].RemoveRange(atomColours[moleculeNum].Count - skippedCount, skippedCount);
					indices[moleculeNum].RemoveRange(indices[moleculeNum].Count - bondsToSkip, bondsToSkip);
					//Debug.Log(skippedCount + " atoms could be reduced");
					totalAtomsRemoved += skippedCount;
					totalIndicesRemoved += bondsToSkip;

					skippedCount = 0;
					notSkippedCount = 0;
					bondsToSkip = 0;
					continue;
				}


				//This section of code is to ensure the indices vector works correctly
				Dictionary<int, int> newAtomIDMap = new Dictionary<int, int>();

				// change all the id's to continue counting
				for (int new_id = molecule[moleculeNum].Count * (usedCounter+1); new_id < molecule[moleculeNum].Count * (usedCounter + 2); new_id++)
				{
					newAtomIDMap[new_id - molecule[moleculeNum].Count] = new_id;
					molecule[moleculeNum][new_id - molecule[moleculeNum].Count * (usedCounter + 1)].id = new_id;
				}
				// change all the bond id's to the new ID
				for (int j = 0; j < molecule[moleculeNum].Count; j++)
				{
					for (int k = 0; k < molecule[moleculeNum][j].bonds.Count; k++)
					{
						molecule[moleculeNum][j].bonds[k] = newAtomIDMap[molecule[moleculeNum][j].bonds[k]];
					}
				}

				skippedCount = 0;
				notSkippedCount = 0;
				bondsToSkip = 0;
				usedCounter++;
			}

			Debug.Log(duplicateCount + " duplicate atoms removed (from symmetry) " + totalAtomsRemoved + " total atoms removed " + totalIndicesRemoved + " total indices removed");
		}

		for(int i = 0; i < reducedAtomPositions[moleculeNum].Count; i++)
		{
			GameObject gObj = null;

			try
			{
				gObj = Instantiate(Resources.Load("Elements/" + reducedAtomNames[moleculeNum][i] + "", typeof(GameObject))) as GameObject;
			}
			catch
			{
				//Debug.LogError(e);
				Debug.Log("Assets/Resources/Elements/" + reducedAtomNames[moleculeNum][i] + "  - was not found");
			}
			if(gObj == null)
			{
				gObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				gObj.isStatic = true;
				gObj.name = reducedAtomNames[moleculeNum][i];

				Material newMat = new Material(Shader.Find("VertexLit"));
				newMat.color = reducedAtomColours[moleculeNum][i];
				newMat.SetFloat("_Shininess", 1.0f);
				newMat.SetColor("_SpecColor", new Color(0.5f, 0.5f, 0.5f));
                #if UNITY_ENGINE
				AssetDatabase.CreateAsset(newMat, "Assets/Resources/Elements/" +  reducedAtomNames[moleculeNum][i] + "_mat.mat");
#endif

				gObj.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Elements/" + reducedAtomNames[moleculeNum][i] + "_mat") as Material;

				//atoms[moleculeNum][i].GetComponent<MeshFilter>().mesh.colors[0] = reducedAtomColours[moleculeNum][i];
				gObj.GetComponent<MeshRenderer>().receiveShadows = false;
				gObj.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;


                #if UNITY_ENGINE
				//http://wiki.unity3d.com/index.php?title=CreatePrefabFromSelected
				PrefabUtility.CreatePrefab("Assets/Resources/Elements/" +  reducedAtomNames[moleculeNum][i] + ".prefab", gObj);
#endif
			}

			float atomRadius = reducedAtomRadii[moleculeNum][i] * atomRadiusScale * 2.0f; // times 2 to convert radius to diameter
			gObj.transform.localScale = new Vector3(atomRadius, atomRadius, atomRadius);
			gObj.transform.position = reducedAtomPositions[moleculeNum][i];//new Vector3(UnityEngine.Random.Range(-10.0F, 10.0F), UnityEngine.Random.Range(-10.0F, 10.0F), UnityEngine.Random.Range(-10.0F, 10.0F));

			atoms[moleculeNum].Add(gObj);
			atoms[moleculeNum][i].transform.SetParent(moleculeParent[moleculeNum].transform);
			atoms[moleculeNum][i].SetActive(false);
			atoms[moleculeNum][i].SetActive(true);
			//atoms[moleculeNum][i].AddComponent<Rigidbody>();
			//Destroy(atoms[moleculeNum][i].GetComponent<SphereCollider>());
		}

		// if the bonds might be visible render them otherwise don't
		//TODO: try replacing them with billboarded quads?
		if(atomRadiusScale < 1.0f)
		{
			int bondCount = 0;
			for(int i = 0; i < indices[moleculeNum].Count; i += 2)
			{
				
				bonds[moleculeNum].Add(GameObject.CreatePrimitive(PrimitiveType.Cube));
				bonds[moleculeNum][bondCount].isStatic = true;
				bonds[moleculeNum][bondCount].name = "Bond " + bondCount;
				bonds[moleculeNum][bondCount].transform.SetParent(moleculeParent[moleculeNum].transform);

				Vector3 a0 = atomPositions[moleculeNum][indices[moleculeNum][i]];
				Vector3 a1 = atomPositions[moleculeNum][indices[moleculeNum][i+1]];
				Vector3 direction = a1 - a0;
				//float length = Vector3.Distance(a0, a1);//Math.Abs(direction.magnitude);

				bonds[moleculeNum][bondCount].transform.localPosition = new Vector3(0f, 1f, 0f);
				bonds[moleculeNum][bondCount].transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
				bonds[moleculeNum][bondCount].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Elements/" + atomNames[moleculeNum][indices[moleculeNum][i]] + "_mat") as Material;
				//bonds[moleculeNum][bondCount].GetComponent<MeshRenderer>().material = chemicalMat;
				//bonds[moleculeNum][bondCount].GetComponent<MeshRenderer>().material.color = atomColours[moleculeNum][indices[moleculeNum][i]];

				bonds[moleculeNum][bondCount].GetComponent<MeshRenderer>().receiveShadows = false;
				bonds[moleculeNum][bondCount].GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;


				bonds[moleculeNum][bondCount].transform.position = a0 + 0.25f*direction;

				//bonds[moleculeNum][bondCount].transform.position = 0.5f * (a0 + a1);
				//var delta = a0 - a1;
				//bonds[moleculeNum][bondCount].transform.position += delta;

				// Match the scale to the distance
				float cubeDistance = 0.5f*Vector3.Distance(a0, a1);
				bonds[moleculeNum][bondCount].transform.localScale = new Vector3(bonds[moleculeNum][bondCount].transform.localScale.x, cubeDistance, bonds[moleculeNum][bondCount].transform.localScale.z);

				// Make the cube look at the main point.
				// Since the cube is pointing up(y) and the forward is z, we need to offset by 90 degrees.
				bonds[moleculeNum][bondCount].transform.LookAt(a1, Vector3.up);
				bonds[moleculeNum][bondCount].transform.rotation *= Quaternion.Euler(90, 0, 0);

				bonds[moleculeNum][bondCount].SetActive(false);
				bonds[moleculeNum][bondCount].SetActive(true);
				//Destroy(bonds[moleculeNum][bondCount].GetComponent<Collider>());
				bondCount++;
			}

			Debug.Log("Bond count " + bondCount);
			//Debug.Log("Reduced Indices: " + reducedIndices[moleculeNum].Count + "   Indices: " + indices[moleculeNum].Count);
		}

		StaticBatchingUtility.Combine(moleculeParent[moleculeNum]);

	}

	void calculateCentroid(int moleculeNum)
	{
		if (molecule[moleculeNum].Count == 0)
		{
			Debug.Log("Error: No elements found in main molecule to generate centroid from.");
			return;
		}

		// extract the atom positions and colours (based on main molecule)
		for (int i = 0; i < molecule[moleculeNum].Count; i++)
		{
			// calculate the fractional positions
			Vector3 fracPosition = new Vector3();

			fracPosition.x = Vector3.Dot(a_axis, molecule[moleculeNum][i].position);
			fracPosition.y = Vector3.Dot(b_axis, molecule[moleculeNum][i].position);
			fracPosition.z = Vector3.Dot(c_axis, molecule[moleculeNum][i].position);

			// save the frac position to the molecule
			molecule[moleculeNum][i].fracPosition = fracPosition;



			// this is to work out centroid based on the cartesian coordinates
			if (moleculeNum == mainMolecule)
			{
				// determine the max and min vertex for the bounding box
				minimum.x = Math.Min(minimum.x, molecule[moleculeNum][i].position.x);
				minimum.y = Math.Min(minimum.y, molecule[moleculeNum][i].position.y);
				minimum.z = Math.Min(minimum.z, molecule[moleculeNum][i].position.z);

				maximum.x = Math.Max(maximum.x, molecule[moleculeNum][i].position.x);
				maximum.y = Math.Max(maximum.y, molecule[moleculeNum][i].position.y);
				maximum.z = Math.Max(maximum.z, molecule[moleculeNum][i].position.z);
			}


			// this is to work out centroid based on the fractional coordinates
			if (moleculeNum == mainMolecule)
			{
				// determine the max and min vertex for the bounding box
				minimumFrac.x = Math.Min(minimumFrac.x, molecule[moleculeNum][i].fracPosition.x);
				minimumFrac.y = Math.Min(minimumFrac.y, molecule[moleculeNum][i].fracPosition.y);
				minimumFrac.z = Math.Min(minimumFrac.z, molecule[moleculeNum][i].fracPosition.z);

				maximumFrac.x = Math.Max(maximumFrac.x, molecule[moleculeNum][i].fracPosition.x);
				maximumFrac.y = Math.Max(maximumFrac.y, molecule[moleculeNum][i].fracPosition.y);
				maximumFrac.z = Math.Max(maximumFrac.z, molecule[moleculeNum][i].fracPosition.z);
			}

		}

		/*Determine the bounding box vertices*/
		if (moleculeNum == mainMolecule)
		{
			// determine the centroid in fractional coordinates
			moleculeCentroidFrac = new Vector3(minimumFrac.x + maximumFrac.x, minimumFrac.y + maximumFrac.y, minimumFrac.z + maximumFrac.z) / 2.0f;


			// determine the offset from the origin and adapt minimum and maximum
			midpointOffset = new Vector3(minimum.x + maximum.x, minimum.y + maximum.y, minimum.z + maximum.z) / 2.0f;
			minimum = minimum - (new Vector4(midpointOffset.x, midpointOffset.y, midpointOffset.z, 0.0f));
			maximum = maximum - (new Vector4(midpointOffset.x, midpointOffset.y, midpointOffset.z, 0.0f));


			// calculate scaling percentage and update max and min
			float maxVertex = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(maximum.x, maximum.y), maximum.z), -minimum.x), -minimum.y), -minimum.z);
			scaleFactor = new Vector3(1.5f / maxVertex, 1.5f / maxVertex, 1.5f / maxVertex); // 1.5 is the max offset from the origin. TODO: make this adjustable
			//minimum *= 2.0f / maxVertex;
			//maximum *= 2.0f / maxVertex;

			// reset min and max
			//minimum = minimum + Vector4(midpointOffset.x, midpointOffset.y, midpointOffset.z, 0.0f);
			//maximum = maximum + Vector4(midpointOffset.x, midpointOffset.y, midpointOffset.z, 0.0f);


			minimum = Matrix4x4.Scale(scaleFactor) * Matrix4x4.TRS((midpointOffset*-1.0f), Quaternion.identity, Vector3.one) * minimum;
			maximum = Matrix4x4.Scale(scaleFactor) * Matrix4x4.TRS((midpointOffset*-1.0f), Quaternion.identity, Vector3.one) * maximum;


			//FIRST CALCULATE THE midpointOffset...
			//Vector3 origin(0.0);
			Vector3 origin = new Vector3(midpointOffset.x * -scaleFactor.x, midpointOffset.y * -scaleFactor.y, midpointOffset.z * -scaleFactor.z);

			if(boxVertices != null && boxVertices.Count == 8)
			{
				boxVertices[4] = origin;
				boxVertices[5] = origin + cellLength_a * Vector3.Scale(a_axis_norm, scaleFactor);
				boxVertices[7] = origin + cellLength_b * Vector3.Scale(b_axis_norm, scaleFactor);
				boxVertices[0] = origin + cellLength_c * Vector3.Scale(c_axis_norm, scaleFactor);

				boxVertices[1] = boxVertices[0] + boxVertices[5] - origin; // the subtraction is to remove the offset present in both other vertices
				boxVertices[2] = boxVertices[1] + boxVertices[7] - origin; // the subtraction is to remove the offset present in both other vertices
				boxVertices[3] = boxVertices[0] + boxVertices[7] - origin; // the subtraction is to remove the offset present in both other vertices
				boxVertices[6] = boxVertices[5] + boxVertices[7] - origin; // the subtraction is to remove the offset present in both other vertices
			}
			else
			{
				Debug.Log("Error: boxVertices not correctly initialized or setup");
			}

			//TODO: assign the new box vertices back to the actual bounding box (note that the above calculations creates unit cell's dimensions.
			//TODO: is this the best? or should I just use collision detection
			//boundingBox.transform.position = midpointOffset;
			//boundingBox.transform.localScale = new Vector3(maximum.x-minimum.x, maximum.y-minimum.y, maximum.z-minimum.z);
		}

	}

	void initElementMapping()
	{
		elementNum["H"] = 1;
		elementNum["He"] = 2;
		elementNum["Li"] = 3;
		elementNum["Be"] = 4;
		elementNum["B"] = 5;
		elementNum["C"] = 6;
		elementNum["N"] = 7;
		elementNum["O"] = 8;
		elementNum["F"] = 9;
		elementNum["Ne"] = 10;
		elementNum["Na"] = 11;
		elementNum["Mg"] = 12;
		elementNum["Al"] = 13;
		elementNum["Si"] = 14;
		elementNum["P"] = 15;
		elementNum["S"] = 16;
		elementNum["Cl"] = 17;
		elementNum["Ar"] = 18;
		elementNum["K"] = 19;
		elementNum["Ca"] = 20;
		elementNum["Sc"] = 21;
		elementNum["Ti"] = 22;
		elementNum["V"] = 23;
		elementNum["Cr"] = 24;
		elementNum["Mn"] = 25;
		elementNum["Fe"] = 26;
		elementNum["Co"] = 27;
		elementNum["Ni"] = 28;
		elementNum["Cu"] = 29;
		elementNum["Zn"] = 30;
		elementNum["Ga"] = 31;
		elementNum["Ge"] = 32;
		elementNum["As"] = 33;
		elementNum["Se"] = 34;
		elementNum["Br"] = 35;
		elementNum["Kr"] = 36;
		elementNum["Rb"] = 37;
		elementNum["Sr"] = 38;
		elementNum["Y"] = 39;
		elementNum["Zr"] = 40;
		elementNum["Nb"] = 41;
		elementNum["Mo"] = 42;
		elementNum["Rc"] = 43;
		elementNum["Ru"] = 44;
		elementNum["Rh"] = 45;
		elementNum["Pd"] = 46;
		elementNum["Ag"] = 47;
		elementNum["Cd"] = 48;
		elementNum["In"] = 49;
		elementNum["Sn"] = 50;
		elementNum["Sb"] = 51;
		elementNum["Te"] = 52;
		elementNum["I"] = 53;
		elementNum["Xe"] = 54;
		elementNum["Cs"] = 55;
		elementNum["Ba"] = 56;
		elementNum["La"] = 57;
		elementNum["Ce"] = 58;
		elementNum["Pr"] = 59;
		elementNum["Nd"] = 60;
		elementNum["Pm"] = 61;
		elementNum["Sm"] = 62;
		elementNum["Eu"] = 63;
		elementNum["Gd"] = 64;
		elementNum["Tb"] = 65;
		elementNum["Dy"] = 66;
		elementNum["Ho"] = 67;
		elementNum["Er"] = 68;
		elementNum["Tm"] = 69;
		elementNum["Yb"] = 70;
		elementNum["Lu"] = 71;
		elementNum["Hf"] = 72;
		elementNum["Ta"] = 73;
		elementNum["W"] = 74;
		elementNum["Re"] = 75;
		elementNum["Os"] = 76;
		elementNum["Ir"] = 77;
		elementNum["Pt"] = 78;
		elementNum["Au"] = 79;
		elementNum["Hg"] = 80;
		elementNum["Tl"] = 81;
		elementNum["Pb"] = 82;
		elementNum["Bi"] = 83;
		elementNum["Po"] = 84;
		elementNum["At"] = 85;
		elementNum["Rn"] = 86;
		elementNum["Fr"] = 87;
		elementNum["Ra"] = 88;
		elementNum["Ac"] = 89;
		elementNum["Th"] = 90;
		elementNum["Pa"] = 91;
		elementNum["U"] = 92;
		elementNum["Np"] = 93;
		elementNum["Pu"] = 94;
		elementNum["Am"] = 95;
		elementNum["Cm"] = 96;
		elementNum["Bk"] = 97;
		elementNum["Cf"] = 98;
		elementNum["Es"] = 99;
		elementNum["Fm"] = 100;
		elementNum["Md"] = 101;
		elementNum["No"] = 102;
		elementNum["Lr"] = 103;
		elementNum["Rf"] = 104;
		elementNum["Db"] = 105;
		elementNum["Sg"] = 106;
		elementNum["Bh"] = 107;
		elementNum["Hs"] = 108;
		elementNum["Mt"] = 109;
		elementNum["Ds"] = 110;
		elementNum["Rg"] = 111;
		elementNum["Cn"] = 112;
		elementNum["Uut"] = 113;
		elementNum["Fl"] = 114;
		elementNum["Uup"] = 115;
		elementNum["Lv"] = 116;
		elementNum["Uus"] = 117;
		elementNum["Uuo"] = 118;
	}

	Color getElementColour(int elemNumber)
	{
		//JMol colours
		Color[] elemColours = new Color[]{ new Color(255, 255, 255), new Color(217, 255, 255), new Color(204, 128, 255), new Color(194, 255, 0), new Color(255, 181, 181), new Color(144, 144, 144), new Color(48, 80, 248), new Color(255, 13, 13),
			new Color(144, 224, 80), new Color(179, 227, 245), new Color(171, 92, 242), new Color(138, 255, 0), new Color(191, 166, 166), new Color(240, 200, 160), new Color(255, 128, 0), new Color(255, 255, 48),
			new Color(31, 240, 31), new Color(128, 209, 227), new Color(143, 64, 212), new Color(61, 255, 0), new Color(230, 230, 230), new Color(191, 194, 199), new Color(166, 166, 171), new Color(138, 153, 199),
			new Color(156, 122, 199), new Color(224, 102, 51), new Color(240, 144, 160), new Color(80, 208, 80), new Color(200, 128, 51), new Color(125, 128, 176), new Color(194, 143, 143), new Color(102, 143, 143),
			new Color(189, 128, 227), new Color(255, 161, 0), new Color(166, 41, 41), new Color(92, 184, 209), new Color(112, 46, 176), new Color(0, 255, 0), new Color(148, 255, 255), new Color(148, 224, 224),
			new Color(115, 194, 201), new Color(84, 181, 181), new Color(59, 158, 158), new Color(36, 143, 143), new Color(10, 125, 140), new Color(0, 105, 133), new Color(192, 192, 192), new Color(255, 217, 143),
			new Color(166, 117, 115), new Color(102, 128, 128), new Color(158, 99, 181), new Color(212, 122, 0), new Color(148, 0, 148), new Color(66, 158, 176), new Color(87, 23, 143), new Color(0, 201, 0),
			new Color(112, 212, 255), new Color(255, 255, 199), new Color(217, 255, 199), new Color(199, 255, 199), new Color(163, 255, 199), new Color(143, 255, 199), new Color(97, 255, 199), new Color(69, 255, 199),
			new Color(48, 255, 199), new Color(31, 255, 199), new Color(0, 255, 156), new Color(0, 230, 117), new Color(0, 212, 82), new Color(0, 191, 56), new Color(0, 171, 36), new Color(77, 194, 255),
			new Color(77, 166, 255), new Color(33, 148, 214), new Color(38, 125, 171), new Color(38, 102, 150), new Color(23, 84, 135), new Color(208, 208, 224), new Color(255, 209, 35), new Color(184, 184, 208),
			new Color(166, 84, 77), new Color(87, 89, 97), new Color(158, 79, 181), new Color(171, 92, 0), new Color(117, 79, 69), new Color(66, 130, 150), new Color(66, 0, 102), new Color(0, 125, 0),
			new Color(112, 171, 250), new Color(0, 186, 255), new Color(0, 161, 255), new Color(0, 143, 255), new Color(0, 128, 255), new Color(0, 107, 255), new Color(84, 92, 242), new Color(120, 92, 227),
			new Color(138, 79, 227), new Color(161, 54, 212), new Color(179, 31, 212), new Color(179, 31, 186), new Color(179, 13, 166), new Color(189, 13, 135), new Color(199, 0, 102), new Color(204, 0, 89),
			new Color(209, 0, 79), new Color(217, 0, 69), new Color(224, 0, 56), new Color(230, 0, 46), new Color(235, 0, 38) };

		if (elemNumber > 109) // JMOL colours only go to 109
			return elemColours[108] / 255.0f;
		else
			return elemColours[elemNumber - 1] / 255.0f;	// -1 Math.Since elements start from 1
	}

	float getAtomRadius(int elemNumber)
	{
		float[] vdWRadius = new float[]{ 1.09f, 1.4f, 1.82f, 2f, 2f, 1.7f, 1.55f, 1.52f, 1.47f, 1.54f, 2.27f, 1.73f, 2f, 2.1f, 1.8f, 1.8f, 1.75f, 1.88f, 2.75f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 1.63f, 1.4f, 1.39f, 1.87f, 2f, 1.85f, 1.9f, 1.85f, 2.02f,
			2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 1.63f, 1.72f, 1.58f, 1.93f, 2.17f, 2f, 2.06f, 1.98f, 2.16f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 1.72f, 1.66f, 1.55f, 1.96f, 2.02f, 2f, 2f, 2f, 2f,
			2f, 2f, 2f, 2f, 2f, 1.86f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f };

		return vdWRadius[elemNumber - 1]; // -1 Math.Since elements start from 1
	}
}