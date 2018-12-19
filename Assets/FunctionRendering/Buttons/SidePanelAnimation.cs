using UnityEngine;
using UnityEngine.UI;

public class SidePanelAnimation : MonoBehaviour
{
    public GameObject paneltomove;
    public Text buttontext;
    public string OpenText;
    public string CloseText;

    public enum SidePanelState { Open, Close }
    public SidePanelState startState = SidePanelState.Open;
    SidePanelState currentsidePanelState;

    RectTransform rectTransform;
    public Vector3 OpenPosition;
    public Vector3 ClosedPosition;
    public float animateDuration = 0.5f;
    bool isAnimating = false;
    float animateTimer = 0;

    public void Start()
    {
        currentsidePanelState = startState;
        rectTransform = paneltomove.GetComponent<RectTransform>();

        if (currentsidePanelState == SidePanelState.Open)
        {
            rectTransform.anchoredPosition = OpenPosition;
            buttontext.text = CloseText;
        }
        else
        {
            rectTransform.anchoredPosition = ClosedPosition;
            buttontext.text = OpenText;
        }
    }

    public void Update()
    {
        if(isAnimating)
        {
            animateTimer += Time.deltaTime;

            //closing
            if (currentsidePanelState == SidePanelState.Open)
            {
                rectTransform.anchoredPosition = Vector3.Lerp(OpenPosition, ClosedPosition, animateTimer / animateDuration);
                if (animateTimer>=animateDuration)
                {
                    animateTimer = 0;
                    isAnimating = false;

                    currentsidePanelState = SidePanelState.Close;
                    rectTransform.anchoredPosition = ClosedPosition;
                    buttontext.text = OpenText;
                }
                
            }
            //opening
            else if (currentsidePanelState == SidePanelState.Close)
            {
                rectTransform.anchoredPosition = Vector3.Lerp(ClosedPosition, OpenPosition, animateTimer / animateDuration);
                if (animateTimer >= animateDuration)
                {
                    animateTimer = 0;
                    isAnimating = false;

                    currentsidePanelState = SidePanelState.Open;
                    rectTransform.anchoredPosition = OpenPosition;
                    buttontext.text = CloseText;
                }
            }
        }
    }
    public void Animate()
    {
        if (!isAnimating)
        {
            isAnimating = true;
        }
    }
}
