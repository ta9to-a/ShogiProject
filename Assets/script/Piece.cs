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
    
    private Vector2Int _currentPos;
    private int _moveDistance;

    /// <summary>
    /// 駒の情報を保存・更新する
    /// </summary>
    /// <param name="pieceType">駒の種類</param>
    /// <param name="position">駒の場所</param>
    /// <param name="unpromotedSprite">成る前のスプライト</param>
    /// <param name="promotedSprite">成り時のスプライト</param>
    public void ApplyStatePiece
        (PieceType pieceType, Vector2Int position, Sprite unpromotedSprite, Sprite promotedSprite)
    {
        // 駒の種類に応じてスプライトを設定
        _unpromSprite = unpromotedSprite;
        _promSprite = promotedSprite;

        _currentSprite = unpromotedSprite;
        GetComponent<SpriteRenderer>().sprite = _currentSprite;

        _pieceType = pieceType;
        
        // 先手と後手のタグを設定
        if (transform.CompareTag("Sente"))
        {
            _pieceTurn = Turn.先手;
            transform.rotation = Quaternion.Euler(0, 0, 0);
            _moveDistance = 1;
        }
        else
        {
            _pieceTurn = Turn.後手;
            transform.rotation = Quaternion.Euler(0, 0, 180);
            _moveDistance = -1;
        }
        
        // 駒の初期位置を設定
        SetPosition(position);
    }
    
    /// <summary>
    /// 駒のポジションの設定
    /// </summary>
    public void SetPosition(Vector2Int pos)
    {
        _currentPos = pos;
        transform.position = new Vector2(pos.x, pos.y);
    }

    /// <summary>
    /// 駒の選択処理
    /// </summary>
    public void SelectPiece()
    {
        if (ShogiManager.Instance.activePlayer == _pieceTurn) // 現在のプレイヤーが駒を操作できるターンかどうか
        {
            // 駒が選択されたときの処理
            if (ShogiManager.Instance.curSelPiece == null) // 現在選択されている駒がない場合
            {
                // 駒が選択されていない場合、現在の駒を選択状態にする
                ShogiManager.Instance.curSelPiece = this.gameObject;
                Debug.Log(ShogiManager.Instance.curSelPiece.name + "が選択されました");

                MovePiece();
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
    private async void MovePiece()
    {
        // 移動可能なマス目の取得
        List<Vector2Int> checkMovablePositions = CheckMovablePositions();
        // クリックされるまで待つ
        Vector2Int clickedPoint = await WaitForMouseClick();

        // クリックされた位置が移動可能なマス目かチェック
        if (!checkMovablePositions.Contains(clickedPoint))
        {
            Debug.Log("移動可能なマス目ではありません");
            ShogiManager.Instance.curSelPiece = null; // 駒の選択を解除
            return;
        }
        // 駒の移動処理
        ShogiManager.Instance.MovePiece(_currentPos, clickedPoint);
        // 成駒動作のチェック

        // 駒の状態を更新
        ShogiManager.Instance.EndMovePhase();
    }
    
    /// <summary>
    /// 移動可能なマス目をチェックする
    /// </summary>
    /// <returns>移動可能なマス目のリスト</returns>
    private List<Vector2Int> CheckMovablePositions()
    {
        const int boardMin = 1;
        const int boardMax = 9;

        PieceData pieceData = ShogiManager.Instance.pieceDatabase.GetPieceData(_pieceType);
        Vector2Int[] moveRange = pieceData.moveRange;
        
        List<Vector2Int> movablePositions = new List<Vector2Int>();
        foreach (var offset in moveRange)
        {
            if (!pieceData.canStraightMove)
            {
                Vector2Int newPos = _currentPos + offset * _moveDistance; // 移動先の座標を計算
                if (newPos.x < boardMin || newPos.x > boardMax || newPos.y < boardMin || newPos.y > boardMax) continue;
            
                // すでに駒があるかチェック
                PieceType checkPiece = ShogiManager.Instance.GetPieceTypeAt(newPos);
                Piece checkPieceObj = ShogiManager.Instance.GetPieceAt(newPos);
            
                // 空マス or 相手の駒なら移動可能
                if (checkPiece == PieceType.None ||
                    (checkPieceObj != null && checkPieceObj._pieceTurn != _pieceTurn))
                {
                    movablePositions.Add(newPos);
                }
            }
            else
            {
                // 直線移動の場合、移動可能なマス目を全てチェック
                for (int i = 1; i < 9; i++)
                {
                    Vector2Int newPos = _currentPos + offset * i * _moveDistance; // 移動先の座標を計算
                    if (newPos.x < boardMin || newPos.x > boardMax || newPos.y < boardMin || newPos.y > boardMax) break;

                    // すでに駒があるかチェック
                    PieceType checkPiece = ShogiManager.Instance.GetPieceTypeAt(newPos);
                    Piece checkPieceObj = ShogiManager.Instance.GetPieceAt(newPos);

                    // 空マス or 相手の駒なら移動可能
                    if (checkPiece == PieceType.None)
                    {
                        movablePositions.Add(newPos);
                    }
                    else if (checkPieceObj != null && checkPieceObj._pieceTurn != _pieceTurn)
                    {
                        movablePositions.Add(newPos);
                        break; // 相手の駒がある場合は処理の終了
                    }
                }
            }
        }

        return movablePositions;
    }

    /// <summary>
    /// マウスクリックを待機する
    /// </summary>
    /// <returns>クリックされた位置の座標</returns>
    private async UniTask<Vector2Int> WaitForMouseClick()
    {
        await UniTask.WaitUntil(() => Input.GetMouseButtonDown(0));

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return new Vector2Int(Mathf.RoundToInt(mousePos.x), Mathf.RoundToInt(mousePos.y));
    }
}