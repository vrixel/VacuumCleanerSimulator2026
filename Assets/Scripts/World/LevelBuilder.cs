using System.Collections.Generic;
using UnityEngine;
using VCS.Player;

namespace VCS.World
{
    /// <summary>The bin where the bag gets emptied. Pulses when the bag is full.</summary>
    public class TrashCan : MonoBehaviour
    {
        public float Radius = 2.6f;
        Renderer body;
        Color baseColor;
        bool highlight;
        float t;

        public void Init(Renderer r) { body = r; baseColor = r.sharedMaterial.color; }
        public void SetHighlight(bool on) { highlight = on; }

        void Update()
        {
            if (body == null) return;
            t += Time.deltaTime;
            if (highlight)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(t * 8f);
                body.material.color = Color.Lerp(baseColor, Palette.Yellow, k);
                transform.localScale = Vector3.one * (1f + 0.08f * k);
            }
            else
            {
                body.material.color = baseColor;
                transform.localScale = Vector3.one;
            }
        }
    }

    /// <summary>
    /// Builds the house: floors, walls with door gaps, furniture and the mess. Coordinates in metres, X right, Z forward.
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        public Transform Root { get; private set; }
        public int MessTotal { get; private set; }
        public int MessCleaned { get; private set; }
        public Vector3 PlayerSpawn { get; private set; }
        public Vector3 HouseCenter { get; private set; }
        public TrashCan Bin { get; private set; }
        public List<WallSocket> Sockets { get; } = new List<WallSocket>();

        const float WallH = 2.4f;
        const float WallT = 0.3f;

        struct Room
        {
            public string Name; public Rect Area; public Color Floor;
            public Room(string name, float x, float z, float w, float d, Color floor) { Name = name; Area = new Rect(x, z, w, d); Floor = floor; }
        }

        static readonly Color[] Brights = { Palette.Red, Palette.Blue, Palette.Yellow, Palette.Green, Palette.Orange, Palette.Purple };

        readonly List<Room> rooms = new List<Room>();
        System.Random rng;
        int colorSeed;

        public void OnMessAbsorbed() { MessCleaned = Mathf.Min(MessTotal, MessCleaned + 1); }
        public void OnMessReleased() { MessCleaned = Mathf.Max(0, MessCleaned - 1); }

        public void Clear()
        {
            if (Root != null) Destroy(Root.gameObject);
            Root = null;
            Bin = null;
            Sockets.Clear();
        }

        public WallSocket NearestSocket(Vector3 pos, float maxDistance)
        {
            WallSocket best = null;
            float bestD = maxDistance;
            foreach (var s in Sockets)
            {
                Vector3 d = s.transform.position - pos;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist < bestD) { bestD = dist; best = s; }
            }
            return best;
        }

        // A white socket flush against a wall, its forward pointing into the room, with a live green LED.
        void Socket(float x, float z, float yaw)
        {
            var root = new GameObject("Socket " + Sockets.Count);
            root.transform.SetParent(Root, false);
            root.transform.position = new Vector3(x, 0.3f, z);
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            PropFactory.Prim(PrimitiveType.Cube, root.transform, new Vector3(0f, 0f, 0.012f), new Vector3(0.16f, 0.16f, 0.025f), Palette.White, "Plate", false);
            PropFactory.Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0f, 0.028f), new Vector3(0.09f, 0.006f, 0.09f), new Color(0.85f, 0.85f, 0.85f), "Well", false, Quaternion.Euler(90f, 0f, 0f));
            PropFactory.Prim(PrimitiveType.Cylinder, root.transform, new Vector3(-0.022f, 0f, 0.03f), new Vector3(0.014f, 0.006f, 0.014f), Palette.Black, "Hole", false, Quaternion.Euler(90f, 0f, 0f));
            PropFactory.Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0.022f, 0f, 0.03f), new Vector3(0.014f, 0.006f, 0.014f), Palette.Black, "Hole", false, Quaternion.Euler(90f, 0f, 0f));
            PropFactory.Prim(PrimitiveType.Sphere, root.transform, new Vector3(0.06f, 0.06f, 0.028f), Vector3.one * 0.014f, Palette.Green, "Led", false);
            var s = root.AddComponent<WallSocket>();
            s.Index = Sockets.Count;
            Sockets.Add(s);
            VCS.UI.RadarView.Marker(root.transform, new Color(0.4f, 0.75f, 1f), 1.1f, 24f);
        }

        public void Build(int seed)
        {
            Clear();
            rng = new System.Random(seed);
            colorSeed = seed * 1000;
            MessTotal = 0; MessCleaned = 0;
            Root = new GameObject("World").transform;
            Root.SetParent(transform, false);

            rooms.Clear();
            rooms.Add(new Room("Living room", 0, 0, 14, 12, Palette.WoodFloor));
            rooms.Add(new Room("Kitchen", 14, 0, 14, 8, Palette.TileFloor));
            rooms.Add(new Room("Hall", 14, 8, 14, 4, Palette.WoodFloor));
            rooms.Add(new Room("Bedroom", 0, 12, 14, 8, Palette.Carpet));
            rooms.Add(new Room("Bathroom", 14, 12, 7, 8, Palette.BathTile));
            rooms.Add(new Room("Entrance", 21, 12, 7, 8, Palette.Stone));
            HouseCenter = new Vector3(14f, 0f, 10f);
            PlayerSpawn = new Vector3(21f, 0.6f, 10f);

            BuildFloorsAndWalls();
            BuildFurniture();
            BuildSockets();
            BuildMess();
            BuildLighting();
        }

        // One socket per room; the first one is where a corded vacuum starts plugged in (hall, east wall).
        void BuildSockets()
        {
            Socket(27.83f, 10.0f, -90f);   // hall, east wall, faces -x
            Socket(0.17f, 4.0f, 90f);      // living room, west wall
            Socket(20.0f, 7.83f, 180f);    // kitchen, wall shared with the hall, faces -z
            Socket(8.0f, 19.83f, 180f);    // bedroom, north wall
            Socket(14.17f, 15.0f, 90f);    // bathroom, west wall
            Socket(27.83f, 16.0f, -90f);   // entrance, east wall
        }

        Room R(string name)
        {
            foreach (var r in rooms) if (r.Name == name) return r;
            return rooms[0];
        }

        void BuildFloorsAndWalls()
        {
            foreach (var r in rooms)
            {
                var c = r.Area.center;
                PropFactory.StaticBox(Root, new Vector3(c.x, -0.1f, c.y), new Vector3(r.Area.width, 0.2f, r.Area.height), r.Floor, "Floor " + r.Name);
            }
            PropFactory.StaticBox(Root, new Vector3(14f, -0.3f, 10f), new Vector3(90f, 0.2f, 90f), Palette.Grass, "Garden");
            PropFactory.StaticBox(Root, new Vector3(24.8f, -0.05f, 22f), new Vector3(2.4f, 0.1f, 4f), Palette.Stone, "Path");

            // outer walls (the gap in the north wall is the front door)
            WallAlongX(0f, 0f, 28f);
            WallAlongX(20f, 0f, 28f, 24f, 25.6f);
            WallAlongZ(0f, 0f, 20f);
            WallAlongZ(28f, 0f, 20f);
            // interior walls
            WallAlongZ(14f, 0f, 12f, 4f, 5.6f, 9f, 10.6f);
            WallAlongX(8f, 14f, 28f, 16f, 17.6f, 24f, 25.6f);
            WallAlongX(12f, 0f, 28f, 6f, 7.6f, 17f, 18.6f, 24f, 25.6f);
            WallAlongZ(14f, 12f, 20f);
            WallAlongZ(21f, 12f, 20f);
        }

        // Wall parallel to X at fixed z, from x0 to x1; gaps are (start, end) pairs along X.
        void WallAlongX(float z, float x0, float x1, params float[] gaps)
        {
            float a = x0 - WallT / 2f;
            float end = x1 + WallT / 2f;
            for (int i = 0; i + 1 < gaps.Length; i += 2)
            {
                Segment(a, gaps[i], z, true);
                a = gaps[i + 1];
            }
            Segment(a, end, z, true);
        }

        void WallAlongZ(float x, float z0, float z1, params float[] gaps)
        {
            float a = z0 - WallT / 2f;
            float end = z1 + WallT / 2f;
            for (int i = 0; i + 1 < gaps.Length; i += 2)
            {
                Segment(a, gaps[i], x, false);
                a = gaps[i + 1];
            }
            Segment(a, end, x, false);
        }

        void Segment(float from, float to, float fixedCoord, bool alongX)
        {
            float len = to - from;
            if (len <= 0.01f) return;
            float mid = (from + to) / 2f;
            Vector3 center = alongX ? new Vector3(mid, WallH / 2f, fixedCoord) : new Vector3(fixedCoord, WallH / 2f, mid);
            Vector3 size = alongX ? new Vector3(len, WallH, WallT) : new Vector3(WallT, WallH, len);
            PropFactory.StaticBox(Root, center, size, Palette.Wall, "Wall");
            Vector3 trimSize = alongX ? new Vector3(len, 0.12f, WallT + 0.04f) : new Vector3(WallT + 0.04f, 0.12f, len);
            PropFactory.Prim(PrimitiveType.Cube, Root, new Vector3(center.x, 0.06f, center.z), trimSize, Palette.WallTrim, "Trim", false);
        }

        Debris Spawn(DebrisKind kind, float x, float z, float yRot = 0f, float y = 0.02f)
        {
            var d = PropFactory.Spawn(kind, new Vector3(x, y, z), Quaternion.Euler(0f, yRot, 0f), Root, colorSeed++);
            if (d.CountsAsMess) MessTotal++;
            return d;
        }

        void Static(Vector3 center, Vector3 size, Color color, string name, float yRot = 0f)
        {
            PropFactory.StaticBox(Root, center, size, color, name, yRot);
        }

        void Flat(Vector3 center, Vector3 size, Color color, string name)
        {
            PropFactory.Prim(PrimitiveType.Cube, Root, center, size, color, name, false);
        }

        void BuildFurniture()
        {
            // Living room
            Flat(new Vector3(7f, 0.005f, 5f), new Vector3(5f, 0.01f, 3.6f), new Color(0.75f, 0.35f, 0.35f), "Rug");
            Spawn(DebrisKind.Couch, 7f, 2.2f);
            Spawn(DebrisKind.Table, 7f, 5f);
            Static(new Vector3(7f, 0.25f, 10.9f), new Vector3(1.8f, 0.5f, 0.5f), Palette.DarkWood, "TV stand");
            Spawn(DebrisKind.Tv, 7f, 10.9f, 180f, 0.5f);
            Static(new Vector3(0.45f, 1.0f, 6f), new Vector3(0.45f, 2.0f, 2.4f), Palette.DarkWood, "Bookshelf");
            for (int i = 0; i < 4; i++)
                Flat(new Vector3(0.62f, 0.35f + i * 0.45f, 6f), new Vector3(0.2f, 0.3f, 2.1f), Brights[i % Brights.Length], "Books");
            Spawn(DebrisKind.Plant, 1f, 1f);
            Spawn(DebrisKind.Plant, 13f, 1f);
            Spawn(DebrisKind.Lamp, 13f, 11f);
            Spawn(DebrisKind.Stool, 4f, 7.5f);
            Spawn(DebrisKind.Stool, 10f, 7.5f);

            // Kitchen
            Static(new Vector3(21f, 0.45f, 0.65f), new Vector3(12f, 0.9f, 0.7f), Palette.White, "Counter");
            Flat(new Vector3(21f, 0.91f, 0.65f), new Vector3(12f, 0.02f, 0.72f), Palette.Gray, "Counter top");
            Static(new Vector3(18f, 0.46f, 0.65f), new Vector3(0.9f, 0.92f, 0.72f), Palette.Black, "Stove");
            Spawn(DebrisKind.Fridge, 27.2f, 2.2f, -90f);
            Spawn(DebrisKind.Table, 21f, 4.2f);
            Spawn(DebrisKind.Chair, 19.6f, 4.2f, 90f);
            Spawn(DebrisKind.Chair, 22.4f, 4.2f, -90f);
            Spawn(DebrisKind.Chair, 21f, 2.9f, 0f);
            Spawn(DebrisKind.Chair, 21f, 5.5f, 180f);

            // Hall
            Static(new Vector3(27.4f, 0.2f, 10f), new Vector3(0.4f, 0.4f, 1.6f), Palette.LightWood, "Shoe rack");
            Spawn(DebrisKind.Plant, 15f, 11f);

            // Bedroom
            Flat(new Vector3(9f, 0.005f, 15.5f), new Vector3(3.5f, 0.01f, 2.5f), Palette.Pink, "Rug");
            Spawn(DebrisKind.Bed, 4f, 16.6f, 0f);
            Static(new Vector3(6.2f, 0.3f, 15.2f), new Vector3(0.5f, 0.6f, 0.5f), Palette.LightWood, "Nightstand");
            Spawn(DebrisKind.Lamp, 6.2f, 15.2f, 0f, 0.6f);
            Static(new Vector3(11f, 1.0f, 19.4f), new Vector3(2.2f, 2.0f, 0.7f), Palette.LightWood, "Wardrobe");
            Spawn(DebrisKind.Stool, 12.5f, 13.5f);

            // Bathroom
            Spawn(DebrisKind.Toilet, 15.3f, 18.8f, 90f);
            Spawn(DebrisKind.Bathtub, 19.6f, 16f, 0f);
            Static(new Vector3(17.5f, 0.45f, 19.3f), new Vector3(0.8f, 0.9f, 0.5f), Palette.White, "Sink");
            Flat(new Vector3(16f, 0.005f, 15f), new Vector3(1.6f, 0.01f, 1.0f), Palette.Blue, "Bath mat");

            // Entrance
            Flat(new Vector3(24.8f, 0.005f, 19.2f), new Vector3(1.3f, 0.01f, 0.8f), new Color(0.5f, 0.35f, 0.2f), "Doormat");
            Static(new Vector3(22f, 0.2f, 19.4f), new Vector3(1.2f, 0.4f, 0.4f), Palette.LightWood, "Shoe rack");
            BuildTrashCan(new Vector3(26.8f, 0f, 13f));
        }

        void BuildTrashCan(Vector3 pos)
        {
            var root = new GameObject("TrashCan");
            root.transform.SetParent(Root, false);
            root.transform.position = pos;
            var body = PropFactory.Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.42f, 0f), new Vector3(0.62f, 0.42f, 0.62f), new Color(0.2f, 0.45f, 0.3f), "Body");
            PropFactory.Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.86f, 0f), new Vector3(0.68f, 0.03f, 0.68f), Palette.Black, "Lid");
            PropFactory.Prim(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.92f, 0f), new Vector3(0.2f, 0.06f, 0.08f), Palette.Black, "Knob", false);
            Bin = root.AddComponent<TrashCan>();
            Bin.Init(body.GetComponent<Renderer>());
            VCS.UI.RadarView.Marker(root.transform, Color.white, 1.4f, 24f);
        }

        void BuildMess()
        {
            var living = R("Living room"); var kitchen = R("Kitchen"); var hall = R("Hall");
            var bedroom = R("Bedroom"); var bath = R("Bathroom"); var entrance = R("Entrance");

            Scatter(DebrisKind.Crumb, living, 12);
            Scatter(DebrisKind.Dust, living, 14);
            Scatter(DebrisKind.Coin, living, 4);
            Scatter(DebrisKind.Brick, living, 16);
            Scatter(DebrisKind.Ball, living, 3);
            Scatter(DebrisKind.Book, living, 4);

            Scatter(DebrisKind.Crumb, kitchen, 40);
            Scatter(DebrisKind.Cereal, kitchen, 26);
            Scatter(DebrisKind.Coin, kitchen, 3);
            Scatter(DebrisKind.Dust, kitchen, 8);
            Scatter(DebrisKind.PaperRoll, kitchen, 1);

            Scatter(DebrisKind.Leaf, hall, 8);
            Scatter(DebrisKind.Dust, hall, 8);
            Scatter(DebrisKind.Coin, hall, 2);

            Scatter(DebrisKind.Sock, bedroom, 14);
            Scatter(DebrisKind.Book, bedroom, 5);
            Scatter(DebrisKind.Brick, bedroom, 6);
            Scatter(DebrisKind.Dust, bedroom, 14);
            Scatter(DebrisKind.Ball, bedroom, 2);

            Scatter(DebrisKind.PaperRoll, bath, 6);
            Scatter(DebrisKind.Sock, bath, 3);
            Scatter(DebrisKind.Dust, bath, 6);

            Scatter(DebrisKind.Leaf, entrance, 18);
            Scatter(DebrisKind.Dust, entrance, 6);
            Scatter(DebrisKind.Coin, entrance, 3);
            Scatter(DebrisKind.Brick, entrance, 2);

            // leaves blown in from the garden, just outside the front door
            ScatterRect(DebrisKind.Leaf, new Rect(22f, 20.5f, 5.5f, 3f), 14, 0.2f);
        }

        void Scatter(DebrisKind kind, Room room, int count, float margin = 0.8f)
        {
            ScatterRect(kind, room.Area, count, margin);
        }

        void ScatterRect(DebrisKind kind, Rect area, int count, float margin)
        {
            int placed = 0, attempts = 0;
            while (placed < count && attempts < count * 10)
            {
                attempts++;
                float x = Mathf.Lerp(area.xMin + margin, area.xMax - margin, (float)rng.NextDouble());
                float z = Mathf.Lerp(area.yMin + margin, area.yMax - margin, (float)rng.NextDouble());
                if (Vector2.Distance(new Vector2(x, z), new Vector2(PlayerSpawn.x, PlayerSpawn.z)) < 1.6f) continue;
                float rot = (float)rng.NextDouble() * 360f;
                float y = 0.15f + (float)rng.NextDouble() * 0.5f;
                Spawn(kind, x, z, rot, y);
                placed++;
            }
        }

        void BuildLighting()
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(Root, false);
            sunGo.transform.rotation = Quaternion.Euler(52f, 38f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.65f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.66f);
            RenderSettings.fog = false;
        }
    }
}
