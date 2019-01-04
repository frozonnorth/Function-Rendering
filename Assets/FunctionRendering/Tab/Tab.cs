using UnityEngine;
using UnityEngine.UI;

//The Tab class just keep track of different object information
public class Tab : MonoBehaviour
{
    public int ObjectMode;
    public string ObjectName;

    //explicit
    public string ParameterDomain;
    public string Resolution;

    //implicit
    public string MarchingBoundingBoxSize;
    public string MarchingBoundingBoxCenter;
    public string BoundingBoxResolution;

    public string Code;

    public string DiffuseColor;
    public string SpecularColor;
    public string Transparency;
    public string Shininess;

    public bool IsWireframe;

    public Tab(int ObjectMode, string ObjectName,
        string ParameterDomain,string Resolution,
        string MarchingBoundingBoxSize, string MarchingBoundingBoxCenter, string BoundingBoxResolution,
        string Code,string DiffuseColor,string SpecularColor,string Transparency,string Shininess,
        bool IsWireframe)
    {
        this.ObjectMode = ObjectMode;
        this.ObjectName = ObjectName;

        this.MarchingBoundingBoxSize = MarchingBoundingBoxSize;
        this.MarchingBoundingBoxCenter = MarchingBoundingBoxCenter;
        this.BoundingBoxResolution = BoundingBoxResolution;

        this.ParameterDomain = ParameterDomain;
        this.Resolution = Resolution;

        this.Code = Code;

        this.DiffuseColor = DiffuseColor;
        this.SpecularColor = SpecularColor;
        this.Transparency = Transparency;
        this.Shininess = Shininess;

        this.IsWireframe = IsWireframe;
    }

    #region Ui stuff
    public Button button;
    public Button closebutton;

    public Text Tabdisplaytext;
    public void SetText(string name)
    {
        Tabdisplaytext.text = name;
    }
    public string GetText()
    {
        return Tabdisplaytext.text;
    }

    //function to link the the button clicks to the another function
    public void AddButtonOnClickEvent()
    {
        button.onClick.AddListener(delegate { Main.Instance.OnTabClick(this);});
        closebutton.onClick.AddListener(delegate { Main.Instance.OnTabCloseButton(this);});
    }
    #endregion
}
