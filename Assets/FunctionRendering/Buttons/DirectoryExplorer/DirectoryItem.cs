using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class that represent a directory item
/// </summary>
public class DirectoryItem : MonoBehaviour
{
    //To be assign in Unity Editor
    public VerticalLayoutGroup verticalLayoutGroup;
    public Button button;
    public Image buttonicon;
    public Sprite openIcon;
    public Sprite closeIcon;

    //To be assign and used by other classes
    public int hierarchylevel;
    public DirectoryInfo directoryinfo;
    public bool expanded = false;
    public List<DirectoryItem> childdirectoryitem = new List<DirectoryItem>();
    public List<FileItem> childfileitem = new List<FileItem>();

    //Text component that represent the display text/name of the directory
    public Text itemtext;
    public void SetName(string name)
    {
        itemtext.text = name;
    }

    public void UpdateIcon()
    {
        if (expanded)
        {
            buttonicon.sprite = openIcon;
        }
        else
        {
            buttonicon.sprite = closeIcon;
        }
    }
    //Executed when the fileitem is created
    void Start()
    {
        UpdateIcon();
    }

    //There is some complicting issues when using Unity VerticalLayoutGroup
    //This function is required to fix some of these issues in order to display the directory item correctly
    public void UpdateUI()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        DirectoryItem parent = transform.parent.GetComponent<DirectoryItem>();
        if (parent != null)
        {
            parent.UpdateUI();
        }
        else
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
        }
    }

    //Removes all child directory
    public void RemoveChildDirectory()
    {
        foreach (DirectoryItem child in childdirectoryitem)
        {
            Destroy(child.gameObject);
        }
        childdirectoryitem.Clear();
    }

    //Removes all child files
    public void RemoveChildFile()
    {
        foreach (FileItem child in childfileitem)
        {
            Destroy(child.gameObject);
        }
        childfileitem.Clear();
    }

    //Change the Hierachy Level of the directory item and increase left padding
    //To shift the item to the right for each hierachylevel
    public void SetHierachyLevel(int level)
    {
        hierarchylevel = level;
        if (level != 0)
        hierarchylevel = 1;
        verticalLayoutGroup.padding.left = hierarchylevel * 40;
    }

    //Helper function to add a buttton onclick event
    public void AddButtononClickEvent()
    {
        button.onClick.AddListener(delegate { Main.Instance.OnDirectoryClick(this); });
    }
}
