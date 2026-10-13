using UnityEngine;

// Detritos visuais da caixa de ferramentas: sorteia prefabs de um vetor,
// aplica força e torque pequenos e descarta as instâncias depois de alguns segundos.
public class DebrisSpawner : MonoBehaviour
{
    public GameObject[] debrisPrefabs;
    public int burstCount = 6;
    public float force = 2.5f;
    public float torque = 1.5f;
    public float lifetime = 4f;
    public ParticleSystem dustFx;
    public AudioSource sfx;

    /// Finalidade: disparar os detritos a partir do próprio objeto (usado no evento da estação).
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: chama SpawnDebris com a posição deste objeto e burstCount.
    public void Burst()
    {
        SpawnDebris(transform.position, burstCount);
    }

    /// Finalidade: criar detritos visuais com física.
    /// Entradas: origin, ponto de saída; count, quantidade de detritos. Retorno: void.
    /// Corpo: valida o vetor, toca poeira e som, e para cada detrito sorteia um índice válido,
    /// instancia o prefab, aplica AddForce para cima e AddTorque aleatório e agenda o descarte.
    /// Os prefabs ficam na camada Debris, que não colide com o Nilo.
    public void SpawnDebris(Vector3 origin, int count)
    {
        if (debrisPrefabs == null || debrisPrefabs.Length == 0)
        {
            Debug.LogWarning("DebrisSpawner: vetor de prefabs vazio.", this);
            return;
        }
        if (dustFx != null) dustFx.Play();
        if (sfx != null) sfx.Play();

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
            GameObject debris = Instantiate(prefab, origin + Random.insideUnitSphere * 0.15f, Random.rotation);
            var rb = debris.GetComponent<Rigidbody>();
            if (rb != null)
            {
                var dir = new Vector3(Random.Range(-1f, 1f), Random.Range(1.2f, 2f), Random.Range(-0.6f, 0.3f)).normalized;
                rb.AddForce(dir * force, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * torque, ForceMode.Impulse);
            }
            Destroy(debris, lifetime);
        }
    }
}
