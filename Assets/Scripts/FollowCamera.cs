using UnityEngine;
using UnityEngine.InputSystem;

// Câmera em terceira pessoa: fica atrás e um pouco acima do Nilo, gira com o mouse
// (ou com as setas) e encurta a distância quando algo sólido fica entre ela e o personagem.
public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public float height = 1.5f;          // ponto que a câmera olha, acima dos pés
    public float distance = 4.5f;
    public float pitch = 14f;            // inclinação para baixo, em graus
    public float mouseSensitivity = 0.12f;
    public float keyTurnSpeed = 120f;    // graus por segundo com as setas
    public float smoothTime = 0.08f;
    public LayerMask obstacleMask;       // o que a câmera não atravessa (chão e rochas)

    private float yaw;
    private Vector3 velocity;

    /// Finalidade: começar com a orientação atual da câmera. Entradas: nenhuma. Retorno: void.
    /// Corpo: usa Awake (e não Start) porque a cutscene de abertura desliga o componente antes do primeiro quadro.
    void Awake()
    {
        yaw = transform.eulerAngles.y;
    }

    /// Finalidade: posicionar a câmera depois que o Nilo se moveu.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: com a partida em andamento, soma o movimento do mouse (só com o cursor travado)
    /// e as setas ao ângulo horizontal; calcula a posição atrás do alvo, encurta a distância se
    /// um SphereCast encontrar obstáculo, suaviza com SmoothDamp e olha para o alvo.
    void LateUpdate()
    {
        if (target == null) return;

        var gm = GameManager.Instance;
        bool playing = gm == null || gm.CurrentState == GameManager.State.Playing;
        if (playing)
        {
            var mouse = Mouse.current;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
                yaw += mouse.delta.ReadValue().x * mouseSensitivity;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.isPressed) yaw -= keyTurnSpeed * Time.deltaTime;
                if (kb.rightArrowKey.isPressed) yaw += keyTurnSpeed * Time.deltaTime;
            }
        }

        Vector3 goal; Quaternion rotGoal;
        CalcularPose(out goal, out rotGoal);
        Vector3 pivot = target.position + Vector3.up * height;
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
        transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
    }

    /// Finalidade: calcular onde a câmera do jogo ficaria agora (usado também pelas cutscenes para voltar suave).
    /// Entradas: nenhuma. Saídas: pos e rot. Retorno: void.
    /// Corpo: ponto atrás e acima do Nilo pelo ângulo atual, encurtado se um SphereCast encontrar obstáculo.
    public void CalcularPose(out Vector3 pos, out Quaternion rot)
    {
        Vector3 pivot = target.position + Vector3.up * height;
        Vector3 back = Quaternion.Euler(pitch, yaw, 0f) * Vector3.back;
        float dist = distance;
        RaycastHit hit;
        if (Physics.SphereCast(pivot, 0.25f, back, out hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
            dist = Mathf.Max(hit.distance - 0.1f, 0.6f);
        pos = pivot + back * dist;
        rot = Quaternion.LookRotation(pivot - pos, Vector3.up);
    }

    /// Finalidade: ao voltar de uma cutscene, não herdar a velocidade antiga da suavização. Retorno: void.
    void OnEnable()
    {
        velocity = Vector3.zero;
    }
}
