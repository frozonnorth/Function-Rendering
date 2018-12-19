
using UnityEngine;
using UnityEngine.UI;

public class PopupMessage : MonoBehaviour
{
    public static PopupMessage Instance;
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public Image popupImage;
    public Text popupText;

    float fadetime = 2.5f;
    float fadetimer = 0f;
    bool isfading = false;

	void Update ()
    {
        if(isfading)
        {
            fadetimer += Time.deltaTime;
            if(fadetimer > fadetime)
            {
                isfading = false;
                fadetimer = fadetime;
            }
            setTransparency(fadetimer / fadetime);
        }
	}
    public void ShowMessage(string text)
    {
        popupText.text = text;
        setTransparency(0);

        isfading = true;
        fadetimer = 0;
    }
    void setTransparency(float value)//0-1 , 1 fully trasparent
    {
        Color popupImageCurrentColor = popupImage.color;
        popupImage.color = new Color(popupImageCurrentColor.r, popupImageCurrentColor.g, popupImageCurrentColor.b, (1 - value) * 100/255);

        Color popupTextCurrentColor = popupText.color;
        popupText.color = new Color(popupTextCurrentColor.r, popupTextCurrentColor.g, popupTextCurrentColor.b, 1-value);
    }
}
