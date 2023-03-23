using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D myRB2D;
    public float moveSpeed;

    private Animator _animator;
    
    // Start is called before the first frame update
    void Start()
    {
        myRB2D = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
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

    }
}
