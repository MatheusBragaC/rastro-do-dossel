using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Cena curta: a câmera passa por pontos-chave enquanto faixas pretas e legendas contam a história.
// Trava o jogo enquanto toca; E, Espaço ou Esc pulam. Cada cena toca uma vez por sessão: reiniciar
// a fase depois de falhar não repete, mas voltar ao menu e jogar de novo sim.
public class Cutscene : MonoBehaviour
{
    [System.Serializable]
    public class Tomada
    {
        public Vector3 posicao;
        public Vector3 olharPara;
        public float duracao = 3f;   // tempo para chegar a esta tomada vindo da anterior (na primeira, tempo parado)
    }

    [System.Serializable]
    public class Fala
    {
        public float inicio;
        public float fim;
        [TextArea] public string texto;
        public bool titulo;          // título grande no centro em vez de legenda
    }

    public string id = "cena";
    public bool tocarAoIniciar;
    public Tomada[] tomadas;
    public Fala[] falas;
    public float retornoDuracao = 1.2f;   // volta suave para a câmera do jogo
    public float neblinaFim;              // 0 = mantém; senão afasta a névoa durante a cena (vista longa)
    public UnityEvent aoComecar = new UnityEvent();
    public UnityEvent aoTerminar = new UnityEvent();

    [Header("Interface")]
    public GameObject raizUI;
    public RectTransform faixaTopo;
    public RectTransform faixaBase;
    public Text legenda;
    public Text titulo;
    public float alturaFaixa = 130f;

    private static readonly HashSet<string> jaTocadas = new HashSet<string>();

    private bool tocando, encerrando;
    private float t, fimCamera, total;
    private Camera cam;
    private FollowCamera seguir;
    private System.Action depois;
    private Vector3 ultimaPos;
    private Quaternion ultimaRot;
    private float neblinaInicioOriginal, neblinaFimOriginal;

    /// Finalidade: esquecer as cenas já vistas (nova sessão: menu ou jogar de novo). Entradas: nenhuma. Retorno: void.
    public static void LimparHistorico() { jaTocadas.Clear(); }

    /// Finalidade: tocar a cena de abertura assim que a fase começa. Entradas: nenhuma. Retorno: void.
    void Start()
    {
        if (tocarAoIniciar) Tocar(null);
    }

    /// Finalidade: versão sem parâmetros para eventos do Inspector (gatilho de entrada). Retorno: void.
    public void TocarCena() { Tocar(null); }

    /// Finalidade: iniciar a cena.
    /// Entrada: aoFim, ação chamada quando a cena termina ou é pulada (pode ser nula). Retorno: bool indicando se tocou.
    /// Corpo: se a cena já foi vista nesta sessão, só dispara os eventos e a ação final; senão desliga a
    /// câmera do jogo, avisa o GameManager (trava o controle e esconde o HUD), mostra a interface e
    /// posiciona a câmera na primeira tomada.
    public bool Tocar(System.Action aoFim)
    {
        var gm = GameManager.Instance;
        if (tocando || gm == null || gm.CurrentState != GameManager.State.Playing) return false;
        if (jaTocadas.Contains(id) || tomadas == null || tomadas.Length == 0)
        {
            aoComecar.Invoke();
            aoTerminar.Invoke();
            if (aoFim != null) aoFim();
            return false;
        }
        jaTocadas.Add(id);
        cam = Camera.main;
        seguir = cam != null ? cam.GetComponent<FollowCamera>() : null;
        if (seguir != null) seguir.enabled = false;
        depois = aoFim;
        tocando = true; encerrando = false; t = 0f;
        fimCamera = 0f;
        foreach (var k in tomadas) fimCamera += k.duracao;
        total = fimCamera + retornoDuracao;
        var ultima = tomadas[tomadas.Length - 1];
        ultimaPos = ultima.posicao;
        ultimaRot = Quaternion.LookRotation(ultima.olharPara - ultima.posicao);
        if (neblinaFim > 0f)
        {
            neblinaInicioOriginal = RenderSettings.fogStartDistance; neblinaFimOriginal = RenderSettings.fogEndDistance;
            RenderSettings.fogStartDistance = neblinaFim * 0.4f; RenderSettings.fogEndDistance = neblinaFim;
        }
        gm.IniciarCutscene();
        raizUI.SetActive(true);
        legenda.text = ""; titulo.text = "";
        aoComecar.Invoke();
        Aplicar(0f);
        return true;
    }

    /// Finalidade: avançar o tempo da cena, faixas e legendas; tratar o pulo.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: no quadro seguinte ao fim devolve o jogo (assim a tecla de pular não vira salto ou pausa);
    /// E, Espaço ou Esc encerram depois de meio segundo; as faixas entram e saem em 0,5 s e cada fala
    /// aparece e some com transparência.
    void Update()
    {
        if (encerrando) { Devolver(); return; }
        if (!tocando) return;
        var kb = Keyboard.current;
        if (t > 0.5f && kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
        {
            Encerrar();
            return;
        }
        t += Time.deltaTime;
        if (t >= total) { Encerrar(); return; }

        float faixa = alturaFaixa * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(t, total - t) / 0.5f));
        faixaTopo.sizeDelta = new Vector2(0f, faixa);
        faixaBase.sizeDelta = new Vector2(0f, faixa);

        string textoLegenda = "", textoTitulo = "";
        float alfaLegenda = 0f, alfaTitulo = 0f;
        foreach (var f in falas)
        {
            if (t < f.inicio || t >= f.fim) continue;
            float a = Mathf.Clamp01(Mathf.Min(t - f.inicio, f.fim - t) / (f.titulo ? 0.6f : 0.25f));
            if (f.titulo) { textoTitulo = f.texto; alfaTitulo = a; }
            else { textoLegenda = f.texto; alfaLegenda = a; }
        }
        legenda.text = textoLegenda; legenda.color = new Color(1f, 1f, 1f, alfaLegenda);
        titulo.text = textoTitulo; titulo.color = new Color(1f, 0.95f, 0.8f, alfaTitulo);
    }

    /// Finalidade: mover a câmera depois que todos os objetos se moveram. Entradas: nenhuma. Retorno: void.
    void LateUpdate()
    {
        if (tocando && !encerrando) Aplicar(t);
    }

    /// Finalidade: posicionar a câmera no instante dado.
    /// Entrada: tempo, segundos desde o início da cena. Retorno: void.
    /// Corpo: encontra o trecho entre duas tomadas, interpola posição e ponto de mira com suavização
    /// nas pontas; no fim, faz a transição até a pose que a câmera do jogo teria atrás do Nilo.
    private void Aplicar(float tempo)
    {
        if (cam == null) return;
        if (tempo <= fimCamera)
        {
            float acumulado = 0f;
            for (int i = 0; i < tomadas.Length; i++)
            {
                var k = tomadas[i];
                if (tempo <= acumulado + k.duracao || i == tomadas.Length - 1)
                {
                    float u = k.duracao > 0f ? Mathf.Clamp01((tempo - acumulado) / k.duracao) : 1f;
                    u = u * u * (3f - 2f * u);
                    var de = i == 0 ? k : tomadas[i - 1];
                    Vector3 p = Vector3.Lerp(de.posicao, k.posicao, u);
                    Vector3 alvo = Vector3.Lerp(de.olharPara, k.olharPara, u);
                    cam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(alvo - p));
                    return;
                }
                acumulado += k.duracao;
            }
        }
        else if (seguir != null)
        {
            Vector3 pos; Quaternion rot;
            seguir.CalcularPose(out pos, out rot);
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((tempo - fimCamera) / Mathf.Max(retornoDuracao, 0.01f)));
            cam.transform.SetPositionAndRotation(Vector3.Lerp(ultimaPos, pos, u), Quaternion.Slerp(ultimaRot, rot, u));
        }
    }

    /// Finalidade: terminar a cena (fim natural ou pulo). Entradas: nenhuma. Retorno: void.
    /// Corpo: esconde a interface, devolve a névoa e a câmera do jogo; o controle volta no quadro seguinte.
    private void Encerrar()
    {
        tocando = false;
        encerrando = true;
        raizUI.SetActive(false);
        if (neblinaFim > 0f) { RenderSettings.fogStartDistance = neblinaInicioOriginal; RenderSettings.fogEndDistance = neblinaFimOriginal; }
        if (seguir != null) seguir.enabled = true;
    }

    /// Finalidade: devolver o jogo ao jogador. Entradas: nenhuma. Retorno: void.
    /// Corpo: avisa o GameManager, dispara o evento de fim e a ação final (ex.: tela de conclusão).
    private void Devolver()
    {
        encerrando = false;
        var gm = GameManager.Instance;
        if (gm != null) gm.TerminarCutscene();
        aoTerminar.Invoke();
        if (depois != null) { var acao = depois; depois = null; acao(); }
    }
}
