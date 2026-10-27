using UnityEngine;

public class TicTacToePiece : MonoBehaviour
{
    public enum PieceType { X, O }

    public PieceType pieceType;

    public int moveOriginIndex = -1;
}