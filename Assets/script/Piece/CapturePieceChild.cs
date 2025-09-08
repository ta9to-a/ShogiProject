using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapturePieceChild : MonoBehaviour
{
    public PieceType capturePieceType;  // 駒の種類
    public Turn capturePieceTurn;       // 駒のターン（先手 or 後手）
    
    public void ApplyStateCapturePiece(PieceType pieceType, Sprite captureSprite)
    {
        // 駒の種類に応じてスプライトを設定
        capturePieceType = pieceType;
        
        // スプライトを設定
        GetComponent<SpriteRenderer>().sprite = captureSprite;

        if (transform.CompareTag("Sente"))
        {
            capturePieceTurn = Turn.先手;
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            capturePieceTurn = Turn.後手;
            transform.rotation = Quaternion.Euler(0, 0, 180);
        }
    }
    
    /// <summary>
    /// 持ち駒の選択処理
    /// </summary>
    public void SelectCapturePiece()
    {
        if (ShogiManager.Instance.ActivePlayer != capturePieceTurn) return;
        
        // 駒の選択処理
        if (ShogiManager.Instance.ActivePlayer == capturePieceTurn)
        {
            if (ShogiManager.Instance.curSelPiece == null)
            {
                ShogiManager.Instance.curSelPiece = this.gameObject;
                Debug.Log(ShogiManager.Instance.curSelPiece.name + "が選択されました");
                
                var key = (capturePieceType, capturePieceTurn);
                GameObject typeParent = CapturePieceUIManager.Instance.CloneGroups[key][0];
                ShogiManager.Instance.curSelPiece = typeParent;
                
                CapturePieceParent cp = typeParent.GetComponent<CapturePieceParent>();
                if (cp != null)
                {
                    cp.DropPiece();
                }
            }
            else
            {
                ShogiManager.Instance.curSelPiece = null;
                Debug.Log("駒の選択が解除されました");
            }
        }
    }
}
