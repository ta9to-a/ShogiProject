using UnityEngine;

[CreateAssetMenu(fileName = "NewPieceData", menuName = "Shogi/Piece Data")]
public class PieceData : ScriptableObject
{
    [Header("駒の種類")]
    public PieceType pieceType;
    public PieceType promotedType;
    [Tooltip("駒の成りタイプ")]
    public PromotionType promotionType;

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
    
    [Header("駒の移動範囲")]
    [Tooltip("直線移動が可能かどうか")]
    public bool canStraightMove;
    [Tooltip("駒の移動可能なマスのリスト")]
    public Vector2Int[] moveRange;
    
    public enum PromotionType
    {
        None,
        Gold,  // 成金 - 歩・香・桂・銀の成駒共通の動き
        龍馬,  // 角行の成駒
        龍王   // 飛車の成駒
    }
}