using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class FileItem : MonoBehaviour
{
    public int hierarchylevel;
    public VerticalLayoutGroup verticalLayoutGroup;
    public FileInfo fileinfo;

    public Button button;
    public void AddButtononClickEvent()
    {
        button.onClick.AddListener(delegate { Main.Instance.OnFileClick(this); });
    }

    public Text itemtext;
    public void SetName(string name)
    {
        itemtext.text = name;
    }

    public void SetHierachyLevel(int level)
    {
        hierarchylevel = level;
        verticalLayoutGroup.padding.left = hierarchylevel * 40;
    }
}
