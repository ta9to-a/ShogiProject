using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;

public class Piece : MonoBehaviour
{
    private PieceType _pieceType;
    private Turn _pieceTurn;
    
    private Sprite _currentSprite;
    private Sprite _unpromSprite;
    private Sprite _promSprite;
    
    /// <summary>
    /// 駒の情報を保存・更新する
    /// </summary>
    /// <param name="pieceType">駒の種類</param>
    /// <param name="unpromotedSprite">成る前のスプライト</param>
    /// <param name="promotedSprite">成り時のスプライト</param>
    public void ApplyStatePiece(PieceType pieceType, Sprite unpromotedSprite, Sprite promotedSprite)
    {
        // 駒の種類に応じてスプライトを設定
        _unpromSprite = unpromotedSprite;
        _promSprite = promotedSprite;
        
        _currentSprite = unpromotedSprite;
        GetComponent<SpriteRenderer>().sprite = _currentSprite;
        
        _pieceType = pieceType;
        _pieceTurn = transform.CompareTag("Sente") ? Turn.先手 : Turn.後手;
        
        transform.rotation = transform.CompareTag("Sente") ?
            Quaternion.Euler(0, 0, 0) :     // 先手
            Quaternion.Euler(0, 0, 180);    // 後手
    }

    /// <summary>
    /// 駒の選択処理
    /// </summary>
    public void SelectPiece()
    {
        if (ShogiManager.Instance.activePlayer == _pieceTurn) // 現在のプレイヤーが駒を操作できるターンかどうか
        {
            // 駒が選択されたときの処理
            if (ShogiManager.Instance.curSelPiece == null)  // 現在選択されている駒がない場合
            {
                // 駒が選択されていない場合、現在の駒を選択状態にする
                ShogiManager.Instance.curSelPiece = this.gameObject;
                Debug.Log(ShogiManager.Instance.curSelPiece.name + "が選択されました");
            }
            else // 現在選択されている駒がある場合
            {
                ShogiManager.Instance.curSelPiece = null;
                Debug.Log("駒の選択が解除されました");
            }
        }
    }

    /// <summary>
    /// 駒の移動処理
    /// </summary>
    /// <param name="movePosition"></param>
    public void MovePiece(Vector2 movePosition)
    {
        // 駒の移動処理
        transform.position = movePosition;
        
        ShogiManager.Instance.curSelPiece = null;
    }
}