using UnityEngine;

// Câmera lateral que acompanha o Nilo com suavização e limites por fase.
public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(2f, 1.5f, -15f);
    public float smoothTime = 0.15f;
    public float minX = -1000f;
    public float maxX = 1000f;

    private Vector3 velocity;

    /// Finalidade: acompanhar o Nilo depois que sua posição foi atualizada.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: calcula o alvo com deslocamento (à frente, para mostrar o próximo pouso),
    /// limita X à área da fase e aplica SmoothDamp. Não altera o personagem.
    void LateUpdate()
    {
        if (target == null) return;
        Vector3 goal = target.position + offset;
        goal.x = Mathf.Clamp(goal.x, minX, maxX);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}
