using UnityEngine;
using System.Collections.Generic;

public class SummonPiece : MonoBehaviour
{
    public GameObject XPiece;
    public GameObject OPiece;

    public void summonX()
    {
        Instantiate(XPiece);
    }

    public void summonO()
    {
        OPiece.SetActive(true);
    }
}
