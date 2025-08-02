using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;

public class Piece : MonoBehaviour
{
    private PieceType _pieceType;
    
    private Sprite _currentSprite;
    private Sprite _unpromSprite;
    private Sprite _promSprite;
    
    public void ApplyStatePiece(PieceType pieceType, Sprite unpromotedSprite, Sprite promotedSprite)
    {
        // 駒の種類に応じてスプライトを設定
        _unpromSprite = unpromotedSprite;
        _promSprite = promotedSprite;
        
        _currentSprite = unpromotedSprite;
        GetComponent<SpriteRenderer>().sprite = _currentSprite;
        
        _pieceType = pieceType;
        
        transform.rotation = transform.CompareTag("Sente") ?
            Quaternion.Euler(0, 0, 0) :     // 先手
            Quaternion.Euler(0, 0, 180);    // 後手
    }

    void OnMouseDown()
    {
        Debug.Log(gameObject.name + " piece clicked");
    }

    /// <summary>
    /// 駒の移動処理
    /// </summary>
    /// <param name="movePosition"></param>
    public void MovePiece(Vector2 movePosition)
    {
        // 駒の移動処理
        transform.position = movePosition;
    }
}