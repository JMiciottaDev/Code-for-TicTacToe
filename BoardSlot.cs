using UnityEngine;

public class BoardSlot : MonoBehaviour
{
    public int index;
    public Transform placementPoint;

    [Tooltip("Slots this square can move to in one move.")]
    public BoardSlot[] adjacentSlots;
}
