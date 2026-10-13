using UnityEngine;
using UnityEngine.InputSystem;

// Controle do Nilo: caminhar, correr, saltar e deslizar num plano lateral (eixo X).
// Pivô do objeto fica nos pés; o colisor é uma cápsula em pé de standHeight.
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Deslocamento")]
    public float walkSpeed = 3f;
    public float runSpeed = 5f;

    [Header("Salto")]
    public float jumpSpeed = 6f;
    public float coyoteTime = 0.1f;   // tolerância após sair da borda
    public float jumpBuffer = 0.1f;   // tolerância para Espaço apertado pouco antes do pouso
    public LayerMask groundMask;

    [Header("Deslize")]
    public float slideSpeed = 5.5f;
    public float slideDuration = 0.55f;
    public float slideHeight = 0.9f;
    public LayerMask headroomMask = ~0;   // o que impede levantar; excluir detritos decorativos

    [Header("Visual")]
    public Transform visual;          // filho com o modelo; gira para o lado do movimento
    public float turnSpeed = 720f;

    public bool controlEnabled = true;
    public bool IsGrounded { get; private set; }
    public bool IsSliding { get; private set; }
    public bool IsCrouched { get; private set; }
    public int Facing { get; private set; } = 1;

    private Rigidbody playerRb;
    private CapsuleCollider capsule;
    private float standHeight;
    private Vector3 standCenter;
    private float moveInput;
    private bool runHeld;
    private bool slideRequested;
    private float jumpRequestTime = float.NegativeInfinity;
    private float lastGroundedTime = float.NegativeInfinity;
    private float slideTimer;

    /// Finalidade: obter componentes e guardar as dimensões originais do colisor.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: busca Rigidbody e CapsuleCollider, trava rotação e profundidade (Z),
    /// liga interpolação e avisa no Console se a máscara de chão não foi configurada.
    void Awake()
    {
        playerRb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        standHeight = capsule.height;
        standCenter = capsule.center;
        playerRb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        playerRb.interpolation = RigidbodyInterpolation.Interpolate;
        if (groundMask == 0)
            Debug.LogWarning("PlayerController: groundMask vazio, o salto nunca será liberado.", this);
    }

    /// Finalidade: ler o teclado e registrar pedidos de movimento, corrida, salto e deslize.
    /// Entradas: nenhuma (usa Keyboard.current do Input System). Retorno: void.
    /// Corpo: lê A/D ou setas, Shift, Espaço e Ctrl/S; salto e deslize são guardados
    /// como pedidos para o FixedUpdate consumir uma única vez. Também gira o visual.
    void Update()
    {
        var kb = Keyboard.current;
        if (!controlEnabled || kb == null)
        {
            moveInput = 0f;
            runHeld = false;
            return;
        }

        float right = (kb.dKey.isPressed || kb.rightArrowKey.isPressed) ? 1f : 0f;
        float left = (kb.aKey.isPressed || kb.leftArrowKey.isPressed) ? 1f : 0f;
        moveInput = right - left;
        runHeld = kb.leftShiftKey.isPressed;

        if (kb.spaceKey.wasPressedThisFrame) jumpRequestTime = Time.time;
        if (kb.leftCtrlKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) slideRequested = true;

        if (moveInput != 0f && !IsSliding) Facing = moveInput > 0f ? 1 : -1;
        if (visual != null)
        {
            var goal = Quaternion.Euler(0f, Facing > 0 ? 90f : 270f, 0f);
            visual.localRotation = Quaternion.RotateTowards(visual.localRotation, goal, turnSpeed * Time.deltaTime);
        }
    }

    /// Finalidade: atualizar a movimentação física no passo de física.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: consulta o chão, processa o deslize, define a velocidade horizontal
    /// do Rigidbody e consome o pedido de salto válido. Não há física no Update.
    void FixedUpdate()
    {
        IsGrounded = CheckGround();
        if (IsGrounded) lastGroundedTime = Time.time;

        if (slideRequested)
        {
            slideRequested = false;
            TrySlide();
        }
        UpdateSlide(Time.fixedDeltaTime);

        float speed = (runHeld && !IsCrouched) ? runSpeed : walkSpeed;
        float vx = IsSliding ? Facing * slideSpeed : moveInput * speed;
        var v = playerRb.linearVelocity;
        v.x = vx;
        playerRb.linearVelocity = v;

        if (Time.time - jumpRequestTime <= jumpBuffer) TryJump();
    }

    /// Finalidade: verificar apoio válido sob os pés.
    /// Entradas: nenhuma (usa posição, raio da cápsula e groundMask). Retorno: bool.
    /// Corpo: lança uma esfera curta para baixo a partir da base da cápsula e aceita
    /// apenas superfícies quase horizontais, para que encostar de lado numa parede
    /// não conte como chão. Subindo (após o salto) também não conta.
    public bool CheckGround()
    {
        if (playerRb.linearVelocity.y > 0.1f) return false;
        float r = capsule.radius * 0.9f;
        Vector3 origin = transform.position + Vector3.up * (capsule.radius + 0.05f);
        RaycastHit hit;
        if (!Physics.SphereCast(origin, r, Vector3.down, out hit, capsule.radius - r + 0.15f, groundMask, QueryTriggerInteraction.Ignore))
            return false;
        return hit.normal.y > 0.7f;
    }

    /// Finalidade: iniciar um salto quando permitido.
    /// Entradas: nenhuma. Retorno: bool indicando se o pedido foi aceito.
    /// Corpo: recusa sem controle, abaixado ou fora do tempo de apoio; zera a velocidade
    /// vertical e aplica AddForce com VelocityChange, dando a mesma altura sempre,
    /// independentemente da massa e de quanto tempo Espaço fica pressionado.
    public bool TryJump()
    {
        if (!controlEnabled || IsCrouched || Time.time - lastGroundedTime > coyoteTime) return false;
        jumpRequestTime = float.NegativeInfinity;
        lastGroundedTime = float.NegativeInfinity;
        var v = playerRb.linearVelocity;
        v.y = 0f;
        playerRb.linearVelocity = v;
        playerRb.AddForce(Vector3.up * jumpSpeed, ForceMode.VelocityChange);
        IsGrounded = false;
        return true;
    }

    /// Finalidade: iniciar o deslize.
    /// Entradas: nenhuma. Retorno: bool indicando se o deslize começou.
    /// Corpo: exige chão e que o personagem não esteja abaixado; fixa a direção atual,
    /// reduz a cápsula e inicia o temporizador. Repetir o comando não prolonga a ação.
    public bool TrySlide()
    {
        if (!controlEnabled || IsCrouched || !IsGrounded) return false;
        IsSliding = true;
        slideTimer = slideDuration;
        SetCrouched(true);
        return true;
    }

    /// Finalidade: controlar a duração e a saída do deslize.
    /// Entrada: deltaTime, tempo do passo de física. Retorno: void.
    /// Corpo: reduz o tempo restante; ao terminar, só restaura a cápsula se houver espaço
    /// acima. Sem espaço, mantém a postura baixa e permite caminhar até sair da passagem.
    public void UpdateSlide(float deltaTime)
    {
        if (IsSliding)
        {
            slideTimer -= deltaTime;
            if (slideTimer <= 0f) IsSliding = false;
        }
        if (!IsSliding && IsCrouched && HasHeadroom()) SetCrouched(false);
    }

    /// Finalidade: saber se a cápsula em pé caberia na posição atual.
    /// Entradas: nenhuma. Retorno: bool (true se não há obstáculo acima).
    /// Corpo: testa uma cápsula do tamanho original contra headroomMask, exceto a camada do
    /// próprio jogador, ignorando triggers.
    private bool HasHeadroom()
    {
        float r = capsule.radius * 0.95f;
        Vector3 bottom = transform.position + Vector3.up * (capsule.radius + 0.1f);
        Vector3 top = transform.position + Vector3.up * (standHeight - capsule.radius);
        int mask = headroomMask & ~(1 << gameObject.layer);
        return !Physics.CheckCapsule(bottom, top, r, mask, QueryTriggerInteraction.Ignore);
    }

    /// Finalidade: alternar entre cápsula em pé e abaixada.
    /// Entrada: crouched, a postura desejada. Retorno: void.
    /// Corpo: ajusta altura e centro mantendo a base nos pés e achata o visual na mesma proporção.
    private void SetCrouched(bool crouched)
    {
        IsCrouched = crouched;
        float h = crouched ? slideHeight : standHeight;
        capsule.height = h;
        capsule.center = new Vector3(standCenter.x, standCenter.y - (standHeight - h) * 0.5f, standCenter.z);
        if (visual != null) visual.localScale = new Vector3(1f, h / standHeight, 1f);
    }
}
