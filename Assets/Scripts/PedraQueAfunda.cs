using UnityEngine;

// Pedra de passagem que afunda com o peso do Nilo (nível 2): treme, desce abaixo da água e volta depois de um tempo.
// Quem ficar em cima cai no rio; quem passar rápido atravessa.
[RequireComponent(typeof(Rigidbody))]
public class PedraQueAfunda : MonoBehaviour
{
    public float espera = 0.6f;        // segundos tremendo antes de afundar
    public float profundidade = 1.5f;  // quanto desce
    public float velocidade = 2.4f;    // metros por segundo
    public float volta = 2.5f;         // segundos submersa antes de subir
    public AudioSource som;

    private enum Estado { Firme, Tremendo, Afundando, Submersa, Subindo }
    private Estado estado;
    private Rigidbody rb;
    private Vector3 topo;
    private float relogio;
    private bool pisando;
    private float topoColisor => GetComponentInChildren<Collider>().bounds.max.y;   // topo atual (desce junto ao afundar)

    /// Finalidade: guardar a posição de cima e preparar o corpo cinemático. Entradas: nenhuma. Retorno: void.
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        topo = transform.position;
    }

    /// Finalidade: perceber o Nilo em cima da pedra. Entrada: a colisão. Retorno: void.
    /// Corpo: marca contato só com os pés do Nilo no alto da pedra (pisando, não encostando de lado).
    void OnCollisionStay(Collision c)
    {
        if (c.collider.CompareTag("Player") && c.transform.position.y >= topoColisor - 0.15f) pisando = true;
    }

    /// Finalidade: máquina de estados da pedra. Entradas: nenhuma. Retorno: void.
    /// Corpo: firme até ser pisada; treme por "espera"; desce até "profundidade"; espera "volta"; sobe e fica firme.
    void FixedUpdate()
    {
        if (!GameManager.Jogando) return;   // parado em cutscene, falha e conclusão
        relogio += Time.fixedDeltaTime;
        Vector3 alvo = topo;
        switch (estado)
        {
            case Estado.Firme:
                if (pisando) { estado = Estado.Tremendo; relogio = 0f; if (som != null) som.Play(); }
                break;
            case Estado.Tremendo:
                alvo = topo + new Vector3(Mathf.Sin(relogio * 70f) * 0.04f, -relogio * 0.08f, 0f);
                if (relogio >= espera) { estado = Estado.Afundando; relogio = 0f; }
                break;
            case Estado.Afundando:
                alvo = topo + Vector3.down * Mathf.Min(profundidade, relogio * velocidade);
                if (relogio * velocidade >= profundidade) { estado = Estado.Submersa; relogio = 0f; }
                break;
            case Estado.Submersa:
                alvo = topo + Vector3.down * profundidade;
                if (relogio >= volta) { estado = Estado.Subindo; relogio = 0f; }
                break;
            case Estado.Subindo:
                alvo = topo + Vector3.down * Mathf.Max(0f, profundidade - relogio * velocidade * 0.6f);
                if (relogio * velocidade * 0.6f >= profundidade) { estado = Estado.Firme; relogio = 0f; }
                break;
        }
        rb.MovePosition(alvo);
        pisando = false;
    }
}
