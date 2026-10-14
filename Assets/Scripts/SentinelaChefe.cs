using UnityEngine;

// Sentinela da Vigília: máquina de patrulha automática que guarda o posto de observação (chefe do nível 1).
// Alterna dois ataques: carga em linha reta (que termina atordoada, com as baterias expostas) e giro com
// braço de varredura (desvie ou salte). O ciclo completo dura cerca de 10 segundos, e ela nunca fica
// parada esperando: se o Nilo não tirar a bateria a tempo, volta a atacar. Três baterias a desligam.
[RequireComponent(typeof(Rigidbody))]
public class SentinelaChefe : MonoBehaviour
{
    public enum Estado { Dormindo, Mirando, Avancando, Girando, Atordoada, Desligada }

    [Header("Arena (limites em que a sentinela se move)")]
    public Vector3 arenaMin;
    public Vector3 arenaMax;
    public float raioAcordar = 20f;      // distância do Nilo que acorda a sentinela

    [Header("Ciclo")]
    public float aimTime = 1.3f;
    public float[] chargeSpeeds = { 8f, 10f, 12f };   // uma velocidade por bateria retirada
    public float stunTime = 3.5f;
    public float turnSpeed = 110f;
    public float spinTime = 2.4f;
    public float spinSpeed = 150f;       // graus por segundo

    [Header("Referências")]
    public GameObject hazardZone;        // zona de dano da carga (frente da máquina)
    public GameObject bracoGiro;         // braço de varredura com Hazard, ligado só durante o giro
    public GameObject bateriaInteragivel;
    public GameObject[] celulas;
    public Light lampada;
    public Color corMira = new Color(1f, 0.15f, 0.1f);
    public Color corAtordoada = new Color(1f, 0.8f, 0.2f);
    public Color corGiro = new Color(1f, 0.45f, 0.05f);
    public AudioSource somAviso;
    public AudioSource somBatida;
    public AudioSource somDesligar;
    public GameObject[] ativarAoDesligar;
    public GameObject[] desativarAoDesligar;

    [Header("Textos")]
    [TextArea] public string mensagemBateria = "Bateria retirada ({0}/{1}). Ela vai atacar mais rápido!";
    [TextArea] public string mensagemFim = "MARA (rádio): Sentinela desligada. O posto de observação está livre. Siga para o terminal.";
    public string objetivoDepois = "usar o terminal de saída";

    public Estado EstadoAtual { get; private set; }
    public int BateriasRestantes { get; private set; }

    private Rigidbody bodyRb;
    private float timer;
    private float girado;
    private Vector3 chargeDir;
    private float lampBase;
    private bool proximoGiro;   // alterna: carga, giro, carga, giro...

    /// Finalidade: preparar o corpo cinemático e o estado inicial. Entradas: nenhuma. Retorno: void.
    /// Corpo: configura o Rigidbody, conta as baterias pelas células, apaga zonas de dano e bateria
    /// interagível e deixa a sentinela dormindo até o Nilo se aproximar.
    void Awake()
    {
        bodyRb = GetComponent<Rigidbody>();
        bodyRb.isKinematic = true;
        bodyRb.interpolation = RigidbodyInterpolation.Interpolate;
        BateriasRestantes = celulas != null && celulas.Length > 0 ? celulas.Length : 3;
        if (hazardZone != null) hazardZone.SetActive(false);
        if (bracoGiro != null) bracoGiro.SetActive(false);
        if (bateriaInteragivel != null) bateriaInteragivel.SetActive(false);
        if (lampada != null) { lampBase = lampada.intensity; lampada.enabled = false; }
        EstadoAtual = Estado.Dormindo;
    }

    /// Finalidade: ativar a sentinela (gatilho de entrada da arena ou aproximação do Nilo).
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: só sai do estado Dormindo; acende a lâmpada e começa a mirar.
    public void Acordar()
    {
        if (EstadoAtual != Estado.Dormindo) return;
        Mirar();
    }

    /// Finalidade: executar o ciclo de mira, carga, giro e atordoamento.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: dormindo, acorda se o Nilo chegar perto; Mirando gira para o Nilo e pisca, e depois ataca
    /// (carga ou giro, alternados); Avancando move em linha reta até bater no limite da arena;
    /// Girando roda no lugar com o braço de varredura; Atordoada espera e volta a mirar.
    /// Para na pausa porque usa o tempo de física.
    void FixedUpdate()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.player == null || gm.CurrentState != GameManager.State.Playing) return;
        float dt = Time.fixedDeltaTime;

        switch (EstadoAtual)
        {
            case Estado.Dormindo:
                if ((gm.player.transform.position - bodyRb.position).sqrMagnitude < raioAcordar * raioAcordar) Acordar();
                break;

            case Estado.Mirando:
                Vector3 toPlayer = gm.player.transform.position - bodyRb.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f)
                {
                    var goal = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
                    bodyRb.MoveRotation(Quaternion.RotateTowards(bodyRb.rotation, goal, turnSpeed * dt));
                }
                if (lampada != null) lampada.intensity = lampBase * (0.4f + 0.6f * Mathf.Abs(Mathf.Sin(Time.time * 12f)));
                timer -= dt;
                if (timer <= 0f) { if (proximoGiro) Girar(); else Avancar(); }
                break;

            case Estado.Avancando:
                int idx = Mathf.Clamp(celulas.Length - BateriasRestantes, 0, chargeSpeeds.Length - 1);
                Vector3 next = bodyRb.position + chargeDir * chargeSpeeds[idx] * dt;
                Vector3 clamped = new Vector3(Mathf.Clamp(next.x, arenaMin.x, arenaMax.x), next.y, Mathf.Clamp(next.z, arenaMin.z, arenaMax.z));
                bodyRb.MovePosition(clamped);
                if (clamped != next) Bater();
                break;

            case Estado.Girando:
                float passo = spinSpeed * dt;
                bodyRb.MoveRotation(bodyRb.rotation * Quaternion.Euler(0f, passo, 0f));
                girado += passo;
                if (girado >= spinSpeed * spinTime) { proximoGiro = false; Mirar(); }
                break;

            case Estado.Atordoada:
                timer -= dt;
                if (timer <= 0f) Mirar();
                break;
        }
    }

    /// Finalidade: entrar no estado Mirando. Entradas: nenhuma. Retorno: void.
    /// Corpo: apaga as zonas de dano e a bateria interagível, acende a lâmpada vermelha e inicia o tempo de mira.
    private void Mirar()
    {
        EstadoAtual = Estado.Mirando;
        timer = aimTime;
        if (hazardZone != null) hazardZone.SetActive(false);
        if (bracoGiro != null) bracoGiro.SetActive(false);
        if (bateriaInteragivel != null) bateriaInteragivel.SetActive(false);
        if (lampada != null) { lampada.enabled = true; lampada.color = corMira; }
    }

    /// Finalidade: iniciar a carga. Entradas: nenhuma. Retorno: void.
    /// Corpo: fixa a direção atual, liga a zona de dano e toca o aviso.
    private void Avancar()
    {
        EstadoAtual = Estado.Avancando;
        chargeDir = bodyRb.rotation * Vector3.forward;
        chargeDir.y = 0f;
        chargeDir.Normalize();
        if (hazardZone != null) hazardZone.SetActive(true);
        if (lampada != null) lampada.intensity = lampBase;
        if (somAviso != null) somAviso.Play();
    }

    /// Finalidade: iniciar o giro com o braço de varredura. Entradas: nenhuma. Retorno: void.
    /// Corpo: liga o braço (zona de dano), muda a lâmpada para laranja e toca o aviso; o giro dura spinTime.
    private void Girar()
    {
        EstadoAtual = Estado.Girando;
        girado = 0f;
        if (bracoGiro != null) bracoGiro.SetActive(true);
        if (lampada != null) { lampada.color = corGiro; lampada.intensity = lampBase; }
        if (somAviso != null) somAviso.Play();
    }

    /// Finalidade: reagir à batida no limite da arena. Entradas: nenhuma. Retorno: void.
    /// Corpo: desliga a zona de dano, expõe a bateria, muda a lâmpada para amarelo, inicia o atordoamento
    /// e deixa o próximo ataque como giro.
    private void Bater()
    {
        EstadoAtual = Estado.Atordoada;
        timer = stunTime;
        proximoGiro = true;
        if (hazardZone != null) hazardZone.SetActive(false);
        if (bateriaInteragivel != null) bateriaInteragivel.SetActive(true);
        if (lampada != null) lampada.color = corAtordoada;
        if (somBatida != null) somBatida.Play();
    }

    /// Finalidade: retirar uma bateria (chamado pela interação com E).
    /// Entradas: nenhuma. Retorno: bool indicando se a bateria foi retirada.
    /// Corpo: só vale atordoada; apaga a célula correspondente, mostra a mensagem e, na última,
    /// desliga a sentinela; senão ela acorda na hora e volta a mirar.
    public bool RemoverBateria()
    {
        if (EstadoAtual != Estado.Atordoada) return false;
        BateriasRestantes--;
        int removidas = celulas.Length - BateriasRestantes;
        if (removidas - 1 < celulas.Length && celulas[removidas - 1] != null) celulas[removidas - 1].SetActive(false);
        if (BateriasRestantes <= 0)
        {
            Desligar();
            return true;
        }
        GameManager.Instance.ShowMessage(string.Format(mensagemBateria, removidas, celulas.Length), 3f);
        Mirar();
        return true;
    }

    /// Finalidade: encerrar o chefe. Entradas: nenhuma. Retorno: void.
    /// Corpo: apaga lâmpada, zonas de dano e bateria; troca os objetos configurados (abre o portão),
    /// toca o som, registra a etapa no GameManager e mostra a fala final.
    private void Desligar()
    {
        EstadoAtual = Estado.Desligada;
        if (hazardZone != null) hazardZone.SetActive(false);
        if (bracoGiro != null) bracoGiro.SetActive(false);
        if (bateriaInteragivel != null) bateriaInteragivel.SetActive(false);
        if (lampada != null) lampada.enabled = false;
        foreach (var go in ativarAoDesligar) if (go != null) go.SetActive(true);
        foreach (var go in desativarAoDesligar) if (go != null) go.SetActive(false);
        if (somDesligar != null) somDesligar.Play();
        GameManager.Instance.StationCompleted(objetivoDepois);
        GameManager.Instance.ShowMessage(mensagemFim, 6f);
    }
}
