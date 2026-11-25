using UnityEngine;

// Moita de capim alto (nível 3): enquanto o Nilo está dentro de uma, os guardas não o veem.
// O colisor deve ser trigger. Várias moitas podem se tocar: conta quantas o Nilo ocupa.
public class Moita : MonoBehaviour
{
    private static int dentro;

    /// Finalidade: saber se o Nilo está escondido. Retorno: bool (dentro de pelo menos uma moita).
    public static bool NiloEscondido { get { return dentro > 0; } }

    /// Finalidade: zerar a contagem ao carregar a fase (as moitas da cena anterior sumiram). Retorno: void.
    void Awake() { dentro = 0; }

    /// Finalidade: Nilo entrou. Entrada: colisor. Retorno: void. Corpo: conta mais uma moita ocupada.
    void OnTriggerEnter(Collider other) { if (other.CompareTag("Player")) dentro++; }

    /// Finalidade: Nilo saiu. Entrada: colisor. Retorno: void. Corpo: conta uma a menos, sem passar de zero.
    void OnTriggerExit(Collider other) { if (other.CompareTag("Player")) dentro = Mathf.Max(0, dentro - 1); }
}
