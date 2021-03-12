using UnityEngine;
using UnityEngine.UI;
using TMPro;

using System;
using System.IO;
using System.Collections.Generic;

using SFB;
using System.Text.RegularExpressions;
using UnityEditor;

/// <summary>
/// The Main Component
/// </summary>
public class Main : MonoBehaviour
{
    #region Input Fields To Be Assign In Inspector
    public Dropdown ModeInputField;
    public InputField ObjectNameInputField;
    public TMP_InputField CodeInputField;
    //Parametric Specific Input Fields
    public InputField ParameterDomainInputField;
    public InputField ParameterResolutionInputField;
    //Implicit Specific Input Fields
    public InputField BoundingBoxSizeInputField;
    public InputField BoundingBoxCenterInputField;
    public InputField BoundingBoxResolutionInputField;
    //Apperance Specific Input Fields
    public InputField DiffuseColorInputField;
    public InputField SpecularColorInputField;
    public InputField TransparencyInputField;
    public InputField ShininessInputField;
    public InputField TimecycleInputField;
    public InputField TimerangeInputField;
    #endregion

    #region Default Values of Input Fields 
    const int DefaultMode = 0; //0-parametric, 1-implicit
    const string DefaultObjectName = "New Object";
    const string DefaultCode =
        "x=1*cos(-pi/2+u*pi)*cos(-pi+v*2*pi);\n" +
        "y=1*cos(-pi/2+u*pi)*sin(-pi+v*2*pi);\n" +
        "z=1*sin(-pi/2+u*pi);";
    //Parametric Specific
    const string DefaultParameterDomain = "0 1 0 1 0 1";
    const string DefaultParameterResolution = "30 30 30";
    //Implicit Specific
    const string DefaultBBoxSize = "10 10 10";
    const string DefaultBBoxCenter = "0 0 0";
    const string DefaultBBoxResolution = "50 50 50";
    //Apperance Specific
    public const string DefaultDiffuse = "1 1 1";
    public const string DefaultSpecular = "0.4 0.1 0.3";
    public const string DefaultTransparency = "0";
    public const string DefaultShininess = "1";
    public const string DefaultTimecycle = "1";
    public const string DefaultTimerange = "0 1";
    #endregion

    #region Directory stuff to be assign in Inspector
    public InputField WorkingProjectPath;

    public GameObject DirectoryContainerGO;
    public GameObject DirectoryItemPrefab;
    List<DirectoryItem> directorylist = new List<DirectoryItem>();

    public GameObject fileItemPrefab;
    List<FileItem> filelist = new List<FileItem>();
    #endregion 

    #region Tab Stuff to be assign in Inspector
    public GameObject TabContainerGO;
    public GameObject TabPrefab;
    #endregion

    #region Save/load Program Settings
    const string SaveFileCommentSymbol = "#";//The symbol/charater if ignore the line in the save file
    const string SaveFileExtention = ".Func";//The file extention name to be saved
    #endregion

    #region Undo/Redo Stack/Buffer
    const int maxundoredostackcount = 20;
    float recordtimer = float.NegativeInfinity;
    const float undorecorddelay = 0.25f;
    bool record = true;
    MaxStack<string> undoinputfieldstack = new MaxStack<string>(maxundoredostackcount);
    MaxStack<string> redoinputfieldstack = new MaxStack<string>(maxundoredostackcount);
    #endregion

    //Refrences to be assign in the unity inspector
    public EditorCamera viewcamera;
    public CoordinateAxes coordinateAxes;
    public Material GeneratedMeshMaterial;

    //Private Variables As Stored Refrences
    GameObject currentObject;
    MeshRenderer currentObjectMeshRenderer;
    Tab currentTab;
    bool ProgramableColors = false;

    //Extras-testing stuff
    public Material ImplicitGPUMaterial;
    public ComputeShader computeshader;

    #region Singleton - for other scripts to access the functions of this script easily
    public static Main Instance;
    void Awake()
    {
        //Ensure that theres only one instance of the Singleton class, otherwise destory it
        if (Instance == null)
        {
            Instance = this;
        }  
        else if (Instance != this)
        {
            Destroy(gameObject);
        } 
    }
    #endregion 

    //Runs when game Starts
    void Start()
    {
        //Window settings - set window fullsreen at start
        Screen.fullScreen = false;

        //Create a gameobject and attach a MeshFilter.
        //MeshFilter is required to asign and change mesh
        currentObject = new GameObject("GeneratedMeshObject", typeof(MeshFilter));

        //Attach a MeshRenderer as well.
        //MeshRenderer is required to display the mesh and change materials/apparence.
        currentObjectMeshRenderer = currentObject.AddComponent<MeshRenderer>();

        //Set Camera to Examine Mode at start
        viewcamera.currentmode = EditorCamera.Mode.Examine;
        viewcamera.SetExamineTarget(currentObject);

        //User Interfaces
        //Add support for undos
        AddCodeInputfieldsToSupportUndo();
        //Set all the user InputFields to the default values
        SetDefaultValuesToInputField();

        #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
        //We want to check if we had previously saved the project path,
        //Update directory to be the previous project path
        string path = PlayerPrefs.GetString("LastProjectPath", Application.persistentDataPath);
        if (path != "nopath")
        {
            WorkingProjectPath.text = path;
            PopulateProjectbrowser(WorkingProjectPath.text);
        }
        #elif UNITY_ANDROID
        //For Android, The project path will always be "Application.persistentDataPath"
        //This is the path allocated by the android device for the app
        WorkingProjectPath.text = Application.persistentDataPath;
        PopulateProjectbrowser(WorkingProjectPath.text);
        #endif
    }

    void CreateMeshFromTabdata(Tab tab)
    {
        string errorMessages = string.Empty;

        FunctionGeneratedMesh functiongeneratedMesh;
        if (!(functiongeneratedMesh = currentObject.GetComponent<FunctionGeneratedMesh>()))
        {
            functiongeneratedMesh = currentObject.AddComponent<FunctionGeneratedMesh>();
        }

        //Set Mode
        functiongeneratedMesh.mode = tab.ObjectMode;

        //Default Resolution Value Fallback
        functiongeneratedMesh.sampleresolution_U = 1;
        functiongeneratedMesh.sampleresolution_V = 1;
        functiongeneratedMesh.sampleresolution_W = 1;

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
                    functiongeneratedMesh.uMinDomain = float.Parse(domains[0].Trim(' '));
                    functiongeneratedMesh.uMaxDomain = float.Parse(domains[1].Trim(' '));

                    if (domains.Length >= 4)
                    {
                        //Process substring three and four
                        //convert the the string to float value as parametric V min,max domain range
                        functiongeneratedMesh.vMinDomain = float.Parse(domains[2].Trim(' '));
                        functiongeneratedMesh.vMaxDomain = float.Parse(domains[3].Trim(' '));

                        if (domains.Length >= 6)
                        {
                            //Process substring five and six
                            //convert the the string to float value as parametric W min,max domain range
                            functiongeneratedMesh.wMinDomain = float.Parse(domains[4].Trim(' '));
                            functiongeneratedMesh.wMaxDomain = float.Parse(domains[5].Trim(' '));
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
                    functiongeneratedMesh.sampleresolution_U = int.Parse(res[0].Trim(' '));
                    functiongeneratedMesh.sampleresolution_V = functiongeneratedMesh.sampleresolution_U;
                    functiongeneratedMesh.sampleresolution_W = functiongeneratedMesh.sampleresolution_U;
                    if (res.Length >= 2)
                    {
                        //Process substring two
                        //convert the second part of the string to int value as resolution V
                        functiongeneratedMesh.sampleresolution_V = int.Parse(res[1].Trim(' '));
                        if (res.Length >= 3)
                        {
                            //Process substring three
                            //convert the third part of the string to int value as resolution W
                            functiongeneratedMesh.sampleresolution_W = int.Parse(res[2].Trim(' '));
                        }
                    }
                }

                //Making sure that if any sampling resolution is zero, throw error
                if (functiongeneratedMesh.sampleresolution_U == 0 || 
                    functiongeneratedMesh.sampleresolution_V == 0 ||
                    functiongeneratedMesh.sampleresolution_W == 0)
                {
                    throw new DivideByZeroException();
                }
                #endregion
            }
            catch (DivideByZeroException e)
            {
                errorMessages += "Parametric Resolution input cannot have Zeros.\n";
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

            long estimateVerticesCount = functiongeneratedMesh.sampleresolution_U * functiongeneratedMesh.sampleresolution_V * functiongeneratedMesh.sampleresolution_W;
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
                //Accepted Formated as: #
                //or                  : # # #
                string size = tab.MarchingBoundingBoxSize;
                string[] sizearray = size.Split(' ');//split string by space
                if (sizearray.Length == 1)
                {
                    float value = float.Parse(sizearray[0]);
                    functiongeneratedMesh.MarchingBoundingBoxSize = new Vector3(value, value, value);
                }
                else if (sizearray.Length == 3)
                {
                    //Convert each part of the substring to the x,y,z vector values as float value for BBoxsize
                    functiongeneratedMesh.MarchingBoundingBoxSize = new Vector3(float.Parse(sizearray[0].Trim(' ')), float.Parse(sizearray[1].Trim(' ')), float.Parse(sizearray[2].Trim(' ')));
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
                    functiongeneratedMesh.MarchingBoundingBoxCenter = new Vector3(float.Parse(posarray[0].Trim(' ')), float.Parse(posarray[1].Trim(' ')), float.Parse(posarray[2].Trim(' ')));
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
                //Accepted Formated as: #
                //or                  : # # #
                string res = tab.BoundingBoxResolution;
                string[] resarray = res.Split(' ');//split string by space
                if (resarray.Length == 1)
                {
                    float value = float.Parse(resarray[0]);
                    functiongeneratedMesh.BoundingBoxResolution = new Vector3(value, value, value);
                }
                else if (resarray.Length == 3)
                {
                    //Convert each part of the substring to the x,y,z vector values as float value for BBox resolution
                    functiongeneratedMesh.BoundingBoxResolution = new Vector3(float.Parse(resarray[0].Trim(' ')), float.Parse(resarray[1].Trim(' ')), float.Parse(resarray[2].Trim(' ')));
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

        #region User Input Validation for diffuse color and check for programable color usage obtain value to be set later
        //Accepted format: R G B
        //Forced R G B to be value between 0 - 1
        string diffusetext = tab.DiffuseColor;
        string[] diffusecolorarray = diffusetext.Split(' ');
        float diffuse_R = 1f, diffuse_G = 1f, diffuse_B = 1f;
        string programColorRed = "1", programColorGreen = "1", programColorBlue = "1";
        try
        {
            if (diffusecolorarray.Length == 3)
            {
                diffuse_R = Mathf.Clamp01(float.Parse(diffusecolorarray[0].Trim(' ')));
                diffuse_G = Mathf.Clamp01(float.Parse(diffusecolorarray[1].Trim(' ')));
                diffuse_B = Mathf.Clamp01(float.Parse(diffusecolorarray[2].Trim(' ')));
                ProgramableColors = false;
            }
            else
            {
                throw new Exception();
            }
        }
        catch
        {
            //This happens when we cant parse the input as floats,
            //Means user has type in letters,
            //we want to check if any of the letters is any of our keywords u,v,w,t
            if (Regex.IsMatch(diffusetext, "([^a-zA-Z0-9]|[\\s]*)[uvwt]|(pi)([^a-zA-Z0-9]|[\\s]*)"))
            {
                ProgramableColors = true;
                programColorRed = diffusecolorarray[0];
                programColorGreen = diffusecolorarray[1];
                programColorBlue = diffusecolorarray[2];
            }
            else
            {
                errorMessages += "Error in Diffuse Color input format:\"R G B\".\n";
            }
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

        #region User Input Validation for Timecycle and obtain value to be set later
        //Accepted format: #
        string timecycletext = tab.Timecycle;
        try
        {
            functiongeneratedMesh.Timecycle = float.Parse(timecycletext.Trim(' '));
        }
        catch
        {
            errorMessages += "Error in Timecycle Input Field.\n";
        }
        #endregion

        #region User Input Validation for Timerange and obtain value to be set later
        //Accepted format: # #
        string timerangetext = tab.Timerange;
        string[] rangearray = timerangetext.Split(' ');
        try
        {
            if (rangearray.Length == 2)
            {
                functiongeneratedMesh.TimeMinRange = float.Parse(rangearray[0].Trim(' '));
                functiongeneratedMesh.TimeMaxRange = float.Parse(rangearray[1].Trim(' '));
            }
        }
        catch
        {
            errorMessages += "Error in Timerange Input Field.\n";
        }
        #endregion

        //stop the run if theres any error
        if (errorMessages.Length > 0)
        {
            DisplayMessage(errorMessages.TrimEnd('\n'));
            return;
        }

        //set the code
        functiongeneratedMesh.inputCode = tab.Code;

        //set material
        functiongeneratedMesh.mat = ImplicitGPUMaterial;
        functiongeneratedMesh.computeshader = computeshader;

        //set the Material
        if (ProgramableColors)
        {
            currentObjectMeshRenderer.material = ProgrammableColor.CreateMaterial(programColorRed, programColorGreen, programColorBlue);
            currentObjectMeshRenderer.material.SetFloat("_Tcycle", functiongeneratedMesh.Timecycle);
            currentObjectMeshRenderer.material.SetVector("_TRange", new Vector4(functiongeneratedMesh.TimeMinRange, functiongeneratedMesh.TimeMaxRange));
            currentObjectMeshRenderer.material.SetVector("_URange", new Vector4(0, 1));
            currentObjectMeshRenderer.material.SetVector("_VRange", new Vector4(0, 1));
            currentObjectMeshRenderer.material.SetVector("_WRange", new Vector4(0, 1));
        }
        else
        {
            currentObjectMeshRenderer.material = new Material(GeneratedMeshMaterial);
        }

        //set diffuse color + transparency
        currentObjectMeshRenderer.material.SetColor("_Color", new Color(diffuse_R, diffuse_G, diffuse_B, 1 - transparencyvalue));

        //set specular color
        currentObjectMeshRenderer.material.SetColor("_SpecColor", new Color(specular_R, specular_G, specular_B, 1));

        //set shininess
        currentObjectMeshRenderer.material.SetFloat("_Glossiness", shininess);

        //Destroy any old mesh
        RemoveExistingMesh(functiongeneratedMesh);

        //Start draag/Rendering
        functiongeneratedMesh.StartDrawing();
    }
    void RemoveExistingMesh(FunctionGeneratedMesh generatedMesh)
    {
        if (generatedMesh.MeshGenerated == true)
        {
            //Obtain all monobehaviour to find the runtime complied scripted called 'CompliedMesh'
            foreach (MonoBehaviour behaviour in currentObject.GetComponents<MonoBehaviour>())
            {
                if (behaviour.GetType().ToString() == "CompliedMesh")
                {
                    //Destory the CompliedMesh
                    //This will also trigger the function CompliedMesh.OnDestory() function
                    DestroyImmediate(behaviour);
                }
            }
        }
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

        tabtocopyto.Timecycle = TimecycleInputField.text;
        tabtocopyto.Timerange = TimerangeInputField.text;
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
        BoundingBoxResolutionInputField.text = tabtocopyfrom.BoundingBoxResolution;

        DiffuseColorInputField.text = tabtocopyfrom.DiffuseColor;
        SpecularColorInputField.text = tabtocopyfrom.SpecularColor;
        TransparencyInputField.text = tabtocopyfrom.Transparency;
        ShininessInputField.text = tabtocopyfrom.Shininess;

        TimecycleInputField.text = tabtocopyfrom.Timecycle;
        TimerangeInputField.text = tabtocopyfrom.Timerange;
    }

    //Helper function to set all inputfields to the default values
    //To change default values, change the constant values at the
    //top of main.cs
    void SetDefaultValuesToInputField()
    {
        //mode input
        ModeInputField.value = DefaultMode;
        OnModeChange(ModeInputField.value);
        //object name input
        ObjectNameInputField.text = DefaultObjectName;

        //parametric inputs
        ParameterResolutionInputField.text = DefaultParameterResolution;
        ParameterDomainInputField.text = DefaultParameterDomain;

        //implicit inputs
        BoundingBoxSizeInputField.text = DefaultBBoxSize;
        BoundingBoxCenterInputField.text = DefaultBBoxCenter;
        BoundingBoxResolutionInputField.text = DefaultBBoxResolution;

        //apperance inputs
        DiffuseColorInputField.text = DefaultDiffuse;
        SpecularColorInputField.text = DefaultSpecular;
        TransparencyInputField.text = DefaultTransparency;
        ShininessInputField.text = DefaultShininess;
        TimecycleInputField.text = DefaultTimecycle;
        TimerangeInputField.text = DefaultTimerange;

        //code input
        CodeInputField.text = DefaultCode;
    }

    //call the PopupMessageUI/component to Popup error message
    void DisplayMessage(string message)
    {
        PopupMessage.Instance.ShowMessage(message);
    }

    void PopulateProjectbrowser(string path)
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

    #region Directory and Files Click Events
    //Event to be trigger when Directory Change Button is Click
    public void OnProjectDirectoryChange()
    {
        string[] paths;

        //Only for Windows
        #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX

        //Uses StandaloneFileBrowser class to help open up a folder 
        //This opens up a popup window in the native OS - Windows or Mac PC
        paths = StandaloneFileBrowser.OpenFolderPanel("Select Project Folder", PlayerPrefs.GetString("LastProjectPath", Application.persistentDataPath), false);

        //If User has not selected a path, and just exited the popup window, do nothing
        if (paths.Length == 0 || paths[0] == "") return;
        paths[0] = paths[0].Replace("file://", "").Replace("%20", " ");

        //Otherwise change the inputfield text to show the selected path
        WorkingProjectPath.text = paths[0];

        //save the current path choosen path into playerpref (PC/MAC save file) to be automatically open next execution
        PlayerPrefs.SetString("LastProjectPath", paths[0]);

        #elif UNITY_ANDROID
        //For Android, do nothing
        #endif

        PopulateProjectbrowser(WorkingProjectPath.text);
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
            FileInfo[] fileinfoarray = directoryitem.directoryinfo.GetFiles("*" + SaveFileExtention);
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

    #region Tab, Tab Click and Tab Close Button Click event
    //Helper function to create new tab component
    Tab CreateNewTab(string name = "New Tab")
    {
        Tab newtab = Instantiate(TabPrefab).GetComponent<Tab>();
        newtab.transform.SetParent(TabContainerGO.transform, false);
        newtab.SetText(name);
        newtab.gameObject.name = name;
        newtab.ObjectName = name;
        newtab.AddButtonOnClickEvent();
        return newtab;
    }
    //function to load a new "tab" as a new object when user loads a file
    void AddTab(FileInfo file)
    {
        string name = file.Name.Remove(file.Name.Length - SaveFileExtention.Length);
        currentTab = CreateNewTab(name);

        #region Load saved file data after clicking on the file into tab
        StreamReader sreader = file.OpenText();
        string line = sreader.ReadLine();
        int linecounter = 0;
        while (line != null)
        {
            try
            {
                if (line.Contains(SaveFileCommentSymbol))
                {
                    //ignore the line that has #, which is a user comment,
                    //User can comment the save file using #
                }

                //find mode
                else if (line.Contains("mode"))
                {
                    line = line.Replace("mode", "");
                    currentTab.ObjectMode = int.Parse(line.Trim());
                }

                //find "parameters"
                else if (line.Contains("parameters"))
                {
                    line = line.Replace("parameters", "");
                    currentTab.ParameterDomain = line.Trim();
                }

                //find "resolution"
                else if (line.Contains("resolution"))
                {
                    line = line.Replace("resolution", "");
                    currentTab.Resolution = line.Trim();
                }

                //find "definition"
                else if (line.Contains("definition"))
                {
                    line = line.Replace("definition", "");
                    int paraentesiscount = line.Length - line.Replace("\"", "").Length;
                    currentTab.Code = line.Trim();
                    while (paraentesiscount < 2)
                    {
                        line = sreader.ReadLine();
                        currentTab.Code += '\n' + line.Trim();
                        paraentesiscount += line.Length - line.Replace("\"", "").Length;
                    }
                    currentTab.Code = currentTab.Code.Replace("\"", "");
                }

                //find "bounding box size"
                else if (line.Contains("bboxSize"))
                {
                    line = line.Replace("bboxSize", "");
                    currentTab.MarchingBoundingBoxSize = line.Trim();
                }

                //find "bounding box center"
                else if (line.Contains("bboxCenter"))
                {
                    line = line.Replace("bboxCenter", "");
                    currentTab.MarchingBoundingBoxCenter = line.Trim();
                }

                //find "bounding box resolution"
                else if (line.Contains("bboxResolution"))
                {
                    line = line.Replace("bboxResolution", "");
                    currentTab.BoundingBoxResolution = line.Trim();
                }

                //find "diffuseColor"
                else if (line.Contains("diffuseColor"))
                {
                    line = line.Replace("diffuseColor", "");
                    currentTab.DiffuseColor = line.Trim();
                }

                //find "diffuseColor"
                else if (line.Contains("SpecularColor"))
                {
                    line = line.Replace("SpecularColor", "");
                    currentTab.SpecularColor = line.Trim();
                }

                //find "Transparency"
                else if (line.Contains("Transparency"))
                {
                    line = line.Replace("Transparency", "");
                    currentTab.Transparency = line.Trim();
                }

                //find "Shininess"
                else if (line.Contains("Shininess"))
                {
                    line = line.Replace("Shininess", "");
                    currentTab.Shininess = line.Trim();
                }

                //find "Timecycle"
                else if (line.Contains("Timecycle"))
                {
                    line = line.Replace("Timecycle", "");
                    currentTab.Timecycle = line.Trim();
                }

                //find "Timerange"
                else if (line.Contains("Timerange"))
                {
                    line = line.Replace("Timerange", "");
                    currentTab.Timerange = line.Trim();
                }

                line = sreader.ReadLine(); // next line

                linecounter++;
            }
            catch
            {
                DisplayMessage("Error Loading File:" + file.FullName + "at line:" + linecounter);
            }
        }
        sreader.Close();
        #endregion

        //after loading, we want to change the current input fields to the reflect loaded data
        CopyTabInfoToInputField(currentTab);

        CreateMeshFromTabdata(currentTab);
    }
    public void OnTabClick(Tab tab)
    {
        currentTab = tab;

        CopyTabInfoToInputField(currentTab);

        CreateMeshFromTabdata(currentTab);
    }
    public void OnTabCloseButton(Tab tab)
    {
        //if theres only one tab
        if (currentTab.transform.parent.childCount == 1)
        {
            Destroy(currentTab.gameObject);
            currentTab = null;

            SetDefaultValuesToInputField();

            currentObject.GetComponent<MeshFilter>().mesh = null;
        }
        //if we trying to close the current selected tab
        else if (currentTab == tab)
        {
            //set current tab to neighbour tab, and close current tab
            int index = currentTab.transform.GetSiblingIndex();

            //if current tab is last tab in list
            if (index == currentTab.transform.parent.childCount - 1)
            {
                //Get the previous tab
                Tab neighbourtab = currentTab.transform.parent.GetChild(index - 1).gameObject.GetComponent<Tab>();
                //Destory current tab
                Destroy(currentTab.gameObject);
                //Change to neighbourtab
                currentTab = neighbourtab;
                //copy tab info to inputfield
                CopyTabInfoToInputField(currentTab);
                //generate mesh from tab
                CreateMeshFromTabdata(currentTab);
            }
            else
            {
                //Get the previous tab
                Tab neightbourtab = currentTab.transform.parent.GetChild(index + 1).gameObject.GetComponent<Tab>();
                //Destory current tab
                Destroy(currentTab.gameObject);
                //Change to neighbourtab
                currentTab = neightbourtab;
                //copy tab info to inputfield
                CopyTabInfoToInputField(currentTab);
                //generate mesh from tab
                CreateMeshFromTabdata(currentTab);
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

    #region ModeDropdown Selection Event
    public void OnModeChange(int mode)
    {
        //0 = Parametric , 1= implicit
        if (mode == 0)
        {
            //Enable Parametric input fields
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

    public void OnCloseClick()
    {
        //If using Unity Editor, just stop Play mode
        //If using anything else, kill the app
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    #region Run/Stop/Save/Load/ Button Clicks
    public void OnRunClick()
    {
        if (currentTab == null)
        {
            currentTab = CreateNewTab();
        }

        CopyAllInputFieldInfoToTab(currentTab); //updates currentTab with current InputFields

        CreateMeshFromTabdata(currentTab);  
    }
    public void OnStopClick()
    {
        RemoveExistingMesh(currentObject.GetComponent<FunctionGeneratedMesh>());
    }
    public void OnSaveClick()
    {
        //Saves the data that is currently on the input field
        //*Not the current Tab

#if UNITY_STANDALONE
        //Open file browser and ask user to select the save path
        string path = StandaloneFileBrowser.SaveFilePanel("Save File", WorkingProjectPath.text, ObjectNameInputField.text, SaveFileExtention.TrimStart('.'));
        if (path == "") return;
        FileInfo file = new FileInfo(path);

#elif UNITY_ANDROID
        FileInfo file = new FileInfo(Application.persistentDataPath + "/" + ObjectNameInputField.text + SaveFileExtention);
#endif

#if !UNITY_WEBGL

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
        //0 - Parametric , 1 - implicit
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
                "Shininess " + ShininessInputField.text + "\n" +

                "Timecycle " + TimecycleInputField.text + "\n" +
                "Timerange " + TimerangeInputField.text
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
                "Shininess " + ShininessInputField.text + "\n" +

                "Timecycle " + TimecycleInputField.text + "\n" +
                "Timerange " + TimerangeInputField.text
            );
        }

        swriter.Close();
        #endregion

        if (WorkingProjectPath.text == "")
        {
            WorkingProjectPath.text = file.FullName.Substring(0, file.FullName.Length - file.Name.Length);
        }

        //update project directory after saving is done
        PopulateProjectbrowser(WorkingProjectPath.text);
#endif
    }
    public void OnLoadClick()
    {
        string[] paths;

#if UNITY_STANDALONE
        paths = StandaloneFileBrowser.OpenFilePanel("Select File", WorkingProjectPath.text, SaveFileExtention.TrimStart('.'), false);
        if (paths.Length == 0 || paths[0] == "") return;

#if UNITY_STANDALONE_OSX
        paths[0] = paths[0].Replace("file://","").Replace("%20"," ");
#endif

#elif UNITY_ANDROID
        paths = new string[] { Application.persistentDataPath + "/" + ObjectNameInputField.text + SaveFileExtention };
#endif

#if !UNITY_WEBGL

        FileInfo file = new FileInfo(paths[0]);

        AddTab(file);
#endif
    }
    public void OnUndoClick()
    {
        DoUndo();
    }
    public void OnRedoClick()
    {
        DoRedo();
    }
#endregion

#region undo/redo support
    void AddCodeInputfieldsToSupportUndo()
    {
        CodeInputField.onValueChanged.AddListener(delegate{InputFieldOnValueChange(CodeInputField);});

        undoinputfieldstack.Push(DefaultCode);
    }
    void InputFieldOnValueChange(TMP_InputField inputfield)
    {
        if(record)
        {
            recordtimer = 0;
        }
        else
        {
            record = true;
        }
    }
    void Update()
    {
        recordtimer += Time.deltaTime;
        if (recordtimer >= undorecorddelay)
        {
            undoinputfieldstack.Push(CodeInputField.text);
            recordtimer = float.NegativeInfinity;
        }

        ListenUndoRedoCommand();
    }
    void ListenUndoRedoCommand()
    {
        //Detecting Ctrl
        if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            //Detecting Z or Y
            if(Input.GetKeyDown(KeyCode.Z))
            {
                DoUndo();
            }
            else if (Input.GetKeyDown(KeyCode.Y))
            {
                DoRedo();
            }
        }
        //Detecting Command key
        else if (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand))
        {
            //Detecting Shift
            if (Input.GetKey(KeyCode.LeftShift)|| Input.GetKey(KeyCode.RightShift))
            {
                //Detecting Z
                if (Input.GetKeyDown(KeyCode.Z))
                    DoUndo();
            }
            //Detecting Cmd+Z
            else
            {
                //Detecting Z
                if (Input.GetKeyDown(KeyCode.Z))
                    DoRedo();
            }
        }
    }
    void DoUndo()
    {
        if (undoinputfieldstack.Count >= 2)
        {
            undoinputfieldstack.Pop();
            redoinputfieldstack.Push(CodeInputField.text);
            CodeInputField.text = undoinputfieldstack.Pop();
            CodeInputField.Select();
            recordtimer = undorecorddelay;
        }
    }
    void DoRedo()
    {
        if (redoinputfieldstack.Count >= 1)
        {
            record = false;
            CodeInputField.text = redoinputfieldstack.Pop();
            CodeInputField.Select();
            undoinputfieldstack.Push(CodeInputField.text);
        }
    }
    void ClearUndoRedoStack()
    {
        undoinputfieldstack.Clear();
        redoinputfieldstack.Clear();
    }
#endregion

    void SaveGeneratedMeshToAssets(Mesh mesh,string path)
    {
        //AssetDatabase.CreateAsset(mesh, path);
        //AssetDatabase.SaveAssets();
    }
}