using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using ZvezdnoePero.CameraRig;
using ZvezdnoePero.Creatures;
using ZvezdnoePero.Input;
using ZvezdnoePero.Player;
using ZvezdnoePero.UI;
using ZvezdnoePero.World;

namespace ZvezdnoePero.EditorTools
{
    /// <summary>
    /// Собирает стартовую сцену «большой карты» по плану переноса:
    /// пять островов-районов примерно 600 × 600 м, здания в реальном масштабе
    /// (игрок 1,7 м, дверь 2,5 м, этаж 3,5 м), мосты, персонаж с метлой, камера и экранное управление.
    /// Всё, кроме трёх GLB-моделей, — серые заготовки (greybox), которые заменяются моделями художников.
    /// </summary>
    public static class WorldBuilder
    {
        const string MatFolder = "Assets/_Project/Materials";
        const string GenFolder = "Assets/_Project/Generated";
        const string Models = "Assets/_Project/Art/Models/";

        // Острова: центр, высота верхней поверхности, радиус.
        static readonly Vector3 Center = new Vector3(0f, 0f, 0f);
        const float CenterR = 110f;
        static readonly Vector3 Academy = new Vector3(0f, 8f, 215f);
        const float AcademyR = 90f;
        static readonly Vector3 Homes = new Vector3(215f, 3f, -55f);
        const float HomesR = 95f;
        static readonly Vector3 Mists = new Vector3(-220f, -8f, -40f);
        const float MistsR = 95f;
        static readonly Vector3 Harbor = new Vector3(0f, -3f, -205f);
        const float HarborR = 75f;

        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        static Mesh coneMesh, pyramidMesh;

        public static void Build(string scenePath)
        {
            mats.Clear();
            Directory.CreateDirectory(MatFolder);
            Directory.CreateDirectory(GenFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            coneMesh = SaveMesh(MakeCone(32), "Cone");
            pyramidMesh = SaveMesh(MakePyramid(), "Pyramid");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupLighting();
            var world = new GameObject("World").transform;

            BuildCenter(world);
            BuildAcademy(world);
            BuildHomes(world);
            BuildMists(world);
            BuildHarbor(world);

            Bridge(world, Center, CenterR, Academy, AcademyR);
            Bridge(world, Center, CenterR, Homes, HomesR);
            Bridge(world, Center, CenterR, Harbor, HarborR);
            // В Туманные Рубежи моста нет: туда только на метле.

            SkyRocks(world);

            var player = BuildPlayer(new Vector3(0f, 0.1f, -22f), out var motor, out var reader);
            var cam = BuildCamera(player, reader, motor);
            motor.Configure(reader, cam.transform, player.Find("Body"), player.Find("Body/Broom").gameObject);
            BuildUi(reader);

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        // ---------------------------------------------------------------- острова

        static void BuildCenter(Transform world)
        {
            var root = Island(world, "Лунный Перекрёсток", "Площадь, базар, почта, зельеварня", Center, CenterR, "Grass", "Rock");
            var p = Center;

            // Площадь и фонтан.
            Disc(root, "Площадь", p + new Vector3(0f, 0.02f, 0f), 32f, "Cobble");
            var fountain = Cyl(root, "Фонтан", p + new Vector3(0f, 0.5f, 0f), new Vector3(9f, 0.5f, 9f), "Stone");
            Cyl(fountain.parent, "Фонтан — вода", p + new Vector3(0f, 0.9f, 0f), new Vector3(8f, 0.1f, 8f), "Water");
            var crystal = Box(root, "Звёздный кристалл", p + new Vector3(0f, 4.5f, 0f), new Vector3(1.4f, 3f, 1.4f), "Glow");
            crystal.rotation = Quaternion.Euler(45f, 0f, 45f);
            crystal.gameObject.AddComponent<FloatingMotion>().Configure(0.4f, 1f, 30f);
            RemoveCollider(crystal);

            // Зельеварня — башня мага (GLB), 28 м.
            var brewPos = p + new Vector3(42f, 0f, 22f);
            const string brewInside = "Варка эликсиров из собранных ингредиентов.";
            var brew = Model(root, "mage-tower.glb", "Зельеварня", brewPos, -120f, 28f, true);
            if (brew == null) Building(root, "Зельеварня", brewInside, brewPos, new Vector3(12f, 3, 12f), "Plaster", "RoofPurple", p);
            else Door(root, "Зельеварня", brewInside, DoorInFront(brew, p));

            Building(root, "Базар", "Покупка и продажа ингредиентов, цены меняются по часам сервера.", p + new Vector3(-44f, 0f, 16f), new Vector3(26f, 2, 16f), "Wood", "RoofRed", p);
            Building(root, "Почта", "Ежедневные задания и посылки.", p + new Vector3(-30f, 0f, -36f), new Vector3(12f, 3, 10f), "Plaster", "RoofBlue", p);
            Building(root, "Лавка одежды", "Одежда и аксессуары для персонажа.", p + new Vector3(30f, 0f, -38f), new Vector3(11f, 2, 10f), "Plaster", "RoofGreen", p);

            // Кольцо городских домов в 2–3 этажа.
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f + 0.2f;
                var pos = p + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 78f;
                if (Vector3.Distance(pos, p + new Vector3(0f, 0f, 78f)) < 20f) continue;   // проход к Академии
                if (Vector3.Distance(pos, p + new Vector3(78f, 0f, -20f)) < 22f) continue; // проход к Жилому берегу
                if (Vector3.Distance(pos, p + new Vector3(0f, 0f, -78f)) < 20f) continue;  // проход к гавани
                int floors = 2 + (i % 2);
                House(root, "Дом", pos, new Vector3(9f + (i % 3) * 2f, floors, 9f), i % 2 == 0 ? "Plaster" : "Wood", i % 3 == 0 ? "RoofRed" : "RoofBlue", p);
            }

            Lamps(root, p, 26f, 10);
            Lamps(root, p, 60f, 16);
            Trees(root, p, 88f, 104f, 22, 11);
        }

        static void BuildAcademy(Transform world)
        {
            var root = Island(world, "Академия", "Замок, аудитории, оранжерея, обсерватория", Academy, AcademyR, "Grass", "Rock");
            var p = Academy;

            // Замок: корпус 50 × 24 × 30 м и центральная башня 45 м.
            var castle = new GameObject("Замок").transform;
            castle.SetParent(root, false);
            Box(castle, "Корпус", p + new Vector3(0f, 12f, 30f), new Vector3(50f, 24f, 30f), "Stone");
            Box(castle, "Главная башня", p + new Vector3(0f, 22f, 30f), new Vector3(14f, 44f, 14f), "Stone");
            Roof(castle, p + new Vector3(0f, 44f, 30f), new Vector3(17f, 12f, 17f), "RoofBlue");
            Roof(castle, p + new Vector3(0f, 24f, 30f), new Vector3(52f, 6f, 32f), "RoofBlue");
            Box(castle, "Ворота", p + new Vector3(0f, 2.5f, 14.9f), new Vector3(5f, 5f, 0.4f), "DarkWood");
            Door(root, "Академия", "Аудитории: уроки заклинаний (рисуешь руну на время).", p + new Vector3(0f, 0f, 13f));

            // Две каменные башни (GLB) по бокам от ворот, 45 м.
            for (int s = -1; s <= 1; s += 2)
            {
                var pos = p + new Vector3(s * 34f, 0f, 22f);
                var t = Model(root, "stone-tower.glb", s < 0 ? "Западная башня" : "Восточная башня", pos, s * 18f, 45f, true);
                if (t == null)
                {
                    Cyl(root, "Башня", pos + Vector3.up * 22f, new Vector3(10f, 22f, 10f), "Stone");
                    Roof(root, pos + Vector3.up * 44f, new Vector3(12f, 10f, 12f), "RoofBlue");
                }
            }

            // Оранжерея и обсерватория.
            Building(root, "Оранжерея", "Грядки для редких ингредиентов.", p + new Vector3(-42f, 0f, -18f), new Vector3(20f, 2, 12f), "Glass", "RoofGreen", p);
            Cyl(root, "Обсерватория", p + new Vector3(44f, 8f, -16f), new Vector3(12f, 8f, 12f), "Stone");
            var dome = Sphere(root, "Купол", p + new Vector3(44f, 16f, -16f), Vector3.one * 12f, "RoofBlue");
            RemoveCollider(dome);
            Door(root, "Обсерватория", "Карта звёзд и ночные события.", p + new Vector3(44f, 0f, -9.5f));

            Disc(root, "Двор", p + new Vector3(0f, 0.02f, -6f), 22f, "Cobble");
            Lamps(root, p + new Vector3(0f, 0f, -6f), 24f, 8);
            Trees(root, p, 55f, 84f, 18, 21);

            // Кот-Хранитель Мрак гуляет по двору.
            Cat(root, p + new Vector3(6f, 0f, 2f), new[]
            {
                p + new Vector3(6f, 0f, 2f), p + new Vector3(-10f, 0f, -4f),
                p + new Vector3(-4f, 0f, -18f), p + new Vector3(12f, 0f, -14f),
            });
        }

        static void BuildHomes(Transform world)
        {
            var root = Island(world, "Жилой берег", "Дома игроков и участки под бизнесы", Homes, HomesR, "Grass", "Rock");
            var p = Homes;
            int n = 0;
            for (int gx = -1; gx <= 1; gx++)
                for (int gz = -1; gz <= 1; gz++)
                {
                    if (gx == 0 && gz == 0) continue;
                    var pos = p + new Vector3(gx * 34f, 0f, gz * 34f);
                    n++;
                    if (n % 2 == 0)
                    {
                        var plot = Box(root, "Участок под бизнес", pos + Vector3.up * 0.05f, new Vector3(16f, 0.1f, 16f), "Dirt");
                        var sign = Box(root, "Табличка «Продаётся»", pos + new Vector3(-6f, 1.2f, -8.5f), new Vector3(2.2f, 1.2f, 0.15f), "Wood");
                        sign.rotation = Quaternion.LookRotation(Vector3.back);
                        Door(root, "Участок под бизнес", "Здесь можно купить и прокачать бизнес.", pos + new Vector3(0f, 0f, -9f));
                        continue;
                    }
                    Building(root, "Дом игрока", "Твой дом: мебель, питомцы, гардероб.", pos, new Vector3(11f, 2, 10f), "Plaster", "RoofRed", p);
                }
            Building(root, "Зверинец", "Магические питомцы: кормление и ивентовый питомец месяца.", p, new Vector3(18f, 2, 14f), "Wood", "RoofGreen", Center);
            Lamps(root, p, 18f, 8);
            Trees(root, p, 55f, 88f, 20, 31);
        }

        static void BuildMists(Transform world)
        {
            var root = Island(world, "Туманные Рубежи", "Дикие места: редкие ингредиенты и существа", Mists, MistsR, "DarkGrass", "Rock");
            var p = Mists;
            var rnd = new System.Random(41);
            for (int i = 0; i < 26; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float r = 10f + (float)rnd.NextDouble() * (MistsR - 18f);
                var pos = p + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                float s = 2f + (float)rnd.NextDouble() * 6f;
                var rock = Sphere(root, "Скала", pos + Vector3.up * s * 0.3f, new Vector3(s, s * 0.8f, s * 1.2f), "Rock");
                rock.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
            }
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f;
                var pos = p + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (25f + (i % 3) * 18f);
                var c = Box(root, "Кристалл-ингредиент", pos + Vector3.up * 1.6f, new Vector3(0.7f, 1.6f, 0.7f), "Glow");
                c.rotation = Quaternion.Euler(20f, i * 40f, 15f);
                c.gameObject.AddComponent<FloatingMotion>().Configure(0.25f, 1.3f, 40f);
                RemoveCollider(c);
            }
            Trees(root, p, 20f, 85f, 30, 51, "DarkLeaves");
        }

        static void BuildHarbor(Transform world)
        {
            var root = Island(world, "Облачная гавань", "Пристань, гонки на мётлах, грифоны", Harbor, HarborR, "Grass", "Rock");
            var p = Harbor;
            // Пристань уходит за край острова.
            Box(root, "Пристань", p + new Vector3(0f, -0.2f, -HarborR - 12f), new Vector3(10f, 0.5f, 40f), "Wood");
            Building(root, "Чайная", "Отдых и разговоры с жителями.", p + new Vector3(-24f, 0f, 10f), new Vector3(12f, 2, 10f), "Wood", "RoofRed", p);
            Building(root, "Гнездо грифонов", "Прокат грифонов (этап «Механики»).", p + new Vector3(26f, 0f, 6f), new Vector3(16f, 2, 12f), "Wood", "RoofGreen", p);

            // Кольца гоночной трассы вокруг гавани.
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                var pos = p + new Vector3(Mathf.Cos(a) * 120f, 25f + Mathf.Sin(a * 2f) * 10f, Mathf.Sin(a) * 120f);
                var tangent = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                Ring(root, "Кольцо трассы", pos, tangent, 6f, "Glow");
            }
            Lamps(root, p, 20f, 8);
        }

        // ---------------------------------------------------------------- игрок, камера, интерфейс

        static Transform BuildPlayer(Vector3 pos, out PlayerMotor motor, out PlayerInputReader reader)
        {
            var go = new GameObject("Player") { tag = "Player" };
            go.transform.position = pos;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.7f; cc.radius = 0.35f; cc.center = new Vector3(0f, 0.85f, 0f);
            cc.stepOffset = 0.45f; cc.slopeLimit = 50f;
            reader = go.AddComponent<PlayerInputReader>();
            motor = go.AddComponent<PlayerMotor>();

            var body = new GameObject("Body").transform;
            body.SetParent(go.transform, false);
            var robe = Prim(PrimitiveType.Capsule, body, "Мантия", new Vector3(0f, 0.75f, 0f), new Vector3(0.7f, 0.75f, 0.7f), "Robe");
            RemoveCollider(robe);
            var head = Prim(PrimitiveType.Sphere, body, "Голова", new Vector3(0f, 1.55f, 0f), Vector3.one * 0.38f, "Skin");
            RemoveCollider(head);
            var hat = MeshObj(body, "Шляпа", coneMesh, new Vector3(0f, 1.72f, 0f), new Vector3(0.5f, 0.55f, 0.5f), "Robe", false);
            hat.localRotation = Quaternion.Euler(-8f, 0f, 0f);

            var broom = new GameObject("Broom").transform;
            broom.SetParent(body, false);
            var stick = Prim(PrimitiveType.Cylinder, broom, "Черенок", new Vector3(0f, 0.55f, 0.1f), new Vector3(0.06f, 0.9f, 0.06f), "Wood");
            stick.localRotation = Quaternion.Euler(90f, 0f, 0f);
            RemoveCollider(stick);
            var bristles = MeshObj(broom, "Прутья", coneMesh, new Vector3(0f, 0.55f, -0.85f), new Vector3(0.35f, 0.6f, 0.35f), "Straw", false);
            bristles.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return go.transform;
        }

        static Camera BuildCamera(Transform player, PlayerInputReader reader, PlayerMotor motor)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.78f, 0.95f);
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 1400f;
            cam.fieldOfView = 55f;
            go.AddComponent<AudioListener>();
            go.transform.position = player.position + new Vector3(0f, 4f, -8f);
            go.AddComponent<FollowCamera>().Configure(player, reader, motor);
            return cam;
        }

        static void BuildUi(PlayerInputReader reader)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)canvasGo.transform;

            // Камера пальцем — правые 60% экрана.
            var look = Rect(root, "Зона камеры", new Vector2(0.4f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            AddImage(look, new Color(0f, 0f, 0f, 0f), null);
            var lookArea = look.gameObject.AddComponent<TouchLookArea>();

            // Джойстик — левая нижняя часть экрана.
            var zone = Rect(root, "Зона джойстика", new Vector2(0f, 0f), new Vector2(0.45f, 0.5f), Vector2.zero, Vector2.zero);
            AddImage(zone, new Color(0f, 0f, 0f, 0f), null);
            var bg = Rect(zone, "Основание", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-20f, -120f), new Vector2(280f, 280f));
            AddImage(bg, new Color(1f, 1f, 1f, 0.25f), Knob()).raycastTarget = false;
            var knob = Rect(bg, "Ручка", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 130f));
            AddImage(knob, new Color(1f, 1f, 1f, 0.7f), Knob()).raycastTarget = false;
            var stick = zone.gameObject.AddComponent<VirtualJoystick>();
            stick.Configure(bg, knob, 120f);

            // Кнопки справа внизу.
            var action = Button(root, "Действие", new Vector2(-170f, 300f), 210f, new Color(1f, 0.82f, 0.35f, 0.9f));
            var jump = Button(root, "Прыжок\nвверх", new Vector2(-410f, 220f), 160f, new Color(0.55f, 0.8f, 1f, 0.85f));
            var down = Button(root, "Вниз", new Vector2(-410f, 420f), 130f, new Color(0.55f, 0.8f, 1f, 0.7f));
            var broom = Button(root, "Метла", new Vector2(-170f, 540f), 160f, new Color(0.75f, 0.6f, 1f, 0.9f));
            reader.Configure(stick, lookArea, jump, down, broom, action);

            // Подписи.
            var title = Label(root, "Название острова", new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1000f, 90f), 58, FontStyle.Bold);
            var sub = Label(root, "Подзаголовок", new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(1000f, 60f), 32, FontStyle.Normal);
            var hint = Label(root, "Подсказка", new Vector2(0.5f, 0f), new Vector2(0f, 720f), new Vector2(900f, 70f), 36, FontStyle.Bold);
            hint.color = new Color(1f, 0.9f, 0.55f);
            var toast = Label(root, "Сообщение", new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(940f, 220f), 36, FontStyle.Normal);
            canvasGo.AddComponent<Hud>().Configure(title, sub, hint, toast);
            title.text = "Лунный Перекрёсток";
        }

        // ---------------------------------------------------------------- строительные блоки мира

        static Transform Island(Transform world, string name, string subtitle, Vector3 top, float radius, string grass, string rock)
        {
            var root = new GameObject(name).transform;
            root.SetParent(world, false);
            root.position = top;
            root.gameObject.AddComponent<IslandZone>().Configure(name, subtitle, radius);

            var ground = Prim(PrimitiveType.Cylinder, root, "Земля", top + Vector3.down * 2f, new Vector3(radius * 2f, 2f, radius * 2f), grass, true);
            ReplaceWithMeshCollider(ground);
            var under = MeshObj(root, "Скала под островом", coneMesh, top + Vector3.down * 4f, new Vector3(radius * 2f, radius * 0.9f, radius * 2f), rock, true);
            under.localRotation = Quaternion.Euler(180f, 0f, 0f);
            return root;
        }

        static void Bridge(Transform world, Vector3 a, float ra, Vector3 b, float rb)
        {
            Vector3 flat = b - a; flat.y = 0f;
            Vector3 dir = flat.normalized;
            Vector3 start = a + dir * (ra - 6f);
            Vector3 end = b - dir * (rb - 6f);
            start.y = a.y; end.y = b.y;
            Vector3 mid = (start + end) * 0.5f;
            float len = Vector3.Distance(start, end);
            var rot = Quaternion.LookRotation(end - start, Vector3.up);

            var bridge = new GameObject("Мост").transform;
            bridge.SetParent(world, false);
            var deck = Box(bridge, "Настил", mid - rot * Vector3.up * 0.25f, new Vector3(6f, 0.5f, len), "Wood");
            deck.rotation = rot;
            for (int s = -1; s <= 1; s += 2)
            {
                var rail = Box(bridge, "Перила", mid + rot * new Vector3(s * 2.9f, 0.9f, 0f), new Vector3(0.2f, 0.2f, len), "DarkWood");
                rail.rotation = rot;
                int posts = Mathf.Max(2, Mathf.RoundToInt(len / 5f));
                for (int i = 0; i <= posts; i++)
                {
                    var pp = Vector3.Lerp(start, end, i / (float)posts) + rot * new Vector3(s * 2.9f, 0.45f, 0f);
                    Box(bridge, "Столбик", pp, new Vector3(0.25f, 0.9f, 0.25f), "DarkWood");
                }
            }
        }

        /// <summary>Здание с дверью и входом. Дверь смотрит в сторону faceTowards.</summary>
        static Transform Building(Transform parent, string name, string inside, Vector3 groundPos, Vector3 sizeFloors, string wall, string roof, Vector3 faceTowards)
        {
            var house = House(parent, name, groundPos, sizeFloors, wall, roof, faceTowards);
            Door(parent, name, inside, groundPos - house.forward * (sizeFloors.z * 0.5f + 1.5f));
            return house;
        }

        /// <summary>Точка входа перед моделью со стороны faceTowards.</summary>
        static Vector3 DoorInFront(Transform model, Vector3 faceTowards)
        {
            var b = WorldBounds(model);
            Vector3 dir = faceTowards - model.position; dir.y = 0f;
            dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.back;
            return model.position + dir * (Mathf.Max(b.extents.x, b.extents.z) + 1.5f);
        }

        static Transform House(Transform parent, string name, Vector3 groundPos, Vector3 sizeFloors, string wall, string roof, Vector3 faceTowards)
        {
            // sizeFloors: x — ширина, y — число этажей (по 3,5 м), z — глубина. Дверь на стороне -z.
            float h = sizeFloors.y * 3.5f;
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.position = groundPos;
            Box(root, "Стены", groundPos + Vector3.up * (h * 0.5f), new Vector3(sizeFloors.x, h, sizeFloors.z), wall);
            Roof(root, groundPos + Vector3.up * h, new Vector3(sizeFloors.x + 1.2f, Mathf.Max(3f, sizeFloors.x * 0.35f), sizeFloors.z + 1.2f), roof);
            Box(root, "Дверь", groundPos + new Vector3(0f, 1.25f, -sizeFloors.z * 0.5f - 0.05f), new Vector3(1.6f, 2.5f, 0.15f), "DarkWood");
            for (int f = 0; f < (int)sizeFloors.y; f++)
                for (int w = -1; w <= 1; w += 2)
                    Box(root, "Окно", groundPos + new Vector3(w * sizeFloors.x * 0.28f, 1.9f + f * 3.5f, -sizeFloors.z * 0.5f - 0.05f), new Vector3(1.3f, 1.5f, 0.1f), "Window");
            Vector3 away = groundPos - faceTowards; away.y = 0f;
            if (away.sqrMagnitude > 0.01f) root.rotation = Quaternion.LookRotation(away.normalized);
            return root;
        }

        static void Door(Transform parent, string name, string inside, Vector3 pos)
        {
            var go = new GameObject("Вход: " + name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.AddComponent<BuildingEntrance>().Configure(name, inside);
        }

        static Transform Model(Transform parent, string file, string name, Vector3 groundPos, float yaw, float height, bool collide)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Models + file);
            if (asset == null)
            {
                Debug.LogWarning($"Модель {file} не найдена или не импортировалась (нужен пакет glTFast). Ставлю заготовку.");
                return null;
            }

            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.position = groundPos;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);

            var inst = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            if (inst == null) inst = Object.Instantiate(asset);
            inst.name = file;
            inst.transform.SetParent(root, false);

            FitToHeight(root, inst.transform, height);
            if (collide)
                foreach (var mf in inst.GetComponentsInChildren<MeshFilter>())
                    if (mf.sharedMesh != null && mf.GetComponent<Collider>() == null)
                        mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            return root;
        }

        /// <summary>Масштабирует модель до нужной высоты и ставит её ногами на точку root, по центру.</summary>
        static void FitToHeight(Transform root, Transform model, float height)
        {
            var b = WorldBounds(model);
            if (b.size.y < 0.0001f) return;
            model.localScale *= height / b.size.y;
            b = WorldBounds(model);
            Vector3 shift = new Vector3(root.position.x - b.center.x, root.position.y - b.min.y, root.position.z - b.center.z);
            model.position += shift;
        }

        static Bounds WorldBounds(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(t.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static void Cat(Transform parent, Vector3 pos, Vector3[] points)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "reaper-cat.glb");
            var root = new GameObject("Кот-Хранитель Мрак").transform;
            root.SetParent(parent, false);
            root.position = pos;
            var body = new GameObject("Body").transform;   // анимируемая точка
            body.SetParent(root, false);

            if (asset != null)
            {
                var inst = PrefabUtility.InstantiatePrefab(asset) as GameObject;
                if (inst == null) inst = Object.Instantiate(asset);
                inst.transform.SetParent(body, false);
                FitToHeight(body, inst.transform, 1.3f);
            }
            else
            {
                var s = Prim(PrimitiveType.Sphere, body, "Кот (заготовка)", pos + Vector3.up * 0.6f, Vector3.one * 1.1f, "Robe");
                RemoveCollider(s);
            }
            root.gameObject.AddComponent<ReaperCatNpc>().Configure(body, points);
        }

        static void Lamps(Transform parent, Vector3 center, float radius, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f + 0.15f;
                var pos = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                Box(parent, "Фонарь", pos + Vector3.up * 1.6f, new Vector3(0.18f, 3.2f, 0.18f), "DarkWood");
                var l = Sphere(parent, "Огонёк", pos + Vector3.up * 3.4f, Vector3.one * 0.5f, "Glow");
                RemoveCollider(l);
            }
        }

        static void Trees(Transform parent, Vector3 center, float minR, float maxR, int count, int seed, string leaves = "Leaves")
        {
            var rnd = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float r = minR + (float)rnd.NextDouble() * (maxR - minR);
                var pos = center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                float s = 0.8f + (float)rnd.NextDouble() * 0.7f;
                Cyl(parent, "Ствол", pos + Vector3.up * 2.5f * s, new Vector3(0.6f * s, 2.5f * s, 0.6f * s), "Wood");
                var crown = Sphere(parent, "Крона", pos + Vector3.up * 6f * s, new Vector3(5f, 4.5f, 5f) * s, leaves);
                RemoveCollider(crown);
            }
        }

        static void Ring(Transform parent, string name, Vector3 pos, Vector3 forward, float radius, string mat)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.position = pos;
            root.rotation = Quaternion.LookRotation(forward);
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f;
                var seg = Box(root, "Сегмент", pos + root.rotation * new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius, new Vector3(0.5f, 2.4f, 0.5f), mat);
                seg.rotation = root.rotation * Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
                RemoveCollider(seg);
            }
        }

        static void SkyRocks(Transform world)
        {
            var root = new GameObject("Летающие камни").transform;
            root.SetParent(world, false);
            var rnd = new System.Random(7);
            for (int i = 0; i < 40; i++)
            {
                var pos = new Vector3((float)rnd.NextDouble() * 700f - 350f, -40f + (float)rnd.NextDouble() * 90f, (float)rnd.NextDouble() * 700f - 350f);
                bool nearIsland = Near(pos, Center, CenterR + 15f) || Near(pos, Academy, AcademyR + 15f) || Near(pos, Homes, HomesR + 15f)
                                  || Near(pos, Mists, MistsR + 15f) || Near(pos, Harbor, HarborR + 15f);
                if (nearIsland) continue;
                float s = 3f + (float)rnd.NextDouble() * 9f;
                var rock = MeshObj(root, "Камень", coneMesh, pos, new Vector3(s, s * 0.9f, s), "Rock", true);
                rock.localRotation = Quaternion.Euler(180f, (float)rnd.NextDouble() * 360f, 0f);
                rock.gameObject.AddComponent<FloatingMotion>().Configure(0.8f + (float)rnd.NextDouble(), 0.3f, 0f);
            }
        }

        static bool Near(Vector3 p, Vector3 c, float r)
        {
            Vector2 d = new Vector2(p.x - c.x, p.z - c.z);
            return d.magnitude < r;
        }

        static void SetupLighting()
        {
            var sun = new GameObject("Солнце");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.85f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.55f, 0.65f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.27f, 0.35f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.62f, 0.78f, 0.95f);
            RenderSettings.fogStartDistance = 220f;
            RenderSettings.fogEndDistance = 1100f;
            RenderSettings.sun = light;
        }

        // ---------------------------------------------------------------- примитивы и материалы

        static Transform Box(Transform p, string n, Vector3 pos, Vector3 size, string mat) => Prim(PrimitiveType.Cube, p, n, pos, size, mat, true);
        static Transform Sphere(Transform p, string n, Vector3 pos, Vector3 size, string mat) => Prim(PrimitiveType.Sphere, p, n, pos, size, mat, true);

        static Transform Cyl(Transform p, string n, Vector3 pos, Vector3 size, string mat)
        {
            var t = Prim(PrimitiveType.Cylinder, p, n, pos, size, mat, true);
            ReplaceWithMeshCollider(t);
            return t;
        }

        static void Disc(Transform p, string n, Vector3 pos, float radius, string mat)
        {
            var t = Prim(PrimitiveType.Cylinder, p, n, pos, new Vector3(radius * 2f, 0.02f, radius * 2f), mat, true);
            RemoveCollider(t);
        }

        static void Roof(Transform p, Vector3 basePos, Vector3 size, string mat)
        {
            MeshObj(p, "Крыша", pyramidMesh, basePos, size, mat, true);
        }

        static Transform Prim(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, string mat, bool world = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            if (world) go.transform.position = pos; else go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = Mat(mat);
            return go.transform;
        }

        static Transform MeshObj(Transform parent, string name, Mesh mesh, Vector3 pos, Vector3 scale, string mat, bool world)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            if (world) go.transform.position = pos; else go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = Mat(mat);
            return go.transform;
        }

        static void RemoveCollider(Transform t)
        {
            var c = t.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }

        /// <summary>У цилиндра Unity по умолчанию капсульный коллайдер — для плоского острова он выпуклый. Меняем на точный.</summary>
        static void ReplaceWithMeshCollider(Transform t)
        {
            RemoveCollider(t);
            var mf = t.GetComponent<MeshFilter>();
            if (mf != null) t.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }

        static readonly Dictionary<string, Color> Palette = new Dictionary<string, Color>
        {
            { "Grass", new Color(0.45f, 0.68f, 0.38f) },
            { "DarkGrass", new Color(0.27f, 0.36f, 0.33f) },
            { "Rock", new Color(0.42f, 0.38f, 0.45f) },
            { "Stone", new Color(0.72f, 0.7f, 0.68f) },
            { "Cobble", new Color(0.62f, 0.58f, 0.55f) },
            { "Plaster", new Color(0.93f, 0.88f, 0.78f) },
            { "Wood", new Color(0.55f, 0.38f, 0.24f) },
            { "DarkWood", new Color(0.32f, 0.21f, 0.14f) },
            { "RoofRed", new Color(0.72f, 0.3f, 0.27f) },
            { "RoofBlue", new Color(0.3f, 0.38f, 0.66f) },
            { "RoofGreen", new Color(0.3f, 0.52f, 0.42f) },
            { "RoofPurple", new Color(0.48f, 0.32f, 0.62f) },
            { "Window", new Color(1f, 0.86f, 0.5f) },
            { "Glass", new Color(0.7f, 0.9f, 0.88f) },
            { "Water", new Color(0.35f, 0.65f, 0.9f) },
            { "Glow", new Color(0.75f, 0.9f, 1f) },
            { "Leaves", new Color(0.33f, 0.58f, 0.32f) },
            { "DarkLeaves", new Color(0.2f, 0.32f, 0.3f) },
            { "Dirt", new Color(0.55f, 0.45f, 0.33f) },
            { "Robe", new Color(0.33f, 0.27f, 0.6f) },
            { "Skin", new Color(0.96f, 0.82f, 0.7f) },
            { "Straw", new Color(0.85f, 0.7f, 0.38f) },
        };

        static Material Mat(string name)
        {
            if (mats.TryGetValue(name, out var cached)) return cached;
            string path = $"{MatFolder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                m = new Material(shader);
                var c = Palette.TryGetValue(name, out var col) ? col : Color.magenta;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                m.color = c;
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", name == "Water" || name == "Glass" ? 0.8f : 0.15f);
                if (name == "Glow" || name == "Window")
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", c * 1.6f);
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                AssetDatabase.CreateAsset(m, path);
            }
            mats[name] = m;
            return m;
        }

        static Mesh SaveMesh(Mesh mesh, string name)
        {
            string path = $"{GenFolder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>Конус высотой 1 и диаметром 1, основание на y = 0.</summary>
        static Mesh MakeCone(int segments)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i < segments; i++)
            {
                float a0 = i / (float)segments * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
                var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f);
                var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f);
                int b = verts.Count;
                verts.Add(p0); verts.Add(Vector3.up); verts.Add(p1);       // бок
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                b = verts.Count;
                verts.Add(p0); verts.Add(p1); verts.Add(Vector3.zero);     // дно
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
            }
            var m = new Mesh { name = "Cone" };
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Четырёхскатная крыша: основание 1 × 1 на y = 0, конёк на y = 1.</summary>
        static Mesh MakePyramid()
        {
            var c = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f),
            };
            var top = new Vector3(0f, 1f, 0f);
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                int b = verts.Count;
                verts.Add(c[i]); verts.Add(top); verts.Add(c[(i + 1) % 4]);
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
            }
            var m = new Mesh { name = "Pyramid" };
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // ---------------------------------------------------------------- интерфейс

        static Sprite Knob() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        static Font UiFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static RectTransform Rect(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static Image AddImage(RectTransform rt, Color color, Sprite sprite)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            return img;
        }

        static HoldButton Button(RectTransform root, string label, Vector2 pos, float size, Color color)
        {
            var rt = Rect(root, "Кнопка " + label.Replace("\n", " "), new Vector2(1f, 0f), new Vector2(1f, 0f), pos, new Vector2(size, size));
            AddImage(rt, color, Knob());
            var t = Label(rt, "Текст", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size), Mathf.RoundToInt(size * 0.17f), FontStyle.Bold);
            t.text = label;
            t.color = new Color(0.15f, 0.12f, 0.25f);
            t.raycastTarget = false;
            Object.DestroyImmediate(t.GetComponent<Outline>());
            return rt.gameObject.AddComponent<HoldButton>();
        }

        static Text Label(RectTransform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, FontStyle style)
        {
            var rt = Rect(parent, name, anchor, anchor, pos, size);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UiFont();
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.color = Color.white;
            t.raycastTarget = false;
            var o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.1f, 0.08f, 0.2f, 0.8f);
            o.effectDistance = new Vector2(2f, -2f);
            return t;
        }
    }
}
