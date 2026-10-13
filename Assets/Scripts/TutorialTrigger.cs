using UnityEngine;
using UnityEngine.Events;

// Zona que mostra um aviso de controle ou uma fala, uma vez por tentativa.
// Pode também disparar um evento (ex.: acordar a sentinela ao entrar na arena).
public class TutorialTrigger : MonoBehaviour
{
    [TextArea] public string message;
    public float duration = 5f;
    public UnityEvent onEnter = new UnityEvent();

    private bool shown;

    /// Finalidade: apresentar a ajuda no local correto.
    /// Entrada: other, colisor que entrou na zona. Retorno: void.
    /// Corpo: confirma o Nilo, verifica se já foi exibida nesta tentativa, mostra a mensagem
    /// (se houver) sem pausar o jogo e dispara onEnter. Recarregar a cena permite exibi-la de novo.
    void OnTriggerEnter(Collider other)
    {
        if (shown || !other.CompareTag("Player") || GameManager.Instance == null) return;
        shown = true;
        if (!string.IsNullOrEmpty(message)) GameManager.Instance.ShowMessage(message, duration);
        onEnter.Invoke();
    }
}
