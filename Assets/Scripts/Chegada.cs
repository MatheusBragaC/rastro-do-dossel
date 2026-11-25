using UnityEngine;

// Fim de um trecho sem terminal (estrada velha): ao cruzar, toca a cena final e conclui a fase.
public class Chegada : MonoBehaviour
{
    public Cutscene cena;

    /// Finalidade: concluir ao cruzar. Entrada: other. Retorno: void. Corpo: só uma vez e só com a tag Player.
    void OnTriggerEnter(Collider other)
    {
        var gm = GameManager.Instance;
        if (!other.CompareTag("Player") || gm == null || gm.CurrentState != GameManager.State.Playing) return;
        if (cena != null) cena.Tocar(gm.CompleteLevel); else gm.CompleteLevel();
    }
}
