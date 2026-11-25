using UnityEngine;

// Cheia na travessia (nível 2): quando o Nilo pisa na primeira pedra, a comporta de cima rompe e a água do
// fosso sobe de "yInicial" até "yFinal" em "duracao" segundos. Acima do topo das pedras, a água derruba o Nilo
// (Hazard no filho). Levar um golpe baixa a água de novo: a travessia recomeça quando ele voltar à pedra.
public class Cheia : MonoBehaviour
{
    public Bounds largada;            // zona (mundo) que solta a água
    public float zSalvo = 58f;        // margem de lá: chegando, a cheia não ameaça mais
    public float yInicial = -0.5f;
    public float yFinal = 0.25f;
    public float duracao = 15f;
    public ParticleSystem espuma;
    public AudioSource som;
    [TextArea] public string falaInicio = "MARA: A comporta de cima rompeu! A água vai cobrir as pedras. Corre!";
    [TextArea] public string falaSalvo = "NILO: Por pouco... a travessia sumiu atrás de mim.";

    private enum Estado { Parada, Subindo, Atravessou }
    private Estado estado;
    private float t;
    private int coracoes;

    /// Finalidade: começar com a água baixa. Entradas: nenhuma. Retorno: void.
    void Start() { Nivel(0f); }

    /// Finalidade: controlar a cheia. Entradas: nenhuma. Retorno: void.
    /// Corpo: parada até o Nilo entrar na largada; subindo, um coração perdido baixa a água e rearma; ao chegar
    /// à margem de lá, a água termina de subir sem perigo para ele.
    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.player == null || gm.CurrentState != GameManager.State.Playing) return;
        Vector3 p = gm.player.transform.position;
        switch (estado)
        {
            case Estado.Parada:
                if (!largada.Contains(p)) return;
                estado = Estado.Subindo; t = 0f; coracoes = gm.Hearts;
                if (espuma != null) espuma.Play();
                if (som != null) som.Play();
                gm.ShowMessage(falaInicio, 4f);
                break;
            case Estado.Subindo:
                if (gm.Hearts < coracoes) { estado = Estado.Parada; Nivel(0f); return; }
                if (p.z > zSalvo) { estado = Estado.Atravessou; gm.ShowMessage(falaSalvo, 3.5f); }
                break;
        }
        if (estado == Estado.Parada) return;
        t += Time.deltaTime;
        Nivel(Mathf.Clamp01(t / duracao));
    }

    /// Finalidade: pôr a água na fração dada da subida. Entrada: u (0 a 1). Retorno: void.
    private void Nivel(float u)
    {
        var pos = transform.position;
        pos.y = Mathf.Lerp(yInicial, yFinal, u);
        transform.position = pos;
    }
}
