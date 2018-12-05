using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;
using SFB;
using System.Collections;

public class Main : MonoBehaviour
{
    #region Camera
    public EditorCamera viewcamera;
    #endregion

    GameObject currentObject;
    Tab currentTab;

    public Material Objectmaterial;

    #region Main inputs
    public InputField ObjectnameInput;
    public InputField ParameterdomainInput;
    public InputField ParameterResolutionInput;
    public InputField CodeInput;
    #endregion

    #region Extra Inputs
    public Toggle wireframetoggle;
    public InputField DiffuseColorInput;
    public InputField SpecularColorInput;
    public InputField ShininessInput;
    #endregion

    #region direcctory , load and save
    List<DirectoryItem> directorylist = new List<DirectoryItem>();
    List<FileItem> filelist = new List<FileItem>();
    #endregion

    public InputField projectPathtxt;
    public GameObject tabContainerGO;
    public GameObject directoryContainerGO;
    public GameObject directoryItemPrefab;
    public GameObject fileItemPrefab;
    public GameObject tabPrefab;


    #region settings
    public const string SaveFileExtention = ".Func";
    #endregion


    #region Singleton - for other scripts for easy acess
    public static Main Instance;
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    #endregion 


    void Start()
    {
        currentObject = new GameObject("ParametricMesh", typeof(MeshFilter));
        MeshRenderer rend = currentObject.AddComponent<MeshRenderer>();
        rend.material = new Material(Objectmaterial);
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

        projectPathtxt.text = paths[0];

        UpdateDirectory(projectPathtxt.text);
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
            DirectoryItem di = Instantiate(directoryItemPrefab).GetComponent<DirectoryItem>();
            di.directoryinfo = directoryinfoarray[i];
            di.SetName(directoryinfoarray[i].Name);
            di.SetHierachyLevel(0);
            di.gameObject.name = directoryinfoarray[i].Name;
            di.AddButtononClickEvent();
            di.transform.SetParent(directoryContainerGO.transform, false);

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
            fi.transform.SetParent(directoryContainerGO.transform, false);

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
                DirectoryItem di = Instantiate(directoryItemPrefab).GetComponent<DirectoryItem>();
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
        Tab newtab = Instantiate(tabPrefab).GetComponent<Tab>();
        newtab.transform.SetParent(tabContainerGO.transform, false);
        newtab.SetTextName(name);
        newtab.gameObject.name = name;
        newtab.AddButtonOnClickEvent();
        return newtab;
    }
    void AddTab(FileInfo file)
    {
        string name = file.Name.Remove(file.Name.Length - SaveFileExtention.Length);
        currentTab = CreateNewTab(name);

        #region Load saved file data into tab
        StreamReader sreader = file.OpenText();
        string line = sreader.ReadLine();
        while (line != null)
        {
            if (line.Contains("#"))
            {

            }
            //find "parameters"
            else if (line.Contains("parameters"))
            {
                line = line.Replace("parameters", "");
                currentTab.parameterdomain = line.Trim();
            }

            //find "resolution"
            else if (line.Contains("resolution"))
            {
                line = line.Replace("resolution", "");
                currentTab.resolution = line.Trim();
            }

            //find "definition"
            else if (line.Contains("definition"))
            {
                line = line.Replace("definition", "");
                int paraentesiscount = line.Length - line.Replace("\"", "").Length;
                currentTab.code = line.Trim();
                while (line != "")
                {
                    if (paraentesiscount >= 2)
                    {
                        break;
                    }
                    line = sreader.ReadLine();
                    currentTab.code += '\n' + line.Trim();
                    paraentesiscount += line.Length - line.Replace("\"", "").Length;
                }
                currentTab.code = currentTab.code.Replace("\"", "");
            }
            line = sreader.ReadLine(); // next line
        }
        sreader.Close();
        #endregion

        ParameterResolutionInput.text = currentTab.resolution;
        ParameterdomainInput.text = currentTab.parameterdomain;
        CodeInput.text = currentTab.code;
        ObjectnameInput.text = name;

        DrawParametricCode(ParameterResolutionInput.text, ParameterdomainInput.text, CodeInput.text, wireframetoggle.isOn);
    }
    #endregion



    #region Button Clicks functions
    public void OnRunClick()
    {
        if (currentTab == null)
        {
            currentTab = CreateNewTab();
        }

        currentTab.resolution = ParameterResolutionInput.text;
        currentTab.parameterdomain = ParameterdomainInput.text;
        currentTab.code = CodeInput.text;

        DrawParametricCode(ParameterResolutionInput.text, ParameterdomainInput.text, CodeInput.text, wireframetoggle.isOn);

        viewcamera.SetTarget(currentObject);
    }
    public void OnTabClick(Tab tab)
    {
        currentTab = tab;

        ParameterdomainInput.text = currentTab.parameterdomain;
        ParameterResolutionInput.text = currentTab.resolution;
        CodeInput.text = currentTab.code;
        ObjectnameInput.text = currentTab.GetTextName();

        DrawParametricCode(ParameterResolutionInput.text, ParameterdomainInput.text, CodeInput.text, wireframetoggle.isOn);
    }
    public void OnTabCloseButton(Tab tab)
    {
        //if theres only one tab
        if (currentTab.transform.parent.childCount == 1)
        {
            Destroy(currentTab.gameObject);
            currentTab = null;

            ParameterResolutionInput.text = "";
            ParameterdomainInput.text = "";
            CodeInput.text = "";

            DrawParametricCode(ParameterResolutionInput.text, ParameterdomainInput.text, CodeInput.text, wireframetoggle.isOn);
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
                Tab neightbourtab = currentTab.transform.parent.GetChild(index - 1).gameObject.GetComponent<Tab>();
                //Destory current tab
                Destroy(currentTab.gameObject);
           
                currentTab = neightbourtab;
                ParameterResolutionInput.text = currentTab.resolution;
                ParameterdomainInput.text = currentTab.parameterdomain;
                CodeInput.text = currentTab.code;

                DrawParametricCode(ParameterResolutionInput.text, ParameterdomainInput.text, CodeInput.text, wireframetoggle.isOn);
            }
            else
            {
                //Get the previous tab
                Tab neightbourtab = currentTab.transform.parent.GetChild(index + 1).gameObject.GetComponent<Tab>();
                //Destory current tab
                Destroy(currentTab.gameObject);

                currentTab = neightbourtab;
                ParameterResolutionInput.text = currentTab.resolution;
                ParameterdomainInput.text = currentTab.parameterdomain;
                CodeInput.text = currentTab.code;

                DrawParametricCode(ParameterResolutionInput.text, ParameterdomainInput.text, CodeInput.text, wireframetoggle.isOn);
            }
        }
        // if we trying to close a tab that is not selected
        else
        {
            Destroy(tab.gameObject);
        }
    }

    public void OnSaveClick()
    {
        //open file browser and ask user to select the save path
        #if UNITY_STANDALONE_WIN
        string path = StandaloneFileBrowser.SaveFilePanel("Save File", projectPathtxt.text, ObjectnameInput.text, SaveFileExtention.TrimStart('.'));
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

        swriter.WriteLine
        (
            "definition \"" + CodeInput.text.Replace("\n", "\n\t\t\t") + "\"\n" +
            "parameters " + ParameterdomainInput.text + "\n" +
            "resolution " + ParameterResolutionInput.text
        );
        swriter.Close();
        #endregion

        //update project directory after saving is done
        UpdateDirectory(projectPathtxt.text);
    }
    public void OnLoadClick()
    {
        string[] paths;
        #if UNITY_STANDALONE_WIN
        paths = StandaloneFileBrowser.OpenFilePanel("Select File", projectPathtxt.text, SaveFileExtention.TrimStart('.'), false);
        if (paths.Length == 0) return;
        #elif UNITY_ANDROID
        paths = new string[] { Application.persistentDataPath + "/" + ObjectnameInput.text + SaveFileExtention };
        #endif

        FileInfo file = new FileInfo(paths[0]);
       
        AddTab(file);
    }

    public void OnWireframeChange()
    {
        DrawParametricCode(ParameterResolutionInput.text, ParameterdomainInput.text, CodeInput.text, wireframetoggle.isOn);
    }


    public void OnDiffuseColorChange()
    {
        string text = DiffuseColorInput.text;
        text = text.TrimStart('(').TrimEnd(')');
        text = text.TrimStart('[').TrimEnd(']');
        DiffuseColorInput.text = text;
        string[] colorarray = text.Split(',');

        byte R;
        byte G;
        byte B;
        byte A;
        try
        {
            if (colorarray.Length == 1)
            {
                R = byte.Parse(colorarray[0]);
                G = R;
                B = R;
                A = 255;
            }
            else if (colorarray.Length == 3)
            {
                R = byte.Parse(colorarray[0]);
                G = byte.Parse(colorarray[1]);
                B = byte.Parse(colorarray[2]);
                A = 255;
            }
            else if (colorarray.Length == 4)
            {
                R = byte.Parse(colorarray[0]);
                G = byte.Parse(colorarray[1]);
                B = byte.Parse(colorarray[2]);
                A = byte.Parse(colorarray[3]);
            }
            else
            {
                //color format error - return and do nothing
                DisplayMessage("Color Format needs to be R,G,B");
                return;
            }
        }
        catch
        {
            //color value format error - return and do nothing
            DisplayMessage("Accept only 0-255 RGB Color Values");
            return;
        }
        
        
        MeshRenderer rend = currentObject.GetComponent<MeshRenderer>();
        rend.material.SetColor("_Color", new Color32(R, G, B, A));
    }

    public void OnSpecularColorChange()
    {
        string text = SpecularColorInput.text;
        text = text.TrimStart('(').TrimEnd(')');
        text = text.TrimStart('[').TrimEnd(']');
        SpecularColorInput.text = text;

        string[] colorarray = text.Split(',');
        byte R;
        byte G;
        byte B;
        byte A;
        try
        {
            if (colorarray.Length == 1)
            {
                R = byte.Parse(colorarray[0]);
                G = R;
                B = R;
                A = 255;
            }
            else if (colorarray.Length == 3)
            {
                R = byte.Parse(colorarray[0]);
                G = byte.Parse(colorarray[1]);
                B = byte.Parse(colorarray[2]);
                A = 255;
            }
            else if (colorarray.Length == 4)
            {
                R = byte.Parse(colorarray[0]);
                G = byte.Parse(colorarray[1]);
                B = byte.Parse(colorarray[2]);
                A = byte.Parse(colorarray[3]);
            }
            else
            {
                //color format error - return and do nothing
                DisplayMessage("Color Format needs to be R,G,B");
                return;
            }
        }
        catch
        {
            //color value format error - return and do nothing
            DisplayMessage("Accept only 0-255 RGB Color Values");
            return;
        }


        MeshRenderer rend = currentObject.GetComponent<MeshRenderer>();
        rend.material.SetColor("_SpecColor",new Color32(R, G, B, A));
    }

    public void OnShininessChange()
    {
        string text = ShininessInput.text;
        text = text.TrimStart('(').TrimEnd(')');
        text = text.TrimStart('[').TrimEnd(']');
        ShininessInput.text = text;

        float value;
        try
        {
            value = Mathf.Clamp01(float.Parse(text));
            ShininessInput.text = value.ToString();
        }
        catch
        {
            //color value format error - return and do nothing
            DisplayMessage("Accept value 0 - 1");
            return;
        }

        MeshRenderer rend = currentObject.GetComponent<MeshRenderer>();
        rend.material.SetFloat("_Shininess", value);
    }

    void OnObjectNameChange(string newname)
    {
        currentTab.SetTextName(newname);
        currentTab.gameObject.name = newname;
    }

    #endregion

    void DisplayMessage(string messgae)
    {

    }


    void DrawParametricCode(string resolution, string parameterdomain, string code,bool iswireframe)
    {
        ParametricMesh parametricMesh;
        if (!(parametricMesh = currentObject.GetComponent<ParametricMesh>()))
        {
            parametricMesh = currentObject.AddComponent<ParametricMesh>();
        }

        #region obtain and set u,v,w resolution [#] or [# #] or [# # #]
        resolution = resolution.Replace("[", "");
        resolution = resolution.Replace("]", "");
        string[] res = resolution.Split(' ');
        if (res.Length >= 1)
        {
            int temp;
            int.TryParse(res[0], out temp);
            parametricMesh.sampleresolution_U = temp;
            parametricMesh.sampleresolution_V = 1;
            parametricMesh.sampleresolution_W = 1;
            if (res.Length >= 2)
            {
                int.TryParse(res[1], out temp);
                parametricMesh.sampleresolution_V = temp;
                if (res.Length >= 3)
                {
                    int.TryParse(res[2], out temp);
                    parametricMesh.sampleresolution_W = temp;
                }
            }
        }
        #endregion

        //check if total vertice number , resolution is someething the device can suppport
        long maximumindex = 0;
        if (SystemInfo.supports32bitsIndexBuffer)
            maximumindex = 4000000000;
        else
            maximumindex = 65535;
       
        long estimateVerticesCount = parametricMesh.sampleresolution_U * parametricMesh.sampleresolution_V * parametricMesh.sampleresolution_W;
        if(estimateVerticesCount > maximumindex)
        {
            PopupMessage.Instance.ShowMessage("Sampling Resolution Too Large For Device To Support.");
            return;
        }


        #region obtain and set u v w domain range [# #] , [# # # # # #] or [# # # # # #]
        parameterdomain = parameterdomain.Replace("[", "");
        parameterdomain = parameterdomain.Replace("]", "");
        string[] domains = parameterdomain.Split(' ');
        if (domains.Length >= 2)
        {
            float temp2;
            float.TryParse(domains[0], out temp2);
            parametricMesh.uMinDomain = temp2;
            float.TryParse(domains[1], out temp2);
            parametricMesh.uMaxDomain = temp2;

            if (domains.Length >= 4)
            {
                float.TryParse(domains[2], out temp2);
                parametricMesh.vMinDomain = temp2;

                float.TryParse(domains[3], out temp2);
                parametricMesh.vMaxDomain = temp2;

                if (domains.Length >= 6)
                {
                    float.TryParse(domains[4], out temp2);
                    parametricMesh.wMinDomain = temp2;

                    float.TryParse(domains[5], out temp2);
                    parametricMesh.wMaxDomain = temp2;
                }
            }
        }
        #endregion

        parametricMesh.iswireframe = iswireframe;

        parametricMesh.SetFomula(code);
        if (parametricMesh.MeshGenerated == true) //if mesh is already generated
        {
            //destory the existing mesh
            foreach (MonoBehaviour a in currentObject.GetComponents<MonoBehaviour>())
            {
                if(a.GetType().ToString() == "CompliedMesh")
                {
                    DestroyImmediate(a);
                }
            }
        }
        parametricMesh.StartDrawing();
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
