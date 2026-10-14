using UnityEngine;

// Seta flutuante sobre um objeto que ainda precisa ser usado (tarefa, estação, bateria, terminal).
// Some quando o alvo deixa de estar disponível; sobe e desce e fica sempre de frente para a câmera.
public class MarcadorObjetivo : MonoBehaviour
{
    public Interactable alvo;
    public Transform visual;
    public float altura = 1.8f;
    public float distanciaMaxima;          // 0 = sempre visível; senão só aparece com o Nilo por perto
    public bool exigeEtapasConcluidas;     // terminal: só aparece depois da estação e da sentinela
    public float distanciaMinima = 2.2f;   // colado no objeto, a seta some (o aviso "E — ..." já aparece)

    /// Finalidade: decidir se a seta aparece e posicioná-la.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: mostra a seta só se o alvo está ativo e disponível, a partida em andamento ou pausada,
    /// as etapas exigidas concluídas e o Nilo entre a distância mínima e a máxima; então a coloca acima do alvo com
    /// um balanço vertical e a vira para a câmera (só no eixo vertical, para não inclinar).
    void LateUpdate()
    {
        var gm = GameManager.Instance;
        bool mostrar = alvo != null && alvo.isActiveAndEnabled && alvo.Disponivel && gm != null
            && (gm.CurrentState == GameManager.State.Playing || gm.CurrentState == GameManager.State.Paused);
        if (mostrar && exigeEtapasConcluidas && !gm.AllStationsDone) mostrar = false;
        if (mostrar && gm.player != null)
        {
            float d2 = (gm.player.transform.position - alvo.transform.position).sqrMagnitude;
            if (distanciaMaxima > 0f && d2 > distanciaMaxima * distanciaMaxima) mostrar = false;
            if (d2 < distanciaMinima * distanciaMinima) mostrar = false;
        }

        if (visual.gameObject.activeSelf != mostrar) visual.gameObject.SetActive(mostrar);
        if (!mostrar) return;

        visual.position = alvo.transform.position + Vector3.up * (altura + Mathf.Sin(Time.time * 3f) * 0.15f);
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 d = visual.position - cam.transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.001f) visual.rotation = Quaternion.LookRotation(d);
    }
}
