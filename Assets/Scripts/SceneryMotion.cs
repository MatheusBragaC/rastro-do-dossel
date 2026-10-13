using UnityEngine;

// Sinalizador do posto: desliza num cabo com transform.Translate e gira uma peça visual.
// Não usar em objetos com Rigidbody dinâmico.
public class SceneryMotion : MonoBehaviour
{
    public Vector3 direction = Vector3.right;
    public float speed = 1f;
    public float distance = 3f;
    public Transform spinner;
    public Vector3 spinAxis = Vector3.up;
    public float spinSpeed = 180f;

    private float travelled;
    private int sign = 1;

    /// Finalidade: demonstrar deslocamento por Translate e rotação num objeto secundário.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: move o objeto na direção configurada até percorrer distance, inverte o sentido
    /// nas extremidades e gira a peça spinner. Usa Time.deltaTime, então para na pausa.
    void Update()
    {
        float step = speed * Time.deltaTime;
        transform.Translate(direction.normalized * step * sign, Space.World);
        travelled += step * sign;
        if (travelled >= distance) sign = -1;
        else if (travelled <= 0f) sign = 1;

        if (spinner != null) spinner.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);
    }
}
