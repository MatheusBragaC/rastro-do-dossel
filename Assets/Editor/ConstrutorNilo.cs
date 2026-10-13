using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Reconstrói o visual do Nilo no prefab em estilo de blocos com textura pixel-art
// (Assets/UI/nilo_skin.png, gerada por Ferramentas/gerar_skin_nilo.py).
// Mantém os pivôs animados (Pose, PernaE, PernaD, BracoE, BracoD) e os acessórios pequenos.
// Menu: Rastro do Dossel > Reconstruir Nilo (blocos).
public static class ConstrutorNilo
{
    private const string PrefabPath = "Assets/Prefabs/Nilo.prefab";
    private const string SkinPath = "Assets/UI/nilo_skin.png";
    private const string MatPath = "Assets/Materials/Nilo_Skin.mat";
    private const float Atlas = 128f;

    private static Dictionary<string, Rect> rects;
    private static Material skin;

    [MenuItem("Rastro do Dossel/Reconstruir Nilo (blocos)")]
    public static void Reconstruir()
    {
        ImportarTextura();
        CarregarRects();
        skin = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (skin == null)
        {
            skin = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(skin, MatPath);
        }
        skin.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(SkinPath));
        skin.SetFloat("_Smoothness", 0.08f);
        skin.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(skin);

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        var pose = root.transform.Find("Visual/Pose");
        var manter = new HashSet<string> { "PernaE", "PernaD", "BracoE", "BracoD", "Cantil", "Lenco_No", "Radio", "Radio_Antena", "Radio_LED", "Saco_Dormir" };
        for (int i = pose.childCount - 1; i >= 0; i--)
        {
            var c = pose.GetChild(i);
            if (!manter.Contains(c.name)) Object.DestroyImmediate(c.gameObject);
        }
        foreach (var membro in new[] { "PernaE", "PernaD", "BracoE", "BracoD" })
        {
            var t = pose.Find(membro);
            for (int i = t.childCount - 1; i >= 0; i--) Object.DestroyImmediate(t.GetChild(i).gameObject);
        }

        // cabeça e chapéu
        Bloco(pose, "Cabeca", new Vector3(0f, 1.74f, 0f), new Vector3(0.44f, 0.44f, 0.44f),
            "cabeca_frente", "cabeca_tras", "cabeca_esquerda", "cabeca_direita", "cabeca_topo", "cabeca_baixo");
        Bloco(pose, "Chapeu_Aba", new Vector3(0f, 1.965f, 0f), new Vector3(0.64f, 0.03f, 0.64f),
            "aba_baixo", "aba_baixo", "aba_baixo", "aba_baixo", "aba_topo", "aba_baixo");
        Bloco(pose, "Chapeu_Copa", new Vector3(0f, 2.06f, 0f), new Vector3(0.42f, 0.17f, 0.42f),
            "chapeu_lado", "chapeu_lado", "chapeu_lado", "chapeu_lado", "chapeu_topo", "chapeu_lado");
        // tronco com colete, cinto e lenço pintados; mochila nas costas
        Bloco(pose, "Tronco", new Vector3(0f, 1.22f, 0f), new Vector3(0.5f, 0.62f, 0.28f),
            "tronco_frente", "tronco_tras", "tronco_lado", "tronco_lado", "tronco_topo", "tronco_topo");
        Bloco(pose, "Mochila", new Vector3(0f, 1.2f, -0.26f), new Vector3(0.4f, 0.46f, 0.22f),
            "mochila_lado", "mochila_lado", "mochila_lado", "mochila_lado", "mochila_topo", "mochila_topo");
        // membros: um bloco por pivô (mão e bota pintadas)
        foreach (var perna in new[] { "PernaE", "PernaD" })
            Bloco(pose.Find(perna), "Perna", new Vector3(0f, -0.45f, 0f), new Vector3(0.2f, 0.88f, 0.22f),
                "perna_lado", "perna_lado", "perna_lado", "perna_lado", "perna_topo", "perna_baixo");
        foreach (var braco in new[] { "BracoE", "BracoD" })
            Bloco(pose.Find(braco), "Braco", new Vector3(0f, -0.25f, 0f), new Vector3(0.18f, 0.62f, 0.18f),
                "braco_lado", "braco_lado", "braco_lado", "braco_lado", "braco_topo", "braco_baixo");

        // acessórios reposicionados para fora dos blocos novos
        Mover(pose, "Saco_Dormir", new Vector3(0f, 1.5f, -0.26f));
        Mover(pose, "Radio", new Vector3(-0.27f, 1.22f, -0.26f));
        Mover(pose, "Radio_Antena", new Vector3(-0.27f, 1.56f, -0.29f));
        Mover(pose, "Radio_LED", new Vector3(-0.27f, 1.3f, -0.19f));
        Mover(pose, "Lenco_No", new Vector3(0f, 1.5f, 0.16f));
        Mover(pose, "Cantil", new Vector3(0.3f, 0.86f, -0.04f));

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        Debug.Log("Nilo reconstruído em blocos.");
    }

    private static void Mover(Transform pose, string nome, Vector3 pos)
    {
        var t = pose.Find(nome);
        if (t != null) t.localPosition = pos;
    }

    // Bloco com uma face do atlas por lado. Ordem das faces: frente (+z), trás, esquerda (-x), direita, topo, base.
    private static GameObject Bloco(Transform pai, string nome, Vector3 centro, Vector3 tam, params string[] faces)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        go.transform.localPosition = centro;
        go.AddComponent<MeshFilter>().sharedMesh = Malha(pai.name + "_" + nome, tam, faces);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = skin;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        return go;
    }

    private static Mesh Malha(string nome, Vector3 tam, string[] faces)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Modelos")) AssetDatabase.CreateFolder("Assets", "Modelos");
        string path = "Assets/Modelos/Nilo_" + nome + ".asset";
        Vector3[] normais = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up, Vector3.down };
        var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
        for (int f = 0; f < 6; f++)
        {
            Vector3 n = normais[f];
            Vector3 texUp = (f >= 4) ? Vector3.forward : Vector3.up;   // topo e base: o "alto" da textura aponta para a frente
            Vector3 right = Vector3.Cross(n, texUp);                   // direita de quem olha a face de frente
            Vector3 half = tam * 0.5f;
            Vector3 cN = Vector3.Scale(n, half), cR = Vector3.Scale(right, half), cU = Vector3.Scale(texUp, half);
            Rect r = rects[faces[f]];
            float u0 = r.x / Atlas, u1 = (r.x + r.width) / Atlas;
            float v1 = 1f - r.y / Atlas, v0 = 1f - (r.y + r.height) / Atlas;
            int b = verts.Count;
            verts.Add(cN - cR - cU); uvs.Add(new Vector2(u0, v0));
            verts.Add(cN + cR - cU); uvs.Add(new Vector2(u1, v0));
            verts.Add(cN + cR + cU); uvs.Add(new Vector2(u1, v1));
            verts.Add(cN - cR + cU); uvs.Add(new Vector2(u0, v1));
            for (int k = 0; k < 4; k++) norms.Add(n);
            tris.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
        }
        var m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        bool novo = m == null;
        if (novo) m = new Mesh();
        m.Clear();
        m.name = "Nilo_" + nome;
        m.SetVertices(verts); m.SetNormals(norms); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
        m.RecalculateBounds(); m.RecalculateTangents();
        if (novo) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
        return m;
    }

    private static void ImportarTextura()
    {
        var imp = AssetImporter.GetAtPath(SkinPath) as TextureImporter;
        if (imp == null) { Debug.LogError("Textura não encontrada: " + SkinPath); return; }
        imp.textureType = TextureImporterType.Default;
        imp.filterMode = FilterMode.Point;
        imp.mipmapEnabled = false;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.alphaIsTransparency = true;
        imp.maxTextureSize = 128;
        imp.SaveAndReimport();
    }

    private static void CarregarRects()
    {
        rects = new Dictionary<string, Rect>();
        string json = System.IO.File.ReadAllText("Assets/UI/nilo_skin.json");
        foreach (Match m in Regex.Matches(json, "\"(\\w+)\":\\s*\\[(\\d+),\\s*(\\d+),\\s*(\\d+),\\s*(\\d+)\\]"))
            rects[m.Groups[1].Value] = new Rect(int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value), int.Parse(m.Groups[4].Value), int.Parse(m.Groups[5].Value));
    }
}
