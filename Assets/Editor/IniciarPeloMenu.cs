using UnityEditor;
using UnityEditor.SceneManagement;

// No Editor, o Play sempre começa pela cena do Menu, como na build,
// qualquer que seja a cena aberta. Só afeta o Editor; não entra no jogo compilado.
[InitializeOnLoad]
public static class IniciarPeloMenu
{
    private const string MenuPath = "Assets/Scenes/Menu.unity";

    /// Finalidade: definir a cena inicial do modo Play ao carregar o Editor.
    /// Entradas: nenhuma. Retorno: nenhum (construtor estático).
    /// Corpo: carrega a cena do Menu e a registra em EditorSceneManager.playModeStartScene;
    /// se a cena não existir, mantém o comportamento padrão do Unity.
    static IniciarPeloMenu()
    {
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuPath);
    }
}
