using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

/// <summary>
/// Unattended screenshots of the built player, so the look and the attack can be checked without a person at the keyboard:
///   Warrior3D.exe --auto --shots 2.0,2.6,3.2 --shot-path D:/temp/proto3d-shots/v4 [--zoom 6] [--yaw 45] [--view char]
/// --auto turns Auto battle on at start (the hero walks to the nearest slime and keeps attacking), --shots are seconds since start,
/// files are written as <shot-path>_1.png ... and the player quits one second after the last one.
/// </summary>
public class ProtoCapture : MonoBehaviour
{
    float[] shots;
    string path;
    int next;

    void Awake() { if (Array.IndexOf(Environment.GetCommandLineArgs(), "--auto") >= 0) HeroController3D.AutoRequested = true; }   // before every Start

    void Start()
    {
        var a = Environment.GetCommandLineArgs();
        string Val(string k) { int i = Array.IndexOf(a, "--" + k); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
        path = Val("shot-path");
        var list = Val("shots");
        if (list != null) shots = list.Split(',').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).OrderBy(x => x).ToArray();
        var at = Val("at");   // --at x,z: put the hero there first (tree / occlusion checks at a given spot)
        var heroCtl = FindFirstObjectByType<HeroController3D>();
        if (at != null && heroCtl != null)
        {
            var xz = at.Split(',').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
            heroCtl.transform.position = new Vector3(xz[0], heroCtl.transform.position.y, xz[1]);
        }
        var cam = FindFirstObjectByType<CameraFollow3D>();
        if (cam != null)
        {
            var zoom = Val("zoom"); var yaw = Val("yaw");
            if (zoom != null || yaw != null)   // only when asked: calling it always froze the 7 m start distance and hid the glade 18 m default
                cam.SetView(zoom != null ? float.Parse(zoom, CultureInfo.InvariantCulture) : cam.Distance, yaw != null ? float.Parse(yaw, CultureInfo.InvariantCulture) : cam.Yaw);
            if (Val("view") == "char") cam.ToggleCharacterView();   // --view char: the V character view
        }
    }

    void Update()
    {
        if (shots == null || path == null) return;
        if (next < shots.Length && Time.unscaledTime >= shots[next])
        {
            ScreenCapture.CaptureScreenshot(path + "_" + (next + 1) + ".png");
            next++;
        }
        if (next >= shots.Length && Time.unscaledTime >= shots[shots.Length - 1] + 1f) Application.Quit();
    }
}
