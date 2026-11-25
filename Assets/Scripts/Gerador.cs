using UnityEngine;

// Gerador da Vigília (nível 3): com E, o Nilo força o gerador, que explode e apaga o que ele alimenta
// (holofote, luzes, cerca). Usa a explosão de partículas feita pela equipe.
public class Gerador : Interactable
{
    public ParticleSystem explosao;
    public AudioSource som;
    public GameObject[] desligarAoExplodir;
    public GameObject[] ligarAoExplodir;
    [TextArea] public string mensagem = "O gerador explodiu: holofote apagado.";

    public bool Explodiu { get; private set; }

    /// Finalidade: só aceita interação antes de explodir. Retorno: bool.
    protected override bool Available { get { return !Explodiu; } }

    /// Finalidade: explodir o gerador. Entradas: nenhuma. Retorno: bool indicando se teve efeito.
    /// Corpo: recusa repetição; toca a explosão e o som; desliga e liga os objetos indicados; mostra a mensagem.
    public override bool Interact()
    {
        if (Explodiu) return false;
        Explodiu = true;
        if (explosao != null) explosao.Play(true);
        if (som != null) som.Play();
        foreach (var g in desligarAoExplodir) if (g != null) g.SetActive(false);
        foreach (var g in ligarAoExplodir) if (g != null) g.SetActive(true);
        if (GameManager.Instance != null) GameManager.Instance.ShowMessage(mensagem, 4f);
        return true;
    }
}
