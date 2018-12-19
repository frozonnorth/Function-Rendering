using UnityEngine;
using UnityEngine.UI;

//The Tab class just keep track of different object information
public class Tab : MonoBehaviour
{
    public string ObjectName;

    public string ParameterDomain;
    public string Resolution;

    public string Code;

    public string DiffuseColor;
    public string SpecularColor;
    public string Transparency;
    public string Shininess;

    public bool IsWireframe;

    public Tab(string ObjectName,string ParameterDomain,string Resolution
        ,string Code,string DiffuseColor,string SpecularColor,string Transparency,string Shininess,
        bool IsWireframe)
    {
        this.ObjectName = ObjectName;
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
