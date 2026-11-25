using UnityEngine;

// Guarda da Vigília com lanterna (nível 3). Vira de um lado para o outro; o Nilo é visto quando entra no cone
// da lanterna, dentro do alcance, sem obstáculo no meio e fora das moitas. Antes do flagrante há um tempo de reação:
// a lanterna amarela avisa que o guarda está desconfiado. Visto de vez: a lanterna fica vermelha,
// o guarda grita, o Nilo perde um coração e volta ao último ponto seguro.
public class VisaoGuarda : MonoBehaviour
{
    public float alcance = 11f;
    public float anguloMeio = 24f;          // metade da abertura do cone, em graus
    public float giro = 55f;                // quanto vira para cada lado
    public float periodo = 6f;              // segundos de uma ida e volta
    public float tempoParaVer = 0.8f;       // segundos à vista até o flagrante (tempo para voltar à moita)
    public Transform olho;                  // de onde a visão sai (a lanterna)
    public Transform pontoSeguro;
    public Light lanterna;
    public LayerMask obstaculos = ~0;
    public AudioSource grito;
    [TextArea] public string fala = "GUARDA: Tem alguém na ravina!";

    private float yawInicial, espera, suspeita;
    private Transform nilo;
    private Color corNormal;

    /// Finalidade: guardar a direção inicial e a cor da lanterna. Entradas: nenhuma. Retorno: void.
    void Start()
    {
        yawInicial = transform.eulerAngles.y;
        if (lanterna != null) corNormal = lanterna.color;
        var p = GameObject.FindWithTag("Player");
        if (p != null) nilo = p.transform;
    }

    /// Finalidade: virar e vigiar. Entradas: nenhuma. Retorno: void.
    /// Corpo: só com a partida em andamento; gira em seno; depois de um flagrante, espera 2 s antes de vigiar de novo.
    /// Com o Nilo à vista a suspeita sobe (lanterna puxa para o amarelo) e, ao chegar a tempoParaVer, vira flagrante;
    /// fora de vista ela desce no mesmo ritmo.
    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.CurrentState != GameManager.State.Playing || nilo == null) return;
        transform.rotation = Quaternion.Euler(0f, yawInicial + Mathf.Sin(Time.time * Mathf.PI * 2f / periodo) * giro, 0f);
        if (espera > 0f) { espera -= Time.deltaTime; if (espera <= 0f && lanterna != null) lanterna.color = corNormal; return; }
        suspeita = Mathf.Clamp(suspeita + (Ve(nilo.position + Vector3.up) ? Time.deltaTime : -Time.deltaTime), 0f, tempoParaVer);
        if (lanterna != null) lanterna.color = Color.Lerp(corNormal, new Color(1f, 0.85f, 0.1f), suspeita / tempoParaVer);
        if (suspeita >= tempoParaVer) Flagrante(gm);
    }

    /// Finalidade: dizer se o Nilo, nesse ponto, está à vista do guarda.
    /// Entrada: ponto no mundo. Retorno: bool. Corpo: escondido na moita nunca é visto; senão, VeAlvo.
    public bool Ve(Vector3 ponto) { return !Moita.NiloEscondido && VeAlvo(ponto); }

    /// Finalidade: dizer se um ponto qualquer está à vista do guarda (também usado para a moradora do nível 3).
    /// Entrada: ponto no mundo. Retorno: bool. Corpo: alcance, ângulo do cone e linha de visão sem obstáculo.
    public bool VeAlvo(Vector3 ponto)
    {
        Vector3 origem = olho != null ? olho.position : transform.position + Vector3.up * 1.6f;
        Vector3 d = ponto - origem;
        if (d.magnitude > alcance) return false;
        Vector3 frente = transform.forward; frente.y = 0f; Vector3 dh = d; dh.y = 0f;
        if (Vector3.Angle(frente, dh) > anguloMeio) return false;
        RaycastHit hit;
        if (Physics.Raycast(origem, d.normalized, out hit, d.magnitude - 0.3f, obstaculos, QueryTriggerInteraction.Ignore)
            && !hit.collider.CompareTag("Player")) return false;
        return true;
    }

    /// Finalidade: reagir ao Nilo visto. Entrada: GameManager. Retorno: void.
    /// Corpo: lanterna vermelha, grito e fala; tira um coração e devolve ao ponto seguro.
    public void Flagrante(GameManager gm)
    {
        espera = 2f; suspeita = 0f;
        if (lanterna != null) lanterna.color = new Color(1f, 0.2f, 0.15f);
        if (grito != null) grito.Play();
        gm.TakeHit(transform.position, pontoSeguro);
        gm.ShowMessage(fala, 2.5f);
    }
}
