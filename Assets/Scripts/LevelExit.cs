using UnityEngine;

// Terminal de saída: só conclui a fase depois das estações.
public class LevelExit : Interactable
{
    [TextArea] public string pendingMessage = "Ative a estação antes de seguir.";

    /// Finalidade: validar a saída.
    /// Entradas: nenhuma. Retorno: bool indicando se a fase foi concluída.
    /// Corpo: se faltar estação, mostra a pendência e não avança; senão chama CompleteLevel.
    public override bool Interact()
    {
        var gm = GameManager.Instance;
        if (!gm.AllStationsDone)
        {
            gm.ShowMessage(pendingMessage, 3f);
            return false;
        }
        gm.CompleteLevel();
        return true;
    }
}
