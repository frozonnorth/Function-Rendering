using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;

//standalone file browser
using SFB;
using System;

public class Main : MonoBehaviour
{
    public Dropdown ModeInputField;

    #region Main Input Fields
    public InputField ObjectNameInputField;
    public InputField CodeInputField;
    #endregion

    #region Explicit Input Fields
    public InputField ParameterDomainInputField;
    public InputField ParameterResolutionInputField;
    #endregion

    #region Implicit Input Fields
    public InputField BoundingBoxSizeInputField;
    public InputField BoundingBoxCenterInputField;
    public InputField BoundingBoxResolutionInputField;
    #endregion

    #region Extra Inputs Fields
    public Toggle WireframeToggle;
    public InputField DiffuseColorInputField;
    public InputField SpecularColorInputField;
    public InputField TransparencyInputField;
    public InputField ShininessInputField;
    #endregion

    #region Directory stuff
    public InputField WorkingProjectPath; 
    public GameObject DirectoryContainerGO;

    public GameObject DirectoryItemPrefab;
    List<DirectoryItem> directorylist = new List<DirectoryItem>();

    public GameObject fileItemPrefab;
    List<FileItem> filelist = new List<FileItem>();
    #endregion

    #region Object Selection Tab
    public GameObject ObjectTabContainerGO;
    public GameObject TabPrefab;
    #endregion

    #region Program Settings
    const string SaveFileExtention = ".Func";//The file extention name to be saved
    #endregion

    #region Camera
    public EditorCamera viewcamera;
    #endregion

    public Material SolidMeshObjectMaterial;
    public Material WireframeMeshObjectMaterial;

    //Stored Refrences
    GameObject currentObject;
    MeshRenderer currentObjectMeshRenderer;
    Tab currentObjectTab;

    #region Singleton - for other scripts to access the functions of this script easily
    public static Main Instance;
    void Awake()
    {
        //Ensure that theres only one instance of the Singleton class, otherwise destory it
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }
    #endregion 

    //This Runs when game Starts
    void Start()
    {
        //Create a new gameobject and attach a MeshFilter component to it. Keep in variable for refrence
        //MeshFilter component is required to asign and change mesh
        currentObject = new GameObject("GeneratedMeshObject", typeof(MeshFilter));

        //Attach a MeshRenderer component to the new GameObject.
        //MeshRenderer component is required to display the mesh and to change the materials/apparence.
        currentObjectMeshRenderer = currentObject.AddComponent<MeshRenderer>();

        //Set Camera to Examine Mode at start
        viewcamera.currentmode = EditorCamera.Mode.Examine;
        viewcamera.SetExamineTarget(currentObject);

        //For Windows only, we want to check if we had previously saved the last project path
        //Update directory to the last project path
        #if UNITY_STANDALONE_WIN
        string path = PlayerPrefs.GetString("LastProjectPath","nopath");
        if(path != "nopath")
        {
            WorkingProjectPath.text = path;
            UpdateDirectory(WorkingProjectPath.text);
        }

        //For Android, //The project path will always be Application.persistentDataPath, 
        //This is the path allocated by the android device for the application
        #elif UNITY_ANDROID
        WorkingProjectPath.text = Application.persistentDataPath
        UpdateDirectory(WorkingProjectPath.text);
        #endif
    }

    #region Directory and Files Click Events
    //Event to be trigger when Directory Change Button is Click
    public void OnProjectDirectoryChange()
    {
        string[] paths;

        //Only for Windows
        #if UNITY_STANDALONE_WIN || UNITY_WEBGL

        //Uses StandaloneFileBrowser class to help open up a folder 
        //This opens up a popup window in the native OS - Windows or Mac PC
        paths = StandaloneFileBrowser.OpenFolderPanel("Select Project Folder", PlayerPrefs.GetString("LastProjectPath", Application.dataPath), false);

        //If User has not selected a path, and just exited the popup window, do nothing
        if (paths.Length == 0) return;

        //Otherwise change the inputfield text to show the selected path
        WorkingProjectPath.text = paths[0];

        //save the current path choosen path into playerpref (PC/MAC save file) to be automatically open next execution
        PlayerPrefs.SetString("LastProjectPath", paths[0]);

        #elif UNITY_ANDROID
        //For Android, do nothing
        #endif

        UpdateDirectory(WorkingProjectPath.text);
    }
    void UpdateDirectory(string path)
    {
        //Remove all existing directory item
        foreach (DirectoryItem item in directorylist)
        {
            Destroy(item.gameObject);
        }
        directorylist.Clear();

        //Remove any existing file object
        foreach (FileItem item in filelist)
        {
            Destroy(item.gameObject);
        }
        filelist.Clear();

        //Create and display all the directories in the project path
        DirectoryInfo directoryinfo = new DirectoryInfo(path);
        DirectoryInfo[] directoryinfoarray = directoryinfo.GetDirectories();
        for (int i = 0; i < directoryinfoarray.Length; i++)
        {
            DirectoryItem di = Instantiate(DirectoryItemPrefab).GetComponent<DirectoryItem>();
            di.directoryinfo = directoryinfoarray[i];
            di.SetName(directoryinfoarray[i].Name);
            di.SetHierachyLevel(0);
            di.gameObject.name = directoryinfoarray[i].Name;
            di.AddButtononClickEvent();
            di.transform.SetParent(DirectoryContainerGO.transform, false);

            directorylist.Add(di);
        }

        //Create and display all the files specific to the extention type in the project path
        FileInfo[] fileinfoarray = directoryinfo.GetFiles("*" + SaveFileExtention);
        for (int i = 0; i < fileinfoarray.Length; i++)
        {
            FileItem fi = Instantiate(fileItemPrefab).GetComponent<FileItem>();
            fi.fileinfo = fileinfoarray[i];
            fi.SetName(fileinfoarray[i].Name);
            fi.SetHierachyLevel(0);
            fi.gameObject.name = fileinfoarray[i].Name;
            fi.AddButtononClickEvent();
            fi.transform.SetParent(DirectoryContainerGO.transform, false);

            filelist.Add(fi);
        }
    }
    //Event to be trigger when Directory is Click
    public void OnDirectoryClick(DirectoryItem directoryitem)
    {
        //If the current directory that was click is already expanded,
        //Remove all child directory and all child files
        if (directoryitem.expanded)
        {
            directoryitem.RemoveChildDirectory();
            directoryitem.RemoveChildFile();
            directoryitem.expanded = false;
            directoryitem.UpdateIcon();
        }

        //Otherwise expand the current directory
        else
        {
            //Find and Populate child directory
            DirectoryInfo[] directoryinfoarray = directoryitem.directoryinfo.GetDirectories();
            for (int i = 0; i < directoryinfoarray.Length; i++)
            {
                DirectoryItem di = Instantiate(DirectoryItemPrefab).GetComponent<DirectoryItem>();
                di.directoryinfo = directoryinfoarray[i];
                di.SetName(directoryinfoarray[i].Name);
                di.SetHierachyLevel(directoryitem.hierarchylevel + 1);
                di.gameObject.name = directoryinfoarray[i].Name;
                di.AddButtononClickEvent();
                di.transform.SetParent(directoryitem.transform, false);

                directoryitem.childdirectoryitem.Add(di);
            }

            //Find and Populate child files
            FileInfo[] fileinfoarray = directoryitem.directoryinfo.GetFiles("*"+ SaveFileExtention);
            for (int i = 0; i < fileinfoarray.Length; i++)
            {
                FileItem fi = Instantiate(fileItemPrefab).GetComponent<FileItem>();
                fi.fileinfo = fileinfoarray[i];
                fi.SetName(fileinfoarray[i].Name);
                fi.SetHierachyLevel(directoryitem.hierarchylevel + 1);
                fi.gameObject.name = fileinfoarray[i].Name;
                fi.AddButtononClickEvent();
                fi.transform.SetParent(directoryitem.transform, false);

                directoryitem.childfileitem.Add(fi);
            }

            directoryitem.expanded = true;
            directoryitem.UpdateIcon();
        }
    }
    //Event to be trigger when File is Click
    public void OnFileClick(FileItem file)
    {
        //Add new tab when user clicks a file
        AddTab(file.fileinfo);
    }
    #endregion


    Tab CreateNewTab(string name = "New Tab")
    {
        Tab newtab = Instantiate(TabPrefab).GetComponent<Tab>();
        newtab.transform.SetParent(ObjectTabContainerGO.transform, false);
        newtab.SetText(name);
        newtab.gameObject.name = name;
        newtab.ObjectName = name;
        newtab.AddButtonOnClickEvent();
        return newtab;
    }
    void AddTab(FileInfo file)
    {
        string name = file.Name.Remove(file.Name.Length - SaveFileExtention.Length);
        currentObjectTab = CreateNewTab(name);

        #region Load saved file data after clicking on the file into tab
        StreamReader sreader = file.OpenText();
        string line = sreader.ReadLine();
        int linecounter = 0;
        while (line != null)
        {
            try
            {
                if (line.Contains("#"))
                {
                    //ignore the line that has #, which is comment,
                    //User can comment the save file using #
                }

                //find mode
                else if (line.Contains("mode"))
                {
                    line = line.Replace("mode", "");
                    currentObjectTab.ObjectMode = int.Parse(line.Trim());
                }

                //find "parameters"
                else if (line.Contains("parameters"))
                {
                    line = line.Replace("parameters", "");
                    currentObjectTab.ParameterDomain = line.Trim();
                }

                //find "resolution"
                else if (line.Contains("resolution"))
                {
                    line = line.Replace("resolution", "");
                    currentObjectTab.Resolution = line.Trim();
                }

                //find "definition"
                else if (line.Contains("definition"))
                {
                    line = line.Replace("definition", "");
                    int paraentesiscount = line.Length - line.Replace("\"", "").Length;
                    currentObjectTab.Code = line.Trim();
                    while (line != "")
                    {
                        if (paraentesiscount >= 2)
                        {
                            break;
                        }
                        line = sreader.ReadLine();
                        currentObjectTab.Code += '\n' + line.Trim();
                        paraentesiscount += line.Length - line.Replace("\"", "").Length;
                    }
                    currentObjectTab.Code = currentObjectTab.Code.Replace("\"", "");
                }

                //find "bounding box size"
                else if (line.Contains("bboxSize"))
                {
                    line = line.Replace("bboxSize", "");
                    currentObjectTab.MarchingBoundingBoxSize = line.Trim();
                }

                //find "bounding box center"
                else if (line.Contains("bboxCenter"))
                {
                    line = line.Replace("bboxCenter", "");
                    currentObjectTab.MarchingBoundingBoxCenter = line.Trim();
                }

                //find "bounding box resolution"
                else if (line.Contains("bboxResolution"))
                {
                    line = line.Replace("bboxResolution", "");
                    currentObjectTab.BoundingBoxResolution = line.Trim();
                }

                //find "bounding box resolution"
                else if (line.Contains("bboxResolution"))
                {
                    line = line.Replace("bboxResolution", "");
                    currentObjectTab.BoundingBoxResolution = line.Trim();
                }

                //find "diffuseColor"
                else if (line.Contains("diffuseColor"))
                {
                    line = line.Replace("diffuseColor", "");
                    currentObjectTab.DiffuseColor = line.Trim();
                }

                //find "diffuseColor"
                else if (line.Contains("SpecularColor"))
                {
                    line = line.Replace("SpecularColor", "");
                    currentObjectTab.SpecularColor = line.Trim();
                }

                //find "Transparency"
                else if (line.Contains("Transparency"))
                {
                    line = line.Replace("Transparency", "");
                    currentObjectTab.Transparency = line.Trim();
                }

                //find "Shininess"
                else if (line.Contains("Shininess"))
                {
                    line = line.Replace("Shininess", "");
                    currentObjectTab.Shininess = line.Trim();
                }

                line = sreader.ReadLine(); // next line

                linecounter++;
            }
            catch
            {
                DisplayMessage("Error Loading File:"+ file.FullName + "at line:"+ linecounter);
            }
        }
        sreader.Close();
        #endregion

        //after loading, we want to change the current input fields to the reflect loaded data
        CopyTabInfoToInputField(currentObjectTab);

        CreateParametricMeshFromTab(currentObjectTab);
    }


    #region ModeDropdown Selection Event
    public void OnModeChange(int mode)
    {
        //0 = explicit , 1= implicit
        if (mode == 0)
        {
            //Enable explict input fields
            ParameterDomainInputField.transform.parent.gameObject.SetActive(true);
            ParameterResolutionInputField.transform.parent.gameObject.SetActive(true);

            //Disable implicit input fields
            BoundingBoxSizeInputField.transform.parent.gameObject.SetActive(false);
            BoundingBoxCenterInputField.transform.parent.gameObject.SetActive(false);
            BoundingBoxResolutionInputField.transform.parent.gameObject.SetActive(false);
        }
        else if (mode == 1)
        {
            //Enable explict input fields
            ParameterDomainInputField.transform.parent.gameObject.SetActive(false);
            ParameterResolutionInputField.transform.parent.gameObject.SetActive(false);

            //Disable implicit input fields
            BoundingBoxSizeInputField.transform.parent.gameObject.SetActive(true);
            BoundingBoxCenterInputField.transform.parent.gameObject.SetActive(true);
            BoundingBoxResolutionInputField.transform.parent.gameObject.SetActive(true);
        }
    }
    #endregion

    #region Tab Click and Tab Close Button Click event
    public void OnTabClick(Tab tab)
    {
        currentObjectTab = tab;

        CopyTabInfoToInputField(currentObjectTab);

        CreateParametricMeshFromTab(currentObjectTab);
    }
    public void OnTabCloseButton(Tab tab)
    {
        //if theres only one tab
        if (currentObjectTab.transform.parent.childCount == 1)
        {
            Destroy(currentObjectTab.gameObject);
            currentObjectTab = null;

            ObjectNameInputField.text = "New Object";
            ModeInputField.value = 0;
            OnModeChange(ModeInputField.value);

            CodeInputField.text = "";

            ParameterDomainInputField.text = "[0 1 0 1 0 1]";
            ParameterResolutionInputField.text = "[75 75 75]";

            BoundingBoxSizeInputField.text = "[100 100 100]";
            BoundingBoxCenterInputField.text = "[0 0 0]";
            BoundingBoxResolutionInputField.text = "[0 1 0 1 0 1]";

            currentObject.GetComponent<MeshFilter>().mesh = null;
        }
        //if we trying to close the current selected tab
        else if (currentObjectTab == tab)
        {
            //set current tab to neighbour tab, and close current tab
            int index = currentObjectTab.transform.GetSiblingIndex();

            //if current tab is last tab in list
            if (index == currentObjectTab.transform.parent.childCount - 1)
            {
                //Get the previous tab
                Tab neighbourtab = currentObjectTab.transform.parent.GetChild(index - 1).gameObject.GetComponent<Tab>();
                //Destory current tab
                Destroy(currentObjectTab.gameObject);
                //Change to neighbourtab
                currentObjectTab = neighbourtab;
                //copy tab info to inputfield
                CopyTabInfoToInputField(currentObjectTab);
                //generate mesh from tab
                CreateParametricMeshFromTab(currentObjectTab);
            }
            else
            {
                //Get the previous tab
                Tab neightbourtab = currentObjectTab.transform.parent.GetChild(index + 1).gameObject.GetComponent<Tab>();
                //Destory current tab
                Destroy(currentObjectTab.gameObject);
                //Change to neighbourtab
                currentObjectTab = neightbourtab;
                //copy tab info to inputfield
                CopyTabInfoToInputField(currentObjectTab);
                //generate mesh from tab
                CreateParametricMeshFromTab(currentObjectTab);
            }
        }
        // if we trying to close a tab that is not selected
        else
        {
            //Just destory only
            Destroy(tab.gameObject);
        }
    }
    #endregion

    #region Extra Input Fields On Change Event (For user Input validation)
    public void OnDiffuseColorChange()
    {
        //Accepted format: R G B
        //Forced R G B to be value between 0 - 1
        string text = DiffuseColorInputField.text;
        string[] colorarray = text.Split(' ');
        try
        {
            if (colorarray.Length == 3)
            {
                float R = Mathf.Clamp01(float.Parse(colorarray[0].Trim(' ')));
                float G = Mathf.Clamp01(float.Parse(colorarray[1].Trim(' ')));
                float B = Mathf.Clamp01(float.Parse(colorarray[2].Trim(' ')));
                DiffuseColorInputField.text = R + " " + G + " " + B;
            }
            else
            {
                throw new Exception();
            }
        }
        catch
        {
            DisplayMessage("Error in Diffuse Color input format:\"R G B\"");
            return;
        }
    }
    public void OnSpecularColorChange()
    {
        //Accepted format: R G B
        //Forced R G B to be value between 0 - 1
        string text = SpecularColorInputField.text;
        string[] colorarray = text.Split(' ');
        try
        {
            if (colorarray.Length == 3)
            {
                float R = Mathf.Clamp01(float.Parse(colorarray[0].Trim(' ')));
                float G = Mathf.Clamp01(float.Parse(colorarray[1].Trim(' ')));
                float B = Mathf.Clamp01(float.Parse(colorarray[2].Trim(' ')));
                SpecularColorInputField.text = R + " " + G + " " + B;
            }
            else
            {
                throw new Exception();
            }
            
        }
        catch
        {
            DisplayMessage("Error in Specular Color input format:\"R G B\"");
            return;
        }
    }
    public void OnTransparencyChange()
    {
        //Accepted format: #
        //Forced # to be value between 0 - 1
        string text = TransparencyInputField.text;
        try
        {
            float transparencyvalue = Mathf.Clamp01(float.Parse(text));
            TransparencyInputField.text = transparencyvalue.ToString();
        }
        catch
        {
            DisplayMessage("Error in Transparency Input Field");
            return;
        }
    }
    public void OnShininessChange()
    {
        //Accepted format: #
        //Forced # to be value between 0 - 1
        string shinytext = ShininessInputField.text;
        try
        {
            float shininess = Mathf.Clamp01(float.Parse(shinytext.Trim(' ')));
            ShininessInputField.text = shininess.ToString();
        }
        catch
        {
            DisplayMessage("Error in Shininess Input Field");
            return;
        }
    }
    #endregion

    #region Run/Save/Load Button Clicks
    public void OnRunClick()
    {
        if (currentObjectTab == null)
        {
            currentObjectTab = CreateNewTab();
        }

        CopyAllInputFieldInfoToTab(currentObjectTab);

        CreateParametricMeshFromTab(currentObjectTab);

        viewcamera.SetExamineTarget(currentObject);
    }
    //Save the data currently on the input field, Not the current Tab
    public void OnSaveClick()
    {
        //open file browser and ask user to select the save path
#if UNITY_STANDALONE_WIN || UNITY_WEBGL
        string path = StandaloneFileBrowser.SaveFilePanel("Save File", WorkingProjectPath.text, ObjectNameInputField.text, SaveFileExtention.TrimStart('.'));
        if (path == "") return;
        FileInfo file = new FileInfo(path);
#elif UNITY_ANDROID
        FileInfo file = new FileInfo(Application.persistentDataPath + "/" + ObjectNameInputField.text + SaveFileExtention);
#endif

        #region write data
        StreamWriter swriter;
        if (!file.Exists)
        {
            swriter = file.CreateText();
        }
        else
        {
            file.Delete();
            swriter = file.CreateText();
        }

        int mode = ModeInputField.value;
        //0 - explicit , 1 - implicit
        if (mode == 0)
        {
            swriter.WriteLine
            (
                "mode " + ModeInputField.value + "\n" +
                "definition \"" + CodeInputField.text.Replace("\n", "\n\t\t\t") + "\"\n" +
                "parameters " + ParameterDomainInputField.text + "\n" +
                "resolution " + ParameterResolutionInputField.text + "\n" +

                "diffuseColor " + DiffuseColorInputField.text + "\n" +
                "SpecularColor " + SpecularColorInputField.text + "\n" +
                "Transparency " + TransparencyInputField.text + "\n" +
                "Shininess " + ShininessInputField.text
            );
        }
        else if (mode == 1)
        {
            swriter.WriteLine
            (
                "mode " + ModeInputField.value + "\n" +
                "definition \"" + CodeInputField.text.Replace("\n", "\n\t\t\t") + "\"\n" +
                "bboxSize " + BoundingBoxSizeInputField.text + "\n" +
                "bboxCenter " + BoundingBoxCenterInputField.text + "\n" +
                "bboxResolution " + BoundingBoxResolutionInputField.text + "\n" +

                "diffuseColor " + DiffuseColorInputField.text + "\n" +
                "SpecularColor " + SpecularColorInputField.text + "\n" +
                "Transparency " + TransparencyInputField.text + "\n" +
                "Shininess " + ShininessInputField.text
            );
        }

        swriter.Close();
        #endregion

        if(WorkingProjectPath.text == "")
        {
            WorkingProjectPath.text = file.FullName.Substring(0,file.FullName.Length - file.Name.Length);
        }

        //update project directory after saving is done
        UpdateDirectory(WorkingProjectPath.text);
    }
    public void OnLoadClick()
    {
        string[] paths;
#if UNITY_STANDALONE_WIN || UNITY_WEBGL
        paths = StandaloneFileBrowser.OpenFilePanel("Select File", WorkingProjectPath.text, SaveFileExtention.TrimStart('.'), false);
        if (paths.Length == 0) return;
#elif UNITY_ANDROID
        paths = new string[] { Application.persistentDataPath + "/" + ObjectNameInputField.text + SaveFileExtention };
#endif

        FileInfo file = new FileInfo(paths[0]);
       
        AddTab(file);
    }
#endregion

    //note, in order to create a single mesh from code, we have to adhere to the limitation the indexbuffer
    //which is if the device can support 32bit index buffer or not. otherwise
    //have to generate multiple meshes from code 
    void CreateParametricMeshFromTab(Tab tab)
    {
        string errorMessages = string.Empty;

        FunctionGeneratedMesh parametricMesh;
        if (!(parametricMesh = currentObject.GetComponent<FunctionGeneratedMesh>()))
        {
            parametricMesh = currentObject.AddComponent<FunctionGeneratedMesh>();
        }

        //set default value
        parametricMesh.sampleresolution_U = 1;
        parametricMesh.sampleresolution_V = 1;
        parametricMesh.sampleresolution_W = 1;

        //set Mode
        parametricMesh.mode = tab.ObjectMode;

        //0 - explicit, 1 - implicit
        if (tab.ObjectMode == 0)
        {
            #region User Input Validation On Parametric Input Parameters
            try
            {
                #region Obtain U V W domain from the tab and set into parametricmesh class 
                //Accepted Formated as: "# #" or "# # # #" or "# # # # # #"
                string parameterdomain = tab.ParameterDomain;

                string[] domains = parameterdomain.Split(' ');//split string by space
                if (domains.Length >= 2)
                {
                    //Process substring one and two
                    //convert the the string to float value as parametric U min,max domain range
                    parametricMesh.uMinDomain = float.Parse(domains[0].Trim(' '));
                    parametricMesh.uMaxDomain = float.Parse(domains[1].Trim(' '));

                    if (domains.Length >= 4)
                    {
                        //Process substring three and four
                        //convert the the string to float value as parametric V min,max domain range
                        parametricMesh.vMinDomain = float.Parse(domains[2].Trim(' '));
                        parametricMesh.vMaxDomain = float.Parse(domains[3].Trim(' '));

                        if (domains.Length >= 6)
                        {
                            //Process substring five and six
                            //convert the the string to float value as parametric W min,max domain range
                            parametricMesh.wMinDomain = float.Parse(domains[4].Trim(' '));
                            parametricMesh.wMaxDomain = float.Parse(domains[5].Trim(' '));
                        }
                    }
                }
                #endregion
            }
            catch
            {
                errorMessages += "Error in Parametric Domain input format.\n";
            }
            try
            {
                #region Obtain Parametric u v w resolution from the tab and set into parametricmesh class 
                //Accepted Formated as: "#" or "# #" or "# # #
                string resolution = tab.Resolution;
                string[] res = resolution.Split(' ');//split string by space
                if (res.Length >= 1)
                {
                    //Process substring one
                    //convert the first part of the string to int value as resolution U
                    parametricMesh.sampleresolution_U = int.Parse(res[0].Trim(' '));
                    if (res.Length >= 2)
                    {
                        //Process substring two
                        //convert the second part of the string to int value as resolution V
                        parametricMesh.sampleresolution_V = int.Parse(res[1].Trim(' '));
                        if (res.Length >= 3)
                        {
                            //Process substring three
                            //convert the third part of the string to int value as resolution W
                            parametricMesh.sampleresolution_W = int.Parse(res[2].Trim(' '));
                        }
                    }
                }
                #endregion
            }
            catch
            {
                errorMessages += "Error in Parametric Resolution input format.\n";
            }

            #region check if the resolution that user has input is something the device(pc,phone,mac) can support
            long maximumindex = 0;
            if (SystemInfo.supports32bitsIndexBuffer)
                maximumindex = 4000000000;
            else
                maximumindex = 65535;

            long estimateVerticesCount = parametricMesh.sampleresolution_U * parametricMesh.sampleresolution_V * parametricMesh.sampleresolution_W;
            if (estimateVerticesCount > maximumindex)
            {
                errorMessages += "Sampling Resolution Too Large For Device To Support.\n";
                DisplayMessage(errorMessages);
                return;
            }
            #endregion
            
            #endregion
        }
        else if (tab.ObjectMode == 1)
        {
            #region User Input Validation on Implicit input parameters
            try
            {
                #region Obtain Bounding Box Size from tab and set into parametricMesh class
                //Accepted Formated as: # # #
                string size = tab.MarchingBoundingBoxSize;
                string[] sizearray = size.Split(' ');//split string by space
                if (sizearray.Length == 3)
                {
                    //Convert each part of the substring to the x,y,z vector values as float value for BBoxsize
                    parametricMesh.MarchingBoundingBoxSize = new Vector3(float.Parse(sizearray[0].Trim(' ')), float.Parse(sizearray[1].Trim(' ')), float.Parse(sizearray[2].Trim(' ')));
                }
                else
                {
                    throw new Exception();
                }
                #endregion
            }
            catch
            {
                errorMessages += "Error in BBSize input format.\n";
            }
            try
            {
                #region obtain bounding box center from tab and set into parametricMesh class
                //Accepted Formated as: # # #
                string pos = tab.MarchingBoundingBoxCenter;
                string[] posarray = pos.Split(' ');//split string by space
                if (posarray.Length == 3)
                {
                    //Convert each part of the substring to the x,y,z vector values as float value for BBoxcenter
                    parametricMesh.MarchingBoundingBoxCenter = new Vector3(float.Parse(posarray[0].Trim(' ')), float.Parse(posarray[1].Trim(' ')), float.Parse(posarray[2].Trim(' ')));
                }
                else
                {
                    throw new Exception();
                }
                #endregion
            }
            catch
            {
                errorMessages += "Error in BBPosition input format.\n";
            }
            try
            {
                #region obtain bounding box resolution from tab and set into parametricMesh class
                //Accepted Formated as: # # #
                string res = tab.BoundingBoxResolution;
                string[] resarray = res.Split(' ');//split string by space
                if (resarray.Length == 3)
                {
                    //Convert each part of the substring to the x,y,z vector values as float value for BBox resolution
                    parametricMesh.BoundingBoxResolution = new Vector3(float.Parse(resarray[0].Trim(' ')), float.Parse(resarray[1].Trim(' ')), float.Parse(resarray[2].Trim(' ')));
                }
                else
                {
                    throw new Exception();
                }
                #endregion
            }
            catch
            {
                errorMessages += "Error in BBResolution input format.\n";
            }
            #endregion
        }

        #region User Input Validation for diffuse color and obtain value to be set later
        //Accepted format: R G B
        //Forced R G B to be value between 0 - 1
        string diffusetext = tab.DiffuseColor;
        string[] diffusecolorarray = diffusetext.Split(' ');
        float diffuse_R = 1f,diffuse_G = 1f, diffuse_B = 1f;
        try
        {
            if (diffusecolorarray.Length == 3)
            {
                diffuse_R = Mathf.Clamp01(float.Parse(diffusecolorarray[0].Trim(' ')));
                diffuse_G = Mathf.Clamp01(float.Parse(diffusecolorarray[1].Trim(' ')));
                diffuse_B = Mathf.Clamp01(float.Parse(diffusecolorarray[2].Trim(' ')));
            }
            else
            {
                throw new Exception();
            }
        }
        catch
        {
            errorMessages += "Error in Diffuse Color input format:\"R G B\".\n";
        }
        #endregion

        #region User Input Validation for specular color and obtain value to be set later
        //Accepted format: R G B
        //Forced R G B to be value between 0 - 1
        string speculartext = tab.SpecularColor;
        string[] specularcolorarray = speculartext.Split(' ');
        float specular_R = 0.4f, specular_G = 0.1f, specular_B = 0.3f;
        try
        {
            if (specularcolorarray.Length == 3)
            {
                specular_R = Mathf.Clamp01(float.Parse(specularcolorarray[0].Trim(' ')));
                specular_G = Mathf.Clamp01(float.Parse(specularcolorarray[1].Trim(' ')));
                specular_B = Mathf.Clamp01(float.Parse(specularcolorarray[2].Trim(' ')));
            }
            else
            {
                throw new Exception();
            }
        }
        catch
        {
            errorMessages += "Error in Specular Color input format:\"R G B\".\n";
        }
        #endregion

        #region User Input Validation for transparency and obtain value to be set later
        //Accepted format: #
        //Forced # to be value between 0 - 1
        string transparency = tab.Transparency;
        float transparencyvalue = 0;
        try
        {
            transparencyvalue = Mathf.Clamp01(float.Parse(transparency.Trim(' ')));
        }
        catch
        {
            errorMessages += "Error in Transparency Input Field.\n";
        }
        #endregion

        #region User Input Validation for shininess and obtain value to be set later
        //Accepted format: #
        //Forced # to be value between 0 - 1
        string shinytext = tab.Shininess;
        float shininess = 1f;
        try
        {
            shininess = Mathf.Clamp01(float.Parse(shinytext.Trim(' ')));
        }
        catch
        {
            errorMessages += "Error in Shininess Input Field.\n";
        }
        #endregion

        //stop the run if theres any error
        if (errorMessages.Length > 0)
        {
            DisplayMessage(errorMessages.TrimEnd('\n'));
            return;
        }

        //set the code
        parametricMesh.SetFomula(tab.Code);

        //set wireframe toggle
        parametricMesh.iswireframe = tab.IsWireframe;

        //set the material based on if its wireframe or solid
        if (parametricMesh.iswireframe)
            currentObjectMeshRenderer.material = new Material(WireframeMeshObjectMaterial);
        else
            currentObjectMeshRenderer.material = new Material(SolidMeshObjectMaterial);

        //set diffuse color
        currentObjectMeshRenderer.material.SetColor("_Color", new Color(diffuse_R, diffuse_G, diffuse_B, 1-transparencyvalue));

        //set specular color
        currentObjectMeshRenderer.material.SetColor("_SpecColor", new Color(specular_R, specular_G, specular_B, 1));

        //set shininess
        currentObjectMeshRenderer.material.SetFloat("_Glossiness", shininess);

        //Destroy the old mesh if it has already been generated
        if (parametricMesh.MeshGenerated == true) //if mesh is already generated
        {
            //Obtain all monobehaviour to find the runtime complied scripted called CompliedMesh
            foreach (MonoBehaviour behaviour in currentObject.GetComponents<MonoBehaviour>())
            {
                if(behaviour.GetType().ToString() == "CompliedMesh")
                {
                    DestroyImmediate(behaviour);
                }
            }
        }
        parametricMesh.StartDrawing();
    }
    void CopyAllInputFieldInfoToTab(Tab tabtocopyto)
    {
        tabtocopyto.ObjectMode = ModeInputField.value;

        tabtocopyto.ObjectName = ObjectNameInputField.text;
        tabtocopyto.SetText(ObjectNameInputField.text);
        tabtocopyto.gameObject.name = ObjectNameInputField.text;

        tabtocopyto.Code = CodeInputField.text;

        tabtocopyto.ParameterDomain = ParameterDomainInputField.text;
        tabtocopyto.Resolution = ParameterResolutionInputField.text;

        tabtocopyto.MarchingBoundingBoxSize = BoundingBoxSizeInputField.text;
        tabtocopyto.MarchingBoundingBoxCenter = BoundingBoxCenterInputField.text;
        tabtocopyto.BoundingBoxResolution = BoundingBoxResolutionInputField.text;

        tabtocopyto.DiffuseColor = DiffuseColorInputField.text;
        tabtocopyto.SpecularColor = SpecularColorInputField.text;
        tabtocopyto.Transparency = TransparencyInputField.text;
        tabtocopyto.Shininess = ShininessInputField.text;

        tabtocopyto.IsWireframe = WireframeToggle.isOn;
    }
    void CopyTabInfoToInputField(Tab tabtocopyfrom)
    {
        ModeInputField.value = tabtocopyfrom.ObjectMode;
        OnModeChange(ModeInputField.value);

        ObjectNameInputField.text = tabtocopyfrom.ObjectName;

        CodeInputField.text = tabtocopyfrom.Code;

        ParameterResolutionInputField.text = tabtocopyfrom.Resolution;
        ParameterDomainInputField.text = tabtocopyfrom.ParameterDomain;

        BoundingBoxSizeInputField.text = tabtocopyfrom.MarchingBoundingBoxSize;
        BoundingBoxCenterInputField.text = tabtocopyfrom.MarchingBoundingBoxCenter;
        BoundingBoxSizeInputField.text = tabtocopyfrom.BoundingBoxResolution;
        
        DiffuseColorInputField.text = tabtocopyfrom.DiffuseColor;
        SpecularColorInputField.text = tabtocopyfrom.SpecularColor;
        TransparencyInputField.text = tabtocopyfrom.Transparency;
        ShininessInputField.text = tabtocopyfrom.Shininess;
    }
    void DisplayMessage(string message)
    {
        PopupMessage.Instance.ShowMessage(message);
    }









    






























    void SaveGameObjectMesh(GameObject go,string locationpath,string name)
    {
        //SaveGameObjectMesh(currentobject, "GeneratedAssets", "SavedUnityObject");

        //string path = UnityEngine.Application.dataPath + "/" + locationpath;
        //if (!Directory.Exists(path))
        //{
        //    Directory.CreateDirectory(path);
        //}

        ////RuntimeSave.CreateOrReplaceAsset(go.GetComponent<MeshFilter>().mesh, "Assets/" + locationpath + "/" + name + ".mesh");
        ////RuntimeSave.CreateOrReplaceAsset(go.GetComponent<Renderer>().material, "Assets/" + locationpath + "/" + name + ".mat");
        //AssetDatabase.CreateAsset(go.GetComponent<MeshFilter>().mesh, "Assets/" + locationpath + "/" + name + ".mesh");
        //AssetDatabase.CreateAsset(go.GetComponent<Renderer>().material, "Assets/" + locationpath + "/" + name + ".mat");
        ////check if prefab exist, if not create new
        ////if(!File.Exists(Application.dataPath + "/" + locationpath + "/" + name + ".prefab"))
        ////{
        //    DestroyImmediate(go.GetComponent("CompliedMesh"));
        //    PrefabUtility.CreatePrefab("Assets/" + locationpath + "/" + name + ".prefab", go);
        //    go.GetComponent<ParametricMesh>().Start();
        ////}
    }
}
