using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>First scene of the player: picks the map from the launch args and loads it. `--map frost` = Frost Hollow, `--map forest` = Willowbrook Meadows (2D painted ground), default = Willowbrook Glade (real 3D forest).</summary>
public class MapBoot : MonoBehaviour
{
    void Start()
    {
        var a = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(a, "--map");
        string map = i >= 0 && i + 1 < a.Length ? a[i + 1] : "glade";
        SceneManager.LoadScene(map.StartsWith("frost") ? "Proto" : map.StartsWith("forest") ? "ProtoForest" : "ProtoGlade");
    }
}
