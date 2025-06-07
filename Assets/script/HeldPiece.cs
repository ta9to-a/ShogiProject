using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class HeldPiece : MonoBehaviour
{
    public static HeldPiece Instance {get; private set;}
    
    // 各持ち駒の管理
    [SerializeField] public int[] senteHeldPieceType = new int[7];
    [SerializeField] public int[] goteHeldPieceType = new int[7];
    
    [SerializeField] private List<GameObject> senteInactivePieces = new List<GameObject>();
    [SerializeField] private List<GameObject> goteInactivePieces = new List<GameObject>();
    
    // 視覚的整合性のための設定
    [SerializeField] private Vector2 textOffset = new Vector2(2.75f, -0.75f);
    [SerializeField] private float senteRotation;
    [SerializeField] private float goteRotation = 180f;
    
    // テキスト回転用の追加オフセット（回転後の位置調整）
    [SerializeField] private Vector2 senteTextRotationOffset = Vector2.zero;
    [SerializeField] private Vector2 goteTextRotationOffset = Vector2.zero;
    
    private Piece.PieceId _selectedHeldPieceType;
    private bool _isHeldPieceSelected = false;
    private bool _selectedPieceIsSente;
    
    public Sprite[] heldSprites = new Sprite[7];
    Piece _pieceScript;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    // 配置 - インライン化により処理を直接記述
    void Start()
    {
        float spacing = 1.5f;
        
        // 先手の持ち駒UI配置
        for (int i = 0; i < heldSprites.Length; i++)
        {
            Vector2 pos = CalculateSentePosition(i, spacing);
            
            // スプライトオブジェクト作成（CreatePieceSprite内容をインライン化）
            GameObject spriteObj = new GameObject("Sente_" + i + "_Sprite");
            spriteObj.transform.position = pos;
            spriteObj.transform.rotation = Quaternion.Euler(0f, 0f, senteRotation);
            spriteObj.transform.parent = transform;
            
            SpriteRenderer sr = spriteObj.AddComponent<SpriteRenderer>();
            sr.sprite = heldSprites[i];
            spriteObj.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            spriteObj.layer = LayerMask.NameToLayer("CapturePiece");
            
            // クリック検知追加（AddClickDetection内容をインライン化）
            if (senteHeldPieceType[i] > 0)
            {
                BoxCollider2D collider = spriteObj.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                
                HeldPieceClickHandler clickHandler = spriteObj.AddComponent<HeldPieceClickHandler>();
                clickHandler.SetPieceInfo((Piece.PieceId)i, true, this);
            }
            
            // テキスト作成（CreatePieceText内容をインライン化）
            GameObject textObj = new GameObject("Sente_" + i + "_Text");
            textObj.transform.parent = spriteObj.transform;
            
            Vector2 finalOffset = textOffset + senteTextRotationOffset;
            textObj.transform.localPosition = finalOffset;
            textObj.transform.localRotation = Quaternion.identity;
            
            TextMesh text = textObj.AddComponent<TextMesh>();
            text.text = "×" + senteHeldPieceType[i];
            text.fontSize = 45;
            text.characterSize = 0.1f;
            text.color = Color.white;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            
            // 表示設定（SetPieceVisibility内容をインライン化）
            if (senteHeldPieceType[i] > 1)
            {
                textObj.SetActive(true);
            }
            else
            {
                textObj.SetActive(false);
            }
            
            if (senteHeldPieceType[i] > 0)
            {
                sr.color = Color.white;
            }
            else
            {
                sr.color = new Color(0.5f, 0.5f, 0.5f);
            }
        }
        
        // 後手の持ち駒UI配置
        for (int i = 0; i < heldSprites.Length; i++)
        {
            Vector2 pos = CalculateGotePosition(i, spacing);
            
            // スプライトオブジェクト作成
            GameObject spriteObj = new GameObject("Gote_" + i + "_Sprite");
            spriteObj.transform.position = pos;
            spriteObj.transform.rotation = Quaternion.Euler(0f, 0f, goteRotation);
            spriteObj.transform.parent = transform;
            
            SpriteRenderer sr = spriteObj.AddComponent<SpriteRenderer>();
            sr.sprite = heldSprites[i];
            spriteObj.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            spriteObj.layer = LayerMask.NameToLayer("CapturePiece");
            
            // クリック検知追加
            if (goteHeldPieceType[i] > 0)
            {
                BoxCollider2D collider = spriteObj.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                
                HeldPieceClickHandler clickHandler = spriteObj.AddComponent<HeldPieceClickHandler>();
                clickHandler.SetPieceInfo((Piece.PieceId)i, false, this);
            }
            
            // テキスト作成
            GameObject textObj = new GameObject("Gote_" + i + "_Text");
            textObj.transform.parent = spriteObj.transform;
            
            Vector2 finalOffset = textOffset + goteTextRotationOffset;
            textObj.transform.localPosition = finalOffset;
            textObj.transform.localRotation = Quaternion.identity;
            
            TextMesh text = textObj.AddComponent<TextMesh>();
            text.text = "×" + goteHeldPieceType[i];
            text.fontSize = 45;
            text.characterSize = 0.1f;
            text.color = Color.white;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            
            // 表示設定
            if (goteHeldPieceType[i] > 1)
            {
                textObj.SetActive(true);
            }
            else
            {
                textObj.SetActive(false);
            }
            
            if (goteHeldPieceType[i] > 0)
            {
                sr.color = Color.white;
            }
            else
            {
                sr.color = new Color(0.5f, 0.5f, 0.5f);
            }
        }
    }
    
    // 先手の持ち駒の位置を計算（複雑なロジックのため関数維持）
    private Vector2 CalculateSentePosition(int index, float spacing)
    {
        if (index == 0)
        {
            return new Vector2(11, 4);
        }
        else
        {
            int adjustedIndex = index - 1;
            int col = adjustedIndex < 3 ? 0 : 1;
            int row = adjustedIndex % 3;
            float x = 11f + spacing * col;
            float y = 3 - row;
            return new Vector2(x, y);
        }
    }

    // 後手の持ち駒の位置を計算（複雑なロジックのため関数維持）
    private Vector2 CalculateGotePosition(int index, float spacing)
    {
        if (index == 0)
        {
            return new Vector2(-1, 6);
        }
        else
        {
            int adjustedIndex = index - 1;
            int col = adjustedIndex < 3 ? 0 : 1;
            int row = adjustedIndex % 3;
            float x = -1f - spacing * col;
            float y = 7 + row;
            return new Vector2(x, y);
        }
    }
    
    public void AddHeldPiece(Piece.PieceId pieceType, bool isSente)
    {
        Piece.PieceId originalPieceType = GetOriginalPieceType(pieceType);
        int pieceIndex = (int)originalPieceType;

        if (pieceIndex >= 0 && pieceIndex < 7)
        {
            if (isSente)
            {
                senteHeldPieceType[pieceIndex]++;
                Debug.Log($"先手が{originalPieceType}を取得。現在{senteHeldPieceType[pieceIndex]}枚");
            }
            else
            {
                goteHeldPieceType[pieceIndex]++;
                Debug.Log($"後手が{originalPieceType}を取得。現在{goteHeldPieceType[pieceIndex]}枚");
            }
            
            // UpdateVisualDisplay()をインライン化
            foreach (Transform child in transform)
            {
                if (child.name.Contains("_Sprite"))
                {
                    Destroy(child.gameObject);
                }
            }
            Start(); // 再描画
        }
    }

    public void OnHeldPieceClicked(Piece.PieceId pieceType, bool isSente)
    {
        ShogiManager shogiManager = ShogiManager.Instance;
        if (shogiManager.nowTurn != isSente) return;
        
        int count = GetHeldPieceCount(pieceType, isSente);
        if (count <= 0) return;

        if (_isHeldPieceSelected && _selectedHeldPieceType == pieceType && _selectedPieceIsSente == isSente)
        {
            ClearHeldPieceSelection();
            return;
        }
        
        _selectedHeldPieceType = pieceType;
        _selectedPieceIsSente = isSente;
        _isHeldPieceSelected = true;
        
        Debug.Log($"持ち駒選択: {pieceType} ({(isSente ? "先手" : "後手")})");
    }

    public void ClearHeldPieceSelection()
    {
        _isHeldPieceSelected = false;
        _selectedHeldPieceType = Piece.PieceId.Hu;
        _selectedPieceIsSente = false;
    }

    public bool ConsumeHeldPiece(Piece.PieceId pieceType, bool isSente)
    {
        int pieceIndex = (int) pieceType;
        if (pieceIndex >= 0 && pieceIndex < 7)
        {
            if (isSente && senteHeldPieceType[pieceIndex] > 0)
            {
                senteHeldPieceType[pieceIndex]--;
                Debug.Log($"先手の{pieceType}を消費。残り{senteHeldPieceType[pieceIndex]}枚");
                
                // UpdateVisualDisplay()をインライン化
                foreach (Transform child in transform)
                {
                    if (child.name.Contains("_Sprite"))
                    {
                        Destroy(child.gameObject);
                    }
                }
                Start();
                return true;
            }
            else if (!isSente && goteHeldPieceType[pieceIndex] > 0)
            {
                goteHeldPieceType[pieceIndex]--;
                Debug.Log($"後手の{pieceType}を消費。残り{goteHeldPieceType[pieceIndex]}枚");
                
                // UpdateVisualDisplay()をインライン化
                foreach (Transform child in transform)
                {
                    if (child.name.Contains("_Sprite"))
                    {
                        Destroy(child.gameObject);
                    }
                }
                Start();
                return true;
            }
        }
        return false;
    }

    public int GetHeldPieceCount(Piece.PieceId pieceType, bool isSente)
    {
        int pieceIndex = (int)pieceType;

        if (pieceIndex >= 0 && pieceIndex < 7)
        {
            return isSente ? senteHeldPieceType[pieceIndex] : goteHeldPieceType[pieceIndex];
        }
        return 0;
    }

    public class HeldPieceClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private Piece.PieceId pieceType;
        private bool isSente;
        private HeldPiece heldPieceManager;
        private IPointerClickHandler _pointerClickHandlerImplementation;

        public void SetPieceInfo(Piece.PieceId type, bool sente, HeldPiece manager)
        {
            pieceType = type;
            isSente = sente;
            heldPieceManager = manager;
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"HeldPieceClickHandler: 持ち駒がクリックされました - PieceType: {pieceType}, Sente: {isSente}");
            heldPieceManager.OnHeldPieceClicked(pieceType, isSente);
        }

        public void OnPieceClick(PointerEventData eventData)
        {
            _pointerClickHandlerImplementation.OnPieceClick(eventData);
        }
    }    
    
    public void DebugShowPieces()
    {
        Debug.Log("=== 先手の持ち駒 ===");
        for (int i = 0; i < 7; i++)
        {
            if (senteHeldPieceType[i] > 0)
            {
                Debug.Log($"{(Piece.PieceId)i}: {senteHeldPieceType[i]}枚");
            }
        }
    
        Debug.Log("=== 後手の持ち駒 ===");
        for (int i = 0; i < 7; i++)
        {
            if (goteHeldPieceType[i] > 0)
            {
                Debug.Log($"{(Piece.PieceId)i}: {goteHeldPieceType[i]}枚");
            }
        }
    }
    
    private Piece.PieceId GetOriginalPieceType(Piece.PieceId pieceType)
    {
        return pieceType;
    }
}