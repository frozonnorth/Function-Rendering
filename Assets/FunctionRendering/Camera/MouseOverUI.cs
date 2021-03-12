using UnityEngine;
using UnityEngine.EventSystems;

//Special script that uses Unity Event System 
public class MouseOverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool isMouseOver;

    //These function gets called automatically by Unity Event System 
    //When the user's Pointer(Mouse/Touch) enters or exits (raycast collision) to the object
    //For UIs is when mouse intersect the Image component's rectangle - must have Raycast Target Enabled
    public void OnPointerEnter(PointerEventData eventData)
    {
        isMouseOver = true;
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        isMouseOver = false;
    }
}
