//Refrence:https://docs.unity3d.com/ScriptReference/GL.html
//Usage: Attach this script a gameobject that the grid is follows
//COMPLETED
using UnityEngine;

public class GridOverlayGizmo : MonoBehaviour
{
    public bool Show = true;
    public bool Centralized = false;
    public int GridsizeX;
    public int GridsizeY;
    public int GridsizeZ;
    public float GridSizeMultipllier;
    public Vector3 GridPosition;
    public Color mainColor = new Color(0f, 1f, 0f, 1f);

    void OnDrawGizmos()
    {
        if (Show && GridSizeMultipllier != 0)
        {
            Gizmos.color = mainColor;

            int starti = 0;
            int startj = 0;
            int startk = 0;
            int endi = GridsizeX;
            int endj = GridsizeY;
            int endk = GridsizeZ;

            Vector3 position = GridPosition;
            if (Centralized)
            {
                starti = -GridsizeX / 2;
                startj = -GridsizeY / 2;
                startk = -GridsizeZ / 2;
                endi = (GridsizeX + 1) / 2;
                endj = (GridsizeY + 1) / 2;
                endk = (GridsizeZ + 1) / 2;

                //handle odd offset
                if (GridsizeX%2 == 1)//odd
                {
                    position -= new Vector3(GridSizeMultipllier/2,0f,0f);
                }
                if (GridsizeY % 2 == 1)//odd
                {
                    position -= new Vector3(0f, GridSizeMultipllier / 2, 0f);
                }
                if (GridsizeZ % 2 == 1)//odd
                {
                    position -= new Vector3(0f, 0f, GridSizeMultipllier / 2);
                }
            }
            Vector3 startline;
            Vector3 endline;
            //x
            for (int i = starti; i < endi + 1; i++)
            {
                //y
                for (int j = startj; j < endj + 1; j++)
                {
                    //z
                    for (int k = startk; k < endk + 1; k++)
                    {
                        //x
                        if (i + 1 < endi + 1)
                        {
                            startline = new Vector3(position.x + i * GridSizeMultipllier, position.y + j * GridSizeMultipllier, position.z + k * GridSizeMultipllier);
                            endline = new Vector3(position.x + (i + 1) * GridSizeMultipllier, position.y + j * GridSizeMultipllier, position.z + k * GridSizeMultipllier);
                            Gizmos.DrawLine(startline, endline);
                        }
                        //y
                        if (j + 1 < endj + 1)
                        {
                            startline = new Vector3(position.x + i * GridSizeMultipllier, position.y + j * GridSizeMultipllier, position.z + k * GridSizeMultipllier);
                            endline = new Vector3(position.x + i * GridSizeMultipllier, position.y + (j + 1) * GridSizeMultipllier, position.z + k * GridSizeMultipllier);
                            Gizmos.DrawLine(startline, endline);
                        }
                        //z
                        if (k + 1 < endk + 1)
                        {
                            startline = new Vector3(position.x + i * GridSizeMultipllier, position.y + j * GridSizeMultipllier, position.z + k * GridSizeMultipllier);
                            endline = new Vector3(position.x + i * GridSizeMultipllier, position.y + j * GridSizeMultipllier, position.z + (k + 1) * GridSizeMultipllier);
                            Gizmos.DrawLine(startline, endline);
                        }
                    }
                    GL.End();
                }
            }
        }
    }
}
