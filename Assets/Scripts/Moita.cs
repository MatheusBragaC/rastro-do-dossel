using UnityEngine;

// Moita de capim alto (nível 3): enquanto o Nilo está dentro de uma, os guardas não o veem.
// O colisor deve ser trigger. Várias moitas podem se tocar: guarda quais o Nilo ocupa.
public class Moita : MonoBehaviour
{
    private static readonly System.Collections.Generic.HashSet<Moita> ocupadas = new System.Collections.Generic.HashSet<Moita>();

    /// Finalidade: saber se o Nilo está escondido. Retorno: bool (dentro de pelo menos uma moita).
    public static bool NiloEscondido { get { return ocupadas.Count > 0; } }

    /// Finalidade: esquecer moitas da cena anterior ao carregar a fase. Entradas: nenhuma. Retorno: void.
    void Awake() { ocupadas.RemoveWhere(m => m == null); }

    /// Finalidade: Nilo entrou. Entrada: colisor. Retorno: void. Corpo: marca esta moita como ocupada.
    void OnTriggerEnter(Collider other) { if (other.CompareTag("Player")) ocupadas.Add(this); }

    /// Finalidade: Nilo saiu. Entrada: colisor. Retorno: void. Corpo: desmarca esta moita.
    void OnTriggerExit(Collider other) { if (other.CompareTag("Player")) ocupadas.Remove(this); }

    /// Finalidade: moita desligada ou destruída não esconde mais ninguém. Entradas: nenhuma. Retorno: void.
    void OnDisable() { ocupadas.Remove(this); }
}
