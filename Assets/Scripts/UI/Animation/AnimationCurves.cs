using UnityEngine;

namespace CRClone.UI.Animation
{
    public static class AnimationCurves
    {
        public static readonly AnimationCurve EaseOutBack = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.7f, 1.1f, 0f, 0f),
            new Keyframe(1f, 1f, 0f, 0f)
        );

        public static readonly AnimationCurve EaseInBack = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.3f, -0.1f, 0f, 0f),
            new Keyframe(1f, 1f, 0f, 0f)
        );
    }
}
