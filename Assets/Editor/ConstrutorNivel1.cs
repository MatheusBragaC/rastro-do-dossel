using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Monta o nível 1 em terceira pessoa a partir de dados (trilha, perigos, posto, arena da sentinela
// e vegetação). Menu: Rastro do Dossel > Montar Nível 1 (3D). Refaz o cenário; mantém câmera, luz,
// Nilo, Canvas, GameManager, EventSystem, música e a estação do viveiro (só a reposiciona).
public static partial class ConstrutorNivel1
{
    private const string ScenePath = "Assets/Scenes/Nivel1_TrilhaEncoberta.unity";

    // Geometria da fase em construção, definida por cada Montar antes de chamar Chao():
    // retângulos xmin, zmin, xmax, zmax com topo em y = 0.
    private static float[][] Trilha;                    // chão da trilha
    private static float[][] Fossos = new float[0][];   // vãos dentro da área jogável (estacas ou água)
    private static float[][] Aguas = new float[0][];    // rio e lagoas fora da área jogável (só visual)
    private static float[] areaMata;                    // limites do chão da mata: xmin, zmin, xmax, zmax

    private static readonly float[][] TrilhaN1 =
    {
        new[] { -4f, -8f, 4f, 40.5f },     // A: Várzea Alta até o fosso 1
        new[] { -4f, 42.5f, 4f, 52f },     // B: pouso do fosso 1 até a curva
        new[] { -4f, 52f, 44f, 60f },      // C: corredor dos galhos e do tronco giratório
        new[] { 36f, 60f, 44f, 66f },      // D1: até o fosso 2
        new[] { 36f, 68f, 44f, 92f },      // D2: pilão e bloqueio da Vigília
        new[] { 36f, 92f, 80f, 104f },     // E: posto do viveiro
        new[] { 80f, 84f, 108f, 112f },    // F: arena da sentinela
        new[] { 108f, 94f, 116f, 102f },   // G: saída
    };
    private static readonly float[][] FossosN1 = { new[] { -4f, 40.5f, 4f, 42.5f }, new[] { 36f, 66f, 44f, 68f } };

    private static Material matChao, matMata, matMadeira, matTronco, matGalho, matMarco, matPerigo, matTerminal, matLedVerde, matLedVermelho, matRocha, matEstaca, matFosso, matLenco, matCaixote, matFolhagem;
    private static System.Random rng;
    private static System.Random rngVar;   // escolha de variação dos kits, separada para não mudar o espalhamento

    [MenuItem("Rastro do Dossel/Montar Nível 1 (3D)")]
    public static void Montar()
    {
        var scene = IniciarCena(ScenePath, "Locais_N1", 7, 3);
        Trilha = TrilhaN1; Fossos = FossosN1; Aguas = new float[0][];
        areaMata = new[] { -60f, -60f, 260f, 260f };
        Chao(false);
        // portão fechado entre a arena e a saída: abre quando a sentinela desliga
        portaoVigilia = Caixa(terreno, "Portao_Vigilia", new Vector3(108f, 1.5f, 98f), new Vector3(0.5f, 3f, 8f), matTerminal, "Ground", "Ground");
        Modelo("C06_Barreira_Vigilia_portao_1", "Assets/Terceiros/PolyHaven/concrete_road_barrier/concrete_road_barrier_1k.fbx", new Vector3(107.6f, 0f, 96f), 90f, 1.04f, portaoVigilia.transform);
        Modelo("C06_Barreira_Vigilia_portao_2", "Assets/Terceiros/PolyHaven/concrete_road_barrier/concrete_road_barrier_1k.fbx", new Vector3(107.6f, 0f, 100f), 90f, 1.04f, portaoVigilia.transform);
        Caixa(portaoVigilia.transform, "Portao_Placa", new Vector3(107.7f, 2.2f, 98f), new Vector3(0.05f, 0.6f, 1.6f), matPerigo, "Default", "Untagged");

        registrarOcupacao = true;
        Inicio();             // trecho A: Várzea Alta
        MarcaEFosso1();       // trecho B: marca antiga e fosso 1
        CorredorDosGalhos();  // trecho C: barreiras altas e tronco giratório
        Bloqueio();           // trecho D: fosso 2, pilão e bloqueio da Vigília
        Posto();              // trecho E: posto do viveiro

        // ---------- trecho F: arena da sentinela ----------
        Sentinela(chefe, luzes, tec, constr, veg, portaoVigilia);
        ArenaDecor();

        // ---------- trecho G: saída ----------
        ModeloAlvo("R07_Paredao_fim", PH + "rock_face_02/rock_face_02_1k.fbx", new Vector3(120.4f, 0f, 98f), 270f, 9f, false, rochas);
        Luz(luzes, "Luz_Saida", new Vector3(113f, 2.5f, 98f), new Color(0.6f, 1f, 0.8f), 2f, 9f);

        Tutoriais();
        Coletaveis();
        Lanternas(new[] { new Vector2(0f, -8f), new Vector2(0f, 56f), new Vector2(40f, 56f), new Vector2(40f, 92f) }, 4.75f);
        VagaLumes(new[] {
            new Vector3(-6.5f, 1.3f, 12f), new Vector3(6.5f, 1.3f, 25f), new Vector3(-6.5f, 1.3f, 39f), new Vector3(6.5f, 1.3f, 49f),
            new Vector3(10f, 1.3f, 62.5f), new Vector3(27f, 1.3f, 49.5f), new Vector3(38f, 1.3f, 62.5f), new Vector3(48f, 1.3f, 74f),
            new Vector3(33f, 1.3f, 86f), new Vector3(56f, 1.3f, 105.5f), new Vector3(86f, 1.3f, 115f), new Vector3(0f, 1.3f, -9f) });
        Marcadores();
        Cutscenes();

        // ---------- vegetação espalhada ----------
        registrarOcupacao = false;
        Ocupar(Modelo("V06_Monguba", PH + "pachira_aquatica_01/pachira_aquatica_01_1k.fbx", new Vector3(50.5f, 0f, 109.5f), 0f, 1.7f, veg));
        Ocupar(Modelo("V12_Arbusto_folha_larga", PH + "shrub_04/shrub_04_1k.fbx", new Vector3(-6.4f, 0f, 14f), 0f, 3.2f, veg));
        Ocupar(Modelo("V14_Anturio", PH + "anthurium_botany_01/anthurium_botany_01_1k.fbx", new Vector3(5.8f, 0f, 33f), 0f, 1.22f, veg));
        Ocupar(Modelo("V15_Calathea", PH + "calathea_orbifolia_01/calathea_orbifolia_01_1k.fbx", new Vector3(-5.9f, 0f, 49f), 0f, 1.41f, veg));
        Vegetacao(veg, new[] { -36f, -32f, 148f, 144f }, 440, 330);

        // ---------- iluminação, HUD, Nilo, câmera, GameManager ----------
        Finalizar(scene, Ambiente.Noite, new Vector3(0f, 0.05f, -3f), 0f,
            "Nível 1 — Trilha encoberta", "chegar ao posto e ativar a estação do viveiro", 2,
            "Ativar a estação do viveiro", "Desligar a sentinela da Vigília", "Usar o terminal de saída",
            "Nível 1 concluído", "A trilha da infância ficou para trás. Próxima parada: a margem do rio.", false,
            "Assets/Settings/PerfilPosProcessamento.asset");
    }

    // Abre a cena da fase, apaga o cenário anterior e cria as raízes da hierarquia.
    private static UnityEngine.SceneManagement.Scene IniciarCena(string path, string pastaLocais, int semente, int sementeVariacao)
    {
        var scene = EditorSceneManager.OpenScene(path);
        rng = new System.Random(semente);
        rngVar = new System.Random(sementeVariacao);
        ocupados.Clear(); registrarOcupacao = false;
        CarregarMateriais();
        foreach (var nome in new[] { "Terreno", "Perigos", "Tutorial", "Cenario", "Cenario_Assets", "Chefe", "Luzes", "Coletaveis", "Volume_Global",
                                     "Cutscenes", "Canvas_Cutscene", "Antena_Serra", "Marcadores", "NPCs" })
        {
            var velho = GameObject.Find(nome);
            if (velho != null) Object.DestroyImmediate(velho);
        }
        terreno = Raiz("Terreno");
        perigos = Raiz("Perigos");
        tutorial = Raiz("Tutorial");
        cenario = Raiz("Cenario");
        assets = Raiz("Cenario_Assets");
        chefe = Raiz("Chefe");
        luzes = Raiz("Luzes");
        coletaveis = Raiz("Coletaveis");
        veg = Filho(assets, "Vegetacao").transform;
        rochas = Filho(assets, "Rochas").transform;
        constr = Filho(assets, "Construcoes").transform;
        tec = Filho(assets, "Tecnologia").transform;
        recup = Filho(assets, "Recuperacao").transform;
        apoio = Filho(assets, "Apoio").transform;
        armad = Filho(assets, "Armadilhas").transform;
        locais = Filho(assets, "Locais_" + pastaLocais.Replace("Locais_", "")).transform;
        return scene;
    }

    // Chão da trilha, fundo dos fossos (com água, se pedido), chão da mata recortado nos fossos e nas águas,
    // espelhos d'água e paredes invisíveis no contorno da área jogável.
    private static void Chao(bool fossosComAgua)
    {
        for (int i = 0; i < Trilha.Length; i++)
        {
            var r = Trilha[i];
            Laje(terreno, "Trilha_" + (i < 26 ? ((char)('A' + i)).ToString() : i.ToString()), new Vector3((r[0] + r[2]) / 2f, -0.235f, (r[1] + r[3]) / 2f), new Vector3(r[2] - r[0], 0.5f, r[3] - r[1]), matChao);
        }
        // Os fossos atravessam o chão da mata: rebaixo de 3 m com fundo próprio.
        for (int i = 0; i < Fossos.Length; i++)
        {
            var f = Fossos[i];
            var centro = new Vector3((f[0] + f[2]) / 2f, 0f, (f[1] + f[3]) / 2f);
            // fundo 3 m abaixo; as laterais são o próprio chão da mata (3 m de espessura), recortado em faixas
            Laje(terreno, "Fosso_" + (i + 1) + "_Fundo", centro + Vector3.down * 3.25f, new Vector3(f[2] - f[0], 0.5f, f[3] - f[1]), matFosso);
            if (fossosComAgua) Espelho("Agua_Fosso_" + (i + 1), f);
        }
        for (int i = 0; i < Aguas.Length; i++)
        {
            var a = Aguas[i];
            Laje(terreno, "Leito_" + (i + 1), new Vector3((a[0] + a[2]) / 2f, -2.4f, (a[1] + a[3]) / 2f), new Vector3(a[2] - a[0], 0.5f, a[3] - a[1]), matFosso);
            Espelho("Agua_" + (i + 1), a);
        }
        // chão da mata em faixas: grade com as bordas de todos os buracos, juntando células vizinhas
        var buracos = new List<float[]>(Fossos); buracos.AddRange(Aguas);
        var xs = new SortedSet<float> { areaMata[0], areaMata[2] };
        var zs = new SortedSet<float> { areaMata[1], areaMata[3] };
        foreach (var b in buracos) { xs.Add(b[0]); xs.Add(b[2]); zs.Add(b[1]); zs.Add(b[3]); }
        var lx = new List<float>(xs); var lz = new List<float>(zs);
        for (int j = 0; j + 1 < lz.Count; j++)
        {
            float z0 = lz[j], z1 = lz[j + 1], inicio = float.NaN;
            for (int i = 0; i + 1 < lx.Count; i++)
            {
                float cx = (lx[i] + lx[i + 1]) / 2f, cz = (z0 + z1) / 2f;
                bool buraco = false;
                foreach (var b in buracos) if (cx > b[0] && cx < b[2] && cz > b[1] && cz < b[3]) { buraco = true; break; }
                if (!buraco && float.IsNaN(inicio)) inicio = lx[i];
                if (buraco && !float.IsNaN(inicio)) { Faixa(terreno, inicio, z0, lx[i], z1); inicio = float.NaN; }
            }
            if (!float.IsNaN(inicio)) Faixa(terreno, inicio, z0, lx[lx.Count - 1], z1);
        }
        Paredes();
    }

    // Paredes invisíveis no contorno da área jogável (trilha + fossos), achado numa grade de 0,5 m:
    // cada aresta entre uma célula de dentro e uma de fora vira parede; arestas seguidas viram um bloco só.
    private static void Paredes()
    {
        const float c = 0.5f;
        var area = new List<float[]>(Trilha); area.AddRange(Fossos);
        float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
        foreach (var r in area) { x0 = Mathf.Min(x0, r[0]); z0 = Mathf.Min(z0, r[1]); x1 = Mathf.Max(x1, r[2]); z1 = Mathf.Max(z1, r[3]); }
        x0 -= c; z0 -= c;
        int nx = Mathf.CeilToInt((x1 - x0) / c) + 2, nz = Mathf.CeilToInt((z1 - z0) / c) + 2;
        var dentro = new bool[nx, nz];
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                float cx = x0 + (i + 0.5f) * c, cz = z0 + (j + 0.5f) * c;
                foreach (var r in area) if (cx > r[0] && cx < r[2] && cz > r[1] && cz < r[3]) { dentro[i, j] = true; break; }
            }
        System.Func<int, int, bool> d = (i, j) => i >= 0 && j >= 0 && i < nx && j < nz && dentro[i, j];
        int n = 0;
        // arestas verticais (x constante)
        for (int i = 0; i <= nx; i++)
        {
            int ini = -1;
            for (int j = 0; j <= nz; j++)
            {
                bool borda = j < nz && d(i - 1, j) != d(i, j);
                if (borda && ini < 0) ini = j;
                if (!borda && ini >= 0) { ParedeInvisivel(++n, new Vector3(x0 + i * c, 2.5f, z0 + (ini + j) * c / 2f), new Vector3(0.4f, 5f, (j - ini) * c)); ini = -1; }
            }
        }
        // arestas horizontais (z constante)
        for (int j = 0; j <= nz; j++)
        {
            int ini = -1;
            for (int i = 0; i <= nx; i++)
            {
                bool borda = i < nx && d(i, j - 1) != d(i, j);
                if (borda && ini < 0) ini = i;
                if (!borda && ini >= 0) { ParedeInvisivel(++n, new Vector3(x0 + (ini + i) * c / 2f, 2.5f, z0 + j * c), new Vector3((i - ini) * c, 5f, 0.4f)); ini = -1; }
            }
        }
    }

    private static void ParedeInvisivel(int n, Vector3 centro, Vector3 tam)
    {
        var parede = new GameObject("Parede_Invisivel_" + n);
        parede.transform.SetParent(terreno);
        parede.transform.position = centro;
        parede.layer = LayerMask.NameToLayer("Ignore Raycast");
        parede.AddComponent<BoxCollider>().size = tam;
    }

    // Espelho d'água sem colisor, um pouco abaixo do chão.
    private static void Espelho(string nome, float[] r)
    {
        var g = Caixa(terreno, nome, new Vector3((r[0] + r[2]) / 2f, -0.5f, (r[1] + r[3]) / 2f), new Vector3(r[2] - r[0], 0.04f, r[3] - r[1]), MaterialAgua());
        g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private static Material MaterialAgua()
    {
        const string path = "Assets/Materials/Agua.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", new Color(0.09f, 0.22f, 0.27f, 0.88f));
        m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        m.SetFloat("_Smoothness", 0.93f); m.SetFloat("_Metallic", 0.1f);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // Etapa final comum: ambiente, Nilo e GameManager, HUD, painel de missões, tela de conclusão,
    // pós-processamento e gravação da cena.
    private static void Finalizar(UnityEngine.SceneManagement.Scene scene, Ambiente amb, Vector3 inicio, float yaw,
        string nomeFase, string objetivo, int etapas, string mEstacao, string mChefe, string mSaida,
        string tituloConclusao, string textoConclusao, bool ultima, string perfilPos)
    {
        foreach (Transform t in veg) Assentar(t.gameObject, t.name.StartsWith("V19") ? -0.55f : 0.04f);
        foreach (Transform t in rochas) if (!t.name.StartsWith("R06") && !t.name.StartsWith("R07") && !t.name.StartsWith("R08")) Assentar(t.gameObject, 0.1f);
        ConfigurarAmbiente(amb, inicio, yaw);
        ConfigurarNilo(inicio, yaw, nomeFase, objetivo, etapas, mEstacao, mChefe, mSaida);
        ConfigurarHud();
        PainelMissoes();
        ConfigurarConclusao(tituloConclusao, textoConclusao, ultima);
        PosProcessamento(perfilPos, amb);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log(nomeFase + " montado e salvo.");
    }

    // ----------------------------------------------------------------------------------------
    private static void Fosso(Transform perigos, Transform armad, int n, float[] f, Vector3 respawn, bool comEstacas = true)
    {
        var centro = new Vector3((f[0] + f[2]) / 2f, 0f, (f[1] + f[3]) / 2f);
        var resp = new GameObject("Fosso_" + n + "_Retorno").transform;
        resp.SetParent(perigos); resp.position = respawn;
        var zona = new GameObject("Fosso_" + n + "_Zona");
        zona.transform.SetParent(perigos); zona.transform.position = centro + Vector3.down * 2f;
        zona.layer = LayerMask.NameToLayer("Hazard"); zona.tag = "Hazard";
        var bc = zona.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(f[2] - f[0], 1.6f, f[3] - f[1]);
        var hz = zona.AddComponent<Hazard>(); hz.mode = Hazard.Mode.HitAndRespawn; hz.respawnPoint = resp;
        for (int i = 0; i < (comEstacas ? 24 : 0); i++)
        {
            float x = Mathf.Lerp(f[0] + 0.4f, f[2] - 0.4f, (float)rng.NextDouble());
            float z = Mathf.Lerp(f[1] + 0.3f, f[3] - 0.3f, (float)rng.NextDouble());
            var e = Modelo("A01_Estaca_" + n + "_" + (i + 1), "Assets/Terceiros/OpenGameArt/WoodKit1/Stick_2.fbx", new Vector3(x, -3f, z), (float)rng.NextDouble() * 360f, 0.4f, armad);
            e.transform.localScale = new Vector3(0.25f + (float)rng.NextDouble() * 0.6f, 0.6f, 0.25f + (float)rng.NextDouble() * 0.6f);
            Caixa(perigos, "Estaca", new Vector3(x, -2.3f, z), new Vector3(0.1f, 1.4f, 0.1f), matEstaca);
        }
    }

    private static TroncoGiratorio TroncoGiratorio(Transform perigos, Transform armad, Vector3 pos)
    {
        var raiz = new GameObject("Tronco_Giratorio");
        raiz.transform.SetParent(perigos); raiz.transform.position = pos;
        raiz.AddComponent<Rigidbody>();
        var tg = raiz.AddComponent<TroncoGiratorio>();
        var cue = raiz.AddComponent<AudioSource>(); cue.playOnAwake = false; cue.spatialBlend = 1f; cue.maxDistance = 25f;
        cue.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/tronco_aviso.wav"); tg.cue = cue;
        Caixa(raiz.transform, "Poste", pos + Vector3.up * 1.6f, new Vector3(0.35f, 3.2f, 0.35f), matMadeira, "Ground", "Ground");
        var tronco = Caixa(raiz.transform, "Tronco", pos + new Vector3(1.8f, 0.7f, 0f), new Vector3(6.4f, 0.6f, 0.6f), matTronco, true);
        var col = tronco.GetComponent<BoxCollider>(); col.isTrigger = true; col.size = new Vector3(1f, 1.3f, 1.2f);
        tronco.layer = LayerMask.NameToLayer("Hazard"); tronco.tag = "Hazard";
        var hz = tronco.AddComponent<Hazard>(); hz.mode = Hazard.Mode.Hit;
        var vis = Modelo("A03_Tronco_visual", "Assets/Terceiros/PolyHaven/dead_tree_trunk/dead_tree_trunk_1k.fbx", pos + new Vector3(1.8f, 0.7f, 0f), 0f, 1f, armad);
        vis.transform.localScale = new Vector3(74.92f, 370.63f, 354.15f);
        vis.transform.rotation = Quaternion.Euler(0f, 0f, 270f);
        vis.transform.SetParent(raiz.transform, true);
        return tg;
    }

    private static void Pilao(Transform perigos, Transform cenario, Transform armad, Vector3 pos)
    {
        for (int k = -1; k <= 1; k += 2) Caixa(cenario, "Pilao_Suporte", pos + new Vector3(k * 4.1f, 2.3f, 0f), new Vector3(0.2f, 4.6f, 0.2f), matMadeira);
        Caixa(cenario, "Pilao_Viga", pos + Vector3.up * 4.6f, new Vector3(8.4f, 0.2f, 0.2f), matMadeira);
        var vig = Modelo("A04_Vigas", "Assets/Terceiros/OpenGameArt/WoodKit1/Planks.fbx", pos + new Vector3(0f, 4.7f, 0f), 0f, 1f, armad);
        vig.transform.localScale = new Vector3(1.67f, 1.25f, 0.23f);
        var tronco = new GameObject("Pilao");
        tronco.transform.SetParent(perigos); tronco.transform.position = pos + Vector3.up * 2.7f;
        tronco.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        tronco.layer = LayerMask.NameToLayer("Hazard"); tronco.tag = "Hazard";
        var mf = tronco.AddComponent<MeshFilter>(); mf.sharedMesh = PrimitiveMesh(PrimitiveType.Cylinder);
        tronco.AddComponent<MeshRenderer>().sharedMaterial = matTronco;
        tronco.transform.localScale = new Vector3(0.7f, 3.6f, 0.7f);
        var cc = tronco.AddComponent<CapsuleCollider>(); cc.isTrigger = true; cc.radius = 0.6f; cc.height = 2.2f;
        tronco.AddComponent<Rigidbody>();
        var mh = tronco.AddComponent<MovingHazard>(); mh.travel = new Vector3(0f, -2.3f, 0f);
        var hz = tronco.AddComponent<Hazard>(); hz.mode = Hazard.Mode.Hit;
        var cue = tronco.AddComponent<AudioSource>(); cue.playOnAwake = false; cue.spatialBlend = 1f; cue.maxDistance = 25f;
        cue.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/tronco_aviso.wav"); mh.cue = cue;
    }

    private static void Sinalizador(Transform cenario, Vector3 pos)
    {
        Caixa(cenario, "Sinalizador_Poste", pos + new Vector3(-2.7f, 1.8f, 0f), new Vector3(0.15f, 3.6f, 0.15f), matMadeira);
        Caixa(cenario, "Sinalizador_Poste", pos + new Vector3(2.7f, 1.8f, 0f), new Vector3(0.15f, 3.6f, 0.15f), matMadeira);
        Caixa(cenario, "Sinalizador_Cabo", pos + Vector3.up * 3.5f, new Vector3(5.4f, 0.03f, 0.03f), matTerminal);
        var s = Caixa(cenario, "Sinalizador", pos + new Vector3(-2.2f, 3.2f, 0f), new Vector3(0.3f, 0.3f, 0.3f), matMarco);
        var sm = s.AddComponent<SceneryMotion>(); sm.direction = Vector3.right; sm.speed = 1f; sm.distance = 4.4f;
        var helice = Caixa(s.transform, "Sinalizador_Helice", s.transform.position + new Vector3(0f, 0f, -0.2f), new Vector3(2.2f, 0.25f, 0.2f), matMadeira);
        sm.spinner = helice.transform; sm.spinAxis = Vector3.forward; sm.spinSpeed = 180f;
        var lamp = Modelo("C08_Lampiao_sinalizador", "Assets/Terceiros/PolyHaven/wooden_lantern_01/wooden_lantern_01_1k.fbx", s.transform.position + Vector3.down * 0.3f, 0f, 0.85f / 0.3f, s.transform);
    }

    private static void Sentinela(Transform chefe, Transform luzes, Transform tec, Transform constr, Transform veg, GameObject portao)
    {
        CriarSentinela(new Vector3(100f, 0f, 98f), -90f, new Vector3(82.5f, 0f, 86.5f), new Vector3(105.5f, 0f, 109.5f), portao);
        ArenaN1();
    }

    // Máquina da sentinela (chefe): corpo, lâmpada, zona de impacto da carga, braço do giro, células e bateria.
    // Montada na origem (coordenadas locais = mundo) e levada para pos/yaw no fim; arena = limites do movimento.
    private static SentinelaChefe CriarSentinela(Vector3 pos, float yaw, Vector3 arenaMin, Vector3 arenaMax, GameObject portao)
    {
        var raiz = new GameObject("Sentinela");
        raiz.transform.SetParent(chefe);
        raiz.AddComponent<Rigidbody>();
        var corpo = raiz.AddComponent<BoxCollider>(); corpo.center = new Vector3(0f, 1f, 0f); corpo.size = new Vector3(2.4f, 2f, 3.4f);
        var boss = raiz.AddComponent<SentinelaChefe>();
        boss.arenaMin = arenaMin; boss.arenaMax = arenaMax;

        Caixa(raiz.transform, "Chassi", new Vector3(0f, 0.9f, 0f), new Vector3(2.4f, 1f, 3.2f), matTerminal, false);
        Caixa(raiz.transform, "Cabine", new Vector3(0f, 1.8f, -0.5f), new Vector3(1.8f, 0.8f, 1.5f), matTerminal, false);
        Caixa(raiz.transform, "Para_choque", new Vector3(0f, 0.55f, 1.75f), new Vector3(2.6f, 0.6f, 0.4f), matPerigo, false);
        Caixa(raiz.transform, "Grade", new Vector3(0f, 1.2f, 1.62f), new Vector3(1.6f, 0.5f, 0.1f), matEstaca, false);
        Caixa(raiz.transform, "Olho_E", new Vector3(-0.6f, 2.0f, 0.3f), new Vector3(0.3f, 0.2f, 0.1f), matLedVermelho, false);
        Caixa(raiz.transform, "Olho_D", new Vector3(0.6f, 2.0f, 0.3f), new Vector3(0.3f, 0.2f, 0.1f), matLedVermelho, false);
        Caixa(raiz.transform, "Antena", new Vector3(0.7f, 2.9f, -1f), new Vector3(0.05f, 1.4f, 0.05f), matEstaca, false);
        for (int i = 0; i < 4; i++)
        {
            var roda = new GameObject("Roda_" + (i + 1));
            roda.transform.SetParent(raiz.transform);
            roda.transform.localPosition = new Vector3(i % 2 == 0 ? -1.25f : 1.25f, 0.5f, i < 2 ? 1.1f : -1.1f);
            roda.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            roda.transform.localScale = new Vector3(1f, 0.3f, 1f);
            roda.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh(PrimitiveType.Cylinder);
            roda.AddComponent<MeshRenderer>().sharedMaterial = matRocha;
        }
        ModeloAlvo("T09_Holofote_sentinela", PH + "portable_searchlight/portable_searchlight_1k.fbx", new Vector3(0f, 2.2f, -0.4f), 0f, 0.8f, false, raiz.transform, false);
        var lampGo = new GameObject("Lampada");
        lampGo.transform.SetParent(raiz.transform); lampGo.transform.localPosition = new Vector3(0f, 2.6f, 0.4f);
        var lamp = lampGo.AddComponent<Light>(); lamp.type = LightType.Point; lamp.range = 14f; lamp.intensity = 3f; lamp.color = boss.corMira;
        boss.lampada = lamp;
        var lampVis = Caixa(raiz.transform, "Lampada_Visual", new Vector3(0f, 2.55f, 0.4f), new Vector3(0.35f, 0.35f, 0.35f), matLedVermelho, false);
        // zona de dano durante o avanço
        var zona = new GameObject("Zona_Impacto");
        zona.transform.SetParent(raiz.transform); zona.transform.localPosition = new Vector3(0f, 1f, 0.4f); zona.transform.localRotation = Quaternion.identity;
        zona.layer = LayerMask.NameToLayer("Hazard"); zona.tag = "Hazard";
        var zc = zona.AddComponent<BoxCollider>(); zc.isTrigger = true; zc.size = new Vector3(3.2f, 2.2f, 4.8f);
        zona.AddComponent<Hazard>().mode = Hazard.Mode.Hit;
        boss.hazardZone = zona;
        // braço de varredura do ataque de giro: faixa listrada que se estende à frente e gira com a máquina
        var braco = new GameObject("Braco_Giro");
        braco.transform.SetParent(raiz.transform); braco.transform.position = new Vector3(0f, 0.6f, 2.95f);
        braco.layer = LayerMask.NameToLayer("Hazard"); braco.tag = "Hazard";
        var bcol = braco.AddComponent<BoxCollider>(); bcol.isTrigger = true; bcol.size = new Vector3(0.9f, 1.1f, 2.9f);
        braco.AddComponent<Hazard>().mode = Hazard.Mode.Hit;
        Caixa(braco.transform, "Braco_Barra", new Vector3(0f, 0.6f, 2.95f), new Vector3(0.45f, 0.4f, 2.8f), matPerigo, false);
        Caixa(braco.transform, "Braco_Ponta", new Vector3(0f, 0.65f, 4.3f), new Vector3(0.85f, 0.85f, 0.45f), matLedVermelho, false);
        boss.bracoGiro = braco;
        // baterias nas costas
        var celulas = new GameObject[3];
        for (int i = 0; i < 3; i++)
            celulas[i] = Caixa(raiz.transform, "Celula_" + (i + 1), new Vector3(-0.7f + i * 0.7f, 1.35f, -1.75f), new Vector3(0.45f, 0.6f, 0.3f), matLedVerde, false);
        boss.celulas = celulas;
        var bat = new GameObject("Bateria");
        bat.transform.SetParent(raiz.transform); bat.transform.localPosition = new Vector3(0f, 1f, -2.2f);
        bat.tag = "Interactable";
        var bs = bat.AddComponent<BateriaSentinela>(); bs.sentinela = boss; bs.prompt = "E — retirar bateria"; bs.range = 2.6f;
        boss.bateriaInteragivel = bat;
        // sons
        boss.somAviso = Som(raiz, "Assets/Audio/tronco_aviso.wav");
        boss.somBatida = Som(raiz, "Assets/Audio/caixa.wav");
        boss.somDesligar = Som(raiz, "Assets/Audio/estacao_ok.wav");
        boss.desativarAoDesligar = portao != null ? new[] { portao } : new GameObject[0];
        raiz.transform.position = pos;
        raiz.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        return boss;
    }

    private static void ArenaN1()
    {
        // cenário da arena: torre de observação, tochas, cerca e detritos nas bordas
        var torre = new GameObject("Torre_Observacao"); torre.transform.SetParent(chefe); torre.transform.position = new Vector3(105f, 0f, 109.5f);
        for (int k = 0; k < 4; k++) Caixa(torre.transform, "Torre_Pe", new Vector3(105f + (k % 2 == 0 ? -1.2f : 1.2f), 2.5f, 109.5f + (k < 2 ? -1.2f : 1.2f)), new Vector3(0.25f, 5f, 0.25f), matMadeira);
        Caixa(torre.transform, "Torre_Piso", new Vector3(105f, 5f, 109.5f), new Vector3(3.2f, 0.2f, 3.2f), matMadeira);
        Caixa(torre.transform, "Torre_Grade", new Vector3(105f, 5.6f, 108f), new Vector3(3.2f, 1f, 0.08f), matMadeira);
        Caixa(torre.transform, "Torre_Teto", new Vector3(105f, 7.2f, 109.5f), new Vector3(3.6f, 0.15f, 3.6f), matMadeira);
        ModeloAlvo("T09_Holofote_torre", PH + "portable_searchlight/portable_searchlight_1k.fbx", new Vector3(105f, 5.1f, 108.6f), 180f, 0.8f, false, tec, false);
        Luz(luzes, "Luz_Torre", new Vector3(104.5f, 5.4f, 107.8f), new Color(0.85f, 0.95f, 1f), 3f, 18f);
        Modelo("T02_Sensor_camera_arena", "Assets/Terceiros/PolyHaven/security_camera_01/security_camera_01_1k.fbx", new Vector3(81f, 3.2f, 111f), 150f, 1.22f, tec);
        Caixa(chefe, "Poste_camera_arena", new Vector3(81.0f, 1.53f, 111.0f), new Vector3(0.12f, 3.05f, 0.12f), matTerminal);   // suporte da câmera
        var cantos = new[] { new Vector3(82f, 0f, 86f), new Vector3(106f, 0f, 86f), new Vector3(82f, 0f, 110f), new Vector3(106f, 0f, 110f) };
        for (int i = 0; i < 4; i++)
        {
            Caixa(chefe, "Tocha_Poste", cantos[i] + Vector3.up * 1.2f, new Vector3(0.12f, 2.4f, 0.12f), matMadeira);
            Modelo("C08_Lampiao_arena_" + (i + 1), "Assets/Terceiros/PolyHaven/wooden_lantern_01/wooden_lantern_01_1k.fbx", cantos[i] + Vector3.up * 2.4f, 0f, 0.85f, constr);
            Luz(luzes, "Luz_Arena_" + (i + 1), cantos[i] + Vector3.up * 2.3f, new Color(1f, 0.6f, 0.3f), 2.2f, 13f);
        }
        for (int i = 0; i < 12; i++)   // painéis de 2 m enfileirados no fundo da arena
            Modelo("C10_Alambrado_arena_" + (i + 1), "Assets/Terceiros/PolyHaven/modular_chainlink_fence/modular_chainlink_fence_1k.fbx", new Vector3(83f + i * 2f, 0f, 112.3f), 0f, 1f, constr);
        ModeloAlvo("V09_Toco_arena", PH + "tree_stump_01/tree_stump_01_1k.fbx", new Vector3(83f, 0f, 114.5f), 40f, 0.7f, true, veg, false);
        Modelo("V08_Tronco_caido_arena", "Assets/Terceiros/PolyHaven/dead_tree_trunk_02/dead_tree_trunk_02_1k.fbx", new Vector3(96f, 0f, 81f), 0f, 0.99f, veg);
    }

    // Move o objeto na vertical para a base dos renderers ficar "afundar" metros abaixo do chão da mata (y = -0,05).
    private static void Assentar(GameObject go, float afundar)
    {
        if (go == null) return;
        Bounds? b = null;
        foreach (var r in go.GetComponentsInChildren<Renderer>(false))
        {
            if (b == null) b = r.bounds;
            else { var bb = b.Value; bb.Encapsulate(r.bounds); b = bb; }
        }
        if (b == null) return;
        go.transform.position += Vector3.up * (-0.05f - afundar - b.Value.min.y);
    }

    // distância do ponto à união dos retângulos da trilha (0 se está dentro)
    private static float DistanciaTrilha(float x, float z)
    {
        float best = float.MaxValue;
        foreach (var r in Trilha.Concat(Fossos))
        {
            float dx = Mathf.Max(r[0] - x, 0f, x - r[2]);
            float dz = Mathf.Max(r[1] - z, 0f, z - r[3]);
            best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dz * dz));
        }
        return best;
    }

    // Iluminação de cada fase: a noite avança até o amanhecer na antena.
    private class Ambiente
    {
        public Vector3 solRot; public Color sol, ambiente, nevoa, fundo, filtro; public float solInt, nevoaIni, nevoaFim, exposicao;
        public static readonly Ambiente Noite = new Ambiente { solRot = new Vector3(38f, 215f, 0f), sol = new Color(0.72f, 0.78f, 1f), solInt = 0.95f,
            ambiente = new Color(0.33f, 0.38f, 0.5f), nevoa = new Color(0.08f, 0.13f, 0.2f), nevoaIni = 16f, nevoaFim = 72f, fundo = new Color(0.07f, 0.11f, 0.2f),
            filtro = new Color(0.93f, 1f, 0.96f), exposicao = 0.15f };
        public static readonly Ambiente NoiteRio = new Ambiente { solRot = new Vector3(32f, 160f, 0f), sol = new Color(0.7f, 0.8f, 1f), solInt = 1.0f,
            ambiente = new Color(0.31f, 0.38f, 0.5f), nevoa = new Color(0.1f, 0.16f, 0.22f), nevoaIni = 14f, nevoaFim = 70f, fundo = new Color(0.08f, 0.13f, 0.21f),
            filtro = new Color(0.9f, 0.98f, 1f), exposicao = 0.15f };
        public static readonly Ambiente Ravina = new Ambiente { solRot = new Vector3(50f, 200f, 0f), sol = new Color(0.66f, 0.7f, 0.95f), solInt = 0.8f,
            ambiente = new Color(0.27f, 0.29f, 0.4f), nevoa = new Color(0.09f, 0.08f, 0.13f), nevoaIni = 12f, nevoaFim = 60f, fundo = new Color(0.06f, 0.06f, 0.12f),
            filtro = new Color(1f, 0.94f, 0.96f), exposicao = 0.2f };
        public static readonly Ambiente Madrugada = new Ambiente { solRot = new Vector3(18f, 95f, 0f), sol = new Color(0.86f, 0.76f, 1f), solInt = 1.0f,
            ambiente = new Color(0.38f, 0.35f, 0.5f), nevoa = new Color(0.22f, 0.18f, 0.3f), nevoaIni = 18f, nevoaFim = 80f, fundo = new Color(0.25f, 0.2f, 0.38f),
            filtro = new Color(0.98f, 0.95f, 1f), exposicao = 0.1f };
        public static readonly Ambiente Amanhecer = new Ambiente { solRot = new Vector3(14f, 70f, 0f), sol = new Color(1f, 0.74f, 0.52f), solInt = 1.5f,
            ambiente = new Color(0.56f, 0.47f, 0.44f), nevoa = new Color(0.78f, 0.56f, 0.46f), nevoaIni = 22f, nevoaFim = 110f, fundo = new Color(0.96f, 0.66f, 0.5f),
            filtro = new Color(1f, 0.95f, 0.88f), exposicao = 0.05f };
    }

    private static void ConfigurarAmbiente(Ambiente a, Vector3 inicio, float yaw)
    {
        var sol = GameObject.Find("Directional Light");
        if (sol != null)
        {
            var l = sol.GetComponent<Light>();
            sol.transform.rotation = Quaternion.Euler(a.solRot);
            l.color = a.sol;
            l.intensity = a.solInt;
            l.shadows = LightShadows.Soft;
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = a.ambiente;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = a.nevoaIni;
        RenderSettings.fogEndDistance = a.nevoaFim;
        RenderSettings.fogColor = a.nevoa;
        RenderSettings.skybox = null;
        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = a.fundo;
            cam.farClipPlane = Mathf.Max(120f, a.nevoaFim + 20f);
            var fc = cam.GetComponent<FollowCamera>();
            if (fc != null) { fc.obstacleMask = LayerMask.GetMask("Ground"); fc.distance = 4.8f; fc.height = 1.5f; fc.pitch = 14f; }
            var rot = Quaternion.Euler(0f, yaw, 0f);
            cam.transform.position = inicio + rot * new Vector3(0f, 2.6f, -4.5f);
            cam.transform.rotation = Quaternion.Euler(14f, yaw, 0f);
        }
    }

    private static void ConfigurarNilo(Vector3 pos, float yaw, string nomeFase, string objetivo, int etapas, string mEstacao, string mChefe, string mSaida)
    {
        var nilo = GameObject.Find("Nilo");
        if (nilo == null) return;
        nilo.transform.position = pos;
        var pc = nilo.GetComponent<PlayerController>();
        if (pc != null && pc.visual != null) pc.visual.rotation = Quaternion.Euler(0f, yaw, 0f);
        var antiga = nilo.transform.Find("Lanterna");
        if (antiga != null) Object.DestroyImmediate(antiga.gameObject);
        var lanterna = new GameObject("Lanterna");
        lanterna.transform.SetParent(nilo.transform); lanterna.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        var l = lanterna.AddComponent<Light>(); l.type = LightType.Point; l.range = 8f; l.intensity = 2.5f; l.color = new Color(1f, 0.85f, 0.6f);
        var gm = GameObject.Find("GameManager").GetComponent<GameManager>();
        gm.levelName = nomeFase;
        gm.stationsTotal = etapas;
        gm.invulnerableTime = 1.6f;
        gm.startObjective = objetivo;
        gm.missaoEstacao = mEstacao;
        gm.missaoChefe = mChefe;
        gm.missaoSaida = mSaida;
        EditorUtility.SetDirty(gm);
    }

    // Tela de conclusão: título, texto e botão principal (próxima fase, ou jogar de novo na última).
    private static void ConfigurarConclusao(string titulo, string texto, bool ultima)
    {
        var gm = GameObject.Find("GameManager").GetComponent<GameManager>();
        var painel = gm.completePanel.transform;
        var t = painel.Find("Titulo"); if (t != null) t.GetComponent<Text>().text = titulo;
        var sub = painel.Find("Subtitulo"); if (sub != null) sub.GetComponent<Text>().text = texto;
        var botao = painel.Find("Botao_Jogarnovamente");
        if (botao == null) return;
        var b = botao.GetComponent<Button>();
        while (b.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(b.onClick, 0);
        if (ultima) UnityEventTools.AddPersistentListener(b.onClick, gm.PlayFromStart);
        else UnityEventTools.AddPersistentListener(b.onClick, gm.ProximaFase);
        var rotulo = botao.GetComponentInChildren<Text>(true);
        if (rotulo != null) rotulo.text = ultima ? "Jogar novamente" : "Próxima fase";
        EditorUtility.SetDirty(b);
    }

    private static void ConfigurarHud()
    {
        var canvas = GameObject.Find("Canvas");
        var velho = canvas.transform.Find("HUD_Vidas");
        if (velho != null) Object.DestroyImmediate(velho.gameObject);
        var importer = AssetImporter.GetAtPath("Assets/UI/coracao.png") as TextureImporter;
        if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single || importer.filterMode != FilterMode.Point))
        {
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/coracao.png");
        var vidas = new GameObject("HUD_Vidas", typeof(RectTransform));
        vidas.transform.SetParent(canvas.transform, false);
        var rt = vidas.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(1f, 1f); rt.anchoredPosition = new Vector2(-30f, -30f); rt.sizeDelta = new Vector2(200f, 50f);
        var icons = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var h = new GameObject("Coracao_" + (i + 1), typeof(RectTransform));
            h.transform.SetParent(vidas.transform, false);
            var hr = h.GetComponent<RectTransform>();
            hr.anchorMin = hr.anchorMax = new Vector2(1f, 1f); hr.pivot = new Vector2(1f, 1f);
            hr.anchoredPosition = new Vector2(-i * 56f, 0f); hr.sizeDelta = new Vector2(48f, 48f);
            var img = h.AddComponent<Image>(); img.sprite = sprite; img.preserveAspect = true;
            icons[2 - i] = img;
        }
        var gm = GameObject.Find("GameManager").GetComponent<GameManager>();
        gm.heartIcons = icons;

        var velhoC = canvas.transform.Find("HUD_Coletaveis");
        if (velhoC != null) Object.DestroyImmediate(velhoC.gameObject);
        gm.coletaveisText = null;   // o contador das tarefas agora fica no painel de missões
    }

    // ----------------------------------------------------------------------------------------
    private static void CarregarMateriais()
    {
        matChao = Mat("Chao_Floresta"); matMadeira = Mat("Madeira"); matTronco = Mat("Tronco"); matGalho = Mat("Galho"); matMarco = Mat("Marco");
        matPerigo = Mat("Perigo"); matTerminal = Mat("Terminal"); matLedVerde = Mat("LedVerde"); matLedVermelho = Mat("LedVermelho"); matRocha = Mat("Rocha");
        matEstaca = Mat("Estaca"); matFosso = Mat("Fosso"); matLenco = Mat("Lenco"); matCaixote = Mat("Caixote"); matFolhagem = Mat("Folhagem");
        matMata = MaterialMata();
    }

    private static Material Mat(string nome)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + nome + ".mat");
        if (m == null) { var g = AssetDatabase.FindAssets(nome + " t:Material"); if (g.Length > 0) m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g[0])); }
        if (m == null) Debug.LogWarning("Material não encontrado: " + nome);
        return m;
    }

    private static Transform Raiz(string nome) { return new GameObject(nome).transform; }
    private static GameObject Filho(Transform pai, string nome) { var g = new GameObject(nome); g.transform.SetParent(pai); return g; }

    private static void Faixa(Transform pai, float x0, float z0, float x1, float z1)
    {
        Laje(pai, "Chao_Mata", new Vector3((x0 + x1) / 2f, -1.55f, (z0 + z1) / 2f), new Vector3(x1 - x0, 3f, z1 - z0), matMata);
    }

    // Bloco de chão com textura repetida a cada 2 m (o cubo padrão esticaria a textura no comprimento).
    private static GameObject Laje(Transform pai, string nome, Vector3 pos, Vector3 tam, Material mat)
    {
        var g = Caixa(pai, nome, pos, tam, mat, "Ground", "Ground");
        g.GetComponent<MeshFilter>().sharedMesh = MalhaCaixa(tam);
        return g;
    }

    private static Mesh MalhaCaixa(Vector3 tam)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Modelos")) AssetDatabase.CreateFolder("Assets", "Modelos");
        string path = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Assets/Modelos/Chao_{0:0.#}x{1:0.#}x{2:0.#}.asset", tam.x, tam.y, tam.z);
        var existente = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existente != null) return existente;
        const float tile = 2f;
        var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
        Vector3[] normais = { Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
        foreach (var n in normais)
        {
            Vector3 u = n == Vector3.up || n == Vector3.down ? Vector3.right : (n == Vector3.right || n == Vector3.left ? Vector3.forward : Vector3.right);
            Vector3 v = Vector3.Cross(n, u);
            float su = Vector3.Scale(u, tam).magnitude, sv = Vector3.Scale(v, tam).magnitude;
            int b = verts.Count;
            Vector3[] cantos = { -u - v, u - v, u + v, -u + v };
            Vector2[] cuv = { new Vector2(0, 0), new Vector2(su / tile, 0), new Vector2(su / tile, sv / tile), new Vector2(0, sv / tile) };
            for (int i = 0; i < 4; i++) { verts.Add((n + cantos[i]) * 0.5f); norms.Add(n); uvs.Add(cuv[i]); }
            // v = n × u, então (u, v, n) é destro e a ordem 0-1-2 fica com a normal geométrica para fora
            int[] q = { 0, 1, 2, 0, 2, 3 };
            foreach (var k in q) tris.Add(b + k);
        }
        var m = new Mesh { name = System.IO.Path.GetFileNameWithoutExtension(path) };
        m.SetVertices(verts); m.SetNormals(norms); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
        m.RecalculateBounds(); m.RecalculateTangents();
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // Caixa decorativa: sem colisor, para postes, placas e marcos não bloquearem o Nilo.
    private static GameObject Caixa(Transform pai, string nome, Vector3 pos, Vector3 tam, Material mat) { var g = Caixa(pai, nome, pos, tam, mat, "Default", "Untagged"); Object.DestroyImmediate(g.GetComponent<BoxCollider>()); return g; }
    private static GameObject Caixa(Transform pai, string nome, Vector3 pos, Vector3 tam, Material mat, bool colisor) { var g = Caixa(pai, nome, pos, tam, mat); if (colisor) g.AddComponent<BoxCollider>(); return g; }
    private static GameObject Caixa(Transform pai, string nome, Vector3 pos, Vector3 tam, Material mat, string layer, string tag)
    {
        var g = new GameObject(nome);
        g.transform.SetParent(pai);
        g.transform.position = pos;
        g.transform.localScale = tam;
        g.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh(PrimitiveType.Cube);
        var mr = g.AddComponent<MeshRenderer>(); if (mat != null) mr.sharedMaterial = mat;
        g.AddComponent<BoxCollider>();
        g.layer = LayerMask.NameToLayer(layer); g.tag = tag;
        return g;
    }

    private static Mesh PrimitiveMesh(PrimitiveType t)
    {
        var tmp = GameObject.CreatePrimitive(t);
        var m = tmp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(tmp);
        return m;
    }

    private static GameObject Placa(Transform cenario, string nome, Vector3 pos)
    {
        Caixa(cenario, nome + "_Poste", pos + Vector3.up * 0.5f, new Vector3(0.06f, 0.9f, 0.06f), matMadeira);
        return Caixa(cenario, nome, pos + Vector3.up * 1.1f, new Vector3(0.5f, 0.5f, 0.05f), matPerigo);
    }

    private static GameObject Gatilho(Transform pai, string nome, Vector3 pos, Vector3 tam, string msg, float dur)
    {
        var g = new GameObject(nome);
        g.transform.SetParent(pai); g.transform.position = pos;
        g.layer = LayerMask.NameToLayer("Ignore Raycast");
        var bc = g.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = tam;
        var t = g.AddComponent<TutorialTrigger>(); t.message = msg; t.duration = dur;
        return g;
    }

    private static Light Luz(Transform pai, string nome, Vector3 pos, Color cor, float intensidade, float alcance)
    {
        var g = new GameObject(nome); g.transform.SetParent(pai); g.transform.position = pos;
        var l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = cor; l.intensity = intensidade * 2.2f; l.range = alcance; l.shadows = LightShadows.None;
        return l;
    }

    private static AudioSource Som(GameObject g, string clipPath)
    {
        var a = g.AddComponent<AudioSource>(); a.playOnAwake = false; a.spatialBlend = 1f; a.maxDistance = 30f;
        a.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        return a;
    }

    // Instancia um modelo de terceiros com o ID do catálogo no nome, escala uniforme e limpeza de LODs/colisores auxiliares.
    private static GameObject Modelo(string nome, string path, Vector3 pos, float yaw, float escala, Transform pai)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) { Debug.LogWarning("Modelo não encontrado: " + path); var vazio = new GameObject(nome); vazio.transform.SetParent(pai); vazio.transform.position = pos; return vazio; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.name = nome;
        go.transform.SetParent(pai);
        go.transform.position = pos;
        // mantém a rotação da raiz do arquivo (muitos FBX vêm com -90° em X para converter Z para cima) e só soma o giro horizontal
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * asset.transform.localRotation;
        go.transform.localScale = Vector3.one * escala;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        var lod = go.GetComponent<LODGroup>();
        if (lod != null) Object.DestroyImmediate(lod);
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
        {
            if (t == go.transform) continue;
            string n = t.name.ToLowerInvariant();
            if (n.Contains("collision") || n.Contains("colbox") || n.EndsWith("_64") || n.EndsWith("_80") || n.EndsWith("_128") || n.EndsWith("_256"))
                t.gameObject.SetActive(false);
        }
        SimplificarKit(go, path);
        EscolherLod(go);
        if (path.Contains("Yughues_ArbustosTropicais"))
        {
            var mat = MaterialFolhagem(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileNameWithoutExtension(path));
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;
        }
        return go;
    }

    // Os FBX dos arbustos do Yughues não trazem material: cria um URP Lit recortado com a textura da pasta.
    private static Material MaterialFolhagem(string pasta, string nome)
    {
        string path = "Assets/Terceiros/_Materiais/" + nome + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var difusa = AssetDatabase.LoadAssetAtPath<Texture2D>(pasta + "/diffuse.png");
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(pasta + "/normal.png");
        var impN = AssetImporter.GetAtPath(pasta + "/normal.png") as TextureImporter;
        if (impN != null && impN.textureType != TextureImporterType.NormalMap) { impN.textureType = TextureImporterType.NormalMap; impN.SaveAndReimport(); normal = AssetDatabase.LoadAssetAtPath<Texture2D>(pasta + "/normal.png"); }
        mat.SetTexture("_BaseMap", difusa);
        mat.SetColor("_BaseColor", Color.white);
        if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
        mat.SetFloat("_AlphaClip", 1f); mat.SetFloat("_Cutoff", 0.5f); mat.EnableKeyword("_ALPHATEST_ON");
        mat.SetFloat("_Cull", 0f); mat.doubleSidedGI = true;
        mat.SetFloat("_Smoothness", 0.1f);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // Os pacotes do Poly Haven trazem várias variações ou peças lado a lado no mesmo arquivo.
    // Cada instância fica com uma variação só (centralizada); kits modulares ficam só com a peça útil.
    private static readonly Regex Variante = new Regex(@"_([a-z])(_LOD\d+)?$");
    private static readonly Regex NivelLod = new Regex(@"^(.*)_LOD(\d+)$", RegexOptions.IgnoreCase);
    private static readonly Regex Resolucao = new Regex(@"^(.*\D)(\d{2,3})$");
    private const int LimiteTriangulosLod = 22000;   // usa o LOD mais detalhado que fica abaixo disso

    private static void SimplificarKit(GameObject go, string path)
    {
        string arquivo = System.IO.Path.GetFileNameWithoutExtension(path);
        var filhos = new List<Transform>();
        foreach (Transform c in go.transform) filhos.Add(c);

        if (arquivo.StartsWith("modular_electricity_poles"))
        {
            // poste pronto "preset_01", deslocado para a origem
            foreach (var c in filhos)
            {
                bool fica = c.name.StartsWith("preset_01_");
                c.gameObject.SetActive(fica);
                if (fica) c.localPosition -= new Vector3(2.5f, 0f, 0f);
            }
            return;
        }
        if (arquivo.StartsWith("modular_chainlink_fence"))
        {
            // painel duplo (2 m); os painéis são enfileirados pelo construtor
            foreach (var c in filhos)
            {
                bool fica = c.name == "modular_chainlink_fence_double";
                c.gameObject.SetActive(fica);
                if (fica) c.localPosition = new Vector3(0f, c.localPosition.y, 0f);
            }
            return;
        }
        if (arquivo.StartsWith("modular_electric_cables"))
        {
            // só as peças que ficam apoiadas no chão
            foreach (var c in filhos) c.gameObject.SetActive(c.localPosition.y < 0.05f);
            return;
        }
        if (arquivo.StartsWith("wooden_barrels_01"))
        {
            // o kit traz dois barris e peças soltas: fica um barril só
            string barril = rngVar.Next(2) == 0 ? "barrel01" : "barrel02";
            foreach (var c in filhos)
            {
                bool fica = c.name.Contains(barril);
                c.gameObject.SetActive(fica);
                if (fica) c.localPosition = new Vector3(0f, c.localPosition.y, 0f);
            }
            return;
        }
        if (arquivo.StartsWith("pachira_aquatica"))
        {
            string letra = "abcd".Substring(rngVar.Next(4), 1);
            foreach (var c in filhos)
            {
                bool fica = c.name.EndsWith("_" + letra);
                c.gameObject.SetActive(fica);
                if (fica) c.localPosition = new Vector3(0f, c.localPosition.y, 0f);
            }
            return;
        }
        // Modelos Torque (3TD): a mesma peça em várias resoluções (Nome96, Nome128, Nome256...); fica a maior
        var resolucoes = new Dictionary<string, List<KeyValuePair<int, Transform>>>();
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
        {
            var m = Resolucao.Match(t.name);
            if (!m.Success) continue;
            int n = int.Parse(m.Groups[2].Value);
            if (n < 32) continue;
            string chave = t.parent.GetInstanceID() + "/" + m.Groups[1].Value;
            if (!resolucoes.ContainsKey(chave)) resolucoes[chave] = new List<KeyValuePair<int, Transform>>();
            resolucoes[chave].Add(new KeyValuePair<int, Transform>(n, t));
        }
        foreach (var g in resolucoes.Values)
        {
            if (g.Count < 2) continue;
            g.Sort((a, b) => b.Key.CompareTo(a.Key));
            for (int i = 1; i < g.Count; i++) g[i].Value.gameObject.SetActive(false);
        }

        var variantes = filhos.FindAll(c => Variante.IsMatch(c.name));
        if (variantes.Count >= 2 && variantes.Count == filhos.Count)
        {
            var escolhida = variantes[rngVar.Next(variantes.Count)];
            foreach (var c in variantes) c.gameObject.SetActive(c == escolhida);
            escolhida.localPosition = new Vector3(0f, escolhida.localPosition.y, 0f);
        }
    }

    // Para cada grupo nome_LOD0..n, liga o nível mais detalhado com até LimiteTriangulosLod triângulos.
    private static void EscolherLod(GameObject go)
    {
        var grupos = new Dictionary<string, List<KeyValuePair<int, Transform>>>();
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
        {
            var m = NivelLod.Match(t.name);
            if (!m.Success) continue;
            string chave = (t.parent != null ? t.parent.GetInstanceID().ToString() : "") + "/" + m.Groups[1].Value;
            if (!grupos.ContainsKey(chave)) grupos[chave] = new List<KeyValuePair<int, Transform>>();
            grupos[chave].Add(new KeyValuePair<int, Transform>(int.Parse(m.Groups[2].Value), t));
        }
        foreach (var g in grupos.Values)
        {
            g.Sort((a, b) => a.Key.CompareTo(b.Key));
            if (!g[0].Value.gameObject.activeSelf) { foreach (var kv in g) kv.Value.gameObject.SetActive(false); continue; }   // variação descartada
            int escolhido = g.Count - 1;
            for (int i = 0; i < g.Count; i++)
            {
                var mf = g[i].Value.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh.triangles.Length / 3 <= LimiteTriangulosLod) { escolhido = i; break; }
            }
            for (int i = 0; i < g.Count; i++) g[i].Value.gameObject.SetActive(i == escolhido);
        }
    }
}
