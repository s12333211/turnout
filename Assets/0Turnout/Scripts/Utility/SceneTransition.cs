using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : SingletonMonoBehaviour<SceneTransition>
{
    [SerializeField] private Fade transition;
    
    public void ChangeScene(string sceneName) {

        // トランジション表示
        transition.Show(() => {

            // シーンを同期で読み込み
            SceneManager.LoadScene(sceneName);

            // トランジション非表示
            transition.Hide();
        });
    }
}
