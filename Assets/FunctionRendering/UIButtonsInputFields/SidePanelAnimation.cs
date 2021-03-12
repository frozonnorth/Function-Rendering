using UnityEngine;
using UnityEngine.UI;

public class SidePanelAnimation : MonoBehaviour
{
    public GameObject paneltomove;
    public Text buttontext;
    public string OpenText;
    public string CloseText;

    public enum SidePanelState { Show, Hide }
    public SidePanelState startState = SidePanelState.Show;
    SidePanelState currentsidePanelState;

    RectTransform rectTransform;
    public Vector3 OpenPosition;
    public Vector3 ClosedPosition;


    public float animateDuration = 0.5f;
    float animateTimer = 0;
    bool isShowing = false;
    bool isHiding = false;
    bool isAnimating => isShowing || isHiding;

    public void Start()
    {
        currentsidePanelState = startState;
        rectTransform = paneltomove.GetComponent<RectTransform>();

        if (currentsidePanelState == SidePanelState.Show)
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
        //animate open or close
        if(isAnimating)
        {
            animateTimer += Time.deltaTime;

            if (isShowing)
            {
                rectTransform.anchoredPosition = Vector3.Lerp(ClosedPosition, OpenPosition, animateTimer / animateDuration);
                if (animateTimer >= animateDuration)
                {
                    animateTimer = 0;

                    isShowing = false;

                    currentsidePanelState = SidePanelState.Show;
                    rectTransform.anchoredPosition = OpenPosition;
                    buttontext.text = CloseText;
                }

            }
            else if (isHiding)
            {
                rectTransform.anchoredPosition = Vector3.Lerp(OpenPosition, ClosedPosition, animateTimer / animateDuration);
                if (animateTimer >= animateDuration)
                {
                    animateTimer = 0;

                    isHiding = false;

                    currentsidePanelState = SidePanelState.Hide;
                    rectTransform.anchoredPosition = ClosedPosition;
                    buttontext.text = OpenText;
                }
            }
        }
    }

    void Toggle()
    {
        if (isShowing)
        {
            isShowing = false;
            isHiding = true;
        }
        else
        {
            isShowing = true;
            isHiding = false;
        }
    }

    public void Show()
    {
        animateTimer = 0;

        isShowing = true;
    }

    public void Hide()
    {
        animateTimer = 0;

        isHiding = true;
    }
}
