using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class ShogiManager : MonoBehaviour
{
    // シングルトン管理
    public static ShogiManager instance { get; private set; }
    
    public Turn activePlayer { get; private set; }                  // 現在の手番（先手 or 後手）
    private PieceType[,] _boardState = new PieceType[9, 9];         // 盤面の状態を管理
    private Dictionary<Vector2Int, Piece> _pieceObjects = new ();   // 盤面上の駒オブジェクト
    
    public GameObject curSelPiece; // 現在選択されている駒
    
    // 持ち駒の状態を管理
    public int[] senteCapturedPieceType { get; private set; } = new int[7];   // 先手の持ち駒の種類ごとの数
    public int[] goteCapturedPieceType { get; private set; } = new int[7];    // 後手の持ち駒の種類ごとの数

    // 二歩チェック用の歩の列情報
    public bool[] senteFuPosition { get; private set; } = new bool[9]; // 先手の歩の列状態
    public bool[] goteFuPosition { get; private set; } = new bool[9];  // 後手の歩の列状態
    
    private int _recMoveCount; // 手数のカウント

    private Dictionary<Turn, Piece> _kingObj = new();   // 玉の位置を管理
    private Dictionary<Turn, Dictionary<Vector2Int, List<Piece>>> _allMovesCache = new(); // 全ての駒の移動可能範囲
    
    /*
    public static bool CanSelect; // 選択状況を管理するフラグ

    [SerializeField] ShogiEngineManager shogiEngMan; // エンジン管理*/
    
    [Header("持ち駒の管理")]
    [SerializeField] private BoardInitializer boardInit; // 持ち駒
    [SerializeField] public MoveHighlight moveHighlight; // 駒の移動可能範囲ハイライト
    
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
        
        _allMovesCache[Turn.先手] = new Dictionary<Vector2Int, List<Piece>>();
        _allMovesCache[Turn.後手] = new Dictionary<Vector2Int, List<Piece>>();
    }

    private void Start()
    {
        SetGame();
    }
    
    /// <summary>
    /// ゲームの開始
    /// </summary>
    private void SetGame()
    {
        boardInit.DefaultPosition();
        boardInit.CreateCapturePieces();
        
        CapturePieceUIManager.instance.Initialize();
        
        KingRegister();
        EndTurnPhase(null);
    }
    
    /// <summary>
    /// 玉の位置を取得
    /// </summary>
    private void KingRegister()
    {
        _kingObj.Clear();
        foreach (var kvp in _pieceObjects)
        {
            Piece kingPiece = kvp.Value;
            if (kingPiece.basePieceType == PieceType.玉将)
            {
                _kingObj[kingPiece.pieceTurn] = kingPiece;
            }
        }
    }
    
    /// <summary>
    /// 局面の移動フェーズを終了し、次の手番に移行
    /// </summary>
    public void EndTurnPhase(Vector2Int? toPos)
    {
        // 二歩のチェック
        CheckTwoFu();
        
        // 局面の保存
        AddKifuEntry();
        
        // 詰み状態ではないかのチェック
        if (IsCheckmate())
        {
            Debug.Log("決着がつきました。");
            return;
        }
        
        _recMoveCount++;
        
        // 手番を切り替える
        if (_recMoveCount >= 2)
        {
            activePlayer = (activePlayer == Turn.先手) ? Turn.後手 : Turn.先手;
            
            CancelSelection();
            if (toPos.HasValue)
            {
                moveHighlight.SetLastMoveHighlight(toPos.Value);
            }
        }
    }
    
    /// <summary>
    /// 選択の解除
    /// </summary>
    public async void CancelSelection()
    {
        await UniTask.Yield();
        
        curSelPiece = null;
        moveHighlight.RemoveHighlight();
    }

    /// <summary>
    /// 現在の局面を記譜法に追加
    /// </summary>
    private void AddKifuEntry()
    {
        
    }

    /// <summary>
    /// 歩の座標検知をし二歩のチェック
    /// </summary>
    private void CheckTwoFu()
    {
        for (int x = 0; x < 9; x++)
        {
            // 先手と後手の歩の列を初期化
            senteFuPosition[x] = false; // 先手の歩の列を初期化
            goteFuPosition[x] = false;  // 後手の歩の列を初期化
            
            // その列に歩があるかチェック
            for (int y = 0; y < 9; y++)
            {
                PieceType pieceType = _boardState[x, y];
                if (pieceType != PieceType.歩兵) continue;
                
                Piece fuObj = GetPieceAt(new Vector2Int(x + 1, y + 1));
                bool[] fuPosition = (fuObj.pieceTurn == Turn.先手) ? senteFuPosition : goteFuPosition;
                    
                fuPosition[x] = true;
            }
        }
    }
    
    /// <summary>
    /// 詰みの状態をチェック
    /// </summary>
    // TODO: 玉将の移動可能範囲を取得し、相手の駒の移動範囲と照合して詰みかどうかを判定
    private bool IsCheckmate()
    {
        // 全ての駒の移動可能範囲を更新
        UpdateMovePos();
        
        // 王手されているかチェック
        Turn? enemySideTurn = IsCheckOute();
        if (enemySideTurn == null) return false; // 王手されていない場合
        
        // 玉が逃げられるか
        GetAllMoves(); // 全駒の移動マスを最新化してキャッシュ
        if (CanKingEscape(enemySideTurn.Value)) return false; // 玉が逃げられる場合
        
        // 合駒で防げるか
        
        return true;
    }

    /// <summary>
    /// 全ての駒の移動可能範囲を更新
    /// </summary>
    private void UpdateMovePos()
    {
        _allMovesCache[Turn.先手].Clear();
        _allMovesCache[Turn.後手].Clear();
        
        foreach (Piece piece in _pieceObjects.Values)
        {
            piece.GetMovePoints();
        }
    }

    /// <summary>
    /// 王手されているかチェック
    /// </summary>
    private Turn? IsCheckOute()
    {
        // 全ての駒の移動可能範囲を取得
        foreach (var kvp in _pieceObjects)
        {
            Piece piece = kvp.Value;
            
            // 駒の移動可能範囲を取得
            List<Vector2Int> moves = piece.movablePositions;
            
            // 相手の玉の位置を取得
            Turn enemySideTurn = (piece.pieceTurn == Turn.先手) ? Turn.後手 : Turn.先手;
            Piece kingPiece = _kingObj[enemySideTurn];
            
            // 玉の位置が移動可能範囲に含まれているかチェック
            if (moves.Contains(kingPiece.currentPos))
            {
                Debug.Log(kingPiece.transform.name + "の玉が王手されています。");
                return enemySideTurn;
            }
        }

        return null;
    }

    /// <summary>
    /// 指定した手番の全ての駒の移動可能範囲を取得
    /// </summary>
    private void GetAllMoves()
    {
        foreach (var kvp in _pieceObjects)
        {
            Piece piece = kvp.Value;
            Turn turn = piece.pieceTurn;
            
            foreach (Vector2Int move in piece.movablePositions)
            {
                if (!_allMovesCache[turn].ContainsKey(move))
                {
                    _allMovesCache[turn][move] = new List<Piece>();
                }
                _allMovesCache[turn][move].Add(piece);
            }
        }
    }
    
    /// <summary>
    /// 玉が逃げられるかチェック
    /// </summary>
    /// <param name="turn">王手されている玉のターン</param>
    private bool CanKingEscape(Turn turn)
    {
        Piece kingPiece = _kingObj[turn];
        bool isEscape = false;
        
        List<Vector2Int> kingMoves = new List<Vector2Int>(kingPiece.movablePositions);
        foreach (Vector2Int move in kingMoves)
        {
            Debug.Log(move + "王の移動場所");
            Turn enemyTurn = (turn == Turn.先手) ? Turn.後手 : Turn.先手;
            if (!_allMovesCache[enemyTurn].ContainsKey(move))
            {
                if (_pieceObjects.ContainsKey(move))
                {
                    Debug.Log(move);
                    if (!CanGetOutePiece(turn, move)) continue;
                }
                isEscape = true;
            }
        }

        if (isEscape) return true;
        
        return false; // すべての移動先が敵の攻撃範囲
    }

    /// <summary>
    /// 王手駒を取る手段があるか
    /// </summary>
    private bool CanGetOutePiece(Turn turn, Vector2Int move)
    {
        if (_allMovesCache[turn].ContainsKey(move))
        {
            Debug.Log(move + "を取れる駒をチェックします");
            foreach (Piece piece in _allMovesCache[turn][move])
            {
                // 取れる駒が玉なら
                if (piece == _kingObj[turn])
                {
                    if (!IsKingSafeAfterMove(turn, move))
                    {
                        Debug.Log("玉で取れません");
                        continue;
                    }
                }
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// 指定した手番の駒を動かした後に玉が安全かチェック
    /// </summary>
    private bool IsKingSafeAfterMove(Turn turn, Vector2Int move)
    {
        Debug.Log("玉で取れるかチェックします");
        // 現在の盤面状態を保存
        PieceType[,] backupBoard = (PieceType[,])_boardState.Clone();
        Dictionary<Vector2Int, Piece> backupPieces = new (_pieceObjects);
        Debug.Log("盤面状態を保存しました");
        
        Piece king = _kingObj[turn];
        Vector2Int originalPos = king.currentPos;
        
        // 仮想環境で駒を動かす
        RemovePiece(originalPos);
        PlacePiece(move, king.basePieceType, king);
        king.SetPosition(move);
        
        UpdateMovePos();
        GetAllMoves();
        
        Turn enemyTurn = (turn == Turn.先手) ? Turn.後手 : Turn.先手;
        bool isSafe = !_allMovesCache[enemyTurn].ContainsKey(move);
        
        // 盤面状態を元に戻す
        RemovePiece(move);
        PlacePiece(originalPos, king.basePieceType, king);
        king.SetPosition(originalPos);
        _boardState = backupBoard;
        _pieceObjects = backupPieces;
        
        UpdateMovePos();
        GetAllMoves();
        Debug.Log("盤面状態を復元しました");
        
        return isSafe;
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
            PieceType basePieceType = enemyPiece.basePieceType;
            // 駒の種類に応じてカウントを増やす
            pieceCount[(int)basePieceType]++;
            
            CapturePieceUIManager.instance.ApplyVisualUI(basePieceType, activePlayer);
            
            // 駒オブジェクトを削除
            Destroy(enemyPiece.gameObject);
            //Debug.Log(pieceType + "を捕獲しました。");
        }
        // 盤面から駒を削除
        RemovePiece(pos);
    }

    /// <summary>
    /// 持ち駒を指す
    /// </summary>
    public void SetCapturedPiece(PieceType pieceType, Vector2Int pos)
    {
        int[] capturedPieceType = (activePlayer == Turn.先手) ? 
            senteCapturedPieceType : 
            goteCapturedPieceType;
        
        // 持ち駒の数を指す
        boardInit.CreatePiece(pieceType, pos, activePlayer, false);
        capturedPieceType[(int)pieceType]--;
        
        Debug.Log(pieceType + "を指しました。");
        // 持ち駒のUIを更新
        CapturePieceUIManager.instance.ApplyVisualUI(pieceType, activePlayer);
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
    }*/
}
