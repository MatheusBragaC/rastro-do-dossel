using UnityEngine;

// Tora desgovernada (nível 1): solta por um gatilho, rola do ponto inicial até "fim" em velocidade constante,
// girando, e se espatifa no fim com poeira. É baixa o bastante para passar sob as barreiras de galhos (o Nilo
// também, deslizando) e pode ser saltada. O dano vem do Hazard (trigger) no próprio objeto.
[RequireComponent(typeof(Rigidbody))]
public class Rolante : MonoBehaviour
{
    public Vector3 fim;
    public float velocidade = 4.5f;
    public float raio = 0.45f;
    public Transform visual;
    public ParticleSystem poeiraFim;
    public AudioSource somRolar;
    public AudioSource somFim;

    private bool rolando, acabou;

    /// Finalidade: preparar o corpo cinemático e esconder a tora até ela se soltar. Entradas: nenhuma. Retorno: void.
    void Awake()
    {
        GetComponent<Rigidbody>().isKinematic = true;
        Mostrar(false);
    }

    /// Finalidade: mostrar ou esconder o visual e o colisor de dano. Entrada: visivel. Retorno: void.
    private void Mostrar(bool visivel)
    {
        if (visual != null) visual.gameObject.SetActive(visivel);
        foreach (var c in GetComponents<Collider>()) c.enabled = visivel;
    }

    /// Finalidade: soltar a tora (evento do gatilho). Entradas: nenhuma. Retorno: void. Corpo: só uma vez por tentativa; aparece com poeira atrás do Nilo.
    public void Soltar()
    {
        if (rolando || acabou) return;
        rolando = true;
        Mostrar(true);
        if (poeiraFim != null) { poeiraFim.transform.position = transform.position; poeiraFim.Play(); }
        if (somRolar != null) somRolar.Play();
    }

    /// Finalidade: rolar até o fim. Entradas: nenhuma. Retorno: void.
    /// Corpo: anda em linha reta com a partida em andamento; gira o visual pelo arco percorrido (distância / raio);
    /// no fim, toca poeira e som e some (visual e colisor).
    void Update()
    {
        var gm = GameManager.Instance;
        if (somRolar != null) somRolar.mute = !GameManager.Jogando;
        if (!rolando || gm == null || gm.CurrentState != GameManager.State.Playing) return;
        Vector3 antes = transform.position;
        transform.position = Vector3.MoveTowards(antes, fim, velocidade * Time.deltaTime);
        Vector3 passo = transform.position - antes;
        if (visual != null && passo.sqrMagnitude > 0f)
            visual.Rotate(Vector3.Cross(Vector3.up, passo.normalized), passo.magnitude / raio * Mathf.Rad2Deg, Space.World);
        if ((transform.position - fim).sqrMagnitude > 0.0001f) return;
        rolando = false; acabou = true;
        if (somRolar != null) somRolar.Stop();
        if (poeiraFim != null) { poeiraFim.transform.position = fim; poeiraFim.Play(); }
        if (somFim != null) somFim.Play();
        Mostrar(false);
    }
}
