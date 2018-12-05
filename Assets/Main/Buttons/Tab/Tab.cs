using UnityEngine;
using UnityEngine.UI;

public class Tab : MonoBehaviour
{
    public string resolution = "[10 10 10]";
    public string parameterdomain = "[0 1 0 1 0 1]";
    public string code = "";

    public Text tabtext;
    public void SetTextName(string name)
    {
        tabtext.text = name;
    }
    public string GetTextName()
    {
        return tabtext.text;
    }

    public Button button;
    public Button closebutton;
    public void AddButtonOnClickEvent()
    {
        button.onClick.AddListener(delegate { Main.Instance.OnTabClick(this); });
        closebutton.onClick.AddListener(delegate { Main.Instance.OnTabCloseButton(this); });
    }
}
