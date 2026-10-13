using UnityEngine;

// Tronco preso a um poste que gira varrendo a passagem: perigo de tempo em terceira pessoa.
// O objeto gira inteiro (poste no centro, tronco deslocado); a zona de dano é um filho com Hazard.
[RequireComponent(typeof(Rigidbody))]
public class TroncoGiratorio : MonoBehaviour
{
    public float degreesPerSecond = 70f;
    public AudioSource cue;   // aviso a cada volta completa

    private Rigidbody trunkRb;
    private float accumulated;

    /// Finalidade: preparar o Rigidbody cinemático. Entradas: nenhuma. Retorno: void.
    /// Corpo: marca o corpo como cinemático e liga a interpolação, para o trigger acompanhar a física.
    void Awake()
    {
        trunkRb = GetComponent<Rigidbody>();
        trunkRb.isKinematic = true;
        trunkRb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    /// Finalidade: girar o tronco em velocidade constante.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: aplica a rotação do passo com MoveRotation (compatível com triggers) e toca o
    /// aviso uma vez por volta. Usa o tempo de física, então para na pausa.
    void FixedUpdate()
    {
        float step = degreesPerSecond * Time.fixedDeltaTime;
        trunkRb.MoveRotation(trunkRb.rotation * Quaternion.Euler(0f, step, 0f));
        accumulated += step;
        if (accumulated >= 360f)
        {
            accumulated -= 360f;
            if (cue != null) cue.Play();
        }
    }
}
