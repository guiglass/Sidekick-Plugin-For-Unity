//------------------------------------------------------------------------------
// Written by Animation Prep Studio
// www.mocapfusion.com
//------------------------------------------------------------------------------
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class DragonController : MonoBehaviour
{
    private static DragonController _instance;
    public static DragonController Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
        
        attackParticles.enableEmission = false;
        attackBelchParticles.enableEmission = false;

        InvokeRepeating("Healing", 1f, 1f);
        
    }
    
    public Text startText;
    public Text scoreText;
    public Text healthText;

    public Canvas canvas;

    public Transform attackTarget;
    
    public SkinnedMeshRenderer sourceRenderer;
    public ParticleSystem attackParticles;
    public ParticleSystem attackBelchParticles;
    public AudioSource attackAudio;
    public AudioSource attackBelchAudio;
    public AudioSource deathAudio;
    public Animator dragonAnimator;

    public int attackBlendshapeIndex = 0;
    public float attackBlendshapeValue = 75f;

    public int attackBelchBlendshapeIndex = 2;
    public float attackBelchBlendshapeValue = 75f;
    
    private bool m_isDead = false;

    
    private Coroutine attackRoutine;
    private Coroutine attackBelchRoutine;
    
    void Update()
    {
        if (attackRoutine == null)
            if (sourceRenderer.GetBlendShapeWeight(attackBlendshapeIndex) >= attackBlendshapeValue)
                attackRoutine = StartCoroutine(AttackRoutine());        
        
        if (attackBelchRoutine == null)
            if (sourceRenderer.GetBlendShapeWeight(attackBelchBlendshapeIndex) >= attackBelchBlendshapeValue)
                attackBelchRoutine = StartCoroutine(AttackBelchRoutine());

        m_belchMultiplier -= Time.deltaTime * 0.25f; //decay
        m_belchMultiplier = Mathf.Clamp01(m_belchMultiplier);
    }

    IEnumerator AttackRoutine()
    {
        if (m_isDead)
            yield break;

        attackAudio.volume = 1;
        attackAudio.Play();
        attackAudio.pitch = Random.Range(0.9f, 1.1f);

        attackParticles.enableEmission = true;
        var yourParticleMain = attackParticles.main;

        float time = 0;
        float shapeWeight = sourceRenderer.GetBlendShapeWeight(attackBlendshapeIndex);
        while (shapeWeight > 10 && time <= 1)
        {
            shapeWeight = sourceRenderer.GetBlendShapeWeight(attackBlendshapeIndex);
            
            attackAudio.volume = shapeWeight / 100f;

            yourParticleMain.startSize = Mathf.Lerp(0.1f, 0.5f, m_belchMultiplier);
            yield return null;

            time += Time.deltaTime;
        }
        
        attackParticles.enableEmission = false;


        while (attackAudio.volume > 0)
        {
            attackAudio.volume -= Time.deltaTime;
            yield return null;
        }
        
        yield return new WaitUntil(() => sourceRenderer.GetBlendShapeWeight(attackBlendshapeIndex) <= 10);

        
        attackRoutine = null;
    }

    public float m_belchMultiplier = 0;
    
    IEnumerator AttackBelchRoutine()
    {
        if (m_isDead)
            yield break;
        
        attackBelchAudio.Play();
        attackBelchAudio.pitch = Random.Range(0.9f, 1.1f);
        
        attackBelchParticles.time = 0;
        attackBelchParticles.enableEmission = true;

        m_belchMultiplier = 0;
        
        float time = 0;
        float shapeWeight = sourceRenderer.GetBlendShapeWeight(attackBelchBlendshapeIndex);
        while (shapeWeight > 10 && time <= 1)
        {
            shapeWeight = sourceRenderer.GetBlendShapeWeight(attackBelchBlendshapeIndex);
            var shapeWeightNormalized = shapeWeight / 100f;
            attackBelchAudio.volume = shapeWeightNormalized;

            m_belchMultiplier += Time.deltaTime;
            m_belchMultiplier = Mathf.Clamp01(m_belchMultiplier);
            
            yield return null;

            time += Time.deltaTime;
        }
        
        
        attackBelchParticles.enableEmission = false;

        while (attackBelchAudio.volume > 0)
        {
            attackBelchAudio.volume -= Time.deltaTime;
            yield return null;
        }

        
        yield return new WaitUntil(() => sourceRenderer.GetBlendShapeWeight(attackBelchBlendshapeIndex) <= 10);
        
        attackBelchRoutine = null;
    }

    public void Death()
    {
        if (m_isDead)
            return;

        m_isDead = true;

        deathAudio.Play();
        
        dragonAnimator.SetTrigger("Death");

        Invoke("Reset", 5);
        
        startText.text = "Try Again";

    }

    public void Reset()
    {
        m_isDead = false;
        
        m_health = 100;
        UpdateHealthText();
        
        m_score = 0;
        UpdateScoreText();
        
        dragonAnimator.SetTrigger("Revive");
       
        DroneEmitter.Instance.Reset();
        
        canvas.GetComponent<Animator>().Rebind();
        canvas.GetComponent<Animator>().Update(0f);
        
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }
    
    private int m_score = 0;
    private int m_health = 100;

    public void AddScore(int amount = 1)
    {
        m_score += amount;
        
        UpdateScoreText();
    }
    
    public void ReceiveDamage(int amount)
    {
        if (m_isDead)
            return;
        
        m_health -= amount;
        m_health = Math.Max(0, m_health);
        
        UpdateHealthText();
        
        if (m_health == 0)
            Death();
    }
    
    void Healing()
    {
        if (m_isDead)
            return;

        m_health += 1;
        m_health = Mathf.Min(100, m_health);

        UpdateHealthText();
    }

    void UpdateHealthText()
    {
        healthText.text = String.Format("Health {0}%", m_health);
    }
    
    void UpdateScoreText()
    {
        scoreText.text = String.Format("Score: {0}", m_score);
    }
}
