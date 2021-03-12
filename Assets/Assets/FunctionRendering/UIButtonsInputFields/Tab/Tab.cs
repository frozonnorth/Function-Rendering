using UnityEngine;
using UnityEngine.UI;

//The Tab class just keep track of all the generated mesh information
public class Tab : MonoBehaviour
{
    public int ObjectMode;
    public string ObjectName;

    //parametric
    public string ParameterDomain;
    public string Resolution;

    //implicit
    public string MarchingBoundingBoxSize;
    public string MarchingBoundingBoxCenter;
    public string BoundingBoxResolution;

    public string Code;

    //extra
    public string DiffuseColor;
    public string SpecularColor;
    public string Transparency;
    public string Shininess;

    public string Timecycle;
    public string Timerange;

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
