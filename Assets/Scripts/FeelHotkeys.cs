using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>B toggles bloom, F toggles all combat effects, for A/B comparison with the web build.</summary>
    public class FeelHotkeys : MonoBehaviour
    {
        BloomEffect bloom;

        public void Init(BloomEffect bloomEffect)
        {
            bloom = bloomEffect;
            if (bloom != null) bloom.enabled = FeelConfig.BloomOn;
        }

        void Update()
        {
            if (InputAdapter.Pressed(GameKey.Bloom))
            {
                FeelConfig.BloomOn = !FeelConfig.BloomOn;
                if (bloom != null) bloom.enabled = FeelConfig.BloomOn;
            }
            if (InputAdapter.Pressed(GameKey.Fx)) FeelConfig.FxOn = !FeelConfig.FxOn;
        }
    }
}
