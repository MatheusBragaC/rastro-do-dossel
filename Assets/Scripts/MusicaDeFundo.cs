using UnityEngine;

// Música de fundo em loop. Sobrevive à troca de cena (Menu -> nível) sem reiniciar
// e abaixa o volume enquanto uma fala, aviso ou cutscene está na tela.
[RequireComponent(typeof(AudioSource))]
public class MusicaDeFundo : MonoBehaviour
{
    public float volume = 0.35f;
    public float volumeDuranteFala = 0.15f;
    public float suavizacao = 1.5f;     // velocidade do ajuste de volume (por segundo)

    private static MusicaDeFundo instancia;
    private AudioSource fonte;

    /// Finalidade: garantir uma única música tocando entre as cenas.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: se já existe uma instância, destrói esta; senão marca para não ser destruída
    /// na troca de cena, configura o AudioSource em loop e começa do volume zero.
    void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        instancia = this;
        DontDestroyOnLoad(gameObject);
        fonte = GetComponent<AudioSource>();
        fonte.loop = true;
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f;
        fonte.volume = 0f;
        if (!fonte.isPlaying) fonte.Play();
    }

    /// Finalidade: aproximar o volume do alvo a cada quadro.
    /// Entradas: nenhuma. Retorno: void.
    /// Corpo: usa o volume baixo quando a caixa de legenda do GameManager está visível;
    /// o ajuste é gradual e usa tempo real, para continuar suave durante a pausa.
    void Update()
    {
        var gm = GameManager.Instance;
        bool fala = gm != null && ((gm.messageBox != null && gm.messageBox.activeInHierarchy) || gm.CurrentState == GameManager.State.Cutscene);
        float alvo = fala ? volumeDuranteFala : volume;
        fonte.volume = Mathf.MoveTowards(fonte.volume, alvo, suavizacao * Time.unscaledDeltaTime);
    }
}
