using UnityEngine;

// Jipe da Vigília na estrada velha: segue os pontos do meio da pista atrás da moto. Fica mais rápido
// quando está longe (a tensão não acaba) e mais lento quando está perto (dá chance de escapar).
// Encostar na moto encerra a tentativa.
public class Perseguidor : MonoBehaviour
{
    public Transform[] pontos;
    public Moto moto;
    public float velocidadePerto = 12.5f;
    public float velocidadeLonge = 19f;
    public float distanciaLonge = 28f;
    public float alcance = 3.2f;          // distância que conta como alcançado
    public float largada = 6f;            // metros que a moto anda antes do jipe sair
    public AudioSource sirene;

    private int atual;
    private bool saiu;
    private Vector3 inicioMoto;

    /// Finalidade: guardar de onde a moto parte. Entradas: nenhuma. Retorno: void.
    void Start() { if (moto != null) inicioMoto = moto.transform.position; }

    /// Finalidade: perseguir. Entradas: nenhuma. Retorno: void.
    /// Corpo: espera a largada; anda até o ponto atual girando para ele; avisa a distância de tempos em tempos;
    /// alcançar a moto chama FailLevel.
    void Update()
    {
        var gm = GameManager.Instance;
        if (moto == null || pontos == null || pontos.Length == 0 || gm == null || gm.CurrentState != GameManager.State.Playing) return;
        if (!saiu)
        {
            if (Vector3.Distance(moto.transform.position, inicioMoto) < largada) return;
            saiu = true;
            if (sirene != null) sirene.Play();
            gm.ShowMessage("A Vigília está na sua cola! Acelere (W).", 3f);
        }
        float dist = Vector3.Distance(transform.position, moto.transform.position);
        if (dist < alcance) { gm.ShowMessage("O jipe da Vigília alcançou você.", 3f); gm.FailLevel(); return; }
        float vel = dist > distanciaLonge ? velocidadeLonge : velocidadePerto;
        Vector3 alvo = pontos[atual].position;
        Vector3 novo = Vector3.MoveTowards(transform.position, alvo, vel * Time.deltaTime);
        Vector3 dir = alvo - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 6f * Time.deltaTime);
        transform.position = novo;
        if ((novo - alvo).sqrMagnitude < 1f && atual < pontos.Length - 1) atual++;
    }
}
