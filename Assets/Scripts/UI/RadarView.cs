using UnityEngine;
using UnityEngine.UI;
using VCS.World;

namespace VCS.UI
{
    /// <summary>
    /// The dirt radar: a top-down camera over the whole house rendered into a round masked view, with bright
    /// markers (layer 8, invisible to the main camera) for mess, sockets, the bin and the player, and a sweep.
    /// </summary>
    public class RadarView : MonoBehaviour
    {
        public const int MarkerLayer = 8;

        Camera cam;
        RenderTexture rt;
        RawImage view;
        RectTransform sweep;
        float angle;

        public static RadarView Build(Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, Vector3 houseCenter, float halfExtent)
        {
            var holder = new GameObject("Radar", typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            UIFactory.Anchor(holder, aMin, aMax, oMin, oMax);
            var r = holder.AddComponent<RadarView>();

            r.rt = new RenderTexture(256, 256, 16) { name = "Radar" };
            var camGo = new GameObject("RadarCamera");
            camGo.transform.position = houseCenter + Vector3.up * 60f;
            camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            r.cam = camGo.AddComponent<Camera>();
            r.cam.orthographic = true;
            r.cam.orthographicSize = halfExtent;
            r.cam.nearClipPlane = 1f;
            r.cam.farClipPlane = 120f;
            r.cam.clearFlags = CameraClearFlags.SolidColor;
            r.cam.backgroundColor = new Color(0.02f, 0.07f, 0.04f);
            r.cam.cullingMask = ~(1 << 5);
            r.cam.targetTexture = r.rt;
            r.cam.enabled = false;
            DontDestroyOnLoad(camGo);

            // round mask with the camera picture inside
            var maskGo = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskGo.transform.SetParent(holder.transform, false);
            UIFactory.Anchor(maskGo, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
            var maskImg = maskGo.GetComponent<Image>();
            maskImg.sprite = UISprites.Circle;
            maskImg.color = Color.white;
            maskGo.GetComponent<Mask>().showMaskGraphic = false;
            var viewGo = new GameObject("View", typeof(RectTransform));
            viewGo.transform.SetParent(maskGo.transform, false);
            UIFactory.Anchor(viewGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            r.view = viewGo.AddComponent<RawImage>();
            r.view.texture = r.rt;
            r.view.color = new Color(0.85f, 1f, 0.9f, 1f);
            r.view.raycastTarget = false;
            // grid and sweep
            UIFactory.Panel(maskGo.transform, "GridH", new Color(0.4f, 1f, 0.5f, 0.18f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -1f), new Vector2(0f, 1f));
            UIFactory.Panel(maskGo.transform, "GridV", new Color(0.4f, 1f, 0.5f, 0.18f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f));
            var sweepImg = UIFactory.Panel(maskGo.transform, "Sweep", new Color(0.45f, 1f, 0.55f, 0.55f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            sweepImg.sprite = UISprites.RadarSweep;
            r.sweep = sweepImg.rectTransform;
            // bezel
            var bezel = UIStyle.Simple(holder.transform, "Bezel", "radar_bezel", Color.white, Vector2.zero, Vector2.one, new Vector2(-10f, -10f), new Vector2(10f, 10f), new Color(0.62f, 0.68f, 0.78f, 0.9f), true);
            if (bezel.sprite == null) { bezel.sprite = UISprites.Ring; UIFactory.Anchor(bezel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); }
            return r;
        }

        public void SetActive(bool on)
        {
            cam.enabled = on;
            gameObject.SetActive(on);
        }

        void LateUpdate()
        {
            angle -= Time.unscaledDeltaTime * 110f;
            sweep.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        void OnDestroy()
        {
            if (cam != null) Destroy(cam.gameObject);
            if (rt != null) rt.Release();
        }

        /// <summary>A flat bright marker high above an object, seen only by the radar camera.</summary>
        public static GameObject Marker(Transform parent, Color color, float size, float height = 25f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "RadarMarker";
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.layer = MarkerLayer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, height, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = Vector3.one * size;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = Palette.Mat(color, 0f, 0f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }
    }
}
