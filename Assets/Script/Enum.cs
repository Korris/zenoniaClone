using UnityEngine;

namespace Assets.Script
{
    public static class EnumColor
    {
        public static readonly Color HitColor = Color.red;
    }

    public static class EnumAnimation
    {
        public const string Idle = "isIdleing";
        public const string Walk = "isMoving";
        public const string Attack = "isAttacking";
        public const string Die = "isDieing";
    }

}
