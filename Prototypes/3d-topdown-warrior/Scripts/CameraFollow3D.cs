using UnityEngine;

/// <summary>
/// Follow camera from the ChatGPT camera spec (docs/reference/3d-warrior-prototype/chatgpt-camera-zoom-face-ro3.txt): six zoom levels, one per
/// wheel notch, blended continuously (SmoothStep between levels, ~0.2 s ease). Hold the right mouse button and drag to orbit (yaw 0.20°/px,
/// pitch 0.12°/px, pitch kept within 10-65°); Q / E still turn 45°. V = character view (2.5 m, 15°, looking at 0.9 m) orbiting round to the
/// front of the hero by the shortest arc in 0.5 s; V again restores the previous view. Home = back to the 7 m / 50° default in 0.25 s.
/// The camera never turns by itself during auto battle. Pitch 0° = level, 90° = straight down.
/// </summary>
public class CameraFollow3D : MonoBehaviour
{
    struct Level { public float Dist, Pitch, Fov, Aim; public Level(float d, float p, float f, float a) { Dist = d; Pitch = p; Fov = f; Aim = a; } }
    static readonly Level[] Levels =
    {
        new Level(27.5f, 55f, 30f, 0.65f), // glade far   - tree-lean fix (ChatGPT 18/11): FOV 38 -> 30 and farther back = same hero size, far less lean
        new Level(21.6f, 55f, 30f, 0.65f), // glade mid
        new Level(15.7f, 55f, 30f, 0.65f), // glade default (was 12.5 m / 50° / FOV 38)
        new Level(12f, 55f, 40f, 0.50f),   // farthest of the first camera table
        new Level(9f, 53f, 38f, 0.55f),    // 1 wide
        new Level(7f, 50f, 35f, 0.60f),    // 2 gameplay default
        new Level(5f, 44f, 35f, 0.70f),    // 3 near
        new Level(3.5f, 32f, 35f, 0.82f),  // 4 character look
        new Level(2.2f, 12f, 35f, 0.95f),  // 5 close
    };
    static readonly Level CharView = new Level(2.5f, 15f, 35f, 0.90f);
    public int DefaultLevel = 5;   // 7 m / 50; the glade scene uses 1 (18 m)

    public Transform Target;
    public float Yaw = 0f, Smooth = 8f;
    public float YawPerPixel = 0.20f, PitchPerPixel = 0.12f, MinPitch = 10f, MaxPitch = 65f;

    float levelGoal = 5, level = 5, levelVel;   // fractional zoom level and its ease
    float yawGoal, pitchOffset, pitchOffsetGoal;                       // right-drag pitch is an offset on top of the level's pitch
    float charBlend, charGoal;                                         // 0 = level camera, 1 = character view
    float savedYaw, savedLevel, savedPitchOffset;
    Vector3 lastMouse;
    Camera cam;

    /// <summary>Current camera distance (read by tools); setting it jumps to the matching fractional level.</summary>
    public float Distance { get => Eval(viewSet || started ? level : DefaultLevel).Dist; set => level = levelGoal = LevelForDistance(value); }

    void Awake() { cam = GetComponent<Camera>(); yawGoal = Yaw; }

    bool started;
    void Start() { if (!viewSet) level = levelGoal = DefaultLevel; started = true; Snap(); }

    /// <summary>Jumps to `distance` / `yaw` without easing (ProtoCapture --zoom / --yaw).</summary>
    bool viewSet;
    public void SetView(float distance, float yaw) { viewSet = true; Distance = distance; Yaw = yawGoal = yaw; if (Target != null) Snap(); }

    /// <summary>V: orbit to the front of the hero in a low portrait view, or back to the view saved when it was entered.</summary>
    public void ToggleCharacterView()
    {
        if (charGoal < 0.5f)
        {
            savedYaw = yawGoal; savedLevel = levelGoal; savedPitchOffset = pitchOffsetGoal;
            charGoal = 1f; pitchOffsetGoal = 0f;
            yawGoal = Yaw + Mathf.DeltaAngle(Yaw, Target.eulerAngles.y + 180f);   // shortest arc round to the hero's face
        }
        else { charGoal = 0f; yawGoal = savedYaw; levelGoal = savedLevel; pitchOffsetGoal = savedPitchOffset; }
    }

    /// <summary>Table value at a fractional level, SmoothStep between the two neighbours.</summary>
    static Level Eval(float l)
    {
        int i = Mathf.Clamp(Mathf.FloorToInt(l), 0, Levels.Length - 2);
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(l - i));
        Level a = Levels[i], b = Levels[i + 1];
        return new Level(Mathf.Lerp(a.Dist, b.Dist, t), Mathf.Lerp(a.Pitch, b.Pitch, t), Mathf.Lerp(a.Fov, b.Fov, t), Mathf.Lerp(a.Aim, b.Aim, t));
    }

    static float LevelForDistance(float d)
    {
        for (int i = 0; i < Levels.Length - 1; i++)
            if (d <= Levels[i].Dist && d >= Levels[i + 1].Dist) return i + Mathf.InverseLerp(Levels[i].Dist, Levels[i + 1].Dist, d);
        return d > Levels[0].Dist ? 0f : Levels.Length - 1;
    }

    /// <summary>The view now: level camera blended into the character view, pitch offset applied and clamped.</summary>
    Level Current()
    {
        Level a = Eval(level), b = CharView; float t = Mathf.SmoothStep(0f, 1f, charBlend);
        float pitch = Mathf.Clamp(Mathf.Lerp(a.Pitch, b.Pitch, t) + pitchOffset * (1f - t), Mathf.Min(MinPitch, a.Pitch), MaxPitch);
        return new Level(Mathf.Lerp(a.Dist, b.Dist, t), pitch, Mathf.Lerp(a.Fov, b.Fov, t), Mathf.Lerp(a.Aim, b.Aim, t));
    }

    Vector3 Goal(Level v)
    {
        Quaternion rot = Quaternion.Euler(v.Pitch, Yaw, 0f);
        return Target.position + Vector3.up * v.Aim - rot * Vector3.forward * v.Dist;
    }

    void Snap()
    {
        Level v = Current();
        transform.position = Goal(v); transform.rotation = Quaternion.Euler(v.Pitch, Yaw, 0f);
        if (cam != null) cam.fieldOfView = v.Fov;
    }

    void LateUpdate()
    {
        if (Target == null) return;
        float dt = Time.unscaledDeltaTime;   // camera keeps easing through the hit stop

        float wheel = Input.mouseScrollDelta.y;
        if (wheel != 0f)
        {
            if (charGoal > 0.5f) ToggleCharacterView();   // scrolling leaves the character view
            levelGoal = Mathf.Clamp(Mathf.Round(levelGoal) + Mathf.Sign(wheel), 0f, Levels.Length - 1);   // 1 notch = 1 level
        }
        if (Input.GetMouseButtonDown(1)) lastMouse = Input.mousePosition;
        if (Input.GetMouseButton(1))
        {
            Vector3 d = Input.mousePosition - lastMouse; lastMouse = Input.mousePosition;
            yawGoal += d.x * YawPerPixel;
            pitchOffsetGoal = Mathf.Clamp(pitchOffsetGoal - d.y * PitchPerPixel, MinPitch - 55f, MaxPitch - 12f);
        }
        if (Input.GetKeyDown(KeyCode.Q)) yawGoal -= 45f;
        if (Input.GetKeyDown(KeyCode.E)) yawGoal += 45f;
        if (Input.GetKeyDown(KeyCode.V)) ToggleCharacterView();
        bool reset = Input.GetKeyDown(KeyCode.Home);
        if (reset) { charGoal = 0f; levelGoal = DefaultLevel; pitchOffsetGoal = 0f; yawGoal = 0f; }

        // eases: zoom ~0.2 s (max 3 levels/s ~ 12 m/s at the far end), rotation 0.10 s, character view 0.5 s, Home 0.25 s
        level = Mathf.SmoothDamp(level, levelGoal, ref levelVel, reset ? 0.12f : 0.2f, 3f, dt);
        float rotK = 1f - Mathf.Exp(-dt / 0.10f * 2.3f);
        float orbitK = charGoal != charBlend ? 1f - Mathf.Exp(-dt / 0.5f * 4.6f) : rotK;
        Yaw = Mathf.LerpAngle(Yaw, yawGoal, orbitK);
        pitchOffset = Mathf.Lerp(pitchOffset, pitchOffsetGoal, rotK);
        charBlend = Mathf.MoveTowards(charBlend, charGoal, dt / 0.5f);

        Level v = Current();
        transform.position = Vector3.Lerp(transform.position, Goal(v), 1f - Mathf.Exp(-Smooth * dt));
        transform.rotation = Quaternion.Euler(v.Pitch, Yaw, 0f);
        if (cam != null) cam.fieldOfView = v.Fov;
    }
}
