using UnityEngine;

// Zona fatal (fosso com estacas, tronco, queda). O colisor deve ser trigger.
public class Hazard : MonoBehaviour
{
    /// Finalidade: detectar contato fatal.
    /// Entrada: other, colisor que entrou no trigger. Retorno: void.
    /// Corpo: se for o Nilo (tag Player), pede FailLevel; o GameManager ignora repetições.
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && GameManager.Instance != null)
            GameManager.Instance.FailLevel();
    }
}
