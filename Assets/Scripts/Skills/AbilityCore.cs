using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NightHunter.combat
{
    // Lets skills intercept incoming damage in Health.
    // (If you already added this elsewhere, delete this copy.)
    public interface IDamageModifier
    {
        int ModifyIncomingDamage(int incoming);
    }

    // Everything a skill needs at runtime.
    public struct AbilityContext
    {
        public Transform Caster;      // player root
        public Camera AimCamera;      // FPS camera
        public Transform Origin;      // muzzle/hand (fallback: camera)
        public Ray AimRay;            // center-screen ray
        public LayerMask HitMask;     // what’s valid to hit
    }

    // ----- Module base classes -----

    public abstract class TargetingSO : ScriptableObject
    {
        // Fill 'hits' with either Colliders, Transforms, or Vector3 points (module decides).
        public abstract void Acquire(AbilityContext ctx, List<object> hits);
    }

    public abstract class DeliverySO : ScriptableObject
    {
        // Call onImpact(targetOrPoint) for each hit; yield while delivering (dash/projectile/etc.)
        public abstract IEnumerator Execute(AbilityContext ctx, List<object> targets, System.Action<object> onImpact);
    }

    public abstract class EffectSO : ScriptableObject
    {
        public virtual void OnCast(AbilityContext ctx) { }
        public virtual void OnImpact(AbilityContext ctx, object target) { }
        public virtual void OnEnd(AbilityContext ctx) { }
    }
}
