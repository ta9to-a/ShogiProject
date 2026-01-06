using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class Piece : MonoBehaviour
{
    public PieceType BasePieceType { get; private set; }    // 駒の基本種類
    public Turn PieceTurn { get; private set; }             // 駒のターン（先手 or 後手）
    public Vector2Int currentPos;      // 駒の現在位置
    
    private int _moveDistance;          // 駒の移動方向（先手は1、後手は-1）
    public bool isPromoted;           // 駒が成り駒かどうか
    private bool _wasInEnemyCamp;       // 前のターンで敵陣にいたかどうか
    
    public List<Vector2Int> MovablePositions { get; private set; } = new ();    // 駒の移動可能なマス目のリスト
    private List<Vector2Int> _extraStepDirs = new();                            // 成駒時の追加の動き方
    
    private Sprite _unpromSprite;
    private Sprite _promSprite;
    
    public void ApplyStatePiece
        (PieceType pieceType, Vector2Int position, bool isPromote, Sprite unpromSprite, Sprite promoSprite)
    {
        BasePieceType = pieceType;
        
        // 駒の種類に応じてスプライトを設定
        _unpromSprite = unpromSprite;
        _promSprite = promoSprite;
        
        Sprite currentSprite =
            !isPromote ? unpromSprite : promoSprite;
        GetComponent<SpriteRenderer>().sprite = currentSprite;
        
        // 先手と後手のタグを設定
        if (transform.CompareTag("Sente"))
        {
            PieceTurn = Turn.先手;
            transform.rotation = Quaternion.Euler(0, 0, 180);
            _moveDistance = -1;
            
        }
        else
        {
            PieceTurn = Turn.後手;
            transform.rotation = Quaternion.Euler(0, 0, 0);
            _moveDistance = 1;
        }
        
        isPromoted = isPromote; // 駒が成り駒かどうか
        _wasInEnemyCamp = (PieceTurn == Turn.先手) ? position.y <= 3 : position.y >= 7;

        // 駒の初期位置を設定
        SetPosition(position);
    }
    
    public void SetPosition(Vector2Int pos)
    {
        currentPos = pos;
        transform.position = new Vector2(currentPos.x, currentPos.y);
    }
    
    public void PromotePiece()
    {
        isPromoted = true;
        GetComponent<SpriteRenderer>().sprite = _promSprite;
    }
    
    public void SelectPiece()
    {
        // プレイヤーのターン確認
        if (ShogiManager.Instance.ActivePlayer != PieceTurn || !ShogiManager.Instance.CanPieceSelect) return;

        // 選択中の駒の確認
        if (ShogiManager.Instance.curSelPiece == null) // 現在選択されている駒がない場合
        {
            // 駒が選択されていない場合、現在の駒を選択状態にする
            ShogiManager.Instance.curSelPiece = this.gameObject;
            MovePiece();
        }
        else // 現在選択されている駒がある場合
        {
            ShogiManager.Instance.curSelPiece = null;
        }
    }

    /// <summary>
    /// 駒の移動処理
    /// </summary>
    private async void MovePiece()
    {
        // 移動可能なマス目をハイライト表示
        // ShogiManager.Instance.moveHighlight.SetCanMovePosHighlight(MovablePositions);
        
        // クリックされるまで待つ
        Vector2Int clickedPoint = await WaitForMouseClick();

        // クリックされた位置が移動可能なマス目かチェック
        if (!MovablePositions.Contains(clickedPoint) || clickedPoint == currentPos)
        {
            ShogiManager.Instance.CancelSelection();
            return;
        }
        
        // 駒の移動処理
        ShogiManager.Instance.MovePiece(currentPos, clickedPoint);

        // 駒の成駒処理
        await CheckPromotion();

        // 駒の状態を更新
        ShogiManager.Instance.EndTurnPhase(clickedPoint);
    }

    /// <summary>
    /// 駒の移動可能なマス目を更新
    /// </summary>
    public void GetMovePoints()
    {
        Vector2Int[] moves = GetMoveRange();
        CheckMovablePositions(moves);
    }

    /// <summary>
    /// 成り不成の状態を取得し、動き方を返す
    /// </summary>
    private Vector2Int[] GetMoveRange()
    {
        PieceData pieceData = ShogiManager.Instance.pieceDatabase.GetPieceData(BasePieceType);
        
        // 駒がなっていない、もしくは成駒動作が存在しない場合
        if (!isPromoted || pieceData.promotionType == PromotionType.None) return pieceData.moveRange;

        // 成駒のデータを取得
        PromotionData promotionData = ShogiManager.Instance.promotionDatabase.GetPromotionData(pieceData.promotionType);
        if (!promotionData.moveUpdate)  // 成駒動作を追加する場合
        {
            _extraStepDirs.Clear();
            foreach (Vector2Int move in promotionData.moveRange)
            {
                _extraStepDirs.Add(move);
            }
            return pieceData.moveRange;
        }
        
        return promotionData.moveRange; // 成駒動作を更新する場合
    }

    /// <summary>
    /// 移動可能なマス目をチェックする
    /// </summary>
    /// <returns>移動可能なマス目のリスト</returns>
    private void CheckMovablePositions(Vector2Int[] moves)
    {
        MovablePositions.Clear();
        
        PieceData pieceData = ShogiManager.Instance.pieceDatabase.GetPieceData(BasePieceType);
        
        foreach (Vector2Int dir in moves)
        {
            if (!pieceData.canStraightMove)
            {
                Vector2Int target = currentPos + dir * _moveDistance;
                AddPiecePos(target);
            }
            else if (pieceData.canStraightMove)
            {
                // 直線移動の場合、移動可能なマス目を全てチェック
                for (int i = 1; i < 9; i++)
                {
                    Vector2Int target = currentPos + dir * i * _moveDistance;
                    if (!AddPiecePos(target)) break;  // すでに駒がある場合はそれ以上移動しない
                }
                // 成駒時の動き方を追加
                if (isPromoted)
                {
                    foreach (Vector2Int promDir in _extraStepDirs)
                    {
                        Vector2Int target = currentPos + promDir * _moveDistance;
                        AddPiecePos(target);
                    }
                }
            }
        }
    }

    /// <summary>
    /// movablePositionsに移動可能なマス目を追加する
    /// </summary>
    private bool AddPiecePos(Vector2Int pos)
    {
        const int boardMin = 1;
        const int boardMax = 9;
        
        if (pos.x < boardMin || pos.x > boardMax || pos.y < boardMin || pos.y > boardMax) return false;

        // すでに駒があるかチェック
        PieceType checkPiece = ShogiManager.Instance.GetPieceTypeAt(pos);
        Piece checkPieceObj = ShogiManager.Instance.GetPieceAt(pos);

        // 空マス or 相手の駒なら移動可能
        if (checkPiece == PieceType.None)
        {
            MovablePositions.Add(pos);
            return true;
        }
        else if (checkPieceObj != null && checkPieceObj.PieceTurn != PieceTurn)
        {
            if (checkPiece == PieceType.玉将)
            {
                // TODO: 王手の処理
            }
            MovablePositions.Add(pos);
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
    private async UniTask CheckPromotion()
    {
        PieceData pieceData = ShogiManager.Instance.pieceDatabase.GetPieceData(BasePieceType);
        if (pieceData.promotionType != PromotionType.None && !isPromoted)
        {
            switch (pieceData.pieceType)
            {
                case PieceType.歩兵:
                case PieceType.香車:
                    if (PieceTurn == Turn.先手 && currentPos.y <= 1 ||
                        PieceTurn == Turn.後手 && currentPos.y >= 9)
                    {
                        ShogiManager.Instance.PromotePiece(currentPos, pieceData);
                        return;
                    }
                    break;
                case PieceType.桂馬:
                    if (PieceTurn == Turn.先手 && currentPos.y <= 2 ||
                        PieceTurn == Turn.後手 && currentPos.y >= 8)
                    {
                        ShogiManager.Instance.PromotePiece(currentPos, pieceData);
                        return;
                    }
                    break;
            }
            
            // 現在、敵陣にいるかどうか
            bool nowInEnemyCamp =
                PieceTurn == Turn.先手 && currentPos.y <= 3 ||
                PieceTurn == Turn.後手 && currentPos.y >= 7;

            // 前のターン、敵陣にいたかどうか
            bool leftEnemyCampThisTurn = _wasInEnemyCamp && !nowInEnemyCamp;

            // 成駒の条件を満たしている場合
            if (nowInEnemyCamp || leftEnemyCampThisTurn)
            {
                // 成るかどうかのUIを表示
                bool isPromote = await PromotionUIManager.Instance.ShowAsync(currentPos, _unpromSprite, _promSprite);
                if (isPromote)
                {
                    // 成る処理
                    ShogiManager.Instance.PromotePiece(currentPos, pieceData);
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