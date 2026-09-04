using UnityEngine;
using VCS.Core;

namespace VCS.World
{
    public enum DebrisKind
    {
        Crumb, Dust, Cereal, Coin, Leaf,
        Sock, Brick, Ball, PaperRoll, Book,
        Plant, Lamp, Stool, Chair,
        Table, Couch, Tv,
        Fridge, Bed, Toilet, Bathtub
    }

    /// <summary>Anything the vacuum can pull on. SizeClass 1..5 must be at or below the power level to be eaten.</summary>
    public class Debris : MonoBehaviour
    {
        public DebrisKind Kind;
        public int SizeClass;
        public int Points;
        public float Volume;
        public float Mass;
        public bool CountsAsMess;
        public int ColorSeed;
        public Color PuffColor;
        public Rigidbody Rb;
    }

    /// <summary>Reports once when a piece of furniture ends up on its side.</summary>
    public class TipOverTracker : MonoBehaviour
    {
        bool reported;
        float t;

        void Update()
        {
            if (reported) return;
            t += Time.deltaTime;
            if (t < 0.4f) return;
            t = 0f;
            if (transform.up.y < 0.45f)
            {
                reported = true;
                var gm = GameManager.I;
                if (gm != null && gm.State == GameState.Playing)
                {
                    gm.Objectives.Report("knock");
                    gm.AddScore(10);
                }
            }
        }
    }

    /// <summary>Attached to things blown out of the bag; reports a long-distance launch.</summary>
    public class LaunchTracker : MonoBehaviour
    {
        Vector3 start;
        float life;

        void Start() { start = transform.position; }

        void Update()
        {
            life += Time.deltaTime;
            if ((transform.position - start).magnitude > 8f)
            {
                var gm = GameManager.I;
                if (gm != null)
                {
                    gm.Objectives.Report("launch");
                    gm.AddScore(15);
                }
                Destroy(this);
            }
            else if (life > 6f) Destroy(this);
        }
    }
}
