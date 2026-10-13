using UnityEngine;

// Resposta audiovisual do Nilo: parâmetros do Animator, sons e partículas.
// Só lê o estado do PlayerController; não altera a física.
[RequireComponent(typeof(PlayerController))]
public class PlayerFeedback : MonoBehaviour
{
    public Animator animator;
    public AudioSource audioSource;
    public AudioClip jumpClip;
    public AudioClip landClip;
    public AudioClip slideClip;
    public AudioClip stepClip;
    public AudioClip failClip;
    public ParticleSystem landDust;
    public ParticleSystem slideLeaves;
    public ParticleSystem failBurst;
    public float walkStepInterval = 0.42f;
    public float runStepInterval = 0.3f;
    public float interactDuration = 0.7f;   // igual ao clipe de interação

    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int GroundedId = Animator.StringToHash("Grounded");
    private static readonly int CrouchedId = Animator.StringToHash("Crouched");
    private static readonly int VerticalSpeedId = Animator.StringToHash("VerticalSpeed");
    private static readonly int FailedId = Animator.StringToHash("Failed");
    private static readonly int InteractingId = Animator.StringToHash("Interacting");

    private PlayerController player;
    private Rigidbody playerRb;
    private Renderer[] renderers;
    private bool wasGrounded = true;
    private bool wasSliding;
    private bool failed;
    private float stepTimer;
    private float interactTimer;
    private float hurtTimer;

    /// Finalidade: obter o controlador, o Rigidbody e os renderers do Nilo. Entradas: nenhuma. Retorno: void.
    void Awake()
    {
        player = GetComponent<PlayerController>();
        playerRb = GetComponent<Rigidbody>();
        renderers = player.visual != null ? player.visual.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
    }

    /// Finalidade: atualizar animação e efeitos a partir do estado atual.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: envia velocidade horizontal, chão, postura, velocidade vertical e interação ao Animator;
    /// dispara som e partícula só na mudança de estado (salto, pouso, início do deslize), nunca a cada
    /// quadro; toca passos em intervalos que dependem da velocidade; pisca o modelo após um golpe.
    void Update()
    {
        if (failed) return;

        Vector3 v = playerRb.linearVelocity;
        float speedX = new Vector2(v.x, v.z).magnitude;
        bool grounded = player.IsGrounded;
        if (interactTimer > 0f) interactTimer -= Time.deltaTime;
        if (hurtTimer > 0f)
        {
            hurtTimer -= Time.deltaTime;
            bool visible = hurtTimer <= 0f || ((int)(hurtTimer * 12f)) % 2 == 0;
            foreach (var r in renderers) r.enabled = visible;
        }

        if (animator != null)
        {
            animator.SetBool(InteractingId, interactTimer > 0f);
            animator.SetFloat(SpeedId, speedX);
            animator.SetBool(GroundedId, grounded);
            animator.SetBool(CrouchedId, player.IsCrouched);
            animator.SetFloat(VerticalSpeedId, v.y);
        }

        if (grounded && !wasGrounded)
        {
            Play(landClip, 0.8f);
            if (landDust != null) landDust.Play();
        }
        if (!grounded && wasGrounded && v.y > 0.1f) Play(jumpClip, 0.8f);
        if (player.IsSliding && !wasSliding)
        {
            Play(slideClip, 0.8f);
            if (slideLeaves != null) slideLeaves.Play();
        }

        if (grounded && !player.IsCrouched && speedX > 0.1f)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                Play(stepClip, 0.35f);
                stepTimer = speedX > player.walkSpeed + 0.5f ? runStepInterval : walkStepInterval;
            }
        }
        else stepTimer = 0f;

        wasGrounded = grounded;
        wasSliding = player.IsSliding;
    }

    /// Finalidade: tocar a animação de interação (estação, terminal), chamada pelo Interactable.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: mantém o parâmetro Interacting ligado por interactDuration; ignorado após a falha.
    public void PlayInteract()
    {
        if (!failed) interactTimer = interactDuration;
    }

    /// Finalidade: resposta a um golpe não fatal, chamada pelo GameManager.
    /// Entrada: duration, segundos de invulnerabilidade (tempo do piscar). Retorno: void.
    /// Corpo: toca o som de impacto mais baixo, a partícula de impacto e inicia o piscar do modelo.
    public void PlayHurt(float duration)
    {
        if (failed) return;
        Play(failClip, 0.5f);
        if (failBurst != null) failBurst.Play();
        hurtTimer = duration;
    }

    /// Finalidade: resposta de falha do Nilo, chamada pelo GameManager.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: executa uma única vez: toca o som, a partícula de impacto e a animação de falha.
    public void PlayFail()
    {
        if (failed) return;
        failed = true;
        hurtTimer = 0f;
        foreach (var r in renderers) r.enabled = true;
        Play(failClip, 1f);
        if (failBurst != null) failBurst.Play();
        if (animator != null) animator.SetBool(FailedId, true);
    }

    /// Finalidade: tocar um efeito sonoro curto. Entradas: clip e volume. Retorno: void.
    /// Corpo: ignora clipe ou fonte ausentes e usa PlayOneShot para permitir sobreposição.
    private void Play(AudioClip clip, float volume)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip, volume);
    }
}
