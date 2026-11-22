using UnityEngine;

// Conversa com um personagem (tecla E): cada toque mostra a próxima fala na legenda; depois da última,
// recomeça. Não é obrigatória para concluir a fase.
public class Conversa : Interactable
{
    [TextArea] public string[] falas;
    public float duracao = 5f;
    public Transform virarPara;   // opcional: o personagem olha para o Nilo ao falar

    private int indice;

    /// Finalidade: mostrar a próxima fala. Entradas: nenhuma. Retorno: bool (true se havia fala).
    /// Corpo: escreve a fala atual na caixa de legenda, avança o índice e vira o personagem para o Nilo.
    public override bool Interact()
    {
        if (falas == null || falas.Length == 0) return false;
        GameManager.Instance.ShowMessage(falas[indice], duracao);
        indice = (indice + 1) % falas.Length;
        if (virarPara != null && GameManager.Instance.player != null)
        {
            Vector3 d = GameManager.Instance.player.transform.position - virarPara.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.01f) virarPara.rotation = Quaternion.LookRotation(d);
        }
        return true;
    }
}
