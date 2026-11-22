using UnityEngine;
using UnityEngine.AI;

// Ivo, o morador do posto (nível 4): espera no abrigo e, depois que o Nilo conclui a estação,
// percorre os pontos de serviço com um NavMeshAgent e fica trabalhando no último.
// Braços e pernas balançam conforme a velocidade do agente; trabalhando, o braço direito martela.
[RequireComponent(typeof(NavMeshAgent))]
public class NpcRoute : MonoBehaviour
{
    public enum Estado { Aguardando, Indo, Trabalhando }

    public Transform[] pontos;            // 0 = abrigo (início); os demais são percorridos em ordem
    public Transform pernaE, pernaD, bracoE, bracoD;
    public float distanciaChegada = 0.45f;
    public float balanco = 32f;           // graus do balanço de braços e pernas andando

    public Estado EstadoAtual { get; private set; }

    private NavMeshAgent agente;
    private int alvo = -1;
    private float fase;

    /// Finalidade: preparar o agente. Entradas: nenhuma. Retorno: void.
    void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
        EstadoAtual = Estado.Aguardando;
    }

    /// Finalidade: começar o trajeto de serviço (chamado pelo evento da estação). Entradas: nenhuma. Retorno: void.
    /// Corpo: vai para o ponto 1; os seguintes são encadeados ao chegar.
    public void IniciarRota()
    {
        if (EstadoAtual == Estado.Aguardando) SetDestination(1);
    }

    /// Finalidade: mandar o Ivo para um ponto da rota.
    /// Entrada: pointIndex, índice em pontos. Retorno: bool indicando se o caminho foi aceito.
    /// Corpo: recusa índice inválido, agente fora do NavMesh ou caminho incompleto; nesses casos o Ivo
    /// fica trabalhando onde está, para uma falha de navegação nunca bloquear o jogador.
    public bool SetDestination(int pointIndex)
    {
        if (pontos == null || pointIndex < 0 || pointIndex >= pontos.Length || pontos[pointIndex] == null) return Parar();
        if (!agente.isOnNavMesh) return Parar();
        var caminho = new NavMeshPath();
        if (!agente.CalculatePath(pontos[pointIndex].position, caminho) || caminho.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning("NpcRoute: sem caminho até o ponto " + pointIndex, this);
            return Parar();
        }
        agente.SetPath(caminho);
        alvo = pointIndex;
        EstadoAtual = Estado.Indo;
        return true;
    }

    /// Finalidade: acompanhar a chegada e animar o corpo. Entradas: nenhuma. Retorno: void.
    /// Corpo: ao chegar a um ponto, segue para o próximo ou passa a trabalhar no último; o balanço dos
    /// membros acompanha a velocidade do agente e, trabalhando, o braço direito martela a bancada.
    void Update()
    {
        if (EstadoAtual == Estado.Indo && !agente.pathPending && agente.remainingDistance <= distanciaChegada)
        {
            if (alvo + 1 < pontos.Length) SetDestination(alvo + 1);
            else Parar();
        }

        float vel = agente.velocity.magnitude;
        fase += Time.deltaTime * (4f + vel * 3f);
        float k = Mathf.Clamp01(vel / Mathf.Max(agente.speed, 0.01f));
        float a = Mathf.Sin(fase) * balanco * k;
        Girar(pernaE, a); Girar(pernaD, -a); Girar(bracoE, -a);
        if (EstadoAtual == Estado.Trabalhando) Girar(bracoD, -60f + Mathf.Sin(Time.time * 7f) * 35f);
        else Girar(bracoD, a);
    }

    /// Finalidade: parar e começar a trabalhar. Entradas: nenhuma. Retorno: bool (sempre false: não houve novo caminho).
    private bool Parar()
    {
        if (agente.isOnNavMesh) agente.ResetPath();
        EstadoAtual = Estado.Trabalhando;
        return false;
    }

    /// Finalidade: girar um membro no eixo X local. Entradas: membro e ângulo. Retorno: void.
    private static void Girar(Transform membro, float angulo)
    {
        if (membro != null) membro.localRotation = Quaternion.Euler(angulo, 0f, 0f);
    }
}
