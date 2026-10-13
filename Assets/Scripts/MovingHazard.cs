using UnityEngine;

// Tronco móvel: vai do ponto inicial até start + travel e volta, com pausas fixas nas pontas.
// Ciclo previsível: espera no início, desce, espera no fim, sobe.
[RequireComponent(typeof(Rigidbody))]
public class MovingHazard : MonoBehaviour
{
    public Vector3 travel = new Vector3(0f, -2.2f, 0f);
    public float moveTime = 0.35f;
    public float holdStart = 1.8f;   // passagem aberta
    public float holdEnd = 0.6f;     // passagem fechada
    public float startOffset;        // defasagem entre troncos do mesmo trecho
    public AudioSource cue;          // aviso sonoro quando o tronco começa a descer

    private Rigidbody hazardRb;
    private Vector3 start;
    private float elapsed;
    private float lastK;

    /// Finalidade: preparar o Rigidbody cinemático e o ponto inicial.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: marca o corpo como cinemático, liga interpolação e guarda a posição de partida.
    void Awake()
    {
        hazardRb = GetComponent<Rigidbody>();
        hazardRb.isKinematic = true;
        hazardRb.interpolation = RigidbodyInterpolation.Interpolate;
        start = hazardRb.position;
        elapsed = startOffset;
    }

    /// Finalidade: movimentar o tronco entre os limites.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: avança o tempo próprio do ciclo (parado na pausa, zerado ao recarregar a cena),
    /// calcula a fração do percurso e move com MovePosition, compatível com a física.
    /// Toca o aviso sonoro uma vez por ciclo, no instante em que sai do ponto inicial.
    void FixedUpdate()
    {
        elapsed += Time.fixedDeltaTime;
        float cycle = holdStart + moveTime + holdEnd + moveTime;
        float t = elapsed % cycle;
        float k;
        if (t < holdStart) k = 0f;
        else if (t < holdStart + moveTime) k = (t - holdStart) / moveTime;
        else if (t < holdStart + moveTime + holdEnd) k = 1f;
        else k = 1f - (t - holdStart - moveTime - holdEnd) / moveTime;
        if (lastK == 0f && k > 0f && cue != null) cue.Play();
        lastK = k;
        hazardRb.MovePosition(start + travel * k);
    }
}
