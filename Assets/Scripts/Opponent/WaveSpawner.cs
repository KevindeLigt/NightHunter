using UnityEngine;
using System.Collections;

public class WaveSpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float timeBetweenWaves = 10f;
    [SerializeField] private int startCount = 5;
    [SerializeField] private int addPerWave = 2;

    int wave = 0;

    void Start() => StartCoroutine(Loop());

    IEnumerator Loop()
    {
        var wait = new WaitForSeconds(timeBetweenWaves);
        while (true)
        {
            wave++;
            int count = startCount + (wave - 1) * addPerWave;

            for (int i = 0; i < count; i++)
            {
                var sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
                var e = Instantiate(enemyPrefab, sp.position, sp.rotation);
                yield return new WaitForSeconds(0.15f); // small stagger
            }

            yield return wait;
        }
    }
}
