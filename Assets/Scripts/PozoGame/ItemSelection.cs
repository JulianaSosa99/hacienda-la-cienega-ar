using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ItemSelection : MonoBehaviour
{
    public bool isCorrect;
    public GameObject auraCorrect;
    public Button button;
    public PozoGameManager gameManager;

    public void OnClickItem()
    {
        if (isCorrect)
        {
            button.interactable = false;
            gameManager.RegisterCorrect();
        }
        else
        {
            gameManager.RegisterWrong();
            StartCoroutine(Shake());
        }
    }

    IEnumerator Shake()
    {
        Vector3 originalPos = transform.localPosition;

        for (int i = 0; i < 10; i++)
        {
            transform.localPosition = originalPos + new Vector3(UnityEngine.Random.Range(-5f, 5f), 0, 0);
            yield return new WaitForSeconds(0.02f);
        }

        transform.localPosition = originalPos;
    }

    public void ShowHint()
    {
        if (isCorrect && auraCorrect != null)
        {
            auraCorrect.transform.position = transform.position;
            auraCorrect.SetActive(true);
        }
    }
}