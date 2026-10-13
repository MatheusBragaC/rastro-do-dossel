using UnityEngine;

// Zona que mostra um aviso de controle ou uma fala, uma vez por tentativa.
public class TutorialTrigger : MonoBehaviour
{
    [TextArea] public string message;
    public float duration = 5f;

    private bool shown;

    /// Finalidade: apresentar a ajuda no local correto.
    /// Entrada: other, colisor que entrou na zona. Retorno: void.
    /// Corpo: confirma o Nilo, verifica se já foi exibida nesta tentativa e mostra a mensagem
    /// sem pausar o jogo. Recarregar a cena permite exibi-la de novo.
    void OnTriggerEnter(Collider other)
    {
        if (shown || !other.CompareTag("Player") || GameManager.Instance == null) return;
        shown = true;
        GameManager.Instance.ShowMessage(message, duration);
    }
}
