using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

// Monta os níveis 2 a 5 com as mesmas peças do nível 1 (chão, paredes, perigos, tarefas, cutscenes,
// floresta e pós-processamento). Cada fase parte de uma cópia da cena do nível 1 (câmera, luz, Nilo,
// Canvas, GameManager e música) e troca todo o cenário. Menu: Rastro do Dossel > Montar Nível N.
public static partial class ConstrutorNivel1
{
    private const string N2Path = "Assets/Scenes/Nivel2_MargemFerida.unity";
    private const string N3Path = "Assets/Scenes/Nivel3_CorredorDeCaca.unity";
    private const string N4Path = "Assets/Scenes/Nivel4_PostoDasRaizes.unity";
    private const string N5Path = "Assets/Scenes/Nivel5_AAntena.unity";
    private const string MenuPath = "Assets/Scenes/Menu.unity";

    private static Transform npcs;

    [MenuItem("Rastro do Dossel/Montar todas as fases")]
    public static void MontarTodas()
    {
        Montar(); MontarN2(); MontarN3(); MontarN4(); MontarN5();
        ConfigurarBuild();
        ConfigurarMenuFases();
        EditorSceneManager.OpenScene(ScenePath);
    }

    // ============================================================================================
    // Peças comuns

    // Copia a cena do nível 1 (se a fase ainda não existe), abre e limpa o cenário.
    private static UnityEngine.SceneManagement.Scene PrepararCena(string path, string pasta, int semente)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) AssetDatabase.CopyAsset(ScenePath, path);
        var scene = IniciarCena(path, "Locais_" + pasta, semente, semente + 1);
        foreach (var nome in new[] { "Estacao_Viveiro", "Estacao_Fase" })
        {
            var velho = GameObject.Find(nome);
            if (velho != null) Object.DestroyImmediate(velho);
        }
        npcs = Raiz("NPCs");
        return scene;
    }

    private static Transform RaizEstacao()
    {
        var r = GameObject.Find("Estacao_Fase");
        return r != null ? r.transform : Raiz("Estacao_Fase");
    }

    private static float[] R(float x0, float z0, float x1, float z1) { return new[] { x0, z0, x1, z1 }; }
    private static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }
    private static float Topo(GameObject g) { return Limites(g).max.y; }

    // Estação da fase: ponto de interação com som e detritos; a mudança visual vem nas listas.
    private static InteractableStation NovaEstacao(Vector3 pos, string prompt, string mensagem, string proximo, GameObject[] ativar, GameObject[] desativar)
    {
        var go = new GameObject("Estacao");
        go.transform.SetParent(RaizEstacao()); go.transform.position = pos; go.tag = "Interactable";
        var st = go.AddComponent<InteractableStation>();
        st.prompt = prompt; st.range = 2.2f; st.completedMessage = mensagem; st.nextObjective = proximo;
        st.enableOnComplete = ativar; st.disableOnComplete = desativar;
        st.onCompleted = new UnityEvent();
        var som = go.AddComponent<AudioSource>(); som.playOnAwake = false; som.spatialBlend = 0.6f;
        som.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AUD + "estacao_ok.wav");
        UnityEventTools.AddVoidPersistentListener(st.onCompleted, som.Play);

        var det = new GameObject("Detritos_Estacao");
        det.transform.SetParent(go.transform); det.transform.position = pos + Vector3.up * 0.8f;
        var ds = det.AddComponent<DebrisSpawner>();
        ds.debrisPrefabs = new[] { "Lasca", "Pedrinha", "Parafuso" }.Select(n => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Detritos/Detrito_" + n + ".prefab")).Where(p => p != null).ToArray();
        ds.sfx = Som(det, AUD + "caixa.wav");
        ds.dustFx = Poeira(det.transform, det.transform.position, "Poeira_Estacao", new Color(0.75f, 0.68f, 0.55f, 0.8f));
        UnityEventTools.AddVoidPersistentListener(st.onCompleted, ds.Burst);
        return st;
    }

    // Nuvem de poeira em rajada (não toca sozinha).
    private static ParticleSystem Poeira(Transform pai, Vector3 pos, string nome, Color cor)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai); go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.playOnAwake = false; main.loop = false; main.duration = 0.6f; main.startLifetime = 1.1f; main.startSpeed = 1.6f;
        main.startSize = 0.35f; main.startColor = cor; main.gravityModifier = 0.15f; main.maxParticles = 60; main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.5f;
        var rend = go.GetComponent<ParticleSystemRenderer>();
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Particula.mat");
        if (mat != null) rend.sharedMaterial = mat;
        return ps;
    }

    // Par de LEDs (desligado vermelho / ligado verde); devolve {desligado, ligado}.
    private static GameObject[] Led(Transform pai, string nome, Vector3 pos)
    {
        var off = Caixa(pai, nome + "_LED_Desligado", pos, V(0.16f, 0.16f, 0.06f), matLedVermelho);
        var on = Caixa(pai, nome + "_LED_Ligado", pos, V(0.16f, 0.16f, 0.06f), matLedVerde);
        on.SetActive(false);
        return new[] { off, on };
    }

    // Terminal de saída. comGabinete = false deixa só o ponto de interação (o visual vem da fase).
    private static LevelExit NovoTerminal(Vector3 pos, float yaw, string pendente, string prompt, bool comGabinete)
    {
        var t = new GameObject("Saida_Terminal");
        t.transform.SetParent(RaizEstacao()); t.transform.position = pos; t.transform.rotation = Quaternion.Euler(0f, yaw, 0f); t.tag = "Interactable";
        var ex = t.AddComponent<LevelExit>(); ex.prompt = prompt; ex.range = 2.2f; ex.pendingMessage = pendente;
        if (comGabinete)
        {
            var r = Quaternion.Euler(0f, yaw, 0f);
            Solido(ModeloAlvo("T10_Terminal_gabinete", PH + "utility_box_01/utility_box_01_1k.fbx", pos + r * V(0f, 0f, 1.15f), yaw, 1.35f, true, t.transform));
            var tela = Caixa(t.transform, "Terminal_Tela", pos + r * V(0f, 1.05f, 0.78f), V(0.55f, 0.36f, 0.04f), matLedVerde);
            tela.transform.rotation = r * Quaternion.Euler(-15f, 0f, 0f);
            Luz(luzes, "Luz_Terminal", pos + r * V(0f, 1.6f, 0.4f), new Color(0.6f, 1f, 0.75f), 1.6f, 7f);
        }
        return ex;
    }

    // Kit do viveiro (cerca de 8 m ao longo do x local): caixote com bandeja, lixeira, adubo, pá,
    // jardineira, vaso, regador, mangueira, aspersor e muda.
    private static void KitViveiro(Vector3 a, float yaw)
    {
        var r = Quaternion.Euler(0f, yaw, 0f);
        System.Func<float, float, Vector3> P = (x, z) => a + r * V(x, 0f, z);
        var cx = Solido(ModeloAlvo("P02_Caixote_madeira", PH + "wooden_crate_02/wooden_crate_02_1k.fbx", P(0f, 0f), yaw + 12f, 0.85f, false, apoio));
        var p0 = P(0f, 0f); p0.y = Topo(cx);
        ModeloAlvo("E02_Bandeja_mudas", PH + "seeding_tray_01/seeding_tray_01_1k.fbx", p0, yaw + 12f, 0.55f, false, recup, false);
        Solido(ModeloAlvo("E04_Lixeira", PH + "metal_trash_can/metal_trash_can_1k.fbx", P(1.1f, 0.2f), yaw, 0.9f, true, recup));
        Solido(ModeloAlvo("E06_Saco_adubo", PH + "compost_bag_02/compost_bag_02_1k.fbx", P(2.1f, 0.1f), yaw + 20f, 0.7f, true, recup));
        ModeloAlvo("E09_Pa", PH + "rusted_spade_01/rusted_spade_01_1k.fbx", P(2.9f, -0.3f) + Vector3.down * 0.12f, yaw + 25f, 1.2f, true, recup);
        Solido(ModeloAlvo("E08_Jardineira", PH + "planter_box_01/planter_box_01_1k.fbx", P(4.1f, 0.2f), yaw, 1.3f, false, recup));
        ModeloAlvo("E07_Vaso_barro", PH + "planter_pot_clay/planter_pot_clay_1k.fbx", P(5.1f, -0.4f), yaw, 0.4f, true, recup);
        ModeloAlvo("E03_Regador", PH + "watering_can_metal_01/watering_can_metal_01_1k.fbx", P(5.7f, 0.1f), yaw + 35f, 0.45f, false, recup);
        ModeloAlvo("E05_Mangueira", PH + "garden_hose_wall_mounted_01/garden_hose_wall_mounted_01_1k.fbx", P(6.3f, 0.3f), yaw, 0.55f, false, recup);
        ModeloAlvo("E10_Aspersor", PH + "garden_sprinkler_01/garden_sprinkler_01_1k.fbx", P(6.9f, -0.4f), yaw + 90f, 0.5f, false, recup);
        ModeloAlvo("E01_Muda_vaso", PH + "potted_plant_02/potted_plant_02_1k.fbx", P(7.6f, 0f), yaw, 0.75f, true, recup);
    }

    // Kit da oficina (cerca de 7 m): banco com rádio, lanterna e multímetro, barris, gerador, cabos,
    // caixote militar com caixa de força, caixa elétrica e tambor.
    private static void KitOficina(Vector3 a, float yaw)
    {
        var r = Quaternion.Euler(0f, yaw, 0f);
        System.Func<float, float, Vector3> P = (x, z) => a + r * V(x, 0f, z);
        var banco = Solido(ModeloAlvo("P05_Banco", PH + "painted_wooden_bench/painted_wooden_bench_1k.fbx", P(0f, 0f), yaw + 180f, 0.95f, true, apoio));
        float topo = Topo(banco);
        System.Func<float, float, Vector3> T = (x, z) => { var p = P(x, z); p.y = topo; return p; };
        ModeloAlvo("T01_Radio", PH + "vintage_radio_transceiver/vintage_radio_transceiver_1k.fbx", T(-0.3f, 0f), yaw + 180f, 0.45f, false, tec, false);
        ModeloAlvo("T03_Lanterna_sinal", PH + "signal_flashlight/signal_flashlight_1k.fbx", T(0.5f, -0.1f), yaw + 100f, 0.25f, false, tec, false);
        ModeloAlvo("T05_Multimetro", PH + "retro_multimeter/retro_multimeter_1k.fbx", T(0.2f, 0.25f), yaw + 160f, 0.24f, true, tec, false);
        Solido(ModeloAlvo("P04_Barris_madeira", PH + "wooden_barrels_01/wooden_barrels_01_1k.fbx", P(1.6f, 0.2f), yaw, 0.95f, true, apoio));
        Solido(ModeloAlvo("T04_Gerador", PH + "portable_generator/portable_generator_1k.fbx", P(2.8f, 0f), yaw + 200f, 0.85f, false, tec));
        ModeloAlvo("T06_Cabos", PH + "modular_electric_cables/modular_electric_cables_1k.fbx", P(3.6f, 0.3f), yaw, 1.4f, false, tec, false);
        var cm = Solido(ModeloAlvo("P01_Caixote_militar", PH + "wooden_military_crate/wooden_military_crate_1k.fbx", P(4.7f, 0.1f), yaw + 8f, 1.2f, false, apoio));
        var pc = P(4.7f, 0.1f); pc.y = Topo(cm);
        ModeloAlvo("T08_Caixa_forca", PH + "power_box_01/power_box_01_1k.fbx", pc, yaw + 8f, 0.5f, true, tec, false);
        Solido(ModeloAlvo("T10_Caixa_eletrica", PH + "utility_box_01/utility_box_01_1k.fbx", P(5.9f, 0.2f), yaw + 180f, 1.35f, true, tec));
        Solido(ModeloAlvo("P03_Tambor", PH + "barrel_03/barrel_03_1k.fbx", P(7f, 0f), yaw, 0.9f, true, apoio));
    }

    // Pedra de passagem (bloco do fundo do fosso até o nível da trilha) com uma rocha musgosa por cima das bordas.
    private static void PedraPassagem(int n, float[] r)
    {
        var c = V((r[0] + r[2]) / 2f, -1.5f, (r[1] + r[3]) / 2f);
        Laje(terreno, "Pedra_Passagem_" + n, c, V(r[2] - r[0], 3f, r[3] - r[1]), MaterialPedra());
    }

    // Pedra com a textura de paredão do Poly Haven (repetida a cada 2 m pela malha da laje).
    private static Material MaterialPedra()
    {
        const string path = "Assets/Materials/Pedra.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(PH + "rock_face_01/textures/rock_face_01_diff_1k.jpg"));
        m.SetColor("_BaseColor", new Color(0.82f, 0.86f, 0.8f));
        m.SetFloat("_Smoothness", 0.12f);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // Degrau: bloco de chão elevado (topo em altura).
    private static void Degrau(string nome, float[] r, float altura)
    {
        Laje(terreno, nome, V((r[0] + r[2]) / 2f, altura / 2f, (r[1] + r[3]) / 2f), V(r[2] - r[0], altura, r[3] - r[1]), matChao);
    }

    private static Material MaterialTransparente(string nome, Color cor, bool unlit)
    {
        string path = "Assets/Materials/" + nome + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", cor);
        m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        if (!unlit) m.SetFloat("_Smoothness", 0.9f);
        EditorUtility.SetDirty(m);
        return m;
    }

    // Prefabs das rochas que rolam na encosta do nível 4: corpo rígido na camada Debris (não empurra o Nilo)
    // e uma esfera de perigo um pouco maior, filha, na camada Hazard.
    private static GameObject[] PrefabsRochas()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Rochas")) AssetDatabase.CreateFolder("Assets/Prefabs", "Rochas");
        string[] fontes = { "boulder_01/boulder_01_1k.fbx|R01", "rock_07/rock_07_1k.fbx|R04", "stone_01/stone_01_1k.fbx|R09" };
        var lista = new List<GameObject>();
        bool registrar = registrarOcupacao; registrarOcupacao = false;
        for (int i = 0; i < fontes.Length; i++)
        {
            var f = fontes[i].Split('|');
            var raiz = new GameObject("Rocha_Rolante_" + (i + 1));
            raiz.layer = LayerMask.NameToLayer("Debris");
            var vis = ModeloAlvo(f[1] + "_Rocha_rolante", PH + f[0], Vector3.zero, 0f, 0.95f, false, raiz.transform, false);
            vis.transform.position -= Limites(vis).center;
            var col = raiz.AddComponent<SphereCollider>(); col.radius = 0.45f;
            var rb = raiz.AddComponent<Rigidbody>(); rb.mass = 60f; rb.interpolation = RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            var pz = new GameObject("Perigo");
            pz.transform.SetParent(raiz.transform, false);
            pz.layer = LayerMask.NameToLayer("Hazard"); pz.tag = "Hazard";
            var sc = pz.AddComponent<SphereCollider>(); sc.isTrigger = true; sc.radius = 0.62f;
            pz.AddComponent<Hazard>().mode = Hazard.Mode.Hit;
            string path = "Assets/Prefabs/Rochas/Rocha_Rolante_" + (i + 1) + ".prefab";
            lista.Add(PrefabUtility.SaveAsPrefabAsset(raiz, path));
            Object.DestroyImmediate(raiz);
        }
        registrarOcupacao = registrar;
        return lista.ToArray();
    }

    // Holofote da Vigília num ponto alto: gira de um lado a outro e a mancha de luz no chão tira um coração.
    private static void Holofote(Vector3 pos, float yaw, float alcance, float periodo, float amplitude)
    {
        var raiz = new GameObject("Holofote_Varredura");
        raiz.transform.SetParent(perigos); raiz.transform.position = pos; raiz.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        raiz.AddComponent<Rigidbody>();
        var v = raiz.AddComponent<Varredura>(); v.periodo = periodo; v.amplitude = amplitude;
        var fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        var alvo = V(pos.x, 0f, pos.z) + fwd * alcance;
        ModeloAlvo("T09_Holofote_varredura", PH + "portable_searchlight/portable_searchlight_1k.fbx", pos + Vector3.down * 0.65f, yaw + 180f, 0.8f, false, raiz.transform, false);
        var lg = new GameObject("Feixe_Luz");
        lg.transform.SetParent(raiz.transform); lg.transform.position = pos + Vector3.up * 0.2f;
        lg.transform.rotation = Quaternion.LookRotation(alvo - lg.transform.position);
        var l = lg.AddComponent<Light>(); l.type = LightType.Spot; l.range = alcance * 1.8f; l.spotAngle = 34f; l.innerSpotAngle = 20f;
        l.intensity = 14f; l.color = new Color(0.92f, 0.97f, 1f); l.shadows = LightShadows.None;
        var zona = new GameObject("Feixe_Chao");
        zona.transform.SetParent(raiz.transform); zona.transform.position = alvo + Vector3.up * 0.7f; zona.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        zona.layer = LayerMask.NameToLayer("Hazard"); zona.tag = "Hazard";
        var bc = zona.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = V(3.2f, 1.4f, 5f);
        zona.AddComponent<Hazard>().mode = Hazard.Mode.Hit;
        var mancha = Cilindro(zona.transform, "Mancha_Luz", alvo + Vector3.up * 0.06f, V(3.2f, 0.01f, 5f), Quaternion.Euler(0f, yaw, 0f), MaterialTransparente("Feixe", new Color(1f, 0.97f, 0.75f, 0.28f), true));
        SemSombra(mancha);
    }

    // Personagem em blocos parado (guardas da Vigília no nível 3).
    private static GameObject Boneco(string nome, string perfil, Vector3 pos, float yaw)
    {
        var g = new GameObject(nome);
        g.transform.SetParent(npcs); g.transform.position = pos; g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        ConstrutorNilo.MontarBoneco(g.transform, perfil, false);
        return g;
    }

    // Ivo: boneco com martelo, NavMeshAgent, rota de serviço e conversa.
    private static NpcRoute Ivo(Vector3[] pontos, float yaw, string[] falas)
    {
        var g = Boneco("Ivo", "ivo", pontos[0], yaw);
        g.tag = "Interactable";
        var pose = g.transform.Find("Pose");
        var bd = pose.Find("BracoD");
        var cabo = Caixa(bd, "Martelo_Cabo", Vector3.zero, V(0.05f, 0.05f, 0.42f), matMadeira);
        cabo.transform.localPosition = V(0f, -0.56f, 0.16f); cabo.transform.localRotation = Quaternion.identity;
        var cabeca = Caixa(bd, "Martelo_Cabeca", Vector3.zero, V(0.09f, 0.18f, 0.09f), matTerminal);
        cabeca.transform.localPosition = V(0f, -0.56f, 0.36f); cabeca.transform.localRotation = Quaternion.identity;
        var ag = g.AddComponent<NavMeshAgent>();
        ag.radius = 0.32f; ag.height = 2f; ag.speed = 1.8f; ag.angularSpeed = 360f; ag.acceleration = 6f; ag.stoppingDistance = 0.15f;
        var nr = g.AddComponent<NpcRoute>();
        var pts = new Transform[pontos.Length];
        string[] nomes = { "Abrigo", "Bancada", "Barreira", "Extra" };
        for (int i = 0; i < pontos.Length; i++)
        {
            var p = new GameObject("Ivo_Ponto_" + nomes[Mathf.Min(i, 3)]).transform;
            p.SetParent(npcs); p.position = pontos[i];
            pts[i] = p;
        }
        nr.pontos = pts;
        nr.pernaE = pose.Find("PernaE"); nr.pernaD = pose.Find("PernaD"); nr.bracoE = pose.Find("BracoE"); nr.bracoD = bd;
        var cv = g.AddComponent<Conversa>(); cv.falas = falas; cv.prompt = "E — conversar com Ivo"; cv.range = 2.3f; cv.virarPara = g.transform; cv.duracao = 5f;
        return nr;
    }

    // NavMesh só da área do posto (volume), gravado como asset ao lado da cena.
    private static void AssarNavMesh(string cenaPath, Vector3 min, Vector3 max)
    {
        var go = new GameObject("NavMesh_Posto");
        go.transform.SetParent(npcs);
        var s = go.AddComponent<NavMeshSurface>();
        s.collectObjects = CollectObjects.Volume;
        s.center = (min + max) / 2f; s.size = max - min;
        s.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        s.layerMask = LayerMask.GetMask("Ground", "Default");
        s.BuildNavMesh();
        string path = cenaPath.Replace(".unity", "_NavMesh.asset");
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(s.navMeshData, path);
        EditorUtility.SetDirty(s);
    }

    // Placa de madeira simples com texto implícito (forma primitiva da equipe).
    private static void Quadro(Transform pai, string nome, Vector3 pos, float yaw, Vector3 tam, Material mat)
    {
        var r = Quaternion.Euler(0f, yaw, 0f);
        for (int k = -1; k <= 1; k += 2)
            Caixa(pai, nome + "_Poste", pos + r * V(k * (tam.x / 2f - 0.05f), (tam.y + 0.9f) / 2f, 0f), V(0.1f, tam.y + 0.9f, 0.1f), matMadeira).transform.rotation = r;
        Caixa(pai, nome, pos + r * V(0f, 0.9f + tam.y / 2f, 0f), tam, mat).transform.rotation = r;
    }

    // ============================================================================================
    // Catálogo: as 70 entradas comuns devem aparecer em todas as fases. O que a fase ainda não usou
    // vai para a beira da trilha, fora do caminho, sem cobrir tomadas de cutscene.

    private static readonly string[] Catalogo =
    {
        "V06|Monguba|pachira_aquatica_01/pachira_aquatica_01_1k.fbx|5|h", "V08|Tronco_caido|dead_tree_trunk_02/dead_tree_trunk_02_1k.fbx|4.2|m",
        "V09|Toco|tree_stump_01/tree_stump_01_1k.fbx|0.6|h", "V10|Raizes|root_cluster_01/root_cluster_01_1k.fbx|2.6|h",
        "V12|Arbusto_folha_larga|shrub_04/shrub_04_1k.fbx|1.4|h", "V14|Anturio|anthurium_botany_01/anthurium_botany_01_1k.fbx|0.7|h",
        "V15|Calathea|calathea_orbifolia_01/calathea_orbifolia_01_1k.fbx|0.8|h", "V18|Raiz|single_root/single_root_1k.fbx|2.4|m",
        "V19|Musgo|moss_01/moss_01_1k.fbx|1.6|m", "V20|Galhos_secos|dry_branches_medium_01/dry_branches_medium_01_1k.fbx|1.2|m",
        "R01|Pedregulho|boulder_01/boulder_01_1k.fbx|2.2|m", "R02|Pedras_musgo|rock_moss_set_01/rock_moss_set_01_1k.fbx|3.6|m",
        "R03|Pedras_musgo|rock_moss_set_02/rock_moss_set_02_1k.fbx|3.4|m", "R04|Rocha_erodida|rock_07/rock_07_1k.fbx|4.6|m",
        "R05|Rocha_09|rock_09/rock_09_1k.fbx|5|m", "R06|Paredao|rock_face_01/rock_face_01_1k.fbx|11|m",
        "R07|Paredao|rock_face_02/rock_face_02_1k.fbx|9|m", "R08|Encosta|mountainside/mountainside_1k.fbx|60|m",
        "R09|Cascalho|stone_01/stone_01_1k.fbx|1.8|m", "R10|Rochas_tropicais|" + TGD + "Rochas/GroupRocks01.DAE|6|m",
        "C01|Pier_madeira|modular_wooden_pier/modular_wooden_pier_1k.fbx|8|m", "C02|Cabana_selva|" + TGD + "CabanaSelva/JungleHut01.DAE|5.6|m",
        "C03|Atracadouro|" + TGD + "Atracadouro/Tropical_Jetty01.DAE|6.5|m", "C04|Cerca_estacas|Assets/Terceiros/OpenGameArt/WoodKit1/Stick_1.fbx|1.2|h",
        "C05|Escada|wooden_ladder/wooden_ladder_1k.fbx|2.1|h", "C06|Barreira_Vigilia|concrete_road_barrier/concrete_road_barrier_1k.fbx|1.6|m",
        "C07|Fogueira|stone_fire_pit/stone_fire_pit_1k.fbx|1.5|m", "C08|Lampiao|wooden_lantern_01/wooden_lantern_01_1k.fbx|0.55|h",
        "C09|Bloqueio_Vigilia|concrete_road_barrier_02/concrete_road_barrier_02_1k.fbx|1.7|m", "C10|Alambrado|modular_chainlink_fence/modular_chainlink_fence_1k.fbx|2|m",
        "T01|Radio|vintage_radio_transceiver/vintage_radio_transceiver_1k.fbx|0.45|m", "T02|Sensor_camera|security_camera_01/security_camera_01_1k.fbx|0.5|m",
        "T03|Lanterna_sinal|signal_flashlight/signal_flashlight_1k.fbx|0.25|m", "T04|Gerador|portable_generator/portable_generator_1k.fbx|0.85|m",
        "T05|Multimetro|retro_multimeter/retro_multimeter_1k.fbx|0.24|h", "T06|Cabos|modular_electric_cables/modular_electric_cables_1k.fbx|1.4|m",
        "T07|Postes_energia|modular_electricity_poles/modular_electricity_poles_1k.fbx|8|h", "T08|Caixa_forca|power_box_01/power_box_01_1k.fbx|0.5|h",
        "T09|Holofote|portable_searchlight/portable_searchlight_1k.fbx|0.75|m", "T10|Caixa_eletrica|utility_box_01/utility_box_01_1k.fbx|1.35|h",
        "E01|Muda_vaso|potted_plant_02/potted_plant_02_1k.fbx|0.75|h", "E02|Bandeja_mudas|seeding_tray_01/seeding_tray_01_1k.fbx|0.55|m",
        "E03|Regador|watering_can_metal_01/watering_can_metal_01_1k.fbx|0.45|m", "E04|Lixeira|metal_trash_can/metal_trash_can_1k.fbx|0.9|h",
        "E05|Mangueira|garden_hose_wall_mounted_01/garden_hose_wall_mounted_01_1k.fbx|0.55|m", "E06|Saco_adubo|compost_bag_02/compost_bag_02_1k.fbx|0.7|h",
        "E07|Vaso_barro|planter_pot_clay/planter_pot_clay_1k.fbx|0.4|h", "E08|Jardineira|planter_box_01/planter_box_01_1k.fbx|1.3|m",
        "E09|Pa|rusted_spade_01/rusted_spade_01_1k.fbx|1.2|h", "E10|Aspersor|garden_sprinkler_01/garden_sprinkler_01_1k.fbx|0.5|m",
        "P01|Caixote_militar|wooden_military_crate/wooden_military_crate_1k.fbx|1.2|m", "P02|Caixote_madeira|wooden_crate_02/wooden_crate_02_1k.fbx|0.85|m",
        "P03|Tambor|barrel_03/barrel_03_1k.fbx|0.9|h", "P04|Barris_madeira|wooden_barrels_01/wooden_barrels_01_1k.fbx|0.95|h",
        "P05|Banco|painted_wooden_bench/painted_wooden_bench_1k.fbx|0.95|h",
        "A01|Estacas_guardadas|Assets/Terceiros/OpenGameArt/WoodKit1/Stick_2.fbx|1.1|h", "A02|Galho|dead_quiver_branch_02/dead_quiver_branch_02_1k.fbx|2.1|m",
        "A03|Tronco_armadilha|dead_tree_trunk/dead_tree_trunk_1k.fbx|3.5|m", "A04|Vigas|Assets/Terceiros/OpenGameArt/WoodKit1/Planks.fbx|2.4|m",
        "A05|Placa_perigo||0|p",
    };
    // vêm da floresta sorteada (Vegetacao), que roda depois
    private static readonly HashSet<string> DaFloresta = new HashSet<string> { "V01", "V02", "V03", "V04", "V05", "V07", "V11", "V13", "V16", "V17" };

    private static void GarantirCatalogo()
    {
        var nomes = new List<string>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)) nomes.Add(t.name);
        bool reg = registrarOcupacao; registrarOcupacao = true;
        var colocados = new List<string>();
        foreach (var linha in Catalogo)
        {
            var e = linha.Split('|');
            string id = e[0];
            if (DaFloresta.Contains(id) || nomes.Any(n => n.StartsWith(id + "_"))) continue;
            float alvo = float.Parse(e[3], System.Globalization.CultureInfo.InvariantCulture);
            float raio = e[4] == "p" ? 0.4f : Mathf.Min(alvo, 8f) * 0.5f;
            if (id == "R08") raio = 30f;   // encosta enorme: longe da trilha, achatada como no nível 1
            Vector3? lugar = null;
            for (int tent = 0; tent < 600 && lugar == null; tent++)
            {
                var r = Trilha[rng.Next(Trilha.Length)];
                int lado = rng.Next(4);
                float u = (float)rng.NextDouble();
                float afast = id == "R08" ? 45f + (float)rng.NextDouble() * 12f : raio + 0.7f + (float)rng.NextDouble() * 2.2f;
                float x = lado < 2 ? Mathf.Lerp(r[0], r[2], u) : (lado == 2 ? r[0] - afast : r[2] + afast);
                float z = lado >= 2 ? Mathf.Lerp(r[1], r[3], u) : (lado == 0 ? r[1] - afast : r[3] + afast);
                if (x < areaMata[0] + 2f || x > areaMata[2] - 2f || z < areaMata[1] + 2f || z > areaMata[3] - 2f) continue;
                if (DistanciaTrilha(x, z) < raio + 0.5f || NaAgua(x, z, Mathf.Min(raio, 3f)) || Ocupado(x, z, Mathf.Min(raio, 3f))) continue;
                lugar = V(x, 0f, z);
            }
            if (lugar == null) { Debug.LogWarning("Catálogo: sem lugar para " + id); continue; }
            var pos = lugar.Value;
            float yaw = (float)rng.NextDouble() * 360f;
            if (e[4] == "p") { Placa(cenario, id + "_" + e[1], pos); colocados.Add(id); continue; }
            string path = e[2].StartsWith("Assets/") ? e[2] : PH + e[2];
            Transform pai = id[0] == 'V' ? (id == "V19" ? rochas : veg) : id[0] == 'R' ? rochas : id[0] == 'C' ? constr : id[0] == 'T' ? tec : id[0] == 'E' ? recup : id[0] == 'P' ? apoio : armad;
            if (id == "R08")
            {
                var enc = Modelo(id + "_" + e[1], path, pos, yaw, 1.76f, rochas);
                enc.transform.localScale = V(1.76f, 0.52f, 1.33f);
                colocados.Add(id); continue;
            }
            var go = ModeloAlvo(id + "_" + e[1], path, pos, yaw, alvo, e[4] == "h", pai);
            if (id == "T02") { go.transform.position += Vector3.up * 2.4f; Caixa(cenario, "Poste_camera_catalogo", pos + Vector3.up * 1.2f, V(0.12f, 2.4f, 0.12f), matTerminal); }
            if (id == "C08") { go.transform.position += Vector3.up * 2.2f; Caixa(cenario, "Poste_lampiao_catalogo", pos + Vector3.up * 1.1f, V(0.14f, 2.2f, 0.14f), matMadeira); }
            colocados.Add(id);
        }
        registrarOcupacao = reg;
        Debug.Log("Catálogo completado com " + colocados.Count + " entradas: " + string.Join(", ", colocados));
    }

    // ============================================================================================
    // Nível 2 — Margem ferida

    [MenuItem("Rastro do Dossel/Montar Nível 2")]
    public static void MontarN2()
    {
        var scene = PrepararCena(N2Path, "N2", 21);
        Trilha = new[] { R(-4, -8, 4, 34), R(-4, 58, 4, 66), R(-4, 66, 22, 74), R(24, 66, 32, 74), R(34.4f, 66, 44, 74), R(36, 74, 44, 96), R(30, 96, 66, 110), R(66, 100, 76, 106) };
        Fossos = new[] { R(-4, 34, 4, 58), R(22, 66, 24, 74), R(32, 66, 34.4f, 74) };
        Aguas = new[] { R(-28, -20, -9, 80), R(-9, 34, -4, 58), R(4, 34, 12, 58), R(22, 58, 24, 66), R(22, 74, 24, 82), R(32, 58, 34.4f, 66), R(32, 74, 34.4f, 82) };
        areaMata = new[] { -60f, -50f, 150f, 160f };
        Chao(true);
        registrarOcupacao = true;

        // ---------- A: margem e passarela velha ----------
        ModeloAlvo("C03_Atracadouro", TGD + "Atracadouro/Tropical_Jetty01.DAE", V(-11.5f, 0f, 2f), 90f, 6.5f, false, constr);
        for (int i = 0; i < 7; i++)
        {
            if (i == 3 || i == 5) continue;   // tábuas que faltam
            var tabua = Caixa(locais, "N2-01_Passarela_quebrada_Tabua_" + (i + 1), V(-5f - i * 1.15f, 0.25f, 14f), V(1.05f, 0.08f, 1.6f), matMadeira);
            if (i == 4) tabua.transform.rotation = Quaternion.Euler(0f, 8f, 18f);
            Ocupar(tabua);
            if (i % 2 == 0) for (int k = -1; k <= 1; k += 2) Caixa(locais, "N2-01_Passarela_quebrada_Poste", V(-5f - i * 1.15f, -0.9f, 14f + k * 0.75f), V(0.12f, 2.4f, 0.12f), matMadeira);
        }
        // barco comunitário encalhado na beira do rio
        var barco = new GameObject("N2-04_Barco_encalhado").transform; barco.SetParent(locais); barco.position = V(-11f, -0.25f, 26f);
        Caixa(barco, "N2-04_Barco_Fundo", barco.position, V(1.4f, 0.2f, 4f), matMadeira);
        for (int k = -1; k <= 1; k += 2) Caixa(barco, "N2-04_Barco_Lateral", barco.position + V(k * 0.7f, 0.3f, 0f), V(0.1f, 0.5f, 4f), matMadeira);
        Caixa(barco, "N2-04_Barco_Proa", barco.position + V(0f, 0.3f, 2f), V(1.4f, 0.5f, 0.1f), matMadeira);
        Caixa(barco, "N2-04_Barco_Banco", barco.position + V(0f, 0.35f, -0.4f), V(1.3f, 0.06f, 0.35f), matMadeira);
        barco.rotation = Quaternion.Euler(0f, 24f, -9f);
        Ocupar(barco.gameObject);
        // marco de nível do rio: faixas alternadas
        for (int k = 0; k < 6; k++)
            Caixa(locais, "N2-05_Marco_nivel_" + (k + 1), V(-10.4f, -2.1f + k * 0.55f, 44f), V(0.22f, 0.55f, 0.22f), k % 2 == 0 ? matMarco : matPerigo);
        Placa(cenario, "A05_Placa_perigo_1", V(3.4f, 0f, 31.5f));
        Luz(luzes, "Luz_Margem", V(-6f, 2.5f, 20f), new Color(0.6f, 0.75f, 1f), 0.6f, 12f);

        // ---------- pedras de passagem sobre o rio ----------
        var pedras = new[] { R(-3.2f, 36.2f, 0.4f, 39.6f), R(-0.6f, 41.6f, 3.2f, 45.2f), R(-3.4f, 47.4f, 0.2f, 51f), R(-1f, 53f, 3f, 56f) };
        for (int i = 0; i < pedras.Length; i++)
        {
            PedraPassagem(i + 1, pedras[i]);
            var r = pedras[i];
            SemSombra(ModeloAlvo("R02_Pedras_musgo_passagem_" + (i + 1), PH + "rock_moss_set_01/rock_moss_set_01_1k.fbx", V(r[0] - 0.25f, -1.15f, (r[1] + r[3]) / 2f), 70f * i, 1.4f, false, rochas, false));
        }
        Fosso(perigos, armad, 1, Fossos[0], V(0f, 0f, 31.5f), false);

        // ---------- B/C: bloqueio à vista, galho baixo e dois canais ----------
        var ponte = new GameObject("Travessia_Principal").transform; ponte.SetParent(cenario);
        Caixa(ponte, "Ponte_Tabuleiro", V(-18.5f, 0.35f, 66f), V(19f, 0.25f, 3f), matMadeira);
        for (int i = 0; i < 5; i++) for (int k = -1; k <= 1; k += 2) Caixa(ponte, "Ponte_Pilar", V(-27f + i * 4.2f, -1f, 66f + k * 1.4f), V(0.25f, 2.8f, 0.25f), matMadeira);
        ModeloAlvo("C09_Bloqueio_Vigilia", PH + "concrete_road_barrier_02/concrete_road_barrier_02_1k.fbx", V(-12f, 0.47f, 66f), 90f, 1.7f, false, constr, false);
        ModeloAlvo("C06_Barreira_Vigilia", PH + "concrete_road_barrier/concrete_road_barrier_1k.fbx", V(-13f, 0.47f, 65.2f), 80f, 1.6f, false, constr, false);
        for (int i = 0; i < 3; i++) Modelo("C10_Alambrado_ponte_" + (i + 1), PH + "modular_chainlink_fence/modular_chainlink_fence_1k.fbx", V(-14.2f, 0.47f, 64f + i * 2f), 90f, 1f, constr);
        Placa(ponte, "A05_Placa_perigo_ponte", V(-11f, 0.47f, 67.6f));
        Luz(luzes, "Luz_Bloqueio_Ponte", V(-12.5f, 2.6f, 66f), new Color(1f, 0.25f, 0.15f), 2.4f, 14f);
        Galho(1, V(12f, 0f, 70f), true);
        Fosso(perigos, armad, 2, Fossos[1], V(19f, 0f, 70f), false);
        Fosso(perigos, armad, 3, Fossos[2], V(29f, 0f, 70f), false);
        Placa(cenario, "A05_Placa_perigo_2", V(19.6f, 0f, 73.4f));
        // comporta pequena no canal de cima
        for (int k = -1; k <= 1; k += 2) Caixa(locais, "N2-02_Comporta_Poste", V(23f + k * 1.1f, -0.4f, 77f), V(0.22f, 2.6f, 0.22f), matMadeira);
        Caixa(locais, "N2-02_Comporta_Viga", V(23f, 0.95f, 77f), V(2.5f, 0.2f, 0.25f), matMadeira);
        Ocupar(Caixa(locais, "N2-02_Comporta_Tampa", V(23f, -0.35f, 77f), V(1.9f, 1.4f, 0.12f), matTerminal));
        Caixa(locais, "N2-02_Comporta_Volante", V(23f, 1.25f, 77f), V(0.5f, 0.5f, 0.06f), matPerigo);

        // ---------- D: pilão ----------
        Pilao(perigos, cenario, armad, V(40f, 0f, 84f));
        Modelo("T07_Postes_energia", PH + "modular_electricity_poles/modular_electricity_poles_1k.fbx", V(47.5f, 0f, 86f), 0f, 1f, tec);

        // ---------- E: estação de água (filtro, painel solar, reservatório) ----------
        var est = RaizEstacao();
        Caixa(est, "Filtro_Corpo", V(48f, 0.7f, 107.6f), V(1.6f, 1.4f, 1.1f), matTerminal, true);
        Caixa(est, "Filtro_Tampa", V(48f, 1.45f, 107.6f), V(1.7f, 0.12f, 1.2f), matMadeira);
        Cilindro(est, "Filtro_Cano", V(46f, 0.45f, 107.6f), V(0.22f, 1.2f, 0.22f), Quaternion.Euler(0f, 0f, 90f), matTerminal);
        var alavOff = Caixa(est, "Alavanca_Travada", V(48.6f, 1.2f, 106.98f), V(0.08f, 0.55f, 0.08f), matPerigo); alavOff.transform.rotation = Quaternion.Euler(0f, 0f, 35f);
        var alavOn = Caixa(est, "Alavanca_Liberada", V(48.6f, 1.2f, 106.98f), V(0.08f, 0.55f, 0.08f), matLedVerde); alavOn.transform.rotation = Quaternion.Euler(0f, 0f, -35f); alavOn.SetActive(false);
        var ledF = Led(est, "Filtro", V(47.5f, 1.15f, 107.03f));
        // painel solar: caído no chão -> no suporte
        for (int k = -1; k <= 1; k += 2) Caixa(est, "Painel_Suporte", V(51.6f + k * 0.8f, 0.8f, 107.9f), V(0.1f, 1.6f, 0.1f), matMadeira);
        var painelOff = Caixa(est, "Painel_Solar_Caido", V(51.4f, 0.12f, 106.2f), V(1.8f, 0.06f, 1.1f), matTerminal); painelOff.transform.rotation = Quaternion.Euler(4f, 25f, 6f);
        var painelOn = Caixa(est, "Painel_Solar_Montado", V(51.6f, 1.75f, 107.8f), V(1.8f, 0.06f, 1.1f), matTerminal); painelOn.transform.rotation = Quaternion.Euler(-30f, 0f, 0f); painelOn.SetActive(false);
        var luzPainel = Luz(est, "Luz_Painel", V(51.6f, 2.3f, 107.2f), new Color(0.5f, 0.8f, 1f), 1f, 5f); luzPainel.gameObject.SetActive(false);
        // reservatório transparente com nível baixo -> alto
        var vidro = MaterialTransparente("Vidro", new Color(0.75f, 0.88f, 1f, 0.22f), false);
        Cilindro(locais, "N2-03_Reservatorio", V(44f, 1.1f, 107.4f), V(1.9f, 1.1f, 1.9f), Quaternion.identity, vidro);
        Cilindro(locais, "N2-03_Reservatorio_Base", V(44f, 0.05f, 107.4f), V(2.1f, 0.05f, 2.1f), Quaternion.identity, matTerminal);
        var aguaBaixa = Cilindro(est, "Reservatorio_Agua_Baixa", V(44f, 0.35f, 107.4f), V(1.8f, 0.3f, 1.8f), Quaternion.identity, MaterialAgua());
        var aguaAlta = Cilindro(est, "Reservatorio_Agua_Alta", V(44f, 0.95f, 107.4f), V(1.8f, 0.9f, 1.8f), Quaternion.identity, MaterialAgua()); aguaAlta.SetActive(false);
        Caixa(est, "Placa_Estacao_Agua", V(48f, 2.2f, 108.3f), V(1.6f, 0.45f, 0.05f), matMadeira);
        NovaEstacao(V(48f, 0f, 105.6f), "E — destravar o filtro e ligar o painel solar",
            "Filtro destravado e painel solar no lugar: o reservatório volta a encher.\nMARA (rádio): O pessoal usa essa água na travessia.\nNILO: Vou liberar a estação. O resto precisa da equipe do posto.",
            "subir pela trilha até o terminal",
            new[] { alavOn, ledF[1], painelOn, aguaAlta, luzPainel.gameObject }, new[] { alavOff, ledF[0], painelOff, aguaBaixa });
        KitOficina(V(57f, 0f, 108.4f), 0f);
        KitViveiro(V(56.5f, 0f, 97.4f), 0f);
        PosteLampiao("C08_Lampiao_estacao_1", 41f, 108.6f, 2.4f);
        PosteLampiao("C08_Lampiao_estacao_2", 55f, 108.6f, 2.4f);
        ModeloAlvo("C01_Pier_madeira", PH + "modular_wooden_pier/modular_wooden_pier_1k.fbx", V(-12.5f, -1.7f, 52f), 90f, 8f, false, constr);
        ModeloAlvo("C02_Cabana_selva", TGD + "CabanaSelva/JungleHut01.DAE", V(60f, 0f, 115f), 180f, 5.6f, false, constr);
        ModeloAlvo("R07_Paredao_fim", PH + "rock_face_02/rock_face_02_1k.fbx", V(80f, 0f, 103f), 270f, 9f, false, rochas);
        var terminal = NovoTerminal(V(73.5f, 0f, 103f), 90f, "Religue a estação de água antes de subir.", "E — usar o terminal de saída", true);

        // ---------- tarefas, luzes, falas ----------
        Residuos(new[] { V(2.6f, 0f, 8f), V(-2.6f, 0f, 22f), V(6f, 0f, 72.4f), V(28f, 0f, 72.4f), V(42.6f, 0f, 79f), V(37.4f, 0f, 91f) });
        Sinais(new[] { V(-2.8f, 0f, 28f), V(16f, 0f, 67.2f) });
        Lanternas(new[] { new Vector2(0f, -8f), new Vector2(0f, 70f), new Vector2(40f, 70f), new Vector2(40f, 103f), new Vector2(70f, 103f) }, 4.75f);
        VagaLumes(new[] { V(-6.5f, 1.3f, 10f), V(6.5f, 1.3f, 24f), V(-6f, 1.3f, 46f), V(8f, 1.3f, 62f), V(18f, 1.3f, 77f), V(47f, 1.3f, 80f), V(33f, 1.3f, 92f), V(62f, 1.3f, 112f) });
        Gatilho(tutorial, "Dica_Residuos", V(0f, 1.5f, 3f), V(8f, 3f, 1.5f), "Resíduos na margem: chegue perto e use E para recolher (opcional).", 5f);
        Gatilho(tutorial, "Dica_Pedras", V(0f, 1.5f, 30f), V(8f, 3f, 2f), "Pedras de passagem: salte de uma para a outra.\nCorrer (Shift) alonga o salto. Cair no rio custa um coração.", 6f);
        Gatilho(tutorial, "Dica_Galho", V(8f, 1.5f, 70f), V(2f, 3f, 8f), "Galho baixo: Ctrl para deslizar.", 4f);
        Gatilho(tutorial, "Dica_Canais", V(17.5f, 1.5f, 70f), V(1.5f, 3f, 8f), "Dois canais seguidos: salte um, respire, salte o outro.", 4f);
        Gatilho(tutorial, "Dica_Pilao", V(40f, 1.5f, 78f), V(8f, 3f, 1.5f), "Pilão: passe quando ele subir.", 4f);
        Gatilho(tutorial, "Chegada_Estacao", V(40f, 1.5f, 95.5f), V(8f, 3f, 1.5f), "Área segura: estação de água da margem. Use E no filtro.", 5f);

        CriarUICutscene();
        var abertura = NovaCutscene("Abertura_N2", new[] {
            K(-6.5f, 7f, -6f,    0f, 0.5f, 30f,  1.2f),
            K(-7f, 3.2f, 33f,    0f, -0.5f, 46f, 4.5f),
            K(0.3f, 1.9f, 0.4f,  0f, 1.55f, -3f, 3.5f),
            K(0f, 4.6f, -4.2f,   0f, 1.0f, 20f,  3.0f),
        }, new[] {
            F(0.3f, 4.4f, "RASTRO DO DOSSEL\n<size=40>Nível 2 — Margem ferida</size>", true),
            F(1.2f, 5.2f, "<color=#9FE0FF>MARA (rádio):</color> O rio está perto. Tem uma estação de água parada na margem."),
            F(5.3f, 8.6f, "<color=#9FE0FF>MARA:</color> Se ela voltar a funcionar, o pessoal da travessia agradece."),
            F(8.7f, 12.2f, "<color=#FFC46B>NILO:</color> As pedras do rio ainda estão lá. Vou atravessar."),
        });
        abertura.tocarAoIniciar = true;
        var bloqueio = NovaCutscene("Bloqueio_N2", new[] {
            K(1.5f, 2.2f, 60.5f,  -4f, 1.2f, 64f,   0.5f),
            K(-2.5f, 3.2f, 63f,   -18f, 0.8f, 66f,  3.5f),
            K(-5f, 5.5f, 70f,     -16f, 0.6f, 66f,  3.8f),
            K(0f, 3f, 62f,        14f, 1f, 70f,     3.2f),
        }, new[] {
            F(0.3f, 3.8f, "<color=#FFC46B>NILO:</color> A travessia principal... já está fechada."),
            F(3.9f, 7.4f, "<color=#9FE0FF>MARA (rádio):</color> Fecharam antes do horário. Alguns moradores já voltaram para casa."),
            F(7.5f, 10.8f, "<color=#9FE0FF>MARA:</color> Os outros vão tentar passar ao amanhecer. Precisamos chegar à antena."),
        });
        var gBloq = Gatilho(tutorial, "Vista_Bloqueio", V(0f, 1.5f, 60f), V(8f, 3f, 1.5f), "", 0f);
        UnityEventTools.AddPersistentListener(gBloq.GetComponent<TutorialTrigger>().onEnter, bloqueio.TocarCena);
        var final = NovaCutscene("Final_N2", new[] {
            K(71f, 1.9f, 105.4f,  73.8f, 1.1f, 103f,  0.5f),
            K(70f, 3.6f, 99f,     46f, 1f, 106f,      3.4f),
            K(64f, 14f, 96f,      90f, 6f, 140f,      4.2f),
        }, new[] {
            F(0.3f, 3.6f, "<color=#FFC46B>NILO:</color> Estação ligada. O reservatório está enchendo."),
            F(3.7f, 7.2f, "<color=#9FE0FF>MARA (rádio):</color> Bom trabalho. A subida passa pelo corredor de caça..."),
            F(7.3f, 8.1f, "<color=#9FE0FF>MARA:</color> Cuidado, Sabiá."),
            F(6.5f, 8.1f, "FIM DO NÍVEL 2", true),
        });
        final.neblinaFim = 140f;
        terminal.cutsceneFinal = final;
        Marcadores();
        GarantirCatalogo();

        registrarOcupacao = false;
        Vegetacao(veg, new[] { -40f, -30f, 110f, 140f }, 400, 300);
        Finalizar(scene, Ambiente.NoiteRio, V(0f, 0.05f, -3f), 0f,
            "Nível 2 — Margem ferida", "atravessar a margem e religar a estação de água", 1,
            "Destravar o filtro e ligar o painel solar", "", "Subir pela trilha até o terminal",
            "Nível 2 concluído", "A estação da margem voltou a funcionar. Agora, o corredor de caça.", false,
            "Assets/Settings/PerfilPos_N2.asset");
    }

    // ============================================================================================
    // Nível 3 — Corredor de caça

    [MenuItem("Rastro do Dossel/Montar Nível 3")]
    public static void MontarN3()
    {
        var scene = PrepararCena(N3Path, "N3", 31);
        Trilha = new[] { R(-3, -8, 3, 30), R(-3, 31.8f, 3, 44), R(-6, 44, 6, 56), R(-3, 56, 3, 64), R(-3, 64, 40, 70), R(34, 70, 40, 90), R(26, 90, 52, 104), R(52, 96, 62, 104) };
        Fossos = new[] { R(-3, 30, 3, 31.8f) };
        Aguas = new float[0][];
        areaMata = new[] { -60f, -50f, 140f, 170f };
        Chao(false);
        registrarOcupacao = true;

        // ---------- A: entrada estreita entre paredões ----------
        ModeloAlvo("R06_Paredao_inicio", PH + "rock_face_01/rock_face_01_1k.fbx", V(0f, 0f, -11f), 0f, 11f, false, rochas);
        ModeloAlvo("R07_Paredao_A", PH + "rock_face_02/rock_face_02_1k.fbx", V(-8.5f, 0f, 8f), 90f, 9f, false, rochas);
        ModeloAlvo("R07_Paredao_B", PH + "rock_face_02/rock_face_02_1k.fbx", V(8.5f, 0f, 22f), 270f, 9f, false, rochas);
        ModeloAlvo("C09_Bloqueio_Vigilia", PH + "concrete_road_barrier_02/concrete_road_barrier_02_1k.fbx", V(-2.2f, 0f, 2f), 15f, 1.6f, false, constr);
        Caixa(cenario, "Simbolo_Vigilia_1", V(-3.3f, 1.8f, 6f), V(0.05f, 0.6f, 0.6f), matLedVermelho).transform.rotation = Quaternion.Euler(45f, 0f, 0f);
        Caixa(cenario, "Marca_Antiga_Nilo", V(3.3f, 1.6f, 7f), V(0.05f, 0.3f, 0.3f), matLenco).transform.rotation = Quaternion.Euler(45f, 0f, 0f);
        var tg1 = TroncoGiratorio(perigos, armad, V(0f, 0f, 20f)); tg1.degreesPerSecond = 55f;
        Caixa(locais, "N3-03_Suporte_tronco_Base", V(0f, 0.12f, 20f), V(1.3f, 0.24f, 1.3f), matRocha);
        Cilindro(locais, "N3-03_Suporte_tronco_Colar", V(0f, 1.6f, 20f), V(0.55f, 0.4f, 0.55f), Quaternion.identity, matTerminal);
        Luz(luzes, "Luz_Tronco_1", V(0f, 4.2f, 20f), new Color(1f, 0.6f, 0.3f), 1.6f, 10f);
        Placa(cenario, "A05_Placa_perigo_1", V(2.7f, 0f, 27.5f));
        Fosso(perigos, armad, 1, Fossos[0], V(0f, 0f, 27f));
        Galho(1, V(0f, 0f, 38f), false);

        // ---------- descanso ----------
        ModeloAlvo("C07_Fogueira", PH + "stone_fire_pit/stone_fire_pit_1k.fbx", V(-4.2f, 0f, 53.5f), 0f, 1.3f, false, constr);
        Luz(luzes, "Luz_Fogueira", V(-4.2f, 1f, 53.5f), new Color(1f, 0.55f, 0.2f), 2.6f, 11f);
        var toco = ModeloAlvo("V09_Toco", PH + "tree_stump_01/tree_stump_01_1k.fbx", V(-2.6f, 0f, 54.6f), 0f, 0.5f, true, veg);
        ColisorPorBounds(toco, "V09_Toco", 0.05f);

        // ---------- D: corredor vigiado (passarela, guardas, holofote, segundo tronco) ----------
        var pas = new GameObject("N3-02_Passarela_vigilancia").transform; pas.SetParent(locais);
        Caixa(pas, "N3-02_Passarela_vigilancia_Piso", V(15f, 4f, 75.5f), V(20f, 0.2f, 1.6f), matMadeira);
        Caixa(pas, "N3-02_Passarela_vigilancia_Grade", V(15f, 4.55f, 74.75f), V(20f, 0.08f, 0.08f), matMadeira);
        for (int i = 0; i <= 5; i++) for (int k = -1; k <= 1; k += 2) Caixa(pas, "N3-02_Passarela_vigilancia_Poste", V(5f + i * 4f, 2f, 75.5f + k * 0.7f), V(0.18f, 4f, 0.18f), matMadeira);
        ModeloAlvo("C05_Escada", PH + "wooden_ladder/wooden_ladder_1k.fbx", V(25.3f, 0f, 75.6f), 90f, 4.2f, true, constr);
        Ocupar(pas.gameObject);
        Boneco("Guarda_Vigilia_1", "guarda", V(8.5f, 4.1f, 75.8f), 180f);
        Boneco("Guarda_Vigilia_2", "guarda", V(21f, 4.1f, 75.8f), 200f);
        Luz(luzes, "Luz_Passarela", V(15f, 6f, 76.5f), new Color(0.85f, 0.95f, 1f), 1.4f, 12f);
        Luz(luzes, "Luz_Guarda_1", V(8.5f, 6.6f, 74.6f), new Color(1f, 0.85f, 0.7f), 1.6f, 5f);
        Luz(luzes, "Luz_Guarda_2", V(21f, 6.6f, 74.6f), new Color(1f, 0.85f, 0.7f), 1.6f, 5f);
        Holofote(V(14.5f, 4.75f, 75f), 180f, 8f, 6f, 45f);
        Modelo("T02_Sensor_camera", PH + "security_camera_01/security_camera_01_1k.fbx", V(5f, 4.3f, 74.8f), 160f, 1.22f, tec);
        var tg2 = TroncoGiratorio(perigos, armad, V(30f, 0f, 67f)); tg2.degreesPerSecond = 85f;
        Luz(luzes, "Luz_Tronco_2", V(30f, 4.2f, 67f), new Color(1f, 0.6f, 0.3f), 1.6f, 10f);
        for (int i = 0; i < 5; i++) Modelo("C10_Alambrado_corredor_" + (i + 1), PH + "modular_chainlink_fence/modular_chainlink_fence_1k.fbx", V(27f + i * 2f, 0f, 72.2f), 0f, 1f, constr);
        Pilao(perigos, cenario, armad, V(37f, 0f, 80f));
        Modelo("T07_Postes_energia", PH + "modular_electricity_poles/modular_electricity_poles_1k.fbx", V(44f, 0f, 78f), 0f, 1f, tec);

        // ---------- estação de resíduos e repetidor ----------
        var est = RaizEstacao();
        var espalhado = new GameObject("Residuos_Espalhados"); espalhado.transform.SetParent(est);
        for (int i = 0; i < 5; i++)
        {
            var p = V(36f + (i % 3) * 0.9f, 0f, 101.2f + (i / 3) * 0.8f);
            ModeloAlvo("Residuo_estacao_" + (i + 1), PH + "compost_bag_02/compost_bag_02_1k.fbx", p, 50f * i, 0.4f, true, espalhado.transform, false);
            Cilindro(espalhado.transform, "Lata", p + V(0.35f, 0.06f, -0.2f), V(0.12f, 0.08f, 0.12f), Quaternion.Euler(0f, 0f, 90f), matTerminal);
        }
        var lixeira = Solido(ModeloAlvo("E04_Lixeira_estacao", PH + "metal_trash_can/metal_trash_can_1k.fbx", V(39.4f, 0f, 102.6f), 0f, 0.95f, true, recup));
        var cheio = new GameObject("Residuos_No_Recipiente"); cheio.transform.SetParent(est);
        ModeloAlvo("Residuo_recipiente", PH + "compost_bag_02/compost_bag_02_1k.fbx", V(39.4f, Topo(lixeira) - 0.12f, 102.6f), 30f, 0.36f, true, cheio.transform, false);
        cheio.SetActive(false);
        var rep = new GameObject("N3-05_Receptor_repetidor").transform; rep.SetParent(locais);
        Caixa(rep, "N3-05_Receptor_repetidor_Mastro", V(43.5f, 2.2f, 102.8f), V(0.18f, 4.4f, 0.18f), matTerminal);
        Caixa(rep, "N3-05_Receptor_repetidor_Caixa", V(43.5f, 1.1f, 102.5f), V(0.7f, 0.8f, 0.4f), matTerminal, true);
        var prato = Cilindro(rep, "N3-05_Receptor_repetidor_Prato", V(43.5f, 4.1f, 102.4f), V(1.1f, 0.06f, 1.1f), Quaternion.Euler(70f, 30f, 0f), matTerminal);
        var ledR = Led(est, "Repetidor", V(43.5f, 1.2f, 102.28f));
        var luzRep = Luz(est, "Luz_Repetidor", V(43.5f, 4.7f, 102.6f), new Color(0.5f, 1f, 0.6f), 1.4f, 8f); luzRep.gameObject.SetActive(false);
        NovaEstacao(V(40.5f, 0f, 100.6f), "E — juntar os resíduos e religar o repetidor",
            "Resíduos no recipiente. Repetidor ligado: o rádio volta a ficar limpo.\nMARA (rádio): Agora te ouço bem, Sabiá. Vá até o mirante.",
            "ir ao mirante e usar o terminal", new[] { cheio, ledR[1], luzRep.gameObject }, new[] { espalhado, ledR[0] });
        // mesa com o mapa da Vigília
        var mesa = new GameObject("N3-01_Mesa_mapa").transform; mesa.SetParent(locais);
        Caixa(mesa, "N3-01_Mesa_mapa_Tampo", V(30.5f, 0.85f, 101.6f), V(1.8f, 0.08f, 1f), matMadeira, true);
        for (int k = 0; k < 4; k++) Caixa(mesa, "N3-01_Mesa_mapa_Pe", V(30.5f + (k % 2 == 0 ? -0.8f : 0.8f), 0.42f, 101.6f + (k < 2 ? -0.4f : 0.4f)), V(0.08f, 0.84f, 0.08f), matMadeira);
        Caixa(mesa, "N3-01_Mesa_mapa_Papel", V(30.5f, 0.9f, 101.6f), V(1.4f, 0.01f, 0.8f), matMarco);
        for (int k = 0; k < 4; k++) Caixa(mesa, "N3-01_Mesa_mapa_X", V(30f + k * 0.32f, 0.91f, 101.4f + (k % 2) * 0.3f), V(0.12f, 0.01f, 0.03f), matLedVermelho).transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        Luz(luzes, "Luz_Mesa", V(30.5f, 2f, 101.6f), new Color(1f, 0.75f, 0.45f), 1.2f, 6f);
        KitOficina(V(27f, 0f, 91.3f), 0f);
        KitViveiro(V(44f, 0f, 91.3f), 0f);
        // mirante e rota alternativa ao fundo (crista com marcas laranja)
        var crista = ModeloAlvo("R05_Rocha_crista", PH + "rock_09/rock_09_1k.fbx", V(80f, 0f, 124f), 0f, 9f, false, rochas);
        float hc = Topo(crista) - 0.3f;
        var marco = new GameObject("N3-04_Marco_rota_alternativa").transform; marco.SetParent(locais);
        Caixa(marco, "N3-04_Marco_rota_alternativa_Poste", V(80f, hc + 1.5f, 124f), V(0.25f, 3f, 0.25f), matMadeira);
        Caixa(marco, "N3-04_Marco_rota_alternativa_Faixa", V(80f, hc + 2.5f, 123.85f), V(0.6f, 0.5f, 0.05f), matLenco);
        Ocupar(marco.gameObject);
        Luz(luzes, "Luz_Rota_Alternativa", V(80f, hc + 3.3f, 123f), new Color(1f, 0.55f, 0.15f), 4f, 22f);
        for (int i = 0; i < 4; i++)
        {
            var p = Vector3.Lerp(V(66f, 0f, 112f), V(78f, 0f, 122f), i / 3f);
            ModeloAlvo("R04_Rocha_crista_" + (i + 1), PH + "rock_07/rock_07_1k.fbx", p, 40f * i, 3f + i, false, rochas);
            Luz(luzes, "Luz_Marca_Rota_" + (i + 1), p + Vector3.up * (2.6f + i), new Color(1f, 0.55f, 0.15f), 2f, 8f);
        }
        // linha de visão do mirante até a crista livre de árvores (a cena final mostra a rota)
        for (float t = 0f; t <= 1f; t += 0.06f) { var p = Vector3.Lerp(V(60f, 0f, 100f), V(80f, 0f, 124f), t); ocupados.Add(new Vector4(p.x, p.z, 4.5f, 0f)); }
        ModeloAlvo("R10_Rochas_tropicais", TGD + "Rochas/GroupRocks01.DAE", V(66f, 0f, 96f), 0f, 6f, false, rochas);
        var terminal = NovoTerminal(V(59.5f, 0f, 100f), 90f, "Religue o repetidor antes de seguir.", "E — usar o terminal de saída", true);

        Residuos(new[] { V(2f, 0f, 6f), V(5f, 0f, 47f), V(2.2f, 0f, 60f), V(-1.5f, 0f, 66f), V(38.6f, 0f, 74f) });
        Marcas(new[] { V(2.6f, 0f, 10f), V(-5.4f, 0f, 50f), V(4f, 0f, 69.4f) });
        Lanternas(new[] { new Vector2(0f, -8f), new Vector2(0f, 67f), new Vector2(37f, 67f), new Vector2(37f, 97f), new Vector2(58f, 100f) }, 3.75f);
        VagaLumes(new[] { V(-5f, 1.3f, 14f), V(5f, 1.3f, 34f), V(-8f, 1.3f, 50f), V(10f, 1.3f, 62f), V(42f, 1.3f, 82f), V(48f, 1.3f, 106f) });
        Gatilho(tutorial, "Dica_Tronco", V(0f, 1.5f, 11f), V(6f, 3f, 1.5f), "Tronco giratório de ciclo lento: observe e passe logo depois dele.", 5f);
        Gatilho(tutorial, "Dica_Fosso", V(0f, 1.5f, 26.5f), V(6f, 3f, 1.5f), "Fosso curto com estacas: salte.", 3f);
        Gatilho(tutorial, "Dica_Galho", V(0f, 1.5f, 35f), V(6f, 3f, 1.5f), "Galho baixo: Ctrl para deslizar.", 3f);
        Gatilho(tutorial, "Dica_Descanso", V(0f, 1.5f, 45f), V(6f, 3f, 1.5f), "Ponto de descanso. Daqui em diante, a Vigília vigia o corredor.", 5f);
        Gatilho(tutorial, "Dica_Holofote", V(2f, 1.5f, 67f), V(1.5f, 3f, 6f), "Holofote da Vigília: se a luz pegar o Nilo, custa um coração.\nEspere a luz ir para o outro lado e passe.", 6f);
        Gatilho(tutorial, "Dica_Tronco2", V(23.5f, 1.5f, 67f), V(1.5f, 3f, 6f), "Outro tronco, outro ritmo: este é mais rápido.", 4f);
        Gatilho(tutorial, "Chegada_Estacao", V(37f, 1.5f, 89f), V(6f, 3f, 1.5f), "Área segura: estação de retransmissão. Use E junto à lixeira.", 5f);

        CriarUICutscene();
        var abertura = NovaCutscene("Abertura_N3", new[] {
            K(0f, 9f, -6f,      0f, 0.5f, 26f,  1.2f),
            K(-2f, 2.6f, 14f,   0f, 1f, 21f,    4.5f),
            K(0.3f, 1.9f, 0.4f, 0f, 1.55f, -3f, 3.5f),
            K(0f, 4.4f, -4.2f,  0f, 1f, 20f,    3.0f),
        }, new[] {
            F(0.3f, 4.4f, "RASTRO DO DOSSEL\n<size=40>Nível 3 — Corredor de caça</size>", true),
            F(1.2f, 4.8f, "<color=#9FE0FF>MARA (rádio):</color> ...Sabiá? O sinal está... falhando..."),
            F(4.9f, 8.0f, "<color=#9FE0FF>MARA:</color> ...corredor... repetidor... religue o..."),
            F(8.1f, 12.2f, "<color=#FFC46B>NILO:</color> As armadilhas estão onde eu marquei no mapa. Exatamente onde."),
        });
        abertura.tocarAoIniciar = true;
        var final = NovaCutscene("Final_N3", new[] {
            K(57f, 1.9f, 102.2f,  59.8f, 1.2f, 100f,  0.5f),
            K(58.5f, 4.2f, 97f,   80f, 6f, 122f,      3.6f),
            K(61f, 5f, 99f,       80f, 8f, 124f,      4.0f),
            K(55f, 9f, 92f,       72f, 4f, 118f,      4.4f),
        }, new[] {
            F(0.3f, 3.8f, "<color=#FFC46B>NILO:</color> Mara... fui eu quem mostrou essas trilhas à Vigília."),
            F(3.9f, 7.6f, "<color=#FFC46B>NILO:</color> A passagem antiga fica acima do corredor. Dá para ver daqui: as marcas laranja."),
            F(7.7f, 10.8f, "<color=#9FE0FF>MARA (rádio):</color> Então me diga cada referência. Vamos orientar os outros."),
            F(10.9f, 12.5f, "<color=#FFC46B>NILO:</color> Pedra grande, marca laranja, depois o posto das raízes."),
            F(11.4f, 12.5f, "FIM DO NÍVEL 3", true),
        });
        final.neblinaFim = 120f;
        terminal.cutsceneFinal = final;
        Marcadores();
        GarantirCatalogo();

        registrarOcupacao = false;
        Vegetacao(veg, new[] { -35f, -30f, 90f, 135f }, 360, 280);
        Finalizar(scene, Ambiente.Ravina, V(0f, 0.05f, -3f), 0f,
            "Nível 3 — Corredor de caça", "atravessar o corredor e religar o repetidor", 1,
            "Recolher os resíduos e religar o repetidor", "", "Usar o terminal do mirante",
            "Nível 3 concluído", "Mara tem as referências da rota antiga. Próxima parada: o posto das raízes.", false,
            "Assets/Settings/PerfilPos_N3.asset");
    }

    // ============================================================================================
    // Nível 4 — Posto das raízes

    [MenuItem("Rastro do Dossel/Montar Nível 4")]
    public static void MontarN4()
    {
        var scene = PrepararCena(N4Path, "N4", 41);
        Trilha = new[] { R(-4, -8, 4, 40), R(-4, 42, 4, 84), R(-14, 84, 18, 108), R(18, 94, 24, 104), R(24, 94, 30, 104), R(30, 94, 38, 104) };
        Fossos = new[] { R(-4, 40, 4, 42) };
        Aguas = new float[0][];
        areaMata = new[] { -60f, -50f, 130f, 170f };
        Chao(false);
        Degrau("Degrau_1", Trilha[3], 0.8f);
        Degrau("Degrau_2", Trilha[4], 1.6f);
        Degrau("Saida_Plataforma", Trilha[5], 1.6f);
        registrarOcupacao = true;

        // ---------- trilha: galho, fosso, encosta instável, galho ----------
        ModeloAlvo("C07_Fogueira", PH + "stone_fire_pit/stone_fire_pit_1k.fbx", V(-2.4f, 0f, -6.5f), 0f, 1.3f, false, constr);
        Luz(luzes, "Luz_Fogueira", V(-2.4f, 1f, -6.5f), new Color(1f, 0.55f, 0.2f), 2.6f, 11f);
        Galho(1, V(0f, 0f, 32f), false);
        Placa(cenario, "A05_Placa_perigo_1", V(3.4f, 0f, 37.5f));
        Fosso(perigos, armad, 1, Fossos[0], V(0f, 0f, 37f));
        Modelo("R08_Encosta", PH + "mountainside/mountainside_1k.fbx", V(-52f, 0f, 60f), 270f, 1.76f, rochas).transform.localScale = V(1.76f, 0.52f, 1.33f);
        for (int i = 0; i < 3; i++)
            ModeloAlvo("R06_Paredao_encosta_" + (i + 1), PH + "rock_face_01/rock_face_01_1k.fbx", V(-9.5f, 0f, 50f + i * 10f), 90f, 10f, false, rochas);
        var encGo = new GameObject("Encosta_Instavel"); encGo.transform.SetParent(perigos);
        var enc = encGo.AddComponent<EncostaInstavel>();
        enc.rochas = PrefabsRochas(); enc.inicio = V(-3.3f, 2.4f, 49f); enc.fim = V(-3.3f, 2.4f, 71f);
        enc.impulso = V(7.5f, 0f, 0f); enc.intervalo = 2.2f; enc.aviso = 0.9f; enc.vida = 4.5f;
        enc.poeira = Poeira(encGo.transform, enc.inicio, "Poeira_Encosta", new Color(0.7f, 0.62f, 0.5f, 0.85f));
        enc.somAviso = Som(enc.poeira.gameObject, AUD + "tronco_aviso.wav");
        for (int i = 0; i < 6; i++) ModeloAlvo("R09_Cascalho_encosta_" + (i + 1), PH + "stone_01/stone_01_1k.fbx", V(-5.4f, 0f, 48f + i * 4.6f), 60f * i, 1.4f, false, rochas, false);
        Placa(cenario, "A05_Placa_perigo_2", V(3.4f, 0f, 46f));
        Galho(2, V(0f, 0f, 78f), false);

        // ---------- posto: abrigo, bancada, barreira, carrinho, mapa ----------
        var loc = locais;
        for (int k = 0; k < 4; k++) Caixa(loc, "N4-01_Abrigo_Ivo_Poste", V(-10f + (k % 2 == 0 ? -1.6f : 1.6f), 1.3f, 104.5f + (k < 2 ? -1.3f : 1.3f)), V(0.16f, 2.6f, 0.16f), matMadeira);
        Caixa(loc, "N4-01_Abrigo_Ivo_Telhado", V(-10f, 2.7f, 104.5f), V(3.8f, 0.14f, 3.2f), matMadeira).transform.rotation = Quaternion.Euler(-8f, 0f, 0f);
        Caixa(loc, "N4-01_Abrigo_Ivo_Fundo", V(-10f, 1.3f, 105.9f), V(3.4f, 2.4f, 0.08f), matMadeira, true);
        Solido(ModeloAlvo("P02_Caixote_abrigo", PH + "wooden_crate_02/wooden_crate_02_1k.fbx", V(-11.3f, 0f, 105.2f), 10f, 0.8f, false, apoio));
        PosteLampiao("C08_Lampiao_abrigo", -7.6f, 106.8f, 2.4f);
        Caixa(loc, "N4-02_Bancada_reparos", V(5f, 0.45f, 104.6f), V(2.2f, 0.9f, 0.8f), matMadeira, true);
        Caixa(loc, "N4-02_Bancada_reparos_Morsa", V(4.3f, 1f, 104.6f), V(0.25f, 0.2f, 0.3f), matTerminal);
        ModeloAlvo("T05_Multimetro_bancada", PH + "retro_multimeter/retro_multimeter_1k.fbx", V(5.6f, 0.9f, 104.6f), 200f, 0.24f, true, tec, false);
        Luz(luzes, "Luz_Bancada", V(5f, 2.3f, 104f), new Color(1f, 0.78f, 0.5f), 1.4f, 7f);
        var est = RaizEstacao();
        // barreira articulada: painéis deitados -> em pé contendo a encosta
        for (int k = 0; k < 5; k++) Caixa(loc, "N4-03_Barreira_articulada_Poste", V(-12.6f, 1.1f, 88f + k * 2f), V(0.22f, 2.2f, 0.22f), matMadeira);
        var abertos = new GameObject("Barreira_Deitada"); abertos.transform.SetParent(est);
        var fechados = new GameObject("Barreira_Erguida"); fechados.transform.SetParent(est);
        for (int k = 0; k < 4; k++)
        {
            Caixa(abertos.transform, "Painel_Deitado", V(-11.5f, 0.08f, 89f + k * 2f), V(1.9f, 0.1f, 1.9f), matMadeira).transform.rotation = Quaternion.Euler(0f, 0f, 8f);
            Caixa(fechados.transform, "Painel_Erguido", V(-12.45f, 0.95f, 89f + k * 2f), V(0.1f, 1.9f, 1.9f), matMadeira);
        }
        Caixa(fechados.transform, "Painel_Faixa", V(-12.38f, 1.6f, 92f), V(0.03f, 0.25f, 7.6f), matLenco);
        fechados.SetActive(false);
        Caixa(est, "Bateria_Apoio", V(-10.4f, 0.3f, 90.4f), V(0.6f, 0.6f, 0.45f), matTerminal, true);
        var ledB = Led(est, "Bateria", V(-10.4f, 0.45f, 90.66f));
        var cabo = Caixa(est, "Bateria_Cabo", V(-11.5f, 0.05f, 90.4f), V(2.2f, 0.04f, 0.04f), matEstaca); cabo.SetActive(false);
        ModeloAlvo("N4-04_Carrinho_mudas", PH + "seeding_tray_01/seeding_tray_01_1k.fbx", V(10f, 0.55f, 90f), 0f, 0.9f, false, loc, false);
        Caixa(loc, "N4-04_Carrinho_mudas_Caixa", V(10f, 0.35f, 90f), V(1.2f, 0.4f, 0.8f), matCaixote, true);
        for (int k = -1; k <= 1; k += 2) Cilindro(loc, "N4-04_Carrinho_mudas_Roda", V(10f + k * 0.45f, 0.2f, 89.55f), V(0.4f, 0.04f, 0.4f), Quaternion.Euler(90f, 0f, 0f), matRocha);
        Caixa(loc, "N4-04_Carrinho_mudas_Alca", V(10.85f, 0.75f, 90f), V(0.5f, 0.05f, 0.7f), matMadeira);
        Quadro(loc, "N4-05_Mapa_comunitario", V(14.5f, 0f, 106.8f), 0f, V(2f, 1.2f, 0.06f), matMarco);
        Caixa(loc, "N4-05_Mapa_comunitario_Rota", V(14.5f, 1.5f, 106.76f), V(1.4f, 0.06f, 0.02f), matLenco).transform.rotation = Quaternion.Euler(0f, 0f, 20f);
        ModeloAlvo("C02_Cabana_selva", TGD + "CabanaSelva/JungleHut01.DAE", V(-2f, 0f, 114f), 180f, 5.6f, false, constr);
        KitOficina(V(-4f, 0f, 107f), 0f);
        KitViveiro(V(6.5f, 0f, 85.6f), 0f);
        PosteLampiao("C08_Lampiao_posto_1", -13f, 85.5f, 2.4f);
        PosteLampiao("C08_Lampiao_posto_2", 16.8f, 95f, 2.4f);
        PosteLampiao("C08_Lampiao_posto_3", 9f, 106.8f, 2.4f);

        var ivo = Ivo(new[] { V(-10f, 0f, 103.6f), V(5f, 0f, 103.4f), V(-10.8f, 0f, 92f) }, 160f, new[] {
            "IVO: A encosta solta pedra desde ontem. Sem a barreira, ninguém trabalha aqui.",
            "IVO: A bateria fica junto da barreira, do lado da encosta. É só ligar e travar.",
            "IVO: Se a antena transmitir, o pessoal de Várzea Alta passa por aqui. Vou deixar as mudas prontas.",
        });
        var estacao = NovaEstacao(V(-9.6f, 0f, 90.4f), "E — travar a barreira e ligar a bateria",
            "Barreira travada e bateria ligada.\nIVO: Com a barreira no lugar, dá para trabalhar aqui.\nNILO: A rota pela encosta está livre. Mara tem as referências.",
            "subir até a saída para a serra", new[] { fechados, ledB[1], cabo }, new[] { abertos, ledB[0] });
        UnityEventTools.AddVoidPersistentListener(estacao.onCompleted, ivo.IniciarRota);
        var terminal = NovoTerminal(V(35.5f, 1.6f, 99f), 90f, "Ajude o Ivo a conter a encosta antes de seguir.", "E — usar o terminal de saída", true);
        Antena(V(60f, 0f, 132f));
        for (float t = 0f; t <= 1f; t += 0.05f) { var p = Vector3.Lerp(V(38f, 0f, 102f), V(58f, 0f, 129f), t); ocupados.Add(new Vector4(p.x, p.z, 4.5f, 0f)); }
        ModeloAlvo("R07_Paredao_fim", PH + "rock_face_02/rock_face_02_1k.fbx", V(42f, 0f, 99f), 270f, 9f, false, rochas);

        Mudas(new[] { new Vector2(2.5f, 10f), new Vector2(-2.6f, 27f), new Vector2(2.6f, 45f), new Vector2(2.6f, 82f) },
              V(8.6f, 0f, 92.6f), V(0.75f, 0f, 0f), "Muda resgatada: vai para o carrinho do posto.");
        Sinais(new[] { V(-2.7f, 0f, 18f), V(2.7f, 0f, 75f) });
        Lanternas(new[] { new Vector2(0f, -8f), new Vector2(0f, 84f) }, 4.75f);
        VagaLumes(new[] { V(-6.5f, 1.3f, 12f), V(6.5f, 1.3f, 30f), V(6f, 1.3f, 60f), V(0f, 1.3f, 96f), V(14f, 1.3f, 100f), V(-12f, 1.3f, 100f) });
        Gatilho(tutorial, "Dica_Galho", V(0f, 1.5f, 29f), V(8f, 3f, 1.5f), "Galho baixo: Ctrl para deslizar.", 3f);
        Gatilho(tutorial, "Dica_Fosso", V(0f, 1.5f, 36.5f), V(8f, 3f, 1.5f), "Fosso com estacas: salte.", 3f);
        Gatilho(tutorial, "Dica_Encosta", V(0f, 1.5f, 45.5f), V(8f, 3f, 1.5f), "Encosta instável: a poeira mostra onde a pedra vai cair.\nObserve e passe entre uma pedra e outra.", 6f);
        Gatilho(tutorial, "Dica_Galho2", V(0f, 1.5f, 75.5f), V(8f, 3f, 1.5f), "Último galho antes do posto.", 3f);
        Gatilho(tutorial, "Dica_Degraus", V(17.2f, 1.5f, 99f), V(1.5f, 3f, 10f), "Degraus: Espaço para subir até a saída.", 4f);

        CriarUICutscene();
        var abertura = NovaCutscene("Abertura_N4", new[] {
            K(6f, 8f, -6f,      0f, 0.5f, 40f,  1.2f),
            K(3f, 3f, 44f,      -6f, 1.5f, 60f, 4.5f),
            K(0.3f, 1.9f, 0.4f, 0f, 1.55f, -3f, 3.5f),
            K(0f, 4.6f, -4.2f,  0f, 1f, 20f,    3.0f),
        }, new[] {
            F(0.3f, 4.4f, "RASTRO DO DOSSEL\n<size=40>Nível 4 — Posto das raízes</size>", true),
            F(1.2f, 5.0f, "<color=#9FE0FF>MARA (rádio):</color> O posto das raízes fica logo depois da encosta."),
            F(5.1f, 8.6f, "<color=#9FE0FF>MARA:</color> O Ivo está lá. Ele conhece cada pedra daquele lugar."),
            F(8.7f, 12.2f, "<color=#FFC46B>NILO:</color> Dá para ouvir as ferramentas daqui."),
        });
        abertura.tocarAoIniciar = true;
        var cenaIvo = NovaCutscene("Ivo_N4", new[] {
            K(0f, 2.4f, 85.5f,   -10f, 1.2f, 103f,  0.5f),
            K(-5.5f, 2f, 98.5f,  -10f, 1.4f, 103.6f, 3.2f),
            K(-6f, 3.2f, 94f,    -12.5f, 0.8f, 91f, 3.6f),
            K(-1f, 3.5f, 88f,    -9f, 1f, 92f,      3.0f),
        }, new[] {
            F(0.3f, 3.7f, "<color=#C8F08C>IVO:</color> Nilo? Achei que ninguém mais vinha pela trilha."),
            F(3.8f, 7.6f, "<color=#C8F08C>IVO:</color> A encosta está soltando pedra. A barreira está montada, mas sem a bateria não trava."),
            F(7.7f, 10.6f, "<color=#FFC46B>NILO:</color> Deixa comigo. Depois a gente fala da rota."),
        });
        var gIvo = Gatilho(tutorial, "Entrada_Posto", V(0f, 1.5f, 86f), V(8f, 3f, 1.5f), "", 0f);
        UnityEventTools.AddPersistentListener(gIvo.GetComponent<TutorialTrigger>().onEnter, cenaIvo.TocarCena);
        var final = NovaCutscene("Final_N4", new[] {
            K(33f, 3.5f, 101.4f,  35.8f, 2.7f, 99f,  0.5f),
            K(31f, 4.6f, 96f,     -4f, 1f, 100f,     3.4f),
            K(35f, 9f, 99f,       60f, 18f, 132f,    4.2f),
        }, new[] {
            F(0.3f, 3.6f, "<color=#FFC46B>NILO:</color> Ivo ficou trabalhando. O posto está seguro."),
            F(3.7f, 7.6f, "<color=#9FE0FF>MARA (rádio):</color> Daqui já dá para ver a antena. É o último trecho, Sabiá."),
            F(6.6f, 8.1f, "FIM DO NÍVEL 4", true),
        });
        final.neblinaFim = 170f;
        terminal.cutsceneFinal = final;
        Marcadores();
        GarantirCatalogo();

        registrarOcupacao = false;
        Vegetacao(veg, new[] { -35f, -30f, 70f, 130f }, 340, 260);
        AssarNavMesh(N4Path, V(-13.8f, -1f, 84.4f), V(17.6f, 3f, 107.8f));
        Finalizar(scene, Ambiente.Madrugada, V(0f, 0.05f, -3f), 0f,
            "Nível 4 — Posto das raízes", "chegar ao posto e conter a encosta", 1,
            "Travar a barreira e ligar a bateria", "", "Subir até a saída para a serra",
            "Nível 4 concluído", "Ivo segue trabalhando no posto. A antena está à vista.", false,
            "Assets/Settings/PerfilPos_N4.asset");
    }

    // ============================================================================================
    // Nível 5 — A antena

    [MenuItem("Rastro do Dossel/Montar Nível 5")]
    public static void MontarN5()
    {
        var scene = PrepararCena(N5Path, "N5", 51);
        Trilha = new[] { R(-4, -8, 4, 20), R(-4, 22.4f, 4, 40), R(-4, 40, 14, 52), R(-4, 52, 4, 72), R(-4, 74.4f, 4, 90), R(-14, 90, 22, 120), R(-4, 120, 12, 134) };
        Fossos = new[] { R(-4, 20, 4, 22.4f), R(-4, 72, 4, 74.4f) };
        Aguas = new float[0][];
        areaMata = new[] { -60f, -50f, 120f, 190f };
        Chao(false);
        registrarOcupacao = true;

        // ---------- subida: fosso, galho, mirante, tronco, fosso, galho ----------
        ModeloAlvo("R06_Paredao_inicio", PH + "rock_face_01/rock_face_01_1k.fbx", V(0f, 0f, -11f), 0f, 11f, false, rochas);
        Placa(cenario, "A05_Placa_perigo_1", V(3.4f, 0f, 17.5f));
        Fosso(perigos, armad, 1, Fossos[0], V(0f, 0f, 17f));
        Galho(1, V(0f, 0f, 32f), false);
        Solido(ModeloAlvo("P05_Banco_mirante", PH + "painted_wooden_bench/painted_wooden_bench_1k.fbx", V(10f, 0f, 50.6f), 180f, 0.9f, true, apoio));
        PosteLampiao("C08_Lampiao_mirante", 13f, 49.5f, 2.4f);
        var tg = TroncoGiratorio(perigos, armad, V(0f, 0f, 61f)); tg.degreesPerSecond = 80f;
        Luz(luzes, "Luz_Tronco", V(0f, 4.2f, 61f), new Color(1f, 0.6f, 0.3f), 1.4f, 10f);
        Placa(cenario, "A05_Placa_perigo_2", V(3.4f, 0f, 69.5f));
        Fosso(perigos, armad, 2, Fossos[1], V(0f, 0f, 69f));
        Galho(2, V(0f, 0f, 84f), false);

        // ---------- área de regeneração (oeste) com a entrada indevida ----------
        var est = RaizEstacao();
        var cercaPath = PH + "modular_chainlink_fence/modular_chainlink_fence_1k.fbx";
        int nc = 0;
        for (int i = 0; i < 10; i++) { float z = 97f + i * 2f; if (z > 101.5f && z < 106.5f) continue; Modelo("C10_Alambrado_regeneracao_" + (++nc), cercaPath, V(-4.4f, 0f, z), 90f, 1f, constr); }
        for (int i = 0; i < 4; i++) { Modelo("C10_Alambrado_regeneracao_" + (++nc), cercaPath, V(-12f + i * 2f, 0f, 96f), 0f, 1f, constr); Modelo("C10_Alambrado_regeneracao_" + (++nc), cercaPath, V(-12f + i * 2f, 0f, 116f), 0f, 1f, constr); }
        var cerca = new GameObject("Colisor_Cerca_Regeneracao").transform; cerca.SetParent(terreno);
        foreach (var b in new[] { new[] { -4.4f, 99f, 0.2f, 6f }, new[] { -4.4f, 111f, 0.2f, 10f }, new[] { -9f, 96f, 9.4f, 0.2f }, new[] { -9f, 116f, 9.4f, 0.2f } })
        {
            var c = new GameObject("Colisor"); c.transform.SetParent(cerca); c.transform.position = V(b[0], 1f, b[1]);
            c.AddComponent<BoxCollider>().size = V(b[2], 2f, b[3]);
        }
        for (int k = -1; k <= 1; k += 2) Caixa(locais, "N5-04_Portal_regeneracao_Poste", V(-4.4f, 1.2f, 104f + k * 2.4f), V(0.25f, 2.4f, 0.25f), matMadeira);
        Caixa(locais, "N5-04_Portal_regeneracao_Viga", V(-4.4f, 2.45f, 104f), V(0.25f, 0.2f, 5f), matMadeira);
        Caixa(locais, "N5-04_Portal_regeneracao_Placa", V(-4.25f, 2.05f, 104f), V(0.05f, 0.45f, 1.6f), matLedVerde);
        var aberto = Caixa(est, "Portao_Regeneracao_Aberto", V(-6.6f, 0.85f, 106.5f), V(4.4f, 1.6f, 0.08f), matMadeira);
        var fechado = Caixa(est, "Portao_Regeneracao_Fechado", V(-4.4f, 0.85f, 104f), V(0.08f, 1.6f, 4.6f), matMadeira, true);
        fechado.SetActive(false);
        var marcasPneu = new GameObject("Marcas_Pneu_Vigilia"); marcasPneu.transform.SetParent(est);
        for (int i = 0; i < 6; i++) for (int k = -1; k <= 1; k += 2)
            Caixa(marcasPneu.transform, "Marca_Pneu", V(-6f - i * 1.3f, 0.035f, 104f + k * 0.8f + (i % 2) * 0.1f), V(1.2f, 0.02f, 0.3f), matFosso);
        Caixa(locais, "N5-04_Portal_regeneracao_Faixa", V(-9f, 0.02f, 106f), V(8f, 0.01f, 18f), matMata);
        NovaEstacao(V(-2.8f, 0f, 104f), "E — fechar a entrada indevida da área protegida",
            "", "desligar a sentinela da antena",
            new[] { fechado }, new[] { aberto, marcasPneu });

        // ---------- sentinela e portão da antena ----------
        var portao = Caixa(terreno, "Portao_Vigilia", V(4f, 1.5f, 120f), V(16f, 3f, 0.5f), matTerminal, "Ground", "Ground");
        Modelo("C06_Barreira_Vigilia_portao_1", PH + "concrete_road_barrier/concrete_road_barrier_1k.fbx", V(0f, 0f, 119.6f), 0f, 1.04f, portao.transform);
        Modelo("C06_Barreira_Vigilia_portao_2", PH + "concrete_road_barrier/concrete_road_barrier_1k.fbx", V(8f, 0f, 119.6f), 0f, 1.04f, portao.transform);
        Caixa(portao.transform, "Portao_Placa", V(4f, 2.2f, 119.7f), V(1.6f, 0.6f, 0.05f), matPerigo);
        var sentinela = CriarSentinela(V(14f, 0f, 110f), 180f, V(4.5f, 0f, 92.5f), V(21f, 0f, 118f), portao);
        sentinela.raioAcordar = 0f;
        sentinela.objetivoDepois = "instalar o módulo na antena";
        sentinela.mensagemFim = "MARA (rádio): A sentinela parou e o portão abriu. Instale o módulo na antena.";
        var cantos = new[] { V(4.5f, 0f, 91f), V(21.5f, 0f, 91f), V(21.5f, 0f, 119f) };
        for (int i = 0; i < cantos.Length; i++)
        {
            Caixa(chefe, "Tocha_Poste", cantos[i] + Vector3.up * 1.2f, V(0.12f, 2.4f, 0.12f), matMadeira);
            Modelo("C08_Lampiao_arena_" + (i + 1), PH + "wooden_lantern_01/wooden_lantern_01_1k.fbx", cantos[i] + Vector3.up * 2.4f, 0f, 0.85f, constr);
            Luz(luzes, "Luz_Arena_" + (i + 1), cantos[i] + Vector3.up * 2.3f, new Color(1f, 0.6f, 0.3f), 2f, 13f);
        }
        Solido(ModeloAlvo("C09_Bloqueio_Vigilia_arena", PH + "concrete_road_barrier_02/concrete_road_barrier_02_1k.fbx", V(21f, 0f, 104f), 90f, 1.7f, false, constr));
        Modelo("T02_Sensor_camera", PH + "security_camera_01/security_camera_01_1k.fbx", V(21.6f, 3.2f, 119f), 210f, 1.22f, tec);
        Caixa(chefe, "Poste_camera_arena", V(21.6f, 1.53f, 119f), V(0.12f, 3.05f, 0.12f), matTerminal);

        // ---------- antena ----------
        Antena(V(6f, 0f, 129.5f), false);
        var antena = GameObject.Find("Antena_Serra").transform;
        var topoVermelho = antena.Find("Luz_Topo").gameObject;
        var topoVerde = Caixa(antena, "Luz_Topo_Transmitindo", topoVermelho.transform.position, V(0.47f, 0.47f, 0.47f), matLedVerde); topoVerde.SetActive(false);
        var luzVermelha = GameObject.Find("Luzes/Luz_Antena");
        foreach (Transform t in antena) t.name = "N5-01_Antena_principal_" + t.name;
        var base0 = new GameObject("Colisor_Antena"); base0.transform.SetParent(terreno); base0.transform.position = V(6f, 1.5f, 129.5f);
        base0.AddComponent<BoxCollider>().size = V(4.6f, 3f, 4.6f);
        var luzVerde = Luz(luzes, "Luz_Antena_Transmitindo", V(6f, 30.2f, 129.5f), new Color(0.3f, 1f, 0.45f), 3f, 34f); luzVerde.gameObject.SetActive(false);
        Caixa(locais, "N5-02_Pedestal_modulo", V(3f, 0.55f, 125.6f), V(0.8f, 1.1f, 0.8f), matTerminal, true);
        Caixa(locais, "N5-02_Pedestal_modulo_Encaixe", V(3f, 1.12f, 125.6f), V(0.5f, 0.06f, 0.35f), matLedVermelho);
        var modulo = Caixa(est, "Modulo_Instalado", V(3f, 1.25f, 125.6f), V(0.42f, 0.22f, 0.3f), matLedVerde); modulo.SetActive(false);
        Quadro(locais, "N5-03_Mapa_final_rotas", V(10.2f, 0f, 122.6f), 0f, V(2.2f, 1.3f, 0.06f), matMarco);
        for (int k = 0; k < 3; k++) Caixa(locais, "N5-03_Mapa_final_rotas_Linha", V(9.6f + k * 0.5f, 1.4f + k * 0.25f, 122.56f), V(0.7f, 0.05f, 0.02f), matLenco).transform.rotation = Quaternion.Euler(0f, 0f, 15f + k * 10f);
        Caixa(locais, "N5-05_Marcador_transmissao_Poste", V(11f, 1.6f, 132f), V(0.14f, 3.2f, 0.14f), matTerminal);
        var marcOff = Caixa(locais, "N5-05_Marcador_transmissao_Lampada", V(11f, 3.35f, 132f), V(0.4f, 0.4f, 0.4f), matTerminal);
        var marcOn = Caixa(est, "Marcador_Transmissao_Aceso", V(11f, 3.35f, 132f), V(0.42f, 0.42f, 0.42f), matLedVerde); marcOn.SetActive(false);
        var luzMarc = Luz(est, "Luz_Marcador_Transmissao", V(11f, 3.6f, 131.5f), new Color(0.4f, 1f, 0.5f), 2.2f, 12f); luzMarc.gameObject.SetActive(false);
        var terminal = NovoTerminal(V(3f, 0f, 124.4f), 0f, "A sentinela ainda guarda a antena.", "E — instalar o módulo e transmitir", false);
        KitOficina(V(-3.4f, 0f, 133.1f), 0f);
        KitViveiro(V(-12.5f, 0f, 91.2f), 0f);

        Mudas(new[] { new Vector2(2.5f, 10f), new Vector2(-2.6f, 36f), new Vector2(11f, 44f) }, V(-8.5f, 0f, 100f), V(0f, 0f, 1.3f), "Muda resgatada: vai para a área de regeneração.");
        Marcas(new[] { V(2.6f, 0f, 28f), V(-2.6f, 0f, 53.4f), V(2.6f, 0f, 88f) });
        Lanternas(new[] { new Vector2(0f, -8f), new Vector2(0f, 90f) }, 4.75f);
        Gatilho(tutorial, "Dica_Fosso", V(0f, 1.5f, 15f), V(8f, 3f, 1.5f), "O caminho junta tudo o que você já sabe. Fosso: salte.", 4f);
        Gatilho(tutorial, "Dica_Galho", V(0f, 1.5f, 29f), V(8f, 3f, 1.5f), "Galho baixo: deslize.", 3f);
        Gatilho(tutorial, "Dica_Mirante", V(0f, 1.5f, 41.5f), V(8f, 3f, 1.5f), "Mirante: dá para ver o vale inteiro daqui. Respire.", 4f);
        Gatilho(tutorial, "Dica_Tronco", V(0f, 1.5f, 54.5f), V(8f, 3f, 1.5f), "Tronco giratório: espere ele passar.", 3f);
        Gatilho(tutorial, "Dica_Fosso2", V(0f, 1.5f, 68.5f), V(8f, 3f, 1.5f), "Fosso: salte.", 3f);
        Gatilho(tutorial, "Dica_Regeneracao", V(0f, 1.5f, 91f), V(8f, 3f, 1.5f), "Área de regeneração: a cerca tem uma entrada aberta pela Vigília.\nFeche com E.", 6f);

        CriarUICutscene();
        var abertura = NovaCutscene("Abertura_N5", new[] {
            K(-6f, 9f, -6f,     6f, 14f, 129f,  1.2f),
            K(-7f, 12f, 60f,    6f, 20f, 129f,  4.5f),
            K(0.3f, 1.9f, 0.4f, 0f, 1.55f, -3f, 3.5f),
            K(0f, 4.6f, -4.2f,  0f, 1f, 20f,    3.0f),
        }, new[] {
            F(0.3f, 4.4f, "RASTRO DO DOSSEL\n<size=40>Nível 5 — A antena</size>", true),
            F(1.2f, 5.0f, "<color=#9FE0FF>MARA (rádio):</color> ...chhh... Sabiá, o sinal está fraco aqui embaixo."),
            F(5.1f, 8.6f, "<color=#9FE0FF>MARA:</color> A antena está logo acima. Instale o módulo e transmita o aviso."),
            F(8.7f, 12.2f, "<color=#FFC46B>NILO:</color> Já está clareando. Vamos."),
        });
        abertura.tocarAoIniciar = true;
        var cenaChefe = NovaCutscene("Sentinela_N5", new[] {
            K(-1f, 2f, 101.5f,  14f, 1.4f, 110f, 0.6f),
            K(7f, 1.6f, 103f,   14f, 1.6f, 110f, 3.2f),
            K(10f, 6f, 116f,    14f, 0.8f, 110f, 3.4f),
            K(0f, 3.2f, 100f,   12f, 1.2f, 108f, 3.0f),
        }, new[] {
            F(0.3f, 2.6f, "<color=#FFC46B>NILO:</color> Entrada fechada. O solo protegido fica fora do caminho das máquinas."),
            F(2.7f, 5.0f, "<color=#9FE0FF>MARA (rádio):</color> Outra sentinela! Ela guarda o portão da antena."),
            F(5.1f, 7.2f, "<color=#9FE0FF>MARA:</color> Mesmo jeito da primeira: desvie do avanço e do braço."),
            F(7.3f, 10.2f, "<color=#9FE0FF>MARA:</color> Quando ela bater e travar, tire as três baterias com E."),
        });
        UnityEventTools.AddPersistentListener(cenaChefe.aoComecar, sentinela.Acordar);
        var estacao = Object.FindAnyObjectByType<InteractableStation>(FindObjectsInactive.Include);
        UnityEventTools.AddVoidPersistentListener(estacao.onCompleted, cenaChefe.TocarCena);
        var final = NovaCutscene("Final_N5", new[] {
            K(0.6f, 1.9f, 123f,   3f, 1.2f, 125.6f, 0.5f),
            K(-3f, 3f, 117f,      6f, 12f, 129.5f,  3.6f),
            K(-10f, 12f, 108f,    6f, 24f, 129.5f,  4.4f),
            K(16f, 26f, 104f,     6f, 28f, 129.5f,  4.4f),
            K(30f, 34f, 96f,      60f, 6f, 170f,    4.2f),
        }, new[] {
            F(0.3f, 4.2f, "<color=#FFC46B>NILO:</color> Várzea Alta, não cruzem pelo rio. Sigam as marcas novas na encosta e passem pelo posto."),
            F(4.3f, 7.4f, "<color=#9FE0FF>MARA (rádio):</color> Recebido, Sabiá. Vamos avisar os outros."),
            F(7.5f, 10.6f, "<color=#B8B8B8>VÁRZEA ALTA (rádio):</color> Recebido. Esperamos o dia e seguimos as marcas."),
            F(10.7f, 13.6f, "<color=#C8F08C>IVO (rádio):</color> O posto das raízes está aberto. Tem lugar para todo mundo."),
            F(13.7f, 17.1f, "FIM\n<size=38>O assobio do Sabiá agora marca um caminho que todos conhecem.</size>", true),
        });
        final.neblinaFim = 220f;
        foreach (var go in new[] { luzVerde.gameObject, modulo, marcOn, luzMarc.gameObject, topoVerde })
            UnityEventTools.AddBoolPersistentListener(final.aoComecar, go.SetActive, true);
        foreach (var go in new[] { marcOff, topoVermelho, luzVermelha })
            if (go != null) UnityEventTools.AddBoolPersistentListener(final.aoComecar, go.SetActive, false);
        terminal.cutsceneFinal = final;
        Marcadores();
        GarantirCatalogo();

        registrarOcupacao = false;
        Vegetacao(veg, new[] { -35f, -30f, 60f, 160f }, 330, 250);
        Finalizar(scene, Ambiente.Amanhecer, V(0f, 0.05f, -3f), 0f,
            "Nível 5 — A antena", "chegar à área de regeneração perto da antena", 2,
            "Fechar a entrada indevida da área protegida", "Desligar a sentinela da antena", "Instalar o módulo e transmitir o aviso",
            "Missão cumprida", "O aviso chegou a Várzea Alta e às outras comunidades. O caminho agora é de todos.", true,
            "Assets/Settings/PerfilPos_N5.asset");
    }

    // ============================================================================================
    // Menu: botão Fases e painel com as cinco fases (útil na apresentação). Pode rodar de novo.

    [MenuItem("Rastro do Dossel/Configurar menu (seleção de fases)")]
    public static void ConfigurarMenuFases()
    {
        var scene = EditorSceneManager.OpenScene(MenuPath);
        var mm = Object.FindAnyObjectByType<MainMenu>();
        var canvas = mm.mainPanel.transform.parent;
        var velho = canvas.Find("Painel_Fases"); if (velho != null) Object.DestroyImmediate(velho.gameObject);
        var velhoB = mm.mainPanel.transform.Find("Botao_Fases"); if (velhoB != null) Object.DestroyImmediate(velhoB.gameObject);

        // botão Fases logo abaixo de Jogar; os outros descem uma posição
        var jogar = mm.mainPanel.transform.Find("Botao_Jogar");
        var botaoFases = Object.Instantiate(jogar.gameObject, mm.mainPanel.transform);
        botaoFases.name = "Botao_Fases";
        botaoFases.transform.SetSiblingIndex(jogar.GetSiblingIndex() + 1);
        string[] ordem = { "Botao_Jogar", "Botao_Fases", "Botao_Controles", "Botao_Créditos", "Botao_Sair" };
        for (int i = 0; i < ordem.Length; i++)
            ((RectTransform)mm.mainPanel.transform.Find(ordem[i])).anchoredPosition = new Vector2(0f, 40f - 90f * i);
        TrocarAcao(botaoFases.GetComponent<UnityEngine.UI.Button>(), "Fases", b => UnityEventTools.AddPersistentListener(b.onClick, mm.ShowFases));

        // painel: cópia do painel de controles, sem a lista, com um botão por fase
        var painel = Object.Instantiate(mm.controlsPanel, canvas);
        painel.name = "Painel_Fases";
        Object.DestroyImmediate(painel.transform.Find("Lista").gameObject);
        painel.transform.Find("Titulo").GetComponent<UnityEngine.UI.Text>().text = "Fases";
        var voltar = painel.transform.Find("Botao_Voltar");
        string[] nomes = { "1 — Trilha encoberta", "2 — Margem ferida", "3 — Corredor de caça", "4 — Posto das raízes", "5 — A antena" };
        for (int i = 0; i < nomes.Length; i++)
        {
            var b = Object.Instantiate(voltar.gameObject, painel.transform);
            b.name = "Botao_Fase_" + (i + 1);
            var rt = (RectTransform)b.transform; rt.anchoredPosition = new Vector2(0f, 190f - 88f * i); rt.sizeDelta = new Vector2(520f, 72f);
            var txt = b.GetComponentInChildren<UnityEngine.UI.Text>(true); ((RectTransform)txt.transform).sizeDelta = new Vector2(520f, 72f);
            int n = i + 1;
            TrocarAcao(b.GetComponent<UnityEngine.UI.Button>(), nomes[i], btn => UnityEventTools.AddIntPersistentListener(btn.onClick, mm.JogarFase, n));
        }
        ((RectTransform)voltar).anchoredPosition = new Vector2(0f, -280f);
        painel.SetActive(false);
        mm.fasesPanel = painel;
        EditorUtility.SetDirty(mm);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void TrocarAcao(UnityEngine.UI.Button b, string rotulo, System.Action<UnityEngine.UI.Button> ligar)
    {
        while (b.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(b.onClick, 0);
        ligar(b);
        b.GetComponentInChildren<UnityEngine.UI.Text>(true).text = rotulo;
        EditorUtility.SetDirty(b);
    }

    // ============================================================================================
    // Build: menu e as cinco fases, na ordem.

    [MenuItem("Rastro do Dossel/Configurar build (menu + 5 fases)")]
    public static void ConfigurarBuild()
    {
        EditorBuildSettings.scenes = new[] { MenuPath, ScenePath, N2Path, N3Path, N4Path, N5Path }
            .Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        Debug.Log("Build: " + string.Join(", ", EditorBuildSettings.scenes.Select(s => s.path)));
    }
}
