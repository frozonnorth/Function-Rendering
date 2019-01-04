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

    #region Settings
    const string SaveFileExtention = ".Func";
    #endregion

    #region Camera
    public EditorCamera viewcamera;
    #endregion

    public Material MeshObjectMaterial;
    GameObject currentObject;//unity gameobject that holds the generated mesh
    MeshRenderer rend;
    Tab currentObjectTab;

    #region Singleton - for other scripts for easy acess
    public static Main Instance;
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }
    #endregion 


    void Start()
    {
        currentObject = new GameObject("ParametricMesh", typeof(MeshFilter));
        rend = currentObject.AddComponent<MeshRenderer>();
        rend.material = new Material(MeshObjectMaterial);
        viewcamera.SetTarget(currentObject);

        #if UNITY_ANDROID
        OnProjectDirectoryChange();//For android, we want to populate the file directory at start
        #endif
    }


    #region file directory
    public void OnProjectDirectoryChange()
    {
        string[] paths;
        #if UNITY_STANDALONE_WIN
        paths = StandaloneFileBrowser.OpenFolderPanel("Select Project Folder", PlayerPrefs.GetString("LastProjectPath", Application.dataPath), false);
        if (paths.Length == 0) return;
        PlayerPrefs.SetString("LastProjectPath", paths[0]);
        #elif UNITY_ANDROID
        paths = new string[] { Application.persistentDataPath};
        #endif

        WorkingProjectPath.text = paths[0];

        UpdateDirectory(WorkingProjectPath.text);
    }
    void UpdateDirectory(string path)
    {
        //Remove any existing directory object
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

        //create and display all the directories and files in the new project path
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
    public void OnDirectoryClick(DirectoryItem directoryitem)
    {
        if (directoryitem.expanded) // remove child directory and files if this directory is already expanded
        {
            directoryitem.RemoveChildDirectory();
            directoryitem.RemoveChildFile();
            directoryitem.expanded = false;
            directoryitem.UpdateIcon();
            //directoryitem.UpdateUI();
            //Canvas.ForceUpdateCanvases();
        }
        else // add child directory and files it not expanded
        {
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
            //directoryitem.UpdateUI();
            //Canvas.ForceUpdateCanvases();
        }
    }
    public void OnFileClick(FileItem file)
    {
        //open file as new tab
        AddTab(file.fileinfo);
    }

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
    #endregion

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

    #region Extra Input Fields On Change Event (only for user input validation)
    public void OnDiffuseColorChange()
    {
        //Format checking and format forcing
        //Accepted format: (R,G,B) [R,G,B] R,G,B (#) [#] #
        //Forced format: R,G,B where R,G,B is 0-1 value
        string text = DiffuseColorInputField.text;
        text = text.TrimStart('[', '(').TrimEnd(']',')');  
        string[] colorarray = text.Split(',');
        float R;
        float G;
        float B;
        try
        {
            if (colorarray.Length == 1)
            {
                R = Mathf.Clamp01(float.Parse(colorarray[0]));
                G = R;
                B = R;
            }
            else if (colorarray.Length == 3)
            {
                R = Mathf.Clamp01(float.Parse(colorarray[0].Trim(' ')));
                G = Mathf.Clamp01(float.Parse(colorarray[1].Trim(' ')));
                B = Mathf.Clamp01(float.Parse(colorarray[2].Trim(' ')));
            }
            else
            {
                throw new Exception();
            }
            DiffuseColorInputField.text = R +","+ G + "," + B;
        }
        catch
        {
            DisplayMessage("Error in Diffuse Color input format:\"R,G,B\"");
            return;
        }
    }
    public void OnSpecularColorChange()
    {
        //Format checking and format forcing
        //Accepted format: (R,G,B) [R,G,B] R,G,B (#) [#] #
        //Forced format: R,G,B where R,G,B is 0-1 value
        string text = SpecularColorInputField.text;
        text = text.TrimStart('[', '(').TrimEnd(']', ')');
        string[] colorarray = text.Split(',');
        float R;
        float G;
        float B;
        try
        {
            if (colorarray.Length == 1)
            {
                R = Mathf.Clamp01(float.Parse(colorarray[0]));
                G = R;
                B = R;
            }
            else if (colorarray.Length == 3)
            {
                R = Mathf.Clamp01(float.Parse(colorarray[0].Trim(' ')));
                G = Mathf.Clamp01(float.Parse(colorarray[1].Trim(' ')));
                B = Mathf.Clamp01(float.Parse(colorarray[2].Trim(' ')));
            }
            else
            {
                throw new Exception();
            }
            SpecularColorInputField.text = R + "," + G + "," + B;
        }
        catch
        {
            DisplayMessage("Error in Specular Color input format:\"R,G,B\"");
            return;
        }
    }
    public void OnTransparencyChange()
    {
        //Format checking and format forcing
        //Accepted format: (#) [#] #
        //Forced format: # where # is 0-1 value
        string text = TransparencyInputField.text;
        text = text.TrimStart('[', '(').TrimEnd(']', ')').Trim(' ');
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
        //Format checking and format forcing
        //Accepted format: (#) [#] #
        //Forced format: # where # is 0-1 value
        string text = ShininessInputField.text;
        text = text.TrimStart('[', '(').TrimEnd(']', ')').Trim(' ');
        try
        {
            float shininess = Mathf.Clamp01(float.Parse(text));
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

        viewcamera.SetTarget(currentObject);
    }
    public void OnSaveClick()
    {
        //open file browser and ask user to select the save path
        #if UNITY_STANDALONE_WIN
        string path = StandaloneFileBrowser.SaveFilePanel("Save File", WorkingProjectPath.text, ObjectNameInputField.text, SaveFileExtention.TrimStart('.'));
        if (path == "") return;
        FileInfo file = new FileInfo(path);
        #elif UNITY_ANDROID
        FileInfo file = new FileInfo(Application.persistentDataPath + "/" + ObjectnameInput.text + SaveFileExtention);
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
                "resolution " + ParameterResolutionInputField.text
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
                "bboxResolution " + BoundingBoxResolutionInputField.text
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
        #if UNITY_STANDALONE_WIN
        paths = StandaloneFileBrowser.OpenFilePanel("Select File", WorkingProjectPath.text, SaveFileExtention.TrimStart('.'), false);
        if (paths.Length == 0) return;
        #elif UNITY_ANDROID
        paths = new string[] { Application.persistentDataPath + "/" + ObjectnameInput.text + SaveFileExtention };
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

        ParametricMesh parametricMesh;
        if (!(parametricMesh = currentObject.GetComponent<ParametricMesh>()))
        {
            parametricMesh = currentObject.AddComponent<ParametricMesh>();
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
            #region user input validation on explicit input parameters
            try
            {
                #region obtain and set u v w domain 
                //Accepted Formated as: [# #] [# # # #] [# # # # # #]
                //                  or  [#,#] [#,#,#,#] [#,#,#,#,#,#]
                //                  or  (# #) (# # # #) (# # # # # #)
                //                  or  (#,#) (#,#,#,#) (#,#,#,#,#,#)
                //                  or   # #   # # # #   # # # # # # 
                //                  or   #,#   #,#,#,#   #,#,#,#,#,# 

                string parameterdomain = tab.ParameterDomain;
                parameterdomain = parameterdomain.TrimStart('[', '(', ' ').TrimEnd(']', ')', ' ');

                string[] domains = parameterdomain.Split(' ');//split string by space
                //no space found
                if (domains.Length == 1)
                {
                    #region processing domain split by comma
                    domains = parameterdomain.Split(',');//split string by comma

                    if (domains.Length == 1)
                    {
                        throw new Exception();
                    }
                    //comma found
                    else if (domains.Length >= 2)
                    {
                        #region processing domain split by space
                        //convert the the string to float value as resolution U min,max domain range
                        parametricMesh.uMinDomain = float.Parse(domains[0].Trim(' '));
                        parametricMesh.uMaxDomain = float.Parse(domains[1].Trim(' '));

                        if (domains.Length >= 4)
                        {
                            //convert the the string to float value as resolution V min,max domain range
                            parametricMesh.vMinDomain = float.Parse(domains[2].Trim(' '));
                            parametricMesh.vMaxDomain = float.Parse(domains[3].Trim(' '));

                            if (domains.Length >= 6)
                            {
                                //convert the the string to float value as resolution W min,max domain range
                                parametricMesh.wMinDomain = float.Parse(domains[4].Trim(' '));
                                parametricMesh.wMaxDomain = float.Parse(domains[5].Trim(' '));
                            }
                        }
                        #endregion
                    }

                    #endregion
                }
                else if (domains.Length >= 2)
                {
                    #region processing domain split by space
                    //convert the the string to float value as resolution U min,max domain range
                    parametricMesh.uMinDomain = float.Parse(domains[0]);
                    parametricMesh.uMaxDomain = float.Parse(domains[1]);

                    if (domains.Length >= 4)
                    {
                        //convert the the string to float value as resolution V min,max domain range
                        parametricMesh.vMinDomain = float.Parse(domains[2]);
                        parametricMesh.vMaxDomain = float.Parse(domains[3]);

                        if (domains.Length >= 6)
                        {
                            //convert the the string to float value as resolution W min,max domain range
                            parametricMesh.wMinDomain = float.Parse(domains[4]);
                            parametricMesh.wMaxDomain = float.Parse(domains[5]);
                        }
                    }
                    #endregion
                }
                #endregion
            }
            catch
            {
                errorMessages += "Error in Domain input format.\n";
            }
            try
            {
                #region obtain and process resolution format 
                //Accepted Formated as: [#] [# #] [# # #] 
                //                  or  [#] [#,#] [#,#,#] 
                //                  or  (#) (# #) (# # #)
                //                  or  (#) (#,#) (#,#,#)
                //                  or   #   # #   # # # 
                //                  or   #   #,#   #,#,# 

                string resolution = tab.Resolution;
                resolution = resolution.TrimStart('[', '(', ' ').TrimEnd(']', ')', ' ');

                string[] res = resolution.Split(' ');//split string by space
                //no space found
                if (res.Length == 1)
                {
                    #region processing resolution split by comma
                    res = resolution.Split(',');//split string by comma

                    if (res.Length >= 1)
                    {
                        //convert the the string to int value as resolution U
                        parametricMesh.sampleresolution_U = int.Parse(res[0].Trim(' '));
                    }
                    else if (res.Length >= 2)
                    {
                        //convert the second part of the string to int value as resolution V
                        parametricMesh.sampleresolution_V = int.Parse(res[1].Trim(' '));
                        if (res.Length >= 3)
                        {
                            //convert the second part of the string to int value as resolution W
                            parametricMesh.sampleresolution_W = int.Parse(res[2].Trim(' '));
                        }
                    }
                    #endregion
                }
                else if (res.Length >= 2)
                {
                    #region processing resolution split by space
                    //convert the first part of the string to int value as resolution U
                    parametricMesh.sampleresolution_U = int.Parse(res[0].Trim(' '));

                    //convert the second part of the string to int value as resolution V
                    parametricMesh.sampleresolution_V = int.Parse(res[1].Trim(' '));
                    if (res.Length >= 3)
                    {
                        //convert the second part of the string to int value as resolution W
                        parametricMesh.sampleresolution_W = int.Parse(res[2].Trim(' '));
                    }
                    #endregion
                }
                #endregion
            }
            catch
            {
                errorMessages += "Error in Resolution input format.\n";
            }

            #region check if the resolution input by user is something the device(pc,phone,mac) can support
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
        else
        {
            #region user input validation on implicit input parameters
            try
            {
                #region obtain and process bounding box size
                //Accepted Formated as: [# # #] , [#,#,#] 
                //                  or  (# # #) , (#,#,#)
                //                  or   # # #  ,  #,#,# 
                string size = BoundingBoxSizeInputField.text;
                size = size.TrimStart('[', '(',' ').TrimEnd(']', ')',' ');
                string[] sizearray = size.Split(' ');//split string by space
                //no space found
                if (sizearray.Length == 1)
                {
                    #region processing size split by comma
                    sizearray = size.Split(',');//split string by comma

                    if (sizearray.Length == 3)
                    {
                        parametricMesh.MarchingBoundingBoxSize = new Vector3(float.Parse(sizearray[0].Trim(' ')), float.Parse(sizearray[1].Trim(' ')), float.Parse(sizearray[2].Trim(' ')));
                    }
                    else
                    {
                        throw new Exception();
                    }
                    #endregion
                }
                //space found
                else if (sizearray.Length == 3)
                {
                    #region processing resolution split by space
                    parametricMesh.MarchingBoundingBoxSize = new Vector3(float.Parse(sizearray[0].Trim(' ')), float.Parse(sizearray[1].Trim(' ')), float.Parse(sizearray[2].Trim(' ')));
                    #endregion
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
                #region obtain and process bounding box center position
                //Accepted Formated as: [# # #] , [#,#,#] 
                //                  or  (# # #) , (#,#,#)
                //                  or   # # #  ,  #,#,# 
                string pos = BoundingBoxCenterInputField.text;
                pos = pos.TrimStart('[', '(', ' ').TrimEnd(']', ')', ' ');
                string[] posarray = pos.Split(' ');//split string by space
                //no space found
                if (posarray.Length == 1)
                {
                    #region processing size split by comma
                    posarray = pos.Split(',');//split string by comma

                    if (posarray.Length == 3)
                    {
                        parametricMesh.MarchingBoundingBoxCenter = new Vector3(float.Parse(posarray[0].Trim(' ')), float.Parse(posarray[1].Trim(' ')), float.Parse(posarray[2].Trim(' ')));
                    }
                    else
                    {
                        throw new Exception();
                    }
                    #endregion
                }
                //space found
                else if (posarray.Length == 3)
                {
                    #region processing resolution split by space
                    parametricMesh.MarchingBoundingBoxCenter = new Vector3(float.Parse(posarray[0].Trim(' ')), float.Parse(posarray[1].Trim(' ')), float.Parse(posarray[2].Trim(' ')));
                    #endregion
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
                #region obtain and process bounding box resolution
                //Accepted Formated as: [# # #] , [#,#,#] 
                //                  or  (# # #) , (#,#,#)
                //                  or   # # #  ,  #,#,# 
                string res = BoundingBoxResolutionInputField.text;
                res = res.TrimStart('[', '(', ' ').TrimEnd(']', ')', ' ');
                string[] resarray = res.Split(' ');//split string by space
                //no space found
                if (resarray.Length == 1)
                {
                    #region processing size split by comma
                    resarray = res.Split(',');//split string by comma

                    if (resarray.Length == 3)
                    {
                        parametricMesh.BoundingBoxResolution = new Vector3(float.Parse(resarray[0].Trim(' ')), float.Parse(resarray[1].Trim(' ')), float.Parse(resarray[2].Trim(' ')));
                    }
                    else
                    {
                        throw new Exception();
                    }
                    #endregion
                }
                //space found
                else if (resarray.Length == 3)
                {
                    #region processing resolution split by space
                    parametricMesh.BoundingBoxResolution = new Vector3(float.Parse(resarray[0].Trim(' ')), float.Parse(resarray[1].Trim(' ')), float.Parse(resarray[2].Trim(' ')));
                    #endregion
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

        #region diffuse check
        //Format checking and format forcing
        //Accepted format: (R,G,B) [R,G,B] R,G,B (#) [#] #
        //Forced format: R,G,B where R,G,B is 0-1 value
        string diffusetext = DiffuseColorInputField.text;
        diffusetext = diffusetext.TrimStart('[', '(', ' ').TrimEnd(']', ')', ' ');
        string[] diffusecolorarray = diffusetext.Split(',');
        float diffuse_R = 0, diffuse_G = 0, diffuse_B = 0;
        try
        {
            if (diffusecolorarray.Length == 1)
            {
                diffuse_R = Mathf.Clamp01(float.Parse(diffusecolorarray[0]));
                diffuse_G = diffuse_R;
                diffuse_B = diffuse_R;
            }
            else if (diffusecolorarray.Length == 3)
            {
                diffuse_R = Mathf.Clamp01(float.Parse(diffusecolorarray[0].Trim(' ')));
                diffuse_G = Mathf.Clamp01(float.Parse(diffusecolorarray[1].Trim(' ')));
                diffuse_B = Mathf.Clamp01(float.Parse(diffusecolorarray[2].Trim(' ')));
            }
            else
            {
                throw new Exception();
            }
            DiffuseColorInputField.text = diffuse_R + "," + diffuse_G + "," + diffuse_B;
        }
        catch
        {
            errorMessages += "Error in Diffuse Color input format:\"R,G,B\".\n";
        }
        #endregion

        #region specular check
        //Format checking and format forcing
        //Accepted format: (R,G,B) [R,G,B] R,G,B (#) [#] #
        //Forced format: R,G,B where R,G,B is 0-1 value
        string speculartext = SpecularColorInputField.text;
        speculartext = speculartext.TrimStart('[', '(', ' ').TrimEnd(']', ')', ' ');
        string[] specularcolorarray = speculartext.Split(',');
        float specular_R = 0,specular_G = 0,specular_B = 0;
        try
        {
            if (specularcolorarray.Length == 1)
            {
                specular_R = Mathf.Clamp01(float.Parse(specularcolorarray[0]));
                specular_G = specular_R;
                specular_B = specular_R;
            }
            else if (specularcolorarray.Length == 3)
            {
                specular_R = Mathf.Clamp01(float.Parse(specularcolorarray[0].Trim(' ')));
                specular_G = Mathf.Clamp01(float.Parse(specularcolorarray[1].Trim(' ')));
                specular_B = Mathf.Clamp01(float.Parse(specularcolorarray[2].Trim(' ')));
            }
            else
            {
                throw new Exception();
            }
            SpecularColorInputField.text = specular_R + "," + specular_G + "," + specular_B;
        }
        catch
        {
            errorMessages += "Error in Specular Color input format:\"R,G,B\".\n";
        }
        #endregion

        #region transparency check
        //Format checking and format forcing
        //Accepted format: (#) [#] #
        //Forced format: # where # is 0-1 value
        string transparency = TransparencyInputField.text;
        transparency = transparency.TrimStart('[', '(', ' ').TrimEnd(']', ')', ' ');
        float transparencyvalue = 1;
        try
        {
            transparencyvalue = Mathf.Clamp01(float.Parse(transparency));
            TransparencyInputField.text = transparencyvalue.ToString();
        }
        catch
        {
            errorMessages += "Error in Transparency Input Field.\n";
        }
        #endregion

        #region shininess check
        //Format checking and format forcing
        //Accepted format: (#) [#] #
        //Forced format: # where # is 0-1 value
        string text = ShininessInputField.text;
        text = text.TrimStart('[', '(', ' ').TrimEnd(']', ')', ' ');
        float shininess = 0.5f;
        try
        {
            shininess = Mathf.Clamp01(float.Parse(text));
            ShininessInputField.text = shininess.ToString();
        }
        catch
        {
            errorMessages += "Error in Shininess Input Field.\n";
        }
        #endregion

        //stop the run if theres an error
        if (errorMessages.Length > 0)
        {
            DisplayMessage(errorMessages.TrimEnd('\n'));
            return;
        }

        //set the code
        parametricMesh.SetFomula(tab.Code);

        //set diffuse color
        rend.material.SetColor("_Color", new Color(diffuse_R, diffuse_G, diffuse_B, transparencyvalue));

        //set specular color
        rend.material.SetColor("_SpecColor", new Color(specular_R, specular_G, specular_B, 1));

        //set shininess
        rend.material.SetFloat("_Glossiness", shininess);

        //set wireframe toggle
        parametricMesh.iswireframe = tab.IsWireframe;

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


        tabtocopyto.ParameterDomain = ParameterDomainInputField.text;
        tabtocopyto.Resolution = ParameterResolutionInputField.text;

        tabtocopyto.MarchingBoundingBoxSize = BoundingBoxSizeInputField.text;
        tabtocopyto.MarchingBoundingBoxCenter = BoundingBoxCenterInputField.text;
        tabtocopyto.BoundingBoxResolution = BoundingBoxResolutionInputField.text;

        tabtocopyto.Code = CodeInputField.text;

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

        CodeInputField.text = tabtocopyfrom.Code;

        ParameterResolutionInputField.text = tabtocopyfrom.Resolution;
        ParameterDomainInputField.text = tabtocopyfrom.ParameterDomain;

        BoundingBoxSizeInputField.text = tabtocopyfrom.MarchingBoundingBoxSize;
        BoundingBoxCenterInputField.text = tabtocopyfrom.MarchingBoundingBoxCenter;
        BoundingBoxSizeInputField.text = tabtocopyfrom.BoundingBoxResolution;

        ObjectNameInputField.text = tabtocopyfrom.ObjectName;
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
