using UnityEngine;

// Moradora encurralada no corredor de caça (nível 3): escondida no capim, só corre até a próxima moita quando
// o Nilo, perto dela, chama com E. No caminho entre moitas os guardas podem vê-la: aí ela volta à primeira moita
// e o Nilo leva o flagrante. Chegando à última moita, a tarefa opcional "Escoltar a Dona Rita" é concluída.
public class Escoltado : Interactable
{
    public Transform[] pontos;            // centros das moitas, da primeira à última
    public float velocidade = 3.4f;
    public float raioMoita = 1.3f;        // dentro deste raio de uma moita ela está escondida
    public Coletavel tarefa;              // contador da tarefa opcional (sem alcance próprio)
    public Transform pernaE, pernaD;
    [TextArea] public string[] falas = { "DONA RITA: Agora? Tá bom, vou.", "DONA RITA: Me espera, Sabiá!", "DONA RITA: Tô indo!" };
    [TextArea] public string falaFim = "DONA RITA: Daqui eu sigo pela encosta. Que Deus te pague, menino.";

    private VisaoGuarda[] guardas;
    private int atual;
    private bool andando, salva;

    /// Finalidade: aceitar o chamado só parada e antes do fim. Retorno: bool.
    protected override bool Available { get { return !andando && !salva; } }

    /// Finalidade: achar os guardas e pôr a moradora na primeira moita. Entradas: nenhuma. Retorno: void.
    void Start()
    {
        guardas = FindObjectsByType<VisaoGuarda>(FindObjectsSortMode.None);
        if (pontos != null && pontos.Length > 0) transform.position = pontos[0].position;
    }

    /// Finalidade: chamar a moradora para a próxima moita. Entradas: nenhuma. Retorno: bool.
    public override bool Interact()
    {
        if (andando || salva || atual >= pontos.Length - 1) return false;
        atual++; andando = true;
        GameManager.Instance.ShowMessage(falas[atual % falas.Length], 2f);
        return true;
    }

    /// Finalidade: correr até a moita e vigiar os guardas. Entradas: nenhuma. Retorno: void.
    /// Corpo: anda em linha reta virada para o destino, balançando as pernas; fora das moitas, se um guarda a vê,
    /// ele dá o alarme e ela volta ao começo; ao chegar à última moita, conclui a tarefa.
    void LateUpdate()
    {
        var gm = GameManager.Instance;
        if (!andando || gm == null || gm.CurrentState != GameManager.State.Playing) return;
        Vector3 alvo = pontos[atual].position, de = pontos[atual - 1].position;
        Vector3 dir = alvo - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);
        transform.position = Vector3.MoveTowards(transform.position, alvo, velocidade * Time.deltaTime);
        float a = Mathf.Sin(Time.time * 12f) * 35f;
        if (pernaE != null) pernaE.localRotation = Quaternion.Euler(a, 0f, 0f);
        if (pernaD != null) pernaD.localRotation = Quaternion.Euler(-a, 0f, 0f);

        bool escondida = Vector3.Distance(transform.position, alvo) < raioMoita || Vector3.Distance(transform.position, de) < raioMoita;
        if (!escondida)
            foreach (var g in guardas)
                if (g != null && g.VeAlvo(transform.position + Vector3.up))
                {
                    g.Flagrante(gm);
                    gm.ShowMessage("GUARDA: Tem gente no capim! — Dona Rita voltou para o começo.", 3f);
                    andando = false; atual = 0; transform.position = pontos[0].position;
                    return;
                }
        if ((transform.position - alvo).sqrMagnitude > 0.0004f) return;
        andando = false;
        if (pernaE != null) pernaE.localRotation = Quaternion.identity;
        if (pernaD != null) pernaD.localRotation = Quaternion.identity;
        if (atual < pontos.Length - 1) return;
        salva = true;
        gm.ClearPrompt(prompt);
        if (tarefa != null) tarefa.Interact();
        gm.ShowMessage(falaFim, 5f);
    }
}
