using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Parte do construtor do nível 1 que monta o cenário: os trechos da trilha, o posto, a floresta,
// as tarefas opcionais, lampiões, vaga-lumes e o pós-processamento.
public static partial class ConstrutorNivel1
{
    private static Transform terreno, perigos, tutorial, cenario, assets, chefe, luzes, coletaveis, veg, rochas, constr, tec, recup, apoio, armad, locais;
    private static GameObject portaoVigilia;
    private static bool registrarOcupacao;
    private static readonly List<Vector4> ocupados = new List<Vector4>();   // x, z, raio de objetos já colocados

    private const string PH = "Assets/Terceiros/PolyHaven/";
    private const string TGD = "Assets/Terceiros/OpenGameArt/3TD_Tropical/";
    private const string AUD = "Assets/Audio/";

    // ---------------------------------------------------------------------------------------------
    // Utilidades

    private static Bounds Limites(GameObject go)
    {
        Bounds? b = null;
        foreach (var r in go.GetComponentsInChildren<Renderer>(false))
        {
            if (b == null) b = r.bounds;
            else { var bb = b.Value; bb.Encapsulate(r.bounds); b = bb; }
        }
        return b ?? new Bounds(go.transform.position, Vector3.zero);
    }

    // Instancia um modelo com o tamanho real desejado, apoiado no chão em pos (y = altura da base).
    // Vários arquivos vêm em centímetros: a escala sai do tamanho alvo, não de um número fixo.
    private static GameObject ModeloAlvo(string nome, string path, Vector3 pos, float yaw, float alvo, bool porAltura, Transform pai, bool ocupar = true)
    {
        var go = Modelo(nome, path, pos, yaw, 1f, pai);   // Modelo já preserva a rotação da raiz do arquivo
        var b = Limites(go);
        float referencia = porAltura ? b.size.y : Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (referencia > 0.00001f) go.transform.localScale = Vector3.one * (alvo / referencia);
        b = Limites(go);
        go.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);
        if (registrarOcupacao && ocupar) Ocupar(go);
        return go;
    }

    private static void Ocupar(GameObject go)
    {
        var b = Limites(go);
        ocupados.Add(new Vector4(b.center.x, b.center.z, Mathf.Max(b.extents.x, b.extents.z) + 0.6f, 0f));
    }

    private static bool Ocupado(float x, float z, float extra)
    {
        foreach (var o in ocupados)
        {
            float dx = x - o.x, dz = z - o.y, r = o.z + extra;
            if (dx * dx + dz * dz < r * r) return true;
        }
        return false;
    }

    // Colisor de caixa (camada Ground) ajustado ao tamanho do objeto, para troncos e pedras baixos bloquearem o Nilo.
    private static void ColisorPorBounds(GameObject go, string nome, float reduz)
    {
        var b = Limites(go);
        var c = new GameObject("Colisor_" + nome);
        c.transform.SetParent(terreno);
        c.transform.position = b.center;
        c.layer = LayerMask.NameToLayer("Ground"); c.tag = "Ground";
        c.AddComponent<BoxCollider>().size = b.size - new Vector3(reduz, 0f, reduz);
    }

    // Colisor de caixa na camada Default (não vira chão nem bloqueia a câmera) para o Nilo não atravessar os objetos do posto.
    private static GameObject Solido(GameObject go)
    {
        var b = Limites(go);
        var c = new GameObject("Colisor_" + go.name);
        c.transform.SetParent(terreno);
        c.transform.position = b.center;
        c.AddComponent<BoxCollider>().size = b.size * 0.9f;
        return go;
    }

    private static void SemSombra(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
    }

    private static GameObject Cilindro(Transform pai, string nome, Vector3 pos, Vector3 escala, Quaternion rot, Material mat)
    {
        var g = new GameObject(nome);
        g.transform.SetParent(pai);
        g.transform.position = pos; g.transform.rotation = rot; g.transform.localScale = escala;
        g.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh(PrimitiveType.Cylinder);
        g.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return g;
    }

    // Poste de madeira com lampião e luz quente.
    private static void PosteLampiao(string nome, float x, float z, float altura)
    {
        Caixa(cenario, "Poste_" + nome, new Vector3(x, altura / 2f, z), new Vector3(0.14f, altura, 0.14f), matMadeira);
        SemSombra(ModeloAlvo(nome, PH + "wooden_lantern_01/wooden_lantern_01_1k.fbx", new Vector3(x, altura, z), 0f, 0.55f, true, constr, false));
        Luz(luzes, "Luz_" + nome, new Vector3(x, altura + 0.35f, z), new Color(1f, 0.72f, 0.4f), 1.7f, 10f);
    }

    // ---------------------------------------------------------------------------------------------
    // Trecho A: Várzea Alta

    private static void Inicio()
    {
        // portão da comunidade, com a largura da trilha
        Caixa(cenario, "N1-01_Portao_VarzeaAlta_Poste", new Vector3(-3.1f, 1.5f, -5f), new Vector3(0.32f, 3f, 0.32f), matMadeira);
        Caixa(cenario, "N1-01_Portao_VarzeaAlta_Poste", new Vector3(3.1f, 1.5f, -5f), new Vector3(0.32f, 3f, 0.32f), matMadeira);
        Caixa(cenario, "N1-01_Portao_VarzeaAlta_Viga", new Vector3(0f, 3f, -5f), new Vector3(6.9f, 0.3f, 0.38f), matMadeira);
        ModeloAlvo("C08_Lampiao", PH + "wooden_lantern_01/wooden_lantern_01_1k.fbx", new Vector3(-3.1f, 3f, -5f), 0f, 0.6f, true, constr, false);
        Luz(luzes, "Luz_Portao", new Vector3(-3.1f, 3.5f, -4.6f), new Color(1f, 0.75f, 0.4f), 1.8f, 9f);

        // cantinho da comunidade: fogueira, bancos e caixote
        ModeloAlvo("C07_Fogueira", PH + "stone_fire_pit/stone_fire_pit_1k.fbx", new Vector3(-1.2f, 0f, -7.2f), 0f, 1.5f, false, constr);
        Luz(luzes, "Luz_Fogueira", new Vector3(-1.2f, 1.0f, -7.2f), new Color(1f, 0.55f, 0.2f), 3f, 12f);
        var toco = ModeloAlvo("V09_Toco", PH + "tree_stump_01/tree_stump_01_1k.fbx", new Vector3(0.9f, 0f, -6.4f), 0f, 0.55f, true, veg);
        ColisorPorBounds(toco, "V09_Toco", 0.05f);
        Solido(ModeloAlvo("P02_Caixote_madeira", PH + "wooden_crate_02/wooden_crate_02_1k.fbx", new Vector3(3.0f, 0f, -6.6f), 20f, 0.9f, false, apoio));
        Solido(ModeloAlvo("P05_Banco", PH + "painted_wooden_bench/painted_wooden_bench_1k.fbx", new Vector3(-3.1f, 0f, -7.2f), 90f, 0.7f, true, apoio));

        // casas da comunidade escondida
        ModeloAlvo("C02_Cabana_selva", TGD + "CabanaSelva/JungleHut01.DAE", new Vector3(-10.3f, 0f, -4.5f), 90f, 5.6f, false, constr);
        ModeloAlvo("C02_Cabana_selva_2", TGD + "CabanaSelva/JungleHut01.DAE", new Vector3(10.3f, 0f, 1.5f), 270f, 5.6f, false, constr);
        ModeloAlvo("C03_Atracadouro", TGD + "Atracadouro/Tropical_Jetty01.DAE", new Vector3(-9.2f, 0f, 6.5f), 90f, 6.5f, false, constr);
        PosteLampiao("C08_Lampiao_casa_1", -5.4f, -1.2f, 2.3f);
        PosteLampiao("C08_Lampiao_casa_2", 5.4f, 4.5f, 2.3f);

        // paredão de pedra fechando o começo da trilha
        ModeloAlvo("R06_Paredao_inicio", PH + "rock_face_01/rock_face_01_1k.fbx", new Vector3(0f, 0f, -11f), 0f, 11f, false, rochas);

        // placa de controles e marco de treinamento
        Caixa(cenario, "Placa_Controles", new Vector3(3.3f, 1.2f, 1f), new Vector3(0.05f, 0.7f, 1.2f), matMadeira);
        Caixa(cenario, "Placa_Controles_Poste", new Vector3(3.3f, 0.5f, 1f), new Vector3(0.08f, 0.9f, 0.08f), matMadeira);
        Caixa(cenario, "N1-02_Marco_Treinamento", new Vector3(0f, 0.8f, 10f), new Vector3(0.15f, 1.5f, 0.15f), matMarco);

        // raiz atravessada na trilha: obstáculo de salto (colisor + visual com tora, raízes e musgo)
        Caixa(terreno, "Obstaculo_Raiz", new Vector3(0f, 0.3f, 18f), new Vector3(8f, 0.6f, 0.6f), matTronco, "Ground", "Ground").GetComponent<MeshRenderer>().enabled = false;
        Cilindro(cenario, "Raiz_Tora", new Vector3(0f, 0.3f, 18f), new Vector3(0.62f, 4f, 0.62f), Quaternion.Euler(0f, 0f, 90f), matTronco);
        var raizA = ModeloAlvo("V18_Raiz", PH + "single_root/single_root_1k.fbx", new Vector3(-1.8f, 0.18f, 18.1f), 90f, 2.4f, false, veg, false);
        ModeloAlvo("V18_Raiz_2", PH + "single_root/single_root_1k.fbx", new Vector3(1.6f, 0.2f, 17.9f), 80f, 2.2f, false, veg, false);
        ModeloAlvo("V19_Musgo", PH + "moss_01/moss_01_1k.fbx", new Vector3(0.4f, 0.6f, 18f), 0f, 1.6f, false, veg, false);

        // tronco caído, raiz e pedras na mata, perto do começo
        var tronco = ModeloAlvo("V08_Tronco_caido", PH + "dead_tree_trunk_02/dead_tree_trunk_02_1k.fbx", new Vector3(-1.6f, 0f, 29f), 0f, 4.2f, false, veg);
        ColisorPorBounds(tronco, "V08_Tronco", 0.1f);
        var tronco2 = ModeloAlvo("V08_Tronco_caido_2", PH + "dead_tree_trunk_02/dead_tree_trunk_02_1k.fbx", new Vector3(1.6f, 0f, 32f), 180f, 4.2f, false, veg);
        ColisorPorBounds(tronco2, "V08_Tronco_2", 0.1f);
        // raiz de árvore tombada (225 mil triângulos): fica atrás da cerca da arena, longe do começo da trilha
        ModeloAlvo("V10_Raizes", PH + "root_cluster_01/root_cluster_01_1k.fbx", new Vector3(96.5f, 0f, 116.8f), 200f, 2.6f, true, veg);
        ModeloAlvo("V20_Galhos_secos", PH + "dry_branches_medium_01/dry_branches_medium_01_1k.fbx", new Vector3(-6.2f, 0f, 24f), 20f, 1.2f, false, veg);
        ModeloAlvo("R01_Pedregulho", PH + "boulder_01/boulder_01_1k.fbx", new Vector3(-6.6f, 0f, 5f), 0f, 2.2f, false, rochas);
        ModeloAlvo("R09_Cascalho", PH + "stone_01/stone_01_1k.fbx", new Vector3(6.4f, 0f, 24f), 0f, 1.8f, false, rochas);
        ModeloAlvo("R02_Pedras_musgo_1", PH + "rock_moss_set_01/rock_moss_set_01_1k.fbx", new Vector3(7.8f, 0f, 11f), 0f, 3.6f, false, rochas);
        for (int i = 0; i < 6; i++)
            Modelo(i == 0 ? "C04_Cerca_estacas_1" : "C04_Cerca_estacas_" + (i + 1), "Assets/Terceiros/OpenGameArt/WoodKit1/Stick_1.fbx", new Vector3(-4.3f, 0.6f, 11f + i * 0.6f), i * 23f, 0.56f, constr);
    }

    // ---------------------------------------------------------------------------------------------
    // Trecho B: marca antiga e primeiro fosso

    private static void MarcaEFosso1()
    {
        var arvoreMarcada = Modelo("N1-03_Arvore_marcada", TGD + "Sumauma/CiabaTree_01.dae", new Vector3(-4.8f, 0f, 35f), 0f, 1.5f, locais);
        Ocupar(arvoreMarcada);
        Caixa(cenario, "Marca_Antiga_Nilo", new Vector3(-3.6f, 1.7f, 35f), new Vector3(0.05f, 0.3f, 0.3f), matLenco).transform.rotation = Quaternion.Euler(45f, 0f, 0f);
        PosteLampiao("C08_Lampiao_marca", -3.4f, 36.6f, 2.4f);
        Placa(cenario, "A05_Placa_perigo_1", new Vector3(3.4f, 0f, 38f));
        Fosso(perigos, armad, 1, Fossos[0], new Vector3(0f, 0f, 36.5f));
        for (int i = 0; i < 4; i++)
        {
            var painel = Modelo(i == 0 ? "C10_Alambrado_Vigilia" : "C10_Alambrado_Vigilia_" + (i + 1), PH + "modular_chainlink_fence/modular_chainlink_fence_1k.fbx", new Vector3(4.6f, 0f, 43f + i * 2f), 90f, 1f, constr);
            if (i == 0) Ocupar(painel);
        }
        var bloqueio = ModeloAlvo("C09_Bloqueio_Vigilia", PH + "concrete_road_barrier_02/concrete_road_barrier_02_1k.fbx", new Vector3(-3.1f, 0f, 47f), 15f, 1.6f, false, constr);
        ModeloAlvo("R04_Rocha_erodida", PH + "rock_07/rock_07_1k.fbx", new Vector3(-8.5f, 0f, 55f), 0f, 4.6f, false, rochas);
        var encosta = Modelo("R08_Encosta", PH + "mountainside/mountainside_1k.fbx", new Vector3(-52f, 0f, 30f), 270f, 1.76f, rochas);
        encosta.transform.localScale = new Vector3(1.76f, 0.52f, 1.33f);
    }

    // ---------------------------------------------------------------------------------------------
    // Trecho C: barreiras altas, tronco giratório

    private static void CorredorDosGalhos()
    {
        Galho(1, new Vector3(12f, 0f, 56f), true);
        Galho(2, new Vector3(19f, 0f, 56f), true);
        TroncoGiratorio(perigos, armad, new Vector3(32f, 0f, 56f));
        Luz(luzes, "Luz_Tronco", new Vector3(32f, 4.2f, 56f), new Color(1f, 0.6f, 0.3f), 1.8f, 10f);
        ModeloAlvo("R03_Pedras_musgo_2", PH + "rock_moss_set_02/rock_moss_set_02_1k.fbx", new Vector3(24f, 0f, 63.5f), 0f, 3.4f, false, rochas);
        var rochas3td = Modelo("R10_Rochas_tropicais", TGD + "Rochas/GroupRocks01.DAE", new Vector3(40f, 0f, 48.5f), 0f, 17.44f, rochas);
        Ocupar(rochas3td);
        ModeloAlvo("R05_Rocha_09", PH + "rock_09/rock_09_1k.fbx", new Vector3(48f, 0f, 56f), 0f, 5f, false, rochas);
    }

    // Barreira alta: fechada em cima e com um vão de 0,98 m embaixo. Só dá para passar deslizando
    // (a cápsula baixa tem 0,9 m); pular não adianta porque a parte de cima chega a 3,4 m.
    private static void Galho(int n, Vector3 pos, bool alongZ)
    {
        var raiz = new GameObject("Barreira_Galhos_" + n).transform;
        raiz.SetParent(perigos);
        const float vao = 0.98f, alto = 2.4f;
        var col = new GameObject("Galho_" + n);
        col.transform.SetParent(raiz);
        col.transform.position = pos + Vector3.up * (vao + alto / 2f);
        col.AddComponent<BoxCollider>().size = alongZ ? new Vector3(0.8f, alto, 8f) : new Vector3(8f, alto, 0.8f);

        Quaternion eixo = alongZ ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.Euler(0f, 0f, 90f);
        float[] alturas = { 1.38f, 2.0f, 2.62f };
        for (int i = 0; i < alturas.Length; i++)
        {
            float raio = 0.26f + 0.03f * ((i + n) % 3);
            Cilindro(raiz, "Tora_" + (i + 1), pos + Vector3.up * alturas[i], new Vector3(raio * 2f, 4f, raio * 2f), eixo, matTronco);
        }
        for (int k = -1; k <= 1; k += 2)
        {
            Vector3 ponta = alongZ ? new Vector3(0f, 1.7f, k * 4.1f) : new Vector3(k * 4.1f, 1.7f, 0f);
            Caixa(raiz, "Poste_" + (k < 0 ? "A" : "B"), pos + ponta, new Vector3(0.5f, 3.4f, 0.5f), matMadeira);
        }
        // galhos decorativos do catálogo (A02) presos às toras, como se a barreira fosse um emaranhado
        for (int i = 0; i < 2; i++)
        {
            float off = (i == 0 ? -1.6f : 1.9f);
            Vector3 p = pos + (alongZ ? new Vector3(0f, 1.3f, off) : new Vector3(off, 1.3f, 0f));
            ModeloAlvo("A02_Galho_" + n + (i == 0 ? "_visual" : "_visual_b"), PH + "dead_quiver_branch_02/dead_quiver_branch_02_1k.fbx", p, (alongZ ? 90f : 0f) + 20f * i, 2.1f, false, armad, false);
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Trecho D: bloqueio da Vigília

    private static void Bloqueio()
    {
        Placa(cenario, "A05_Placa_perigo_2", new Vector3(43.4f, 0f, 63.5f));
        Fosso(perigos, armad, 2, Fossos[1], new Vector3(40f, 0f, 62f));
        Pilao(perigos, cenario, armad, new Vector3(40f, 0f, 76f));
        Galho(3, new Vector3(40f, 0f, 84f), false);
        ModeloAlvo("C06_Barreira_Vigilia", PH + "concrete_road_barrier/concrete_road_barrier_1k.fbx", new Vector3(42.4f, 0f, 89.2f), 0f, 1.6f, false, constr);
        ModeloAlvo("T09_Holofote", PH + "portable_searchlight/portable_searchlight_1k.fbx", new Vector3(45.4f, 0f, 90.2f), 230f, 0.75f, false, tec);
        Luz(luzes, "Luz_Holofote_D", new Vector3(44.4f, 1.6f, 89.8f), new Color(0.85f, 0.95f, 1f), 2.2f, 12f);
        Modelo("T02_Sensor_camera", PH + "security_camera_01/security_camera_01_1k.fbx", new Vector3(35.6f, 2.6f, 90f), 120f, 1.22f, tec);
        Caixa(cenario, "Poste_camera_D", new Vector3(35.6f, 1.23f, 90f), new Vector3(0.12f, 2.45f, 0.12f), matTerminal);
        Modelo("T07_Postes_energia", PH + "modular_electricity_poles/modular_electricity_poles_1k.fbx", new Vector3(31f, 0f, 80f), 90f, 1f, tec);
    }

    // ---------------------------------------------------------------------------------------------
    // Trecho E: posto do viveiro, organizado em zonas

    private static void Posto()
    {
        var estacao = GameObject.Find("Estacao_Viveiro");
        if (estacao != null)
        {
            estacao.transform.position = new Vector3(-32f, 0f, 99.4f);   // caixa em (56; 100,3), junto à borda norte
            var terminal = estacao.transform.Find("Saida_Terminal");
            if (terminal != null) { terminal.SetParent(null); terminal.position = new Vector3(114.5f, 0f, 98f); terminal.rotation = Quaternion.Euler(0f, -90f, 0f); terminal.SetParent(estacao.transform, true); }
            foreach (Transform t in estacao.transform)
            {
                if (t.name == "Placa_Viveiro") t.name = "N1-05_Placa_viveiro";
                if (t.name == "Muda") t.gameObject.SetActive(false);   // cubos verdes antigos: as mudas reais vêm das tarefas
            }
            var est = estacao.GetComponentInChildren<InteractableStation>();
            if (est != null) est.nextObjective = "atravessar o posto de observação da Vigília";
        }

        // zona oeste: viveiro (canteiros, mudas, ferramentas)
        var caixote = Solido(ModeloAlvo("P02_Caixote_madeira_2", PH + "wooden_crate_02/wooden_crate_02_1k.fbx", new Vector3(42.8f, 0f, 102.4f), 12f, 0.85f, false, apoio));
        ModeloAlvo("E02_Bandeja_mudas", PH + "seeding_tray_01/seeding_tray_01_1k.fbx", new Vector3(42.8f, Limites(caixote).max.y, 102.4f), 12f, 0.55f, false, recup, false);
        Solido(ModeloAlvo("E04_Lixeira", PH + "metal_trash_can/metal_trash_can_1k.fbx", new Vector3(44.6f, 0f, 102.9f), 0f, 0.9f, true, recup));
        Solido(ModeloAlvo("E06_Saco_adubo", PH + "compost_bag_02/compost_bag_02_1k.fbx", new Vector3(45.8f, 0f, 102.7f), 20f, 0.7f, true, recup));
        ModeloAlvo("E09_Pa", PH + "rusted_spade_01/rusted_spade_01_1k.fbx", new Vector3(47.0f, -0.12f, 102.0f), 25f, 1.2f, true, recup);
        for (int i = 0; i < 3; i++)
            Solido(ModeloAlvo(i == 0 ? "E08_Jardineira" : "E08_Jardineira_" + (i + 1), PH + "planter_box_01/planter_box_01_1k.fbx", new Vector3(48.4f + i * 1.45f, 0f, 103.0f), 0f, 1.3f, false, recup));
        ModeloAlvo("E07_Vaso_barro", PH + "planter_pot_clay/planter_pot_clay_1k.fbx", new Vector3(52.2f, 0f, 101.2f), 0f, 0.4f, true, recup);
        ModeloAlvo("E03_Regador", PH + "watering_can_metal_01/watering_can_metal_01_1k.fbx", new Vector3(52.9f, 0f, 102.3f), 35f, 0.45f, false, recup);
        ModeloAlvo("E05_Mangueira", PH + "garden_hose_wall_mounted_01/garden_hose_wall_mounted_01_1k.fbx", new Vector3(53.1f, 0f, 103.3f), 0f, 0.55f, false, recup);
        ModeloAlvo("E10_Aspersor", PH + "garden_sprinkler_01/garden_sprinkler_01_1k.fbx", new Vector3(53.7f, 0f, 101.3f), 90f, 0.5f, false, recup);
        PosteLampiao("C08_Lampiao_posto_1", 44f, 100.8f, 2.4f);
        PosteLampiao("C08_Lampiao_posto_2", 53.5f, 99.8f, 2.4f);

        // zona leste: casa, oficina de rádio e energia
        ModeloAlvo("C02_Cabana_selva_3", TGD + "CabanaSelva/JungleHut01.DAE", new Vector3(68f, 0f, 107.6f), 180f, 5.6f, false, constr);
        ModeloAlvo("C05_Escada", PH + "wooden_ladder/wooden_ladder_1k.fbx", new Vector3(68.1f, 0f, 105.6f), 0f, 2.1f, true, constr);
        var pier = ModeloAlvo("C01_Pier_madeira", PH + "modular_wooden_pier/modular_wooden_pier_1k.fbx", new Vector3(75.2f, -1.7f, 107.8f), 0f, 8f, false, constr);
        var banco = Solido(ModeloAlvo("P05_Banco_2", PH + "painted_wooden_bench/painted_wooden_bench_1k.fbx", new Vector3(62.4f, 0f, 103.0f), 180f, 0.95f, true, apoio));
        float topo = Limites(banco).max.y;
        ModeloAlvo("T01_Radio", PH + "vintage_radio_transceiver/vintage_radio_transceiver_1k.fbx", new Vector3(62.1f, topo, 103.0f), 180f, 0.45f, false, tec);
        ModeloAlvo("T03_Lanterna_sinal", PH + "signal_flashlight/signal_flashlight_1k.fbx", new Vector3(62.9f, topo, 102.9f), 100f, 0.25f, false, tec);
        ModeloAlvo("T05_Multimetro", PH + "retro_multimeter/retro_multimeter_1k.fbx", new Vector3(62.6f, topo, 103.3f), 160f, 0.24f, true, tec);
        Solido(ModeloAlvo("P04_Barris_madeira", PH + "wooden_barrels_01/wooden_barrels_01_1k.fbx", new Vector3(64.9f, 0f, 103.1f), 0f, 0.95f, true, apoio));
        Solido(ModeloAlvo("P04_Barris_madeira_2", PH + "wooden_barrels_01/wooden_barrels_01_1k.fbx", new Vector3(65.9f, 0f, 103.4f), 140f, 0.95f, true, apoio));
        Solido(ModeloAlvo("T04_Gerador", PH + "portable_generator/portable_generator_1k.fbx", new Vector3(71.6f, 0f, 102.4f), 200f, 0.85f, false, tec));
        ModeloAlvo("T06_Cabos", PH + "modular_electric_cables/modular_electric_cables_1k.fbx", new Vector3(70.4f, 0f, 103.4f), 0f, 1.4f, false, tec, false);
        var caixa = Solido(ModeloAlvo("P01_Caixote_militar", PH + "wooden_military_crate/wooden_military_crate_1k.fbx", new Vector3(74.4f, 0f, 102.7f), 8f, 1.2f, false, apoio));
        ModeloAlvo("T08_Caixa_forca", PH + "power_box_01/power_box_01_1k.fbx", new Vector3(74.4f, Limites(caixa).max.y, 102.7f), 8f, 0.5f, true, tec, false);
        Solido(ModeloAlvo("T10_Caixa_eletrica", PH + "utility_box_01/utility_box_01_1k.fbx", new Vector3(77.0f, 0f, 103.0f), 180f, 1.35f, true, tec));
        Solido(ModeloAlvo("P03_Tambor", PH + "barrel_03/barrel_03_1k.fbx", new Vector3(78.5f, 0f, 101.9f), 0f, 0.9f, true, apoio));
        PosteLampiao("C08_Lampiao_posto_3", 61f, 100.8f, 2.4f);
        PosteLampiao("C08_Lampiao_posto_4", 72.5f, 100.4f, 2.4f);

        // zona sul: só luz, para a faixa central ficar livre
        ModeloAlvo("E01_Muda_vaso", PH + "potted_plant_02/potted_plant_02_1k.fbx", new Vector3(47.8f, 0f, 101.0f), 0f, 0.75f, true, recup);
        PosteLampiao("C08_Lampiao_posto_5", 50f, 92.6f, 2.4f);
        PosteLampiao("C08_Lampiao_posto_6", 63f, 92.6f, 2.4f);
        PosteLampiao("C08_Lampiao_posto_7", 75f, 92.6f, 2.4f);
        Sinalizador(cenario, new Vector3(58f, 0f, 94.5f));
    }

    // ---------------------------------------------------------------------------------------------
    // Arena: objetos nas bordas (a sentinela se move no miolo), para a clareira não ficar vazia

    private static void ArenaDecor()
    {
        Solido(ModeloAlvo("C09_Bloqueio_Vigilia_2", PH + "concrete_road_barrier_02/concrete_road_barrier_02_1k.fbx", new Vector3(90f, 0f, 85.2f), 0f, 1.7f, false, constr));
        Solido(ModeloAlvo("C09_Bloqueio_Vigilia_3", PH + "concrete_road_barrier_02/concrete_road_barrier_02_1k.fbx", new Vector3(97f, 0f, 85.2f), 8f, 1.7f, false, constr));
        Solido(ModeloAlvo("P03_Tambor_2", PH + "barrel_03/barrel_03_1k.fbx", new Vector3(104.2f, 0f, 85.8f), 0f, 0.9f, true, apoio));
        Solido(ModeloAlvo("P04_Barris_madeira_3", PH + "wooden_barrels_01/wooden_barrels_01_1k.fbx", new Vector3(105.2f, 0f, 87f), 40f, 0.95f, true, apoio));
        Solido(ModeloAlvo("P01_Caixote_militar_2", PH + "wooden_military_crate/wooden_military_crate_1k.fbx", new Vector3(84.6f, 0f, 110.6f), 15f, 1.2f, false, apoio));
        Solido(ModeloAlvo("P02_Caixote_madeira_3", PH + "wooden_crate_02/wooden_crate_02_1k.fbx", new Vector3(86f, 0f, 110.8f), 100f, 0.85f, false, apoio));
        Solido(ModeloAlvo("E04_Lixeira_2", PH + "metal_trash_can/metal_trash_can_1k.fbx", new Vector3(104f, 0f, 110.9f), 0f, 0.9f, true, recup));
        ModeloAlvo("R03_Pedras_musgo_3", PH + "rock_moss_set_02/rock_moss_set_02_1k.fbx", new Vector3(94f, 0f, 112.8f), 0f, 3.6f, false, rochas);
    }

    // ---------------------------------------------------------------------------------------------
    // Tarefas opcionais: mudas abandonadas e sinais derrubados (GDD: o guia recolocava sinais e protegia mudas)

    private static void Coletaveis()
    {
        var somOk = AssetDatabase.LoadAssetAtPath<AudioClip>(AUD + "estacao_ok.wav");
        var somSinal = AssetDatabase.LoadAssetAtPath<AudioClip>(AUD + "caixa.wav");

        // mudas: cada uma resgatada reaparece no canteiro do viveiro
        var mudas = new[] { new Vector2(-2.4f, 7.5f), new Vector2(2.7f, 27.5f), new Vector2(15.5f, 58.3f), new Vector2(41.8f, 72.5f), new Vector2(47f, 96f) };
        for (int i = 0; i < mudas.Length; i++)
        {
            string id = (i + 1).ToString();
            var vaso = ModeloAlvo("Muda_abandonada_" + id, PH + "potted_plant_02/potted_plant_02_1k.fbx", new Vector3(mudas[i].x, 0f, mudas[i].y), 40f * i, 0.8f, true, coletaveis, false);
            var terra = vaso.transform.Find("potted_plant_02_dirt"); if (terra != null) terra.gameObject.SetActive(false);
            var brilho = Luz(luzes, "Brilho_Muda_" + id, new Vector3(mudas[i].x, 1.0f, mudas[i].y), new Color(0.55f, 1f, 0.6f), 0.6f, 3.2f);
            var plantada = ModeloAlvo("Muda_resgatada_" + id, PH + "potted_plant_02/potted_plant_02_1k.fbx", new Vector3(48.9f + i * 0.75f, 0f, 101.7f), 30f * i, 0.8f, true, coletaveis, false);
            var terra2 = plantada.transform.Find("potted_plant_02_dirt"); if (terra2 != null) terra2.gameObject.SetActive(false);
            plantada.SetActive(false);
            var inter = new GameObject("Coletavel_Muda_" + id);
            inter.transform.SetParent(coletaveis); inter.transform.position = new Vector3(mudas[i].x, 0f, mudas[i].y); inter.tag = "Interactable";
            var c = inter.AddComponent<Coletavel>();
            c.tipo = "Mudas"; c.prompt = "E — resgatar muda"; c.range = 1.9f; c.som = somOk;
            c.mensagem = "Muda resgatada: vai para o canteiro do viveiro.";
            c.desativarAoColetar = new[] { vaso, brilho.gameObject };
            c.ativarAoColetar = new[] { plantada };
        }

        // sinais derrubados da trilha: a placa deitada no chão volta a ficar em pé
        var sinais = new[] { new Vector3(2.7f, 0f, 21.8f), new Vector3(24f, 0f, 53.6f), new Vector3(38.7f, 0f, 81.2f) };
        for (int i = 0; i < sinais.Length; i++)
        {
            string id = (i + 1).ToString();
            var p = sinais[i];
            var caido = new GameObject("Sinal_caido_" + id); caido.transform.SetParent(coletaveis);
            Caixa(caido.transform, "Poste", new Vector3(p.x, 0.07f, p.z), new Vector3(0.12f, 0.12f, 1.7f), matMadeira).transform.rotation = Quaternion.Euler(0f, 18f * i, 0f);
            Caixa(caido.transform, "Placa", new Vector3(p.x + 0.35f, 0.1f, p.z + 0.95f), new Vector3(0.75f, 0.05f, 0.5f), matMadeira).transform.rotation = Quaternion.Euler(0f, 18f * i, 0f);
            Caixa(caido.transform, "Seta", new Vector3(p.x + 0.35f, 0.14f, p.z + 0.95f), new Vector3(0.5f, 0.02f, 0.14f), matMarco).transform.rotation = Quaternion.Euler(0f, 18f * i, 0f);
            var empe = new GameObject("Sinal_em_pe_" + id); empe.transform.SetParent(coletaveis);
            Caixa(empe.transform, "Poste", new Vector3(p.x, 0.85f, p.z), new Vector3(0.12f, 1.7f, 0.12f), matMadeira);
            Caixa(empe.transform, "Placa", new Vector3(p.x, 1.45f, p.z), new Vector3(0.9f, 0.5f, 0.06f), matMadeira);
            Caixa(empe.transform, "Seta", new Vector3(p.x, 1.45f, p.z - 0.04f), new Vector3(0.6f, 0.14f, 0.03f), matMarco);
            empe.SetActive(false);
            var inter = new GameObject("Coletavel_Sinal_" + id);
            inter.transform.SetParent(coletaveis); inter.transform.position = new Vector3(p.x, 0f, p.z + 0.5f); inter.tag = "Interactable";
            var c = inter.AddComponent<Coletavel>();
            c.tipo = "Sinais"; c.prompt = "E — recolocar sinal"; c.range = 2f; c.som = somSinal;
            c.mensagem = "Sinal recolocado: o caminho volta a estar marcado.";
            c.desativarAoColetar = new[] { caido };
            c.ativarAoColetar = new[] { empe };
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Lampiões ao longo da rota e vaga-lumes

    private static void Lanternas()
    {
        // eixo da trilha (pontos x,z) e meia-largura de cada trecho; o posto tem lampiões próprios
        var eixo = new[] { new Vector2(0f, -8f), new Vector2(0f, 56f), new Vector2(40f, 56f), new Vector2(40f, 92f) };
        int n = 0; float lado = 1f; float falta = 8f;
        for (int s = 0; s < eixo.Length - 1; s++)
        {
            Vector2 a = eixo[s], b = eixo[s + 1];
            float comp = Vector2.Distance(a, b);
            Vector2 dir = (b - a) / comp, normal = new Vector2(-dir.y, dir.x);
            for (float t = falta; t < comp; t += 13f)
            {
                var p = a + dir * t + normal * lado * 4.75f;
                lado = -lado; falta = t + 13f - comp;
                if (Ocupado(p.x, p.y, 0.8f)) continue;
                n++;
                PosteLampiao("C08_Lampiao_rota_" + n, p.x, p.y, 2.2f);
            }
        }
    }

    private static void VagaLumes()
    {
        var pontos = new[] {
            new Vector3(-6.5f, 1.3f, 12f), new Vector3(6.5f, 1.3f, 25f), new Vector3(-6.5f, 1.3f, 39f), new Vector3(6.5f, 1.3f, 49f),
            new Vector3(10f, 1.3f, 62.5f), new Vector3(27f, 1.3f, 49.5f), new Vector3(38f, 1.3f, 62.5f), new Vector3(48f, 1.3f, 74f),
            new Vector3(33f, 1.3f, 86f), new Vector3(56f, 1.3f, 105.5f), new Vector3(86f, 1.3f, 115f), new Vector3(0f, 1.3f, -9f)
        };
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Particula.mat");
        for (int i = 0; i < pontos.Length; i++)
        {
            var go = new GameObject("VagaLumes_" + (i + 1));
            go.transform.SetParent(cenario); go.transform.position = pontos[i];
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true; main.prewarm = true; main.startLifetime = 7f; main.startSpeed = 0.1f; main.startSize = 0.09f; main.maxParticles = 36;
            main.startColor = new Color(0.85f, 1f, 0.45f, 1f); main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 5f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(6f, 2.2f, 6f);
            var ruido = ps.noise; ruido.enabled = true; ruido.strength = 0.7f; ruido.frequency = 0.35f; ruido.scrollSpeed = 0.2f;
            var cor = ps.colorOverLifetime; cor.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            cor.color = g;
            var rend = go.GetComponent<ParticleSystemRenderer>();
            if (mat != null) rend.sharedMaterial = mat;
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Setas de objetivo e painel de missões

    private static Material MaterialSeta()
    {
        const string tex = "Assets/UI/seta.png", path = "Assets/Materials/Seta.mat";
        var imp = AssetImporter.GetAtPath(tex) as TextureImporter;
        if (imp != null && (imp.filterMode != FilterMode.Point || imp.mipmapEnabled || !imp.alphaIsTransparency))
        {
            imp.textureType = TextureImporterType.Default; imp.filterMode = FilterMode.Point; imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true; imp.textureCompression = TextureImporterCompression.Uncompressed; imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(m, path); }
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tex));
        m.SetColor("_BaseColor", new Color(1.3f, 1.15f, 0.6f));   // um pouco acima de 1: o bloom faz a seta brilhar
        m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON");
        m.SetFloat("_Cull", 0f);
        m.renderQueue = (int)RenderQueue.AlphaTest;
        EditorUtility.SetDirty(m);
        return m;
    }

    private static void Marcadores()
    {
        var velho = GameObject.Find("Marcadores");
        if (velho != null) Object.DestroyImmediate(velho);
        var raiz = new GameObject("Marcadores").transform;
        var mat = MaterialSeta();
        var quad = PrimitiveMesh(PrimitiveType.Quad);
        System.Action<string, Interactable, float, float, float, bool> criar = (nome, alvo, altura, escala, dist, exige) =>
        {
            if (alvo == null) { Debug.LogWarning("Marcador sem alvo: " + nome); return; }
            var go = new GameObject("Marcador_" + nome);
            go.transform.SetParent(raiz);
            var m = go.AddComponent<MarcadorObjetivo>();
            m.alvo = alvo; m.altura = altura; m.distanciaMaxima = dist; m.exigeEtapasConcluidas = exige;
            var seta = new GameObject("Seta");
            seta.transform.SetParent(go.transform);
            seta.transform.position = alvo.transform.position + Vector3.up * altura;
            seta.transform.localScale = Vector3.one * escala;
            seta.AddComponent<MeshFilter>().sharedMesh = quad;
            var mr = seta.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
            m.visual = seta.transform;
        };
        // tarefas opcionais: só com o Nilo a até 28 m, para não poluir a tela
        foreach (var c in Object.FindObjectsByType<Coletavel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            criar(c.name.Replace("Coletavel_", ""), c, c.tipo == "Sinais" ? 1.3f : 1.6f, 0.55f, 28f, false);
        // missões principais: sempre visíveis enquanto estiverem disponíveis
        var estacao = GameObject.Find("Estacao_Viveiro");
        if (estacao != null) criar("Estacao", estacao.GetComponentInChildren<InteractableStation>(true), 1.9f, 0.85f, 0f, false);
        criar("Bateria", Object.FindAnyObjectByType<BateriaSentinela>(FindObjectsInactive.Include), 1.5f, 0.8f, 0f, false);
        criar("Terminal", Object.FindAnyObjectByType<LevelExit>(FindObjectsInactive.Include), 2.4f, 0.85f, 0f, true);
    }

    private static void PainelMissoes()
    {
        var canvas = GameObject.Find("Canvas");
        var gm = GameObject.Find("GameManager").GetComponent<GameManager>();
        var velho = canvas.transform.Find("Painel_Missoes");
        if (velho != null) Object.DestroyImmediate(velho.gameObject);
        var modelo = canvas.transform.Find("HUD_Estacao").GetComponent<Text>();

        var painel = new GameObject("Painel_Missoes", typeof(RectTransform));
        painel.transform.SetParent(canvas.transform, false);
        var rt = painel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-24f, -92f); rt.sizeDelta = new Vector2(450f, 280f);
        var fundo = painel.AddComponent<Image>();
        fundo.color = new Color(0.02f, 0.05f, 0.04f, 0.55f);
        fundo.raycastTarget = false;

        var txt = new GameObject("Texto", typeof(RectTransform));
        txt.transform.SetParent(painel.transform, false);
        var trt = txt.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = new Vector2(16f, 12f); trt.offsetMax = new Vector2(-14f, -12f);
        var t = txt.AddComponent<Text>();
        t.font = modelo.font; t.fontSize = 24; t.lineSpacing = 1.05f; t.supportRichText = true; t.raycastTarget = false;
        t.alignment = TextAnchor.UpperLeft; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.color = Color.white;
        var sombra = txt.AddComponent<Shadow>(); sombra.effectColor = new Color(0f, 0f, 0f, 0.8f); sombra.effectDistance = new Vector2(1.5f, -1.5f);
        t.text = "<b>MISSÕES</b>";
        gm.painelMissoes = painel;
        gm.missoesText = t;
    }

    // ---------------------------------------------------------------------------------------------
    // Floresta: árvores bem juntas perto da trilha, mais espaçadas ao longe, sub-bosque nas bordas

    private class Especie
    {
        public string path, nome; public float escala, rx, y, peso;
        public Especie(string path, string nome, float escala, float rx, float y, float peso) { this.path = path; this.nome = nome; this.escala = escala; this.rx = rx; this.y = y; this.peso = peso; }
    }

    private static Especie Sortear(List<Especie> lista)
    {
        float total = 0f; foreach (var e in lista) total += e.peso;
        float r = (float)rng.NextDouble() * total;
        foreach (var e in lista) { r -= e.peso; if (r <= 0f) return e; }
        return lista[lista.Count - 1];
    }

    private static void Vegetacao(Transform raizVeg)
    {
        // espécies de peso (dezenas de milhares de triângulos) entram uma vez, em pontos escolhidos
        Ocupar(Modelo("V06_Monguba", PH + "pachira_aquatica_01/pachira_aquatica_01_1k.fbx", new Vector3(50.5f, 0f, 109.5f), 0f, 1.7f, raizVeg));
        Ocupar(Modelo("V12_Arbusto_folha_larga", PH + "shrub_04/shrub_04_1k.fbx", new Vector3(-6.4f, 0f, 14f), 0f, 3.2f, raizVeg));
        Ocupar(Modelo("V14_Anturio", PH + "anthurium_botany_01/anthurium_botany_01_1k.fbx", new Vector3(5.8f, 0f, 33f), 0f, 1.22f, raizVeg));
        Ocupar(Modelo("V15_Calathea", PH + "calathea_orbifolia_01/calathea_orbifolia_01_1k.fbx", new Vector3(-5.9f, 0f, 49f), 0f, 1.41f, raizVeg));

        var arvores = new List<Especie> {
            new Especie(TGD + "Sumauma/CiabaTree_01.dae", "V01_Sumauma", 2.1f, 0f, 0f, 22f),
            new Especie("Assets/Terceiros/OpenGameArt/Yughues_Palmeiras_v3/palm_straight.FBX", "V02_Palmeira_reta", 2.5f, 270f, 0f, 13f),
            new Especie("Assets/Terceiros/OpenGameArt/Yughues_Palmeiras_v3/palm_dual.FBX", "V02_Palmeira_dupla", 2.6f, 270f, 0f, 10f),
            new Especie("Assets/Terceiros/OpenGameArt/Yughues_Palmeiras_v3/palm_trio.FBX", "V02_Palmeira_trio", 2.7f, 270f, 0f, 8f),
            new Especie("Assets/Terceiros/OpenGameArt/Yughues_Palmeira_v2/palm_tree.FBX", "V03_Palmeira_v2", 1.3f, 270f, 0f, 14f),
            new Especie(TGD + "Embauba/CecropiaTree01.DAE", "V04_Embauba", 2.0f, 0f, 0f, 16f),
            new Especie(TGD + "Bananeira/Banana01.DAE", "V05_Bananeira", 0.75f, 0f, 0f, 9f),
            new Especie(TGD + "Bambu/Bamboo01.DAE", "V07_Bambuzal", 1.0f, 0f, 0f, 8f),
        };
        var sub = new List<Especie> {
            new Especie(PH + "fern_02/fern_02_1k.fbx", "V11_Samambaia", 1.0f, 0f, 0f, 30f),
            new Especie("Assets/Terceiros/OpenGameArt/Yughues_ArbustosTropicais/01/trop_shrub_01.FBX", "V13_Arbusto_tropical_1", 0.62f, 0f, 0.7f, 18f),
            new Especie("Assets/Terceiros/OpenGameArt/Yughues_ArbustosTropicais/03/trop_shrub_03.FBX", "V13_Arbusto_tropical_3", 1.06f, 0f, 0.9f, 18f),
            new Especie("Assets/Terceiros/OpenGameArt/Yughues_ArbustosTropicais/05/trop_shrub_05.FBX", "V13_Arbusto_tropical_5", 0.72f, 0f, 0.7f, 18f),
            new Especie(PH + "shrub_03/shrub_03_1k.fbx", "V16_Capim_alto", 2.0f, 0f, 0f, 8f),
            new Especie(PH + "weed_plant_02/weed_plant_02_1k.fbx", "V17_Mato_rasteiro", 2.5f, 0f, 0f, 8f),
        };

        var colocadas = new List<Vector2>();
        int nArv = 0, tent = 0;
        while (nArv < 440 && tent++ < 90000)
        {
            float x = Mathf.Lerp(-36f, 148f, (float)rng.NextDouble());
            float z = Mathf.Lerp(-32f, 144f, (float)rng.NextDouble());
            float d = DistanciaTrilha(x, z);
            if (d < 2.9f || d > 27f) continue;
            float minD = d < 9f ? 3.4f : (d < 16f ? 5f : 9.5f);   // junto da trilha mais denso, ao longe esparso
            if (Ocupado(x, z, 1.2f)) continue;
            bool livre = true;
            foreach (var p in colocadas) { float dx = p.x - x, dz = p.y - z; if (dx * dx + dz * dz < minD * minD) { livre = false; break; } }
            if (!livre) continue;
            var e = Sortear(arvores);
            float esc = e.escala * (0.85f + (float)rng.NextDouble() * 0.4f);
            var go = Modelo(e.nome + "_" + (++nArv), e.path, new Vector3(x, e.y, z), (float)rng.NextDouble() * 360f, esc, raizVeg);
            if (e.rx != 0f) go.transform.rotation = Quaternion.Euler(e.rx, (float)rng.NextDouble() * 360f, 0f);
            if (d > 9f) SemSombra(go);   // árvores afastadas não projetam sombra: economiza metade dos triângulos
            colocadas.Add(new Vector2(x, z));
        }

        var baixas = new List<Vector2>();
        int nSub = 0; tent = 0;
        while (nSub < 330 && tent++ < 90000)
        {
            float x = Mathf.Lerp(-30f, 142f, (float)rng.NextDouble());
            float z = Mathf.Lerp(-26f, 138f, (float)rng.NextDouble());
            float d = DistanciaTrilha(x, z);
            if (d < 1.7f || d > 7f) continue;
            if (Ocupado(x, z, 0.4f)) continue;
            bool livre = true;
            foreach (var p in baixas) { float dx = p.x - x, dz = p.y - z; if (dx * dx + dz * dz < 1.8f * 1.8f) { livre = false; break; } }
            if (!livre) continue;
            var e = Sortear(sub);
            float esc = e.escala * (0.8f + (float)rng.NextDouble() * 0.5f);
            SemSombra(Modelo(e.nome + "_" + (++nSub), e.path, new Vector3(x, e.y, z), (float)rng.NextDouble() * 360f, esc, raizVeg));
            baixas.Add(new Vector2(x, z));
        }
        Debug.Log("Floresta: " + nArv + " árvores e " + nSub + " plantas de sub-bosque.");
    }

    // ---------------------------------------------------------------------------------------------
    // Chão da mata e pós-processamento

    private static Material MaterialMata()
    {
        string pasta = PH + "_Texturas/leafy_grass/";
        var imp = AssetImporter.GetAtPath(pasta + "leafy_grass_nor_gl_1k.jpg") as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; imp.SaveAndReimport(); }
        var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Chao_Mata.mat");
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, "Assets/Materials/Chao_Mata.mat"); }
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(pasta + "leafy_grass_diff_1k.jpg"));
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(pasta + "leafy_grass_nor_gl_1k.jpg");
        if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
        m.SetColor("_BaseColor", new Color(0.78f, 0.9f, 0.72f));
        m.SetFloat("_Smoothness", 0.05f);
        m.mainTextureScale = Vector2.one; m.mainTextureOffset = Vector2.zero;
        EditorUtility.SetDirty(m);
        return m;
    }

    private static void PosProcessamento()
    {
        var existente = GameObject.Find("Volume_Global");
        if (existente != null) Object.DestroyImmediate(existente);
        const string path = "Assets/Settings/PerfilPosProcessamento.asset";
        AssetDatabase.DeleteAsset(path);
        var perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(perfil, path);

        var bloom = perfil.Add<Bloom>(true);
        bloom.threshold.Override(0.9f); bloom.intensity.Override(0.7f); bloom.scatter.Override(0.65f);
        var vinheta = perfil.Add<Vignette>(true);
        vinheta.intensity.Override(0.3f); vinheta.smoothness.Override(0.45f);
        var cores = perfil.Add<ColorAdjustments>(true);
        cores.postExposure.Override(0.15f); cores.contrast.Override(12f); cores.saturation.Override(10f); cores.colorFilter.Override(new Color(0.93f, 1f, 0.96f));
        var tom = perfil.Add<Tonemapping>(true);
        tom.mode.Override(TonemappingMode.ACES);
        foreach (var comp in new VolumeComponent[] { bloom, vinheta, cores, tom }) AssetDatabase.AddObjectToAsset(comp, perfil);
        EditorUtility.SetDirty(perfil);
        AssetDatabase.SaveAssets();

        var go = new GameObject("Volume_Global");
        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true; vol.sharedProfile = perfil; vol.priority = 1;
        var cam = Camera.main;
        if (cam != null) cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
    }

    // ---------------------------------------------------------------------------------------------
    // Gatilhos de aviso e falas

    private static void Tutoriais()
    {
        Gatilho(tutorial, "Fala_Abertura", new Vector3(0f, 1.5f, -2f), new Vector3(8f, 3f, 2f), "MARA (rádio): A travessia fecha ao amanhecer. Leve o módulo até a antena. Eu acompanho enquanto houver sinal.\nNILO: Conheço o começo da trilha. Vou avisando.", 7f);
        Gatilho(tutorial, "Tutorial_Andar", new Vector3(0f, 1.5f, 3f), new Vector3(8f, 3f, 2f), "W A S D para andar; mouse ou ← → giram a câmera.\nShift para correr. Pare junto ao marco amarelo.", 6f);
        Gatilho(tutorial, "Dica_Tarefas", new Vector3(0f, 1.5f, 6f), new Vector3(8f, 3f, 1.5f), "Pelo caminho há mudas abandonadas e sinais derrubados.\nChegue perto e use E para resgatar ou recolocar (opcional).", 7f);
        Gatilho(tutorial, "Tutorial_Salto", new Vector3(0f, 1.5f, 14f), new Vector3(8f, 3f, 2f), "Espaço para saltar a raiz.", 4f);
        Gatilho(tutorial, "Tutorial_Troncos", new Vector3(0f, 1.5f, 26f), new Vector3(8f, 3f, 2f), "Dois troncos fecham metade do caminho: contorne ou salte.", 4f);
        Gatilho(tutorial, "Fala_Marca", new Vector3(0f, 1.5f, 32.5f), new Vector3(8f, 3f, 1.5f), "NILO: Essa marca fui eu que fiz. As estacas vieram depois.", 5f);
        Gatilho(tutorial, "Tutorial_Fosso", new Vector3(0f, 1.5f, 37f), new Vector3(8f, 3f, 2f), "Fosso com estacas à frente: corra e salte.\nCair custa um coração.", 5f);
        Gatilho(tutorial, "Tutorial_Deslize", new Vector3(7f, 1.5f, 56f), new Vector3(2f, 3f, 8f), "Barreira alta com vão por baixo: Ctrl para deslizar.\nPular não adianta; o deslize começa no chão.", 6f);
        Gatilho(tutorial, "Tutorial_Tronco", new Vector3(25f, 1.5f, 56f), new Vector3(2f, 3f, 8f), "Tronco giratório: espere ele passar e atravesse.\nEncostar custa um coração.", 5f);
        Gatilho(tutorial, "Tutorial_Pilao", new Vector3(40f, 1.5f, 71f), new Vector3(8f, 3f, 2f), "Pilão: observe o ciclo e passe quando ele subir.", 5f);
        Gatilho(tutorial, "Chegada_Posto", new Vector3(40f, 1.5f, 93.5f), new Vector3(8f, 3f, 2f), "Área segura: posto do viveiro. Use E na caixa de ferramentas.", 5f);
        var entradaArena = Gatilho(tutorial, "Entrada_Arena", new Vector3(82f, 1.5f, 98f), new Vector3(2f, 3f, 12f), "MARA (rádio): Sinal estranho à frente... é uma sentinela automática da Vigília. Ela avança em linha reta e depois gira o braço: desvie, salte o braço e, quando ela bater e travar, retire as baterias com E.", 9f);
        var sentinela = GameObject.Find("Chefe/Sentinela").GetComponent<SentinelaChefe>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(entradaArena.GetComponent<TutorialTrigger>().onEnter, sentinela.Acordar);
    }
}
