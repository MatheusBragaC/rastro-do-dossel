using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Terrain de cada fase: relevo, lagoas e pintura com as dez camadas de textura (L01 a L10).
// A trilha continua com colisores simples (Chao): o Terrain forma encostas, margens, fundos e o chão da mata.
// Montado em Finalizar, depois de tudo posicionado, para respeitar objetos, águas e vistas das cutscenes.
public static partial class ConstrutorNivel1
{
    private const string PastaTerreno = "Assets/Terreno";
    private const float ChaoMata = -0.05f;        // altura do chão fora da trilha (o topo da trilha fica em 0,015)
    private const float BaseTerreno = -4f, AlturaTerreno = 26f;
    private const float TopoTrilha = 0.015f;      // o Terrain desenha o caminho na altura do colisor da trilha

    // relevo da fase: distância da trilha em que o chão começa a subir, largura da subida e altura máxima (m)
    private static float[] relevo = { 5f, 30f, 7f };
    private static Terrain terrenoAtual;

    private static readonly string[] NomesCamadas = {
        "L01_TerraSeca", "L02_TerraUmida", "L03_Lama", "L04_AreiaMargem", "L05_Cascalho",
        "L06_RochaExposta", "L07_GramaBaixa", "L08_FolhasCaidas", "L09_SoloMusgo", "L10_SoloRecuperacao" };

    // ============================================================================================
    // Lagoa só visual fora da trilha (fases sem rio): dá margem para a areia e a lama.
    // Chamada depois da vegetação; tira as árvores que caíram dentro dela.
    private static void Lagoa(float[] r)
    {
        var lista = new List<float[]>(Aguas) { r };
        Aguas = lista.ToArray();
        Espelho("Agua_Lagoa_" + lista.Count, r);
        var remover = new List<GameObject>();
        foreach (Transform t in veg)
        {
            var p = t.position;
            if (p.x > r[0] - 3f && p.x < r[2] + 3f && p.z > r[1] - 3f && p.z < r[3] + 3f) remover.Add(t.gameObject);   // 3 m: largura da margem
        }
        foreach (var g in remover) Object.DestroyImmediate(g);
        foreach (var o in ocupados)
            if (o.x > r[0] - o.z && o.x < r[2] + o.z && o.y > r[1] - o.z && o.y < r[3] + o.z)
                Debug.LogWarning("Lagoa sobre área ocupada em " + o.x + ", " + o.y);
    }

    // ============================================================================================
    // Terrain da fase

    private static void MontarTerreno(UnityEngine.SceneManagement.Scene scene)
    {
        var camadas = CamadasTerreno();
        float x0 = areaMata[0], z0 = areaMata[1], sx = areaMata[2] - x0, sz = areaMata[3] - z0;
        const int res = 513, resA = 512;
        int semente = 0; foreach (char c in scene.name) semente = semente * 31 + c;
        float ox = (semente & 1023) * 0.37f, oz = ((semente >> 10) & 1023) * 0.53f;

        // o asset é reaproveitado (não apagado e recriado): assim a pintura é gravada e o GUID não muda
        if (!AssetDatabase.IsValidFolder(PastaTerreno)) AssetDatabase.CreateFolder("Assets", "Terreno");
        string path = PastaTerreno + "/" + scene.name + "_Terreno.asset";
        var td = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
        if (td == null) { td = new TerrainData(); AssetDatabase.CreateAsset(td, path); }
        td.heightmapResolution = res;
        td.alphamapResolution = resA;
        td.baseMapResolution = 512;
        td.size = new Vector3(sx, AlturaTerreno, sz);
        td.terrainLayers = camadas;

        // ---------- relevo ----------
        var tetos = TetosCutscene();
        float celula = sx / (res - 1);
        var alt = new float[res, res];
        for (int j = 0; j < res; j++)
            for (int i = 0; i < res; i++)
            {
                float wx = x0 + i * sx / (res - 1), wz = z0 + j * sz / (res - 1);
                alt[j, i] = (Altura(wx, wz, ox, oz, celula, tetos) - BaseTerreno) / AlturaTerreno;
            }
        td.SetHeights(0, 0, alt);

        // ---------- pintura ----------
        var rochasPts = new List<Vector3>();
        foreach (Transform t in rochas) { var b = Limites(t.gameObject); rochasPts.Add(new Vector3(b.center.x, b.center.z, Mathf.Min(Mathf.Max(b.extents.x, b.extents.z), 6f) + 1.2f)); }
        var recPts = new List<Vector3>();
        foreach (Transform t in recup) recPts.Add(new Vector3(t.position.x, t.position.z, 2.5f));
        foreach (Transform t in locais) if (t.name.Contains("Muda") || t.name.Contains("Canteiro") || t.name.Contains("regeneracao")) recPts.Add(new Vector3(t.position.x, t.position.z, 3f));
        foreach (var c in Object.FindObjectsByType<Coletavel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.tipo == "Mudas") recPts.Add(new Vector3(c.transform.position.x, c.transform.position.z, 2.2f));

        var a = new float[resA, resA, 10];
        var w = new float[10];
        var soma = new double[10]; int nVisiveis = 0;
        for (int j = 0; j < resA; j++)
            for (int i = 0; i < resA; i++)
            {
                float u = (i + 0.5f) / resA, v = (j + 0.5f) / resA;
                float wx = x0 + u * sx, wz = z0 + v * sz;
                float h = td.GetInterpolatedHeight(u, v) + BaseTerreno;
                float inc = td.GetSteepness(u, v);
                Pesos(w, wx, wz, h, inc, rochasPts, recPts);
                float total = 0f;
                for (int k = 0; k < 10; k++) { w[k] = w[k] * w[k]; total += w[k]; }   // ao quadrado: transições mais nítidas
                for (int k = 0; k < 10; k++) a[j, i, k] = w[k] / total;
                float d = DistTrilha(wx, wz);
                if (d > 0.2f && d < 18f) { nVisiveis++; for (int k = 0; k < 10; k++) soma[k] += a[j, i, k]; }
            }
        td.SetAlphamaps(0, 0, a);
        // as texturas de pintura são sub-assets: sem marcar cada uma, a pintura não é gravada no disco
        foreach (var tex in td.alphamapTextures) EditorUtility.SetDirty(tex);
        EditorUtility.SetDirty(td);
        AssetDatabase.SaveAssets();

        var go = Terrain.CreateTerrainGameObject(td);
        go.name = "Terrain_Mata";
        go.transform.SetParent(terreno);
        go.transform.position = new Vector3(x0, BaseTerreno, z0);
        go.layer = LayerMask.NameToLayer("Ground"); go.tag = "Ground";
        var ter = go.GetComponent<Terrain>();
        ter.materialTemplate = MaterialTerreno();
        ter.drawInstanced = true;
        ter.heightmapPixelError = 4f;
        ter.basemapDistance = 140f;
        ter.drawTreesAndFoliage = false;
        ter.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        terrenoAtual = ter;
        // a trilha passa a ser desenhada pelo Terrain; as lajes ficam só como colisor (salto e deslize iguais)
        foreach (Transform t in terreno) if (t.name.StartsWith("Trilha_")) { var mr = t.GetComponent<MeshRenderer>(); if (mr != null) mr.enabled = false; }
        GramineasEArvores(ter, x0, z0, sx, sz, tetos);

        var log = new System.Text.StringBuilder(scene.name + " — Terrain " + sx + " x " + sz + " m; camadas na faixa visível (até 18 m da trilha):");
        for (int k = 0; k < 10; k++) log.Append(" " + NomesCamadas[k].Substring(0, 3) + " " + (100.0 * soma[k] / Mathf.Max(1, nVisiveis)).ToString("0.0") + "%");
        Debug.Log(log.ToString());
    }

    // distância à trilha + fossos (0 dentro) e se o ponto está num fosso (com meia célula de folga)
    private static float DistTrilha(float x, float z)
    {
        float best = float.MaxValue;
        for (int n = 0; n < 2; n++)
            foreach (var r in n == 0 ? Trilha : Fossos)
            {
                float dx = Mathf.Max(r[0] - x, 0f, x - r[2]), dz = Mathf.Max(r[1] - z, 0f, z - r[3]);
                float d2 = dx * dx + dz * dz;
                if (d2 < best) best = d2;
            }
        return Mathf.Sqrt(best);
    }

    private static bool Dentro(float[][] rets, float x, float z, float folga)
    {
        foreach (var r in rets) if (x >= r[0] - folga && x <= r[2] + folga && z >= r[1] - folga && z <= r[3] + folga) return true;
        return false;
    }

    // distância com sinal à união das águas (negativa dentro); grande se a fase não tem água
    private static float DistAgua(float x, float z)
    {
        float best = 999f;
        foreach (var r in Aguas)
        {
            float dx = Mathf.Max(r[0] - x, 0f, x - r[2]), dz = Mathf.Max(r[1] - z, 0f, z - r[3]);
            float s = (dx > 0f || dz > 0f) ? Mathf.Sqrt(dx * dx + dz * dz) : -Mathf.Min(Mathf.Min(x - r[0], r[2] - x), Mathf.Min(z - r[1], r[3] - z));
            if (s < best) best = s;
        }
        return best;
    }

    private static float S01(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

    // Altura do chão no ponto: mata plana junto da trilha, encostas que sobem com a distância,
    // plano em volta dos objetos do cenário, margens e fundo das águas, fossos e teto das câmeras das cutscenes.
    private static float Altura(float x, float z, float ox, float oz, float celula, List<Vector4[]> tetos)
    {
        float d0 = relevo[0], larg = relevo[1], A = relevo[2];
        float d = DistTrilha(x, z);
        float morros = Mathf.PerlinNoise(x * 0.03f + ox, z * 0.03f + oz);
        float ondas = Mathf.PerlinNoise(x * 0.12f + oz, z * 0.12f + ox);
        float h = ChaoMata + A * S01((d - d0) / larg) * (0.45f + 0.75f * morros) + 0.7f * ondas * S01((d - d0 + 1f) / 5f);

        foreach (var o in ocupados)
        {
            float dx = x - o.x, dz = z - o.y;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (dist < o.z + 3f) h = Mathf.Lerp(h, ChaoMata, 1f - S01((dist - o.z) / 3f));
        }

        float s = DistAgua(x, z);
        if (s < 0f) h = Mathf.Lerp(-0.52f, -2f, S01(-s / 2f));
        else if (s < 3f) h = Mathf.Lerp(-0.52f, h, S01(s / 3f));

        if (Dentro(Fossos, x, z, celula * 0.5f)) h = -3.4f;
        else if (Dentro(Trilha, x, z, 0f)) h = TopoTrilha;

        // teto: nada de morro na frente das câmeras das cutscenes
        foreach (var seg in tetos)
        {
            Vector4 p = seg[0], q = seg[1];   // x, y, z de cada ponta; w = raio (p) e folga (q)
            float vx = q.x - p.x, vz = q.z - p.z, len2 = vx * vx + vz * vz;
            float t = len2 > 0.001f ? Mathf.Clamp01(((x - p.x) * vx + (z - p.z) * vz) / len2) : 0f;
            float cx = p.x + vx * t - x, cz = p.z + vz * t - z;
            if (cx * cx + cz * cz < p.w * p.w) h = Mathf.Min(h, Mathf.Max(ChaoMata, Mathf.Lerp(p.y, q.y, t) - q.w));
        }
        return h;
    }

    // Segmentos que o relevo não pode atravessar: caminho da câmera entre tomadas e a linha de visão de cada tomada.
    private static List<Vector4[]> TetosCutscene()
    {
        var lista = new List<Vector4[]>();
        foreach (var cs in Object.FindObjectsByType<Cutscene>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cs.tomadas == null) continue;
            for (int k = 0; k < cs.tomadas.Length; k++)
            {
                var tk = cs.tomadas[k];
                Vector3 fimVisao = tk.olharPara;   // o teto nunca desce abaixo do chão da mata, então não cava em volta do alvo
                lista.Add(new[] { new Vector4(tk.posicao.x, tk.posicao.y, tk.posicao.z, 3.5f), new Vector4(fimVisao.x, fimVisao.y, fimVisao.z, 1.2f) });
                if (k > 0)
                {
                    var ant = cs.tomadas[k - 1].posicao;
                    lista.Add(new[] { new Vector4(ant.x, ant.y, ant.z, 5f), new Vector4(tk.posicao.x, tk.posicao.y, tk.posicao.z, 2f) });
                }
            }
        }
        return lista;
    }

    // Peso de cada camada no ponto, por regra: trilha, água, inclinação, altura, rochas, áreas de recuperação e manchas.
    private static void Pesos(float[] w, float x, float z, float h, float inc, List<Vector3> rochasPts, List<Vector3> recPts)
    {
        float d = DistTrilha(x, z), s = DistAgua(x, z);
        float hr = (h - ChaoMata) / Mathf.Max(1f, relevo[2]);
        System.Func<int, float, float> mancha = (k, limiar) => S01((Mathf.PerlinNoise(x * 0.08f + k * 37.1f, z * 0.08f + k * 11.7f) - limiar) / 0.12f);
        float fino = Mathf.PerlinNoise(x * 0.35f + 5.3f, z * 0.35f + 9.1f);
        float plano = 1f - S01(inc / 18f);
        float borda = 1f - S01((d - 2.2f) / 1.5f);       // acostamento da trilha
        float margem = s < 0f ? 1f : 1f - S01((s - 1.5f) / 1.5f);
        float perto = 1f - S01((d - 14f) / 6f);

        float rocha = 0f;
        foreach (var r in rochasPts) { float dx = x - r.x, dz = z - r.y; float dist = Mathf.Sqrt(dx * dx + dz * dz); if (dist < r.z + 1f) rocha = Mathf.Max(rocha, 1f - S01((dist - r.z + 1f) / 2f)); }
        float rec = 0f;
        foreach (var r in recPts) { float dx = x - r.x, dz = z - r.y; float dist = Mathf.Sqrt(dx * dx + dz * dz); if (dist < r.z + 2f) rec = Mathf.Max(rec, 1f - S01((dist - r.z) / 2f)); }

        w[0] = borda * (0.45f + 0.6f * mancha(0, 0.35f)) + 0.5f * mancha(10, 0.6f) * plano * perto;             // L01 terra seca
        w[1] = 0.25f + 0.9f * mancha(1, 0.48f) * (1f - S01(hr * 2f)) + 0.9f * (s >= 0f ? 1f - S01((s - 3f) / 4f) : 0f); // L02 terra úmida
        w[2] = 1.3f * (s > -0.6f ? 1f - S01((s - 1.2f) / 1.2f) : 0f) + 1.1f * mancha(2, 0.66f) * plano * perto;  // L03 lama
        w[3] = 2.0f * (s < 0.4f ? 1f : 0f) + 1.4f * margem * S01((s + 0.5f) / 0.5f);                            // L04 areia de margem
        w[4] = borda * (0.25f + 0.9f * fino) + 0.8f * mancha(4, 0.68f) * plano;                                  // L05 cascalho
        w[5] = 2.4f * S01((inc - 24f) / 10f) + 0.9f * S01((hr - 0.8f) / 0.3f) + 1.6f * rocha + 0.9f * mancha(5, 0.74f) * S01((d - 4f) / 4f); // L06 rocha exposta
        w[6] = 1.0f * mancha(6, 0.42f) * plano * S01((d - 1.5f) / 3f);                                          // L07 grama baixa
        w[7] = 0.2f + 0.6f * S01((d - 2.5f) / 4f);                                                              // L08 folhas caídas
        w[8] = 1.1f * mancha(8, 0.47f) * S01((inc - 6f) / 8f) * (1f - S01((inc - 32f) / 8f)) + 0.6f * rocha * mancha(18, 0.4f); // L09 solo com musgo
        w[9] = 2.6f * rec + 0.45f * mancha(9, 0.76f) * perto;                                                     // L10 solo em recuperação
        bool praca = false;   // retângulos largos (praças do posto e das arenas) viram terreiro de terra batida
        if (d <= 0f) foreach (var r in Trilha) if (x > r[0] && x < r[2] && z > r[1] && z < r[3] && Mathf.Min(r[2] - r[0], r[3] - r[1]) >= 20f) praca = true;
        if (d <= 0f && praca)
        {
            w[0] += 1.6f + 0.5f * fino; w[1] += 0.5f; w[4] += 0.4f * mancha(14, 0.5f);
            w[5] *= 0.2f; w[6] *= 0.1f; w[7] *= 0.3f; w[8] *= 0.15f; w[9] *= 0.5f;
        }
        else if (d <= 0f)   // o caminho: terra batida e cascalho, com um pouco de folha nas beiras
        {
            w[0] += 0.5f; w[4] += 1.9f + 0.6f * fino;
            w[5] *= 0.2f; w[6] *= 0.05f; w[7] *= 0.25f; w[8] *= 0.15f; w[9] *= 0.3f;
        }
        if (s < -0.3f) for (int k = 0; k < 10; k++) if (k != 3 && k != 2) w[k] *= 0.1f;                       // fundo da água: areia e lama
        for (int k = 0; k < 10; k++) w[k] = Mathf.Max(w[k], 0.0001f);
    }

    // Leva com o relevo os objetos colocados no chão da mata (a base deles foi ajustada para y = -0,05).
    private static void AcompanharRelevo()
    {
        if (terrenoAtual == null) return;
        var lista = new List<Transform>();
        foreach (Transform pasta in assets) foreach (Transform t in pasta) lista.Add(t);
        foreach (Transform t in cenario) lista.Add(t);
        foreach (Transform t in luzes) lista.Add(t);
        int movidos = 0;
        foreach (var t in lista)
        {
            if (t.GetComponent<Light>() == null && t.GetComponentInChildren<Renderer>() == null) continue;
            Vector3 c = t.GetComponent<Light>() != null ? t.position : Limites(t.gameObject).center;
            if (DistTrilha(c.x, c.z) < 0.5f || DistTrilha(t.position.x, t.position.z) < 0.5f) continue;
            if (DistAgua(c.x, c.z) < 0.5f) continue;   // o que está na água fica na altura da água
            float hm = Mathf.Min(AlturaEm(t.position), AlturaEm(c));
            if (t.GetComponent<Light>() == null)
            {
                // grandes: o ponto mais baixo sob a base (nada flutua); pequenos: a média, para não sumirem na encosta
                var b = Limites(t.gameObject);
                float ex = Mathf.Min(b.extents.x, 1.5f), ez = Mathf.Min(b.extents.z, 1.5f), media = hm;
                foreach (var o in new[] { new Vector3(ex, 0, 0), new Vector3(-ex, 0, 0), new Vector3(0, 0, ez), new Vector3(0, 0, -ez) })
                {
                    float y = AlturaEm(c + o);
                    hm = Mathf.Min(hm, y); media += y;
                }
                if (Mathf.Max(b.extents.x, b.extents.z) < 1.5f) hm = media / 5f;
            }
            float delta = hm - ChaoMata;
            if (Mathf.Abs(delta) < 0.02f) continue;
            t.position += Vector3.up * delta;
            movidos++;
        }
        // antena da serra (nível 1 e 4): sobe inteira com o chão sob o mastro
        var antena = GameObject.Find("Antena_Serra");
        var mastro = antena != null ? antena.transform.Find("Mastro") : null;
        if (mastro != null)
        {
            float delta = AlturaEm(mastro.position) - ChaoMata;
            if (Mathf.Abs(delta) > 0.02f) { antena.transform.position += Vector3.up * delta; movidos++; }
        }
        Debug.Log("Relevo: " + movidos + " objetos acompanharam o Terrain.");
    }

    private static float AlturaEm(Vector3 p) { return terrenoAtual.SampleHeight(p) + terrenoAtual.transform.position.y; }

    private static Material MaterialTerreno()
    {
        string path = PastaTerreno + "/Terreno.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.EnableKeyword("_NORMALMAP");
        m.EnableKeyword("_MASKMAP");
        EditorUtility.SetDirty(m);
        return m;
    }

    // ============================================================================================
    // Camadas e texturas. L07 e L08 usam texturas Poly Haven (CC0) já no projeto;
    // as outras oito são geradas aqui, sem emenda (ruído periódico), com mapa normal tirado do próprio relevo.

    private static TerrainLayer[] CamadasTerreno()
    {
        if (!AssetDatabase.IsValidFolder(PastaTerreno)) AssetDatabase.CreateFolder("Assets", "Terreno");
        if (!AssetDatabase.IsValidFolder(PastaTerreno + "/Texturas")) AssetDatabase.CreateFolder(PastaTerreno, "Texturas");
        string tx = PastaTerreno + "/Texturas/";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(tx + "L01_TerraSeca.png") == null) GerarTexturasTerreno();
        string ph = PH + "_Texturas/";
        // nome, difusa, normal, tamanho do ladrilho (m), suavidade
        var def = new[] {
            new object[] { tx + "L01_TerraSeca.png",       tx + "L01_TerraSeca_N.png",       3.5f, 0.05f },
            new object[] { tx + "L02_TerraUmida.png",      tx + "L02_TerraUmida_N.png",      3.5f, 0.22f },
            new object[] { tx + "L03_Lama.png",            tx + "L03_Lama_N.png",            4f,   0.5f  },
            new object[] { tx + "L04_AreiaMargem.png",     tx + "L04_AreiaMargem_N.png",     3f,   0.08f },
            new object[] { tx + "L05_Cascalho.png",        tx + "L05_Cascalho_N.png",        2.2f, 0.1f  },
            new object[] { tx + "L06_RochaExposta.png",    tx + "L06_RochaExposta_N.png",    5f,   0.15f },
            new object[] { ph + "leafy_grass/leafy_grass_diff_1k.jpg",             ph + "leafy_grass/leafy_grass_nor_gl_1k.jpg",             2.5f, 0.05f },
            new object[] { ph + "forrest_ground_01/forrest_ground_01_diff_1k.jpg", ph + "forrest_ground_01/forrest_ground_01_nor_gl_1k.jpg", 3f,   0.05f },
            new object[] { tx + "L09_SoloMusgo.png",       tx + "L09_SoloMusgo_N.png",       3f,   0.1f  },
            new object[] { tx + "L10_SoloRecuperacao.png", tx + "L10_SoloRecuperacao_N.png", 3f,   0.06f },
        };
        var camadas = new TerrainLayer[10];
        for (int k = 0; k < 10; k++)
        {
            string path = PastaTerreno + "/" + NomesCamadas[k] + ".terrainlayer";
            var tl = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (tl == null) { tl = new TerrainLayer(); AssetDatabase.CreateAsset(tl, path); }
            string nor = (string)def[k][1];
            var imp = AssetImporter.GetAtPath(nor) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; imp.SaveAndReimport(); }
            tl.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>((string)def[k][0]);
            tl.normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(nor);
            tl.tileSize = Vector2.one * (float)def[k][2];
            tl.smoothness = (float)def[k][3];
            tl.smoothnessSource = TerrainLayerSmoothnessSource.Constant;
            tl.metallic = 0f;
            tl.normalScale = 1f;
            tl.maskMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MapaMascara(k, (string)def[k][0]));
            tl.maskMapRemapMin = Vector4.zero;
            tl.maskMapRemapMax = new Vector4(0f, 1f, 1f, tl.smoothness);   // R metal, G oclusão, B altura, A suavidade
            tl.diffuseRemapMax = k == 6 ? new Vector4(0.78f, 0.9f, 0.72f, 1f) : Vector4.one;   // grama com o mesmo tom do chão antigo
            EditorUtility.SetDirty(tl);
            camadas[k] = tl;
        }
        AssetDatabase.SaveAssets();
        return camadas;
    }

    [MenuItem("Rastro do Dossel/Gerar texturas do Terrain")]
    public static void GerarTexturasTerreno()
    {
        if (!AssetDatabase.IsValidFolder(PastaTerreno)) AssetDatabase.CreateFolder("Assets", "Terreno");
        if (!AssetDatabase.IsValidFolder(PastaTerreno + "/Texturas")) AssetDatabase.CreateFolder(PastaTerreno, "Texturas");
        // cada receita devolve cor (rgb) e relevo (w) para u, v em [0, 1)
        Textura("L01_TerraSeca", 3f, (u, v) =>
        {
            float b = Fbm(u, v, 4, 5, 1), g = Valor(u, v, 128, 128, 2), tom = Fbm(u, v, 2, 3, 8);
            float uw = u + 0.05f * (Fbm(u, v, 4, 3, 5) - 0.5f), vw = v + 0.05f * (Fbm(u, v, 4, 3, 6) - 0.5f);
            float fenda = Mathf.Max(S01((Sulco(uw, vw, 5, 3) - 0.94f) / 0.035f), 0.6f * S01((Sulco(uw, vw, 12, 4) - 0.95f) / 0.03f));
            fenda *= 0.4f + 0.6f * Valor(u, v, 8, 8, 7);
            var c = Color.Lerp(new Color(0.47f, 0.36f, 0.24f), new Color(0.64f, 0.51f, 0.35f), 0.6f * b + 0.4f * tom) * (0.9f + 0.18f * g);
            c = Color.Lerp(c, new Color(0.27f, 0.20f, 0.13f), fenda * 0.75f);
            return new Vector4(c.r, c.g, c.b, 0.6f * b + 0.1f * g - 0.5f * fenda);
        });
        Textura("L02_TerraUmida", 2.5f, (u, v) =>
        {
            float b = Fbm(u, v, 4, 5, 11), g = Valor(u, v, 96, 96, 12);
            var wl = Worley(u, v, 22, 13);
            float pedra = wl.z < 0.35f ? 1f - S01((wl.x - 0.16f) / 0.06f) : 0f;
            var c = Color.Lerp(new Color(0.20f, 0.14f, 0.09f), new Color(0.34f, 0.25f, 0.16f), b) * (0.9f + 0.2f * g);
            c = Color.Lerp(c, new Color(0.42f, 0.37f, 0.31f), pedra);
            return new Vector4(c.r, c.g, c.b, 0.5f * b + 0.1f * g + 0.5f * pedra);
        });
        Textura("L03_Lama", 2f, (u, v) =>
        {
            float b = Fbm(u, v, 3, 5, 21), g = Fbm(u, v, 16, 3, 22);
            float poca = S01((b - 0.55f) / 0.1f);
            var c = Color.Lerp(new Color(0.27f, 0.20f, 0.13f), new Color(0.15f, 0.11f, 0.08f), poca) * (0.9f + 0.15f * g);
            return new Vector4(c.r, c.g, c.b, 0.6f * (1f - poca) * g + 0.2f * b);
        });
        Textura("L04_AreiaMargem", 2f, (u, v) =>
        {
            float g = Valor(u, v, 192, 192, 31), b = Fbm(u, v, 4, 4, 32);
            float onda = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (10f * v + 1.2f * Fbm(u, v, 3, 3, 33)));
            var c = Color.Lerp(new Color(0.64f, 0.56f, 0.41f), new Color(0.78f, 0.70f, 0.52f), b) * (0.9f + 0.18f * g) * (0.94f + 0.08f * onda);
            return new Vector4(c.r, c.g, c.b, 0.35f * onda + 0.2f * g + 0.3f * b);
        });
        Textura("L05_Cascalho", 4f, (u, v) =>
        {
            float uw = u + 0.025f * (Fbm(u, v, 8, 3, 45) - 0.5f), vw = v + 0.025f * (Fbm(u, v, 8, 3, 46) - 0.5f);
            var c = new Color(0.27f, 0.23f, 0.18f) * (0.85f + 0.3f * Valor(u, v, 64, 64, 47));
            float h = 0f;
            for (int camada = 0; camada < 2; camada++)   // pedras grandes por cima das pequenas
            {
                var wl = Worley(uw, vw, camada == 0 ? 26 : 13, 41 + camada * 10);
                float raio = (camada == 0 ? 0.40f : 0.36f) + 0.14f * Hash((int)(wl.z * 9973f), camada, 48);
                float dome = Mathf.Sqrt(Mathf.Max(0f, 1f - (wl.x / raio) * (wl.x / raio)));
                float pedra = S01(dome / 0.25f) * (camada == 1 && wl.z > 0.6f ? 0f : 1f);
                float tom = 0.34f + 0.3f * wl.z;
                var cp = new Color(tom * (1.02f + 0.1f * wl.z), tom, tom * (0.86f + 0.08f * wl.z)) * (0.75f + 0.35f * dome) * (0.9f + 0.2f * Valor(u, v, 96, 96, 42));
                c = Color.Lerp(c, cp, pedra);
                h = Mathf.Lerp(h, 0.3f + 0.7f * dome, pedra);
            }
            return new Vector4(c.r, c.g, c.b, h);
        });
        Textura("L06_RochaExposta", 3f, (u, v) =>
        {
            float b = Fbm(u, v, 3, 6, 51), g = Valor(u, v, 128, 128, 53);
            float uw = u + 0.06f * (Fbm(u, v, 3, 3, 56) - 0.5f), vw = v + 0.06f * (Fbm(u, v, 3, 3, 57) - 0.5f);
            float estrato = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (5f * vw + 1.5f * Fbm(u, v, 2, 3, 58)));
            float fenda = Mathf.Max(0.85f * S01((Sulco(uw, vw, 3, 55) - 0.968f) / 0.02f), 0.55f * S01((Sulco(uw, vw, 9, 59) - 0.955f) / 0.025f));
            float liquen = S01((Fbm(u, v, 8, 3, 54) - 0.64f) / 0.08f);
            var c = Color.Lerp(new Color(0.33f, 0.32f, 0.30f), new Color(0.56f, 0.54f, 0.50f), 0.65f * b + 0.35f * estrato) * (0.88f + 0.18f * g);
            c = Color.Lerp(c, new Color(0.38f, 0.43f, 0.29f), liquen * 0.5f);
            c = Color.Lerp(c, new Color(0.15f, 0.14f, 0.13f), fenda * 0.85f);
            return new Vector4(c.r, c.g, c.b, 0.6f * b + 0.25f * estrato + 0.1f * g - 0.6f * fenda);
        });
        Textura("L09_SoloMusgo", 2.5f, (u, v) =>
        {
            float b = Fbm(u, v, 4, 5, 61), g = Valor(u, v, 160, 160, 63), f = Valor(u, v, 64, 64, 64);
            float m = S01((Fbm(u, v, 6, 4, 62) + 0.25f * (f - 0.5f) - 0.5f) / 0.1f);
            var solo = Color.Lerp(new Color(0.20f, 0.15f, 0.10f), new Color(0.32f, 0.24f, 0.15f), b);
            var musgo = Color.Lerp(new Color(0.13f, 0.21f, 0.07f), new Color(0.25f, 0.35f, 0.12f), g) * (0.85f + 0.3f * f);
            var c = Color.Lerp(solo, musgo, m);
            return new Vector4(c.r, c.g, c.b, 0.3f * b + 0.4f * m + 0.3f * g * m);
        });
        Textura("L10_SoloRecuperacao", 3f, (u, v) =>
        {
            float b = Fbm(u, v, 4, 4, 71), g = Valor(u, v, 128, 128, 72);
            float palha = Mathf.Max(S01((Valor(u, v, 160, 20, 74) - 0.72f) / 0.06f), S01((Valor(u, v, 20, 160, 75) - 0.74f) / 0.06f));
            palha *= S01((Fbm(u, v, 4, 3, 76) - 0.35f) / 0.15f);
            var wl = Worley(u, v, 10, 73);
            float broto = wl.z < 0.55f ? 1f - S01((wl.x - 0.08f - 0.08f * wl.z) / 0.05f) : 0f;
            var c = new Color(0.21f, 0.15f, 0.10f) * (0.8f + 0.35f * b);
            c = Color.Lerp(c, new Color(0.55f, 0.46f, 0.29f) * (0.85f + 0.25f * g), palha * 0.8f);
            c = Color.Lerp(c, Color.Lerp(new Color(0.20f, 0.40f, 0.12f), new Color(0.36f, 0.60f, 0.20f), 1f - wl.x / 0.2f), broto);
            return new Vector4(c.r, c.g, c.b, 0.3f * b + 0.3f * palha + 0.6f * broto);
        });
        AssetDatabase.Refresh();
    }

    // Grava a difusa e o mapa normal de uma receita (512 x 512, repetível).
    private static void Textura(string nome, float forcaNormal, System.Func<float, float, Vector4> receita, string destino = null)
    {
        const int N = 512;
        var cor = new Color[N * N]; var alt = new float[N * N];
        for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                var r = receita((float)i / N, (float)j / N);
                cor[j * N + i] = new Color(Mathf.Clamp01(r.x), Mathf.Clamp01(r.y), Mathf.Clamp01(r.z), 1f);
                alt[j * N + i] = r.w;
            }
        var nor = new Color[N * N];
        for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                float dx = alt[j * N + (i + 1) % N] - alt[j * N + (i + N - 1) % N];
                float dy = alt[((j + 1) % N) * N + i] - alt[((j + N - 1) % N) * N + i];
                var n = new Vector3(-dx * forcaNormal, -dy * forcaNormal, 1f).normalized;
                nor[j * N + i] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }
        string pasta = (destino ?? PastaTerreno + "/Texturas") + "/";
        Gravar(pasta + nome + ".png", cor, N, false);
        Gravar(pasta + nome + "_N.png", nor, N, true);
        GravarMascara(pasta + nome + "_M.png", alt, N);
    }

    // Mapa de máscara do URP: R metal (0), G oclusão (1), B altura (height map normalizado), A suavidade (1, ajustada na camada).
    private static void GravarMascara(string path, float[] alt, int n)
    {
        float lo = float.MaxValue, hi = float.MinValue;
        foreach (var a in alt) { lo = Mathf.Min(lo, a); hi = Mathf.Max(hi, a); }
        var px = new Color[n * n];
        for (int i = 0; i < px.Length; i++) px[i] = new Color(0f, 1f, (alt[i] - lo) / Mathf.Max(0.0001f, hi - lo), 1f);
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false, true);
        t.SetPixels(px); t.Apply();
        System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.sRGBTexture = false; imp.wrapMode = TextureWrapMode.Repeat; imp.maxTextureSize = 512;
        imp.SaveAndReimport();
    }

    // Caminho do mapa de máscara de uma camada; nas texturas Poly Haven a altura sai da luminância da difusa.
    private static string MapaMascara(int k, string difusa)
    {
        string pasta = PastaTerreno + "/Texturas/";
        string path = pasta + NomesCamadas[k] + "_M.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null) return path;
        if (difusa.StartsWith(pasta)) return path;   // gerada junto com a textura
        var src = new Texture2D(2, 2);
        src.LoadImage(System.IO.File.ReadAllBytes(difusa));
        int n = 512; var alt = new float[n * n];
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                var c = src.GetPixelBilinear((i + 0.5f) / n, (j + 0.5f) / n);
                alt[j * n + i] = 0.3f * c.r + 0.59f * c.g + 0.11f * c.b;
            }
        Object.DestroyImmediate(src);
        GravarMascara(path, alt, n);
        return path;
    }

    private static void Gravar(string path, Color[] px, int n, bool normal)
    {
        var t = new Texture2D(n, n, TextureFormat.RGB24, false);
        t.SetPixels(px); t.Apply();
        System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        imp.wrapMode = TextureWrapMode.Repeat;
        imp.anisoLevel = 4;
        imp.maxTextureSize = 512;
        imp.SaveAndReimport();
    }

    // ---------- ruído periódico (repete nas bordas da textura) ----------
    private static float Hash(int x, int y, int s)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + s * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFFFF) / 16777215f;
        }
    }

    private static float Valor(float u, float v, int px, int py, int s)
    {
        float x = u * px, y = v * py;
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
        float fx = S01(x - xi), fy = S01(y - yi);
        int x0 = ((xi % px) + px) % px, x1 = (x0 + 1) % px, y0 = ((yi % py) + py) % py, y1 = (y0 + 1) % py;
        return Mathf.Lerp(Mathf.Lerp(Hash(x0, y0, s), Hash(x1, y0, s), fx), Mathf.Lerp(Hash(x0, y1, s), Hash(x1, y1, s), fx), fy);
    }

    private static float Fbm(float u, float v, int p, int oitavas, int s)
    {
        float soma = 0f, amp = 0.5f, norma = 0f;
        for (int k = 0; k < oitavas; k++) { soma += amp * Valor(u, v, p << k, p << k, s + k * 101); norma += amp; amp *= 0.5f; }
        return soma / norma;
    }

    // sulco: perto de 1 nas linhas finas onde o ruído cruza o meio (bom para fendas)
    private static float Sulco(float u, float v, int p, int s) { return 1f - Mathf.Abs(2f * Fbm(u, v, p, 3, s) - 1f); }

    // Worley periódico: x = distância ao ponto mais próximo, y = ao segundo, z = valor aleatório da célula mais próxima
    private static Vector3 Worley(float u, float v, int p, int s)
    {
        float x = u * p, y = v * p;
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
        float f1 = 9f, f2 = 9f, id = 0f;
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int cx = xi + dx, cy = yi + dy, mx = ((cx % p) + p) % p, my = ((cy % p) + p) % p;
                float ppx = cx + Hash(mx, my, s), ppy = cy + Hash(mx, my, s + 7);
                float d = Mathf.Sqrt((ppx - x) * (ppx - x) + (ppy - y) * (ppy - y));
                if (d < f1) { f2 = f1; f1 = d; id = Hash(mx, my, s + 13); }
                else if (d < f2) f2 = d;
            }
        return new Vector3(f1, f2, id);
    }
}
