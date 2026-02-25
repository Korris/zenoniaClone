using UnityEngine;

/// <summary>
/// Lives on each equipment child GO (HelmetLayer, ArmorLayer, WeaponLayer).
/// LateUpdate syncs sprite frame with body SpriteRenderer — no duplicate Animator needed.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class EquipmentSlot : MonoBehaviour
{
    [Header("Config")]
    public EquipmentSlotType SlotType;
    public int SortingOrder = 1;

    private SpriteRenderer _renderer;
    private SpriteRenderer _bodyRenderer;
    private EquipmentData _equipped;

    /// <summary>Read by EquipmentManager for stat bonus calculation</summary>
    public EquipmentData CurrentEquipment => _equipped;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _renderer.sortingOrder = SortingOrder;
        _renderer.enabled = false;
    }

    /// <summary>Called by EquipmentManager on Awake — pass parent body SpriteRenderer</summary>
    public void Init(SpriteRenderer bodyRenderer)
    {
        _bodyRenderer = bodyRenderer;
    }

    public void Equip(EquipmentData data)
    {
        _equipped = data;
        _renderer.enabled = true;
    }

    public void Unequip()
    {
        _equipped = null;
        _renderer.enabled = false;
        _renderer.sprite = null;
    }

    private void LateUpdate()
    {
        if (_equipped == null || _bodyRenderer == null) return;
        SyncSprite();
    }

    /// <summary>
    /// Reads body sprite name, parses frame index, sets matching equipment sprite.
    /// Handles both "sheet_N" and "sheet (N)" Unity naming formats.
    /// </summary>
    private void SyncSprite()
    {
        int frameIndex = ParseFrameIndex(_bodyRenderer.sprite);
        if (frameIndex < 0 || frameIndex >= _equipped.Sprites.Length)
        {
            _renderer.enabled = false;
            return;
        }
        _renderer.enabled = true;
        _renderer.sprite = _equipped.Sprites[frameIndex];
    }

    /// <summary>
    /// Parses frame index from Unity sprite name. Supports "sheet_N" and "sheet (N)" formats.
    /// Allocation-free — no Regex.
    /// </summary>
    private static int ParseFrameIndex(Sprite sprite)
    {
        if (sprite == null) return -1;
        string name = sprite.name;

        // Try "sheet_N" format first (most common)
        int underscore = name.LastIndexOf('_');
        if (underscore >= 0 && underscore < name.Length - 1)
            if (int.TryParse(name.Substring(underscore + 1), out int idx)) return idx;

        // Fallback: "sheet (N)" format
        int open = name.LastIndexOf('(');
        int close = name.LastIndexOf(')');
        if (open >= 0 && close > open)
            if (int.TryParse(name.Substring(open + 1, close - open - 1).Trim(), out int idx2)) return idx2;

        return -1;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = SortingOrder;
    }
#endif
}
