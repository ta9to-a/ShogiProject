using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;

public class Piece : MonoBehaviour
{
    public PieceType basePieceType;         // 駒の基本種類
    private PieceType _currentPieceType;    // 駒の種類
    public Turn pieceTurn;                  // 駒のターン（先手 or 後手）

    private Vector2Int _currentPos;     // 駒の現在位置
    private int _moveDistance;          // 駒の移動方向（先手は1、後手は-1）
    private bool _isPromoted;           // 駒が成り駒かどうか
    private bool _wasInEnemyCamp;       // 前のターンで敵陣にいたかどうか

    private Sprite _currentSprite;
    private Sprite _unpromSprite;
    private Sprite _promSprite;

    List<Vector2Int> _combined = new(); // 駒の移動範囲を結合するためのリスト

    /// <summary>
    /// 駒の情報を保存・更新する
    /// </summary>
    /// <param name="pieceType">駒の種類</param>
    /// <param name="position">駒の場所</param>
    /// <param name="isPromoted">成るか否か</param>
    /// <param name="unpromSprite">成る前のスプライト</param>
    /// <param name="promoSprite">成り時のスプライト</param>
    public void ApplyStatePiece
        (PieceType pieceType, Vector2Int position, bool isPromoted, Sprite unpromSprite, Sprite promoSprite)
    {
        // 駒の種類に応じてスプライトを設定
        _unpromSprite = unpromSprite;
        _promSprite = promoSprite;

        basePieceType = pieceType;
        _currentSprite =
            !isPromoted ? unpromSprite : promoSprite;
        GetComponent<SpriteRenderer>().sprite = _currentSprite;
        
        // 先手と後手のタグを設定
        if (transform.CompareTag("Sente"))
        {
            pieceTurn = Turn.先手;
            transform.rotation = Quaternion.Euler(0, 0, 0);
            _moveDistance = 1;
        }
        else
        {
            pieceTurn = Turn.後手;
            transform.rotation = Quaternion.Euler(0, 0, 180);
            _moveDistance = -1;
        }
        
        _isPromoted = isPromoted; // 駒が成り駒かどうか
        _wasInEnemyCamp = false;

        // 駒の初期位置を設定
        SetPosition(position);
    }

    /// <summary>
    /// 駒のポジションの設定
    /// </summary>
    public void SetPosition(Vector2Int pos)
    {
        _currentPos = pos;
        transform.position = new Vector2(_currentPos.x, _currentPos.y);
    }
    
    /// <summary>
    /// 駒を成る処理
    /// </summary>
    public void PromotePiece(PieceData piece)
    {
        _isPromoted = true;
        _currentPieceType = piece.promotedType; // 駒の種類を更新
        GetComponent<SpriteRenderer>().sprite = _promSprite;
        Debug.Log("駒が成りました: " + _currentPieceType);
    }

    /// <summary>
    /// 駒の選択処理
    /// </summary>
    public void SelectPiece()
    {
        // プレイヤーのターン確認
        if (ShogiManager.instance.activePlayer != pieceTurn) return;

        // 選択中の駒の確認
        if (ShogiManager.instance.curSelPiece == null) // 現在選択されている駒がない場合
        {
            // 駒が選択されていない場合、現在の駒を選択状態にする
            ShogiManager.instance.curSelPiece = this.gameObject;
            Debug.Log(ShogiManager.instance.curSelPiece.name + "が選択されました");
            MovePiece();
        }
        else // 現在選択されている駒がある場合
        {
            ShogiManager.instance.curSelPiece = null;
            Debug.Log("駒の選択が解除されました");
        }
    }

    /// <summary>
    /// 駒の移動処理
    /// </summary>
    private async void MovePiece()
    {
        // 駒の動きを取得
        Vector2Int[] moves = GetMoveRange();
        // 移動可能なマス目の取得
        List<Vector2Int> checkMovablePositions = CheckMovablePositions(moves);
        // 移動可能なマス目をハイライト表示
        ShogiManager.instance.moveHighlight.SetCanMovePosHighlight(checkMovablePositions);
        
        // クリックされるまで待つ
        Vector2Int clickedPoint = await WaitForMouseClick();

        // クリックされた位置が移動可能なマス目かチェック
        if (!checkMovablePositions.Contains(clickedPoint) || clickedPoint == _currentPos)
        {
            ShogiManager.instance.CancelSelection();
            return;
        }
        
        // クリックされた位置に駒があるかチェック
        if (ShogiManager.instance.GetPieceAt(clickedPoint) != null) ShogiManager.instance.AddCapturedPiece(clickedPoint);

        // 駒の移動処理
        ShogiManager.instance.MovePiece(_currentPos, clickedPoint);

        // 駒の成駒処理
        CheckPromotion();

        // 駒の状態を更新
        ShogiManager.instance.EndTurnPhase(clickedPoint);
    }

    /// <summary>
    /// 成り不成の状態を取得し、動き方を返す
    /// </summary>
    private Vector2Int[] GetMoveRange()
    {
        PieceData pieceData = ShogiManager.instance.pieceDatabase.GetPieceData(basePieceType);
        
        // 駒がなっていない、もしくは成駒動作が存在しない場合
        if (!_isPromoted || pieceData.promotionType == PromotionType.None) return pieceData.moveRange;

        // 成駒のデータを取得
        PromotionData promotionData = ShogiManager.instance.promotionDatabase.GetPromotionData(pieceData.promotionType);
        if (!promotionData.moveUpdate)  // 成駒動作を追加する場合
        {
            _combined.Clear();
            foreach (Vector2Int move in promotionData.moveRange)
            {
                _combined.Add(move);
            }
            return pieceData.moveRange;
        }
        
        return promotionData.moveRange; // 成駒動作を更新する場合
    }

    /// <summary>
    /// 移動可能なマス目をチェックする
    /// </summary>
    /// <returns>移動可能なマス目のリスト</returns>
    private List<Vector2Int> CheckMovablePositions(Vector2Int[] moves)
    {
        PieceData pieceData = ShogiManager.instance.pieceDatabase.GetPieceData(basePieceType);
        List<Vector2Int> movablePositions = new List<Vector2Int>();
        
        foreach (Vector2Int dir in moves)
        {
            if (!pieceData.canStraightMove)
            {
                Vector2Int target = _currentPos + dir * _moveDistance;
                AddPiecePos(target, movablePositions);
            }
            else if (pieceData.canStraightMove)
            {
                // 直線移動の場合、移動可能なマス目を全てチェック
                for (int i = 1; i < 9; i++)
                {
                    Vector2Int target = _currentPos + dir * i * _moveDistance;
                    if (!AddPiecePos(target, movablePositions)) break;  // すでに駒がある場合はそれ以上移動しない
                }
                // 成駒時の動き方を追加
                if (_isPromoted)
                {
                    foreach (Vector2Int promDir in _combined)
                    {
                        Vector2Int target = _currentPos + promDir * _moveDistance;
                        AddPiecePos(target, movablePositions);
                    }
                }
            }
        }
        return movablePositions;
    }

    /// <summary>
    /// movablePositionsに移動可能なマス目を追加する
    /// </summary>
    private bool AddPiecePos(Vector2Int pos, List<Vector2Int> movablePositions)
    {
        const int boardMin = 1;
        const int boardMax = 9;
        
        if (pos.x < boardMin || pos.x > boardMax || pos.y < boardMin || pos.y > boardMax) return false;

        // すでに駒があるかチェック
        PieceType checkPiece = ShogiManager.instance.GetPieceTypeAt(pos);
        Piece checkPieceObj = ShogiManager.instance.GetPieceAt(pos);

        // 空マス or 相手の駒なら移動可能
        if (checkPiece == PieceType.None)
        {
            movablePositions.Add(pos);
            return true;
        }
        else if (checkPieceObj != null && checkPieceObj.pieceTurn != pieceTurn)
        {
            movablePositions.Add(pos);
        }
        return false;
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
    
    /// <summary>
    /// 成駒のチェックと選択
    /// </summary>
    private async void CheckPromotion()
    {
        PieceData pieceData = ShogiManager.instance.pieceDatabase.GetPieceData(basePieceType);
        if (pieceData.promotionType != PromotionType.None && !_isPromoted)
        {
            switch (pieceData.pieceType)
            {
                case PieceType.歩兵:
                case PieceType.香車:
                    if (pieceTurn == Turn.先手 && _currentPos.y >= 9 ||
                        pieceTurn == Turn.後手 && _currentPos.y <= 1)
                    {
                        ShogiManager.instance.PromotePiece(_currentPos, pieceData);
                        return;
                    }
                    break;
                case PieceType.桂馬:
                    if (pieceTurn == Turn.先手 && _currentPos.y >= 8 ||
                        pieceTurn == Turn.後手 && _currentPos.y <= 2)
                    {
                        ShogiManager.instance.PromotePiece(_currentPos, pieceData);
                        return;
                    }
                    break;
            }
            
            // 現在、敵陣にいるかどうか
            bool nowInEnemyCamp =
                pieceTurn == Turn.先手 && _currentPos.y >= 7 ||
                pieceTurn == Turn.後手 && _currentPos.y <= 3;

            // 前のターン、敵陣にいたかどうか
            bool leftEnemyCampThisTurn = _wasInEnemyCamp && !nowInEnemyCamp;

            // 成駒の条件を満たしている場合
            if (nowInEnemyCamp || leftEnemyCampThisTurn)
            {
                // 成るかどうかのUIを表示
                bool isPromote = await PromotionUIManager.instance.ShowAsync(_currentPos, _unpromSprite, _promSprite);
                if (isPromote)
                {
                    // 成る処理
                    ShogiManager.instance.PromotePiece(_currentPos, pieceData);
                }
                else if (nowInEnemyCamp)
                {
                    _wasInEnemyCamp = true; // 成り駒の状態を記録
                }
                else
                {
                    _wasInEnemyCamp = false;
                }
            }
        }
    }
}