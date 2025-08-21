using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class ShogiManager : MonoBehaviour
{
    // シングルトン管理
    public static ShogiManager instance { get; private set; }
    
    [Header("ゲーム進行・状態管理")]
    public Turn activePlayer; // 現在の手番（先手 or 後手）
    
    private PieceType[,] _boardState = new PieceType[9, 9]; // 盤面の状態を管理
    private Dictionary<Vector2Int, Piece> _pieceObjects = new (); // 盤面上の駒オブジェクトを管理（座標 -> 駒オブジェクト）
    
    public GameObject curSelPiece; // 現在選択されている駒
    
    // 持ち駒の状態を管理
    public int[] senteCapturedPieceType = new int[7];   // 先手の持ち駒の種類ごとの数
    public int[] goteCapturedPieceType = new int[7];    // 後手の持ち駒の種類ごとの数

    /*// 二歩チェック用の歩の列情報
    public bool[] SenteFuPosition = new bool[9]; // 先手の歩の列状態
    public bool[] GoteFuPosition = new bool[9];  // 後手の歩の列状態*/
    
    private int _recMoveCount = 0; // 手数のカウント
    
    /*// ハイライトの管理
    [SerializeField] GameObject highlightPrefab; // 駒のハイライト用プレハブ
    List<GameObject> _activeHighlights = new();
    SpriteRenderer _sr;
    
    public static bool CanSelect; // 選択状況を管理するフラグ

    private bool? _playerChoice;
    private Camera _camera;

    Piece _piece;
    [SerializeField] HeldPieceManager heldPieceManager; // 持ち駒管理
    [SerializeField] ShogiEngineManager shogiEngMan; // エンジン管理

    bool _isFastPromote; // 成駒の選択がされているか*/
    
    [Header("駒のデータベース")]
    [SerializeField] public PieceDatabase pieceDatabase;
    [SerializeField] public PromotionDatabase promotionDatabase;
    
    
    private void Awake()
    {
        // シングルトンの初期化
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        // 盤面の初期化
        for (int x = 0; x < 9; x++)
        {
            for (int y = 0; y < 9; y++)
            {
                _boardState[x, y] = PieceType.None;
            }
        }
    }
    
    public void CancelSelection()
    {
        curSelPiece = null;
        //ClearHighlights();
    }
    
    /// <summary>
    /// 局面の移動フェーズを終了し、次の手番に移行
    /// </summary>
    public void EndTurnPhase()
    {
        // 局面の保存
        AddKifuEntry();
        
        // 手番を切り替える
        curSelPiece = null;
        activePlayer = (activePlayer == Turn.先手) ? Turn.後手 : Turn.先手;
    }

    /// <summary>
    /// 現在の局面を記譜法に追加
    /// </summary>
    private void AddKifuEntry()
    {
        _recMoveCount++;
    }
    
    /// <summary>
    /// 駒を指定した位置に移動する
    /// </summary>
    public void MovePiece(Vector2Int from, Vector2Int to)
    {
        Piece movingPiece = _pieceObjects[from];
        PieceType type = _boardState[from.x - 1, from.y - 1];

        RemovePiece(from); // 元の場所を空に
        PlacePiece(to, type, movingPiece); // 新しい場所に設置
        movingPiece.SetPosition(to); // Piece側の位置も更新
    }
    
    /// <summary>
    /// 成駒に状態変化する際の処理
    /// </summary>
    public void PromotePiece(Vector2Int pos, PieceData pieceData)
    {
        Piece piece = GetPieceAt(pos);
        if (piece == null) return; // 駒が存在しない場合は何もしない

        // 成り駒の状態を設定
        piece.PromotePiece(pieceData);
        // 盤面の状態を更新
        _boardState[pos.x - 1, pos.y - 1] = pieceData.promotedType;
    }

    /// <summary>
    /// 駒を取得した際のゲーム状況の更新
    /// </summary>
    public void AddCapturedPiece(Vector2Int pos)
    {
        Piece enemyPiece = GetPieceAt(pos);
        int[] pieceCount = (activePlayer == Turn.先手) ? senteCapturedPieceType : goteCapturedPieceType;
        // 駒の種類を取得
        PieceType pieceType = GetPieceTypeAt(pos);
        if (pieceType != PieceType.None)
        {
            // 駒の種類に応じてカウントを増やす
            pieceCount[(int)enemyPiece.basePieceType]++;
            
            // 駒オブジェクトを削除
            Destroy(enemyPiece.gameObject);
            Debug.Log(pieceType + "を捕獲しました。");
        }
        // 盤面から駒を削除
        RemovePiece(pos);
    }

    public void SetCapturedPiece(PieceType pieceType, Vector2Int pos)
    {
        
    }

    /// <summary>
    /// 盤面に駒を配置する
    /// </summary>
    public void PlacePiece(Vector2Int pos, PieceType type, Piece pieceObj)
    {
        _boardState[pos.x - 1, pos.y - 1] = type;
        _pieceObjects[pos] = pieceObj;
    }

    /// <summary>
    /// 盤面から駒を削除する
    /// </summary>
    public void RemovePiece(Vector2Int pos)
    {
        _boardState[pos.x - 1, pos.y - 1] = PieceType.None;
        _pieceObjects.Remove(pos);
    }
    
    /// <summary>
    /// 指定した位置にある駒を取得する
    /// </summary>
    public Piece GetPieceAt(Vector2Int pos)
    {
        return _pieceObjects.ContainsKey(pos) ? _pieceObjects[pos] : null;
    }

    /// <summary>
    /// 指定した位置にある駒の種類を取得する
    /// </summary>
    public PieceType GetPieceTypeAt(Vector2Int pos)
    {
        return _boardState[pos.x - 1, pos.y - 1];
    }
    
    /*void Start()
    {
        buttons.SetActive(false);
        ActivePlayer = true;
        CanSelect = false; // 初期状態では選択可能
        _isFastPromote = false;
        
        trueButton.onClick.AddListener(() => Choose(true));
        falseButton.onClick.AddListener(() => Choose(false));

        shogiEngMan.SetStartPosition();
    }
    
    //----------------------------------
    //-----------AI専用処理--------------
    //----------------------------------
    
    // エンジンからの移動情報を受信する
    public async void ReceiveEngineMove(string moveString)
    {
        if (moveString[1].ToString() == "*") // 持ち駒の場合の処理
        {
            await UniTask.SwitchToMainThread();
            DropMove(moveString);
        }
        else // 通常の移動の場合
        {
            var moveData =  ParseMoveString(moveString);

            if (moveData != null)
            {
                var data = moveData.Value;
                await ExecuteEngineMoveAsync(data.startIndex, data.endIndex, data.toX, data.toY);
            }
        }
    }
    
    private async UniTask ExecuteEngineMoveAsync(int fromX, int fromY, int toX, int toY)
    {
        // メインスレッドに切り替え
        await UniTask.SwitchToMainThread();
        ExecuteEngineMove(fromX, fromY, toX, toY);
    }
    
    void ExecuteEngineMove(int fromX, int fromY, int toX, int toY)
    {
        // AI手番チェック
        if (ActivePlayer)
        {
            return;
        }
    
        // 駒を探す
        LayerMask pieceLayer = LayerMask.GetMask("Piece");
        Vector2 fromPosition = new Vector2(fromX, fromY);
        Collider2D fromPieceCollider = Physics2D.OverlapPoint(fromPosition, pieceLayer);

        if (fromPieceCollider != null)
        {
            Piece movingPiece = fromPieceCollider.GetComponent<Piece>();
            if (movingPiece != null)
            {
                string expectedTag = "Gote";
                string actualTag = fromPieceCollider.gameObject.tag;
            
                if (actualTag != expectedTag)
                {
                    Debug.LogError($"❌ Wrong piece! AI trying to move {actualTag} piece, but should move {expectedTag}");
                    return;
                }
                
                Vector2 toPosition = new Vector2(toX, toY);
                movingPiece.ExecuteAIMove(toPosition, _isFastPromote);
                
                // ✅ AIの手を記譜法に変換して履歴に追加
                string aiMoveNotation = ConvertToShogiNotation(fromPosition, toPosition);
                
                ShogiEngineManager engineManager = FindObjectOfType<ShogiEngineManager>();
                if (engineManager != null)
                {
                    engineManager.AddMoveToHistory(aiMoveNotation);
                }

                _isFastPromote = false;
                ActivePlayer = !ActivePlayer;
            }
        }
        else
        {
            Debug.LogError($"❌ 駒がない ({fromX},{fromY})");
        }
    }
    
    //---------駒形式の変換------------
    // aiの移動形式の変換
    (int startIndex, int endIndex, int toX, int toY)? ParseMoveString(string moveString)
    {
        //　文字列チェック
        if (moveString.Length < 4)
        {
            Debug.LogWarning($"フォーマットが違います: {moveString}");
            return null;
        }
        // 成駒のチェック
        if (moveString.Length == 5 && moveString[4].ToString() == "+")
        {
            _isFastPromote = true;
        }
        
        // 駒の種類を取得
        int shogiFromX = int.Parse(moveString[0].ToString());
        char fromYChar = moveString[1];
        int shogiToX = int.Parse(moveString[2].ToString());
        char toYChar = moveString[3];
        
        // Debug.Log(moveString);
        
        // 文字を数字に変換
        int fromY = fromYChar - 'a' + 1;
        int toY = toYChar - 'a' + 1;
        return (shogiFromX, fromY, shogiToX, toY);
    }

    void DropMove(string moveString)
    {
        // 持ち駒の処理
        if (moveString.Length < 4)
        {
            Debug.LogWarning($"フォーマットが違います: {moveString}");
            return;
        }
        
        char pieceChar = moveString[0]; // 駒の種類を取得
        int toX = int.Parse(moveString[2].ToString());
        char toYChar = moveString[3];
        int toY = toYChar - 'a' + 1;

        Piece.PieceId pieceType = pieceChar switch
        {
            'P' => Piece.PieceId.Hu,    // 歩兵
            'N' => Piece.PieceId.Keima, // 桂馬
            'S' => Piece.PieceId.Gin,   // 銀将
            'G' => Piece.PieceId.Kin,   // 金将
            'K' => Piece.PieceId.Gyoku, // 玉将
            'L' => Piece.PieceId.Kyosha, // 香車
            'R' => Piece.PieceId.Hisha,  // 飛車
            'B' => Piece.PieceId.Kaku,   // 角
            _ => throw new ArgumentException("不明な持ち駒: " + pieceChar)
        };
          
        heldPieceManager.RemoveHeldPiece(pieceType);
        if (HeldPieceManager.FoundPiece != null && HeldPieceManager.IsHeldPieceSelected)
        {
            HeldPieceManager.FoundPiece.transform.position = new Vector2(toX, toY);
            HeldPieceManager.FoundPiece.SetActive(true);

            Piece pieceScript = HeldPieceManager.FoundPiece.GetComponent<Piece>();
            pieceScript.ApplyStatePiece(pieceType);

            // 持ち駒リストから削除 & 個数を減らす
            bool capturerIsSente = HeldPieceManager.FoundPiece.CompareTag("Sente");
            int pieceTypeIndex = (int)pieceScript.pieceType;
            if (capturerIsSente)
            {
                heldPieceManager.senteInactivePieces.Remove(HeldPieceManager.FoundPiece);
                heldPieceManager.senteHeldPieceType[pieceTypeIndex]--;
            }
            else
            {
                heldPieceManager.goteInactivePieces.Remove(HeldPieceManager.FoundPiece);
                heldPieceManager.goteHeldPieceType[pieceTypeIndex]--;
            }
            heldPieceManager.OnHeldPieceChanged?.Invoke();

            HeldPieceManager.IsHeldPieceSelected = false;
            HeldPieceManager.FoundPiece = null;
            ActivePlayer = !ActivePlayer;

            ShogiEngineManager engineManager = FindObjectOfType<ShogiEngineManager>();
            if (engineManager != null)
            {
                engineManager.AddMoveToHistory(moveString);
            }
        }
    }
    
    // aiの移動形式の変換
    public string ConvertToShogiNotation(Vector2 fromPos, Vector2 toPos)
    {
        char fromYChar = (char)('a' + (int)fromPos.y - 1);
        char toYChar = (char)('a' + (int)toPos.y - 1);
    
        string notation = $"{fromPos.x}{fromYChar}{toPos.x}{toYChar}";

        if (_isFastPromote)
        {
            notation += "+";
        }
    
        return notation;
    }
    
    //----------------------------------
    //---------ハイライトの管理------------
    //----------------------------------
    
    // ハイライトの生成
    void CreateHighlightSquare(Vector2 position)
    {
        GameObject highlight = Instantiate(highlightPrefab, position, Quaternion.identity);
        highlight.tag = "Highlight";
        highlight.layer = LayerMask.NameToLayer("Default");
        highlight.GetComponent<SpriteRenderer>().sortingOrder = 1;
        
        _sr = highlight.GetComponent<SpriteRenderer>();
        _sr.color = new Color(1f, 1f, 1f, 0.6f);
        
        _activeHighlights.Add(highlight);
        highlight.name = $"{position.x}.{position.y}";
        
        highlight.transform.SetParent(this.transform, false);
    }

    public void CreateMoveHighlightSquares(List<Vector2> canMovePositions, Vector2 position)
    {
        for (int x = 1; x <= 9; x++)
        {
            for (int y = 1; y <= 9; y++)
            {
                Vector2 highlightPosition = new Vector2(x, y);
                GameObject nowCheckedPiece = 
                    Physics2D.OverlapPoint(highlightPosition, LayerMask.GetMask("Piece"))?.gameObject;
                
                if (!canMovePositions.Contains(highlightPosition) && highlightPosition != position)
                {
                    CreateHighlightSquare(highlightPosition);
                }
                else if (canMovePositions.Contains(highlightPosition) && nowCheckedPiece != null)
                {
                    string currentTurnTag = ActivePlayer ? "Sente" : "Gote";
                    
                    if (nowCheckedPiece.CompareTag(currentTurnTag))
                    {
                        CreateHighlightSquare(highlightPosition);
                    }
                }
            }
        }
    }

    public void CreateDropHighlightSquares(Piece.PieceId pieceType)
    {
        for (int x = 1; x <= 9; x++)
        {
            if (pieceType == Piece.PieceId.Hu)
            {
                bool fuPositionCheck = ActivePlayer ? SenteFuPosition[x - 1] : GoteFuPosition[x - 1];
                if (fuPositionCheck)
                {
                    // その列（x座標）の全マスを設置不可としてハイライト
                    for (int fy = 1; fy <= 9; fy++)
                    {
                        Vector2 invalidPosition = new Vector2(x, fy);
                        CreateHighlightSquare(invalidPosition);
                    }
                    continue;
                }
            }
            for (int y = 1; y <= 9; y++)
            {
                Vector2 highlightPosition = new Vector2(x, y);
                GameObject nowCheckedPiece = 
                    Physics2D.OverlapPoint(highlightPosition, LayerMask.GetMask("Piece"))?.gameObject;

                switch (pieceType)
                {
                    case Piece.PieceId.Hu:
                    case Piece.PieceId.Kyosha:
                    case Piece.PieceId.Keima:
                        if (!IsValidDropPosition(pieceType, highlightPosition))
                        {
                            CreateHighlightSquare(highlightPosition);
                            continue;
                        }
                        break;
                }

                // 盤上に駒がない場合はハイライトを生成
                if (nowCheckedPiece != null)
                {
                    CreateHighlightSquare(highlightPosition);
                }
            }
        }
    }

    // 強制成りの場合
    bool IsValidDropPosition(Piece.PieceId pieceType, Vector2 position)
    {
        int y = (int)position.y;
    
        switch (pieceType)
        {
            case Piece.PieceId.Hu:    // 歩兵
            case Piece.PieceId.Kyosha: // 香車
                return ActivePlayer ? y > 1 : y < 9;
            
            case Piece.PieceId.Keima:  // 桂馬
                return ActivePlayer ? y > 2 : y < 8;
            
            default:
                return true;
        }
    }

    // 駒の選択���クリア
    public void ClearHighlights()
    {
        foreach (GameObject highlight in _activeHighlights)
        {
            if (highlight != null) Destroy(highlight);
        }
        _activeHighlights.Clear();
    }*/
}
