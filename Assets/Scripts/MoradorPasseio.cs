using UnityEngine;
using UnityEngine.AI;

// Morador do posto das raízes (nível 4): anda pelo NavMesh entre pontos da aldeia, para um pouco em cada um
// e segue para o próximo, em ciclo. Pernas e braços balançam conforme a velocidade.
[RequireComponent(typeof(NavMeshAgent))]
public class MoradorPasseio : MonoBehaviour
{
    public Transform[] pontos;
    public float pausa = 2.5f;
    public Transform pernaE, pernaD, bracoE, bracoD;
    public float balanco = 30f;

    private NavMeshAgent agente;
    private int atual;
    private float parado;

    /// Finalidade: preparar o agente e mandar para o primeiro ponto. Entradas: nenhuma. Retorno: void.
    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        if (pontos != null && pontos.Length > 0 && agente.isOnNavMesh) agente.SetDestination(pontos[0].position);
    }

    /// Finalidade: andar, parar e seguir. Entradas: nenhuma. Retorno: void.
    /// Corpo: ao chegar, conta a pausa e escolhe o próximo ponto (em ciclo); anima o corpo pela velocidade.
    void Update()
    {
        if (pontos == null || pontos.Length == 0 || !agente.isOnNavMesh) return;
        if (!agente.pathPending && agente.remainingDistance <= agente.stoppingDistance + 0.2f)
        {
            parado += Time.deltaTime;
            if (parado >= pausa)
            {
                parado = 0f;
                atual = (atual + 1) % pontos.Length;
                agente.SetDestination(pontos[atual].position);
            }
        }
        float v = Mathf.Clamp01(agente.velocity.magnitude / Mathf.Max(0.1f, agente.speed));
        float a = Mathf.Sin(Time.time * 8f) * balanco * v;
        if (pernaE != null) pernaE.localRotation = Quaternion.Euler(a, 0f, 0f);
        if (pernaD != null) pernaD.localRotation = Quaternion.Euler(-a, 0f, 0f);
        if (bracoE != null) bracoE.localRotation = Quaternion.Euler(-a * 0.8f, 0f, 0f);
        if (bracoD != null) bracoD.localRotation = Quaternion.Euler(a * 0.8f, 0f, 0f);
    }
}
