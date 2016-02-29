using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameTimer : MonoBehaviour {

    //Scene Management
  
   private string scene="Scenes/Navigational Menus/Options_Menu";

    [SerializeField]
    private float timeLeft = 60f;
    public bool stop = true;

    private float minutes;
    private float seconds;

    public Text text;

    void Start()
    {
        startTimer(timeLeft);
    }

    public void startTimer(float from)
    {
        stop = false;
        timeLeft = from;
        Update();
        StartCoroutine(updateCoroutine());
    }

    void Update()
    {
        if (stop) return;
        timeLeft -= Time.deltaTime;

        minutes = Mathf.Floor(timeLeft / 60);
        seconds = timeLeft % 60;
        if (seconds > 59) seconds = 59;
        if (minutes < 0)
        {
            stop = true;
            minutes = 0;
            seconds = 0;
            SceneManager.LoadScene(1);
        }
        else if(Input.GetKeyDown(KeyCode.Escape))
            SceneManager.LoadScene(1);
    }

    private IEnumerator updateCoroutine()
    {
        while (!stop)
        {
            text.text = string.Format("{0:0}:{1:00}", minutes, seconds);
            yield return new WaitForSeconds(0.2f);
        }
    }

}
