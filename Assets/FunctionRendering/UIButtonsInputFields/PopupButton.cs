using UnityEngine;

public class PopupButton : MonoBehaviour
{
	public void ShowOrHide()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}