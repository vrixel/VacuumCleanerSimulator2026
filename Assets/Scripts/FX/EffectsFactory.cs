using UnityEngine;
using VCS.World;

namespace VCS.FX
{
    /// <summary>Particle systems configured from code: dust puffs, sparkles and the suction swirl.</summary>
    public class EffectsFactory : MonoBehaviour
    {
        ParticleSystem puff;
        ParticleSystem sparkle;

        public static EffectsFactory Create(Transform parent)
        {
            var go = new GameObject("FX");
            go.transform.SetParent(parent, false);
            var f = go.AddComponent<EffectsFactory>();
            f.Build();
            return f;
        }

        void Build()
        {
            puff = MakeBurstSystem("Puff", 0.35f, 0.8f, 0.15f, 0.4f, 0.5f, 2.5f, 0f);
            sparkle = MakeBurstSystem("Sparkle", 0.4f, 0.9f, 0.06f, 0.14f, 2f, 5f, 0.6f);
        }

        ParticleSystem MakeBurstSystem(string name, float lifeMin, float lifeMax, float sizeMin, float sizeMax, float speedMin, float speedMax, float gravity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = gravity;
            main.maxParticles = 4000;

            var em = ps.emission;
            em.enabled = false;

            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.15f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = Palette.Particle;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortMode = ParticleSystemSortMode.None;
            ps.Play();
            return ps;
        }

        public void Puff(Vector3 pos, Color c, int n)
        {
            var ep = new ParticleSystem.EmitParams { position = pos, startColor = c, applyShapeToPosition = true };
            puff.Emit(ep, n);
        }

        public void Sparkle(Vector3 pos, int n)
        {
            var ep = new ParticleSystem.EmitParams { position = pos, startColor = Palette.Gold, applyShapeToPosition = true };
            sparkle.Emit(ep, n);
        }

        /// <summary>Looping particles that converge on the nozzle. Negative start speed on a cone points them inward.</summary>
        /// <summary>
        /// The boost trail: a dust plume and a few hot sparks thrown out behind the vacuum. Both systems start
        /// silent; the controller drives their emission with the turbo state.
        /// </summary>
        public ParticleSystem[] CreateBoostTrail(Transform root)
        {
            var holder = new GameObject("BoostTrail");
            holder.transform.SetParent(root, false);
            holder.transform.localPosition = new Vector3(0f, 0.12f, -0.35f);
            holder.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // +z of the emitters points backwards

            var dust = holder.AddComponent<ParticleSystem>();
            var m = dust.main;
            m.loop = true; m.playOnAwake = true;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 6.5f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            m.startColor = new ParticleSystem.MinMaxGradient(new Color(0.97f, 0.96f, 0.94f, 1f), new Color(0.75f, 0.74f, 0.72f, 1f));
            m.maxParticles = 600;
            m.gravityModifier = -0.08f;
            var em = dust.emission; em.rateOverTime = 110f; em.rateOverTimeMultiplier = 0f;
            var sh = dust.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 25f; sh.radius = 0.15f;
            var sz = dust.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 2.2f));
            var col = dust.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.75f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = holder.GetComponent<ParticleSystemRenderer>();
            r.material = Palette.Particle; r.renderMode = ParticleSystemRenderMode.Billboard;

            var sparkGo = new GameObject("Sparks");
            sparkGo.transform.SetParent(holder.transform, false);
            var sparks = sparkGo.AddComponent<ParticleSystem>();
            var m2 = sparks.main;
            m2.loop = true; m2.playOnAwake = true;
            m2.simulationSpace = ParticleSystemSimulationSpace.World;
            m2.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            m2.startSpeed = new ParticleSystem.MinMaxCurve(6f, 11f);
            m2.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            m2.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.5f, 1f), new Color(1f, 0.55f, 0.15f, 1f));
            m2.maxParticles = 300;
            m2.gravityModifier = 0.8f;
            var em2 = sparks.emission; em2.rateOverTime = 60f; em2.rateOverTimeMultiplier = 0f;
            var sh2 = sparks.shape; sh2.shapeType = ParticleSystemShapeType.Cone; sh2.angle = 20f; sh2.radius = 0.08f;
            var r2 = sparkGo.GetComponent<ParticleSystemRenderer>();
            r2.material = Palette.Particle; r2.renderMode = ParticleSystemRenderMode.Stretch; r2.velocityScale = 0.12f; r2.lengthScale = 2f;
            dust.Play(); sparks.Play();
            return new[] { dust, sparks };
        }

        public ParticleSystem CreateSuctionSwirl(Transform nozzle)
        {
            var go = new GameObject("Swirl");
            go.transform.SetParent(nozzle, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.45f;
            main.startSpeed = -3.2f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new Color(0.9f, 0.9f, 0.95f, 0.5f);
            main.maxParticles = 300;
            main.gravityModifier = 0f;

            var em = ps.emission;
            em.rateOverTime = 40f;

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 30f;
            sh.radius = 1.1f;
            sh.position = new Vector3(0f, 0f, 1.3f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = Palette.Particle;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            ps.Play();
            return ps;
        }
    }
}
