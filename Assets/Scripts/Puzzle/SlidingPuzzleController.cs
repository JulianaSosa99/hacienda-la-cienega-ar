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

    //Cuando se hace click en un tile
    public void TryMoveTile(Tile tile)
    {
        if (isGameCompleted) return;

        int tileIndex = tile.transform.GetSiblingIndex();
        int emptyIndex = GetEmptyIndex();

        if (IsAdjacent(tileIndex, emptyIndex))
        {
            Swap(tileIndex, emptyIndex);

            PlayMoveSound();

            if (IsSolved())
            {
                UnityEngine.Debug.Log("PUZZLE COMPLETADO!!");
                OnPuzzleSolved();
            }
        }
        else
        {
            UnityEngine.Debug.Log("Movimiento inválido");
            if (invalidMoveSound != null)
                audioSource.PlayOneShot(invalidMoveSound);
        }
    }

    //Verifica si dos índices son vecinos en la grilla
    bool IsAdjacent(int a, int b)
    {
        int ax = a % size;
        int ay = a / size;

        int bx = b % size;
        int by = b / size;

        int dx = Mathf.Abs(ax - bx);
        int dy = Mathf.Abs(ay - by);

        return (dx + dy == 1);
    }

    //Encuentra el tile vacío
    int GetEmptyIndex()
    {
        for (int i = 0; i < board.childCount; i++)
        {
            Tile t = board.GetChild(i).GetComponent<Tile>();

            if (t == null)
            {
                UnityEngine.Debug.Log("Tile sin script en índice: " + i);
                continue;
            }

            if (t.number == 0)
                return i;
        }

        UnityEngine.Debug.Log("No se encontró tile vacío");
        return -1;
    }

    //Intercambia posiciones en la jerarquía
    
    void Swap(int a, int b)
    {
        if (a < 0 || b < 0 || a >= board.childCount || b >= board.childCount)
        {
            UnityEngine.Debug.Log($"Swap inválido: {a}, {b}");
            return;
        }

        Transform tileA = board.GetChild(a);
        Transform tileB = board.GetChild(b);

        tileA.SetSiblingIndex(b);
        tileB.SetSiblingIndex(a);
    }

    //Mezcla el puzzle
    void Shuffle(int moves)
    {
        for (int i = 0; i < moves; i++)
        {
            int empty = GetEmptyIndex();

            if (empty == -1)
            {
                UnityEngine.Debug.Log("Shuffle detenido: no se encontró tile vacío");
                return;
            }

            List<int> neighbors = GetNeighbors(empty);

            if (neighbors.Count == 0)
            {
                UnityEngine.Debug.Log("No hay vecinos para mover");
                return;
            }

            int randomIndex = neighbors[Random.Range(0, neighbors.Count)];

            Swap(randomIndex, empty);
        }
    }

    //Obtiene índices vecinos válidos
    List<int> GetNeighbors(int index)
    {
        List<int> neighbors = new List<int>();

        int x = index % size;
        int y = index / size;

        if (x > 0) neighbors.Add(index - 1);
        if (x < size - 1) neighbors.Add(index + 1);
        if (y > 0) neighbors.Add(index - size);
        if (y < size - 1) neighbors.Add(index + size);

        return neighbors;
    }

    bool IsSolved()
    {
        for (int i = 0; i < board.childCount; i++)
        {
            Tile tile = board.GetChild(i).GetComponent<Tile>();

            // el último debe ser el vacío (0)
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

    //    if (winSound != null)
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
        if (winSound != null)
        {
            audioSource.PlayOneShot(winSound);

            yield return new WaitForSeconds(winSound.length);
        }

        FindObjectOfType<AdolfoNarrativeManager>().StartFinalNarrative();
    }

    void PlayMoveSound()
    {
        if (moveSound != null)
            audioSource.PlayOneShot(moveSound);
    }
}