using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Estado da partida numa fase: HUD, pausa, falha, reinício e conclusão.
// Existe um por cena de nível; os outros scripts o acessam por GameManager.Instance.
public class GameManager : MonoBehaviour
{
    public enum State { Playing, Paused, Failed, Completed }

    public static GameManager Instance { get; private set; }

    [Header("Fase")]
    public string levelName = "Nível 1 — Trilha encoberta";
    public string startObjective = "chegar ao posto e ativar a estação do viveiro";
    public int stationsTotal = 1;
    public bool partialDelivery = true;   // build do 1º bimestre: a conclusão do nível 1 encerra a demonstração
    public int menuSceneIndex = 0;
    public int firstLevelIndex = 1;

    [Header("Vida")]
    public int maxHearts = 3;
    public float invulnerableTime = 1.2f;   // após um golpe, outros contatos são ignorados

    [Header("Referências")]
    public PlayerController player;
    public Text levelText;
    public Text objectiveText;
    public Text stationText;
    public Text promptText;
    public GameObject messageBox;
    public Text messageText;
    public GameObject pausePanel;
    public GameObject failPanel;
    public GameObject completePanel;
    public Image[] heartIcons;   // um ícone por coração, da esquerda para a direita
    public Text coletaveisText;  // "Mudas 0/5 · Sinais 0/3"

    public State CurrentState { get; private set; }
    public int StationsDone { get; private set; }
    public int Hearts { get; private set; }
    public bool AllStationsDone { get { return StationsDone >= stationsTotal; } }

    private float messageTimer;
    private float invulnerableUntil;
    private readonly List<string> tiposColetaveis = new List<string>();
    private readonly Dictionary<string, int> coletaveisTotal = new Dictionary<string, int>();
    private readonly Dictionary<string, int> coletaveisFeitos = new Dictionary<string, int>();

    /// Finalidade: preparar a fase no carregamento da cena.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: registra a instância, garante tempo normal, esconde painéis, preenche o HUD,
    /// enche os corações e trava o cursor para a câmera girar com o mouse.
    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        CurrentState = State.Playing;
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        pausePanel.SetActive(false);
        failPanel.SetActive(false);
        completePanel.SetActive(false);
        messageBox.SetActive(false);
        promptText.text = "";
        levelText.text = levelName;
        SetObjective(startObjective);
        UpdateStationText();
        Hearts = maxHearts;
        UpdateHearts();
        ContarColetaveis();
        SetCursorLocked(true);
    }

    /// Finalidade: ler R (reiniciar) e Esc (pausa) e expirar a mensagem atual.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: R chama RestartLevel em qualquer estado; Esc alterna a pausa só entre
    /// jogando e pausado. O tempo da mensagem não corre durante a pausa.
    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.rKey.wasPressedThisFrame) RestartLevel();
            else if (kb.escapeKey.wasPressedThisFrame) SetPaused(CurrentState == State.Playing);
        }

        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f) messageBox.SetActive(false);
        }
    }

    /// Finalidade: iniciar uma cena válida.
    /// Entrada: sceneIndex, índice na lista de cenas da build. Retorno: void.
    /// Corpo: recusa índices fora da build (evita carregar fases ainda ausentes),
    /// restaura a escala de tempo e carrega a cena.
    public static void StartLevel(int sceneIndex)
    {
        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError("GameManager: cena " + sceneIndex + " não está na build.");
            return;
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
    }

    /// Finalidade: encerrar a tentativa após contato fatal.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: ignora chamadas repetidas, bloqueia o controle, congela o Nilo, aciona o
    /// som, a partícula e a animação de falha uma única vez e abre o painel com Reiniciar.
    public void FailLevel()
    {
        if (CurrentState == State.Failed || CurrentState == State.Completed) return;
        if (CurrentState == State.Paused) SetPaused(false);
        CurrentState = State.Failed;
        player.controlEnabled = false;
        var rb = player.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        var feedback = player.GetComponent<PlayerFeedback>();
        if (feedback != null) feedback.PlayFail();
        promptText.text = "";
        messageBox.SetActive(false);
        failPanel.SetActive(true);
        SetCursorLocked(false);
    }

    /// Finalidade: aplicar um golpe não fatal ao Nilo.
    /// Entradas: sourcePosition, de onde veio o golpe (define o empurrão); respawnPoint, ponto
    /// seguro para onde voltar (fossos) ou null para só empurrar. Retorno: void.
    /// Corpo: ignora fora da partida e durante a invulnerabilidade; tira um coração, atualiza o HUD
    /// e toca a resposta de dano; sem corações chama FailLevel; senão teleporta ao ponto seguro
    /// ou empurra o Nilo para longe da origem do golpe.
    public void TakeHit(Vector3 sourcePosition, Transform respawnPoint)
    {
        if (CurrentState != State.Playing || Time.time < invulnerableUntil) return;
        invulnerableUntil = Time.time + invulnerableTime;
        Hearts--;
        UpdateHearts();
        var feedback = player.GetComponent<PlayerFeedback>();
        if (Hearts <= 0)
        {
            FailLevel();
            return;
        }
        if (feedback != null) feedback.PlayHurt(invulnerableTime);
        if (respawnPoint != null)
        {
            var rb = player.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.position = respawnPoint.position;
            player.transform.position = respawnPoint.position;
        }
        else
        {
            player.Knockback(player.transform.position - sourcePosition);
        }
    }

    /// Finalidade: repetir a fase atual.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: recarrega a cena ativa, o que restaura estações, perigos e efeitos.
    /// Botões Reiniciar e a tecla R chamam este mesmo método.
    public void RestartLevel()
    {
        StartLevel(SceneManager.GetActiveScene().buildIndex);
    }

    /// Finalidade: alterar a pausa.
    /// Entrada: paused, estado desejado. Retorno: void.
    /// Corpo: só pausa jogando e só retoma pausado; ajusta escala de tempo, controle e painel.
    public void SetPaused(bool paused)
    {
        if (paused && CurrentState != State.Playing) return;
        if (!paused && CurrentState != State.Paused) return;
        CurrentState = paused ? State.Paused : State.Playing;
        Time.timeScale = paused ? 0f : 1f;
        player.controlEnabled = !paused;
        pausePanel.SetActive(paused);
        SetCursorLocked(!paused);
    }

    /// Finalidade: botão Continuar da pausa. Entradas: nenhuma. Retorno: void.
    /// Corpo: chama SetPaused(false).
    public void Resume() { SetPaused(false); }

    /// Finalidade: botão Menu. Entradas: nenhuma. Retorno: void.
    /// Corpo: carrega a cena do menu com o tempo restaurado.
    public void GoToMenu() { StartLevel(menuSceneIndex); }

    /// Finalidade: botão Jogar novamente. Entradas: nenhuma. Retorno: void.
    /// Corpo: carrega o primeiro nível.
    public void PlayFromStart() { StartLevel(firstLevelIndex); }

    /// Finalidade: concluir a fase.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: bloqueia chamadas repetidas; na entrega parcial ou sem próxima cena na build,
    /// mostra a tela de conclusão; caso contrário carrega o nível seguinte.
    public void CompleteLevel()
    {
        if (CurrentState != State.Playing) return;
        CurrentState = State.Completed;
        player.controlEnabled = false;
        promptText.text = "";
        SetCursorLocked(false);
        EscreverResumoColetaveis();
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (partialDelivery || next >= SceneManager.sceneCountInBuildSettings)
        {
            messageBox.SetActive(false);
            completePanel.SetActive(true);
        }
        else
            StartLevel(next);
    }

    /// Finalidade: registrar uma estação concluída.
    /// Entrada: nextObjective, texto do objetivo seguinte. Retorno: void.
    /// Corpo: soma uma estação, atualiza o contador e o objetivo do HUD.
    public void StationCompleted(string nextObjective)
    {
        StationsDone++;
        UpdateStationText();
        SetObjective(nextObjective);
    }

    /// Finalidade: trocar o objetivo exibido. Entrada: text. Retorno: void.
    /// Corpo: escreve "Objetivo: ..." no HUD.
    public void SetObjective(string text)
    {
        objectiveText.text = "Objetivo: " + text;
    }

    /// Finalidade: mostrar uma fala ou aviso na caixa de legenda.
    /// Entradas: text, conteúdo; duration, segundos visíveis. Retorno: void.
    /// Corpo: substitui a mensagem atual e reinicia o tempo de exibição.
    public void ShowMessage(string text, float duration)
    {
        messageText.text = text;
        messageBox.SetActive(true);
        messageTimer = duration;
    }

    /// Finalidade: mostrar ou esconder o aviso de interação. Entrada: text ("" esconde). Retorno: void.
    /// Corpo: escreve o texto no prompt central.
    public void SetPrompt(string text)
    {
        promptText.text = text;
    }

    /// Finalidade: esconder o aviso de interação de um objeto específico. Entrada: text. Retorno: void.
    /// Corpo: só apaga se o aviso exibido ainda for o desse objeto, para que um objeto
    /// saindo do alcance não apague o aviso de outro que acabou de entrar.
    public void ClearPrompt(string text)
    {
        if (promptText.text == text) promptText.text = "";
    }

    /// Finalidade: registrar uma tarefa opcional concluída (muda resgatada, sinal recolocado).
    /// Entradas: tipo, nome do contador no HUD; mensagem, fala curta (pode ser vazia). Retorno: void.
    /// Corpo: soma ao contador do tipo, atualiza o HUD e mostra a mensagem com o progresso.
    public void ColetavelConcluido(string tipo, string mensagem)
    {
        if (!coletaveisFeitos.ContainsKey(tipo)) return;
        coletaveisFeitos[tipo]++;
        AtualizarColetaveis();
        string progresso = tipo + " " + coletaveisFeitos[tipo] + "/" + coletaveisTotal[tipo];
        ShowMessage(string.IsNullOrEmpty(mensagem) ? progresso : mensagem + " (" + progresso + ")", 3f);
    }

    /// Finalidade: contar as tarefas opcionais da cena por tipo. Entradas: nenhuma. Retorno: void.
    /// Corpo: percorre os Coletavel (inclusive inativos), guarda a ordem de aparição e os totais.
    private void ContarColetaveis()
    {
        foreach (var c in FindObjectsByType<Coletavel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!coletaveisTotal.ContainsKey(c.tipo)) { coletaveisTotal[c.tipo] = 0; coletaveisFeitos[c.tipo] = 0; tiposColetaveis.Add(c.tipo); }
            coletaveisTotal[c.tipo]++;
        }
        tiposColetaveis.Sort();
        AtualizarColetaveis();
    }

    /// Finalidade: escrever "Mudas 1/5   Sinais 0/3" no HUD. Entradas: nenhuma. Retorno: void.
    private void AtualizarColetaveis()
    {
        if (coletaveisText == null) return;
        var partes = new List<string>();
        foreach (var t in tiposColetaveis) partes.Add(t + " " + coletaveisFeitos[t] + "/" + coletaveisTotal[t]);
        coletaveisText.text = string.Join("   ", partes.ToArray());
    }

    /// Finalidade: acrescentar o resumo das tarefas opcionais na tela de conclusão. Entradas: nenhuma. Retorno: void.
    /// Corpo: soma uma linha ao subtítulo do painel de conclusão, se houver tarefas na fase.
    private void EscreverResumoColetaveis()
    {
        if (tiposColetaveis.Count == 0 || completePanel == null) return;
        var sub = completePanel.transform.Find("Subtitulo");
        if (sub == null) return;
        var texto = sub.GetComponent<Text>();
        if (texto == null) return;
        var partes = new List<string>();
        foreach (var t in tiposColetaveis) partes.Add(t + ": " + coletaveisFeitos[t] + " de " + coletaveisTotal[t]);
        texto.text += "\n" + string.Join("  ·  ", partes.ToArray());
    }

    /// Finalidade: atualizar o contador "Etapas x/y" (estação e sentinela). Entradas: nenhuma. Retorno: void.
    /// Corpo: formata StationsDone e stationsTotal no HUD.
    private void UpdateStationText()
    {
        stationText.text = "Etapas " + StationsDone + "/" + stationsTotal;
    }

    /// Finalidade: mostrar os corações restantes. Entradas: nenhuma. Retorno: void.
    /// Corpo: deixa visíveis só os primeiros Hearts ícones.
    private void UpdateHearts()
    {
        if (heartIcons == null) return;
        for (int i = 0; i < heartIcons.Length; i++)
            if (heartIcons[i] != null) heartIcons[i].enabled = i < Hearts;
    }

    /// Finalidade: travar ou soltar o cursor do mouse. Entrada: locked. Retorno: void.
    /// Corpo: travado e invisível durante a partida (a câmera gira com o mouse); livre nos painéis.
    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
