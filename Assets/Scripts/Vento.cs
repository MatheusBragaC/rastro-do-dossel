using UnityEngine;

// Rajadas de vento na subida da serra (nível 5). De tempos em tempos a poeira avisa e o vento empurra o Nilo
// para o lado enquanto ele está dentro da zona (trigger). No ar o empurrão é maior: saltar na hora errada custa a plataforma.
public class Vento : MonoBehaviour
{
    public Vector3 direcao = Vector3.right;
    public float forca = 9f;              // aceleração no ar (no chão, um quinto)
    public float intervalo = 4f;          // tempo entre o início de duas rajadas
    public float duracao = 1.6f;
    public float aviso = 0.8f;            // poeira antes do empurrão
    public ParticleSystem poeira;
    public AudioSource som;

    private float relogio;
    private PlayerController controle;

    /// Finalidade: rajada em andamento. Retorno: bool.
    public bool Soprando { get { return relogio >= aviso && relogio < aviso + duracao; } }

    /// Finalidade: Nilo dentro da zona. Entrada: colisor. Retorno: void. Corpo: guarda o corpo para empurrar.
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        controle = other.GetComponentInParent<PlayerController>();
    }

    /// Finalidade: Nilo saiu. Entrada: colisor. Retorno: void.
    void OnTriggerExit(Collider other) { if (other.CompareTag("Player")) controle = null; }

    /// Finalidade: ciclo da rajada. Entradas: nenhuma. Retorno: void.
    /// Corpo: conta o tempo com a partida em andamento; no aviso solta a poeira e o som.
    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.CurrentState != GameManager.State.Playing) return;
        float antes = relogio;
        relogio += Time.deltaTime;
        if (antes < 0.01f && poeira != null) poeira.Play();
        if (antes < aviso && relogio >= aviso && som != null) som.Play();
        if (relogio >= intervalo) relogio = 0f;
    }

    /// Finalidade: empurrar durante a rajada. Entradas: nenhuma. Retorno: void.
    /// Corpo: só com a partida em andamento; passa a aceleração ao PlayerController, que a soma à velocidade
    /// do controle (um AddForce seria apagado, porque o controle define a velocidade horizontal a cada passo).
    void FixedUpdate()
    {
        if (controle == null || !Soprando || !GameManager.Jogando) return;
        controle.Empurrar(direcao.normalized * forca);
    }
}
