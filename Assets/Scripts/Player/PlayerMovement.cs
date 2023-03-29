using DefaultNamespace;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D myRB2D;
    public float moveSpeed;

    public float power;
    public float maxPower;
    
    private Animator _animator;

    public Light2D powerLight;

    public bool visible = false;
    
    public bool[] artifacts = {false, false, false, false};

    public GameObject deathScreen;
    
    // Start is called before the first frame update
    void Start()
    {
        myRB2D = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();

        //powerLight = Camera.current.GetComponentInChildren<Light2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (visible)
        {
            power -= Time.deltaTime;
            _animator.SetBool("dashing", true);
        }
        else if (power < maxPower)
        {
            power += Time.deltaTime / 2;
            _animator.SetBool("dashing", false);
        }
        else
        {
            _animator.SetBool("dashing", false);
        }
        
        Vector2 velocity = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * moveSpeed;
        myRB2D.velocity = velocity;
        if (velocity.normalized.sqrMagnitude > 0.1)
        {
            _animator.SetBool("walking", true);
            _animator.SetFloat("X", velocity.normalized.x);
            _animator.SetFloat("Y", velocity.normalized.y);
        }
        else
        {
            _animator.SetBool("walking", false);
        }

        powerLight.pointLightInnerAngle = power / maxPower * 40;
        powerLight.pointLightOuterAngle = power / maxPower * 40;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        Artifact artifact = col.gameObject.GetComponent<Artifact>();
        if (artifact != null)
        {
            artifacts[(int) artifact.type] = true;
            Destroy(artifact.gameObject, 0.5F);
            _animator.SetFloat("grabbing", 1);
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        Pathfinder pathfinder = col.gameObject.GetComponent<Pathfinder>();
        if (pathfinder != null)
        {
            SceneManager.LoadScene("PlayScene");
        }
    }
}
