using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

/// <summary>
/// Unattended screenshots of the built player, so the look and the attack can be checked without a person at the keyboard:
///   Warrior3D.exe --auto --shots 2.0,2.6,3.2 --shot-path D:/temp/proto3d-shots/v4 [--zoom 6] [--yaw 45] [--view char]
/// --auto turns Auto battle on at start (the hero walks to the nearest slime and keeps attacking), --shots are seconds since start,
/// files are written as <shot-path>_1.png ... and the player quits one second after the last one.
/// Feel tests (19/11): --path "0.5:0,1;2:0,1r;3:0,0;3.6:1,0;4.4:0,0" drives the stick (time:x,z, r = run) and --log file.csv writes one line
/// per frame (time, dt, hero x/z, speed, animator state, both feet world x/y/z) so start / stop / turn and foot sliding can be measured.
/// </summary>
public class ProtoCapture : MonoBehaviour
{
    float[] shots;
    string path;
    int next;
    (float t, Vector3 move, bool run)[] script;
    int scriptNext;
    System.IO.StreamWriter log;
    HeroController3D hero;
    Transform[] feet;
    Animator heroAnim;

    void Awake() { if (Array.IndexOf(Environment.GetCommandLineArgs(), "--auto") >= 0) HeroController3D.AutoRequested = true; }   // before every Start

    void Start()
    {
        var a = Environment.GetCommandLineArgs();
        string Val(string k) { int i = Array.IndexOf(a, "--" + k); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
        path = Val("shot-path");
        var list = Val("shots");
        if (list != null) shots = list.Split(',').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).OrderBy(x => x).ToArray();
        var pathArg = Val("path");
        if (pathArg != null)
            script = pathArg.Split(';').Select(e =>
            {
                var tv = e.Split(':'); bool run = tv[1].EndsWith("r"); var xz = tv[1].TrimEnd('r').Split(',').Select(v => float.Parse(v, CultureInfo.InvariantCulture)).ToArray();
                return (float.Parse(tv[0], CultureInfo.InvariantCulture), new Vector3(xz[0], 0f, xz[1]), run);
            }).OrderBy(e => e.Item1).ToArray();
        var logArg = Val("log");
        hero = FindFirstObjectByType<HeroController3D>();
        if (logArg != null && hero != null)
        {
            log = new System.IO.StreamWriter(logArg);
            log.WriteLine("t,dt,x,z,speed,state,lfx,lfy,lfz,rfx,rfy,rfz");
            var all = hero.GetComponentsInChildren<Transform>();
            feet = new[] { all.FirstOrDefault(t => t.name.EndsWith("LeftFoot")), all.FirstOrDefault(t => t.name.EndsWith("RightFoot")) };
            heroAnim = hero.GetComponentInChildren<Animator>();
        }
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
        if (script != null)
            while (scriptNext < script.Length && Time.time >= script[scriptNext].t)
            {
                var e = script[scriptNext++];
                HeroController3D.ScriptMove = e.move.sqrMagnitude > 0f ? e.move : Vector3.zero;   // zero (not null): keep the keyboard out of the test
                HeroController3D.ScriptRun = e.run;
            }
        if (shots == null || path == null) return;
        if (next < shots.Length && Time.unscaledTime >= shots[next])
        {
            ScreenCapture.CaptureScreenshot(path + "_" + (next + 1) + ".png");
            next++;
        }
        if (next >= shots.Length && Time.unscaledTime >= shots[shots.Length - 1] + 1f) Application.Quit();
    }

    void LateUpdate() { if (log != null) WriteLog(); }   // after every Update and the animator pass: hero and feet from the same frame

    void WriteLog()
    {
        var ci = CultureInfo.InvariantCulture;
        var st = heroAnim != null ? heroAnim.GetCurrentAnimatorStateInfo(0) : default;
        string F(Transform f) => f == null ? ",," : string.Format(ci, "{0:0.0000},{1:0.0000},{2:0.0000}", f.position.x, f.position.y, f.position.z);
        log.WriteLine(string.Format(ci, "{0:0.0000},{1:0.0000},{2:0.0000},{3:0.0000},{4:0.000},{5},{6},{7}", Time.time, Time.deltaTime, hero.transform.position.x, hero.transform.position.z, hero.Speed, st.shortNameHash, F(feet[0]), F(feet[1])));
    }

    void OnDestroy() { HeroController3D.ScriptMove = null; if (log != null) { log.Flush(); log.Close(); } }
}
