using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class DirectoryItem : MonoBehaviour
{
    public int hierarchylevel;
    public VerticalLayoutGroup verticalLayoutGroup;
    public DirectoryInfo directoryinfo;
    public bool expanded = false;

    public List<DirectoryItem> childdirectoryitem = new List<DirectoryItem>();
    public void RemoveChildDirectory()
    {
        foreach (DirectoryItem child in childdirectoryitem)
        {
            Destroy(child.gameObject);
        }
        childdirectoryitem.Clear();
    }

    public List<FileItem> childfileitem = new List<FileItem>();
    public void RemoveChildFile()
    {
        foreach (FileItem child in childfileitem)
        {
            Destroy(child.gameObject);
        }
        childfileitem.Clear();
    }

    public Text itemtext;
    public void SetName(string name)
    {
        itemtext.text = name;
    }

    public void SetHierachyLevel(int level)
    {
        hierarchylevel = level;
        if (level != 0)
        hierarchylevel = 1;
        verticalLayoutGroup.padding.left = hierarchylevel * 40;
    }

    public Button button;
    public void AddButtononClickEvent()
    {
        button.onClick.AddListener(delegate { Main.Instance.OnDirectoryClick(this); });
    }
    public Image buttonicon;
    public Sprite openIcon;
    public Sprite closeIcon;
    public void UpdateIcon()
    {
        if(expanded)
        {
            buttonicon.sprite = openIcon;
        }
        else
        {
            buttonicon.sprite = closeIcon;
        }
    }

    //hard fix
    public void UpdateUI()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        DirectoryItem parent = transform.parent.GetComponent<DirectoryItem>();
        if(parent != null)
        {
            parent.UpdateUI();
        }
        else
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
        }
    }
}
