using UnityEngine;

[CreateAssetMenu(fileName = "NewPieceData", menuName = "Shogi/Piece Data")]
public class PieceData : ScriptableObject
{
    [Tooltip("駒の種類")]
    public PieceType pieceType;

    [Header("スプライト")]
    [Tooltip("先手の通常スプライト")]
    public Sprite unpromotedSenteSprite;
    [Tooltip("後手の通常スプライト")]
    public Sprite unpromotedGoteSprite;

    [Header("成り駒のスプライト")]
    [Tooltip("先手の成りスプライト")]
    public Sprite promotedSenteSprite;
    [Tooltip("後手の成りスプライト")]
    public Sprite promotedGoteSprite;
}