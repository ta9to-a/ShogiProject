using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class PromotionUIManager : MonoBehaviour
{
    public static PromotionUIManager Instance { get; private set; }
    
    [SerializeField] private GameObject panel;
    [SerializeField] private Button promoteButton;
    [SerializeField] private Button unpromoteButton;
    
    private UniTaskCompletionSource<bool> _tcs;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        panel.SetActive(false);
    }

    /// <summary>
    /// 駒の成り・不成のUIを表示する
    /// </summary>
    /// <param name="currentPos">駒の座標</param>
    /// <param name="unpromSprite">不成スプライト</param>
    /// <param name="promSprite">成駒スプライト</param>
    /// <returns></returns>
    public async UniTask<bool> ShowAsync(Vector2Int currentPos, Sprite unpromSprite, Sprite promSprite)
    {
        promoteButton.GetComponent<Image>().sprite = promSprite;
        unpromoteButton.GetComponent<Image>().sprite = unpromSprite;
        
        _tcs = new UniTaskCompletionSource<bool>();
        
        // 選択された駒の位置に移動
        Vector2 screenPos = Camera.main.WorldToScreenPoint(new Vector2(currentPos.x, currentPos.y));
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panel.transform.parent.GetComponent<RectTransform>(),
            screenPos,
            null,
            out var localPoint
        );
        rectTransform.anchoredPosition = localPoint;
        // 先手と後手の向き設定
        rectTransform.rotation = (ShogiManager.Instance.activePlayer == Turn.先手) ?
            Quaternion.identity :
            Quaternion.Euler(0f, 0f, 180f);
        
        await UniTask.Yield();
        panel.SetActive(true);
          
        // ボタンのクリックイベントを設定
        promoteButton.onClick.AddListener(() => _tcs.TrySetResult(true));
        unpromoteButton.onClick.AddListener(() => _tcs.TrySetResult(false));
        
        bool result = await _tcs.Task; // 結果待ち
        
        promoteButton.onClick.RemoveAllListeners();
        unpromoteButton.onClick.RemoveAllListeners();
        
        panel.SetActive(false);
        
        return result;
    }
}
