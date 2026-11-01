using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Rocket Barrage: the mechanic holds the cannon two-handed, aimed at the sky, while the cast roots him. Picture only; RocketBarrageUltimate keeps its own timeline.</summary>
    public sealed class MechanicRocketPose : MonoBehaviour
    {
        public const string PoseAnim = "rocket";
        const float HoldMs = 850f; // lockMs of Rocket Barrage in SkillTable.Mechanic
        HeroController hero;
        float age;

        public static void Begin(HeroController owner)
        {
            if (!NetMode.RendersFx || owner.Class.Id != HeroClass.Mechanic) return;
            var pose = owner.gameObject.AddComponent<MechanicRocketPose>();
            pose.hero = owner;
        }

        void Update()
        {
            age += Time.deltaTime * 1000f;
            if (age >= HoldMs) { Destroy(this); return; }
            hero.SetSkillPose(PoseAnim, 0, 0f);
        }

        void OnDestroy() { if (hero != null) hero.ClearSkillPose(); }
    }
}
