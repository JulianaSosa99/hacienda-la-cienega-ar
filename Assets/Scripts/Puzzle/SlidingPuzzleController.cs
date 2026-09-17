using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class SlidingPuzzleController : MonoBehaviour
{
    public int size = 3;
    public Transform board;

    public AudioSource audioSource;
    public AudioClip moveSound;
    public AudioClip winSound;
    public AudioClip invalidMoveSound;

    private bool isGameCompleted = false;

    void Start()
    {
        InitializeBoard();
        Shuffle(15);
    }

    //Asigna el controller a cada tile
    void InitializeBoard()
    {
        foreach (Transform child in board)
        {
            Tile tile = child.GetComponent<Tile>();

            if (tile == null)
            {
                UnityEngine.Debug.Log("Objeto sin Tile: " + child.name);
                continue;
            }

            tile.controller = this;
        }
    }

    // Direction uses board coordinates: right is +x, down is +y.
    public void TryMoveTile(Tile tile, Vector2Int direction)
    {
        if (isGameCompleted || tile == null || tile.transform.parent != board) return;
        if (size <= 0 || Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1) return;

        int tileIndex = tile.transform.GetSiblingIndex();
        int x = tileIndex % size + direction.x;
        int y = tileIndex / size + direction.y;
        int targetIndex = y * size + x;
        if (x < 0 || x >= size || y < 0 || targetIndex >= board.childCount)
        {
            PlayInvalidMoveSound();
            return;
        }

        Swap(tileIndex, targetIndex);
        PlayMoveSound();
        if (IsSolved()) OnPuzzleSolved();
    }

    public void PlayInvalidMoveSound()
    {
        if (!isGameCompleted && audioSource != null && invalidMoveSound != null)
            audioSource.PlayOneShot(invalidMoveSound);
    }
    //Intercambia posiciones en la jerarqu�a
    
    void Swap(int a, int b)
    {
        if (a < 0 || b < 0 || a >= board.childCount || b >= board.childCount)
        {
            UnityEngine.Debug.Log($"Swap inv�lido: {a}, {b}");
            return;
        }

        // Move the lower index first to preserve all intermediate siblings.
        if (a > b)
        {
            int previousA = a;
            a = b;
            b = previousA;
        }

        Transform tileA = board.GetChild(a);
        Transform tileB = board.GetChild(b);

        tileA.SetSiblingIndex(b);
        tileB.SetSiblingIndex(a);
    }

    //Mezcla el puzzle
    void Shuffle(int moves)
    {
        if (size <= 0 || board.childCount < 2) return;

        for (int i = 0; i < moves; i++)
        {
            int origin = Random.Range(0, board.childCount);
            List<int> neighbors = GetNeighbors(origin);
            if (neighbors.Count == 0) continue;
            Swap(origin, neighbors[Random.Range(0, neighbors.Count)]);
        }

        // Never start with an already completed board.
        if (IsSolved()) Swap(0, 1);
    }
    //Obtiene �ndices vecinos v�lidos
    List<int> GetNeighbors(int index)
    {
        List<int> neighbors = new List<int>();

        int x = index % size;
        int y = index / size;

        if (x > 0) neighbors.Add(index - 1);
        if (x < size - 1 && index + 1 < board.childCount) neighbors.Add(index + 1);
        if (y > 0) neighbors.Add(index - size);
        if (index + size < board.childCount) neighbors.Add(index + size);

        return neighbors;
    }

    bool IsSolved()
    {
        for (int i = 0; i < board.childCount; i++)
        {
            Tile tile = board.GetChild(i).GetComponent<Tile>();

            // el �ltimo debe ser el vac�o (0)
            if (i == board.childCount - 1)
            {
                if (tile.number != 0)
                    return false;
            }
            else
            {
                if (tile.number != i + 1)
                    return false;
            }
        }

        return true;
    }

    //void OnPuzzleSolved()
    //{
    //    isGameCompleted = true;
    //    UnityEngine.Debug.Log("Juego terminado");

    //    if (audioSource != null && winSound != null)
    //        audioSource.PlayOneShot(winSound);

    //    FindObjectOfType<AdolfoNarrativeManager>().StartFinalNarrative();
    //}

    void OnPuzzleSolved()
    {
        isGameCompleted = true;

        UnityEngine.Debug.Log("Juego terminado");

        StartCoroutine(PuzzleSolvedSequence());
    }

    IEnumerator PuzzleSolvedSequence()
    {
        if (audioSource != null && winSound != null)
        {
            audioSource.PlayOneShot(winSound);

            yield return new WaitForSeconds(winSound.length);
        }

        FindObjectOfType<AdolfoNarrativeManager>().StartFinalNarrative();
    }

    void PlayMoveSound()
    {
        if (audioSource != null && moveSound != null)
            audioSource.PlayOneShot(moveSound);
    }
}