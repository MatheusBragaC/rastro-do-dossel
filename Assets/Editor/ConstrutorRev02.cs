using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Itens pedidos na revisão 02 do trabalho (04/11) e nos tutoriais de 28/10 e 11/11:
// gramíneas e árvores pintadas pelo próprio Terrain, dez materiais com textura difusa e mapa normal
// aplicados aos objetos, e partículas criadas pela equipe (explosão e fumaça).
public static partial class ConstrutorNivel1
{
    private const string PastaRev = "Assets/Materials/Rev02";

    // ============================================================================================
    // Gramíneas (Paint Details) e árvores (Paint Trees) do Terrain

    /// Finalidade: pintar gramíneas e plantar árvores com as ferramentas do Terrain.
    /// Entradas: o Terrain, origem e tamanho da área e os segmentos de câmera das cutscenes. Retorno: void.
    /// Corpo: cria dois protótipos de detalhe (capim e capim florido) e lê a pintura para decidir a densidade
    /// (mais capim onde há grama, flores onde o solo está em recuperação, nada na trilha nem na água);
    /// cria os protótipos de árvore e planta em grade com sorteio nas encostas afastadas, longe das câmeras.
    private static void GramineasEArvores(Terrain ter, float x0, float z0, float sx, float sz, List<Vector4[]> tetos)
    {
        var td = ter.terrainData;
        // ---------- gramíneas ----------
        // touceiras em malha instanciada (o modo Grass do Terrain não desenha no URP deste projeto)
        var capim = new DetailPrototype
        {
            prototype = TouceiraPrefab("Graminea_Capim", false), usePrototypeMesh = true, useInstancing = true,
            renderMode = DetailRenderMode.VertexLit,
            minWidth = 0.6f, maxWidth = 1.1f, minHeight = 0.45f, maxHeight = 0.9f,
            noiseSpread = 0.3f, healthyColor = Color.white, dryColor = new Color(0.85f, 0.8f, 0.6f)
        };
        var florido = new DetailPrototype
        {
            prototype = TouceiraPrefab("Graminea_Florida", true), usePrototypeMesh = true, useInstancing = true,
            renderMode = DetailRenderMode.VertexLit,
            minWidth = 0.5f, maxWidth = 0.9f, minHeight = 0.4f, maxHeight = 0.7f,
            noiseSpread = 0.5f, healthyColor = Color.white, dryColor = new Color(0.9f, 0.85f, 0.7f)
        };
        td.detailPrototypes = new[] { capim, florido };
        int dres = td.alphamapResolution;
        td.SetDetailResolution(dres, 32);
        td.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);   // valores = touceiras por célula (o padrão do Unity 6 é cobertura 0-255)
        var alfa = td.GetAlphamaps(0, 0, dres, dres);
        var c0 = new int[dres, dres]; var c1 = new int[dres, dres];
        int totalCapim = 0, totalFlor = 0;
        for (int j = 0; j < dres; j++)
            for (int i = 0; i < dres; i++)
            {
                float u = (i + 0.5f) / dres, v = (j + 0.5f) / dres, wx = x0 + u * sx, wz = z0 + v * sz;
                float d = DistTrilha(wx, wz);
                if (d < 0.6f || d > 34f || DistAgua(wx, wz) < 0.6f || td.GetSteepness(u, v) > 32f) continue;
                float ruido = Mathf.PerlinNoise(wx * 0.15f + 3.1f, wz * 0.15f + 7.7f);
                float g = alfa[j, i, 6] * 7f + alfa[j, i, 7] * 2.5f * ruido + alfa[j, i, 1] * 1.5f;   // grama, folhas e terra úmida
                if (d < 2.5f) g += 2.5f * ruido;                                                        // beira do caminho
                c0[j, i] = Mathf.Clamp(Mathf.RoundToInt(g * 0.6f), 0, 5);
                c1[j, i] = Mathf.Clamp(Mathf.RoundToInt(alfa[j, i, 9] * 6f + alfa[j, i, 6] * 2f * (ruido > 0.62f ? 1f : 0f)), 0, 5);
                totalCapim += c0[j, i]; totalFlor += c1[j, i];
            }
        td.SetDetailLayer(0, 0, 0, c0);
        td.SetDetailLayer(0, 0, 1, c1);

        // ---------- árvores ----------
        var prefabs = PrefabsArvoresTerreno();
        var protos = new TreePrototype[prefabs.Length];
        for (int k = 0; k < prefabs.Length; k++) protos[k] = new TreePrototype { prefab = prefabs[k], bendFactor = 0.2f };
        td.treePrototypes = protos;
        var arvores = new List<TreeInstance>();
        var rnd = new System.Random(td.name.Length * 97 + (int)sx);
        const float passo = 8f;
        for (float z = z0 + 4f; z < z0 + sz - 4f; z += passo)
            for (float x = x0 + 4f; x < x0 + sx - 4f; x += passo)
            {
                float px = x + ((float)rnd.NextDouble() - 0.5f) * passo * 0.8f, pz = z + ((float)rnd.NextDouble() - 0.5f) * passo * 0.8f;
                double sorte = rnd.NextDouble();
                float d = DistTrilha(px, pz);
                if (d < 24f || DistAgua(px, pz) < 4f || sorte > 0.6) continue;
                float u = (px - x0) / sx, v = (pz - z0) / sz;
                if (td.GetSteepness(u, v) > 34f || Ocupado(px, pz, 2f) || PertoDeCamera(px, pz, tetos, 7f)) continue;
                float esc = 0.85f + 0.4f * (float)rnd.NextDouble();
                arvores.Add(new TreeInstance
                {
                    prototypeIndex = rnd.Next(protos.Length),
                    position = new Vector3(u, td.GetInterpolatedHeight(u, v) / td.size.y, v),
                    widthScale = esc, heightScale = esc * (0.9f + 0.2f * (float)rnd.NextDouble()),
                    rotation = (float)rnd.NextDouble() * Mathf.PI * 2f,
                    color = Color.white, lightmapColor = Color.white
                });
            }
        td.SetTreeInstances(arvores.ToArray(), true);
        foreach (var tex in td.alphamapTextures) EditorUtility.SetDirty(tex);
        EditorUtility.SetDirty(td);
        AssetDatabase.SaveAssets();

        ter.drawTreesAndFoliage = true;
        ter.detailObjectDistance = 55f;
        ter.detailObjectDensity = 1f;
        ter.treeDistance = 320f;
        ter.treeMaximumFullLODCount = 400;
        Debug.Log("Terrain: gramíneas " + totalCapim + " (capim) e " + totalFlor + " (florido); " + arvores.Count + " árvores pintadas.");
    }

    // ponto perto do caminho de alguma câmera de cutscene (as árvores não podem tapar a vista)
    private static bool PertoDeCamera(float x, float z, List<Vector4[]> tetos, float raio)
    {
        foreach (var seg in tetos)
        {
            Vector4 p = seg[0], q = seg[1];
            float vx = q.x - p.x, vz = q.z - p.z, len2 = vx * vx + vz * vz;
            float t = len2 > 0.001f ? Mathf.Clamp01(((x - p.x) * vx + (z - p.z) * vz) / len2) : 0f;
            float cx = p.x + vx * t - x, cz = p.z + vz * t - z;
            if (cx * cx + cz * cz < raio * raio) return true;
        }
        return false;
    }

    /// Finalidade: gerar a textura de uma touceira de capim (folhas finas com fundo transparente).
    /// Entradas: nome do arquivo e se leva flores. Retorno: Texture2D importada.
    /// Corpo: desenha folhas afinando até a ponta, com tom mais escuro na base, e pontos amarelos/brancos se florida.
    private static Texture2D TexturaGraminea(string nome, bool flores)
    {
        string pasta = PastaTerreno + "/Texturas/", path = pasta + nome + ".png";
        var existente = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existente != null) return existente;
        const int W = 128, H = 128;
        var px = new Color[W * H];
        var r = new System.Random(flores ? 5 : 3);
        for (int f = 0; f < 22; f++)
        {
            float bx = 10f + (float)r.NextDouble() * (W - 20), alt = H * (0.55f + 0.42f * (float)r.NextDouble());
            float inc = ((float)r.NextDouble() - 0.5f) * 0.7f, larg = 2.2f + 1.6f * (float)r.NextDouble();
            for (int y = 0; y < (int)alt; y++)
            {
                float t = y / alt, cx = bx + inc * y + 6f * t * t * inc * 4f, meia = larg * (1f - t);
                var cor = Color.Lerp(new Color(0.22f, 0.36f, 0.12f), new Color(0.62f, 0.78f, 0.38f), t);
                for (int x = Mathf.FloorToInt(cx - meia); x <= Mathf.CeilToInt(cx + meia); x++)
                    if (x >= 0 && x < W) px[y * W + x] = new Color(cor.r, cor.g, cor.b, 1f);
            }
            if (flores && f % 3 == 0)
            {
                float fx = bx + inc * alt, fy = alt - 2f; var cf = f % 2 == 0 ? new Color(1f, 0.86f, 0.25f) : new Color(0.95f, 0.95f, 0.9f);
                for (int y = -3; y <= 3; y++) for (int x = -3; x <= 3; x++)
                    if (x * x + y * y <= 9) { int xx = (int)fx + x, yy = (int)fy + y; if (xx >= 0 && xx < W && yy >= 0 && yy < H) px[yy * W + xx] = cf; }
            }
        }
        var t2 = new Texture2D(W, H, TextureFormat.RGBA32, false);
        t2.SetPixels(px); t2.Apply();
        System.IO.File.WriteAllBytes(path, t2.EncodeToPNG());
        Object.DestroyImmediate(t2);
        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 128;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    /// Finalidade: prefab de uma touceira de capim para o Paint Details.
    /// Entradas: nome e se é florida. Retorno: prefab com malha de três planos cruzados e material recortado.
    /// Corpo: cria (se faltar) a malha com a base no chão, o material URP Lit com recorte por alfa, dupla face e
    /// instanciamento, e o prefab com MeshFilter e MeshRenderer.
    private static GameObject TouceiraPrefab(string nome, bool flores)
    {
        string pasta = PastaTerreno + "/Gramineas";
        if (!AssetDatabase.IsValidFolder(pasta)) AssetDatabase.CreateFolder(PastaTerreno, "Gramineas");
        string pp = pasta + "/" + nome + ".prefab";
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>(pp);
        if (pf != null) return pf;
        var malha = AssetDatabase.LoadAssetAtPath<Mesh>(pasta + "/Touceira.asset");
        if (malha == null)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var n = new List<Vector3>(); var tri = new List<int>();
            for (int k = 0; k < 3; k++)
            {
                var r = Quaternion.Euler(0f, k * 60f, 0f); int b = v.Count;
                v.Add(r * new Vector3(-0.5f, 0f, 0f)); v.Add(r * new Vector3(0.5f, 0f, 0f)); v.Add(r * new Vector3(0.5f, 1f, 0f)); v.Add(r * new Vector3(-0.5f, 1f, 0f));
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
                for (int q = 0; q < 4; q++) n.Add(Vector3.up);   // normal para cima: a touceira recebe a luz como o chão
                tri.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
            }
            malha = new Mesh { name = "Touceira" };
            malha.SetVertices(v); malha.SetUVs(0, uv); malha.SetNormals(n); malha.SetTriangles(tri, 0); malha.RecalculateBounds();
            AssetDatabase.CreateAsset(malha, pasta + "/Touceira.asset");
        }
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetTexture("_BaseMap", TexturaGraminea(nome, flores));
        mat.SetFloat("_AlphaClip", 1f); mat.SetFloat("_Cutoff", 0.45f); mat.EnableKeyword("_ALPHATEST_ON");
        mat.SetFloat("_Cull", 0f); mat.SetFloat("_Smoothness", 0.05f);
        mat.SetColor("_BaseColor", new Color(0.5f, 0.58f, 0.45f));   // tom mais fechado: a touceira recebe luz de cima como o chão
        mat.enableInstancing = true;
        AssetDatabase.CreateAsset(mat, pasta + "/" + nome + ".mat");
        var go = new GameObject(nome);
        go.AddComponent<MeshFilter>().sharedMesh = malha;
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        pf = PrefabUtility.SaveAsPrefabAsset(go, pp);
        Object.DestroyImmediate(go);
        return pf;
    }

    /// Finalidade: preparar as árvores que o Terrain pode plantar.
    /// Entradas: nenhuma. Retorno: vetor de prefabs.
    /// Corpo: para cada espécie, cria (se faltar) um prefab com LODGroup na raiz e o modelo no tamanho certo como filho.
    private static GameObject[] PrefabsArvoresTerreno()
    {
        string pasta = PastaTerreno + "/Arvores";
        if (!AssetDatabase.IsValidFolder(pasta)) AssetDatabase.CreateFolder(PastaTerreno, "Arvores");
        var especies = new[] {
            new object[] { "Arvore_Palmeira_reta", "Assets/Terceiros/OpenGameArt/Yughues_Palmeiras_v3/palm_straight.FBX", 2.5f },
            new object[] { "Arvore_Palmeira_v2",   "Assets/Terceiros/OpenGameArt/Yughues_Palmeira_v2/palm_tree.FBX",     1.3f },
            new object[] { "Arvore_Embauba",       TGD + "Embauba/CecropiaTree01.DAE",                                   2.0f },
            new object[] { "Arvore_Sumauma",       TGD + "Sumauma/CiabaTree_01.dae",                                     2.1f },
            new object[] { "Arvore_Bananeira",     TGD + "Bananeira/Banana01.DAE",                                       0.75f },
        };
        var lista = new List<GameObject>();
        foreach (var e in especies)
        {
            string path = pasta + "/" + e[0] + ".prefab";
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (pf == null)
            {
                var raiz = new GameObject((string)e[0]);
                var modelo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>((string)e[1]));
                var rotOriginal = modelo.transform.rotation;
                PrefabUtility.UnpackPrefabInstance(modelo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                modelo.transform.SetParent(raiz.transform, false);
                modelo.transform.localRotation = rotOriginal;
                modelo.transform.localScale = Vector3.one * (float)e[2];
                foreach (var c in modelo.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                foreach (var r in modelo.GetComponentsInChildren<Renderer>(true))   // malhas de "efeito de cor" dos modelos 3TD
                    if (r != null) foreach (var m in r.sharedMaterials) if (m != null && m.name.Contains("ColorEffect")) { Object.DestroyImmediate(r.gameObject); break; }
                foreach (var l in modelo.GetComponentsInChildren<LODGroup>()) Object.DestroyImmediate(l);
                var b = Limites(raiz);
                modelo.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);
                var lod = raiz.AddComponent<LODGroup>();
                lod.SetLODs(new[] { new LOD(0.015f, raiz.GetComponentsInChildren<Renderer>()) });
                pf = PrefabUtility.SaveAsPrefabAsset(raiz, path);
                Object.DestroyImmediate(raiz);
            }
            lista.Add(pf);
        }
        return lista.ToArray();
    }

    // ============================================================================================
    // Dez materiais com textura difusa e mapa normal, aplicados aos objetos do jogo

    private static readonly string[][] MateriaisRev = {
        // material existente, textura gerada, ladrilho
        new[] { "Madeira",  "M01_MadeiraTabuas" },
        new[] { "Caixote",  "M02_MadeiraCaixote" },
        new[] { "Terminal", "M03_MetalPintado" },
        new[] { "Pedra",    "M04_PedraLisa" },
        new[] { "Rocha",    "M05_RochaBruta" },
        new[] { "Tronco",   "M06_CascaEscura" },
        new[] { "Galho",    "M07_CascaClara" },
        new[] { "Bloco",    "M08_Concreto" },
        new[] { "Fosso",    "M09_TerraBatida" },
        new[] { "Lenco",    "M10_Lona" },
    };

    /// Finalidade: dar textura difusa e mapa normal aos dez materiais usados nos objetos montados pela equipe.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: gera as texturas que faltarem (mesmo gerador do Terrain) e liga difusa, normal e cor base em cada material.
    [MenuItem("Rastro do Dossel/Materiais com textura (Rev02)")]
    public static void MateriaisComTextura()
    {
        if (!AssetDatabase.IsValidFolder(PastaRev)) AssetDatabase.CreateFolder("Assets/Materials", "Rev02");
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(PastaRev + "/M01_MadeiraTabuas.png") == null) GerarTexturasMateriais();
        foreach (var par in MateriaisRev)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + par[0] + ".mat");
            if (m == null) { Debug.LogWarning("Material não encontrado: " + par[0]); continue; }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(PastaRev + "/" + par[1] + ".png"));
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(PastaRev + "/" + par[1] + "_N.png"));
            m.SetFloat("_BumpScale", 1.2f);
            m.EnableKeyword("_NORMALMAP");
            var c = m.GetColor("_BaseColor");
            m.SetColor("_BaseColor", Color.Lerp(Color.white, new Color(c.r, c.g, c.b, 1f), 0.25f));   // a textura manda; o tom antigo fica de leve
            EditorUtility.SetDirty(m);
        }
        AssetDatabase.SaveAssets();
    }

    private static void GerarTexturasMateriais()
    {
        Textura("M01_MadeiraTabuas", 3f, (u, v) =>
        {
            int tabua = Mathf.FloorToInt(v * 6f); float fv = v * 6f - tabua, tom = Hash(tabua, 0, 101);
            float veio = Valor(u + 0.02f * tabua, v, 4, 96, 102 + tabua) * 0.6f + Valor(u, v, 2, 192, 103) * 0.4f;
            float junta = 1f - S01(Mathf.Min(fv, 1f - fv) / 0.04f);
            var c = Color.Lerp(new Color(0.36f, 0.24f, 0.14f), new Color(0.58f, 0.42f, 0.26f), 0.5f * tom + 0.5f * veio);
            c = Color.Lerp(c, new Color(0.16f, 0.1f, 0.06f), junta);
            return new Vector4(c.r, c.g, c.b, 0.5f * veio - 0.6f * junta);
        }, PastaRev);
        Textura("M02_MadeiraCaixote", 3f, (u, v) =>
        {
            int tabua = Mathf.FloorToInt(u * 4f); float fu = u * 4f - tabua;
            float veio = Valor(u, v + 0.03f * tabua, 96, 4, 111 + tabua);
            float junta = 1f - S01(Mathf.Min(fu, 1f - fu) / 0.05f);
            float prego = 0f; foreach (var py in new[] { 0.08f, 0.92f }) { float dx = fu - 0.5f, dy = v - py; prego = Mathf.Max(prego, 1f - S01((Mathf.Sqrt(dx * dx * 16f + dy * dy * 16f) - 0.04f) / 0.02f)); }
            var c = Color.Lerp(new Color(0.55f, 0.42f, 0.25f), new Color(0.74f, 0.6f, 0.38f), veio);
            c = Color.Lerp(c, new Color(0.25f, 0.18f, 0.1f), junta); c = Color.Lerp(c, new Color(0.3f, 0.3f, 0.32f), prego);
            return new Vector4(c.r, c.g, c.b, 0.4f * veio - 0.6f * junta + 0.5f * prego);
        }, PastaRev);
        Textura("M03_MetalPintado", 4f, (u, v) =>
        {
            float b = Fbm(u, v, 4, 5, 121), lasca = S01((Fbm(u, v, 6, 4, 122) - 0.62f) / 0.06f), risco = S01((Valor(u, v, 2, 64, 123) - 0.85f) / 0.05f);
            var tinta = new Color(0.22f, 0.32f, 0.34f) * (0.85f + 0.3f * b);
            var ferrugem = Color.Lerp(new Color(0.42f, 0.2f, 0.08f), new Color(0.62f, 0.34f, 0.14f), Valor(u, v, 64, 64, 124));
            var c = Color.Lerp(Color.Lerp(tinta, new Color(0.5f, 0.52f, 0.5f), risco * 0.6f), ferrugem, lasca);
            return new Vector4(c.r, c.g, c.b, 0.2f * b - 0.4f * lasca);
        }, PastaRev);
        Textura("M04_PedraLisa", 3f, (u, v) =>
        {
            var wl = Worley(u, v, 6, 131); float borda = 1f - S01((wl.y - wl.x) / 0.08f), g = Valor(u, v, 128, 128, 132);
            var c = Color.Lerp(new Color(0.42f, 0.42f, 0.4f), new Color(0.62f, 0.6f, 0.56f), wl.z) * (0.9f + 0.15f * g);
            c = Color.Lerp(c, new Color(0.2f, 0.2f, 0.19f), borda);
            return new Vector4(c.r, c.g, c.b, 0.6f * (1f - wl.x) - 0.6f * borda);
        }, PastaRev);
        Textura("M05_RochaBruta", 4f, (u, v) =>
        {
            float b = Fbm(u, v, 3, 6, 141), f = S01((Sulco(u, v, 4, 142) - 0.95f) / 0.03f), m = S01((Fbm(u, v, 6, 3, 143) - 0.6f) / 0.1f);
            var c = Color.Lerp(new Color(0.3f, 0.29f, 0.27f), new Color(0.52f, 0.5f, 0.46f), b);
            c = Color.Lerp(c, new Color(0.25f, 0.34f, 0.15f), m * 0.6f); c = Color.Lerp(c, new Color(0.12f, 0.12f, 0.11f), f);
            return new Vector4(c.r, c.g, c.b, 0.8f * b - 0.6f * f);
        }, PastaRev);
        Textura("M06_CascaEscura", 4f, (u, v) =>
        {
            float fibra = Sulco(u + 0.1f * Fbm(u, v, 2, 3, 151), v, 12, 152), b = Fbm(u, v, 4, 4, 153);
            var c = Color.Lerp(new Color(0.16f, 0.11f, 0.08f), new Color(0.34f, 0.25f, 0.17f), 0.6f * fibra + 0.4f * b);
            return new Vector4(c.r, c.g, c.b, fibra);
        }, PastaRev);
        Textura("M07_CascaClara", 3f, (u, v) =>
        {
            float fibra = Valor(u, v, 24, 3, 161), b = Fbm(u, v, 4, 4, 162), no = S01((Fbm(u, v, 8, 2, 163) - 0.7f) / 0.05f);
            var c = Color.Lerp(new Color(0.4f, 0.32f, 0.22f), new Color(0.62f, 0.53f, 0.4f), 0.6f * fibra + 0.4f * b);
            c = Color.Lerp(c, new Color(0.25f, 0.18f, 0.12f), no * 0.7f);
            return new Vector4(c.r, c.g, c.b, 0.6f * fibra - 0.5f * no);
        }, PastaRev);
        Textura("M08_Concreto", 2.5f, (u, v) =>
        {
            float b = Fbm(u, v, 4, 6, 171), poro = S01((Valor(u, v, 160, 160, 172) - 0.86f) / 0.05f), mancha = Fbm(u, v, 2, 3, 173);
            var c = Color.Lerp(new Color(0.48f, 0.48f, 0.46f), new Color(0.66f, 0.65f, 0.62f), b) * (0.9f + 0.15f * mancha);
            c = Color.Lerp(c, new Color(0.28f, 0.28f, 0.27f), poro);
            return new Vector4(c.r, c.g, c.b, 0.3f * b - 0.5f * poro);
        }, PastaRev);
        Textura("M09_TerraBatida", 3f, (u, v) =>
        {
            float b = Fbm(u, v, 4, 5, 181), g = Valor(u, v, 128, 128, 182);
            var wl = Worley(u, v, 20, 183); float pedra = wl.z < 0.3f ? 1f - S01((wl.x - 0.15f) / 0.05f) : 0f;
            var c = Color.Lerp(new Color(0.24f, 0.17f, 0.11f), new Color(0.4f, 0.3f, 0.2f), b) * (0.9f + 0.2f * g);
            c = Color.Lerp(c, new Color(0.45f, 0.42f, 0.38f), pedra);
            return new Vector4(c.r, c.g, c.b, 0.5f * b + 0.5f * pedra);
        }, PastaRev);
        Textura("M10_Lona", 5f, (u, v) =>
        {
            float trama = 0.5f + 0.25f * Mathf.Sin(u * Mathf.PI * 2f * 96f) + 0.25f * Mathf.Sin(v * Mathf.PI * 2f * 96f), b = Fbm(u, v, 3, 4, 191);
            var c = Color.Lerp(new Color(0.55f, 0.45f, 0.3f), new Color(0.75f, 0.66f, 0.48f), b) * (0.85f + 0.2f * trama);
            return new Vector4(c.r, c.g, c.b, 0.4f * trama + 0.2f * b);
        }, PastaRev);
        AssetDatabase.Refresh();
    }

    // ============================================================================================
    // Partículas criadas pela equipe: explosão (bola de fogo, faíscas e fumaça escura) e fumaça contínua

    private static Material MaterialParticula(string nome, bool aditivo)
    {
        if (!AssetDatabase.IsValidFolder(PastaRev)) AssetDatabase.CreateFolder("Assets/Materials", "Rev02");
        string path = PastaRev + "/" + nome + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        m.SetTexture("_BaseMap", TexturaFumaca());
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", aditivo ? 2f : 0f);   // 2 = aditivo, 0 = alfa
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)(aditivo ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // mancha de fumaça macia (alfa radial com ruído)
    private static Texture2D TexturaFumaca()
    {
        string path = PastaRev + "/Particula_Fumaca.png";
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (t != null) return t;
        const int N = 128; var px = new Color[N * N];
        for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                float u = (i + 0.5f) / N, v = (j + 0.5f) / N, dx = u - 0.5f, dy = v - 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) * 2f, n = Fbm(u, v, 4, 4, 201);
                float a = Mathf.Clamp01((1f - r) * 1.4f) * (0.55f + 0.6f * n);
                px[j * N + i] = new Color(1f, 1f, 1f, a * a);
            }
        var tx = new Texture2D(N, N, TextureFormat.RGBA32, false); tx.SetPixels(px); tx.Apply();
        System.IO.File.WriteAllBytes(path, tx.EncodeToPNG()); Object.DestroyImmediate(tx);
        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path); imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp; imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    /// Finalidade: criar a explosão (sem tocar ao iniciar; disparada por script). Entradas: pai, posição, nome, escala.
    /// Retorno: o ParticleSystem principal. Corpo: rajada de fogo aditivo, faíscas rápidas com gravidade e fumaça escura que sobe e some.
    private static ParticleSystem Explosao(Transform pai, Vector3 pos, string nome, float escala)
    {
        var raiz = new GameObject(nome); raiz.transform.SetParent(pai); raiz.transform.position = pos;
        var fogo = NovoSistema(raiz.transform, "Fogo", MaterialParticula("Explosao_Fogo", true), false);
        var m = fogo.main; m.duration = 0.6f; m.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f); m.startSpeed = new ParticleSystem.MinMaxCurve(2f * escala, 6f * escala);
        m.startSize = new ParticleSystem.MinMaxCurve(1.2f * escala, 2.6f * escala); m.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.35f, 0.05f));
        var e = fogo.emission; e.rateOverTime = 0f; e.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
        var sh = fogo.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.4f * escala;
        var col = fogo.colorOverLifetime; col.enabled = true; col.color = Gradiente(new Color(1f, 0.9f, 0.5f), new Color(0.6f, 0.1f, 0f), 1f, 0f);
        var tam = fogo.sizeOverLifetime; tam.enabled = true; tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.4f));

        var faisca = NovoSistema(raiz.transform, "Faiscas", MaterialParticula("Explosao_Fogo", true), false);
        m = faisca.main; m.duration = 0.4f; m.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f); m.startSpeed = new ParticleSystem.MinMaxCurve(6f * escala, 12f * escala);
        m.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f); m.gravityModifier = 1.2f; m.startColor = new Color(1f, 0.8f, 0.3f);
        e = faisca.emission; e.rateOverTime = 0f; e.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
        sh = faisca.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = 0.3f;

        var fum = NovoSistema(raiz.transform, "Fumaca", MaterialParticula("Fumaca_Escura", false), false);
        m = fum.main; m.duration = 1f; m.startDelay = 0.15f; m.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.5f); m.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2f);
        m.startSize = new ParticleSystem.MinMaxCurve(1.5f * escala, 3f * escala); m.startColor = new Color(0.16f, 0.15f, 0.14f, 0.85f); m.gravityModifier = -0.08f;
        e = fum.emission; e.rateOverTime = 0f; e.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
        sh = fum.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.6f * escala;
        col = fum.colorOverLifetime; col.enabled = true; col.color = Gradiente(new Color(0.25f, 0.22f, 0.2f), new Color(0.4f, 0.4f, 0.4f), 0.85f, 0f);
        tam = fum.sizeOverLifetime; tam.enabled = true; tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));
        return fogo;
    }

    /// Finalidade: fumaça contínua (chaminé, fogueira, gerador). Entradas: pai, posição, nome e se é clara.
    /// Retorno: o ParticleSystem. Corpo: puffs que sobem devagar, crescem, são levados de leve pelo vento e somem.
    private static ParticleSystem Fumaca(Transform pai, Vector3 pos, string nome, bool clara)
    {
        var fum = NovoSistema(pai, nome, MaterialParticula(clara ? "Fumaca_Clara" : "Fumaca_Escura", false), true);
        fum.transform.position = pos;
        var m = fum.main; m.loop = true; m.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f); m.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
        m.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.2f); m.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
        m.startColor = clara ? new Color(0.75f, 0.75f, 0.75f, 0.55f) : new Color(0.2f, 0.19f, 0.18f, 0.7f); m.maxParticles = 60;
        var e = fum.emission; e.rateOverTime = 6f;
        var sh = fum.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 8f; sh.radius = 0.15f;
        sh.rotation = new Vector3(-90f, 0f, 0f);
        var vel = fum.velocityOverLifetime; vel.enabled = true; vel.x = new ParticleSystem.MinMaxCurve(0.2f, 0.5f); vel.y = new ParticleSystem.MinMaxCurve(0f, 0f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var col = fum.colorOverLifetime; col.enabled = true; col.color = Gradiente(m.startColor.color, m.startColor.color, 1f, 0f);
        var tam = fum.sizeOverLifetime; tam.enabled = true; tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 2.5f));
        var rot = fum.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
        return fum;
    }

    private static ParticleSystem NovoSistema(Transform pai, string nome, Material mat, bool tocar)
    {
        var go = new GameObject(nome); go.transform.SetParent(pai, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var m = ps.main; m.playOnAwake = tocar; m.loop = false; m.simulationSpace = ParticleSystemSimulationSpace.World;
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        return ps;
    }

    private static Gradient Gradiente(Color a, Color b, float alfaIni, float alfaFim)
    {
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                  new[] { new GradientAlphaKey(alfaIni, 0f), new GradientAlphaKey(alfaIni, 0.3f), new GradientAlphaKey(alfaFim, 1f) });
        return g;
    }

    // ============================================================================================
    // Iluminação: pontual, de área, baked e mixed (tutorial de 11/11)

    public static bool assarIluminacao = true;   // desligar só para montar rápido durante testes

    /// Finalidade: aplicar os quatro tipos de iluminação pedidos e gravar o lightmap da fase.
    /// Entradas: a cena aberta. Retorno: void.
    /// Corpo: sol/lua em Mixed; lampiões (luz pontual) em Mixed e fogueiras em Baked; duas luzes de área (Baked)
    /// sobre a estação e o terminal; objetos fixos de cenário e o Terrain marcados como estáticos;
    /// sondas de luz ao longo da trilha para o Nilo e os NPCs receberem a luz assada; configura e assa o lightmap.
    private static void Iluminacao(UnityEngine.SceneManagement.Scene scene)
    {
        var sol = GameObject.Find("Directional Light");
        if (sol != null) sol.GetComponent<Light>().lightmapBakeType = LightmapBakeType.Mixed;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (l.type != LightType.Point || !l.gameObject.activeSelf) continue;   // luzes que acendem por evento ficam em tempo real
            if (l.name.StartsWith("Luz_C08")) l.lightmapBakeType = LightmapBakeType.Mixed;
            else if (l.name.StartsWith("Luz_Fogueira")) l.lightmapBakeType = LightmapBakeType.Baked;
        }
        // luzes de área (só existem assadas no URP)
        var raizArea = new GameObject("Luzes_Area").transform; raizArea.SetParent(luzes);
        var est = Object.FindAnyObjectByType<InteractableStation>(FindObjectsInactive.Include);
        if (est != null) LuzArea(raizArea, "Luz_Area_Estacao", est.transform.position + Vector3.up * 3.2f, new Vector2(4f, 3f), new Color(1f, 0.82f, 0.55f), 6f);
        var saida = Object.FindAnyObjectByType<LevelExit>(FindObjectsInactive.Include);
        if (saida != null) LuzArea(raizArea, "Luz_Area_Terminal", saida.transform.position + Vector3.up * 3f, new Vector2(3f, 2f), new Color(0.6f, 0.85f, 1f), 5f);

        // estáticos: Terrain e peças fixas com UV de lightmap
        var flags = StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;
        if (terrenoAtual != null) GameObjectUtility.SetStaticEditorFlags(terrenoAtual.gameObject, flags);
        int estaticos = 0;
        foreach (var raiz in new[] { constr, locais, tec, apoio, recup, rochas, cenario })
            foreach (var mf in raiz.GetComponentsInChildren<MeshFilter>(true))
            {
                var m = mf.sharedMesh; if (m == null || mf.GetComponentInParent<Animator>() != null) continue;
                bool primitiva = m.name == "Cube" || m.name == "Cylinder" || m.name == "Sphere" || m.name == "Quad";
                if (!primitiva && (m.uv2 == null || m.uv2.Length == 0)) continue;
                GameObjectUtility.SetStaticEditorFlags(mf.gameObject, flags);
                estaticos++;
            }
        // sondas de luz ao longo da trilha
        var sondas = new List<Vector3>();
        foreach (var r in Trilha)
            for (float x = r[0] + 1f; x < r[2]; x += 5f)
                for (float z = r[1] + 1f; z < r[3]; z += 5f)
                { sondas.Add(new Vector3(x, 0.6f, z)); sondas.Add(new Vector3(x, 2.6f, z)); }
        var lpg = new GameObject("Sondas_de_Luz").AddComponent<LightProbeGroup>();
        lpg.transform.SetParent(luzes); lpg.probePositions = sondas.ToArray();

        // configuração do lightmap (Baked Indirect: luz indireta assada + sombras em tempo real)
        string path = "Assets/Settings/Iluminacao_" + scene.name + ".lighting";
        var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
        if (ls == null) { ls = new LightingSettings(); AssetDatabase.CreateAsset(ls, path); }
        ls.bakedGI = true; ls.realtimeGI = false;
        ls.mixedBakeMode = MixedLightingMode.IndirectOnly;
        ls.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;   // a GPU do lab pode não ter OpenCL
        ls.lightmapResolution = 1f; ls.lightmapMaxSize = 1024; ls.lightmapPadding = 2;
        ls.directSampleCount = 8; ls.indirectSampleCount = 32; ls.environmentSampleCount = 16; ls.maxBounces = 1;
        ls.ao = false; ls.lightmapCompression = LightmapCompression.NormalQuality;
        EditorUtility.SetDirty(ls);
        Lightmapping.lightingSettings = ls;
        if (assarIluminacao)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Lightmapping.Bake();
            Debug.Log("Iluminação de " + scene.name + ": " + estaticos + " peças estáticas, " + sondas.Count + " sondas, lightmap em " + (sw.ElapsedMilliseconds / 1000f).ToString("0.0") + " s.");
        }
    }

    // Pedra que afunda (nível 2): a mesma rocha, escurecida e esverdeada de musgo molhado — a pista para o jogador.
    private static Material MaterialPedraAfunda()
    {
        string path = "Assets/Materials/PedraAfunda.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Rocha.mat"));
        m.SetColor("_BaseColor", new Color(0.28f, 0.36f, 0.24f));
        m.SetFloat("_Smoothness", 0.55f);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    private static Light LuzArea(Transform pai, string nome, Vector3 pos, Vector2 tam, Color cor, float intensidade)
    {
        var g = new GameObject(nome); g.transform.SetParent(pai); g.transform.position = pos; g.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var l = g.AddComponent<Light>(); l.type = LightType.Rectangle; l.areaSize = tam; l.color = cor; l.intensity = intensidade; l.range = 12f;
        l.lightmapBakeType = LightmapBakeType.Baked;
        return l;
    }
}
