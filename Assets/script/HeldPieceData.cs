using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeldPieceData : MonoBehaviour
{
    [SerializeField] public Piece.PieceId pieceType;
    [SerializeField] public bool isSente;
	[SerializeField] public bool isHeldPieceCount;
    
    private HeldPieceManager _heldPieceManager;
    ShogiManager _shogiManager;
    HeldPieceUI _heldPieceUI;

    void OnDestroy()
    {
        if (_heldPieceManager != null)
            _heldPieceManager.OnHeldPieceChanged -= UpdateVisualState;
    }
    
    void Start()
    {
        _heldPieceManager = FindObjectOfType<HeldPieceManager>();
        _shogiManager = FindObjectOfType<ShogiManager>();
        _heldPieceUI = FindObjectOfType<HeldPieceUI>();

        if (isHeldPieceCount)
        {
            _heldPieceManager.OnHeldPieceChanged += UpdateVisualState;
            UpdateVisualState();
        }
    }

    void OnMouseDown()
    {
        if (ShogiManager.ActivePlayer == isSente && ShogiManager.CanSelect && !HeldPieceManager.IsHeldPieceSelected && !ShogiManager.CurrentSelectedPiece)
        {
            _shogiManager.ClearPieceSelection(); //駒の選択をリセット
            HeldPieceManager.SelectedPieceType = pieceType;
            _heldPieceManager.RemoveHeldPiece(pieceType);
        }
        else if(HeldPieceManager.IsHeldPieceSelected)
        {
            _shogiManager.ClearHeldPieceSelection();
        }
        else
        {
            _shogiManager.ClearPieceSelection();
        }
    }
    
    void UpdateVisualState()
    {
        int pieceIndex = (int)pieceType;
        int currentCount = isSente ? 
            _heldPieceManager.senteHeldPieceType[pieceIndex] : 
            _heldPieceManager.goteHeldPieceType[pieceIndex];
        
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = currentCount > 0 ? Color.white : Color.gray;

        // 駒の個数分のクローンを生成と削除
        if (currentCount > 0)
        {
            AdjustCloneCount(currentCount);
        }
    }

    void AdjustCloneCount(int currentCount)
    {
        Vector3 basePosition = transform.position;
        _heldPieceUI.ManageClones(pieceType, isSente, basePosition, currentCount, this.transform);
    }
}