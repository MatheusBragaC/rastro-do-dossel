using UnityEngine;
using UnityEngine.InputSystem;

// Base dos objetos acionados com E (estação e terminal de saída).
// Detecta o Nilo por distância, mostra o aviso e chama Interact uma vez por toque na tecla.
public abstract class Interactable : MonoBehaviour
{
    public string prompt = "E — realizar ação";
    public float range = 1.8f;

    private bool showingPrompt;

    /// Finalidade: dizer se o objeto ainda aceita interação. Retorno: bool.
    protected virtual bool Available { get { return true; } }

    /// Finalidade: deixar outros componentes (como a seta de objetivo) saberem se o objeto ainda aceita interação. Retorno: bool.
    public bool Disponivel { get { return Available; } }

    /// Finalidade: executar a ação do objeto. Retorno: bool indicando se teve efeito.
    public abstract bool Interact();

    /// Finalidade: detectar proximidade e a tecla E.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: só com a partida em andamento; atualiza o aviso apenas quando a proximidade
    /// muda e chama Interact quando E é pressionado dentro do alcance; se a ação teve efeito,
    /// toca a animação de interação do Nilo.
    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.player == null) return;

        bool near = Available && gm.CurrentState == GameManager.State.Playing
            && Vector3.Distance(gm.player.transform.position, transform.position) <= range;
        if (near != showingPrompt)
        {
            showingPrompt = near;
            if (near) gm.SetPrompt(prompt);
            else gm.ClearPrompt(prompt);
        }

        var kb = Keyboard.current;
        if (near && kb != null && kb.eKey.wasPressedThisFrame && Interact())
        {
            var feedback = gm.player.GetComponent<PlayerFeedback>();
            if (feedback != null) feedback.PlayInteract();
        }
    }
}
