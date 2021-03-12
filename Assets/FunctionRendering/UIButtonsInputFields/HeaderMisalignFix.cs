using UnityEngine;
using UnityEngine.UI;

//This script fixes UI position bug when you enter play mode and exit play mode
public class HeaderMisalignFix : MonoBehaviour
{
    void Awake()
    {
        GetComponent<HorizontalLayoutGroup>().enabled = false;
    }
    void Start()
    {
        Invoke("LateStart",0.25f);
    }
    void LateStart()
    {
        GetComponent<HorizontalLayoutGroup>().enabled = true;
    }
}
