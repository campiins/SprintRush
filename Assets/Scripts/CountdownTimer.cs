using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class CountdownTimer : MonoBehaviour
{

    [SerializeField] TMP_Text countdownText;
    [SerializeField] AudioSource horn1;
    [SerializeField] AudioSource horn2;

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(Countdown());
    }


    public IEnumerator Countdown(Action onCountdownFinished)
    {
        yield return new WaitForSeconds(0.5f);
        countdownText.text = "3";
        horn1.Play();
        countdownText.gameObject.SetActive(true);

        yield return new WaitForSeconds(1f);
        countdownText.gameObject.SetActive(false);
        countdownText.text = "2";
        horn1.Play();
        countdownText.gameObject.SetActive(true);

        yield return new WaitForSeconds(1f);
        countdownText.gameObject.SetActive(false);
        countdownText.text = "1";
        horn1.Play();
        countdownText.gameObject.SetActive(true);

        yield return new WaitForSeconds(1f);
        countdownText.gameObject.SetActive(false);
        countdownText.text = "GO!";
        horn2.Play();
        countdownText.gameObject.SetActive(true);
        onCountdownFinished?.Invoke();

        yield return new WaitForSeconds(1f);
        countdownText.gameObject.SetActive(false);
    }

}
