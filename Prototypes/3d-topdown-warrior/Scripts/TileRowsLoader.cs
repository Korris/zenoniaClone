using UnityEngine;

/// <summary>Hands the map's tile rows (a text asset, row 0 = north) to TileWalk before anything moves.</summary>
[DefaultExecutionOrder(-100)]
public class TileRowsLoader : MonoBehaviour
{
    public TextAsset Rows;
    void Awake() { TileWalk.Rows = Rows.text.Replace("\r", "").Split('\n'); }
}
