using System.Collections.Generic;
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

    // Retângulos de chão da trilha: xmin, zmin, xmax, zmax (topo em y = 0).
    private static readonly float[][] Trilha =
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
    private static readonly float[][] Fossos = { new[] { -4f, 40.5f, 4f, 42.5f }, new[] { 36f, 66f, 44f, 68f } };
    // Contorno da trilha (paredes invisíveis), em sequência de pontos x,z.
    private static readonly float[][] Contorno =
    {
        new[] { -4f, -8f }, new[] { 4f, -8f }, new[] { 4f, 52f }, new[] { 44f, 52f }, new[] { 44f, 92f }, new[] { 80f, 92f },
        new[] { 80f, 84f }, new[] { 108f, 84f }, new[] { 108f, 94f }, new[] { 116f, 94f }, new[] { 116f, 102f }, new[] { 108f, 102f },
        new[] { 108f, 112f }, new[] { 80f, 112f }, new[] { 80f, 104f }, new[] { 36f, 104f }, new[] { 36f, 60f }, new[] { -4f, 60f },
    };

    private static Material matChao, matMata, matMadeira, matTronco, matGalho, matMarco, matPerigo, matTerminal, matLedVerde, matLedVermelho, matRocha, matEstaca, matFosso, matLenco, matCaixote, matFolhagem;
    private static System.Random rng;
    private static System.Random rngVar;   // escolha de variação dos kits, separada para não mudar o espalhamento

    [MenuItem("Rastro do Dossel/Montar Nível 1 (3D)")]
    public static void Montar()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        rng = new System.Random(7);
        rngVar = new System.Random(3);
        ocupados.Clear(); registrarOcupacao = false;
        CarregarMateriais();

        foreach (var nome in new[] { "Terreno", "Perigos", "Tutorial", "Cenario", "Cenario_Assets", "Chefe", "Luzes", "Coletaveis", "Volume_Global" })
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
        locais = Filho(assets, "Locais_N1").transform;

        // ---------- chão, fossos e paredes ----------
        for (int i = 0; i < Trilha.Length; i++)
        {
            var r = Trilha[i];
            Laje(terreno, "Trilha_" + "ABCDEFGH"[i], new Vector3((r[0] + r[2]) / 2f, -0.235f, (r[1] + r[3]) / 2f), new Vector3(r[2] - r[0], 0.5f, r[3] - r[1]), matChao);
        }
        // Os fossos atravessam o chão da mata: rebaixo de 3 m com fundo próprio.
        for (int i = 0; i < Fossos.Length; i++)
        {
            var f = Fossos[i];
            var centro = new Vector3((f[0] + f[2]) / 2f, 0f, (f[1] + f[3]) / 2f);
            // fundo 3 m abaixo; as laterais são o próprio chão da mata (3 m de espessura), recortado em faixas
            Laje(terreno, "Fosso_" + (i + 1) + "_Fundo", centro + Vector3.down * 3.25f, new Vector3(f[2] - f[0], 0.5f, f[3] - f[1]), matFosso);
        }
        // chão da mata em faixas, deixando os dois fossos abertos
        float[] zc = { -60f, 40.5f, 42.5f, 66f, 68f, 160f };
        for (int b = 0; b < 5; b++)
        {
            float z0 = zc[b], z1 = zc[b + 1];
            if (b == 1) { Faixa(terreno, -60f, z0, -4f, z1); Faixa(terreno, 4f, z0, 160f, z1); }
            else if (b == 3) { Faixa(terreno, -60f, z0, 36f, z1); Faixa(terreno, 44f, z0, 160f, z1); }
            else Faixa(terreno, -60f, z0, 160f, z1);
        }
        for (int i = 0; i < Contorno.Length; i++)
        {
            var a = Contorno[i]; var b = Contorno[(i + 1) % Contorno.Length];
            var pa = new Vector3(a[0], 0f, a[1]); var pb = new Vector3(b[0], 0f, b[1]);
            var meio = (pa + pb) / 2f + Vector3.up * 2.5f;
            var tam = new Vector3(Mathf.Max(Mathf.Abs(pb.x - pa.x), 0.4f), 5f, Mathf.Max(Mathf.Abs(pb.z - pa.z), 0.4f));
            var parede = Caixa(terreno, "Parede_Invisivel_" + i, meio, tam, null, "Ignore Raycast", "Untagged");
            Object.DestroyImmediate(parede.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(parede.GetComponent<MeshFilter>());
        }
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
        Lanternas();
        VagaLumes();
        Marcadores();

        // ---------- vegetação espalhada ----------
        registrarOcupacao = false;
        Vegetacao(veg);
        // plantas e pedras apoiadas no chão pela base real do modelo (os pivôs variam de pacote para pacote)
        foreach (Transform t in veg) Assentar(t.gameObject, t.name.StartsWith("V19") ? -0.55f : 0.04f);
        foreach (Transform t in rochas) if (!t.name.StartsWith("R06") && !t.name.StartsWith("R07") && !t.name.StartsWith("R08")) Assentar(t.gameObject, 0.1f);

        // ---------- iluminação, HUD, Nilo, câmera, GameManager ----------
        ConfigurarAmbiente();
        ConfigurarNilo();
        ConfigurarHud();
        PainelMissoes();
        PosProcessamento();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Nível 1 (3D) montado e salvo.");
    }

    // ----------------------------------------------------------------------------------------
    private static void Fosso(Transform perigos, Transform armad, int n, float[] f, Vector3 respawn)
    {
        var centro = new Vector3((f[0] + f[2]) / 2f, 0f, (f[1] + f[3]) / 2f);
        var resp = new GameObject("Fosso_" + n + "_Retorno").transform;
        resp.SetParent(perigos); resp.position = respawn;
        var zona = new GameObject("Fosso_" + n + "_Zona");
        zona.transform.SetParent(perigos); zona.transform.position = centro + Vector3.down * 2f;
        zona.layer = LayerMask.NameToLayer("Hazard"); zona.tag = "Hazard";
        var bc = zona.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(f[2] - f[0], 1.6f, f[3] - f[1]);
        var hz = zona.AddComponent<Hazard>(); hz.mode = Hazard.Mode.HitAndRespawn; hz.respawnPoint = resp;
        for (int i = 0; i < 24; i++)
        {
            float x = Mathf.Lerp(f[0] + 0.4f, f[2] - 0.4f, (float)rng.NextDouble());
            float z = Mathf.Lerp(f[1] + 0.3f, f[3] - 0.3f, (float)rng.NextDouble());
            var e = Modelo("A01_Estaca_" + n + "_" + (i + 1), "Assets/Terceiros/OpenGameArt/WoodKit1/Stick_2.fbx", new Vector3(x, -3f, z), (float)rng.NextDouble() * 360f, 0.4f, armad);
            e.transform.localScale = new Vector3(0.25f + (float)rng.NextDouble() * 0.6f, 0.6f, 0.25f + (float)rng.NextDouble() * 0.6f);
            Caixa(perigos, "Estaca", new Vector3(x, -2.3f, z), new Vector3(0.1f, 1.4f, 0.1f), matEstaca);
        }
    }

    private static void TroncoGiratorio(Transform perigos, Transform armad, Vector3 pos)
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
        // montada na origem (coordenadas locais = mundo) e levada para a arena no fim
        var raiz = new GameObject("Sentinela");
        raiz.transform.SetParent(chefe);
        raiz.AddComponent<Rigidbody>();
        var corpo = raiz.AddComponent<BoxCollider>(); corpo.center = new Vector3(0f, 1f, 0f); corpo.size = new Vector3(2.4f, 2f, 3.4f);
        var boss = raiz.AddComponent<SentinelaChefe>();
        boss.arenaMin = new Vector3(82.5f, 0f, 86.5f); boss.arenaMax = new Vector3(105.5f, 0f, 109.5f);

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
        boss.desativarAoDesligar = new[] { portao };
        raiz.transform.position = new Vector3(100f, 0f, 98f);
        raiz.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

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
        foreach (var r in Trilha)
        {
            float dx = Mathf.Max(r[0] - x, 0f, x - r[2]);
            float dz = Mathf.Max(r[1] - z, 0f, z - r[3]);
            best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dz * dz));
        }
        return best;
    }

    // distância de um ponto interno ao contorno da trilha (aproximada pelas arestas)
    private static float DistanciaBorda(float x, float z)
    {
        float best = float.MaxValue;
        for (int i = 0; i < Contorno.Length; i++)
        {
            var a = Contorno[i]; var b = Contorno[(i + 1) % Contorno.Length];
            var p = new Vector2(x, z); var pa = new Vector2(a[0], a[1]); var pb = new Vector2(b[0], b[1]);
            float t = Mathf.Clamp01(Vector2.Dot(p - pa, pb - pa) / (pb - pa).sqrMagnitude);
            best = Mathf.Min(best, Vector2.Distance(p, pa + t * (pb - pa)));
        }
        return best;
    }

    private static void ConfigurarAmbiente()
    {
        var sol = GameObject.Find("Directional Light");
        if (sol != null)
        {
            var l = sol.GetComponent<Light>();
            // fim de tarde azulado: a noite da missão, ainda legível
            sol.transform.rotation = Quaternion.Euler(38f, 215f, 0f);
            l.color = new Color(0.72f, 0.78f, 1f);
            l.intensity = 0.95f;
            l.shadows = LightShadows.Soft;
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.33f, 0.38f, 0.5f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 16f;
        RenderSettings.fogEndDistance = 72f;
        RenderSettings.fogColor = new Color(0.08f, 0.13f, 0.2f);
        RenderSettings.skybox = null;
        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.11f, 0.2f);
            cam.farClipPlane = 120f;
            var fc = cam.GetComponent<FollowCamera>();
            if (fc != null) { fc.obstacleMask = LayerMask.GetMask("Ground"); fc.distance = 4.8f; fc.height = 1.5f; fc.pitch = 14f; }
            cam.transform.position = new Vector3(0f, 2.6f, -7.5f);
            cam.transform.rotation = Quaternion.Euler(14f, 0f, 0f);
        }
    }

    private static void ConfigurarNilo()
    {
        var nilo = GameObject.Find("Nilo");
        if (nilo == null) return;
        nilo.transform.position = new Vector3(0f, 0.05f, -3f);
        var pc = nilo.GetComponent<PlayerController>();
        if (pc != null && pc.visual != null) pc.visual.rotation = Quaternion.identity;
        var antiga = nilo.transform.Find("Lanterna");
        if (antiga != null) Object.DestroyImmediate(antiga.gameObject);
        var lanterna = new GameObject("Lanterna");
        lanterna.transform.SetParent(nilo.transform); lanterna.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        var l = lanterna.AddComponent<Light>(); l.type = LightType.Point; l.range = 8f; l.intensity = 2.5f; l.color = new Color(1f, 0.85f, 0.6f);
        var gm = GameObject.Find("GameManager").GetComponent<GameManager>();
        gm.stationsTotal = 2;
        gm.invulnerableTime = 1.6f;
        gm.startObjective = "chegar ao posto e ativar a estação do viveiro";
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
