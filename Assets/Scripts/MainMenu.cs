using UnityEngine;

// Tela inicial: Jogar, Fases, Controles, Créditos e Sair.
public class MainMenu : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject controlsPanel;
    public GameObject creditsPanel;
    public GameObject fasesPanel;   // seleção de fase (útil na apresentação)
    public int firstLevelIndex = 1;

    /// Finalidade: abrir o menu no painel principal. Entradas: nenhuma. Retorno: void.
    /// Corpo: restaura a escala de tempo (caso venha de uma pausa) e mostra o painel principal.
    void Awake()
    {
        Time.timeScale = 1f;
        ShowPanel(mainPanel);
    }

    /// Finalidade: botão Jogar. Entradas: nenhuma. Retorno: void. Corpo: carrega o nível 1.
    public void Play() { Cutscene.LimparHistorico(); GameManager.StartLevel(firstLevelIndex); }

    /// Finalidade: botões da seleção de fase. Entrada: numeroDaFase (1 a 5). Retorno: void.
    /// Corpo: começa uma sessão nova (cutscenes voltam a tocar) e carrega a cena da fase pedida.
    public void JogarFase(int numeroDaFase) { Cutscene.LimparHistorico(); GameManager.StartLevel(firstLevelIndex + numeroDaFase - 1); }

    /// Finalidade: botão Fases. Entradas: nenhuma. Retorno: void. Corpo: mostra o painel de seleção de fase.
    public void ShowFases() { ShowPanel(fasesPanel); }

    /// Finalidade: botão Controles. Entradas: nenhuma. Retorno: void. Corpo: mostra o painel de controles.
    public void ShowControls() { ShowPanel(controlsPanel); }

    /// Finalidade: botão Créditos. Entradas: nenhuma. Retorno: void. Corpo: mostra o painel de créditos.
    public void ShowCredits() { ShowPanel(creditsPanel); }

    /// Finalidade: botão Voltar. Entradas: nenhuma. Retorno: void. Corpo: volta ao painel principal.
    public void Back() { ShowPanel(mainPanel); }

    /// Finalidade: botão Sair. Entradas: nenhuma. Retorno: void.
    /// Corpo: fecha o aplicativo (no Editor não tem efeito).
    public void Quit() { Application.Quit(); }

    /// Finalidade: deixar visível só um painel. Entrada: panel. Retorno: void.
    /// Corpo: ativa o painel pedido e desativa os demais (o de fases é opcional).
    private void ShowPanel(GameObject panel)
    {
        mainPanel.SetActive(panel == mainPanel);
        controlsPanel.SetActive(panel == controlsPanel);
        creditsPanel.SetActive(panel == creditsPanel);
        if (fasesPanel != null) fasesPanel.SetActive(panel == fasesPanel);
    }
}
