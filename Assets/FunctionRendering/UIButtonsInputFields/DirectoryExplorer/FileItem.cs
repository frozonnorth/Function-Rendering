using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class that represent a file item
/// </summary>
public class FileItem : MonoBehaviour
{
    //To be assign in Unity Editor
    public VerticalLayoutGroup verticalLayoutGroup;
    public Button button;
    public Image buttonicon;
    public Sprite fileIcon;

    //To be assign and used by other classes
    public int hierarchylevel;
    public FileInfo fileinfo;

    //Text component that represent the display text/name of the file
    public Text itemtext;
    public void SetName(string name)
    {
        itemtext.text = name;
    }

    public void UpdateIcon()
    {
        buttonicon.sprite = fileIcon;
    }
    //Executed when the fileitem is created
    void Start()
    {
        UpdateIcon();
    }
    
    //Change the Hierachy Level of the file item and increase left padding
    //To shift the item to the right for each hierachylevel
    public void SetHierachyLevel(int level)
    {
        hierarchylevel = level;
        verticalLayoutGroup.padding.left = hierarchylevel * 40;
    }

    //Helper function to add a buttton onclick event
    public void AddButtononClickEvent()
    {
        button.onClick.AddListener(delegate { Main.Instance.OnFileClick(this); });
    }
}
