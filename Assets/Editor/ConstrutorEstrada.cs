using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

// Nível 4B — A estrada velha: fuga de moto entre o posto das raízes e o pé da serra, com o jipe da
// Vigília atrás. Mesmas peças das outras fases (chão, Terrain, floresta, HUD, cutscenes, luz).
// Menu: Rastro do Dossel > Montar Nível 4B (moto).
public static partial class ConstrutorNivel1
{
    private const string N4bPath = "Assets/Scenes/Nivel4B_EstradaVelha.unity";

    // Explosão de cutscene: partículas, som e clarão no tempo t (o clarão apaga 0,25 s depois) e tremor de câmera.
    private static GameObject ExplosaoCena(List<Cutscene.Evento> ev, List<Cutscene.Tremor> tr, Transform pai, Vector3 pos, string nome, float escala, float t)
    {
        var raiz = Explosao(pai, pos, nome, escala).transform.parent.gameObject;
        var som = Som(raiz, AUD + "explosao.wav"); som.maxDistance = 80f; som.spatialBlend = 0.4f;
        var luz = Luz(raiz.transform, nome + "_Clarao", pos + Vector3.up, new Color(1f, 0.6f, 0.25f), 6f * escala, 16f * escala);
        luz.gameObject.SetActive(false);
        ev.Add(new Cutscene.Evento { tempo = t, tocar = raiz.GetComponentsInChildren<ParticleSystem>(true), ligar = new[] { luz.gameObject }, som = som });
        ev.Add(new Cutscene.Evento { tempo = t + 0.25f, desligar = new[] { luz.gameObject } });
        tr.Add(new Cutscene.Tremor { inicio = t, duracao = 0.5f + 0.3f * escala, forca = 0.22f * escala });
        return raiz;
    }

    // Fim da sentinela (níveis 1 e 5): faíscas, explosão grande e fumaça presas a ela; as tomadas acompanham
    // a máquina, que para em qualquer ponto da arena.
    private static void CenaSentinelaExplode(SentinelaChefe sentinela, string id)
    {
        var evS = new List<Cutscene.Evento>(); var trS = new List<Cutscene.Tremor>();
        var sp = sentinela.transform.position;
        ExplosaoCena(evS, trS, sentinela.transform, sp + V(0.6f, 1.6f, 0f), "Faiscas_Sentinela", 0.35f, 0.5f);
        ExplosaoCena(evS, trS, sentinela.transform, sp + V(-0.5f, 1.2f, 0.4f), "Faiscas_Sentinela_2", 0.35f, 1.1f);
        ExplosaoCena(evS, trS, sentinela.transform, sp + Vector3.up * 1.2f, "Explosao_Sentinela", 1.9f, 2.2f);
        var fumS = Fumaca(sentinela.transform, sp + Vector3.up * 1.5f, "Fumaca_Sentinela", false); fumS.gameObject.SetActive(false);
        evS.Add(new Cutscene.Evento { tempo = 2.4f, ligar = new[] { fumS.gameObject } });
        var cena = NovaCutscene(id, new[] {
            K(sp.x - 1.5f, 2f, sp.z - 5f,   sp.x, 1.5f, sp.z, 0.4f),
            K(sp.x - 2.5f, 1.6f, sp.z - 6f, sp.x, 1.4f, sp.z, 1.6f),
            K(sp.x - 12f, 7f, sp.z - 12f,   sp.x, 1f, sp.z,   2.8f),
        }, new[] {
            F(0.2f, 1.9f, "<color=#9FE0FF>MARA (rádio):</color> Ela está faiscando! Sai de perto, Sabiá!"),
            F(3.0f, 4.8f, "<color=#FFC46B>NILO:</color> ...essa não levanta mais."),
        });
        cena.ancora = sentinela.transform; cena.ancoraOrigem = sp;
        cena.eventos = evS.ToArray(); cena.tremores = trS.ToArray();
        sentinela.cenaFim = cena;
    }

    // Explosão durante o jogo: ao cruzar o gatilho, toca as partículas e o som e liga o que estiver em "ligar".
    private static void ExplosaoNoCaminho(string nome, Vector3 gatilho, Vector3 tamGatilho, Vector3 pos, float escala, GameObject[] ligar, string aviso)
    {
        var raiz = Explosao(perigos, pos, nome, escala).transform.parent.gameObject;
        var som = Som(raiz, AUD + "explosao.wav"); som.maxDistance = 60f; som.spatialBlend = 0.5f;
        var g = Gatilho(tutorial, "Gatilho_" + nome, gatilho, tamGatilho, aviso, 2.5f);
        var on = g.GetComponent<TutorialTrigger>().onEnter;
        foreach (var ps in raiz.GetComponentsInChildren<ParticleSystem>(true)) UnityEventTools.AddVoidPersistentListener(on, new UnityAction(ps.Play));
        UnityEventTools.AddVoidPersistentListener(on, new UnityAction(som.Play));
        foreach (var go in ligar) UnityEventTools.AddBoolPersistentListener(on, go.SetActive, true);
    }

    // Zona de perigo (trigger) do tipo Hit: tira um coração e freia a moto.
    private static GameObject ZonaPerigo(Transform pai, string nome, Vector3 pos, Vector3 tam)
    {
        var z = new GameObject(nome); z.transform.SetParent(pai); z.transform.position = pos;
        z.layer = LayerMask.NameToLayer("Hazard"); z.tag = "Hazard";
        var bc = z.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = tam;
        z.AddComponent<Hazard>().mode = Hazard.Mode.Hit;
        return z;
    }

    // Moto velha do Ivo, de primitivas: rodas, quadro, tanque, banco, guidão e farol.
    private static Moto MontarMoto(Vector3 pos)
    {
        var nilo = GameObject.Find("Nilo");
        var raiz = new GameObject("N4B-03_Moto_Ivo");
        raiz.transform.position = pos;
        raiz.tag = "Player"; raiz.layer = nilo.layer;
        var rb = raiz.AddComponent<Rigidbody>(); rb.mass = 120f; rb.angularDamping = 5f;
        var col = raiz.AddComponent<CapsuleCollider>(); col.direction = 2; col.radius = 0.38f; col.height = 2f; col.center = V(0f, 0.42f, 0f);
        string pmPath = "Assets/Settings/SemAtrito.asset";
        var pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(pmPath);
        if (pm == null)
        {
            pm = new PhysicsMaterial("SemAtrito") { dynamicFriction = 0f, staticFriction = 0f, frictionCombine = PhysicsMaterialCombine.Minimum, bounciness = 0f };
            AssetDatabase.CreateAsset(pm, pmPath);
        }
        col.sharedMaterial = pm;
        var vis = new GameObject("Visual").transform; vis.SetParent(raiz.transform, false);
        var rodas = new List<Transform>();
        foreach (float z in new[] { -0.68f, 0.7f })
        {
            var eixo = new GameObject("Roda").transform; eixo.SetParent(vis, false); eixo.localPosition = V(0f, 0.34f, z);
            Cilindro(eixo, "Pneu", eixo.position, V(0.66f, 0.07f, 0.66f), Quaternion.Euler(0f, 0f, 90f), matRocha);
            Caixa(eixo, "Raio", eixo.position, V(0.1f, 0.5f, 0.06f), matTerminal);
            rodas.Add(eixo);
        }
        Caixa(vis, "Quadro", pos + V(0f, 0.6f, 0f), V(0.22f, 0.22f, 1.25f), matTerminal);
        Caixa(vis, "Tanque", pos + V(0f, 0.86f, 0.28f), V(0.36f, 0.24f, 0.5f), matLenco);
        Caixa(vis, "Banco", pos + V(0f, 0.82f, -0.3f), V(0.3f, 0.1f, 0.6f), matMadeira);
        Caixa(vis, "Garfo", pos + V(0f, 0.62f, 0.62f), V(0.08f, 0.6f, 0.08f), matTerminal).transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
        Cilindro(vis, "Guidao", pos + V(0f, 1.02f, 0.56f), V(0.05f, 0.34f, 0.05f), Quaternion.Euler(0f, 0f, 90f), matTerminal);
        Caixa(vis, "Farol", pos + V(0f, 0.9f, 0.74f), V(0.2f, 0.16f, 0.1f), matLedVerde);
        Caixa(vis, "Escapamento", pos + V(0.2f, 0.42f, -0.45f), V(0.08f, 0.08f, 0.7f), matEstaca);
        var farol = new GameObject("Luz_Farol"); farol.transform.SetParent(vis, false); farol.transform.localPosition = V(0f, 0.95f, 0.8f);
        farol.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
        var l = farol.AddComponent<Light>(); l.type = LightType.Spot; l.range = 30f; l.spotAngle = 55f; l.intensity = 30f; l.color = new Color(1f, 0.92f, 0.7f);
        var assento = new GameObject("Assento").transform; assento.SetParent(raiz.transform, false); assento.localPosition = V(0f, 0.3f, -0.25f);
        var moto = raiz.AddComponent<Moto>();
        moto.nilo = nilo.GetComponent<PlayerController>(); moto.assento = assento; moto.visual = vis; moto.rodas = rodas.ToArray();
        moto.motor = Som(raiz, AUD + "motor_moto.wav"); moto.motor.loop = true; moto.motor.playOnAwake = true; moto.motor.volume = 0.35f; moto.motor.spatialBlend = 0f;
        return moto;
    }

    // Jipe da Vigília, de primitivas, com faróis e giroflex.
    private static GameObject MontarJipe(Vector3 pos)
    {
        var raiz = new GameObject("N4B-04_Jipe_Vigilia"); raiz.transform.SetParent(perigos); raiz.transform.position = pos;
        Caixa(raiz.transform, "Carroceria", pos + V(0f, 0.9f, 0f), V(2f, 0.9f, 3.8f), matTerminal);
        Caixa(raiz.transform, "Cabine", pos + V(0f, 1.65f, -0.4f), V(1.8f, 0.7f, 1.8f), matPerigo);
        Caixa(raiz.transform, "Parachoque", pos + V(0f, 0.6f, 1.95f), V(2.1f, 0.25f, 0.15f), matEstaca);
        foreach (var p in new[] { V(-1f, 0.42f, 1.2f), V(1f, 0.42f, 1.2f), V(-1f, 0.42f, -1.2f), V(1f, 0.42f, -1.2f) })
            Cilindro(raiz.transform, "Roda", pos + p, V(0.84f, 0.12f, 0.84f), Quaternion.Euler(0f, 0f, 90f), matRocha);
        Caixa(raiz.transform, "Giroflex", pos + V(0f, 2.08f, -0.4f), V(0.5f, 0.18f, 0.25f), matLedVermelho);
        for (int k = -1; k <= 1; k += 2)
        {
            var f = new GameObject("Luz_Farol_Jipe"); f.transform.SetParent(raiz.transform); f.transform.position = pos + V(k * 0.7f, 1f, 2f);
            f.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            var l = f.AddComponent<Light>(); l.type = LightType.Spot; l.range = 28f; l.spotAngle = 50f; l.intensity = 22f; l.color = new Color(0.9f, 0.95f, 1f);
        }
        Luz(raiz.transform, "Luz_Giroflex", pos + V(0f, 2.4f, -0.4f), new Color(1f, 0.15f, 0.1f), 3f, 12f);
        return raiz;
    }

    [MenuItem("Rastro do Dossel/Montar Nível 4B (moto)")]
    public static void MontarN4b()
    {
        var scene = PrepararCena(N4bPath, "N4B", 45);
        // estrada em S: largada no galpão do Ivo, curva leste, subida norte, curva oeste e a reta da ponte
        Trilha = new[] { R(-5, -40, 5, 80), R(-5, 80, 45, 92), R(35, 92, 45, 170), R(-25, 170, 45, 182), R(-25, 182, -15, 300), R(-25, 320, -15, 340) };
        Fossos = new[] { R(-25, 300, -15, 320) };   // vão do córrego: só as tábuas da ponte seguram a moto
        Aguas = new[] { R(-60, 302, 40, 318) };
        areaMata = new[] { -70f, -60f, 90f, 360f };
        relevo = new[] { 6f, 26f, 8f };
        Chao(false);
        registrarOcupacao = true;
        var moveis = new GameObject("Cena_Moveis").transform;   // fora das raízes estáticas: estes objetos se movem nas cenas

        // ---------- galpão do Ivo (largada) ----------
        for (int k = 0; k < 4; k++) Caixa(locais, "N4B-01_Galpao_Poste", V(-8.5f + (k % 2) * 5f, 1.6f, -6f + (k < 2 ? 0f : 6f)), V(0.2f, 3.2f, 0.2f), matMadeira);
        Caixa(locais, "N4B-01_Galpao_Telhado", V(-6f, 3.3f, -3f), V(5.8f, 0.14f, 7f), matMadeira).transform.rotation = Quaternion.Euler(0f, 0f, 6f);
        KitOficina(V(-7f, 0f, 2f), 90f);
        Solido(ModeloAlvo("C07_Fogueira_galpao", PH + "stone_fire_pit/stone_fire_pit_1k.fbx", V(8f, 0f, -4f), 0f, 1.3f, false, constr));
        Luz(luzes, "Luz_Fogueira_Galpao", V(8f, 1f, -4f), new Color(1f, 0.55f, 0.2f), 2.6f, 11f);
        Fumaca(cenario, V(8f, 0.6f, -4f), "Fumaca_Fogueira_Galpao", false);
        var ivo = Boneco("Ivo", "ivo", V(-3.4f, 0f, 1.5f), 120f);
        var barris = new List<GameObject>();
        for (int i = 0; i < 3; i++) barris.Add(Cilindro(moveis, "Barril_Galpao_" + (i + 1), V(-8f + i * 0.9f, 0.5f, -12f), V(0.8f, 0.5f, 0.8f), Quaternion.identity, matPerigo));

        var moto = MontarMoto(V(0f, 0.05f, 0f));
        var pontos = new[] { V(0f, 0f, -30f), V(0f, 0f, 86f), V(40f, 0f, 86f), V(40f, 0f, 176f), V(-20f, 0f, 176f), V(-20f, 0f, 340f) };
        var rota = new GameObject("Rota_Jipe").transform; rota.SetParent(perigos);
        var pts = new List<Transform>();
        for (int i = 1; i < pontos.Length; i++) { var p = new GameObject("Ponto_" + i).transform; p.SetParent(rota); p.position = pontos[i]; pts.Add(p); }
        var jipe = MontarJipe(V(0f, 0f, -30f));
        jipe.transform.SetParent(moveis);
        var per = jipe.AddComponent<Perseguidor>(); per.moto = moto; per.pontos = pts.ToArray();
        per.distanciaLonge = 18f;
        per.sirene = Som(jipe, AUD + "sirene_vigilia.wav"); per.sirene.loop = true; per.sirene.maxDistance = 60f; per.sirene.volume = 0.5f;

        // ---------- trecho A: barreiras em zigue-zague ----------
        int nb = 0;
        foreach (var b in new[] { V(-2.6f, 0f, 38f), V(2.6f, 0f, 52f), V(-2.6f, 0f, 66f) })
        {
            nb++;
            Modelo("C06_Barreira_Vigilia_estrada_" + nb, PH + "concrete_road_barrier/concrete_road_barrier_1k.fbx", b, 0f, 1.04f, armad);
            ZonaPerigo(perigos, "Barreira_Perigo_" + nb, b + Vector3.up * 0.6f, V(4.6f, 1.2f, 0.8f));
        }
        Placa(cenario, "A05_Placa_curva_1", V(6.4f, 0f, 74f));

        // ---------- trecho C: troncos que caem da encosta ----------
        int nt = 0;
        foreach (var t in new[] { new[] { 112f, 0f }, new[] { 134f, 1.2f }, new[] { 156f, 0.5f } })
        {
            nt++;
            var raiz = new GameObject("Tronco_Queda_" + nt); raiz.transform.SetParent(perigos); raiz.transform.position = V(40f, 4.4f, t[0]);
            raiz.layer = LayerMask.NameToLayer("Hazard"); raiz.tag = "Hazard";
            raiz.AddComponent<Rigidbody>();
            var mh = raiz.AddComponent<MovingHazard>(); mh.travel = V(0f, -3.6f, 0f); mh.moveTime = 0.35f; mh.holdStart = 1.7f; mh.holdEnd = 0.9f; mh.startOffset = t[1];
            mh.cue = Som(raiz, AUD + "tronco_aviso.wav");
            Cilindro(raiz.transform, "Tronco", raiz.transform.position, V(0.9f, 5.2f, 0.9f), Quaternion.Euler(0f, 0f, 90f), matTronco);
            var bc = raiz.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = V(10f, 0.9f, 0.9f);
            raiz.AddComponent<Hazard>().mode = Hazard.Mode.Hit;
            for (int k = -1; k <= 1; k += 2) Caixa(cenario, "Tronco_Queda_Poste", V(40f + k * 5.4f, 3f, t[0]), V(0.25f, 6f, 0.25f), matMadeira);
        }
        Gatilho(tutorial, "Dica_Troncos", V(40f, 1.5f, 100f), V(10f, 3f, 1.5f), "Troncos caindo: o som avisa. Freie (S) ou acelere na hora certa.", 4f);

        // ---------- trecho D: tanque de combustível da Vigília explode e deixa destroços em chamas ----------
        var destrocos = new GameObject("Destrocos_Fogo"); destrocos.transform.SetParent(perigos);
        Caixa(destrocos.transform, "Destroco_1", V(14f, 0.5f, 174f), V(3f, 1f, 4f), matEstaca).transform.rotation = Quaternion.Euler(0f, 25f, 8f);
        Caixa(destrocos.transform, "Destroco_2", V(10f, 0.35f, 178f), V(2.4f, 0.7f, 2f), matTerminal).transform.rotation = Quaternion.Euler(0f, -15f, -6f);
        Luz(destrocos.transform, "Luz_Fogo_Destrocos", V(12f, 1.5f, 176f), new Color(1f, 0.5f, 0.15f), 4f, 14f);
        Fumaca(destrocos.transform, V(12f, 1f, 176f), "Fumaca_Destrocos", false);
        ZonaPerigo(destrocos.transform, "Fogo_Perigo", V(12f, 0.8f, 175.5f), V(6f, 1.6f, 7f));
        destrocos.SetActive(false);
        Cilindro(cenario, "Tanque_Combustivel_Vigilia", V(12f, 1f, 184f), V(1.6f, 1f, 1.6f), Quaternion.identity, matPerigo);
        ExplosaoNoCaminho("Explosao_Tanque", V(30f, 1.5f, 176f), V(1.5f, 3f, 12f), V(12f, 1f, 178f), 1.6f, new[] { destrocos },
            "MARA: Eles explodiram o tanque! Desvie pelo lado!");

        // ---------- trecho E: holofotes da Vigília cruzando a reta e explosão na ponte velha ----------
        Holofote(V(-28f, 4f, 210f), 90f, 8f, 3.2f, 35f);
        Holofote(V(-12f, 4f, 236f), 270f, 8f, 2.6f, 35f);
        Holofote(V(-28f, 4f, 262f), 90f, 8f, 2.2f, 35f);
        Gatilho(tutorial, "Dica_Holofotes", V(-20f, 1.5f, 196f), V(10f, 3f, 1.5f), "Holofotes: passe pelo lado escuro da estrada.", 3f);
        var entulho = new GameObject("Entulho_Estrada"); entulho.transform.SetParent(perigos);
        for (int i = 0; i < 4; i++) Caixa(entulho.transform, "Entulho", V(-17.5f - i * 0.6f, 0.4f, 282f + i * 1.3f), V(1.6f, 0.8f, 1.2f), matRocha).transform.rotation = Quaternion.Euler(0f, 30f * i, 10f);
        ZonaPerigo(entulho.transform, "Entulho_Perigo", V(-18.5f, 0.6f, 284f), V(4.5f, 1.2f, 6f));
        entulho.SetActive(false);
        ExplosaoNoCaminho("Explosao_Barranco", V(-20f, 1.5f, 270f), V(10f, 3f, 1.5f), V(-15f, 1.5f, 282f), 1.3f, new[] { entulho }, "");

        // ponte de tábuas sobre o córrego (as tábuas caem na cena final)
        var ponte = new GameObject("N4B-05_Ponte_Velha"); ponte.transform.SetParent(moveis);
        var tabuas = new List<Transform>();
        for (int i = 0; i < 8; i++) tabuas.Add(Caixa(ponte.transform, "Tabua_" + (i + 1), V(-20f, -0.06f, 301.25f + i * 2.5f), V(10.4f, 0.12f, 2.5f), matMadeira, "Ground", "Ground").transform);
        for (int k = -1; k <= 1; k += 2) for (int i = 0; i < 3; i++) Caixa(ponte.transform, "Ponte_Poste", V(-20f + k * 5.3f, 0.6f, 302f + i * 8f), V(0.25f, 1.2f, 0.25f), matMadeira);

        var chegadaGo = new GameObject("Chegada_Serra"); chegadaGo.transform.SetParent(perigos); chegadaGo.transform.position = V(-20f, 1.5f, 326f);
        chegadaGo.layer = LayerMask.NameToLayer("Ignore Raycast");
        var cbc = chegadaGo.AddComponent<BoxCollider>(); cbc.isTrigger = true; cbc.size = V(10f, 3f, 2f);
        var chegada = chegadaGo.AddComponent<Chegada>();
        Caixa(locais, "N4B-02_Portal_serra_Poste", V(-25.5f, 2f, 330f), V(0.3f, 4f, 0.3f), matMadeira);
        Caixa(locais, "N4B-02_Portal_serra_Poste", V(-14.5f, 2f, 330f), V(0.3f, 4f, 0.3f), matMadeira);
        Caixa(locais, "N4B-02_Portal_serra_Viga", V(-20f, 4f, 330f), V(11.4f, 0.3f, 0.3f), matMadeira);
        Antena(V(-10f, 0f, 400f), false);

        Lanternas(new[] { new Vector2(0f, -30f), new Vector2(0f, 86f), new Vector2(40f, 86f), new Vector2(40f, 176f), new Vector2(-20f, 176f), new Vector2(-20f, 300f) }, 5.75f);
        Gatilho(tutorial, "Dica_Moto", V(0f, 1.5f, 8f), V(10f, 3f, 1.5f), "Moto: W acelera, S freia, A/D esterçam.\nEspaço: freio de mão para derrapar nas curvas.", 6f);
        Gatilho(tutorial, "Dica_Curva", V(0f, 1.5f, 72f), V(10f, 3f, 1.5f), "Curva fechada: solte o W ou use o freio de mão.", 3f);

        // ---------- cutscenes ----------
        CriarUICutscene();
        var ev = new List<Cutscene.Evento>(); var tr = new List<Cutscene.Tremor>();
        ExplosaoCena(ev, tr, moveis, V(-7f, 0.8f, -12f), "Explosao_Galpao", 1.5f, 6.4f);
        var abertura = NovaCutscene("Abertura_N4B", new[] {
            K(9f, 3.5f, -9f,    0f, 0.8f, 0f,    0.5f),
            K(2.6f, 1.2f, 3.4f, 0f, 0.8f, 0f,    3.6f),
            K(4f, 5f, 12f,      0f, 0.5f, -30f,  3.2f),
            K(6f, 3f, -2f,      -7f, 0.8f, -12f, 1.6f),
            K(0f, 2.6f, -5.5f,  0f, 1f, 10f,     2.6f),
        }, new[] {
            F(0.3f, 4.2f, "RASTRO DO DOSSEL\n<size=40>Nível 4B — A estrada velha</size>", true),
            F(1.0f, 4.0f, "<color=#C8F08C>IVO:</color> Ela pega no tranco, mas pega. Foi do meu pai."),
            F(4.1f, 7.2f, "<color=#9FE0FF>MARA (rádio):</color> Sabiá, tem um jipe da Vigília descendo atrás de você!"),
            F(7.3f, 9.4f, "<color=#C8F08C>IVO:</color> VAI! A estrada velha desce até o pé da serra!"),
            F(9.5f, 11.4f, "<color=#FFC46B>NILO:</color> Segura firme, Ivo."),
        });
        abertura.tocarAoIniciar = true;
        // o jipe aparece na estrada e os barris do galpão voam com a explosão
        var movs = new List<Cutscene.Movimento> { new Cutscene.Movimento { alvo = jipe.transform, posicao = V(0f, 0f, -14f), rotacao = Vector3.zero, inicio = 3.6f, duracao = 3f } };
        for (int i = 0; i < barris.Count; i++)
            movs.Add(new Cutscene.Movimento { alvo = barris[i].transform, posicao = V(-10f + i * 3f, 0.4f, -15f - i), rotacao = V(80f + 20f * i, 40f * i, 90f), inicio = 6.4f, duracao = 0.7f });
        abertura.eventos = ev.ToArray(); abertura.tremores = tr.ToArray(); abertura.movimentos = movs.ToArray();
        abertura.neblinaFim = 90f;

        ev = new List<Cutscene.Evento>(); tr = new List<Cutscene.Tremor>();
        ExplosaoCena(ev, tr, moveis, V(-20f, 0.5f, 306f), "Explosao_Ponte_1", 1.4f, 2.2f);
        ExplosaoCena(ev, tr, moveis, V(-20f, 0.5f, 314f), "Explosao_Ponte_2", 1.8f, 2.6f);
        var final = NovaCutscene("Final_N4B", new[] {
            K(-11f, 2.2f, 327f,  -20f, 0.8f, 305f, 0.5f),
            K(-9f, 3.5f, 324f,   -20f, 0.6f, 300f, 2.4f),
            K(-30f, 6f, 322f,    -20f, 0f, 306f,   2.6f),
            K(-12.5f, 3.2f, 331f, -20f, 0f, 306f,   2.6f),
            K(-24f, 9f, 338f,    -10f, 18f, 400f,  3.2f),
        }, new[] {
            F(0.3f, 2.2f, "<color=#9FE0FF>MARA (rádio):</color> Ele ainda está atrás de você!"),
            F(2.3f, 4.4f, "<color=#FFC46B>NILO:</color> Não por muito tempo."),
            F(5.6f, 8.0f, "<color=#B8B8B8>VIGÍLIA (rádio):</color> ...a ponte caiu! Perdemos o Sabiá na serra!"),
            F(8.1f, 11.0f, "<color=#9FE0FF>MARA:</color> Daqui é a pé. A antena está logo acima."),
            F(9.6f, 11.2f, "FIM DO NÍVEL 4B", true),
        });
        var movsF = new List<Cutscene.Movimento> { new Cutscene.Movimento { alvo = jipe.transform, posicao = V(-20f, 0f, 297f), rotacao = V(0f, 12f, 0f), inicio = 0.2f, duracao = 2.4f } };
        // tábuas: sobem com a explosão girando e caem espalhadas no córrego (dois movimentos seguidos no mesmo alvo)
        for (int i = 0; i < tabuas.Count; i++)
        {
            float lado = i % 2 == 0 ? 1f : -1f, ini = 2.25f + (i % 4) * 0.07f;
            movsF.Add(new Cutscene.Movimento { alvo = tabuas[i], posicao = tabuas[i].position + V(lado * (1f + i % 3), 3f + (i % 3) * 1.2f, (i - 3.5f) * 0.4f), rotacao = V(70f * lado, 25f * i, 40f * lado), inicio = ini, duracao = 0.4f });
            movsF.Add(new Cutscene.Movimento { alvo = tabuas[i], posicao = tabuas[i].position + V(lado * (2.5f + i % 3), -6.5f, (i - 3.5f) * 0.6f), rotacao = V(150f * lado, 60f * i, 95f * lado), inicio = ini + 0.4f, duracao = 0.75f, acelerar = true });
        }
        final.eventos = ev.ToArray(); final.tremores = tr.ToArray(); final.movimentos = movsF.ToArray();
        final.neblinaFim = 160f;
        chegada.cena = final;
        GarantirCatalogo();

        registrarOcupacao = false;
        Vegetacao(veg, new[] { -50f, -45f, 70f, 345f }, 420, 300);
        Finalizar(scene, Ambiente.Madrugada, V(0f, 0.05f, 0f), 0f,
            "Nível 4B — A estrada velha", "fugir de moto até o pé da serra", 0,
            "", "", "Chegar ao pé da serra antes do jipe",
            "Nível 4B concluído", "A ponte caiu atrás do Nilo. O jipe ficou do outro lado do córrego.", false,
            "Assets/Settings/PerfilPos_N4.asset");
    }
}
