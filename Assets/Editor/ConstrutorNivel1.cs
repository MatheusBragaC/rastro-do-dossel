using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Monta o nível 1 em terceira pessoa a partir de dados (trilha, perigos, posto, arena da sentinela
// e vegetação). Menu: Rastro do Dossel > Montar Nível 1 (3D). Refaz o cenário; mantém câmera, luz,
// Nilo, Canvas, GameManager, EventSystem, música e a estação do viveiro (só a reposiciona).
public static class ConstrutorNivel1
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

    [MenuItem("Rastro do Dossel/Montar Nível 1 (3D)")]
    public static void Montar()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        rng = new System.Random(7);
        CarregarMateriais();

        foreach (var nome in new[] { "Terreno", "Perigos", "Tutorial", "Cenario", "Cenario_Assets", "Chefe", "Luzes" })
        {
            var velho = GameObject.Find(nome);
            if (velho != null) Object.DestroyImmediate(velho);
        }

        var terreno = Raiz("Terreno");
        var perigos = Raiz("Perigos");
        var tutorial = Raiz("Tutorial");
        var cenario = Raiz("Cenario");
        var assets = Raiz("Cenario_Assets");
        var chefe = Raiz("Chefe");
        var luzes = Raiz("Luzes");
        var veg = Filho(assets, "Vegetacao").transform;
        var rochas = Filho(assets, "Rochas").transform;
        var constr = Filho(assets, "Construcoes").transform;
        var tec = Filho(assets, "Tecnologia").transform;
        var recup = Filho(assets, "Recuperacao").transform;
        var apoio = Filho(assets, "Apoio").transform;
        var armad = Filho(assets, "Armadilhas").transform;
        var locais = Filho(assets, "Locais_N1").transform;

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
        var portaoVigilia = Caixa(terreno, "Portao_Vigilia", new Vector3(108f, 1.5f, 98f), new Vector3(0.5f, 3f, 8f), matTerminal, "Ground", "Ground");
        Modelo("C06_Barreira_Vigilia_portao_1", "Assets/Terceiros/PolyHaven/concrete_road_barrier/concrete_road_barrier_1k.fbx", new Vector3(107.6f, 0f, 96f), 90f, 1.04f, portaoVigilia.transform);
        Modelo("C06_Barreira_Vigilia_portao_2", "Assets/Terceiros/PolyHaven/concrete_road_barrier/concrete_road_barrier_1k.fbx", new Vector3(107.6f, 0f, 100f), 90f, 1.04f, portaoVigilia.transform);
        Caixa(portaoVigilia.transform, "Portao_Placa", new Vector3(107.7f, 2.2f, 98f), new Vector3(0.05f, 0.6f, 1.6f), matPerigo, "Default", "Untagged");

        // ---------- trecho A: Várzea Alta ----------
        Caixa(cenario, "Portao_VarzeaAlta_Poste", new Vector3(-1.4f, 1.3f, -5f), new Vector3(0.25f, 2.6f, 0.25f), matMadeira);
        Caixa(cenario, "Portao_VarzeaAlta_Poste", new Vector3(1.4f, 1.3f, -5f), new Vector3(0.25f, 2.6f, 0.25f), matMadeira);
        Caixa(cenario, "Portao_VarzeaAlta_Viga", new Vector3(0f, 2.6f, -5f), new Vector3(3.1f, 0.25f, 0.3f), matMadeira);
        Modelo("C07_Fogueira", "Assets/Terceiros/PolyHaven/stone_fire_pit/stone_fire_pit_1k.fbx", new Vector3(-2.6f, 0.6f, -6.5f), 0f, 82.84f, constr);
        Luz(luzes, "Luz_Fogueira", new Vector3(-2.6f, 1.2f, -6.5f), new Color(1f, 0.55f, 0.2f), 2.6f, 11f);
        Modelo("P02_Caixote_madeira", "Assets/Terceiros/PolyHaven/wooden_crate_02/wooden_crate_02_1k.fbx", new Vector3(2.8f, 0f, -6.8f), 20f, 0.86f, apoio);
        Modelo("C08_Lampiao", "Assets/Terceiros/PolyHaven/wooden_lantern_01/wooden_lantern_01_1k.fbx", new Vector3(1.4f, 2.1f, -5f), 0f, 0.85f, constr);
        Luz(luzes, "Luz_Portao", new Vector3(1.4f, 2.0f, -4.6f), new Color(1f, 0.75f, 0.4f), 1.6f, 8f);
        Modelo("C03_Atracadouro", "Assets/Terceiros/OpenGameArt/3TD_Tropical/Atracadouro/Tropical_Jetty01.DAE", new Vector3(-7.5f, 1.9f, -13f), 0f, 0.9f, constr);
        Modelo("R06_Paredao_inicio", "Assets/Terceiros/PolyHaven/rock_face_01/rock_face_01_1k.fbx", new Vector3(0f, 1f, -12f), 0f, 106.68f, rochas);
        Caixa(cenario, "Placa_Controles", new Vector3(3.3f, 1.2f, 1f), new Vector3(0.05f, 0.7f, 1.2f), matMadeira);
        Caixa(cenario, "Placa_Controles_Poste", new Vector3(3.3f, 0.5f, 1f), new Vector3(0.08f, 0.9f, 0.08f), matMadeira);
        Caixa(cenario, "Marco_Treinamento", new Vector3(0f, 0.8f, 10f), new Vector3(0.15f, 1.5f, 0.15f), matMarco);
        var raiz = Caixa(terreno, "Obstaculo_Raiz", new Vector3(0f, 0.25f, 18f), new Vector3(8f, 0.5f, 0.6f), matTronco, "Ground", "Ground");
        Modelo("V10_Raizes", "Assets/Terceiros/PolyHaven/root_cluster_01/root_cluster_01_1k.fbx", new Vector3(0f, 1.2f, 18f), 90f, 72.77f, veg);
        Modelo("V18_Raiz", "Assets/Terceiros/PolyHaven/single_root/single_root_1k.fbx", new Vector3(-2.5f, 0.1f, 18.2f), 0f, 0.98f, veg);
        Modelo("V19_Musgo", "Assets/Terceiros/PolyHaven/moss_01/moss_01_1k.fbx", new Vector3(2.8f, 1.0f, 18f), 0f, 1f, veg);
        Modelo("R01_Pedregulho", "Assets/Terceiros/PolyHaven/boulder_01/boulder_01_1k.fbx", new Vector3(-6.5f, 0.1f, 6f), 0f, 1.59f, rochas);
        Modelo("R09_Cascalho", "Assets/Terceiros/PolyHaven/stone_01/stone_01_1k.fbx", new Vector3(3.2f, 0.1f, 24f), 0f, 4.87f, rochas);
        Modelo("V08_Tronco_caido", "Assets/Terceiros/PolyHaven/dead_tree_trunk_02/dead_tree_trunk_02_1k.fbx", new Vector3(-7f, 0.3f, 26f), 30f, 0.99f, veg);
        Modelo("V09_Toco", "Assets/Terceiros/PolyHaven/tree_stump_01/tree_stump_01_1k.fbx", new Vector3(3.4f, 0.3f, 13f), 0f, 37.73f, veg);
        Modelo("V20_Galhos_secos", "Assets/Terceiros/PolyHaven/dry_branches_medium_01/dry_branches_medium_01_1k.fbx", new Vector3(-3.3f, 0f, 28f), 0f, 1.13f, veg);
        for (int i = 0; i < 6; i++) Modelo(i == 0 ? "C04_Cerca_estacas_1" : "C04_Cerca_estacas_" + (i + 1), "Assets/Terceiros/OpenGameArt/WoodKit1/Stick_1.fbx", new Vector3(-4.3f, 0.6f, -2f + i * 0.6f), i * 23f, 0.56f, constr);

        // ---------- trecho B: marca antiga e fosso 1 ----------
        var arvoreMarcada = Modelo("N1-03_Arvore_marcada", "Assets/Terceiros/OpenGameArt/3TD_Tropical/Sumauma/CiabaTree_01.dae", new Vector3(-4.6f, 0f, 35f), 0f, 1.46f, locais);
        Caixa(cenario, "Marca_Antiga_Nilo", new Vector3(-3.6f, 1.7f, 35f), new Vector3(0.05f, 0.3f, 0.3f), matLenco).transform.rotation = Quaternion.Euler(45f, 0f, 0f);
        Modelo("C08_Lampiao_marca", "Assets/Terceiros/PolyHaven/wooden_lantern_01/wooden_lantern_01_1k.fbx", new Vector3(-3.6f, 2.4f, 35.8f), 0f, 0.85f, constr);
        Luz(luzes, "Luz_Marca", new Vector3(-3.3f, 2.3f, 35.8f), new Color(1f, 0.75f, 0.4f), 1.5f, 8f);
        Placa(cenario, "Fosso_1_Placa", new Vector3(3.4f, 0f, 38f));
        Fosso(perigos, armad, 1, Fossos[0], new Vector3(0f, 0f, 36.5f));
        Modelo("C10_Alambrado_Vigilia", "Assets/Terceiros/PolyHaven/modular_chainlink_fence/modular_chainlink_fence_1k.fbx", new Vector3(5.5f, 0f, 44f), 0f, 0.74f, constr);
        Modelo("C09_Bloqueio_Vigilia", "Assets/Terceiros/PolyHaven/concrete_road_barrier_02/concrete_road_barrier_02_1k.fbx", new Vector3(-3.2f, 0f, 47f), 15f, 1.02f, constr);
        Modelo("R02_Pedras_musgo_1", "Assets/Terceiros/PolyHaven/rock_moss_set_01/rock_moss_set_01_1k.fbx", new Vector3(5.5f, 0.3f, 30f), 0f, 0.5f, rochas);
        Modelo("R04_Rocha_erodida", "Assets/Terceiros/PolyHaven/rock_07/rock_07_1k.fbx", new Vector3(-6.5f, 0f, 55f), 0f, 7.65f, rochas);
        Modelo("R08_Encosta", "Assets/Terceiros/PolyHaven/mountainside/mountainside_1k.fbx", new Vector3(-34f, 0.1f, 30f), 270f, 1.76f, rochas).transform.localScale = new Vector3(1.76f, 0.52f, 1.33f);

        // ---------- trecho C: galhos baixos e tronco giratório ----------
        Galho(perigos, cenario, armad, 1, new Vector3(12f, 0f, 56f), true);
        Galho(perigos, cenario, armad, 2, new Vector3(19f, 0f, 56f), true);
        Modelo("C08_Lampiao_galho", "Assets/Terceiros/PolyHaven/wooden_lantern_01/wooden_lantern_01_1k.fbx", new Vector3(15.5f, 2.3f, 60.3f), 0f, 0.85f, constr);
        Luz(luzes, "Luz_Galhos", new Vector3(15.5f, 2.2f, 59.8f), new Color(1f, 0.75f, 0.4f), 1.5f, 9f);
        TroncoGiratorio(perigos, armad, new Vector3(32f, 0f, 56f));
        Luz(luzes, "Luz_Tronco", new Vector3(32f, 4.2f, 56f), new Color(1f, 0.6f, 0.3f), 1.8f, 10f);
        Modelo("R03_Pedras_musgo_2", "Assets/Terceiros/PolyHaven/rock_moss_set_02/rock_moss_set_02_1k.fbx", new Vector3(24f, 0.2f, 61.5f), 0f, 0.48f, rochas);
        Modelo("R10_Rochas_tropicais", "Assets/Terceiros/OpenGameArt/3TD_Tropical/Rochas/GroupRocks01.DAE", new Vector3(40f, 0.3f, 49f), 0f, 17.44f, rochas);
        Modelo("R05_Rocha_09", "Assets/Terceiros/PolyHaven/rock_09/rock_09_1k.fbx", new Vector3(47f, 0f, 56f), 0f, 27.32f, rochas);

        // ---------- trecho D: fosso 2, pilão e bloqueio ----------
        Placa(cenario, "Fosso_2_Placa", new Vector3(43.4f, 0f, 63.5f));
        Fosso(perigos, armad, 2, Fossos[1], new Vector3(40f, 0f, 62f));
        Pilao(perigos, cenario, armad, new Vector3(40f, 0f, 76f));
        Galho(perigos, cenario, armad, 3, new Vector3(40f, 0f, 84f), false);
        Modelo("C06_Barreira_Vigilia", "Assets/Terceiros/PolyHaven/concrete_road_barrier/concrete_road_barrier_1k.fbx", new Vector3(42.5f, 0f, 89f), 0f, 1.04f, constr);
        Modelo("T09_Holofote", "Assets/Terceiros/PolyHaven/portable_searchlight/portable_searchlight_1k.fbx", new Vector3(45.2f, 0.2f, 90f), 230f, 138.13f, tec);
        Luz(luzes, "Luz_Holofote_D", new Vector3(44.2f, 1.6f, 89.6f), new Color(0.85f, 0.95f, 1f), 2.2f, 12f);
        Modelo("T02_Sensor_camera", "Assets/Terceiros/PolyHaven/security_camera_01/security_camera_01_1k.fbx", new Vector3(35.6f, 2.6f, 90f), 120f, 1.22f, tec);
        Modelo("T07_Postes_energia", "Assets/Terceiros/PolyHaven/modular_electricity_poles/modular_electricity_poles_1k.fbx", new Vector3(30f, 0f, 80f), 90f, 1f, tec);

        // ---------- trecho E: posto do viveiro ----------
        var estacao = GameObject.Find("Estacao_Viveiro");
        if (estacao != null)
        {
            // a estação foi montada ao longo de X em (82..99, 0, 0..3); leva para o posto
            estacao.transform.position = new Vector3(-34f, 0f, 97.2f);
            var terminal = estacao.transform.Find("Saida_Terminal");
            if (terminal != null) { terminal.SetParent(null); terminal.position = new Vector3(114.5f, 0f, 98f); terminal.rotation = Quaternion.Euler(0f, -90f, 0f); terminal.SetParent(estacao.transform, true); }
            var est = estacao.GetComponentInChildren<InteractableStation>();
            if (est != null) est.nextObjective = "atravessar o posto de observação da Vigília";
        }
        Modelo("C02_Cabana_selva", "Assets/Terceiros/OpenGameArt/3TD_Tropical/CabanaSelva/JungleHut01.DAE", new Vector3(60f, 1.7f, 106f), 180f, 1.03f, constr);
        Modelo("C01_Pier_madeira", "Assets/Terceiros/PolyHaven/modular_wooden_pier/modular_wooden_pier_1k.fbx", new Vector3(70f, 0.5f, 106f), 90f, 0.47f, constr);
        Modelo("C05_Escada", "Assets/Terceiros/PolyHaven/wooden_ladder/wooden_ladder_1k.fbx", new Vector3(63.5f, 0f, 104.6f), 0f, 1.5f, constr);
        Modelo("P01_Caixote_militar", "Assets/Terceiros/PolyHaven/wooden_military_crate/wooden_military_crate_1k.fbx", new Vector3(66f, 0f, 102.5f), 15f, 0.89f, apoio);
        Modelo("P03_Tambor", "Assets/Terceiros/PolyHaven/barrel_03/barrel_03_1k.fbx", new Vector3(48f, 0.9f, 102.8f), 0f, 96.72f, apoio).transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        Modelo("P04_Barris_madeira", "Assets/Terceiros/PolyHaven/wooden_barrels_01/wooden_barrels_01_1k.fbx", new Vector3(72f, 0f, 102f), 0f, 0.68f, apoio);
        Modelo("P05_Banco", "Assets/Terceiros/PolyHaven/painted_wooden_bench/painted_wooden_bench_1k.fbx", new Vector3(57f, 0.3f, 102.5f), 0f, 103.01f, apoio);
        Modelo("T01_Radio", "Assets/Terceiros/PolyHaven/vintage_radio_transceiver/vintage_radio_transceiver_1k.fbx", new Vector3(57f, 0.8f, 102.5f), 0f, 0.73f, tec);
        Modelo("T03_Lanterna_sinal", "Assets/Terceiros/PolyHaven/signal_flashlight/signal_flashlight_1k.fbx", new Vector3(57.6f, 0.8f, 102.6f), 0f, 1.33f, tec);
        Modelo("T04_Gerador", "Assets/Terceiros/PolyHaven/portable_generator/portable_generator_1k.fbx", new Vector3(64f, 0f, 93f), 0f, 0.98f, tec);
        Modelo("T05_Multimetro", "Assets/Terceiros/PolyHaven/retro_multimeter/retro_multimeter_1k.fbx", new Vector3(62.5f, 0.5f, 93f), 0f, 0.79f, tec);
        Modelo("T06_Cabos", "Assets/Terceiros/PolyHaven/modular_electric_cables/modular_electric_cables_1k.fbx", new Vector3(65f, 0f, 93.2f), 0f, 0.77f, tec);
        Modelo("T08_Caixa_forca", "Assets/Terceiros/PolyHaven/power_box_01/power_box_01_1k.fbx", new Vector3(61f, 0.3f, 93f), 0f, 0.99f, tec);
        Modelo("T10_Caixa_eletrica", "Assets/Terceiros/PolyHaven/utility_box_01/utility_box_01_1k.fbx", new Vector3(66f, 0.6f, 93f), 0f, 254.63f, tec);
        Modelo("E01_Muda_vaso", "Assets/Terceiros/PolyHaven/potted_plant_02/potted_plant_02_1k.fbx", new Vector3(50f, 0f, 103f), 0f, 0.83f, recup);
        Modelo("E02_Bandeja_mudas", "Assets/Terceiros/PolyHaven/seeding_tray_01/seeding_tray_01_1k.fbx", new Vector3(51.2f, 0.1f, 103f), 0f, 90.82f, recup);
        Modelo("E03_Regador", "Assets/Terceiros/PolyHaven/watering_can_metal_01/watering_can_metal_01_1k.fbx", new Vector3(52f, 0.5f, 103f), 0f, 183.23f, recup);
        Modelo("E04_Lixeira", "Assets/Terceiros/PolyHaven/metal_trash_can/metal_trash_can_1k.fbx", new Vector3(45.5f, 0f, 102.5f), 0f, 0.99f, recup);
        Modelo("E05_Mangueira", "Assets/Terceiros/PolyHaven/garden_hose_wall_mounted_01/garden_hose_wall_mounted_01_1k.fbx", new Vector3(53f, 0.3f, 103.2f), 0f, 0.9f, recup);
        Modelo("E06_Saco_adubo", "Assets/Terceiros/PolyHaven/compost_bag_02/compost_bag_02_1k.fbx", new Vector3(46.8f, 0.2f, 102.8f), 20f, 76.32f, recup);
        Modelo("E07_Vaso_barro", "Assets/Terceiros/PolyHaven/planter_pot_clay/planter_pot_clay_1k.fbx", new Vector3(54f, 0.1f, 103f), 0f, 94.89f, recup);
        Modelo("E08_Jardineira", "Assets/Terceiros/PolyHaven/planter_box_01/planter_box_01_1k.fbx", new Vector3(55f, 0.2f, 103.2f), 0f, 98.62f, recup);
        Modelo("E09_Pa", "Assets/Terceiros/PolyHaven/rusted_spade_01/rusted_spade_01_1k.fbx", new Vector3(49f, 0.5f, 103.2f), 0f, 95.8f, recup).transform.rotation = Quaternion.Euler(280f, 0f, 8f);
        Modelo("E10_Aspersor", "Assets/Terceiros/PolyHaven/garden_sprinkler_01/garden_sprinkler_01_1k.fbx", new Vector3(56f, 0.2f, 103.5f), 0f, 111.24f, recup);
        for (int i = 0; i < 2; i++) Modelo("E01_Muda_viveiro_" + (i + 1), "Assets/Terceiros/PolyHaven/potted_plant_02/potted_plant_02_1k.fbx", new Vector3(49.2f + i * 0.5f, 0f, 101.3f), 47f * i, 1.13f, recup);
        Sinalizador(cenario, new Vector3(58f, 0f, 94f));
        Luz(luzes, "Luz_Posto_1", new Vector3(52f, 2.4f, 99f), new Color(1f, 0.8f, 0.5f), 2.2f, 12f);
        Luz(luzes, "Luz_Posto_2", new Vector3(66f, 2.4f, 99f), new Color(1f, 0.8f, 0.5f), 2.2f, 12f);
        Modelo("C08_Lampiao_posto", "Assets/Terceiros/PolyHaven/wooden_lantern_01/wooden_lantern_01_1k.fbx", new Vector3(66f, 2.4f, 99.3f), 0f, 0.85f, constr);
        Caixa(cenario, "Lampiao_posto_poste", new Vector3(66f, 1.2f, 99.3f), new Vector3(0.12f, 2.4f, 0.12f), matMadeira);

        // ---------- trecho F: arena da sentinela ----------
        Sentinela(chefe, luzes, tec, constr, veg, portaoVigilia);

        // ---------- trecho G: saída ----------
        Modelo("R07_Paredao_fim", "Assets/Terceiros/PolyHaven/rock_face_02/rock_face_02_1k.fbx", new Vector3(120f, 0.4f, 98f), 270f, 172.77f, rochas);
        Luz(luzes, "Luz_Saida", new Vector3(113f, 2.5f, 98f), new Color(0.6f, 1f, 0.8f), 2f, 9f);

        // ---------- tutoriais e falas ----------
        Gatilho(tutorial, "Fala_Abertura", new Vector3(0f, 1.5f, -2f), new Vector3(8f, 3f, 2f), "MARA (rádio): A travessia fecha ao amanhecer. Leve o módulo até a antena. Eu acompanho enquanto houver sinal.\nNILO: Conheço o começo da trilha. Vou avisando.", 7f);
        Gatilho(tutorial, "Tutorial_Andar", new Vector3(0f, 1.5f, 3f), new Vector3(8f, 3f, 2f), "W A S D para andar; mouse ou ← → giram a câmera.\nShift para correr. Pare junto ao marco amarelo.", 6f);
        Gatilho(tutorial, "Tutorial_Salto", new Vector3(0f, 1.5f, 14f), new Vector3(8f, 3f, 2f), "Espaço para saltar a raiz.", 4f);
        Gatilho(tutorial, "Fala_Marca", new Vector3(0f, 1.5f, 32f), new Vector3(8f, 3f, 2f), "NILO: Essa marca fui eu que fiz. As estacas vieram depois.", 5f);
        Gatilho(tutorial, "Tutorial_Fosso", new Vector3(0f, 1.5f, 36.5f), new Vector3(8f, 3f, 2f), "Fosso com estacas à frente: corra e salte.\nCair custa um coração.", 5f);
        Gatilho(tutorial, "Tutorial_Deslize", new Vector3(7f, 1.5f, 56f), new Vector3(2f, 3f, 8f), "Ctrl para deslizar sob o galho.\nO deslize começa no chão.", 5f);
        Gatilho(tutorial, "Tutorial_Tronco", new Vector3(25f, 1.5f, 56f), new Vector3(2f, 3f, 8f), "Tronco giratório: espere ele passar e atravesse.\nEncostar custa um coração.", 5f);
        Gatilho(tutorial, "Tutorial_Pilao", new Vector3(40f, 1.5f, 71f), new Vector3(8f, 3f, 2f), "Pilão: observe o ciclo e passe quando ele subir.", 5f);
        Gatilho(tutorial, "Chegada_Posto", new Vector3(40f, 1.5f, 93.5f), new Vector3(8f, 3f, 2f), "Área segura: posto do viveiro. Use E na caixa de ferramentas.", 5f);
        var entradaArena = Gatilho(tutorial, "Entrada_Arena", new Vector3(82f, 1.5f, 98f), new Vector3(2f, 3f, 12f), "MARA (rádio): Sinal estranho à frente... é uma sentinela automática da Vigília. Ela avança em linha reta: desvie e, quando ela bater e travar, retire as baterias com E.", 8f);
        var sentinela = GameObject.Find("Chefe/Sentinela").GetComponent<SentinelaChefe>();
        UnityEventTools.AddPersistentListener(entradaArena.GetComponent<TutorialTrigger>().onEnter, sentinela.Acordar);

        // ---------- vegetação espalhada ----------
        Vegetacao(veg);

        // ---------- iluminação, HUD, Nilo, câmera, GameManager ----------
        ConfigurarAmbiente();
        ConfigurarNilo();
        ConfigurarHud();

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

    private static void Galho(Transform perigos, Transform cenario, Transform armad, int n, Vector3 pos, bool alongZ)
    {
        // barra sólida baixa cruzando o corredor: só passa deslizando (altura livre 1,05 m)
        var g = Caixa(perigos, "Galho_" + n, pos + Vector3.up * 1.2f, alongZ ? new Vector3(0.3f, 0.3f, 8f) : new Vector3(8f, 0.3f, 0.3f), matTronco, true);
        var visual = Modelo("A02_Galho_" + n + "_visual", "Assets/Terceiros/PolyHaven/dead_quiver_branch_02/dead_quiver_branch_02_1k.fbx", pos + Vector3.up * 1.2f, alongZ ? 90f : 0f, 1f, armad);
        visual.transform.localScale = new Vector3(455.94f, 139.21f, 248.72f);
        for (int k = -1; k <= 1; k += 2)
        {
            var sp = alongZ ? pos + new Vector3(0f, 0.5f, k * 3.8f) : pos + new Vector3(k * 3.8f, 0.5f, 0f);
            Caixa(cenario, "Galho_" + n + "_Suporte", sp, new Vector3(0.15f, 1.05f, 0.15f), matMadeira);
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
        var holofote = Modelo("T09_Holofote_sentinela", "Assets/Terceiros/PolyHaven/portable_searchlight/portable_searchlight_1k.fbx", Vector3.zero, 0f, 138.13f, raiz.transform);
        holofote.transform.localPosition = new Vector3(0f, 2.2f, -0.4f); holofote.transform.localRotation = Quaternion.identity;
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
        Modelo("T09_Holofote_torre", "Assets/Terceiros/PolyHaven/portable_searchlight/portable_searchlight_1k.fbx", new Vector3(105f, 5.3f, 108.6f), 180f, 138.13f, tec);
        Luz(luzes, "Luz_Torre", new Vector3(104.5f, 5.4f, 107.8f), new Color(0.85f, 0.95f, 1f), 3f, 18f);
        Modelo("T02_Sensor_camera_arena", "Assets/Terceiros/PolyHaven/security_camera_01/security_camera_01_1k.fbx", new Vector3(81f, 3.2f, 111f), 150f, 1.22f, tec);
        var cantos = new[] { new Vector3(82f, 0f, 86f), new Vector3(106f, 0f, 86f), new Vector3(82f, 0f, 110f), new Vector3(106f, 0f, 110f) };
        for (int i = 0; i < 4; i++)
        {
            Caixa(chefe, "Tocha_Poste", cantos[i] + Vector3.up * 1.2f, new Vector3(0.12f, 2.4f, 0.12f), matMadeira);
            Modelo("C08_Lampiao_arena_" + (i + 1), "Assets/Terceiros/PolyHaven/wooden_lantern_01/wooden_lantern_01_1k.fbx", cantos[i] + Vector3.up * 2.4f, 0f, 0.85f, constr);
            Luz(luzes, "Luz_Arena_" + (i + 1), cantos[i] + Vector3.up * 2.3f, new Color(1f, 0.6f, 0.3f), 2.2f, 13f);
        }
        for (int i = 0; i < 6; i++)
            Modelo("C10_Alambrado_arena_" + (i + 1), "Assets/Terceiros/PolyHaven/modular_chainlink_fence/modular_chainlink_fence_1k.fbx", new Vector3(84f + i * 4f, 0f, 112.3f), 0f, 0.74f, constr);
        Modelo("V09_Toco_arena", "Assets/Terceiros/PolyHaven/tree_stump_01/tree_stump_01_1k.fbx", new Vector3(84f, 0.3f, 108f), 40f, 37.73f, veg);
        Modelo("V08_Tronco_caido_arena", "Assets/Terceiros/PolyHaven/dead_tree_trunk_02/dead_tree_trunk_02_1k.fbx", new Vector3(96f, 0.3f, 85.5f), 0f, 0.99f, veg);
    }

    private static void Vegetacao(Transform veg)
    {
        string[] arvores = {
            "Assets/Terceiros/OpenGameArt/3TD_Tropical/Sumauma/CiabaTree_01.dae|V01_Sumauma|2.0|0",
            "Assets/Terceiros/OpenGameArt/Yughues_Palmeiras_v3/palm_straight.FBX|V02_Palmeira_reta|2.5|270",
            "Assets/Terceiros/OpenGameArt/Yughues_Palmeiras_v3/palm_dual.FBX|V02_Palmeira_dupla|2.6|270",
            "Assets/Terceiros/OpenGameArt/Yughues_Palmeiras_v3/palm_trio.FBX|V02_Palmeira_trio|2.7|270",
            "Assets/Terceiros/OpenGameArt/Yughues_Palmeira_v2/palm_tree.FBX|V03_Palmeira_v2|1.3|270",
            "Assets/Terceiros/OpenGameArt/3TD_Tropical/Embauba/CecropiaTree01.DAE|V04_Embauba|1.9|0",
            "Assets/Terceiros/OpenGameArt/3TD_Tropical/Bananeira/Banana01.DAE|V05_Bananeira|0.7|0",
            "Assets/Terceiros/OpenGameArt/3TD_Tropical/Bambu/Bamboo01.DAE|V07_Bambuzal|1.0|0",
        };
        string[] arbustos = {
            "Assets/Terceiros/PolyHaven/fern_02/fern_02_1k.fbx|V11_Samambaia|1.0|0",
            "Assets/Terceiros/OpenGameArt/Yughues_ArbustosTropicais/01/trop_shrub_01.FBX|V13_Arbusto_tropical_1|0.62|0.7",
            "Assets/Terceiros/OpenGameArt/Yughues_ArbustosTropicais/03/trop_shrub_03.FBX|V13_Arbusto_tropical_3|1.06|0.9",
            "Assets/Terceiros/OpenGameArt/Yughues_ArbustosTropicais/05/trop_shrub_05.FBX|V13_Arbusto_tropical_5|0.72|0.7",
            "Assets/Terceiros/PolyHaven/shrub_03/shrub_03_1k.fbx|V16_Capim_alto|2.0|0",
            "Assets/Terceiros/PolyHaven/weed_plant_02/weed_plant_02_1k.fbx|V17_Mato_rasteiro|2.5|0",
        };
        // espécies pesadas (dezenas de milhares de triângulos) entram uma vez só, em pontos escolhidos
        Modelo("V06_Monguba", "Assets/Terceiros/PolyHaven/pachira_aquatica_01/pachira_aquatica_01_1k.fbx", new Vector3(46f, 0f, 106.5f), 0f, 1.6f, veg);
        Modelo("V12_Arbusto_folha_larga", "Assets/Terceiros/PolyHaven/shrub_04/shrub_04_1k.fbx", new Vector3(-5.5f, 0f, 12f), 0f, 3.2f, veg);
        Modelo("V14_Anturio", "Assets/Terceiros/PolyHaven/anthurium_botany_01/anthurium_botany_01_1k.fbx", new Vector3(3.3f, 0f, 30f), 0f, 1.22f, veg);
        Modelo("V15_Calathea", "Assets/Terceiros/PolyHaven/calathea_orbifolia_01/calathea_orbifolia_01_1k.fbx", new Vector3(-3.3f, 0f, 48f), 0f, 1.41f, veg);
        // garante ao menos uma de cada espécie, depois espalha (densidade maior perto da trilha)
        int ia = 0, ib = 0, placedA = 0, placedB = 0, tent = 0;
        const int metaArvores = 150, metaArbustos = 90;
        while ((placedA < metaArvores || placedB < metaArbustos) && tent++ < 20000)
        {
            float x = Mathf.Lerp(-30f, 140f, (float)rng.NextDouble());
            float z = Mathf.Lerp(-22f, 132f, (float)rng.NextDouble());
            bool arvore = placedA < metaArvores && (placedB >= metaArbustos || rng.NextDouble() < 0.6);
            float margem = arvore ? 1.6f : 0.0f;
            float distTrilha = DistanciaTrilha(x, z);
            if (arvore)
            {
                if (distTrilha < margem) continue;            // tronco fora da trilha
                if (distTrilha > 16f && rng.NextDouble() < 0.7) continue;   // mais denso perto da trilha
            }
            else
            {
                // arbustos só na faixa das bordas: dentro da trilha até 1 m da parede, ou fora até 4 m
                if (distTrilha > 4f) continue;
                if (distTrilha == 0f && DistanciaBorda(x, z) > 1.0f) continue;
            }
            var def = (arvore ? arvores[ia % arvores.Length] : arbustos[ib % arbustos.Length]).Split('|');
            if (arvore) ia++; else ib++;
            float esc = float.Parse(def[2], System.Globalization.CultureInfo.InvariantCulture) * (0.85f + (float)rng.NextDouble() * 0.35f);
            float rx = arvore ? float.Parse(def[3], System.Globalization.CultureInfo.InvariantCulture) : 0f;
            float y = arvore ? 0f : float.Parse(def[3], System.Globalization.CultureInfo.InvariantCulture);
            int idx = arvore ? placedA : placedB;
            var go = Modelo(def[1] + (idx == 0 ? "" : "_" + (idx + 1)), def[0], new Vector3(x, y, z), (float)rng.NextDouble() * 360f, esc, veg);
            if (rx != 0f) go.transform.rotation = Quaternion.Euler(rx, (float)rng.NextDouble() * 360f, 0f);
            if (arvore) placedA++; else placedB++;
        }
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
        RenderSettings.fogStartDistance = 25f;
        RenderSettings.fogEndDistance = 85f;
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
    }

    // ----------------------------------------------------------------------------------------
    private static void CarregarMateriais()
    {
        matChao = Mat("Chao_Floresta"); matMadeira = Mat("Madeira"); matTronco = Mat("Tronco"); matGalho = Mat("Galho"); matMarco = Mat("Marco");
        matPerigo = Mat("Perigo"); matTerminal = Mat("Terminal"); matLedVerde = Mat("LedVerde"); matLedVermelho = Mat("LedVermelho"); matRocha = Mat("Rocha");
        matEstaca = Mat("Estaca"); matFosso = Mat("Fosso"); matLenco = Mat("Lenco"); matCaixote = Mat("Caixote"); matFolhagem = Mat("Folhagem");
        matMata = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Chao_Mata.mat");
        if (matMata == null)
        {
            matMata = new Material(matChao);
            matMata.color = new Color(0.55f, 0.7f, 0.5f);
            matMata.mainTextureScale = matChao.mainTextureScale * 2f;
            AssetDatabase.CreateAsset(matMata, "Assets/Materials/Chao_Mata.mat");
        }
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
        Caixa(cenario, nome + "Poste", pos + Vector3.up * 0.5f, new Vector3(0.06f, 0.9f, 0.06f), matMadeira);
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
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = Vector3.one * escala;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
        {
            if (t == go.transform) continue;
            string n = t.name.ToLowerInvariant();
            if ((n.Contains("_lod") && !n.Contains("_lod0")) || n.Contains("collision") || n.Contains("colbox") || n.EndsWith("_64") || n.EndsWith("_80") || n.EndsWith("_128") || n.EndsWith("_256"))
                t.gameObject.SetActive(false);
        }
        var lod = go.GetComponent<LODGroup>();
        if (lod != null)
        {
            var lods = lod.GetLODs();
            for (int i = 1; i < lods.Length; i++) foreach (var r in lods[i].renderers) if (r != null) r.gameObject.SetActive(false);
            Object.DestroyImmediate(lod);
        }
        return go;
    }
}
