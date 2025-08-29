using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class MoveHighlight : MonoBehaviour
{
    // 移動可能なマス目のハイライト
    [SerializeField] private GameObject highlightPrefab; // ハイライトのプレハブ
    private List<GameObject> _highlights = new List<GameObject>(); // ハイライトのリスト
    
    // 移動があったマス目のハイライト
    [SerializeField] public GameObject lastMoveHighlight; // 最後の移動のハイライトのプレハブ
    private bool _isChanging;
    
    // キャンセルトークン
    private System.Threading.CancellationTokenSource _cts;

    private void Awake()
    {
        _isChanging = false;
        _highlights.Clear();
        
        lastMoveHighlight.SetActive(false);
        lastMoveHighlight.GetComponent<SpriteRenderer>().sortingOrder = 0;
    }

    private void OnEnable()
    {
        _cts?.Cancel();
        _cts = new System.Threading.CancellationTokenSource();
        _ = ChangingMovedPointMoveAsync(_cts.Token);
    }

    private void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>
    /// ハイライトを表示する
    /// </summary>
    /// <param name="positions"></param>
    public void SetCanMovePosHighlight(List<Vector2Int> positions)
    {
        if (highlightPrefab == null || positions == null) return;
        _isChanging = false;
        
        for (int x = 1; x <= 9; x++)
        {
            for (int y = 1; y <= 9; y++)
            {
                Vector2Int checkPosition = new Vector2Int(x,y);
                if (!positions.Contains(checkPosition))
                {
                    Vector3 highlightPos = new Vector3(x, y, 0f);
                    GameObject highlight = Instantiate(highlightPrefab, highlightPos, Quaternion.identity, transform);
                    highlight.name = "highlightPos:" + (int)highlightPos.x + "," + (int)highlightPos.y;
                    highlight.GetComponent<SpriteRenderer>().sortingOrder = 2;
                    
                    _highlights.Add(highlight);
                }
            }
        }
    }
    
    /// <summary>
    /// ハイライトを非表示にする
    /// </summary>
    public void RemoveHighlight()
    {
        foreach (var point in _highlights)
        {
            if (point != null)
            {
                Destroy(point);
            }
        }
        _highlights.Clear();
        
        // TODO: 点滅を止める
        _isChanging = true;
    }
    
    /// <summary>
    /// 最後に移動したマス目にハイライトを表示
    /// </summary>
    /// <param name="toPos"></param>
    public void SetLastMoveHighlight(Vector2Int? toPos)
    {
        if (lastMoveHighlight == null) return;

        // 移動元と移動先にハイライトを表示
        if (toPos != null) lastMoveHighlight.transform.position = new Vector3(toPos.Value.x, toPos.Value.y, 0f);
        _isChanging = true;
    }

    /// <summary>
    /// 
    /// </summary>
    private async UniTask ChangingMovedPointMoveAsync(System.Threading.CancellationToken ct)
    {
        if (lastMoveHighlight == null) return;
        
        SpriteRenderer spriteRenderer = lastMoveHighlight.GetComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(1f, 1f, 1f, 0f);
        
        while (!ct.IsCancellationRequested)
        {
            if (!_isChanging)
            {
                if (lastMoveHighlight.activeSelf)
                {
                    lastMoveHighlight.SetActive(false);
                    spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
                }
                // 次フレームまで待機
                await UniTask.Yield(cancellationToken: ct);
                continue;
            }

            // 点滅開始
            lastMoveHighlight.SetActive(true);
            
            for (float alpha = 0.5f; _isChanging && alpha <= 1f; alpha += 0.025f)
            {
                spriteRenderer.color = new Color(1f, 1f, 1f, alpha);
                await UniTask.Delay(30, cancellationToken: ct);
            }
            for (float alpha = 1f; _isChanging && alpha >= 0.5f; alpha -= 0.025f)
            {
                spriteRenderer.color = new Color(1f, 1f, 1f, alpha);
                await UniTask.Delay(30, cancellationToken: ct);
            }
        }
    }
}
