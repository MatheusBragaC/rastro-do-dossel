using UnityEngine;
using UnityEngine.InputSystem;

// Moto velha do Ivo (fase da estrada velha): o Nilo começa montado. Física arcade em Rigidbody:
// W acelera, S freia (e dá ré parada), A/D esterçam, Espaço é o freio de mão (derrapa).
// A moto recebe a tag Player para que perigos e gatilhos da fase reajam a ela.
[RequireComponent(typeof(Rigidbody))]
public class Moto : MonoBehaviour
{
    public PlayerController nilo;
    public Transform assento;
    public Transform visual;              // corpo que inclina nas curvas
    public Transform[] rodas;
    public float aceleracao = 12f;
    public float velocidadeMax = 17f;
    public float re = 3f;
    public float giro = 95f;              // graus por segundo na velocidade plena
    public float aderencia = 7f;
    public float aderenciaFreioMao = 1.2f;
    public float freioBatida = 0.35f;     // fração da velocidade que sobra ao bater num perigo
    public AudioSource motor;

    public float Velocidade { get; private set; }

    private Rigidbody rb;
    private float gas, esterco;
    private bool freioMao;

    /// Finalidade: montar o Nilo e preparar a física. Entradas: nenhuma. Retorno: void.
    /// Corpo: trava inclinação física (só o visual inclina); o Nilo vira filho do assento, sem física
    /// nem colisor, e perde a tag Player para a moto; a câmera passa a seguir a moto pelas costas.
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.centerOfMass = new Vector3(0f, -0.4f, 0f);
        if (nilo != null)
        {
            var nrb = nilo.GetComponent<Rigidbody>();
            nrb.isKinematic = true; nrb.interpolation = RigidbodyInterpolation.None;
            nilo.GetComponent<Collider>().enabled = false;
            nilo.enabled = false;
            nilo.tag = "Untagged";
            nilo.transform.SetParent(assento, false);
            nilo.transform.localPosition = Vector3.zero; nilo.transform.localRotation = Quaternion.identity;
            if (nilo.visual != null) nilo.visual.localRotation = Quaternion.identity;
        }
        var cam = Camera.main != null ? Camera.main.GetComponent<FollowCamera>() : null;
        if (cam != null) { cam.target = transform; cam.seguirRumo = true; cam.distance = 6.5f; cam.height = 1.4f; }
    }

    /// Finalidade: ler o teclado. Entradas: nenhuma. Retorno: void. Corpo: sem partida em andamento, solta tudo.
    void Update()
    {
        var kb = Keyboard.current; var gm = GameManager.Instance;
        bool ok = kb != null && gm != null && gm.CurrentState == GameManager.State.Playing;
        gas = ok ? (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f) : 0f;
        esterco = ok ? (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f) : 0f;
        freioMao = ok && kb.spaceKey.isPressed;
        if (motor != null) motor.pitch = 0.7f + Mathf.Abs(Velocidade) / velocidadeMax;
    }

    /// Finalidade: mover a moto no passo de física. Entradas: nenhuma. Retorno: void.
    /// Corpo: velocidade para frente segue o acelerador; o esterço gira mais quanto mais rápido;
    /// a aderência apaga o deslize lateral (pouco com freio de mão); a gravidade é preservada.
    void FixedUpdate()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.CurrentState != GameManager.State.Playing)
        {
            Velocidade = 0f;
            rb.linearVelocity = new Vector3(0f, Mathf.Min(rb.linearVelocity.y, 0f), 0f);
            return;
        }
        float dt = Time.fixedDeltaTime;
        Vector3 v = rb.linearVelocity;
        float fwd = Vector3.Dot(v, transform.forward);
        float alvo = gas > 0f ? velocidadeMax : gas < 0f ? (fwd > 0.5f ? 0f : -re) : fwd * 0.98f;
        float taxa = gas < 0f && fwd > 0.5f ? aceleracao * 2f : aceleracao;
        fwd = Mathf.MoveTowards(fwd, alvo, taxa * dt);
        if (freioMao) fwd *= 0.97f;
        float fator = Mathf.Clamp01(Mathf.Abs(fwd) / 5f) * Mathf.Sign(fwd);
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, esterco * giro * fator * dt * (freioMao ? 1.5f : 1f), 0f));
        float lat = Vector3.Dot(v, transform.right);
        lat = Mathf.Lerp(lat, 0f, Mathf.Clamp01((freioMao ? aderenciaFreioMao : aderencia) * dt));
        rb.linearVelocity = transform.forward * fwd + transform.right * lat + Vector3.up * v.y;
        Velocidade = fwd;
        if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, -esterco * 20f * Mathf.Abs(fator));
        if (rodas != null) foreach (var r in rodas) r.Rotate(fwd * dt * 160f, 0f, 0f, Space.Self);
    }

    /// Finalidade: perder velocidade ao bater num perigo. Entrada: c. Retorno: void.
    /// Corpo: o coração já sai pelo Hazard (a moto tem a tag Player); aqui só freia e dá um tranco para cima.
    void OnTriggerEnter(Collider c)
    {
        if (c.GetComponentInParent<Hazard>() == null) return;
        var v = rb.linearVelocity;
        rb.linearVelocity = v * freioBatida + Vector3.up * 2f;
    }
}
