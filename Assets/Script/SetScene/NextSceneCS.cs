using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextSceneCS : MonoBehaviour
{
    public string nextSceneName;

    // // Start is called before the first frame update
    // void Start()
    // {
        
    // }

    // // Update is called once per frame
    // void Update()
    // {
        
    // }

    void OnTriggerEnter2D(Collider2D other){
        if(other.CompareTag("Player")){
            // บันทึกชื่อฉากใหม่ลงใน DB ความคืบหน้าของเกม
            GameProgressManagerCS.SaveCurrentScene(nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        }
    }


}
