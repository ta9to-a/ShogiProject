using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapturePieceChild : MonoBehaviour
{
    public PieceType capturePieceType;  // 駒の種類
    public Turn capturePieceTurn;
    
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
        if (ShogiManager.Instance.activePlayer != capturePieceTurn) return;
        
        // 駒の選択処理
        if (ShogiManager.Instance.activePlayer == capturePieceTurn)
        {
            if (ShogiManager.Instance.curSelPiece == null)
            {
                ShogiManager.Instance.curSelPiece = this.gameObject;
                Debug.Log(ShogiManager.Instance.curSelPiece.name + "が選択されました");
                
                var key = (capturePieceType, capturePieceTurn);
                GameObject typeParent = CapturePieceUIManager.instance.CloneGroups[key][0];
                ShogiManager.Instance.curSelPiece = typeParent;
                
                CapturePiece cp = typeParent.GetComponent<CapturePiece>();
                if (cp != null)
                {
                    cp.SettingCapturePiece();
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
