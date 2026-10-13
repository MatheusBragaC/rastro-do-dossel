using UnityEngine;
using UnityEngine.Events;

// Estação de serviço do nível: ação ambiental + equipamento tecnológico num único ponto.
// A mudança visual troca objetos ativos e inativos (ex.: tampa fechada -> aberta, LED vermelho -> verde).
public class InteractableStation : Interactable
{
    public string nextObjective = "usar o terminal de saída";
    [TextArea] public string completedMessage;
    public GameObject[] enableOnComplete;
    public GameObject[] disableOnComplete;
    public UnityEvent onCompleted;   // efeitos extras: detritos, som, partículas

    public bool Completed { get; private set; }

    protected override bool Available { get { return !Completed; } }

    /// Finalidade: concluir a estação.
    /// Entradas: nenhuma (alcance e estado já verificados pela base). Retorno: bool.
    /// Corpo: recusa se já concluída; troca os objetos visuais, dispara onCompleted,
    /// atualiza o objetivo e mostra a mensagem. Acionamentos repetidos não duplicam progresso.
    public override bool Interact()
    {
        if (Completed) return false;
        Completed = true;
        foreach (var go in enableOnComplete) if (go != null) go.SetActive(true);
        foreach (var go in disableOnComplete) if (go != null) go.SetActive(false);
        onCompleted.Invoke();
        GameManager.Instance.StationCompleted(nextObjective);
        if (!string.IsNullOrEmpty(completedMessage)) GameManager.Instance.ShowMessage(completedMessage, 5f);
        return true;
    }
}
