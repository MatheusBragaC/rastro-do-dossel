using UnityEngine;

// Tarefa opcional do caminho: resgatar uma muda abandonada ou recolocar um sinal derrubado.
// É um Interactable (tecla E); cada tipo aparece no HUD como "Tipo x/y".
public class Coletavel : Interactable
{
    public string tipo = "Mudas";
    [TextArea] public string mensagem = "";
    public GameObject[] ativarAoColetar;
    public GameObject[] desativarAoColetar;
    public AudioClip som;
    public ParticleSystem efeito;

    public bool Coletado { get; private set; }

    /// Finalidade: só aceitar interação enquanto não foi coletado. Retorno: bool.
    protected override bool Available { get { return !Coletado; } }

    /// Finalidade: concluir a tarefa (resgatar a muda, recolocar o sinal).
    /// Entradas: nenhuma (proximidade e estado já verificados pela base). Retorno: bool.
    /// Corpo: marca como coletado, troca os objetos visuais, toca som e partícula, soma ao
    /// contador do tipo no GameManager e mostra a mensagem curta. Repetir a tecla não soma de novo.
    public override bool Interact()
    {
        if (Coletado) return false;
        Coletado = true;
        foreach (var go in ativarAoColetar) if (go != null) go.SetActive(true);
        foreach (var go in desativarAoColetar) if (go != null) go.SetActive(false);
        if (som != null) AudioSource.PlayClipAtPoint(som, transform.position, 0.8f);
        if (efeito != null) efeito.Play();
        GameManager.Instance.ColetavelConcluido(tipo, mensagem);
        return true;
    }
}
