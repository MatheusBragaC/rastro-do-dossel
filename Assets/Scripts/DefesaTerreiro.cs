using UnityEngine;

// Defesa do terreiro (nível 4): depois da barreira, a encosta desce de vez e as pedras passam por cima,
// atravessando o terreiro. O Nilo acende as tochas (tarefas com E) para os moradores acharem o abrigo; com
// todas acesas, a encosta para. Se o prazo acabar, uma pedra o atinge e o prazo recomeça.
public class DefesaTerreiro : MonoBehaviour
{
    public Coletavel[] tochas;
    public GameObject encosta;            // segunda EncostaInstavel, ligada só durante a defesa
    public float prazo = 45f;
    [TextArea] public string falaInicio = "IVO: A encosta desceu de vez! Acende as tochas do terreiro pro pessoal achar o abrigo!";
    [TextArea] public string falaFim = "IVO: Todo mundo no abrigo! A encosta parou. Obrigado, Sabiá.";

    private bool ativa;
    private float resta, avisoAte, falaEm;
    private bool falou;

    /// Finalidade: começar a defesa (evento da estação). Entradas: nenhuma. Retorno: void.
    /// Corpo: só uma vez; mostra as tochas e liga a encosta; o aviso do Ivo vem 4 s depois, após a mensagem da estação.
    public void Iniciar()
    {
        if (ativa) return;
        ativa = true; resta = prazo; falaEm = Time.time + 4f; avisoAte = falaEm + 6f;
        foreach (var t in tochas) if (t != null) t.gameObject.SetActive(true);
        if (encosta != null) encosta.SetActive(true);
    }

    /// Finalidade: contar o prazo e as tochas. Entradas: nenhuma. Retorno: void.
    /// Corpo: com todas acesas, desliga a encosta e encerra; a cada 5 s mostra quantas faltam e o tempo;
    /// prazo esgotado tira um coração e recomeça a contagem.
    void Update()
    {
        var gm = GameManager.Instance;
        if (!ativa || gm == null || gm.CurrentState != GameManager.State.Playing) return;
        int acesas = 0;
        foreach (var t in tochas) if (t != null && t.Coletado) acesas++;
        if (acesas == tochas.Length)
        {
            ativa = false;
            if (encosta != null) encosta.SetActive(false);
            gm.ShowMessage(falaFim, 6f);
            return;
        }
        if (!falou && Time.time >= falaEm) { falou = true; gm.ShowMessage(falaInicio, 6f); }
        float antes = resta;
        resta -= Time.deltaTime;
        if (Time.time > avisoAte && Mathf.FloorToInt(antes / 5f) != Mathf.FloorToInt(resta / 5f))
            gm.ShowMessage("Tochas acesas " + acesas + "/" + tochas.Length + " — " + Mathf.CeilToInt(resta) + " s", 2f);
        if (resta > 0f) return;
        resta = prazo; avisoAte = Time.time + 3f;
        gm.TakeHit(gm.player.transform.position + Vector3.left, null);
        gm.ShowMessage("IVO: As pedras estão chegando nas casas! Rápido com as tochas!", 3f);
    }
}
