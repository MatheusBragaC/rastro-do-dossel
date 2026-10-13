using UnityEngine;

// Bateria exposta nas costas da sentinela atordoada: interação com E que retira uma bateria.
public class BateriaSentinela : Interactable
{
    public SentinelaChefe sentinela;

    /// Finalidade: só aceitar interação enquanto a sentinela está atordoada. Retorno: bool.
    protected override bool Available { get { return sentinela != null && sentinela.EstadoAtual == SentinelaChefe.Estado.Atordoada; } }

    /// Finalidade: retirar a bateria. Entradas: nenhuma. Retorno: bool indicando se teve efeito.
    /// Corpo: repassa ao chefe, que valida o estado e conta as baterias.
    public override bool Interact()
    {
        return sentinela != null && sentinela.RemoverBateria();
    }
}
