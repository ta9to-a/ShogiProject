using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapturePiece : MonoBehaviour
{
    private PieceType _capturePieceType; // 駒の種類
    private int _heldPieceCount; // 持ち駒の数
    private Turn _capturePieceTurn;
    
    private Sprite _captureSprite; // 成る前のスプライト
    
    /// <summary>
    /// 持ち駒の情報を保存・更新する
    /// </summary>
    /// <param name="pieceType">持ち駒の種類</param>
    /// <param name="captureSprite">持ち駒のスプライト</param>
    public void ApplyStateCapturePiece(PieceType pieceType, Sprite captureSprite)
    {
        // 駒の種類に応じてスプライトを設定
        _capturePieceType = pieceType;
        _capturePieceTurn = transform.CompareTag("Sente") ? Turn.先手 : Turn.後手;
        
        _captureSprite = captureSprite;
        
        // スプライトを設定
        GetComponent<SpriteRenderer>().sprite = _captureSprite;
        
        // 先手と後手のタグを設定
        transform.rotation = transform.CompareTag("Sente") ?
            Quaternion.Euler(0, 0, 0) :     // 先手
            Quaternion.Euler(0, 0, 180);    // 後手

        UpdateVisualState();
    }
    
    /// <summary>
    /// 持ち駒の選択処理
    /// </summary>
    public void SelectCapturePiece()
    {
        // 駒の選択処理
        if (ShogiManager.Instance.activePlayer == _capturePieceTurn)
        {
            if (ShogiManager.Instance.curSelPiece == null)
            {
                ShogiManager.Instance.curSelPiece = this.gameObject;
                Debug.Log(ShogiManager.Instance.curSelPiece.name + "が選択されました");
            }
            else
            {
                ShogiManager.Instance.curSelPiece = null;
                Debug.Log("駒の選択が解除されました");
            }
        }
    }

    /// <summary>
    /// 持ち駒のビジュアルを状態に応じて更新する
    /// </summary>
    private void UpdateVisualState()
    {
        int pieceIndex = (int)_capturePieceType;
        // 先手と後手の持ち駒の数を取得
        int currentCount = _capturePieceTurn == Turn.先手 ? 
            ShogiManager.Instance.senteCapturedPieceType[pieceIndex] : 
            ShogiManager.Instance.goteCapturedPieceType[pieceIndex];
        
        // スプライトの色を更新
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = currentCount > 0 ? Color.white : Color.gray;
    }
}
