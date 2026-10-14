using UnityEngine;

// Terminal de saída: só conclui a fase depois das estações.
public class LevelExit : Interactable
{
    [TextArea] public string pendingMessage = "Ative a estação e desligue a sentinela antes de seguir.";
    public Cutscene cutsceneFinal;   // opcional: toca antes da tela de conclusão

    /// Finalidade: validar a saída.
    /// Entradas: nenhuma. Retorno: bool indicando se a fase foi concluída.
    /// Corpo: se faltar estação, mostra a pendência e não avança; senão toca a cena final (se houver)
    /// e, ao fim dela, chama CompleteLevel.
    public override bool Interact()
    {
        var gm = GameManager.Instance;
        if (!gm.AllStationsDone)
        {
            gm.ShowMessage(pendingMessage, 3f);
            return false;
        }
        if (cutsceneFinal != null) cutsceneFinal.Tocar(gm.CompleteLevel);
        else gm.CompleteLevel();
        return true;
    }
}
