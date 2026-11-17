using UnityEngine;

/// <summary>The 2D Mire Hydra Queen sheet (4x4 cells, row 0 = facing down) drawn as a camera-facing billboard standing on the 3D ground: a stand-in until the boss has a 3D model.</summary>
public class BossBillboard : MonoBehaviour
{
    public Texture2D Sheet;
    public float HeightMeters = 5f, Fps = 6f;
    SpriteRenderer sr;
    Sprite[] frames;

    void Start()
    {
        int cell = Sheet.width / 4;
        frames = new Sprite[4];
        for (int i = 0; i < 4; i++)
            frames[i] = Sprite.Create(Sheet, new Rect(i * cell, Sheet.height - cell, cell, cell), new Vector2(0.5f, 0.02f), cell / HeightMeters);
        var go = new GameObject("Sprite");
        go.transform.SetParent(transform, false);
        sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }

    void LateUpdate()
    {
        Transform cam = Camera.main.transform;
        sr.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(cam.forward, Vector3.up), Vector3.up);
        sr.sprite = frames[(int)(Time.time * Fps) % frames.Length];
    }
}
