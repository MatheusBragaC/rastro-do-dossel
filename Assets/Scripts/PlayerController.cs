using UnityEngine;
using UnityEngine.InputSystem;

// Controle do Nilo em terceira pessoa: caminhar, correr, saltar e deslizar em qualquer direção do plano.
// O movimento é relativo à câmera (W afasta da câmera, A/D andam de lado).
// Pivô do objeto fica nos pés; o colisor é uma cápsula em pé de standHeight.
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Deslocamento")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6f;
    public Transform cameraTransform;   // referência de direção; vazio usa Camera.main

    [Header("Salto")]
    public float jumpSpeed = 6f;
    public float coyoteTime = 0.1f;   // tolerância após sair da borda
    public float jumpBuffer = 0.1f;   // tolerância para Espaço apertado pouco antes do pouso
    public LayerMask groundMask;

    [Header("Deslize")]
    public float slideSpeed = 7f;
    public float slideDuration = 0.55f;
    public float slideHeight = 0.9f;
    public LayerMask headroomMask = ~0;   // o que impede levantar; excluir detritos decorativos

    [Header("Visual")]
    public Transform visual;          // filho com o modelo; gira para a direção do movimento
    public float turnSpeed = 720f;

    [Header("Dano")]
    public float knockbackSpeed = 6f;   // empurrão ao ser atingido
    public float knockbackTime = 0.35f; // tempo sem controle horizontal após o empurrão

    public bool controlEnabled = true;
    public bool IsGrounded { get; private set; }
    public bool IsSliding { get; private set; }
    public bool IsCrouched { get; private set; }
    public Vector3 Facing { get; private set; } = Vector3.forward;   // direção horizontal para onde olha
    public Vector3 MoveDir { get; private set; }                      // direção pedida pelo teclado (mundo)

    private Rigidbody playerRb;
    private CapsuleCollider capsule;
    private float standHeight;
    private Vector3 standCenter;
    private bool runHeld;
    private bool slideRequested;
    private float jumpRequestTime = float.NegativeInfinity;
    private float lastGroundedTime = float.NegativeInfinity;
    private float slideTimer;
    private float knockbackTimer;

    /// Finalidade: obter componentes e guardar as dimensões originais do colisor.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: busca Rigidbody e CapsuleCollider, trava só a rotação física (o Nilo anda em X e Z),
    /// liga interpolação e avisa no Console se a máscara de chão não foi configurada.
    void Awake()
    {
        playerRb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        standHeight = capsule.height;
        standCenter = capsule.center;
        playerRb.constraints = RigidbodyConstraints.FreezeRotation;
        playerRb.interpolation = RigidbodyInterpolation.Interpolate;
        if (groundMask == 0)
            Debug.LogWarning("PlayerController: groundMask vazio, o salto nunca será liberado.", this);
    }

    /// Finalidade: ler o teclado e registrar pedidos de movimento, corrida, salto e deslize.
    /// Entradas: nenhuma (usa Keyboard.current do Input System). Retorno: void.
    /// Corpo: lê W/A/S/D, Shift, Espaço e Ctrl; converte a entrada para o mundo usando a
    /// direção horizontal da câmera; salto e deslize são guardados como pedidos para o
    /// FixedUpdate consumir uma única vez. Também gira o visual para a direção do movimento.
    void Update()
    {
        var kb = Keyboard.current;
        if (!controlEnabled || kb == null)
        {
            MoveDir = Vector3.zero;
            runHeld = false;
            return;
        }

        float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float z = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
        runHeld = kb.leftShiftKey.isPressed;

        if (kb.spaceKey.wasPressedThisFrame) jumpRequestTime = Time.time;
        if (kb.leftCtrlKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame) slideRequested = true;

        Transform cam = cameraTransform != null ? cameraTransform : (Camera.main != null ? Camera.main.transform : null);
        Vector3 forward = cam != null ? cam.forward : Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 dir = forward * z + right * x;
        MoveDir = dir.sqrMagnitude > 1f ? dir.normalized : dir;

        if (MoveDir.sqrMagnitude > 0.01f && !IsSliding) Facing = MoveDir.normalized;
        if (visual != null)
        {
            var goal = Quaternion.LookRotation(Facing, Vector3.up);
            visual.rotation = Quaternion.RotateTowards(visual.rotation, goal, turnSpeed * Time.deltaTime);
        }
    }

    /// Finalidade: atualizar a movimentação física no passo de física.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: consulta o chão, processa o deslize, define a velocidade horizontal do Rigidbody
    /// (exceto durante o empurrão de dano, em que a física segue livre) e consome o pedido de
    /// salto válido. Não há física no Update.
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

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
        }
        else
        {
            float speed = (runHeld && !IsCrouched) ? runSpeed : walkSpeed;
            Vector3 horizontal = IsSliding ? Facing * slideSpeed : MoveDir * speed;
            var v = playerRb.linearVelocity;
            v.x = horizontal.x;
            v.z = horizontal.z;
            playerRb.linearVelocity = v;
        }

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

    /// Finalidade: empurrar o Nilo para longe de um perigo que o atingiu.
    /// Entrada: direction, direção horizontal do empurrão. Retorno: void.
    /// Corpo: interrompe o deslize, aplica velocidade para trás e para cima e bloqueia o
    /// controle horizontal por knockbackTime, para o impacto ser percebido.
    public void Knockback(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) direction = -Facing;
        IsSliding = false;
        knockbackTimer = knockbackTime;
        playerRb.linearVelocity = direction.normalized * knockbackSpeed + Vector3.up * 3f;
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
