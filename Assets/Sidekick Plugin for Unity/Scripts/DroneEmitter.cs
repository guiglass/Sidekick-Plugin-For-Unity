//------------------------------------------------------------------------------
// Written by Animation Prep Studio
// www.mocapfusion.com
//------------------------------------------------------------------------------
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class DroneEmitter : MonoBehaviour
{
    private static DroneEmitter _instance;
    public static DroneEmitter Instance { get { return _instance; } }
    
    public GameObject dronePrefab;

    public Text levelText;
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    private float m_startTime = 0;

    float timeSinceGameStart
    {
        get { return Time.timeSinceLevelLoad - m_startTime; }
    }
    
    public void Start()
    {
        Reset();

        StartCoroutine(SpawnEnemy());
    }
    
    public void Reset()
    {
        m_startTime = Time.timeSinceLevelLoad;
        
        foreach (Transform child in transform)
            GameObject.Destroy(child.gameObject);
        
        levelText.text = "Level: 1";
    }
    
    IEnumerator SpawnEnemy()
    {
        while (true)
        {
            if (timeSinceGameStart < 10)
            {
                levelText.text = "Level: 1";
                yield return new WaitForSeconds(Random.Range(1.5f, 10.0f));
            }
            else if (timeSinceGameStart < 30)
            {
                levelText.text = "Level: 2";
                yield return new WaitForSeconds(Random.Range(1.0f, 9.0f));
            }
            else if (timeSinceGameStart < 60)
            {
                levelText.text = "Level: 3";
                yield return new WaitForSeconds(Random.Range(1.0f, 8.0f));
            }
            else if (timeSinceGameStart < 90)
            {
                levelText.text = "Level: 4";
                yield return new WaitForSeconds(Random.Range(2.0f, 15.0f));
            }
            else if (timeSinceGameStart < 120)
            {
                levelText.text = "Level: 5";
                yield return new WaitForSeconds(Random.Range(1.0f, 6.0f));
            }
            else if (timeSinceGameStart < 180)
            {
                levelText.text = "Level: 6";
                yield return new WaitForSeconds(Random.Range(2.0f, 15.0f));
            }
            else if (timeSinceGameStart < 240)
            {
                levelText.text = "Level: 7";
                yield return new WaitForSeconds(Random.Range(1.0f, 4.0f));
            }
            else if (timeSinceGameStart < 300)
            {
                levelText.text = "Level: 8";
                yield return new WaitForSeconds(Random.Range(2.0f, 15.0f));
            }
            else
            {
                levelText.text = "Level: 9";
                yield return new WaitForSeconds(Random.Range(1.0f, 2.0f));
            }

            var newDrone = Instantiate(dronePrefab, transform);
            newDrone.transform.localPosition = new Vector3(Random.Range(-50, 50), Random.Range(0, 50), Random.Range(50, 0));
        }
    }
}
