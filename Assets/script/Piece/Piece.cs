using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;

public class Piece : MonoBehaviour
{
    private PieceType _basePieceType;       // 駒の基本種類
    private PieceType _currentPieceType;    // 駒の種類
    private Turn _pieceTurn;                // 駒のターン（先手 or 後手）

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
    /// <param name="unpromotedSprite">成る前のスプライト</param>
    /// <param name="promotedSprite">成り時のスプライト</param>
    public void ApplyStatePiece
        (PieceType pieceType, Vector2Int position, bool isPromoted, Sprite unpromotedSprite, Sprite promotedSprite)
    {
        // 駒の種類に応じてスプライトを設定
        _unpromSprite = unpromotedSprite;
        _promSprite = promotedSprite;

        _basePieceType = pieceType;
        _currentSprite =
            !isPromoted ? unpromotedSprite : promotedSprite;
        GetComponent<SpriteRenderer>().sprite = _currentSprite;
        
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
        transform.position = new Vector2(pos.x, pos.y);
    }

    /// <summary>
    /// 駒の選択処理
    /// </summary>
    public void SelectPiece()
    {
        // プレイヤーのターン確認
        if (ShogiManager.Instance.activePlayer != _pieceTurn) return;

        // 選択中の駒の確認
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

    /// <summary>
    /// 駒の移動処理
    /// </summary>
    private async void MovePiece()
    {
        // 駒の動きを取得
        Vector2Int[] moves = GetMoveRange();
        // 移動可能なマス目の取得
        List<Vector2Int> checkMovablePositions = CheckMovablePositions(moves);
        // クリックされるまで待つ
        Vector2Int clickedPoint = await WaitForMouseClick();

        // クリックされた位置が移動可能なマス目かチェック
        if (!checkMovablePositions.Contains(clickedPoint) || clickedPoint == _currentPos)
        {
            ShogiManager.Instance.CancelSelection();
            return;
        }
        
        if (ShogiManager.Instance.GetPieceAt(clickedPoint) != null)
        {
            Debug.Log("駒を捕獲しました: " + ShogiManager.Instance.GetPieceAt(clickedPoint)._currentPieceType);
        }

        // 駒の移動処理
        ShogiManager.Instance.MovePiece(_currentPos, clickedPoint);

        // 駒の成駒処理
        PieceData pieceData = ShogiManager.Instance.pieceDatabase.GetPieceData(_basePieceType);
        if (pieceData.promotionType != PromotionType.None && !_isPromoted)
        {
            // 現在、敵陣にいるかどうか
            bool nowInEnemyCamp =
                (_pieceTurn == Turn.先手 && clickedPoint.y >= 7) ||
                (_pieceTurn == Turn.後手 && clickedPoint.y <= 3);

            // 前のターン、敵陣にいたかどうか
            bool leftEnemyCampThisTurn = _wasInEnemyCamp && !nowInEnemyCamp;

            // 成駒の条件を満たしている場合
            if (nowInEnemyCamp || leftEnemyCampThisTurn)
            {
                // 成るかどうかのUIを表示
                bool isPromote = await PromotionUIManager.Instance.ShowAsync(_currentPos, _unpromSprite, _promSprite);
                if (isPromote)
                {
                    // 成る処理
                    _isPromoted = true;
                    _currentPieceType = pieceData.promotedType; // 駒の種類を更新
                    GetComponent<SpriteRenderer>().sprite = _promSprite;
                    Debug.Log("駒が成りました: " + _currentPieceType);
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

        // 駒の状態を更新
        ShogiManager.Instance.EndTurnPhase();
    }

    /// <summary>
    /// 成り不成の状態を取得し、動き方を返す
    /// </summary>
    private Vector2Int[] GetMoveRange()
    {
        PieceData pieceData = ShogiManager.Instance.pieceDatabase.GetPieceData(_basePieceType);
        
        // 駒がなっていない、もしくは成駒動作が存在しない場合
        if (!_isPromoted || pieceData.promotionType == PromotionType.None) return pieceData.moveRange;
        Debug.Log("成り駒の動き方を取得");

        // 成駒のデータを取得
        PromotionData promotionData = ShogiManager.Instance.promotionDatabase.GetPromotionData(pieceData.promotionType);
        if (!promotionData.moveUpdate)  // 成駒動作を追加する場合
        {
            Debug.Log("成駒動作の追加");
            _combined.Clear();
            foreach (Vector2Int move in promotionData.moveRange)
            {
                _combined.Add(move);
            }
            Debug.Log(_combined.Count + "個の成駒動作が追加されました");
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
        const int boardMin = 1; // ボードの最小座標
        const int boardMax = 9; // ボードの最大座標

        PieceData pieceData = ShogiManager.Instance.pieceDatabase.GetPieceData(_basePieceType);
        List<Vector2Int> movablePositions = new List<Vector2Int>();
        
        foreach (var point in moves)
        {
            if (!pieceData.canStraightMove)
            {
                Vector2Int newPos = _currentPos + point * _moveDistance; // 移動先の座標を計算
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
            else if (pieceData.canStraightMove)
            {
                // 直線移動の場合、移動可能なマス目を全てチェック
                for (int i = 1; i < 9; i++)
                {
                    Vector2Int newPos = _currentPos + point * i * _moveDistance; // 移動先の座標を計算
                    if (newPos.x < boardMin || newPos.x > boardMax || newPos.y < boardMin || newPos.y > boardMax) break;

                    // すでに駒があるかチェック
                    PieceType checkPiece = ShogiManager.Instance.GetPieceTypeAt(newPos);
                    Piece checkPieceObj = ShogiManager.Instance.GetPieceAt(newPos);
                    
                    if (checkPiece == PieceType.None)   // 空マスなら移動ポジションに追加
                    {
                        movablePositions.Add(newPos);
                    }
                    else
                    {
                        if (checkPieceObj !=null && checkPieceObj._pieceTurn != _pieceTurn) // 敵駒なら移動ポジションに追加
                        {
                            movablePositions.Add(newPos);
                        }
                        break;
                    }
                }
                // 成駒時の動き方を追加
                if (_isPromoted)
                {
                    //Debug.Log("_combinedの長さ: " + _combined.Count);
                    foreach (Vector2Int promPoint in _combined)
                    {
                        Vector2Int newPos = _currentPos + promPoint * _moveDistance; // 成駒の移動先の座標を計算
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
                }
            }
        }

        for (int i = 0; i < movablePositions.Count; i++)
        {
            Debug.Log(movablePositions[i]);
        }
        return movablePositions;
    }

    /*private List<Vector2Int> AddPiecePos()
    {
        const int boardMin = 1;
        const int boardMax = 9;
        
        Vector2Int newPos = _currentPos + offset * _moveDistance; // 移動先の座標を計算
        if (newPos.x < boardMin || newPos.x > boardMax || newPos.y < boardMin || newPos.y > boardMax)
        {
            continue;
        }

        // すでに駒があるかチェック
        PieceType checkPiece = ShogiManager.Instance.GetPieceTypeAt(newPos);
        Piece checkPieceObj = ShogiManager.Instance.GetPieceAt(newPos);

        // 空マス or 相手の駒なら移動可能
        if (checkPiece == PieceType.None ||
            (checkPieceObj != null && checkPieceObj._pieceTurn != _pieceTurn))
        {
            movablePositions.Add(newPos);
        }
    }*/

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