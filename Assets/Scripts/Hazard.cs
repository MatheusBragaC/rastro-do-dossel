using UnityEngine;

// Zona de perigo (fosso com estacas, tronco, pilão, sentinela). O colisor deve ser trigger.
// Fatal encerra a tentativa; Hit tira um coração e empurra; HitAndRespawn tira um coração
// e devolve o Nilo ao ponto seguro indicado (usado nos fossos).
public class Hazard : MonoBehaviour
{
    public enum Mode { Fatal, Hit, HitAndRespawn }

    public Mode mode = Mode.Hit;
    public Transform respawnPoint;   // só para HitAndRespawn

    /// Finalidade: detectar o contato ao entrar na zona. Entrada: other. Retorno: void.
    void OnTriggerEnter(Collider other) { Touch(other); }

    /// Finalidade: detectar o contato de quem fica parado dentro da zona (ex.: tronco girando
    /// sobre o Nilo parado). Entrada: other. Retorno: void.
    void OnTriggerStay(Collider other) { Touch(other); }

    /// Finalidade: aplicar o efeito do perigo ao Nilo.
    /// Entrada: other, colisor que tocou o trigger. Retorno: void.
    /// Corpo: só reage à tag Player; conforme o modo pede FailLevel ou TakeHit ao GameManager,
    /// que ignora repetições e o tempo de invulnerabilidade.
    private void Touch(Collider other)
    {
        if (!other.CompareTag("Player") || GameManager.Instance == null) return;
        if (mode == Mode.Fatal) GameManager.Instance.FailLevel();
        else GameManager.Instance.TakeHit(transform.position, mode == Mode.HitAndRespawn ? respawnPoint : null);
    }
}
