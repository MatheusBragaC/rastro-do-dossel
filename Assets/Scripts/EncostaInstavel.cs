using UnityEngine;

// Encosta instável (nível 4): de tempos em tempos uma rocha se solta e rola pela trilha.
// Antes de cada queda, poeira e um aviso sonoro marcam o ponto; a rocha é sorteada de um vetor de prefabs,
// recebe força e torque e é descartada depois de alguns segundos.
public class EncostaInstavel : MonoBehaviour
{
    public GameObject[] rochas;
    public Vector3 inicio;               // extremos da linha de onde as rochas se soltam (coordenadas do mundo)
    public Vector3 fim;
    public Vector3 impulso = new Vector3(7f, 0f, 0f);
    public float intervalo = 2.4f;
    public float aviso = 0.9f;           // segundos de aviso antes da queda
    public float vida = 5f;
    public ParticleSystem poeira;
    public AudioSource somAviso;

    private float timer;
    private bool avisou;
    private Vector3 proximo;

    /// Finalidade: começar com uma pausa, para o jogador ver o perigo antes da primeira rocha. Retorno: void.
    void Start()
    {
        timer = intervalo + 1f;
    }

    /// Finalidade: controlar aviso e queda. Entradas: nenhuma. Retorno: void.
    /// Corpo: só conta o tempo com a partida em andamento; no momento do aviso sorteia o ponto, toca a poeira
    /// e o som; ao zerar, solta a rocha e recomeça o ciclo.
    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.CurrentState != GameManager.State.Playing) return;
        timer -= Time.deltaTime;
        if (!avisou && timer <= aviso)
        {
            proximo = Vector3.Lerp(inicio, fim, Random.value);
            if (poeira != null) { poeira.transform.position = proximo; poeira.Play(); }
            if (somAviso != null) { somAviso.transform.position = proximo; somAviso.Play(); }
            avisou = true;
        }
        if (timer <= 0f)
        {
            Soltar(proximo);
            timer = intervalo;
            avisou = false;
        }
    }

    /// Finalidade: criar uma rocha rolando.
    /// Entrada: origem, ponto de onde ela se solta. Retorno: GameObject criado (ou null se o vetor estiver vazio).
    /// Corpo: sorteia um índice válido do vetor, instancia com rotação aleatória, aplica impulso e torque e agenda o descarte.
    public GameObject Soltar(Vector3 origem)
    {
        if (rochas == null || rochas.Length == 0) return null;
        var rocha = Instantiate(rochas[Random.Range(0, rochas.Length)], origem, Random.rotation);
        var rb = rocha.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(impulso + new Vector3(0f, 0f, Random.Range(-0.8f, 0.8f)), ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 4f, ForceMode.VelocityChange);
        }
        Destroy(rocha, vida);
        return rocha;
    }
}
