using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Piece : MonoBehaviour, IPointerClickHandler
{
    //変数一覧
    int _shogiPositionX; // x現在地
    int _shogiPositionY;　// y現在地
    Vector2 _minMax = new Vector2(0.5f, 0.5f); // マウス選択の座標の下限値
    Vector2 _maxsize = new Vector2(9.5f, 9.5f); // マウス選択の座標の上限値
    bool _selectPosition; // 駒の選択状態
    bool _promote; // 成駒
    public int senteGote; // 先手と後手の時の動きを変える変数
    bool _isHeldPiece; // 持ち駒として選択されているか
    int huPosition; // 二歩防止ように座標を取得
    
    SpriteRenderer _renderer;
    
    [SerializeField] List<Vector2> canMovePosition = new List<Vector2>(); //駒の移動範囲(list)
    
    ShogiManager _scriptA; //ShogiManagerのスクリプト情報を入手

    [SerializeField] private PieceId pieceType;

    public Sprite defaultSprite; //デフォルトスプライト
    public Sprite promotedSprite; //成駒スプライト
    
    //[SerializeField] ShogiManager gm;
    //[SerializeField] Piece gMscript;
    
    public enum PieceId //各駒の動作と条件を管理
    {
        Hu,
        Kyosha,
        Keima,
        Gin,
        Kin,
        Kaku,
        Hisha,
        Gyoku,
    }
    
    // Start is called before the first frame update
    void Start()
    {
        _scriptA = FindObjectOfType<ShogiManager>();
        
        _shogiPositionX = (int)transform.position.x;
        _shogiPositionY = (int)transform.position.y;
        
        _renderer = GetComponent<SpriteRenderer>(); // SpriteRendererを取得し代入する
        _renderer.sprite = defaultSprite;
    }

    public void StatePiece() //駒の向きの関数
    {
        if (gameObject.CompareTag("Sente"))
        {
            gameObject.transform.eulerAngles = new Vector3(0f, 0f, 0f);
        }
        else if(gameObject.CompareTag("Gote"))
        {
            gameObject.transform.eulerAngles = new Vector3(0f, 0f, 180f);
        }
    }
    
    public void SetPieceType(PieceId type) //ShogiManagerからpieceTypeを取得
    {
        pieceType = type;
    }
    
    void CanMovePosition(Vector2 position) //移動範囲のリストを管理
    {
        // 歩の動ける範囲をリスト化
        if (pieceType == PieceId.Hu)
        {
            canMovePosition.Clear();
            canMovePosition.Add(new Vector2(position.x, position.y + 1 * senteGote));

            if (_promote) // 成金時の動きを追加
            {
                _passedPawn(position);
            }

        }
        //桂馬の動ける範囲
        if (pieceType == PieceId.Keima)
        {
            canMovePosition.Clear();
            canMovePosition.Add(new Vector2(position.x + 1, position.y + 2 * senteGote));
            canMovePosition.Add(new Vector2(position.x - 1, position.y + 2 * senteGote));

            if (_promote) // 成金時の動きを追加
            {
                _passedPawn(position);
            }

        }

        if (pieceType == PieceId.Gin) //銀
        {
            canMovePosition.Clear();
            canMovePosition.Add(new Vector2(position.x, position.y + 1 * senteGote));
            canMovePosition.Add(new Vector2(position.x - 1, position.y + 1 * senteGote));
            canMovePosition.Add(new Vector2(position.x + 1, position.y + 1 * senteGote));
            canMovePosition.Add(new Vector2(position.x - 1, position.y - 1 * senteGote));
            canMovePosition.Add(new Vector2(position.x + 1, position.y - 1 * senteGote));
            
            if (_promote) // 成金時の動きを追加
            {
                _passedPawn(position);
            }
        }
        if (pieceType == PieceId.Kin) //金
        {
            _passedPawn(position);
        }

        if (pieceType == PieceId.Gyoku) //玉将
        {
            _passedPawn(position);
            canMovePosition.Add(new Vector2(position.x, position.y - 1 * senteGote));
        }

        if (pieceType == PieceId.Kyosha) //香車
        {
            canMovePosition.Clear();
            Vector2 canUpPosition = Vector2.up;
            
            for (int i = 1; i < 9; i++)
            {
                Vector2 CheckPosition = position + canUpPosition * i * senteGote;
                
                if (CheckPosition.x < 1 || CheckPosition.x > 9 || CheckPosition.y < 1 || CheckPosition.y > 9) break;
                
                Collider2D collider = Physics2D.OverlapPoint(CheckPosition);
                if (collider != null)
                {
                    Piece otherPiece = collider.GetComponent<Piece>();
                    if (otherPiece != null)
                    {
                        if (otherPiece.gameObject.tag != gameObject.tag)
                        {
                            canMovePosition.Add(CheckPosition);
                        }
                        break;
                    }
                }
                canMovePosition.Add(CheckPosition);
            }
        }
        
        if (pieceType == PieceId.Hisha) //飛車
        {
            canMovePosition.Clear();
            Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
            foreach (var _direction in directions)
            {
                for (int i = 1; i < 9; i++) //縦横8回オブジェクトがあるかを確認し、移動できるマスなのかをチェックする
                {
                    Vector2 CheckPosition = position + _direction * i;
                    
                    if (CheckPosition.x < 1 || CheckPosition.x > 9 || CheckPosition.y < 1 || CheckPosition.y > 9) break; //もし枠外まで確認したらその方向の処理をやめる
                    
                    Collider2D collider = Physics2D.OverlapPoint(CheckPosition); //確認した方向に駒のオブジェクトがあるかを確かめる
                    if (collider != null)
                    {
                        Piece otherPiece = collider.GetComponent<Piece>();
                        if (otherPiece != null)
                        {
                            if (otherPiece.gameObject.tag != gameObject.tag)
                            {
                                canMovePosition.Add(CheckPosition);
                            }
                            break;
                        }
                    }
                    canMovePosition.Add(CheckPosition);
                }
            }
            if (_promote)
            {
                canMovePosition.Add(new Vector2(position.x + 1, position.y + 1));
                canMovePosition.Add(new Vector2(position.x - 1, position.y + 1));
                canMovePosition.Add(new Vector2(position.x + 1, position.y - 1));
                canMovePosition.Add(new Vector2(position.x - 1, position.y - 1));
            }
        }

        if (pieceType == PieceId.Kaku)
        {
            Vector2[] directions = {new Vector2(1,1), new Vector2(1,-1), new Vector2(-1,1), new Vector2(-1,-1) };
            
            foreach (var _direction in directions)
            {
                for (int i = 1; i < 9; i++)
                {
                    Vector2 CheckPosition = position + _direction * i;
                    if (CheckPosition.x < 1 || CheckPosition.x > 9 || CheckPosition.y < 1 || CheckPosition.y > 9) break;
                    
                    Collider2D otherCollider = Physics2D.OverlapPoint(CheckPosition);
                    if (otherCollider != null)
                    {
                        Piece otherPiece = otherCollider.GetComponent<Piece>();
                        if (otherPiece != null)
                        {
                            if (otherPiece.gameObject.tag != gameObject.tag)
                            {
                                canMovePosition.Add(CheckPosition);
                            }
                            break;
                        }
                    }
                    canMovePosition.Add(CheckPosition);
                }
            }

            if (_promote)
            {
                canMovePosition.Add(new Vector2(position.x, position.y + 1));
                canMovePosition.Add(new Vector2(position.x, position.y - 1));
                canMovePosition.Add(new Vector2(position.x + 1, position.y));
                canMovePosition.Add(new Vector2(position.x - 1, position.y));
            }
        }
    }

    void _passedPawn(Vector2 position) //金・成金の関数
    {
        canMovePosition.Clear();
        canMovePosition.Add(new Vector2(position.x, position.y + 1 * senteGote));
        canMovePosition.Add(new Vector2(position.x + 1 * senteGote, position.y));
        canMovePosition.Add(new Vector2(position.x - 1 * senteGote, position.y));
        canMovePosition.Add(new Vector2(position.x + 1 * senteGote, position.y + 1 * senteGote));
        canMovePosition.Add(new Vector2(position.x - 1 * senteGote, position.y + 1 * senteGote));
        canMovePosition.Add(new Vector2(position.x, position.y - 1 * senteGote));
    }

    public void OnPointerClick(PointerEventData eventData)　// 駒のクリックを検知
         {
             if (_scriptA.nowTurn && gameObject.CompareTag("Sente") || !_scriptA.nowTurn && gameObject.CompareTag("Gote")) //先手後手でクリックできるかの判断
             {
                 ShogiManager gameManagerScript = FindObjectOfType<ShogiManager>();
                 if (gameManagerScript.capturedPieceSente.Contains(gameObject) || gameManagerScript.capturedPieceGote.Contains(gameObject)) //持ち駒を選択したか否か
                 {
                     _isHeldPiece = true; //持ち駒の処理に変更
                     Debug.Log("持ち駒を選択");
                 }
                 else
                 {
                     _selectPosition = true; // 選択中に変更
                     if (_scriptA.nowTurn && gameObject.CompareTag("Sente")) senteGote = 1;
                     else if (!_scriptA.nowTurn && gameObject.CompareTag("Gote")) senteGote = -1;
                     canMovePosition.Clear();
                     CanMovePosition(transform.position); // リストを読み込み
                     Debug.Log("選択中：" + _selectPosition);
                 }
             }
         }
    void Update()
{
    
    //画像を連動
    
    transform.position = new Vector2(_shogiPositionX, _shogiPositionY); //駒の座標を変数の値にする
    StatePiece(); //TAGによって向きを変える

    if (Input.GetMouseButtonDown(0))
    {
        Vector2 mousePosition = Input.mousePosition;
        
        if (Camera.main != null)
        {
            Vector2 worldMousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

            // 持ち駒を配置する処理を先に処理する
            if (_isHeldPiece)
            {
                //将棋盤の外側に設置できないようにする
                if (worldMousePosition.x < _minMax.x || worldMousePosition.y < _minMax.y ||
                    worldMousePosition.x > _maxsize.x || worldMousePosition.y > _maxsize.y)
                {
                    Debug.Log("持ち駒を置けない範囲");
                    return;
                }

                Vector2 intMousePos = new Vector2((int)Math.Round(worldMousePosition.x), (int)Math.Round(worldMousePosition.y));
                
                //持ち駒を設置する場所に駒がある場合
                Collider2D collider = Physics2D.OverlapPoint(intMousePos);
                if (collider != null)
                {
                    Debug.Log("駒が被っている");
                    _isHeldPiece = false;
                    Debug.Log(_isHeldPiece);
                    return;
                }

                if (pieceType == PieceId.Hu) //二歩を感知
                {
                    int nowMousePosition = (int)intMousePos.x - 1;
                    
                    if (_scriptA.nowTurn && gameObject.CompareTag("Sente")) //先手の場合
                    {
                        if (_scriptA.senteFuPosition[nowMousePosition])
                        {
                            Debug.Log("二歩");
                            return;
                        }

                        if ((int)intMousePos.y > 8)
                        {
                            Debug.Log("駒を設置できません");
                            return;
                        }
                    }
                    else if (_scriptA.nowTurn == false && gameObject.CompareTag("Gote")) //後手の場合
                    {
                        if (_scriptA.goteFuPosition[nowMousePosition])
                        {
                            Debug.Log("二歩");
                            return;
                        }
                        if ((int)intMousePos.y < 2)
                        {
                            Debug.Log("駒を設置できません");
                            return;
                        }
                    }
                }

                if (_scriptA.nowTurn && gameObject.CompareTag("Sente"))
                {
                    if (pieceType == PieceId.Keima)
                    {
                        if ((int)intMousePos.y > 7)
                        {
                            Debug.Log("駒を設置できません");
                            return;
                        }
                    }

                    if (pieceType == PieceId.Kyosha)
                    {
                        if ((int)intMousePos.x == 8)
                        {
                            Debug.Log("駒を設置できません");
                            return;
                        }
                    }
                }
                else if (_scriptA.nowTurn == false && gameObject.CompareTag("Gote"))
                {
                    if (pieceType == PieceId.Keima)
                    {
                        if ((int)intMousePos.y < 3)
                        {
                            Debug.Log("駒を設置できません");
                            return;
                        }
                    }
                    if (pieceType == PieceId.Kyosha)
                    {
                        if ((int)intMousePos.x == 2)
                        {
                            Debug.Log("駒を設置できません");
                            return;
                        }
                    }
                }
                
                _shogiPositionX = (int)intMousePos.x;
                _shogiPositionY = (int)intMousePos.y;

                _isHeldPiece = false;
                
                ShogiManager gameManagerScript = FindObjectOfType<ShogiManager>();
                
                //持ち駒リストから削除する
                if (gameObject.CompareTag("Sente"))
                {
                    gameManagerScript.capturedPieceSente.Remove(gameObject);
                }
                if (gameObject.CompareTag("Gote"))
                {  
                    gameManagerScript.capturedPieceGote.Remove(gameObject);
                }
                
                _scriptA.nowTurn = !_scriptA.nowTurn;

                Debug.Log("持ち駒を配置：" + _shogiPositionX + "." + _shogiPositionY);
            }

            // 以下は通常の選択状態での移動処理
            if (_selectPosition)
            {

                if (worldMousePosition.x <= _minMax.x || worldMousePosition.y <= _minMax.y ||
                    worldMousePosition.x >= _maxsize.x || worldMousePosition.y >= _maxsize.y) return;

                Vector2 intMousePos = new Vector2((int)Math.Round(worldMousePosition.x), (int)Math.Round(worldMousePosition.y));

                if (canMovePosition.Contains(intMousePos))
                {
                    Collider2D collider = Physics2D.OverlapPoint(worldMousePosition);
                    if (collider != null)
                    {
                        GameObject obj = collider.gameObject;

                        if (obj.CompareTag(gameObject.tag))
                        {
                            _selectPosition = false;
                            return;
                        }
                        else
                        {
                            Piece capturedPiece = obj.GetComponent<Piece>();

                            if (gameObject.CompareTag("Sente") && obj.CompareTag("Gote")) //先手が後手を取ったら
                            {
                                if (pieceType == PieceId.Hu)
                                {
                                    _scriptA.goteFuPosition[(int)(transform.position.x - 1)] = false; //後手の歩がとられた際goteFuPositionから現在のポジションを消す
                                }
                                if (capturedPiece.pieceType == PieceId.Gyoku)
                                {
                                    Debug.Log("先手の勝ち");
                                    _shogiPositionX = (int)intMousePos.x;
                                    _shogiPositionY = (int)intMousePos.y;
                                    
                                    obj.SetActive(false);
                                    
                                    StartCoroutine(ResetGameAfterDelay(3.0f)); // 3秒待ってリセット
                                    return;
                                }
                                
                                _scriptA.AddCapturedPiece(obj, true);
                                
                                Piece targetPiece = obj.GetComponent<Piece>();
                                targetPiece.Reset();
                            }
                            else if (gameObject.CompareTag("Gote") && obj.CompareTag("Sente")) //後手が先手を取ったら
                            {
                                if (pieceType == PieceId.Hu)
                                {
                                    _scriptA.senteFuPosition[(int)(transform.position.x - 1)] = false;　//先手の歩がとられた際senteFuPositionから現在のポジションを消す
                                }
                                
                                if (capturedPiece.pieceType == PieceId.Gyoku)
                                {
                                    Debug.Log("後手の勝ち");
                                    _shogiPositionX = (int)intMousePos.x;
                                    _shogiPositionY = (int)intMousePos.y;
                                    
                                    obj.SetActive(false);
                                    
                                    StartCoroutine(ResetGameAfterDelay(3.0f)); // 3秒待ってリセット
                                    return;
                                }
                                _scriptA.AddCapturedPiece(obj, false);
                                
                                Piece targetPiece = obj.GetComponent<Piece>();
                                targetPiece.Reset();
                            }
                        }
                    }

                    _shogiPositionX = (int)intMousePos.x;
                    _shogiPositionY = (int)intMousePos.y;

                    _selectPosition = false;
                    _scriptA.nowTurn = !_scriptA.nowTurn;
                    Debug.Log("nowTurn = " + _scriptA.nowTurn);

                    if (_shogiPositionY >= 7 && gameObject.CompareTag("Sente") || _shogiPositionY <= 3 && gameObject.CompareTag("Gote"))
                    {
                        if (!_promote) _promote = Promote();
                        if (pieceType == PieceId.Hu) //ト金になった際にlistから外す
                        {
                            if (gameObject.CompareTag("Sente"))
                            {
                                _scriptA.senteFuPosition[(int)intMousePos.x - 1] = false;
                            }
                            else if (gameObject.CompareTag("Gote"))
                            {
                                _scriptA.goteFuPosition[(int)intMousePos.x - 1] = false;
                            }
                        }
                    }
                }
                else
                {
                    _selectPosition = false;
                    Debug.Log("_selectPosition == cancel");
                }
            }
        }
    }
}
    public void SetPosition(int x, int y)
    {
        _shogiPositionX = x;
        _shogiPositionY = y;
    }
    bool Promote() //成金
    {
        _renderer.sprite = promotedSprite;
        return true;
    }

    public void Reset()
    {
        _promote = false;
        _renderer.sprite = defaultSprite;
    }

    IEnumerator ResetGameAfterDelay(float delay)
    {
        // 指定された秒数だけ待機
        yield return new WaitForSeconds(delay);

        // シーンをリロード
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}