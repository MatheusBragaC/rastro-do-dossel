using UnityEngine;

// Holofote da Vigília (nível 3): gira de um lado para o outro varrendo o corredor. A área iluminada no
// chão é um filho com Hazard: quem for pego pela luz perde um coração. O ciclo é fixo e previsível.
[RequireComponent(typeof(Rigidbody))]
public class Varredura : MonoBehaviour
{
    public float amplitude = 50f;   // graus para cada lado
    public float periodo = 6f;      // segundos para ir e voltar
    public float defasagem;         // segundos de adiantamento (holofotes vizinhos fora de fase)

    private Rigidbody corpo;
    private Quaternion inicial;
    private float t;

    /// Finalidade: preparar o corpo cinemático e guardar a direção central. Entradas: nenhuma. Retorno: void.
    void Awake()
    {
        corpo = GetComponent<Rigidbody>();
        corpo.isKinematic = true;
        corpo.interpolation = RigidbodyInterpolation.Interpolate;
        inicial = corpo.rotation;
        t = defasagem;
    }

    /// Finalidade: girar o holofote no passo de física. Entradas: nenhuma. Retorno: void.
    /// Corpo: ângulo senoidal em torno da direção central, aplicado com MoveRotation (o trigger acompanha a física).
    void FixedUpdate()
    {
        if (!GameManager.Jogando) return;   // parado em cutscene, falha e conclusão
        t += Time.fixedDeltaTime;
        float angulo = Mathf.Sin(t * Mathf.PI * 2f / periodo) * amplitude;
        corpo.MoveRotation(inicial * Quaternion.Euler(0f, angulo, 0f));
    }
}
