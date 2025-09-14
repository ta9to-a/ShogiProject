using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class ShogiManager : MonoBehaviour
{
    // シングルトン管理
    public static ShogiManager Instance { get; private set; }
    
    public Turn ActivePlayer { get; private set; }                  // 現在の手番（先手 or 後手）
    private PieceType[,] _boardState = new PieceType[9, 9];         // 盤面の状態を管理
    private Dictionary<Vector2Int, Piece> _pieceObjects = new ();   // 盤面上の駒オブジェクト
    public Dictionary<Turn, List<CapturePieceParent>> CapturePieceObjects = new ();         // 持ち駒のオブジェクト
    
    public GameObject curSelPiece;  // 現在選択されている駒
    public bool CanPieceSelect { get; private set; }
    
    // 持ち駒の状態を管理
    public int[] SenteCapturedPieceType { get; private set; } = new int[7];   // 先手の持ち駒の種類ごとの数
    public int[] GoteCapturedPieceType { get; private set; } = new int[7];    // 後手の持ち駒の種類ごとの数

    // 二歩チェック用の歩の列情報
    public bool[] SenteFuPosition { get; private set; } = new bool[9]; // 先手の歩の列状態
    public bool[] GoteFuPosition { get; private set; } = new bool[9];  // 後手の歩の列状態
    
    public int RecMoveCount { get; private set; } // 手数のカウント

    private Dictionary<Turn, Piece> _kingObj = new();   // 玉のオブジェクトを管理
    private Dictionary<Turn, Dictionary<Vector2Int, List<Piece>>> _allMovesCache = new(); // 全ての駒の移動可能範囲
    
    private UsiMoveData _data;
    
    [Header("持ち駒の管理")]
    [SerializeField] private BoardInitializer boardInit;    // 持ち駒
    [SerializeField] public MoveHighlight moveHighlight;    // 駒の移動可能範囲ハイライト
    [SerializeField] private UsiEngineConnector usiEngine;  // USIエンジン接続
    
    [Header("駒のデータベース")]
    [SerializeField] public PieceDatabase pieceDatabase;
    [SerializeField] public PromotionDatabase promotionDatabase;

    private void Awake()
    {
        // シングルトンの初期化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        CanPieceSelect = false;
    }
    
    /// <summary>
    /// ゲームの開始
    /// </summary>
    public async void SetGame()
    {
        // 盤面の初期化
        InitializeBoard();

        // 駒の初期配置
        switch (GameModeManager.Instance.CurrentGameMode)
        {
            case GameModeManager.GameMode.PlayerVsPlayer:
                boardInit.DefaultPosition();
                break;
            case GameModeManager.GameMode.PlayerVsAI:
                await usiEngine.StartEngin();
                boardInit.DefaultPosition();
                usiEngine.SetStartPosition();
                break;
            /*case GameMode.Mode.詰将棋:
                boardInit.SetTsumeShogiPosition();
                break;*/
        }
        
        // 玉の登録
        KingRegister();
        // 歩の列情報を設定
        CheckTwoFu();
        // 全ての駒の移動可能範囲を更新
        UpdateMovePosFresh();
        
        CapturePieceUIManager.Instance.Initialize();
        CanPieceSelect = true;
    }
    
    /// <summary>
    /// 盤面の初期化
    /// </summary>
    private void InitializeBoard()
    {
        ActivePlayer = Turn.先手;
        RecMoveCount = 1;
        
        _pieceObjects.Clear();
        for (int x = 0; x < 9; x++)
        {
            for (int y = 0; y < 9; y++)
            {
                _boardState[x, y] = PieceType.None;
            }
        }
        Array.Clear(SenteCapturedPieceType, 0, SenteCapturedPieceType.Length);
        Array.Clear(GoteCapturedPieceType, 0, GoteCapturedPieceType.Length);
        Array.Clear(SenteFuPosition, 0, SenteFuPosition.Length);
        Array.Clear(GoteFuPosition, 0, GoteFuPosition.Length);
        
        curSelPiece = null;
        moveHighlight.RemoveHighlight();
        
        _kingObj.Clear();
        
        _allMovesCache[Turn.先手] = new Dictionary<Vector2Int, List<Piece>>();
        _allMovesCache[Turn.後手] = new Dictionary<Vector2Int, List<Piece>>();
        
        CapturePieceObjects[Turn.先手] = new List<CapturePieceParent>();
        CapturePieceObjects[Turn.後手] = new List<CapturePieceParent>();
        
        boardInit.CreateCapturePieces();
    }
    
    /// <summary>
    /// 玉のオブジェクトを登録
    /// </summary>
    private void KingRegister()
    {
        _kingObj.Clear();
        foreach (var kvp in _pieceObjects)
        {
            Piece kingPiece = kvp.Value;
            if (kingPiece.BasePieceType == PieceType.玉将)
            {
                _kingObj[kingPiece.PieceTurn] = kingPiece;
            }
        }
    }
    
    /// <summary>
    /// 局面の移動フェーズを終了し、次の手番に移行
    /// </summary>
    public void EndTurnPhase(Vector2Int toPos)
    {
        DebugLastTurn(toPos);
        
        // 盤面の記録と手数を更新
        AddKifuEntry();
        RecMoveCount++;
        
        // 二歩の更新
        CheckTwoFu();
        
        // 手番交代と選択の解除
        ActivePlayer = (ActivePlayer == Turn.先手) ? Turn.後手 : Turn.先手;
        
        // 詰み状態ではないかのチェック
        if (IsCheckmate(ActivePlayer))
        {
            Debug.Log("詰み");
            moveHighlight.RemoveCanMovePosHighlight();
            return;
        }

        CancelSelection();
        moveHighlight.SetLastMoveHighlight(toPos);
        
        // AIのターンの場合、エンジンに指し手を要求
        if (GameModeManager.Instance.CurrentGameMode == GameModeManager.GameMode.PlayerVsAI)
        {
            if (ActivePlayer == Turn.後手)
            {
                CanPieceSelect = false;
                SendMoveToEngine();
            }
            else
            {
                CanPieceSelect = true;
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
            SenteFuPosition[x] = false; // 先手の歩の列を初期化
            GoteFuPosition[x] = false;  // 後手の歩の列を初期化
            
            // その列に歩があるかチェック
            for (int y = 0; y < 9; y++)
            {
                PieceType pieceType = _boardState[x, y];
                if (pieceType != PieceType.歩兵) continue;
                
                Piece fuObj = GetPieceAt(new Vector2Int(x + 1, y + 1));
                bool[] fuPosition = (fuObj.PieceTurn == Turn.先手) ? SenteFuPosition : GoteFuPosition;
                    
                fuPosition[x] = true;
            }
        }
    }
    
    /// <summary>
    /// 詰みの状態をチェック
    /// </summary>
    private bool IsCheckmate(Turn defenderTurn)
    {
        // 全ての駒の移動可能範囲を更新
        UpdateMovePosFresh();
        
        // 王手されているかチェック
        List<Piece> attackers = CollectAttackers(defenderTurn);
        if (attackers.Count == 0) return false; // 王手されていない場合
        
        // 二重王手の場合
        if (attackers.Count > 1)
        {
            // 玉が逃げられるかチェック
            if (CanKingEscape(defenderTurn)) return false;
            
            return true;
        }
        
        Piece attacker = attackers[0];
        // 玉が逃げられるかチェック
        if (CanKingEscape(defenderTurn)) return false; // 玉が逃げられる場合
        
        // 王手駒を取る手段があるかチェック
        if (CanCaptureAttacker(defenderTurn, attacker)) return false;
        
        // 直線移動する駒の王手を遮断できるかチェック
        if (CanAvoidCheck(defenderTurn, attacker)) return false;
        
        return true;
    }

    /// <summary>
    /// 全ての駒の移動可能範囲を更新
    /// </summary>
    private void UpdateMovePosFresh()
    {
        _allMovesCache[Turn.先手].Clear();
        _allMovesCache[Turn.後手].Clear();
        
        // 全ての駒の移動可能範囲を更新
        foreach (Piece piece in _pieceObjects.Values)
        {
            piece.GetMovePoints();
        }
        // 全ての持ち駒の指せる位置を更新
        foreach (var captureList in CapturePieceObjects.Values)
        {
            foreach (var capturePiece in captureList)
            {
                capturePiece.CheckDroppablePositions();
            }
        }

        GetAllMoves();
    }
    
    /// <summary>
    /// 全ての駒の移動可能範囲を取得
    /// </summary>
    private void GetAllMoves()
    {
        foreach (var kvp in _pieceObjects)
        {
            Piece piece = kvp.Value;
            Turn turn = piece.PieceTurn;
            
            foreach (Vector2Int move in piece.MovablePositions)
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
    /// 王手されているかチェック
    /// </summary>
    /// <returns>王手している駒</returns>
    private List<Piece> CollectAttackers(Turn defender)
    {
        List<Piece> outePieces = new List<Piece>();
        Piece king = _kingObj[defender];
        Turn attackerSide = (defender == Turn.先手) ? Turn.後手 : Turn.先手;

        if (_allMovesCache[attackerSide].TryGetValue(king.currentPos, out List<Piece> attackers))
            outePieces.AddRange(attackers);
        
        return outePieces;
    }
    
    /// <summary>
    /// 玉が逃げられるかチェック
    /// </summary>
    /// <param name="defender">王手されている玉のターン</param>
    private bool CanKingEscape(Turn defender)
    {
        Piece kingPiece = _kingObj[defender];
        
        // 玉の移動可能範囲をチェック
        
        List<Vector2Int> originalPositions = new List<Vector2Int>(kingPiece.MovablePositions);
        foreach (Vector2Int to in originalPositions)
        {
            // 移動先が敵の攻撃範囲に含まれていないかチェック
            if (!IsKingSafeAfterMove(defender, kingPiece, to)) continue;
            return true;
        }
        
        return false; // 逃げられない場合
    }

    /// <summary>
    /// 王手駒を取る手段があるか
    /// </summary>
    private bool CanCaptureAttacker(Turn defender, Piece attacker)
    {
        if (_allMovesCache[defender].TryGetValue(attacker.currentPos, out List<Piece> candidates))
        {
            // 駒を取れる駒があるかチェック
            List<Piece> snapCandidates = new List<Piece>(candidates);
            foreach (Piece defenderPiece in snapCandidates)
            {
                if (IsKingSafeAfterMove(defender, defenderPiece, attacker.currentPos)) return true;
            }
        }
        return false; // 駒を取る手段がない場合
    }
    
    /// <summary>
    /// 直線移動する駒の王手を遮断できるかチェック
    /// </summary>
    private bool CanAvoidCheck(Turn defender, Piece attacker)
    {
        Piece king = _kingObj[defender];
        // 直線移動する駒かどうかチェック
        if (!pieceDatabase.GetPieceData(attacker.BasePieceType).canStraightMove) return false;
        
        var capParentsSnapshot = new List<CapturePieceParent>(CapturePieceObjects[defender]);
        foreach (var pos in GetDirection(attacker.currentPos, king.currentPos))
        {
            // 盤上の駒を置いて遮断できるかチェック
            if (_allMovesCache[defender].TryGetValue(pos, out List<Piece> movers))
            {
                // 駒を動かせる駒があるかチェック
                var snapshot = new List<Piece>(movers);
                foreach (Piece mover in snapshot)
                {
                    // 王は遮断できないからスキップ
                    if (mover == king) continue;
                    // 遮断できる駒を置いたとき別の駒が王手していないかチェック
                    if (IsKingSafeAfterMove(defender, mover, pos))
                    {
                        return true;
                    }
                }
            }
            
            // 持ち駒を置いて遮断できるかチェック
            foreach (var capturePieceParent in capParentsSnapshot)
            {
                int count = (defender == Turn.先手) 
                    ? SenteCapturedPieceType[(int)capturePieceParent.capturePieceType] 
                    : GoteCapturedPieceType[(int)capturePieceParent.capturePieceType];
                
                if (count <= 0) continue; // 持ち駒がない場合はスキップ
                if (!capturePieceParent.checkMovablePositions.Contains(pos)) continue; // 置ける場所でなければスキップ
                
                if (IsKingSafeAfterDrop(defender, capturePieceParent.capturePieceType, pos))
                {
                    return true;
                }
            }
        }
        
        return false; // 遮断できる手段がない場合
    }
    
    /// <summary>
    /// 直線移動する駒の間のマス目を取得
    /// </summary>
    private IEnumerable<Vector2Int> GetDirection(Vector2Int from, Vector2Int to)
    {
        Vector2Int direction = new Vector2Int
        (
            Math.Sign(to.x - from.x),
            Math.Sign(to.y - from.y)
        );
        
        Vector2Int current = from + direction;
        while (current != to)
        {
            yield return current;
            current += direction;
        }
    }

    /// <summary>
    /// 指定した手番の駒を動かした後に玉が安全かチェック
    /// </summary>
    private bool IsKingSafeAfterMove(Turn moverSide, Piece pieceObj, Vector2Int to)
    {
        // 駒を動かす前の状態を保存
        MoveUndo unmake = MakeMove(pieceObj, to);
        UpdateMovePosFresh();
        
        Turn enemyTurn = (moverSide == Turn.先手) ? Turn.後手 : Turn.先手;
        Piece kingPiece = _kingObj[moverSide];
        bool isSafe = !_allMovesCache[enemyTurn].ContainsKey(kingPiece.currentPos);
        
        // 駒を元の位置に戻す
        UnmakeMove(unmake);
        UpdateMovePosFresh();
        
        return isSafe;
    }
    
    /// <summary>
    /// 指定した手番の持ち駒を置いた後に玉が安全かチェック
    /// </summary>
    private bool IsKingSafeAfterDrop(Turn defender, PieceType pieceType, Vector2Int to)
    {
        DropUndo unmake = MakeDrop(defender, pieceType, to);
        if (unmake.SpawnedPiece == null) return false;
        
        UpdateMovePosFresh();
        
        Turn enemyTurn = (defender == Turn.先手) ? Turn.後手 : Turn.先手;
        Piece kingPiece = _kingObj[defender];
        bool isSafe = !_allMovesCache[enemyTurn].ContainsKey(kingPiece.currentPos);
        
        // 盤面状態を元に戻す
        UnmakeDrop(unmake);
        UpdateMovePosFresh();
        
        return isSafe;
    }

    /// <summary>
    /// 駒の移動・Undo用の構造体
    /// </summary>
    private struct MoveUndo
    {
        public Piece Piece;
        public Vector2Int From;
        public Vector2Int To;
        public Piece Captured;
        public PieceType CapturedType;
        public bool WasPromoted;
    }

    /// <summary>
    /// 駒を指定した位置に移動する（Undo用）
    /// </summary>
    private MoveUndo MakeMove(Piece piece, Vector2Int to)
    {
        MoveUndo undo = new MoveUndo
        {
            Piece = piece,
            From = piece.currentPos,
            To = to,
            Captured = GetPieceAt(to),
            CapturedType = GetPieceTypeAt(to),
            WasPromoted = piece.isPromoted
        };
        
        if (undo.Captured != null)
        {
            RemovePiece(to);
        }
        
        RemovePiece(undo.From);
        PlacePiece(to, GetCurrentPieceType(piece), piece);
        piece.SetPosition(to);
        return undo;
    }

    /// <summary>
    /// 駒を元の位置に戻す
    /// </summary>
    private void UnmakeMove(MoveUndo undo)
    {
        RemovePiece(undo.To);
        undo.Piece.isPromoted = undo.WasPromoted;
        
        PieceType typeAtFrom = ResolveType(undo.Piece.BasePieceType, undo.WasPromoted);
        PlacePiece(undo.From, typeAtFrom, undo.Piece);
        undo.Piece.SetPosition(undo.From);
        
        if (undo.Captured != null)
        {
            PlacePiece(undo.To, undo.CapturedType, undo.Captured);
            undo.Captured.SetPosition(undo.To);
        }
    }
    
    private PieceType GetCurrentPieceType(Piece piece)
    {
        var data = pieceDatabase.GetPieceData(piece.BasePieceType);
        return piece.isPromoted ? data.promotedType : piece.BasePieceType;
    }
    
    private PieceType ResolveType(PieceType baseType, bool promoted)
    {
        var data = pieceDatabase.GetPieceData(baseType);
        return promoted ? data.promotedType : baseType;
    }
    
    /// <summary>
    /// 持ち駒の配置・Undo用の構造体
    /// </summary>
    private struct DropUndo
    {
        public Turn Side;
        public Piece SpawnedPiece;
        public Vector2Int At;
        public PieceType PieceType;
    }
    
    /// <summary>
    /// 持ち駒を指定した位置に置く（Undo用）
    /// </summary>
    private DropUndo MakeDrop(Turn side, PieceType pieceType, Vector2Int to)
    {
        var undo = new DropUndo {Side = side, At = to, PieceType = pieceType, SpawnedPiece = null};
        if (GetPieceAt(to) != null) return undo;
        
        var tmp = new GameObject("TmpDrop").AddComponent<Piece>();
        SpriteRenderer spriteRenderer = tmp.AddComponent<SpriteRenderer>();
        spriteRenderer.enabled = false;
        
        tmp.tag = (side == Turn.先手) ? "Sente" : "Gote";
        
        tmp.ApplyStatePiece(pieceType, to, false, null, null);
        PlacePiece(to, pieceType, tmp);

        undo.SpawnedPiece = tmp;
        return undo;
    }
    
    /// <summary>
    /// 持ち駒を元の位置に戻す
    /// </summary>
    private void UnmakeDrop(DropUndo undo)
    {
        if (undo.SpawnedPiece == null) return;
        RemovePiece(undo.At);
        Destroy(undo.SpawnedPiece.gameObject);
    }
    
    /// <summary>
    /// 駒を指定した位置に移動する
    /// </summary>
    public void MovePiece(Vector2Int from, Vector2Int to)
    {
        Piece movingPiece = _pieceObjects[from];
        PieceType type = _boardState[from.x - 1, from.y - 1];

        RemovePiece(from); // 元の場所を空に
        if (GetPieceAt(to) != null) AddCapturedPiece(to);
        PlacePiece(to, type, movingPiece); // 新しい場所に設置
        
        // Piece側の位置も更新
        movingPiece.SetPosition(to);
        
        // 移動データを保存
        _data = new UsiMoveData
        {
            From = from,
            To = to,
            IsFastPromote = false,
            Type = null
        };
    }
    
    /// <summary>
    /// 成駒に状態変化する際の処理
    /// </summary>
    public void PromotePiece(Vector2Int pos, PieceData pieceData)
    {
        Piece piece = GetPieceAt(pos);
        if (piece == null) return; // 駒が存在しない場合は何もしない

        // 成り駒の状態を設定
        piece.PromotePiece();
        // 盤面の状態を更新
        _boardState[pos.x - 1, pos.y - 1] = pieceData.promotedType;
        
        if (_data.Type == null && _data.To == pos)
        {
            _data.IsFastPromote = true; // 成り選択がされたことを記録
        }
    }

    /// <summary>
    /// 駒を取得した際のゲーム状況の更新
    /// </summary>
    public void AddCapturedPiece(Vector2Int pos)
    {
        Piece enemyPiece = GetPieceAt(pos);
        int[] pieceCount = (ActivePlayer == Turn.先手) ? SenteCapturedPieceType : GoteCapturedPieceType;
        // 駒の種類を取得
        PieceType pieceType = GetPieceTypeAt(pos);
        if (pieceType != PieceType.None)
        {
            PieceType basePieceType = enemyPiece.BasePieceType;
            // 駒の種類に応じてカウントを増やす
            pieceCount[(int)basePieceType]++;
            
            CapturePieceUIManager.Instance.ApplyVisualUI(basePieceType, ActivePlayer);
            
            // 駒オブジェクトを削除
            Destroy(enemyPiece.gameObject);
        }
        // 盤面から駒を削除
        RemovePiece(pos);
    }

    /// <summary>
    /// 持ち駒を指す
    /// </summary>
    public void SetCapturedPiece(PieceType pieceType, Vector2Int pos)
    {
        int[] capturedPieceType = (ActivePlayer == Turn.先手) ? 
            SenteCapturedPieceType : 
            GoteCapturedPieceType;
        
        // 持ち駒の数を指す
        boardInit.CreatePiece(pieceType, pos, ActivePlayer, false);
        capturedPieceType[(int)pieceType]--;
        
        // 持ち駒のUIを更新
        CapturePieceUIManager.Instance.ApplyVisualUI(pieceType, ActivePlayer);
        
        // 移動データを保存
        _data = new UsiMoveData
        {
            From = Vector2Int.zero,
            To = pos,
            IsFastPromote = false,
            Type = pieceType
        };
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

    private struct UsiMoveData
    {
        public Vector2Int From;
        public Vector2Int To;
        public bool IsFastPromote; // 成り選択がされたか
        public PieceType? Type; // 持ち駒の種類
    }
    
    /// <summary>
    /// エンジンに指し手を送信
    /// </summary>
    private void SendMoveToEngine()
    {
        string usiMove;

        if (_data.Type == null) // 通常の移動の場合
        {
            usiMove = UsiConverter.ToUsiMove(_data.From, _data.To);
            if (_data.IsFastPromote) usiMove = UsiConverter.AddPromote(usiMove);
        }
        else // 持ち駒の場合
        {
            usiMove = UsiConverter.ToUsiDrop(_data.Type.Value, _data.To);
        }
        usiEngine.AddMoveToHistory(usiMove);
        
        // エンジンに盤面の状態を送信し、次の指し手を要求
        usiEngine.RequestBestMoveWithHistory();
    }

    /// <summary>
    /// USI形式の指し手の種類
    /// </summary>
    public enum UsiMoveKind {Move, Drop}
    
    /// <summary>
    /// USI形式の指し手を解析した結果を保管する構造体
    /// </summary>
    public struct ParsedUsiMove
    {
        public UsiMoveKind Kind;
        public Vector2Int From;
        public Vector2Int To;
        public bool IsPromote;
        public PieceType? DropPieceType;
    }
    
    /// <summary>
    /// USI形式の指し手をParsedUsiMoveに保管
    /// </summary>
    private ParsedUsiMove ParseUsiMove(string moveString)
    {
        if (moveString[1] != '*')
        {
            var moveData = UsiConverter.ParseMoveString(moveString);
            return new ParsedUsiMove
            {
                Kind = UsiMoveKind.Move,
                From = new Vector2Int(moveData.fromX , moveData.fromY),
                To = new Vector2Int(moveData.toX, moveData.toY),
                IsPromote = moveData.isPromote,
                DropPieceType = null
            };
        }
        else
        {
            var moveData = UsiConverter.ParseDropString(moveString);
            return new ParsedUsiMove
            {
                Kind = UsiMoveKind.Drop,
                From = Vector2Int.zero,
                To = new Vector2Int(moveData.toX, moveData.toY),
                IsPromote = false,
                DropPieceType = moveData.type
            };
        }
    }
    
    /// <summary>
    /// USIEngineからの指し手を受け取る
    /// </summary>
    public void ReceiveEngineMove(string moveString)
    {
        var parsed = ParseUsiMove(moveString);
        Vector2Int toPos = parsed.To;

        if (parsed.Kind == UsiMoveKind.Move)    // 通常の移動の場合
        {
            MovePiece(parsed.From, parsed.To);
            if (parsed.IsPromote) PromotePiece(parsed.To, pieceDatabase.GetPieceData(GetPieceAt(parsed.To).BasePieceType));
        }
        else if (parsed.Kind == UsiMoveKind.Drop && parsed.DropPieceType.HasValue)  // 持ち駒の場合
        {
            SetCapturedPiece(parsed.DropPieceType.Value, parsed.To);
        }
        
        // 指し手を履歴に追加
        usiEngine.AddMoveToHistory(moveString);
        EndTurnPhase(toPos);
        
        _data = default;
    }
    
    private static readonly Dictionary<int, string> KanjiDigits = new()
    {
        { 1, "一" }, { 2, "二" }, { 3, "三" }, { 4, "四" }, { 5, "五" },
        { 6, "六" }, { 7, "七" }, { 8, "八" }, { 9, "九" }
    };
    
    /// <summary>
    /// 指し手をデバッグログに表示
    /// </summary>
    private void DebugLastTurn(Vector2Int toPos)
    {
        string activeTurn = (ActivePlayer == Turn.先手) ? "☗" : "☖";
        float rank = 9 - toPos.x + 1;
        string kanjiFile = KanjiDigits[toPos.y];
        PieceType pieceType = GetPieceTypeAt(toPos);
        string type = pieceType.ToString().Substring(0, 1);
        
        Debug.Log($"{activeTurn}{rank}{kanjiFile}{type.Substring(0)}");
    }
}
